#if UNITY_EDITOR

#if UNITY_2018_2_OR_NEWER
#define ALLOW_PHYSICAL_CAMERA
#endif

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (GameCamera25D))]
	public class GameCamera25DEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			GameCamera25D _target = (GameCamera25D) target;
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Background image", EditorStyles.boldLabel);
		
			EditorGUILayout.BeginHorizontal ();
			_target.backgroundImage = (BackgroundImage) CustomGUILayout.ObjectField <BackgroundImage> ("Background:", _target.backgroundImage, true, "", "The BackgroundImage to display underneath all scene objects");
			
			if (_target.backgroundImage)
			{
				if (!Application.isPlaying && GUILayout.Button ("Set as active", GUILayout.MaxWidth (90f)))
				{
					Undo.RecordObject (_target, "Set active background");
					
					_target.SetActiveBackground ();
				}
			}
			else
			{
				if (GUILayout.Button ("Create", GUILayout.MaxWidth (90f)))
				{
					Undo.RecordObject (_target, "Create Background Image");
					BackgroundImage newBackgroundImage = SceneManager.AddPrefab ("SetGeometry", "BackgroundImage", true, false, true).GetComponent <BackgroundImage>();
					
					string cameraName = _target.gameObject.name;

					newBackgroundImage.gameObject.name = AdvGame.UniqueName (cameraName + ": Background");
					_target.backgroundImage = newBackgroundImage;
				}
			}

			EditorGUILayout.EndHorizontal ();

			if (MainCamera.AllowProjectionShifting (_target.GetComponent <Camera>()))
			{
				EditorGUILayout.Space ();
				EditorGUILayout.LabelField ("Perspective offset", EditorStyles.boldLabel);
				_target.perspectiveOffset.x = CustomGUILayout.Slider ("Horizontal:", _target.perspectiveOffset.x, -0.05f, 0.05f, "", "The horizontal offset in perspective from the camera's centre");
				_target.perspectiveOffset.y = CustomGUILayout.Slider ("Vertical:", _target.perspectiveOffset.y, -0.05f, 0.05f, "", "The vertical offset in perspective from the camera's centre");
			}

			CustomGUILayout.EndVertical ();

			if (_target.isActiveEditor)
			{
				_target.UpdateCameraSnap ();
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif