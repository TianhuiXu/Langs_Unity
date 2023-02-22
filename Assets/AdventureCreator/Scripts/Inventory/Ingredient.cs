/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Ingredient.cs"
 * 
 *	A data container for an ingredient within a Recipe.
 *	If multiple instances of the associated inventory item can be carried, then the amount needed of that item can also be set.
 * 
 */

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container for an ingredient within a Recipe.
	 * If multiple instances of the associated inventory item can be carried, then the amount needed of that item can also be set.
	 */
	[System.Serializable]
	public class Ingredient
	{

		#region Variables

		[SerializeField] private int itemID = 0;
		[SerializeField] private int amount = 0;
		[SerializeField] private int slotNumber = 1;
		
		#endregion


		#region Constructor

		/** The default Constructor. */
		public Ingredient ()
		{
			itemID = 0;
			amount = 1;
			slotNumber = 1;
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI (int itemIndex, string[] labelList, string apiPrefix, Recipe recipe)
		{
			itemIndex = CustomGUILayout.Popup ("Item:", itemIndex, labelList, apiPrefix + ".ingredients [" + recipe.ingredients.IndexOf (this) + "].itemID");
			itemID = KickStarter.inventoryManager.items[itemIndex].id;

			if (InvItem.canCarryMultiple)
			{
				if (recipe.useSpecificSlots && InvItem.maxCount == 1)
				{
					// Don't show amount in this case: make other ingredients of same item
					amount = 1;
				}
				else
				{
					amount = EditorGUILayout.IntField ("Amount:", amount);
					if (amount < 1) amount = 1;
				}
			}

			if (recipe.useSpecificSlots)
			{
				slotNumber = EditorGUILayout.IntField ("Crafting slot:", slotNumber);
			}
		}


		public string EditorLabel
		{
			get
			{
				if (KickStarter.inventoryManager)
				{
					InvItem item = KickStarter.inventoryManager.GetItem (itemID);
					if (item != null) return item.label;
				}
				return "(No item)";
			}
		}

		#endif


		#region GetSet
		
		/** The associated Inventory item */
		public InvItem InvItem
		{
			get
			{
				if (KickStarter.inventoryManager)
				{
					return KickStarter.inventoryManager.GetItem (itemID);
				}
				return null;
			}
		}

		/** The ID number of the associated Inventory item */
		public int ItemID
		{
			get
			{
				return itemID;
			}
			#if UNITY_EDITOR
			set
			{
				itemID = value;
			}
			#endif
		}


		/** Which index in an InvCollection that the item must be placed in, if the Rrecipe's useSpecificSlots = True */
		public int CraftingIndex
		{
			get
			{
				return slotNumber - 1;
			}
		}


		/** The amount needed, if the InvItem's canCarryMultiple = True */
		public int Amount
		{
			get
			{
				InvItem invItem = InvItem;
				if (invItem != null && invItem.canCarryMultiple)
				{
					return amount;
				}
				return 1;
			}
		}

		#endregion

	}

}