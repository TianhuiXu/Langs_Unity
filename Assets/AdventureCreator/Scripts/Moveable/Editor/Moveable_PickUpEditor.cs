#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(Moveable_PickUp))]
	public class Moveable_PickUpEditor : DragBaseEditor
	{

		public override void OnInspectorGUI ()
		{
			Moveable_PickUp _target = (Moveable_PickUp) target;
			GetReferences ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Movement settings:", EditorStyles.boldLabel);
			_target.maxSpeed = CustomGUILayout.FloatField ("Max speed:", _target.maxSpeed, string.Empty, "The maximum force magnitude that can be applied to itself");
			_target.playerMovementReductionFactor = CustomGUILayout.Slider ("Player movement reduction:", _target.playerMovementReductionFactor, 0f, 1f, string.Empty, "How much player movement is reduced by when the object is being dragged");
			_target.invertInput = CustomGUILayout.Toggle ("Invert input?", _target.invertInput, string.Empty, "If True, input vectors will be inverted");
			_target.breakForce = CustomGUILayout.FloatField ("Break force:", _target.breakForce, string.Empty, "The maximum force magnitude that can be applied by the player - if exceeded, control will be removed");
			_target.initialLift = CustomGUILayout.Slider ("Initial lift:", _target.initialLift, 0f, 1f, string.Empty, "The lift to give objects picked up, so that they aren't touching the ground when initially held");
			_target.minDistance = CustomGUILayout.FloatField ("Min distance from camera:", _target.minDistance, string.Empty, "The minimum distance to keep from the camera when grabbed");
			_target.maxDistance = CustomGUILayout.FloatField ("Max distance from camera:", _target.maxDistance, string.Empty, "The maximum distance to keep from the camera when grabbed");
			_target.autoSetConstraints = CustomGUILayout.Toggle ("Auto set RB constraints?", _target.autoSetConstraints, string.Empty, "If True, the Rigidbody's constraints will be set automatically based on the state of the interaction.");

			_target.offScreenRelease = (OffScreenRelease)CustomGUILayout.EnumPopup ("Off-screen release:", _target.offScreenRelease, string.Empty, "What should cause the object to be automatically released upon leaving the screen");

			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Interactions", EditorStyles.boldLabel);

			_target.actionListSource = (ActionListSource) CustomGUILayout.EnumPopup ("Actions source:", _target.actionListSource, string.Empty, "The source of the commands that are run when the object is interacted with");

			if (_target.actionListSource == ActionListSource.InScene)
			{
				EditorGUILayout.BeginHorizontal ();
				_target.interactionOnGrab = (Interaction) CustomGUILayout.ObjectField <Interaction> ("Interaction on grab:", _target.interactionOnGrab, true, string.Empty, "The Interaction to run whenever the object is moved by the player");
				if (_target.interactionOnGrab == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent <Interaction>();
						newInteraction.gameObject.name = AdvGame.UniqueName (_target.gameObject.name + ": Grab");
						_target.interactionOnGrab = newInteraction;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.interactionOnGrab != null && _target.interactionOnGrab.source == ActionListSource.InScene && _target.interactionOnGrab.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnGrab.parameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}
				else if (_target.interactionOnGrab != null && _target.interactionOnGrab.source == ActionListSource.AssetFile && _target.interactionOnGrab.assetFile != null && _target.interactionOnGrab.assetFile.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnGrab.assetFile.DefaultParameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}

				EditorGUILayout.BeginHorizontal ();
				_target.interactionOnDrop = (Interaction) CustomGUILayout.ObjectField <Interaction> ("Interaction on let go:", _target.interactionOnDrop, true, string.Empty, "The Interaction to run whenever the object is let go by the player");
				if (_target.interactionOnDrop == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent <Interaction>();
						newInteraction.gameObject.name = AdvGame.UniqueName (_target.gameObject.name + ": LetGo");
						_target.interactionOnDrop = newInteraction;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.interactionOnDrop)
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
				_target.actionListAssetOnGrab = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("Interaction on grab:", _target.actionListAssetOnGrab, true, string.Empty, "The ActionList asset to run whenever the object is moved by the player");

				if (_target.actionListAssetOnGrab != null && _target.actionListAssetOnGrab.NumParameters> 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.actionListAssetOnGrab.DefaultParameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}

				_target.actionListAssetOnDrop = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("Interaction on let go:", _target.actionListAssetOnDrop, true, string.Empty, "The ActionList asset to run whenever the object is let go by the player");

				if (_target.actionListAssetOnDrop != null && _target.actionListAssetOnDrop.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.dropParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.actionListAssetOnDrop.DefaultParameters, _target.dropParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}
			}
			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Rotation settings:", EditorStyles.boldLabel);
			_target.allowRotation = CustomGUILayout.Toggle ("Allow rotation?", _target.allowRotation, string.Empty, "If True, the object can be rotated");
			if (_target.allowRotation)
			{
				_target.rotationFactor = CustomGUILayout.FloatField ("Rotation factor:", _target.rotationFactor, string.Empty, "Controls the speed by which the object can be rotated (higher values = slower)");
				_target.maxAngularVelocity = CustomGUILayout.FloatField ("Max angular velocity:", _target.maxAngularVelocity, string.Empty, "The Rigidbody's maxAngularVelocity value");
			}
			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Zoom settings:", EditorStyles.boldLabel);
			_target.allowZooming = CustomGUILayout.Toggle ("Allow zooming?", _target.allowZooming, string.Empty, "If True, the object can be moved towards and away from the camera");
			if (_target.allowZooming)
			{
				_target.zoomSpeed = CustomGUILayout.FloatField ("Zoom speed:", _target.zoomSpeed, string.Empty, "The speed at which the object can be moved towards and away from the camera");
				_target.minZoom = CustomGUILayout.FloatField ("Closest distance:", _target.minZoom, string.Empty, "The minimum distance that there can be between the object and the camera");
				_target.maxZoom = CustomGUILayout.FloatField ("Farthest distance:", _target.maxZoom, string.Empty, "The maximum distance that there can be between the object and the camera");
			}
			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Throw settings:", EditorStyles.boldLabel);
			_target.allowThrow = CustomGUILayout.Toggle ("Allow throwing?", _target.allowThrow, string.Empty, "If True, the object can be thrown");
			if (_target.allowThrow)
			{
				_target.throwForce = CustomGUILayout.FloatField ("Force scale:", _target.throwForce, string.Empty, "How far the object can be thrown");
				_target.chargeTime = CustomGUILayout.FloatField ("Charge time:", _target.chargeTime, string.Empty, "How long a 'charge' takes, if the object cen be thrown");
				_target.pullbackDistance = CustomGUILayout.FloatField ("Pull-back distance:", _target.pullbackDistance, string.Empty, "How far the object is pulled back while chargine, if the object can be thrown");
			}		
			CustomGUILayout.EndVertical ();

			SharedGUI (_target, false);

			DisplayInputList (_target);
		
			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void DisplayInputList (Moveable_PickUp _target)
		{
			string result = "";

			if (_target.allowRotation)
			{
				result += "\n";
				result += "- RotateMoveable (Button)";
				result += "\n";
				result += "- RotateMoveableToggle (Button";
			}
			if (_target.allowZooming)
			{
				result += "\n";
				result += "- ZoomMoveable (Axis)";
			}
			if (_target.allowThrow)
			{
				result += "\n";
				result += "- ThrowMoveable (Button)";
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