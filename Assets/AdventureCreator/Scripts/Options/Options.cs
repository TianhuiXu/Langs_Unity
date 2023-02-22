/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Options.cs"
 * 
 *	This script provides a runtime instance of OptionsData,
 *	and has functions for saving and loading this data
 *	into the PlayerPrefs
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Stores the local instances of OptionsData, and provides functions for saving and loading options and profiles to and from the PlayerPrefs.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_options.html")]
	public class Options : MonoBehaviour
	{

		/** A local copy of the currently-active profile */
		public static OptionsData optionsData;

		/** The maximum number of profiles that can be created */
		public const int maxProfiles = 50;

		protected static iOptionsFileHandler optionsFileHandlerOverride;


		protected void OnEnable ()
		{
			EventManager.OnInitialiseScene += OnInitialiseScene;
		}


		protected void OnDisable ()
		{
			EventManager.OnInitialiseScene -= OnInitialiseScene;
		}


		public void OnInitPersistentEngine ()
		{
			LoadPrefs ();
			
			KickStarter.runtimeLanguages.LoadAssetBundle (GetVoiceLanguage ());
			KickStarter.eventManager.Call_OnChangeLanguage (optionsData.language);
		}
		

		/**
		 * <summary>Saves the default options data (i.e. the values chosen in SettingsManager) to the default profile.</summary>
		 * <param name = "defaultOptionsData">An instance of OptionsData that represents default values</param>
		 */
		public static void SaveDefaultPrefs (OptionsData defaultOptionsData)
		{
			SavePrefsToID (0, defaultOptionsData, false);
		}
		

		/**
		 * <summary>Loads the OptionsData from the default profile.</summary>
		 * <returns>An instance of OptionsData used by the default profile</returns>
		 */
		public static OptionsData LoadDefaultPrefs ()
		{
			return LoadPrefsFromID (0, false, false);
		}
		

		/**
		 * Deletes the default profile.
		 */
		public static void DeleteDefaultProfile ()
		{
			DeleteProfilePrefs (0);
		}
		

		/**
		 * <summary>Saves the current options to the active profile.</summary>
		 * <param name = "updateVariables">If True, then the values of variables linked to options data will be updated in the options data</param>
		 */
		public static void SavePrefs (bool updateVariables = true)
		{
			if (Application.isPlaying && updateVariables)
			{
				// Linked Variables
				GlobalVariables.DownloadAll ();
				if (KickStarter.runtimeVariables)
				{
					optionsData.linkedVariables = SaveSystem.CreateVariablesData (KickStarter.runtimeVariables.globalVars, true, VariableLocation.Global);
				}
			}
			SavePrefsToID (GetActiveProfileID (), null, true);
		}
		

		/**
		 * <summary>Saves specific options to a specific profile.</summary>
		 * <param name = "ID">A unique identifier for the profile to save to</param>
		 * <param name = "_optionsData">An instance of OptionsData containing the options to save</param>
		 * <param name = "showLog">If True, the details of this save will be printed in the Console window</param>
		 */
		public static void SavePrefsToID (int ID, OptionsData _optionsData = null, bool showLog = false)
		{
			if (_optionsData == null)
			{
				_optionsData = Options.optionsData;
			}

			string optionsSerialized = Serializer.SerializeObject <OptionsData> (_optionsData, true, SaveSystem.OptionsFileFormatHandler);
			if (!string.IsNullOrEmpty (optionsSerialized))
			{
				OptionsFileHandler.SaveOptions (ID, optionsSerialized, showLog);
			}
		}
		

		/** Sets the options values to those stored within the active profile. */
		public static void LoadPrefs ()
		{
			optionsData = LoadPrefsFromID (GetActiveProfileID (), Application.isPlaying, true);
			if (optionsData == null)
			{
				ACDebug.LogWarning ("No Options Data found!");
			}
			else if (KickStarter.runtimeLanguages)
			{
				int numLanguages = (Application.isPlaying) ? KickStarter.runtimeLanguages.Languages.Count : AdvGame.GetReferences ().speechManager.Languages.Count;
				if (optionsData.language >= numLanguages)
				{
					if (numLanguages != 0)
					{
						ACDebug.LogWarning ("Language set to an invalid index - reverting to original language.");
					}
					optionsData.language = 0;
					SavePrefs (false);
				}
				if (optionsData.voiceLanguage >= numLanguages && KickStarter.speechManager && KickStarter.speechManager.separateVoiceAndTextLanguages)
				{
					if (numLanguages != 0)
					{
						ACDebug.LogWarning ("Voice language set to an invalid index - reverting to original language.");
					}
					optionsData.voiceLanguage = 0;
					SavePrefs (false);
				}

				if (KickStarter.speechManager && KickStarter.runtimeLanguages.Languages[optionsData.language].isDisabled)
				{
					int newLanguage = KickStarter.runtimeLanguages.GetEnabledLanguageIndex (optionsData.language);
					if (optionsData.language > 0) Debug.LogWarning ("Language #" + optionsData.language + " is disabled. Switching to #" + newLanguage);
					optionsData.language = newLanguage;
					SavePrefs (false);
				}
				if (KickStarter.speechManager && KickStarter.speechManager.separateVoiceAndTextLanguages && KickStarter.runtimeLanguages.Languages[optionsData.voiceLanguage].isDisabled)
				{
					int newLanguage = KickStarter.runtimeLanguages.GetEnabledLanguageIndex (optionsData.voiceLanguage);
					if (optionsData.voiceLanguage > 0) Debug.LogWarning ("Voice language #" + optionsData.voiceLanguage + " is disabled. Switching to #" + newLanguage);
					optionsData.voiceLanguage = newLanguage;
					SavePrefs (false);
				}

				KickStarter.eventManager.Call_OnChangeLanguage (optionsData.language);
				KickStarter.eventManager.Call_OnChangeSubtitles (optionsData.showSubtitles);
				KickStarter.eventManager.Call_OnChangeVolume (SoundType.Music, optionsData.musicVolume);
				KickStarter.eventManager.Call_OnChangeVolume (SoundType.SFX, optionsData.sfxVolume);
				KickStarter.eventManager.Call_OnChangeVolume (SoundType.Speech, optionsData.speechVolume);
			}
			
			if (Application.isPlaying && KickStarter.saveSystem)
			{
				KickStarter.saveSystem.GatherSaveFiles ();
			}
		}


		/**
		 * <summary>Gets the options values associated with a specific profile.</summary>
		 * <param name = "profileID">A unique identifier for the profile to save to</param>
		 * <param name = "showLog">If True, the details of this save will be printed in the Console window</param>
		 * <param name = "doSave">If True, and if the profile had no OptionsData to read, then new values will be saved to it</param>
		 * <returns>An instance of OptionsData containing the profile's options</returns>
		 */
		public static OptionsData LoadPrefsFromID (int profileID, bool showLog = false, bool doSave = true)
		{
			if (DoesProfileIDExist (profileID))
			{
				string optionsSerialized = OptionsFileHandler.LoadOptions (profileID, showLog);
				
				if (!string.IsNullOrEmpty (optionsSerialized))
				{
					try
					{
						return Serializer.DeserializeOptionsData (optionsSerialized);
					}
					catch (System.Exception e)
					{
						ACDebug.LogWarning ("Error retrieving OptionsData for profile #" + profileID + " - rebuilding..\nException: " + e);

						OptionsData fallbackOptionsData = new OptionsData (profileID);
						if (KickStarter.settingsManager)
						{
							fallbackOptionsData = GenerateDefaultOptionsData (profileID);
						}
						SavePrefsToID (profileID, fallbackOptionsData);
						return fallbackOptionsData;
					}
				}
			}
			
			// No data exists, so create new
			if (KickStarter.settingsManager == null)
			{
				return null;
			}

			OptionsData _optionsData = GenerateDefaultOptionsData (profileID);
			if (doSave)
			{
				optionsData = _optionsData;
				SavePrefs ();
			}
			
			return _optionsData;
		}


		private static OptionsData GenerateDefaultOptionsData (int profileID)
		{
			return new OptionsData (KickStarter.settingsManager.defaultLanguage,
									KickStarter.settingsManager.defaultVoiceLanguage,
									KickStarter.settingsManager.defaultShowSubtitles,
									KickStarter.settingsManager.defaultSfxVolume,
									KickStarter.settingsManager.defaultMusicVolume,
									KickStarter.settingsManager.defaultSpeechVolume,
									profileID);
		}


		/** Displays Options-related information for the AC Status window */
		public static void DrawStatus ()
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				GUILayout.Label ("Current profile ID: " + GetActiveProfileID ());
			}
		}


		/**
		 * <summary>Switches to a specific profile, provided that it exists.</summary>
		 * <param name = "index">The index of profiles in a MenuProfilesList element that represents the profile to switch to</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that contains the profile to switch to also lists the active profile</param>
		 * <returns>True if the switch was successful</returns>
		 */
		public bool SwitchProfile (int index, bool includeActive)
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				int profileID = ProfileIndexToID (index, includeActive);

				if (DoesProfileIDExist (profileID))
				{
					return SwitchProfileID (profileID);
				}
				ACDebug.Log ("Profile switch failed - " + index + " doesn't exist");
			}
			return false;
		}
		

		/**
		 * <summary>Converts a profile's index in a MenuProfilesList element to an ID number.</summary>
		 * <param name = "index">The index of profiles in a MenuProfilesList element that represents the profile to switch to</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that contains the profile to switch to also lists the active profile</param>
		 * <returns>The profile's unique identifier</returns>
		 */
		public int ProfileIndexToID (int index, bool includeActive = true)
		{
			for (int i=0; i<maxProfiles; i++)
			{
				if (DoesProfileIDExist (i))
				{
					if (!includeActive && i == GetActiveProfileID ())
					{}
					else
					{
						index --;
					}
				}
				
				if (index < 0)
				{
					return i;
				}
			}
			return -1;
		}
		

		/**
		 * <summary>Gets the ID number of the active profile.</summary>
		 * <returns>The active profile's unique identifier</returns>
		 */
		public static int GetActiveProfileID ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.useProfiles)
			{
				return OptionsFileHandler.GetActiveProfile ();
			}
			return 0;
		}
		

		/**
		 * <summary>Sets the ID number of the active profile.</summary>
		 * <param name = "profileID">A unique identifier for the profile</param>
		 */
		public static void SetActiveProfileID (int profileID)
		{
			OptionsFileHandler.SetActiveProfile (profileID);
		}
		
		
		protected int FindFirstEmptyProfileID ()
		{
			for (int i=0; i<maxProfiles; i++)
			{
				if (!DoesProfileIDExist (i))
				{
					return i;
				}
			}
			return 0;
		}
		

		/**
		 * <summary>Creates a new profile (instance of OptionsData).</summary>
		 * <param name = "_label">The name of the new profile.</param>
		 * <returns>The ID number of the new profile</returns>
		 */
		public int CreateProfile (string _label = "")
		{
			int newProfileID = FindFirstEmptyProfileID ();
			
			OptionsData newOptionsData = new OptionsData (optionsData, newProfileID);
			if (!string.IsNullOrEmpty (_label))
			{
				newOptionsData.label = _label;
			}
			optionsData = newOptionsData;

			SetActiveProfileID (newProfileID);
			SavePrefs ();
				
			if (Application.isPlaying)
			{
				KickStarter.saveSystem.GatherSaveFiles ();
				KickStarter.playerMenus.RecalculateAll ();
			}

			return newProfileID;
		}


		/**
		 * <summary>Renames a profile by referencing its entry in a MenuProfilesList element.</summary>
		 * <param name = "newProfileLabel">The new label for the profile</param>
		 * <param name = "profileIndex">The index in the MenuProfilesList element that represents the profile to rename. If it is set to its default, -2, the active profile will be renamed</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 */
		public void RenameProfile (string newProfileLabel, int profileIndex = -2, bool includeActive = true)
		{
			if (!KickStarter.settingsManager.useProfiles || string.IsNullOrEmpty (newProfileLabel))
			{
				return;
			}
			
			int profileID = KickStarter.options.ProfileIndexToID (profileIndex, includeActive);
			if (profileID == -1)
			{
				ACDebug.LogWarning ("Invalid profile index: " + profileIndex + " - nothing to delete!");
				return;
			}
			else if (profileIndex == -2)
			{
				profileID = Options.GetActiveProfileID ();
			}

			RenameProfileID (newProfileLabel, profileID);
		}


		/**
		 * <summary>Renames a profile ID.</summary>
		 * <param name = "newProfileLabel">The new label for the profile</param>
		 * <param name = "profileID">The profile ID to rename</param>
		 */
		public void RenameProfileID (string newProfileLabel, int profileID)
		{
			if (!KickStarter.settingsManager.useProfiles || string.IsNullOrEmpty (newProfileLabel))
			{
				return;
			}
			
			if (profileID == GetActiveProfileID ())
			{
				optionsData.label = newProfileLabel;
				SavePrefs ();
			}
			else if (DoesProfileIDExist (profileID))
			{
				OptionsData tempOptionsData = LoadPrefsFromID (profileID, false);
				tempOptionsData.label = newProfileLabel;
				SavePrefsToID (profileID, tempOptionsData, true);
			}
			else
			{
				ACDebug.LogWarning ("Cannot rename profile " + profileID + " as it does not exist!");
				return;
			}

			KickStarter.playerMenus.RecalculateAll ();
		}


		/**
		 * <summary>Gets the name of a specific profile.</summary>
		 * <param name = "index">The index in the MenuProfilesList element that represents the profile to get the name of.</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 * <returns>The display name of the profile</returns>
		 */
		public string GetProfileName (int index = -1, bool includeActive = true)
		{
			if (index == -1 || !KickStarter.settingsManager.useProfiles)
			{
				if (Options.optionsData == null)
				{
					LoadPrefs ();
				}
				return AdvGame.ConvertTokens (Options.optionsData.label);
			}

			int profileID = KickStarter.options.ProfileIndexToID (index, includeActive);
			return AdvGame.ConvertTokens (GetProfileIDName (profileID));
		}


		/**
		 * <summary>Gets the name of a specific profile ID.</summary>
		 * <param name = "profileID">The profile ID to get the name of</param>
		 * <returns>The display name of the profile</returns>
		 */
		public string GetProfileIDName (int profileID)
		{
			if (!KickStarter.settingsManager.useProfiles)
			{
				if (Options.optionsData == null)
				{
					LoadPrefs ();
				}
				return AdvGame.ConvertTokens (Options.optionsData.label);
			}

			if (DoesProfileIDExist (profileID))
			{
				OptionsData tempOptionsData = LoadPrefsFromID (profileID, false, false);
				return AdvGame.ConvertTokens (tempOptionsData.label);
			}
			else
			{
				return string.Empty;
			}
		}

		

		/**
		 * <summary>Gets the number of profiles associated with the game.</summary>
		 * <returns>The number of profiles found</returns>
		 */
		public int GetNumProfiles ()
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				int count = 0;
				for (int i=0; i<maxProfiles; i++)
				{
					if (DoesProfileIDExist (i))
					{
						count ++;
					}
				}
				//return count;
				return Mathf.Max (1, count);
			}
			return 1;
		}
		

		/**
		 * <summary>Deletes the PlayerPrefs key associated with a specfic profile</summary>
		 * <param name = "profileID">The unique identifier of the profile to delete</param>
		 */
		public static void DeleteProfilePrefs (int profileID)
		{
			bool isDeletingCurrentProfile = (profileID == GetActiveProfileID ());

			OptionsFileHandler.DeleteOptions (profileID);

			if (isDeletingCurrentProfile)
			{
				for (int i=0; i<maxProfiles; i++)
				{
					if (DoesProfileIDExist (i))
					{
						SwitchProfileID (i);
						return;
					}
				}
				
				// No other profile found, create new
				SwitchProfileID (0);
			}
		}


		/**
		 * <summary>Checks if a profile exists.</summary>
		 * <param name = "index">The index in the MenuProfilesList element that represents the profile to search for.</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 * <returns>True if the profile exists</returns>
		 */
		public bool DoesProfileExist (int index, bool includeActive = true)
		{
			if (index < 0) return false;

			int profileID = KickStarter.options.ProfileIndexToID (index, includeActive);
			return DoesProfileIDExist (profileID);
		}


		/**
		 * <summary>Checks if a specific profile ID exists.</summary>
		 * <param name = "profileID">The profile ID to check for</param>
		 * <returns>True if the given profile ID exists</returns>
		 */
		public static bool DoesProfileIDExist (int profileID)
		{
			if (KickStarter.settingsManager && !KickStarter.settingsManager.useProfiles)
			{
				profileID = 0;
			}

			return OptionsFileHandler.DoesProfileExist (profileID);
		}


		/**
		 * <summary>Checks if a profile with a specific name exists</summary>
		 * <param name = "label">The name of the profile to check for</param>
		 * <returns>True if a profile with the supplied name exists</returns>
		 */
		public bool DoesProfileExist (string label)
		{
			if (string.IsNullOrEmpty (label))
			{
				return false;
			}

			if (KickStarter.settingsManager && !KickStarter.settingsManager.useProfiles)
			{
				return false;
			}

			for (int i=0; i<maxProfiles; i++)
			{
				string profileName = GetProfileName (i);
				if (profileName == label)
				{
					return true;
				}
			}

			return false;
		}
		

		/**
		 * <summary>Switches to a specific profile ID, provided that it exists.</summary>
		 * <param name = "profileID">The unique identifier of the profile to switch to</param>
		 * <returns>True if the switch was successful</returns>
		 */
		public static bool SwitchProfileID (int profileID)
		{
			if (!Options.DoesProfileIDExist (profileID))
			{
				ACDebug.LogWarning ("Cannot switch to profile ID " + profileID.ToString () + ", as it has not been created.");
				return false;
			}

			SetActiveProfileID (profileID);
			LoadPrefs ();

			ACDebug.Log ("Switched to profile " + profileID.ToString () + ": '" + optionsData.label + "'");
			
			if (Application.isPlaying)
			{
				KickStarter.saveSystem.GatherSaveFiles ();
				KickStarter.playerMenus.RecalculateAll ();
				KickStarter.runtimeVariables.AssignOptionsLinkedVariables ();
			}

			KickStarter.eventManager.Call_OnSwitchProfile (profileID);

			return true;
		}
		

		/**
		 * <summary>Updates the labels of all save files by storing them in the profile's OptionsData.</summary>
		 * <param name = "foundSaveFiles">An array of SaveFile instances, that represent the found save game files found on disk</param>
		 */
		public static void UpdateSaveLabels (SaveFile[] foundSaveFiles)
		{
			System.Text.StringBuilder newSaveNameData = new System.Text.StringBuilder ();

			if (foundSaveFiles != null)
			{
				foreach (SaveFile saveFile in foundSaveFiles)
				{
					newSaveNameData.Append (saveFile.saveID.ToString ());
					newSaveNameData.Append (SaveSystem.colon);
					newSaveNameData.Append (saveFile.GetSafeLabel ());
					newSaveNameData.Append (SaveSystem.pipe);
				}
				
				if (foundSaveFiles.Length > 0)
				{
					newSaveNameData.Remove (newSaveNameData.Length - 1, 1);
				}
			}

			optionsData.saveFileNames = newSaveNameData.ToString ();
			SavePrefs ();
		}
		
		
		protected void OnInitialiseScene ()
		{
			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				StartCoroutine (UpdateMixerVolumes ());
			}

			SetVolume (SoundType.Music);
			SetVolume (SoundType.SFX);
			SetVolume (SoundType.Speech);
		}


		protected IEnumerator UpdateMixerVolumes ()
		{
			yield return null;

			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				if (optionsData == null)
				{
					LoadPrefs ();
				}
				AdvGame.SetMixerVolume (KickStarter.settingsManager.musicMixerGroup, KickStarter.settingsManager.musicAttentuationParameter, optionsData.musicVolume);
				AdvGame.SetMixerVolume (KickStarter.settingsManager.sfxMixerGroup, KickStarter.settingsManager.sfxAttentuationParameter, optionsData.sfxVolume);
				AdvGame.SetMixerVolume (KickStarter.settingsManager.speechMixerGroup, KickStarter.settingsManager.speechAttentuationParameter, optionsData.speechVolume);
			}
		}
		

		/**
		 * <summary>Updates the volume of all Sound object of a specific SoundType to their correct values.</summary>
		 * <param name = "_soundType">The SoundType that matches the Sound objects to update (Music, SFX, Other)</param>
		 * <param name = "newVolume">If >= 0, the OptionsData will be updated as well</param>
		 */
		public void SetVolume (SoundType _soundType, float newVolume = -1f)
		{
			if (newVolume >= 0f)
			{
				if (optionsData != null)
				{
					if (_soundType == SoundType.Music)
					{
						optionsData.musicVolume = newVolume;
					}
					else if (_soundType == SoundType.SFX)
					{
						optionsData.sfxVolume = newVolume;
					}
					else if (_soundType == SoundType.Speech)
					{
						optionsData.speechVolume = newVolume;
					}

					SavePrefs ();

					KickStarter.eventManager.Call_OnChangeVolume (_soundType, newVolume);
				}
				else
				{
					ACDebug.LogWarning ("Could not find Options data!");
				}
			}
		}
		

		/**
		 * <summary>Changes the currently-selected language. If voice and text languages are synced (which is the default), then both will be updated</summary>
		 * <param name = "i">The language's index number in SpeechManager</param>
		 */
		public static void SetLanguage (int i)
		{
			if (KickStarter.runtimeLanguages)
			{
				i = KickStarter.runtimeLanguages.GetEnabledLanguageIndex (i);
			}

			if (Options.optionsData != null)
			{
				Options.optionsData.language = i;
				Options.SavePrefs ();

				KickStarter.eventManager.Call_OnChangeLanguage (i);
				KickStarter.runtimeLanguages.LoadAssetBundle (GetVoiceLanguage ());
			}
			else
			{
				ACDebug.LogWarning ("Could not find Options data!");
			}
		}


		/**
		 * <summary>Changes the currently-selected voice language. If voice and text languages are synced, the text language will be set to this as well.</summary>
		 * <param name = "i">The language's index number in SpeechManager</param>
		 */
		public static void SetVoiceLanguage (int i)
		{
			if (KickStarter.speechManager == null || !KickStarter.speechManager.separateVoiceAndTextLanguages)
			{
				SetLanguage (i);
				return;
			}

			if (KickStarter.runtimeLanguages)
			{
				i = KickStarter.runtimeLanguages.GetEnabledLanguageIndex (i);
			}

			if (Options.optionsData != null)
			{
				Options.optionsData.voiceLanguage = i;
				Options.SavePrefs ();

				KickStarter.eventManager.Call_OnChangeVoiceLanguage (i);
				KickStarter.runtimeLanguages.LoadAssetBundle (i);
			}
			else
			{
				ACDebug.LogWarning ("Could not find Options data!");
			}
		}


		/**
		 * <summary>Changes the subtitle display setting.</summary>
		 * <param name = "showSubtitles">If True, subtitles will be shown</param>
		 */
		public static void SetSubtitles (bool showSubtitles)
		{
			if (Options.optionsData != null)
			{
				Options.optionsData.showSubtitles = showSubtitles;
				Options.SavePrefs ();

				KickStarter.eventManager.Call_OnChangeSubtitles (showSubtitles);
			}
			else
			{
				ACDebug.LogWarning ("Could not find Options data!");
			}
		}


		/**
		 * <summary>Sets the value of the 'SFX volume'.</summary>
		 * <param name = "newVolume">The new value of the 'SFX volume'</param>
		 */
		public static void SetSFXVolume (float newVolume)
		{
			KickStarter.options.SetVolume (SoundType.SFX, newVolume);
			AdvGame.SetMixerVolume (KickStarter.settingsManager.sfxMixerGroup, KickStarter.settingsManager.sfxAttentuationParameter, newVolume);
		}


		/**
		 * <summary>Sets the value of the 'Speech volume'.</summary>
		 * <param name = "newVolume">The new value of the 'Speech volume'</param>
		 */
		public static void SetSpeechVolume (float newVolume)
		{
			KickStarter.options.SetVolume (SoundType.Speech, newVolume);
			AdvGame.SetMixerVolume (KickStarter.settingsManager.speechMixerGroup, KickStarter.settingsManager.speechAttentuationParameter, newVolume);
		}


		/**
		 * <summary>Sets the value of the 'Music volume'.</summary>
		 * <param name = "newVolume">The new value of the 'Music volume'</param>
		 */
		public static void SetMusicVolume (float newVolume)
		{
			KickStarter.options.SetVolume (SoundType.Music, newVolume);
			AdvGame.SetMixerVolume (KickStarter.settingsManager.musicMixerGroup, KickStarter.settingsManager.musicAttentuationParameter, newVolume);
		}
		

		/**
		 * <summary>Gets the name of the currently-selected language.</summary>
		 * <returns>The name of the currently-selected language, as defined in SpeechManager</returns>
		 */
		public static string GetLanguageName ()
		{
			return KickStarter.runtimeLanguages.Languages [GetLanguage ()].name;
		}


		/**
		 * <summary>Gets the name of the currently-selected voice language.</summary>
		 * <returns>The name of the currently-selected language, as defined in SpeechManager. If voice and text languages are synced, this will return the same as GetLanguageName().</returns>
		 */
		public static string GetVoiceLanguageName ()
		{
			return KickStarter.runtimeLanguages.Languages [GetVoiceLanguage ()].name;
		}
		

		/**
		 * <summary>Gets the index number of the currently-selected language.</summary>
		 * <returns>The language's index number in SpeechManager.</returns>
		 */
		public static int GetLanguage ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.language;
			}
			return 0;
		}


		/**
		 * <summary>Gets the index number of the currently-selected voice language.</summary>
		 * <returns>The voice language's index number in SpeechManager. If voice and text languages are synced, this will return the same as GetLanguage().</returns>
		 */
		public static int GetVoiceLanguage ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				if (KickStarter.speechManager)
				{
					if (!KickStarter.speechManager.translateAudio)
					{
						return 0;
					}
					if (KickStarter.speechManager.separateVoiceAndTextLanguages)
					{
						return optionsData.voiceLanguage;
					}
				}
				return optionsData.language;
			}
			return 0;
		}


		/**
		 * <summary>Checks if subtitles are enabled</summary>
		 * <returns>True if subtitles are enabled</returns>
		 */
		public static bool AreSubtitlesOn ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.showSubtitles;
			}
			return false;
		}


		/**
		 * <summary>Gets the current value of the 'SFX volume'.</summary>
		 * <returns>The current value of the 'SFX volume', as defined in the current instance of OptionsData</returns>
		 */
		public static float GetSFXVolume ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.sfxVolume;
			}
			return 1f;
		}


		/**
		 * <summary>Gets the current value of the 'Music volume'.</summary>
		 * <returns>The current value of the 'Music volume', as defined in the current instance of OptionsData</returns>
		 */
		public static float GetMusicVolume ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.musicVolume;
			}
			return 1f;
		}


		/**
		 * <summary>Gets the current value of the 'Speech volume'.</summary>
		 * <returns>The current value of the 'Speech volume', as defined in the current instance of OptionsData</returns>
		 */
		public static float GetSpeechVolume ()
		{
			if (Application.isPlaying && optionsData != null)
			{
				return optionsData.speechVolume;
			}
			return 1f;
		}


		/**
		 * <summary>Gets the variable associated with a specific profile</summary>
		 * <param name="profileID">The ID of the profile</param>
		 * <param name="variableID">The ID of the Global variable</param>
		 * <returns>The variable associated with the profile</returns>
		 */
		public static GVar GetProfileVariable (int profileID, int variableID)
		{
			string optionsSerialized = OptionsFileHandler.LoadOptions (profileID, false);
			if (!string.IsNullOrEmpty (optionsSerialized))
			{
				OptionsData optionsData = Serializer.DeserializeOptionsData (optionsSerialized);
				if (optionsData != null)
				{
					List<GVar> variables = SaveSystem.UnloadVariablesData (optionsData.linkedVariables, false, KickStarter.runtimeVariables.globalVars, true);
					foreach (GVar variable in variables)
					{
						if (variable.id == variableID)
						{
							return variable;
						}
					}
				}
			}
			return null;
		}


		/** The iOptionsFileHandler class that handles the creation, loading, and deletion of save files */
		public static iOptionsFileHandler OptionsFileHandler
		{
			get
			{
				if (optionsFileHandlerOverride != null)
				{
					return optionsFileHandlerOverride;
				}

				return new OptionsFileHandler_PlayerPrefs ();
			}
			set
			{
				optionsFileHandlerOverride = value;
				LoadPrefs ();
			}
		}
		
	}
	
}