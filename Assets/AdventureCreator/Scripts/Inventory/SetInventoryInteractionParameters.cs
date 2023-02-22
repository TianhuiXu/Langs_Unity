/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SetInventoryInteractionParameters.cs"
 * 
 *	A component used to set all of an Inventory interaction's parameters at the moment it is interacted with by the player.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A component used to set all of an Inventory interaction's parameters at the moment it is interacted with by the player. */
	[AddComponentMenu("Adventure Creator/Logic/Set Inventory Interaction parameters")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_set_inventory_interaction_parameters.html")]
	public class SetInventoryInteractionParameters : SetParametersBase
	{

		#region Variables

		[SerializeField] protected int itemID = 0;
		[SerializeField] protected int cursorIndex = 0;
		[SerializeField] protected InteractionType interactionType = InteractionType.Use;

		protected enum InteractionType { Use, Examine };

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			EventManager.OnInventoryInteract_Alt += OnInventoryInteract;
		}


		protected void OnDisable ()
		{
			EventManager.OnInventoryInteract_Alt -= OnInventoryInteract;
		}

		#endregion


		#region CustomEvents

		protected void OnInventoryInteract (InvInstance invInstance, int iconID)
		{
			if (invInstance.ItemID == itemID)
			{
				if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
				{
					if (cursorIndex < invInstance.Interactions.Length && invInstance.Interactions[cursorIndex].icon.id == iconID)
					{
						AssignParameterValues (invInstance.Interactions[cursorIndex].actionList);
					}
					return;
				}

				switch (interactionType)
				{
					case InteractionType.Use:
						if (iconID == 0)
						{
							AssignParameterValues (invInstance.InvItem.useActionList);
						}
						break;

					case InteractionType.Examine:
						if (iconID == KickStarter.cursorManager.lookCursor_ID)
						{
							AssignParameterValues (invInstance.InvItem.lookActionList);
						}
						break;

					default:
						break;
				}
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned in the AC Game Window.", MessageType.Warning);
				return;
			}

			InventoryManager inventoryManager = KickStarter.inventoryManager;
			if (inventoryManager == null)
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be assigned in the AC Game Window.", MessageType.Warning);
				return;
			}

			List<string> itemList = new List<string>();
			int itemIndex = 0;
			if (inventoryManager.items.Count > 0)
			{
				for (int i=0; i<inventoryManager.items.Count; i++)
				{
					itemList.Add (inventoryManager.items[i].label);
					if (inventoryManager.items[i].id == itemID)
					{
						itemIndex = i;
					}
				}
			}

			if (itemList.Count == 0)
			{
				EditorGUILayout.HelpBox ("No inventory items found!", MessageType.Warning);
				return;
			}

			itemIndex = EditorGUILayout.Popup ("Inventory item:", itemIndex, itemList.ToArray ());

			if (itemIndex >= inventoryManager.items.Count)
			{
				return;
			}

			itemID = inventoryManager.items[itemIndex].id;

			InvItem item = inventoryManager.GetItem (itemID);
			if (item != null)
			{
				if (settingsManager.interactionMethod != AC_InteractionMethod.ContextSensitive && settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
				{
					// Standard interactions

					CursorManager cursorManager = KickStarter.cursorManager;
					if (cursorManager == null)
					{
						EditorGUILayout.HelpBox ("A Cursor Manager must be assigned in the AC Game Window.", MessageType.Warning);
						return;
					}

					List<string> iconList = new List<string>();
					foreach (CursorIcon icon in cursorManager.cursorIcons)
					{
						iconList.Add (icon.label);
					}

					List<string> interactionList = new List<string>();
					for (int i=0; i<item.interactions.Count; i++)
					{
						int iconID = item.interactions[i].icon.id;
						int index = GetIconSlot (iconID, cursorManager.cursorIcons.ToArray ());
						string label = i.ToString () + ": " + cursorManager.cursorIcons[index].label;
						if (string.IsNullOrEmpty (label)) label = i.ToString () + ": Unnamed";
						interactionList.Add (label);
					}

					if (interactionList.Count == 0)
					{
						EditorGUILayout.HelpBox ("No standard interactions found!", MessageType.Warning);
						return;
					}

					cursorIndex = EditorGUILayout.Popup ("Interaction:", cursorIndex, interactionList.ToArray ());
					ShowParametersGUI (item.interactions[cursorIndex].actionList);
				}
				else
				{
					interactionType = (InteractionType) EditorGUILayout.EnumPopup ("Interaction type:", interactionType);
					switch (interactionType)
					{
						case InteractionType.Use:
							ShowParametersGUI (item.useActionList);
							break;

						case InteractionType.Examine:
							ShowParametersGUI (item.lookActionList);
							break;
					}
				}
			}
		}


		protected int GetIconSlot (int _id, CursorIcon[] cursorIcons)
		{
			int i = 0;
			foreach (CursorIcon icon in cursorIcons)
			{
				if (icon.id == _id)
				{
					return i;
				}
				i++;
			}
			
			return 0;
		}


		protected void ShowParametersGUI (ActionListAsset actionListAsset)
		{
			if (actionListAsset == null)
			{
				EditorGUILayout.HelpBox ("No " + interactionType.ToString () + " ActionList found!", MessageType.Warning);
				return;
			}

			if (actionListAsset.NumParameters > 0)
			{
				ShowActionListReference (actionListAsset);
				ShowParametersGUI (actionListAsset.DefaultParameters, true);
			}
			else
			{
				EditorGUILayout.HelpBox ("No parameters defined for ActionList Assset '" + actionListAsset.name + "'.", MessageType.Warning);
			}
		}


		protected void ShowActionListReference (ActionListAsset actionListAsset)
		{
			if (actionListAsset)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Asset file: " + actionListAsset);
				if (GUILayout.Button ("Ping", GUILayout.Width (50f)))
				{
					EditorGUIUtility.PingObject (actionListAsset);
				}
				EditorGUILayout.EndHorizontal ();
			}
		}

		#endif

	}

}