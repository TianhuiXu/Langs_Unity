/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SceneManager.cs"
 * 
 *	This script handles the "Scene" tab of the main wizard.
 *	It is used to create the prefabs needed to run the game,
 *	as well as provide easy-access to game logic.
 * 
 */
#if UNITY_2018_3_OR_NEWER
	#define NEW_PREFABS
#endif

using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Collections.Generic;
#endif


namespace AC
{

	/**
	 * Handles the "Scene" tab of the Game Editor window.
	 * It doesn't store any data from the scene itself, only references objects that do.
	 * It provides a list of Adventure Creator prefabs that can be created and managed.
	 */
	[System.Serializable]
	public class SceneManager : ScriptableObject
	{
		
		#if UNITY_EDITOR

		private readonly string[] cameraPerspective_list = { "2D", "2.5D", "3D" };
		private bool tempOverrideCameraPerspective;
		private CameraPerspective tempCameraPerspective;
		private MovingTurning tempMovingTurning = MovingTurning.Unity2D;
		
		private int selectedSceneObject;
		private string[] prefabTextArray;
		
		[SerializeField] private int activeScenePrefab;
		private List<ScenePrefab> scenePrefabs;
		
		private string newFolderName = "";
		private string newPrefabName;
		private bool positionHotspotOverMesh = false;
		private static GUILayoutOption buttonWidth = GUILayout.MaxWidth(120f);

		private bool showStructure = true;
		private bool showSettings = true;
		private bool showCutscenes = true;
		private bool showVisibility = true;
		private bool showPrefabs = true;
		private bool showAttributes = true;
		
		private Texture2D hotspotIcon, triggerIcon, collisionIcon, markerIcon, playerStartIcon, navMeshIcon;


		private static string assetFolder
		{
			get
			{
				return Resource.MainFolderPath + "/Prefabs/";
			}
		}


		/** Shows the GUI. */
		public void ShowGUI (Rect windowRect)
		{
			ShowSettingsGUI (windowRect);

			if (GUI.changed)
			{
				UnityVersionHandler.CustomSetDirty (KickStarter.sceneSettings);
				UnityVersionHandler.CustomSetDirty (KickStarter.playerMovement);

				if (KickStarter.sceneSettings != null && KickStarter.sceneSettings.defaultPlayerStart != null)
				{
					UnityVersionHandler.CustomSetDirty (KickStarter.sceneSettings.defaultPlayerStart);
				}
			}
		}


		private void ShowSettingsGUI (Rect windowRect)
		{
			string sceneName = MultiSceneChecker.EditActiveScene ();
			if (!string.IsNullOrEmpty (sceneName) && UnityEngine.SceneManagement.SceneManager.sceneCount > 1)
			{
				EditorGUILayout.LabelField ("Editing scene: '" + sceneName + "'", EditorStyles.boldLabel);
				EditorGUILayout.Space ();
				GetPrefabsInScene ();
			}

			SceneStructureGUI ();

			if (AdvGame.GetReferences ().settingsManager == null)
			{
				EditorGUILayout.HelpBox ("No Settings Manager defined - cannot display full Editor without it!", MessageType.Warning);
				return;
			}
			
			if (KickStarter.sceneSettings == null)
			{
				return;
			}

			EditorGUILayout.Space ();

			SceneSettingsGUI ();

			EditorGUILayout.Space ();

			CutscenesGUI ();

			EditorGUILayout.Space ();

			SceneAttributesGUI ();

			EditorGUILayout.Space ();

			VisibilityGUI (windowRect);

			EditorGUILayout.Space ();

			ListPrefabs (windowRect);
		}


		private void SceneStructureGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showStructure = CustomGUILayout.ToggleHeader (showStructure, "Basic structure");
			if (showStructure)
			{
				if (KickStarter.sceneSettings == null && KickStarter.settingsManager != null && KickStarter.settingsManager.movementMethod != MovementMethod.FirstPerson)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Override the default camera perspective?");
					tempOverrideCameraPerspective = EditorGUILayout.Toggle (tempOverrideCameraPerspective);
					EditorGUILayout.EndHorizontal ();
					if (tempOverrideCameraPerspective)
					{
						int cameraPerspective_int = (int) tempCameraPerspective;
						cameraPerspective_int = EditorGUILayout.Popup ("Camera perspective:", cameraPerspective_int, cameraPerspective_list);
						tempCameraPerspective = (CameraPerspective) cameraPerspective_int;

						if (tempCameraPerspective == CameraPerspective.TwoD)
						{
							tempMovingTurning = (MovingTurning) EditorGUILayout.EnumPopup ("Moving and turning:", tempMovingTurning);
							if (tempMovingTurning == MovingTurning.TopDown)
							{
								EditorGUILayout.HelpBox ("This mode is now deprecated - use Unity 2D mode instead.", MessageType.Warning);
							}
						}
					}
				}

				if (KickStarter.sceneSettings != null)
				{
					KickStarter.sceneSettings.ShowCameraOverrideLabel ();
				}

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Label ("Organise scene:");
				if (GUILayout.Button ("With folders", EditorStyles.miniButtonLeft))
				{
					InitialiseObjects ();
				}
				if (GUILayout.Button ("Without folders", EditorStyles.miniButtonRight))
				{
					InitialiseObjects (false);
				}
				EditorGUILayout.EndHorizontal ();

				if (KickStarter.sceneSettings == null)
				{
					CustomGUILayout.EndVertical ();
					return;
				}

				EditorGUILayout.BeginHorizontal ();
				newFolderName = EditorGUILayout.TextField (newFolderName);
		
				if (GUILayout.Button ("Create new folder", buttonWidth))
				{
					if (newFolderName != "")
					{
						GameObject newFolder = new GameObject();
						
						if (!newFolderName.StartsWith ("_"))
							newFolder.name = "_" + newFolderName;
						else
							newFolder.name = newFolderName;
						
						Undo.RegisterCreatedObjectUndo (newFolder, "Create folder " + newFolder.name);
						
						if (Selection.activeGameObject)
						{
							newFolder.transform.parent = Selection.activeGameObject.transform;
						}
						
						Selection.activeObject = newFolder;
					}
				}
				EditorGUILayout.EndHorizontal ();
			}
			CustomGUILayout.EndVertical ();
		}


		private void SceneSettingsGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSettings = CustomGUILayout.ToggleHeader (showSettings, "Scene settings");
			if (showSettings)
			{
				KickStarter.sceneSettings.navigationMethod = (AC_NavigationMethod) CustomGUILayout.EnumPopup ("Pathfinding method:", KickStarter.sceneSettings.navigationMethod, "AC.KickStarter.sceneSettings.navigationMethod", "The scene's navigation method");
				if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.Custom)
				{
					KickStarter.sceneSettings.customNavigationClass = CustomGUILayout.DelayedTextField ("Script name:", KickStarter.sceneSettings.customNavigationClass, "AC.KickStarter.sceneSettings.customNavigationClass", "The class name of the NavigationEngine ScriptableObject that is used to handle pathfinding");
				}
				KickStarter.navigationManager.ResetEngine ();
				if (KickStarter.navigationManager.navigationEngine != null)
				{
					KickStarter.navigationManager.navigationEngine.SceneSettingsGUI ();
				}
				
				EditorGUILayout.BeginHorizontal ();
				KickStarter.sceneSettings.defaultPlayerStart = (PlayerStart) CustomGUILayout.ObjectField <PlayerStart> ("Default PlayerStart:", KickStarter.sceneSettings.defaultPlayerStart, true, "AC.KickStarter.sceneSettings.defaultPlayerStart", "The scene's default PlayerStart");
				if (KickStarter.sceneSettings.defaultPlayerStart == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						PlayerStart newPlayerStart = (SceneSettings.IsUnity2D ())
													? AddPrefab ("Navigation", "PlayerStart2D", true, false, true).GetComponent <PlayerStart>()
													: AddPrefab ("Navigation", "PlayerStart", true, false, true).GetComponent <PlayerStart>();

						newPlayerStart.gameObject.name = "Default PlayerStart";
						KickStarter.sceneSettings.defaultPlayerStart = newPlayerStart;
						EditorGUIUtility.PingObject (newPlayerStart.gameObject);
					}
				}
				EditorGUILayout.EndHorizontal ();
				if (KickStarter.sceneSettings.defaultPlayerStart)
				{
					EditorGUILayout.BeginHorizontal ();
					KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Default Camera:", KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart, true, "AC.KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart", "The Camera that should be made active when the Player starts the scene from this point");
					if (KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart == null)
					{
						if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
						{
							if (SceneSettings.CameraPerspective == CameraPerspective.ThreeD)
							{
								GameCamera newCamera = AddPrefab ("Camera", "GameCamera", true, false, true).GetComponent <GameCamera>();
								newCamera.gameObject.name = "NavCam 1";
								KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart = newCamera;
							}
							else if (SceneSettings.CameraPerspective == CameraPerspective.TwoD)
							{
								GameCamera2D newCamera = AddPrefab ("Camera", "GameCamera2D", true, false, true).GetComponent <GameCamera2D>();
								newCamera.gameObject.name = "NavCam 1";
								KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart = newCamera;
							}
							else if (SceneSettings.CameraPerspective == CameraPerspective.TwoPointFiveD)
							{
								GameCamera25D newCamera = AddPrefab ("Camera", "GameCamera2.5D", true, false, true).GetComponent <GameCamera25D>();
								newCamera.gameObject.name = "NavCam 1";
								KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart = newCamera;
							}
							EditorGUIUtility.PingObject (KickStarter.sceneSettings.defaultPlayerStart.cameraOnStart);
						}
					}
					else
					{
						if (KickStarter.settingsManager != null && KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
						{
							EditorGUILayout.EndHorizontal ();
							EditorGUILayout.HelpBox ("Since this is a First Person game, the scene's default camera will be overridden during gameplay by the Player's first person camera.", MessageType.Info);
							EditorGUILayout.BeginHorizontal ();
						}
					}
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.BeginHorizontal ();
				KickStarter.sceneSettings.sortingMap = (SortingMap) CustomGUILayout.ObjectField <SortingMap> ("Default Sorting map:", KickStarter.sceneSettings.sortingMap, true, "AC.KickStarter.sceneSettings.sortingMap", "The scene's default SortingMap");
				if (KickStarter.sceneSettings.sortingMap == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						SortingMap newSortingMap = AddPrefab ("Navigation", "SortingMap", true, false, true).GetComponent <SortingMap>();
						newSortingMap.gameObject.name = "Default SortingMap";
						KickStarter.sceneSettings.sortingMap = newSortingMap;
						EditorGUIUtility.PingObject (newSortingMap.gameObject);
					}
				}
				EditorGUILayout.EndHorizontal ();
				if (SceneSettings.IsUnity2D ())
				{
					EditorGUILayout.BeginHorizontal ();
					KickStarter.sceneSettings.tintMap = (TintMap) CustomGUILayout.ObjectField <TintMap> ("Default Tint map:", KickStarter.sceneSettings.tintMap, true, "AC.KickStarter.sceneSettings.tintMap", "The scene's default TintMap");
					if (KickStarter.sceneSettings.tintMap == null)
					{
						if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
						{
							TintMap newTintMap = AddPrefab ("Camera", "TintMap", true, false, true).GetComponent <TintMap>();
							PutInFolder (newTintMap.gameObject, "_SetGeometry");
							newTintMap.gameObject.name = "Default TintMap";
							KickStarter.sceneSettings.tintMap = newTintMap;
							EditorGUIUtility.PingObject (newTintMap.gameObject);
						}
					}
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.BeginHorizontal ();
				KickStarter.sceneSettings.defaultSound = (Sound) CustomGUILayout.ObjectField <Sound> ("Default Sound:", KickStarter.sceneSettings.defaultSound, true, "AC.KickStarter.sceneSettings.defaultSound", "The scene's default Sound. This is used to play Menu audio");
				if (KickStarter.sceneSettings.defaultSound == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Sound newSound = AddPrefab ("Logic", "Sound", true, false, true).GetComponent <Sound>();
						newSound.gameObject.name = "Default Sound";
						KickStarter.sceneSettings.defaultSound = newSound;
						newSound.playWhilePaused = true;
						EditorGUIUtility.PingObject (newSound.gameObject);
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (SceneSettings.IsTopDown () || SceneSettings.IsUnity2D ())
				{
					KickStarter.sceneSettings.overrideVerticalReductionFactor = EditorGUILayout.BeginToggleGroup (new GUIContent ("Override vertical movement factor?", "If True, then the 'Vertical movement factor' in the Settings Manager will be overridden with a scene-specific value"), KickStarter.sceneSettings.overrideVerticalReductionFactor);
					KickStarter.sceneSettings.verticalReductionFactor = EditorGUILayout.Slider (new GUIContent ("Vertical movement factor:", "How much slower vertical movement is compared to horizontal movement"), KickStarter.sceneSettings.verticalReductionFactor, 0.1f, 1f);
					EditorGUILayout.EndToggleGroup ();
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void CutscenesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCutscenes = CustomGUILayout.ToggleHeader (showCutscenes, "Scene cutscenes");
			if (showCutscenes)
			{
				KickStarter.sceneSettings.actionListSource = (ActionListSource) CustomGUILayout.EnumPopup ("ActionList source:", KickStarter.sceneSettings.actionListSource, "AC.KickStarter.sceneSettings.actionListSource", "The source of Actions used for the scene's main cutscenes");

				EditorGUILayout.BeginHorizontal ();
				if (KickStarter.sceneSettings.actionListSource == ActionListSource.InScene)
				{
					KickStarter.sceneSettings.cutsceneOnStart = (Cutscene) CustomGUILayout.ObjectField <Cutscene> ("On start:", KickStarter.sceneSettings.cutsceneOnStart, true, "AC.KickStarter.sceneSettings.cutsceneOnStart", "The Cutscene to run whenever the game beings from this scene, or when this scene is visited during gameplay");
					if (KickStarter.sceneSettings.cutsceneOnStart == null)
					{
						if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
						{
							Cutscene newCutscene = AddPrefab ("Logic", "Cutscene", true, false, true).GetComponent <Cutscene>();
							newCutscene.gameObject.name = "OnStart";
							KickStarter.sceneSettings.cutsceneOnStart = newCutscene;
							EditorGUIUtility.PingObject (newCutscene.gameObject);
						}
					}
				}
				else if (KickStarter.sceneSettings.actionListSource == ActionListSource.AssetFile)
				{
					KickStarter.sceneSettings.actionListAssetOnStart = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("On start:", KickStarter.sceneSettings.actionListAssetOnStart, true, "AC.KickStarter.sceneSettings.actionListAssetOnStart", "The ActionList asset to run whenever the game beings from this scene, or when this scene is visited during gameplay");
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (KickStarter.sceneSettings.actionListSource == ActionListSource.InScene)
				{
					KickStarter.sceneSettings.cutsceneOnLoad = (Cutscene) CustomGUILayout.ObjectField <Cutscene> ("On load:", KickStarter.sceneSettings.cutsceneOnLoad, true, "AC.KickStarter.sceneSettings.cutsceneOnLoad", "The Cutscene to run whenever this scene is loaded after restoring a saved game file");
					if (KickStarter.sceneSettings.cutsceneOnLoad == null)
					{
						if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
						{
							Cutscene newCutscene = AddPrefab ("Logic", "Cutscene", true, false, true).GetComponent <Cutscene>();
							newCutscene.gameObject.name = "OnLoad";
							KickStarter.sceneSettings.cutsceneOnLoad = newCutscene;
							EditorGUIUtility.PingObject (newCutscene.gameObject);
						}
					}
				}
				else if (KickStarter.sceneSettings.actionListSource == ActionListSource.AssetFile)
				{
					KickStarter.sceneSettings.actionListAssetOnLoad = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("On load:", KickStarter.sceneSettings.actionListAssetOnLoad, true, "AC.KickStarter.sceneSettings.actionListAssetOnLoad", "The ActionList asset to run whenever this scene is loaded after restoring a saved game file");
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (KickStarter.sceneSettings.actionListSource == ActionListSource.InScene)
				{
					KickStarter.sceneSettings.cutsceneOnVarChange = (Cutscene) CustomGUILayout.ObjectField <Cutscene> ("On variable change:", KickStarter.sceneSettings.cutsceneOnVarChange, true, "AC.KickStarter.sceneSettings.cutsceneOnVarChange", "The Cutscene to run whenever a variable's value is changed");
					if (KickStarter.sceneSettings.cutsceneOnVarChange == null)
					{
						if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
						{
							Cutscene newCutscene = AddPrefab ("Logic", "Cutscene", true, false, true).GetComponent <Cutscene>();
							newCutscene.gameObject.name = "OnVarChange";
							KickStarter.sceneSettings.cutsceneOnVarChange = newCutscene;
							EditorGUIUtility.PingObject (newCutscene.gameObject);
						}
					}
				}
				else if (KickStarter.sceneSettings.actionListSource == ActionListSource.AssetFile)
				{
					KickStarter.sceneSettings.actionListAssetOnVarChange = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("On variable change:", KickStarter.sceneSettings.actionListAssetOnVarChange, true, "AC.KickStarter.sceneSettings.actionListAssetOnVarChange", "The ActionList asset to run whenever a variable's value is changed");
				}
				EditorGUILayout.EndHorizontal ();
			}
			CustomGUILayout.EndVertical ();
		}

		
		private void VisibilityGUI (Rect windowRect)
		{
			if (KickStarter.sceneSettings == null) return;

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showVisibility = CustomGUILayout.ToggleHeader (showVisibility, "Visibility");
			if (showVisibility)
			{
				GUILayoutOption[] options = new GUILayoutOption[2] { GUILayout.Width (((windowRect.width - 10f) / 3f) - 11f), GUILayout.Height (20f) };
				EditorGUI.BeginChangeCheck ();

				EditorGUILayout.BeginHorizontal ();
				if (hotspotIcon == null) hotspotIcon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/Hotspot.png", typeof (Texture2D));
				KickStarter.sceneSettings.visibilityHotspots = GUILayout.Toggle (KickStarter.sceneSettings.visibilityHotspots, (hotspotIcon) ? new GUIContent (" Hotspots", hotspotIcon) : new GUIContent ("Hotspots"), options);

				if (triggerIcon == null) triggerIcon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/AC_Trigger.png", typeof (Texture2D));
				KickStarter.sceneSettings.visibilityTriggers = GUILayout.Toggle (KickStarter.sceneSettings.visibilityTriggers, (triggerIcon) ? new GUIContent (" Triggers", triggerIcon) : new GUIContent ("Triggers"), options);

				if (collisionIcon == null) collisionIcon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/_Collision.png", typeof (Texture2D));
				KickStarter.sceneSettings.visibilityCollision = GUILayout.Toggle (KickStarter.sceneSettings.visibilityCollision, (collisionIcon) ? new GUIContent (" Collision", collisionIcon) : new GUIContent ("Collision"), options);
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				if (markerIcon == null) markerIcon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/Marker.png", typeof (Texture2D));
				KickStarter.sceneSettings.visibilityMarkers = GUILayout.Toggle (KickStarter.sceneSettings.visibilityMarkers, (markerIcon) ? new GUIContent (" Markers", markerIcon) : new GUIContent ("Markers"), options);

				if (playerStartIcon == null) playerStartIcon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/PlayerStart.png", typeof (Texture2D));
				KickStarter.sceneSettings.visibilityPlayerStarts = GUILayout.Toggle (KickStarter.sceneSettings.visibilityPlayerStarts, (playerStartIcon) ? new GUIContent (" PlayerStarts", playerStartIcon) : new GUIContent ("PlayerStarts"), options);

				if (navMeshIcon == null) navMeshIcon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/NavigationMesh.png", typeof (Texture2D));
				KickStarter.sceneSettings.visibilityNavMesh = GUILayout.Toggle (KickStarter.sceneSettings.visibilityNavMesh, (navMeshIcon) ? new GUIContent (" NavMesh", navMeshIcon) : new GUIContent ("NavMesh"), options);
				EditorGUILayout.EndHorizontal ();

				if (EditorGUI.EndChangeCheck ())
				{		
					SceneView.RepaintAll ();
					Marker[] markers = FindObjectsOfType<Marker>();
					foreach (Marker marker in markers)
					{
						marker.DrawGizmos ();
					}
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void SceneAttributesGUI ()
		{
			if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager is required to view and edit scene attributes.", MessageType.Info);
				return;
			}

			if (KickStarter.sceneSettings == null)
			{
				return;
			}

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showAttributes = CustomGUILayout.ToggleHeader (showAttributes, "Scene attributes");
			if (showAttributes)
			{
				if (AdvGame.GetReferences ().settingsManager == null)
				{
					EditorGUILayout.HelpBox ("A Settings Manager must be assigned in order to make use of scene attributes.", MessageType.Warning);
				}
				else
				{
					InvVar[] sceneAttributes = AdvGame.GetReferences ().settingsManager.sceneAttributes.ToArray ();
					if (AdvGame.GetReferences ().settingsManager.sceneAttributes.Count > 0)
					{
						RebuildProperties (sceneAttributes);

						// UI for setting property values
						if (KickStarter.sceneSettings.attributes.Count > 0)
						{
							foreach (InvVar attribute in KickStarter.sceneSettings.attributes)
							{
								string apiPrefix = "AC.KickStarter.sceneSettings.GetAttribute (" + attribute.id + ")";
								attribute.ShowGUI (apiPrefix, true);
							}

						}
						else
						{
							EditorGUILayout.HelpBox ("No properties have been defined that this inventory item can use.", MessageType.Info);
						}
					}

					if (GUILayout.Button ("Manage attributes"))
					{
						SceneAttributesWindow.Init ();
					}
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void RebuildProperties (InvVar[] invVars)
		{
			// Which properties are available?
			List<int> availableVarIDs = new List<int>();
			foreach (InvVar invVar in invVars)
			{
				availableVarIDs.Add (invVar.id);
			}
			
			// Create new properties / transfer existing values
			List<InvVar> newInvVars = new List<InvVar>();
			foreach (InvVar invVar in invVars)
			{
				if (availableVarIDs.Contains (invVar.id))
				{
					InvVar newInvVar = new InvVar (invVar);
					InvVar oldInvVar = KickStarter.sceneSettings.GetAttribute (invVar.id);
					if (oldInvVar != null)
					{
						newInvVar.TransferValues (oldInvVar);
					}
					newInvVars.Add (newInvVar);
				}
			}
			
			KickStarter.sceneSettings.attributes = newInvVars;
		}


		private void PrefabButton (string subFolder, string prefabName)
		{
			if (GUILayout.Button (prefabName))
			{
				AddPrefab (subFolder, prefabName, true, true, true);
			}	
		}
		
		
		private void PrefabButton (string subFolder, string prefabName, Texture icon)
		{
			if (GUILayout.Button (icon))
			{
				AddPrefab (subFolder, prefabName, true, true, true);
			}	
		}
		

		/**
		 * <summary>Makes the current scene AC-ready, by setting up the MainCamera, instantiating the GameEngine prefab, and optionally creating "folder" objects.</summary>
		 * <param name = "createFolders">If True, then empty GameObjects that acts as folders, to aid organisation, will be created</param>
		 */
		public void InitialiseObjects (bool createFolders = true)
		{
			bool reallyDoOverrideCameraPerspective = (KickStarter.sceneSettings == null && tempOverrideCameraPerspective) ? true : false;

			if (createFolders)
			{
				CreateFolder ("_Cameras");
				CreateFolder ("_Cutscenes");
				CreateFolder ("_DialogueOptions");
				CreateFolder ("_Interactions");
				CreateFolder ("_Lights");
				CreateFolder ("_Logic");
				CreateFolder ("_Moveables");
				CreateFolder ("_Navigation");
				CreateFolder ("_NPCs");
				CreateFolder ("_Sounds");
				CreateFolder ("_SetGeometry");
				CreateFolder ("_UI");
				
				// Create subfolders
				CreateSubFolder ("_Cameras", "_GameCameras");
				
				CreateSubFolder ("_Logic", "_ArrowPrompts");
				CreateSubFolder ("_Logic", "_Conversations");
				CreateSubFolder ("_Logic", "_Containers");
				CreateSubFolder ("_Logic", "_Hotspots");
				CreateSubFolder ("_Logic", "_Triggers");
				CreateSubFolder ("_Logic", "_Variables");
				
				CreateSubFolder ("_Moveables", "_Tracks");
				
				CreateSubFolder ("_Navigation", "_CollisionCubes");
				CreateSubFolder ("_Navigation", "_CollisionCylinders");
				CreateSubFolder ("_Navigation", "_Markers");
				CreateSubFolder ("_Navigation", "_NavMeshSegments");
				CreateSubFolder ("_Navigation", "_NavMesh");
				CreateSubFolder ("_Navigation", "_Paths");
				CreateSubFolder ("_Navigation", "_PlayerStarts");
				CreateSubFolder ("_Navigation", "_SortingMaps");
			}

			// Tag the SampleScene's MainCamera as such so that it can be replaced
			GameObject defaultCamera = GameObject.Find ("Main Camera");
			if (defaultCamera != null && UnityVersionHandler.ObjectIsInActiveScene (defaultCamera) && defaultCamera.GetComponent <MainCamera>() == null && defaultCamera.tag == Tags.untagged)
			{
				defaultCamera.tag = Tags.mainCamera;
			}
						
			// Delete default main camera
			GameObject[] mainCameras = GameObject.FindGameObjectsWithTag (Tags.mainCamera);
			foreach (GameObject oldMainCam in mainCameras)
			{
				if (UnityVersionHandler.ObjectIsInActiveScene (oldMainCam) && oldMainCam.GetComponent <MainCamera>() == null)
				{
					if (oldMainCam.GetComponent <Camera>())
					{
						string camName = oldMainCam.name;

						bool replaceCamera = true;

						if (camName != "Main Camera" || oldMainCam.transform.parent != null)
						{
							replaceCamera = EditorUtility.DisplayDialog ("MainCamera detected", "AC has detected the scene object '" + camName + "', which is tagged as 'MainCamera'." +
						                                             "\n\n" +
						                                             "AC requires that the scene's MainCamera also has the AC.MainCamera script attached.  Should it convert '" + camName + "', or untag it and create a separate MainCamera for AC?" +
						                                             "\n\n" +
						                                             "(Note: If '" + camName + "' is part of a Player prefab, it is recommended to simply untag it.)", "Convert it", "Untag it");
						}

						if (replaceCamera)
						{
							oldMainCam.AddComponent <MainCamera>();
							
							string camPrefabfileName = assetFolder + "Automatic" + Path.DirectorySeparatorChar.ToString () + "MainCamera.prefab";
							GameObject camPrefab = (GameObject) AssetDatabase.LoadAssetAtPath (camPrefabfileName, typeof (GameObject));
							if (camPrefab)
							{
								Texture2D prefabFadeTexture = camPrefab.GetComponent <MainCamera>().GetFadeTexture ();
								oldMainCam.GetComponent <MainCamera>().SetDefaultFadeTexture (prefabFadeTexture);
							}
							ACDebug.Log ("'" + oldMainCam.name + "' has been converted to an Adventure Creator MainCamera.");
						}
						else
						{
							ACDebug.Log ("Untagged MainCamera '" + oldMainCam.name + "'.", oldMainCam);
							oldMainCam.tag = Tags.untagged;
							oldMainCam.GetComponent <Camera>().enabled = false;
							if (camName == "Main Camera") oldMainCam.gameObject.name += " (Untagged)";
						}
					}
					else
					{
						ACDebug.Log ("Untagged MainCamera '" + oldMainCam.name + "', as it had no Camera component.", oldMainCam);
						oldMainCam.tag = Tags.untagged;
					}
				}
			}

			bool foundMainCamera = false;
			foreach (GameObject oldMainCam in mainCameras)
			{
				if (UnityVersionHandler.ObjectIsInActiveScene (oldMainCam) && oldMainCam.GetComponent <MainCamera>() != null)
				{
					foundMainCamera = true;
				}
			}
			
			// Create Game engine
			GameObject gameEngineOb = AddPrefab ("Automatic", "GameEngine", false, false, false);
			if (gameEngineOb == null && !UnityVersionHandler.ObjectIsInActiveScene ("GameEngine"))
			{
				gameEngineOb = new GameObject ("GameEngine");
				gameEngineOb.AddComponent<MenuSystem> ();
				gameEngineOb.AddComponent<Dialog> ();
				gameEngineOb.AddComponent<PlayerInput> ();
				gameEngineOb.AddComponent<PlayerInteraction> ();
				gameEngineOb.AddComponent<PlayerMovement> ();
				gameEngineOb.AddComponent<PlayerCursor> ();
				gameEngineOb.AddComponent<PlayerQTE> ();
				gameEngineOb.AddComponent<SceneSettings> ();
				gameEngineOb.AddComponent<NavigationManager> ();
				gameEngineOb.AddComponent<ActionListManager> ();
				gameEngineOb.AddComponent<LocalVariables> ();
				gameEngineOb.AddComponent<MenuPreview> ();
				gameEngineOb.AddComponent<EventManager> ();
				gameEngineOb.AddComponent<KickStarter> ();
			}

			// Camera perspective override
			if (reallyDoOverrideCameraPerspective)
			{
				KickStarter.sceneSettings.SetOverrideCameraPerspective (tempCameraPerspective, tempMovingTurning);
			}

			// Create main camera if none exists
			if (!foundMainCamera)
			{
				GameObject mainCamOb = AddPrefab ("Automatic", "MainCamera", false, false, false);
				#if NEW_PREFABS
				try
				{
					PrefabUtility.UnpackPrefabInstance (mainCamOb, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				}
				catch {}
				#else
				PrefabUtility.DisconnectPrefabInstance (mainCamOb);
				#endif
				PutInFolder (mainCamOb, "_Cameras");
				if (SceneSettings.IsUnity2D ())
				{
					KickStarter.CameraMain.orthographic = true;
				}
			}
			
			// Create Background Camera (if 2.5D)
			if (SceneSettings.CameraPerspective == CameraPerspective.TwoPointFiveD)
			{
				CreateSubFolder ("_SetGeometry", "_BackgroundImages");
				GameObject newOb = AddPrefab ("Automatic", "BackgroundCamera", false, false, false);
				PutInFolder (newOb, "_Cameras");
			}
			
			// Assign Player Start
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.defaultPlayerStart == null)
			{
				string playerStartPrefab = "PlayerStart";
				if (SceneSettings.IsUnity2D ())
				{
					playerStartPrefab += "2D";
				}

				GameObject playerStartOb = AddPrefab ("Navigation", playerStartPrefab, true, false, true);
				if (playerStartOb)
				{
					PlayerStart playerStart = playerStartOb.GetComponent<PlayerStart> ();
					KickStarter.sceneSettings.defaultPlayerStart = playerStart;
				}
			}
			
			// Pathfinding method
			if (SceneSettings.IsUnity2D ())
			{
				KickStarter.sceneSettings.navigationMethod = AC_NavigationMethod.PolygonCollider;
				KickStarter.navigationManager.ResetEngine ();
			}

			tempOverrideCameraPerspective = false;
		}
		

		private static void RenameObject (GameObject ob, string resourceName)
		{
			int slash = resourceName.IndexOf ("/");
			string newName;

			if (slash > 0)
			{
				newName = resourceName.Remove (0, slash + 1);
			}
			else
			{
				newName = resourceName;
			}

			ob.name = newName;
		}


		/**
		 * <summary>Adds an Adventure Creator prefab to the scene.</summary>
		 * <param name = "folderName">The name of the subfolder that the prefab lives in, within /Assets/AdventureCreator/Prefabs</param>
		 * <param name = "prefabName">The name of the prefab filename, without the '.asset' extension</param>
		 * <param name = "canCreateMultiple">If True, then multiple instances of the prefab can exuist within the scene</param>
		 * <param name = "selectAfter">If True, the created GameObject will be selected in the Hierarchy window</param>
		 * <param name = "putInFolder">If True, then the Scene Manager will attempt to place the created GameObject in an appropriate "folder" object.</param>
		 * <returns>The created prefab GameObject</param>
		 */
		public static GameObject AddPrefab (string folderName, string prefabName, bool canCreateMultiple, bool selectAfter, bool putInFolder)
		{
			if (canCreateMultiple || !UnityVersionHandler.ObjectIsInActiveScene (prefabName))
			{
				string fileName = assetFolder + folderName + Path.DirectorySeparatorChar.ToString () + prefabName + ".prefab";
				
				GameObject newOb = (GameObject) PrefabUtility.InstantiatePrefab (AssetDatabase.LoadAssetAtPath (fileName, typeof (GameObject)));
				if (newOb == null)
				{
					ACDebug.LogWarning ("Error creating new object '" + prefabName + "' from filepath '" + fileName + "'.");
					return null;
				}
				newOb.name = "Temp";
				
				if (folderName != "" && putInFolder)
				{
					if (!PutInFolder (newOb, "_" + prefabName + "s"))
					{
						string newName = "_" + prefabName;
						
						if (newName.Contains ("2D"))
						{
							newName = newName.Substring (0, newName.IndexOf ("2D"));
							
							if (!PutInFolder (newOb, newName + "s"))
							{
								PutInFolder (newOb, newName);
							}
							else
							{
								PutInFolder (newOb, newName);
							}
						}
						else if (newName.Contains ("2.5D"))
						{
							newName = newName.Substring (0, newName.IndexOf ("2.5D"));
							
							if (!PutInFolder (newOb, newName + "s"))
							{
								PutInFolder (newOb, newName);
							}
							else
							{
								PutInFolder (newOb, newName);
							}
						}
						else if (newName.Contains ("Animated"))
						{
							newName = newName.Substring (0, newName.IndexOf ("Animated"));
							
							if (!PutInFolder (newOb, newName + "s"))
							{
								PutInFolder (newOb, newName);
							}
							else
							{
								PutInFolder (newOb, newName);
							}
						}
						else if (newName.Contains ("ThirdPerson"))
						{
							newName = newName.Substring (0, newName.IndexOf ("ThirdPerson"));
							
							if (!PutInFolder (newOb, newName + "s"))
							{
								PutInFolder (newOb, newName);
							}
							else
							{
								PutInFolder (newOb, newName);
							}
						}
						else
						{
							PutInFolder (newOb, newName);
						}
					}
				}
				
				if (newOb.GetComponent <GameCamera2D>())
				{
					newOb.GetComponent <GameCamera2D>().SetCorrectRotation ();
				}

				RenameObject (newOb, prefabName);

				RegisterNewObject (newOb);

				// Select the object
				if (selectAfter)
				{
					Selection.activeObject = newOb;
				}
				
				return newOb;
			}
			
			return null;
		}
		
		
		private static bool PutInFolder (GameObject ob, string folderName)
		{
			return UnityVersionHandler.PutInFolder (ob, folderName);
		}
		
		
		private void CreateFolder (string folderName)
		{
			if (!UnityVersionHandler.ObjectIsInActiveScene (folderName))
			{
				GameObject newFolder = new GameObject();
				newFolder.name = folderName;
				Undo.RegisterCreatedObjectUndo (newFolder, "Created " + newFolder.name);
			}
		}
		
		
		private void CreateSubFolder (string baseFolderName, string subFolderName)
		{
			CreateFolder (baseFolderName);
			
			if (!UnityVersionHandler.ObjectIsInActiveScene (subFolderName))
			{
				GameObject newFolder = new GameObject ();
				newFolder.name = subFolderName;
				Undo.RegisterCreatedObjectUndo (newFolder, "Created " + newFolder.name);

				if (!PutInFolder (newFolder, baseFolderName))
				{
					ACDebug.Log ("Folder " + baseFolderName + " does not exist!");
				}
			}
		}
		
		
		private ScenePrefab GetActiveScenePrefab ()
		{
			if (scenePrefabs == null || scenePrefabs.Count <= activeScenePrefab)
			{
				DeclareScenePrefabs ();
			}
			if (scenePrefabs.Count <= activeScenePrefab)
			{
				activeScenePrefab = 0;
			}
			
			if (scenePrefabs.Count > 0)
			{
				return scenePrefabs[activeScenePrefab];
			}
			return null;
		}
		
		
		private void ListPrefabs (Rect windowRect)
		{
			if (scenePrefabs == null || GUI.changed)
			{
				DeclareScenePrefabs ();
				GetPrefabsInScene ();
			}

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showPrefabs = CustomGUILayout.ToggleHeader (showPrefabs, "Scene prefabs");
			if (showPrefabs)
			{
				#if NEW_PREFABS
				GameObject prefabRoot = UnityVersionHandler.GetPrefabStageRoot ();
				if (prefabRoot)
				{
					EditorGUILayout.HelpBox ("Currently editing prefab " + prefabRoot.name + " - new objects added here will be added to this prefab.", MessageType.Info);
				}
				#endif

				GUILayout.BeginHorizontal ();
				GUIContent prefabHeader = new GUIContent ("  " + GetActiveScenePrefab ().subCategory, GetActiveScenePrefab ().icon);
				EditorGUILayout.LabelField (prefabHeader, EditorStyles.boldLabel, GUILayout.Height (40f));

				if (windowRect.width > 300f)
				{
					EditorGUILayout.HelpBox (GetActiveScenePrefab ().helpText, MessageType.Info);
				}
				GUILayout.EndHorizontal ();
				
				EditorGUILayout.Space ();
				
				GUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("New object name:", GUILayout.Width (120f));
				newPrefabName = EditorGUILayout.TextField (newPrefabName);
				
				if (GUILayout.Button ("Add new", GUILayout.Width (60f)))
				{
					string fileName = assetFolder + GetActiveScenePrefab ().prefabPath + ".prefab";
					GameObject newOb = (GameObject) PrefabUtility.InstantiatePrefab (AssetDatabase.LoadAssetAtPath (fileName, typeof (GameObject)));
					
					if (newPrefabName != null && newPrefabName != "" && newPrefabName.Length > 0)
					{
						newOb.name = newPrefabName;
						newPrefabName = string.Empty;
					}

					RegisterNewObject (newOb, true);
					PutInFolder (newOb, GetActiveScenePrefab ().sceneFolder);
					
					if (CanWrapHotspot () && positionHotspotOverMesh)
					{
						positionHotspotOverMesh = false;
						
						Renderer r = Selection.activeGameObject.GetComponent <Renderer>();
						newOb.transform.position = r.bounds.center;
						Vector3 scale = r.bounds.size;
						scale.x = Mathf.Max (scale.x, 0.01f);
						scale.y = Mathf.Max (scale.y, 0.01f);
						scale.z = Mathf.Max (scale.z, 0.01f);
						newOb.transform.localScale = scale;
					}

					Selection.activeGameObject = newOb;
					GetPrefabsInScene ();
				}

				GUILayout.EndHorizontal ();

				if (CanWrapHotspot ())
				{
					string positionLabel = "Position over selected " + ((Selection.activeGameObject.GetComponent <SpriteRenderer>() != null) ? "sprite" : "mesh") + "?";
					positionHotspotOverMesh = EditorGUILayout.ToggleLeft (positionLabel, positionHotspotOverMesh);
				}
				
				EditorGUILayout.Space ();
				
				if (GUI.changed || prefabTextArray == null)
				{
					GetPrefabsInScene ();
				}

				if (prefabTextArray.Length > 0)
				{
					EditorGUILayout.Space ();
					EditorGUILayout.LabelField ("Existing " + GetActiveScenePrefab ().subCategory + " objects:");
					EditorGUILayout.BeginHorizontal ();
					selectedSceneObject = EditorGUILayout.Popup (selectedSceneObject, prefabTextArray);
				
					if (GUILayout.Button ("Select", EditorStyles.miniButtonLeft))
					{
						if (Type.GetType ("AC." + GetActiveScenePrefab ().componentName) != null)
						{
							MonoBehaviour[] objects = FindObjectsOfType (Type.GetType ("AC." + GetActiveScenePrefab ().componentName)) as MonoBehaviour [];
							if (objects != null && objects.Length > selectedSceneObject && objects[selectedSceneObject].gameObject != null)
							{
								Selection.activeGameObject = objects[selectedSceneObject].gameObject;
							}
						}
						else if (!string.IsNullOrEmpty (GetActiveScenePrefab ().componentName))
						{
							MonoBehaviour[] objects = FindObjectsOfType (Type.GetType (GetActiveScenePrefab ().componentName)) as MonoBehaviour [];
							if (objects != null && objects.Length > selectedSceneObject && objects[selectedSceneObject].gameObject != null)
							{
								Selection.activeGameObject = objects[selectedSceneObject].gameObject;
							}
						}
					
					}
					if (GUILayout.Button ("Refresh", EditorStyles.miniButtonRight))
					{
						GetPrefabsInScene ();
					}

					EditorGUILayout.EndHorizontal ();
				}

				ListAllPrefabs ("Camera", windowRect);
				ListAllPrefabs ("Logic", windowRect);
				ListAllPrefabs ("Moveable", windowRect);
				ListAllPrefabs ("Navigation", windowRect);
			}
			CustomGUILayout.EndVertical ();
		}
		
		
		private void ListAllPrefabs (string _category, Rect windowRect)
		{
			EditorGUILayout.Space ();

			GUISkin testSkin = (GUISkin) Resources.Load ("SceneManagerSkin");
			GUI.skin = testSkin;
			bool isEven = false;
			
			EditorGUILayout.LabelField (_category,  CustomStyles.smallCentre);
			
			EditorGUILayout.BeginHorizontal ();
			
			float buttonWidth = windowRect.width / 2f - 17f;

			foreach (ScenePrefab prefab in scenePrefabs)
			{
				if (prefab.category == _category)
				{
					isEven = !isEven;

					if (prefab.icon && buttonWidth > 65f)
					{
						if (GUILayout.Button (new GUIContent (" " + prefab.subCategory, prefab.icon), GUILayout.Width (buttonWidth)))
						{
							GUI.skin = null;
							ClickPrefabButton (prefab);
							GUI.skin = testSkin;
						}
					}
					else if (prefab.icon)
					{
						if (GUILayout.Button (new GUIContent (prefab.icon), GUILayout.Width (buttonWidth)))
						{
							GUI.skin = null;
							ClickPrefabButton (prefab);
							GUI.skin = testSkin;
						}
					}
					else
					{
						if (GUILayout.Button (new GUIContent (" " + prefab.subCategory)))
						{
							GUI.skin = null;
							ClickPrefabButton (prefab);
							GUI.skin = testSkin;
						}
					}
					
					if (!isEven)
					{
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
					}
					else
					{
					//	GUILayout.FlexibleSpace ();
					}
				}
			}
			
			EditorGUILayout.EndHorizontal ();
			//GUILayout.EndVertical ();
			
			GUI.skin = null;
		}


		private static void RegisterNewObject (GameObject newOb, bool canAddToPrefab = false)
		{
			#if NEW_PREFABS
			PrefabUtility.UnpackPrefabInstance (newOb, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
			#endif
			Undo.RegisterCreatedObjectUndo (newOb, "Created " + newOb.name);

			if (canAddToPrefab)
			{
				GameObject prefabRoot = UnityVersionHandler.GetPrefabStageRoot ();
				if (prefabRoot)
				{
					newOb.transform.SetParent (prefabRoot.transform);
				}
			}
		}
		
		
		private void ClickPrefabButton (ScenePrefab _prefab)
		{
			if (activeScenePrefab == scenePrefabs.IndexOf (_prefab))
			{
				// Clicked twice, add new
				string fileName = assetFolder + _prefab.prefabPath + ".prefab";
				GameObject newOb = (GameObject) PrefabUtility.InstantiatePrefab (AssetDatabase.LoadAssetAtPath (fileName, typeof (GameObject)));

				RegisterNewObject (newOb, true);
				
				if (newOb.GetComponent <GameCamera2D>())
				{
					newOb.GetComponent <GameCamera2D>().SetCorrectRotation ();
				}
				
				PutInFolder (newOb, _prefab.sceneFolder);
				EditorGUIUtility.PingObject (newOb);
			}
			
			activeScenePrefab = scenePrefabs.IndexOf (_prefab);
			GetPrefabsInScene ();
		}
		
		
		private bool CanWrapHotspot ()
		{
			if (Selection.activeGameObject != null && GetActiveScenePrefab ().subCategory.Contains ("Hotspot") && Selection.activeGameObject.GetComponent <Renderer>())
			{
				return true;
			}
			return false;
		}
		
		
		private void DeclareScenePrefabs ()
		{
			scenePrefabs = new List<ScenePrefab>();

			if (SceneSettings.CameraPerspective == CameraPerspective.ThreeD)
			{
				scenePrefabs.Add (new ScenePrefab ("Camera", "GameCamera", "Camera/GameCamera", "_GameCameras", "The standard camera type for 3D games.", "GameCamera"));
				scenePrefabs.Add (new ScenePrefab ("Camera", "GameCamera Animated", "Camera/GameCameraAnimated", "_GameCameras", "Plays an Animation Clip when active, or syncs it with its target's position.", "GameCameraAnimated"));
				scenePrefabs.Add (new ScenePrefab ("Camera", "GameCamera Third-person", "Camera/GameCameraThirdPerson", "_GameCameras", "Rigidly follows its target, but can still be rotated.", "GameCameraThirdPerson"));
				scenePrefabs.Add (new ScenePrefab ("Camera", "SimpleCamera", "Camera/SimpleCamera", "_GameCameras", "A stationary but lightweight 3D camera.", "_Camera"));
			}
			else
			{
				if (SceneSettings.CameraPerspective == CameraPerspective.TwoD)
				{
					scenePrefabs.Add (new ScenePrefab ("Camera", "GameCamera 2D", "Camera/GameCamera2D", "_GameCameras", "The standard camera type for 2D games.", "GameCamera2D"));

					if (SceneSettings.IsUnity2D ())
					{
						scenePrefabs.Add (new ScenePrefab ("Camera", "GameCamera 2D Drag", "Camera/GameCamera2DDrag", "_GameCameras", "A 2D camera that can be panned by dragging the mouse/touch.", "GameCamera2D"));
						scenePrefabs.Add (new ScenePrefab ("Camera", "TintMap", "Camera/TintMap", "_SetGeometry", "A texture used to tint 2D sprites.", "TintMap"));
					}
				}
				else
				{
					scenePrefabs.Add (new ScenePrefab ("Camera", "GameCamera 2.5D", "Camera/GameCamera2.5D", "_GameCameras", "A stationary camera that can display images in the background.", "GameCamera25D"));
					scenePrefabs.Add (new ScenePrefab ("Camera", "Background Image", "SetGeometry/BackgroundImage", "_BackgroundImages", "A container for a 2.5D camera's background image.", "BackgroundImage"));
					scenePrefabs.Add (new ScenePrefab ("Camera", "Scene sprite", "SetGeometry/SceneSprite", "_SetGeometry", "An in-scene sprite for 2.5D games.", "", "SceneSprite"));
				}
			}
			
			scenePrefabs.Add (new ScenePrefab ("Logic", "Arrow prompt", "Logic/ArrowPrompt", "_ArrowPrompts", "An on-screen directional prompt for the player.", "ArrowPrompt"));
			scenePrefabs.Add (new ScenePrefab ("Logic", "Conversation", "Logic/Conversation", "_Conversations", "Stores a list of Dialogue Options, from which the player can choose.", "Conversation"));
			scenePrefabs.Add (new ScenePrefab ("Logic", "Container", "Logic/Container", "_Containers", "Can store a list of Inventory Items, for the player to retrieve and add to.", "Container"));
			scenePrefabs.Add (new ScenePrefab ("Logic", "Cutscene", "Logic/Cutscene", "_Cutscenes", "A sequence of Actions that can form a cinematic.", "Cutscene"));
			scenePrefabs.Add (new ScenePrefab ("Logic", "Dialogue Option", "Logic/DialogueOption", "_DialogueOptions", "An option available to the player when a Conversation is active.", "DialogueOption"));
			
			if (SceneSettings.IsUnity2D ())
			{
				scenePrefabs.Add (new ScenePrefab ("Logic", "Hotspot 2D", "Logic/Hotspot2D", "_Hotspots", "A portion of the scene that can be interacted with.", "Hotspot"));
			}
			else
			{
				scenePrefabs.Add (new ScenePrefab ("Logic", "Hotspot", "Logic/Hotspot", "_Hotspots", "A portion of the scene that can be interacted with.", "Hotspot"));
			}
			
			scenePrefabs.Add (new ScenePrefab ("Logic", "Interaction", "Logic/Interaction", "_Interactions", "A sequence of Actions that run when a Hotspot is activated.", "Interaction"));

			if (SceneSettings.IsUnity2D ())
			{
				scenePrefabs.Add (new ScenePrefab ("Logic", "InteractiveBoundary2D", "Logic/InteractiveBoundary2D", "_Hotspots", "An optional boundary that the player must be inside for a Hotspot to be interactive.", "InteractiveBoundary"));
			}
			else
			{
				scenePrefabs.Add (new ScenePrefab ("Logic", "Interactive Boundary", "Logic/InteractiveBoundary", "_Hotspots", "An optional boundary that the player must be inside for a Hotspot to be interactive.", "InteractiveBoundary"));
			}

			scenePrefabs.Add (new ScenePrefab ("Logic", "Sound", "Logic/Sound", "_Sounds", "An audio source that syncs with AC's sound levels.", "Sound"));
			
			if (SceneSettings.IsUnity2D ())
			{
				scenePrefabs.Add (new ScenePrefab ("Logic", "Trigger 2D", "Logic/Trigger2D", "_Triggers", "A portion of the scene that responds to objects entering it.", "AC_Trigger"));
			}
			else
			{
				scenePrefabs.Add (new ScenePrefab ("Logic", "Trigger", "Logic/Trigger", "_Triggers", "A portion of the scene that responds to objects entering it.", "AC_Trigger"));
			}

			scenePrefabs.Add (new ScenePrefab ("Logic", "Variables", "Logic/Variables", "_Variables", "A collection of Variables that can be used to handle logic processing.", "Variables"));
			
			scenePrefabs.Add (new ScenePrefab ("Moveable", "Draggable", "Moveable/Draggable", "_Moveables", "Can move along pre-defined Tracks, along planes, or be rotated about its centre.", "Moveable_Drag"));
			scenePrefabs.Add (new ScenePrefab ("Moveable", "PickUp", "Moveable/PickUp", "_Moveables", "Can be grabbed, rotated and thrown freely in 3D space.", "Moveable_PickUp"));
			scenePrefabs.Add (new ScenePrefab ("Moveable", "Straight Track", "Moveable/StraightTrack", "_Tracks", "Constrains a Drag object along a straight line, optionally adding rolling or screw effects.", "DragTrack_Straight"));
			scenePrefabs.Add (new ScenePrefab ("Moveable", "Curved Track", "Moveable/CurvedTrack", "_Tracks", "Constrains a Drag object along a circular line.", "DragTrack_Curved"));
			scenePrefabs.Add (new ScenePrefab ("Moveable", "Hinge Track", "Moveable/HingeTrack", "_Tracks", "Constrains a Drag object's position, only allowing it to rotate in a circular motion.", "DragTrack_Hinge"));
			
			scenePrefabs.Add (new ScenePrefab ("Navigation", "SortingMap", "Navigation/SortingMap", "_SortingMaps", "Defines how sprites are scaled and sorted relative to one another.", "SortingMap"));
			
			if (SceneSettings.IsUnity2D ())
			{
				scenePrefabs.Add (new ScenePrefab ("Navigation", "Collision Cube 2D", "Navigation/CollisionCube2D", "_CollisionCubes", "Blocks Character movement, as well as cursor clicks if placed on the Default layer.", "_Collision"));
				scenePrefabs.Add (new ScenePrefab ("Navigation", "Marker 2D", "Navigation/Marker2D", "_Markers", "A point in the scene used by Characters and objects.", "Marker"));
			}
			else
			{
				scenePrefabs.Add (new ScenePrefab ("Navigation", "Collision Cube", "Navigation/CollisionCube", "_CollisionCubes", "Blocks Character movement, as well as cursor clicks if placed on the Default layer.", "_Collision"));
				scenePrefabs.Add (new ScenePrefab ("Navigation", "Collision Cylinder", "Navigation/CollisionCylinder", "_CollisionCylinders", "Blocks Character movement, as well as cursor clicks if placed on the Default layer.", "_Collision"));
				scenePrefabs.Add (new ScenePrefab ("Navigation", "Marker", "Navigation/Marker", "_Markers", "A point in the scene used by Characters and objects.", "Marker"));
			}
			
			if (KickStarter.sceneSettings)
			{
				AC_NavigationMethod engine = KickStarter.sceneSettings.navigationMethod;
				if (engine == AC_NavigationMethod.meshCollider)
				{
					scenePrefabs.Add (new ScenePrefab ("Navigation", "NavMesh", "Navigation/NavMesh", "_NavMesh", "A mesh that defines the area that Characters can move in.", "NavigationMesh"));
				}
				else if (engine == AC_NavigationMethod.PolygonCollider)
				{
					scenePrefabs.Add (new ScenePrefab ("Navigation", "NavMesh 2D", "Navigation/NavMesh2D", "_NavMesh", "A polygon that defines the area that Characters can move in.", "NavigationMesh"));
				}
				else if (engine == AC_NavigationMethod.UnityNavigation)
				{
					scenePrefabs.Add (new ScenePrefab ("Navigation", "NavMesh segment", "Navigation/NavMeshSegment", "_NavMeshSegments", "A plane that defines a portion of the area that Characters can move in.", "NavMeshSegment"));
					scenePrefabs.Add (new ScenePrefab ("Navigation", "Static obstacle", "Navigation/StaticObstacle", "_NavMeshSegments", "A cube that defines a portion of the area that Characters cannot move in.", "", "StaticObstacle"));
				}
			}
			
			scenePrefabs.Add (new ScenePrefab ("Navigation", "Path", "Navigation/Path", "_Paths", "A sequence of points that describe a Character's movement.", "Paths"));
			
			if (SceneSettings.IsUnity2D ())
			{
				scenePrefabs.Add (new ScenePrefab ("Navigation", "PlayerStart 2D", "Navigation/PlayerStart2D", "_PlayerStarts", "A point in the scene from which the Player begins.", "PlayerStart"));
			}
			else
			{
				scenePrefabs.Add (new ScenePrefab ("Navigation", "PlayerStart", "Navigation/PlayerStart", "_PlayerStarts", "A point in the scene from which the Player begins.", "PlayerStart"));
			}
		}
		

		/** Populates the list of 'Existing prefabs' for the currently-selected prefab button in the Scene Manager GUI. */
		public void GetPrefabsInScene ()
		{
			List<string> titles = new List<string>();
			MonoBehaviour[] objects;
			int i=1;

			ScenePrefab activeScenePrefab = GetActiveScenePrefab ();
			if (activeScenePrefab == null) return;

			if (Type.GetType ("AC." + activeScenePrefab.componentName) != null)
			{
				objects = FindObjectsOfType (Type.GetType ("AC." + GetActiveScenePrefab ().componentName)) as MonoBehaviour [];
				foreach (MonoBehaviour _object in objects)
				{
					if (UnityVersionHandler.ObjectIsInActiveScene (_object.gameObject))
					{
						titles.Add (i.ToString () + ": " + _object.gameObject.name);
						i++;
					}
				}
			}
			else if (!string.IsNullOrEmpty (activeScenePrefab.componentName))
			{
				objects = FindObjectsOfType (Type.GetType (GetActiveScenePrefab ().componentName)) as MonoBehaviour [];
				foreach (MonoBehaviour _object in objects)
				{
					if (UnityVersionHandler.ObjectIsInActiveScene (_object.gameObject))
					{
						titles.Add (i.ToString () + ": " + _object.gameObject.name);
						i++;
					}
				}
			}
			
			if (i == 1)
			{
				titles.Add ("(None found in scene)");
			}
			
			prefabTextArray = titles.ToArray ();
		}
		
		#endif
		
	}
	
	
	#if UNITY_EDITOR

	/**
	 * A data container for an Adventure Creator prefab, used by SceneManager to provide a list of possible prefabs the user can instantiate.
	 */
	public class ScenePrefab
	{

		/** The prefab's category */
		public string category;
		/** The prefab's name, as it appears in the Scene Manager */
		public string subCategory;
		/** The filepath to the prefab, starting from /Assets/AdventureCreator/Prefabs/, and including the file's name */
		public string prefabPath;
		/** The name of the scene folder to place this prefab in on generation */
		public string sceneFolder;
		/** A brief description of the prefab's purpose */
		public string helpText;
		/** The defining MonoBehaviour component that makes the prefab unique */
		public string componentName;
		/** The prefab's icon, when listed in SceneManager */
		public Texture2D icon;


		/**
		 * <summary>The default Constructor>
		 * <param name = "_category">The prefab's category</param>
		 * <param name = "_subCategory">The prefab's name, as it appears in the Scene Manager</param>
		 * <param name = "_prefabPath">The filepath to the prefab, starting from /Assets/AdventureCreator/Prefabs/, and including the file's name</param>
		 * <param name = "_sceneFolder">The name of the scene folder to place this prefab in on generation</param>
		 * <param name = "_helpText">A brief description of the prefab's purpose</param>
		 * <param name = "_componentName">The defining MonoBehaviour component that makes the prefab unique</param>
		 * <param name = "_graphicName">The name of the prefab's icon, inside /Assets/AdventureCreator/Graphics/PrefabIcons</param>
		 */
		public ScenePrefab (string _category, string _subCategory, string _prefabPath, string _sceneFolder, string _helpText, string _componentName, string _graphicName = "")
		{
			category = _category;
			subCategory = _subCategory;
			prefabPath = _prefabPath;
			sceneFolder = _sceneFolder;
			helpText = _helpText;
			componentName = _componentName;
			
			if (!string.IsNullOrEmpty (_graphicName))
			{
				icon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/" + _graphicName +  ".png", typeof (Texture2D));
			}
			else
			{
				icon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/" + _componentName +  ".png", typeof (Texture2D));
			}
			
			if (_subCategory == "Collision Cylinder")
			{
				icon = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/PrefabIcons/" + _componentName +  "Cylinder.png", typeof (Texture2D));
			}
		}
		
	}
	
	#endif
	
}