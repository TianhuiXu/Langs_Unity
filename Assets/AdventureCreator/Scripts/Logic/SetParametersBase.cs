/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SetInteractionBase.cs"
 * 
 *	A base class to handle the setting and transferring of ActionParameter values in bulk.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A base class to handle the setting and transferring of ActionParameter values in bulk. */
	public abstract class SetParametersBase : MonoBehaviour
	{

		#region Variables

		[SerializeField] protected GUIData initialGUIData = new GUIData (new List<ActionParameter>(), new List<int>());
		[SerializeField] protected GUIData[] successiveGUIData = new GUIData[0];

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets a List of all ActionList assets referenced by the component</summary>
		 * <returns>A List of all ActionList assets referenced by the component</returns>
		 */
		public virtual List<ActionListAsset> GetReferencedActionListAssets ()
		{
			List<ActionListAsset> foundAssets = new List<ActionListAsset>();
			foundAssets = GetAssetsFromParameterGUIData (initialGUIData, foundAssets);
			if (successiveGUIData != null)
			{
				foreach (GUIData data in successiveGUIData)
				{
					foundAssets = GetAssetsFromParameterGUIData (data, foundAssets);
				}
			}
			return foundAssets;
		}

		#endregion


		#region ProtectedFunctions

		/**
		 * <summary>Updates an ActionLists's parameter values with its own</summary>
		 * <param name = "_actionList">The ActionList to update</param>
		 */
		protected void AssignParameterValues (ActionList _actionList, int runIndex = 0)
		{
			if (_actionList && _actionList.source == ActionListSource.InScene && _actionList.useParameters && _actionList.parameters != null)
			{
				BulkAssignParameterValues (_actionList.parameters, GetFromParameters (runIndex), false, false);
			}
			else if (_actionList && _actionList.source == ActionListSource.AssetFile && _actionList.assetFile && _actionList.assetFile.NumParameters > 0)
			{
				if (_actionList.syncParamValues)
				{
					BulkAssignParameterValues (_actionList.assetFile.GetParameters (), GetFromParameters (runIndex), false, true);
				}
				else
				{
					BulkAssignParameterValues (_actionList.parameters, GetFromParameters (runIndex), true, false);
				}
			}
		}


		/**
		 * <summary>Updates an ActionList asset's parameter values with its own</summary>
		 * <param name = "_actionListAsset">The ActionList asset to update</param>
		 */
		protected void AssignParameterValues (ActionListAsset _actionListAsset, int runIndex = 0)
		{
			if (_actionListAsset && _actionListAsset.NumParameters > 0)
			{
				BulkAssignParameterValues (_actionListAsset.GetParameters (), GetFromParameters (runIndex), false, true);
			}
		}


		protected List<ActionParameter> GetFromParameters (int index)
		{
			if (index <= 0)
			{
				return initialGUIData.fromParameters;
			}
			return successiveGUIData[index-1].fromParameters;
		}


		protected List<ActionListAsset> GetAssetsFromParameterGUIData (SetParametersBase.GUIData guiData, List<ActionListAsset> existingList)
		{
			if (guiData.fromParameters != null)
			{
				foreach (ActionParameter parameter in guiData.fromParameters)
				{
					if (parameter.parameterType == ParameterType.UnityObject)
					{
						if (parameter.objectValue)
						{
							if (parameter.objectValue is ActionListAsset)
							{
								ActionListAsset _actionListAsset = (ActionListAsset) parameter.objectValue;
								existingList.Add (_actionListAsset);
							}
						}
					}
				}
			}
			return existingList;
		}

		#endregion


		#region StaticFunctions

		/**
		 * <summary>Transfers values from one list of parameters to another</summary>
		 * <param name = "externalParameters">The parameters to update</param>
		 * <param name = "fromParameters">The parameters to get the new values from</param>
		 * <param name = "sendingToAsset">If True, the parameters to update are part of an asset file</param>
		 * <param name = "_isAssetFile">If True, the parameters to get the new values from are part of an asset file</param>
		 */
		public static void BulkAssignParameterValues (List<ActionParameter> externalParameters, List<ActionParameter> fromParameters, bool sendingToAsset, bool _isAssetFile)
		{
			for (int i=0; i<externalParameters.Count; i++)
			{
				if (fromParameters.Count > i)
				{
					switch (externalParameters[i].parameterType)
					{
						case ParameterType.String:
							externalParameters[i].SetValue (fromParameters[i].stringValue);
							break;

						case ParameterType.Float:
							externalParameters[i].SetValue (fromParameters[i].floatValue);
							break;

						case ParameterType.UnityObject:
							externalParameters[i].SetValue (fromParameters[i].objectValue);
							break;

						case ParameterType.Vector3:
							externalParameters[i].SetValue (fromParameters[i].vector3Value);
							break;

						case ParameterType.ComponentVariable:
							Variables variables = fromParameters[i].variables;
							if (variables == null && fromParameters[i].constantID != 0)
							{
								variables = ConstantID.GetComponent<Variables> (fromParameters[i].constantID);
							}
							externalParameters[i].SetValue (variables, fromParameters[i].intValue);
							break;

						case ParameterType.GameObject:
							externalParameters[i].gameObjectParameterReferences = fromParameters[i].gameObjectParameterReferences;
							if (sendingToAsset)
							{
								if (_isAssetFile)
								{
									if (fromParameters[i].gameObject)
									{
										// Referencing a prefab

										if (fromParameters[i].gameObjectParameterReferences == GameObjectParameterReferences.ReferencePrefab)
										{
											externalParameters[i].SetValue (fromParameters[i].gameObject);
										}
										else if (fromParameters[i].gameObjectParameterReferences == GameObjectParameterReferences.ReferenceSceneInstance)
										{
											int idToSend = 0;
											if (fromParameters[i].gameObject && fromParameters[i].gameObject.GetComponent <ConstantID>())
											{
												idToSend = fromParameters[i].gameObject.GetComponent <ConstantID>().constantID;
											}
											else
											{
												ACDebug.LogWarning (fromParameters[i].gameObject.name + " requires a ConstantID script component!", fromParameters[i].gameObject);
											}
											externalParameters[i].SetValue (fromParameters[i].gameObject, idToSend);
										}
									}
									else
									{
										externalParameters[i].SetValue (fromParameters[i].intValue);
									}
								}
								else if (fromParameters[i].gameObject)
								{
									int idToSend = 0;
									if (fromParameters[i].gameObject && fromParameters[i].gameObject.GetComponent <ConstantID>())
									{
										idToSend = fromParameters[i].gameObject.GetComponent <ConstantID>().constantID;
									}
									else
									{
										ACDebug.LogWarning (fromParameters[i].gameObject.name + " requires a ConstantID script component!", fromParameters[i].gameObject);
									}
									externalParameters[i].SetValue (fromParameters[i].gameObject, idToSend);
								}
								else
								{
									externalParameters[i].SetValue (fromParameters[i].intValue);
								}
							}
							else if (fromParameters[i].gameObject)
							{
								externalParameters[i].SetValue (fromParameters[i].gameObject);
							}
							else
							{
								externalParameters[i].SetValue (fromParameters[i].intValue);
							}
							break;

						default:
							externalParameters[i].SetValue (fromParameters[i].intValue);
							break;
					}
				}
			}
		}


		/**
		 * <summary>Syncronises two lists of parameters so that their sizes and IDs match</summary>
		 * <param name = "externalParameters">The parameters that will be updated by the component</param>
		 * <param name = "guiData">Data about the parameters to get the values from</param>
		 * <returns>Updated data about the parameters to get the values from</param>
		 */
		public static GUIData SyncLists (List<ActionParameter> externalParameters, GUIData originalGUIData)
		{
			GUIData guiData = new GUIData (originalGUIData); // Fixes issue where values are reset when Object Selection window opens

			List<ActionParameter> newLocalParameters = new List<ActionParameter>();
			List<int> newParameterIDs = new List<int>();

			foreach (ActionParameter externalParameter in externalParameters)
			{
				bool foundMatch = false;
				for (int i=0; i<guiData.fromParameters.Count; i++)
				{
					if (!foundMatch && guiData.fromParameters[i].ID == externalParameter.ID)
					{
						newLocalParameters.Add (new ActionParameter (guiData.fromParameters[i], true));
						guiData.fromParameters.RemoveAt (i);

						if (guiData.parameterIDs != null && i < guiData.parameterIDs.Count)
						{
							newParameterIDs.Add (guiData.parameterIDs[i]);
							guiData.parameterIDs.RemoveAt (i);
						}
						else
						{
							newParameterIDs.Add (-1);
						}

						foundMatch = true;
					}
				}

				if (!foundMatch)
				{
					newLocalParameters.Add (new ActionParameter (externalParameter, true));
					newParameterIDs.Add (-1);
				}
			}

			return new GUIData (newLocalParameters, newParameterIDs);
		}

		#endregion


		/** A data container for a list parameters used to update another list */
		[System.Serializable]
		public struct GUIData
		{

			/** A list of parameters used to set the values of another with */
			public List<ActionParameter> fromParameters;
			/** A list of parameter IDs used for reference */
			public List<int> parameterIDs;


			/** The default constructor */
			public GUIData (List<ActionParameter> _fromParameters, List<int> _parameterIDs)
			{
				fromParameters = new List<ActionParameter> ();
				if (_fromParameters != null)
				{
					foreach (ActionParameter param in _fromParameters)
					{
						fromParameters.Add (new ActionParameter (param, true));
					}
				}

				parameterIDs = new List<int>();
				if (_parameterIDs != null)
				{
					foreach (int ID in _parameterIDs)
					{
						parameterIDs.Add (ID);
					}
				}
			}


			public GUIData (GUIData guiData)
			{
				fromParameters = new List<ActionParameter> ();
				if (guiData.fromParameters != null)
				{
					foreach (ActionParameter param in guiData.fromParameters)
					{
						fromParameters.Add (new ActionParameter (param, true));
					}
				}

				parameterIDs = new List<int>();
				if (guiData.parameterIDs != null)
				{
					foreach (int ID in guiData.parameterIDs)
					{
						parameterIDs.Add (ID);
					}
				}
			}


			#if UNITY_EDITOR

			public bool ReferencesObjectOrID (GameObject gameObject, int id)
			{
				foreach (ActionParameter fromParameter in fromParameters)
				{
					if (fromParameter.gameObject && fromParameter.gameObject == gameObject) return true;
					if (fromParameter.intValue == id) return true;
				}
				return false;
			}

			#endif

		}


		#if UNITY_EDITOR

		public static GUIData SetParametersGUI (List<ActionParameter> externalParameters, bool isAssetFile, GUIData guiData, List<ActionParameter> ownParameters = null)
		{
			guiData = SyncLists (externalParameters, guiData);

			CustomGUILayout.BeginVertical ();
			for (int i=0; i<externalParameters.Count; i++)
			{
				string label = externalParameters[i].label;
				string tooltip = externalParameters[i].description;
				int linkedID = (i < guiData.parameterIDs.Count)
								? guiData.parameterIDs[i]
								: -1;

				guiData.fromParameters[i].parameterType = externalParameters[i].parameterType;

				switch (externalParameters[i].parameterType)
				{
					case ParameterType.GameObject:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.GameObject);
							if (linkedID < 0)
							{
								if (isAssetFile)
								{
									guiData.fromParameters[i].gameObject = (GameObject) CustomGUILayout.ObjectField <GameObject> (label + ":", guiData.fromParameters[i].gameObject, true, string.Empty, tooltip);
									if (guiData.fromParameters[i].gameObject)
									{
										if (!UnityVersionHandler.IsPrefabFile (guiData.fromParameters[i].gameObject))
										{
											guiData.fromParameters[i].intValue = Action.FieldToID (guiData.fromParameters[i].gameObject, guiData.fromParameters[i].intValue, false, isAssetFile);
											guiData.fromParameters[i].gameObject = Action.IDToField (guiData.fromParameters[i].gameObject, guiData.fromParameters[i].intValue, true, false, isAssetFile);
											guiData.fromParameters[i].gameObjectParameterReferences = GameObjectParameterReferences.ReferenceSceneInstance;
										}
										else
										{
											// A prefab, ask if we want to affect the prefab or the scene-based instance?
											guiData.fromParameters[i].gameObjectParameterReferences = (GameObjectParameterReferences) EditorGUILayout.EnumPopup ("GameObject parameter:", guiData.fromParameters[i].gameObjectParameterReferences);
										}
									}
									else
									{
										guiData.fromParameters[i].intValue = CustomGUILayout.IntField (label + " (ID #):", guiData.fromParameters[i].intValue, string.Empty, tooltip);
									}
								}
								else
								{
									// Gameobject
									guiData.fromParameters[i].gameObject = (GameObject) CustomGUILayout.ObjectField <GameObject> (label + ":", guiData.fromParameters[i].gameObject, true, string.Empty, tooltip);
									guiData.fromParameters[i].intValue = 0;
									if (guiData.fromParameters[i].gameObject && guiData.fromParameters[i].gameObject.GetComponent <ConstantID>() == null)
									{
										UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (guiData.fromParameters[i].gameObject);
									}
								}
							}
						}
						break;

					case ParameterType.UnityObject:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.UnityObject);
							if (linkedID < 0)
							{
								guiData.fromParameters[i].objectValue = (Object) CustomGUILayout.ObjectField <Object> (label + ":", guiData.fromParameters[i].objectValue, true, string.Empty, tooltip);
							}
						}
						break;

					case ParameterType.GlobalVariable:
						{
							if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
							{
								linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.GlobalVariable, -1, tooltip);
								if (linkedID < 0)
								{
									VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
									guiData.fromParameters[i].intValue = ActionRunActionList.ShowVarSelectorGUI (label + ":", variablesManager.vars, guiData.fromParameters[i].intValue, tooltip);
								}	
							}
							else
							{
								EditorGUILayout.HelpBox ("A Variables Manager is required to pass Global Variables.", MessageType.Warning);
							}
						}
						break;

					case ParameterType.InventoryItem:
						{
							if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
							{
								linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.InventoryItem, -1, tooltip);
								if (linkedID < 0)
								{
									InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
									guiData.fromParameters[i].intValue = ActionRunActionList.ShowInvItemSelectorGUI (label + ":", inventoryManager.items, guiData.fromParameters[i].intValue, tooltip);
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("An Inventory Manager is required to pass Inventory items.", MessageType.Warning);
							}
						}
						break;

					case ParameterType.Document:
						{
							if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
							{
								linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.Document, -1, tooltip);
								if (linkedID < 0)
								{
									InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
									guiData.fromParameters[i].intValue = ActionRunActionList.ShowDocumentSelectorGUI (label + ":", inventoryManager.documents, guiData.fromParameters[i].intValue, tooltip);
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("An Inventory Manager is required to pass Documents.", MessageType.Warning);
							}
						}
						break;

					case ParameterType.LocalVariable:
						{
							if (KickStarter.localVariables)
							{
								linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.LocalVariable, -1, tooltip);
								if (linkedID < 0)
								{
									guiData.fromParameters[i].intValue = ActionRunActionList.ShowVarSelectorGUI (label + ":", KickStarter.localVariables.localVars, guiData.fromParameters[i].intValue, tooltip);
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("A GameEngine prefab is required to pass Local Variables.", MessageType.Warning);
							}
						}
						break;

					case ParameterType.String:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.String, -1, tooltip);
							if (linkedID < 0)
							{
								guiData.fromParameters[i].stringValue = CustomGUILayout.TextArea (label, guiData.fromParameters[i].stringValue, string.Empty, tooltip);
							}
						}
						break;

					case ParameterType.Float:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.Float, -1, tooltip);
							if (linkedID < 0)
							{
								guiData.fromParameters[i].floatValue = CustomGUILayout.FloatField (label + ":", guiData.fromParameters[i].floatValue, string.Empty, tooltip);
							}
						}
						break;

					case ParameterType.Integer:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.Integer, -1, tooltip);
							if (linkedID < 0)
							{
								guiData.fromParameters[i].intValue = CustomGUILayout.IntField (label + ":", guiData.fromParameters[i].intValue, string.Empty, tooltip);
							}
						}
						break;

					case ParameterType.Vector3:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.Vector3, -1, tooltip);
							if (linkedID < 0)
							{
								guiData.fromParameters[i].vector3Value = CustomGUILayout.Vector3Field (label + ":", guiData.fromParameters[i].vector3Value, string.Empty, tooltip);
							}
						}
						break;

					case ParameterType.Boolean:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.Boolean, -1, tooltip);
							if (linkedID < 0)
							{
								BoolValue boolValue = BoolValue.False;
								if (guiData.fromParameters[i].intValue == 1)
								{
									boolValue = BoolValue.True;
								}

								boolValue = (BoolValue) CustomGUILayout.EnumPopup (label + ":", boolValue, string.Empty, tooltip);

								if (boolValue == BoolValue.True)
								{
									guiData.fromParameters[i].intValue = 1;
								}
								else
								{
									guiData.fromParameters[i].intValue = 0;
								}
							}
						}
						break;

					case ParameterType.ComponentVariable:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.ComponentVariable);
							if (linkedID < 0)
							{
								guiData.fromParameters[i].variables = (Variables) EditorGUILayout.ObjectField ("'" + label + "' component:", guiData.fromParameters[i].variables, typeof (Variables), true);
								guiData.fromParameters[i].constantID = FieldToID<Variables> (isAssetFile, guiData.fromParameters[i].variables, guiData.fromParameters[i].constantID);
								guiData.fromParameters[i].variables = IDToField<Variables> (isAssetFile, guiData.fromParameters[i].variables, guiData.fromParameters[i].constantID, false);
								if (guiData.fromParameters[i].variables != null)
								{
									guiData.fromParameters[i].intValue = ActionRunActionList.ShowVarSelectorGUI (label + ":", guiData.fromParameters[i].variables.vars, guiData.fromParameters[i].intValue);
								}
							}
						}
						break;

					case ParameterType.PopUp:
						{
							linkedID = Action.ChooseParameterGUI (label + ":", ownParameters, linkedID, ParameterType.PopUp, -1, tooltip);
							if (linkedID < 0)
							{
								if (guiData.fromParameters[i].intValue < 0)
								{
									guiData.fromParameters[i].intValue = 0;
								}

								PopUpLabelData popUpLabelData = KickStarter.variablesManager.GetPopUpLabelData (guiData.fromParameters[i].popUpID);
								if (popUpLabelData != null)
								{
									string[] labels = popUpLabelData.GenerateEditorPopUpLabels ();
									if (guiData.fromParameters[i].intValue >= labels.Length)
									{
										if (labels.Length == 0)
										{
											EditorGUILayout.HelpBox ("The PopUp parameter " + guiData.fromParameters[i].label + " has no labels defined.", MessageType.Warning);
											break;
										}
										guiData.fromParameters[i].intValue = labels.Length - 1;
										Debug.LogWarning ("Value for PopUp parameter " + guiData.fromParameters[i].label + " was reduced because it exceeded the maximum value.");
									}
									guiData.fromParameters[i].intValue = EditorGUILayout.Popup (label + ":", guiData.fromParameters[i].intValue, labels);
								}
								else
								{
									guiData.fromParameters[i].intValue = EditorGUILayout.IntField (label + " index:", guiData.fromParameters[i].intValue);
								}
							}
						}
						break;

					default:
						break;
				}

				if (i < guiData.parameterIDs.Count)
				{
					guiData.parameterIDs[i] = linkedID;
				}

				if (i < externalParameters.Count - 1)
				{
					EditorGUILayout.Space ();
				}
			}
			CustomGUILayout.EndVertical ();

			return guiData;
		}


		protected static int FieldToID<T> (bool isAssetFile, T field, int _constantID, bool alwaysAssign = false) where T : Behaviour
		{
			if (field != null)
			{
				if (alwaysAssign || isAssetFile || (!isAssetFile && !field.gameObject.activeInHierarchy))
				{
					if (field.GetComponent<ConstantID> ())
					{
						if (!field.gameObject.activeInHierarchy && field.GetComponent<ConstantID> ().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject<ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent<ConstantID> ().constantID;
					}
					else if (field.GetComponent<Player> () == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject<ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		protected static T IDToField<T> (bool isAssetFile, T field, int _constantID, bool moreInfo) where T : Behaviour
		{
			if (isAssetFile || (!isAssetFile && (field == null || !field.gameObject.activeInHierarchy)))
			{
				T newField = field;
				if (_constantID != 0)
				{
					newField = ConstantID.GetComponent<T> (_constantID);
					if (field && field.GetComponent<ConstantID> () != null && field.GetComponent<ConstantID> ().constantID == _constantID)
					{ }
					else if (newField && !Application.isPlaying)
					{
						field = newField;
					}

					CustomGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (!Application.isPlaying && GUILayout.Button ("Locate", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();

					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					CustomGUILayout.EndVertical ();
				}
			}
			return field;
		}


		protected void ShowParametersGUI (List<ActionParameter> externalParameters, bool isAssetFile, bool runMultipleTimes = false)
		{
			if (!runMultipleTimes || successiveGUIData == null)
			{
				successiveGUIData = ResizeGUIDataArray (successiveGUIData, 0);
			}

			if (!runMultipleTimes)
			{
				EditorGUILayout.Space ();
				initialGUIData = SetParametersGUI (externalParameters, isAssetFile, initialGUIData);
			}

			if (runMultipleTimes)
			{
				int numTimes = successiveGUIData.Length + 1;
				numTimes = EditorGUILayout.DelayedIntField ("# of times to run:", numTimes);
				numTimes = Mathf.Clamp (numTimes, 1, 20);
				if (numTimes != (successiveGUIData.Length + 1))
				{
					successiveGUIData = ResizeGUIDataArray (successiveGUIData, numTimes-1);
				}

				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Configuration #1:", CustomStyles.toggleHeader);
				if (successiveGUIData.Length > 0 && GUILayout.Button (string.Empty, CustomStyles.IconCog))
				{
					SideMenu (0, successiveGUIData.Length+1);
				}
				EditorGUILayout.EndHorizontal ();

				initialGUIData = SetParametersGUI (externalParameters, isAssetFile, initialGUIData);

				for (int i=0; i<successiveGUIData.Length; i++)
				{
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Configration #" + (i+2).ToString () + ":", CustomStyles.toggleHeader);
					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (i+1, successiveGUIData.Length+1);
					}
					EditorGUILayout.EndHorizontal ();

					successiveGUIData[i] = SetParametersGUI (externalParameters, isAssetFile, successiveGUIData[i]);
				}
			}
		}


		private int sideIndex;
		private void SideMenu (int index, int total)
		{
			sideIndex = index;

			GenericMenu menu = new GenericMenu ();

			if (total > 1)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
				menu.AddSeparator (string.Empty);
			}

			if (index > 0)
			{
				if (index > 1)
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				}
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (index < total-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				if (index < total-2)
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
				}
			}
			
			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			if (sideIndex >= 0)
			{
				GUIData tempData = (sideIndex > 0)
									? new GUIData (successiveGUIData[sideIndex-1])
									: new GUIData (initialGUIData);

				switch (obj.ToString ())
				{
					case "Delete":
						Undo.RecordObject (this, "Delete configuration");
						for (int i=sideIndex; i<successiveGUIData.Length; i++)
						{
							if (i == 0)
							{
								initialGUIData = new GUIData (successiveGUIData[0]);
							}
							else
							{
								successiveGUIData[i-1] = new GUIData (successiveGUIData[i]);
							}
						}
						successiveGUIData = ResizeGUIDataArray (successiveGUIData, successiveGUIData.Length-1);
						break;

					case "Move up":
						Undo.RecordObject (this, "Move configuration up");
						if (sideIndex == 1)
						{
							successiveGUIData[sideIndex-1] = new GUIData (initialGUIData);
							initialGUIData = tempData;
						}
						else
						{
							successiveGUIData[sideIndex-1] = new GUIData (successiveGUIData[sideIndex-2]);
							successiveGUIData[sideIndex-2] = tempData;
						}
						break;
						
					case "Move down":
						Undo.RecordObject (this, "Move configuration down");
						if (sideIndex == 0)
						{
							initialGUIData = successiveGUIData[sideIndex];
							successiveGUIData[sideIndex] = tempData;
						}
						else
						{
							successiveGUIData[sideIndex-1] = new GUIData (successiveGUIData[sideIndex]);
							successiveGUIData[sideIndex] = tempData;
						}
						break;

					case "Move to top":
						Undo.RecordObject (this, "Move configuration to top");
						for (int i=sideIndex-1; i>=0; i--)
						{
							if (i == 0)
							{
								successiveGUIData[i] = new GUIData (initialGUIData);
							}
							else
							{
								successiveGUIData[i] = new GUIData (successiveGUIData[i-1]);
							}
						}
						initialGUIData = tempData;
						break;
					
					case "Move to bottom":
						Undo.RecordObject (this, "Move configuration to bottom");
						for (int i=sideIndex; i<successiveGUIData.Length; i++)
						{
							if (i == 0)
							{
								initialGUIData = new GUIData (successiveGUIData[i]);
							}
							else
							{
								successiveGUIData[i-1] = new GUIData (successiveGUIData[i]);
							}
						}
						successiveGUIData[successiveGUIData.Length-1] = tempData;
						break;
				}
			}
			
			sideIndex = -1;

			UnityVersionHandler.CustomSetDirty (this, true);
		}


		private static GUIData[] ResizeGUIDataArray (GUIData[] originalDataArray, int newLength)
		{
			GUIData[] newArray = new GUIData[newLength];
			for (int i=0; i<newLength; i++)
			{
				newArray[i] = new GUIData (new List<ActionParameter>(), null);

				if (originalDataArray != null && i < originalDataArray.Length)
				{
					// Transfer values
					List<ActionParameter> oldFromParameters = originalDataArray[i].fromParameters;
					foreach (ActionParameter oldParam in oldFromParameters)
					{
						newArray[i].fromParameters.Add (new ActionParameter (oldParam, true));
					}
				}
			}

			return newArray;
		}


		public bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (initialGUIData.ReferencesObjectOrID (_gameObject, id)) return true;
			if (successiveGUIData != null)
			{
				foreach (GUIData guiData in successiveGUIData)
				{
					if (guiData.ReferencesObjectOrID (_gameObject, id)) return true;
				}
			}
			return false;
		}
		#endif

	}

}