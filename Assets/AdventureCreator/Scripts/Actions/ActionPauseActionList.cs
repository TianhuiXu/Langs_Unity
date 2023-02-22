/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionPauseActionList.cs"
 * 
 *	This action pauses and resumes ActionLists.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionPauseActionList : Action
	{

		public enum PauseResume { Pause, Resume };
		public PauseResume pauseResume = PauseResume.Pause;

		public ActionRunActionList.ListSource listSource = ActionRunActionList.ListSource.InScene;
		public ActionListAsset actionListAsset;

		public bool rerunPausedActions;

		public ActionList actionList;
		protected ActionList _runtimeActionList;

		public int constantID = 0;
		public int parameterID = -1;

		protected RuntimeActionList[] runtimeActionLists = new RuntimeActionList[0];


		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Pause or resume"; }}
		public override string Description { get { return "Pauses and resumes ActionLists."; }}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (listSource == ActionRunActionList.ListSource.InScene)
			{
				_runtimeActionList = AssignFile <ActionList> (parameters, parameterID, constantID, actionList);
			}
			else
			{
				actionListAsset = (ActionListAsset) AssignObject <ActionListAsset> (parameters, parameterID, actionListAsset);
			}
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				runtimeActionLists = new RuntimeActionList[0];

				if (pauseResume == PauseResume.Pause)
				{
					if (listSource == ActionRunActionList.ListSource.AssetFile && actionListAsset != null)
					{
						if (actionListAsset.actions.Contains (this))
						{
							LogWarning ("An ActionList Asset cannot Pause itself - it must be performed indirectly.");
						}
						else
						{
							runtimeActionLists = KickStarter.actionListAssetManager.Pause (actionListAsset);

							if (willWait && runtimeActionLists.Length > 0)
							{
								return defaultPauseTime;
							}
						}
					}
					else if (listSource == ActionRunActionList.ListSource.InScene && _runtimeActionList != null)
					{
						if (_runtimeActionList.actions.Contains (this))
						{
							LogWarning ("An ActionList cannot Pause itself - it must be performed indirectly.");
						}
						else
						{
							_runtimeActionList.Pause ();

							if (willWait)
							{
								return defaultPauseTime;
							}
						}
					}
				}
				else if (pauseResume == PauseResume.Resume)
				{
					if (listSource == ActionRunActionList.ListSource.AssetFile && actionListAsset != null && !actionListAsset.actions.Contains (this))
					{
						KickStarter.actionListAssetManager.Resume (actionListAsset, rerunPausedActions);
					}
					else if (listSource == ActionRunActionList.ListSource.InScene && _runtimeActionList != null && !_runtimeActionList.actions.Contains (this))
					{
						KickStarter.actionListManager.Resume (_runtimeActionList, rerunPausedActions);
					}
				}
			}
			else
			{
				if (listSource == ActionRunActionList.ListSource.AssetFile)
				{
					foreach (RuntimeActionList runtimeActionList in runtimeActionLists)
					{
						if (runtimeActionList != null && KickStarter.actionListManager.IsListRunning (runtimeActionList))
						{
							return defaultPauseTime;
						}
					}
				}
				else if (listSource == ActionRunActionList.ListSource.InScene)
				{
					if (KickStarter.actionListManager.IsListRunning (_runtimeActionList))
					{
						return defaultPauseTime;
					}
				}

				isRunning = false;
				return 0f;
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			pauseResume = (PauseResume) EditorGUILayout.EnumPopup ("Method:", pauseResume);

			listSource = (ActionRunActionList.ListSource) EditorGUILayout.EnumPopup ("Source:", listSource);
			if (listSource == ActionRunActionList.ListSource.InScene)
			{
				parameterID = Action.ChooseParameterGUI ("ActionList:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					actionList = null;
				}
				else
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					constantID = FieldToID <ActionList> (actionList, constantID);
					actionList = IDToField <ActionList> (actionList, constantID, true);

					if (actionList != null && actionList.actions.Contains (this))
					{
						EditorGUILayout.HelpBox ("An ActionList cannot " + pauseResume.ToString () + " itself - it must be performed indirectly.", MessageType.Warning);
					}
				}
			}
			else if (listSource == ActionRunActionList.ListSource.AssetFile)
			{
				parameterID = Action.ChooseParameterGUI ("ActionList asset:", parameters, parameterID, ParameterType.UnityObject);
				if (parameterID < 0)
				{
					actionListAsset = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", actionListAsset, typeof (ActionListAsset), false);

					if (actionListAsset != null && actionListAsset.actions.Contains (this))
					{
						EditorGUILayout.HelpBox ("An ActionList Asset cannot " + pauseResume.ToString () + " itself - it must be performed indirectly.", MessageType.Warning);
					}
				}

			}
			
			if (pauseResume == PauseResume.Pause)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
				if (willWait)
				{
					EditorGUILayout.HelpBox ("The ActionList will complete any currently-running Actions before it pauses.", MessageType.Info);
				}
			}
			else if (pauseResume == PauseResume.Resume)
			{
				rerunPausedActions = EditorGUILayout.ToggleLeft ("Re-run Action(s) at time of pause?", rerunPausedActions);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (listSource == ActionRunActionList.ListSource.InScene)
			{
				AssignConstantID <ActionList> (actionList, constantID, parameterID);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (listSource == ActionRunActionList.ListSource.InScene && actionList != null)
			{
				return pauseResume.ToString () + " " + actionList.name;
			}
			else if (listSource == ActionRunActionList.ListSource.AssetFile && actionList != null)
			{
				return pauseResume.ToString () + " " + actionList.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0 && listSource == ActionRunActionList.ListSource.InScene)
			{
				if (actionList && actionList.gameObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}


		public override bool ReferencesAsset (ActionListAsset _actionListAsset)
		{
			if (listSource == ActionRunActionList.ListSource.AssetFile && _actionListAsset == actionListAsset)
				return true;
			return base.ReferencesAsset (_actionListAsset);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Pause or resume' Action, set to pause an ActionList</summary>
		 * <param name = "actionList">The ActionList to pause</param>
		 * <param name = "waitUntilFinish">If True, any Actions currently running in the ActionList will complete before it is paused</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPauseActionList CreateNew_Pause (ActionList actionList, bool waitUntilFinish = false)
		{
			ActionPauseActionList newAction = CreateNew<ActionPauseActionList> ();
			newAction.pauseResume = PauseResume.Pause;
			newAction.listSource = ActionRunActionList.ListSource.InScene;
			newAction.actionList = actionList;
			newAction.TryAssignConstantID (newAction.actionList, ref newAction.constantID);
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Pause or resume' Action, set to pause an ActionList</summary>
		 * <param name = "actionListAsset">The ActionList asset to pause</param>
		 * <param name = "waitUntilFinish">If True, any Actions currently running in the ActionList will complete before it is paused</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPauseActionList CreateNew_Pause (ActionListAsset actionListAsset, bool waitUntilFinish = false)
		{
			ActionPauseActionList newAction = CreateNew<ActionPauseActionList> ();
			newAction.pauseResume = PauseResume.Pause;
			newAction.listSource = ActionRunActionList.ListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Pause or resume' Action, set to resume an ActionList</summary>
		 * <param name = "actionList">The ActionList to resume</param>
		 * <param name = "rerunLastAction">If True, the Action which was running at the time the ActionList was previously paused will be re-run</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPauseActionList CreateNew_Resume (ActionList actionList, bool rerunLastAction = false)
		{
			ActionPauseActionList newAction = CreateNew<ActionPauseActionList> ();
			newAction.pauseResume = PauseResume.Resume;
			newAction.listSource = ActionRunActionList.ListSource.InScene;
			newAction.actionList = actionList;
			newAction.TryAssignConstantID (newAction.actionList, ref newAction.constantID);
			newAction.rerunPausedActions = rerunLastAction;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Pause or resume' Action, set to resume an ActionList asset</summary>
		 * <param name = "actionListAsset">The ActionList to resume</param>
		 * <param name = "rerunLastAction">If True, the Action which was running at the time the ActionList was previously paused will be re-run</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPauseActionList CreateNew_Resume (ActionListAsset actionListAsset, bool rerunLastAction = false)
		{
			ActionPauseActionList newAction = CreateNew<ActionPauseActionList> ();
			newAction.pauseResume = PauseResume.Resume;
			newAction.listSource = ActionRunActionList.ListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.rerunPausedActions = rerunLastAction;
			return newAction;
		}
		
	}
	
}