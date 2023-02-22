/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCheckActionList.cs"
 * 
 *	This Action will return "TRUE" if the supplied ActionList
 *	is running, and "FALSE" if it is not.
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
	public class ActionCheckActionList : ActionCheck
	{
		
		public enum ListSource { InScene, AssetFile };
		public ListSource listSource = ListSource.InScene;

		public bool checkSelfSkipping = false;
		public ActionList actionList;
		protected ActionList runtimeActionList;
		public ActionListAsset actionListAsset;
		public int constantID = 0;
		public int parameterID = -1;

		protected ActionListAsset runtimeActionListAsset;
		protected bool isSkipping = false;


		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Check running"; }}
		public override string Description { get { return "Queries whether or not a supplied ActionList is currently running. By looping the If condition is not met field back onto itself, this will effectively “wait” until the supplied ActionList has completed before continuing."; }}


		public override float Run ()
		{
			isSkipping = false;
			return 0f;
		}


		public override void Skip ()
		{
			isSkipping = true;
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (listSource == ListSource.InScene)
			{
				runtimeActionList = AssignFile<ActionList> (parameters, parameterID, constantID, actionList);
			}
			else
			{
				runtimeActionListAsset = (ActionListAsset)AssignObject<ActionListAsset> (parameters, parameterID, actionListAsset);
			}
		}


		public override bool CheckCondition ()
		{
			if (checkSelfSkipping)
			{
				return isSkipping;
			}

			if (isSkipping && IsTargetSkippable ())
			{
				return false;
			}

			if (listSource == ListSource.InScene && runtimeActionList != null)
			{
				return KickStarter.actionListManager.IsListRunning (runtimeActionList);
			}
			else if (listSource == ListSource.AssetFile && runtimeActionListAsset != null)
			{
				return KickStarter.actionListAssetManager.IsListRunning (runtimeActionListAsset);
			}
			
			return false;
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			checkSelfSkipping = EditorGUILayout.Toggle ("Check self is skipping?", checkSelfSkipping);
			if (checkSelfSkipping)
			{
				return;
			}

			listSource = (ListSource) EditorGUILayout.EnumPopup ("Source:", listSource);
			if (listSource == ListSource.InScene)
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
				}
			}
			else if (listSource == ListSource.AssetFile)
			{
				parameterID = Action.ChooseParameterGUI ("ActionList asset:", parameters, parameterID, ParameterType.UnityObject);
				if (parameterID < 0)
				{
					actionListAsset = (ActionListAsset)EditorGUILayout.ObjectField ("ActionList asset:", actionListAsset, typeof (ActionListAsset), true);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (listSource == ListSource.InScene)
			{
				AssignConstantID <ActionList> (actionList, constantID, parameterID);
			}
		}


		public override string SetLabel ()
		{
			if (listSource == ListSource.InScene && actionList != null)
			{
				return actionList.name;
			}
			else if (listSource == ListSource.AssetFile && actionListAsset != null)
			{
				return actionListAsset.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (listSource == ListSource.InScene && parameterID < 0)
			{
				if (actionList && actionList.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesAsset (ActionListAsset _actionListAsset)
		{
			if (listSource == ListSource.AssetFile && _actionListAsset == actionListAsset)
				return true;
			return base.ReferencesAsset (_actionListAsset);
		}

		#endif


		public override void SetLastResult (int _lastRunOutput)
		{
			if (!IsTargetSkippable () && !checkSelfSkipping)
			{
				// When skipping, don't want to rely on last result if target can be skipped as well
				base.SetLastResult (_lastRunOutput);
				return;
			}

			ResetLastResult ();
		}


		protected bool IsTargetSkippable ()
		{
			if (listSource == ListSource.InScene && actionList != null)
			{
				return actionList.IsSkippable ();
			}
			else if (listSource == ListSource.AssetFile && actionListAsset != null)
			{
				return actionListAsset.IsSkippable ();
			}
			return false;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check running' Action, set query if its own ActionList is being skipped</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCheckActionList CreateNew_CheckSelfIsSkipping ()
		{
			ActionCheckActionList newAction = CreateNew<ActionCheckActionList> ();
			newAction.checkSelfSkipping = true;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check running' Action, set query if a ActionList is running</summary>
		 * <param name = "actionList">The ActionList to check</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCheckActionList CreateNew_CheckOther (ActionList actionList)
		{
			ActionCheckActionList newAction = CreateNew<ActionCheckActionList> ();
			newAction.listSource = ListSource.InScene;
			newAction.actionList = actionList;
			newAction.TryAssignConstantID (newAction.actionList, ref newAction.constantID);
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check running' Action, set query if a ActionList is running</summary>
		 * <param name = "actionListAsset">The ActionList asset to check</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCheckActionList CreateNew_CheckOther (ActionListAsset actionListAsset)
		{
			ActionCheckActionList newAction = CreateNew<ActionCheckActionList> ();
			newAction.listSource = ListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			return newAction;
		}

	}

}