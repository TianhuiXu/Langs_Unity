/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberSound.cs"
 * 
 *	This script is attached to Sound objects in the scene
 *	we wish to save.
 * 
 */

using UnityEngine;
#if AddressableIsPresent
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	/** Attach this script to Sound objects you wish to save. */
	[RequireComponent (typeof (AudioSource))]
	[RequireComponent (typeof (Sound))]
	[AddComponentMenu("Adventure Creator/Save system/Remember Sound")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_sound.html")]
	public class RememberSound : Remember
	{

		private Sound sound;


		public override string SaveData ()
		{
			SoundData soundData = new SoundData();
			soundData.objectID = constantID;
			soundData.savePrevented = savePrevented;

			soundData = Sound.GetSaveData (soundData);
			
			return Serializer.SaveScriptData <SoundData> (soundData);
		}
		

		public override void LoadData (string stringData)
		{
			SoundData data = Serializer.LoadScriptData <SoundData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (Sound is Music) return;

			if (KickStarter.saveSystem.loadingGame == LoadingGame.No && Sound.surviveSceneChange)
			{
				return;
			}

			#if AddressableIsPresent

			if (data.isPlaying && KickStarter.settingsManager.saveAssetReferencesWithAddressables && !string.IsNullOrEmpty (data.clipID))
			{
				StopAllCoroutines ();
				StartCoroutine (LoadDataFromAddressables (data));
				return;
			}

			#endif

			if (data.isPlaying)
			{
				Sound.audioSource.clip = AssetLoader.RetrieveAsset (Sound.audioSource.clip, data.clipID);
			}

			Sound.LoadData (data);
		}


		#if AddressableIsPresent

		private IEnumerator LoadDataFromAddressables (SoundData data)
		{
			AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip> (data.clipID);
			yield return handle;
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				Sound.audioSource.clip = handle.Result;
			}
			Addressables.Release (handle);

			Sound.LoadData (data);
		}

		#endif


		private Sound Sound
		{
			get
			{
				if (sound == null)
				{
					sound = GetComponent <Sound>();
				}
				return sound;
			}
		}
		
	}
	

	/** A data container used by the RememberSound script. */
	[System.Serializable]
	public class SoundData : RememberData
	{

		/** True if a sound is playing */
		public bool isPlaying;
		/** True if a sound is looping */
		public bool isLooping;
		/** How far along the track a sound is */
		public int samplePoint;
		/** A unique identifier for the currently-playing AudioClip */
		public string clipID;
		/** The relative volume on the Sound component */
		public float relativeVolume;
		/** The Sound's maximum volume (internally calculated) */
		public float maxVolume;
		/** The Sound's smoothed-out volume (internally calculated) */
		public float smoothVolume;

		/** The time remaining in a fade effect */
		public float fadeTime;
		/** The original time duration of the active fade effect */
		public float originalFadeTime;
		/** The fade type, where 0 = FadeIn, 1 = FadeOut */
		public int fadeType;
		/** The volume if the Sound's soundType is SoundType.Other */
		public float otherVolume;

		/** The Sound's new relative volume, if changing over time */
		public float targetRelativeVolume;
		/** The Sound's original relative volume, if changing over time */
		public float originalRelativeVolume;
		/** The time remaining in a change in relative volume */
		public float relativeChangeTime;
		/** The original time duration of the active change in relative volume */
		public float originalRelativeChangeTime;

		/** The default Constructor. */
		public SoundData () { }

	}
	
}