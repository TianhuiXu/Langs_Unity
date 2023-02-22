/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVarCopy.cs"
 * 
 *	This action is used to transfer the value of one Variable to another
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
	public class ActionVarCopy : Action
	{
		
		public int oldParameterID = -1;
		public int oldVariableID;
		public VariableLocation oldLocation;

		public int newParameterID = -1;
		public int newVariableID;
		public VariableLocation newLocation;

		public Variables oldVariables;
		public int oldVariablesConstantID = 0;

		public Variables newVariables;
		public int newVariablesConstantID = 0;

		#if UNITY_EDITOR
		protected VariableType oldVarType = VariableType.Boolean;
		protected VariableType newVarType = VariableType.Boolean;
		#endif

		protected LocalVariables localVariables;
		protected GVar oldRuntimeVariable;
		protected GVar newRuntimeVariable;
		protected Variables newRuntimeVariables;


		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Copy"; }}
		public override string Description { get { return "Copies the value of one Variable to another. This can be between Global and Local Variables, but only of those with the same type, such as Integer or Float."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			oldRuntimeVariable = null;
			switch (oldLocation)
			{
				case VariableLocation.Global:
					oldVariableID = AssignVariableID (parameters, oldParameterID, oldVariableID);
					oldRuntimeVariable = GlobalVariables.GetVariable (oldVariableID);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						oldVariableID = AssignVariableID (parameters, oldParameterID, oldVariableID);
						oldRuntimeVariable = LocalVariables.GetVariable (oldVariableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					Variables oldRuntimeVariables = AssignFile <Variables> (oldVariablesConstantID, oldVariables);
					if (oldRuntimeVariables != null)
					{
						oldRuntimeVariable = oldRuntimeVariables.GetVariable (oldVariableID);
					}
					oldRuntimeVariable = AssignVariable (parameters, oldParameterID, oldRuntimeVariable);
					break;
			}

			newRuntimeVariable = null;
			switch (newLocation)
			{
				case VariableLocation.Global:
					newVariableID = AssignVariableID (parameters, newParameterID, newVariableID);
					newRuntimeVariable = GlobalVariables.GetVariable (newVariableID, true);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						newVariableID = AssignVariableID (parameters, newParameterID, newVariableID);
						newRuntimeVariable = LocalVariables.GetVariable (newVariableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					newRuntimeVariables = AssignFile <Variables> (newVariablesConstantID, newVariables);
					if (newRuntimeVariables != null)
					{
						newRuntimeVariable = newRuntimeVariables.GetVariable (newVariableID);
					}
					newRuntimeVariable = AssignVariable (parameters, newParameterID, newRuntimeVariable);
					newRuntimeVariables = AssignVariablesComponent (parameters, newParameterID, newRuntimeVariables);
					break;
			}

		}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}


		public override float Run ()
		{
			if (oldRuntimeVariable != null && newRuntimeVariable != null)
			{
				CopyVariable (newRuntimeVariable, oldRuntimeVariable);
				newRuntimeVariable.Upload (newLocation, newRuntimeVariables);
			}

			return 0f;
		}

		
		protected void CopyVariable (GVar newVar, GVar oldVar)
		{
			if (newVar == null || oldVar == null)
			{
				LogWarning ("Cannot copy variable since it cannot be found!");
				return;
			}

			newVar.CopyFromVariable (oldVar, oldLocation);
			KickStarter.actionListManager.VariableChanged ();
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			// OLD

			oldLocation = (VariableLocation) EditorGUILayout.EnumPopup ("'From' source:", oldLocation);
			bool gotOldVar = false;

			switch (oldLocation)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager != null)
					{
						oldVariableID = ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, oldVariableID, oldParameterID, false);
						gotOldVar = (AdvGame.GetReferences ().variablesManager.vars.Count > 0);
					}
					break;

				case VariableLocation.Local:
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					else if (localVariables != null)
					{
						oldVariableID = ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, oldVariableID, oldParameterID, false);
						gotOldVar = (localVariables.localVars.Count > 0);
					}
					else
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					oldParameterID = Action.ChooseParameterGUI ("'From' variable:", parameters, oldParameterID, ParameterType.ComponentVariable);
					if (oldParameterID >= 0)
					{
						oldVariables = null;
						oldVariablesConstantID = 0;	
					}
					else
					{
						oldVariables = (Variables) EditorGUILayout.ObjectField ("'From' component:", oldVariables, typeof (Variables), true);
						if (oldVariables != null)
						{
							oldVariableID = ShowVarGUI (oldVariables.vars, null, ParameterType.ComponentVariable, oldVariableID, oldParameterID, false);
							gotOldVar = (oldVariables.vars.Count > 0);
						}

						oldVariablesConstantID = FieldToID <Variables> (oldVariables, oldVariablesConstantID);
						oldVariables = IDToField <Variables> (oldVariables, oldVariablesConstantID, false);
					}
					break;
			}

			EditorGUILayout.Space ();

			// NEW

			newLocation = (VariableLocation) EditorGUILayout.EnumPopup ("'To' source:", newLocation);
			bool gotNewVar = false;

			switch (newLocation)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager != null)
					{
						newVariableID = ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, newVariableID, newParameterID, true);
						gotNewVar = (AdvGame.GetReferences ().variablesManager.vars.Count > 0);
					}
					break;

				case VariableLocation.Local:
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					else if (localVariables != null)
					{
						newVariableID = ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, newVariableID, newParameterID, true);
						gotNewVar = (localVariables.localVars.Count > 0);
					}
					else
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					newParameterID = Action.ChooseParameterGUI ("'To' variable:", parameters, newParameterID, ParameterType.ComponentVariable);
					if (newParameterID >= 0)
					{
						newVariables = null;
						newVariablesConstantID = 0;	
					}
					else
					{
						newVariables = (Variables) EditorGUILayout.ObjectField ("'To' component:", newVariables, typeof (Variables), true);
						if (newVariables != null)
						{
							newVariableID = ShowVarGUI (newVariables.vars, null, ParameterType.ComponentVariable, newVariableID, newParameterID, true);
							gotNewVar = (newVariables.vars.Count > 0);
						}

						newVariablesConstantID = FieldToID <Variables> (newVariables, newVariablesConstantID);
						newVariables = IDToField <Variables> (newVariables, newVariablesConstantID, false);
					}
					break;
			}

			// Types match?
			if (oldParameterID == -1 && newParameterID == -1 && newVarType != oldVarType && gotOldVar && gotNewVar)
			{
				EditorGUILayout.HelpBox ("The chosen Variables do not share the same Type - a conversion will be attemped", MessageType.Info);
			}
		}


		private int ShowVarGUI (List<GVar> vars, List<ActionParameter> parameters, ParameterType parameterType, int variableID, int parameterID, bool isNew)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int variableNumber = -1;

			if (vars.Count > 0)
			{
				foreach (GVar _var in vars)
				{
					labelList.Add (_var.label);
					
					// If a GlobalVar variable has been removed, make sure selected variable is still valid
					if (_var.id == variableID)
					{
						variableNumber = i;
					}
					
					i ++;
				}
				
				if (variableNumber == -1 && (parameters == null || parameters.Count == 0 || parameterID == -1))
				{
					// Wasn't found (variable was deleted?), so revert to zero
					if (variableID > 0) LogWarning ("Previously chosen variable no longer exists!");
					variableNumber = 0;
					variableID = 0;
				}

				string label = "'From' variable:";
				if (isNew)
				{
					label = "'To' variable:";
				}

				parameterID = Action.ChooseParameterGUI (label, parameters, parameterID, parameterType);
				if (parameterID >= 0)
				{
					//variableNumber = 0;
					variableNumber = Mathf.Min (variableNumber, vars.Count-1);
					variableID = -1;
				}
				else
				{
					variableNumber = EditorGUILayout.Popup (label, variableNumber, labelList.ToArray());
					variableNumber = Mathf.Max (0, variableNumber);
					variableID = vars [variableNumber].id;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No variables exist!", MessageType.Info);
				variableID = -1;
				variableNumber = -1;
			}

			if (isNew)
			{
				newParameterID = parameterID;

				if (variableNumber >= 0)
				{
					newVarType = vars[variableNumber].type;
				}
			}
			else
			{
				oldParameterID = parameterID;

				if (variableNumber >= 0)
				{
					oldVarType = vars[variableNumber].type;
				}
			}

			return variableID;
		}


		public override string SetLabel ()
		{
			switch (newLocation)
			{
				case VariableLocation.Local:
					if (!isAssetFile && localVariables)
					{
						return GetLabelString (localVariables.localVars, newVariableID);
					}
					break;

				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager)
					{
						return GetLabelString (AdvGame.GetReferences ().variablesManager.vars, newVariableID);
					}
					break;

				case VariableLocation.Component:
					if (newVariables != null)
					{
						return GetLabelString (newVariables.vars, newVariableID);
					}
					break;
			}
			return string.Empty;
		}


		private string GetLabelString (List<GVar> vars, int variableID)
		{
			if (vars.Count > 0)
			{
				foreach (GVar _var in vars)
				{
					if (_var.id == variableID)
					{
						return _var.label;
					}
				}
			}
			return string.Empty;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (oldLocation == VariableLocation.Local && oldVariableID == oldLocalID)
			{
				oldLocation = VariableLocation.Global;
				oldVariableID = newGlobalID;
				wasAmended = true;
			}

			if (newLocation == VariableLocation.Local && newVariableID == oldLocalID)
			{
				newLocation = VariableLocation.Global;
				newVariableID = newGlobalID;
				wasAmended = true;
			}

			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool wasAmended = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (oldLocation == VariableLocation.Global && oldVariableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					oldLocation = VariableLocation.Local;
					oldVariableID = newLocalID;
				}
			}

			if (newLocation == VariableLocation.Global && newVariableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					newLocation = VariableLocation.Local;
					newVariableID = newLocalID;
				}
			}

			return wasAmended;
		}


		public override int GetNumVariableReferences (VariableLocation _location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (oldLocation == _location && oldVariableID == varID)
			{
				if (_location != VariableLocation.Component || (_variables && _variables == oldVariables) || (oldVariablesConstantID != 0 && _variablesConstantID == oldVariablesConstantID))
				{
					thisCount ++;
				}
			}

			if (newLocation == _location && newVariableID == varID)
			{
				if (_location != VariableLocation.Component || (_variables && _variables == newVariables) || (newVariablesConstantID != 0 && _variablesConstantID == newVariablesConstantID))
				{
					thisCount ++;
				}
			}

			thisCount += base.GetNumVariableReferences (_location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation _location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (oldLocation == _location && oldVariableID == oldVarID)
			{
				if (_location != VariableLocation.Component || (_variables && _variables == oldVariables) || (oldVariablesConstantID != 0 && _variablesConstantID == oldVariablesConstantID))
				{
					oldVariableID = newVarID;
					thisCount++;
				}
			}

			if (newLocation == _location && newVariableID == newVarID)
			{
				if (_location != VariableLocation.Component || (_variables && _variables == newVariables) || (newVariablesConstantID != 0 && _variablesConstantID == newVariablesConstantID))
				{
					newVariableID = newVarID;
					thisCount++;
				}
			}

			thisCount += base.UpdateVariableReferences (_location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (oldLocation == VariableLocation.Component)
			{
				if (saveScriptsToo && oldVariables && oldParameterID < 0)
				{
					AddSaveScript<RememberVariables> (oldVariables);
				}

				AssignConstantID <Variables> (oldVariables, oldVariablesConstantID, oldParameterID);
			}

			if (newLocation == VariableLocation.Component)
			{
				if (saveScriptsToo && newVariables && newParameterID < 0)
				{
					AddSaveScript<RememberVariables> (newVariables);
				}

				AssignConstantID <Variables> (newVariables, newVariablesConstantID, newParameterID);
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (oldParameterID < 0 && oldLocation == VariableLocation.Component)
			{
				if (oldVariables && oldVariables.gameObject == gameObject) return true;
				if (oldVariablesConstantID == id && id != 0) return true;
			}
			if (newParameterID < 0 && oldLocation == VariableLocation.Component)
			{
				if (newVariables && newVariables.gameObject == gameObject) return true;
				if (newVariablesConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Variable: Copy' Action</summary>
		 * <param name = "fromVariableLocation">The location of the variable to copy from</param>
		 * <param name = "fromVariables">The Variables component of the variable to copy from, if a Component Variable</param>
		 * <param name = "fromVariableID">The ID number of the variable to copy from</param>
		 * <param name = "toVariableLocation">The location of the variable to copy to</param>
		 * <param name = "toVariables">The Variables component of the variable to copy to, if a Component Variable</param>
		 * <param name = "toVariableID">The ID number of the variable to copy to</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCopy CreateNew (VariableLocation fromVariableLocation, Variables fromVariables, int fromVariableID, VariableLocation toVariableLocation, Variables toVariables, int toVariableID)
		{
			ActionVarCopy newAction = CreateNew<ActionVarCopy> ();
			newAction.oldLocation = fromVariableLocation;
			newAction.oldVariables = fromVariables;
			newAction.TryAssignConstantID (newAction.oldVariables, ref newAction.oldVariablesConstantID);
			newAction.oldVariableID = fromVariableID;
			newAction.newLocation = toVariableLocation;
			newAction.newVariables = toVariables;
			newAction.TryAssignConstantID (newAction.newVariables, ref newAction.newVariablesConstantID);
			newAction.newVariableID = toVariableID;
			return newAction;
		}

	}

}