/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionList.cs"
 * 
 *	This script stores, and handles the sequentual triggering of, actions.
 *	It is derived by Cutscene, Hotspot, Trigger, and DialogOption.
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

	/**
	 * An ActionList stores and handles the sequential triggering of Action objects.
	 * Strung together, Actions can be used to create cutscenes, effects and gameplay logic.
	 * This base class is never used itself - only subclasses are intended to be placed on GameObjects.
	 */
	[System.Serializable]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list.html")]
#if AC_ActionListPrefabs
	public class ActionList : MonoBehaviour, iActionListAssetReferencer, ISerializationCallbackReceiver, IItemReferencer, IDocumentReferencer, IObjectiveReferencer, IMenuReferencer, IVariableReferencer
#else
	public class ActionList : MonoBehaviour, iActionListAssetReferencer, IItemReferencer, IDocumentReferencer, IObjectiveReferencer, IMenuReferencer, IVariableReferencer
#endif
	{

		/** The Actions */
		#if AC_ActionListPrefabs
		[HideInInspector] [SerializeReference] public List<AC.Action> actions = new List<AC.Action> ();
		#else
		[HideInInspector] public List<AC.Action> actions = new List<AC.Action> ();
		#endif

		/** If True, the Actions will be skipped when the user presses the 'EndCutscene' Input button */
		[HideInInspector] public bool isSkippable = true;
		/** The delay, in seconds, before the Actions are run when the ActionList is triggered */
		[HideInInspector] public float triggerTime = 0f;
		/** If True, the game will auto-save when the Actions have finished running */
		[HideInInspector] public bool autosaveAfter = false;
		/** The effect that running the Actions has on the rest of the game (PauseGameplay, RunInBackground) */
		[HideInInspector] public ActionListType actionListType = ActionListType.PauseGameplay;
		/** The Conversation to run when the Actions have finished running */
		[HideInInspector] public Conversation conversation = null;
		/** The ActionListAsset file that stores the Actions, if source = ActionListSource.AssetFile */
		[HideInInspector] public ActionListAsset assetFile;
		/** Where the Actions are stored when not being run (InScene, AssetFile) */
		[HideInInspector] public ActionListSource source;
		/** If True, the game will un-freeze itself while the Actions run if the game was previously paused due to an enabled Menu */
		[HideInInspector] public bool unfreezePauseMenus = true;
		/** If True, ActionParameters can be used to override values within the Action objects */
		[HideInInspector] public bool useParameters = false;
		/** A List of ActionParameter objects that can be used to override values within the Actions, if useParameters = True */
		[HideInInspector] public List<ActionParameter> parameters = new List<ActionParameter> ();
		/** The ID of the associated SpeechTag */
		[HideInInspector] public int tagID;
		/** If True, and source = ActionListSource.AssetFile, the asset file's parameter values will be shared amongst all linked ActionLists */
		[HideInInspector] public bool syncParamValues = true;

		protected bool isSkipping = false;
		protected LayerMask LayerHotspot;
		protected LayerMask LayerOff;

		protected List<int> resumeIndices = new List<int> ();
		private bool pauseWhenActionFinishes = false;
		private const string parameterSeparator = "{PARAM_SEP}";

		private WaitForSeconds delayWait = new WaitForSeconds (0.05f);
		private WaitForEndOfFrame delayFrame = null;//new WaitForEndOfFrame (); // set to null to keep updates in main Update loop
		private bool waitedAnyFrame;

		protected bool isChangingScene = false;
		private int skipIteractions = 0; // Used to combat StackOverflow exceptions


		#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER

		[SerializeField] private JsonAction[] backupData;

		[MenuItem ("CONTEXT/ActionList/Action data/Backup")]
		public static void BackupData (MenuCommand command)
		{
			ActionList actionList = (ActionList) command.context;
			actionList.BackupData ();
		}


		public void BackupData ()
		{
			var newBackupData = JsonAction.BackupActions (actions);
			if (newBackupData != null)
			{
				backupData = newBackupData;
				EditorUtility.SetDirty (this);
			}
		}


		[MenuItem ("CONTEXT/ActionList/Action data/Restore")]
		public static void RestoreData (MenuCommand command)
		{
			ActionList actionList = (ActionList) command.context;
			actionList.RestoreData ();
		}


		public bool HasDataToRestore ()
		{
			return backupData != null && backupData.Length > 0;
		}


		public void RestoreData ()
		{
			if (HasDataToRestore ())
			{
				actions = JsonAction.RestoreActions (backupData);
				EditorUtility.SetDirty (this);
			}
			else
			{
				ACDebug.LogWarning ("Could not restore data for ActionList "+ this + " - no backup data found!", this);
			}
		}


		[MenuItem ("CONTEXT/ActionList/Action data/Clear")]
		public static void ClearData (MenuCommand command)
		{
			ActionList actionList = (ActionList) command.context;
			actionList.ClearData ();
		}


		public void ClearData ()
		{
			if (HasDataToRestore ())
			{
				backupData = null;
				EditorUtility.SetDirty (this);
			}
		}

		#endif


		private void Awake ()
		{
			if (KickStarter.settingsManager != null)
			{
				LayerHotspot = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
				LayerOff = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
			}

			DownloadParameters ();

			/*if (useParameters)
			{
				// Reset all parameters
				foreach (ActionParameter _parameter in parameters)
				{
					_parameter.Reset ();
				}
			}*/
		}


		private void DownloadParameters ()
		{
			// If asset-based, download actions
			if (source == ActionListSource.AssetFile)
			{
				actions.Clear ();
				if (assetFile && assetFile.actions.Count > 0)
				{
					foreach (AC.Action action in assetFile.actions)
					{
						actions.Add (action);
						actions[actions.Count - 1].isAssetFile = false;
					}

					if (!syncParamValues && useParameters && assetFile.useParameters && parameters.Count == assetFile.DefaultParameters.Count)
					{
						// Using local parameters
						return;
					}

					if (!assetFile.useParameters)
					{
						useParameters = false;
					}
					else
					{
						if (syncParamValues)
						{
							parameters = assetFile.GetParameters ();
							useParameters = true;
						}
						else
						{
							parameters.Clear ();
							foreach (ActionParameter parameter in assetFile.DefaultParameters)
							{
								if (parameter != null)
								{
									ActionParameter newParameter = new ActionParameter (parameter, !useParameters);
									parameters.Add (newParameter);
								}
							}
							useParameters = true;
						}
					}
				}
			}
		}


		/** Clears the List of Actions and creates one instance of the default, as set within ActionsManager. */
		public void Initialise ()
		{
			actions.Clear ();
			if (actions == null || actions.Count < 1)
			{
				actions.Add (GetDefaultAction ());
			}
		}


		/** Runs the Actions normally, from the beginning. */
		public virtual void Interact ()
		{
			Interact (0, true);
		}


		/**
		 * <summary>Runs the Actions from a set point.</summary>
		 * <param name = "index">The index number of actions to start from</param>
		 */
		public void RunFromIndex (int index)
		{
			Interact (index, true);
		}


		/**
		 * <summary>Runs the Actions from a set point.</summary>
		 * <param name = "i">The index number of actions to start from</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be skipped when the user presses the 'EndCutscene' Input button</param>
		 */
		public void Interact (int i, bool addToSkipQueue)
		{
			if (!gameObject.activeSelf)
			{
				ACDebug.LogWarning ("Cannot run ActionList '" + name + "' because its GameObject is disabled!", this);
				return;
			}

			if (actions.Count > 0 && actions.Count > i)
			{
				if (triggerTime > 0f && i == 0)
				{
					StartCoroutine (PauseUntilStart (addToSkipQueue));
				}
				else
				{
					ResetList ();
					ResetSkips ();
					BeginActionList (i, addToSkipQueue);
				}
			}
			else
			{
				Kill ();
			}
		}


		/**
		 * Runs the Actions instantly, from the beginning.
		 */
		public void Skip ()
		{
			Skip (0);
		}


		/**
		 * <summary>Runs the Actions instantly, from a set point.</summary>
		 * <param name = "i">The index number of actions to start from</param>
		 */
		public void Skip (int i)
		{
			skipIteractions = 0;
			
			if (actionListType == ActionListType.RunInBackground)
			{
				Interact (i, false);
				return;
			}

			if (i < 0 || actions.Count <= i)
			{
				return;
			}

			if (actionListType == ActionListType.RunInBackground || !isSkippable)
			{
				// Can't skip, so just run normally
				Interact ();
				return;
			}

			// Already running
			if (!isSkipping)
			{
				ResetList ();
				if (KickStarter.actionListManager.CanResetSkipVars (this))
				{
					// We need to reset skip vars if the ActionList is not currently in the skip queue
					ResetSkips ();
				}

				isSkipping = true;

				BeginActionList (i, false);
			}
		}


		private IEnumerator PauseUntilStart (bool addToSkipQueue)
		{
			if (triggerTime > 0f)
			{
				yield return new WaitForSeconds (triggerTime);
			}

			ResetList ();
			ResetSkips ();
			BeginActionList (0, addToSkipQueue);
		}


		private void ResetSkips ()
		{
			// "lastResult" is used to backup Check results when skipping
			foreach (Action action in actions)
			{
				if (action != null)
				{
					action.ResetLastResult ();
				}
			}
		}


		protected virtual void BeginActionList (int i, bool addToSkipQueue)
		{
			pauseWhenActionFinishes = false;

			if (KickStarter.actionListManager)
			{
				KickStarter.actionListManager.AddToList (this, addToSkipQueue, i);
				KickStarter.eventManager.Call_OnBeginActionList (this, null, i, isSkipping);

				if (KickStarter.actionListManager.IsListRegistered (this))
				{
					ProcessAction (i);
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot run " + this.name + " because no ActionListManager was found.", gameObject);
			}
		}


		private IEnumerator DelayProcessAction (int i)
		{
			yield return delayWait;
			ProcessAction (i);
		}


		protected void ProcessAction (int i)
		{
			if (i >= 0 && i < actions.Count && actions[i] != null && actions[i] is Action)
			{
				// Action exists
				if (!actions[i].isEnabled)
				{
					// Disabled, try next
					ProcessAction (i + 1);
				}
				else
				{
					// Run it
					#if UNITY_EDITOR
					actions[i].BreakPoint (i, this);
					#endif
					StartCoroutine (RunAction (actions[i]));

				}
			}
			else
			{
				CheckEndCutscene ();
			}
		}


		private IEnumerator RunAction (Action action)
		{
			if (isChangingScene)
			{
				ACDebug.Log ("Cannot run Action while changing scene, will resume once loading is complete.", this, action);
				while (isChangingScene)
				{
					yield return null;
				}
			}

			action.AssignParentList (this);
			if (useParameters)
			{
				action.AssignValues (parameters);
			}
			else
			{
				action.AssignValues (null);
			}

			action.Upgrade ();

			if (isSkipping)
			{
				skipIteractions++;
				action.Skip ();

				PrintActionComment (action);
			}
			else
			{
				if (action is ActionRunActionList)
				{
					ActionRunActionList actionRunActionList = (ActionRunActionList)action;
					actionRunActionList.isSkippable = IsSkippable ();
				}

				action.isRunning = false;
				float waitTime = action.Run ();

				if (action.RunAllOutputs)
				{
					waitedAnyFrame = true;
				}

				PrintActionComment (action);

				if (!Mathf.Approximately (waitTime, 0f))
				{
					while (action.isRunning)
					{
						waitedAnyFrame = true;

						if (isChangingScene)
						{
							ACDebug.Log ("Cannot continue ActionList " + this + " while changing scene - will resume once loading is complete.", this, action);
							while (isChangingScene)
							{
								yield return null;
							}
						}

						bool runInRealtime = (this is RuntimeActionList && actionListType == ActionListType.PauseGameplay && !unfreezePauseMenus && KickStarter.playerMenus.ArePauseMenusOn (null));
						if (waitTime < 0)
						{
							if (!runInRealtime && Time.timeScale <= 0f)
							{
								while (Time.timeScale <= 0f)
								{
									yield return delayFrame;
								}
							}
							else
							{
								yield return delayFrame;
							}
						}
						else if (runInRealtime)
						{
							float endTime = Time.realtimeSinceStartup + waitTime;
							while (Time.realtimeSinceStartup < endTime)
							{
								yield return null;
							}
						}
						else
						{
							yield return new WaitForSeconds (waitTime);
						}

						if (!action.isRunning)
						{
							// In rare cases (once an actionlist is reset) isRunning may be false but this while loop will still run
							ResetList ();
							break;
						}

						waitTime = action.Run ();
					}
				}
			}

			if (KickStarter.actionListManager.IsListRunning (this))
			{
				if (action.RunAllOutputs)
				{
					EndActionParallel (action);
				}
				else
				{
					EndAction (action);
				}
			}
		}


		private void EndAction (Action action)
		{
			action.isRunning = false;

			int endIndex = action.GetNextOutputIndex ();
			ActionEnd actionEnd = (endIndex < 0 || endIndex > action.endings.Count) ? Action.GenerateStopActionEnd () : action.endings[endIndex];
			
			if (action.NumSockets <= 0)
			{
				actionEnd = Action.GenerateStopActionEnd ();
			}
			else if (isSkipping && action.NumSockets > 1 && action.LastRunOutput >= 0 && action.LastRunOutput < action.endings.Count)
			{
				// When skipping an Action with multiple outputs that has already run, revert to previous result
				actionEnd = action.endings[action.LastRunOutput];
			}
			else
			{
				int index = action.endings.IndexOf (actionEnd);
				action.SetLastResult (index);
				ReturnLastResultToSource (index, actions.IndexOf (action));
			}

			if (actionEnd.resultAction == ResultAction.Skip && actionEnd.skipAction == actions.IndexOf (action))
			{
				// Looping on itself will cause a StackOverflowException, so delay slightly
				ProcessActionEnd (actionEnd, actions.IndexOf (action), true);
				return;
			}

			ProcessActionEnd (actionEnd, actions.IndexOf (action));
		}


		private void ProcessActionEnd (ActionEnd actionEnd, int i, bool doStackOverflowDelay = false)
		{
			if (isSkipping && skipIteractions > (actions.Count * 3))
			{
				// StackOverFlow
				ACDebug.LogWarning (skipIteractions +" Looping ActionList '" + gameObject.name + "' detected while skipping - ending prematurely to avoid a StackOverflow exception.", gameObject);
				CheckEndCutscene ();
				return;
			}

			if (pauseWhenActionFinishes)
			{
				resumeIndices.Add (i);
				if (!AreActionsRunning ())
				{
					FinishPause ();
				}
				return;
			}
			
			switch (actionEnd.resultAction)
			{
				case ResultAction.Stop:
					CheckEndCutscene ();
					break;

				case ResultAction.Continue:
					ProcessAction (i + 1);
					break;

				case ResultAction.Skip:
					if (doStackOverflowDelay)
					{
						StartCoroutine (DelayProcessAction (actionEnd.skipAction));
					}
					else
					{
						ProcessAction (actionEnd.skipAction);
					}
					break;

				case ResultAction.RunCutscene:
					if (actionEnd.linkedAsset)
					{
						if (isSkipping)
						{
							AdvGame.SkipActionListAsset (actionEnd.linkedAsset);
						}
						else
						{
							AdvGame.RunActionListAsset (actionEnd.linkedAsset, 0, !IsSkippable ());
						}
						CheckEndCutscene ();
					}
					else if (actionEnd.linkedCutscene)
					{
						if (actionEnd.linkedCutscene != this)
						{
							if (isSkipping)
							{
								actionEnd.linkedCutscene.Skip ();
							}
							else
							{
								actionEnd.linkedCutscene.Interact (0, !IsSkippable ());
							}
							CheckEndCutscene ();
						}
						else
						{
							if (triggerTime > 0f)
							{
								Kill ();
								StartCoroutine (PauseUntilStart (!IsSkippable ()));
							}
							else
							{
								ProcessAction (0);
							}
						}
					}
					else
					{
						CheckEndCutscene ();
					}
					break;

				default:
					break;
			}

			pauseWhenActionFinishes = false;
		}


		private void EndActionParallel (Action action)
		{
			action.isRunning = false;

			foreach (ActionEnd ending in action.endings)
			{
				if (ending.resultAction == ResultAction.Skip)
				{
					int skip = ending.skipAction;
					if (ending.skipActionActual != null && actions.Contains (ending.skipActionActual))
					{
						skip = actions.IndexOf (ending.skipActionActual);
					}
					else if (skip == -1)
					{
						skip = 0;
					}

					ending.skipAction = skip;
				}
			}

			foreach (ActionEnd actionEnd in action.endings)
			{
				ProcessActionEnd (actionEnd, actions.IndexOf (action));
			}
		}


		private IEnumerator EndCutscene ()
		{
			if (!isSkipping && waitedAnyFrame)
			{
				yield return delayFrame;
			}

			if (AreActionsRunning ())
			{
				yield break;
			}

			Kill ();
		}


		protected void CheckEndCutscene ()
		{
			if (!AreActionsRunning ())
			{
				StartCoroutine (EndCutscene ());
			}
		}


		/**
		 * <summary>Checks if any Actions are currently being run.</summary>
		 * <returns>True if any Actions are currently being run</returns>
		 */
		public bool AreActionsRunning ()
		{
			for (int i = 0; i < actions.Count; i++)
			{
				if (actions[i] != null && actions[i].isRunning)
				{
					return true;
				}
			}
			return false;
		}


		private void TurnOn ()
		{
			gameObject.layer = LayerHotspot;
		}


		private void TurnOff ()
		{
			gameObject.layer = LayerOff;
		}


		/** Stops the Actions from running. */
		public void ResetList ()
		{
			isSkipping = false;
			waitedAnyFrame = false;
			StopAllCoroutines ();

			foreach (Action action in actions)
			{
				if (action != null)
				{
					action.Reset (this);
				}
			}
		}


		/** Stops the Actions from running and sets the gameState in StateHandler to the correct value. */
		public virtual void Kill ()
		{
			StopAllCoroutines ();

			KickStarter.actionListManager.EndList (this);
			KickStarter.eventManager.Call_OnEndActionList (this, null, isSkipping);
		}


		/**
		 * <summary>Gets the default Action set within ActionsManager.</summary>
		 * <returns>The default Action set within ActionsManager</returns>
		 */
		public static AC.Action GetDefaultAction ()
		{
			if (AdvGame.GetReferences ().actionsManager)
			{
				string defaultAction = ActionsManager.GetDefaultAction ();
				Action newAction = Action.CreateNew (defaultAction);
				return newAction;
			}
			else
			{
				ACDebug.LogError ("Cannot create Action - no Actions Manager found.");
				return null;
			}
		}


		protected virtual void ReturnLastResultToSource (int index, int i)
		{ }


		/**
		 * <summary>Checks if the ActionListAsset is skippable. This is safer than just reading 'isSkippable', because it also accounts for actionListType - since ActionLists that run in the background cannot be skipped</summary>
		 * <returns>True if the ActionListAsset is skippable</returns>
		 */
		public bool IsSkippable ()
		{
			if (isSkippable && actionListType == ActionListType.PauseGameplay)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the List of Actions that this ActionList runs, regardless of source.</summary>
		 * <returns>The List of Actions that this ActionList runs, regardless of source.</returns>
		 */
		public List<Action> GetActions ()
		{
			if (source == ActionListSource.AssetFile)
			{
				if (assetFile)
				{
					return assetFile.actions;
				}
			}
			else
			{
				return actions;
			}
			return null;
		}


		/**
		 * <summary>Gets a parameter of a given name.</summary>
		 * <param name = "label">The name of the parameter to get</param>
		 * <returns>The parameter with the given name</returns>
		 */
		public ActionParameter GetParameter (string label)
		{
			if (useParameters)
			{
				if (source == ActionListSource.InScene)
				{
					return GetParameter (label, parameters);
				}
				else if (source == ActionListSource.AssetFile && assetFile && assetFile.useParameters)
				{
					if (syncParamValues)
					{
						return GetParameter (label, assetFile.GetParameters ());
					}
					else
					{
						return GetParameter (label, parameters);
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Gets a parameter of a given ID number.</summary>
		 * <param name = "_ID">The ID of the parameter to get</param>
		 * <returns>The parameter with the given ID number</returns>
		 */
		public ActionParameter GetParameter (int _ID)
		{
			if (useParameters)
			{
				if (source == ActionListSource.InScene)
				{
					return GetParameter (_ID, parameters);
				}
				else if (source == ActionListSource.AssetFile && assetFile && assetFile.useParameters)
				{
					if (syncParamValues)
					{
						return GetParameter (_ID, assetFile.GetParameters ());
					}
					else
					{
						return GetParameter (_ID, parameters);
					}
				}
			}
			return null;
		}


		private ActionParameter GetParameter (int _ID, List<ActionParameter> _parameters)
		{
			if (_parameters != null)
			{
				foreach (ActionParameter parameter in _parameters)
				{
					if (parameter.ID == _ID)
					{
						return parameter;
					}
				}
			}
			return null;
		}


		private ActionParameter GetParameter (string _label, List<ActionParameter> _parameters)
		{
			if (_parameters != null)
			{
				foreach (ActionParameter parameter in _parameters)
				{
					if (parameter.label == _label)
					{
						return parameter;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Pauses the ActionList once it has finished running it's current Action.</summary>
		 */
		public void Pause ()
		{
			resumeIndices.Clear ();
			pauseWhenActionFinishes = true;

			KickStarter.eventManager.Call_OnPauseActionList (this);
		}


		protected virtual void FinishPause ()
		{
			KickStarter.actionListManager.AssignResumeIndices (this, resumeIndices.ToArray ());
			CheckEndCutscene ();
		}


		protected virtual void PrintActionComment (Action action)
		{
			switch (KickStarter.settingsManager.actionCommentLogging)
			{
				case ActionCommentLogging.Always:
					action.PrintComment (this);
					break;

				case ActionCommentLogging.OnlyIfVisible:
					if (action.showComment)
					{
						action.PrintComment (this);
					}
					break;

				default:
					break;
			}
		}



		/**
		 * <summary>Resumes the ActionList.</summary>
		 * <param name = "_startIndex">The Action index that the ActionList was originally started from.</param>
		 * <param name = "_resumeIndices">An array of Action indices to resume from</param>
		 * <param name = "_parameterData">The ActionParameter values when paused, as a serializable string</param>
		 * <param name = "rerunPreviousAction">If True, then any Actions that were running when the ActionList was paused will be re-run</param>
		 */
		public void Resume (int _startIndex, int[] _resumeIndices, string _parameterData, bool rerunPreviousAction = false)
		{
			resumeIndices.Clear ();
			foreach (int resumeIndex in _resumeIndices)
			{
				resumeIndices.Add (resumeIndex);
			}

			if (resumeIndices.Count > 0)
			{
				ResetList ();
				ResetSkips ();

				SetParameterData (_parameterData);

				pauseWhenActionFinishes = false;

				if (KickStarter.actionListManager == null)
				{
					ACDebug.LogWarning ("Cannot run " + this.name + " because no ActionListManager was found.", gameObject);
					return;
				}

				AddResumeToManager (_startIndex);

				KickStarter.eventManager.Call_OnResumeActionList (this);

				foreach (int resumeIndex in resumeIndices)
				{
					if (resumeIndex >= 0 && resumeIndex < actions.Count)
					{
						if (rerunPreviousAction)
						{
							ProcessAction (resumeIndex);
						}
						else
						{
							Action action = actions[resumeIndex];

							if (useParameters)
							{
								action.AssignValues (parameters);
							}
							else
							{
								action.AssignValues (null);
							}

							if (action.RunAllOutputs)
							{
								EndActionParallel (action);
							}
							else
							{
								EndAction (action);
							}
						}
					}
				}
			}
			else
			{
				Kill ();
				Interact ();
			}
		}


		protected virtual void AddResumeToManager (int startIndex)
		{
			KickStarter.actionListManager.AddToList (this, true, startIndex);
		}


		/**
		 * <summary>Gets the current ActionParameter values as a serializable string.</summary>
		 * <returns>The current ActionParameter values as a serializable string</returns>
		 */
		public string GetParameterData ()
		{
			if (useParameters)
			{
				string dataString = string.Empty;
				for (int i = 0; i < parameters.Count; i++)
				{
					dataString += parameters[i].GetSaveData ();

					if (i < (parameters.Count - 1))
					{
						dataString += parameterSeparator;
					}
				}
				return dataString;
			}
			return string.Empty;
		}


		/**
		 * <summary>Assigns parameter values based on a string generated by the GetParameterData function</summary>
		 * <param name = "dataString">The data string to load parameter data from</param>
		 */
		public void SetParameterData (string dataString)
		{
			if (useParameters && !string.IsNullOrEmpty (dataString))
			{
				string[] stringSeparators = new string[] { parameterSeparator };
				string[] dataArray = dataString.Split (stringSeparators, System.StringSplitOptions.None);

				for (int i = 0; i < parameters.Count; i++)
				{
					if (i < dataArray.Length)
					{
						parameters[i].LoadData (dataArray[i]);
					}
				}
			}
		}


		/** The number of parameters associated with the ActionList */
		public int NumParameters
		{
			get
			{
				if (useParameters && parameters != null) return parameters.Count;
				return 0;
			}
		}


		#if UNITY_EDITOR

		private void OnValidate ()
		{
			CopyScriptable ();
		}


		public static string logSuffix;

		public int GetNumItemReferences (int itemID)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = GetParameterReferences (parameters, itemID, ParameterType.InventoryItem);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IItemReferencerAction)
				{
					IItemReferencerAction itemReferencerAction = action as IItemReferencerAction;
					int thisNumReferences = itemReferencerAction.GetNumItemReferences (itemID, parameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": "+ action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = GetParameterReferences (parameters, oldItemID, ParameterType.InventoryItem, null, 0, true, newItemID);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IItemReferencerAction)
				{
					IItemReferencerAction itemReferencerAction = action as IItemReferencerAction;
					int thisNumReferences = itemReferencerAction.UpdateItemReferences (oldItemID, newItemID, parameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumMenuReferences (string menuName, string elementName = "")
		{
			int totalNumReferences = 0;

			foreach (Action action in actions)
			{
				if (action != null && action is IMenuReferencer)
				{
					IMenuReferencer menuReferencer = action as IMenuReferencer;
					int thisNumReferences = menuReferencer.GetNumMenuReferences (menuName, elementName);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumVariableReferences (VariableLocation _location, int variableID, Variables _variables = null, int _variablesConstantID = 0)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = 0;

				switch (_location)
				{
					case VariableLocation.Global:
						thisNumReferences = GetParameterReferences (parameters, variableID, ParameterType.GlobalVariable);
						break;

					case VariableLocation.Local:
						thisNumReferences = GetParameterReferences (parameters, variableID, ParameterType.LocalVariable);
						break;

					case VariableLocation.Component:
						thisNumReferences = GetParameterReferences (parameters, variableID, ParameterType.ComponentVariable, _variables, _variablesConstantID);
						break;

					default:
						break;
				}

				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IVariableReferencerAction)
				{
					IVariableReferencerAction variableReferencer = action as IVariableReferencerAction;
					int thisNumReferences = variableReferencer.GetNumVariableReferences (_location, variableID, parameters, _variables, _variablesConstantID);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateVariableReferences (VariableLocation _location, int oldVariableID, int newVariableID, Variables _variables = null, int _variablesConstantID = 0)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = 0;

				switch (_location)
				{
					case VariableLocation.Global:
						thisNumReferences = GetParameterReferences (parameters, oldVariableID, ParameterType.GlobalVariable, null, 0, true, newVariableID);
						break;

					case VariableLocation.Local:
						thisNumReferences = GetParameterReferences (parameters, oldVariableID, ParameterType.LocalVariable, null, 0, true, newVariableID);
						break;

					case VariableLocation.Component:
						thisNumReferences = GetParameterReferences (parameters, oldVariableID, ParameterType.ComponentVariable, _variables, _variablesConstantID, true, newVariableID);
						break;

					default:
						break;
				}

				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IVariableReferencerAction)
				{
					IVariableReferencerAction variableReferencer = action as IVariableReferencerAction;
					int thisNumReferences = variableReferencer.UpdateVariableReferences (_location, oldVariableID, newVariableID, parameters, _variables, _variablesConstantID);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumDocumentReferences (int documentID)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = GetParameterReferences (parameters, documentID, ParameterType.Document);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IDocumentReferencerAction)
				{
					IDocumentReferencerAction documentReferencerAction = action as IDocumentReferencerAction;
					int thisNumReferences = documentReferencerAction.GetNumDocumentReferences (documentID, parameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateDocumentReferences (int oldDocumentID, int newDocumentID)
		{
			int totalNumReferences = 0;

			if ((source == ActionListSource.InScene && NumParameters > 0) ||
				(source == ActionListSource.AssetFile && assetFile && assetFile.NumParameters > 0 && !syncParamValues && useParameters))
			{
				int thisNumReferences = GetParameterReferences (parameters, oldDocumentID, ParameterType.Document, null, 0, true, newDocumentID);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IDocumentReferencerAction)
				{
					IDocumentReferencerAction documentReferencerAction = action as IDocumentReferencerAction;
					int thisNumReferences = documentReferencerAction.UpdateDocumentReferences (oldDocumentID, newDocumentID, parameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumObjectiveReferences (int objectiveID)
		{
			int totalNumReferences = 0;

			foreach (Action action in actions)
			{
				if (action != null && action is IObjectiveReferencerAction)
				{
					IObjectiveReferencerAction objectiveReferencerAction = action as IObjectiveReferencerAction;
					int thisNumReferences = objectiveReferencerAction.GetNumObjectiveReferences (objectiveID);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateObjectiveReferences (int oldObjectiveID, int newObjectiveID)
		{
			int totalNumReferences = 0;

			foreach (Action action in actions)
			{
				if (action != null && action is IObjectiveReferencerAction)
				{
					IObjectiveReferencerAction objectiveReferencerAction = action as IObjectiveReferencerAction;
					int thisNumReferences = objectiveReferencerAction.UpdateObjectiveReferences (oldObjectiveID, newObjectiveID);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		private int GetParameterReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType, Variables _variables = null, int _variablesConstantID = 0, bool updateID = false, int _newID = 0)
		{
			int thisCount = 0;

			foreach (ActionParameter parameter in parameters)
			{
				if (parameter != null && parameter.parameterType == _paramType && _ID == parameter.intValue)
				{
					if (_paramType == ParameterType.ComponentVariable)
					{
						if (_variables && parameter.variables != _variables)
						{
							continue;
						}
						if (_variablesConstantID == 0 || parameter.constantID == 0 || parameter.constantID != _variablesConstantID)
						{
							continue;
						}
						continue;
					}

					if (updateID)
					{ 
						parameter.intValue = _newID;
					}
					thisCount++;
				}
			}

			return thisCount;
		}


		private void CopyScriptable ()
		{
			#if !AC_ActionListPrefabs
			if (Application.isPlaying)
			{
				return;
			}

			if (actions == null || actions.Count == 0 || source == ActionListSource.AssetFile)
			{
				return;
			}

			bool modified = false;
			for (int i = 0; i < actions.Count; i++)
			{
				if (actions[i] != null && actions[i].parentActionListInEditor != this)
				{
					actions[i] = Instantiate (actions[i]) as Action;
					actions[i].name = actions[i].name.Replace ("(Clone)", string.Empty);
					actions[i].parentActionListInEditor = this;
					modified = true;
				}
			}
			if (modified) UnityVersionHandler.CustomSetDirty (this);
			#endif
		}


		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			bool foundReference = false;

			if (source == ActionListSource.InScene)
			{
				foreach (Action action in actions)
				{
					if (action != null && action is iActionListAssetReferencer)
					{
						iActionListAssetReferencer objectiveReferencerAction = action as iActionListAssetReferencer;
						{
							if (objectiveReferencerAction.ReferencesAsset (actionListAsset))
							{
								string actionLabel = (KickStarter.actionsManager != null) ? (" (" + KickStarter.actionsManager.GetActionTypeLabel (action) + ")") : "";
								Debug.Log ("'" + actionListAsset.name + "' is referenced by Action #" + actions.IndexOf (action) + actionLabel + " in ActionList '" + gameObject.name + "' in scene '" + gameObject.scene.name + "'", this);
								foundReference = true;
							}
						}
					}
				}
			}

			if (source == ActionListSource.AssetFile && assetFile == actionListAsset)
			{
				foundReference = true;
			}

			return foundReference;
		}


		public bool ActionModified (int index)
		{
			#if AC_ActionListPrefabs
			return modifiedPrefabIndices.Contains (index);
			#else
			return false;
			#endif
		}

		
		#endif


		#if AC_ActionListPrefabs

		private List<int> modifiedPrefabIndices = new List<int> ();
		
		public void OnBeforeSerialize ()
		{
			UnityEditor.PropertyModification[] propertyModifications = UnityEditor.PrefabUtility.GetPropertyModifications (this);
			if (propertyModifications == null) return;

			modifiedPrefabIndices.Clear ();
			for (int i = 0; i < propertyModifications.Length; i++)
			{
				if (propertyModifications[i].propertyPath.StartsWith ("actions.Array.data[") &&
					!propertyModifications[i].propertyPath.Contains ("].nodeRect") &&
					!propertyModifications[i].propertyPath.Contains ("].isDisplayed") &&
					!propertyModifications[i].propertyPath.Contains ("].overrideColor") &&
					!propertyModifications[i].propertyPath.Contains ("].comment") &&
					!propertyModifications[i].propertyPath.Contains ("].showComment") &&
					!propertyModifications[i].propertyPath.Contains ("].showOutputSockets"))
				{
					string pathStart = propertyModifications[i].propertyPath.Substring ("actions.Array.data[".Length);
					int bracketIndex = pathStart.IndexOf ("]"[0]);
					if (bracketIndex > 0)
					{
						string actionIndexString = pathStart.Substring (0, bracketIndex);
						int actionIndex;
						if (int.TryParse (actionIndexString, out actionIndex))
						{
							modifiedPrefabIndices.Add (actionIndex);
						}
					}
				}
			}
		}


		public void OnAfterDeserialize ()
		{}

		#endif


	}
	
}