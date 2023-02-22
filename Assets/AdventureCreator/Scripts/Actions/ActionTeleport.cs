/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionTeleport.cs"
 * 
 *	This action moves an object to a specified GameObject's position.
 *	Markers are helpful in this regard.
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
	public class ActionTeleport : Action
	{

		public int obToMoveParameterID = -1;
		public int obToMoveID = 0;
		public GameObject obToMove;
		protected GameObject runtimeObToMove;

		public int markerParameterID = -1;
		public int markerID = 0;
		public Marker teleporter;
		protected Marker runtimeTeleporter;

		public GameObject relativeGameObject = null;
		public int relativeGameObjectID = 0;
		public int relativeGameObjectParameterID = -1;

		public PositionRelativeTo positionRelativeTo = PositionRelativeTo.Nothing;

		public int relativeVectorParameterID = -1;
		public Vector3 relativeVector;

		public int vectorVarParameterID = -1;
		public int vectorVarID;
		public VariableLocation variableLocation = VariableLocation.Global;

		public bool recalculateActivePathFind = false;
		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;
		public bool snapCamera;

		public bool copyRotation;
		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;
		protected LocalVariables localVariables;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Teleport"; }}
		public override string Description { get { return "Moves a GameObject to a Marker instantly. Can also copy the Marker's rotation. The final position can optionally be made relative to the active camera, or the player. For example, if the Marker's position is (0, 0, 1) and Positon relative to is set to Relative To Active Camera, then the object will be teleported in front of the camera."; }}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeObToMove = AssignFile (parameters, obToMoveParameterID, obToMoveID, obToMove);
			runtimeTeleporter = AssignFile <Marker> (parameters, markerParameterID, markerID, teleporter);
			relativeGameObject = AssignFile (parameters, relativeGameObjectParameterID, relativeGameObjectID, relativeGameObject);
			
			relativeVector = AssignVector3 (parameters, relativeVectorParameterID, relativeVector);

			if (isPlayer)
			{
				Player _player = AssignPlayer (playerID, parameters, playerParameterID);
				if (_player) runtimeObToMove = _player.gameObject;
				else runtimeObToMove = null;
			}

			runtimeVariable = null;
			if (positionRelativeTo == PositionRelativeTo.VectorVariable)
			{
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
			if (runtimeObToMove != null)
			{
				Vector3 position = (runtimeTeleporter) ? runtimeTeleporter.Position : runtimeObToMove.transform.position;
				Quaternion rotation = (runtimeTeleporter) ? runtimeTeleporter.Rotation : runtimeObToMove.transform.rotation;

				switch (positionRelativeTo)
				{
					case PositionRelativeTo.RelativeToActiveCamera:
						{
							Transform mainCam = KickStarter.mainCamera.transform;

							float right = position.x;
							float up = position.y;
							float forward = position.z;

							position = mainCam.position + (mainCam.forward * forward) + (mainCam.right * right) + (mainCam.up * up);
							rotation.eulerAngles += mainCam.transform.rotation.eulerAngles;
						}
						break;

					case PositionRelativeTo.RelativeToPlayer:
						if (!isPlayer && KickStarter.player)
						{
							Transform playerTransform = KickStarter.player.transform;

							float right = position.x;
							float up = position.y;
							float forward = position.z;

							position = playerTransform.position + (playerTransform.forward * forward) + (playerTransform.right * right) + (playerTransform.up * up);
							rotation.eulerAngles += playerTransform.rotation.eulerAngles;
						}
						break;

					case PositionRelativeTo.RelativeToGameObject:
						if (relativeGameObject != null)
						{
							Transform relativeTransform = relativeGameObject.transform;

							float right = position.x;
							float up = position.y;
							float forward = position.z;

							position = relativeTransform.position + (relativeTransform.forward * forward) + (relativeTransform.right * right) + (relativeTransform.up * up);
							rotation.eulerAngles += relativeTransform.rotation.eulerAngles;
						}
						break;

					case PositionRelativeTo.EnteredValue:
						position += relativeVector;
						break;

					case PositionRelativeTo.VectorVariable:
						if (runtimeVariable != null)
						{
							position += runtimeVariable.Vector3Value;
						}
						break;

					default:
						break;
				}

				Char charToMove = runtimeObToMove.GetComponent <Char>();
				if (copyRotation)
				{
					runtimeObToMove.transform.rotation = rotation;

					if (charToMove != null)
					{
						// Is a character, so set the lookDirection, otherwise will revert back to old rotation
						charToMove.SetLookDirection (runtimeTeleporter.ForwardDirection, true);
						charToMove.Halt ();
					}
				}

				if (charToMove != null)
				{
					charToMove.Teleport (position, recalculateActivePathFind);
				}
				else
				{
					runtimeObToMove.transform.position = position;
				}

				if (isPlayer && snapCamera)
				{
					if (KickStarter.mainCamera != null && KickStarter.mainCamera.attachedCamera != null && KickStarter.mainCamera.attachedCamera.targetIsPlayer)
					{
						KickStarter.mainCamera.attachedCamera.MoveCameraInstant ();
					}
				}

				KickStarter.eventManager.Call_OnTeleport (runtimeObToMove);
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerParameterID = ChooseParameterGUI ("Player ID:", parameters, playerParameterID, ParameterType.Integer);
					if (playerParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				obToMoveParameterID = Action.ChooseParameterGUI ("Object to move:", parameters, obToMoveParameterID, ParameterType.GameObject);
				if (obToMoveParameterID >= 0)
				{
					obToMoveID = 0;
					obToMove = null;
				}
				else
				{
					obToMove = (GameObject) EditorGUILayout.ObjectField ("Object to move:", obToMove, typeof(GameObject), true);
					
					obToMoveID = FieldToID (obToMove, obToMoveID);
					obToMove = IDToField (obToMove, obToMoveID, false);
				}
			}

			markerParameterID = Action.ChooseParameterGUI ("Teleport to:", parameters, markerParameterID, ParameterType.GameObject);
			if (markerParameterID >= 0)
			{
				markerID = 0;
				teleporter = null;
			}
			else
			{
				teleporter = (Marker) EditorGUILayout.ObjectField ("Teleport to:", teleporter, typeof (Marker), true);
				
				markerID = FieldToID <Marker> (teleporter, markerID);
				teleporter = IDToField <Marker> (teleporter, markerID, false);
			}
			
			positionRelativeTo = (PositionRelativeTo) EditorGUILayout.EnumPopup ("Position relative to:", positionRelativeTo);

			if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
			{
				relativeGameObjectParameterID = Action.ChooseParameterGUI ("Relative GameObject:", parameters, relativeGameObjectParameterID, ParameterType.GameObject);
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
				relativeVectorParameterID = Action.ChooseParameterGUI ("Value:", parameters, relativeVectorParameterID, ParameterType.Vector3);
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
						vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.GlobalVariable);
						if (vectorVarParameterID < 0)
						{
							vectorVarID = AdvGame.GlobalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
						}
						break;

					case VariableLocation.Local:
						if (!isAssetFile)
						{
							vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.LocalVariable);
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
						vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.ComponentVariable);
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

			copyRotation = EditorGUILayout.Toggle ("Copy rotation?", copyRotation);

			if (isPlayer)
			{
				snapCamera = EditorGUILayout.Toggle ("Teleport active camera too?", snapCamera);
			}

			if (isPlayer || (obToMove != null && obToMove.GetComponent <Char>()))
			{
				recalculateActivePathFind = EditorGUILayout.Toggle ("Recalculate pathfinding?", recalculateActivePathFind);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo && obToMove != null)
			{
				if (!isPlayer)
				{
					Char charToMove = obToMove.GetComponent<Char>();
					if (charToMove != null && !charToMove.IsPlayer)
					{
						AddSaveScript <RememberNPC> (obToMove);
					}
					else
					{
						AddSaveScript<RememberTransform> (obToMove);
					}
				}
			}

			if (!isPlayer)
			{
				AssignConstantID (obToMove, obToMoveID, obToMoveParameterID);
			}
			AssignConstantID <Marker> (teleporter, markerID, markerParameterID);

			if (positionRelativeTo == PositionRelativeTo.VectorVariable &&
				variableLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, vectorVarParameterID);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (teleporter != null)
			{
				if (obToMove != null)
				{
					return obToMove.name + " to " + teleporter.name;
				}
				else if (isPlayer)
				{
					return "Player to " + teleporter.name;
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isPlayer && obToMoveParameterID < 0)
			{
				if (obToMove && obToMove == gameObject) return true;
				if (obToMoveID == id && id != 0) return true;
			}
			if (isPlayer && gameObject && gameObject.GetComponent <Player>()) return true;
			if (relativeGameObjectParameterID < 0 && positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
			{
				if (relativeGameObject && relativeGameObject == gameObject) return true;
				if (relativeGameObjectID == id && id != 0) return true;
			}
			if (positionRelativeTo == PositionRelativeTo.VectorVariable && variableLocation == VariableLocation.Component && vectorVarParameterID < 0)
			{
				if (variables && variables.gameObject == gameObject) return true;
				if (variablesConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Teleport' Action</summary>
		 * <param name = "objectToMove">The GameObject to teleport</param>
		 * <param name = "markerToTeleportTo">The Marker to teleport to</param>
		 * <param name = "copyRotation">If True, the teleported object will copy the Marker's rotation</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTeleport CreateNew (GameObject objectToMove, Marker markerToTeleportTo, bool copyRotation = true)
		{
			ActionTeleport newAction = CreateNew<ActionTeleport> ();
			newAction.obToMove = objectToMove;
			newAction.TryAssignConstantID (newAction.obToMove, ref newAction.obToMoveID);
			newAction.teleporter = markerToTeleportTo;
			newAction.TryAssignConstantID (newAction.teleporter, ref newAction.markerID);
			newAction.copyRotation = copyRotation;
			return newAction;
		}

	}

}