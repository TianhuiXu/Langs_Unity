/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVarCheck.cs"
 * 
 *	This action checks to see if a Variable has been assigned a certain value,
 *	and performs something accordingly.
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
	public class ActionVarCheck : ActionCheck
	{

		public int parameterID = -1;
		public int variableID;
		public int variableNumber;

		public int checkParameterID = -1;

		public GetVarMethod getVarMethod = GetVarMethod.EnteredValue;
		public int compareVariableID;

		public int intValue;
		public float floatValue;
		public IntCondition intCondition;
		public bool isAdditive = false;
		
		public BoolValue boolValue = BoolValue.True;
		public BoolCondition boolCondition;

		public string stringValue;
		public bool checkCase = true;

		public Vector3 vector3Value;
		public VectorCondition vectorCondition = VectorCondition.EqualTo;

		public GameObject gameObjectValue;
		protected GameObject runtimeGameObjectValue;

		public Object unityObjectValue;
		protected Object runtimeUnityObjectValue;

		public VariableLocation location = VariableLocation.Global;
		protected LocalVariables localVariables;

		public Variables variables;
		public int variablesConstantID = 0;

		public Variables compareVariables;
		public int compareVariablesConstantID = 0;

		protected GVar runtimeVariable;
		protected GVar runtimeCompareVariable;

		#if UNITY_EDITOR
		[SerializeField] protected VariableType placeholderType;
		[SerializeField] protected int placeholderPopUpLabelDataID = 0;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Queries the value of both Global and Local Variables declared in the Variables Manager. Variables can be compared with a fixed value, or with the values of other Variables."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			intValue = AssignInteger (parameters, checkParameterID, intValue);
			boolValue = AssignBoolean (parameters, checkParameterID, boolValue);
			floatValue = AssignFloat (parameters, checkParameterID, floatValue);
			vector3Value = AssignVector3 (parameters, checkParameterID, vector3Value);
			stringValue = AssignString (parameters, checkParameterID, stringValue);
			runtimeGameObjectValue = AssignFile (parameters, checkParameterID, intValue, gameObjectValue);
			runtimeUnityObjectValue = AssignObject <Object> (parameters, checkParameterID, unityObjectValue);

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

			runtimeCompareVariable = null;
			switch (getVarMethod)
			{
				case GetVarMethod.GlobalVariable:
					compareVariableID = AssignVariableID (parameters, checkParameterID, compareVariableID);
					runtimeCompareVariable = GlobalVariables.GetVariable (compareVariableID, true);
					break;

				case GetVarMethod.LocalVariable:
					compareVariableID = AssignVariableID (parameters, checkParameterID, compareVariableID);
					runtimeCompareVariable = LocalVariables.GetVariable (compareVariableID, localVariables);
					break;

				case GetVarMethod.ComponentVariable:
					Variables runtimeCompareVariables = AssignFile <Variables> (compareVariablesConstantID, compareVariables);
					if (runtimeCompareVariables != null)
					{
						runtimeCompareVariable = runtimeCompareVariables.GetVariable (compareVariableID);
					}
					runtimeCompareVariable = AssignVariable (parameters, checkParameterID, runtimeCompareVariable);
					break;

				default:
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

		
		public override int GetNextOutputIndex ()
		{
			if (getVarMethod == GetVarMethod.GlobalVariable ||
				getVarMethod == GetVarMethod.LocalVariable ||
				getVarMethod == GetVarMethod.ComponentVariable)
			{
				if (runtimeCompareVariable == null)
				{
					LogWarning ("The 'Variable: Check' Action halted the ActionList because it cannot find the " + getVarMethod.ToString () + " to compare with.");
					return -1;
				}
			}

			if (runtimeVariable != null)
			{
				return CheckCondition (runtimeVariable, runtimeCompareVariable) ? 0 : 1;
			}

			LogWarning ("The 'Variable: Check' Action halted the ActionList because it cannot find the " + location.ToString () + " Variable with an ID of " + variableID);
			return -1;
		}
		
		
		protected bool CheckCondition (GVar _var, GVar _compareVar)
		{
			if (_var == null)
			{
				LogWarning ("Cannot check state of variable since it cannot be found!");
				return false;
			}

			if (_compareVar != null && _var != null && _compareVar.type != _var.type)
			{
				LogWarning ("Cannot compare " + _var.label + " and " + _compareVar.label + " as they are not the same type!");
				return false;
			}

			if (_var.type == VariableType.Boolean)
			{
				int fieldValue = _var.IntegerValue;
				int compareValue = (int) boolValue;
				if (_compareVar != null)
				{
					compareValue = _compareVar.IntegerValue;
				}

				switch (boolCondition)
				{
					case BoolCondition.EqualTo:
						return (fieldValue == compareValue);

					case BoolCondition.NotEqualTo:
						return (fieldValue != compareValue);

					default:
						break;
				}
			}

			else if (_var.type == VariableType.Integer || _var.type == VariableType.PopUp)
			{
				int fieldValue = _var.IntegerValue;
				int compareValue = intValue;
				if (_compareVar != null)
				{
					compareValue = _compareVar.IntegerValue;
				}

				switch (intCondition)
				{
					case IntCondition.EqualTo:
						return (fieldValue == compareValue);

					case IntCondition.NotEqualTo:
						return (fieldValue != compareValue);

					case IntCondition.LessThan:
						return (fieldValue < compareValue);

					case IntCondition.MoreThan:
						return (fieldValue > compareValue);

					default:
						break;
				}
			}

			else if (_var.type == VariableType.Float)
			{
				float fieldValue = _var.FloatValue;
				float compareValue = floatValue;
				if (_compareVar != null)
				{
					compareValue = _compareVar.FloatValue;
				}

				switch (intCondition)
				{
					case IntCondition.EqualTo:
						return Mathf.Approximately (fieldValue, compareValue);

					case IntCondition.NotEqualTo:
						return !Mathf.Approximately (fieldValue, compareValue);

					case IntCondition.LessThan:
						return (fieldValue < compareValue);

					case IntCondition.MoreThan:
						return (fieldValue > compareValue);

					default:
						break;
				}
			}

			else if (_var.type == VariableType.String)
			{
				string fieldValue = _var.TextValue;
				string compareValue = AdvGame.ConvertTokens (stringValue);
				if (_compareVar != null)
				{
					compareValue = _compareVar.TextValue;
				}

				if (!checkCase)
				{
					fieldValue = fieldValue.ToLower ();
					compareValue = compareValue.ToLower ();
				}

				switch (boolCondition)
				{
					case BoolCondition.EqualTo:
						return (fieldValue == compareValue);

					case BoolCondition.NotEqualTo:
						return (fieldValue != compareValue);

					default:
						break;
				}
			}

			else if (_var.type == VariableType.GameObject)
			{
				GameObject fieldValue = _var.GameObjectValue;
				if (_compareVar != null)
				{
					runtimeGameObjectValue = _compareVar.GameObjectValue;
				}

				ConstantID fieldConstantID = fieldValue.GetComponent <ConstantID>();
				ConstantID runtimeConstantID = runtimeGameObjectValue.GetComponent <ConstantID>();

				switch (boolCondition)
				{
					case BoolCondition.EqualTo:
						if (runtimeGameObjectValue == fieldValue)
						{
							return true;
						}
						else if (fieldConstantID && runtimeConstantID && fieldConstantID.constantID != 0 && fieldConstantID.constantID == runtimeConstantID.constantID)
						{
							return true;
						}
						return false;

					case BoolCondition.NotEqualTo:
						if (runtimeGameObjectValue != fieldValue)
						{
							return true;
						}
						else if (fieldConstantID && runtimeConstantID && fieldConstantID.constantID != 0 && fieldConstantID.constantID != runtimeConstantID.constantID)
						{
							return true;
						}
						return false;

					default:
						break;
				}
			}

			else if (_var.type == VariableType.UnityObject)
			{
				Object fieldValue = _var.UnityObjectValue;
				if (_compareVar != null)
				{
					runtimeUnityObjectValue = _compareVar.UnityObjectValue;
				}

				switch (boolCondition)
				{
					case BoolCondition.EqualTo:
						return (runtimeUnityObjectValue == fieldValue);

					case BoolCondition.NotEqualTo:
						return (runtimeUnityObjectValue != fieldValue);

					default:
						break;
				}
			}

			else if (_var.type == VariableType.Vector3)
			{
				if (_compareVar != null)
				{
					switch (vectorCondition)
					{
						case VectorCondition.EqualTo:
							return (_var.Vector3Value == _compareVar.Vector3Value);

						case VectorCondition.MagnitudeGreaterThan:
							return (_var.Vector3Value.sqrMagnitude > _compareVar.Vector3Value.sqrMagnitude);

						default:
							break;
					}
				}

				switch (vectorCondition)
				{
					case VectorCondition.EqualTo:
						return (_var.Vector3Value == vector3Value);

					case VectorCondition.MagnitudeGreaterThan:
						return (_var.Vector3Value.magnitude > floatValue);

					default:
						break;
				}
			}
			
			return false;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			location = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", location);

			if (isAssetFile && getVarMethod == GetVarMethod.LocalVariable)
			{
				EditorGUILayout.HelpBox ("Local Variables cannot be referenced by Asset-based Actions.", MessageType.Warning);
				return;
			}

			switch (location)
			{
				case VariableLocation.Global:
					if (AdvGame.GetReferences ().variablesManager)
					{
						VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;

						parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.GlobalVariable);
						if (parameterID >= 0)
						{
							placeholderType = (VariableType) EditorGUILayout.EnumPopup ("Placeholder type:", placeholderType);
							variableID = ShowVarGUI (parameters, variablesManager.vars, variableID, false);
						}
						else
						{
							variableID = ShowVarGUI (parameters, variablesManager.vars, variableID, true);
						}
					}
					break;

				case VariableLocation.Local:
					if (localVariables != null)
					{
						parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.LocalVariable);
						if (parameterID >= 0)
						{
							placeholderType = (VariableType) EditorGUILayout.EnumPopup ("Placeholder type:", placeholderType);
							variableID = ShowVarGUI (parameters, localVariables.localVars, variableID, false);
						}
						else
						{
							variableID = ShowVarGUI (parameters, localVariables.localVars, variableID, true);
						}
					}
					else
					{
						EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.ComponentVariable);
					if (parameterID >= 0)
					{
						placeholderType = (VariableType) EditorGUILayout.EnumPopup ("Placeholder type:", placeholderType);
						variableID = ShowVarGUI (parameters, (variables != null) ? variables.vars : null, variableID, false);
					}
					else
					{
						variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
						variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
						variables = IDToField <Variables> (variables, variablesConstantID, false);
						
						if (variables != null)
						{
							variableID = ShowVarGUI (parameters, variables.vars, variableID, true);
						}
					}
					break;
			}
		}


		protected int ShowVarSelectorGUI (List<GVar> vars, int ID, string label)
		{
			variableNumber = -1;
			
			List<string> labelList = new List<string>();
			foreach (GVar _var in vars)
			{
				labelList.Add (_var.label);
			}

			variableNumber = GetVarNumber (vars, ID);

			if (variableNumber == -1)
			{
				// Wasn't found (variable was deleted?), so revert to zero
				if (ID > 0) LogWarning ("Previously chosen variable no longer exists!");
				variableNumber = 0;
				ID = 0;
			}

			variableNumber = EditorGUILayout.Popup (label, variableNumber, labelList.ToArray());
			ID = vars[variableNumber].id;

			return ID;
		}


		private int ShowVarGUI (List<ActionParameter> parameters, List<GVar> _vars, int ID, bool changeID)
		{
			VariableType showType = VariableType.Boolean;

			if (changeID)
			{
				if (_vars != null && _vars.Count > 0)
				{
					ID = ShowVarSelectorGUI (_vars, ID, "Variable:");

					variableNumber = Mathf.Min (variableNumber, _vars.Count-1);
					getVarMethod = (GetVarMethod) EditorGUILayout.EnumPopup ("Compare with:", getVarMethod);

					showType = _vars[variableNumber].type;
				}
				else
				{
					EditorGUILayout.HelpBox ("No variables exist!", MessageType.Info);
					ID = -1;
					variableNumber = -1;
					return ID;
				}

				placeholderType = showType;
			}
			else
			{
				showType = placeholderType;
			}

			switch (showType)
			{
				case VariableType.Boolean:
					boolCondition = (BoolCondition) EditorGUILayout.EnumPopup ("Condition:", boolCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("Boolean:", parameters, checkParameterID, ParameterType.Boolean);
						if (checkParameterID < 0)
						{
							boolValue = (BoolValue) EditorGUILayout.EnumPopup ("Boolean:", boolValue);
						}
					}
					break;

				case VariableType.Integer:
					intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Condition:", intCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("Integer:", parameters, checkParameterID, ParameterType.Integer);
						if (checkParameterID < 0)
						{
							intValue = EditorGUILayout.IntField ("Integer:", intValue);
						}
					}
					break;

				case VariableType.Float:
					intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Condition:", intCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("Float:", parameters, checkParameterID, ParameterType.Float);
						if (checkParameterID < 0)
						{
							floatValue = EditorGUILayout.FloatField ("Float:", floatValue);
						}
					}
					break;

				case VariableType.PopUp:
					intCondition = (IntCondition) EditorGUILayout.EnumPopup ("Condition:", intCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("Value:", parameters, checkParameterID, new ParameterType[] { ParameterType.Integer, ParameterType.PopUp });
						if (checkParameterID < 0)
						{
							if (changeID && _vars != null && _vars.Count > variableNumber)
							{
								string[] popUpLabels = _vars[variableNumber].GenerateEditorPopUpLabels ();
								intValue = EditorGUILayout.Popup ("Value:", intValue, popUpLabels);
								placeholderPopUpLabelDataID = _vars[variableNumber].popUpID;
							}
							else if (!changeID && AdvGame.GetReferences ().variablesManager != null)
							{
								// Parameter override
								placeholderPopUpLabelDataID = AdvGame.GetReferences ().variablesManager.ShowPlaceholderPresetData (placeholderPopUpLabelDataID);
								PopUpLabelData popUpLabelData = AdvGame.GetReferences ().variablesManager.GetPopUpLabelData (placeholderPopUpLabelDataID);

								if (popUpLabelData != null && placeholderPopUpLabelDataID > 0)
								{
									// Show placeholder labels
									intValue = EditorGUILayout.Popup ("Index value:", intValue, popUpLabelData.GenerateEditorPopUpLabels ());
								}
								else
								{
									intValue = EditorGUILayout.IntField ("Index value:", intValue);
								}
							}
							else
							{
								intValue = EditorGUILayout.IntField ("Index value:", intValue);
							}
						}
					}
					break;

				case VariableType.String:
					boolCondition = (BoolCondition) EditorGUILayout.EnumPopup ("Condition:", boolCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("String:", parameters, checkParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (checkParameterID < 0)
						{
							stringValue = EditorGUILayout.TextField ("String:", stringValue);
						}
					}
					checkCase = EditorGUILayout.Toggle ("Case-sensitive?", checkCase);
					break;

				case VariableType.GameObject:
					boolCondition = (BoolCondition) EditorGUILayout.EnumPopup ("Condition:", boolCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("GameObject:", parameters, checkParameterID, ParameterType.GameObject);
						if (checkParameterID < 0)
						{
							gameObjectValue = (GameObject) EditorGUILayout.ObjectField ("GameObject:", gameObjectValue, typeof (GameObject), true);
							intValue = FieldToID (gameObjectValue, intValue);
							gameObjectValue = IDToField (gameObjectValue, intValue, false);
						}
						EditorGUILayout.HelpBox ("A match will be found if the two GameObjects share the same Constant ID value", MessageType.Info);
					}
					break;

				case VariableType.UnityObject:
					boolCondition = (BoolCondition) EditorGUILayout.EnumPopup ("Condition:", boolCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						checkParameterID = Action.ChooseParameterGUI ("Object:", parameters, checkParameterID, ParameterType.UnityObject);
						if (checkParameterID < 0)
						{
							unityObjectValue = EditorGUILayout.ObjectField ("Object:", unityObjectValue, typeof (Object), false);
						}
						EditorGUILayout.HelpBox ("A match will be found if the two GameObjects share the same Constant ID value", MessageType.Info);
					}
					break;

				case VariableType.Vector3:
					vectorCondition = (VectorCondition) EditorGUILayout.EnumPopup ("Condition:", vectorCondition);
					if (getVarMethod == GetVarMethod.EnteredValue)
					{
						if (vectorCondition == VectorCondition.MagnitudeGreaterThan)
						{
							checkParameterID = Action.ChooseParameterGUI ("Float:", parameters, checkParameterID, ParameterType.Float);
							if (checkParameterID < 0)
							{
								floatValue = EditorGUILayout.FloatField ("Float:", floatValue);
							}
						}
						else if (vectorCondition == VectorCondition.EqualTo)
						{
							checkParameterID = Action.ChooseParameterGUI ("Vector3:", parameters, checkParameterID, ParameterType.Vector3);
							if (checkParameterID < 0)
							{
								EditorGUILayout.BeginHorizontal ();
								EditorGUILayout.LabelField ("Vector3:", GUILayout.MaxWidth (60f));
								vector3Value = EditorGUILayout.Vector3Field ("", vector3Value);
								EditorGUILayout.EndHorizontal ();
							}
						}
					}
					break;

				default:
					break;
			}

			if (getVarMethod == GetVarMethod.GlobalVariable)
			{
				if (AdvGame.GetReferences ().variablesManager == null || AdvGame.GetReferences ().variablesManager.vars == null || AdvGame.GetReferences ().variablesManager.vars.Count == 0)
				{
					EditorGUILayout.HelpBox ("No Global variables exist!", MessageType.Info);
				}
				else
				{
					checkParameterID = Action.ChooseParameterGUI ("Global variable:", parameters, checkParameterID, ParameterType.GlobalVariable);
					if (checkParameterID < 0)
					{
						compareVariableID = ShowVarSelectorGUI (AdvGame.GetReferences ().variablesManager.vars, compareVariableID, "Global variable:");
					}
				}
			}
			else if (getVarMethod == GetVarMethod.LocalVariable)
			{
				if (localVariables == null || localVariables.localVars == null || localVariables.localVars.Count == 0)
				{
					EditorGUILayout.HelpBox ("No Local variables exist!", MessageType.Info);
				}
				else
				{
					checkParameterID = Action.ChooseParameterGUI ("Local variable:", parameters, checkParameterID, ParameterType.LocalVariable);
					if (checkParameterID < 0)
					{
						compareVariableID = ShowVarSelectorGUI (localVariables.localVars, compareVariableID, "Local variable:");
					}
				}
			}
			else if (getVarMethod == GetVarMethod.ComponentVariable)
			{
				checkParameterID = Action.ChooseParameterGUI ("Component variable:", parameters, checkParameterID, ParameterType.ComponentVariable);
				if (checkParameterID < 0)
				{
					compareVariables = (Variables) EditorGUILayout.ObjectField ("Component", compareVariables, typeof (Variables), true);
					compareVariablesConstantID = FieldToID <Variables> (compareVariables, compareVariablesConstantID);
					compareVariables = IDToField <Variables> (compareVariables, compareVariablesConstantID, false);
					
					if (compareVariables != null)
					{
						compareVariableID = ShowVarSelectorGUI (compareVariables.vars, compareVariableID, "Component variable:");
					}
				}
			}

			return ID;
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
			string labelAdd = string.Empty;

			if (parameterID < 0 && vars.Count > 0 && vars.Count > variableNumber && variableNumber > -1)
			{
				labelAdd = vars[variableNumber].label;

				switch (vars[variableNumber].type)
				{
					case VariableType.Boolean:
						labelAdd += " " + boolCondition.ToString () + " " + boolValue.ToString ();
						break;

					case VariableType.Integer:
						labelAdd += " " + intCondition.ToString () + " " + intValue.ToString ();
						break;

					case VariableType.Float:
						labelAdd += " " + intCondition.ToString () + " " + floatValue.ToString ();
						break;

					case VariableType.String:
						labelAdd += " " + boolCondition.ToString () + " " + stringValue;
						break;

					case VariableType.PopUp:
						labelAdd += " " + intCondition.ToString () + " " + vars[variableNumber].GetPopUpForIndex (intValue);
						break;

					case VariableType.GameObject:
						if (gameObjectValue)
						{
							labelAdd += " " + boolCondition.ToString () + " " + gameObjectValue.name;
						}
						break;

					case VariableType.UnityObject:
						if (unityObjectValue)
						{
							labelAdd += " " + unityObjectValue.name;
						}
						break;

					default:
						break;
				}
			}

			return labelAdd;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			if (location == VariableLocation.Local && variableID == oldLocalID)
			{
				location = VariableLocation.Global;
				variableID = newGlobalID;
				wasAmended = true;
			}

			if (getVarMethod == GetVarMethod.LocalVariable && compareVariableID == oldLocalID)
			{
				getVarMethod = GetVarMethod.GlobalVariable;
				compareVariableID = newGlobalID;
				wasAmended = true;
			}

			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool wasAmended = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			if (location == VariableLocation.Global && variableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					location = VariableLocation.Local;
					variableID = newLocalID;
				}
			}

			if (getVarMethod == GetVarMethod.GlobalVariable && compareVariableID == oldGlobalID)
			{
				wasAmended = true;
				if (isCorrectScene)
				{
					getVarMethod = GetVarMethod.LocalVariable;
					compareVariableID = newLocalID;
				}
			}

			return wasAmended;
		}


		public override int GetNumVariableReferences (VariableLocation _location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			if (location == _location && variableID == varID && parameterID < 0)
			{
				if (_location != VariableLocation.Component || (variables != null && variables == _variables) || (_variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
				{
					thisCount ++;
				}
			}

			if (getVarMethod == GetVarMethod.LocalVariable && _location == VariableLocation.Local && compareVariableID == varID)
			{
				thisCount ++;
			}
			else if (getVarMethod == GetVarMethod.GlobalVariable && _location == VariableLocation.Global && compareVariableID == varID)
			{
				thisCount ++;
			}
			else if (getVarMethod == GetVarMethod.ComponentVariable && _location == VariableLocation.Component && compareVariableID == varID)
			{
				if ((compareVariables && compareVariables == _variables) ||
					(compareVariablesConstantID != 0 && variablesConstantID == compareVariablesConstantID))
				{
					thisCount++;
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
				if (_location != VariableLocation.Component || (variables != null && variables == _variables) || (_variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
				{
					variableID = newVarID;
					thisCount++;
				}
			}

			if (getVarMethod == GetVarMethod.LocalVariable && _location == VariableLocation.Local && compareVariableID == oldVarID)
			{
				compareVariableID = newVarID;
				thisCount++;
			}
			else if (getVarMethod == GetVarMethod.GlobalVariable && _location == VariableLocation.Global && compareVariableID == oldVarID)
			{
				compareVariableID = newVarID;
				thisCount++;
			}
			else if (getVarMethod == GetVarMethod.ComponentVariable && _location == VariableLocation.Component && compareVariableID == oldVarID)
			{
				if ((compareVariables && compareVariables == _variables) ||
					(compareVariablesConstantID != 0 && variablesConstantID == compareVariablesConstantID))
				{
					compareVariableID = newVarID;
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
			if (gameObjectValue && gameObjectValue == gameObject)
			{
				return true;
			}

			if (parameterID < 0 && location == VariableLocation.Component)
			{
				if (variables && variables.gameObject && variables.gameObject == gameObject) return true;
				if (variablesConstantID == id && id != 0) return true;
			}
			if (checkParameterID < 0 && getVarMethod == GetVarMethod.ComponentVariable)
			{
				if (compareVariables && compareVariables.gameObject && compareVariables.gameObject == gameObject) return true;
				if (compareVariablesConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		protected int GetVarNumber (List<GVar> vars, int ID)
		{
			int i = 0;
			foreach (GVar _var in vars)
			{
				if (_var.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Global integer variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Global (int globalVariableID, int checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.intValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Global float variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Global (int globalVariableID, float checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.floatValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Global boolean variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Global (int globalVariableID, bool checkValue = true)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.intValue = (checkValue) ? 1 : 0;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Global Vector3 variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Global (int globalVariableID, Vector3 checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.vector3Value = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Global string variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Global (int globalVariableID, string checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.stringValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Local integer variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Local (int localVariableID, int checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.intValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Local float variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Local (int localVariableID, float checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.floatValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Local boolean variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Local (int localVariableID, bool checkValue = true)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.intValue = (checkValue) ? 1 : 0;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Local Vector3 variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Local (int localVariableID, Vector3 checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.vector3Value = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Local string variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Local (int localVariableID, string checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.stringValue = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Component integer variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Component (Variables variables, int componentVariableID, int checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.intValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Component float variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Component (Variables variables, int componentVariableID, float checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.floatValue = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Component boolean variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Component (Variables variables, int componentVariableID, bool checkValue = true)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.intValue = (checkValue) ? 1 : 0;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Component Vector3 variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Component (Variables variables, int componentVariableID, Vector3 checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.vector3Value = checkValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to check a Component string variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarCheck CreateNew_Component (Variables variables, int componentVariableID, string checkValue)
		{
			ActionVarCheck newAction = CreateNew<ActionVarCheck> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.stringValue = checkValue;
			return newAction;
		}

	}

}