#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	public class AutoCreateVariableWindow : EditorWindow
	{
	
		private string variableName;
		private VariableLocation location;
		private VariableType variableType;
		private Action actionFor;


		public static void Init (string _variableName, VariableLocation _location, VariableType _variableType, Action _actionFor)
		{
			AutoCreateVariableWindow window = (AutoCreateVariableWindow) EditorWindow.GetWindow (typeof (AutoCreateVariableWindow));
			window.titleContent.text = "Auto-create Variable";
			window.position = new Rect (300, 200, 320, 100);

			window.location = _location;
			window.variableName = _variableName;
			window.variableType = _variableType;
			window.actionFor = _actionFor;
		}
		
		
		private void OnGUI ()
		{
			variableName = EditorGUILayout.TextField ("New Variable name:", variableName);

			if (GUILayout.Button ("Create variable"))
			{
				OnCreateVariable ();
				GUIUtility.ExitGUI ();
			}
		}
	

		private void OnCreateVariable ()
		{
			variableName = variableName.Trim ();
			
			if (string.IsNullOrEmpty (variableName))
			{
				EditorUtility.DisplayDialog ("Unable to create Variable", "Please specify a valid Variable name.", "Close");
				return;
			}
			
			if (location == VariableLocation.Global && AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager != null)
			{
				Undo.RecordObject (AdvGame.GetReferences ().variablesManager, "Create variable");
				CreateNewVariable (AdvGame.GetReferences ().variablesManager.vars);

				UnityVersionHandler.CustomSetDirty (AdvGame.GetReferences ().variablesManager);
			}
			else if (location == VariableLocation.Local && KickStarter.localVariables != null)
			{
				Undo.RecordObject (KickStarter.localVariables, "Create variable");
				CreateNewVariable (KickStarter.localVariables.localVars);

				UnityVersionHandler.CustomSetDirty (KickStarter.localVariables);
			}

			Close ();
		}


		private void CreateNewVariable (List<GVar> _vars)
		{
			if (_vars == null) return;

			GVar newVariable = new GVar (GetIDArray (_vars));
			newVariable.label = variableName;
			newVariable.type = variableType;

			_vars.Add (newVariable);
			ACDebug.Log ("Created new " + location.ToString () + " variable '" + variableName + "'");

			if (actionFor != null)
			{
				if (actionFor is ActionVarSequence)
				{
					ActionVarSequence actionVarSequence = (ActionVarSequence) actionFor;
					actionVarSequence.variableID = newVariable.id;
				}
			}
		}


		private int[] GetIDArray (List<GVar> _vars)
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (GVar variable in _vars)
			{
				idArray.Add (variable.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}

#endif