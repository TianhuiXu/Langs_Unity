/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVarSequence.cs"
 * 
 *	This action reads a Popup Variable and performs
 *	different follow-up Actions based on its value.
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
	public class ActionVarPopup : Action
	{
		
		public int variableID;
		public int variableNumber;
		public VariableLocation location = VariableLocation.Global;

		public int numSockets = 2;
		protected LocalVariables localVariables;

		public Variables variables;
		public int variablesConstantID = 0;

		[SerializeField] protected int parameterID = -1;

		#if UNITY_EDITOR
		[SerializeField] protected int placeholderNumValues = 2;
		[SerializeField] protected int placeholderPopUpLabelDataID = 0;
		#endif

		protected GVar runtimeVariable;


		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Pop Up switch"; }}
		public override string Description { get { return "Uses the value of a Pop Up Variable to determine which Action is run next. An option for each possible value the Variable can take will be displayed, allowing for different subsequent Actions to run."; }}
		public override int NumSockets { get { return numSockets; }}


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


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeVariable = null;
			switch (location)
			{
				case VariableLocation.Global:
					variableID = AssignVariableID (parameters, parameterID, variableID);
					runtimeVariable = GlobalVariables.GetVariable (variableID, true);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						variableID = AssignVariableID (parameters, parameterID, variableID);
						runtimeVariable = LocalVariables.GetVariable (variableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
					if (runtimeVariables != null)
					{
						runtimeVariable = runtimeVariables.GetVariable (variableID);
					}
					runtimeVariable = AssignVariable (parameters, parameterID, runtimeVariable);
					break;
			}
		}
		

		public override int GetNextOutputIndex ()
		{
			if (runtimeVariable != null && runtimeVariable.type != VariableType.PopUp)
			{
				LogWarning ("Variable: Pop Up switch Action is referencing a Variable that is not a PopUp!");
				runtimeVariable = null;
			}
			else if (runtimeVariable == null)
			{
				LogWarning ("Variable: Pop Up switch Action is referencing a Variable that does not exist!");
			}

			if (numSockets <= 0)
			{
				LogWarning ("Could not compute Random check because no values were possible!");
				return -1;
			}
			
			if (runtimeVariable != null)
			{
				return runtimeVariable.IntegerValue;
			}
			
			return -1;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			location = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", location);

			switch (location)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager != null)
					{
						parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.GlobalVariable);
						if (parameterID >= 0)
						{
							placeholderPopUpLabelDataID = AdvGame.GetReferences ().variablesManager.ShowPlaceholderPresetData (placeholderPopUpLabelDataID);
							if (placeholderPopUpLabelDataID <= 0)
							{
								placeholderNumValues = EditorGUILayout.DelayedIntField ("Placeholder # of values:", placeholderNumValues);
								if (placeholderNumValues < 1) placeholderNumValues = 1;
							}
						}
						else
						{
							variableID = AdvGame.GlobalVariableGUI ("PopUp variable:", variableID, VariableType.PopUp);
						}
					}
					break;

				case VariableLocation.Local:
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					else if (localVariables == null)
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					else
					{
						parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.LocalVariable);
						if (parameterID >= 0)
						{
							placeholderPopUpLabelDataID = AdvGame.GetReferences ().variablesManager.ShowPlaceholderPresetData (placeholderPopUpLabelDataID);
							if (placeholderPopUpLabelDataID <= 0)
							{
								placeholderNumValues = EditorGUILayout.DelayedIntField ("Placeholder # of values:", placeholderNumValues);
								if (placeholderNumValues < 1) placeholderNumValues = 1;
							}
						}
						else
						{
							variableID = AdvGame.LocalVariableGUI ("PopUp variable:", variableID, VariableType.PopUp);
						}
					}
					break;

				case VariableLocation.Component:
					parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.ComponentVariable);
					if (parameterID >= 0)
					{
						placeholderPopUpLabelDataID = AdvGame.GetReferences ().variablesManager.ShowPlaceholderPresetData (placeholderPopUpLabelDataID);
						if (placeholderPopUpLabelDataID <= 0)
						{
							placeholderNumValues = EditorGUILayout.DelayedIntField ("Placeholder # of values:", placeholderNumValues);
							if (placeholderNumValues < 1) placeholderNumValues = 1;
						}
					}
					else
					{
						variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
						variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
						variables = IDToField <Variables> (variables, variablesConstantID, false);
						
						if (variables != null)
						{
							variableID = AdvGame.ComponentVariableGUI ("PopUp variable:", variableID, VariableType.PopUp, variables);	
						}
					}
					break;
			}

			if (parameterID >= 0)
			{
				numSockets = placeholderNumValues;
				PopUpLabelData popUpLabelData = AdvGame.GetReferences ().variablesManager.GetPopUpLabelData (placeholderPopUpLabelDataID);
				if (popUpLabelData != null)
				{
					numSockets = popUpLabelData.Length;
					placeholderNumValues = numSockets;
				}
			}
			else
			{
				GVar _var = GetVariable ();
				if (_var != null)
				{
					numSockets = _var.GetNumPopUpValues ();
					placeholderNumValues = numSockets;
					placeholderPopUpLabelDataID = _var.popUpID;
				}
			}

			if (numSockets == 0)
			{
				EditorGUILayout.HelpBox ("The selected variable has no values!", MessageType.Warning);
			}
		}


		public override string SetLabel ()
		{
			switch (location)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager != null)
					{
						return GetLabelString (AdvGame.GetReferences ().variablesManager.vars);
					}
					break;

				case VariableLocation.Local:
					if (!isAssetFile && localVariables != null)
					{
						return GetLabelString (localVariables.localVars);
					}
					break;

				case VariableLocation.Component:
					if (variables != null)
					{
						return GetLabelString (variables.vars);
					}
					break;
			}

			return string.Empty;
		}
		
		
		private string GetLabelString (List<GVar> vars)
		{
			if (vars != null && parameterID < 0)
			{
				foreach (GVar _var in vars)
				{
					if (_var.id == variableID && _var.type == VariableType.PopUp)
					{
						return _var.label;
					}
				}
			}

			return string.Empty;
		}

		protected override string GetSocketLabel (int i)
		{
			GVar _var = (parameterID < 0) ? GetVariable () : null;
			if (_var != null)
			{
				string[] popUpLabels = _var.GenerateEditorPopUpLabels ();
				if (i < popUpLabels.Length)
				{
					return "If = '" + popUpLabels[i] + "':";
				}
			}
			else if (KickStarter.variablesManager)
			{
				PopUpLabelData popUpLabelData = AdvGame.GetReferences ().variablesManager.GetPopUpLabelData (placeholderPopUpLabelDataID);
				if (parameterID >= 0 && popUpLabelData != null)
				{
					return "If = '" + popUpLabelData.GetValue (i) + "':";
				}
			}
			return "If = '" + i.ToString () + "':";
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (location == VariableLocation.Local && variableID == oldLocalID && parameterID < 0)
			{
				location = VariableLocation.Global;
				variableID = newGlobalID;
				wasAmended = true;
			}
			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool wasAmended = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (location == VariableLocation.Global && variableID == oldGlobalID && parameterID < 0)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					location = VariableLocation.Local;
					variableID = newLocalID;
				}
			}
			return wasAmended;
		}


		public override int GetNumVariableReferences (VariableLocation _location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (location == _location && variableID == varID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables && variables == _variables) || (_variablesConstantID != 0 && variablesConstantID == _variablesConstantID))
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

			if (location == _location && variableID == oldVarID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables && variables == _variables) || (_variablesConstantID != 0 && variablesConstantID == _variablesConstantID))
				{
					variableID = newVarID;
					thisCount++;
				}
			}

			thisCount += base.UpdateVariableReferences (_location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (location == VariableLocation.Component && parameterID < 0)
			{
				if (saveScriptsToo && variables && parameterID < 0)
				{
					AddSaveScript<RememberVariables> (variables);
				}

				AssignConstantID <Variables> (variables, variablesConstantID, -1);
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0 && location == VariableLocation.Component)
			{
				if (variables && variables.gameObject == gameObject) return true;
				return (variablesConstantID == id && id != 0);
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		protected GVar GetVariable ()
		{
			GVar _var = null;

			switch (location)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager)
					{
						_var = AdvGame.GetReferences ().variablesManager.GetVariable (variableID);
					}
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						_var = LocalVariables.GetVariable (variableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
					if (runtimeVariables != null)
					{
						_var = runtimeVariables.GetVariable (variableID);
					}
					break;
			}

			if (_var != null && _var.type == VariableType.PopUp)
			{
				return _var;
			}
			return null;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Pop Up switch' Action, set to switch a Global PopUp variable</summary>
		 * <param name = "globalVariableID">The ID number of the Global PopUp variable</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarPopup CreateNew_Global (int globalVariableID)
		{
			ActionVarPopup newAction = CreateNew<ActionVarPopup> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;

			GVar variable = newAction.GetVariable ();
			if (variable != null)
			{
				newAction.numSockets = variable.GetNumPopUpValues ();
			}

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Pop Up switch' Action, set to switch a Local PopUp variable</summary>
		 * <param name = "localVariableID">The ID number of the Local PopUp variable</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarPopup CreateNew_Local (int localVariableID)
		{
			ActionVarPopup newAction = CreateNew<ActionVarPopup> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;

			GVar variable = newAction.GetVariable ();
			if (variable != null)
			{
				newAction.numSockets = variable.GetNumPopUpValues ();
			}

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Pop Up switch' Action, set to switch a Component PopUp variable</summary>
		 * <param name = "variables">The variable's associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the Component PopUp variable</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarPopup CreateNew_Component (Variables variables, int componentVariableID)
		{
			ActionVarPopup newAction = CreateNew<ActionVarPopup> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;

			GVar variable = newAction.GetVariable ();
			if (variable != null)
			{
				newAction.numSockets = variable.GetNumPopUpValues ();
			}

			return newAction;
		}


	}
	
}