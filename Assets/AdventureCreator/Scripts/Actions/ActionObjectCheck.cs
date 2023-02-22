/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionObjectCheck.cs"
 * 
 *	This action checks if an object is
 *	in the scene.
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
	public class ActionObjectCheck : ActionCheck
	{

		public GameObject gameObject;
		public int parameterID = -1;
		public int constantID = 0; 
		protected GameObject runtimeGameObject;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Check presence"; }}
		public override string Description { get { return "Use to determine if a particular GameObject or prefab is present in the current scene."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeGameObject = AssignFile (parameters, parameterID, constantID, gameObject);
		}
		
		
		public override bool CheckCondition ()
		{
			if (runtimeGameObject != null && runtimeGameObject.activeInHierarchy)
			{
				return true;
			}
			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Object to check:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				gameObject = null;
			}
			else
			{
				gameObject = (GameObject) EditorGUILayout.ObjectField ("Object to check:", gameObject, typeof (GameObject), true);
				
				constantID = FieldToID (gameObject, constantID);
				gameObject = IDToField (gameObject, constantID, false);
			}
		}


		public override string SetLabel ()
		{
			if (gameObject != null)
			{
				return gameObject.name;
			}
			return string.Empty;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (gameObject, constantID, parameterID);
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (gameObject && gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Check presence' Action</summary>
		 * <param name = "objectToCheck">The GameObject to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionObjectCheck CreateNew (GameObject objectToCheck)
		{
			ActionObjectCheck newAction = CreateNew<ActionObjectCheck> ();
			newAction.gameObject = objectToCheck;
			newAction.TryAssignConstantID (newAction.gameObject, ref newAction.constantID);
			return newAction;
		}
		
	}

}