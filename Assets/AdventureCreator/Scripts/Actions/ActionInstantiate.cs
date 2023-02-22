/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInstantiate.cs"
 * 
 *	This Action spawns prefabs and deletes
 *  objects from the scene
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

	[System.Serializable]
	public class ActionInstantiate : Action
	{
		
		public GameObject gameObject;
		public int parameterID = -1;
		public int constantID = 0; 

		public GameObject replaceGameObject;
		public int replaceParameterID = -1;
		public int replaceConstantID = 0;

		public GameObject relativeGameObject = null;
		public int relativeGameObjectID = 0;
		public int relativeGameObjectParameterID = -1;

		public int relativeVectorParameterID = -1;
		public Vector3 relativeVector = Vector3.zero;

		public int vectorVarParameterID = -1;
		public int vectorVarID;
		public VariableLocation variableLocation = VariableLocation.Global;

		public InvAction invAction;
		public PositionRelativeTo positionRelativeTo = PositionRelativeTo.Nothing;
		protected GameObject _gameObject;

		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;
		protected LocalVariables localVariables;

		public int spawnedObjectParameterID = -1;
		protected ActionParameter runtimeSpawnedObjectParameter;

		#if AddressableIsPresent
		public bool referenceByAddressable = false;
		public string addressableName;
		public int addressableNameParameterID = -1;
		protected bool isAwaitingAddressable = false;
		#endif

		#if UNITY_EDITOR
		ParameterType[] parameterTypes;
		#endif

		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Add or remove"; }}
		public override string Description { get { return "Instantiates or deletes GameObjects within the current scene. To ensure this works with save games correctly, place any prefabs to be added in a Resources asset folder."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			GameObject invPrefab = null;
			ActionParameter parameter = GetParameterWithID (parameters, parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.InventoryItem)
			{
				InvItem invItem = KickStarter.inventoryManager.GetItem (parameter.intValue);
				if (invItem != null)
				{
					invPrefab = invItem.linkedPrefab;
				}
			}

			#if AddressableIsPresent
			addressableName = AssignString (parameters, addressableNameParameterID, addressableName);
			isAwaitingAddressable = false;
			#endif

			switch (invAction)
			{
				case InvAction.Add:
				case InvAction.Replace:
					if (invPrefab != null)
					{
						_gameObject = invPrefab;
					}
					else
					{
						_gameObject = AssignFile (parameters, parameterID, 0, gameObject);
					}

					if (invAction == InvAction.Replace)
					{
						replaceGameObject = AssignFile (parameters, replaceParameterID, replaceConstantID, replaceGameObject);
					}
					else if (invAction == InvAction.Add)
					{
						relativeGameObject = AssignFile (parameters, relativeGameObjectParameterID, relativeGameObjectID, relativeGameObject);
					}
					break;

				case InvAction.Remove:
					if (invPrefab != null)
					{
						ConstantID invPrefabConstantID = invPrefab.GetComponent<ConstantID> ();
						if (invPrefabConstantID != null && invPrefabConstantID.constantID != 0)
						{
							_gameObject = AssignFile (invPrefabConstantID.constantID, _gameObject);
						}
						else
						{
							LogWarning ("Cannot locate scene instance of prefab " + invPrefab + " as it has no Constant ID number");
						}
					}
					else
					{
						_gameObject = AssignFile (parameters, parameterID, constantID, gameObject);
					}

					if (_gameObject != null && !_gameObject.activeInHierarchy)
					{
						_gameObject = null;
					}
					break;

				default:
					break;
			}
		
			relativeVector = AssignVector3 (parameters, relativeVectorParameterID, relativeVector);

			if (invAction == InvAction.Add && positionRelativeTo == PositionRelativeTo.VectorVariable)
			{
				runtimeVariable = null;
				switch (variableLocation)
				{
					case VariableLocation.Global:
						vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);
						runtimeVariable = GlobalVariables.GetVariable (vectorVarID, true);
						break;

					case VariableLocation.Local:
						if (!isAssetFile)
						{
							vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);
							runtimeVariable = LocalVariables.GetVariable (vectorVarID, localVariables);
						}
						break;

					case VariableLocation.Component:
						Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
						if (runtimeVariables != null)
						{
							runtimeVariable = runtimeVariables.GetVariable (vectorVarID);
						}
						runtimeVariable = AssignVariable (parameters, vectorVarParameterID, runtimeVariable);
						break;
				}
			}

			runtimeSpawnedObjectParameter = null;
			if (invAction == InvAction.Add)
			{
				runtimeSpawnedObjectParameter = GetParameterWithID (parameters, spawnedObjectParameterID);
				if (runtimeSpawnedObjectParameter != null && runtimeSpawnedObjectParameter.parameterType != ParameterType.GameObject)
				{
					runtimeSpawnedObjectParameter = null;
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
		
		
		public override float Run ()
		{
			#if AddressableIsPresent
			if (isRunning)
			{
				if (isAwaitingAddressable)
				{
					return defaultPauseTime;
				}

				isRunning = false;
				return 0f;
			}
			#endif

			#if AddressableIsPresent
			if (_gameObject == null && !referenceByAddressable)
			#else
			if (_gameObject == null)
			#endif
			{
				return 0f;
			}

			switch (invAction)
			{
				case InvAction.Add:
					#if AddressableIsPresent
					if (referenceByAddressable)
					{
						if (!string.IsNullOrEmpty (addressableName))
						{
							Addressables.InstantiateAsync (addressableName).Completed += OnCompleteAddGameObject;
							isAwaitingAddressable = true;
							isRunning = true;
							return defaultPauseTime;
						}
					}
					else
					#endif
					{
						InstantiateObject (_gameObject);
					}
					break;

				case InvAction.Remove:
					#if AddressableIsPresent
					if (!Addressables.ReleaseInstance (_gameObject))
					{
						KickStarter.sceneChanger.ScheduleForDeletion (_gameObject);
					}
					#else
					KickStarter.sceneChanger.ScheduleForDeletion (_gameObject);
					#endif
					break;

				case InvAction.Replace:
					{
						if (replaceGameObject == null)
						{
							LogWarning ("Cannot perform swap because the object to remove was not found in the scene.");
							return 0f;
						}

						Vector3 position = replaceGameObject.transform.position;
						Quaternion rotation = replaceGameObject.transform.rotation;

						GameObject oldOb = AssignFile (constantID, _gameObject);
						if (gameObject.activeInHierarchy || (oldOb != null && oldOb.activeInHierarchy))
						{
							Log (_gameObject.name + " won't be instantiated, as it is already present in the scene.", _gameObject);
							return 0f;
						}

						KickStarter.sceneChanger.ScheduleForDeletion (replaceGameObject);

						GameObject newObject = Object.Instantiate (_gameObject, position, rotation);
						newObject.name = _gameObject.name;
						KickStarter.stateHandler.IgnoreNavMeshCollisions ();
					}
					break;

				default:
					break;
			}	

			return 0f;
		}


		#if AddressableIsPresent

		private void OnCompleteAddGameObject (AsyncOperationHandle<GameObject> obj)
		{
			isAwaitingAddressable = false;
			InstantiateObject (obj.Result, true);
		}

		#endif


		private void InstantiateObject (GameObject newOb, bool wasSpawnedByAddressable = false)
		{
			if (newOb == null) return;

			GameObject oldOb = AssignFile (constantID, newOb);

			if (newOb.activeInHierarchy || (oldOb != null && oldOb.activeInHierarchy))
			{
				RememberTransform rememberTransform = oldOb.GetComponent<RememberTransform> ();

				if (rememberTransform && rememberTransform.saveScenePresence)
				{
					if (rememberTransform.linkedPrefabID != 0)
					{
						// Bypass this check
					}
					else
					{
						if (wasSpawnedByAddressable)
						{
							#if AddressableIsPresent
							Addressables.ReleaseInstance (newOb);
							#endif
						}
						LogWarning (newOb.name + " cannot be instantiated, as it is already present in the scene.  To allow for multiple instances to be spawned, assign a non-zero value in its Remember Transform component's 'Linked prefab ConstantID' field.", _gameObject);
						return;
					}
				}
			}

			Vector3 position = newOb.transform.position;
			Quaternion rotation = newOb.transform.rotation;

			if (positionRelativeTo != PositionRelativeTo.Nothing)
			{
				float forward = newOb.transform.position.z;
				float right = newOb.transform.position.x;
				float up = newOb.transform.position.y;

				if (positionRelativeTo == PositionRelativeTo.RelativeToActiveCamera)
				{
					Transform mainCam = KickStarter.mainCamera.transform;
					position = mainCam.position + (mainCam.forward * forward) + (mainCam.right * right) + (mainCam.up * up);
					rotation.eulerAngles += mainCam.transform.rotation.eulerAngles;
				}
				else if (positionRelativeTo == PositionRelativeTo.RelativeToPlayer)
				{
					if (KickStarter.player)
					{
						Transform playerTranform = KickStarter.player.transform;
						position = playerTranform.position + (playerTranform.forward * forward) + (playerTranform.right * right) + (playerTranform.up * up);
						rotation.eulerAngles += playerTranform.rotation.eulerAngles;
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
				{
					if (relativeGameObject != null)
					{
						Transform relativeTransform = relativeGameObject.transform;
						position = relativeTransform.position + (relativeTransform.forward * forward) + (relativeTransform.right * right) + (relativeTransform.up * up);
						rotation.eulerAngles += relativeTransform.rotation.eulerAngles;
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.EnteredValue)
				{
					position += relativeVector;
				}
				else if (positionRelativeTo == PositionRelativeTo.VectorVariable)
				{
					if (runtimeVariable != null)
					{
						position += runtimeVariable.Vector3Value;
					}
				}
			}

			GameObject newObject = wasSpawnedByAddressable ? newOb : Object.Instantiate (newOb, position, rotation);
			if (wasSpawnedByAddressable)
			{
				newOb.transform.SetPositionAndRotation (position, rotation);
			}
			else
			{
				newObject.name = newOb.name;
			}

			if (newObject.GetComponent<RememberTransform> ())
			{
				newObject.GetComponent<RememberTransform> ().OnSpawn ();
			}

			KickStarter.stateHandler.IgnoreNavMeshCollisions ();

			if (runtimeSpawnedObjectParameter != null)
			{
				runtimeSpawnedObjectParameter.SetValue (newObject);
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			invAction = (InvAction) EditorGUILayout.EnumPopup ("Method:", invAction);

			string _label = "Object to instantiate:";
			if (invAction == InvAction.Remove)
			{
				_label = "Object to delete:";
			}

			#if AddressableIsPresent
			if (invAction == InvAction.Replace)
			{
				referenceByAddressable = false;
			}
			else if (invAction == InvAction.Add)
			{
				referenceByAddressable = EditorGUILayout.Toggle ("Reference Addressable?", referenceByAddressable);
			}

			if (referenceByAddressable && invAction != InvAction.Remove)
			{
				addressableNameParameterID = ChooseParameterGUI ("Addressable name:", parameters, addressableNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (addressableNameParameterID < 0)
				{
					addressableName = EditorGUILayout.TextField ("Addressable name:", addressableName);
				}

				if (gameObject)
				{
					Log ("Clearing reference to GameObject '" + gameObject + "' to save memory.");
					gameObject = null;
				}
			}
			else
			#endif
			{
				if (parameterTypes == null || parameterTypes.Length == 0)
				{
					parameterTypes = new ParameterType[2] { ParameterType.GameObject, ParameterType.InventoryItem };
				}

				parameterID = ChooseParameterGUI (_label, parameters, parameterID, parameterTypes);
				if (parameterID >= 0)
				{
					constantID = 0;
					gameObject = null;
				}
				else
				{
					if (invAction == InvAction.Add)
					{
						gameObject = (GameObject) EditorGUILayout.ObjectField (_label, gameObject, typeof (GameObject), false);
						constantID = FieldToID (gameObject, constantID);
					}
					else
					{
						gameObject = (GameObject) EditorGUILayout.ObjectField (_label, gameObject, typeof (GameObject), true);

						constantID = FieldToID (gameObject, constantID);
						gameObject = IDToField (gameObject, constantID, false);
					}
				}
			}

			if (invAction == InvAction.Add)
			{
				positionRelativeTo = (PositionRelativeTo) EditorGUILayout.EnumPopup ("Position relative to:", positionRelativeTo);

				if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
				{
					relativeGameObjectParameterID = ChooseParameterGUI ("Relative GameObject:", parameters, relativeGameObjectParameterID, ParameterType.GameObject);
					if (relativeGameObjectParameterID >= 0)
					{
						relativeGameObjectID = 0;
						relativeGameObject = null;
					}
					else
					{
						relativeGameObject = (GameObject) EditorGUILayout.ObjectField ("Relative GameObject:", relativeGameObject, typeof (GameObject), true);
						
						relativeGameObjectID = FieldToID (relativeGameObject, relativeGameObjectID);
						relativeGameObject = IDToField (relativeGameObject, relativeGameObjectID, false);
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.EnteredValue)
				{
					relativeVectorParameterID = ChooseParameterGUI ("Value:", parameters, relativeVectorParameterID, ParameterType.Vector3);
					if (relativeVectorParameterID < 0)
					{
						relativeVector = EditorGUILayout.Vector3Field ("Value:", relativeVector);
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.VectorVariable)
				{
					variableLocation = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", variableLocation);

					switch (variableLocation)
					{
						case VariableLocation.Global:
							vectorVarParameterID = ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.GlobalVariable);
							if (vectorVarParameterID < 0)
							{
								vectorVarID = AdvGame.GlobalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
							}
							break;

						case VariableLocation.Local:
							if (!isAssetFile)
							{
								vectorVarParameterID = ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.LocalVariable);
								if (vectorVarParameterID < 0)
								{
									vectorVarID = AdvGame.LocalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
							}
							break;

						case VariableLocation.Component:
							vectorVarParameterID = ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.ComponentVariable);
							if (vectorVarParameterID >= 0)
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
									vectorVarID = AdvGame.ComponentVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3, variables);
								}
							}
							break;
					}
				}

				spawnedObjectParameterID = ChooseParameterGUI ("Send to parameter:", parameters, spawnedObjectParameterID, ParameterType.GameObject);
			}
			else if (invAction == InvAction.Replace)
			{
				EditorGUILayout.Space ();
				replaceParameterID = ChooseParameterGUI ("Object to delete:", parameters, replaceParameterID, ParameterType.GameObject);
				if (replaceParameterID >= 0)
				{
					replaceConstantID = 0;
					replaceGameObject = null;
				}
				else
				{
					replaceGameObject = (GameObject) EditorGUILayout.ObjectField ("Object to delete:", replaceGameObject, typeof (GameObject), true);
					
					replaceConstantID = FieldToID (replaceGameObject, replaceConstantID);
					replaceGameObject = IDToField (replaceGameObject, replaceConstantID, false);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberTransform> (replaceGameObject);
				AddSaveScript <RememberTransform> (gameObject);

				if (replaceGameObject != null && replaceGameObject.GetComponent <RememberTransform>())
				{
					replaceGameObject.GetComponent <RememberTransform>().saveScenePresence = true;
				}
				if (gameObject != null && gameObject.GetComponent <RememberTransform>())
				{
					gameObject.GetComponent <RememberTransform>().saveScenePresence = true;
				}
			}

			if (invAction == InvAction.Replace)
			{
				AssignConstantID (replaceGameObject, replaceConstantID, replaceParameterID);
			}
			else if (invAction == InvAction.Remove)
			{
				AssignConstantID (gameObject, constantID, parameterID);
			}

			if (invAction == InvAction.Add &&
				positionRelativeTo == PositionRelativeTo.VectorVariable &&
				variableLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, vectorVarParameterID);
			}
		}

		
		public override string SetLabel ()
		{
			string labelAdd = invAction.ToString ();
			if (gameObject != null)
			{
				labelAdd += " " + gameObject.name;
			}
			return labelAdd;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (gameObject && gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (invAction == InvAction.Add && positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
			{
				if (relativeGameObjectParameterID < 0)
				{
					if (relativeGameObject && relativeGameObject == _gameObject) return true;
					if (relativeGameObjectID == id) return true;
				}
			}
			if (invAction == InvAction.Replace)
			{
				if (replaceParameterID < 0)
				{
					if (replaceGameObject && replaceGameObject == _gameObject) return true;
					if (replaceConstantID == id) return true;
				}
			}
			if (invAction == InvAction.Add && positionRelativeTo == PositionRelativeTo.VectorVariable)
			{
				if (variableLocation == VariableLocation.Component && vectorVarParameterID < 0)
				{
					if (variables && variables.gameObject == _gameObject) return true;
					if (variablesConstantID == id) return true;
				}
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Add or remove' Action, set to spawn a new GameObject.</summary>
		 * <param name = "prefabToAdd">The prefab to spawn</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInstantiate CreateNew_Add (GameObject prefabToAdd)
		{
			ActionInstantiate newAction = CreateNew<ActionInstantiate> ();
			newAction.invAction = InvAction.Add;
			newAction.gameObject = prefabToAdd;
			newAction.TryAssignConstantID (newAction.gameObject, ref newAction.constantID);

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Add or remove' Action, set to remove a GameObject from the scene.</summary>
		 * <param name = "objectToRemove">The object to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInstantiate CreateNew_Remove (GameObject objectToRemove)
		{
			ActionInstantiate newAction = CreateNew<ActionInstantiate> ();
			newAction.invAction = InvAction.Remove;
			newAction.gameObject = objectToRemove;
			newAction.TryAssignConstantID (newAction.gameObject, ref newAction.constantID);

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Add or remove' Action, set to replace one GameObject with another</summary>
		 * <param name = "prefabToAdd">The prefab to spawn</param>
		 * <param name = "objectToRemove">The object to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInstantiate CreateNew_Replace (GameObject prefabToAdd, GameObject objectToRemove)
		{
			ActionInstantiate newAction = CreateNew<ActionInstantiate> ();
			newAction.invAction = InvAction.Replace;
			newAction.gameObject = prefabToAdd;
			newAction.TryAssignConstantID (newAction.gameObject, ref newAction.constantID);
			newAction.replaceGameObject = objectToRemove;
			newAction.TryAssignConstantID (newAction.replaceGameObject, ref newAction.replaceConstantID);

			return newAction;
		}
		
	}

}