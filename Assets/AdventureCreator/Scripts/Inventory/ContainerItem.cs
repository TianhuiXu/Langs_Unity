/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ContainerItem.cs"
 * 
 *	This script is a container class for inventory items stored in a Container.
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AC
{

	/** A data container for an inventory item stored within a Container. */
	[System.Serializable]
	public class ContainerItem
	{

		#region Variables

		[SerializeField] private int linkedID = -1;
		[SerializeField] private int count;
		[SerializeField] int id;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_linkedID">The ID number of the associated inventory item (InvItem) being stored</param>
		 * <param name = "otherItems">An array of existing items, so that a unique ID can be generated</param>
		 */
		public ContainerItem (int _linkedID, ContainerItem[] otherItems)
		{
			count = 1;
			linkedID = _linkedID;
			id = 0;
			
			// Update id based on array
			foreach (ContainerItem otherItem in otherItems)
			{
				if (id == otherItem.id)
					id ++;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI (InventoryManager inventoryManager)
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Item name:", GUILayout.Width (80f));

			string itemName = inventoryManager.GetLabel (linkedID);
			if (string.IsNullOrEmpty (itemName))
			{
				if (linkedID < 0)
				{
					itemName = "(Empty)";
				}
				else
				{
					itemName = "(Missing)";
				}
			}
			else
			{
				itemName = "'" + itemName + "'";
			}
			
			InvItem linkedItem = inventoryManager.GetItem (linkedID);
			if (linkedItem != null && linkedItem.canCarryMultiple)
			{
				EditorGUILayout.LabelField (itemName, EditorStyles.boldLabel, GUILayout.Width (135f));

				EditorGUILayout.LabelField ("Count:", GUILayout.Width (50f));
				count = EditorGUILayout.IntField (count, GUILayout.Width (44f));
				if (count <= 0) count = 1;
			}
			else
			{
				EditorGUILayout.LabelField (itemName, EditorStyles.boldLabel);
				count = 1;
			}
		}

		#endif


		#region GetSet

		/** The ID number of the associated inventory item (InvItem) being stored */
		public int ItemID
		{
			get
			{
				return linkedID;
			}
			#if UNITY_EDITOR
			set
			{
				linkedID = value;
			}
			#endif
		}


		/** How many instances of the item are being stored, if the InvItem's canCarryMultiple = True */
		public int Count
		{
			get
			{
				return count;
			}
			#if UNITY_EDITOR
			set
			{
				count = value;
			}
			#endif
		}


		public InvItem InvItem
		{
			get
			{
				if (KickStarter.inventoryManager)
				{
					return KickStarter.inventoryManager.GetItem (linkedID);
				}
				return null;
			}
		}

		#endregion

	}

}