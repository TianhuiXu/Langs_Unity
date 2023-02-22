/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RuntimeInventory.cs"
 * 
 *	This script creates a local copy of the InventoryManager's items.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component is where inventory items (see InvItem) are stored at runtime.
	 * When the player aquires an item, it is transferred here (into _localItems) from the InventoryManager asset.
	 * It should be placed on the PersistentEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_inventory.html")]
	public class RuntimeInventory : MonoBehaviour
	{

		#region Variables

		protected InvCollection playerInvCollection = new InvCollection ();

		protected List<IngredientCollection> ingredientCollections = new List<IngredientCollection> ();

		protected InvInstance selectedInstance = null;
		protected InvInstance lastSelectedInstance = null;
		protected InvInstance hoverInstance = null;
		protected InvInstance highlightInstance = null;
		protected bool showHoverLabel = true;
		
		protected InvInstance lastClickedInstance;

		protected GUIStyle countStyle = new GUIStyle ();
		protected TextEffects countTextEffects;
		
		protected HighlightState highlightState = HighlightState.None;
		protected float pulse = 0f;
		protected int pulseDirection = 0; // 0 = none, 1 = in, -1 = out

		#endregion


		#region UnityStandards

		protected void OnApplicationQuit ()
		{
			if (KickStarter.inventoryManager)
			{
				foreach (InvItem invItem in KickStarter.inventoryManager.items)
				{
					if (invItem.cursorIcon != null)
					{
						invItem.cursorIcon.ClearCache ();
					}
				}
			}
		}


		protected void OnEnable ()
		{
			EventManager.OnInitialiseScene += OnInitialiseScene;
			EventManager.OnInventoryInteract += OnInventoryInteract;
			EventManager.OnInventoryCombine += OnInventoryCombine;
			EventManager.OnUpdatePlayableScreenArea += OnUpdatePlayableScreenArea;
		}


		protected void OnDisable ()
		{
			EventManager.OnInitialiseScene -= OnInitialiseScene;
			EventManager.OnInventoryInteract -= OnInventoryInteract;
			EventManager.OnInventoryCombine -= OnInventoryCombine;
		}

		#endregion


		#region PublicFunctions

		/** Transfers any relevant data from InventoryManager when the game begins or restarts. */
		public void OnInitPersistentEngine ()
		{
			SetNull ();
			hoverInstance = null;
			showHoverLabel = true;
			
			ingredientCollections = new List<IngredientCollection> ();

			AssignStartingItems ();
		}


		/** De-selects the active inventory item. */
		public void SetNull ()
		{
			if (selectedInstance == null)
			{
				return;
			}

			InvInstance oldInstance = selectedInstance;
			selectedInstance = null;

			if (InvInstance.IsValid (oldInstance))
			{
				KickStarter.eventManager.Call_OnChangeInventory (null, oldInstance, InventoryEventType.Deselect);
			}
				
			highlightInstance = null;
			PlayerMenus.ResetInventoryBoxes ();
		}
		

		/**
		 * <summary>Selects an inventory item (InvItem) by referencing its ID number.</summary>
		 * <param name = "_id">The inventory item's ID number</param>
		 * <param name = "_mode">What mode the item is selected in (Use, Give)</param>
		 */
		public void SelectItemByID (int _id, SelectItemMode _mode = SelectItemMode.Use, bool ignoreInventory = false)
		{
			if (_id == -1)
			{
				SetNull ();
				return;
			}

			InvInstance invInstance = null;

			if (ignoreInventory)
			{
				foreach (InvItem item in KickStarter.inventoryManager.items)
				{
					if (item != null && item.id == _id)
					{
						invInstance = new InvInstance (item);
						break;
					}
				}
			}
			else
			{
				invInstance = playerInvCollection.GetFirstInstance (_id);
			}

			if (InvInstance.IsValid (invInstance))
			{
				SelectItem (invInstance, _mode);
				return;
			}

			SetNull ();
			ACDebug.LogWarning ("Want to select inventory item " + KickStarter.inventoryManager.GetLabel (_id) + " but player is not carrying it.");
		}


		/**
		 * <summary>Re-selects the last-selected inventory item, if available.</summary>
		 */
		public void ReselectLastItem ()
		{
			if (InvInstance.IsValid (lastSelectedInstance) && playerInvCollection.Contains (lastSelectedInstance))
			{
				SelectItem (lastSelectedInstance, lastSelectedInstance.SelectItemMode);
			}
		}


		/**
		 * <summary>Selects an inventory item (InvItem)</summary>
		 * <param name = "invInstance">The instance of the inventory item to select</param>
		 * <param name = "_mode">What mode the item is selected in (Use, Give)</param>
		 */
		public void SelectItem (InvInstance invInstance, SelectItemMode _mode = SelectItemMode.Use)
		{
			if (!InvInstance.IsValid (invInstance))
			{
				SetNull ();
			}
			else if (selectedInstance == invInstance)
			{
				if (invInstance.CanStack ())
				{
					invInstance.AddStack ();
				}
				else
				{ 
					SetNull ();
					KickStarter.playerCursor.ResetSelectedCursor ();
				}
			}
			else
			{
				invInstance.SelectItemMode = _mode;
				lastSelectedInstance = selectedInstance = invInstance;

				switch (invInstance.ItemStackingMode)
				{
					case ItemStackingMode.Single:
					case ItemStackingMode.Stack:
						invInstance.TransferCount = 1;
						break;

					default:
						invInstance.TransferCount = 0;
						break;
				}

				KickStarter.eventManager.Call_OnChangeInventory (null, selectedInstance, InventoryEventType.Select);
				PlayerMenus.ResetInventoryBoxes ();
				KickStarter.playerInput.EnforcePreInventoryDragState ();
			}
		}


		/**
		 * <summary>Replaces one inventory item carried by the player with another, retaining its position in its MenuInventoryBox element.</summary>
		 * <param name = "_addID">The ID number of the inventory item (InvItem) to add</param>
		 * <param name = "_removeID">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "addAmount">The amount if the new inventory item to add, if the InvItem's canCarryMultiple = True</param>
		 */
		public void Replace (int _addID, int _removeID, int addAmount = 1)
		{
			if (playerInvCollection.Contains (_addID))
			{
				return;
			}

			InvItem addItem = KickStarter.inventoryManager.GetItem (_addID);
			if (addItem == null)
			{
				return;
			}

			if (!addItem.canCarryMultiple)
			{
				addAmount = 1;
			}
			InvInstance newInvInstance = new InvInstance (addItem, addAmount);

			if (!playerInvCollection.Contains (_removeID))
			{
				// Not carrying
				playerInvCollection.AddToEnd (newInvInstance);
				return;
			}

			InvInstance removeInstance = playerInvCollection.GetFirstInstance (_removeID);
			int removeIndex = playerInvCollection.IndexOf (removeInstance);

			playerInvCollection.Insert (newInvInstance, removeIndex, OccupiedSlotBehaviour.Overwrite);
		}


		/**
		 * <summary>Adds an inventory item to the player's inventory.</summary>
		 * <param name = "itemID">The ID of the inventory item to add</param>
		 * <param name = "amount">The number of instances of the item to add</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <param name = "playerID">The ID number of the Player to receive the item, if multiple Player prefabs are supported. If playerID = -1, the current player will receive the item</param>
		 * <param name = "addToFront">If True, the new item will be added to the front of the inventory</param>
		 */
		public void Add (int itemID, int amount = 1, bool selectAfter = false, int playerID = -1, bool addToFront = false)
		{
			InvInstance newInstance = new InvInstance (itemID, amount);
			Add (newInstance, selectAfter, playerID, addToFront);
		}


		/**
		 * <summary>Adds an inventory item to the player's inventory.</summary>
		 * <param name = "newInstance">The instance of the inventory item to add</param>
		 * <param name = "selectAfter">If True, then the inventory item will be automatically selected</param>
		 * <param name = "playerID">The ID number of the Player to receive the item, if multiple Player prefabs are supported. If playerID = -1, the current player will receive the item</param>
		 * <param name = "addToFront">If True, the new item will be added to the front of the inventory</param>
		 */
		public void Add (InvInstance newInstance, bool selectAfter = false, int playerID = -1, bool addToFront = false)
		{
			if (!InvInstance.IsValid (newInstance)) return;

			if (playerID >= 0 && KickStarter.saveSystem.CurrentPlayerID != playerID)
			{
				AddToOtherPlayer (newInstance, playerID, addToFront);
			}
			else
			{
				if (addToFront)
				{
					playerInvCollection.Insert (newInstance, 0);
				}
				else
				{
					playerInvCollection.AddToEnd (newInstance);
				}

				if (selectAfter)
				{
					SelectItem (newInstance);
				}
			}
		}


		/**
		 * <summary>Removes an inventory item from the player's inventory. If multiple instances of the item can be held, all instances will be removed.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 */
		public void Remove (int _id)
		{
			playerInvCollection.DeleteAllOfType (_id);
		}


		public void Remove (InvInstance invInstance)
		{
			playerInvCollection.Delete (invInstance);
		}


		/**
		 * <summary>Removes some instances of an inventory items from the player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "amount">The amount if the inventory item to remove, if the InvItem's canCarryMultiple = True</param>
		 */
		public void Remove (int _id, int amount)
		{
			playerInvCollection.Delete (_id, amount);
		}


		public void AddToOtherPlayer (InvInstance invInstance, int playerID, bool addToFront)
		{
			InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (playerID);

			if (addToFront)
			{
				otherPlayerInvCollection.Insert (invInstance, 0);
			}
			else
			{
				otherPlayerInvCollection.AddToEnd (invInstance);
			}

			KickStarter.saveSystem.AssignItemsToPlayer (otherPlayerInvCollection, playerID);
		}


		/**
		 * <summary>Removes an inventory item from a player's inventory. If multiple instances of the item can be held, all instances will be removed.</summary>
		 * <param name = "itemID">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "playerID">The ID number of the player to affect, if player-switching is enabled</param>
		 */
		public void RemoveFromOtherPlayer (int itemID, int playerID)
		{
			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				RemoveFromOtherPlayer (itemID, 1, false, playerID);
			}
			else
			{
				playerInvCollection.DeleteAllOfType (itemID);
			}
		}


		/**
		 * <summary>Removes some instances of an inventory item from a player's inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item (InvItem) to remove</param>
		 * <param name = "amount">The amount if the inventory item to remove, if the InvItem's canCarryMultiple = True.</param>
		 * <param name = "playerID">The ID number of the player to affect, if player-switching is enabled</param>
		 */
		public void RemoveFromOtherPlayer (int itemID, int amount, int playerID)
		{
			if (playerID >= 0 && KickStarter.player.ID != playerID)
			{
				RemoveFromOtherPlayer (itemID, amount, true, playerID);
			}
			else
			{
				playerInvCollection.Delete (itemID, amount);
			}
		}


		/**
		 * <summary>Removes an inventory item from the player's inventory.</summary>
		 * <param name = "_name">The name of the item to remove</param>
		 * <param name = "amount">If >0, then only that quantity will be removed, if the item's canCarryMultiple property is True. Otherwise, all instances will be removed</param>
		 */
		public void Remove (string _name, int amount = 0)
		{
			InvItem itemToRemove = KickStarter.inventoryManager.GetItem (_name);
			if (itemToRemove != null)
			{
				if (amount > 0)
				{
					playerInvCollection.Delete (itemToRemove.id, amount);
				}
				else
				{
					playerInvCollection.DeleteAllOfType (itemToRemove.id);
				}
			}
		}
		

		/**
		 * <summary>Removes all items from the player's inventory</summary>
		 */
		public void RemoveAll ()
		{
			playerInvCollection.DeleteAll ();
		}


		/**
		 * <summary>Gets the amount of a particular inventory item within the player's inventory.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <returns>The amount of the inventory item within the player's inventory.</returns>
		 */
		public int GetCount (int _invID)
		{
			return playerInvCollection.GetCount (_invID);
		}
		

		/**
		 * <summary>Gets the amount of a particular inventory item within any player's inventory, if multiple Player prefabs are supported.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The amount of the inventory item within the player's inventory.</returns>
		 */
		public int GetCount (int _invID, int _playerID)
		{
			InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (_playerID);
			if (otherPlayerInvCollection != null)
			{
				return otherPlayerInvCollection.GetCount (_invID);
			}
			return 0;
		}


		/**
		 * <summary>Gets the amount of a particular inventory item within all player inventories, if multiple Player prefabs are supported.</summary>
		 * <param name = "_invID">The ID number of the inventory item (InvItem) in question</param>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The amount of the inventory item within all player inventories.</returns>
		 */
		public int GetCountFromAllPlayers (int _invID)
		{
			int count = 0;
			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				count += GetCount (_invID, playerPrefab.ID);
			}
			return count;
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by the active Player.</summary>
		 * <param name="includeMultipleInSameSlot">If True, then multiple items in the same slot will be counted separately</param>
		 * <returns>The total number of inventory items currently held by the active Player</returns>
		 */
		public int GetNumberOfItemsCarried (bool includeMultipleInSameSlot = false)
		{
			return playerInvCollection.GetCount (includeMultipleInSameSlot);
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by a given Player, if multiple Players are supported.</summary>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <param name="includeMultipleInSameSlot">If True, then multiple items in the same slot will be counted separately</param>
		 * <returns>The total number of inventory items currently held by the given Player</returns>
		 */
		public int GetNumberOfItemsCarried (int _playerID, bool includeMultipleInSameSlot = false)
		{
			InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (_playerID);
			if (otherPlayerInvCollection != null)
			{
				return otherPlayerInvCollection.GetCount (includeMultipleInSameSlot);
			}
			return 0;
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by all Players, if multiple Players are supported.</summary>
		 * <param name="includeMultipleInSameSlot">If True, then multiple items in the same slot will be counted separately</param>
		 * <returns>The total number of inventory items currently held by all Players</returns>
		 */
		public int GetNumberOfItemsCarriedByAllPlayers (bool includeMultipleInSameSlot = false)
		{
			int count = 0;
			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (playerPrefab.ID);
				if (otherPlayerInvCollection != null)
				{
					count += otherPlayerInvCollection.GetCount (includeMultipleInSameSlot);
				}
			}
			return count;
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by the active Player.</summary>
		 * <param name = "categoryID">If >=0, then only items placed in the category with that ID will be counted</param>
		 * <returns>The total number of inventory items currently held by the active Player</returns>
		 */
		public int GetNumberOfItemsCarriedInCategory (int categoryID, bool includeMultipleInSameSlot = false)
		{
			return playerInvCollection.GetCountInCategory (categoryID, includeMultipleInSameSlot);
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by a given Player, if multiple Players are supported.</summary>
		 * <param name = "categoryID">If >=0, then only items placed in the category with that ID will be counted</param>
		 * <param name = "playerID">The ID number of the Player to refer to</param>
		 * <returns>The total number of inventory items currently held by the given Player</returns>
		 */
		public int GetNumberOfItemsCarriedInCategory (int categoryID, int _playerID, bool includeMultipleInSameSlot = false)
		{
			InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (_playerID);
			if (otherPlayerInvCollection != null)
			{
				return otherPlayerInvCollection.GetCountInCategory (categoryID, includeMultipleInSameSlot);
			}
			return 0;
		}


		/**
		 * <summary>Gets the total number of inventory items currently held by all Players, if multiple Players are supported.</summary>
		 * <param name = "categoryID">If >=0, then only items placed in the category with that ID will be counted</param>
		 * <returns>The total number of inventory items currently held by the all Players</returns>
		 */
		public int GetNumberOfItemsCarriedInCategoryByAllPlayers (int categoryID, bool includeMultipleInSameSlot = false)
		{
			int count = 0;
			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (playerPrefab.ID);
				if (otherPlayerInvCollection != null)
				{
					count += otherPlayerInvCollection.GetCountInCategory (categoryID, includeMultipleInSameSlot);
				}
			}
			return count;
		}


		/**
		 * <summary>Gets an inventory item instance within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvInstance GetInstance (int _id)
		{
			return playerInvCollection.GetFirstInstance (_id);
		}


		/**
		 * <summary>Gets an inventory item within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the inventory item</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvItem GetItem (int _id)
		{
			InvInstance invInstance = playerInvCollection.GetFirstInstance (_id);
			if (InvInstance.IsValid (invInstance)) return invInstance.InvItem;
			return null;
		}


		/**
		 * <summary>Gets the first-found inventory item within the player's current inventory.</summary>
		 * <param name = "_name">The name of the InvItem to find</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvItem GetItem (string _name)
		{
			InvInstance invInstance = playerInvCollection.GetFirstInstance (_name);
			if (InvInstance.IsValid (invInstance)) return invInstance.InvItem;
			return null;
		}


		/**
		 * <summary>Gets the first-found instance of an inventory item within the player's current inventory.</summary>
		 * <param name = "_name">The name of the InvItem to find</param>
		 * <returns>The inventory item, if it is held by the player</returns>
		 */
		public InvInstance GetInstance (string _name)
		{
			return playerInvCollection.GetFirstInstance (_name);
		}


		/**
		 * <summary>Gets all instances of an inventory item within the player's current inventory.</summary>
		 * <param name = "_id">The ID number of the InvItem to find</param>
		 * <returns>All instances of the inventory item</returns>
		 */
		public InvInstance[] GetInstances (int _id)
		{
			return playerInvCollection.GetAllInstances (_id);
		}


		/**
		 * <summary>Gets all instances of an inventory item within the player's current inventory.</summary>
		 * <param name = "_name">The name of the InvItem to find</param>
		 * <returns>All instances of the inventory item</returns>
		 */
		public InvInstance[] GetInstances (string _name)
		{
			return playerInvCollection.GetAllInstances (_name);
		}


		/**
		 * <summary>Checks if an inventory item is within the player's current inventory.</summary>
		 * <param name = "itemID">The ID number of the inventory item</param>
		 * <returns>True if the inventory item is within the player's current inventory</returns>
		 */
		public bool IsCarryingItem (int itemID)
		{
			return playerInvCollection.Contains (itemID);
		}


		/**
		 * <summary>Checks if an inventory item is within the player's current inventory.</summary>
		 * <param name = "invItem">The inventory item</param>
		 * <returns>True if the inventory item is within the player's current inventory</returns>
		 */
		public bool IsCarryingItem (InvItem invItem)
		{
			if (invItem == null) return false;
			return playerInvCollection.Contains (invItem.id);
		}


		public bool IsCarryingItem (InvInstance invInstance)
		{
			if (!InvInstance.IsValid (invInstance)) return false;
			return playerInvCollection.Contains (invInstance);
		}


		/**
		 * <summary>Sets up all "Interaction" menus according to a specific inventory item.</summary>
		 * <param name = "invInstance">The relevant inventory item instance</param>
		 */
		public void ShowInteractions (InvInstance invInstance)
		{
			hoverInstance = invInstance;
			if (KickStarter.settingsManager.SeeInteractions != SeeInteractions.ViaScriptOnly)
			{
				KickStarter.playerMenus.EnableInteractionMenus (invInstance);
			}
		}


		/**
		 * <summary>Sets the item currently being hovered over by the mouse cursor.</summary>
		 * <param name = "invInstance">The instance of the item to set</param>
		 * <param name = "menuInventoryBox">The MenuInventoryBox that the item is displayed within</param>
		 */
		public void SetHoverItem (InvInstance invInstance, MenuInventoryBox menuInventoryBox)
		{
			hoverInstance = invInstance;

			if (menuInventoryBox.displayType == ConversationDisplayType.IconOnly)
			{
				if (menuInventoryBox.inventoryBoxType == AC_InventoryBoxType.Container && InvInstance.IsValid (selectedInstance))
				{
					showHoverLabel = false;
				}
				else
				{
					showHoverLabel = true;
				}
			}
			else
			{
				showHoverLabel = menuInventoryBox.updateHotspotLabelWhenHover;
			}
		}


		/**
		 * <summary>Sets the item currently being hovered over by the mouse cursor.</summary>
		 * <param name = "invInstance">The instance of the item to set</param>
		 * <param name = "menuCrafting">The MenuInventoryBox that the item is displayed within</param>
		 */
		public void SetHoverItem (InvInstance invInstance, MenuCrafting menuCrafting)
		{
			hoverInstance = invInstance;

			if (menuCrafting.displayType == ConversationDisplayType.IconOnly)
			{
				showHoverLabel = true;
			}
			else
			{
				showHoverLabel = false;
			}
		}


		/** Clears the item being hovered */
		public void ClearHoverItem ()
		{
			hoverInstance = null;
		}


		/** Resets all active recipes, and clears all MenuCrafting elements */
		public void RemoveRecipes ()
		{
			foreach (IngredientCollection ingredientCollection in ingredientCollections)
			{
				playerInvCollection.TransferAll (ingredientCollection.InvCollection);
			}
		}


		/** 
		 * <summary>Resets the inventory associated with a specific Crafting Ingredients element</summary>
		 * <param name = "menuName">The name of the Menu</param>
		 * <param name = "craftingIngredientsName">The name of the Crafting menu element of type Ingredients</param>
		 */
		public void RemoveRecipe (string menuName, string craftingIngredientsName)
		{
			foreach (IngredientCollection ingredientCollection in ingredientCollections)
			{
				if (ingredientCollection.Matches (menuName, craftingIngredientsName))
				{
					playerInvCollection.TransferAll (ingredientCollection.InvCollection);
				}
			}
		}


		/**
		 * <summary>Works out which Recipe, if any, for which all ingredients have been correctly arranged.</summary>
		 * <param name = "ingredientsInvCollection">The InvCollection to get ingredients from</param>
		 * <returns>The Recipe, if any, for which all ingredients have been correctly arranged</returns>
		 */
		public Recipe CalculateRecipe (InvCollection ingredientsInvCollection)
		{
			if (KickStarter.inventoryManager == null)
			{
				return null;
			}

			foreach (Recipe recipe in KickStarter.inventoryManager.recipes)
			{
				if (recipe.CanBeCrafted (ingredientsInvCollection))
				{
					return recipe;
				}
			}

			return null;
		}


		/**
		 * <summary>Crafts a new inventory item, and removes the relevent ingredients, according to a Recipe.</summary>
		 * <param name = "ingredientsInvCollection">The InvCollection to get ingredients from</param>
		 * <param name = "recipe">The Recipe to perform</param>
		 * <param name = "selectAfter">If True, then the resulting inventory item will be selected once the crafting is complete</param>
		 */
		public void PerformCrafting (InvCollection ingredientsInvCollection, Recipe recipe, bool selectAfter)
		{
			ingredientsInvCollection.DeleteRecipeIngredients (recipe);
			InvInstance addedInstance = playerInvCollection.Add (new InvInstance (recipe.resultID));
			
			if (selectAfter)
			{
				SelectItem (addedInstance);
			}
		}


		/**
		 * <summary>Crafts a new inventory item, and removes the relevent ingredients, according to a Recipe.</summary>
		 * <param name = "ingredientsInvCollection">The InvCollection to get ingredients from</param>
		 * <param name = "recipe">The Recipe to perform</param>
		 * <param name = "toInvCollection">If assigned, the InvCollection to place the newly-created Recipe item into</param>
		 */
		public InvInstance PerformCrafting (InvCollection ingredientsInvCollection, Recipe recipe, InvCollection toInvCollection = null)
		{
			ingredientsInvCollection.DeleteRecipeIngredients (recipe);
			if (toInvCollection != null)
			{
				InvInstance addedInstance = toInvCollection.Add (new InvInstance (recipe.resultID));
				return addedInstance;
			}
			else
			{
				return new InvInstance (recipe.resultID);
			}
		}


		public List<InvInstance> RemoveEmptySlots (List<InvInstance> invInstances)
		{
			// Remove empty slots on end
			for (int i = invInstances.Count - 1; i >= 0; i--)
			{
				if (!InvInstance.IsValid (invInstances[i]))
				{
					invInstances.RemoveAt (i);
				}
				else
				{
					break;
				}
			}

			return invInstances;
		}


		/**
		 * <summary>Gets an InvCollection of ingredients associated with a given MenuCrafting element</summary>
		 * <param name = "menuName">The title of the Menu that contains the MenuCrafting element</param>
		 * <param name = "craftingElementName">The title of the "Ingredients" MenuCrafting element</param>
		 * <returns>The InvCollection of ingredients associated with the MenuCrafting element</summary>
		 */
		public InvCollection GetIngredientsInvCollection (string menuName, string craftingElementName)
		{
			for (int i = 0; i < ingredientCollections.Count; i++)
			{
				if (ingredientCollections[i].Matches (menuName, craftingElementName))
				{
					return ingredientCollections[i].InvCollection;
				}
			}

			IngredientCollection newIngredientCollection = new IngredientCollection (menuName, craftingElementName);
			ingredientCollections.Add (newIngredientCollection);
			return newIngredientCollection.InvCollection;
		}


		/**
		 * <summary>Assign's the player's current inventory in bulk</summary>
		 * <param name = "newInventory">A list of the InvInstance classes that make up the new inventory</param>
		 */
		public void AssignPlayerInventory (InvCollection invCollection)
		{
			playerInvCollection = invCollection;
			PlayerMenus.ResetInventoryBoxes ();
		}
		

		/**
		 * <summary>Draws the currently-highlight item across a set region of the screen.</summary>
		 * <param name = "_rect">The Screen-Space co-ordinates at which to draw the highlight item</param>
		 */
		public void DrawHighlighted (Rect _rect)
		{
			if (!InvInstance.IsValid (highlightInstance) || highlightInstance.ActiveTex == null) return;
			
			if (highlightState == HighlightState.None)
			{
				GUI.DrawTexture (_rect, highlightInstance.ActiveTex, ScaleMode.StretchToFill, true, 0f);
				return;
			}
			
			if (pulseDirection == 0)
			{
				pulse = 0f;
				pulseDirection = 1;
			}
			else if (pulseDirection == 1)
			{
				pulse += KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
			}
			else if (pulseDirection == -1)
			{
				pulse -= KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
			}
			
			if (pulse > 1f)
			{
				pulse = 1f;
				
				if (highlightState == HighlightState.Normal)
				{
					highlightState = HighlightState.None;
					GUI.DrawTexture (_rect, highlightInstance.ActiveTex, ScaleMode.StretchToFill, true, 0f);
					return;
				}
				else
				{
					pulseDirection = -1;
				}
			}
			else if (pulse < 0f)
			{
				pulse = 0f;
				
				if (highlightState == HighlightState.Pulse)
				{
					pulseDirection = 1;
				}
				else
				{
					highlightState = HighlightState.None;
					GUI.DrawTexture (_rect, highlightInstance.Tex, ScaleMode.StretchToFill, true, 0f);
					highlightInstance = null;
					return;
				}
			}

			Color backupColor = GUI.color;
			Color tempColor = GUI.color;
			
			tempColor.a = pulse;
			GUI.color = tempColor;
			GUI.DrawTexture (_rect, highlightInstance.ActiveTex, ScaleMode.StretchToFill, true, 0f);
			GUI.color = backupColor;
			GUI.DrawTexture (_rect, highlightInstance.Tex, ScaleMode.StretchToFill, true, 0f);
		}
		

		/**
		 * <summary>Fully highlights an inventory item instantly.</summary>
		 * <param name = "_id">The ID number of the inventory item (see InvItem) to highlight</param>
		 */
		public void HighlightItemOnInstant (int _id)
		{
			highlightInstance = GetInstance (_id);
			highlightState = HighlightState.None;
			pulse = 1f;
		}
		

		/**
		 * Removes all highlighting from the inventory item curently being highlighted.
		 */
		public void HighlightItemOffInstant ()
		{
			highlightInstance = null;
			highlightState = HighlightState.None;
			pulse = 0f;
		}
		

		/**
		 * <summary>Highlights an inventory item.</summary>
		 * <param name = "_id">The ID number of the inventory item to highlight</param>
		 * <param name = "_type">The type of highlighting effect to perform (Enable, Disable, PulseOnce, PulseContinuously)</param>
		 */
		public void HighlightItem (int _id, HighlightType _type)
		{
			HighlightItem (GetInstance (_id), _type);
		}


		/**
		 * <summary>Highlights an inventory item instance.</summary>
		 * <param name = "invInstance">The inventory item instance to highlight</param>
		 * <param name = "_type">The type of highlighting effect to perform (Enable, Disable, PulseOnce, PulseContinuously)</param>
		 */
		public void HighlightItem (InvInstance invInstance, HighlightType _type)
		{
			highlightInstance = invInstance;
			if (!InvInstance.IsValid (highlightInstance)) return;

			switch (_type)
			{
				case HighlightType.Enable:
					highlightState = HighlightState.Normal;
					pulseDirection = 1;
					break;

				case HighlightType.Disable:
					highlightState = HighlightState.Normal;
					pulseDirection = -1;
					break;

				case HighlightType.PulseOnce:
					highlightState = HighlightState.Flash;
					pulse = 0f;
					pulseDirection = 1;
					break;

				case HighlightType.PulseContinually:
					highlightState = HighlightState.Pulse;
					pulse = 0f;
					pulseDirection = 1;
					break;

				default:
					break;
			}

			KickStarter.eventManager.Call_OnInventoryHighlight (highlightInstance, _type);
		}


		/** Draws how much of the selected item are selected, if greater than one. This should be called within an OnGUI function. */
		public virtual void DrawSelectedInventoryCount ()
		{
			if (KickStarter.cursorManager.displayCountSize <= 0)
			{
				return;
			}

			if (InvInstance.IsValid (selectedInstance))
			{
				string countText = GetInventoryCountText ();
				if (!string.IsNullOrEmpty (countText))
				{
					Vector2 cursorPosition = KickStarter.playerInput.GetMousePosition ();
					float cursorSize = KickStarter.cursorManager.inventoryCursorSize;

					if (countTextEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (AdvGame.GUIBox (cursorPosition, cursorSize), countText, countStyle, Color.black, countStyle.normal.textColor, 2, countTextEffects);
					}
					else
					{
						GUI.Label (AdvGame.GUIBox (cursorPosition, cursorSize), countText, countStyle);
					}
				}
			}
		}


		protected virtual string GetInventoryCountText ()
		{
			if (InvInstance.IsValid (selectedInstance))
			{
				string customText = KickStarter.eventManager.Call_OnRequestInventoryCountText (selectedInstance, true);
				if (!string.IsNullOrEmpty (customText))
				{
					return customText;
				}

				int displayCount = selectedInstance.TransferCount;
				if (displayCount > 1)
				{
					return displayCount.ToString ();
				}
			}
			return string.Empty;
		}


		/**
		 * <summary>Processes the clicking of an inventory item within a MenuInventoryBox element</summary>
		 * <param name = "_menu">The Menu that contains the MenuInventoryBox element</param>
		 * <param name = "inventoryBox">The MenuInventoryBox element that was clicked on</param>
		 * <param name = "_slot">The index number of the MenuInventoryBox slot that was clicked on</param>
		 * <param name = "_mouseState">The state of the mouse when the click occured (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo)</param>
		 * <returns>True if the click had an effect and should be consumed</returns>
		 */
		public bool ProcessInventoryBoxClick (AC.Menu _menu, MenuInventoryBox inventoryBox, int _slot, MouseState _mouseState)
		{
			bool clickConsumed = true;

			switch (inventoryBox.inventoryBoxType)
			{
				case AC_InventoryBoxType.Default:
				case AC_InventoryBoxType.DisplayLastSelected:
					{
						clickConsumed = inventoryBox.HandleDefaultClick (_mouseState, _slot);
						_menu.Recalculate ();
					}
					break;

				case AC_InventoryBoxType.Container:
					{
						clickConsumed = inventoryBox.ClickContainer (_mouseState, _slot);
						_menu.Recalculate ();
					}
					break;

				case AC_InventoryBoxType.HotspotBased:
					{
						if (_mouseState == MouseState.LetGo)
						{
							// Invalid
						}
						else if (InvInstance.IsValid (_menu.TargetInvInstance))
						{
							_menu.TargetInvInstance.Combine (inventoryBox.GetInstance (_slot), true);
							KickStarter.playerInput.ResetMouseClick ();
							clickConsumed = true;
						}
						else if (_menu.TargetHotspot)
						{
							InvInstance _invInstance = inventoryBox.GetInstance (_slot);
							if (InvInstance.IsValid (_invInstance))
							{
								_menu.TurnOff ();
								KickStarter.playerInteraction.UseInventoryOnHotspot (_menu.TargetHotspot, _invInstance);
								KickStarter.playerCursor.ResetSelectedCursor ();
								clickConsumed = true;
							}
						}
						else
						{
							ACDebug.LogWarning ("Cannot handle inventory click since there is no active Hotspot.");
						}
					}
					break;

				default:
					break;
			}

			return clickConsumed;
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			if (InvInstance.IsValid (selectedInstance))
			{
				mainData.selectedInventoryID = selectedInstance.ItemID;
				mainData.isGivingItem = (selectedInstance.SelectItemMode == SelectItemMode.Give);
			}
			else
			{
				mainData.selectedInventoryID = -1;
			}

			return mainData;
		}


		public void LoadMainData (MainData mainData)
		{
			if (mainData.selectedInventoryID > -1)
			{
				if (mainData.isGivingItem)
				{
					SelectItemByID (mainData.selectedInventoryID, SelectItemMode.Give);
				}
				else
				{
					SelectItemByID (mainData.selectedInventoryID, SelectItemMode.Use);
				}
			}
			else
			{
				SetNull ();
			}
			RemoveRecipes ();
		}


		/**
		 * <summary>Checks if an item can be transferred from a Container to the current Player's inventory</summary>
		 * <param name = "container">The Container to transfer from</param>
		 * <param name = "invInstance">The inventory item to  to transfer</param>
		 * <returns>True if the item can be tranferred.  This is always True, provided containerItem is not null, but the method can be overridden through subclassing</returns>
		 */
		public virtual bool CanTransferContainerItemsToInventory (Container container, InvInstance invInstance)
		{
			return (InvInstance.IsValid (invInstance));
		}

		#endregion


		#region CustomEvents

		protected void OnInitialiseScene ()
		{
			if (!KickStarter.settingsManager.IsInLoadingScene () && KickStarter.sceneSettings)
			{
				SetNull ();
				lastSelectedInstance = null;
			}
		}


		protected void OnInventoryInteract (InvItem invItem, int cursorID)
		{
			if (KickStarter.settingsManager.autoCycleWhenInteract && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple)
			{
				if (!InvInstance.IsValid(selectedInstance))
				{
					KickStarter.playerCursor.ResetSelectedCursor ();
				}
			}
		}


		protected void OnInventoryCombine (InvItem invItemA, InvItem invItemB)
		{
			OnInventoryInteract (null, 0);
		}


		protected void OnUpdatePlayableScreenArea ()
		{
			countStyle = KickStarter.cursorManager.GetDisplayCountStyle ();
			countTextEffects = KickStarter.cursorManager.displayCountTextEffects;
		}

		#endregion


		#region ProtectedFunctions

		protected void AssignStartingItems ()
		{
			if (KickStarter.inventoryManager)
			{
				playerInvCollection = GetItemsOnStart (-1);

				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && !KickStarter.settingsManager.shareInventory)
				{
					foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
					{
						if (playerPrefab != null)
						{
							InvCollection otherPlayerInvCollection = GetItemsOnStart (playerPrefab.ID);
							if (otherPlayerInvCollection != null)
							{
								KickStarter.saveSystem.AssignItemsToPlayer (otherPlayerInvCollection, playerPrefab.ID);
							}
						}
					}
				}
			}
			else
			{
				ACDebug.LogError ("No Inventory Manager found - please use the Adventure Creator window to create one.");
			}

		}


		protected InvCollection GetItemsOnStart (int playerID = -1)
		{
			List<InvItem> playerStartItems = new List<InvItem> ();

			if (KickStarter.inventoryManager)
			{
				foreach (InvItem item in KickStarter.inventoryManager.items)
				{
					if (item.carryOnStart)
					{
						if (!item.canCarryMultiple)
						{
							item.count = 1;
						}

						if (item.count < 1)
						{
							continue;
						}

						item.Upgrade ();

						if (!item.carryOnStartNotDefault && playerID == -1)
						{
							playerStartItems.Add (item);
						}
						else if (playerID >= 0 && item.carryOnStartIDs.Contains (playerID))
						{
							playerStartItems.Add (item);
						}
						
					}
				}

				return new InvCollection (playerStartItems);
			}
			else
			{
				ACDebug.LogError ("No Inventory Manager found - please use the Adventure Creator window to create one.");
			}

			return null;
		}


		protected void RemoveFromOtherPlayer (int invID, int amount, bool setAmount, int playerID)
		{
			InvCollection otherPlayerInvCollection = KickStarter.saveSystem.GetItemsFromPlayer (playerID);

			if (setAmount)
			{
				otherPlayerInvCollection.Delete (invID, amount);
			}
			else
			{
				otherPlayerInvCollection.DeleteAllOfType (invID);
			}

			KickStarter.saveSystem.AssignItemsToPlayer (otherPlayerInvCollection, playerID);
		}


		protected List<InvInstance> ReorderItems (List<InvInstance> invInstances)
		{
			if (!KickStarter.settingsManager.canReorderItems)
			{
				for (int i=0; i< invInstances.Count; i++)
				{
					if (!InvInstance.IsValid (invInstances[i]))
					{
						invInstances.RemoveAt (i);
						i=0;
					}
				}
			}
			return invInstances;
		}
		
		#endregion


		#region GetSet

		/** The instance inventory item that is currently selected */
		public InvInstance SelectedInstance
		{
			get
			{
				return selectedInstance;
			}
		}


		/** The  inventory item that is currently selected */
		public InvItem SelectedItem
		{
			get
			{
				if (InvInstance.IsValid (selectedInstance))
				{
					return selectedInstance.InvItem;
				}
				return null;
			}
		}


		/** The inventory item that is currently being highlighted within an MenuInventoryBox element */
		public InvInstance HighlightInstance
		{
			get
			{
				return highlightInstance;
			}
		}


		/** A List of inventory items carried by the player. This is calculated from LocalInstances and should not be called every frame */
		public List<InvItem> localItems
		{
			get
			{
				return playerInvCollection.InvItems;
			}
		}


		/** The InvCollections that holds the current set of items to be crafted */
		public InvCollection[] CraftingInvCollections
		{
			get
			{
				InvCollection[] _invCollections = new InvCollection[ingredientCollections.Count];
				for (int i = 0; i < _invCollections.Length; i++)
				{
					_invCollections[i] = ingredientCollections[i].InvCollection;
				}
				return _invCollections;
			}
		}


		/** The InvCollection that holds the current set of items in the Player#s inventory */
		public InvCollection PlayerInvCollection
		{
			get
			{
				return playerInvCollection;
			}
		}


		/** The inventory item that is currently being hovered over */
		public InvItem hoverItem
		{
			get
			{
				if (InvInstance.IsValid (hoverInstance))
				{
					return hoverInstance.InvItem;
				}
				return null;
			}
		}


		/** The instance of the inventory item that is currently being hovered over */
		public InvInstance HoverInstance
		{
			get
			{
				return hoverInstance;
			}
		}


		/** The instance of the last inventory item to be selected.  This will return the currently-selected item if one exists */
		public InvInstance LastSelectedInstance
		{
			get
			{
				return lastSelectedInstance;
			}
		}


		/** The instance of the last inventory item that the player clicked on, in any MenuInventoryBox element type */
		public InvInstance LastClickedInstance
		{
			get
			{
				return lastClickedInstance;
			}
			set
			{
				lastClickedInstance = value;
			}
		}


		/** The last inventory item that the player clicked on, in any MenuInventoryBox element type */
		public InvItem LastClickedItem
		{
			get
			{
				if (InvInstance.IsValid (lastClickedInstance))
				{
					return lastClickedInstance.InvItem;
				}
				return null;
			}
		}


		/** The last inventory item to be selected.  This will return the currently-selected item if one exists */
		public InvItem LastSelectedItem
		{
			get
			{
				if (InvInstance.IsValid (lastSelectedInstance))
				{
					return lastSelectedInstance.InvItem;
				}
				return null;
			}
		}


		/** If True, then the Hotspot label will show the name of the inventory item that the mouse is hovering over */
		public bool ShowHoverLabel
		{
			get
			{
				return showHoverLabel;
			}
		}

		#endregion

	}

}