/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberFootstepSounds.cs"
 * 
 *	This script is attached to FootstepSound components whose change in AudioClips you wish to save. 
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Text;
#if AddressableIsPresent
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	/**
	 * This script is attached to FootstepSound components whose change in AudioClips you wish to save. 
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Footstep Sounds")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_footstep_sounds.html")]
	public class RememberFootstepSounds : Remember
	{

		private FootstepSounds footstepSounds;
		

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			FootstepSoundData footstepSoundData = new FootstepSoundData ();

			footstepSoundData.objectID = constantID;
			footstepSoundData.savePrevented = savePrevented;

			if (FootstepSounds)
			{
				footstepSoundData.walkSounds = SoundsToString (FootstepSounds.footstepSounds);
				footstepSoundData.runSounds = SoundsToString (FootstepSounds.runSounds);
			}

			return Serializer.SaveScriptData <FootstepSoundData> (footstepSoundData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			FootstepSoundData data = Serializer.LoadScriptData <FootstepSoundData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (FootstepSounds)
			{
				#if AddressableIsPresent

				if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
				{
					StopAllCoroutines ();
					StartCoroutine (LoadDataFromAddressable (data));
					return;
				}

				#endif

				LoadDataFromResources (data);
			}
		}


		#if AddressableIsPresent

		private IEnumerator LoadDataFromAddressable (FootstepSoundData data)
		{
			if (!string.IsNullOrEmpty (data.walkSounds))
			{
				List<AudioClip> soundsList = new List<AudioClip> ();

				string[] valuesArray = data.walkSounds.Split (SaveSystem.pipe[0]);
				for (int i = 0; i < valuesArray.Length; i++)
				{
					string audioClipName = valuesArray[i];
					if (string.IsNullOrEmpty (audioClipName)) continue;

					AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip> (audioClipName);
					yield return handle;
					if (handle.Status == AsyncOperationStatus.Succeeded)
					{
						soundsList.Add (handle.Result);
					}
					Addressables.Release (handle);
				}
				if (soundsList.Count > 0)
				{
					FootstepSounds.footstepSounds = soundsList.ToArray ();
				}
			}

			if (!string.IsNullOrEmpty (data.runSounds))
			{
				List<AudioClip> soundsList = new List<AudioClip> ();

				string[] valuesArray = data.runSounds.Split (SaveSystem.pipe[0]);
				for (int i = 0; i < valuesArray.Length; i++)
				{
					string audioClipName = valuesArray[i];
					if (string.IsNullOrEmpty (audioClipName)) continue;

					AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip> (audioClipName);
					yield return handle;
					if (handle.Status == AsyncOperationStatus.Succeeded)
					{
						soundsList.Add (handle.Result);
					}
					Addressables.Release (handle);
				}
				if (soundsList.Count > 0)
				{
					FootstepSounds.runSounds = soundsList.ToArray ();
				}
			}
		}

		#endif


		private void LoadDataFromResources (FootstepSoundData data)
		{
			AudioClip[] walkSounds = StringToSounds (data.walkSounds);
			if (walkSounds != null && walkSounds.Length > 0)
			{
				FootstepSounds.footstepSounds = walkSounds;
			}

			AudioClip[] runSounds = StringToSounds (data.runSounds);
			if (runSounds != null && runSounds.Length > 0)
			{
				FootstepSounds.runSounds = runSounds;
			}
		}


		private AudioClip[] StringToSounds (string dataString)
		{
			if (string.IsNullOrEmpty (dataString))
			{
				return null;
			}

			List<AudioClip> soundsList = new List<AudioClip>();
			
			string[] valuesArray = dataString.Split (SaveSystem.pipe[0]);
			for (int i=0; i<valuesArray.Length; i++)
			{
				string audioClipName = valuesArray[i];
				AudioClip audioClip = AssetLoader.RetrieveAudioClip (audioClipName);
				if (audioClip)
				{
					soundsList.Add (audioClip);
				}
			}

			return soundsList.ToArray ();
		}


		private string SoundsToString (AudioClip[] audioClips)
		{
			StringBuilder soundString = new StringBuilder ();

			for (int i=0; i<audioClips.Length; i++)
			{
				if (audioClips[i] != null)
				{
					soundString.Append (AssetLoader.GetAssetInstanceID (audioClips[i]));
					
					if (i < audioClips.Length-1)
					{
						soundString.Append (SaveSystem.pipe);
					}
				}
			}

			return soundString.ToString ();
		}


		private FootstepSounds FootstepSounds
		{
			get
			{
				if (footstepSounds == null)
				{
					footstepSounds = GetComponent <FootstepSounds>();
				}
				return footstepSounds;
			}
		}

	}


	/** A data container used by the RememberFootstepSounds script. */
	[System.Serializable]
	public class FootstepSoundData : RememberData
	{

		public string walkSounds;
		public string runSounds;

		/** The default Constructor. */
		public FootstepSoundData () { }

	}

}