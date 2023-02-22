/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SaveSystem.cs"
 * 
 *	This script processes saved game data to and from the scene objects.
 * 
 *	It is partially based on Zumwalt's code here:
 *	http://wiki.unity3d.com/index.php?title=Save_and_Load_from_XML
 *	and uses functions by Nitin Pande:
 *	http://www.eggheadcafe.com/articles/system.xml.xmlserialization.asp 
 * 
 */

#if UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII || UNITY_PS4 || UNITY_WSA || UNITY_WEBGL
#define SAVE_IN_PLAYERPREFS
#endif

#if UNITY_IPHONE || UNITY_WP8 || UNITY_WINRT || UNITY_WII || UNITY_PS4
#define SAVE_USING_XML
#endif

#if AddressableIsPresent
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/** Processes save game data to and from scene objects. */
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_save_system.html")]
	public class SaveSystem : MonoBehaviour
	{

		protected LoadingGame _loadingGame;
		/** An List of SaveFile variables, storing all available save files. */
		[HideInInspector] public List<SaveFile> foundSaveFiles = new List<SaveFile> ();
		/** A List of SaveFile variables, storing all available import files. */
		[HideInInspector] public List<SaveFile> foundImportFiles = new List<SaveFile> ();

		public const string pipe = "|";
		public const string colon = ":";
		public const string mainDataDivider = "||";
		public const string mainDataDivider_Replacement = "*DOUBLEPIPE*";

		public const int MAX_SAVES = 50;
		
		private SaveData saveData = new SaveData ();
		private SelectiveLoad activeSelectiveLoad = new SelectiveLoad ();

		private static iSaveFileHandler saveFileHandlerOverride = null;
		private static iFileFormatHandler fileFormatHandlerOverride = null;
		private static iFileFormatHandler optionsFileFormatHandlerOverride = null;

		private SaveFile requestedLoad = null;
		private SaveFile requestedImport = null;
		
		private bool isTakingSaveScreenshot;
		private string persistentDataPath;


		protected void OnEnable ()
		{
			EventManager.OnAddSubScene += OnAddSubScene;
		}


		protected void OnDisable ()
		{
			EventManager.OnAddSubScene -= OnAddSubScene;
		}


		/** Searches the filesystem for all available save files, and stores them in foundSaveFiles. */
		public void GatherSaveFiles ()
		{
			foundSaveFiles = SaveFileHandler.GatherSaveFiles (Options.GetActiveProfileID ());

			if (KickStarter.settingsManager && KickStarter.settingsManager.orderSavesByUpdateTime)
			{
				foundSaveFiles.Sort (delegate (SaveFile a, SaveFile b) { return a.updatedTime.CompareTo (b.updatedTime); });
			}

			UpdateSaveFileLabels ();
		}


		private void UpdateSaveFileLabels ()
		{
			// Now get save file labels
			if (Options.optionsData != null && !string.IsNullOrEmpty (Options.optionsData.saveFileNames))
			{
				string[] profilesArray = Options.optionsData.saveFileNames.Split (SaveSystem.pipe[0]);
				foreach (string chunk in profilesArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);

					int _id = 0;
					int.TryParse (chunkData[0], out _id);
					string _label = chunkData[1];

					for (int i = 0; i < Mathf.Min (MAX_SAVES, foundSaveFiles.Count); i++)
					{
						if (foundSaveFiles[i].saveID == _id)
						{
							SaveFile newSaveFile = new SaveFile (foundSaveFiles[i]);
							newSaveFile.SetLabel (_label);
							foundSaveFiles[i] = newSaveFile;
						}
					}
				}
			}
		}


		/**
		 * <summary>Searches the filesystem for all available import files, and stores them in foundImportFiles.</summary>
		 * <param name = "projectName">The project name of the game whose save files we're looking to import</param>
		 * <param name = "filePrefix">The "save filename" of the game whose save files we're looking to import, as set in the Settings Manager</param>
		 * <param name = "boolID">If >= 0, the ID of the boolean Global Variable that must be True for the file to be considered valid for import</param>
		 */
		public void GatherImportFiles (string projectName, string filePrefix, int boolID)
		{
			#if !UNITY_STANDALONE
			ACDebug.LogWarning ("Cannot import save files unless running on Windows, Mac or Linux standalone platforms.");
			#else
			foundImportFiles = SaveFileHandler.GatherImportFiles (Options.GetActiveProfileID (), boolID, projectName, filePrefix);
			#endif
		}


		/**
		 * <summary>Gets the extension of the current save method.</summary>
		 * <returns>The extension of the current save method</returns>
		 */
		public static string GetSaveExtension ()
		{
			return FileFormatHandler.GetSaveExtension ();
		}


		/**
		 * <summary>Checks if an import file with a particular ID number exists.</summary>
		 * <param name = "saveID">The import ID to check for</param>
		 * <returns>True if an import file with a matching ID number exists</returns>
		 */
		public static bool DoesImportExist (int saveID)
		{
			if (KickStarter.saveSystem)
			{
				foreach (SaveFile file in KickStarter.saveSystem.foundImportFiles)
				{
					if (file.saveID == saveID)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if a save file with a particular ID number exists</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element</param>
		 * <param name = "saveID">The save ID to check for</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to check for</param>
		 * <returns>True if a save file with a matching ID number exists</returns>
		 */
		public static bool DoesSaveExist (int elementSlot, int saveID, bool useSaveID)
		{
			if (!useSaveID)
			{
				if (elementSlot >= 0 && KickStarter.saveSystem.foundSaveFiles.Count > elementSlot)
				{
					saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].saveID;
				}
				else
				{
					saveID = -1;
				}
			}

			if (KickStarter.saveSystem)
			{
				foreach (SaveFile file in KickStarter.saveSystem.foundSaveFiles)
				{
					if (file.saveID == saveID)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if a save file with a particular ID number exists</summary>
		 * <param name = "saveID">The save ID to check for</param>
		 * <returns>True if a save file with a matching ID number exists</returns>
		 */
		public static bool DoesSaveExist (int saveID)
		{
			return DoesSaveExist (0, saveID, true);
		}


		/**
		 * Loads the AutoSave save file.  If multiple save profiles are used, the current profiles AutoSave will be loaded.
		 */
		public static void LoadAutoSave ()
		{
			if (KickStarter.saveSystem)
			{
				if (DoesSaveExist (0))
				{
					SaveSystem.LoadGame (0);
				}
				else
				{
					ACDebug.LogWarning ("Could not load autosave - file does not exist.");
				}
			}
		}


		/**
		 * <summary>Imports a save file from another Adventure Creator game.</summary>
		 * <param name = "elementSlot">The slot index of the MenuProfilesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to import</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to import</param>
		 */
		public static void ImportGame (int elementSlot, int saveID, bool useSaveID)
		{
			if (KickStarter.saveSystem)
			{
				if (!useSaveID)
				{
					if (KickStarter.saveSystem.foundImportFiles.Count > elementSlot)
					{
						saveID = KickStarter.saveSystem.foundImportFiles[elementSlot].saveID;
					}
				}

				if (saveID >= 0)
				{
					KickStarter.saveSystem.ImportSaveGame (saveID);
				}
			}
		}


		/**
		 * <summary>Sets the local instance of SelectiveLoad, which determines which save data is restored the next time (and only the next time) LoadGame is called.</summary>
		 * <param name = "selectiveLoad">An instance of SelectiveLoad the defines what elements to load</param>
		 */
		public void SetSelectiveLoadOptions (SelectiveLoad selectiveLoad)
		{
			activeSelectiveLoad = selectiveLoad;
		}


		/**
		 * <summary>Loads the last-recorded save game file.</summary>
		 * <returns>True if a save-game file was found to load, False otherwise</returns>
		 */
		public static bool ContinueGame ()
		{
			if (Options.optionsData != null && Options.optionsData.lastSaveID >= 0)
			{
				return LoadGame (Options.optionsData.lastSaveID);
			}
			return false;
		}


		/**
		 * <summary>Loads a save game file.</summary>
		 * <param name = "saveID">The save ID of the file to load</param>
		 * <returns>True if a file was found</returns>
		 */
		public static bool LoadGame (int saveID)
		{
			return LoadGame (0, saveID, true);
		}


		/**
		 * <summary>Loads a save game file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to load</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to load</param>
		 * <returns>True if a file was found</returns>
		 */
		public static bool LoadGame (int elementSlot, int saveID, bool useSaveID)
		{
			if (KickStarter.saveSystem)
			{
				if (!useSaveID)
				{
					if (elementSlot >= 0 && KickStarter.saveSystem.foundSaveFiles.Count > elementSlot)
					{
						saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].saveID;
					}
					else
					{
						ACDebug.LogWarning ("Can't select save slot " + elementSlot + " because only " + KickStarter.saveSystem.foundSaveFiles.Count + " have been found!");
					}
				}

				foreach (SaveFile foundSaveFile in KickStarter.saveSystem.foundSaveFiles)
				{
					if (foundSaveFile.saveID == saveID)
					{
						SaveFile saveFileToLoad = foundSaveFile;
						KickStarter.saveSystem.LoadSaveGame (saveFileToLoad);
						return true;
					}
				}

				if (useSaveID && saveID < 0)
				{
					SaveFile hiddenSaveFile = SaveFileHandler.GetSaveFile (saveID, Options.GetActiveProfileID ());
					KickStarter.saveSystem.LoadSaveGame (hiddenSaveFile);
					return true;
				}

				ACDebug.LogWarning ("Could not load game: file with ID " + saveID + " does not exist.");
			}
			return false;
		}


		/**
		 * Clears all save data stored in the SaveData class.
		 */
		public void ClearAllData ()
		{
			saveData = new SaveData ();
		}


		/**
		 * <summary>Requests that a save game from another AC game be imported..</summary>
		 * <param name = "saveID">The ID number of the save file to import</param>
		 */
		private void ImportSaveGame (int saveID)
		{
			foreach (SaveFile foundImportFile in foundImportFiles)
			{
				if (foundImportFile.saveID == saveID)
				{
					requestedImport = new SaveFile (foundImportFile);
					SaveFileHandler.Load (foundImportFile, true, ReceiveDataToImport);
					return;
				}
			}
		}


		/**
		 * <summary>Processes the save data requested by ImportSaveGame</summary>
		 * <param name = "saveFile">A data container for information about the save file to import.  Its saveID and profileID need to match up with that requested in the iSaveFileHandler's Import function in order for the data to be processed</param>
		 * <param name = "saveFileContents">The file contents of the save file. This is empty if the import failed.</param>
		 */
		public void ReceiveDataToImport (SaveFile saveFile, string fileData)
		{
			if (requestedImport != null && saveFile != null && requestedImport.saveID == saveFile.saveID && requestedImport.profileID == saveFile.profileID)
			{
				// Received data matches requested
				requestedImport = null;

				if (!string.IsNullOrEmpty (fileData))
				{
					KickStarter.eventManager.Call_OnImport (FileAccessState.Before);

					saveData = ExtractMainData (fileData);

					// Stop any current-running ActionLists, dialogs and interactions
					KillActionLists ();
					SaveSystem.AssignVariables (saveData.mainData.runtimeVariablesData);

					KickStarter.eventManager.Call_OnImport (FileAccessState.After);
				}
				else
				{
					KickStarter.eventManager.Call_OnImport (FileAccessState.Fail);
				}
			}
		}


		private void LoadSaveGame (SaveFile saveFile)
		{
			if (saveFile == null) return;

			requestedLoad = new SaveFile (saveFile);
			SaveFileHandler.Load (saveFile, true, ReceiveDataToLoad);
		}


		/**
		 * <summary>Extracts global data from a save game file's raw (serialized) contents</summary>
		 * <param name = "saveFileContents">The raw contents of the save file</param>
		 * <returns>The global data stored within the save file</returns>
		 */
		public static SaveData ExtractMainData (string saveFileContents)
		{
			if (!string.IsNullOrEmpty (saveFileContents))
			{
				int divider = GetDivider (saveFileContents);
				string mainData = saveFileContents.Substring (0, divider);
				mainData = mainData.Replace (mainDataDivider_Replacement, mainDataDivider);

				SaveData newSaveData = (SaveData)Serializer.DeserializeObject<SaveData> (mainData);
				return newSaveData;
			}
			return null;
		}


		/**
		 * <summary>Extracts all scene data from a save game file's raw (serialized) contents</summary>
		 * <param name = "saveFileContents">The raw contents of the save file</param>
		 * <returns>All scene data stored within the save file</returns>
		 */
		public static List<SingleLevelData> ExtractSceneData (string saveFileContents)
		{
			int divider = GetDivider (saveFileContents) + mainDataDivider.Length;
			string roomData = saveFileContents.Substring (divider);
			roomData = roomData.Replace (mainDataDivider_Replacement, mainDataDivider);

			return FileFormatHandler.DeserializeAllRoomData (roomData);
		}


		/**
		 * <summary>Extracts the Global Variables data of from a save file</summary>
		 * <param name = "saveFile">The save file to extract Global Global Variables from</param>
		 * <param name = "callback">A callback with the resulting List of GVar variables</param>
		 */
		public static void ExtractSaveFileVariables (SaveFile saveFile, System.Action<List<GVar>> callback)
		{
			if (saveFile != null)
			{
				variableExtractionCallback = callback;
				SaveFileHandler.Load (saveFile, false, OnLoadSaveFileVariables);
			}
		}


		private static System.Action<List<GVar>> variableExtractionCallback;
		private static void OnLoadSaveFileVariables (SaveFile saveFile, string fileData)
		{
			if (variableExtractionCallback == null) return;

			SaveData saveData = ExtractMainData (fileData);
			if (saveData != null)
			{
				string runtimeVariablesData = saveData.mainData.runtimeVariablesData;
				List<GVar> extractedVariables = UnloadVariablesData (runtimeVariablesData, false, KickStarter.runtimeVariables.globalVars);
				variableExtractionCallback.Invoke (extractedVariables);
				variableExtractionCallback = null;
				return;
			}
			variableExtractionCallback.Invoke (null);
			variableExtractionCallback = null;
			ACDebug.LogWarning ("Cannot extract variable data from save file ID = " + saveFile.saveID);
		}


		protected static int GetDivider (string saveFileContents)
		{
			return saveFileContents.IndexOf (mainDataDivider);
		}

		

		/**
		 * <summary>Processes the save data requested by LoadSaveGame</summary>
		 * <param name = "saveFile">A data container for information about the save file to load.  Its saveID and profileID need to match up with that requested in the iSaveFileHandler's Load function in order for the data to be processed</param>
		 * <param name = "saveFileContents">The file contents of the save file. This is empty if the load failed.</param>
		 */
		public void ReceiveDataToLoad (SaveFile saveFile, string fileData)
		{
			if (requestedLoad == null || saveFile == null)
			{
				return;
			}

			if (requestedLoad.saveID != saveFile.saveID || requestedLoad.profileID != saveFile.profileID)
			{
				return;
			}

			// Received data matches requested
			requestedLoad = null;

			if (string.IsNullOrEmpty (fileData))
			{
				KickStarter.eventManager.Call_OnLoad (FileAccessState.Fail, saveFile.saveID);
				return;
			}

			KickStarter.eventManager.Call_OnLoad (FileAccessState.Before, saveFile.saveID, saveFile);

			saveData = ExtractMainData (fileData);

			if (activeSelectiveLoad.loadSceneObjects)
			{
				KickStarter.levelStorage.allLevelData = ExtractSceneData (fileData);
			}

			// Stop any current-running ActionLists, dialogs and interactions
			KillActionLists ();
					
			bool forceReload = KickStarter.settingsManager.reloadSceneWhenLoading;

			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					string newSceneName = GetPlayerSceneName (CurrentPlayerID);
					if (forceReload || (SceneChanger.CurrentSceneName != newSceneName && activeSelectiveLoad.loadScene))
					{
						if (KickStarter.settingsManager.reloadSceneWhenLoading)
						{
							// Force a fade-out to hide the player switch
							KickStarter.mainCamera.FadeOut (0f);
						}

						_loadingGame = LoadingGame.InNewScene;
						KickStarter.sceneChanger.ChangeScene (newSceneName, false, forceReload);
						return;
					}
					break;

				case ChooseSceneBy.Number:
				default:
					int newSceneIndex = GetPlayerSceneIndex (CurrentPlayerID);
					if (forceReload || (SceneChanger.CurrentSceneIndex != newSceneIndex && activeSelectiveLoad.loadScene))
					{
						if (KickStarter.settingsManager.reloadSceneWhenLoading)
						{
							// Force a fade-out to hide the player switch
							KickStarter.mainCamera.FadeOut (0f);
						}

						_loadingGame = LoadingGame.InNewScene;
						KickStarter.sceneChanger.ChangeScene (newSceneIndex, false, forceReload);
						return;
					}
					break;
			}

			// If player has changed, destroy the old one and load in the new one
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				KickStarter.PreparePlayer ();
			}

			// No need to change scene
			_loadingGame = LoadingGame.InSameScene;

			// Already in the scene
			Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
			foreach (Sound sound in sounds)
			{
				if (sound.GetComponent <AudioSource>())
				{
					if (sound.soundType != SoundType.Music && !sound.GetComponent <AudioSource>().loop)
					{
						sound.Stop ();
					}
				}
			}

			InitAfterLoad ();
		}


		/**
		 * <summary>Gets all recorded data related to a given Player.  This should typically be an inactive Player - otherwise the active Player should just be read directly</summary>
		 * <param name = "playerID">The ID of the Player to get data for</param>
		 * <returns>All recorded data related to the Player</returns>
		 */
		public PlayerData GetPlayerData (int playerID)
		{
			if (saveData != null && saveData.playerData.Count > 0)
			{
				foreach (PlayerData _data in saveData.playerData)
				{
					if (_data.playerID == playerID)
					{
						return _data;
					}
				}
			}

			switch (KickStarter.settingsManager.playerSwitching)
			{
				case PlayerSwitching.Allow:
					if (playerID >= 0)
					{
						// None found, so make

						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						if (playerPrefab != null)
						{
							Player player = playerPrefab.GetSceneInstance ();
							if (player == null) player = playerPrefab.playerOb;

							PlayerData playerData = new PlayerData ();
							if (player)
							{
								playerData = player.SaveData (playerData);
							}

							playerData.playerID = playerID;
							saveData.playerData.Add (playerData);
							playerPrefab.SetInitialPosition (playerData);

							return playerData;
						}
					}
					break;

				case PlayerSwitching.DoNotAllow:
				default:
					{
						Player player = KickStarter.player;
						if (player) player = KickStarter.settingsManager.player;

						PlayerData playerData = new PlayerData ();
						if (player)
						{
							playerData = player.SaveData (playerData);
						}

						playerData.playerID = playerID;

						saveData.playerData.Add (playerData);
						return playerData;
					}
			}

			return null;
		}


		public void InitAfterLoad ()
		{
			if (KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			KickStarter.eventManager.Call_OnInitialiseScene ();

			PlayerData playerData = GetPlayerData (CurrentPlayerID);

			LoadingGame thisFrameLoadingGame = loadingGame;
			_loadingGame = LoadingGame.No;

			switch (thisFrameLoadingGame)
			{
				case LoadingGame.InNewScene:
				case LoadingGame.InSameScene:
					ReturnMainData ();
					KickStarter.levelStorage.ReturnCurrentLevelData ();
					KickStarter.playerInput.OnLoad ();
					KickStarter.sceneSettings.OnLoad ();
					KickStarter.eventManager.Call_OnLoad (FileAccessState.After, -1);
					break;

				case LoadingGame.JustSwitchingPlayer:
					if (playerData != null)
					{
						ReturnCameraData (playerData);
						KickStarter.sceneChanger.LoadPlayerData (playerData);
					}
					KickStarter.levelStorage.ReturnCurrentLevelData ();
					KickStarter.playerInput.OnLoad ();
					KickStarter.sceneSettings.OnLoad ();
					PlayerMenus.ResetInventoryBoxes ();
					break;

				case LoadingGame.No:
					if (playerData != null)
					{
						playerData.UpdateCurrentAndShiftPrevious (SceneChanger.CurrentSceneIndex);
						playerData.UpdateCurrentAndShiftPrevious (SceneChanger.CurrentSceneName);
					}
					KickStarter.levelStorage.ReturnCurrentLevelData ();
					KickStarter.sceneSettings.OnStart ();
					break;
			}

			AssetLoader.UnloadAssets ();

			KickStarter.eventManager.Call_OnAfterChangeScene (thisFrameLoadingGame);
		}


		/**
		 * <summary>Switches to a new Player in a different scene</summary>
		 * <param name = "playerID">The ID of the Player to switch to</param>
		 * <param name = "sceneIndex">The new scene to switch to</param>
		 * <param name = "doOverlay">If True, then a screenshot of the existing scene will be overlaid on top of the camera to mask the transition</param>
		 */
		public void SwitchToPlayerInDifferentScene (int playerID, int sceneIndex, bool doOverlay)
		{
			CurrentPlayerID = playerID;
			_loadingGame = LoadingGame.JustSwitchingPlayer;
			KickStarter.sceneChanger.ChangeScene (sceneIndex, true, false, doOverlay);
		}


		/**
		 * <summary>Switches to a new Player in a different scene</summary>
		 * <param name = "playerID">The ID of the Player to switch to</param>
		 * <param name = "sceneName">The new scene to switch to</param>
		 * <param name = "doOverlay">If True, then a screenshot of the existing scene will be overlaid on top of the camera to mask the transition</param>
		 */
		public void SwitchToPlayerInDifferentScene (int playerID, string sceneName, bool doOverlay)
		{
			CurrentPlayerID = playerID;
			_loadingGame = LoadingGame.JustSwitchingPlayer;
			KickStarter.sceneChanger.ChangeScene (sceneName, true, false, doOverlay);
		}


		/**
		 * <summary>Create a new save game file.</summary>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set</param>
		 */
		public static void SaveNewGame (bool overwriteLabel = true, string newLabel = "")
		{
			if (KickStarter.saveSystem)
			{
				KickStarter.saveSystem.SaveNewSaveGame (overwriteLabel, newLabel);
			}
		}
		

		private void SaveNewSaveGame (bool overwriteLabel = true, string newLabel = "")
		{
			if (foundSaveFiles != null && foundSaveFiles.Count > 0)
			{
				int expectedID = -1;

				List<SaveFile> foundSaveFilesOrdered = new List<SaveFile>();
				foreach (SaveFile foundSaveFile in foundSaveFiles)
				{
					foundSaveFilesOrdered.Add (new SaveFile (foundSaveFile));
				}
				foundSaveFilesOrdered.Sort (delegate (SaveFile a, SaveFile b) {return a.saveID.CompareTo (b.saveID);});

				for (int i=0; i<foundSaveFilesOrdered.Count; i++)
				{
					if (expectedID != -1 && expectedID != foundSaveFilesOrdered[i].saveID)
					{
						SaveSaveGame (expectedID, overwriteLabel, newLabel);
						return;
					}

					expectedID = foundSaveFilesOrdered[i].saveID + 1;
				}

				// Saves present, but no gap
				int newSaveID = (foundSaveFilesOrdered [foundSaveFilesOrdered.Count-1].saveID+1);
				SaveSaveGame (newSaveID, overwriteLabel, newLabel);
			}
			else
			{
				SaveSaveGame (1, overwriteLabel, newLabel);
			}
		}


		/**
		 * <summary>Overwrites the AutoSave file.</summary>
		 */
		public static void SaveAutoSave ()
		{
			if (KickStarter.saveSystem)
			{
				KickStarter.saveSystem.SaveSaveGame (0);
			}
		}


		/**
		 * <summary>Saves the game.</summary>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set. If blank, a default label will be generated.</param>
		 */
		public static void SaveGame (int saveID, bool overwriteLabel = true, string newLabel = "")
		{
			SaveSystem.SaveGame (0, saveID, true, overwriteLabel, newLabel);
		}
		

		/**
		 * <summary>Saves the game.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to save</param>
		 * <param name = "overwriteLabel">True if the label should be updated</param>
		 * <param name = "newLabel">The new label, if it can be set. If blank, a default label will be generated.</param>
		 */
		public static void SaveGame (int elementSlot, int saveID, bool useSaveID, bool overwriteLabel = true, string newLabel = "")
		{
			if (KickStarter.saveSystem)
			{
				if (!useSaveID)
				{
					if (elementSlot >= 0 && elementSlot < KickStarter.saveSystem.foundSaveFiles.Count)
					{
						saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].saveID;
					}
					else
					{
						saveID = -1;
					}
				}

				if (saveID == -1)
				{
					SaveSystem.SaveNewGame (overwriteLabel, newLabel);
				}
				else
				{
					KickStarter.saveSystem.SaveSaveGame (saveID, overwriteLabel, newLabel);
				}
			}
		}


		private void SaveSaveGame (int saveID, bool overwriteLabel = true, string newLabel = "")
		{
			if (GetNumSaves () >= KickStarter.settingsManager.maxSaves && !DoesSaveExist (saveID))
			{
				ACDebug.LogWarning ("Cannot save - maximum number of save files has already been reached.");
				KickStarter.eventManager.Call_OnSave (FileAccessState.Fail, saveID);
				return;
			}

			KickStarter.eventManager.Call_OnSave (FileAccessState.Before, saveID);
			KickStarter.levelStorage.StoreAllOpenLevelData ();

			if (KickStarter.settingsManager.saveWithThreading)
			{
				// Make sure Persistent components are set, as cannot use GetComponent in a thread
				if (KickStarter.runtimeVariables == null ||
					KickStarter.stateHandler == null ||
					KickStarter.runtimeInventory == null ||
					KickStarter.runtimeLanguages == null ||
					KickStarter.runtimeVariables == null ||
					KickStarter.playerMenus == null ||
					KickStarter.sceneChanger == null)
				{
					Debug.LogWarning ("Cannot save using threading - not all Persistent components found.");
					return;
				}
			}

			StartCoroutine (PrepareSaveCoroutine (saveID, overwriteLabel, newLabel));
		}

		
		private IEnumerator PrepareSaveCoroutine (int saveID, bool overwriteLabel = true, string newLabel = "")
		{
			while (loadingGame != LoadingGame.No)
			{
				ACDebug.LogWarning ("Delaying request to save due to the game currently loading.");
				yield return new WaitForEndOfFrame ();
			}

			// Update label
			if (overwriteLabel)
			{
				if (string.IsNullOrEmpty (newLabel))
				{
					newLabel = SaveFileHandler.GetDefaultSaveLabel (saveID);
				}
			}
			else
			{
				newLabel = string.Empty;
			}

			int profileID = Options.GetActiveProfileID ();
			SaveFile saveFile = new SaveFile (saveID, profileID, newLabel, string.Empty, null, string.Empty);

			if (KickStarter.settingsManager.saveScreenshots == SaveScreenshots.Always || (KickStarter.settingsManager.saveScreenshots == SaveScreenshots.ExceptWhenAutosaving && !saveFile.IsAutoSave))
			{
				isTakingSaveScreenshot = true;
				KickStarter.playerMenus.PreScreenshotBackup ();

				yield return new WaitForEndOfFrame ();

				Texture2D screenshotTex = GetScreenshotTexture ();
				if (screenshotTex)
				{
					saveFile.screenShot = screenshotTex;
					SaveFileHandler.SaveScreenshot (saveFile);
					Destroy (screenshotTex);
				}

				KickStarter.playerMenus.PostScreenshotBackup ();
				isTakingSaveScreenshot = false;
			}

			// Run save operations that can't be threaded
			SaveCurrentPlayerData ();
			SaveNonPlayerData (false);

			saveData.mainData = KickStarter.playerInput.SaveMainData (saveData.mainData);
			saveData.mainData.activeAssetLists = KickStarter.actionListAssetManager.GetSaveData ();

			saveData.mainData = KickStarter.levelStorage.SavePersistentData (saveData.mainData);

			SaveOperation saveOperation = gameObject.AddComponent <SaveOperation>();
			saveOperation.BeginOperation (ref saveData, saveFile);
		}


		public void OnCompleteSaveOperation (SaveFile saveFile, bool wasSuccesful, SaveOperation saveOperation)
		{
			Destroy (saveOperation);

			if (!wasSuccesful)
			{
				return;
			}

			GatherSaveFiles ();

			// Update label
			if (!string.IsNullOrEmpty (saveFile.label))
			{
				for (int i = 0; i < Mathf.Min (MAX_SAVES, foundSaveFiles.Count); i++)
				{
					if (foundSaveFiles[i].saveID == saveFile.saveID)
					{
						SaveFile newSaveFile = new SaveFile (foundSaveFiles[i]);
						newSaveFile.SetLabel (saveFile.label);
						foundSaveFiles[i] = newSaveFile;
						break;
					}
				}
			}

			// Update PlayerPrefs
			List<int> previousSaveIDs = Options.optionsData.GetPreviousSaveIDs ();
			if (Options.optionsData.lastSaveID >= 0)
			{
				previousSaveIDs.Add (Options.optionsData.lastSaveID);
			}
			Options.optionsData.lastSaveID = saveFile.saveID;
			if (previousSaveIDs.Contains (saveFile.saveID))
			{
				previousSaveIDs.Remove (saveFile.saveID);
			}
			Options.optionsData.SetPreviousSaveIDs (previousSaveIDs);

			Options.UpdateSaveLabels (foundSaveFiles.ToArray ());

			UpdateSaveFileLabels ();

			KickStarter.eventManager.Call_OnSave (FileAccessState.After, saveFile.saveID, saveFile);
		}


		protected virtual Texture2D GetScreenshotTexture ()
		{
			if (KickStarter.mainCamera)
			{
				Texture2D screenshotTexture = new Texture2D (ScreenshotWidth, ScreenshotHeight);
				Rect screenRect = KickStarter.mainCamera.GetPlayableScreenArea (false);

				screenshotTexture.ReadPixels (screenRect, 0, 0);

				if (KickStarter.settingsManager.linearColorTextures)
				{
					for (int y = 0; y < screenshotTexture.height; y++)
					{
						for (int x = 0; x < screenshotTexture.width; x++)
						{
							Color color = screenshotTexture.GetPixel (x, y);
							screenshotTexture.SetPixel (x, y, color.linear);
						}
					}
				}

				screenshotTexture.Apply ();

				return screenshotTexture;
			}
			ACDebug.LogWarning ("Cannot take screenshot - no main Camera found!");
			return null;
		}


		/** The width of save-game screenshot textures */
		public virtual int ScreenshotWidth
		{
			get
			{
				if (KickStarter.mainCamera)
				{
					int width = (int) (KickStarter.mainCamera.GetPlayableScreenArea (false).width * KickStarter.settingsManager.screenshotResolutionFactor);
					return Mathf.Min (width, Screen.width);
				}
				else
				{
					int width = (int) (Screen.width * KickStarter.settingsManager.screenshotResolutionFactor);
					return Mathf.Min (width, Screen.width);
				}
			}
		}


		/** The height of save-game screenshot textures */
		public virtual int ScreenshotHeight
		{
			get
			{
				if (KickStarter.mainCamera)
				{
					int height = (int) (KickStarter.mainCamera.GetPlayableScreenArea (false).height * KickStarter.settingsManager.screenshotResolutionFactor);
					return Mathf.Min (height, Screen.height);
				}
				else
				{
					int height = (int) (Screen.height * KickStarter.settingsManager.screenshotResolutionFactor);
					return Mathf.Min (height, Screen.height);
				}
			}
		}


		/**
		 * <summary>Stores the PlayerData of the active Player.</summary>
		 */
		public void SaveCurrentPlayerData ()
		{
			if (loadingGame == LoadingGame.JustSwitchingPlayer)
			{
				// When switching player, new player is loaded into old scene first before switching - so in this case we don't want to save the player data
				return;
			}

			PlayerData playerData = GetPlayerData (CurrentPlayerID);
			if (playerData == null)
			{
				playerData = new PlayerData ();
			}
			SavePlayerData (playerData, KickStarter.player);
			return;
		}


		private void SavePlayerData (PlayerData playerData, Player player)
		{
			playerData.currentScene = SceneChanger.CurrentSceneIndex;
			playerData.currentSceneName = SceneChanger.CurrentSceneName;
			
			playerData = KickStarter.sceneChanger.SavePlayerData (playerData);
			
			KickStarter.runtimeInventory.RemoveRecipes ();
			playerData.inventoryData = KickStarter.runtimeInventory.PlayerInvCollection.GetSaveData ();
			playerData = KickStarter.runtimeDocuments.SavePlayerDocuments (playerData);
			playerData = KickStarter.runtimeObjectives.SavePlayerObjectives (playerData);

			// Camera
			MainCamera mainCamera = KickStarter.mainCamera;
			if (mainCamera)
			{
				playerData = mainCamera.SaveData (playerData);
			}
           
			if (player == null)
			{
				playerData.playerPortraitGraphic = string.Empty;
				playerData.playerID = KickStarter.settingsManager.GetEmptyPlayerID ();
				return;
			}
			
			playerData = player.SaveData (playerData);
		}


		public void SaveNonPlayerData (bool stopFollowCommands)
		{ 
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
				{
					if (KickStarter.player == null || playerPrefab.ID != KickStarter.player.ID)
					{
						Player sceneInstance = playerPrefab.GetSceneInstance ();
						if (sceneInstance)
						{
							if (stopFollowCommands)
							{
								if (loadingGame == LoadingGame.No && sceneInstance.IsFollowingActivePlayerAcrossScenes ())
								{}
								else
								{
									sceneInstance.StopFollowing ();
								}
							}

							PlayerData existingData = GetPlayerData (playerPrefab.ID);
							sceneInstance.SaveData (existingData);
						}
					}
				}
			}
		}


		/**
		 * <summary>Gets the number of found import files.</summary>
		 * <returns>The number of found import files</returns>
		 */
		public static int GetNumImportSlots ()
		{
			return KickStarter.saveSystem.foundImportFiles.Count;
		}


		/**
		 * <summary>Gets the number of found save files.</summary>
		 * <returns>The number of found save files</returns>
		 */
		public static int GetNumSlots ()
		{
			return KickStarter.saveSystem.foundSaveFiles.Count;
		}


		/**
		 * <summary>Checks that another game's save file data is OK to import, by checking the state of a given boolean variable</summary>
		 * <param name = "fileData">The de-serialized data string from the file</param>
		 * <param name = "boolID">The ID number of the Boolean Global Variable that must be True in the fileData for the import check to pass</param>
		 * <returns>True if the other game's save file data is OK to import</returns>
		 */
		public bool DoImportCheck (string fileData, int boolID)
		{
			if (!string.IsNullOrEmpty (fileData.ToString ()))
			{
				SaveData tempSaveData = ExtractMainData (fileData);
				if (tempSaveData == null)
				{
					tempSaveData = new SaveData ();
				}

				string varData = tempSaveData.mainData.runtimeVariablesData;
				if (!string.IsNullOrEmpty (varData))
				{
					string[] varsArray = varData.Split (SaveSystem.pipe[0]);
					
					foreach (string chunk in varsArray)
					{
						string[] chunkData = chunk.Split (SaveSystem.colon[0]);
						
						int _id = 0;
						int.TryParse (chunkData[0], out _id);

						if (_id == boolID)
						{
							int _value = 0;
							int.TryParse (chunkData[1], out _value);

							if (_value == 1)
							{
								return true;
							}
							return false;
						}
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Creates a suffix for save filenames based on a given save slot and profile</summary>
		 * <param name = "saveID">The ID of the save slot</param>
		 * <param name = "profileID">The ID of the profile</param>
		 * <return>A save file suffix based on the slot and profile</returns>
		 */
		public static string GenerateSaveSuffix (int saveID, int profileID = -1)
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.useProfiles)
			{
				if (profileID == -1)
				{
					// None set, so just use the active profile
					profileID = Options.GetActiveProfileID ();
				}
				return ("_" + saveID.ToString () + "_" + profileID.ToString ());
			}
			return ("_" + saveID.ToString ());
		}


		private void KillActionLists ()
		{
			KickStarter.actionListManager.KillAllLists ();

			Moveable[] moveables = FindObjectsOfType (typeof (Moveable)) as Moveable[];
			foreach (Moveable moveable in moveables)
			{
				moveable.StopMoving ();
			}
		}


		/**
		 * <summary>Gets the label of an import file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuProfilesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to import</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to import</param>
		 * <returns>The label of the import file.</returns>
		 */
		public static string GetImportSlotLabel (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundImportFiles != null)
			{
				return KickStarter.saveSystem.GetSlotLabel (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundImportFiles.ToArray ());
			}
			return ("Save test (01/01/2001 12:00:00)"); 
		}


		/**
		 * <summary>Gets the label of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to save</param>
		 * <returns>The label of the save file.  If the save file is not found, an empty string is returned.</returns>
		 */
		public static string GetSaveSlotLabel (int elementSlot, int saveID, bool useSaveID)
		{
			if (!Application.isPlaying)
			{
				if (useSaveID)
				{
					elementSlot = saveID;
				}
				return SaveFileHandler.GetDefaultSaveLabel (elementSlot);
			}
			else if (KickStarter.saveSystem.foundSaveFiles != null)
			{
				return KickStarter.saveSystem.GetSlotLabel (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundSaveFiles.ToArray ());
			}

			return ("Save game file"); 
		}


		/**
		 * <summary>Gets the label of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to save</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to save</param>
		 * <param name = "saveFiles">An array of SaveFile instances that the save file to retrieve is assumed to be in</param>
		 * <returns>The label of the save file.  If the save file is not found, an empty string is returned.</returns>
		 */
		public string GetSlotLabel (int elementSlot, int saveID, bool useSaveID, SaveFile[] saveFiles)
		{
			if (Application.isPlaying)
			{
				if (useSaveID)
				{
					foreach (SaveFile saveFile in saveFiles)
					{
						if (saveFile.saveID == saveID)
						{
							return AdvGame.ConvertTokens (saveFile.label);
						}
					}
				}
				else if (elementSlot >= 0)
				{
					if (elementSlot < saveFiles.Length)
					{
						return AdvGame.ConvertTokens (saveFiles [elementSlot].label);
					}
				}
				return string.Empty;
			}
			return ("Save test (01/01/2001 12:00:00)");
		}


		/**
		 * <summary>Gets the screenshot of an import file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to get the screenshot of</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to look for</param>
		 * <returns>The import files's screenshots as a Texture2D.  If the save file is not found, null is returned.</returns>
		 */
		public static Texture2D GetImportSlotScreenshot (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundImportFiles != null)
			{
				return KickStarter.saveSystem.GetScreenshot (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundImportFiles.ToArray ());
			}
			return null;
		}
		

		/**
		 * <summary>Gets the screenshot of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to get the screenshot of</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to look for</param>
		 * <return>The save files's screenshots as a Texture2D.  If the save file is not found, null is returned.</returns>
		 */
		public static Texture2D GetSaveSlotScreenshot (int elementSlot, int saveID, bool useSaveID)
		{
			if (Application.isPlaying && KickStarter.saveSystem.foundSaveFiles != null)
			{
				return KickStarter.saveSystem.GetScreenshot (elementSlot, saveID, useSaveID, KickStarter.saveSystem.foundSaveFiles.ToArray ());
			}
			return null;
		}


		/**
		 * <summary>Gets the screenshot of a save file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuSavesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to get the screenshot of</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to look for</param>
		 * <param name = "saveFiles">An array of SaveFile instances that the save file to retrieve is assumed to be in</param>
		 * <returns>The save files's screenshots as a Texture2D.  If the save file is not found, null is returned.</returns>
		 */
		public Texture2D GetScreenshot (int elementSlot, int saveID, bool useSaveID, SaveFile[] saveFiles)
		{
			if (Application.isPlaying)
			{
				if (useSaveID)
				{
					foreach (SaveFile saveFile in saveFiles)
					{
						if (saveFile.saveID == saveID)
						{
							return saveFile.screenShot;
						}
					}
				}
				else if (elementSlot >= 0)
				{
					if (elementSlot < saveFiles.Length)
					{
						return saveFiles [elementSlot].screenShot;
					}
				}
			}
			return null;
		}


		public int CurrentPlayerID
		{
			get
			{
				return saveData.mainData.currentPlayerID;
			}
			set
			{
				saveData.mainData.currentPlayerID = value;
			}
		}


		private void ReturnMainData ()
		{
			if (KickStarter.playerInput && KickStarter.runtimeInventory && KickStarter.settingsManager && KickStarter.stateHandler)
			{
				PlayerData playerData = GetPlayerData (CurrentPlayerID);

				if (activeSelectiveLoad.loadPlayer)
				{
					SpawnAllPlayers ();
					ReturnPlayerData (playerData, KickStarter.player);
				}
				if (activeSelectiveLoad.loadSceneObjects)
				{
					ReturnCameraData (playerData);
				}

				KickStarter.stateHandler.LoadMainData (saveData.mainData);
				KickStarter.actionListAssetManager.LoadData (saveData.mainData.activeAssetLists);
				KickStarter.settingsManager.movementMethod = (MovementMethod) saveData.mainData.movementMethod;
				ActiveInput.LoadSaveData (saveData.mainData.activeInputsData);
				Timer.LoadSaveData (saveData.mainData.timersData);

				if (activeSelectiveLoad.loadScene)
				{
					KickStarter.sceneChanger.LoadPlayerData (playerData, activeSelectiveLoad.loadSubScenes);
				}

				// Inventory
				KickStarter.runtimeInventory.RemoveRecipes ();
				if (activeSelectiveLoad.loadInventory)
				{
					KickStarter.runtimeInventory.AssignPlayerInventory (InvCollection.LoadData (playerData.inventoryData));
					KickStarter.runtimeDocuments.AssignPlayerDocuments (playerData);
					KickStarter.runtimeObjectives.AssignPlayerObjectives (playerData);
					KickStarter.runtimeInventory.LoadMainData (saveData.mainData);
				}

				KickStarter.playerInput.LoadMainData (saveData.mainData);

				// Variables
				if (activeSelectiveLoad.loadVariables)
				{
					AssignVariables (saveData.mainData.runtimeVariablesData);
					KickStarter.runtimeVariables.AssignCustomTokensFromString (saveData.mainData.customTokenData);
				}

				// Menus
				KickStarter.playerMenus.LoadMainData (saveData.mainData);

				// Speech
				KickStarter.runtimeLanguages.LoadMainData (saveData.mainData);

				// Scene
				KickStarter.sceneChanger.LoadMainData (saveData.mainData);

				// Persistent Remember components
				KickStarter.levelStorage.LoadPersistentData (saveData.mainData);
			}
			else
			{
				if (KickStarter.playerInput == null)
				{
					ACDebug.LogWarning ("Load failed - no PlayerInput found.");
				}
				if (KickStarter.runtimeInventory == null)
				{
					ACDebug.LogWarning ("Load failed - no RuntimeInventory found.");
				}
				if (KickStarter.sceneChanger == null)
				{
					ACDebug.LogWarning ("Load failed - no SceneChanger found.");
				}
				if (KickStarter.settingsManager == null)
				{
					ACDebug.LogWarning ("Load failed - no Settings Manager found.");
				}
			}
		}


		/**
		 * <summary>Gets the current scene index that a Player is in.</summary>
		 * <param name = "ID">The ID number of the Player to check</param>
		 * <returns>The current scene index that the Player is in.</returns>
		 */
		public int GetPlayerSceneIndex (int ID)
		{
			PlayerData playerData = GetPlayerData (ID);
			if (playerData != null)
			{
				return playerData.currentScene;
			}
			return -1;
		}


		/**
		 * <summary>Gets the current scene name that a Player is in.</summary>
		 * <param name = "ID">The ID number of the Player to check</param>
		 * <returns>The current scene name that the Player is in.</returns>
		 */
		public string GetPlayerSceneName (int ID)
		{
			PlayerData playerData = GetPlayerData (ID);
			if (playerData != null)
			{
				return playerData.currentSceneName;
			}
			return string.Empty;
		}


		/**
		 * <summary>Updates the internal record of an inactive Player's position to the current scene, provided that player-switching is allowed. If that Player has an Associated NPC, then it will be spawned or teleported to the Player's new position</summary>
		 * <param name = "ID">The ID number of the Player to affect, as set in the Settings Manager's list of Player prefabs</param>
		 * <param name = "teleportPlayerStartMethod">How to select which PlayerStart to appear at (SceneDefault, BasedOnPrevious, EnteredHere)</param>
		 * <param name = "newPlayerStart">If teleportPlayerStartMethod = EnteredHere, a PlayerStart to use as the basis for the Player's new position and rotation</param>
		 */
		public void MoveInactivePlayerToCurrentScene (int ID, TeleportPlayerStartMethod teleportPlayerStartMethod, PlayerStart newPlayerStart = null)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return;
			}

			if (KickStarter.player && KickStarter.player.ID == ID)
			{
				ACDebug.LogWarning ("Cannot update position of player " + ID + " because that Player (" + KickStarter.player + ") is currently active!");
				return;
			}

			PlayerData playerData = GetPlayerData (ID);

			playerData.ClearPathData ();
			playerData.UpdatePosition (teleportPlayerStartMethod, newPlayerStart);
			playerData.UpdatePresenceInScene ();
		}


		/**
		 * <summary>Moves an inactive Player to a new scene</summary>
		 * <param name = "ID">The inactive Player's ID number</param>
		 * <param name = "newSceneIndex">The new scene to switch to</param>
		 * <param name = "teleportPlayerStartMethod">How to select which PlayerStart to appear at (SceneDefault, BasedOnPrevious, EnteredHere)</param>
		 * <param name = "newPlayerStartConstantID">If teleportPlayerStartMethod = EnteredHere, the Constant ID number of the associated PlayerStart to appear at in the new scene</param>
		 */
		public void MoveInactivePlayer (int ID, int newSceneIndex, TeleportPlayerStartMethod teleportPlayerStartMethod, int newPlayerStartConstantID = 0)
		{
			OnMoveInactivePlayer (ID);

			PlayerData playerData = GetPlayerData (ID);
			playerData.UpdatePosition (newSceneIndex, teleportPlayerStartMethod, newPlayerStartConstantID);
			playerData.UpdatePresenceInScene ();
		}


		/**
		 * <summary>Moves an inactive Player to a new scene</summary>
		 * <param name = "ID">The inactive Player's ID number</param>
		 * <param name = "newSceneNamex">The new scene to switch to</param>
		 * <param name = "teleportPlayerStartMethod">How to select which PlayerStart to appear at (SceneDefault, BasedOnPrevious, EnteredHere)</param>
		 * <param name = "newPlayerStartConstantID">If teleportPlayerStartMethod = EnteredHere, the Constant ID number of the associated PlayerStart to appear at in the new scene</param>
		 */
		public void MoveInactivePlayer (int ID, string newSceneNamex, TeleportPlayerStartMethod teleportPlayerStartMethod, int newPlayerStartConstantID = 0)
		{
			OnMoveInactivePlayer (ID);

			PlayerData playerData = GetPlayerData (ID);
			playerData.UpdatePosition (newSceneNamex, teleportPlayerStartMethod, newPlayerStartConstantID);
			playerData.UpdatePresenceInScene ();
		}


		private void OnMoveInactivePlayer (int ID)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return;
			}

			if (KickStarter.player && KickStarter.player.ID == ID)
			{
				ACDebug.LogWarning ("Cannot update position of player " + ID + " because that Player (" + KickStarter.player + ") is currently active!");
				return;
			}

			PlayerData playerData = GetPlayerData (ID);

			PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (ID);
			if (playerPrefab != null)
			{
				Player sceneInstance = playerPrefab.GetSceneInstance ();
				if (sceneInstance)
				{
					playerData = sceneInstance.SaveData (playerData);
				}
			}

			playerData.ClearPathData ();
		}


		/**
		 * <summary>Updates a Player object with its associated saved data, if it exists.</summary>
		 * <param name = "player">The Player to load animation data for</param>
		 */
		public void AssignPlayerData (Player player)
		{
			if (player && saveData.playerData.Count > 0)
			{
				foreach (PlayerData _data in saveData.playerData)
				{
					if (player.ID == _data.playerID)
					{
						player.LoadData (_data);
					}
				}
			}
		}


		private void ReturnPlayerData (PlayerData playerData, Player player)
		{
			if (player == null)
			{
				return;
			}

			player.LoadData (playerData);
		}


		private void ReturnCameraData (PlayerData playerData, bool snapCamera = true)
		{
			// Camera
			MainCamera mainCamera = KickStarter.mainCamera;
			if (mainCamera)
			{
				mainCamera.LoadData (playerData, snapCamera);
			}
		}



		/**
		 * <summary>Unloads stored global variable data back into the RuntimeVariables script.</summary>
		 * <param name = "runtimeVariablesData">The values of all global variables, combined into a stingle string</param>
		 * <param name = "fromOptions">If true, only global variables that are linked to OptionsData will be affected</param>
		 */
		public static void AssignVariables (string runtimeVariablesData, bool fromOptions = false)
		{
			if (runtimeVariablesData == null)
			{
				return;
			}
			KickStarter.runtimeVariables.ClearSpeechLog ();
			KickStarter.runtimeVariables.globalVars = UnloadVariablesData (runtimeVariablesData, true, KickStarter.runtimeVariables.globalVars, fromOptions);

			GlobalVariables.UploadAll ();
		}


		/**
		 * <summary>Condenses the values of a List of variables into a single string.</summary>
		 * <param name = "vars">A List of variables (see GVar) to condense</param>
		 * <param name = "isOptionsData">If True, only global variables that are linked to OptionsData will be included</param>
		 * <param name = "location">The variables' location (Local, Variable)</param>
		 * <returns>The variable's values, condensed into a single string</returns>
		 */
		public static string CreateVariablesData (List<GVar> vars, bool isOptionsData, VariableLocation location)
		{
			System.Text.StringBuilder variablesString = new System.Text.StringBuilder ();

			foreach (GVar _var in vars)
			{
				if ((isOptionsData && _var.link == VarLink.OptionsData) ||
					(!isOptionsData && _var.link != VarLink.OptionsData) ||
					location == VariableLocation.Local ||
					location == VariableLocation.Component)
				{
					variablesString.Append (_var.id.ToString ());
					variablesString.Append (SaveSystem.colon);

					switch (_var.type)
					{
						case VariableType.String:
							string textVal = _var.TextValue;
							textVal = AdvGame.PrepareStringForSaving (textVal);
							variablesString.Append (textVal);

							// The ID can be changed with SetStringValue, so needs recording
							variablesString.Append (SaveSystem.colon);
							variablesString.Append (_var.textValLineID);
							break;

						case VariableType.Float:
							variablesString.Append (_var.FloatValue.ToString ());
							break;

						case VariableType.Vector3:
							string vector3Val = _var.Vector3Value.x.ToString () + "," + _var.Vector3Value.y.ToString () + "," + _var.Vector3Value.z.ToString ();
							vector3Val = AdvGame.PrepareStringForSaving (vector3Val);
							variablesString.Append (vector3Val);
							break;

						case VariableType.GameObject:
							if (_var.GameObjectValue)
							{
								if (location == VariableLocation.Global)
								{
									variablesString.Append (_var.TextValue);
								}
								else if (_var.SavePrefabReference)
								{
									variablesString.Append (_var.GameObjectValue.name);
								}
								else
								{
									ConstantID constantID = _var.GameObjectValue.GetComponent <ConstantID>();
									if (constantID)
									{
										variablesString.Append (constantID.constantID.ToString ());
									}
									else
									{
										ACDebug.LogWarning ("Cannot save the value of " + location + " GameObject variable " + _var.label + ", because the assigned object, '" + _var.GameObjectValue.name + "', has no Constant ID value.", _var.GameObjectValue);
									}
								}
							}
							break;

						case VariableType.UnityObject:
							if (_var.UnityObjectValue)
							{
								variablesString.Append (_var.TextValue);
							}
							break;

						default:
							variablesString.Append (_var.IntegerValue.ToString ());
							break;
					}

					variablesString.Append (SaveSystem.pipe);
				}
			}
			
			if (variablesString.Length > 0)
			{
				variablesString.Remove (variablesString.Length-1, 1);
			}

			return variablesString.ToString ();
		}


		/**
		 * <summary>Updates a list of Variables with value data stored as a string.</summary>
		 * <param name = "data">The save-data string, serialized in the save file</param>
		 * <param name="updateExistingVars">If True, the variabels stored in the existingVars parameter List will be updated, as opposed to merely used for reference</param>
		 * <param name = "existingVars">A list of existing Variables whose values will be updated.
		 * <param name = "fromOptions">If True, the only Variables that are linked to Options Data will be updated</param>
		 * <returns>The updated list of Variables</returns>
		 */
		public static List<GVar> UnloadVariablesData (string data, bool updateExistingVars, List<GVar> existingVars, bool fromOptions = false)
		{
			if (existingVars == null) 
			{
				return null;
			}

			List<GVar> copiedVars = new List<GVar>();
			if (updateExistingVars)
			{
				copiedVars = existingVars;
			}
			else
			{
				foreach (GVar existingVar in existingVars)
				{
					GVar newVar = new GVar (existingVar);
					newVar.CreateRuntimeTranslations ();
					copiedVars.Add (newVar);
				}
			}

			if (string.IsNullOrEmpty (data))
			{
				return copiedVars;
			}

			string[] varsArray = data.Split (pipe[0]);

			Object[] prefabAssets = null;
			bool searchedResources = false;

			foreach (string chunk in varsArray)
			{
				string[] chunkData = chunk.Split (colon[0]);
				
				int _id = 0;
				int.TryParse (chunkData[0], out _id);

				foreach (GVar _var in copiedVars)
				{
					if (_var != null && _var.id == _id)
					{
						if (fromOptions)
						{
							if (_var.link != VarLink.OptionsData)
							{
								continue;
							}
						}
						else
						{
							if (_var.link == VarLink.OptionsData)
							{
								continue;
							}
						}

						switch (_var.type)
						{
							case VariableType.String:
								{
									string _text = chunkData[1];
									_text = AdvGame.PrepareStringForLoading (_text);

									int _textLineID = -1;
									if (chunkData.Length > 2)
									{
										int.TryParse (chunkData[2], out _textLineID);
									}

									_var.SetStringValue (_text, _textLineID);
									break;
								}

							case VariableType.Float:
								{
									float _value = 0f;
									float.TryParse (chunkData[1], out _value);
									_var.FloatValue = _value;
									break;
								}

							case VariableType.Vector3:
								{
									string _text = chunkData[1];
									_text = AdvGame.PrepareStringForLoading (_text);

									Vector3 _value = Vector3.zero;
									string[] valuesArray = _text.Split (","[0]);
									if (valuesArray != null && valuesArray.Length == 3)
									{
										float xValue = 0f;
										float.TryParse (valuesArray[0], out xValue);

										float yValue = 0f;
										float.TryParse (valuesArray[1], out yValue);

										float zValue = 0f;
										float.TryParse (valuesArray[2], out zValue);

										_value = new Vector3 (xValue, yValue, zValue);
									}

									_var.Vector3Value = _value;
									break;
								}

							case VariableType.GameObject:
								{
									if (existingVars.Count == 0) break;
									bool isGlobal = existingVars[0].IsGlobalVariable ();
									if ((isGlobal || existingVars[0].SavePrefabReference) && !string.IsNullOrEmpty (chunkData[1]))
									{
										#if AddressableIsPresent
										if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
										{
											KickStarter.saveSystem.UnloadVariableDataFromAddressables (_var, chunkData[1]);
											break;
										}
										#endif

										bool foundObject = false;

										if (!searchedResources)
										{
											prefabAssets = Resources.LoadAll ("SaveableData/Prefabs", typeof (GameObject));
											if (prefabAssets == null || prefabAssets.Length == 0)
											{
												prefabAssets = Resources.LoadAll (string.Empty, typeof (GameObject));
											}
											searchedResources = true;
										}

										foreach (Object prefabAsset in prefabAssets)
										{
											if (prefabAsset is GameObject)
											{
												GameObject prefabGameObject = (GameObject) prefabAsset;
												if (prefabGameObject.name == chunkData[1])
												{
													_var.GameObjectValue = prefabGameObject;
													foundObject = true;
												}
											}
										}

										if (!foundObject)
										{
											ACDebug.LogWarning ("Could not find Resources prefab with ID " + chunkData[1] + "- cannot restore GameObject variable " + _var.label + " value.  Is it placed in a Resources folder?");
										}
									}
									else
									{
										int _idValue = 0;
										if (int.TryParse (chunkData[1], out _idValue))
										{
											if (_idValue != 0)
											{
												ConstantID constantID = ConstantID.GetComponent (_idValue);
												if (constantID)
												{
													_var.GameObjectValue = constantID.gameObject;
												}
												else
												{
													ACDebug.LogWarning ("Could not find GameObject with ID " + chunkData[1] + " - cannot restore GameObject variable " + _var.label + " value");
												}
											}
										}
									}
								}
								break;

							case VariableType.UnityObject:
								{
									if (existingVars.Count == 0) break;
									#if AddressableIsPresent
									if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
									{
										KickStarter.saveSystem.UnloadVariableDataFromAddressables (_var, chunkData[1]);
										break;
									}
									#endif

									bool foundObject = false;

									if (!searchedResources)
									{
										prefabAssets = Resources.LoadAll (string.Empty, typeof (GameObject));
										searchedResources = true;
									}

									foreach (Object prefabAsset in prefabAssets)
									{
										if (prefabAsset.name == chunkData[1])
										{
											_var.UnityObjectValue = prefabAsset;
											foundObject = true;
										}
									}

									if (!foundObject)
									{
										ACDebug.LogWarning ("Could not find Resources object with ID " + chunkData[1] + "- cannot restore Unity Object variable " + _var.label + " value.  Is it placed in a Resources folder?");
									}
								}
								break;

							default:
								{
									int _value = 0;
									int.TryParse (chunkData[1], out _value);
									_var.IntegerValue = _value;
									break;
								}
						}
					}
				}
			}

			if (searchedResources)
			{
				Resources.UnloadUnusedAssets ();
			}

			return copiedVars;
		}


		#if AddressableIsPresent

		private void UnloadVariableDataFromAddressables (GVar variableToUpdate, string savedData)
		{
			switch (variableToUpdate.type)
			{
				case VariableType.GameObject:
					StartCoroutine (UnloadGameObjectVariableDataFromAddressablesCo (variableToUpdate, savedData));
					break;

				case VariableType.UnityObject:
					StartCoroutine (UnloadUnityObjectVariableDataFromAddressablesCo (variableToUpdate, savedData));
					break;

				default:
					break;
			}
		}

		private IEnumerator UnloadUnityObjectVariableDataFromAddressablesCo (GVar variableToUpdate, string savedData)
		{
			AsyncOperationHandle<Object> handle = Addressables.LoadAssetAsync<Object> (savedData);
			yield return handle;
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				variableToUpdate.UnityObjectValue = handle.Result;
			}
			Addressables.Release (handle);
		}


		private IEnumerator UnloadGameObjectVariableDataFromAddressablesCo (GVar variableToUpdate, string savedData)
		{
			AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject> (savedData);
			yield return handle;
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				variableToUpdate.GameObjectValue = handle.Result;
			}
			Addressables.Release (handle);
		}

		#endif


		/**
		 * <summary>Returns a collection of  inventory items currently carried by a particular Player.</summary>
		 * <param name = "_playerID">The ID number of the Player to check the inventory of</param>
		 * <returns>A collection representing the inventory items</returns>
		 */
		public InvCollection GetItemsFromPlayer (int _playerID)
		{
			if (CurrentPlayerID == _playerID)
			{
				return KickStarter.runtimeInventory.PlayerInvCollection;
			}

			PlayerData playerData = GetPlayerData (_playerID);
			if (playerData != null)
			{
				return InvCollection.LoadData (playerData.inventoryData);
			}
			return new InvCollection ();
		}


		/**
		 * <summary>Re-assigns the inventory items currently carried by a particular Player.</summary>
		 * <param name = "invCollection">The collection of items representing the inventory items</param>
		 * <param name = "_playerID">The ID number of the Player to assign the inventory of</param>
		 */
		public void AssignItemsToPlayer (InvCollection invCollection, int _playerID)
		{
			string invData = invCollection.GetSaveData ();

			PlayerData playerData = GetPlayerData (_playerID);
			playerData.inventoryData = invData;
		}


		public void SetInitialPlayerID ()
		{
			CurrentPlayerID = KickStarter.settingsManager.GetDefaultPlayerID ();
		}

		
		public void SpawnAllPlayers ()
		{
			if (KickStarter.settingsManager.playerSwitching != PlayerSwitching.Allow) return;

			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				PlayerData playerData = GetPlayerData (playerPrefab.ID);
				playerData.UpdatePresenceInScene ();
			}
		}


		public void SpawnFollowingPlayers ()
		{
			if (KickStarter.settingsManager.playerSwitching != PlayerSwitching.Allow) return;

			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				PlayerData playerData = GetPlayerData (playerPrefab.ID);
				playerData.SpawnIfFollowingActive ();
			}
		}


		private void SpawnAllInactivePlayers ()
		{
			if (KickStarter.settingsManager.playerSwitching != PlayerSwitching.Allow) return;

			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				if (playerPrefab.ID != CurrentPlayerID)
				{
					PlayerData playerData = GetPlayerData (playerPrefab.ID);
					playerData.UpdatePresenceInScene ();
				}
			}
		}


		public void AssignObjectivesToPlayer (string dataString, int _playerID)
		{
			PlayerData playerData = GetPlayerData (_playerID);
			playerData.playerObjectivesData = dataString;
		}


		/**
		 * <summary>Renames the label of a save game file.</summary>
		 * <param name = "newLabel">The new label to give the save game file</param>
		 * <param name = "saveIndex">The index of the foundSaveFiles List that represents the save file to affect</param>
		 */
		public void RenameSave (string newLabel, int saveIndex)
		{
			if (string.IsNullOrEmpty (newLabel))
			{
				return;
			}

			GatherSaveFiles ();

			if (foundSaveFiles.Count > saveIndex && saveIndex >= 0)
			{
				SaveFile newSaveFile = new SaveFile (foundSaveFiles [saveIndex]);
				newSaveFile.SetLabel (newLabel);
				foundSaveFiles [saveIndex] = newSaveFile;
				Options.UpdateSaveLabels (foundSaveFiles.ToArray ());
			}
		}


		/**
		 * <summary>Renames the label of a save game file.</summary>
		 * <param name = "newLabel">The new label to give the save game file</param>
		 * <param name = "saveID">The ID that represents the save file to affect</param>
		 */
		public void RenameSaveByID (string newLabel, int saveID)
		{
			if (string.IsNullOrEmpty (newLabel))
			{
				return;
			}

			GatherSaveFiles ();

			for (int i=0; i<foundSaveFiles.Count; i++)
			{
				if (foundSaveFiles[i].saveID == saveID)
				{
					RenameSave (newLabel, i);
					return;
				}
			}
		}


		/**
		 * <summary>Deletes a player profile by referencing its entry in a MenuProfilesList element.</summary>
		 * <param name = "profileIndex">The index in the MenuProfilesList element that represents the profile to delete. If it is set to its default, -2, the active profile will be deleted</param>
		 * <param name = "includeActive">If True, then the MenuProfilesList element that the profile was selected from also displays the active profile</param>
		 */
		public void DeleteProfile (int profileIndex = -2, bool includeActive = true)
		{
			if (!KickStarter.settingsManager.useProfiles)
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

			DeleteProfileID (profileID);
		}


		/**
		 * <summary>Deletes a player profile ID.</summary>
		 * <param name = "profileID">The profile ID to delete</param>
		 */
		public void DeleteProfileID (int profileID)
		{
			if (!KickStarter.settingsManager.useProfiles || profileID < 0)
			{
				return;
			}

			if (!Options.DoesProfileIDExist (profileID))
			{
				ACDebug.LogWarning ("Cannot delete profile ID " + profileID + " as it does not exist!");
				return;
			}

			// Delete save files
			SaveFileHandler.DeleteAll (profileID);

			bool isActive = (profileID == Options.GetActiveProfileID ()) ? true : false;
			Options.DeleteProfilePrefs (profileID);
			if (isActive)
			{
				GatherSaveFiles ();
			}
			KickStarter.playerMenus.RecalculateAll ();

			ACDebug.Log ("Profile ID " + profileID + " deleted.");
		}


		/**
		 * <summary>Deletes a save game file.</summary>
		 * <param name = "saveID">The save ID of the file to load</param>
		 */
		public static void DeleteSave (int saveID)
		{
			KickStarter.saveSystem.DeleteSave (0, saveID, true);
		}


		/**
		 * <summary>Deletes a save game file.</summary>
		 * <param name = "elementSlot">The slot index of the MenuProfilesList element that was clicked on</param>
		 * <param name = "saveID">The save ID to delete</param>
		 * <param name = "useSaveID">If True, the saveID overrides the elementSlot to determine which file to delete</param>
		 */
		public void DeleteSave (int elementSlot, int saveID, bool useSaveID)
		{
			if (!useSaveID)
			{
				// For this to work, must have loaded the list of saves into a SavesList
				saveID = KickStarter.saveSystem.foundSaveFiles[elementSlot].saveID;
			}

			foreach (SaveFile saveFile in foundSaveFiles)
			{
				if (saveFile.saveID == saveID)
				{
					SaveFileHandler.Delete (saveFile);
				}
			}

			// Also remove save label
			GatherSaveFiles ();
			foreach (SaveFile saveFile in foundSaveFiles)
			{
				if (saveFile.saveID == saveID)
				{
					foundSaveFiles.Remove (saveFile);
					Options.UpdateSaveLabels (foundSaveFiles.ToArray ());
					break;
				}
			}

			if (Options.optionsData != null)
			{
				List<int> previousSaveIDs = Options.optionsData.GetPreviousSaveIDs ();
				if (previousSaveIDs.Contains (saveID))
				{
					previousSaveIDs.Remove (saveID);
				}


				if (Options.optionsData.lastSaveID == saveID)
				{
					// Deleting the "last save", find a replacement
					if (previousSaveIDs.Count > 0)
					{
						Options.optionsData.lastSaveID = previousSaveIDs[previousSaveIDs.Count - 1];
						previousSaveIDs.RemoveAt (previousSaveIDs.Count - 1);
					}
					else
					{
						Options.optionsData.lastSaveID = -1;
					}
				}

				Options.optionsData.SetPreviousSaveIDs (previousSaveIDs);
				
				Options.SavePrefs ();
			}
			KickStarter.playerMenus.RecalculateAll ();
		}


		protected void OnAddSubScene (SubScene subScene)
		{
			SpawnAllInactivePlayers ();
		}


		/**
		 * <summary>Gets the number of save game files found.</summary>
		 * <param name = "includeAutoSaves">If True, then autosave files will be included in the result</param>
		 * <returns>The number of save games found</returns>
		 */
		public int GetNumSaves (bool includeAutoSaves = true)
		{
			int numFound = 0;
			foreach (SaveFile saveFile in foundSaveFiles)
			{
				if (!saveFile.IsAutoSave || includeAutoSaves)
				{
					numFound ++;
				}
			}
			return numFound;
		}


		/**
		 * If True, then a save-game screenshot is being taken
		 */
		public bool IsTakingSaveScreenshot
		{
			get
			{
				return isTakingSaveScreenshot;
			}
		}


		/**
		 * <summary>Gets an existing SaveFile found on the system</summary>
		 * <param name="saveID">The ID number of the save to retrieve</param>
		 * <returns>The SaveFile class instance</returns>
		 */
		public SaveFile GetSaveFile (int saveID)
		{
			foreach (SaveFile saveFile in foundSaveFiles)
			{
				if (saveFile.saveID == saveID)
				{
					return saveFile;
				}
			}
			return null;
		}

		
		/** The iSaveFileHandler class that handles the creation, loading, and deletion of save files */
		public static iSaveFileHandler SaveFileHandler
		{
			get
			{
				if (saveFileHandlerOverride != null)
				{
					return saveFileHandlerOverride;
				}

				#if SAVE_IN_PLAYERPREFS
				return new SaveFileHandler_PlayerPrefs ();
				#else
				return new SaveFileHandler_SystemFile ();
				#endif
			}
			set
			{
				saveFileHandlerOverride = value;
			}
		}


		/** The iFileFormatHandler class that handles the serialization and deserialzation of data */
		public static iFileFormatHandler FileFormatHandler
		{
			get
			{
				if (fileFormatHandlerOverride != null)
				{
					return fileFormatHandlerOverride;
				}

				#if SAVE_USING_XML
				return new FileFormatHandler_Xml ();
				#else
				return new FileFormatHandler_Binary ();
				#endif
			}
			set
			{
				fileFormatHandlerOverride = value;
			}
		}


		/** The iFileFormatHandler class that handles the serialization and deserialzation of Options data.  If this is not explicitly set, it will return the same value as FileFormatHandler */
		public static iFileFormatHandler OptionsFileFormatHandler
		{
			get
			{
				if (optionsFileFormatHandlerOverride != null)
				{
					return optionsFileFormatHandlerOverride;
				}
				return FileFormatHandler;
			}
			set
			{
				optionsFileFormatHandlerOverride = value;
			}
		}


		/** What type of load is being performed (No, InNewScene, InSameScene, JustSwitchingPlayer) */
		public LoadingGame loadingGame
		{
			get
			{
				return _loadingGame;
			}
		}


		public string PersistentDataPath
		{
			get
			{
				if (string.IsNullOrEmpty (persistentDataPath))
				{
					persistentDataPath = Application.persistentDataPath;
				}
				return persistentDataPath;
			}
		}


		public static string SaveLabel
		{
			get
			{
				if (KickStarter.runtimeLanguages)
				{
					return KickStarter.runtimeLanguages.GetTranslatableText (KickStarter.settingsManager.saveLabels, 0);
				}
				return "Save";
			}
		}


		public static string ImportLabel
		{
			get
			{
				if (KickStarter.runtimeLanguages)
				{
					return KickStarter.runtimeLanguages.GetTranslatableText (KickStarter.settingsManager.saveLabels, 1);
				}
				return "Import";
			}
		}


		public static string AutosaveLabel
		{
			get
			{
				if (KickStarter.runtimeLanguages)
				{
					return KickStarter.runtimeLanguages.GetTranslatableText (KickStarter.settingsManager.saveLabels, 2);
				}
				return "Autosave";
			}
		}

	}

}