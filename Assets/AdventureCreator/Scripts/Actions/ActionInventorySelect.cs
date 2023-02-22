/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInventorySelect.cs"
 * 
 *	This action is used to automatically-select an inventory item.
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
	public class ActionInventorySelect : Action, IItemReferencerAction
	{

		public enum InventorySelectType { SelectItem, DeselectActive };
		public InventorySelectType selectType = InventorySelectType.SelectItem;
		public SelectItemMode selectItemMode = SelectItemMode.Use;

		public CarryCondition carryCondition = CarryCondition.OnlyIfCarrying;
		public enum CarryCondition { OnlyIfCarrying, AddIfNotCarrying, IgnoreCurrentInventory };

		/** Deprecated */
		public bool giveToPlayer = false;

		public bool setAmount = false;
		public int amountParameterID = -1;
		public int amount = 1;

		public int parameterID = -1;
		public int invID;
		protected int invNumber;

		#if UNITY_EDITOR
		private InventoryManager inventoryManager;
		private SettingsManager settingsManager;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Inventory; }}
		public override string Title { get { return "Select"; }}
		public override string Description { get { return "Selects a chosen inventory item, as though the player clicked on it in the Inventory menu. Will optionally add the specified item to the inventory if it is not currently held."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, parameterID, invID);
			amount = AssignInteger (parameters, amountParameterID, amount);
		}
		

		public override void Upgrade ()
		{
			if (giveToPlayer)
			{
				giveToPlayer = false;
				carryCondition = CarryCondition.AddIfNotCarrying;
			}

			base.Upgrade ();
		}

		
		public override float Run ()
		{
			if (KickStarter.runtimeInventory == null)
			{
				return 0f;
			}

			if (!setAmount)
			{
				amount = 1;
			}

			if (selectType == InventorySelectType.DeselectActive)
			{
				KickStarter.runtimeInventory.SetNull ();
			}
			else
			{
				if (!KickStarter.settingsManager.CanSelectItems (true))
				{
					return 0f;
				}

				InvItem originalItem = KickStarter.inventoryManager.GetItem (invID);
				if (originalItem == null) return 0;
				int maxAmount = originalItem.maxCount;

				if (setAmount && originalItem.canCarryMultiple)
				{
					int runtimeAmount = Mathf.Min (maxAmount, amount);

					switch (carryCondition)
					{
						case CarryCondition.OnlyIfCarrying:
							if (KickStarter.runtimeInventory.PlayerInvCollection.Contains (invID))
							{
								InvInstance validInstance = GetValidInstance (invID, runtimeAmount, false);
								if (InvInstance.IsValid (validInstance))
								{
									validInstance.Select (selectItemMode);
									validInstance.TransferCount = runtimeAmount;
								}
							}
							break;

						case CarryCondition.AddIfNotCarrying:
							if (!KickStarter.runtimeInventory.PlayerInvCollection.Contains (invID))
							{
								InvInstance newInstance = KickStarter.runtimeInventory.PlayerInvCollection.AddToEnd (new InvInstance (invID, runtimeAmount));
								newInstance.Select (selectItemMode);
								newInstance.TransferCount = runtimeAmount;
							}
							else
							{
								InvInstance validInstance = GetValidInstance (invID, runtimeAmount, true);
								if (InvInstance.IsValid (validInstance))
								{
									validInstance.Select (selectItemMode);
									validInstance.TransferCount = runtimeAmount;
								}
							}
							break;

						case CarryCondition.IgnoreCurrentInventory:
							{
								InvInstance newInstance = new InvInstance (originalItem, runtimeAmount);
								KickStarter.runtimeInventory.SelectItem (newInstance, selectItemMode);
								newInstance.TransferCount = newInstance.Count;
							}
							break;

						default:
							break;
					}
				}
				else
				{
					switch (carryCondition)
					{
						case CarryCondition.OnlyIfCarrying:
							if (KickStarter.runtimeInventory.PlayerInvCollection.Contains (invID))
							{
								KickStarter.runtimeInventory.SelectItemByID (invID, selectItemMode);
							}
							break;

						case CarryCondition.AddIfNotCarrying:
							if (!KickStarter.runtimeInventory.PlayerInvCollection.Contains (invID))
							{
								InvInstance newInstance = KickStarter.runtimeInventory.PlayerInvCollection.AddToEnd (new InvInstance (invID, 1));
								KickStarter.runtimeInventory.SelectItem (newInstance);
							}
							else
							{
								KickStarter.runtimeInventory.SelectItemByID (invID, selectItemMode);
							}
							break;

						case CarryCondition.IgnoreCurrentInventory:
							KickStarter.runtimeInventory.SelectItemByID (invID, selectItemMode, true);
							break;

						default:
							break;
					}
				}
			}
			
			return 0f;
		}


		private InvInstance GetValidInstance (int invID, int amount, bool canCreate)
		{
			InvInstance[] allInstances = KickStarter.runtimeInventory.PlayerInvCollection.GetAllInstances (invID);
			foreach (InvInstance allInstance in allInstances)
			{
				if (allInstance.Count >= amount)
				{ 
					return allInstance;
				}
			}

			if (canCreate && allInstances.Length > 0)
			{
				allInstances[0].Count = amount;
				return allInstances[0];
			}
			return null;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			selectType = (InventorySelectType) EditorGUILayout.EnumPopup ("Select type:", selectType);
			if (selectType == InventorySelectType.DeselectActive)
			{
				return;
			}

			inventoryManager = AdvGame.GetReferences ().inventoryManager;
			settingsManager = AdvGame.GetReferences ().settingsManager;
			
			if (inventoryManager)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
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
						invNumber = 0;
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

					if (parameterID >= 0 || (invNumber >= 0 && invNumber < inventoryManager.items.Count && inventoryManager.items[invNumber].canCarryMultiple))
					{
						setAmount = EditorGUILayout.Toggle ("Set amount?", setAmount);

						if (setAmount)
						{
							amountParameterID = Action.ChooseParameterGUI ("Amount to select:", parameters, amountParameterID, ParameterType.Integer);
							if (amountParameterID < 0)
							{
								amount = EditorGUILayout.IntField ("Amount to select:", amount);
							}
						}
					}

					carryCondition = (CarryCondition) EditorGUILayout.EnumPopup ("Carry condition:", carryCondition);

					if (settingsManager && settingsManager.CanGiveItems ())
					{
						selectItemMode = (SelectItemMode) EditorGUILayout.EnumPopup ("Select item mode:", selectItemMode);
					}

				}
				else
				{
					EditorGUILayout.HelpBox ("No inventory items exist!", MessageType.Info);
					invID = -1;
					invNumber = -1;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be assigned in the AC Game Editor window!", MessageType.Warning);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (selectType == InventorySelectType.DeselectActive)
			{
				return "Deselect active";
			}

			if (inventoryManager)
			{
				return inventoryManager.GetLabel (invID);
			}
			return string.Empty;
		}


		public int GetNumItemReferences (int _itemID, List<ActionParameter> parameters)
		{
			if (selectType == InventorySelectType.SelectItem && parameterID < 0 && invID == _itemID)
			{
				return 1;
			}
			return 0;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> parameters)
		{
			if (selectType == InventorySelectType.SelectItem && parameterID < 0 && invID == oldItemID)
			{
				invID = newItemID;
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Select' Action, set to select a specific inventory item</summary>
		 * <param name = "itemID">The ID number of the inventory item to select</param>
		 * <param name = "addIfNotCarrying">If True, the item will be added to the player's inventory</param>
		 * <param name = "selectItemMode">The 'select mode' to be in (Use, Give), if supported</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySelect CreateNew_Select (int itemID, bool addIfNotCarrying = false, SelectItemMode selectItemMode = SelectItemMode.Use)
		{
			ActionInventorySelect newAction = CreateNew<ActionInventorySelect> ();
			newAction.selectType = InventorySelectType.SelectItem;
			newAction.invID = itemID;
			newAction.giveToPlayer = addIfNotCarrying;
			newAction.selectItemMode = selectItemMode;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Select' Action, set to deselect the current inventory item</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySelect CreateNew_DeselectActive ()
		{
			ActionInventorySelect newAction = CreateNew<ActionInventorySelect> ();
			newAction.selectType = InventorySelectType.DeselectActive;
			return newAction;
		}

	}

}