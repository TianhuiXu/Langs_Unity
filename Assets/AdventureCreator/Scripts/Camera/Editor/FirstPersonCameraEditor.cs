#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (FirstPersonCamera))]
	public class FirstPersonCameraEditor : Editor
	{
		
		private static GUILayoutOption
			labelWidth = GUILayout.MaxWidth (60),
			intWidth = GUILayout.MaxWidth (130);
		
		
		public override void OnInspectorGUI ()
		{
			FirstPersonCamera _target = (FirstPersonCamera) target;
			
			CustomGUILayout.BeginVertical ();
			_target.headBob = EditorGUILayout.Toggle ("Bob head when moving?", _target.headBob);
			if (_target.headBob)
			{
				_target.headBobMethod = (FirstPersonHeadBobMethod) EditorGUILayout.EnumPopup ("Head-bob method:", _target.headBobMethod);
				if (_target.headBobMethod == FirstPersonHeadBobMethod.BuiltIn)
				{
					_target.builtInSpeedFactor = EditorGUILayout.FloatField ("Speed factor:", _target.builtInSpeedFactor);
					_target.bobbingAmount = EditorGUILayout.FloatField ("Height change factor:", _target.bobbingAmount);
				}
				else if (_target.headBobMethod == FirstPersonHeadBobMethod.CustomAnimation)
				{
					_target.headBobSpeedParameter = EditorGUILayout.TextField ("Forward speed float parameter:", _target.headBobSpeedParameter);
					_target.headBobSpeedSideParameter = EditorGUILayout.TextField ("Sideways speed float parameter:", _target.headBobSpeedSideParameter);
					if (_target.GetComponent <Animator>() == null)
					{
						EditorGUILayout.HelpBox ("This GameObject must have an Animator component attached.", MessageType.Warning);
					}
				}
				else if (_target.headBobMethod == FirstPersonHeadBobMethod.CustomScript)
				{
					EditorGUILayout.HelpBox ("The component's public method 'GetHeadBobSpeed' will return the desired head-bobbing speed.", MessageType.Info);
				}
			}
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			_target.allowMouseWheelZooming = EditorGUILayout.Toggle ("Allow mouse-wheel zooming?", _target.allowMouseWheelZooming);
			if (_target.allowMouseWheelZooming)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Min FOV:", labelWidth);
				_target.minimumZoom = EditorGUILayout.FloatField (_target.minimumZoom, intWidth);
				EditorGUILayout.LabelField ("Max FOV:", labelWidth);
				_target.maximumZoom = EditorGUILayout.FloatField (_target.maximumZoom, intWidth);
				EditorGUILayout.EndHorizontal ();
			}
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Constrain pitch-rotation (degrees)");
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Min:", labelWidth);
			_target.minY = EditorGUILayout.FloatField (_target.minY, intWidth);
			EditorGUILayout.LabelField ("Max:", labelWidth);
			_target.maxY = EditorGUILayout.FloatField (_target.maxY, intWidth);
			EditorGUILayout.EndHorizontal ();
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			_target.sensitivity = EditorGUILayout.Vector2Field ("Freelook sensitivity:", _target.sensitivity);
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
		
	}

}

#endif