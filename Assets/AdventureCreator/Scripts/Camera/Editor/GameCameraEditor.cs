#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(GameCamera))]
	public class GameCameraEditor : Editor
	{
		
		public override void OnInspectorGUI()
		{
			GameCamera _target = (GameCamera) target;
			
			_target.ShowCursorInfluenceGUI ();
			EditorGUILayout.Space ();
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("X-axis movement", EditorStyles.boldLabel);
			
			_target.lockXLocAxis = CustomGUILayout.Toggle ("Lock?", _target.lockXLocAxis, "", "If True, movement in the X-axis is prevented");
			
			if (!_target.lockXLocAxis)
			{
				_target.xLocConstrainType = (CameraLocConstrainType) CustomGUILayout.EnumPopup ("Affected by:", _target.xLocConstrainType, "", "The constrain type on X-axis movement");
				
				CustomGUILayout.BeginVertical ();
				if (_target.xLocConstrainType == CameraLocConstrainType.SideScrolling)
				{
					_target.xFreedom = CustomGUILayout.FloatField ("Track freedom:", _target.xFreedom, "", "The track freedom along the X-axis");
				}
				else
				{
					_target.xGradient = CustomGUILayout.FloatField ("Influence:", _target.xGradient, "", "The influence of the target's position on X-axis movement");
				}
				_target.xOffset = CustomGUILayout.FloatField ("Offset:", _target.xOffset, "", "The X-axis position offset");
				CustomGUILayout.EndVertical ();
	
				_target.limitX = CustomGUILayout.Toggle ("Constrain?", _target.limitX, "", "If True, then X-axis movement will be limited to minimum and maximum values");
				if (_target.limitX)
				{
					CustomGUILayout.BeginVertical ();
					_target.constrainX[0] = CustomGUILayout.FloatField ("Minimum constraint:", _target.constrainX[0], "", "The lower X-axis movement limit");
					_target.constrainX[1] = CustomGUILayout.FloatField ("Maximum constraint:", _target.constrainX[1], "", "The upper X-axis movement limit");
					CustomGUILayout.EndVertical ();
				}
			}
				
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Y-axis movement", EditorStyles.boldLabel);
			
			_target.lockYLocAxis = CustomGUILayout.Toggle ("Lock?", _target.lockYLocAxis, "", "If True, movement in the Y-axis is prevented");
			
			if (!_target.lockYLocAxis)
			{
				_target.yLocConstrainType = (CameraLocConstrainType) CustomGUILayout.EnumPopup ("Affected by:", _target.yLocConstrainType, "", "The constrain type on Y-axis movement");

				CustomGUILayout.BeginVertical ();
				if (_target.yLocConstrainType == CameraLocConstrainType.SideScrolling)
				{
					_target.yFreedom = CustomGUILayout.FloatField ("Track freedom:", _target.yFreedom, "", "The track freedom along the Y-axis");
				}
				else
				{
					_target.yGradientLoc = CustomGUILayout.FloatField ("Influence:", _target.yGradientLoc, "", "The influence of the target's position on Y-axis movement");
				}
				_target.yOffsetLoc = CustomGUILayout.FloatField ("Offset:", _target.yOffsetLoc, "", "The Y-axis position offset");
				CustomGUILayout.EndVertical ();

				_target.limitYLoc = CustomGUILayout.Toggle ("Constrain?", _target.limitYLoc, "", "If True, then Y-axis movement will be limited to minimum and maximum values");
				if (_target.limitYLoc)
				{
					CustomGUILayout.BeginVertical ();
					_target.constrainYLoc[0] = CustomGUILayout.FloatField ("Minimum constraint:", _target.constrainYLoc[0], "", "The lower Y-axis movement limit");
					_target.constrainYLoc[1] = CustomGUILayout.FloatField ("Maximum constraint:", _target.constrainYLoc[1], "", "The upper Y-axis movement limit");
					CustomGUILayout.EndVertical ();
				}
			}
			
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Z-axis movement", EditorStyles.boldLabel);
	
			_target.lockZLocAxis = CustomGUILayout.Toggle ("Lock?", _target.lockZLocAxis, "", "If True, movement in the Z-axis is prevented");
			
			if (!_target.lockZLocAxis)
			{
				_target.zLocConstrainType = (CameraLocConstrainType) CustomGUILayout.EnumPopup ("Affected by:", _target.zLocConstrainType, "", "The constrain type on Z-axis movement");
				
				CustomGUILayout.BeginVertical ();
				if (_target.zLocConstrainType == CameraLocConstrainType.SideScrolling)
				{
					_target.zFreedom = CustomGUILayout.FloatField ("Track freedom:", _target.zFreedom, "", "The track freedom along the Z-axis");
				}
				else
				{
					_target.zGradient = CustomGUILayout.FloatField ("Influence:", _target.zGradient, "", "The influence of the target's position on Z-axis movement");
				}
				_target.zOffset = CustomGUILayout.FloatField ("Offset:", _target.zOffset, "", "The Z-axis position offset");
				CustomGUILayout.EndVertical ();
	
				_target.limitZ = CustomGUILayout.Toggle ("Constrain?", _target.limitZ, "", "If True, then Z-axis movement will be limited to minimum and maximum values");
				if (_target.limitZ)
				{
					CustomGUILayout.BeginVertical ();
					_target.constrainZ[0] = CustomGUILayout.FloatField ("Minimum constraint:", _target.constrainZ[0], "", "The lower Z-axis movement limit");
					_target.constrainZ[1] = CustomGUILayout.FloatField ("Maximum constraint:", _target.constrainZ[1], "", "The upper Z-axis movement limit");
					CustomGUILayout.EndVertical ();
				}
			}
			
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Spin rotation", EditorStyles.boldLabel);
			
			_target.lockYRotAxis = CustomGUILayout.Toggle ("Lock?", _target.lockYRotAxis, "", "If True, spin rotation is prevented");
			
			if (!_target.lockYRotAxis)
			{
				_target.yRotConstrainType = (CameraRotConstrainType) CustomGUILayout.EnumPopup ("Affected by:", _target.yRotConstrainType, "", "The constrain type on spin rotation");
				
				if (_target.yRotConstrainType != CameraRotConstrainType.LookAtTarget)
				{
					CustomGUILayout.BeginVertical ();
					_target.directionInfluence = CustomGUILayout.FloatField ("Target direction fac.:", _target.directionInfluence, "", "The influence that the target's facing direction has on the tracking position");
					_target.yGradient = CustomGUILayout.FloatField ("Influence:", _target.yGradient, "", "The influence of the target's position on spin rotation");
					_target.yOffset = CustomGUILayout.FloatField ("Offset:", _target.yOffset, "", "The spin rotation offset");
					CustomGUILayout.EndVertical ();
				}
				else
				{
					CustomGUILayout.BeginVertical ();
					_target.directionInfluence = CustomGUILayout.FloatField ("Target direction fac.:", _target.directionInfluence, "", "The influence that the target's facing direction has on the tracking position");
					_target.targetHeight = CustomGUILayout.FloatField ("Target height offset:", _target.targetHeight, "", "The target positional offset in the Y-axis");
					_target.targetXOffset = CustomGUILayout.FloatField ("Target X offset:", _target.targetXOffset, "", "The target positional offset in the X-axis");
					_target.targetZOffset = CustomGUILayout.FloatField ("Target Z offset:", _target.targetZOffset, "", "The target positional offset in the Z-axis");
					CustomGUILayout.EndVertical ();
				}

				_target.limitY = CustomGUILayout.Toggle ("Constrain?", _target.limitY, "", "If True, then spin rotation will be limited to minimum and maximum values");
				if (_target.limitY)
				{
					CustomGUILayout.BeginVertical ();
					_target.constrainY[0] = CustomGUILayout.FloatField ("Minimum constraint:", _target.constrainY[0], "", "The lower spin rotation limit");
					_target.constrainY[1] = CustomGUILayout.FloatField ("Maximum constraint:", _target.constrainY[1], "", "The upper spin rotation limit");
					CustomGUILayout.EndVertical ();
				}
			}
			
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Pitch rotation", EditorStyles.boldLabel);

			if (!_target.lockYRotAxis && _target.yRotConstrainType == CameraRotConstrainType.LookAtTarget)
			{
				EditorGUILayout.HelpBox ("Pitch rotation is overriden by 'Look At Target' spin rotation.", MessageType.Info);

				_target.limitXRot = CustomGUILayout.Toggle ("Constrain?", _target.limitXRot, "", "If True, then pitch rotation will be limited to minimum and maximum values");
			}
			else
			{
				_target.lockXRotAxis = CustomGUILayout.Toggle ("Lock?", _target.lockXRotAxis, "", "If True, pitch rotation is prevented");
				
				if (!_target.lockXRotAxis)
				{
					_target.xRotConstrainType = (CameraLocConstrainType) CustomGUILayout.EnumPopup ("Affected by:", _target.xRotConstrainType, "", "The constrain type on pitch rotation");
					
					if (_target.xRotConstrainType == CameraLocConstrainType.SideScrolling)
					{
						EditorGUILayout.HelpBox ("This option is not available for Pitch rotation", MessageType.Warning);
					}
					else
					{
						CustomGUILayout.BeginVertical ();
						_target.xGradientRot = CustomGUILayout.FloatField ("Influence:", _target.xGradientRot, "", "The influence of the target's position on pitch rotation");
						_target.xOffsetRot = CustomGUILayout.FloatField ("Offset:", _target.xOffsetRot, "", "The pitch rotation offset");
						CustomGUILayout.EndVertical ();
					}
					
					_target.limitXRot = CustomGUILayout.Toggle ("Constrain?", _target.limitXRot, "", "If True, then pitch rotation will be limited to minimum and maximum values");
					if (_target.limitXRot)
					{
						CustomGUILayout.BeginVertical ();
						_target.constrainXRot[0] = CustomGUILayout.FloatField ("Minimum constraint:", _target.constrainXRot[0], "", "The lower pitch rotation limit");
						_target.constrainXRot[1] = CustomGUILayout.FloatField ("Maximum constraint:", _target.constrainXRot[1], "", "The upper pitch rotation limit");
						CustomGUILayout.EndVertical ();
					}
				}
			}

			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();
			
			CustomGUILayout.BeginVertical ();
			if (_target.GetComponent <Camera>() && _target.GetComponent <Camera>().orthographic)
			{
				EditorGUILayout.LabelField ("Orthographic size", EditorStyles.boldLabel);
			}
			else if (_target.GetComponentInChildren <Camera>() && _target.GetComponentInChildren <Camera>().orthographic)
			{
				EditorGUILayout.LabelField ("Orthographic size", EditorStyles.boldLabel);
			}
			else
			{
				EditorGUILayout.LabelField ("Field of view", EditorStyles.boldLabel);
			}
			
			_target.lockFOV = CustomGUILayout.Toggle ("Lock?", _target.lockFOV, "", "If True, changing of the FOV is prevented");
			
			if (!_target.lockFOV)
			{
				EditorGUILayout.HelpBox ("This value will vary with the target's distance from the camera.", MessageType.Info);
				
				CustomGUILayout.BeginVertical ();
				_target.FOVGradient = CustomGUILayout.FloatField ("Influence:", _target.FOVGradient, "", "The influence of the target's position on FOV");
				_target.FOVOffset = CustomGUILayout.FloatField ("Offset:", _target.FOVOffset, "", "The FOV offset");
				CustomGUILayout.EndVertical ();
				
				_target.limitFOV = CustomGUILayout.Toggle ("Constrain?", _target.limitFOV, "", "If True, then FOV will be limited to minimum and maximum value");
				if (_target.limitFOV)
				{
					CustomGUILayout.BeginVertical ();
					_target.constrainFOV[0] = CustomGUILayout.FloatField ("Minimum constraint:", _target.constrainFOV[0], "", "The lower FOV limit");
					_target.constrainFOV[1] = CustomGUILayout.FloatField ("Maximum constraint:", _target.constrainFOV[1], "", "The upper FOV limit");
					CustomGUILayout.EndVertical ();
				}
			}
			
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Depth of field", EditorStyles.boldLabel);
			_target.focalPointIsTarget = CustomGUILayout.Toggle ("Focal point is target object?", _target.focalPointIsTarget, "", "If True, then the focal distance will match the distance to the target");
			if (!_target.focalPointIsTarget)
			{
				_target.focalDistance = CustomGUILayout.FloatField ("Focal distance:", _target.focalDistance, "", "The camera's focal distance.  When the MainCamera is attached to this camera, it can be read through script with 'AC.KickStarter.mainCamera.GetFocalDistance()' and used to update your post-processing method.");
			}
			else if (Application.isPlaying)
			{
				EditorGUILayout.LabelField ("Focal distance: " +  _target.focalDistance.ToString (), EditorStyles.miniLabel);
			}
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			if (!_target.lockXLocAxis || !_target.lockYRotAxis || !_target.lockFOV || !_target.lockYLocAxis || !_target.lockZLocAxis || _target.focalPointIsTarget)
			{
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("Target object to control camera movement", EditorStyles.boldLabel);
				
				_target.targetIsPlayer = CustomGUILayout.Toggle ("Target is Player?", _target.targetIsPlayer, "", "If True, the camera will follow the active Player");
				
				if (!_target.targetIsPlayer)
				{
					_target.target = (Transform) CustomGUILayout.ObjectField <Transform> ("Target:", _target.target, true, "", "The object for the camera to follow");
				}
				
				_target.dampSpeed = CustomGUILayout.FloatField ("Follow speed:", _target.dampSpeed, "", "The follow speed when tracking a target");
				_target.actFromDefaultPlayerStart = CustomGUILayout.Toggle ("Use default PlayerStart?", _target.actFromDefaultPlayerStart, "", "If True, then the camera's position will be relative to the scene's default PlayerStart, rather then the Player's initial position. This ensures that camera movement is the same regardless of where the Player begins in the scene");
				CustomGUILayout.EndVertical ();
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}

#endif