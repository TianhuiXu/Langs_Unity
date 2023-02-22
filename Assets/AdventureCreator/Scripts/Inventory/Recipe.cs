/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Recipe.cs"
 * 
 *	This script is a container class for recipes.
 * 
 */

using System.Collections.Generic;


namespace AC
{

	/**
	 * A data container for a recipe.
	 * A recipe requires multiple inventory items, optionally arranged in a specific pattern, and replaces them with a new inventory item.
	 */
	[System.Serializable]
	public class Recipe : IItemReferencer
	{

		#region Variables

		/** The recipe's editor name */
		public string label;
		/** A unique identifier */
		public int id;

		/** The required ingredients */
		public List<Ingredient> ingredients = new List<Ingredient> ();
		/** The ID number of the associated inventory item (InvItem) created when the recipe is complete */
		public int resultID;
		/** If True, then the ingredients must be placed in specific slots within a MenuCrafting element */
		public bool useSpecificSlots;
		/** What happens when the recipe is created (JustMoveToInventory, SelectItem, RunActionList) */
		public OnCreateRecipe onCreateRecipe = OnCreateRecipe.JustMoveToInventory;
		/** The ActionListAsset to run, if onCreateRecipe = OnCreateRecipe.RunActionList */
		public ActionListAsset invActionList;
		/** The ActionListAsset to run when the recipe is created */
		public ActionListAsset actionListOnCreate;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public Recipe (int[] idArray)
		{
			ingredients = new List<Ingredient> ();
			resultID = 0;
			useSpecificSlots = false;
			invActionList = null;
			onCreateRecipe = OnCreateRecipe.JustMoveToInventory;
			actionListOnCreate = null;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
					id++;
			}

			label = "Recipe " + (id + 1).ToString ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if a collection of inventory item instances has all the items necessary to craft this recipe's item.</summary>
		 * <param name="invCollection">The collection of inventory item instances to check</param>
		 * <returns>True if the collection has all the items necessary to craft the recipe's item</returns>
		 */
		public bool CanBeCrafted (InvCollection invCollection)
		{
			if (HasInvalidItems (invCollection))
			{
				return false;
			}

			if (useSpecificSlots)
			{
				int maxSlot = invCollection.InvInstances.Count-1;
				foreach (Ingredient ingredient in ingredients)
				{
					if (maxSlot < ingredient.CraftingIndex)
					{
						maxSlot = ingredient.CraftingIndex;
					}
				}
				
				if (maxSlot < 0)
				{
					return false;
				}

				for (int i=0; i<=maxSlot; i++)
				{
					Ingredient ingredient = GetIngredientForIndex (i);
					InvInstance invInstance = invCollection.GetInstanceAtIndex (i);

					if (InvInstance.IsValid (invInstance))
					{
						// Item in slot

						if (ingredient == null)
						{
							return false;
						}

						if (ingredient.ItemID != invInstance.ItemID)
						{
							return false;
						}

						if (ingredient.Amount > invInstance.Count)
						{
							return false;
						}
					}
					else
					{
						// Slot is empty

						if (ingredient != null)
						{
							return false;
						}
					}
				}
			}
			else
			{
				// Indices don't matter, just check counts are correct

				foreach (Ingredient ingredient in ingredients)
				{
					int ingredientCount = invCollection.GetCount (ingredient.ItemID);
					if (ingredientCount < ingredient.Amount)
					{
						return false;
					}
				}
			}
			return true;
		}

		#endregion


		#region PrivateFunctions

		private bool HasInvalidItems (InvCollection invCollection)
		{
			if (ingredients.Count == 0)
			{
				return true;
			}

			// Are any invalid ingredients present?
			List<InvItem> craftingItems = invCollection.InvItems;

			foreach (InvItem craftingItem in craftingItems)
			{
				if (!RequiresItem (craftingItem))
				{
					return true;
				}
			}
			return false;
		}


		private Ingredient GetIngredientForIndex (int index)
		{
			// No item in slot
			foreach (Ingredient ingredient in ingredients)
			{
				if (ingredient.CraftingIndex == index)
				{
					return ingredient;
				}
			}
			return null;
		}

		private bool RequiresItem (InvItem invItem)
		{
			if (invItem == null) return false;

			foreach (Ingredient ingredient in ingredients)
			{
				if (ingredient.ItemID == invItem.id)
				{
					return true;
				}
			}

			return false;
		}


		#endregion


		#if UNITY_EDITOR

		public string EditorLabel
		{
			get
			{
				return id.ToString () + " (" + label + ")";
			}
		}


		public int GetNumItemReferences (int itemID)
		{
			int numReferences = 0;
			if (resultID == itemID)
			{
				numReferences++;
			}
			foreach (Ingredient ingredient in ingredients)
			{
				if (ingredient.ItemID == itemID)
				{
					numReferences++;
				}
			}
			return numReferences;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID)
		{
			int numReferences = 0;
			if (resultID == oldItemID)
			{
				resultID = newItemID;
				numReferences++;
			}
			foreach (Ingredient ingredient in ingredients)
			{
				if (ingredient.ItemID == oldItemID)
				{
					ingredient.ItemID = newItemID;
					numReferences++;
				}
			}
			return numReferences;
		}

		#endif

	}

}