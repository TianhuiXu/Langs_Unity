/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionParamSet.cs"
 * 
 *	This action sets the value of an ActionList's parameter.
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
	public class ActionParamSet : Action, IItemReferencerAction, IDocumentReferencerAction
	{

		public ActionListSource actionListSource = ActionListSource.InScene;
		
		public ActionList actionList;
		public int actionListConstantID;
		public ActionListAsset actionListAsset;

		public bool changeOwn;
		public int parameterID = -1;
		public int parameterToCopyID = -1;
		
		public int intValue, intValueMax;
		public float floatValue, floatValueMax;
		public string stringValue;

		public GameObject gameobjectValue;
		protected GameObject runtimeGameobjectValue;
		public int gameObjectConstantID;

		public Object unityObjectValue;

		public Vector3 vector3Value;

		public SetParamMethod setParamMethod = SetParamMethod.EnteredHere;
		public int globalVariableID;
		public int globalVariableParameterID = -1;
		private GVar runtimeVariable;

		public int ownParamID = -1;

		public Variables variables;
		protected Variables runtimeVariables;
		protected ActionParameter _parameter;
		protected ActionParameter _parameterToCopy;

		public Animator animator;
		public int animatorConstantID;
		public int animatorParameterID = -1;
		protected Animator runtimeAnimator;
		public string animatorParameterName;

		#if UNITY_EDITOR
		[SerializeField] private string parameterLabel = "";
		#endif


		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Set parameter"; }}
		public override string Description { get { return "Sets the value of a parameter in an ActionList."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{	
			if (!changeOwn)
			{
				if (actionListSource == ActionListSource.InScene)
				{
					actionList = AssignFile <ActionList> (actionListConstantID, actionList);
					if (actionList != null)
					{
						if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.syncParamValues && actionList.assetFile.useParameters)
							{
								_parameter = GetParameterWithID (actionList.assetFile.GetParameters (), parameterID);
								_parameterToCopy = GetParameterWithID (actionList.assetFile.GetParameters (), parameterToCopyID);
							}
							else
							{
								_parameter = GetParameterWithID (actionList.parameters, parameterID);
								_parameterToCopy = GetParameterWithID (actionList.parameters, parameterToCopyID);
							}
						}
						else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							_parameter = GetParameterWithID (actionList.parameters, parameterID);
							_parameterToCopy = GetParameterWithID (actionList.parameters, parameterToCopyID);
						}
					}
				}
				else if (actionListSource == ActionListSource.AssetFile)
				{
					if (actionListAsset != null)
					{
						_parameter = GetParameterWithID (actionListAsset.GetParameters (), parameterID);
						_parameterToCopy = GetParameterWithID (actionListAsset.GetParameters (), parameterToCopyID);

						if (_parameter.parameterType == ParameterType.GameObject && !isAssetFile && gameobjectValue != null && gameObjectConstantID == 0)
						{
							if (gameobjectValue.GetComponent <ConstantID>())
							{
								gameObjectConstantID = gameobjectValue.GetComponent <ConstantID>().constantID;
							}
							else
							{
								ACDebug.LogWarning ("The GameObject '" + gameobjectValue.name + "' must have a Constant ID component in order to be passed as a parameter to an asset file.", gameobjectValue);
							}
						}
					}
				}
			}
			else
			{
				_parameter = GetParameterWithID (parameters, parameterID);
				_parameterToCopy = GetParameterWithID (parameters, parameterToCopyID);

				if (_parameter.parameterType == ParameterType.GameObject && isAssetFile && gameobjectValue != null && gameObjectConstantID == 0)
				{
					if (gameobjectValue.GetComponent <ConstantID>())
					{
						gameObjectConstantID = gameobjectValue.GetComponent <ConstantID>().constantID;
					}
					else
					{
						ACDebug.LogWarning ("The GameObject '" + gameobjectValue.name + "' must have a Constant ID component in order to be passed as a parameter to an asset file.", gameobjectValue);
					}
				}
			}

			runtimeGameobjectValue = AssignFile (gameObjectConstantID, gameobjectValue);

			if (setParamMethod == SetParamMethod.EnteredHere && _parameter != null)
			{
				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
						BoolValue boolValue = (intValue == 1) ? BoolValue.True : BoolValue.False; 
						boolValue = AssignBoolean (parameters, ownParamID, boolValue);
						intValue = (boolValue == BoolValue.True) ? 1 : 0;
						break;

					case ParameterType.Float:
						floatValue = AssignFloat (parameters, ownParamID, floatValue);
						break;

					case ParameterType.GameObject:
						runtimeGameobjectValue = AssignFile (parameters, ownParamID, gameObjectConstantID, gameobjectValue);
						break;

					case ParameterType.GlobalVariable:
					case ParameterType.LocalVariable:
						intValue = AssignVariableID (parameters, ownParamID, intValue);
						break;

					case ParameterType.ComponentVariable:
						runtimeVariables = AssignFile <Variables>  (gameObjectConstantID, variables);
						ActionParameter _param = GetParameterWithID (parameters, ownParamID);
						if (_param != null && _param.parameterType == ParameterType.ComponentVariable)
						{
							runtimeVariables = _param.variables;
							intValue = _param.intValue;
						}
						break;

					case ParameterType.Integer:
					case ParameterType.PopUp:
						intValue = AssignInteger (parameters, ownParamID, intValue);
						break;

					case ParameterType.InventoryItem:
						intValue = AssignInvItemID (parameters, ownParamID, intValue);
						break;

					case ParameterType.Document:
						intValue = AssignDocumentID (parameters, ownParamID, intValue);
						break;

					case ParameterType.String:
						stringValue = AssignString (parameters, ownParamID, stringValue);
						break;

					case ParameterType.UnityObject:
						unityObjectValue = AssignObject <Object> (parameters, ownParamID, unityObjectValue);
						break;

					case ParameterType.Vector3:
						vector3Value = AssignVector3 (parameters, ownParamID, vector3Value);
						break;
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromGlobalVariable)
			{
				ActionParameter parameter = GetParameterWithID (parameters, globalVariableParameterID);
				if (parameter != null && parameter.parameterType == ParameterType.GlobalVariable)
				{
					runtimeVariable = parameter.GetVariable ();
				}
				else
				{
					runtimeVariable = GlobalVariables.GetVariable (globalVariableID);
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromAnimator)
			{
				runtimeAnimator = AssignFile <Animator> (parameters, animatorParameterID, animatorConstantID, animator);
			}
			else if (setParamMethod == SetParamMethod.CopiedFromComponentVariable)
			{
				ActionParameter parameter = GetParameterWithID (parameters, globalVariableParameterID);
				if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
				{
					runtimeVariable = parameter.GetVariable ();
				}
				else
				{
					runtimeVariables = AssignFile<Variables> (gameObjectConstantID, variables);
					if (runtimeVariables)
					{
						globalVariableID = AssignVariableID (parameters, globalVariableParameterID, globalVariableID);
						runtimeVariable = runtimeVariables.GetVariable (globalVariableID);
					}
				}
			}
		}


		public override float Run ()
		{
			if (_parameter == null)
			{
				LogWarning ("Cannot set parameter value since it cannot be found!");
				return 0f;
			}

			if (setParamMethod == SetParamMethod.CopiedFromGlobalVariable)
			{
				SetValueFromVariable (runtimeVariable);
			}
			else if (setParamMethod == SetParamMethod.CopiedFromComponentVariable)
			{
				SetValueFromVariable (runtimeVariable);
			}
			else if (setParamMethod == SetParamMethod.EnteredHere)
			{
				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
					case ParameterType.Integer:
					case ParameterType.GlobalVariable:
					case ParameterType.LocalVariable:
					case ParameterType.InventoryItem:
					case ParameterType.Document:
					case ParameterType.PopUp:
						_parameter.intValue = intValue;
						break;

					case ParameterType.Float:
						_parameter.floatValue = floatValue;
						break;

					case ParameterType.String:
						_parameter.stringValue = stringValue;
						break;

					case ParameterType.GameObject:
						_parameter.gameObject = runtimeGameobjectValue;
						_parameter.intValue = gameObjectConstantID;
						break;

					case ParameterType.UnityObject:
						_parameter.objectValue = unityObjectValue;
						break;

					case ParameterType.Vector3:
						_parameter.vector3Value = vector3Value;
						break;

					case ParameterType.ComponentVariable:
						_parameter.SetValue (runtimeVariables, intValue);
						break;

					default:
						break;
				}
			}
			else if (setParamMethod == SetParamMethod.Random)
			{
				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
						_parameter.intValue = Random.Range (0, 2);
						break;

					case ParameterType.Integer:
						_parameter.intValue = Random.Range (intValue, intValueMax + 1);
						break;

					case ParameterType.PopUp:
						{
							PopUpLabelData popUpLabelData = KickStarter.variablesManager.GetPopUpLabelData (_parameter.popUpID);
							if (popUpLabelData != null)
							{
								_parameter.intValue = Random.Range (0, popUpLabelData.Length);
							}
						}
						break;

					case ParameterType.Float:
						_parameter.floatValue = Random.Range (floatValue, floatValueMax);
						break;

					default:
						LogWarning ("Parameters of type '" + _parameter.parameterType + "' cannot be set randomly.");
						break;
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromParameter)
			{
				if (_parameterToCopy == null)
				{
					LogWarning ("Cannot copy parameter value since it cannot be found!");
					return 0f;
				}

				_parameter.CopyValues (_parameterToCopy);
			}
			else if (setParamMethod == SetParamMethod.CopiedFromAnimator)
			{
				if (runtimeAnimator == null)
				{
					LogWarning ("Cannot set the value of parameter " + _parameter.label + ", because no Animator was found.");
					return 0f;
				}

				if (string.IsNullOrEmpty (animatorParameterName))
				{
					LogWarning ("Cannot set a parameter value from Animator because no Animator parameter was named.");
					return 0f;
				}

				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
						bool val = runtimeAnimator.GetBool (animatorParameterName);
						_parameter.SetValue ((val) ? 1 : 0);
						break;

					case ParameterType.Integer:
					case ParameterType.PopUp:
						_parameter.SetValue (runtimeAnimator.GetInteger (animatorParameterName));
						break;

					case ParameterType.Float:
						_parameter.SetValue (runtimeAnimator.GetFloat (animatorParameterName));
						break;

					default:
						LogWarning ("Parameters of type '" + _parameter.parameterType + "' cannot be set from an Animator.");
						break;
				}
			}

			return 0f;
		}


		private void SetValueFromVariable (GVar gVar)
		{
			if (gVar != null)
			{
				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
					case ParameterType.Integer:
					case ParameterType.PopUp:
						_parameter.intValue = gVar.IntegerValue;
						break;

					case ParameterType.Float:
						_parameter.floatValue = gVar.FloatValue;
						break;

					case ParameterType.Vector3:
						_parameter.vector3Value = gVar.Vector3Value;
						break;

					case ParameterType.String:
						_parameter.stringValue = GlobalVariables.GetStringValue (globalVariableID, true, Options.GetLanguage ());
						break;

					case ParameterType.GameObject:
						_parameter.SetValue (gVar.GameObjectValue);
						break;

					case ParameterType.UnityObject:
						_parameter.SetValue (gVar.UnityObjectValue);
						break;

					case ParameterType.InventoryItem:
						if (gVar.type == VariableType.Integer)
						{
							_parameter.SetValue (gVar.IntegerValue);
						}
						else if (gVar.type == VariableType.String)
						{
							string itemName = gVar.TextValue;
							InvItem invItem = KickStarter.inventoryManager.GetItem (itemName);
							if (invItem != null)
							{
								_parameter.SetValue (invItem.id);
							}
						}
						else
						{
							LogWarning ("Only variables of type Integer or String can update an Inventory item parameter");
						}
						break;

					case ParameterType.Document:
						if (gVar.type == VariableType.Integer)
						{
							_parameter.SetValue (gVar.IntegerValue);
						}
						else
						{
							LogWarning ("Only variables of type Integer can update a Document parameter");
						}
						break;

					default:
						break;
				}
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			changeOwn = EditorGUILayout.Toggle ("Change own?", changeOwn);
			if (changeOwn)
			{
				if (parameters == null || parameters.Count == 0)
				{
					EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
				}

				parameterID = Action.ChooseParameterGUI (parameters, parameterID);
				SetParamGUI (parameters);
			}
			else
			{
				actionListSource = (ActionListSource) EditorGUILayout.EnumPopup ("Source:", actionListSource);
				if (actionListSource == ActionListSource.InScene)
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					actionListConstantID = FieldToID <ActionList> (actionList, actionListConstantID);
					actionList = IDToField <ActionList> (actionList, actionListConstantID, true);

					if (actionList != null)
					{
						if (actionList.source == ActionListSource.InScene)
						{
							if (actionList.NumParameters > 0)
							{
								parameterID = Action.ChooseParameterGUI (actionList.parameters, parameterID);
								SetParamGUI (actionList.parameters, parameters);
							}
							else
							{
								EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
							}
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.assetFile.NumParameters > 0)
							{
								parameterID = Action.ChooseParameterGUI (actionList.assetFile.DefaultParameters, parameterID);
								SetParamGUI (actionList.assetFile.DefaultParameters, parameters, true);
							}
							else
							{
								EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
							}
						}
						else
						{
							EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
						}
					}
				}
				else if (actionListSource == ActionListSource.AssetFile)
				{
					actionListAsset = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", actionListAsset, typeof (ActionListAsset), true);

					if (actionListAsset != null)
					{
						if (actionListAsset.NumParameters > 0)
						{
							parameterID = Action.ChooseParameterGUI (actionListAsset.DefaultParameters, parameterID);
							SetParamGUI (actionListAsset.DefaultParameters, parameters, true);
						}
						else
						{
							EditorGUILayout.HelpBox ("This ActionList Asset has no parameters defined!", MessageType.Warning);
						}
					}
				}
			}
		}
		
		
		private void SetParamGUI (List<ActionParameter> parameters, List<ActionParameter> ownParameters = null, bool forceConstantIDs = false)
		{
			if (parameters == null || parameters.Count == 0)
			{
				parameterLabel = string.Empty;
				return;
			}

			_parameter = GetParameterWithID (parameters, parameterID);

			if (_parameter == null)
			{
				parameterLabel = string.Empty;
				return;
			}

			parameterLabel = _parameter.label;

			setParamMethod = (SetParamMethod) EditorGUILayout.EnumPopup ("New value is:", setParamMethod);

			if (setParamMethod == SetParamMethod.EnteredHere)
			{
				if (ownParameters != null && ownParameters.Count > 0)
				{
					ownParamID = Action.ChooseParameterGUI ("Set as:", ownParameters, ownParamID, _parameter.parameterType);
					if (ownParamID >= 0)
					{
						return;
					}
				}

				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
						bool boolValue = (intValue == 1) ? true : false;
						boolValue = EditorGUILayout.Toggle ("Set as:", boolValue);
						intValue = (boolValue) ? 1 : 0;
						break;

					case ParameterType.Integer:
						intValue = EditorGUILayout.IntField ("Set as:", intValue);
						break;

					case ParameterType.PopUp:
						{
							PopUpLabelData popUpLabelData = KickStarter.variablesManager.GetPopUpLabelData (_parameter.popUpID);
							if (popUpLabelData != null)
							{
								intValue = EditorGUILayout.Popup ("Set as:", intValue, popUpLabelData.GenerateEditorPopUpLabels ());
							}
							else
							{
								intValue = EditorGUILayout.IntField ("Set as:", intValue);
								EditorGUILayout.HelpBox ("No PopUp label data found for ID = " + _parameter.popUpID, MessageType.Warning);
							}
						}
						break;

					case ParameterType.Float:
						floatValue = EditorGUILayout.FloatField ("Set as:", floatValue);
						break;

					case ParameterType.String:
						stringValue = EditorGUILayout.TextField ("Set as:", stringValue);
						break;

					case ParameterType.GameObject:
						gameobjectValue = (GameObject) EditorGUILayout.ObjectField ("Set to:", gameobjectValue, typeof (GameObject), true);
						gameObjectConstantID = FieldToID (gameobjectValue, gameObjectConstantID, forceConstantIDs);
						gameobjectValue = IDToField (gameobjectValue, gameObjectConstantID, false, forceConstantIDs);
						break;

					case ParameterType.GlobalVariable:
						if (AdvGame.GetReferences ().variablesManager == null || AdvGame.GetReferences ().variablesManager.vars == null || AdvGame.GetReferences ().variablesManager.vars.Count == 0)
						{
							EditorGUILayout.HelpBox ("No Global variables exist!", MessageType.Info);
						}
						else
						{
							intValue = ShowVarSelectorGUI (AdvGame.GetReferences ().variablesManager.vars, intValue);
						}
						break;

					case ParameterType.UnityObject:
						unityObjectValue = (Object) EditorGUILayout.ObjectField ("Set to:", unityObjectValue, typeof (Object), true);
						break;

					case ParameterType.InventoryItem:
						intValue = ShowInvSelectorGUI (intValue);
						break;

					case ParameterType.Document:
						intValue = ShowDocSelectorGUI (intValue);
						break;

					case ParameterType.LocalVariable:
						if (isAssetFile)
						{
							EditorGUILayout.HelpBox ("Cannot access local variables from an asset file.", MessageType.Warning);
						}
						else if (KickStarter.localVariables == null || KickStarter.localVariables.localVars == null || KickStarter.localVariables.localVars.Count == 0)
						{
							EditorGUILayout.HelpBox ("No Local variables exist!", MessageType.Info);
						}
						else
						{
							intValue = ShowVarSelectorGUI (KickStarter.localVariables.localVars, intValue);
						}
						break;

					case ParameterType.Vector3:
						vector3Value = EditorGUILayout.Vector3Field ("Set as:", vector3Value);
						break;

					case ParameterType.ComponentVariable:
						variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
						gameObjectConstantID = FieldToID<Variables> (variables, gameObjectConstantID);
						variables = IDToField<Variables> (variables, gameObjectConstantID, false);
						if (variables != null)
						{
							intValue = ShowVarSelectorGUI (variables.vars, intValue);
						}
						break;

					default:
						break;
				}
			}
			else if (setParamMethod == SetParamMethod.Random)
			{
				switch (_parameter.parameterType)
				{
					case ParameterType.Integer:
						intValue = EditorGUILayout.IntField ("Minimum:", intValue);
						intValueMax = EditorGUILayout.IntField ("Maximum:", intValueMax);
						if (intValueMax < intValue) intValueMax = intValue;
						break;

					case ParameterType.Float:
						floatValue = EditorGUILayout.FloatField ("Minimum:", floatValue);
						floatValueMax = EditorGUILayout.FloatField ("Maximum:", floatValueMax);
						if (floatValueMax < floatValue) floatValueMax = floatValue;
						break;

					case ParameterType.PopUp:
					case ParameterType.Boolean:
						break;

					default:
						EditorGUILayout.HelpBox ("Parameters of type '" + _parameter.parameterType + "' cannot be set randomly.", MessageType.Warning);
						break;
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromGlobalVariable)
			{
				globalVariableParameterID = Action.ChooseParameterGUI ("Variable:", parameters, globalVariableParameterID, ParameterType.GlobalVariable);
				if (globalVariableParameterID < 0)
				{
					if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager != null && AdvGame.GetReferences ().variablesManager.vars != null && AdvGame.GetReferences ().variablesManager.vars.Count > 0)
					{
						switch (_parameter.parameterType)
						{
							case ParameterType.Vector3:
								globalVariableID = AdvGame.GlobalVariableGUI ("Vector3 variable:", globalVariableID, VariableType.Vector3);
								break;

							case ParameterType.Boolean:
							case ParameterType.Float:
							case ParameterType.Integer:
							case ParameterType.PopUp:
							case ParameterType.String:
							case ParameterType.GameObject:
							case ParameterType.UnityObject:
								globalVariableID = AdvGame.GlobalVariableGUI ("Variable:", globalVariableID);
								break;

							case ParameterType.InventoryItem:
								{
									globalVariableID = AdvGame.GlobalVariableGUI ("Variable:", globalVariableID, new VariableType[] { VariableType.Integer, VariableType.String });
									GVar _gVar = KickStarter.variablesManager.GetVariable (globalVariableID);
									if (_gVar != null)
									{
										if (_gVar.type == VariableType.Integer)
										{
											EditorGUILayout.HelpBox ("The Integer value will refer to the Inventory item's ID.", MessageType.Info);
										}
										else if (_gVar.type == VariableType.String)
										{
											EditorGUILayout.HelpBox ("The String value will refer to the Inventory item's name.", MessageType.Info);
										}
									}
								}
								break;

							case ParameterType.Document:
								{
									globalVariableID = AdvGame.GlobalVariableGUI ("Variable:", globalVariableID, VariableType.Integer);
									GVar _gVar = KickStarter.variablesManager.GetVariable (globalVariableID);
									if (_gVar != null)
									{
										if (_gVar.type == VariableType.Integer)
										{
											EditorGUILayout.HelpBox ("The Integer value will refer to the Document's ID.", MessageType.Info);
										}
									}
								}
								break;

							default:
								EditorGUILayout.HelpBox ("Parameters of type '" + _parameter.parameterType + "' cannot have values transferred from Global Variables.", MessageType.Warning);
								break;
						}
					}
					else
					{
						EditorGUILayout.HelpBox ("No Global Variables found!", MessageType.Warning);
					}
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromComponentVariable)
			{
				globalVariableParameterID = Action.ChooseParameterGUI ("Variable:", parameters, globalVariableParameterID, ParameterType.ComponentVariable);
				if (globalVariableParameterID < 0)
				{
					variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
					gameObjectConstantID = FieldToID<Variables> (variables, gameObjectConstantID);
					variables = IDToField<Variables> (variables, gameObjectConstantID, false);

					if (variables && variables.vars != null && variables.vars.Count > 0)
					{
						switch (_parameter.parameterType)
						{
							case ParameterType.Vector3:
								globalVariableID = AdvGame.ComponentVariableGUI ("Vector3 variable:", globalVariableID, VariableType.Vector3, variables);
								break;

							case ParameterType.Boolean:
								globalVariableID = AdvGame.ComponentVariableGUI ("Boolean variable:", globalVariableID, VariableType.Boolean, variables);
								break;

							case ParameterType.Float:
								globalVariableID = AdvGame.ComponentVariableGUI ("Float variable:", globalVariableID, VariableType.Float, variables);
								break;

							case ParameterType.Integer:
								globalVariableID = AdvGame.ComponentVariableGUI ("Integer variable:", globalVariableID, VariableType.Integer, variables);
								break;

							case ParameterType.PopUp:
								globalVariableID = AdvGame.ComponentVariableGUI ("PopUp variable:", globalVariableID, new VariableType[] { VariableType.Integer, VariableType.PopUp }, variables);
								break;

							case ParameterType.String:
								globalVariableID = AdvGame.ComponentVariableGUI ("String variable:", globalVariableID, VariableType.String, variables);
								break;

							case ParameterType.GameObject:
								globalVariableID = AdvGame.ComponentVariableGUI ("GameObject variable:", globalVariableID, VariableType.GameObject, variables);
								break;

							case ParameterType.UnityObject:
								globalVariableID = AdvGame.ComponentVariableGUI ("Object Variable:", globalVariableID, VariableType.UnityObject, variables);
								break;

							case ParameterType.InventoryItem:
								{
									globalVariableID = AdvGame.ComponentVariableGUI ("InvItem variable:", globalVariableID, new VariableType[] { VariableType.Integer, VariableType.String }, variables);
									GVar _gVar = variables.GetVariable (globalVariableID);
									if (_gVar != null)
									{
										if (_gVar.type == VariableType.Integer)
										{
											EditorGUILayout.HelpBox ("The Integer value will refer to the Inventory item's ID.", MessageType.Info);
										}
										else if (_gVar.type == VariableType.String)
										{
											EditorGUILayout.HelpBox ("The String value will refer to the Inventory item's name.", MessageType.Info);
										}
									}
								}
								break;

							case ParameterType.Document:
								{
									globalVariableID = AdvGame.ComponentVariableGUI ("Document variable:", globalVariableID, VariableType.Integer, variables);
									GVar _gVar = variables.GetVariable (globalVariableID);
									if (_gVar != null)
									{
										if (_gVar.type == VariableType.Integer)
										{
											EditorGUILayout.HelpBox ("The Integer value will refer to the Document's ID.", MessageType.Info);
										}
									}
								}
								break;

							default:
								EditorGUILayout.HelpBox ("Parameters of type '" + _parameter.parameterType + "' cannot have values transferred from Component Variables.", MessageType.Warning);
								break;
						}
					}
					else if (variables)
					{
						EditorGUILayout.HelpBox ("No Global Variables found!", MessageType.Warning);
					}
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromParameter)
			{
				if (changeOwn)
				{
					parameterToCopyID = Action.ChooseParameterGUI (parameters, parameterToCopyID);
				}
				else
				{
					if (actionListSource == ActionListSource.InScene && actionList != null)
					{
						if (actionList.source == ActionListSource.InScene)
						{
							if (actionList.NumParameters > 0)
							{
								parameterToCopyID = Action.ChooseParameterGUI (actionList.parameters, parameterToCopyID);
							}
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.assetFile.NumParameters  > 0)
							{
								parameterToCopyID = Action.ChooseParameterGUI (actionList.assetFile.DefaultParameters, parameterToCopyID);
							}
						}
					}
					else if (actionListSource == ActionListSource.AssetFile && actionListAsset != null)
					{
						if (actionListAsset.NumParameters  > 0)
						{
							parameterToCopyID = Action.ChooseParameterGUI (actionListAsset.DefaultParameters, parameterToCopyID);
						}
					}
				}
			}
			else if (setParamMethod == SetParamMethod.CopiedFromAnimator)
			{
				switch (_parameter.parameterType)
				{
					case ParameterType.Boolean:
					case ParameterType.Float:
					case ParameterType.Integer:
					case ParameterType.PopUp:
						animatorParameterID = Action.ChooseParameterGUI ("Animator:", parameters, animatorParameterID, ParameterType.GameObject);
						if (animatorParameterID >= 0)
						{
							animatorConstantID = 0;
							animator = null;
						}
						else
						{
							animator = (Animator) EditorGUILayout.ObjectField ("Animator:", animator, typeof (Animator), true);
							animatorConstantID = FieldToID<Animator> (animator, animatorConstantID);
							animator = IDToField<Animator> (animator, animatorConstantID, true);
						}

						animatorParameterName = EditorGUILayout.TextField ("Animator parameter:", animatorParameterName);
						if (!string.IsNullOrEmpty (animatorParameterName))
						{
							EditorGUILayout.HelpBox ("The Animator parameter '" + animatorParameterName + "' must be of type '" + _parameter.parameterType.ToString (), MessageType.Info);
						}
						break;

					default:
						EditorGUILayout.HelpBox ("Parameters of type '" + _parameter.parameterType + "' cannot have values transferred from Animators.", MessageType.Warning);
						break;
				}
			}
		}
		
		
		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (gameobjectValue, gameObjectConstantID, 0);
		}
		
		
		public override string SetLabel ()
		{
			return parameterLabel;
		}


		private int ShowVarSelectorGUI (List<GVar> vars, int ID)
		{
			int variableNumber = -1;
			
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
			
			variableNumber = EditorGUILayout.Popup ("Variable:", variableNumber, labelList.ToArray());
			ID = vars[variableNumber].id;
			
			return ID;
		}
		
		
		private int ShowInvSelectorGUI (int ID)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager == null)
			{
				return ID;
			}
			
			int invNumber = -1;
			List<string> labelList = new List<string>();
			int i=0;
			foreach (InvItem _item in inventoryManager.items)
			{
				labelList.Add (_item.label);
				
				// If an item has been removed, make sure selected variable is still valid
				if (_item.id == ID)
				{
					invNumber = i;
				}
				
				i++;
			}
			
			if (invNumber == -1)
			{
				// Wasn't found (item was possibly deleted), so revert to zero
				if (ID > 0) LogWarning ("Previously chosen item no longer exists!");
				
				invNumber = 0;
				ID = 0;
			}
			
			invNumber = EditorGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray());
			ID = inventoryManager.items[invNumber].id;
			
			return ID;
		}


		private int ShowDocSelectorGUI (int ID)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager == null)
			{
				return ID;
			}
			
			int docNumber = -1;
			List<string> labelList = new List<string>();
			int i=0;
			foreach (Document _document in inventoryManager.documents)
			{
				labelList.Add (_document.Title);
				
				// If an item has been removed, make sure selected variable is still valid
				if (_document.ID == ID)
				{
					docNumber = i;
				}
				
				i++;
			}
			
			if (docNumber == -1)
			{
				// Wasn't found (item was possibly deleted), so revert to zero
				if (ID > 0) LogWarning ("Previously chosen document no longer exists!");
				
				docNumber = 0;
				ID = 0;
			}
			
			docNumber = EditorGUILayout.Popup ("Document:", docNumber, labelList.ToArray());
			ID = inventoryManager.documents[docNumber].ID;
			
			return ID;
		}
		
		
		private int GetVarNumber (List<GVar> vars, int ID)
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


		public override int GetNumVariableReferences (VariableLocation location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			if (setParamMethod == SetParamMethod.CopiedFromGlobalVariable && location == VariableLocation.Global && globalVariableID == varID)
			{
				thisCount ++;
			}

			if (setParamMethod == SetParamMethod.EnteredHere)
			{
				ActionParameter _param = null;

				if (changeOwn)
				{
					if (parameters != null)
					{
						_param = GetParameterWithID (parameters, parameterID);
					}
				}
				else
				{
					if (actionListSource == ActionListSource.InScene && actionList != null)
					{
						if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							_param = GetParameterWithID (actionList.parameters, parameterID);
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
						{
							_param = GetParameterWithID (actionList.assetFile.DefaultParameters, parameterID);
						}
					}
					else if (actionListSource == ActionListSource.AssetFile && actionListAsset != null && actionListAsset.useParameters)
					{
						_param = GetParameterWithID (actionListAsset.DefaultParameters, parameterID);
					}
				}

				if (_param != null && _param.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && varID == intValue)
				{
					thisCount ++;
				}
				else if (_param != null && _param.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && varID == intValue)
				{
					thisCount ++;
				}
				else if (_param != null && _param.parameterType == ParameterType.ComponentVariable && location == VariableLocation.Component && varID == intValue && _variables)
				{
					if ((_param.variables && _param.variables == _variables) || 
						(_param.constantID != 0 && _param.constantID == _variablesConstantID))
					{
						thisCount++;
					}
				}
			}

			thisCount += base.GetNumVariableReferences (location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			if (setParamMethod == SetParamMethod.CopiedFromGlobalVariable && location == VariableLocation.Global && globalVariableID == oldVarID)
			{
				globalVariableID = newVarID;
				thisCount++;
			}

			if (setParamMethod == SetParamMethod.EnteredHere)
			{
				ActionParameter _param = null;

				if (changeOwn)
				{
					if (parameters != null)
					{
						_param = GetParameterWithID (parameters, parameterID);
					}
				}
				else
				{
					if (actionListSource == ActionListSource.InScene && actionList != null)
					{
						if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							_param = GetParameterWithID (actionList.parameters, parameterID);
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
						{
							_param = GetParameterWithID (actionList.assetFile.DefaultParameters, parameterID);
						}
					}
					else if (actionListSource == ActionListSource.AssetFile && actionListAsset != null && actionListAsset.useParameters)
					{
						_param = GetParameterWithID (actionListAsset.DefaultParameters, parameterID);
					}
				}

				if (_param != null && _param.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && oldVarID == intValue)
				{
					intValue = newVarID;
					thisCount++;
				}
				else if (_param != null && _param.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && oldVarID == intValue)
				{
					intValue = newVarID;
					thisCount++;
				}
				else if (_param != null && _param.parameterType == ParameterType.ComponentVariable && location == VariableLocation.Component && oldVarID == intValue && _variables)
				{
					if ((_param.variables && _param.variables == _variables) ||
						(_param.constantID != 0 && _param.constantID == _variablesConstantID))
					{
						intValue = newVarID;
						thisCount++;
					}
				}
			}

			thisCount += base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public int GetNumItemReferences (int _itemID, List<ActionParameter> parameters)
		{
			return GetParamReferences (parameters, _itemID, ParameterType.InventoryItem);
		}


		public int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> parameters)
		{
			return GetParamReferences (parameters, oldItemID, ParameterType.InventoryItem, true, newItemID);
		}


		public int GetNumDocumentReferences (int _docID, List<ActionParameter> parameters)
		{
			return GetParamReferences (parameters, _docID, ParameterType.Document);
		}


		public int UpdateDocumentReferences (int oldDocumentID, int newDocumentID, List<ActionParameter> parameters)
		{
			return GetParamReferences (parameters, oldDocumentID, ParameterType.Document, true, newDocumentID);
		}


		private int GetParamReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType, bool updateID = false, int newID = 0)
		{
			if (setParamMethod == SetParamMethod.EnteredHere)
			{
				ActionParameter _param = null;

				if (changeOwn)
				{
					if (parameters != null)
					{
						_param = GetParameterWithID (parameters, parameterID);
					}
				}
				else
				{
					if (actionListSource == ActionListSource.InScene && actionList != null)
					{
						if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							_param = GetParameterWithID (actionList.parameters, parameterID);
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
						{
							_param = GetParameterWithID (actionList.assetFile.DefaultParameters, parameterID);
						}
					}
					else if (actionListSource == ActionListSource.AssetFile && actionListAsset != null && actionListAsset.useParameters)
					{
						_param = GetParameterWithID (actionListAsset.DefaultParameters, parameterID);
					}
				}

				if (_param != null && _param.parameterType == _paramType && _ID == intValue)
				{
					if (updateID)
					{
						intValue = newID;
					}
					return 1;
				}
			}

			return 0;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!changeOwn && actionListSource == ActionListSource.InScene)
			{
				if (actionList && actionList.gameObject == _gameObject) return true;
				if (actionListConstantID == id) return true;
			}
			if (gameobjectValue && gameobjectValue == _gameObject) return true;
			if (gameObjectConstantID == id) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesAsset (ActionListAsset _actionListAsset)
		{
			if (!changeOwn && actionListSource == ActionListSource.AssetFile && _actionListAsset == actionListAsset)
				return true;
			return base.ReferencesAsset (_actionListAsset);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change the a Bool parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "newBoolValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, bool newBoolValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.intValue = (newBoolValue) ? 1 : 0;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Bool parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "newBoolValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, bool newBoolValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.intValue = (newBoolValue) ? 1 : 0;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Bool parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList asset with the parameter</param>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "newBoolValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, bool newBoolValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.intValue = (newBoolValue) ? 1 : 0;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change an Integer parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Integer parameter</param>
		 * <param name = "newIntegerValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, int newIntegerValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.intValue = newIntegerValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Bool parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "newBoolValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, int newIntegerValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.intValue = newIntegerValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change the an Integer parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Integer parameter</param>
		 * <param name = "newIntegerValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, int newIntegerValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.intValue = newIntegerValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Float parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "newFloatValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, float newFloatValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.floatValue = newFloatValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Float parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "newFloatValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, float newFloatValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.floatValue = newFloatValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Float parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "newFloatValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, float newFloatValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.floatValue = newFloatValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a String parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the String parameter</param>
		 * <param name = "newStringValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, string newStringValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.stringValue = newStringValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a String parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the String parameter</param>
		 * <param name = "newStringValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, string newStringValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.stringValue = newStringValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a String parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the String parameter</param>
		 * <param name = "newStringValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, string newStringValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.stringValue = newStringValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Vector2 parameter on its own ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Vector3 parameter</param>
		 * <param name = "newVectorValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */		
		public static ActionParamSet CreateNew (int parameterID, Vector3 newVectorValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.vector3Value = newVectorValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Vector3 parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Vector3 parameter</param>
		 * <param name = "newVectorValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, Vector3 newVectorValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.vector3Value = newVectorValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Vector3 parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Vector3 parameter</param>
		 * <param name = "newVectorValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */		
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, Vector3 newVectorValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.vector3Value = newVectorValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Variable parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Component Variable parameter</param>
		 * <param name = "variables">The new parameter's new Variables component</param>
		 * <param name = "newComponentVariableIDValue">The parameter's new variable ID number</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, Variables variables, int newComponentVariableIDValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.variables = variables;
			newAction.intValue = newComponentVariableIDValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Variable parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Component Variable parameter</param>
		 * <param name = "variables">The new parameter's new Variables component</param>
		 * <param name = "newComponentVariableIDValue">The parameter's new variable ID number</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, Variables variables, int newComponentVariableIDValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.variables = variables;
			newAction.intValue = newComponentVariableIDValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a Variable parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Component Variable parameter</param>
		 * <param name = "variables">The new parameter's new Variables component</param>
		 * <param name = "newComponentVariableIDValue">The parameter's new variable ID number</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, Variables variables, int newComponentVariableIDValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.variables = variables;
			newAction.intValue = newComponentVariableIDValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a GameObject parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the GameObject parameter</param>
		 * <param name = "newGameObjectValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, GameObject newGameObjectValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.gameobjectValue = newGameObjectValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a GameObject parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the GameObject parameter</param>
		 * <param name = "newGameObjectValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */	
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, GameObject newGameObjectValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.gameobjectValue = newGameObjectValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a GameObject parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the GameObject parameter</param>
		 * <param name = "newGameObjectValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, GameObject newGameObjectValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.gameobjectValue = newGameObjectValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a UnityObject parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the UnityObject parameter</param>
		 * <param name = "newObjectValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (int parameterID, Object newObjectValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = true;
			newAction.parameterID = parameterID;

			newAction.unityObjectValue = newObjectValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a UnityObject parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the UnityObject parameter</param>
		 * <param name = "newObjectValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionList actionList, int parameterID, Object newObjectValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.unityObjectValue = newObjectValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Set parameter' Action, set to change a UnityObject parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the UnityObject parameter</param>
		 * <param name = "newObjectValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamSet CreateNew (ActionListAsset actionListAsset, int parameterID, Object newObjectValue)
		{
			ActionParamSet newAction = CreateNew<ActionParamSet> ();
			newAction.changeOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.unityObjectValue = newObjectValue;

			return newAction;
		}

	}
	
}