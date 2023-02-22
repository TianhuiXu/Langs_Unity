/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInventorySet.cs"
 * 
 *	This action is used to add or remove items from the player's inventory, defined in the Inventory Manager.
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
	public class ActionInventorySet : Action, IItemReferencerAction
	{
		
		public InvAction invAction;

		public int parameterID = -1;
		public int invID;
		public int replaceParameterID = -1;
		public int invIDReplace;
		protected int invNumber;
		protected int replaceInvNumber;
		
		public bool setAmount = false;
		public int amountParameterID = -1;
		public int amount = 1;

		public bool setPlayer = false;
		public int playerID;

		public bool addToFront = false;
		public bool removeLast = false;

		#if UNITY_EDITOR
		private InventoryManager inventoryManager;
		private SettingsManager settingsManager;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Inventory; }}
		public override string Title { get { return "Add or remove"; }}
		public override string Description { get { return "Adds or removes an item from the Player's inventory. Items are defined in the Inventory Manager. If the player can carry multiple amounts of the item, more options will show."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			invID = AssignInvItemID (parameters, parameterID, invID);
			invIDReplace = AssignInvItemID (parameters, replaceParameterID, invIDReplace);
			amount = AssignInteger (parameters, amountParameterID, amount);
		}
		
		
		public override float Run ()
		{
			if (KickStarter.runtimeInventory)
			{
				if (!setAmount)
				{
					amount = 1;
				}

				int _playerID = -1;

				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && !KickStarter.settingsManager.shareInventory && setPlayer)
				{
					_playerID = playerID;
				}

				switch (invAction)
				{
					case InvAction.Add:
						InvItem linkedItem = KickStarter.inventoryManager.GetItem (invID);
						if (linkedItem == null) return 0f;

						int maxAmount = (linkedItem.canCarryMultiple) ? linkedItem.maxCount : -1;
						if (setAmount && maxAmount > 0 && amount > maxAmount)
						{
							int localAmount = amount;
							while (localAmount > maxAmount)
							{
								AddItem (_playerID, maxAmount);
								localAmount -= maxAmount;
							}
							if (localAmount > 0)
							{
								AddItem (_playerID, localAmount);
							}
						}
						else if (linkedItem.canCarryMultiple && setAmount)
						{
							AddItem (_playerID, amount);
						}
						else
						{
							AddItem (_playerID, 1);
						}
						break;

					case InvAction.Remove:
						if (removeLast)
						{
							InvInstance invInstance = KickStarter.runtimeInventory.LastSelectedInstance;
							if (InvInstance.IsValid (invInstance))
							{
								if (setAmount && invInstance.InvItem.canCarryMultiple && invInstance.Count > amount)
								{
									invInstance.Count -= amount;
								}
								else
								{
									KickStarter.runtimeInventory.SetNull ();
									KickStarter.runtimeInventory.PlayerInvCollection.Delete (invInstance);
								}
							}
						}
						else
						{
							if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.runtimeInventory.SelectedInstance.ItemID == invID)
							{
								KickStarter.runtimeInventory.SetNull ();
							}

							if (_playerID >= 0)
							{
								if (setAmount)
								{
									KickStarter.runtimeInventory.RemoveFromOtherPlayer (invID, amount, _playerID);
								}
								else
								{
									KickStarter.runtimeInventory.RemoveFromOtherPlayer (invID, _playerID);
								}
							}
							else
							{
								if (setAmount)
								{
									KickStarter.runtimeInventory.PlayerInvCollection.Delete (invID, amount);
								}
								else
								{
									KickStarter.runtimeInventory.PlayerInvCollection.DeleteAllOfType (invID);
								}
							}
						}
						break;

					case InvAction.Replace:
						if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.runtimeInventory.SelectedInstance.ItemID == invIDReplace)
						{
							KickStarter.runtimeInventory.SetNull ();
						}
						KickStarter.runtimeInventory.Replace (invID, invIDReplace, amount);
						break;
				}
			}
			
			return 0f;
		}


		private void AddItem (int _playerID, int _amount)
		{
			if (_playerID >= 0 && KickStarter.saveSystem.CurrentPlayerID != _playerID)
			{
				KickStarter.runtimeInventory.AddToOtherPlayer (new InvInstance (invID, _amount), _playerID, addToFront);
			}
			else
			{
				if (addToFront)
				{
					KickStarter.runtimeInventory.PlayerInvCollection.Insert (new InvInstance (invID, _amount), 0);
				}
				else
				{
					KickStarter.runtimeInventory.PlayerInvCollection.Add (new InvInstance (invID, _amount));
				}
			}
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (inventoryManager == null && AdvGame.GetReferences ().inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}
			if (settingsManager == null && AdvGame.GetReferences ().settingsManager)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (inventoryManager != null)
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
						
						// If a item has been removed, make sure selected variable is still valid
						if (_item.id == invID)
						{
							invNumber = i;
						}
						if (_item.id == invIDReplace)
						{
							replaceInvNumber = i;
						}
						
						i++;
					}
					
					if (invNumber == -1)
					{
						if (invID != 0)
						{
							LogWarning ("Previously chosen item no longer exists!");
						}
						invNumber = 0;
						invID = 0;
					}

					if (invAction == InvAction.Replace && replaceInvNumber == -1)
					{
						if (invIDReplace != 0)
						{
							LogWarning ("Previously chosen item no longer exists!");
						}
						replaceInvNumber = 0;
						invIDReplace = 0;
					}

					invAction = (InvAction) EditorGUILayout.EnumPopup ("Method:", invAction);

					string label = "Item to add:";
					if (invAction == InvAction.Remove)
					{
						label = "Item to remove:";

						removeLast = EditorGUILayout.Toggle ("Remove last-selected?", removeLast);
					}

					if (invAction != InvAction.Remove || !removeLast)
					{
						parameterID = Action.ChooseParameterGUI (label, parameters, parameterID, ParameterType.InventoryItem);
						if (parameterID >= 0)
						{
							invNumber = Mathf.Min (invNumber, inventoryManager.items.Count-1);
							invID = -1;
						}
						else
						{
							invNumber = EditorGUILayout.Popup (label, invNumber, labelList.ToArray());
							invID = inventoryManager.items[invNumber].id;
						}
					}

					if ((invAction == InvAction.Remove && removeLast) || inventoryManager.items[invNumber].canCarryMultiple)
					{
						setAmount = EditorGUILayout.Toggle ("Set amount?", setAmount);
					
						if (setAmount)
						{
							string _label = (invAction == InvAction.Remove) ? "Reduce count by:" : "Increase count by:";

							amountParameterID = Action.ChooseParameterGUI (_label, parameters, amountParameterID, ParameterType.Integer);
							if (amountParameterID < 0)
							{
								amount = EditorGUILayout.IntField (_label, amount);
							}
						}
					}

					if (invAction == InvAction.Replace)
					{
						replaceParameterID = Action.ChooseParameterGUI ("Item to remove:", parameters, replaceParameterID, ParameterType.InventoryItem);
						if (replaceParameterID >= 0)
						{
							replaceInvNumber = Mathf.Min (replaceInvNumber, inventoryManager.items.Count-1);
							invIDReplace = -1;
						}
						else
						{
							replaceInvNumber = EditorGUILayout.Popup ("Item to remove:", replaceInvNumber, labelList.ToArray());
							invIDReplace = inventoryManager.items[replaceInvNumber].id;
						}
					}
					else if (invAction == InvAction.Add)
					{
						addToFront = EditorGUILayout.Toggle ("Add to front?", addToFront);
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No inventory items exist!", MessageType.Info);
					invID = -1;
					invNumber = -1;
					invIDReplace = -1;
					replaceInvNumber = -1;
				}

				if (settingsManager != null && settingsManager.playerSwitching == PlayerSwitching.Allow && !settingsManager.shareInventory && invAction != InvAction.Replace)
				{
					EditorGUILayout.Space ();

					setPlayer = EditorGUILayout.Toggle ("Affect specific player?", setPlayer);
					if (setPlayer)
					{
						ChoosePlayerGUI ();
					}
				}
				else
				{
					setPlayer = false;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be assigned for this Action to work", MessageType.Warning);
			}
		}
		
		
		public override string SetLabel ()
		{
			string labelItem = string.Empty;

			if (!inventoryManager)
			{
				inventoryManager = AdvGame.GetReferences ().inventoryManager;
			}

			if (inventoryManager)
			{
				labelItem = " " + inventoryManager.GetLabel (invID);
			}
			
			if (invAction == InvAction.Remove)
			{
				return "Remove" + labelItem;
			}
			else
			{
				return "Add" + labelItem;
			}
		}


		private void ChoosePlayerGUI ()
		{
			List<string> labelList = new List<string>();
			int i = 0;
			int playerNumber = -1;

			if (settingsManager.players.Count > 0)
			{
				foreach (PlayerPrefab playerPrefab in settingsManager.players)
				{
					if (playerPrefab.playerOb != null)
					{
						labelList.Add (playerPrefab.playerOb.name);
					}
					else
					{
						labelList.Add ("(Undefined prefab)");
					}
					
					// If a player has been removed, make sure selected player is still valid
					if (playerPrefab.ID == playerID)
					{
						playerNumber = i;
					}
					
					i++;
				}
				
				if (playerNumber == -1)
				{
					// Wasn't found (item was possibly deleted), so revert to zero
					if (playerID > 0) LogWarning ("Previously chosen Player no longer exists!");
					
					playerNumber = 0;
					playerID = 0;
				}

				string label = "Add to player:";
				if (invAction == InvAction.Remove)
				{
					label = "Remove from player:";
				}

				playerNumber = EditorGUILayout.Popup (label, playerNumber, labelList.ToArray());
				playerID = settingsManager.players[playerNumber].ID;
			}
		}


		public int GetNumItemReferences (int _itemID, List<ActionParameter> parameters)
		{
			int numFound = 0;

			if (parameterID < 0 && invID == _itemID)
			{
				numFound ++;
			}

			if (invAction == InvAction.Replace && replaceParameterID < 0 && invIDReplace == _itemID)
			{
				numFound ++;
			}

			return numFound;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> parameters)
		{
			int numFound = 0;

			if (parameterID < 0 && invID == oldItemID)
			{
				invID = newItemID;
				numFound++;
			}

			if (invAction == InvAction.Replace && replaceParameterID < 0 && invIDReplace == oldItemID)
			{
				invIDReplace = newItemID;
				numFound++;
			}

			return numFound;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Inventory: Add or remove' Action, set to add an item to the player's inventory</summary>
		 * <param name = "itemID">The ID number of the item to add</param>
		 * <param name = "addToFront">If True, the item will be added to the front of the inventory list</param>
		 * <param name = "amountToAdd">The number of items to add, if multiple instances are supported</param>
		 * <param name = "playerID">If non-negative, and player-switching is enabled, the ID number of the Player to add the item to</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySet CreateNew_Add (int itemID, bool addToFront = false, int amountToAdd = 1, int playerID = -1)
		{
			ActionInventorySet newAction = CreateNew<ActionInventorySet> ();
			newAction.invAction = InvAction.Add;
			newAction.invID = itemID;
			newAction.addToFront = addToFront;
			newAction.setAmount = amountToAdd > 1;
			newAction.amount = amountToAdd;
			newAction.playerID = playerID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Add or remove' Action, set to remove an item from the player's inventory</summary>
		 * <param name = "itemID">The ID number of the item to remove</param>
		 * <param name = "removeAllInstances">If True, all instances of the item will be removed</param>
		 * <param name = "amountToRemove">The number of instances of the item to remove, if removeAllInstances = False and multiple instances are supported</param>
		 * <param name = "playerID">If non-negative, and player-switching is enabled, the ID number of the Player to remove the item from</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySet CreateNew_Remove (int itemID, bool removeAllInstances = true, int amountToRemove = 1, int playerID = -1)
		{
			ActionInventorySet newAction = CreateNew<ActionInventorySet> ();
			newAction.invAction = InvAction.Remove;
			newAction.invID = itemID;
			newAction.setAmount = !removeAllInstances;
			newAction.amount = amountToRemove;
			newAction.playerID = playerID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Inventory: Add or remove' Action, set to replace an item to the player's inventory with another</summary>
		 * <param name = "itemIDToAdd">The ID number of the item to add</param>
		 * <param name = "itemIDToRemove">The ID number of the item to remove</param>
		 * <param name = "amountToAdd">The number of items to add, if multiple instances are supported</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInventorySet CreateNew_Replace (int itemIDToAdd, int itemIDToRemove, int amountToAdd = 1)
		{
			ActionInventorySet newAction = CreateNew<ActionInventorySet> ();
			newAction.invAction = InvAction.Replace;
			newAction.invID = itemIDToAdd;
			newAction.invIDReplace = itemIDToRemove;
			newAction.setAmount = amountToAdd > 1;
			newAction.amount = amountToAdd;
			return newAction;
		}

	}

}