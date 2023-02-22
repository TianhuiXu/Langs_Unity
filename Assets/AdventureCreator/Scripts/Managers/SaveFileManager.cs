#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	public class SaveFileManager : EditorWindow
	{

		private SettingsManager settingsManager;
		
		private bool showHandlers = true;
		private bool showProfiles = true;
		private bool showProfile = true;
		private bool showSaves = true;
		private bool showSave = true;
		private bool showSaveData = false;
		private Vector2 _scrollPos;

		private int selectedProfileID = 0;
		private int selectedSaveIndex = -1;
		private List<SaveFile> foundSaveFiles = new List<SaveFile> ();
		private SaveData cachedSaveData;
		private List<SingleLevelData> cachedLevelData;

		private bool runCache = false;


		public static void Init ()
		{
			SaveFileManager window = (SaveFileManager) GetWindow (typeof (SaveFileManager));
			window.titleContent.text = "Save-game Manager";
			window.position = new Rect (300, 200, 450, 660);
			window.minSize = new Vector2 (300, 180);
		}


		private void OnGUI ()
		{
			settingsManager = AdvGame.GetReferences ().settingsManager;

			if (settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			_scrollPos = EditorGUILayout.BeginScrollView (_scrollPos);
			SaveFileGUI ();
			EditorGUILayout.EndScrollView ();
		}


		private void SaveFileGUI ()
		{
			iSaveFileHandler saveFileHandler = SaveSystem.SaveFileHandler;
			iOptionsFileHandler optionsFileHandler = Options.OptionsFileHandler;
			iFileFormatHandler fileFormatHandler = SaveSystem.FileFormatHandler;
			iFileFormatHandler optionsFileFormatHandler = SaveSystem.OptionsFileFormatHandler;

			if (optionsFileHandler == null)
			{
				EditorGUILayout.HelpBox ("No Options File Handler assigned - one must be set in order to locate Profile Data.", MessageType.Warning);
				return;
			}

			if (saveFileHandler == null)
			{
				EditorGUILayout.HelpBox ("No Save File Handler assigned - one must be set in order to locate Save Data.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField ("Save-game file manager", CustomStyles.managerHeader);

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showHandlers = CustomGUILayout.ToggleHeader (showHandlers, "File and format handlers");
			if (showHandlers)
			{
				if (saveFileHandler != null)
				{
					EditorGUILayout.LabelField ("Save file location:", saveFileHandler.GetType ().Name);
				}

				if (optionsFileHandler != null)
				{
					EditorGUILayout.LabelField ("Options location:", optionsFileHandler.GetType ().Name);
				}

				if (fileFormatHandler != null)
				{
					EditorGUILayout.LabelField ("File format:", fileFormatHandler.GetType ().Name);
				}

				if (optionsFileFormatHandler != null && fileFormatHandler == null || (optionsFileFormatHandler.GetType ().Name != fileFormatHandler.GetType ().Name))
				{
					EditorGUILayout.LabelField ("Options format:", optionsFileFormatHandler.GetType ().Name);
				}

				EditorGUILayout.HelpBox ("Save format and location handlers can be modified through script - see the Manual's 'Custom save formats and handling' chapter.", MessageType.Info);
			}
			CustomGUILayout.EndVertical ();

			bool foundSome = false;
			if (settingsManager.useProfiles)
			{
				EditorGUILayout.Space ();

				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showProfiles = CustomGUILayout.ToggleHeader (showProfiles, "Profiles");
				if (showProfiles)
				{

					for (int profileID = 0; profileID < Options.maxProfiles; profileID++)
					{
						if (optionsFileHandler.DoesProfileExist (profileID))
						{
							foundSome = true;
							OptionsData tempOptionsData = Options.LoadPrefsFromID (profileID, false, false);

							string label = profileID.ToString () + ": " + tempOptionsData.label;
							if (profileID == Options.GetActiveProfileID ())
							{
								label += " (ACTIVE)";
							}

							if (GUILayout.Toggle (selectedProfileID == profileID, label, "Button"))
							{
								if (selectedProfileID != profileID)
								{
									selectedProfileID = profileID;
									selectedSaveIndex = -1;
									foundSaveFiles.Clear ();
								}
							}
						}
					}

					if (!foundSome)
					{
						selectedProfileID = -1;
						EditorGUILayout.HelpBox ("No save profiles found.", MessageType.Warning);
					}
				}
				CustomGUILayout.EndVertical ();
			}
			else
			{
				selectedProfileID = 0;
				foundSome = true;
			}

			if (foundSome && (selectedProfileID < 0 || !optionsFileHandler.DoesProfileExist (selectedProfileID)))
			{
				EditorGUILayout.HelpBox ("No save profiles found! Run the game to create a new save profile", MessageType.Warning);
				return;
			}

			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showProfile = CustomGUILayout.ToggleHeader (showProfile, "Profile " + selectedProfileID + ": Properties");
			if (showProfile)
			{
				OptionsData prefsData = GetPrefsData (selectedProfileID);
				if (prefsData != null)
				{
					EditorGUILayout.LabelField ("Label:", prefsData.label);
					EditorGUILayout.LabelField ("ID:", prefsData.ID.ToString ());
					EditorGUILayout.LabelField ("Last save ID:", prefsData.lastSaveID.ToString ());
					EditorGUILayout.LabelField ("Previous save IDs:", prefsData.previousSaveIDs);
					EditorGUILayout.LabelField ("Language:", prefsData.language.ToString ());
					if (prefsData.language != prefsData.voiceLanguage)
					{
						EditorGUILayout.LabelField ("Voice language:", prefsData.voiceLanguage.ToString ());
					}
					EditorGUILayout.LabelField ("Show subtitles:", prefsData.showSubtitles.ToString ());
					EditorGUILayout.LabelField ("SFX volume:", prefsData.sfxVolume.ToString ());
					EditorGUILayout.LabelField ("Music volume:", prefsData.musicVolume.ToString ());
					EditorGUILayout.LabelField ("Speech volume:", prefsData.speechVolume.ToString ());
					
					if (KickStarter.variablesManager != null)
					{
						List<GVar> linkedVariables = SaveSystem.UnloadVariablesData (prefsData.linkedVariables, false, KickStarter.variablesManager.vars, true);
						foreach (GVar linkedVariable in linkedVariables)
						{
							if (linkedVariable.link == VarLink.OptionsData)
							{
								EditorGUILayout.LabelField (linkedVariable.label + ":", linkedVariable.GetValue ());
							}
						}
					}
					else
					{
						EditorGUILayout.LabelField ("Linked Variables:", prefsData.linkedVariables);
					}

					EditorGUILayout.BeginHorizontal ();
					if (settingsManager.useProfiles)
					{
						GUI.enabled = (selectedProfileID != Options.GetActiveProfileID ());
						if (GUILayout.Button ("Make active"))
						{
							SwitchActiveProfile (selectedProfileID);
						}
						GUI.enabled = true;
					}
					if (GUILayout.Button ("Delete profile"))
					{
						bool canDelete = EditorUtility.DisplayDialog ("Delete profile?", "Are you sure you want to delete profile #" + selectedProfileID + "? This operation cannot be undone.", "Yes", "No");
						if (canDelete)
						{
							Options.DeleteProfilePrefs (selectedProfileID);
						}
					}
					EditorGUILayout.EndHorizontal ();
				}
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			foundSaveFiles = saveFileHandler.GatherSaveFiles (selectedProfileID);

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSaves = CustomGUILayout.ToggleHeader (showSaves, "Save game files");
			if (showSaves)
			{
				if (foundSaveFiles != null)
				{
					for (int saveIndex = 0; saveIndex < foundSaveFiles.Count; saveIndex++)
					{
						SaveFile saveFile = foundSaveFiles[saveIndex];
						string label = saveFile.saveID.ToString () + ": " + saveFile.label;

						if (GUILayout.Toggle (selectedSaveIndex == saveIndex, label, "Button"))
						{
							selectedSaveIndex = saveIndex;
						}
					}
				}

				if (foundSaveFiles == null || foundSaveFiles.Count == 0)
				{
					selectedSaveIndex = -1;
					EditorGUILayout.HelpBox ("No save game files found.", MessageType.Warning);
				}

				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				GUI.enabled = Application.isPlaying;
				if (GUILayout.Button ("Autosave"))
				{
					if (!PlayerMenus.IsSavingLocked (null, true))
					{
						SwitchActiveProfile (selectedProfileID);
						SaveSystem.SaveAutoSave ();
					}
				}
				if (GUILayout.Button ("Save new"))
				{
					if (!PlayerMenus.IsSavingLocked (null, true))
					{
						SwitchActiveProfile (selectedProfileID);
						SaveSystem.SaveNewGame ();
					}
				}
				GUI.enabled = (foundSaveFiles != null && foundSaveFiles.Count > 0);
				if (GUILayout.Button ("Delete all saves"))
				{
					bool canDelete = EditorUtility.DisplayDialog ("Delete all save files?", "Are you sure you want to delete all save files? This operation cannot be undone.", "Yes", "No");
					if (canDelete)
					{
						saveFileHandler.DeleteAll (selectedProfileID);
					}
				}
				CustomGUILayout.EndVertical ();
			}
			CustomGUILayout.EndVertical ();

			if (selectedSaveIndex < 0 || foundSaveFiles == null || selectedSaveIndex >= foundSaveFiles.Count)
			{
				return;
			}

			EditorGUILayout.Space ();

			SaveFile selectedSaveFile = foundSaveFiles[selectedSaveIndex];

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSave = CustomGUILayout.ToggleHeader (showSave, "Save game " + selectedSaveIndex + ": Properties");
			if (showSave)
			{
				EditorGUILayout.LabelField ("Label:", selectedSaveFile.label);
				EditorGUILayout.LabelField ("ID:", selectedSaveFile.saveID.ToString ());

				CustomGUILayout.MultiLineLabelGUI ("Filename:", selectedSaveFile.fileName);

				EditorGUILayout.LabelField ("Timestamp:", selectedSaveFile.updatedTime.ToString ());
				if (!string.IsNullOrEmpty (selectedSaveFile.screenshotFilename))
				{
					CustomGUILayout.MultiLineLabelGUI ("Filename:", selectedSaveFile.screenshotFilename);
				}
				EditorGUILayout.LabelField ("Is auto-save?", selectedSaveFile.IsAutoSave.ToString ());

				GUILayout.BeginHorizontal ();
				GUI.enabled = Application.isPlaying;
				if (GUILayout.Button ("Load"))
				{
					SwitchActiveProfile (selectedProfileID);
					SaveSystem.LoadGame (0, selectedSaveFile.saveID, true);
				}
				if (GUILayout.Button ("Save over"))
				{
					if (!PlayerMenus.IsSavingLocked (null, true))
					{
						SwitchActiveProfile (selectedProfileID);
						SaveSystem.SaveGame (0, selectedSaveFile.saveID, true);
					}
				}
				GUI.enabled = true;

				if (GUILayout.Button ("Delete"))
				{
					bool canDelete = EditorUtility.DisplayDialog ("Delete save file?", "Are you sure you want to delete the save file " + selectedSaveFile.label + "? This operation cannot be undone.", "Yes", "No");
					if (canDelete)
					{
						saveFileHandler.Delete (selectedSaveFile);
					}
				}
				GUILayout.EndHorizontal ();
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSaveData = CustomGUILayout.ToggleHeader (showSaveData, "Save game " + selectedSaveIndex + ": Data");
			if (showSaveData)
			{
				if (GUI.changed || !runCache)
				{
					CacheSaveData (saveFileHandler, selectedSaveFile);
				}

				if (cachedSaveData != null)
				{
					cachedSaveData.ShowGUI ();
				}

				if (cachedLevelData != null)
				{
					for (int i = 0; i < cachedLevelData.Count; i++)
					{
						CustomGUILayout.DrawUILine ();
						EditorGUILayout.LabelField ("Scene data " + i.ToString () + ":", CustomStyles.subHeader);
						cachedLevelData[i].ShowGUI ();
					}
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void CacheSaveData (iSaveFileHandler saveFileHandler, SaveFile saveFile)
		{
			runCache = true;
			saveFileHandler.Load (saveFile, false, OnCompleteLoadForCache);
		}


		private void OnCompleteLoadForCache (SaveFile saveFile, string fileData)
		{
			cachedSaveData = SaveSystem.ExtractMainData (fileData);
			cachedLevelData = SaveSystem.ExtractSceneData (fileData);
		}


		private void SwitchActiveProfile (int profileID)
		{
			if (Options.GetActiveProfileID () != profileID)
			{
				Options.SwitchProfileID (profileID);
			}
		}


		private OptionsData GetPrefsData (int profileID)
		{
			if (KickStarter.settingsManager.useProfiles)
			{
				return Options.LoadPrefsFromID (profileID, false, false);
			}
			
			if (Options.optionsData == null)
			{
				Options.LoadPrefs ();
			}
			return Options.optionsData;
		}

	}

}

#endif