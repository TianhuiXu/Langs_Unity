/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionParameter.cs"
 * 
 *	This defines a parameter that can be used by ActionLists
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if AddressableIsPresent
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	/** A data container for an ActionList parameter. A parameter can change the value of an Action's public variables dynamically during gameplay, allowing the same Action to be repurposed for different tasks. */
	[System.Serializable]
	public class ActionParameter
	{

		/** The display name in the Editor */
		public string label = "";
		/** A description to display as a tooltip when setting its value externally */
		public string description = "";
		/** A unique identifier */
		public int ID = 0;
		/** The type of variable it overrides (GameObject, InventoryItem, GlobalVariable, LocalVariable, String, Float, Integer, Boolean, Vector3, Document, ComponentVariable, Parameter) */
		public ParameterType parameterType = ParameterType.GameObject;
		/** The new value or ID number, if parameterType = ParameterType.Integer / Boolean / LocalVariable / GlobalVariable / InventoryItem / Document / ComponentVariable / PopUp.  If parameterType = ParameterType.GameObject, it is the ConstantID number of the GameObject if it is not currently accessible */
		public int intValue = -1;
		/** The constant ID number of the Variables component, if parameterType = ParameterType.ComponentVariabel */
		public int constantID = 0;
		/** The Preset ID for the popup, if parameterType = ParameterType.PopUp */
		public int popUpID = 0;
		/** The new value, if parameterType = ParameterType.Float */
		public float floatValue = 0f;
		/** The new value, if parameterType = ParameterType.String */
		public string stringValue = "";
		/** The new value, if parameterType = ParameterType.GameObject */
		public GameObject gameObject;
		/** The new value, if parameterType = ParameterType.UnityObject */
		public Object objectValue;
		/** The new value, if parameterType = ParameterType.Vector3 */
		public Vector3 vector3Value;
		/** If a prefab is passed between assets as a GameObject parameter, what should ultimately be referenced (ReferencePrefab, ReferenceSceneInstance) */
		public GameObjectParameterReferences gameObjectParameterReferences = GameObjectParameterReferences.ReferencePrefab;

		public Variables variables = null;

		#if UNITY_EDITOR
		[System.NonSerialized] public bool isDisplayed = true;
		#endif


		/**
		 * <summary>A Constructor that generates a unique ID number.</summary>
		 * <param name = "idArray">An array of previously-used ID numbers, to ensure its own ID is unique.</param>
		 */
		public ActionParameter (int[] idArray)
		{
			label = string.Empty;
			description = string.Empty;
			ID = 0;
			intValue = -1;
			floatValue = 0f;
			stringValue = string.Empty;
			gameObject = null;
			objectValue = null;
			parameterType = ParameterType.GameObject;
			vector3Value = Vector3.zero;
			gameObjectParameterReferences = GameObjectParameterReferences.ReferencePrefab;
			variables = null;
			constantID = 0;
			popUpID = 0;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (ID == _id)
					ID++;
			}

			label = "Parameter " + (ID + 1).ToString ();
		}


		/**
		 * <summary>A Constructor that sets the ID number explicitly.</summary>
		 * <param name = "id">The unique identifier to assign</param>
		 */
		public ActionParameter (int id)
		{
			label = string.Empty;
			description = string.Empty;
			ID = id;
			intValue = -1;
			floatValue = 0f;
			stringValue = string.Empty;
			gameObject = null;
			objectValue = null;
			parameterType = ParameterType.GameObject;
			vector3Value = Vector3.zero;
			gameObjectParameterReferences = GameObjectParameterReferences.ReferencePrefab;
			variables = null;
			constantID = 0;
			popUpID = 0;

			label = "Parameter " + (ID + 1).ToString ();
		}


		/**
		 * <summary>A Constructor that duplicates another ActionParameter.</summary>
		 */
		public ActionParameter (ActionParameter _actionParameter, bool alsoCopyValues = false)
		{
			label = _actionParameter.label;
			description = _actionParameter.description;
			ID = _actionParameter.ID;
			parameterType = _actionParameter.parameterType;

			if (alsoCopyValues)
			{
				intValue = _actionParameter.intValue;
				floatValue = _actionParameter.floatValue;
				stringValue = _actionParameter.stringValue;
				gameObject = _actionParameter.gameObject;
				objectValue = _actionParameter.objectValue;
				vector3Value = _actionParameter.vector3Value;
				gameObjectParameterReferences = _actionParameter.gameObjectParameterReferences;
				variables = _actionParameter.variables;
				constantID = _actionParameter.constantID;
				popUpID = _actionParameter.popUpID;
			}
			else
			{
				intValue = -1;
				floatValue = 0f;
				stringValue = string.Empty;
				gameObject = null;
				objectValue = null;
				vector3Value = Vector3.zero;
				gameObjectParameterReferences = GameObjectParameterReferences.ReferencePrefab;
				variables = null;
				constantID = 0;
				popUpID = 0;
			}
		}


		/**
		 * <summary>Copies the "value" variables from another ActionParameter, without changing the type, ID, or label.</summary>
		 * <parameter name = "otherParameter">The ActionParameter to copy from</param>
		 */
		public void CopyValues (ActionParameter otherParameter)
		{
			intValue = otherParameter.intValue;
			floatValue = otherParameter.floatValue;
			stringValue = otherParameter.stringValue;
			gameObject = otherParameter.gameObject;
			objectValue = otherParameter.objectValue;
			vector3Value = otherParameter.vector3Value;
			gameObjectParameterReferences = otherParameter.gameObjectParameterReferences;
			variables = otherParameter.variables;
			constantID = otherParameter.constantID;
			popUpID = otherParameter.popUpID;
		}


		/** Resets the value that the parameter assigns. */
		public void Reset ()
		{
			intValue = -1;
			floatValue = 0f;
			stringValue = string.Empty;
			gameObject = null;
			objectValue = null;
			vector3Value = Vector3.zero;
			gameObjectParameterReferences = GameObjectParameterReferences.ReferencePrefab;
			variables = null;
			constantID = 0;
			popUpID = 0;
		}


		/**
		 * <summary>Checks if the parameter's value is an integer. This is the case if parameterType = ParameterType.GameObject, GlobalVariable, Integer, InventoryItem or LocalVariable.</summary>
		 * <returns>True if the parameter's value is an integer.</returns>
		 */
		public bool IsIntegerBased ()
		{
			switch (parameterType)
			{
				case ParameterType.GameObject:
				case ParameterType.GlobalVariable:
				case ParameterType.Integer:
				case ParameterType.Boolean:
				case ParameterType.InventoryItem:
				case ParameterType.Document:
				case ParameterType.LocalVariable:
				case ParameterType.ComponentVariable:
				case ParameterType.PopUp:
					return true;

				default:
					return false;
			}
		}


		/**
		 * <summary>Sets the intValue that the parameter assigns</summary>
		 * <param name = "_value">The new value or ID number, if parameterType = ParameterType.Integer / Boolean / LocalVariable / GlobalVariable / InventoryItem.  If parameterType = ParameterType.GameObject, it is the ConstantID number of the GameObject if it is not currently accessible</param>
		 */
		public void SetValue (int _value)
		{
			intValue = _value;
			floatValue = 0f;
			stringValue = string.Empty;
			gameObject = null;
			objectValue = null;
			vector3Value = Vector3.zero;
			variables = null;

			if (gameObject == null && parameterType == ParameterType.GameObject && gameObjectParameterReferences != GameObjectParameterReferences.ReferencePrefab)
			{
				ConstantID constantID = ConstantID.GetComponent (intValue);
				if (constantID) gameObject = constantID.gameObject;
			}
		}


		/**
		 * <summary>Sets the floatValue that the parameter assigns</summary>
		 * <param name = "_value">The new value, if parameterType = ParameterType.Float</param>
		 */
		public void SetValue (float _value)
		{
			floatValue = _value;
			stringValue = string.Empty;
			intValue = -1;
			gameObject = null;
			objectValue = null;
			vector3Value = Vector3.zero;
			variables = null;
		}


		/**
		 * <summary>Sets the stringValue that the parameter assigns</summary>
		 * <param name = "_value">The new value, if parameterType = ParameterType.String</param>
		 */
		public void SetValue (string _value)
		{
			stringValue = AdvGame.ConvertTokens (_value);
			floatValue = 0f;
			intValue = -1;
			gameObject = null;
			objectValue = null;
			vector3Value = Vector3.zero;
			variables = null;
		}


		/**
		 * <summary>Sets the vector3Value that the parameter assigns</summary>
		 * <param name = "_value">The new value, if parameterType = ParameterType.Vector3</param>
		 */
		public void SetValue (Vector3 _value)
		{
			stringValue = string.Empty;
			floatValue = 0f;
			intValue = -1;
			gameObject = null;
			objectValue = null;
			vector3Value = _value;
			variables = null;
		}


		/**
		 * <summary>Sets the gameObject that the parameter assigns</summary>
		 * <param name = "_object">The new GameObject, if parameterType = ParameterType.GameObject</param>
		 */
		public void SetValue (GameObject _object)
		{
			gameObject = _object;
			floatValue = 0f;
			stringValue = string.Empty;
			intValue = -1;
			objectValue = null;
			vector3Value = Vector3.zero;
			variables = null;
		}


		/**
		 * <summary>Sets the objectValue that the parameter assigns</summary>
		 * <param name = "_object">The new Unity Object, if parameterType = ParameterType.UnityObject</param>
		 */
		public void SetValue (Object _object)
		{
			gameObject = null;
			floatValue = 0f;
			stringValue = string.Empty;
			intValue = -1;
			objectValue = _object;
			vector3Value = Vector3.zero;
			variables = null;
		}


		/**
		 * <summary>Sets the gameObject that the parameter assigns</summary>
		 * <param name = "_object">The new GameObject, if parameterType = ParameterType.GameObject</param>
		 * <param name = "_value">The GameObject's ConstantID number, which is used to find the GameObject if it is not always in the same scene as the ActionParameter class</param>
		 */
		public void SetValue (GameObject _object, int _value)
		{
			gameObject = _object;
			floatValue = 0f;
			stringValue = string.Empty;
			intValue = _value;
			objectValue = null;
			vector3Value = Vector3.zero;
			variables = null;
		}


		public void SetValue (Variables _variables, int _value)
		{
			gameObject = null;
			floatValue = 0f;
			stringValue = string.Empty;
			intValue = _value;
			objectValue = null;
			vector3Value = Vector3.zero;
			variables = _variables;
		}


		public GVar GetVariable ()
		{
			switch (parameterType)
			{
				case ParameterType.GlobalVariable:
					return GlobalVariables.GetVariable (intValue, true);

				case ParameterType.LocalVariable:
					return LocalVariables.GetVariable (intValue);

				case ParameterType.ComponentVariable:
					if (variables != null)
					{
						return variables.GetVariable (intValue);
					}
					break;

				default:
					break;
			}
			return null;
		}


		/**
		 * <summary>Generates a label that represents the name of the parameter's value, if appropriate<summary>
		 * <returns>A label that represents the name of the parameter's value<summary>
		 */
		public string GetLabel ()
		{
			switch (parameterType)
			{
				case ParameterType.GameObject:
					if (gameObject != null)
					{
						Hotspot _hotspot = gameObject.GetComponent<Hotspot> ();
						if (_hotspot) return _hotspot.GetName (Options.GetLanguage ());

						Char _char = gameObject.GetComponent<Char> ();
						if (_char) return _char.GetName (Options.GetLanguage ());

						return gameObject.name;
					}
					return string.Empty;

				case ParameterType.InventoryItem:
					InvItem invItem = KickStarter.inventoryManager.GetItem (intValue);
					if (invItem != null)
					{
						return invItem.GetLabel (Options.GetLanguage ());
					}
					break;

				case ParameterType.Document:
					Document document = KickStarter.inventoryManager.GetDocument (intValue);
					if (document != null)
					{
						return KickStarter.runtimeLanguages.GetTranslation (document.title,
																			document.titleLineID,
																			Options.GetLanguage (),
																			AC_TextType.Document);
					}
					break;

				case ParameterType.GlobalVariable:
					GVar gVar = GetVariable ();
					if (gVar != null)
					{
						return gVar.label;
					}
					break;

				case ParameterType.LocalVariable:
					GVar lVar = GetVariable ();
					if (lVar != null)
					{
						return lVar.label;
					}
					break;

				case ParameterType.ComponentVariable:
					GVar cVar = GetVariable ();
					if (cVar != null)
					{
						return cVar.label;
					}
					break;

				default:
					return GetValueAsString ();
			}

			return GetValueAsString ();
		}


		/** Gets the parameters value formatted to a string */
		public string GetValueAsString ()
		{
			switch (parameterType)
			{
				case ParameterType.Float:
					return floatValue.ToString ();

				case ParameterType.String:
					return stringValue;

				case ParameterType.GameObject:
					if (gameObject != null)
					{
						ConstantID _constantID = gameObject.GetComponent <ConstantID>();
						if (_constantID)
						{
							return _constantID.constantID.ToString ();
						}
						ACDebug.LogWarning ("Could not save parameter data for '" + gameObject.name + "' as it has no Constant ID number.", gameObject);
					}
					return string.Empty;

				case ParameterType.UnityObject:
					if (objectValue != null)
					{
						return objectValue.name;
					}
					return string.Empty;

				case ParameterType.Vector3:
					string vector3Val = vector3Value.x.ToString () + "," + vector3Value.y.ToString () + "," + vector3Value.z.ToString ();
					return vector3Val;

				case ParameterType.PopUp:
					PopUpLabelData popUpLabelData = KickStarter.variablesManager.GetPopUpLabelData (popUpID);
					if (popUpLabelData != null)
					{
						return popUpLabelData.GetValue (intValue);
					}
					ACDebug.LogWarning ("Could not get parameter text for PopUp parameter " + label + " as no PopUp preset with ID = " + popUpID + " was found");
					break;

				default:
					break;
			}

			return intValue.ToString ();
		}


		/**
		 * <summary>Generates a string that represents the parameter's saveable data</summar>
		 * <returns>The data string</returns>
		 */
		public string GetSaveData ()
		{
			switch (parameterType)
			{
				case ParameterType.Float:
					return floatValue.ToString ();

				case ParameterType.String:
					return AdvGame.PrepareStringForSaving (stringValue);

				case ParameterType.GameObject:
					if (gameObject != null)
					{
						ConstantID _constantID = gameObject.GetComponent <ConstantID>();
						if (_constantID)
						{
							return _constantID.constantID.ToString ();
						}
						ACDebug.LogWarning ("Could not save parameter data for '" + gameObject.name + "' as it has no Constant ID number.", gameObject);
					}
					return string.Empty;

				case ParameterType.UnityObject:
					if (objectValue != null)
					{
						return objectValue.name;
					}
					return string.Empty;

				case ParameterType.Vector3:
					string vector3Val = vector3Value.x.ToString () + "," + vector3Value.y.ToString () + "," + vector3Value.z.ToString ();
					vector3Val = AdvGame.PrepareStringForSaving (vector3Val);
					return vector3Val;

				default:
					return intValue.ToString ();
			}
		}


		/**
		 * <summary>Restores data from a data string</summary>
		 * <param name="dataString">The data</param>
		 */
		public void LoadData (string dataString)
		{
			switch (parameterType)
			{
				case ParameterType.Float:
					floatValue = 0f;
					float.TryParse (dataString, out floatValue);
					break;

				case ParameterType.String:
					stringValue = AdvGame.PrepareStringForLoading (dataString);
					break;

				case ParameterType.GameObject:
					gameObject = null;
					int constantID = 0;
					if (int.TryParse (dataString, out constantID))
					{
						ConstantID _constantID = ConstantID.GetComponent <ConstantID> (constantID);
						if (_constantID)
						{
							gameObject = _constantID.gameObject;
						}
					}
					break;

				case ParameterType.UnityObject:
					if (string.IsNullOrEmpty (dataString))
					{
						objectValue = null;
					}
					else
					{
						#if AddressableIsPresent
						if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
						{
							Addressables.LoadAssetAsync<Object> (dataString).Completed += OnCompleteLoad;
							return;
						}
						#endif

						Object[] objects = Resources.LoadAll (string.Empty);
						foreach (Object _object in objects)
						{
							if (_object.name == dataString)
							{
								objectValue = _object;
								return;
							}
						}
					}
					break;

				case ParameterType.Vector3:
					if (!string.IsNullOrEmpty (dataString))
					{
						dataString = AdvGame.PrepareStringForLoading (dataString);

						Vector3 _value = Vector3.zero;
						string[] valuesArray = dataString.Split (","[0]);
						if (valuesArray != null && valuesArray.Length == 3)
						{
							float xValue = 0f;
							float.TryParse (valuesArray[0], out xValue);

							float yValue = 0f;
							float.TryParse (valuesArray[1], out yValue);

							float zValue = 0f;
							float.TryParse (valuesArray[2], out zValue);

							_value = new Vector3 (xValue, yValue, zValue);
						}

						vector3Value = _value;
					}
					break;

				default:
					intValue = 0;
					int.TryParse (dataString, out intValue);
					break;
			}
		}


		#if AddressableIsPresent

		private void OnCompleteLoad (AsyncOperationHandle<Object> obj)
		{
			if (obj.Result == null) return;
			objectValue = obj.Result;
		}

		#endif


		#if UNITY_EDITOR

		public void ShowGUI (bool isAssetFile, bool onlyEditValues = false, bool readOnly = false)
		{
			if (Application.isPlaying || readOnly)
			{
				EditorGUILayout.LabelField ("Label:", label);
			}
			else
			{
				label = CustomGUILayout.TextField ("Label:", label, string.Empty, "The parameter's name");
			}
			
			if (Application.isPlaying || readOnly)
			{
				EditorGUILayout.LabelField ("Type:", parameterType.ToString ());
				EditorGUILayout.LabelField ("Current value:", "'" + GetLabel () + "'");
				CustomGUILayout.TokenLabel ("[param:" + ID.ToString () + "]");
			}
			else
			{
				if (onlyEditValues)
				{
					EditorGUILayout.LabelField ("Type:", parameterType.ToString ());
				}
				else
				{
					parameterType = (ParameterType) CustomGUILayout.EnumPopup ("Type:", parameterType, string.Empty, "The parameter's type");
					description = CustomGUILayout.TextArea ("Description:", description, string.Empty, "Descriptive text to display as a tooltip when assigning this parameter's value");
				}

				switch (parameterType)
				{
					case ParameterType.Boolean:
						BoolValue boolValue = (intValue == 1) ? BoolValue.True : BoolValue.False;
						boolValue = (BoolValue) CustomGUILayout.EnumPopup ("Default value:", boolValue);
						intValue = (boolValue == BoolValue.True) ? 1 : 0;
						break;

					case ParameterType.Integer:
						intValue = CustomGUILayout.IntField ("Default value:", intValue);
						break;

					case ParameterType.Float:
						floatValue = CustomGUILayout.FloatField ("Default value:", floatValue);
						break;

					case ParameterType.String:
						stringValue = CustomGUILayout.TextArea ("Default value:", stringValue);
						break;

					case ParameterType.Vector3:
						vector3Value = CustomGUILayout.Vector3Field ("Default value:", vector3Value);
						break;

					case ParameterType.UnityObject:
						objectValue = CustomGUILayout.ObjectField <Object> ("Default value:", objectValue, true);
						break;

					case ParameterType.Document:
						if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
						{
							InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
							intValue = ActionRunActionList.ShowDocumentSelectorGUI ("Default value:", inventoryManager.documents, intValue);
						}
						else
						{
							EditorGUILayout.HelpBox ("An Inventory Manager is required.", MessageType.Warning);
						}
						break;

					case ParameterType.InventoryItem:
						if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
						{
							InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
							intValue = ActionRunActionList.ShowInvItemSelectorGUI ("Default value:", inventoryManager.items, intValue);
						}
						else
						{
							EditorGUILayout.HelpBox ("An Inventory Manager is required to pass Inventory items.", MessageType.Warning);
						}
						break;

					case ParameterType.GlobalVariable:
						if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
						{
							VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
							intValue = ActionRunActionList.ShowVarSelectorGUI ("Default value:", variablesManager.vars, intValue);
						}
						else
						{
							EditorGUILayout.HelpBox ("A Variables Manager is required to pass Global Variables.", MessageType.Warning);
						}
						break;

					case ParameterType.LocalVariable:
						if (isAssetFile)
						{
							intValue = 0;
							EditorGUILayout.HelpBox ("Local Variable parameters cannot have default values in ActionList Assets.", MessageType.Info);
						}
						else
						{
							if (KickStarter.localVariables)
							{
								intValue = ActionRunActionList.ShowVarSelectorGUI ("Default value:", KickStarter.localVariables.localVars, intValue);
							}
							else
							{
								EditorGUILayout.HelpBox ("A GameEngine prefab is required to pass Local Variables.", MessageType.Warning);
							}
						}
						break;

					case ParameterType.ComponentVariable:
						if (isAssetFile)
						{
							variables = null;
							intValue = 0;
							EditorGUILayout.HelpBox ("Component Variable parameters cannot have default values in ActionList Assets.", MessageType.Info);
						}
						else
						{
							variables = (Variables) CustomGUILayout.ObjectField <Variables> ("Variables component:", variables, true);
							if (variables != null)
							{
								intValue = ActionRunActionList.ShowVarSelectorGUI ("Default value:", variables.vars, intValue);
							}
						}
						break;

					case ParameterType.GameObject:
						if (isAssetFile)
						{
							gameObject = (GameObject) EditorGUILayout.ObjectField ("Default value:", gameObject, typeof (GameObject), true);
							if (gameObject != null)
							{
								if (!UnityVersionHandler.IsPrefabFile (gameObject))
								{
									intValue = Action.FieldToID (gameObject, intValue, false, isAssetFile);
									gameObject = Action.IDToField (gameObject, intValue, true, false, isAssetFile);
								}
								else
								{
									// A prefab, ask if we want to affect the prefab or the scene-based instance?
									gameObjectParameterReferences = (GameObjectParameterReferences) EditorGUILayout.EnumPopup ("GameObject parameter:", gameObjectParameterReferences);
								}
							}
							else
							{
								intValue = EditorGUILayout.IntField ("Default value (ID #):", intValue);
							}
						}
						else
						{
							// Gameobject
							gameObject = (GameObject) CustomGUILayout.ObjectField <GameObject> ("Default value:", gameObject, true);
							intValue = 0;
							if (gameObject && gameObject.GetComponent<ConstantID> () == null)
							{
								UnityVersionHandler.AddConstantIDToGameObject<ConstantID> (gameObject);
							}
						}
						break;

					case ParameterType.PopUp:
						{
							if (KickStarter.variablesManager == null)
							{
								EditorGUILayout.HelpBox ("A Variables Manager must be assigned for PopUp parameter values to display.", MessageType.Warning);
								break;
							}

							List<string> popUpPresetLabels = new List<string> ();
							int j = 0;
							for (int i = 0; i < KickStarter.variablesManager.popUpLabelData.Count; i++)
							{
								popUpPresetLabels.Add (KickStarter.variablesManager.popUpLabelData[i].EditorLabel);
								if (popUpID == KickStarter.variablesManager.popUpLabelData[i].ID)
								{
									j = i;
								}
							}

							popUpPresetLabels.Add ("Create new...");
							j = EditorGUILayout.Popup ("Label preset:", j, popUpPresetLabels.ToArray ());
							
							if (j == popUpPresetLabels.Count - 1)
							{
								// Create new
								if (popUpPresetLabels.Count > PopUpLabelData.MaxPresets)
								{
									ACDebug.LogWarning ("The maximum number of popup presets has been reached!");
									break;
								}

								List<int> idArray = new List<int> ();
								foreach (PopUpLabelData data in KickStarter.variablesManager.popUpLabelData)
								{
									idArray.Add (data.ID);
								}
								idArray.Sort ();
								PopUpLabelData newData = new PopUpLabelData (idArray.ToArray (), new string[0], -1);
								KickStarter.variablesManager.popUpLabelData.Add (newData);
								popUpID = newData.ID;

								EditorUtility.SetDirty (KickStarter.variablesManager);
							}
							else
							{
								popUpID = KickStarter.variablesManager.popUpLabelData[j].ID;
							}

							PopUpLabelData presetData = KickStarter.variablesManager.GetPopUpLabelData (popUpID);
							if (presetData != null)
							{
								presetData.ShowGUI (!Application.isPlaying, KickStarter.variablesManager);
								intValue = CustomGUILayout.Popup ("Default value:", intValue, presetData.Labels);
							}
							else
							{
								EditorGUILayout.HelpBox ("No PopUp preset data found with ID = " + popUpID + " - is the correct Variables Manager assigned?", MessageType.Warning);
							}
						}
						break;

					default:
						break;
				}

				CustomGUILayout.TokenLabel ("[param:" + ID.ToString () + "]");
			}
		}

		#endif

	}

}