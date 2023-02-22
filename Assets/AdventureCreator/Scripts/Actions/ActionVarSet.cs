/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVarSet.cs"
 * 
 *	This action is used to set the value of Global and Local Variables
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
	public class ActionVarSet : Action, ITranslatable
	{
		
		public SetVarMethod setVarMethod;
		public SetVarMethodString setVarMethodString = SetVarMethodString.EnteredHere;
		public SetVarMethodIntBool setVarMethodIntBool = SetVarMethodIntBool.EnteredHere;

		public SetVarMethodVector setVarMethodVector = SetVarMethodVector.SetValue;
		public enum SetVarMethodVector { SetValue, IncreaseByValue };

		public int parameterID = -1;
		public int variableID;
		public int variableNumber;

		public int setParameterID = -1;
		public int slotNumber = 0;
		public int slotNumberParameterID = -1;
		public bool slotAccountsForOffset = false;
		
		public int intValue;
		public float floatValue;
		public BoolValue boolValue;
		public string stringValue;
		public string formula;
		public Vector3 vector3Value;
		public GameObject gameObjectValue;
		public Object unityObjectValue;

		private string runtimeFormula;

		public int lineID = -1;

		public VariableLocation location;

		public string menuName;
		public string elementName;

		public Animator animator;
		public string parameterName;

		protected LocalVariables localVariables;

		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;
		protected Variables runtimeVariables;
		protected string runtimeStringValue;
		protected GameObject runtimeGameObjectValue;
		protected Object runtimeUnityObjectValue;

		#if UNITY_EDITOR
		[SerializeField] protected VariableType placeholderType;
		[SerializeField] protected int placeholderPopUpLabelDataID = -1;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Set"; }}
		public override string Description { get { return "Sets the value of both Global and Local Variables, as declared in the Variables Manager. Integers can be set to absolute, incremented or assigned a random value. Strings can also be set to the value of a MenuInput element, while Integers, Booleans and Floats can also be set to the value of a Mecanim parameter. When setting Integers and Floats, you can also opt to type in a forumla (e.g. 2 + 3 *4), which can also include tokens of the form [var:ID] to denote the value of a Variable, where ID is the unique number given to a Variable in the Variables Manager."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			intValue = AssignInteger (parameters, setParameterID, intValue);
			boolValue = AssignBoolean (parameters, setParameterID, boolValue);
			floatValue = AssignFloat (parameters, setParameterID, floatValue);
			vector3Value = AssignVector3 (parameters, setParameterID, vector3Value);
			runtimeStringValue = AssignString (parameters, setParameterID, stringValue);
			runtimeStringValue = AdvGame.ConvertTokens (runtimeStringValue, Options.GetLanguage (), null, parameters);
			formula = AssignString (parameters, setParameterID, formula);
			slotNumber = AssignInteger (parameters, slotNumberParameterID, slotNumber);
			runtimeGameObjectValue = AssignFile (parameters, setParameterID, 0, gameObjectValue);
			runtimeUnityObjectValue = AssignObject <Object> (parameters, setParameterID, unityObjectValue);

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
					runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
					if (runtimeVariables != null)
					{
						runtimeVariable = runtimeVariables.GetVariable (variableID);
					}
					runtimeVariable = AssignVariable (parameters, parameterID, runtimeVariable);
					runtimeVariables = AssignVariablesComponent (parameters, parameterID, runtimeVariables);
					break;
			}

			runtimeFormula = AdvGame.ConvertTokens (formula, Options.GetLanguage (), localVariables, parameters);
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
			if (runtimeVariable != null)
			{
				SetVariable (runtimeVariable, location, false);
			}

			return 0f;
		}


		public override void Skip ()
		{
			if (runtimeVariable != null)
			{
				SetVariable (runtimeVariable, location, true);
			}
		}

		
		protected void SetVariable (GVar var, VariableLocation location, bool doSkip)
		{
			if (var == null)
			{
				return;
			}

			switch (var.type)
			{
				case VariableType.Integer:
					{
						int _value = 0;

						if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
						{
							if (setVarMethod == SetVarMethod.Formula)
							{
								_value = (int) AdvGame.CalculateFormula (runtimeFormula);
							}
							else
							{
								_value = intValue;
							}
						}
						else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
						{
							if (animator && !string.IsNullOrEmpty (parameterName))
							{
								_value = animator.GetInteger (parameterName);
								setVarMethod = SetVarMethod.SetValue;
							}	
						}

						if (setVarMethod == SetVarMethod.IncreaseByValue && doSkip)
						{
							var.RestoreBackupValue ();
						}

						if (setVarMethod == SetVarMethod.IncreaseByValue)
						{
							var.IntegerValue += _value;
						}
						else if (setVarMethod == SetVarMethod.SetAsRandom)
						{
							var.IntegerValue = Random.Range (0, _value);
						}
						else
						{
							var.IntegerValue = _value;
						}

						if (doSkip)
						{
							var.BackupValue ();
						}
						break;
					}

				case VariableType.Float:
					{
						float _value = 0;
					
						if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
						{
							if (setVarMethod == SetVarMethod.Formula)
							{
								_value = (float) AdvGame.CalculateFormula (AdvGame.ConvertTokens (formula, Options.GetLanguage (), localVariables));
							}
							else
							{
								_value = floatValue;
							}
						}
						else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
						{
							if (animator && !string.IsNullOrEmpty (parameterName))
							{
								_value = animator.GetFloat (parameterName);
								setVarMethod = SetVarMethod.SetValue;
							}	
						}

						if (setVarMethod == SetVarMethod.IncreaseByValue && doSkip)
						{
							var.RestoreBackupValue ();
						}

						if (setVarMethod == SetVarMethod.IncreaseByValue)
						{
							var.FloatValue += _value;
						}
						else if (setVarMethod == SetVarMethod.SetAsRandom)
						{
							var.FloatValue = Random.Range (0f, _value);
						}
						else
						{
							var.FloatValue = _value;
						}
					
						if (doSkip)
						{
							var.BackupValue ();
						}

						break;
					}

				case VariableType.Boolean:
					{
						int _value = 0;

						if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
						{
							_value = (int) boolValue;
						}
						else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
						{
							if (animator && !string.IsNullOrEmpty (parameterName))
							{
								if (animator.GetBool (parameterName))
								{
									_value = 1;
								}
							}
						}

						var.BooleanValue = (_value > 0);
						break;
					}

				case VariableType.Vector3:
					{
						Vector3 newValue = vector3Value;
						if (setVarMethodVector == SetVarMethodVector.IncreaseByValue)
						{
							newValue += var.Vector3Value;
						}

						var.Vector3Value = newValue;
						break;
					}

				case VariableType.GameObject:
					{
						var.GameObjectValue = runtimeGameObjectValue;
						break;
					}

				case VariableType.UnityObject:
					{
						var.UnityObjectValue = runtimeUnityObjectValue;
						break;
					}

				case VariableType.PopUp:
					{
						int _value = 0;
					
						if (setVarMethod == SetVarMethod.Formula)
						{
							_value = (int) AdvGame.CalculateFormula (AdvGame.ConvertTokens (formula, Options.GetLanguage (), localVariables));
						}
						else if (setVarMethod == SetVarMethod.SetAsRandom)
						{
							_value = Random.Range (0, var.GetNumPopUpValues ());
						}
						else
						{
							_value = intValue;
						}

						if (setVarMethod == SetVarMethod.IncreaseByValue && doSkip)
						{
							var.RestoreBackupValue ();
						}

						if (setVarMethod == SetVarMethod.IncreaseByValue)
						{
							var.IntegerValue += _value;
						}
						else if (setVarMethod == SetVarMethod.SetAsRandom)
						{
							var.IntegerValue = Random.Range (0, _value);
						}
						else
						{
							var.IntegerValue = _value;
						}
										
						if (doSkip)
						{
							var.BackupValue ();
						}
						break;
					}

				case VariableType.String:
					{
						string _value = string.Empty;

						if (setVarMethodString == SetVarMethodString.EnteredHere)
						{
							_value = AdvGame.ConvertTokens (runtimeStringValue, Options.GetLanguage (), localVariables);
						}
						else if (setVarMethodString == SetVarMethodString.SetAsMenuElementText)
						{
							MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
							if (menuElement != null)
							{
								if (menuElement is MenuInput)
								{
									MenuInput menuInput = (MenuInput) menuElement;
									_value = menuInput.GetContents ();

									if (KickStarter.runtimeLanguages.LanguageReadsRightToLeft (Options.GetLanguage ()) && _value.Length > 0)
									{
										// Invert
										char[] charArray = _value.ToCharArray ();
										_value = string.Empty;
										for (int i = charArray.Length-1; i >= 0; i --)
										{
											_value += charArray[i];
										}
									}
								}
								else
								{
									PlayerMenus.GetMenuWithName (menuName).Recalculate ();

									int _slot = slotAccountsForOffset ? (slotNumber - menuElement.GetOffset ()) : slotNumber;

									menuElement.PreDisplay (_slot, Options.GetLanguage (), false);
									_value = menuElement.GetLabel (_slot, Options.GetLanguage ());
								}
							}
							else
							{
								LogWarning ("Could not find MenuInput '" + elementName + "' in Menu '" + menuName + "'");
							}
						}
						else if (setVarMethodString == SetVarMethodString.CombinedWithOtherString)
						{
							_value = var.TextValue + AdvGame.ConvertTokens (runtimeStringValue, Options.GetLanguage (), localVariables);
						}

						var.SetStringValue (_value, lineID);
						break;
					}

				default:
					break;
			}

			var.Upload (location, runtimeVariables);

			KickStarter.actionListManager.VariableChanged ();
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			location = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", location);
			
			if (location == VariableLocation.Global)
			{
				if (AdvGame.GetReferences ().variablesManager != null)
				{
					parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.GlobalVariable);

					if (parameterID >= 0)
					{
						placeholderType = (VariableType) EditorGUILayout.EnumPopup ("Placeholder type:", placeholderType);
						ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, false);
					}
					else
					{
						ShowVarGUI (AdvGame.GetReferences ().variablesManager.vars, parameters, ParameterType.GlobalVariable, true);
					}
				}
			}
			else if (location == VariableLocation.Local)
			{
				if (isAssetFile)
				{
					EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
				}
				else if (localVariables != null)
				{
					parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.LocalVariable);

					if (parameterID >= 0)
					{
						placeholderType = (VariableType) EditorGUILayout.EnumPopup ("Placeholder type:", placeholderType);
						ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, false);
					}
					else
					{
						ShowVarGUI (localVariables.localVars, parameters, ParameterType.LocalVariable, true);
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
				}
			}
			else if (location == VariableLocation.Component)
			{
				parameterID = Action.ChooseParameterGUI ("Variable:", parameters, parameterID, ParameterType.ComponentVariable);
				if (parameterID >= 0)
				{
					placeholderType = (VariableType) EditorGUILayout.EnumPopup ("Placeholder type:", placeholderType);
					ShowVarGUI ((variables != null) ? variables.vars : null, parameters, ParameterType.ComponentVariable, false);
				}
				else
				{
					variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
					variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
					variables = IDToField <Variables> (variables, variablesConstantID, false);
					
					if (variables != null)
					{
						ShowVarGUI (variables.vars, parameters, ParameterType.ComponentVariable, true);
					}
				}
			}
		}


		private void ShowVarGUI (List<GVar> _vars, List<ActionParameter> parameters, ParameterType parameterType, bool changeID)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			if (parameterID == -1)
			{
				variableNumber = -1;
			}

			VariableType showType = VariableType.Boolean;

			if (changeID)
			{
				if (_vars != null && _vars.Count > 0)
				{
					foreach (GVar _var in _vars)
					{
						labelList.Add (_var.label);
						
						// If a GlobalVar variable has been removed, make sure selected variable is still valid
						if (changeID && _var.id == variableID)
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

					variableNumber = EditorGUILayout.Popup ("Variable:", variableNumber, labelList.ToArray());
					variableID = _vars [variableNumber].id;
					showType = _vars[variableNumber].type;
				}
				else
				{
					EditorGUILayout.HelpBox ("No variables exist!", MessageType.Info);
					variableID = -1;
					variableNumber = -1;
					return;
				}

				placeholderType = showType;
			}
			else
			{
				showType = placeholderType;
			}
			string label = "Statement: ";

			switch (showType)
			{
				case VariableType.Boolean:
					setVarMethodIntBool = (SetVarMethodIntBool) EditorGUILayout.EnumPopup ("New value is:", setVarMethodIntBool);
					label += "=";
					if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
					{
						setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, ParameterType.Boolean);
						if (setParameterID < 0)
						{
							boolValue = (BoolValue) EditorGUILayout.EnumPopup (label, boolValue);
						}
					}
					else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
					{
						ShowMecanimGUI ();
					}
					break;

				case VariableType.Float:
					setVarMethodIntBool = (SetVarMethodIntBool) EditorGUILayout.EnumPopup ("New value is:", setVarMethodIntBool);

					if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
					{
						setVarMethod = (SetVarMethod) EditorGUILayout.EnumPopup ("Method:", setVarMethod);

						if (setVarMethod == SetVarMethod.Formula)
						{
							label += "=";

							setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
							if (setParameterID < 0)
							{
								formula = EditorGUILayout.TextField (label, formula);
							}
							
							#if UNITY_WP8
							EditorGUILayout.HelpBox ("This feature is not available for Windows Phone 8.", MessageType.Warning);
							#endif
						}
						else
						{
							if (setVarMethod == SetVarMethod.IncreaseByValue)
							{
								label += "+=";
							}
							else if (setVarMethod == SetVarMethod.SetValue)
							{
								label += "=";
							}
							else if (setVarMethod == SetVarMethod.SetAsRandom)
							{
								label += "= 0 to (exc.)";
							}

							setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, ParameterType.Float);
							if (setParameterID < 0)
							{
								floatValue = EditorGUILayout.FloatField (label, floatValue);
								
								if (setVarMethod == SetVarMethod.SetAsRandom && floatValue < 0f)
								{
									floatValue = 0f;
								}
							}
						}
					}
					else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
					{
						ShowMecanimGUI ();
					}
					break;

				case VariableType.Integer:
					setVarMethodIntBool = (SetVarMethodIntBool) EditorGUILayout.EnumPopup ("New value is:", setVarMethodIntBool);

					if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
					{
						setVarMethod = (SetVarMethod) EditorGUILayout.EnumPopup ("Method:", setVarMethod);

						if (setVarMethod == SetVarMethod.Formula)
						{
							label += "=";
							
							setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
							if (setParameterID < 0)
							{
								formula = EditorGUILayout.TextField (label, formula);
							}
							
							#if UNITY_WP8
							EditorGUILayout.HelpBox ("This feature is not available for Windows Phone 8.", MessageType.Warning);
							#endif
						}
						else
						{
							if (setVarMethod == SetVarMethod.IncreaseByValue)
							{
								label += "+=";
							}
							else if (setVarMethod == SetVarMethod.SetValue)
							{
								label += "=";
							}
							else if (setVarMethod == SetVarMethod.SetAsRandom)
							{
								label += ("= 0 to");
							}

							setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, new ParameterType[2] { ParameterType.Integer, ParameterType.PopUp });
							if (setParameterID < 0)
							{
								intValue = EditorGUILayout.IntField (label, intValue);

								if (setVarMethod == SetVarMethod.SetAsRandom && intValue < 0)
								{
									intValue = 0;
								}
							}
						}
					}
					else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
					{
						ShowMecanimGUI ();
					}
					break;

				case VariableType.PopUp:
					setVarMethod = (SetVarMethod) EditorGUILayout.EnumPopup ("Method:", setVarMethod);
				
					if (setVarMethod == SetVarMethod.Formula)
					{
						label += "=";
						
						setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (setParameterID < 0)
						{
							formula = EditorGUILayout.TextField (label, formula);
						}
						
						#if UNITY_WP8
						EditorGUILayout.HelpBox ("This feature is not available for Windows Phone 8.", MessageType.Warning);
						#endif
					}
					else if (setVarMethod == SetVarMethod.IncreaseByValue || setVarMethod == SetVarMethod.SetValue)
					{
						if (setVarMethod == SetVarMethod.IncreaseByValue)
						{
							label += "+=";
						}
						else if (setVarMethod == SetVarMethod.SetValue)
						{
							label += "=";
						}

						setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, new ParameterType[2] { ParameterType.Integer, ParameterType.PopUp });
						if (setParameterID < 0)
						{
							if (setVarMethod == SetVarMethod.SetValue && changeID && _vars != null)
							{
								string[] popUpLabels = _vars[variableNumber].GenerateEditorPopUpLabels ();
								intValue = EditorGUILayout.Popup (label, intValue, popUpLabels);
								placeholderPopUpLabelDataID = _vars[variableNumber].popUpID;
							}
							else if (setVarMethod == SetVarMethod.SetValue && !changeID && AdvGame.GetReferences ().variablesManager != null)
							{
								// Parameter override
								placeholderPopUpLabelDataID = AdvGame.GetReferences ().variablesManager.ShowPlaceholderPresetData (placeholderPopUpLabelDataID);
								PopUpLabelData popUpLabelData = AdvGame.GetReferences ().variablesManager.GetPopUpLabelData (placeholderPopUpLabelDataID);

								if (popUpLabelData != null && placeholderPopUpLabelDataID >= 0)
								{
									// Show placeholder labels
									intValue = EditorGUILayout.Popup (label, intValue, popUpLabelData.GenerateEditorPopUpLabels ());
								}
								else
								{
									intValue = EditorGUILayout.IntField (label, intValue);
								}
							}
							else
							{
								intValue = EditorGUILayout.IntField (label, intValue);
							}
							
							if (setVarMethod == SetVarMethod.SetAsRandom && intValue < 0)
							{
								intValue = 0;
							}
						}
					}
					break;

				case VariableType.String:
					setVarMethodString = (SetVarMethodString) EditorGUILayout.EnumPopup ("New value is:", setVarMethodString);

					if (setVarMethodString == SetVarMethodString.CombinedWithOtherString)
					{
						label += "+=";
					}
					else
					{
						label += "=";
					}

					if (setVarMethodString == SetVarMethodString.EnteredHere || setVarMethodString == SetVarMethodString.CombinedWithOtherString)
					{
						setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (setParameterID < 0)
						{
							EditorGUILayout.BeginHorizontal ();
							EditorGUILayout.LabelField (label, GUILayout.Width (140f));
							EditorStyles.textField.wordWrap = true;
							stringValue = EditorGUILayout.TextArea (stringValue, GUILayout.MaxWidth (400f));
							EditorGUILayout.EndHorizontal ();
						}
					}
					else if (setVarMethodString == SetVarMethodString.SetAsMenuElementText)
					{
						menuName = EditorGUILayout.TextField ("Menu name:", menuName);
						elementName = EditorGUILayout.TextField ("Element name:", elementName);

						slotNumberParameterID = Action.ChooseParameterGUI ("Slot # (optional):", parameters, slotNumberParameterID, new ParameterType[2] { ParameterType.Integer, ParameterType.PopUp });
						if (slotNumberParameterID < 0)
						{
							slotNumber = EditorGUILayout.IntField ("Slot # (optional):", slotNumber);
						}
						slotAccountsForOffset = EditorGUILayout.Toggle ("Slot # includes offset?", slotAccountsForOffset);
					}
					break;

				case VariableType.Vector3:
					setVarMethodVector = (SetVarMethodVector) EditorGUILayout.EnumPopup ("Method:", setVarMethodVector);

					if (setVarMethodVector == SetVarMethodVector.IncreaseByValue)
					{
						label += "+=";
					}
					else if (setVarMethodVector == SetVarMethodVector.SetValue)
					{
						label += "=";
					}

					setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, ParameterType.Vector3);
					if (setParameterID < 0)
					{
						vector3Value = EditorGUILayout.Vector3Field (label, vector3Value);
					}
					break;

				case VariableType.GameObject:
					setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, ParameterType.GameObject);
					if (setParameterID < 0)
					{
						gameObjectValue = (GameObject) EditorGUILayout.ObjectField (label, gameObjectValue, typeof (GameObject), location != VariableLocation.Global);
					}
					break;

				case VariableType.UnityObject:
					setParameterID = Action.ChooseParameterGUI (label, parameters, setParameterID, ParameterType.UnityObject);
					if (setParameterID < 0)
					{
						unityObjectValue = EditorGUILayout.ObjectField (label, unityObjectValue, typeof (Object), false);
					}
					break;

				default:
					break;
			}
		}


		private void ShowMecanimGUI ()
		{
			animator = (Animator) EditorGUILayout.ObjectField ("Animator:", animator, typeof (Animator), true);
			parameterName = EditorGUILayout.TextField ("Parameter name:", parameterName);
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
			string labelAdd = "";

			if (vars.Count > 0 && variableNumber > -1 && vars.Count > variableNumber)
			{
				labelAdd = vars [variableNumber].label;

				if (vars[variableNumber].type == VariableType.Integer)
				{
					if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
					{
						switch (setVarMethod)
						{
							case SetVarMethod.IncreaseByValue:
								labelAdd += " += " + intValue;
								break;

							case SetVarMethod.SetValue:
								labelAdd += " = " + intValue;
								break;

							case SetVarMethod.SetAsRandom:
								labelAdd += " = 0 to " + intValue;
								break;

							case SetVarMethod.Formula:
								labelAdd += " = " + formula;
								break;
						}
					}
					else
					{
						labelAdd += " = " + parameterName;
					}
				}
				else if (vars[variableNumber].type == VariableType.Boolean)
				{
					switch (setVarMethodIntBool)
					{
						case SetVarMethodIntBool.EnteredHere:
							labelAdd += " = " + boolValue;
							break;

						case SetVarMethodIntBool.SetAsMecanimParameter:
							labelAdd += " = " + parameterName;
							break;
					}
				}
				else if (vars[variableNumber].type == VariableType.PopUp)
				{
					switch (setVarMethod)
					{
						case SetVarMethod.Formula:
							break;

						case SetVarMethod.IncreaseByValue:
							labelAdd += " += " + intValue;
							break;

						case SetVarMethod.SetAsRandom:
							break;

						case SetVarMethod.SetValue:
							if (intValue >= 0 && intValue < vars[variableNumber].GetNumPopUpValues ())
							{
								labelAdd += " = " + vars[variableNumber].GetPopUpForIndex (intValue);
							}
							break;
					}
				}
				else if (vars[variableNumber].type == VariableType.Float)
				{
					if (setVarMethodIntBool == SetVarMethodIntBool.EnteredHere)
					{
						switch (setVarMethod)
						{
							case SetVarMethod.IncreaseByValue:
								labelAdd += " += " + floatValue;
								break;

							case SetVarMethod.SetValue:
								labelAdd += " = " + floatValue;
								break;

							case SetVarMethod.SetAsRandom:
								labelAdd += " = 0 to " + floatValue;
								break;

							case SetVarMethod.Formula:
								labelAdd += " = " + formula;
								break;
						}
					}
					else if (setVarMethodIntBool == SetVarMethodIntBool.SetAsMecanimParameter)
					{
						labelAdd += " = " + parameterName;
					}
				}
				else if (vars[variableNumber].type == VariableType.String)
				{
					switch (setVarMethodString)
					{
						case SetVarMethodString.EnteredHere:
							labelAdd += " = " + stringValue;
							break;

						case SetVarMethodString.SetAsMenuElementText:
							labelAdd += " = " + elementName;
							break;

						case SetVarMethodString.CombinedWithOtherString:
							labelAdd += " += " + stringValue;
							break;
					}
				}
				else if (vars[variableNumber].type == VariableType.GameObject)
				{
					if (gameObjectValue)
					{
						labelAdd += " = " + gameObjectValue;
					}
				}
				else if (vars[variableNumber].type == VariableType.UnityObject)
				{
					if (unityObjectValue)
					{
						labelAdd += " = " + unityObjectValue;
					}
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
			return wasAmended;
		}


		public override int GetNumVariableReferences (VariableLocation _location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (location == _location && variableID == varID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables && variables == _variables) || (variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
				{
					thisCount ++;
				}
			}

			thisCount += base.GetNumVariableReferences (location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation _location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (location == _location && variableID == oldVarID && parameterID < 0)
			{
				if (location != VariableLocation.Component || (variables && variables == _variables) || (variablesConstantID != 0 && _variablesConstantID == variablesConstantID))
				{
					variableID = newVarID;
					thisCount++;
				}
			}

			thisCount += base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
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

	
		private GVar GetVariable ()
		{
			switch (location)
			{
				case VariableLocation.Global:
					VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
					if (variablesManager != null)
					{
						return variablesManager.GetVariable (variableID);
					}
					return null;

				case VariableLocation.Local:
					return LocalVariables.GetVariable (variableID);

				case VariableLocation.Component:
					if (variables != null)
					{
						return variables.GetVariable (variableID);
					}
					break;
			}

			return null;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0 && gameObjectValue && gameObjectValue == gameObject)
			{
				return true;
			}

			if (parameterID < 0 && location == VariableLocation.Component)
			{
				if (variables && variables.gameObject == gameObject) return true;
				return (variablesConstantID == id && id != 0);
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return stringValue;
		}


		public int GetTranslationID (int index)
		{
			return lineID;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			stringValue = updatedText;
		}


		public int GetNumTranslatables ()
		{
			return 1;
		}


		public bool HasExistingTranslation (int index)
		{
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			lineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Variable;
		}


		public bool CanTranslate (int index)
		{
			if (setVarMethodString == SetVarMethodString.EnteredHere && setParameterID < 0)
			{
				GVar variable = GetVariable ();
				if (variable != null && variable.type == VariableType.String && !string.IsNullOrEmpty (stringValue))
				{
					return true;
				}
			}
			return false;
		}

		#endif

		#endregion


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Global integer variable</summary>
		 * <param name = "globalVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Global (int globalVariableID, int newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.intValue = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Global float variable</summary>
		 * <param name = "globalVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Global (int globalVariableID, float newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.floatValue = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Global boolean variable</summary>
		 * <param name = "globalVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Global (int globalVariableID, bool newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.intValue = (newValue) ? 1 : 0;
			newAction.boolValue = (newValue) ? BoolValue.True : BoolValue.False;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Global Vector3 variable</summary>
		 * <param name = "globalVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Global (int globalVariableID, Vector3 newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.vector3Value = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Global string variable</summary>
		 * <param name = "globalVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Global (int globalVariableID, string newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Global;
			newAction.variableID = globalVariableID;
			newAction.stringValue = newValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Local integer variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Local (int localVariableID, int newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.intValue = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Local float variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Local (int localVariableID, float newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.floatValue = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Local bool variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Local (int localVariableID, bool newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.intValue = (newValue) ? 1 : 0;
			newAction.boolValue = (newValue) ? BoolValue.True : BoolValue.False;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Local Vector3 variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Local (int localVariableID, Vector3 newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.vector3Value = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Local string variable</summary>
		 * <param name = "localVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Local (int localVariableID, string newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Local;
			newAction.variableID = localVariableID;
			newAction.stringValue = newValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Component integer variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Component (Variables variables, int componentVariableID, int newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.intValue = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Component float variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Component (Variables variables, int componentVariableID, float newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.floatValue = newValue;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Component boolean variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Component (Variables variables, int componentVariableID, bool newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.intValue = (newValue) ? 1 : 0;
			newAction.boolValue = (newValue) ? BoolValue.True : BoolValue.False;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Component Vector3 variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarSet CreateNew_Component (Variables variables, int componentVariableID, Vector3 newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.vector3Value = newValue;
			return newAction;
		}

		/**
		 * <summary>Creates a new instance of the 'Variable: Set' Action, set to update a Component string variable</summary>
		 * <param name = "variables">The associated Variables component</param>
		 * <param name = "componentVariableID">The ID number of the variable</param>
		 * <param name = "newValue">The variable's new value</param>
		 * <returns>The generated Action</returns>
		 */

		public static ActionVarSet CreateNew_Component (Variables variables, int componentVariableID, string newValue)
		{
			ActionVarSet newAction = CreateNew<ActionVarSet> ();
			newAction.location = VariableLocation.Component;
			newAction.variables = variables;
			newAction.TryAssignConstantID (newAction.variables, ref newAction.variablesConstantID);
			newAction.variableID = componentVariableID;
			newAction.stringValue = newValue;
			return newAction;
		}

	}

}