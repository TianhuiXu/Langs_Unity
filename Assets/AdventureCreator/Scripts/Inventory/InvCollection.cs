/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"InvCollection.cs"
 * 
 *	This class stores a list of InvInstance (inventory item instances), and has functions to manage them.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/** This class stores a list of InvInstance (inventory item instances), and has functions to manage them. */
	public class InvCollection
	{

		#region Variables

		private int maxSlots;
		private List<InvInstance> invInstances;
		private List<int> limitToCategoryIDs;

		#endregion


		#region Constructors

		/** The default Constructor */
		public InvCollection ()
		{
			invInstances = new List<InvInstance>();
			limitToCategoryIDs = null;
			maxSlots = 0;
		}


		/** A Constructor that populates itself with default inventory data */
		public InvCollection (List<InvItem> invItems)
		{
			maxSlots = 0;
			limitToCategoryIDs = null;
			invInstances = new List<InvInstance>();

			foreach (InvItem invItem in invItems)
			{
				bool alreadyAdded = false;

				if (invItem.canCarryMultiple && invItem.count > invItem.maxCount)
				{
					int countLeft = invItem.count;
					while (countLeft > 0)
					{
						if (countLeft > invItem.maxCount)
						{
							AddToEnd (new InvInstance (invItem, invItem.maxCount));
							alreadyAdded = true;
							countLeft -= invItem.maxCount;
						}
						else
						{
							AddToEnd (new InvInstance (invItem, countLeft));
							alreadyAdded = true;
							countLeft = 0;
						}
					}
				}

				if (!alreadyAdded)
				{
					AddToEnd (new InvInstance (invItem, invItem.count));
				}
			}

			Clean ();
		}


		/**
		 * <summary>A Constructor that populates itself based on an existing list of inventory item instances.</summary>
		 * <param name="_invInstances">A List of InvInstances that represent the items to be added</param>
		 * <param name="allowEmptySlots">If True, then invalid or empty entries in the _invInstances List will be included and used to add empty slots, rather than removed</param>
		 */
		public InvCollection (List<InvInstance> _invInstances, bool allowEmptySlots = false)
		{
			maxSlots = 0;
			limitToCategoryIDs = null;
			invInstances = new List<InvInstance> ();

			foreach (InvInstance invInstance in _invInstances)
			{
				if (allowEmptySlots)
				{
					if (InvInstance.IsValid (invInstance))
					{
						InvInstance addedInstance = new InvInstance (invInstance);
						invInstances.Add (addedInstance);
						KickStarter.eventManager.Call_OnChangeInventory (this, addedInstance, InventoryEventType.Add);
					}
					else
					{
						invInstances.Add (null);
					}
				}
				else
				{
					AddToEnd (invInstance);
				}
			}

			Clean ();
		}


		/** A Constructor that populates itself based on a Container's default set of items */
		public InvCollection (Container container)
		{
			List<int> emptySlotIndices = new List<int>();

			maxSlots = container.maxSlots;
			limitToCategoryIDs = (container.limitToCategory) ? container.categoryIDs : null;
			invInstances = new List<InvInstance> ();

			foreach (ContainerItem containerItem in container.items)
			{
				InvItem invItem = containerItem.InvItem;
				if (invItem == null)
				{
					if (KickStarter.settingsManager.canReorderItems && containerItem.ItemID == -1)
					{
						emptySlotIndices.Add (InvInstances.Count);
					}

					continue;
				}

				bool alreadyAdded = false;
				if (invItem.canCarryMultiple && containerItem.Count > invItem.maxCount)
				{
					int countLeft = containerItem.Count;
					while (countLeft > 0)
					{
						if (countLeft > invItem.maxCount)
						{
							AddToEnd (new InvInstance (invItem, invItem.maxCount));
							alreadyAdded = true;
							countLeft -= invItem.maxCount;
						}
						else
						{
							AddToEnd (new InvInstance (invItem, countLeft));
							alreadyAdded = true;
							countLeft = 0;
						}
					}
				}

				if (!alreadyAdded)
				{
					AddToEnd (new InvInstance (containerItem));
				}
			}

			Clean ();

			if (emptySlotIndices.Count > 0)
			{
				for (int i=emptySlotIndices.Count-1; i>=0; i--)
				{
					invInstances.Insert (emptySlotIndices[i], null);
				}
				Clean ();
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Transfer all counts of a given inventory item from another collection</summary>
		 * <param name="invID">The ID of the inventory item (InvItem) to transfer</param>
		 * <param name="fromCollection">The collection to transfer from</param>
		 */
		public void Transfer (int invID, InvCollection fromCollection)
		{
			if (fromCollection == null || !fromCollection.Contains (invID))
			{
				return;
			}

			foreach (InvInstance invInstance in fromCollection.invInstances)
			{
				if (!InvInstance.IsValid (invInstance) || invInstance.ItemID != invID)
				{
					continue;
				}

				Add (invInstance);
			}
		}


		/**
		 * <summary>Transfer all counts of a given inventory item from another collection</summary>
		 * <param name="invInstance">The instance of the inventory item (InvItem) to transfer</param>
		 * <param name="fromCollection">The collection to transfer from</param>
		 */
		public void Transfer (InvInstance invInstance, InvCollection fromCollection)
		{
			if (!InvInstance.IsValid (invInstance) || fromCollection == null || !fromCollection.InvInstances.Contains (invInstance))
			{
				return;
			}

			Add (invInstance);
		}


		/**
		 * <summary>Transfer a set count of a given inventory item from another collection</summary>
		 * <param name="invID">The ID of the inventory item (InvItem) to transfer</param>
		 * <param name="fromCollection">The collection to transfer from</param>
		 * <param name="amount">The amount of items to transfer</param>
		 * <param name="matchPropertiesWhenMerging">If True, then the property values of two slots that represent the same item must match for them to merge</param>
		 */
		public void Transfer (int invID, InvCollection fromCollection, int amount, bool matchPropertiesWhenMerging = true)
		{
			if (fromCollection == null || !fromCollection.Contains (invID))
			{
				return;
			}

			foreach (InvInstance invInstance in fromCollection.invInstances)
			{
				if (!InvInstance.IsValid (invInstance) || invInstance.ItemID != invID)
				{
					continue;
				}

				int thisItemCount = invInstance.Count;
				if (thisItemCount < amount)
				{
					// Will need more after this
					invInstance.TransferCount = thisItemCount;
					amount -= thisItemCount;
				}
				else
				{
					// All we need
					invInstance.TransferCount = amount;
					amount = 0;
				}

				Add (invInstance, matchPropertiesWhenMerging);

				if (amount == 0)
				{
					break;
				}
			}
		}


		/**
		 * <summary>Transfer a set count of a given inventory item from another collection</summary>
		 * <param name="invInstance">The instance of the inventory item to transfer</param>
		 * <param name="fromCollection">The collection to transfer from</param>
		 * <param name="amount">The amount of items to transfer</param>
		 * <param name="matchPropertiesWhenMerging">If True, then the property values of two slots that represent the same item must match for them to merge</param>
		 */
		public void Transfer (InvInstance invInstance, InvCollection fromCollection, int amount, bool matchPropertiesWhenMerging = true)
		{
			if (!InvInstance.IsValid (invInstance) || fromCollection == null || !fromCollection.InvInstances.Contains (invInstance))
			{
				return;
			}

			int thisItemCount = invInstance.Count;
			if (thisItemCount < amount)
			{
				// Will need more after this
				invInstance.TransferCount = thisItemCount;
			}
			else
			{
				// All we need
				invInstance.TransferCount = amount;
			}

			Add (invInstance, matchPropertiesWhenMerging);
		}
			

		/**
		 * <summary>Transfers all items from another collection</summary>
		 * <param name="fromCollection">The collection of inventory item instances to transfer from</param>
		 */
		public void TransferAll (InvCollection fromCollection)
		{
			if (fromCollection == null) return;

			InvInstance[] transferInstances = fromCollection.InvInstances.ToArray ();
			foreach (InvInstance transferInstance in transferInstances)
			{
				Add (transferInstance);
			}
		}


		/**
		 * <summary>Adds an inventory item instance to the collection.  If the item exists in another collection, it will be removed from there automatically.</summary>
		 * <param name="addInstance">The inventory item instance to add</param>
		 * <returns>The new instance of the added item</returns>
		 */
		public InvInstance Add (InvInstance addInstance, bool matchPropertiesWhenMerging = true)
		{
			// Add to the first-available slot, or a filled slot if the same item

			if (!CanAccept (addInstance))
			{
				if (InvInstance.IsValid (addInstance))
				{
					if (KickStarter.eventManager) KickStarter.eventManager.Call_OnUseContainerFail (addInstance.GetSourceContainer (), addInstance);
				}
				return null;
			}

			InvCollection fromCollection = addInstance.GetSource ();

			InvInstance addedInstance = null;

			for (int i = 0; i < invInstances.Count; i++)
			{
				// First find existing

				if (InvInstance.IsValid (invInstances[i]) && invInstances[i] != addInstance && invInstances[i].IsMatch (addInstance, matchPropertiesWhenMerging) && addInstance.InvItem.canCarryMultiple && invInstances[i].Capacity > 0)
				{
					// Merge
					bool transferredAll = true;
					if (addInstance.TransferCount > invInstances[i].Capacity)
					{
						addInstance.TransferCount = invInstances[i].Capacity;
						transferredAll = false;
					}
					int numAdded = Mathf.Min (addInstance.CreateTransferInstance ().Count, invInstances[i].Capacity);
					invInstances[i].Count += numAdded;
					if (KickStarter.eventManager) KickStarter.eventManager.Call_OnChangeInventory (this, invInstances[i], InventoryEventType.Add, numAdded);
					if (transferredAll)
					{
						addedInstance = invInstances[i];
						break;
					}
				}
			}

			if (!InvInstance.IsValid (addedInstance))
			{
				for (int i=0; i<invInstances.Count; i++)
				{
					// Inside
				
					if (!InvInstance.IsValid (invInstances[i]))
					{
						// Empty slot
						invInstances[i] = addInstance.CreateTransferInstance ();
						if (KickStarter.eventManager) KickStarter.eventManager.Call_OnChangeInventory (this, invInstances[i], InventoryEventType.Add);
						addedInstance = invInstances[i];
						break;
					}
					else if (invInstances[i] == addInstance)
					{
						// Same
					}
					else if (invInstances[i].IsMatch (addInstance, matchPropertiesWhenMerging) && addInstance.InvItem.canCarryMultiple && invInstances[i].Capacity > 0)
					{
						// Merge
						bool transferredAll = true;
						if (addInstance.TransferCount > invInstances[i].Capacity)
						{
							addInstance.TransferCount = invInstances[i].Capacity;
							transferredAll = false;
						}
						int numAdded = Mathf.Min (addInstance.CreateTransferInstance ().Count, invInstances[i].Capacity);
						invInstances[i].Count += numAdded;
						if (KickStarter.eventManager) KickStarter.eventManager.Call_OnChangeInventory (this, invInstances[i], InventoryEventType.Add, numAdded);
						if (transferredAll)
						{
							addedInstance = invInstances[i];
							break;
						}
					}
				}
			}

			if (!InvInstance.IsValid (addedInstance))
			{
				return AddToEnd (addInstance);
			}

			if (fromCollection != null) fromCollection.Clean ();
			Clean ();
			PlayerMenus.ResetInventoryBoxes ();

			return addedInstance;
		}


		/**
		 * <summary>Adds an inventory item instance to the end of the collection - or the first-available empty slot.  If the item exists in another collection, it will be removed from there automatically.</summary>
		 * <param name="addInstance">The inventory item instance to add</param>
		 * <returns>The new instance of the added item</returns>
		 */
		public InvInstance AddToEnd (InvInstance addInstance)
		{
			return Insert (addInstance, -1);
		}


		/**
		 * <summary>Inserts an inventory item instance into a specific index in the collection.  If the item exists in another collection, it will be removed from there automatically.</summary>
		 * <param name="addInstance">The inventory item instance to add</param>
		 * <param name="index">The index to insert the item at</param>
		 * <param name="occupiedSlotBehaviour">How to react if the intended index is already occupied by another item instance.</param>
		 * <returns>The new instance of the added item</returns>
		 */
		public InvInstance Insert (InvInstance addInstance, int index, OccupiedSlotBehaviour occupiedSlotBehaviour = OccupiedSlotBehaviour.ShiftItems, bool matchPropertiesWhenMerging = true)
		{
			if (occupiedSlotBehaviour == OccupiedSlotBehaviour.SwapItems)
			{
				if (index >= 0 && index < invInstances.Count)
				{
					InvInstance existingInstance = invInstances[index];
					if (InvInstance.IsValid (existingInstance) && !existingInstance.InvItem.canCarryMultiple && addInstance.GetSource () == KickStarter.runtimeInventory.PlayerInvCollection && KickStarter.runtimeInventory.PlayerInvCollection.Contains (existingInstance.ItemID))
					{
						// Already carrying clicked item, don't swap
						occupiedSlotBehaviour = OccupiedSlotBehaviour.ShiftItems;
					}
				}
			}

			// Adds to a specific index, or the end/first empty slot if -1
			if (!CanAccept (addInstance, index, occupiedSlotBehaviour))
			{
				if (InvInstance.IsValid (addInstance))
				{
					if (KickStarter.eventManager) KickStarter.eventManager.Call_OnUseContainerFail (addInstance.GetSourceContainer (), addInstance);
				}
				return null;
			}

			InvInstance addedInstance = null;
			
			if (Contains (addInstance))
			{
				if (!CanReorder ())
				{
					return addInstance;
				}

				if (MaxSlots > 0 && index >= MaxSlots)
				{
					return addInstance;
				}

				occupiedSlotBehaviour = OccupiedSlotBehaviour.SwapItems;
			}

			int numAdded = -1;

			InvCollection fromCollection = Contains (addInstance) ? this : addInstance.GetSource ();
			if (index >= 0 && index < invInstances.Count)
			{
				// Inside
				InvInstance existingInstance = invInstances[index];

				if (!InvInstance.IsValid (existingInstance))
				{
					// Empty slot
					addedInstance = addInstance.CreateTransferInstance ();

					if (InvInstance.IsValid (addedInstance))
					{
						numAdded = addedInstance.Count;
						invInstances[index] = addedInstance;
					}
				}
				else
				{
					if (existingInstance == addInstance)
					{
						// Same
						return existingInstance;
					}
					else if (existingInstance.IsMatch (addInstance, matchPropertiesWhenMerging) && addInstance.InvItem.canCarryMultiple && existingInstance.Capacity > 0)
					{
						// Merge
						if (addInstance.TransferCount > existingInstance.Capacity) addInstance.TransferCount = existingInstance.Capacity;
						numAdded = Mathf.Min (addInstance.CreateTransferInstance ().Count, existingInstance.Capacity);
						existingInstance.Count += numAdded;
						addedInstance = existingInstance;
					}
					else
					{
						switch (occupiedSlotBehaviour)
						{
							case OccupiedSlotBehaviour.ShiftItems:
								invInstances.Insert (index, addInstance.CreateTransferInstance ());
								addedInstance = invInstances[index];
								break;

							case OccupiedSlotBehaviour.FailTransfer:
								if (InvInstance.IsValid (addInstance))
								{
									if (KickStarter.eventManager) KickStarter.eventManager.Call_OnUseContainerFail (addInstance.GetSourceContainer (), addInstance);
									return null;
								}
								break;

							case OccupiedSlotBehaviour.SwapItems:
								if (fromCollection != null)
								{
									if (addInstance.IsPartialTransfer ())
									{
										if (KickStarter.eventManager) KickStarter.eventManager.Call_OnUseContainerFail (addInstance.GetSourceContainer (), addInstance);
										return null;
									}
									
									fromCollection.invInstances[fromCollection.IndexOf (addInstance)] = existingInstance;
									invInstances[index] = addInstance;
									addedInstance = invInstances[index];

									if (KickStarter.runtimeInventory.SelectedInstance == addInstance)
									{
										KickStarter.runtimeInventory.SelectItem (existingInstance);
									}
								}
								break;

							case OccupiedSlotBehaviour.Overwrite:
								if (KickStarter.eventManager) KickStarter.eventManager.Call_OnChangeInventory (this, existingInstance, InventoryEventType.Remove);
								invInstances[index] = addInstance.CreateTransferInstance ();
								addedInstance = invInstances[index];
								break;

							default:
								break;
						}
					}
				}
			}
			else
			{
				// Add to first empty slot, or end
				bool addedInside = false;

				if (index < 0)
				{
					// Find first empty slot
					for (int i=0; i<invInstances.Count; i++)
					{
						if (!InvInstance.IsValid (invInstances[i]))
						{
							invInstances[i] = addInstance.CreateTransferInstance ();
							addedInstance = invInstances[i];
							index = i;
							addedInside = true;
							break;
						}
						else if (invInstances[i] == addInstance)
						{
							return addInstance;
						}
					}
				}

				if (!addedInside)
				{
					if (maxSlots > 0 && invInstances.Count >= maxSlots)
					{
						return null;
					}

					if (index > 0 && CanReorder ())
					{
						while (invInstances.Count < index)
						{
							invInstances.Add (null);
						}
					}

					invInstances.Add (addInstance.CreateTransferInstance ());
					addedInstance = invInstances[invInstances.Count-1];
				}
			}

			if (fromCollection != null && fromCollection != this)
			{
				fromCollection.Clean ();
			}

			Clean ();
			PlayerMenus.ResetInventoryBoxes ();

			if (KickStarter.eventManager)
			{
				if (numAdded >= 0)
				{
					KickStarter.eventManager.Call_OnChangeInventory (this, addedInstance, InventoryEventType.Add, numAdded);
				}
				else
				{
					KickStarter.eventManager.Call_OnChangeInventory (this, addedInstance, InventoryEventType.Add);
				}
			}
			return addedInstance;
		}


		/**
		 * <summary>Deletes a given inventory item instance, provided it is a part of this collection</summary>
		 * <param name="invInstance">The inventory item instance to delete</param>
		 */
		public void Delete (InvInstance invInstance)
		{
			if (invInstances.Contains (invInstance))
			{
				invInstance.Clear ();
				
				Clean ();
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Deletes a given inventory item instance, provided it is a part of this collection</summary>
		 * <param name="invInstance">The inventory item instance to delete</param>
		 * <param name="amount">The amount to delete</param>
		 */
		public void Delete (InvInstance invInstance, int amount)
		{
			if (invInstances.Contains (invInstance))
			{
				invInstance.Clear (amount);
				
				Clean ();
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Deletes a set number of inventory item instances</summary>
		 * <param name="itemID">The ID of the inventory item to delete</param>
		 * <param name="amount">The amount to delete</param>
		 */
		public void Delete (int itemID, int amount)
		{
			if (KickStarter.inventoryManager == null) return;

			InvItem itemToRemove = KickStarter.inventoryManager.GetItem (itemID);
			if (itemToRemove == null) return;

			for (int i = 0; i < invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].ItemID == itemID)
				{
					// Count check
					if (itemToRemove.canCarryMultiple)
					{
						int diff = invInstances[i].Count - amount;
						if (diff >= 0)
						{
							invInstances[i].Clear (amount);
							amount = 0;
						}
						else
						{
							amount -= invInstances[i].Count;
							invInstances[i].Clear ();
						}
					}

					if (!itemToRemove.canCarryMultiple || invInstances[i].Count <= 0)
					{
						invInstances[i].Clear ();
					}

					if (!itemToRemove.canCarryMultiple || amount <= 0)
					{
						break;
					}
				}
			}

			Clean ();
			PlayerMenus.ResetInventoryBoxes ();
		}


		/**
		 * <summary>Deletes a set number of inventory item instances at a given index</summary>
		 * <param name="index">The index to delete from</param>
		 * <param name="amount">The amount to delete</param>
		 */
		public void DeleteAtIndex (int index, int amount)
		{
			if (index > 0 && index < invInstances.Count && InvInstance.IsValid (invInstances[index]))
			{
				invInstances[index].Clear (amount);

				Clean ();
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Deletes all inventory item instances associated with a given inventory item</summary>
		 * <param name="itemID">The ID of the inventory item to delete</param>
		 */
		public void DeleteAllOfType (int itemID)
		{
			if (KickStarter.inventoryManager == null) return;

			InvItem itemToRemove = KickStarter.inventoryManager.GetItem (itemID);
			if (itemToRemove == null) return;

			for (int i = 0; i < invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].ItemID == itemID)
				{
					invInstances[i].Clear ();
				}
			}

			Clean ();
			PlayerMenus.ResetInventoryBoxes ();
		}


		/** Deletes all item instances in the collection */
		public void DeleteAll ()
		{
			invInstances.Clear ();
			Clean ();
			PlayerMenus.ResetInventoryBoxes ();
		}


		/**
		 * <summary>Deletes all item instances in a specific category</summary>
		 * <param name="categoryID">The ID of the category to remove items from</param>
		 */
		public void DeleteAllInCategory (int categoryID)
		{
			for (int i = 0; i < invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].InvItem.binID == categoryID)
				{
					invInstances[i] = null;
					i = -1;
				}
			}
			Clean ();
		}


		/**
		 * <summary>Deletes all item instances that make up a recipe's ingredients</summary>
		 * <param name="recipe">The recipe to delete ingredients from</param>
		 */
		public void DeleteRecipeIngredients (Recipe recipe)
		{
			if (recipe.useSpecificSlots)
			{
				for (int i = 0; i < recipe.ingredients.Count; i++)
				{
					int numToRemove = recipe.ingredients[i].Amount;

					if (i >= 0 && i < invInstances.Count && InvInstance.IsValid (invInstances[i]))
					{
						invInstances[i].Count -= numToRemove;
					}
				}

				Clean ();
				PlayerMenus.ResetInventoryBoxes ();
			}
			else
			{
				foreach (Ingredient ingredient in recipe.ingredients)
				{
					int itemIDToRemove = ingredient.ItemID;
					int numToRemove = ingredient.Amount;

					Delete (itemIDToRemove, numToRemove);
				}
			}
		}


		/** Gets the index of a given inventory item instance */
		public int IndexOf (InvInstance invInstance)
		{
			return invInstances.IndexOf (invInstance);
		}


		/** Generates a single string that represents the class's saveable data */
		public string GetSaveData ()
		{
			if (invInstances == null) return string.Empty;

			System.Text.StringBuilder inventoryString = new System.Text.StringBuilder ();

			foreach (InvInstance invInstance in invInstances)
			{
				inventoryString.Append (InvInstance.GetSaveData (invInstance));
			}

			if (invInstances.Count > 0)
			{
				inventoryString.Remove (inventoryString.Length - 1, 1);
			}
			return inventoryString.ToString ();
		}


		/**
		 * <summary>Gets the inventory item instance in the collection at a given index</summary>
		 * <param name="index">The index to get</param>
		 * <returns>The inventory item instance at the index</returns>
		 */
		public InvInstance GetInstanceAtIndex (int index)
		{
			if (index >= 0 && index < invInstances.Count)
			{
				if (InvInstance.IsValid (invInstances[index]))
				{
					return invInstances[index];
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the amount of instances in the collection that represent a given inventory item</summary>
		 * <param name="invID">The inventory item's ID number</param>
		 * <param name="includeMultipleInSameSlot">If True, then the result will account for multiple amounts of the item in a single slot</param>
		 * <returns>The amount of instances in the collection that represent the inventory item</returns>
		 */
		public int GetCount (int invID, bool includeMultipleInSameSlot = true)
		{
			int count = 0;
			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance) && invInstance.ItemID == invID)
				{
					if (includeMultipleInSameSlot && invInstance.InvItem.canCarryMultiple)
					{
						count += invInstance.Count;
					}
					else
					{
						count++;
					}
				}
			}
			return count;
		}


		/**
		 * <summary>Gets the amount of instances in the collection</summary>
		 * <param name="includeMultipleInSameSlot">If True, then the result will account for multiple amounts of an item in a single slot</param>
		 * <returns>The amount of instances in the collection</returns>
		 */
		public int GetCount (bool includeMultipleInSameSlot = true)
		{
			int count = 0;
			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance))
				{
					if (includeMultipleInSameSlot && invInstance.InvItem.canCarryMultiple)
					{
						count += invInstance.Count;
					}
					else
					{
						count ++;
					}
				}
			}
			return count;
		}


		/**
		 * <summary>Gets the amount of instances in the collection that represent an inventory item in a given category</summary>
		 * <param name="categoryID">The category's ID number.</param>
		 * <param name="includeMultipleInSameSlot">If True, then the result will account for multiple amounts of the item in a single slot</param>
		 * <returns>The amount of instances in the collection that represent inventory items in the category</returns>
		 */
		public int GetCountInCategory (int categoryID, bool includeMultipleInSameSlot = true)
		{
			int count = 0;

			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance) && invInstance.InvItem.binID == categoryID)
				{
					if (includeMultipleInSameSlot && invInstance.InvItem.canCarryMultiple)
					{
						count += invInstance.Count;
					}
					else
					{
						count++;
					}
				}
			}

			return count;
		}


		/** Checks if the collection contains a given inventory instance */
		public bool Contains (InvInstance invInstance)
		{
			return InvInstance.IsValid (invInstance) && invInstances.Contains (invInstance);
		}


		/** Checks if the collection contains an inventory instance associated with a given inventory item */
		public bool Contains (int invID)
		{
			foreach (InvInstance invInstance in invInstances)
			{ 
				if (InvInstance.IsValid (invInstance) && invInstance.ItemID == invID)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Gets the first-found inventory item instance in the collection that's associated with a specific inventory item</summary>
		 * <param name="invID">The ID number of the inventory item</param>
		 * <returns>The first-found inventory item instance</returns>
		 */
		public InvInstance GetFirstInstance (int invID)
		{
			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance) && invInstance.ItemID == invID)
				{
					return invInstance;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the first-found inventory item instance in the collection that's associated with a specific inventory item</summary>
		 * <param name="invName">The name of the inventory item</param>
		 * <returns>The first-found inventory item instance</returns>
		 */
		public InvInstance GetFirstInstance (string invName)
		{
			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance) && invInstance.InvItem.label == invName)
				{
					return invInstance;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets all inventory item instance in the collection that are associated with a specific inventory item</summary>
		 * <param name="invID">The ID of the inventory item</param>
		 * <returns>An array of inventory item instance</returns>
		 */
		public InvInstance[] GetAllInstances (int invID)
		{
			List<InvInstance> foundInstances = new List<InvInstance>();

			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance) && invInstance.ItemID == invID)
				{
					foundInstances.Add (invInstance);
				}
			}
			return foundInstances.ToArray ();
		}


		/**
		 * <summary>Gets all inventory item instance in the collection that are associated with a specific inventory item</summary>
		 * <param name="invName">The nameof the inventory item</param>
		 * <returns>An array of inventory item instance</returns>
		 */
		public InvInstance[] GetAllInstances (string invName)
		{
			List<InvInstance> foundInstances = new List<InvInstance> ();

			foreach (InvInstance invInstance in invInstances)
			{
				if (InvInstance.IsValid (invInstance) && invInstance.InvItem.label == invName)
				{
					foundInstances.Add (invInstance);
				}
			}
			return foundInstances.ToArray ();
		}


		/**
		 * <summary>Gets the total value of all instances of an Integer inventory property (e.g. currency) within a set of inventory items.</summary>
		 * <param name = "propertyID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Integer inventory property within the set of inventory items</returns>
		 */
		public int GetTotalIntProperty (int propertyID)
		{
			int result = 0;
			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;
				InvVar invVar = invInstance.GetProperty (propertyID);
				if (invVar != null && invVar.type == VariableType.Integer)
				{
					result += invVar.IntegerValue;
				}
			}
			return result;
		}


		/**
		 * <summary>Gets the total value of all instances of an Integer inventory property (e.g. currency) within a set of inventory items.</summary>
		 * <param name = "propertyName">The name of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Integer inventory property within the set of inventory items</returns>
		 */
		public int GetTotalIntProperty (string propertyName)
		{
			int result = 0;
			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;
				InvVar invVar = invInstance.GetProperty (propertyName);
				if (invVar != null && invVar.type == VariableType.Integer)
				{
					result += invVar.IntegerValue;
				}
			}
			return result;
		}


		/**
		 * <summary>Gets the total value of all instances of an Float inventory property (e.g. weight) within a set of inventory items.</summary>
		 * <param name = "propertyID">The ID number of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Float inventory property within the set of inventory items</returns>
		 */
		public float GetTotalFloatProperty (int propertyID)
		{
			float result = 0f;
			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;
				InvVar invVar = invInstance.GetProperty (propertyID);
				if (invVar != null && invVar.type == VariableType.Float)
				{
					result += invVar.FloatValue;
				}
			}
			return result;
		}


		/**
		 * <summary>Gets the total value of all instances of an Float inventory property (e.g. weight) within a set of inventory items.</summary>
		 * <param name = "propertyName">The name of the Inventory property (see InvVar) to get the total value of</param>
		 * <returns>The total value of all instances of the Float inventory property within the set of inventory items</returns>
		 */
		public float GetTotalFloatProperty (string propertyName)
		{
			float result = 0f;
			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;
				InvVar invVar = invInstance.GetProperty (propertyName);
				if (invVar != null && invVar.type == VariableType.Float)
				{
					result += invVar.FloatValue;
				}
			}
			return result;
		}


		/**
		 * <summary>Gets the total amount of given integer or float inventory property, found in the current inventory<summary>
		 * <param name = "propertyID">The ID of the integer or float inventory property</param>
		 * <returns>The total amount of the property's value</returns>
		 */
		public InvVar GetPropertyTotals (int propertyID)
		{
			InvVar originalVar = KickStarter.inventoryManager.GetProperty (propertyID);
			if (originalVar == null) return null;

			InvVar totalVar = new InvVar (propertyID, originalVar.type);

			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;
				InvVar var = invInstance.GetProperty (propertyID);
				if (var != null)
				{
					totalVar.TransferValues (var);
				}
			}
			return totalVar;
		}


		/**
		 * <summary>Gets an array of all carried inventory items in a given category</summary>
		 * <param name = "categoryID">The ID number of the category in question</param>
		 * <returns>An array of all carried inventory items in the category</returns>
		 */
		public InvInstance[] GetInstancesInCategory (int categoryID)
		{
			List<InvInstance> invList = new List<InvInstance> ();
			foreach (InvInstance invInstance in invInstances)
			{
				if (!InvInstance.IsValid (invInstance)) continue;
				if (invInstance.InvItem.binID == categoryID)
				{
					invList.Add (invInstance);
				}
			}

			return invList.ToArray ();
		}


		/** Checks if this collection represents the player's inventory */
		public bool IsPlayerInventory ()
		{
			return KickStarter.runtimeInventory && this == KickStarter.runtimeInventory.PlayerInvCollection;
		}


		/** Gets the Container that this collection represents, if appropriate */
		public Container GetSourceContainer ()
		{
			if (KickStarter.stateHandler)
			{
				foreach (Container container in KickStarter.stateHandler.Containers)
				{
					if (container && container.InvCollection == this)
					{
						return container;
					}
				}
			}
			return null;
		}

		#endregion


		#region PrivateFunctions

		private bool CanAccept (InvInstance invInstance, int slot = -1, OccupiedSlotBehaviour occupiedSlotBehaviour = OccupiedSlotBehaviour.ShiftItems, bool matchPropertiesWhenMerging = true)
		{
			if (!InvInstance.IsValid (invInstance))
			{
				return false;
			}

			if (invInstances.Contains (invInstance))
			{
				return true;
			}

			if (!invInstance.InvItem.canCarryMultiple && Contains (invInstance.InvItem.id))
			{ 
				return false;
			}

			if (ItemBlockedByCategory (invInstance.InvItem))
			{
				return false;
			}

			if (KickStarter.runtimeInventory != null && KickStarter.runtimeInventory.PlayerInvCollection == this)
			{
				Container sourceContainer = GetSourceContainer (invInstance);
				if (sourceContainer && !KickStarter.runtimeInventory.CanTransferContainerItemsToInventory (sourceContainer, invInstance))
				{ 
					return false;
				}
			}

			if (slot >= 0 && slot < invInstances.Count)
			{
				InvInstance existingInstance = invInstances[slot];

				if (!InvInstance.IsValid (existingInstance))
				{
					return true;
				}

				if (InvInstance.IsValid (existingInstance) && existingInstance.IsMatch (invInstance, matchPropertiesWhenMerging) && invInstance.InvItem.canCarryMultiple && existingInstance.Capacity >= invInstance.TransferCount)
				{
					return true;
				}
			}

			if (maxSlots > 0 && invInstances.Count >= maxSlots && ItemIsInEverySlot ())
			{
				if (slot < 0)
				{
					for (int i = 0; i < invInstances.Count; i++)
					{
						InvInstance existingInstance = invInstances[i];
						if (InvInstance.IsValid (existingInstance) && existingInstance.IsMatch (invInstance, matchPropertiesWhenMerging) && invInstance.InvItem.canCarryMultiple && existingInstance.Capacity >= invInstance.TransferCount)
						{
							return true;
						}
					}
				}

				// Full
				switch (occupiedSlotBehaviour)
				{
					case OccupiedSlotBehaviour.FailTransfer:
					case OccupiedSlotBehaviour.ShiftItems:
						return false;

					default:
						break;
				}
			}

			return true;
		}


		private bool ItemIsInEverySlot ()
		{
			for (int i = 0; i < invInstances.Count; i++)
			{
				if (!InvInstance.IsValid (invInstances[i]))
				{
					return false;
				}
			}
			return true;
		}


		private bool ItemBlockedByCategory (InvItem invItem)
		{
			if (limitToCategoryIDs == null || limitToCategoryIDs.Count == 0)
			{
				return false;
			}
			
			return !limitToCategoryIDs.Contains (invItem.binID);
		}


		private void Clean ()
		{
			// Limit max slots
			if (maxSlots > 0 && maxSlots < invInstances.Count)
			{
				invInstances.RemoveRange (maxSlots, invInstances.Count - maxSlots);
			}

			// Convert invalid to empty
			for (int i = 0; i < invInstances.Count; i++)
			{
				if (!InvInstance.IsValid (invInstances[i]))
				{
					invInstances[i] = null;
				}
			}

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

			// Remove empty slots inside, if re-ordering is disallowed
			if (!CanReorder ())
			{
				for (int i = 0; i < invInstances.Count; i++)
				{
					if (invInstances[i] == null)
					{
						invInstances.RemoveAt (i);
						i--;
					}
				}
			}

			// Separate by max count
			/*for (int i=0; i< invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].InvItem.canCarryMultiple && invInstances[i].Count > invInstances[i].InvItem.maxCount)
				{
					while (invInstances[i].Count > invInstances[i].InvItem.maxCount)
					{
						int surplus = invInstances[i].Count - invInstances[i].InvItem.maxCount;
						invInstances[i].TransferCount = Mathf.Clamp (surplus, 0, invInstances[i].InvItem.maxCount);
						

						Insert (invInstances[i], i+1);
					}
				}
			}*/
		}


		private Container GetSourceContainer (InvInstance invInstance)
		{
			if (!InvInstance.IsValid (invInstance))
			{
				return null;
			}

			if (KickStarter.stateHandler)
			{
				foreach (Container container in KickStarter.stateHandler.Containers)
				{
					if (container && container.InvCollection.Contains (invInstance))
					{
						return container;
					}
				}
			}
			return null;
		}


		private bool CanReorder ()
		{
			if (KickStarter.settingsManager.canReorderItems)
			{
				return true;
			}

			if (KickStarter.runtimeInventory)
			{
				InvCollection[] craftingInvCollections = KickStarter.runtimeInventory.CraftingInvCollections;
				foreach (InvCollection craftingInvCollection in craftingInvCollections)
				{
					if (craftingInvCollection == this)
					{
						return true;
					}
				}
			}
			return false;
		}

		#endregion


		#region StaticFunctions
		
		/** Create a new class isntance based on a serialised data string generated by the GetSaveData function */
		public static InvCollection LoadData (string data)
		{
			List<InvInstance> invInstances = new List<InvInstance> ();

			if (!string.IsNullOrEmpty (data))
			{
				string[] countArray = data.Split (SaveSystem.pipe[0]);
				foreach (string chunk in countArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);

					int _id = -2;
					if (int.TryParse (chunkData[0], out _id))
					{
						if (_id >= 0)
						{
							int _count = 0;
							int.TryParse (chunkData[1], out _count);

							string _propertyData = string.Empty;
							if (chunkData.Length > 2)
							{
								_propertyData = chunkData[2];
								if (_propertyData.StartsWith ("#")) _propertyData.Remove (0, 1);
								if (_propertyData.EndsWith ("#")) _propertyData.Remove (_propertyData.Length - 1, 1);
							}

							string _disabledInteractionData = string.Empty;
							if (chunkData.Length > 3)
							{
								_disabledInteractionData = chunkData[3];
								if (_disabledInteractionData.StartsWith ("#")) _disabledInteractionData.Remove (0, 1);
								if (_disabledInteractionData.EndsWith ("#")) _disabledInteractionData.Remove (_disabledInteractionData.Length - 1, 1);
							}

							string _disabledCombineData = string.Empty;
							if (chunkData.Length > 4)
							{
								_disabledCombineData = chunkData[4];
								if (_disabledInteractionData.StartsWith ("#")) _disabledInteractionData.Remove (0, 1);
								if (_disabledInteractionData.EndsWith ("#")) _disabledInteractionData.Remove (_disabledInteractionData.Length - 1, 1);
							}

							invInstances.Add (new InvInstance (_id, _count, _propertyData, _disabledInteractionData, _disabledCombineData));
						}
						else if (_id == -1)
						{
							invInstances.Add (null);
						}
					}
				}
			}

			return new InvCollection (invInstances, true);
		}

		#endregion


		#region GetSet

		/** All Inventory item instances in the collection */
		public List<InvInstance> InvInstances
		{
			get
			{
				return invInstances;
			}
		}


		/** A list of all Inventory Items represented in the collection. */
		public List<InvItem> InvItems
		{
			get
			{
				List<InvItem> invItemsList = new List<InvItem>();
				foreach (InvInstance invInstance in invInstances)
				{
					if (InvInstance.IsValid (invInstance))
					{
						invItemsList.Add (invInstance.InvItem);
					}
				}
				return invItemsList;
			}
		}


		/** If > 0, the maximum number of slots the collection has to store items */
		public int MaxSlots
		{
			get
			{
				return maxSlots;
			}
			set
			{
				maxSlots = value;
			}
		}

		#endregion

	}

}