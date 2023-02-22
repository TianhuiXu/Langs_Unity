/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActiveList.cs"
 * 
 *	A container for data about ActionLists and ActionListAssets that have been run.  It stores information about what to skip, pause-points and current parameter data.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if AddressableIsPresent
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{
	
	/** A container for data about ActionLists and ActionListAssets that have been run.  It stores information about what to skip, pause-points and current parameter data. */
	public class ActiveList
	{

		#region Variables

		/** The ActionList this references */
		public ActionList actionList;
		/** The ActionListAsset this references */
		public ActionListAsset actionListAsset;
		/** The index number of the Action to skip from */
		public int startIndex;
		/** Whether or not the ActionList this class references should be skipped when 'EndCutscene' is triggered. */
		public bool inSkipQueue;

		private bool isRunning;
		private int[] resumeIndices;
		private Conversation conversationOnEnd;
		private string parameterData;
		private bool gamePausedWhenStarted;

		#endregion


		#region Constructors

		/** The default Constructor. */
		public ActiveList ()
		{
			actionList = null;
			actionListAsset = null;
			conversationOnEnd = null;
			inSkipQueue = false;
			isRunning = false;
			resumeIndices = new int[0];
			parameterData = string.Empty;
			gamePausedWhenStarted = (KickStarter.stateHandler != null) ? KickStarter.stateHandler.IsPaused () : false;
		}


		/**
		 * <summary>A Constructor</summary>
		 * <param name = "_actionList">The ActionList that this class will store data for</param>
		 * <param name = "_inSkipQueue">Whether or not the ActionList will be skipped when 'EndCutscene' is triggered</param>
		 * <param name = "_startIndex">The index of Actions within the ActionList that it starts from when run</param>
		 */
		public ActiveList (ActionList _actionList, bool _inSkipQueue, int _startIndex)
		{
			actionList = _actionList;

			if (actionList.conversation)
			{
				conversationOnEnd = actionList.conversation;
			}

			RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
			if (runtimeActionList != null)
			{
				actionListAsset = runtimeActionList.assetSource;
			}
			else
			{
				actionListAsset = null;
			}

			inSkipQueue = _inSkipQueue;
			startIndex = _startIndex;
			isRunning = true;
			resumeIndices = new int[0];
			parameterData = string.Empty;
			gamePausedWhenStarted = (KickStarter.stateHandler != null) ? KickStarter.stateHandler.IsPaused () : false;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks whether or not the associated ActionList is running.</summary>
		 * <returns>True if the associated ActionList is running</returns>
		 */
		public bool IsRunning ()
		{
			if (actionList != null)
			{
				return isRunning;
			}
			return false;
		}


		/**
		 * <summary>Checks whether the class contains any useful information. If not, the ActionListManager will delete it.</summary>
		 * <returns>True if the class contains any useful information</returns>
		 */
		public bool IsNecessary ()
		{
			if (IsRunning () || inSkipQueue || resumeIndices.Length > 0)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Clears any data that is causing the class to be deemed necessary.</summary>
		 */
		public void ClearNecessity ()
		{
			resumeIndices = new int[0];
		}


		/**
		 * <summary>Resets the associated ActionList.</summary>
		 * <param name = "removeFromSkipQueue">If True, then the associated ActionList will not be run when 'EndCutscene' is triggered</param>
		 */
		public void Reset (bool removeFromSkipQueue)
		{
			isRunning = false;
			
			if (actionList != null)
			{
				actionList.ResetList ();

				RuntimeActionList runtimeActionList = actionList as RuntimeActionList;
				if (runtimeActionList != null)
				{
					runtimeActionList.DestroySelf ();
				}
			}

			if (removeFromSkipQueue)
			{
				inSkipQueue = false;
			}
		}


		public bool CanResetSkipVars ()
		{
			if (isRunning || inSkipQueue)
			{
				return false;
			}
			return true;
		}


		/**
		 * Shows some information about the associated ActionList, if it is running.
		 */
		public void ShowGUI ()
		{
			if (actionList != null && IsRunning ())
			{
				if (GUILayout.Button (actionList.gameObject.name))
				{
					#if UNITY_EDITOR
					UnityEditor.EditorGUIUtility.PingObject (actionList.gameObject);
					#endif
				}
			}
		}


		/**
		 * <summary>Checks if the class is linked to a specific ActionList.</summary>
		 * <param name = "_actionList">The ActionList to check against</param>
		 * <returns>True if the class is linked to the ActionList</returns>
		 */
		public bool IsFor (ActionList _actionList)
		{
			if (_actionList != null && actionList == _actionList)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the class is linked to a specific ActionListAsset.</summary>
		 * <param name = "_actionListAsset">The ActionListAsset to check against</param>
		 * <returns>True if the class is linked to the ActionListAsset</returns>
		 */
		public bool IsFor (ActionListAsset _actionListAsset)
		{
			if (_actionListAsset != null)
			{
				if (actionListAsset == _actionListAsset)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Skips the associated ActionList.</summary>
		 */
		public void Skip ()
		{
			if (inSkipQueue)
			{
				if (actionListAsset != null)
				{
					bool isRunningBackup = isRunning;
					bool inSkipQueueBackup = inSkipQueue;

					// Destroy old list, but don't go through ActionListManager's Reset code, to bypass changing GameState etc
					KickStarter.actionListAssetManager.DestroyAssetList (actionListAsset);

					// Revert to backup so CanResetSkipVars returns correct value after destroying list
					isRunning = isRunningBackup;
					inSkipQueue = inSkipQueueBackup;

					actionList = AdvGame.SkipActionListAsset (actionListAsset, startIndex, conversationOnEnd);
				}
				else if (actionList != null)
				{
					actionList.Skip (startIndex);
				}
			}
		}


		/** Updates the internal record of the ActionList's current parameter data **/
		public void UpdateParameterData ()
		{
			parameterData = actionList.GetParameterData ();
		}


		/**
		 * <summary>Checks if the associated ActionList is capable of unfreezing pause Menus.</summary>
		 * <returns>True if the associated ActionList is capable of unfreezing pause Menus.</returns>
		 */
		public bool CanUnfreezePauseMenus ()
		{
			if (actionListAsset != null && actionListAsset.unfreezePauseMenus && actionListAsset.actionListType == ActionListType.PauseGameplay)
			{
				return gamePausedWhenStarted;
			}
			return false;
		}


		/**
		 * <summary>Gets the Conversation to run once the associated ActionList has finished running.</summary>
		 * <returns>The Conversation to run once the assocated ActionList has finished running</returns>
		 */
		public Conversation GetConversationOnEnd ()
		{
			return conversationOnEnd;
		}


		/**
		 * <summary>Runs the Conversation set to do so when the associated ActionList has finished.</summary>
		 */
		public void RunConversation ()
		{
			conversationOnEnd.Interact ();
			conversationOnEnd = null;
		}


		/**
		 * <summary>Resumes the associated ActionList, if it had previously been paused.</summary>
		 * <param name = "runtimeActionList">The RuntimeActionList to re-associate the class with</param>
		 * <param name = "rerunPausedActions">If True, then any Actions that were midway-through running when the ActionList was paused will be restarted. Otherwise, the Actions that follow them will be reun instead.</param>
		 */
		public void Resume (RuntimeActionList runtimeActionList = null, bool rerunPausedActions = false)
		{
			if (runtimeActionList != null)
			{ 
				isRunning = true;
				actionList = runtimeActionList;
				runtimeActionList.Resume (startIndex, resumeIndices, parameterData, rerunPausedActions);
			}
			else
			{
				actionList.Resume (startIndex, resumeIndices, parameterData, rerunPausedActions);
			}

			gamePausedWhenStarted = (KickStarter.stateHandler != null) ? KickStarter.stateHandler.IsPaused () : false;
		}


		/**
		 * <summary>Records the Action indices that the associated ActionList was running before being paused.</summary>
		 * <param name = "_resumeIndices">An array of Action indices to run when the ActionList is resumed</param>
		 */
		public void SetResumeIndices (int[] _resumeIndices)
		{
			List<int> resumeIndexList = new List<int>();
			foreach (int resumeIndex in _resumeIndices)
			{
				resumeIndexList.Add (resumeIndex);
			}
			resumeIndices = resumeIndexList.ToArray ();
		}


		/**
		 * <summary>Converts the class's data into a string that can be saved.</summary>
		 * <param name = "subScene">If set, only data for a given subscene will be saved. If null, only data for the active scene will be saved</param>
		 * <returns>The class's data, converted to a string</returns>
		 */
		public string GetSaveData (SubScene subScene)
		{
			string ID = string.Empty;
			string convID = string.Empty;

			if (IsRunning ())
			{
				// Unless ActionLists can be saved mid-stream, don't save info about those currently-running
				return string.Empty;
			}

			string parameterData = string.Empty;
			if (actionListAsset)
			{
				ID = AdvGame.PrepareStringForSaving (actionListAsset.name);
			}
			else if (actionList)
			{
				ConstantID actionListID = actionList.GetComponent <ConstantID>();

				if (actionListID)
				{
					ID = actionListID.constantID.ToString ();

					if (subScene == null && UnityVersionHandler.ObjectIsInActiveScene (actionList.gameObject))
					{
						// OK
					}
					else if (KickStarter.settingsManager.referenceScenesInSave == ChooseSceneBy.Number && subScene && UnityVersionHandler.GetSceneIndexFromGameObject (actionList.gameObject) == subScene.SceneIndex)
					{
						// OK
					}
					else if (KickStarter.settingsManager.referenceScenesInSave == ChooseSceneBy.Name && subScene && UnityVersionHandler.GetSceneNameFromGameObject (actionList.gameObject) == subScene.SceneName)
					{
						// OK
					}
					else
					{
						return string.Empty;
					}
				}
				else
				{
					ACDebug.LogWarning ("Data for the ActionList '" + actionList.gameObject.name + "' was not saved because it has no Constant ID.", actionList.gameObject);
					return string.Empty;
				}
			}

			if (actionList != null)
			{
				parameterData = actionList.GetParameterData ();
			}

			if (conversationOnEnd && conversationOnEnd.GetComponent <ConstantID>())
			{
				convID = conversationOnEnd.GetComponent <ConstantID>().ToString ();
			}

			return (ID + SaveSystem.colon +
					ConvertIndicesToString () + SaveSystem.colon +
					startIndex + SaveSystem.colon +
					((inSkipQueue) ? 1 : 0) + SaveSystem.colon +
					((isRunning) ? 1 : 0) + SaveSystem.colon +
					convID + SaveSystem.colon +
					parameterData);
		}


		/**
		 * <summary>Restores the class's data from a saved string.</summary>
		 * <param name = "data">The saved string to restore from</param>
		 * <param name = "subScene">If set, only data for a given subscene will be loaded. If null, only data for the active scene will be loaded</param>
		 */
		public void LoadData (string dataString, SubScene subScene = null)
		{
			if (string.IsNullOrEmpty (dataString)) return;

			string[] dataArray = dataString.Split (SaveSystem.colon[0]);

			// ID
			string listName = AdvGame.PrepareStringForLoading (dataArray[0]);
			resumeIndices = new int[0];

			// Resume
			string[] resumeData = dataArray[1].Split ("]"[0]);
			if (resumeData.Length > 0)
			{
				List<int> resumeIndexList = new List<int>();
				for (int i=0; i<resumeData.Length; i++)
				{
					int resumeIndex = -1;
					if (int.TryParse (resumeData[i], out resumeIndex) && resumeIndex >= 0)
					{
						resumeIndexList.Add (resumeIndex);
					}
				}
				resumeIndices = resumeIndexList.ToArray ();
			}

			// StartIndex
			int.TryParse (dataArray[2], out startIndex);

			// Skip queue
			int j = 0;
			int.TryParse (dataArray[3], out j);
			inSkipQueue = (j == 1) ? true : false;

			// IsRunning
			j = 0;
			int.TryParse (dataArray[4], out j);
			isRunning = (j == 1) ? true : false;

			// Conversation on end
			int convID = 0;
			int.TryParse (dataArray[5], out convID);
			if (convID != 0)
			{
				if (subScene != null)
				{
					conversationOnEnd = ConstantID.GetComponent <Conversation> (convID, subScene.gameObject.scene);
				}
				else
				{
					conversationOnEnd = ConstantID.GetComponent <Conversation> (convID);
				}
			}

			// Parameter data
			parameterData = dataArray[6];

			// ActionList
			if (!string.IsNullOrEmpty (listName))
			{
				// Asset file
				#if AddressableIsPresent
				if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
				{
					Addressables.LoadAssetAsync<ActionListAsset> (listName).Completed += OnCompleteLoad;
				}
				else
				#endif
				{
					ActionListAsset tempAsset = ScriptableObject.CreateInstance<ActionListAsset> ();
					actionListAsset = AssetLoader.RetrieveAsset<ActionListAsset> (tempAsset, listName);

					if (actionListAsset == null || actionListAsset == tempAsset)
					{
						ACDebug.LogWarning ("Could not restore data related to the ActionList asset '" + listName + "' - to restore it correctly, the asset must be placed in a folder named Resources.");
					}
					else
					{
						KickStarter.actionListAssetManager.AddToList (this);
					}
				}
			}
			else
			{
				int ID = 0;
				if (int.TryParse (listName, out ID))
				{
					// Scene
					ConstantID constantID = (subScene != null)
						? ConstantID.GetComponent (ID, subScene.gameObject.scene)
						: ConstantID.GetComponent (ID);
				
					if (constantID)
					{
						actionList = constantID.GetComponent <ActionList>();
						if (actionList)
						{
							KickStarter.actionListManager.AddToList (this);
						}
					}
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected string ConvertIndicesToString ()
		{
			string data = string.Empty;
			if (resumeIndices != null && resumeIndices.Length > 0)
			{
				for (int i=0; i<resumeIndices.Length; i++)
				{
					data += resumeIndices[i];
					if (i < (resumeIndices.Length - 1))
					{
						data += "]";
					}
				}
			}
			return data;
		}

		#endregion


		#if AddressableIsPresent

		private void OnCompleteLoad (AsyncOperationHandle<ActionListAsset> obj)
		{
			if (obj.Result == null) return;
			actionListAsset = obj.Result;
			KickStarter.actionListAssetManager.AddToList (this);
		}
		
		#endif

	}

}