/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionRandomCheck.cs"
 * 
 *	This action checks the value of a random number
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
	public class ActionRandomCheck : Action
	{

		public int numSockets = 2;
		public bool disallowSuccessive = false;
		public bool saveToVariable = true;
		protected int ownVarValue = -1;

		public int parameterID = -1;
		public int variableID;
		public VariableLocation location = VariableLocation.Global;

		public Variables variables;
		public int variablesConstantID;

		protected LocalVariables localVariables;
		protected GVar runtimeVariable = null;


		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Check random number"; }}
		public override string Description { get { return "Picks a number at random between zero and a specified integer – the value of which determine which subsequent Action is run next."; }}
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
						Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
						if (runtimeVariables != null)
						{
							runtimeVariable = runtimeVariables.GetVariable (variableID);
						}
						runtimeVariable = AssignVariable (parameters, parameterID, runtimeVariable);
						break;
				}
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
		
		
		public override int GetNextOutputIndex ()
		{
			if (numSockets <= 0)
			{
				LogWarning ("Could not compute Random check because no values were possible!");
				return -1;
			}

			int randomResult = Random.Range (0, numSockets);
			if (numSockets > 1 && disallowSuccessive)
			{
				if (saveToVariable)
				{
					if (runtimeVariable != null && runtimeVariable.type == VariableType.Integer)
					{
						ownVarValue = runtimeVariable.IntegerValue;
					}
					else
					{
						LogWarning ("No Integer variable found!");
					}
				}

				while (ownVarValue == randomResult)
				{
					randomResult = Random.Range (0, numSockets);
				}

				ownVarValue = randomResult;

				if (saveToVariable && runtimeVariable != null && runtimeVariable.type == VariableType.Integer)
				{
					runtimeVariable.IntegerValue = ownVarValue;
				}
			}

			return randomResult;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			numSockets = EditorGUILayout.DelayedIntField ("# of possible values:", numSockets);
			numSockets = Mathf.Clamp (numSockets, 1, 100);

			disallowSuccessive = EditorGUILayout.Toggle ("Prevent same value twice?", disallowSuccessive);

			if (disallowSuccessive)
			{
				saveToVariable = EditorGUILayout.Toggle ("Save last value?", saveToVariable);
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
						(location == VariableLocation.Local && KickStarter.localVariables != null && !isAssetFile) ||
						(location == VariableLocation.Component))
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
						if (parameterID >= 0)
						{
							if (location == VariableLocation.Component)
							{
								variablesConstantID = 0;
								variables = null;
							}

							variableID = ShowVarGUI (variableID, false);
						}
						else
						{
							if (location == VariableLocation.Component)
							{
								variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
								variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
								variables = IDToField <Variables> (variables, variablesConstantID, false);

								if (variables != null)
								{
									variableID = ShowVarGUI (variableID, true);
								}
							}
							else
							{
								EditorGUILayout.BeginHorizontal ();
								variableID = ShowVarGUI (variableID, true);

								if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
								{
									SideMenu ();
								}
								EditorGUILayout.EndHorizontal ();
							}
						}
					}
				}
			}
		}


		protected void SideMenu ()
		{
			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Auto-create " + location.ToString () + " variable"), false, Callback, "AutoCreate");
			menu.ShowAsContext ();
		}
		
		
		protected void Callback (object obj)
		{
			switch (obj.ToString ())
			{
				case "AutoCreate":
					AutoCreateVariableWindow.Init ("Random/New integer", location, VariableType.Integer, this);
					break;

				case "Show":
					if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager != null)
					{
						AdvGame.GetReferences ().variablesManager.ShowVariable (variableID, location);
					}
					break;
			}
		}


		protected int ShowVarGUI (int ID, bool changeID)
		{
			if (changeID)
			{
				switch (location)
				{
					case VariableLocation.Global:
						ID = AdvGame.GlobalVariableGUI ("Global integer:", ID, VariableType.Integer);
						break;

					case VariableLocation.Local:
						ID = AdvGame.LocalVariableGUI ("Local integer:", ID, VariableType.Integer);
						break;

					case VariableLocation.Component:
						ID = AdvGame.ComponentVariableGUI ("Component integer:", ID, VariableType.Integer, variables);
						break;
				}
			}

			return ID;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (saveToVariable)
			{
				if (location == VariableLocation.Local && variableID == oldLocalID)
				{
					location = VariableLocation.Global;
					variableID = newGlobalID;
					wasAmended = true;
				}
			}

			return wasAmended;
		}


		public override int GetNumVariableReferences (VariableLocation _location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			if (saveToVariable && location == _location && variableID == varID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables != null && variables == _variables) || (variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
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
				if (location != VariableLocation.Component || (variables != null && variables == _variables) || (variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
				{
					variableID = newVarID;
					thisCount++;
				}
			}
			thisCount += base.UpdateVariableReferences (_location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (saveToVariable)
			{
				if (location == VariableLocation.Global && variableID == oldGlobalID)
				{
					isAffected = true;

					if (isCorrectScene)
					{
						location = VariableLocation.Local;
						variableID = newLocalID;
					}
				}
			}

			return isAffected;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveToVariable &&
				location == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, parameterID);
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (disallowSuccessive && saveToVariable && location == VariableLocation.Component && parameterID < 0)
			{
				if (variables && variables.gameObject == gameObject) return true;
				if (variablesConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Variable: Check random number' Action</summary>
		 * <param name = "numOutcomes">The number of possible outcomes</param>
		 * <param name = "disallowSuccessive">If True, the same value cannot be used twice in a row</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionRandomCheck CreateNew (int numOutcomes, bool disallowSuccessive)
		{
			ActionRandomCheck newAction = CreateNew<ActionRandomCheck> ();
			newAction.numSockets = numOutcomes;
			newAction.disallowSuccessive = disallowSuccessive;
			return newAction;
		}
		
	}

}