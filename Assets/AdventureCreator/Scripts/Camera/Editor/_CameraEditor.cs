#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(_Camera))]
	public class _CameraEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			_Camera _target = (_Camera) target;

			EditorGUILayout.HelpBox ("Attach this script to a custom Camera type to integrate it with Adventure Creator.", MessageType.Info);

			_target.isFor2D = CustomGUILayout.Toggle ("Is for a 2D game?", _target.isFor2D, "", "Check this box if the scene is in 2D, i.e. makes use of 2D Colliders and Raycasts");

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Depth of field", EditorStyles.boldLabel);
			_target.focalDistance = CustomGUILayout.FloatField ("Focal distance", _target.focalDistance, "", "The camera's focal distance.  When the MainCamera is attached to this camera, it can be read through script with 'AC.KickStarter.mainCamera.GetFocalDistance()' and used to update your post-processing method.");
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif