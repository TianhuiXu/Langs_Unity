#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	[InitializeOnLoad]
	public class HierarchyIcons
	{

		private static HashSet<int> actionListIDs;
		private static HashSet<int> rememberIDs;

		private static ActionList[] actionLists;
		private static ConstantID[] constantIDs;

		#if UNITY_EDITOR_OSX
			#if UNITY_2019_1_OR_NEWER
			private static float defaultOffset = 20;
			#else
			private static float defaultOffset = -10;
			#endif
		#else
			#if UNITY_2019_1_OR_NEWER
			private static float defaultOffset = 25;
			#else
			private static float defaultOffset = 0;
			#endif
		#endif

		private const float rememberOffset = 25;
		private const float iconWidth = 18;


		static HierarchyIcons ()
		{
			EditorApplication.update += UpdateCB;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
			EditorApplication.playModeStateChanged += LogPlayModeState;
			UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ActiveSceneChanged;
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_sceneLoaded;
			UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

			#if UNITY_2021_2_OR_NEWER
			UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
			UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabStageOpened;
			#elif UNITY_2019_4_OR_NEWER
			UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabStageOpened;
			UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
			#endif
		}


		#if UNITY_2021_2_OR_NEWER

		private static void OnPrefabStageClosing (UnityEditor.SceneManagement.PrefabStage obj)
		{
			GatherIDs ();
		}


		private static void OnPrefabStageOpened (UnityEditor.SceneManagement.PrefabStage prefabStage)
		{
			GameObject root = prefabStage.prefabContentsRoot;
			GatherIDs (root);
		}

		#elif UNITY_2019_4_OR_NEWER

		private static void OnPrefabStageClosing (UnityEditor.Experimental.SceneManagement.PrefabStage obj)
		{
			GatherIDs ();
		}


		private static void OnPrefabStageOpened (UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage)
		{
			GameObject root = prefabStage.prefabContentsRoot;
			GatherIDs (root);
		}

		#endif


		private static void SceneManager_sceneUnloaded (UnityEngine.SceneManagement.Scene arg0)
		{
			GatherIDs ();
		}


		private static void SceneManager_sceneLoaded (UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
		{
			GatherIDs ();
		}


		private static void ActiveSceneChanged (UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
		{
			GatherIDs ();
		}


		private static void UpdateCB ()
		{
			if (!Application.isPlaying)
			{
				#if UNITY_2021_2_OR_NEWER
				UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage ();
				if (prefabStage != null)
				{
					GatherIDs (prefabStage.prefabContentsRoot);
				}
				else
				{
					GatherIDs ();
				}
				#elif UNITY_2019_4_OR_NEWER
				UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage ();
				if (prefabStage != null)
				{
					GatherIDs (prefabStage.prefabContentsRoot);
				}
				else
				{
					GatherIDs ();
				}
				#else
				GatherIDs ();
				#endif
			}
		}


		private static void GatherIDs (GameObject root = null)
		{
			if (!ACEditorPrefs.ShowHierarchyIcons)
			{
				return;
			}

			if (root == null)
			{
				actionLists = Object.FindObjectsOfType (typeof (ActionList)) as ActionList[];
			}
			else
			{
				actionLists = root.GetComponentsInChildren<ActionList> ();
			}

			actionListIDs = new HashSet<int> ();
			foreach (ActionList actionList in actionLists)
			{
				actionListIDs.Add (actionList.gameObject.GetInstanceID ());
			}

			if (root == null)
			{
				constantIDs = Object.FindObjectsOfType (typeof (ConstantID)) as ConstantID[];
			}
			else
			{
				constantIDs = root.GetComponentsInChildren<ConstantID> ();
			}

			rememberIDs = new HashSet<int> ();
			foreach (ConstantID constantID in constantIDs)
			{
				if (constantID == null || !constantID.isActiveAndEnabled) continue;

				rememberIDs.Add (constantID.gameObject.GetInstanceID ());
			}
		}


		private static void LogPlayModeState (PlayModeStateChange state)
		{
			GatherIDs ();
		}


		private static void HierarchyItemCB (int instanceID, Rect selectionRect)
		{
			if (!ACEditorPrefs.ShowHierarchyIcons)
			{
				return;
			}

			// place the icon to the right of the list:
			Rect r = new Rect (selectionRect);
			r.x = r.width + defaultOffset + ACEditorPrefs.HierarchyIconOffset;
			r.width = iconWidth;

			if (actionListIDs != null && actionListIDs.Contains (instanceID))
			{
				foreach (ActionList actionList in actionLists)
				{
					if (actionList == null || !actionList.isActiveAndEnabled) continue;

					if (actionList.gameObject.GetInstanceID () == instanceID)
					{
						if (GUI.Button (r, string.Empty, CustomStyles.IconNodes))
						{
							ActionListEditorWindow.Init (actionList);

							if (actionList.gameObject.GetComponents<ActionList> ().Length > 1)
							{
								ACDebug.Log ("Multiple ActionLists are attached to the GameObject '" + actionList.gameObject.name + "' - only the first-found will be opened when clicking the Hierarchy node icon.", actionList.gameObject);
							}
							return;
						}
					}
				}
			}

			r.x -= rememberOffset;
			if (rememberIDs != null && rememberIDs.Contains (instanceID))
			{
				foreach (ConstantID constantID in constantIDs)
				{
					if (constantID == null || !constantID.isActiveAndEnabled) continue;

					if (constantID.gameObject.GetInstanceID () == instanceID)
					{
						GUI.Label (r, string.Empty, CustomStyles.IconSave);
						return;
					}
				}
			}
		}


		#region CameraPrefabs

		[MenuItem ("GameObject/Adventure Creator/2D/Camera/GameCamera 2D", false, 10)]
		private static void CreateGameCamera2D (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "GameCamera2D");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Camera/GameCamera 2D Drag", false, 10)]
		private static void CreateGameCamera2DDrag (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "GameCamera2DDrag");
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Camera/GameCamera 2.5D", false, 10)]
		private static void CreateGameCamera25D (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "GameCamera2.5D");
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Camera/Background Image", false, 10)]
		private static void CreateBackgroundImage (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "SetGeometry", "BackgroundImage");
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Camera/Scene sprite", false, 10)]
		private static void CreateSceneSprite (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "SetGeometry", "SceneSprite");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Camera/GameCamera", false, 10)]
		private static void CreateGameCamera (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "GameCamera");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Camera/GameCamera Third-person", false, 10)]
		private static void CreateGameCameraThirdPerson (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "GameCameraThirdPerson");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Camera/GameCamera Animated", false, 10)]
		private static void CreateGameCameraAnimated (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "GameCameraAnimated");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Camera/SimpleCamera", false, 10)]
		private static void CreateSimpleCamera (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Camera", "SimpleCamera");
		}

		#endregion


		#region LogicPrefabs

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Arrow prompt", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Arrow prompt", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Arrow prompt", false, 10)]
		private static void CreateArrowPrompt (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "ArrowPrompt");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Container", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Container", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Container", false, 10)]
		private static void CreateContainer (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Container");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Conversation", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Conversation", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Conversation", false, 10)]
		private static void CreateConversation (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Conversation");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Cutscene", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Cutscene", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Cutscene", false, 10)]
		private static void CreateCutscene (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Cutscene");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Dialogue option", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Dialogue option", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Dialogue option", false, 10)]
		private static void CreateDialogueOption (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "DialogueOption");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Hotspot", false, 10)]
		private static void CreateHotspot2D (MenuCommand menuCommand)
		{
			string prefabName = IsUnity2D () ? "Hotspot2D" : "Hotspot";
			CreateObjectFromHierarchy (menuCommand, "Logic", prefabName);
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Hotspot", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Hotspot", false, 10)]
		private static void CreateHotspot (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Hotspot");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Interaction", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Interaction", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Interaction", false, 10)]
		private static void CreateInteraction (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Interaction");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Sound", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Sound", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Sound", false, 10)]
		private static void CreateSound (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Sound");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Logic/Trigger", false, 10)]
		private static void CreateTrigger2D (MenuCommand menuCommand)
		{
			string prefabName = IsUnity2D () ? "Trigger2D" : "Trigger";
			CreateObjectFromHierarchy (menuCommand, "Logic", prefabName);
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Logic/Trigger", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Logic/Trigger", false, 10)]
		private static void CreateTrigger (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Logic", "Trigger");
		}

		#endregion


		#region MoveablePrefabs

		[MenuItem ("GameObject/Adventure Creator/3D/Moveable/Draggable", false, 10)]
		private static void CreateDraggable (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Moveable", "Draggable");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Moveable/PickUp", false, 10)]
		private static void CreatePickUp (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Moveable", "PickUp");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Moveable/Straight track", false, 11)]
		private static void CreateStraightTracke (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Moveable", "StraightTrack");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Moveable/Curved track", false, 12)]
		private static void CreateCurvedTrack (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Moveable", "CurvedTrack");
		}

		[MenuItem ("GameObject/Adventure Creator/3D/Moveable/Hinge track", false, 13)]
		private static void CreateHingeTrack (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Moveable", "HingeTrack");
		}

		#endregion


		#region NavigationPrefabs

		[MenuItem ("GameObject/Adventure Creator/2D/Navigation/Sorting map", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/Sorting map", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/Sorting map", false, 10)]
		private static void CreateSortingMap (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Navigation", "SortingMap");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Navigation/Collision Cube", false, 10)]
		private static void CreateCollisionCube2D (MenuCommand menuCommand)
		{
			string prefabName = IsUnity2D () ? "CollisionCube2D" : "CollisionCube";
			CreateObjectFromHierarchy (menuCommand, "Navigation", prefabName);
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/Collision Cube", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/Collision Cube", false, 10)]
		private static void CreateCollisionCube (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Navigation", "CollisionCube");
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/Collision Cylinder", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/Collision Cylinder", false, 10)]
		private static void CreateCollisionCylinder (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Navigation", "CollisionCylinder");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Navigation/Marker", false, 10)]
		private static void CreateMarker2D (MenuCommand menuCommand)
		{
			string prefabName = IsUnity2D () ? "Marker2D" : "Marker";
			CreateObjectFromHierarchy (menuCommand, "Navigation", prefabName);
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/Marker", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/Marker", false, 10)]
		private static void CreateMarker (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Navigation", "Marker");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Navigation/NavMesh", false, 10)]
		private static void CreateNavMesh2D (MenuCommand menuCommand)
		{
			if (IsUnity2D ())
			{
				CreateObjectFromHierarchy (menuCommand, "Navigation", "NavMesh2D");
			}
			else
			{
				CreateNavMesh (menuCommand);
			}
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/NavMesh", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/NavMesh", false, 10)]
		private static void CreateNavMesh (MenuCommand menuCommand)
		{
			if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.UnityNavigation)
			{
				CreateObjectFromHierarchy (menuCommand, "Navigation", "NavMeshSegment");
			}
			else if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.meshCollider)
			{
				CreateObjectFromHierarchy (menuCommand, "Navigation", "NavMesh");
			}
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Navigation/Path", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/Path", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/Path", false, 10)]
		private static void CreatePath (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Navigation", "Path");
		}

		[MenuItem ("GameObject/Adventure Creator/2D/Navigation/PlayerStart", false, 10)]
		private static void CreatePlayerStart2D (MenuCommand menuCommand)
		{
			string prefabName = IsUnity2D () ? "PlayerStart2D" : "PlayerStart";
			CreateObjectFromHierarchy (menuCommand, "Navigation", prefabName);
		}

		[MenuItem ("GameObject/Adventure Creator/2.5D/Navigation/PlayerStart", false, 10)]
		[MenuItem ("GameObject/Adventure Creator/3D/Navigation/PlayerStart", false, 10)]
		private static void CreatePlayerStart (MenuCommand menuCommand)
		{
			CreateObjectFromHierarchy (menuCommand, "Navigation", "PlayerStart");
		}

		#endregion


		private static void CreateObjectFromHierarchy (MenuCommand menuCommand, string folderName, string prefabName)
		{
			if (KickStarter.sceneSettings == null)
			{
				ACDebug.LogWarning ("Cannot create " + prefabName + " until Adventure Creator has prepared the scene from the top of the Scene Manager.");
				return;
			}

			GameObject newObject = SceneManager.AddPrefab (folderName, prefabName, true, true, false);
			GameObjectUtility.SetParentAndAlign (newObject, menuCommand.context as GameObject);
			Undo.RegisterCreatedObjectUndo (newObject, "Create " + newObject.name);
			Selection.activeObject = newObject;
		}


		private static bool IsUnity2D ()
		{
			return SceneSettings.IsUnity2D ();
		}

	}

}

#endif