/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInventoryInteraction.cs"
 * 
 *	This action is used to enable and disable inventory interactions.
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
	public class ActionInventoryInteraction : Action, IItemReferencerAction
	{

		public int parameterID = -1;
		public int invID;
		protected int invNumber;

		[SerializeField] protected InvInteractionType invInteractionType;
		protected enum InvInteractionType { Standard, Combine };

		public int interactionID;
		public ChangeType changeType = ChangeType.Enable;

		#if UNITY_EDITOR
		private InventoryManager inventoryManager;
		private SettingsManager settingsManager;
		private CursorManager cursorManager;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Inventory; }}
		public override string Title { get { return "Change interaction"; }}
		public override string Description { get { return "Enables or disables an Inventory interaction.  This will only affect items that are already present in the Player's inventory"; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, parameterID, invID);
		}
		

		public override float Run ()
		{
			if (KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Single && invInteractionType == InvInteractionType.Standard)
			{
				LogWarning ("Cannot change the state of Standard Inventory interactions unless 'Inventory interactions' is set to 'Multiple'.");
				return 0f;
			}

			InvInstance[] invInstances = KickStarter.runtimeInventory.PlayerInvCollection.GetAllInstances (invID);

			if (invInstances == null || invInstances.Length == 0)
			{
				LogWarning ("Cannot change the Interaction state of the Inventory Item " + invID + " because no instances of the item are currently held by the Player!");
				return 0f;
			}

			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;

				switch (invInteractionType)
				{
					case InvInteractionType.Standard:
						invInstance.SetInteractionState (interactionID, (changeType == ChangeType.Enable));
						break;

					case InvInteractionType.Combine:
						invInstance.SetCombineInteractionState (interactionID, (changeType == ChangeType.Enable));
						break;

					default:
						break;
				}
			}
			
			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			inventoryManager = AdvGame.GetReferences ().inventoryManager;
			settingsManager = AdvGame.GetReferences ().settingsManager;
			cursorManager = AdvGame.GetReferences ().cursorManager;

			if (inventoryManager && settingsManager && cursorManager)
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

					if (settingsManager.InventoryInteractions == InventoryInteractions.Single)
					{
						invInteractionType = InvInteractionType.Combine;
					}
					else
					{
						invInteractionType = (InvInteractionType) EditorGUILayout.EnumPopup ("Interaction type:", invInteractionType);
					}

					InvItem invItem = (parameterID < 0) ? inventoryManager.GetItem (invID) : null;

					if (invItem != null)
					{ 
						invItem.Upgrade ();
					}

					switch (invInteractionType)
					{
						case InvInteractionType.Standard:
							{
								if (invItem != null)
								{
									if (invItem.interactions == null || invItem.interactions.Count == 0)
									{
										EditorGUILayout.HelpBox ("No Standard Interactions defined for this item!", MessageType.Info);
										return;
									}

									int interactionIndex = 0;

									List<string> interactionLabelList = new List<string>();
									for (int j=0; j<invItem.interactions.Count; j++)
									{
										int iconID = invItem.interactions[j].icon.id;
										string cursorLabel = KickStarter.cursorManager.GetLabelFromID (iconID, 0);
										if (string.IsNullOrEmpty (cursorLabel)) cursorLabel = "Icon " + iconID.ToString ();
										interactionLabelList.Add (invItem.interactions[j].ID + ": " + cursorLabel);
										
										if (interactionID == invItem.interactions[j].ID)
										{
											interactionIndex = j;
										}
									}

									interactionIndex = EditorGUILayout.Popup ("Standard Interaction:", interactionIndex, interactionLabelList.ToArray ());
									interactionID = invItem.interactions[interactionIndex].ID;

									changeType = (ChangeType) EditorGUILayout.EnumPopup ("Change type:", changeType);
								}
								else if (parameterID >= 0)
								{
									interactionID = EditorGUILayout.IntField ("Standard Interaction ID:", interactionID);
									changeType = (ChangeType) EditorGUILayout.EnumPopup ("Change type:", changeType);
								}
							}
							break;

						case InvInteractionType.Combine:
							{
								if (invItem != null)
								{
									if (invItem.combineInteractions == null || invItem.combineInteractions.Count == 0)
									{
										EditorGUILayout.HelpBox ("No Combine Interactions defined for this item!", MessageType.Info);
										return;
									}

									int interactionIndex = 0;

									List<string> interactionLabelList = new List<string> ();
									for (int j = 0; j < invItem.combineInteractions.Count; j++)
									{
										int combineID = invItem.combineInteractions[j].combineID;
										string otherInvLabel = KickStarter.inventoryManager.GetLabel (combineID);
										if (string.IsNullOrEmpty (otherInvLabel)) otherInvLabel = "Item " + combineID.ToString ();
										interactionLabelList.Add (invItem.combineInteractions[j].ID + ": " + otherInvLabel);

										if (interactionID == invItem.combineInteractions[j].ID)
										{
											interactionIndex = j;
										}
									}

									if (interactionIndex >= interactionLabelList.Count) interactionIndex = interactionLabelList.Count - 1;

									interactionIndex = EditorGUILayout.Popup ("Combine Interaction:", interactionIndex, interactionLabelList.ToArray ());
									interactionID = invItem.combineInteractions[interactionIndex].ID;

									changeType = (ChangeType) EditorGUILayout.EnumPopup ("Change type:", changeType);
								}
								else if (parameterID >= 0)
								{
									interactionID = EditorGUILayout.IntField ("Combine Interaction ID:", interactionID);
									changeType = (ChangeType) EditorGUILayout.EnumPopup ("Change type:", changeType);
								}
							}
							break;

						default:
							break;
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
				EditorGUILayout.HelpBox ("Inventory, Settings and Cursor Managers must be assigned in the AC Game Editor window!", MessageType.Warning);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (inventoryManager)
			{
				return inventoryManager.GetLabel (invID);
			}
			return string.Empty;
		}


		public int GetNumItemReferences (int _itemID, List<ActionParameter> parameters)
		{
			if (parameterID < 0 && invID == _itemID)
			{
				return 1;
			}
			return 0;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> parameters)
		{
			if (parameterID < 0 && invID == oldItemID)
			{
				invID = newItemID;
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Change interaction' Action, set to affect a specific inventory item's Standard interaction</summary>
		 * <param name = "itemID">The ID number of the inventory item to affect</param>
		 * <param name = "interactionID">The ID of the Standard Interaction to change</param>
		 * <param name = "changeType">What kind of change to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryInteraction CreateNew_StandardInteraction (int itemID, int interactionID, ChangeType changeType)
		{
			ActionInventoryInteraction newAction = CreateNew<ActionInventoryInteraction> ();
			newAction.invID = itemID;
			newAction.invInteractionType = InvInteractionType.Standard;
			newAction.interactionID = interactionID;
			newAction.changeType = changeType;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Change interaction' Action, set to affect a specific inventory item's Combine interaction</summary>
		 * <param name = "itemID">The ID number of the inventory item to affect</param>
		 * <param name = "interactionID">The ID of the Combine Interaction to change</param>
		 * <param name = "changeType">What kind of change to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventoryInteraction CreateNew_CombineInteraction (int itemID, int interactionID, ChangeType changeType)
		{
			ActionInventoryInteraction newAction = CreateNew<ActionInventoryInteraction> ();
			newAction.invID = itemID;
			newAction.invInteractionType = InvInteractionType.Combine;
			newAction.interactionID = interactionID;
			newAction.changeType = changeType;
			return newAction;
		}

	}

}