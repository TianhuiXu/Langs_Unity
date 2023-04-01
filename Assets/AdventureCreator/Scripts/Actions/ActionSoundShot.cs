 /*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSoundShot.cs"
 * 
 *	This action plays an AudioClip without the need for a Sound object.
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
	public class ActionSoundShot : Action
	{
		
		public int constantID = 0;
		public int parameterID = -1;
		public Transform origin;
		protected Transform runtimeOrigin;

		public AudioSource audioSource;
		public int audioSourceConstantID = 0;
		public int audioSourceParameterID = -1;
		protected AudioSource runtimeAudioSource;
		
		public AudioClip audioClip;
		public int audioClipParameterID = -1;


		public override ActionCategory Category { get { return ActionCategory.Sound; }}
		public override string Title { get { return "Play one-shot"; }}
		public override string Description { get { return "Plays an AudioClip once without the need for a Sound object."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeOrigin = AssignFile (parameters, parameterID, constantID, origin);
			audioClip = (AudioClip) AssignObject <AudioClip> (parameters, audioClipParameterID, audioClip);
			runtimeAudioSource = (AudioSource) AssignFile <AudioSource> (parameters, audioSourceParameterID, audioSourceConstantID, audioSource);
		}
		
		
		public override float Run ()
		{
			if (audioClip == null)
			{
				return 0f;
			}

			if (!isRunning)
			{
				isRunning = true;

				if (runtimeAudioSource)
				{
					runtimeAudioSource.PlayOneShot (audioClip, Options.GetSFXVolume ());
				}
				else
				{
					Vector3 originPos = KickStarter.CameraMainTransform.position;
					if (runtimeOrigin != null)
					{
						originPos = runtimeOrigin.position;
					}
					
					float volume = Options.GetSFXVolume ();
					AudioSource.PlayClipAtPoint (audioClip, originPos, volume);
				}

				if (willWait)
				{
					return audioClip.length;
				}
			}
		
			isRunning = false;
			return 0f;
		}
		
		
		public override void Skip ()
		{
			if (audioClip == null)
			{
				return;
			}

			if (runtimeAudioSource)
			{
				// Can't stop audio in this case
			}
			else
			{
				AudioSource[] audioSources = Object.FindObjectsOfType (typeof (AudioSource)) as AudioSource[];
				foreach (AudioSource audioSource in audioSources)
				{
					if (audioSource.clip == audioClip && audioSource.isPlaying && audioSource.GetComponent<Sound>() == null)
					{
						audioSource.Stop ();
						return;
					}
				}
			}
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			audioClipParameterID = ChooseParameterGUI ("Clip to play:", parameters, audioClipParameterID, ParameterType.UnityObject);
			if (audioClipParameterID < 0)
			{
				audioClip = (AudioClip) EditorGUILayout.ObjectField ("Clip to play:", audioClip, typeof (AudioClip), false);
			}

			audioSourceParameterID = ChooseParameterGUI ("Audio source (optional):", parameters, audioSourceParameterID, ParameterType.GameObject);
			if (audioSourceParameterID >= 0)
			{
				audioSourceConstantID = 0;
				audioSource = null;
			}
			else
			{
				audioSource = (AudioSource) EditorGUILayout.ObjectField ("Audio source (optional):", audioSource, typeof (AudioSource), false);

				audioSourceConstantID = FieldToID (audioSource, audioSourceConstantID);
				audioSource = IDToField (audioSource, audioSourceConstantID, false);
			}

			if (audioSource == null && audioSourceParameterID < 0)
			{
				parameterID = ChooseParameterGUI ("Position (optional):", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					origin = null;
				}
				else
				{
					origin = (Transform) EditorGUILayout.ObjectField ("Position (optional):", origin, typeof (Transform), true);
				
					constantID = FieldToID (origin, constantID);
					origin = IDToField (origin, constantID, false);
				}
			}

			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (origin, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (audioClip != null)
			{
				return audioClip.name;
			}
			return string.Empty;
		}

		
		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (origin && origin.gameObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Sound: Play one-shot' Action</summary>
		 * <param name = "clipToPlay">The clip to play</param>
		 * <param name = "origin">Where to play the clip from</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the sound has finished playing</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSoundShot CreateNew (AudioClip clipToPlay, Transform origin = null, bool waitUntilFinish = false)
		{
			ActionSoundShot newAction = CreateNew<ActionSoundShot> ();
			newAction.audioClip = clipToPlay;
			newAction.origin = origin;
			newAction.TryAssignConstantID (newAction.origin, ref newAction.constantID);
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}
	
}