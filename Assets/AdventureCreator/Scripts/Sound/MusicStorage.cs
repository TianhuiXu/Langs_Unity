/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MusicStorage.cs"
 * 
 *	A data container for any music track that can be played using the 'Sound: Play music' Action.
 * 
 */

using UnityEngine;
using UnityEngine.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class MusicStorage
	{

		#region Variables

		/** A unique identifier */
		public int ID;
		/** An Editor label */
		public string label;
		/** The music's AudioClip */
		public AudioClip audioClip;
		/** The relative volume to play the music at, as a decimal of the global music volume */
		public float relativeVolume;
		/** If assigned, the AudioMixerGroup to use when this track is played - as opposed to the default. For this to be used, the use of mixer groups must be enabled in the Settings Manager */
		public AudioMixerGroup overrideMixerGroup = null;

		#endregion


		#region PublicFunctions

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public MusicStorage (int[] idArray)
		{
			label = string.Empty;
			ID = 0;
			audioClip = null;
			relativeVolume = 1f;
			
			// Update id based on array
			if (idArray != null && idArray.Length > 0)
			{
				foreach (int _id in idArray)
				{
					if (ID == _id)
						ID ++;
				}
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI (string apiPrefix, bool allowMixerGroups)
		{
			label = CustomGUILayout.TextField ("Label:", label);
			audioClip = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Clip:", audioClip, false, apiPrefix + ".audioClip", "The audio clip associated with this track");
			relativeVolume = CustomGUILayout.Slider ("Relative volume:", relativeVolume, 0f, 1f, apiPrefix + ".relativeVolume", "The volume to play this track at, relative to the global volume setting for this sound type");

			if (allowMixerGroups)
			{
				overrideMixerGroup = (AudioMixerGroup) CustomGUILayout.ObjectField <AudioMixerGroup> ("Mixer group (override):", overrideMixerGroup, false, apiPrefix + ".overrideMixerGroup", "If set, this mixer group will be used instead of the default used by this sound type");
			}
		}

		#endif


		#region GetSet

		public string Label
		{
			get
			{
				if (string.IsNullOrEmpty (label))
				{
					if (audioClip)
					{
						return audioClip.name;
					}
					return "Untitled";
				}
				return label;
			}
		}

		#endregion

	}

}