/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CustomInteractionSystemExample.cs"
 * 
 *	This script serves as an example of how you can create a custom interaction system that can affect both Hotspots and inventory items.
 *	To use it, add it to any GameObject in your scene and set your game's 'Interaction method' to 'Custom Script'.
 *	Disabling the 'Left-click deselects active item?' will also prevent unwanted inventory item deselection.
 *
 *	During gameplay, you will now be able to cycle through Hotspots and inventory items and use/examine them by clicking buttons from an on-screen GUI.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script serves as an example of how you can create a custom interaction system that can affect both Hotspots and inventory items.
	 * To use it, add it to any GameObject in your scene and set your game's 'Interaction method' to 'Custom Script'.
	 * Disabling the 'Left-click deselects active item?' will also prevent unwanted inventory item deselection.
	 *
	 * During gameplay, you will now be able to cycle through Hotspots and inventory items and use/examine them by clicking buttons from an on-screen GUI.
	 */
	[AddComponentMenu("Adventure Creator/3rd-party/Custom interaction system example")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_custom_interaction_system_example.html")]
	public class CustomInteractionSystemExample : MonoBehaviour
	{

		private Hotspot[] sceneHotspots = new Hotspot[0];
		private Hotspot selectedHotspot = null;
		private int hotspotIndex = -1;
		private int inventoryItemIndex = -1;


		private void Start ()
		{
			// Gather the Hotspots in the scene, and report any necessary messages

			sceneHotspots = FindObjectsOfType <Hotspot>();

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.CustomScript)
			{
				ACDebug.LogWarning ("This script works best when the Settings Manager's 'Interaction method' field is set to 'Custom Script'.", this);
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.inventoryDisableLeft)
			{
				ACDebug.LogWarning ("This script works best when the Settings Manager's 'Left-click deselects active item?' field is unchecked.", this);
			}
		}


		private void OnGUI ()
		{
			if (KickStarter.stateHandler.gameState != GameState.Normal)
			{
				// Only show the GUI during normal gameplay
				return;
			}

			GUILayout.BeginArea (new Rect (0f, ACScreen.height * 0.2f, ACScreen.width * 0.3f, ACScreen.height * 0.8f));

			// Show the Hotspot GUI
			ShowHotspotGUI ();

			GUILayout.Space (10f);

			// Show the Inventory item GUI
			ShowInventoryGUI ();

			GUILayout.EndArea ();
		}


		private void ShowHotspotGUI ()
		{
			GUILayout.BeginVertical ("Button");

			GUILayout.Label ("Hotspots: " + sceneHotspots.Length + " found");
			if (GUILayout.Button ("Select next"))
			{
				SetNextHotspot ();
			}

			if (selectedHotspot != null)
			{
				GUILayout.Label ("Selected Hotspot: " + selectedHotspot.GetName (0));

				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Deselect"))
				{
					selectedHotspot = null;
					KickStarter.playerInteraction.SetActiveHotspot (null);
				}
				if (GUILayout.Button ("Use"))
				{
					selectedHotspot.RunUseInteraction ();
				}
				if (GUILayout.Button ("Examine"))
				{
					selectedHotspot.RunExamineInteraction ();
				}
				GUILayout.EndHorizontal ();
			}

			GUILayout.EndVertical ();
		}


		private void ShowInventoryGUI ()
		{
			GUILayout.BeginVertical ("Button");

			GUILayout.Label ("Inventory: " + KickStarter.runtimeInventory.GetNumberOfItemsCarried () + " found");
			if (GUILayout.Button ("Select next"))
			{
				SetNextInventoryItem ();
			}

			if (KickStarter.runtimeInventory.SelectedItem != null)
			{
				GUILayout.Label ("Selected item: " + KickStarter.runtimeInventory.SelectedItem.GetLabel (0));

				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Deselect"))
				{
					KickStarter.runtimeInventory.SetNull ();

				}
				if (GUILayout.Button ("Use"))
				{
					KickStarter.runtimeInventory.SelectedItem.RunUseInteraction ();
				}
				if (GUILayout.Button ("Examine"))
				{
					KickStarter.runtimeInventory.SelectedItem.RunExamineInteraction ();
				}

				if (selectedHotspot != null && GUILayout.Button ("Use on selected Hotspot"))
				{
					selectedHotspot.RunInventoryInteraction (KickStarter.runtimeInventory.SelectedInstance);
				}

				GUILayout.EndHorizontal ();
			}

			GUILayout.EndVertical ();
		}


		private void SetNextHotspot ()
		{
			// Cycle through the collection of Hotspots and select the next one

			selectedHotspot = null;

			if (sceneHotspots.Length > 0)
			{
				hotspotIndex ++;
				if (hotspotIndex >= sceneHotspots.Length)
				{
					hotspotIndex = 0;
				}
				else
				{
					selectedHotspot = sceneHotspots[hotspotIndex];
				}
			}

			KickStarter.playerInteraction.SetActiveHotspot (selectedHotspot);
		}


		private void SetNextInventoryItem ()
		{
			// Cycle through the collection of inventory items and select the next one

			if (KickStarter.runtimeInventory.localItems.Count > 0)
			{
				inventoryItemIndex ++;
				if (inventoryItemIndex >= KickStarter.runtimeInventory.localItems.Count)
				{
					inventoryItemIndex = 0;
				}

				InvItem invItem = KickStarter.runtimeInventory.localItems[inventoryItemIndex];
				KickStarter.runtimeInventory.SelectItem (new InvInstance (invItem));
			}
		}
								
	}

}