#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (Sound))]
	public class SoundEditor : Editor
	{
		
		public override void OnInspectorGUI()
		{
			Sound _target = (Sound) target;
			
			_target.soundType = (SoundType) CustomGUILayout.EnumPopup ("Sound type:", _target.soundType, "", "The type of sound, so far as volume levels go");
			_target.playWhilePaused = CustomGUILayout.Toggle ("Play while game paused?", _target.playWhilePaused, "", "If True, then the sound can play when the game is paused");
			_target.relativeVolume = CustomGUILayout.Slider ("Relative volume:", _target.relativeVolume, 0f, 1f, "", "The volume of the sound, relative to its categoriy's 'global' volume set within OptionsData");

			_target.surviveSceneChange = CustomGUILayout.Toggle ("Play across scenes?", _target.surviveSceneChange, "", "If True, then the GameObject this is attached to will not be destroyed when changing scene");
			if (_target.surviveSceneChange)
			{
				if (_target.transform.root && _target.transform.root != _target.gameObject.transform)
				{
					EditorGUILayout.HelpBox ("For Sound to survive scene-changes, please move this object out of its hierarchy, so that it has no parent GameObject.", MessageType.Warning);
				}
				if (_target.GetComponent <ConstantID>() == null)
				{
					EditorGUILayout.HelpBox ("To avoid duplicates when re-loading the scene, please attach a ConstantID or RememberSound script component.", MessageType.Warning);
				}
			}
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
	}

}

#endif