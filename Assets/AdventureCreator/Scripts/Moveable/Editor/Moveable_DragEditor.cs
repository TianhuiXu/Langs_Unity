#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(Moveable_Drag))]
	public class Moveable_DragEditor : DragBaseEditor
	{

		public override void OnInspectorGUI ()
		{
			Moveable_Drag _target = (Moveable_Drag) target;
			GetReferences ();

			if (Application.isPlaying)
			{
				if (KickStarter.settingsManager && _target.gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer))
				{
					EditorGUILayout.HelpBox ("Current state: OFF", MessageType.Info);
				}
			}

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Movement settings:", EditorStyles.boldLabel);
			_target.maxSpeed = CustomGUILayout.FloatField ("Max speed:", _target.maxSpeed, string.Empty, "The maximum force magnitude that can be applied to itself");
			_target.playerMovementReductionFactor = CustomGUILayout.Slider ("Player motion reduction:", _target.playerMovementReductionFactor, 0f, 1f, string.Empty, "How much player movement is reduced by when the object is being dragged");
			_target.playerMovementInfluence = CustomGUILayout.FloatField ("Player motion influence:", _target.playerMovementInfluence, string.Empty, "The influence that player movement has on the drag force");
			_target.invertInput = CustomGUILayout.Toggle ("Invert input?", _target.invertInput, string.Empty, "If True, input vectors will be inverted");
			_target.offScreenRelease = (OffScreenRelease)CustomGUILayout.EnumPopup("Off-screen release:", _target.offScreenRelease, string.Empty, "What should cause the object to be automatically released upon leaving the screen");
			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();

			EditorGUILayout.LabelField ("Drag settings:", EditorStyles.boldLabel);
			_target.dragMode = (DragMode) CustomGUILayout.EnumPopup ("Drag mode:", _target.dragMode, string.Empty, "The way in which the object can be dragged");
			if (_target.dragMode == DragMode.LockToTrack)
			{
				_target.track = (DragTrack) CustomGUILayout.ObjectField <DragTrack> ("Track to stick to:", _target.track, true, string.Empty, "The DragTrack the object is locked to");
				
				if (_target.track != null && _target.track.UsesEndColliders && _target.GetComponent <SphereCollider>() == null)
				{
					EditorGUILayout.HelpBox ("For best results, ensure the first collider on this GameObject is a Sphere Collider covering the breath of the mesh.\r\nIt can be disabled if necessary, but will be used to set correct limit boundaries.", MessageType.Info);
				}
				
				_target.dragTrackDirection = (DragTrackDirection) CustomGUILayout.EnumPopup ("Drag direction:", _target.dragTrackDirection);

				if (_target.GetComponent<Rigidbody>())
				{
					_target.moveWithRigidbody = EditorGUILayout.Toggle ("Move with Rigidbody?", _target.moveWithRigidbody);
				}
				if (!_target.UsesRigidbody)
				{
					_target.simulatedMass = CustomGUILayout.Slider ("Simulated mass:", _target.simulatedMass, 0f, 2f, string.Empty, "The object's simulated mass, if not using a Rigidbody");
				}
				else
				{
					_target.applyGravity = CustomGUILayout.Toggle ("Apply gravity force?", _target.applyGravity, string.Empty, "If True, an additional gravitational force will be applied when not held or moving automatically");
				}

				_target.setOnStart = CustomGUILayout.ToggleLeft ("Set starting position?", _target.setOnStart, string.Empty, "If True, then the object will be placed at a specific point along the track when the game begins");
				if (_target.setOnStart)
				{
					_target.trackValueOnStart = CustomGUILayout.Slider ("Initial distance along:", _target.trackValueOnStart, 0f, 1f, string.Empty, "How far along its DragTrack that the object should be placed at when the game begins");
				}
				_target.retainOriginalTransform = CustomGUILayout.ToggleLeft ("Maintain original child transforms?", _target.retainOriginalTransform, string.Empty, "If True, then the position and rotation of all child objects will be maintained when the object is attached to the track");

				if (Application.isPlaying && _target.track != null)
				{
					EditorGUILayout.Space ();
					EditorGUILayout.LabelField ("Distance along: " + _target.GetPositionAlong ().ToString (), EditorStyles.miniLabel);
				}

				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("Interactions", EditorStyles.boldLabel);

				_target.actionListSource = (ActionListSource) CustomGUILayout.EnumPopup ("Actions source:", _target.actionListSource, "", "The source of the commands that are run when the object is interacted with");

				
			}
			else if (_target.dragMode == DragMode.MoveAlongPlane)
			{
				_target.alignMovement = (AlignDragMovement) CustomGUILayout.EnumPopup ("Align movement:", _target.alignMovement, "", "What movement is aligned to");
				if (_target.alignMovement == AlignDragMovement.AlignToPlane)
				{
					_target.plane = (Transform) CustomGUILayout.ObjectField <Transform> ("Movement plane:", _target.plane, true, "", "The plane to align movement to");
				}

				if (_target.GetComponent<Rigidbody> ())
				{
					_target.moveWithRigidbody = EditorGUILayout.Toggle ("Turn with Rigidbody?", _target.moveWithRigidbody);
					_target.noGravityWhenHeld = CustomGUILayout.Toggle ("Gravity set by 'held' state?", _target.noGravityWhenHeld, "", "If True, then gravity will be disabled on the object while it is held by the player");
				}
			}
			else if (_target.dragMode == DragMode.RotateOnly)
			{
				_target.rotationFactor = CustomGUILayout.FloatField ("Rotation factor:", _target.rotationFactor, "", "The speed by which the object can be rotated");
				_target.allowZooming = CustomGUILayout.Toggle ("Allow zooming?", _target.allowZooming, "", "If True, the object can be moved towards and away from the camera");
				if (_target.allowZooming)
				{
					_target.zoomSpeed = CustomGUILayout.FloatField ("Zoom speed:", _target.zoomSpeed, "", "The speed at which the object can be moved towards and away from the camera");
					_target.minZoom = CustomGUILayout.FloatField ("Closest distance:", _target.minZoom, "", "The minimum distance that there can be between the object and the camera");
					_target.maxZoom = CustomGUILayout.FloatField ("Farthest distance:", _target.maxZoom, "", "The maximum distance that there can be between the object and the camera");
				}

				if (_target.GetComponent<Rigidbody> ())
				{
					_target.moveWithRigidbody = EditorGUILayout.Toggle ("Turn with Rigidbody?", _target.moveWithRigidbody);
					_target.noGravityWhenHeld = CustomGUILayout.Toggle ("Gravity set by 'held' state?", _target.noGravityWhenHeld, "", "If True, then gravity will be disabled on the object while it is held by the player");
				}

				if (_target.GetComponent<Rigidbody>() == null || !_target.moveWithRigidbody)
				{
					_target.toruqeDamping = CustomGUILayout.FloatField ("Damping factor:", _target.toruqeDamping, "", "The damping to apply to player input when rotating.");
				}
			}

			if (_target.actionListSource == ActionListSource.InScene)
			{
				EditorGUILayout.BeginHorizontal ();
				if (_target.dragMode == DragMode.LockToTrack)
				{
					_target.interactionOnMove = (Interaction) CustomGUILayout.ObjectField<Interaction> ("Interaction on move:", _target.interactionOnMove, true, "", "The Interaction to run whenever the object is moved by the player");
				}
				else
				{
					_target.interactionOnMove = (Interaction) CustomGUILayout.ObjectField<Interaction> ("Interaction on grab:", _target.interactionOnMove, true, "", "The Interaction to run whenever the object is grabbed by the player");
				}
				if (_target.interactionOnMove == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent<Interaction> ();
						newInteraction.gameObject.name = AdvGame.UniqueName (_target.gameObject.name + ": Move");
						_target.interactionOnMove = newInteraction;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.interactionOnMove && _target.interactionOnMove.source == ActionListSource.InScene && _target.interactionOnMove.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("Drag parameter:", _target.interactionOnMove.parameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this draggable object");
					EditorGUILayout.EndHorizontal ();
				}
				else if (_target.interactionOnMove && _target.interactionOnMove.source == ActionListSource.AssetFile && _target.interactionOnMove.assetFile != null && _target.interactionOnMove.assetFile.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("Drag parameter:", _target.interactionOnMove.assetFile.DefaultParameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this draggable object");
					EditorGUILayout.EndHorizontal ();
				}

				EditorGUILayout.BeginHorizontal ();
				_target.interactionOnDrop = (Interaction) CustomGUILayout.ObjectField<Interaction> ("Interaction on let go:", _target.interactionOnDrop, true, "", "The Interaction to run whenever the object is let go by the player");
				if (_target.interactionOnDrop == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent<Interaction> ();
						newInteraction.gameObject.name = AdvGame.UniqueName (_target.gameObject.name + ": LetGo");
						_target.interactionOnDrop = newInteraction;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.interactionOnDrop != null)
				{
					if (_target.interactionOnDrop.source == ActionListSource.InScene && _target.interactionOnDrop.NumParameters > 0)
					{
						EditorGUILayout.BeginHorizontal ();
						_target.dropParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnDrop.parameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
						EditorGUILayout.EndHorizontal ();
					}
					else if (_target.interactionOnDrop.source == ActionListSource.AssetFile && _target.interactionOnDrop.assetFile != null && _target.interactionOnDrop.assetFile.NumParameters > 0)
					{
						EditorGUILayout.BeginHorizontal ();
						_target.dropParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnDrop.assetFile.DefaultParameters, _target.dropParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
						EditorGUILayout.EndHorizontal ();
					}
				}
			}
			else if (_target.actionListSource == ActionListSource.AssetFile)
			{
				_target.actionListAssetOnMove = (ActionListAsset) CustomGUILayout.ObjectField<ActionListAsset> ("Interaction on move:", _target.actionListAssetOnMove, true, "", "The ActionList asset to run whenever the object is moved by the player");

				if (_target.actionListAssetOnMove != null && _target.actionListAssetOnMove.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("Drag parameter:", _target.actionListAssetOnMove.DefaultParameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this draggable object");
					EditorGUILayout.EndHorizontal ();
				}

				_target.actionListAssetOnDrop = (ActionListAsset) CustomGUILayout.ObjectField<ActionListAsset> ("Interaction on let go:", _target.actionListAssetOnDrop, true, "", "The ActionList asset to run whenever the object is let go by the player");

				if (_target.actionListAssetOnDrop != null && _target.actionListAssetOnDrop.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.dropParameterID = Action.ChooseParameterGUI ("Drag parameter:", _target.actionListAssetOnDrop.DefaultParameters, _target.dropParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this draggable object");
					EditorGUILayout.EndHorizontal ();
				}
			}

			CustomGUILayout.EndVertical ();

			if (_target.dragMode == DragMode.LockToTrack && _target.track is DragTrack_Hinge)
			{
				SharedGUI (_target, true);
			}
			else
			{
				SharedGUI (_target, false);
			}

			DisplayInputList (_target);

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void DisplayInputList (Moveable_Drag _target)
		{
			string result = "";

			if (_target.dragMode == DragMode.RotateOnly)
			{
				if (_target.allowZooming)
				{
					result += "\n";
					result += "- ZoomMoveable";
				}
			}

			if (result != "")
			{
				EditorGUILayout.Space ();
				EditorGUILayout.LabelField ("Required inputs:", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox ("The following input axes are available for the chosen settings:" + result, MessageType.Info);
			}
		}

	}

}

#endif