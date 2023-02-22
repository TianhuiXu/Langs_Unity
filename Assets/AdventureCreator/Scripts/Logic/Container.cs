/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Container.cs"
 * 
 *	This script is used to store a set of
 *	Inventory items in the scene, to be
 *	either taken or added to by the player.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component that is used to store a local set of inventory items within a scene.
	 * The items stored here are separate to those held by the player, who can retrieve or place items in here for safe-keeping.
	 */
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_container.html")]
	public class Container : MonoBehaviour, ITranslatable, IItemReferencer
	{

		#region Variables

		/** The Containers default list of items */
		public List<ContainerItem> items = new List<ContainerItem> ();
		/** If True, only inventory items (InvItem) with a specific category will be displayed */
		public bool limitToCategory;
		/** The category IDs to limit the display of inventory items by, if limitToCategory = True */
		public List<int> categoryIDs = new List<int> ();
		/** If > 0, the maximum number of item slots the Container can hold */
		public int maxSlots = 0;
		/** If True, and maxSlots > 0, then attempting to place an item in the Container when full will result in the item being swapped with that in the occupied slot */
		public bool swapIfFull = false;

		/** The Container's label text */
		public string label;
		/** A unique identifier for the label's translation */
		public int labelLineID = -1;

		protected InvCollection invCollection = new InvCollection ();

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			CreateDefaultInstances ();
		}


		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}

		#endregion


		#region PublicFunctions

		/** Activates the Container.  If a Menu with an appearType = AppearType.OnContainer, it will be enabled and show the Container's contents. */
		public void Interact ()
		{
			if (gameObject.activeInHierarchy)
			{
				KickStarter.playerInput.activeContainer = this;
			}
			else
			{
				ACDebug.LogWarning ("Cannot open the Container " + this.name + " because its GameObject is disabled", this);
			}
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents.</summary>
		 * <param name = "_id">The ID number of the InvItem to add</param>
		 * <param name = "amount">How many instances of the inventory item to add</param>
		 */
		public void Add (int _id, int amount)
		{
			invCollection.Add (new InvInstance (_id, amount));
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents.</summary>
		 * <param name = "addInstance">The instance of the InvItem to add</param>
		 */
		public void Add (InvInstance addInstance)
		{
			invCollection.Add (addInstance);
		}


		public void Remove (int itemID, int amount)
		{
			invCollection.Delete (itemID, amount);
		}


		public void Remove (InvInstance invInstance)
		{
			invCollection.Delete (invInstance);
		}


		public void Remove (int itemID)
		{
			invCollection.DeleteAllOfType (itemID);
		}


		/** Removes all inventory items from the Container's contents. */
		public void RemoveAll ()
		{
			invCollection.DeleteAll ();
		}


		/**
		 * <summary>Gets the number of instances of a particular inventory item stored within the Container.</summary>
		 * <param name = "invID">The ID number of the InvItem to search for</param>
		 * <returns>The number of instances of the inventory item stored within the Container</returns>
		 */
		public int GetCount (int invID)
		{
			return invCollection.GetCount (invID);
		}


		/**
		 * <summary>Adds an inventory item to the Container's contents, at a particular index.</summary>
		 * <param name = "itemInstance">The instance of the item to place within the Container</param>
		 * <param name = "_index">The index number within the Container's current contents to insert the new item</param>
		 * <param name = "count">If >0, the quantity of the item to be added. Otherwise, the same quantity as _item will be added</param>
		 */
		public void InsertAt (InvInstance itemInstance, int index, int amountOverride = 0)
		{
			if (!InvInstance.IsValid (itemInstance)) return;

			itemInstance.TransferCount = (amountOverride > 0) ? amountOverride : 0;
			invCollection.Insert (itemInstance, index);
		}


		/**
		 * <summary>Gets the Container's label text in a given language</summary>
		 * <param name = "languageNumber">The language index, where 0 = the game's default language</param>
		 * <returns>The label text</returns>
		 */
		public string GetLabel (int languageNumber = 0)
		{
			return KickStarter.runtimeLanguages.GetTranslation (label, labelLineID, languageNumber, GetTranslationType (0));
		}


		#if UNITY_EDITOR

		public int GetNumItemReferences (int itemID)
		{
			int numReferences = 0;
			foreach (ContainerItem containerItem in items)
			{
				if (containerItem.ItemID == itemID)
				{
					numReferences ++;
				}
			}
			return numReferences;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID)
		{
			int numReferences = 0;
			foreach (ContainerItem containerItem in items)
			{
				if (containerItem.ItemID == oldItemID)
				{
					containerItem.ItemID = newItemID;
					numReferences++;
				}
			}
			return numReferences;
		}

		#endif

		#endregion


		#region ProtectedFunctions

		protected void CreateDefaultInstances ()
		{
			invCollection = new InvCollection (this);
		}

		#endregion


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return label;
		}


		public int GetTranslationID (int index)
		{
			return labelLineID;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Container;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			label = updatedText;
		}


		public int GetNumTranslatables ()
		{
			return 1;
		}


		public bool HasExistingTranslation (int index)
		{
			return (labelLineID > -1);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			labelLineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public bool CanTranslate (int index)
		{
			return (!string.IsNullOrEmpty (label));
		}

		#endif

		#endregion


		#region GetSet

		/** The total number of items */
		public int Count
		{
			get
			{
				return invCollection.GetCount (true);
			}
		}


		/** The total number of filled slots */
		public int FilledSlots
		{
			get
			{
				return invCollection.GetCount (false);
			}
		}


		/** The Container's associated InvCollection, where item data is stored */
		public InvCollection InvCollection
		{
			get
			{
				return invCollection;
			}
			set
			{
				invCollection = value;
			}
		}


		/** Returns True if the number of filled slots is that of the maxSlots, if maxSlots > 0 */
		public bool IsFull
		{
			get
			{
				if (maxSlots > 0)
				{
					return (FilledSlots >= maxSlots);
				}
				return false;
			}
		}

		#endregion

	}

}