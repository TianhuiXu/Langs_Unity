/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSound.cs"
 * 
 *	This action triggers the sound component of any GameObject, overriding that object's play settings.
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
	public class ActionSound : Action
	{

		public int constantID = 0;
		public int parameterID = -1;
		public Sound soundObject;

		public AudioClip audioClip;
		public int audioClipParameterID = -1;

		public enum SoundAction { Play, FadeIn, FadeOut, Stop }
		public float fadeTime;
		public bool loop;
		public bool ignoreIfPlaying = false;
		public SoundAction soundAction;
		public bool affectChildren = false;

		public bool autoEndOtherMusicWhenPlayed = true;

		protected Sound runtimeSound;


		public override ActionCategory Category { get { return ActionCategory.Sound; }}
		public override string Title { get { return "Play"; }}
		public override string Description { get { return "Triggers a Sound object to start playing. Can be used to fade sounds in or out."; }}
		

		public override void AssignValues (List<ActionParameter> parameters)
		{
			audioClip = (AudioClip) AssignObject <AudioClip> (parameters, audioClipParameterID, audioClip);

			runtimeSound = AssignFile <Sound> (parameters, parameterID, constantID, soundObject);
			if (runtimeSound == null && audioClip != null)
			{
				runtimeSound = KickStarter.sceneSettings.defaultSound;
			}
		}

		
		public override float Run ()
		{
			if (runtimeSound == null)
			{
				return 0f;
			}

			if (!isRunning)
			{
				isRunning = true;

				if (ignoreIfPlaying && (soundAction == SoundAction.Play || soundAction == SoundAction.FadeIn))
				{
					if ((audioClip != null && runtimeSound.IsPlaying (audioClip)) || (audioClip == null && runtimeSound.IsPlaying ()))
					{
						// Sound object is already playing the desired clip
						return 0f;
					}
				}

				if (audioClip && runtimeSound.GetComponent <AudioSource>())
				{
					if (soundAction == SoundAction.Play || soundAction == SoundAction.FadeIn)
					{
						runtimeSound.GetComponent <AudioSource>().clip = audioClip;
					}
				}

				if (runtimeSound.soundType == SoundType.Music && autoEndOtherMusicWhenPlayed && (soundAction == SoundAction.Play || soundAction == SoundAction.FadeIn))
				{
					Sound[] sounds = Object.FindObjectsOfType (typeof (Sound)) as Sound[];
					foreach (Sound sound in sounds)
					{
						sound.EndOld (SoundType.Music, runtimeSound);
					}
				}

				if (soundAction == SoundAction.Play)
				{
					runtimeSound.Play (loop);

					if (!loop && willWait)
					{
						return defaultPauseTime;
					}
				}
				else if (soundAction == SoundAction.FadeIn)
				{
					if (fadeTime <= 0f)
					{
						runtimeSound.Play (loop);
					}
					else
					{
						runtimeSound.FadeIn (fadeTime, loop);

						if (!loop && willWait)
						{
							return defaultPauseTime;
						}
					}
				}
				else if (soundAction == SoundAction.FadeOut)
				{
					if (fadeTime <= 0f)
					{
						runtimeSound.Stop ();
					}
					else
					{
						runtimeSound.FadeOut (fadeTime);

						if (willWait)
						{
							return fadeTime;
						}
					}
				}
				else if (soundAction == SoundAction.Stop)
				{
					runtimeSound.Stop ();

					if (affectChildren)
					{
						foreach (Transform child in runtimeSound.transform)
						{
							if (child.GetComponent <Sound>())
							{
								child.GetComponent <Sound>().Stop ();
							}
						}
					}
				}
			}
			else
			{
				if (soundAction == SoundAction.FadeOut)
				{
					isRunning = false;
					return 0f;
				}

				if (runtimeSound.IsPlaying ())
				{
					return defaultPauseTime;
				}
				else
				{
					isRunning = false;
				}
			}
			
			return 0f;
		}


		public override void Skip ()
		{
			if (soundAction == SoundAction.FadeOut || soundAction == SoundAction.Stop)
			{
				Run ();
			}
			else if (loop)
			{
				Run ();
			}
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Sound object:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				soundObject = null;
			}
			else
			{
				soundObject = (Sound) EditorGUILayout.ObjectField ("Sound object:", soundObject, typeof (Sound), true);
				
				constantID = FieldToID <Sound> (soundObject, constantID);
				soundObject = IDToField <Sound> (soundObject, constantID, false);
			}

			soundAction = (SoundAction) EditorGUILayout.EnumPopup ("Sound action:", (SoundAction) soundAction);
			
			if (soundAction == SoundAction.Play || soundAction == SoundAction.FadeIn)
			{
				loop = EditorGUILayout.Toggle ("Loop?", loop);
				ignoreIfPlaying = EditorGUILayout.Toggle ("Ignore if already playing?", ignoreIfPlaying);

				audioClipParameterID = Action.ChooseParameterGUI ("New clip (optional):", parameters, audioClipParameterID, ParameterType.UnityObject);
				if (audioClipParameterID < 0)
				{
					audioClip = (AudioClip) EditorGUILayout.ObjectField ("New clip (optional):", audioClip, typeof (AudioClip), false);
				}

				if (soundObject != null && soundObject.soundType == SoundType.Music)
				{
					autoEndOtherMusicWhenPlayed = EditorGUILayout.Toggle ("Auto-end other music?", autoEndOtherMusicWhenPlayed);
				}
			}
			
			if (soundAction == SoundAction.FadeIn || soundAction == SoundAction.FadeOut)
			{
				fadeTime = EditorGUILayout.Slider ("Fade time:", fadeTime, 0f, 10f);
			}

			if (soundAction == SoundAction.Stop)
			{
				affectChildren = EditorGUILayout.Toggle ("Stop child Sounds, too?", affectChildren);
			}
			else
			{
				if (soundAction == SoundAction.FadeOut || !loop)
				{
					willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberSound> (soundObject);
			}
			AssignConstantID <Sound> (soundObject, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (soundObject != null)
			{
				return soundAction.ToString () + " " + soundObject.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (soundObject && soundObject.gameObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Sound: Play' Action, set to play a new sound</summary>
		 * <param name = "sound">The Sound to play from</param>
		 * <param name = "newClip">If set, the clip to play</param>
		 * <param name = "fadeDuration">If non-negative, the duration of the fade-in effect</param>
		 * <param name = "doLoop">If True, the sound will loop</param>
		 * <param name = "ignoreIfAlreadyPlaying">If True, nothing will happen if the Sound is already playing the chosen audio clip</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the sound has finished playing</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSound CreateNew_Play (Sound sound, AudioClip newClip, float fadeDuration = 0f, bool doLoop = false, bool ignoreIfAlreadyPlaying = true, bool waitUntilFinish = false)
		{
			ActionSound newAction = CreateNew<ActionSound> ();
			newAction.soundObject = sound;
			newAction.TryAssignConstantID (newAction.soundObject, ref newAction.constantID);
			newAction.soundAction = (fadeDuration > 0f) ? SoundAction.FadeIn : SoundAction.Play;
			newAction.audioClip = newClip;
			newAction.loop = doLoop;
			newAction.ignoreIfPlaying = ignoreIfAlreadyPlaying;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Sound: Play' Action, set to stop a sound</summary>
		 * <param name = "sound">The Sound to stop</param>
		 * <param name = "fadeDuration">If non-negative, the duration of the fade-out effect</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the sound has finished playing</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSound CreateNew_Stop (Sound sound, float fadeDuration = 0f, bool waitUntilFinish = false)
		{
			ActionSound newAction = CreateNew<ActionSound> ();
			newAction.soundObject = sound;
			newAction.TryAssignConstantID (newAction.soundObject, ref newAction.constantID);
			newAction.soundAction = (fadeDuration > 0f) ? SoundAction.FadeOut : SoundAction.Stop;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

	}

}