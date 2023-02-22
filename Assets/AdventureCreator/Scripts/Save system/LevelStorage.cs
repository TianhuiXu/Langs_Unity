/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"LevelStorage.cs"
 * 
 *	This script handles the loading and unloading of per-scene data.
 *	Below the main class is a series of data classes for the different object types.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if AddressableIsPresent
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using System.Collections;
#endif

namespace AC
{

	/**
	 * Manages the loading and storage of per-scene data (the various Remember scripts).
	 * This needs to be attached to the PersistentEngine prefab
	 */
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_level_storage.html")]
	public class LevelStorage : MonoBehaviour
	{

		#region Variables

		/** A collection of level data for each visited scene */
		[HideInInspector] public List<SingleLevelData> allLevelData = new List<SingleLevelData> ();

		#endregion


		#region PublicFunctions

		public void OnInitPersistentEngine ()
		{
			ClearAllLevelData ();
		}


		/** Wipes all stored scene save data from memory. */
		public void ClearAllLevelData ()
		{
			allLevelData.Clear ();
			allLevelData = new List<SingleLevelData>();
		}


		/**
		 * <summary>Wipes stored data for a specific scene from memory.</summary>
		 * <param name="sceneIndex">The build index number of the scene to clear save data for</param>
		 */
		public void ClearLevelData (int sceneIndex)
		{
			if (allLevelData == null) return;
			foreach (SingleLevelData levelData in allLevelData)
			{
				if (levelData.sceneNumber == sceneIndex)
				{
					allLevelData.Remove (levelData);
					return;
				}
			}
		}


		/**
		 * <summary>Wipes stored data for a specific scene from memory.</summary>
		 * <param name="sceneName">The name of the scene to clear save data for</param>
		 */
		public void ClearLevelData (string sceneName)
		{
			if (allLevelData == null) return;
			foreach (SingleLevelData levelData in allLevelData)
			{
				if (levelData.sceneName == sceneName)
				{
					allLevelData.Remove (levelData);
					return;
				}
			}
		}


		/** Wipes the currently-loaded scene's save data from memory */
		public void ClearCurrentLevelData ()
		{
			if (allLevelData == null) allLevelData = new List<SingleLevelData>();
			foreach (SingleLevelData levelData in allLevelData)
			{
				switch (KickStarter.settingsManager.referenceScenesInSave)
				{
					case ChooseSceneBy.Name:
						if (levelData.sceneName == SceneChanger.CurrentSceneName)
						{
							allLevelData.Remove (levelData);
							return;
						}
						break;

					case ChooseSceneBy.Number:
					default:
						if (levelData.sceneNumber == SceneChanger.CurrentSceneIndex)
						{
							allLevelData.Remove (levelData);
							return;
						}
						break;
				}
			}
		}


		/**
		 * <summary>Removes all data related to a given object's Constant ID value in the current scene. This is equivalent to resetting that object's Remember component values, so that it has no Remember data stored</param>
		 * <param name = "constantID">The object's Constant ID value</param>
		 */
		public void RemoveDataFromCurrentLevelData (int constantID)
		{
			if (allLevelData == null) allLevelData = new List<SingleLevelData>();
			foreach (SingleLevelData levelData in allLevelData)
			{
				switch (KickStarter.settingsManager.referenceScenesInSave)
				{
					case ChooseSceneBy.Name:
						if (levelData.sceneName == SceneChanger.CurrentSceneName)
						{
							levelData.RemoveDataForID (constantID);
							return;
						}
						break;

					case ChooseSceneBy.Number:
					default:
						if (levelData.sceneNumber == SceneChanger.CurrentSceneIndex)
						{
							levelData.RemoveDataForID (constantID);
							return;
						}
						break;
				}
			}
		}
		

		/** Returns the currently-loaded scene's save data to the appropriate Remember components. */
		public void ReturnCurrentLevelData ()
		{
			SingleLevelData levelData = GetLevelData ();

			if (levelData == null)
			{
				return;
			}

			LoadSceneData (levelData);
			AssetLoader.UnloadAssets ();
		}


		/**
		 * <summary>Returns a sub-scene's save data to the appropriate Remember components.</summary>
		 * <param name = "subScene">The SubScene component associated with the sub-scene</param>
		 */
		public void ReturnSubSceneData (SubScene subScene)
		{
			SingleLevelData levelData = null;
			
			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					levelData = GetLevelData (subScene.SceneName);
					break;

				case ChooseSceneBy.Number:
				default:
					levelData = GetLevelData (subScene.SceneIndex);
					break;
			}

			if (levelData == null)
			{
				return;
			}

			LoadSceneData (levelData, subScene);
			AssetLoader.UnloadAssets ();
		}


		public PlayerData SavePlayerData (Player player, PlayerData playerData)
		{
			List<ScriptData> playerScriptData = new List<ScriptData> ();
			Remember[] playerSaveScripts = player.gameObject.GetComponentsInChildren<Remember> ();

			foreach (Remember remember in playerSaveScripts)
			{
				if (remember.constantID != 0)
				{
					if (remember.retainInPrefab)
					{
						playerScriptData.Add (new ScriptData (remember.constantID, remember.SaveData ()));
					}
					else
					{
						ACDebug.LogWarning ("Could not save GameObject " + remember.name + " because 'Retain in prefab?' is not checked!", remember);
					}
				}
				else
				{
					ACDebug.LogWarning ("GameObject " + remember.name + " was not saved because its ConstantID has not been set!", remember);
				}
			}

			playerData.playerScriptData = playerScriptData;

			return playerData;
		}


		public void LoadPlayerData (Player player, PlayerData playerData)
		{
			Remember[] playerSaveScripts = player.gameObject.GetComponentsInChildren<Remember> ();
			if (playerData.playerScriptData != null)
			{
				foreach (ScriptData _scriptData in playerData.playerScriptData)
				{
					if (!string.IsNullOrEmpty (_scriptData.data))
					{
						foreach (Remember playerSaveScript in playerSaveScripts)
						{
							if (playerSaveScript.constantID == _scriptData.objectID)
							{
								playerSaveScript.LoadData (_scriptData.data);
							}
						}
					}
				}
			}
			AssetLoader.UnloadAssets ();
		}


		public MainData SavePersistentData (MainData mainData)
		{
			List<ScriptData> persistentScriptData = new List<ScriptData> ();

			HashSet<Remember> persistentSaveScripts = KickStarter.stateHandler.ConstantIDManager.GetPersistentButNotPlayerComponents <Remember>();
			foreach (Remember remember in persistentSaveScripts)
			{
				if (remember.constantID != 0)
				{
					if (remember.retainInPrefab)
					{
						persistentScriptData.Add (new ScriptData (remember.constantID, remember.SaveData ()));
					}
					else
					{
						ACDebug.LogWarning ("Could not save GameObject " + remember.name + " because 'Retain in prefab?' is not checked!", remember);
					}
				}
				else
				{
					ACDebug.LogWarning ("GameObject " + remember.name + " was not saved because its ConstantID has not been set!", remember);
				}
			}

			mainData.persistentScriptData = persistentScriptData;

			return mainData;
		}


		public void LoadPersistentData (MainData mainData)
		{
			HashSet<Remember> persistentSaveScripts = KickStarter.stateHandler.ConstantIDManager.GetPersistentButNotPlayerComponents <Remember>();
			if (mainData.persistentScriptData != null)
			{
				foreach (ScriptData _scriptData in mainData.persistentScriptData)
				{
					if (!string.IsNullOrEmpty (_scriptData.data))
					{
						foreach (Remember remember in persistentSaveScripts)
						{
							if (remember.constantID == _scriptData.objectID)
							{
								remember.LoadData (_scriptData.data);
							}
						}
					}
				}
			}
			AssetLoader.UnloadAssets ();
		}


		/** Combs the active scene for data to store, combines it into a SingleLevelData variable, and adds it to the SingleLevelData List, allLevelData. */
		public void StoreCurrentLevelData ()
		{
			// Active scene
			SaveSceneData ();
		}


		/** Combs all open scenes for data to store, combines each into a SingleLevelData variable, and adds them to the SingleLevelData List, allLevelData. */
		public void StoreAllOpenLevelData ()
		{
			// Active scene
			SaveSceneData ();

			// Sub-scenes
			foreach (SubScene subScene in KickStarter.sceneChanger.SubScenes)
			{
				SaveSceneData (subScene);
			}
		}


		/**
		 * <summary>Combs a sub-scene for data to store, combines it into a SingleLevelData variable, and adds it to the SingleLevelData List, allLevelData.</summary>
		 * <param name = "subScene">The SubScene component associated with the sub-scene</param>
		 */
		public void StoreSubSceneData (SubScene subScene)
		{
			SaveSceneData (subScene);
		}

		#endregion


		#region PrivateFunctions

		private SingleLevelData GetLevelData ()
		{
			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					return GetLevelData (SceneChanger.CurrentSceneName);

				case ChooseSceneBy.Number:
				default:
					return GetLevelData (SceneChanger.CurrentSceneIndex);
			}
		}


		private SingleLevelData GetLevelData (int sceneNumber)
		{
			if (allLevelData == null) allLevelData = new List<SingleLevelData>();
			if (allLevelData != null)
			{
				foreach (SingleLevelData levelData in allLevelData)
				{
					if (levelData.sceneNumber == sceneNumber)
					{
						return levelData;
					}
				}
			}
			return null;
		}


		private SingleLevelData GetLevelData (string sceneName)
		{
			if (allLevelData == null) allLevelData = new List<SingleLevelData> ();
			if (allLevelData != null)
			{
				foreach (SingleLevelData levelData in allLevelData)
				{
					if (levelData.sceneName == sceneName)
					{
						return levelData;
					}
				}
			}
			return null;
		}


		private void LoadSceneData (SingleLevelData levelData, SubScene subScene = null)
		{
			Scene scene = (subScene) ? subScene.gameObject.scene : SceneChanger.CurrentScene;

			SceneSettings sceneSettings = (subScene == null) ? KickStarter.sceneSettings : subScene.SceneSettings;
			LocalVariables localVariables = (subScene == null) ? KickStarter.localVariables : subScene.LocalVariables;
			KickStarter.actionListManager.LoadData (levelData.activeLists, subScene);

			if (sceneSettings)
			{
				UnloadCutsceneOnLoad (levelData.onLoadCutscene, sceneSettings);
				UnloadCutsceneOnStart (levelData.onStartCutscene, sceneSettings);
				UnloadNavMesh (levelData.navMesh, sceneSettings);
				UnloadPlayerStart (levelData.playerStart, sceneSettings);
				UnloadSortingMap (levelData.sortingMap, sceneSettings);
				UnloadTintMap (levelData.tintMap, sceneSettings);
			}

			UnloadTransformData (levelData.allTransformData, scene);
			UnloadScriptData (levelData.allScriptData, scene);
			
			if (localVariables)
			{
				localVariables.localVars = SaveSystem.UnloadVariablesData (levelData.localVariablesData, true, localVariables.localVars);
			}
		}


		private void SaveSceneData (SubScene subScene = null)
		{
			Scene scene = (subScene) ? subScene.gameObject.scene : SceneChanger.CurrentScene;

			SceneSettings sceneSettings = (subScene == null) ? KickStarter.sceneSettings : subScene.SceneSettings;
			LocalVariables localVariables = (subScene == null) ? KickStarter.localVariables : subScene.LocalVariables;

			List<TransformData> thisLevelTransforms = PopulateTransformData (scene);
			List<ScriptData> thisLevelScripts = PopulateScriptData (scene);

			SingleLevelData thisLevelData = new SingleLevelData ();
			thisLevelData.sceneNumber = (subScene == null) ? SceneChanger.CurrentSceneIndex : subScene.SceneIndex;
			thisLevelData.sceneName = (subScene == null) ? SceneChanger.CurrentSceneName : subScene.SceneName;

			thisLevelData.activeLists = KickStarter.actionListManager.GetSaveData (subScene);
			
			if (sceneSettings)
			{
				if (sceneSettings.navMesh)
				{
					thisLevelData.navMesh = Serializer.GetConstantID (sceneSettings.navMesh.gameObject, false);
				}
				if (sceneSettings.defaultPlayerStart)
				{
					thisLevelData.playerStart = Serializer.GetConstantID (sceneSettings.defaultPlayerStart.gameObject, false);
				}
				if (sceneSettings.sortingMap)
				{
					thisLevelData.sortingMap = Serializer.GetConstantID (sceneSettings.sortingMap.gameObject, false);
				}
				if (sceneSettings.cutsceneOnLoad)
				{
					thisLevelData.onLoadCutscene = Serializer.GetConstantID (sceneSettings.cutsceneOnLoad.gameObject, false);
				}
				if (sceneSettings.cutsceneOnStart)
				{
					thisLevelData.onStartCutscene = Serializer.GetConstantID (sceneSettings.cutsceneOnStart.gameObject, false);
				}
				if (sceneSettings.tintMap)
				{
					thisLevelData.tintMap = Serializer.GetConstantID (sceneSettings.tintMap.gameObject, false);
				}
			}

			if (localVariables)
			{ 
				thisLevelData.localVariablesData = SaveSystem.CreateVariablesData (localVariables.localVars, false, VariableLocation.Local);
			}
			thisLevelData.allTransformData = thisLevelTransforms;
			thisLevelData.allScriptData = thisLevelScripts;

			if (allLevelData == null) allLevelData = new List<SingleLevelData>();
			for (int i=0; i<allLevelData.Count; i++)
			{
				if (allLevelData[i].DataMatchesScene (thisLevelData))
				{
					allLevelData[i] = thisLevelData;
					return;
				}
			}
			
			allLevelData.Add (thisLevelData);
		}

		
		private void UnloadNavMesh (int navMeshInt, SceneSettings sceneSettings)
		{
			NavigationMesh navMesh = ConstantID.GetComponent <NavigationMesh> (navMeshInt, sceneSettings.gameObject.scene);

			if (navMesh != null && sceneSettings.navigationMethod != AC_NavigationMethod.UnityNavigation)
			{
				if (sceneSettings.navMesh)
				{
					NavigationMesh oldNavMesh = sceneSettings.navMesh;
					oldNavMesh.TurnOff ();
				}

				navMesh.TurnOn ();
				sceneSettings.navMesh = navMesh;

				// Bugfix: Need to cycle this otherwise weight caching doesn't always work
				navMesh.TurnOff ();
				navMesh.TurnOn ();
			}
		}


		private void UnloadPlayerStart (int playerStartInt, SceneSettings sceneSettings)
		{
			PlayerStart playerStart = ConstantID.GetComponent <PlayerStart> (playerStartInt, sceneSettings.gameObject.scene);
			if (playerStart)
			{
				sceneSettings.defaultPlayerStart = playerStart;
			}
		}


		private void UnloadSortingMap (int sortingMapInt, SceneSettings sceneSettings)
		{
			SortingMap sortingMap = ConstantID.GetComponent <SortingMap> (sortingMapInt, sceneSettings.gameObject.scene);
			if (sortingMap)
			{
				KickStarter.sceneSettings.SetSortingMap (sortingMap);
			}
		}


		private void UnloadTintMap (int tintMapInt, SceneSettings sceneSettings)
		{
			TintMap tintMap = ConstantID.GetComponent <TintMap> (tintMapInt, sceneSettings.gameObject.scene);
			if (tintMap)
			{
				sceneSettings.SetTintMap (tintMap);
			}
		}


		private void UnloadCutsceneOnLoad (int cutsceneInt, SceneSettings sceneSettings)
		{
			Cutscene cutscene = ConstantID.GetComponent <Cutscene> (cutsceneInt, sceneSettings.gameObject.scene);
			if (cutscene)
			{
				sceneSettings.cutsceneOnLoad = cutscene;
			}
		}


		private void UnloadCutsceneOnStart (int cutsceneInt, SceneSettings sceneSettings)
		{
			Cutscene cutscene = ConstantID.GetComponent <Cutscene> (cutsceneInt, sceneSettings.gameObject.scene);

			if (cutscene)
			{
				sceneSettings.cutsceneOnStart = cutscene;
			}
		}


		private List<TransformData> PopulateTransformData (Scene scene)
		{
			List<TransformData> allTransformData = new List<TransformData>();
			HashSet<RememberTransform> transforms = ConstantID.GetComponents <RememberTransform> (scene);

			foreach (RememberTransform _transform in transforms)
			{
				if (_transform.constantID != 0)
				{
					allTransformData.Add (_transform.SaveTransformData ());
				}
				else
				{
					ACDebug.LogWarning ("GameObject " + _transform.name + " was not saved because its ConstantID has not been set!", _transform);
				}
			}
			
			return allTransformData;
		}


		private void UnloadTransformData (List<TransformData> allTransformData, Scene scene)
		{
			// Delete any objects (if told to)
			HashSet<RememberTransform> currentTransforms = ConstantID.GetComponents <RememberTransform> (scene);
			foreach (RememberTransform transformOb in currentTransforms)
			{
				if (transformOb.saveScenePresence)
				{
					// Was object not saved?
					bool found = false;
					foreach (TransformData transformData in allTransformData)
					{
						if (transformData.objectID == transformOb.constantID)
						{
							found = !transformData.savePrevented;
						}
					}

					if (!found)
					{
						// Can't find: delete
						KickStarter.sceneChanger.ScheduleForDeletion (transformOb.gameObject);
					}
				}
			}

			#if AddressableIsPresent
			if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
			{
				StopAllCoroutines ();
				StartCoroutine (UnloadTransformDataFromAddressables (allTransformData, scene));
				return;
			}
			#endif

			Object[] prefabAssets = null;
			bool searchedResources = false;
			
			foreach (TransformData transformData in allTransformData)
			{
				RememberTransform saveObject = ConstantID.GetComponent <RememberTransform> (transformData.objectID, scene);

				if (saveObject == null)
				{
					// Restore any deleted objects (if told to)
					if (transformData.bringBack && !transformData.savePrevented)
					{
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
								RememberTransform prefabRememberTransform = prefabGameObject.GetComponent<RememberTransform>();
								if (prefabRememberTransform)
								{
									int prefabID = prefabRememberTransform.constantID;
									if ((transformData.linkedPrefabID != 0 && prefabID == transformData.linkedPrefabID) ||
										(transformData.linkedPrefabID == 0 && prefabID == transformData.objectID))
									{
										GameObject newObject = Instantiate (prefabGameObject);
										newObject.name = prefabGameObject.name;
										saveObject = newObject.GetComponent <RememberTransform>();
										foundObject = true;

										if (transformData.linkedPrefabID != 0 && prefabID == transformData.linkedPrefabID)
										{
											// Spawned object has wrong ID, re-assign it
											ConstantID[] idScripts = saveObject.GetComponents <ConstantID>();
											foreach (ConstantID idScript in idScripts)
											{
												idScript.constantID = transformData.objectID;
											}
										}

										break;
									}
								}
							}
						}

						if (!foundObject)
						{
							ACDebug.LogWarning ("Could not find Resources prefab with ID " + transformData.objectID + " - is it placed in a Resources folder?");
						}
					}
				}

				if (saveObject)
				{
					saveObject.LoadTransformData (transformData);
				}
			}

			if (searchedResources)
			{
				Resources.UnloadUnusedAssets ();
			}
			KickStarter.stateHandler.IgnoreNavMeshCollisions ();
		}


		#if AddressableIsPresent

		private IEnumerator UnloadTransformDataFromAddressables (List<TransformData> allTransformData, Scene scene)
		{
			foreach (TransformData transformData in allTransformData)
			{
				RememberTransform saveObject = ConstantID.GetComponent<RememberTransform> (transformData.objectID, scene);

				if (saveObject == null)
				{
					// Restore any deleted objects (if told to)
					if (transformData.bringBack && !transformData.savePrevented)
					{
						AsyncOperationHandle<GameObject> goHandle = Addressables.LoadAssetAsync<GameObject> (transformData.addressableName);
						yield return goHandle;
						if (goHandle.Status == AsyncOperationStatus.Succeeded)
						{
							GameObject prefabGameObject = goHandle.Result;
							if (prefabGameObject)
							{
								GameObject newObject = Instantiate (prefabGameObject);
								newObject.name = prefabGameObject.name;
								saveObject = newObject.GetComponent<RememberTransform> ();
								saveObject.LoadTransformData (transformData);
							}
						}
						Addressables.Release (goHandle);
					}
				}
			}

			KickStarter.stateHandler.IgnoreNavMeshCollisions ();
		}

		#endif


		private void UnloadScriptData (List<ScriptData> allScriptData, Scene scene)
		{
			HashSet<Remember> saveObjects = ConstantID.GetComponents <Remember> (scene);
			foreach (ScriptData _scriptData in allScriptData)
			{
				if (!string.IsNullOrEmpty (_scriptData.data))
				{
					foreach (Remember saveObject in saveObjects)
					{
						if (!saveObject.isActiveAndEnabled) continue;

						if (saveObject.constantID == _scriptData.objectID)
						{
							saveObject.LoadData (_scriptData.data);
						}
					}
				}
			}
		}


		private List<ScriptData> PopulateScriptData (Scene scene)
		{
			List<ScriptData> allScriptData = new List<ScriptData>();
			HashSet<Remember> scripts = ConstantID.GetComponents <Remember> (scene);

			foreach (Remember _script in scripts)
			{
				if (!_script.isActiveAndEnabled) continue;

				if (_script.constantID != 0)
				{
					allScriptData.Add (new ScriptData (_script.constantID, _script.SaveData ()));
				}
				else
				{
					ACDebug.LogWarning ("GameObject " + _script.name + " was not saved because its ConstantID has not been set!", _script);
				}
			}
			
			return allScriptData;
		}

		#endregion

	}


	/** A data container for a single scene's save data. Used by the LevelStorage component. */
	[System.Serializable]
	public class SingleLevelData
	{

		/** A List of all data recorded by the scene's Remember scripts */
		public List<ScriptData> allScriptData;
		/** A List of all data recorded by the scene's RememberTransform scripts */
		public List<TransformData> allTransformData;
		/** The scene number this data is for */
		public int sceneNumber;
		/** The scene name this data is for */
		public string sceneName;

		/** The ConstantID number of the default NavMesh */
		public int navMesh;
		/** The ConstantID number of the default PlayerStart */
		public int playerStart;
		/** The ConstantID number of the scene's SortingMap */
		public int sortingMap;
		/** The ConstantID number of the scene's TintMap */
		public int tintMap;
		/** The ConstantID number of the "On load" Cutscene */
		public int onLoadCutscene;
		/** The ConstantID number of the "On start" Cutscene */
		public int onStartCutscene;
		/** Data regarding paused and skipping ActionLists */
		public string activeLists;

		/** The values of the scene's local Variables, combined into a single string */
		public string localVariablesData;


		/** The default Constructor. */
		public SingleLevelData ()
		{
			allScriptData = new List<ScriptData> ();
			allTransformData = new List<TransformData> ();
		}


		/**
		 * <summary>Checks if a given SingleLevelData class instance matches this own instance's intended scene</summary>
		 * <param name = "otherLevelData">The other class instance to check</param>
		 * <returns>True if the two instances match the same scene</returns>
		 */
		public bool DataMatchesScene (SingleLevelData otherLevelData)
		{
			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					if (otherLevelData.sceneName == sceneName)
					{
						return true;
					}
					return false;

				case ChooseSceneBy.Number:
				default:
					if (otherLevelData.sceneNumber == sceneNumber)
					{
						return true;
					}
					return false;
			}
		}


		/**
		 * <summary>Removes all save data related to an object with a given Constant ID</summary>
		 * <param name = "id">The object's ConstantID value</param>
		 */
		public void RemoveDataForID (int id)
		{
			foreach (ScriptData scriptData in allScriptData)
			{
				if (scriptData.objectID == id)
				{
					allScriptData.Remove (scriptData);
				}
			}

			foreach (TransformData transformData in allTransformData)
			{
				if (transformData.objectID == id)
				{
					allTransformData.Remove (transformData);
				}
			}
		}


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.MultiLineLabelGUI ("Scene number:", sceneNumber.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Scene name:", sceneName);
			CustomGUILayout.MultiLineLabelGUI ("Active NavMesh:", navMesh.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Default PlayerStart:", playerStart.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Default SortingMap:", sortingMap.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Default TintMap:", tintMap.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("OnStart Cutscene:", onStartCutscene.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("OnLoad Cutscene:", onLoadCutscene.ToString ());

			if (allScriptData != null && allScriptData.Count > 0)
			{
				EditorGUILayout.LabelField ("Remember data:");
				foreach (ScriptData scriptData in allScriptData)
				{
					if (string.IsNullOrEmpty (scriptData.data))
					{ 
						Debug.LogWarning ("Invalid Remember data for object ID " + scriptData.objectID + " in scene " + sceneName + ", " + sceneNumber);
						continue;
					}
					RememberData rememberData = SaveSystem.FileFormatHandler.DeserializeObject<RememberData> (scriptData.data);
					if (rememberData != null)
					{
						CustomGUILayout.MultiLineLabelGUI ("   " + rememberData.GetType ().ToString () + ":", EditorJsonUtility.ToJson (rememberData, true));
					}
				}
			}

			if (allTransformData != null && allTransformData.Count > 0)
			{
				foreach (TransformData transformData in allTransformData)
				{
					CustomGUILayout.MultiLineLabelGUI ("   " + transformData.GetType ().ToString () + ":", EditorJsonUtility.ToJson (transformData, true));
				}
			}

			CustomGUILayout.MultiLineLabelGUI ("Active ActionLists:", activeLists.ToString ());
			CustomGUILayout.MultiLineLabelGUI ("Local Variables:", localVariablesData);
		}

		#endif

	}


	/** A data container for save data returned by each Remember script.  Used by the SingleLevelData class. */
	[System.Serializable]
	public struct ScriptData
	{

		/** The Constant ID number of the Remember script component */
		public int objectID;
		/** The data returned by the Remember script, serialised into a string */
		public string data;


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_objectID">The Remember script's Constant ID number</param>
		 * <param name = "_data">The serialised data</param>
		 */
		public ScriptData (int _objectID, string _data)
		{
			objectID = _objectID;
			data = _data;
		}
	}

}