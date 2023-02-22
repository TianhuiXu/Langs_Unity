/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"OptionsData.cs"
 * 
 *	This script contains any variables we want to appear in our Options menu.
 * 
 */

using System.Collections.Generic;
using System.Text;

namespace AC
{

	/**
	 * A data container for all variables stored as Options data, and those associated with player profiles and save game filenames.
	 */
	[System.Serializable]
	public class OptionsData
	{

		/** The current language, represented by an index of languages in SpeechManager */
		public int language;
		/** The current voice language, represented by an index of languages in SpeechManager. Note that this will only be used if SpeechManager.separateVoiceAndTextLanguages = True */
		public int voiceLanguage;
		/** True if subtitles are enabled */
		public bool showSubtitles;
		/** The current SFX volume (ranges from 0 to 1) */
		public float sfxVolume;
		/** The current music volume (ranges from 0 to 1) */
		public float musicVolume;
		/** The current speech volume (ranges from 0 to 1) */
		public float speechVolume;
		/** A condensed string representing the values of all Global Variables that link to Options Data */
		public string linkedVariables = "";
		/** A condensed string representing the labels of all save game files */
		public string saveFileNames = "";
		/** A unique identifier of the last save game to be written */
		public int lastSaveID = -1;
		/** An array of save IDs that were written to in order (except the last, which is recorded in lastSaveID) */
		public string previousSaveIDs = "";
		/** The name of the profile associated with this instance */
		public string label;	
		/** A unique identifier */
		public int ID;
		

		/** The default Constructor. */
		public OptionsData ()
		{
			language = 0;
			voiceLanguage = 0;
			showSubtitles = false;
			
			sfxVolume = 0.9f;
			musicVolume = 0.6f;
			speechVolume = 1f;

			linkedVariables = string.Empty;
			saveFileNames = string.Empty;
			lastSaveID = -1;
			previousSaveIDs = string.Empty;

			ID = 0;
			label = "Profile " + (ID + 1).ToString ();
		}


		/** A Constructor with default values, except the ProfileID, which is explicitly set. */
		public OptionsData (int _ID)
		{
			language = 0;
			voiceLanguage = 0;
			showSubtitles = false;
			
			sfxVolume = 0.9f;
			musicVolume = 0.6f;
			speechVolume = 1f;

			linkedVariables = string.Empty;
			saveFileNames = string.Empty;
			lastSaveID = -1;
			previousSaveIDs = string.Empty;

			ID = _ID;
			label = "Profile " + (ID + 1).ToString ();
		}


		/** A Constructor in which the basic options values are explicitly set. */
		public OptionsData (int _language, int _voiceLanguage, bool _showSubtitles, float _sfxVolume, float _musicVolume, float _speechVolume, int _ID)
		{
			language = _language;
			voiceLanguage = _voiceLanguage;
			showSubtitles = _showSubtitles;

			sfxVolume = _sfxVolume;
			musicVolume = _musicVolume;
			speechVolume = _speechVolume;

			linkedVariables = string.Empty;
			saveFileNames = string.Empty;
			lastSaveID = -1;
			previousSaveIDs = string.Empty;

			ID = _ID;
			label = "Profile " + (ID + 1).ToString ();
		}


		/** A Constructor in which the basic options values are copied from another instance of OptionsData. */
		public OptionsData (OptionsData _optionsData, int _ID)
		{
			language = _optionsData.language;
			voiceLanguage = _optionsData.voiceLanguage;
			showSubtitles = _optionsData.showSubtitles;
			
			sfxVolume = _optionsData.sfxVolume;
			musicVolume = _optionsData.musicVolume;
			speechVolume = _optionsData.speechVolume;
			
			linkedVariables = _optionsData.linkedVariables;
			saveFileNames = _optionsData.saveFileNames;
			lastSaveID = -1;
			previousSaveIDs = string.Empty;

			ID =_ID;
			label = "Profile " + (ID + 1).ToString ();
		}


		public void SetPreviousSaveIDs (List<int> saveIDs)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < saveIDs.Count; i++)
			{
				sb.Append (saveIDs[i]);
				if (i < (saveIDs.Count - 1))
				{
					sb.Append (";");
				}
			}
			previousSaveIDs = sb.ToString ();
		}


		public List<int> GetPreviousSaveIDs ()
		{
			List<int> saveIDs = new List<int>();

			if (!string.IsNullOrEmpty (previousSaveIDs))
			{
				string[] splitData = previousSaveIDs.Split (";"[0]);
				if (splitData != null)
				{
					foreach (string s in splitData)
					{
						int saveID = 0;
						if (int.TryParse (s, out saveID))
						{
							saveIDs.Add (saveID);
						}
					}
				}
			}

			return saveIDs;
		}

	}

}