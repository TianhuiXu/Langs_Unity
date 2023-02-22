/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SaveData.cs"
 * 
 *	This script contains all the non-scene-specific data we wish to save.
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
	 * A data container for all global data that gets stored in save games.
	 */
	[System.Serializable]
	public class SaveData
	{

		/** An instance of the MainData class */
		public MainData mainData;
		/** Instances of PlayerData for each of the game's Players */
		public List<PlayerData> playerData = new List<PlayerData>();

		/** The default Constructor. */
		public SaveData () { }


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			EditorGUILayout.LabelField ("Main data:", CustomStyles.subHeader);
			mainData.ShowGUI ();
			if (playerData != null)
			{
				for (int i=0; i<playerData.Count; i++)
				{
					GUILayout.Box (string.Empty, GUILayout.ExpandWidth (true), GUILayout.Height (1));
					EditorGUILayout.LabelField ("Player data #" + i + ":", CustomStyles.subHeader);
					playerData[i].ShowGUI ();
				}
			}
		}

		#endif

	}


	/**
	 * A data container for all non-player global data that gets stored in save games.
	 * A single instance of this class is stored in SaveData by SaveSystem.
	 */
	[System.Serializable]
	public struct MainData
	{

		/** The ID number of the currently-active Player */
		public int currentPlayerID;
		/** The game's current timeScale */
		public float timeScale;

		/** The build index of the previous scene visited. This may not be the same as the actual Player's last-visited scene, if player-switching or local players are involved */
		public int previousSceneIndex;
		/** The name of the previous scene visited. This may not be the same as the actual Player's last-visited scene, if player-switching or local players are involved */
		public string previousSceneName;

		/** The current values of all Global Variables */
		public string runtimeVariablesData;
		/** All user-generated CustomToken variables */
		public string customTokenData;

		/** The locked state of all Menu variables */
		public string menuLockData;
		/** The visibility state of all Menu instances */
		public string menuVisibilityData;
		/** The visibility state of all MenuElement instances */
		public string menuElementVisibilityData;
		/** The page data of all MenuJournal instances */
		public string menuJournalData;

		/** The Constant ID number of the currently-active ArrowPrompt */
		public int activeArrows;
		/** The Constant ID number of the currently-active Conversation */
		public int activeConversation;

		/** The ID number of the currently-selected InvItem */
		public int selectedInventoryID;
		/** True if the currently-selected InvItem is in "give" mode, as opposed to "use" */
		public bool isGivingItem;

		/** True if the cursor system, PlayerCursor, is disabled */
		public bool cursorIsOff;
		/** True if the input system, PlayerInput, is disabled */
		public bool inputIsOff;
		/** True if the interaction system, PlayerInteraction, is disabled */
		public bool interactionIsOff;
		/** True if the menu system, PlayerMenus, is disabled */
		public bool menuIsOff;
		/** True if the movement system, PlayerMovement, is disabled */
		public bool movementIsOff;
		/** True if the camera system is disabled */
		public bool cameraIsOff;
		/** True if Triggers are disabled */
		public bool triggerIsOff;
		/** True if Players are disabled */
		public bool playerIsOff;
		/** True if keyboard/controller can be used to control menus during gameplay */
		public bool canKeyboardControlMenusDuringGameplay;
		/** The state of the cursor toggle (1 = on, 2 = off) */
		public int toggleCursorState;

		/** The IDs and loop states of all queued music tracks, including the one currently-playing */
		public string musicQueueData;
		/** The IDs and loop states of the last set of queued music tracks */
		public string lastMusicQueueData;
		/** The time position of the current music track */
		public int musicTimeSamples;
		/** The time position of the last-played music track */
		public int lastMusicTimeSamples;
		/** The IDs and time positions of all tracks that have been played before */
		public string oldMusicTimeSamples;

		/** The IDs and loop states of all queued ambience tracks, including the one currently-playing */
		public string ambienceQueueData;
		/** The IDs and loop states of the last set of queued ambience tracks */
		public string lastAmbienceQueueData;
		/** The time position of the current ambience track */
		public int ambienceTimeSamples;
		/** The time position of the last-played ambience track */
		public int lastAmbienceTimeSamples;
		/** The IDs and time positions of all ambience tracks that have been played before */
		public string oldAmbienceTimeSamples;

		/** The currently-set AC_MovementMethod enum, converted to an integer */
		public int movementMethod;
		/** Data regarding paused and skipping ActionList assets */
		public string activeAssetLists;
		/** Data regarding active inputs */
		public string activeInputsData;
		/** Data regarding timers */
		public string timersData;

		/** Data regarding which speech lines, that can only be spoken once, have already been spoken */
		public string spokenLinesData;

		/** A record of the current global objectives */
		public string globalObjectivesData;

		/** Save data for any Remember components attached that are persistent, but not associated with the Player */
		public List<ScriptData> persistentScriptData;


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.MultiLineLabelGUI ("Current Player ID:", currentPlayerID.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("TimeScale:", timeScale.ToString ());

			if (KickStarter.variablesManager)
			{
				EditorGUILayout.LabelField ("Global Variables:");

				List<GVar> linkedVariables = SaveSystem.UnloadVariablesData (runtimeVariablesData, false, KickStarter.variablesManager.vars);
				foreach (GVar linkedVariable in linkedVariables)
				{
					if (linkedVariable.link != VarLink.OptionsData)
					{
						EditorGUILayout.LabelField ("   " + linkedVariable.label + ":", linkedVariable.GetValue ());
					}
				}
			}
			else
			{
				CustomGUILayout.MultiLineLabelGUI ("Global Variables:", runtimeVariablesData);
			}

			EditorGUILayout.LabelField ("Menus:");
			CustomGUILayout.MultiLineLabelGUI ("   Menu locks:", menuLockData);
			CustomGUILayout.MultiLineLabelGUI ("   Menu visibility:", menuVisibilityData);
			CustomGUILayout.MultiLineLabelGUI ("   Menu element visibility:", menuElementVisibilityData);
			CustomGUILayout.MultiLineLabelGUI ("   Menu journal pages:", menuJournalData);
			CustomGUILayout.MultiLineLabelGUI ("   Direct-control gameplay?", canKeyboardControlMenusDuringGameplay.ToString ());

			EditorGUILayout.LabelField ("Inventory:");
			CustomGUILayout.MultiLineLabelGUI ("   Selected InvItem ID:", selectedInventoryID.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Is giving item?", isGivingItem.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Global objectives:", globalObjectivesData);

			EditorGUILayout.LabelField ("Systems:");
			CustomGUILayout.MultiLineLabelGUI ("   Cursors disabled?", cursorIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Input disabled?", inputIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Interaction disabled?", interactionIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Menus disabled?", menuIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Movement disabled?", movementIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Cameras disabled?", cameraIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Triggers disabled", triggerIsOff.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Players disabled?", playerIsOff.ToString ());

			CustomGUILayout.MultiLineLabelGUI ("Toggle cursor state:", toggleCursorState.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Movement method:", ((MovementMethod) movementMethod).ToString ());

			EditorGUILayout.LabelField ("Music:");
			CustomGUILayout.MultiLineLabelGUI ("   Music queue data:", musicQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Last music data:", lastMusicQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Music time samples:", musicTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last music samples:", lastMusicTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Old music samples:", oldMusicTimeSamples);

			EditorGUILayout.LabelField ("Ambience:");
			CustomGUILayout.MultiLineLabelGUI ("   Ambience queue data:", ambienceQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Last ambience data:", lastAmbienceQueueData);
			CustomGUILayout.MultiLineLabelGUI ("   Ambience time samples:", ambienceTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Last ambience samples:", lastAmbienceTimeSamples.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Old ambience samples:", oldAmbienceTimeSamples);

			EditorGUILayout.LabelField ("Speech:");
			CustomGUILayout.MultiLineLabelGUI ("Custom tokens:", customTokenData);
			CustomGUILayout.MultiLineLabelGUI ("Spoken lines:", spokenLinesData);

			EditorGUILayout.LabelField ("Active logic:");
			CustomGUILayout.MultiLineLabelGUI ("   Active Conversation:", activeConversation.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Active ArrowPrompt:", activeArrows.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("   Active ActionList assets:", activeAssetLists);
			CustomGUILayout.MultiLineLabelGUI ("   Active inputs:", activeInputsData);
			CustomGUILayout.MultiLineLabelGUI ("   Timers:", timersData);

			if (persistentScriptData != null && persistentScriptData.Count > 0)
			{
				EditorGUILayout.LabelField ("Persistent data:");
				foreach (ScriptData scriptData in persistentScriptData)
				{
					RememberData rememberData = SaveSystem.FileFormatHandler.DeserializeObject<RememberData> (scriptData.data);
					if (rememberData != null)
					{
						CustomGUILayout.MultiLineLabelGUI ("   " + rememberData.GetType ().ToString () + ":", EditorJsonUtility.ToJson (rememberData, true));
					}
				}
			}
		}

		#endif

	}
	
}