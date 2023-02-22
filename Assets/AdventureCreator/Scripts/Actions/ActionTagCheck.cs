/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionTagCheck.cs"
 * 
 *	This action checks which tag has been assigned to a given GameObject.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class ActionTagCheck : ActionCheck
	{
		
		public GameObject objectToCheck;
		public int objectToCheckConstantID;
		public int objectToCheckParameterID = -1;
		protected GameObject runtimeObjectToCheck;

		public string tagsToCheck;
		public int tagsToCheckParameterID = -1;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Check tag"; }}
		public override string Description { get { return "This action checks which tag has been assigned to a given GameObject."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeObjectToCheck = AssignFile (parameters, objectToCheckParameterID, objectToCheckConstantID, objectToCheck);
			tagsToCheck = AssignString (parameters, tagsToCheckParameterID, tagsToCheck);
		}


		public override bool CheckCondition ()
		{
			if (runtimeObjectToCheck != null && !string.IsNullOrEmpty (tagsToCheck))
			{
				if (!tagsToCheck.StartsWith (";"))
				{
					tagsToCheck = ";" + tagsToCheck;
				}
				if (!tagsToCheck.EndsWith (";"))
				{
					tagsToCheck += ";";
				}

				string objectTag = runtimeObjectToCheck.tag;
				return (tagsToCheck.Contains (";" + objectTag + ";"));
			}

			return false;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			objectToCheckParameterID = Action.ChooseParameterGUI ("GameObject to check:", parameters, objectToCheckParameterID, ParameterType.GameObject);
			if (objectToCheckParameterID >= 0)
			{
				objectToCheckConstantID = 0;
				objectToCheck = null;
			}
			else
			{
				objectToCheck = (GameObject) EditorGUILayout.ObjectField ("GameObject to check:", objectToCheck, typeof (GameObject), true);
				
				objectToCheckConstantID = FieldToID (objectToCheck, objectToCheckConstantID);
				objectToCheck = IDToField (objectToCheck, objectToCheckConstantID, false);
			}

			tagsToCheckParameterID = Action.ChooseParameterGUI ("Check has tag(s):", parameters, tagsToCheckParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
			if (tagsToCheckParameterID < 0)
			{
				tagsToCheck = EditorGUILayout.TextField ("Check has tag(s):", tagsToCheck);
			}
			EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (objectToCheck, objectToCheckConstantID, objectToCheckParameterID);
		}
		

		public override string SetLabel ()
		{
			if (objectToCheck != null)
			{
				return objectToCheck.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (objectToCheckParameterID < 0)
			{
				if (objectToCheck && objectToCheck == gameObject) return true;
				if (objectToCheckConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Check tag' Action</summary>
		 * <param name = "gameObject">The GameObject to query</param>
		 * <param name = "tag">The tag to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTagCheck CreateNew (GameObject gameObject, string tag)
		{
			ActionTagCheck newAction = CreateNew<ActionTagCheck> ();
			newAction.objectToCheck = gameObject;
			newAction.TryAssignConstantID (newAction.objectToCheck, ref newAction.objectToCheckConstantID);
			newAction.tagsToCheck = tag;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Check tag' Action</summary>
		 * <param name = "gameObject">The GameObject to query</param>
		 * <param name = "tags">An array of tags to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTagCheck CreateNew (GameObject gameObject, string[] tags)
		{
			ActionTagCheck newAction = CreateNew<ActionTagCheck> ();
			newAction.objectToCheck = gameObject;
			newAction.TryAssignConstantID (newAction.objectToCheck, ref newAction.objectToCheckConstantID);
			string combined = string.Empty;
			for (int i=0; i<tags.Length; i++)
			{
				combined += tags[i];
				if (i < (tags.Length-1))
				{
					combined += ";";
				}
			}
			newAction.tagsToCheck = combined;
			return newAction;
		}
		
	}

}