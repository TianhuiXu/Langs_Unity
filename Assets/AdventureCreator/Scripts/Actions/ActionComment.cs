/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionComment.cs"
 * 
 *	This action simply displays a comment in the Editor / Inspector.
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
	public class ActionComment : Action
	{
		
		public string commentText = "";

		protected enum ACLogType { No, AsInfo, AsWarning, AsError };
		[SerializeField] protected ACLogType acLogType = ACLogType.AsInfo;
		protected string convertedText;
		
		
		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Comment"; }}
		public override string Description { get { return "Prints a comment for debug purposes."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			convertedText = AdvGame.ConvertTokens (commentText, 0, null, parameters);
		}


		public override float Run ()
		{
			if (!string.IsNullOrEmpty (convertedText))
			{
				switch (acLogType)
				{
					case ACLogType.No:
					default:
						break;

					case ACLogType.AsInfo:
						Log (convertedText);
						break;

					case ACLogType.AsWarning:
						LogWarning (convertedText);
						break;

					case ACLogType.AsError:
						LogError (convertedText);
						break;
				}
			}
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			EditorStyles.textField.wordWrap = true;
			commentText = CustomGUILayout.TextArea ("Comment:", commentText);

			acLogType = (ACLogType) EditorGUILayout.EnumPopup ("Display in Console?", acLogType);

			if (!string.IsNullOrEmpty (commentText) && acLogType != ACLogType.No)
			{
				if (KickStarter.settingsManager.showDebugLogs == ShowDebugLogs.Never || (KickStarter.settingsManager.showDebugLogs == ShowDebugLogs.OnlyWarningsOrErrors && acLogType == ACLogType.AsInfo))
				{
					EditorGUILayout.HelpBox ("To enable comment-logging, configure the Settings Manager's 'Show logs in Console' field.", MessageType.Warning);
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			if (!string.IsNullOrEmpty (commentText))
			{
				int i = commentText.IndexOf ("\n");
				if (i > 0)
				{
					return commentText.Substring (0, i);
				}
				return commentText;
			}
			return string.Empty;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			string updatedCommentText = AdvGame.ConvertLocalVariableTokenToGlobal (commentText, oldLocalID, newGlobalID);
			if (commentText != updatedCommentText)
			{
				wasAmended = true;
				commentText = updatedCommentText;
			}
			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			string updatedCommentText = AdvGame.ConvertGlobalVariableTokenToLocal (commentText, oldGlobalID, newLocalID);
			if (commentText != updatedCommentText)
			{
				isAffected = true;
				if (isCorrectScene)
				{
					commentText = updatedCommentText;
				}
			}
			return isAffected;
		}


		public override int GetNumVariableReferences (VariableLocation location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			string tokenText = AdvGame.GetVariableTokenText (location, varID, _variablesConstantID);

			if (!string.IsNullOrEmpty (tokenText) && commentText.Contains (tokenText))
			{
				thisCount ++;
			}
			thisCount += base.GetNumVariableReferences (location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			string oldTokenText = AdvGame.GetVariableTokenText (location, oldVarID, _variablesConstantID);

			if (!string.IsNullOrEmpty (oldTokenText) && commentText.Contains (oldTokenText))
			{
				string newTokenText = AdvGame.GetVariableTokenText (location, newVarID, _variablesConstantID);
				commentText = commentText.Replace (oldTokenText, newTokenText);
				thisCount++;
			}
			thisCount += base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Pause' Action with key variables already set.</summary>
		 * <param name = "text">The text to display in the Console</param>
		 * <param name = "displayAsWarning">If True, the text will display as a Warning. Otherwise, it will display as Info</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionComment CreateNew (string text, bool displayAsWarning = false)
		{
			ActionComment newAction = CreateNew<ActionComment> ();
			newAction.commentText = text;
			newAction.acLogType = (displayAsWarning) ? ACLogType.AsWarning : ACLogType.AsInfo;
			return newAction;
		}
		
	}
	
}