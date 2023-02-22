#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(FootstepSounds))]
	public class FootstepSoundsEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			FootstepSounds _target = (FootstepSounds) target;

			EditorGUILayout.Space ();
			_target.footstepSounds = ShowClipsGUI (_target.footstepSounds, "Walking sounds");
			EditorGUILayout.Space ();
			_target.runSounds = ShowClipsGUI (_target.runSounds, "Running sounds (optional)");
			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			_target.character = (Char) CustomGUILayout.ObjectField <Char> ("Character:", _target.character, true, string.Empty, "The Player or NPC that this component is for");
			if (_target.character != null || _target.GetComponent <Char>())
			{
				_target.doGroundedCheck = CustomGUILayout.ToggleLeft ("Only play when grounded?", _target.doGroundedCheck, string.Empty, "If True, sounds will only play when the character is grounded");

				if (_target.footstepPlayMethod == FootstepSounds.FootstepPlayMethod.ViaAnimationEvents)
				{
					_target.doMovementCheck = CustomGUILayout.ToggleLeft ("Only play when moving?", _target.doMovementCheck, string.Empty, "If True, sounds will only play when the character is walking or running");
				}
			}
			_target.soundToPlayFrom = (Sound) CustomGUILayout.ObjectField <Sound> ("Sound to play from:", _target.soundToPlayFrom, true, "", "The Sound object to play from");

			_target.footstepPlayMethod = (FootstepSounds.FootstepPlayMethod) CustomGUILayout.EnumPopup ("Play sounds:", _target.footstepPlayMethod, "", "How the sounds are played");
			if (_target.footstepPlayMethod == FootstepSounds.FootstepPlayMethod.Automatically)
			{
				_target.walkSeparationTime = CustomGUILayout.Slider ("Walk separation (s):", _target.walkSeparationTime, 0f, 3f, string.Empty, "The separation time between sounds when walking");
				_target.runSeparationTime = CustomGUILayout.Slider ("Run separation (s):", _target.runSeparationTime, 0f, 3f, string.Empty, "The separation time between sounds when running");
			}
			else if (_target.footstepPlayMethod == FootstepSounds.FootstepPlayMethod.ViaAnimationEvents)
			{
				EditorGUILayout.HelpBox ("A sound will be played whenever this component's PlayFootstep function is run. This component should be placed on the same GameObject as the Animator.", MessageType.Info);
			}
			_target.pitchVariance = CustomGUILayout.Slider ("Pitch variance:", _target.pitchVariance, 0f, 0.8f, string.Empty, "How much the audio pitch can randomly vary by.");
			_target.volumeVariance = CustomGUILayout.Slider ("Volume variance:", _target.volumeVariance, 0f, 0.8f, string.Empty, "How much the audio volume can randomly vary by.");
			CustomGUILayout.EndVertical ();

			if (_target.soundToPlayFrom == null && _target.GetComponent <AudioSource>() == null)
			{
				EditorGUILayout.HelpBox ("To play sounds, the 'Sound to play from' must be assigned, or an AudioSource must be attached.", MessageType.Warning);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private AudioClip[] ShowClipsGUI (AudioClip[] clips, string headerLabel)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField (headerLabel, EditorStyles.boldLabel);
			List<AudioClip> clipsList = new List<AudioClip>();

			if (clips != null)
			{
				foreach (AudioClip clip in clips)
				{
					clipsList.Add (clip);
				}
			}

			int numParameters = clipsList.Count;
			numParameters = EditorGUILayout.DelayedIntField ("# of footstep sounds:", numParameters);

			if (numParameters < clipsList.Count)
			{
				clipsList.RemoveRange (numParameters, clipsList.Count - numParameters);
			}
			else if (numParameters > clipsList.Count)
			{
				if (numParameters > clipsList.Capacity)
				{
					clipsList.Capacity = numParameters;
				}
				for (int i=clipsList.Count; i<numParameters; i++)
				{
					clipsList.Add (null);
				}
			}

			for (int i=0; i<clipsList.Count; i++)
			{
				clipsList[i] = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Sound #" + (i+1).ToString (), clipsList[i], false, "", headerLabel);
			}
			if (clipsList.Count > 1)
			{
				EditorGUILayout.HelpBox ("Sounds will be chosen at random.", MessageType.Info);
			}
			CustomGUILayout.EndVertical ();

			return clipsList.ToArray ();
		}

	}

}

#endif