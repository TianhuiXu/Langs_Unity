#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(GameCameraAnimated))]
	public class GameCameraAnimatedEditor : Editor
	{
		
		public override void OnInspectorGUI()
		{
			GameCameraAnimated _target = (GameCameraAnimated) target;

			if (_target.GetComponent <Animation>() == null)
			{
				EditorGUILayout.HelpBox ("This camera type requires an Animation component.", MessageType.Warning);
			}

			CustomGUILayout.BeginVertical ();
			_target.animatedCameraType = (AnimatedCameraType) CustomGUILayout.EnumPopup ("Animated camera type:", _target.animatedCameraType, "", "The way in which animations are played");
			_target.clip = (AnimationClip) CustomGUILayout.ObjectField <AnimationClip> ("Animation clip:", _target.clip, false, "", "The animation to play when this camera is made active");

			if (_target.animatedCameraType == AnimatedCameraType.PlayWhenActive)
			{
				_target.loopClip = CustomGUILayout.Toggle ("Loop animation?", _target.loopClip, "", "If True, then the animation will loop");
				_target.playOnStart = CustomGUILayout.Toggle ("Play on start?", _target.playOnStart, "", "If True, then the animation will play when the scene begins, rather than waiting for it to become active");
			}
			else if (_target.animatedCameraType == AnimatedCameraType.SyncWithTargetMovement)
			{
				_target.pathToFollow = (Paths) CustomGUILayout.ObjectField <Paths> ("Path to follow:", _target.pathToFollow, true, "", "The Paths object to sync with animation");
				_target.targetIsPlayer = CustomGUILayout.Toggle ("Target is Player?", _target.targetIsPlayer, "", "If True, the camera will follow the active Player");
				
				if (!_target.targetIsPlayer)
				{
					_target.target = (Transform) CustomGUILayout.ObjectField <Transform> ("Target:", _target.target, true, "", "The object for the camera to follow");
				}
			}
			CustomGUILayout.EndVertical ();

			if (_target.animatedCameraType == AnimatedCameraType.SyncWithTargetMovement)
			{
				EditorGUILayout.Space ();
				_target.ShowCursorInfluenceGUI ();
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}

#endif