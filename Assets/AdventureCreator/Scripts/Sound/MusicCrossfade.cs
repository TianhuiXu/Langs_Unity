/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MusicCrossfade.cs"
 * 
 *	Handles the fading-out half of crossfading music.
 *	When music crossfades, the original track is copied here to be fade out, while the Music object fades in the next track.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Handles the fading-out half of crossfading music.
 	 * When music crossfades, the original track is copied here to be fade out, while the Music object fades in the next track.
	 */
	[RequireComponent (typeof (AudioSource))]
	public class MusicCrossfade : MonoBehaviour
	{

		#region Variables

		protected AudioSource _audioSource;
		protected float fadeTime = 0f;
		protected float originalFadeTime = 0f;
		protected float originalVolume = 0f;

		#endregion


		#region PublicFunctions

		/**
		 * Updates the AudioSource's volume.
		 * This is called every frame by Music.
		 */
		public void _Update ()
		{
			float i = fadeTime / originalFadeTime;  // starts as 1, ends as 0
			_audioSource.volume = originalVolume * i;

			if (Mathf.Approximately (Time.deltaTime, 0f))
			{
				fadeTime -= Time.fixedDeltaTime;
			}
			else
			{
				fadeTime -= Time.deltaTime;
			}

			if (fadeTime <= 0f)
			{
				Stop ();
			}
		}


		/** Stops the current audio immediately. */
		public void Stop ()
		{
			Destroy (gameObject);
		}


		/**
		 * <summary>Fades out a new AudioSource</summary>
		 * <param name = "audioSourceToCopy">The AudioSource to copy clip and volume data from</param>
		 * <param name = "_fadeTime">The duration, in seconds, of the fade effect</param>
		 */
		public static MusicCrossfade FadeOut (Soundtrack soundtrack, AudioSource audioSourceToCopy, float _fadeTime)
		{
			if (soundtrack == null || audioSourceToCopy == null || audioSourceToCopy.clip == null || _fadeTime <= 0f)
			{
				return null;
			}
			GameObject newOb = new GameObject (soundtrack.soundType.ToString () + " crossfade");
			newOb.transform.parent = soundtrack.transform;
			newOb.transform.localPosition = Vector3.zero;

			AudioSource newAudioSource = newOb.AddComponent <AudioSource>();
			newAudioSource.spatialBlend = 0f;
			newAudioSource.playOnAwake = false;
			newAudioSource.ignoreListenerPause = soundtrack.playWhilePaused;

			MusicCrossfade newCrossfade = newOb.AddComponent <MusicCrossfade>();
			newCrossfade._audioSource = newAudioSource;

			newAudioSource.clip = audioSourceToCopy.clip;
			newAudioSource.outputAudioMixerGroup = audioSourceToCopy.outputAudioMixerGroup;
			newAudioSource.volume = audioSourceToCopy.volume;
			newAudioSource.timeSamples = audioSourceToCopy.timeSamples;
			newAudioSource.loop = audioSourceToCopy.loop;
			newAudioSource.Play ();

			newCrossfade.originalFadeTime = _fadeTime;
			newCrossfade.originalVolume = audioSourceToCopy.volume;
			newCrossfade.fadeTime = _fadeTime;

			return newCrossfade;
		}

		#endregion

	}

}