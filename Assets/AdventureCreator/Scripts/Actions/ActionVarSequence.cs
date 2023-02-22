/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVarSequence.cs"
 * 
 *	This action runs an Integer Variable through a sequence
 *	and performs different follow-up Actions accordingly.
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
	public class ActionVarSequence : Action
	{
		
		public int parameterID = -1;
		public int variableID;
		public bool doLoop = false;

		public int numSockets = 2;

		public bool saveToVariable = false;
		protected int ownVarValue = 0;

		public VariableLocation location = VariableLocation.Global;
		protected LocalVariables localVariables;

		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;
		protected Variables runtimeVariables;

		
		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Run sequence"; }}
		public override string Description { get { return "Uses the value of an integer Variable to determine which Action is run next. The value is incremented by one each time (and reset to zero when a limit is reached), allowing for different subsequent Actions to play each time the Action is run."; }}
		public override int NumSockets { get { return numSockets; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeVariable = null;
			if (saveToVariable)
			{
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
						runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
						if (runtimeVariables != null)
						{
							runtimeVariable = runtimeVariables.GetVariable (variableID);
						}
						runtimeVariable = AssignVariable (parameters, parameterID, runtimeVariable);
						runtimeVariables = AssignVariablesComponent (parameters, parameterID, runtimeVariables);
						break;
				}
			}
		}


		public override void AssignParentList (ActionList actionList)
		{
			if (saveToVariable)
			{
				if (actionList != null)
				{
					localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
				}
				if (localVariables == null)
				{
					localVariables = KickStarter.localVariables;
				}
			}

			base.AssignParentList (actionList);
		}
		

		public override void ResetAssetValues ()
		{
			ownVarValue = 0;
		}
		

		public override int GetNextOutputIndex ()
		{
			if (numSockets <= 0)
			{
				LogWarning ("Could not compute Random check because no values were possible!");
				return -1;
			}

			if (!saveToVariable)
			{
				int value = ownVarValue;
				ownVarValue ++;
				if (ownVarValue >= numSockets)
				{
					if (doLoop)
					{
						ownVarValue = 0;
					}
					else
					{
						ownVarValue = numSockets - 1;
					}
				}

				return value;
			}
			
			if (variableID == -1)
			{
				return -1;
			}
			
			if (runtimeVariable != null)
			{
				if (runtimeVariable.type == VariableType.Integer)
				{
					int newValue = runtimeVariable.IntegerValue;
					if (newValue < 1) newValue = 1;

					int originalValue = newValue - 1;
					newValue ++;

					if (newValue > numSockets)
					{
						newValue = (doLoop) ? 1 : numSockets;
					}

					runtimeVariable.IntegerValue = newValue;
					runtimeVariable.Upload (location, runtimeVariables);
					return originalValue;
				}
				else
				{
					LogWarning ("'Variable: Run sequence' Action is referencing a Variable that does not exist or is not an Integer!");
				}
			}
			
			return -1;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			numSockets = EditorGUILayout.DelayedIntField ("# of possible values:", numSockets);
			numSockets = Mathf.Clamp (numSockets, 1, 20);
		
			doLoop = EditorGUILayout.Toggle ("Run on a loop?", doLoop);

			saveToVariable = EditorGUILayout.Toggle ("Save sequence value?", saveToVariable);
			if (saveToVariable)
			{
				location = (VariableLocation) EditorGUILayout.EnumPopup ("Variable source:", location);

				if (location == VariableLocation.Local && KickStarter.localVariables == null)
				{
					EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
				}
				else if (location == VariableLocation.Local && isAssetFile)
				{
					EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
				}

				if ((location == VariableLocation.Global && AdvGame.GetReferences ().variablesManager != null) ||
					(location == VariableLocation.Local && KickStarter.localVariables != null && !isAssetFile))
				{
					ParameterType _parameterType = ParameterType.GlobalVariable;
					if (location == VariableLocation.Local)
					{
						_parameterType = ParameterType.LocalVariable;
					}
					else if (location == VariableLocation.Component)
					{
						_parameterType = ParameterType.ComponentVariable;
					}

					parameterID = Action.ChooseParameterGUI ("Integer variable:", parameters, parameterID, _parameterType);
					if (parameterID < 0)
					{
						EditorGUILayout.BeginHorizontal ();
						variableID = ShowVarGUI (variableID);
						if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
						{
							SideMenu ();
						}
						EditorGUILayout.EndHorizontal ();
					}
				}
				else if (location == VariableLocation.Component)
				{
					parameterID = Action.ChooseParameterGUI ("Integer variable:", parameters, parameterID, ParameterType.ComponentVariable);

					if (parameterID > 0)
					{
						variables = null;
						variablesConstantID = 0;
					}
					else
					{
						variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
						variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
						variables = IDToField <Variables> (variables, variablesConstantID, false);
						
						if (variables != null)
						{
							variableID = ShowVarGUI (variableID);
						}
					}
				}
			}
		}


		private void SideMenu ()
		{
			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Auto-create " + location.ToString () + " variable"), false, Callback, "AutoCreate");
			menu.ShowAsContext ();
		}
		
		
		private void Callback (object obj)
		{
			switch (obj.ToString ())
			{
				case "AutoCreate":
					AutoCreateVariableWindow.Init ("Sequence/New integer", location, VariableType.Integer, this);
					break;

				case "Show":
					if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager != null)
					{
						AdvGame.GetReferences ().variablesManager.ShowVariable (variableID, location);
					}
					break;
			}
		}


		private int ShowVarGUI (int ID)
		{
			switch (location)
			{
				case VariableLocation.Global:
					return AdvGame.GlobalVariableGUI ("Global integer:", ID, VariableType.Integer);

				case VariableLocation.Local:
					return AdvGame.LocalVariableGUI ("Local integer:", ID, VariableType.Integer);

				case VariableLocation.Component:
					return AdvGame.ComponentVariableGUI ("Component integer:", ID, VariableType.Integer, variables);
			}

			return ID;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (saveToVariable && location == VariableLocation.Local && variableID == oldLocalID)
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

			if (saveToVariable && location == VariableLocation.Global && variableID == oldGlobalID)
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

			if (saveToVariable && location == _location && variableID == varID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables && variables == _variables) || (variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
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

			if (saveToVariable && location == _location && variableID == oldVarID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables && variables == _variables) || (variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
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
			if (location == VariableLocation.Component)
			{
				if (saveScriptsToo && variables && parameterID < 0)
				{
					AddSaveScript<RememberVariables> (variables);
				}

				AssignConstantID <Variables> (variables, variablesConstantID, parameterID);
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


		/**
		 * <summary>Creates a new instance of the 'Variable: Run sequence' Action</summary>
		 * <param name = "numOutcomes">The number of different outcomes</param>
		 * <param name = "doLoop">If True, the first outcome will be run the next time after the last outcome is run</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSequence CreateNew (int numOutcomes, bool doLoop)
		{
			ActionVarSequence newAction = CreateNew<ActionVarSequence> ();
			newAction.numSockets = numOutcomes;
			newAction.doLoop = doLoop;
			return newAction;
		}
		
	}
	
}