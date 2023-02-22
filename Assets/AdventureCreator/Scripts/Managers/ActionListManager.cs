/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionListManager.cs"
 * 
 *	This script keeps track of which ActionLists are running in a scene.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component keeps track of which ActionLists are running.
	 * When an ActionList runs or ends, it is passed to this script, which sets up the correct GameState in StateHandler.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_manager.html")]
	public class ActionListManager : MonoBehaviour
	{

		#region Variables

		/** If True, then the next time ActionConversation's Skip() function is called, it will be ignored */
		[HideInInspector] public bool ignoreNextConversationSkip = false;

		protected bool playCutsceneOnVarChange = false;
		protected bool saveAfterCutscene = false;

		protected int playerIDOnStartQueue;
		protected bool noPlayerOnStartQueue;

		protected List<ActiveList> activeLists = new List<ActiveList>();

		#endregion


		#region UnityStandards

		private void OnEnable ()
		{
			EventManager.OnEndActionList += OnEndActionList;
			EventManager.OnEnterGameState += OnEnterGameState;
			EventManager.OnManuallyTurnACOff += KillAllLists;
		}

		private void OnDisable ()
		{
			EventManager.OnEndActionList -= OnEndActionList;
			EventManager.OnEnterGameState -= OnEnterGameState;
			EventManager.OnManuallyTurnACOff -= KillAllLists;
		}

		#endregion


		#region PublicFunctions

		/**
		 * Ends all skippable ActionLists.
		 * This is triggered when the user presses the "EndCutscene" Input button.
		 */
		public void EndCutscene ()
		{
			if (!IsInSkippableCutscene ())
			{
				return;
			}

			if (AdvGame.GetReferences ().settingsManager.blackOutWhenSkipping)
			{
				KickStarter.mainCamera.ForceOverlayForFrames (4);
			}

			KickStarter.eventManager.Call_OnSkipCutscene ();

			// Stop all non-looping sound
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				if (sound.GetComponent <AudioSource>())
				{
					if (sound.soundType != SoundType.Music && !sound.GetComponent <AudioSource>().loop)
					{
						sound.Stop ();
					}
				}
			}

			// Set correct Player prefab before skipping
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				if (!noPlayerOnStartQueue && playerIDOnStartQueue >= 0)
				{
					if (KickStarter.player == null || KickStarter.player.ID != playerIDOnStartQueue)
					{
						//
						PlayerPrefab oldPlayerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerIDOnStartQueue);
						if (oldPlayerPrefab != null)
						{
							if (KickStarter.player != null)
							{
								KickStarter.player.Halt ();
							}

							Player oldPlayer = oldPlayerPrefab.GetSceneInstance (true);
							KickStarter.player = oldPlayer;
						}
					}
				}
			}

			List<ActiveList> listsToSkip = new List<ActiveList>();
			List<ActiveList> listsToReset = new List<ActiveList>();

			foreach (ActiveList activeList in activeLists)
			{
				if (!activeList.inSkipQueue && activeList.actionList.IsSkippable ())
				{
					listsToReset.Add (activeList);
				}
				else
				{
					listsToSkip.Add (activeList);
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
			{
				if (!activeList.inSkipQueue && activeList.actionList.IsSkippable ())
				{
					listsToReset.Add (activeList);
				}
				else
				{
					listsToSkip.Add (activeList);
				}
			}

			foreach (ActiveList listToReset in listsToReset)
			{
				// Kill, but do isolated, to bypass setting GameState etc
				listToReset.Reset (true);
			}

			foreach (ActiveList listToSkip in listsToSkip)
			{
				listToSkip.Skip ();
			}
		}


		/**
		 * <summary>Checks if a particular ActionList is running.</summary>
		 * <param name = "actionList">The ActionList to search for</param>
		 * <returns>True if the ActionList is currently running</returns>
		 */
		public bool IsListRunning (ActionList actionList)
		{
			if (actionList == null) return false;

			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
				{
					if (activeList.IsFor (runtimeActionList))
					{
						if (activeList.IsRunning ())
						{
							return true;
						}
					}
				}
				return false;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					if (activeList.IsRunning ())
					{
						return true;
					}
				}
			}
			
			return false;
		}


		public bool IsListRegistered (ActionList actionList)
		{
			if (actionList == null) return false;

			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
				{
					if (activeList.IsFor (runtimeActionList))
					{
						return true;
					}
				}
				return false;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					return true;
				}
			}
			
			return false;
		}


		public bool CanResetSkipVars (ActionList actionList)
		{
			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
				{
					if (activeList.IsFor (runtimeActionList))
					{
						return activeList.CanResetSkipVars ();
					}
					if (activeList.IsFor (runtimeActionList.assetSource))
					{
						return activeList.CanResetSkipVars ();
					}
				}
				return true;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					return activeList.CanResetSkipVars ();
				}
			}
			
			return true;
		}


		/**
		 * <summary>Checks if any currently-running ActionLists pause gameplay.</summary>
		 * <param name = "_actionToIgnore">Any ActionList that contains this Action will be excluded from the check</param>
		 * <param name = "showSaveDebug">If True, and an ActionList is pausing gameplay, a Console warning will be given to explain why saving is not currently possible</param>
		 * <returns>True if any currently-running ActionLists pause gameplay</returns>
		 */
		public bool IsGameplayBlocked (Action _actionToIgnore = null, bool showSaveDebug = false)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList.actionListType == ActionListType.PauseGameplay && activeList.IsRunning ())
				{
					if (_actionToIgnore != null)
					{
						if (activeList.actionList.actions.Contains (_actionToIgnore))
						{
							continue;
						}
					}
					
					if (showSaveDebug)
					{
						ACDebug.LogWarning ("Cannot save at this time - the ActionList '" + activeList.actionList.name + "' is blocking gameplay.", activeList.actionList);
					}
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
			{
				if (activeList.actionList != null && activeList.actionList.actionListType == ActionListType.PauseGameplay && activeList.IsRunning ())
				{
					if (_actionToIgnore != null)
					{
						if (activeList.actionList.actions.Contains (_actionToIgnore))
						{
							continue;
						}
					}

					if (showSaveDebug)
					{
						ACDebug.LogWarning ("Cannot save at this time - the ActionListAsset '" + activeList.actionList.name + "' is blocking gameplay.", activeList.actionList);
					}
					return true;
				}
			}

			return false;
		}
		

		/**
		 * <summary>Checks if any currently-running ActionListAssets pause gameplay and unfreeze 'Pause' Menus.</summary>
		 * <returns>True if any currently-running ActionListAssets pause gameplay and unfreeze 'Pause' Menus.</returns>
		 */
		public bool IsGameplayBlockedAndUnfrozen ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.CanUnfreezePauseMenus () && activeList.IsRunning ())
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
			{
				if (activeList.CanUnfreezePauseMenus () && activeList.IsRunning ())
				{
					return true;
				}
			}
			return false;
		}
		
		
		/**
		 * <summary>Checks if any skippable ActionLists are currently running.</summary>
		 * <returns>True if any skippable ActionLists are currently running.</returns>
		 */
		public bool IsInSkippableCutscene ()
		{
			if (!IsGameplayBlocked ())
			{
				return false;
			}

			if (HasSkipQueue ())
			{
				return true;
			}

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning () && activeList.actionList.IsSkippable ())
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
			{
				if (activeList.IsRunning () && activeList.actionListAsset != null && activeList.actionListAsset.IsSkippable ())
				{
					return true;
				}
			}
			
			return false;
		}


		/**
		 * <summary>Adds a new ActionList, assumed to already be running, to the internal record of currently-running ActionLists, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "actionList">The ActionList to run</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be added to the list of ActionLists to skip</param>
		 * <param name = "_startIndex">The index number of the Action to start skipping from, if addToSkipQueue = True</param>
		 * <param name = "actionListAsset">The ActionListAsset that is the ActionList's source, if it has one.</param>
		 */
		public void AddToList (ActionList actionList, bool addToSkipQueue, int _startIndex)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					activeLists.RemoveAt (i);
				}
			}
			addToSkipQueue = CanAddToSkipQueue (actionList, addToSkipQueue);
			activeLists.Add (new ActiveList (actionList, addToSkipQueue, _startIndex));
		}
		

		/**
		 * <summary>Resets and removes a ActionList from the internal record of currently-running ActionLists, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "actionList">The ActionList to end</param>
		 */
		public void EndList (ActionList actionList)
		{
			if (actionList == null)
			{
				return;
			}
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					EndList (activeLists[i]);
					return;
				}
			}
		}


		/**
		 * <summary>Ends the ActionList or ActionListAsset associated with a given ActiveList data container</summary>
		 * <param name = "activeList">The ActiveList associated with the ActionList or ActionListAsset to end.</param>
		 */
		public void EndList (ActiveList activeList)
		{
			activeList.Reset (false);
			if (activeList.GetConversationOnEnd ())
			{
				ResetSkipVars ();
				activeList.RunConversation ();
			}
		}


		/** Inform ActionListManager that a Variable's value has changed. */
		public void VariableChanged ()
		{
			playCutsceneOnVarChange = true;
		}


		/** Ends all currently-running ActionLists and ActionListAssets. */
		public void KillAllLists ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				activeList.Reset (true);
			}
			foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
			{
				activeList.Reset (true);
			}
		}


		/**
		 * <summary>Ends all currently-running ActionLists present within a given scene.</summary>
		 * <param name = "sceneIndex">The index of the scene</param>
		 */
		public void KillAllFromScene (int sceneIndex)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList != null && UnityVersionHandler.GetSceneIndexFromGameObject (activeList.actionList.gameObject) == sceneIndex && activeList.actionListAsset == null)
				{
					activeList.Reset (true);
				}
			}
		}


		/**
		 * <summary>Ends all currently-running ActionLists present within a given scene.</summary>
		 * <param name = "sceneName">The name of the scene</param>
		 */
		public void KillAllFromScene (string sceneName)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.actionList != null && UnityVersionHandler.GetSceneNameFromGameObject (activeList.actionList.gameObject) == sceneName && activeList.actionListAsset == null)
				{
					activeList.Reset (true);
				}
			}
		}


		/**
		 * <summary>Clears all data about the current state of "skippable" Cutscenes, allowing you to prevent previously-run Cutscenes in the same block of gameplay-blocking ActionLists. Use with caution!</summary>
		 */
		public void ResetSkippableData ()
		{
			ResetSkipVars (true);
		}


		/**
		 * <summary>Checks if a given ActionList should be skipped when the 'EndCutscene' input is triggered.</summary>
		 * <param name = "actionList">The ActionList to check</param>
		 * <param name = "originalValue">If True, the user would like it to be skippable.</param>
		 * <returns>True if the ActionList can be skipped.</returns>
		 */
		public bool CanAddToSkipQueue (ActionList actionList, bool originalValue)
		{
			if (!actionList.IsSkippable ())
			{
				return false;
			}
			else if (!KickStarter.actionListManager.HasSkipQueue ()) // was InSkippableCutscene
			{
				if (KickStarter.player)
				{
					playerIDOnStartQueue = KickStarter.player.ID;
					noPlayerOnStartQueue = false;
				}
				else
				{
					//playerIDOnStartQueue = -1;
					noPlayerOnStartQueue = true;
				}
				return true;
			}
			return originalValue;
		}


		/**
		 * <summary>Records the Action indices that the associated ActionList was running before being paused. This data is sent to the ActionList's associated ActiveList</summary>
		 * <param name = "actionList">The ActionList that is being paused</param>
		 * <param name = "resumeIndices">An array of Action indices to run when the ActionList is resumed</param>
		 */
		public void AssignResumeIndices (ActionList actionList, int[] resumeIndices)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionList))
				{
					activeList.SetResumeIndices (resumeIndices);
				}
			}
		}


		/**
		 * <summary>Resumes a previously-paused ActionList. If the ActionList is already running, nothing will happen.</summary>
		 * <param name = "actionList">The ActionList to pause</param>
		 * <param name = "rerunPausedActions">If True, then any Actions that were midway-through running when the ActionList was paused will be restarted. Otherwise, the Actions that follow them will be reun instead.</param>
		 */
		public void Resume (ActionList actionList, bool rerunPausedActions)
		{
			if (IsListRunning (actionList))
			{
				return;
			}

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionList))
				{
					activeLists[i].Resume (null, rerunPausedActions);
					return;
				}
			}

			actionList.Interact ();
		}


		/**
		 * <summary>Generates a save-able string out of the ActionList resume data.<summary>
		 * <param name = "If set, only data for a given subscene will be saved. If null, only data for the active scene will be saved</param>
		 * <returns>A save-able string out of the ActionList resume data<returns>
		 */
		public string GetSaveData (SubScene subScene = null)
		{
			PurgeLists ();
			string localResumeData = "";
			for (int i=0; i<activeLists.Count; i++)
			{
				localResumeData += activeLists[i].GetSaveData (subScene);

				if (i < (activeLists.Count - 1))
				{
					localResumeData += SaveSystem.pipe;
				}
			}
			return localResumeData;
		}


		/**
		 * <summary>Recreates ActionList resume data from a saved data string.</summary>
		 * <param name = "If set, the data is for a subscene and so existing data will not be cleared.</param>
		 * <param name = "_localResumeData">The saved data string</param>
		 */
		public void LoadData (string _dataString, SubScene subScene = null)
		{
			saveAfterCutscene = false;
			playCutsceneOnVarChange = false;

			if (subScene == null)
			{
				activeLists.Clear ();
			}

			if (!string.IsNullOrEmpty (_dataString))
			{
				string[] dataArray = _dataString.Split (SaveSystem.pipe[0]);
				foreach (string chunk in dataArray)
				{
					ActiveList activeList = new ActiveList ();
					activeList.LoadData (chunk, subScene);
				}
			}
		}


		public void AddToList (ActiveList activeList)
		{
			activeLists.Add (activeList);
		}


		public void DrawStatus ()
		{
			bool anyRunning = false;
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning ())
				{
					anyRunning = true;
					break;
				}
			}

			if (anyRunning)
			{
				GUILayout.Label ("ActionLists running:");

				for (int i = 0; i < activeLists.Count; i++)
				{
					KickStarter.actionListManager.activeLists[i].ShowGUI ();
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void OnEndActionList (ActionList actionList, ActionListAsset actionListAsset, bool isSkipping)
		{
			if (!IsGameplayBlocked ())
			{
				ResetSkipVars ();
			}
			PurgeLists ();
		}


		protected void OnEnterGameState (GameState gameState)
		{
			if (gameState == GameState.Normal && saveAfterCutscene)
			{
				saveAfterCutscene = false;
				SaveSystem.SaveAutoSave ();
			}

			if (playCutsceneOnVarChange && (gameState == GameState.Normal || gameState == GameState.DialogOptions))
			{
				playCutsceneOnVarChange = false;

				if (KickStarter.sceneSettings.actionListSource == ActionListSource.InScene && KickStarter.sceneSettings.cutsceneOnVarChange != null)
				{
					KickStarter.sceneSettings.cutsceneOnVarChange.Interact ();
				}
				else if (KickStarter.sceneSettings.actionListSource == ActionListSource.AssetFile && KickStarter.sceneSettings.actionListAssetOnVarChange != null)
				{
					KickStarter.sceneSettings.actionListAssetOnVarChange.Interact ();
				}
			}
		}


		protected void PurgeLists ()
		{
			bool checkAutoSave = false;
			for (int i=0; i<activeLists.Count; i++)
			{
				if (!activeLists[i].IsNecessary ())
				{
					if (!saveAfterCutscene && !checkAutoSave && activeLists[i].actionList != null && activeLists[i].actionList.autosaveAfter)
					{
						checkAutoSave = true;
					}

					activeLists.RemoveAt (i);
					i--;
				}
			}
			for (int i=0; i<KickStarter.actionListAssetManager.ActiveLists.Count; i++)
			{
				if (!KickStarter.actionListAssetManager.ActiveLists[i].IsNecessary ())
				{
					KickStarter.actionListAssetManager.ActiveLists.RemoveAt (i);
					i--;
				}
			}

			if (checkAutoSave)
			{
				if (!IsGameplayBlocked ())
				{
					SaveSystem.SaveAutoSave ();
				}
				else
				{
					saveAfterCutscene = true;
				}
			}
		}


		protected void ResetSkipVars (bool ignoreBlockCheck = false)
		{
			if (ignoreBlockCheck || !IsGameplayBlocked ())
			{
				ignoreNextConversationSkip = false;
				foreach (ActiveList activeList in activeLists)
				{
					activeList.inSkipQueue = false;
				}
				foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
				{
					activeList.inSkipQueue = false;
				}

				GlobalVariables.BackupAll ();
				KickStarter.localVariables.BackupAllValues ();
			}
		}


		protected bool HasSkipQueue ()
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning () && activeList.inSkipQueue)
				{
					return true;
				}
			}

			foreach (ActiveList activeList in KickStarter.actionListAssetManager.ActiveLists)
			{
				if (activeList.IsRunning () && activeList.inSkipQueue)
				{
					return true;
				}
			}
			
			return false;
		}

		#endregion


		#region StaticFunctions

		/**
		 * Ends all currently-running ActionLists and ActionListAssets.
		 */
		public static void KillAll ()
		{
			KickStarter.actionListManager.KillAllLists ();
		}

		#endregion


		#region GetSet

		/** Data about any ActionList that has been run and we need to store information about */
		public List<ActiveList> ActiveLists
		{
			get
			{
				if (activeLists == null) activeLists = new List<ActiveList> ();
				return activeLists;
			}
		}

		#endregion

	}

}