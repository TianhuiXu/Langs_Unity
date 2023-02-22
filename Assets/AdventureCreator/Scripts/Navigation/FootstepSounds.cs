/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"FootstepSounds.cs"
 * 
 *	A component that can play footstep sounds whenever a Mecanim-animated Character moves.
 * The component stores an array of AudioClips, one of which is played at random whenever the PlayFootstep method is called.
 * This method should be invoked as part of a Unity AnimationEvent: http://docs.unity3d.com/Manual/animeditor-AnimationEvents.html
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A component that can play footstep sounds whenever a Mecanim-animated Character moves.
	 * The component stores an array of AudioClips, one of which is played at random whenever the PlayFootstep method is called.
	 * This method should be invoked as part of a Unity AnimationEvent: http://docs.unity3d.com/Manual/animeditor-AnimationEvents.html
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_footstep_sounds.html")]
	[AddComponentMenu("Adventure Creator/Characters/Footstep sounds")]
	public class FootstepSounds: MonoBehaviour
	{

		#region Variables

		/** An array of footstep AudioClips to play at random */
		public AudioClip[] footstepSounds;
		/** An array of footstep AudioClips to play at random when running - if left blank, normal sounds will play */
		public AudioClip[] runSounds;
		/** The Sound object to play from */
		public Sound soundToPlayFrom;
		/** How the sounds are played */
		public FootstepPlayMethod footstepPlayMethod = FootstepPlayMethod.ViaAnimationEvents;
		public enum FootstepPlayMethod { Automatically, ViaAnimationEvents };
		/** The Player or NPC that this component is for */
		public Char character;
		/** If True, and character is assigned, sounds will only play when the character is grounded */
		public bool doGroundedCheck = false;
		/** If True, and character is assigned, sounds will only play when the character is moving */
		public bool doMovementCheck = true;

		/** How much the audio pitch can randomly vary by */
		public float pitchVariance = 0f;
		/** How much the audio volume can randomly vary by */
		public float volumeVariance = 0f;

		/** The separation time between sounds when walking */
		public float walkSeparationTime = 0.5f;
		/** The separation time between sounds when running */
		public float runSeparationTime = 0.25f;

		protected float originalRelativeSound = 1f;
		protected int lastIndex;
		protected AudioSource audioSource;
		protected float delayTime;

		#endregion


		#region UnityStandards
		
		protected void Awake ()
		{
			if (soundToPlayFrom)
			{
				audioSource = soundToPlayFrom.GetComponent <AudioSource>();
			}
			if (audioSource == null)
			{
				audioSource = GetComponent <AudioSource>();
			}

			if (character == null)
			{
				character = GetComponent <Char>();
			}
			delayTime = walkSeparationTime / 2f;

			RecordOriginalRelativeSound ();
		}


		protected void Update ()
		{
			if (character == null || footstepPlayMethod == FootstepPlayMethod.ViaAnimationEvents) return;

			if (character.charState == CharState.Move && !character.IsJumping)
			{
				delayTime -= Time.deltaTime;

				if (delayTime <= 0f)
				{
					delayTime = (character.isRunning) ? runSeparationTime : walkSeparationTime;
					PlayFootstep ();
				}
			}
			else
			{
				delayTime = walkSeparationTime / 2f;
			}
		}

		#endregion


		#region PublicFunctions		

		/** Plays one of the footstepSounds at random on the assigned Sound object. */
		public void PlayFootstep ()
		{
			if (audioSource && footstepSounds.Length > 0 &&
			    (!doMovementCheck || character == null || character.charState == CharState.Move))
			{
				if (doGroundedCheck && character && !character.IsGrounded (true))
				{
					return;
				}

				bool doRun = (character.isRunning && runSounds.Length > 0) ? true : false;
				if (doRun)
				{
					PlaySound (runSounds, doRun);
				}
				else
				{
					PlaySound (footstepSounds, doRun);
				}
			}
		}

		/** Records the associated Sound component's relative volume. */
		public void RecordOriginalRelativeSound ()
		{
			if (soundToPlayFrom)
			{
				originalRelativeSound = soundToPlayFrom.relativeVolume;
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void PlaySound (AudioClip[] clips, bool isRunSound)
		{
			if (clips == null) return;

			if (clips.Length == 1)
			{
				PlaySound (clips[0], isRunSound);
				return;
			}

			int newIndex = Random.Range (0, clips.Length - 1);
			if (newIndex == lastIndex)
			{
				newIndex ++;
				if (newIndex >= clips.Length)
				{
					newIndex = 0;
				}
			}

			PlaySound (clips[newIndex], isRunSound);
			lastIndex = newIndex;
		}


		protected void PlaySound (AudioClip clip, bool isRunSound)
		{
			if (clip == null) return;

			audioSource.clip = clip;

			if (pitchVariance > 0f)
			{
				float randomPitch = 1f + Random.Range (-pitchVariance, pitchVariance);
				audioSource.pitch = randomPitch;
			}

			float localVolume = (volumeVariance > 0f) ? (1f - Random.Range (0f, volumeVariance)): 1f;
			
			if (soundToPlayFrom)
			{
				if (soundToPlayFrom.audioSource)
				{
					soundToPlayFrom.audioSource.PlayOneShot (clip, localVolume);
				}
				if (KickStarter.eventManager) KickStarter.eventManager.Call_OnPlayFootstepSound (character, this, !isRunSound, soundToPlayFrom.audioSource, clip);
			}
			else
			{
				audioSource.PlayOneShot (clip, localVolume);
				if (KickStarter.eventManager) KickStarter.eventManager.Call_OnPlayFootstepSound (character, this, !isRunSound, audioSource, clip);
			}
		}

		#endregion

	}

}