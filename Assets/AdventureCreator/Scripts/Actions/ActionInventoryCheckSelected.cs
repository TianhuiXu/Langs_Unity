/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInventoryCheckSelected.cs"
 * 
 *	This action is used to check the currently-selected item.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionInventoryCheckSelected : ActionCheck, IItemReferencerAction
	{
		
		public int parameterID = -1;
		public int invID;
		public int binID;
		[SerializeField] private bool checkNothing = false; // Deprecated
		public bool includeLast = false;

		[SerializeField] protected SelectedCheckMethod selectedCheckMethod = SelectedCheckMethod.SpecificItem;
		public enum SelectedCheckMethod { SpecificItem, InSpecificCategory, NoneSelected };

		#if UNITY_EDITOR
		protected InventoryManager inventoryManager;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Inventory; }}
		public override string Title { get { return "Check selected"; }}
		public override string Description { get { return "Queries whether or not the chosen item, or no item, is currently selected."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, parameterID, invID);
		}
		
		
		public override bool CheckCondition ()
		{
			if (KickStarter.runtimeInventory)
			{
				switch (selectedCheckMethod)
				{
					case SelectedCheckMethod.NoneSelected:
						if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
						{
							return true;
						}
						break;

					case SelectedCheckMethod.SpecificItem:
						if (includeLast)
						{
							if (InvInstance.IsValid (KickStarter.runtimeInventory.LastSelectedInstance) && KickStarter.runtimeInventory.LastSelectedInstance.ItemID == invID)
							{
								return true;
							}
						}
						else
						{
							if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.runtimeInventory.SelectedInstance.ItemID == invID)
							{
								return true;
							}
						}
						break;

					case SelectedCheckMethod.InSpecificCategory:
						if (includeLast)
						{
							if (InvInstance.IsValid (KickStarter.runtimeInventory.LastSelectedInstance) && KickStarter.runtimeInventory.LastSelectedInstance.InvItem.binID == binID)
							{
								return true;
							}
						}
						else
						{
							if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.runtimeInventory.SelectedInstance.InvItem.binID == binID)
							{
								return true;
							}
						}
						break;
				}
			}
			return false;
		}


		public override void Upgrade ()
		{
			if (checkNothing)
			{
				selectedCheckMethod = SelectedCheckMethod.NoneSelected;
				checkNothing = false;
			}

			base.Upgrade ();
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (inventoryManager == null)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}

			selectedCheckMethod = (SelectedCheckMethod) EditorGUILayout.EnumPopup ("Check selected item is:", selectedCheckMethod);

			if (inventoryManager != null)
			{
				if (selectedCheckMethod == SelectedCheckMethod.InSpecificCategory)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					
					int i = 0;
					int binNumber = 0;
					if (parameterID == -1)
					{
						binNumber = -1;
					}
					
					if (inventoryManager.bins != null && inventoryManager.bins.Count > 0)
					{
						foreach (InvBin _bin in inventoryManager.bins)
						{
							labelList.Add (_bin.id.ToString () + ": " + _bin.label);
							
							// If a category has been removed, make sure selected is still valid
							if (_bin.id == binID)
							{
								binNumber = i;
							}
							
							i++;
						}
						
						if (binNumber == -1)
						{
							if (binID > 0) LogWarning ("Previously chosen category no longer exists!");
							binNumber = 0;
						}
						
						binNumber = EditorGUILayout.Popup ("Inventory category:", binNumber, labelList.ToArray());
						binID = inventoryManager.bins[binNumber].id;

						includeLast = EditorGUILayout.Toggle ("Include last-selected?", includeLast);
					}
					else
					{
						EditorGUILayout.HelpBox ("No inventory categories exist!", MessageType.Info);
						binID = -1;
					}
				}
				else if (selectedCheckMethod == SelectedCheckMethod.SpecificItem)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					
					int i = 0;
					int invNumber = 0;
					if (parameterID == -1)
					{
						invNumber = -1;
					}
					
					if (inventoryManager.items.Count > 0)
					{
						foreach (InvItem _item in inventoryManager.items)
						{
							labelList.Add (_item.label);
							
							// If an item has been removed, make sure selected variable is still valid
							if (_item.id == invID)
							{
								invNumber = i;
							}
							
							i++;
						}
						
						if (invNumber == -1)
						{
							if (invID > 0) LogWarning ("Previously chosen item no longer exists!");
							invID = 0;
						}
						
						parameterID = Action.ChooseParameterGUI ("Inventory item:", parameters, parameterID, ParameterType.InventoryItem);
						if (parameterID >= 0)
						{
							invNumber = Mathf.Min (invNumber, inventoryManager.items.Count-1);
							invID = -1;
						}
						else
						{
							invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray());
							invID = inventoryManager.items[invNumber].id;
						}

						includeLast = EditorGUILayout.Toggle ("Include last-selected?", includeLast);
					}
					else
					{
						EditorGUILayout.HelpBox ("No inventory items exist!", MessageType.Info);
						invID = -1;
					}
				}
			}

		}
		
		
		public override string SetLabel ()
		{
			switch (selectedCheckMethod)
			{
				case SelectedCheckMethod.NoneSelected:
					return "Nothing";

				case SelectedCheckMethod.SpecificItem:
					if (inventoryManager)
					{
						return inventoryManager.GetLabel (invID);
					}
					break;

				case SelectedCheckMethod.InSpecificCategory:
					if (inventoryManager)
					{
						InvBin category = inventoryManager.GetCategory (binID);
						if (category != null)
						{
							return category.label;
						}
					}
					break;
			}
			return string.Empty;
		}


		public int GetNumItemReferences (int _itemID, List<ActionParameter> actionParameters)
		{
			if (selectedCheckMethod == SelectedCheckMethod.SpecificItem && invID == _itemID)
			{
				return 1;
			}
			return 0;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> parameters)
		{
			if (selectedCheckMethod == SelectedCheckMethod.SpecificItem && invID == oldItemID)
			{
				invID = newItemID;
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Check selected' Action, set to check if a specific item is selected</summary>
		 * <param name = "itemID">The ID number of the item to check for</param>
		 * <param name = "includeLastSelected">If True, the query will return 'True' if the last-selected item matches the itemID, even if it is not currently selected</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCheckSelected CreateNew_SpecificItem (int itemID, bool includeLastSelected = false)
		{
			ActionInventoryCheckSelected newAction = CreateNew<ActionInventoryCheckSelected> ();
			newAction.selectedCheckMethod = SelectedCheckMethod.SpecificItem;
			newAction.invID = itemID;
			newAction.includeLast = includeLastSelected;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Check selected' Action, set to check the selected item is in a specific category</summary>
		 * <param name = "categoryID">The ID number of the category to check for</param>
		 * <param name = "includeLastSelected">If True, the query will return 'True' if the last-selected item matches the categoryID, even if it is not currently selected</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCheckSelected CreateNew_InSpecificCategory (int categoryID, bool includeLastSelected = false)
		{
			ActionInventoryCheckSelected newAction = CreateNew<ActionInventoryCheckSelected> ();
			newAction.selectedCheckMethod = SelectedCheckMethod.InSpecificCategory;
			newAction.binID = categoryID;
			newAction.includeLast = includeLastSelected;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Check selected' Action, set to check if no item is selected</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryCheckSelected CreateNew_NoneSelected ()
		{
			ActionInventoryCheckSelected newAction = CreateNew<ActionInventoryCheckSelected> ();
			newAction.selectedCheckMethod = SelectedCheckMethod.NoneSelected;
			return newAction;
		}

	}
	
}