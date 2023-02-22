/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ACEditorPrefs.cs"
 * 
 *	This script allows for the setting of Editor-wide preferences via Unity's Player settings window (Unity 2019.2 and later).
 * 
 */

#if UNITY_2019_2_OR_NEWER && UNITY_EDITOR
#define CAN_USE_EDITOR_PREFS
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace AC
{

	/** This script allows for the setting of Editor-wide preferences via Unity's Player settings window (Unity 2019.2 and later). */
	public class ACEditorPrefs : ScriptableObject
	{

		private const string DefaultInstallPath = "Assets/AdventureCreator";

		#if UNITY_EDITOR

		private const string settingsPath = "/Editor/ACEditorPrefs.asset";

		[SerializeField] protected int hierarchyIconOffset = 0;
		[SerializeField] protected Color hotspotGizmoColor = new Color (1f, 1f, 0f, 0.6f);
		[SerializeField] protected Color triggerGizmoColor = new Color (1f, 0.3f, 0f, 0.8f);
		[SerializeField] protected Color collisionGizmoColor = new Color (0f, 1f, 1f, 0.8f);
		[SerializeField] protected Color pathGizmoColor = Color.blue;
		[SerializeField] protected int menuItemsBeforeScroll = 15;
		[SerializeField] protected CSVFormat csvFormat = CSVFormat.Standard;
		[SerializeField] protected bool showHierarchyIcons = true;
		[SerializeField] protected int editorLabelWidth = 0;
		[SerializeField] protected int actionNodeWidth = 300;
		[SerializeField] protected bool disableInstaller = false;
		[SerializeField] protected string installPath = DefaultInstallPath;
		[SerializeField] protected int autosaveActionListsInterval = 10;


		internal static ACEditorPrefs GetOrCreateSettings ()
		{
			string fullPath = DefaultInstallPath + settingsPath;

			ACEditorPrefs settings = AssetDatabase.LoadAssetAtPath<ACEditorPrefs> (fullPath);
			
			if (settings == null)
			{
				bool canCreateNew = AssetDatabase.IsValidFolder (DefaultInstallPath + "/Editor");
				
				if (!canCreateNew)
				{
					AssetDatabase.CreateFolder (DefaultInstallPath, "Editor");
					canCreateNew = AssetDatabase.IsValidFolder (DefaultInstallPath + "/Editor");
				}

				if (!canCreateNew)
				{
					fullPath = "Assets/" + settingsPath;
					settings = AssetDatabase.LoadAssetAtPath<ACEditorPrefs> (fullPath);

					if (settings)
					{
						return settings;
					}
					canCreateNew = AssetDatabase.IsValidFolder ("Assets/Editor");
					if (!canCreateNew)
					{
						AssetDatabase.CreateFolder ("Assets", "Editor");
						canCreateNew = AssetDatabase.IsValidFolder ("Assets/Editor");
					}
				}
				
				if (canCreateNew)
				{
					settings = CreateInstance<ACEditorPrefs> ();
					settings.hierarchyIconOffset = DefaultHierarchyIconOffset;
					settings.hotspotGizmoColor = DefaultHotspotGizmoColor;
					settings.triggerGizmoColor = DefaultTriggerGizmoColor;
					settings.collisionGizmoColor = DefaultCollisionGizmoColor;
					settings.pathGizmoColor = DefaultPathGizmoColor;
					settings.menuItemsBeforeScroll = DefaultMenuItemsBeforeScroll;
					settings.csvFormat = DefaultCSVFormat;
					settings.showHierarchyIcons = DefaultShowHierarchyIcons;
					settings.editorLabelWidth = DefaultEditorLabelWidth;
					settings.actionNodeWidth = DefaultActionNodeWidth;
					settings.disableInstaller = DefaultDisableInstaller;
					settings.installPath = DefaultInstallPath;
					settings.autosaveActionListsInterval = DefaultAutosaveActionListsInterval;
					AssetDatabase.CreateAsset (settings, fullPath);
					Debug.Log ("Created new AC EditorPrefs asset: '" + fullPath + "'", settings);
					AssetDatabase.SaveAssets ();
				}
				else
				{
					Debug.LogWarning ("Cannot create AC editor prefs - does the folder '" + DefaultInstallPath + "/Editor' exist?");
					return null;
				}
			}
			return settings;
		}


		#if CAN_USE_EDITOR_PREFS

		internal static SerializedObject GetSerializedSettings ()
		{
			ACEditorPrefs settings = GetOrCreateSettings ();
			if (settings)
			{
				return new SerializedObject (settings);
			}
			return null;
		}

		#endif


		/** A horizontal offset to apply to Hierarchy icons */
		public static int HierarchyIconOffset
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.hierarchyIconOffset : DefaultHierarchyIconOffset;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.hierarchyIconOffset = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static int DefaultHierarchyIconOffset
		{
			get
			{
				return 0;
			}
		}


		/** How wide to render labels in the Managers and other Editors */
		public static int EditorLabelWidth
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.editorLabelWidth : DefaultEditorLabelWidth;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.editorLabelWidth = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static int DefaultEditorLabelWidth
		{
			get
			{
				return 0;
			}
		}


		/** How wide Action nodes are in the ActionList Editor window */
		public static int ActionNodeWidth
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				int value = (settings != null) ? settings.actionNodeWidth : DefaultActionNodeWidth;

				if (value <= 0) return DefaultActionNodeWidth;
				return Mathf.Clamp (value, 200, 800);
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.actionNodeWidth = Mathf.Clamp (value, 200, 800);
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static int DefaultActionNodeWidth
		{
			get
			{
				return 300;
			}
		}


		/** If True, then icons can be displayed in the Hierarchy window */
		public static bool ShowHierarchyIcons
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.showHierarchyIcons : DefaultShowHierarchyIcons;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.showHierarchyIcons = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static bool DefaultShowHierarchyIcons
		{
			get
			{
				return true;
			}
		}


		/** The colour to paint Hotspot gizmos with */
		public static Color HotspotGizmoColor
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.hotspotGizmoColor : DefaultHotspotGizmoColor;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.hotspotGizmoColor = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static Color DefaultHotspotGizmoColor
		{
			get
			{
				return new Color (1f, 1f, 0f, 0.6f);
			}
		}


		/** The colour to paint Trigger gizmos with */
		public static Color TriggerGizmoColor
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.triggerGizmoColor : DefaultTriggerGizmoColor;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.triggerGizmoColor = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static Color DefaultTriggerGizmoColor
		{
			get
			{
				return new Color (1f, 0.3f, 0f, 0.8f);
			}
		}


		/** The colour to paint Collision gizmos with */
		public static Color CollisionGizmoColor
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.collisionGizmoColor : DefaultCollisionGizmoColor;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.collisionGizmoColor = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static Color DefaultCollisionGizmoColor
		{
			get
			{
				return new Color (0f, 1f, 1f, 0.8f);
			}
		}


		/** The colour to paint Paths with */
		public static Color PathGizmoColor
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.pathGizmoColor : DefaultPathGizmoColor;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.pathGizmoColor = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static Color DefaultPathGizmoColor
		{
			get
			{
				return Color.blue;
			}
		}


		/** The format to read/write CSV files */
		public static CSVFormat CSVFormat
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.csvFormat : DefaultCSVFormat;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.csvFormat = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static CSVFormat DefaultCSVFormat
		{
			get
			{
				return CSVFormat.Standard;
			}
		}


		/** How many menu items can be displayed in the Editor window before scrolling is required */
		public static int MenuItemsBeforeScroll
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.menuItemsBeforeScroll : DefaultMenuItemsBeforeScroll;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.menuItemsBeforeScroll = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static int DefaultMenuItemsBeforeScroll
		{
			get
			{
				return 15;
			}
		}


		/** If True, checks for AC's required Input and Layer settings will be bypassed */
		public static bool DisableInstaller
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.disableInstaller : DefaultDisableInstaller;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.disableInstaller = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static bool DefaultDisableInstaller
		{
			get
			{
				return false;
			}
		}


		public static string InstallPath
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return (settings) ? settings.installPath : DefaultInstallPath;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.installPath = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		public static int AutosaveActionListsInterval
		{
			get
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				return settings ? settings.autosaveActionListsInterval : DefaultAutosaveActionListsInterval;
			}
			set
			{
				ACEditorPrefs settings = GetOrCreateSettings ();
				if (settings) settings.autosaveActionListsInterval = value;
				if (settings) EditorUtility.SetDirty (settings);
			}
		}


		private static int DefaultAutosaveActionListsInterval
		{
			get
			{
				return 10;
			}
		}

		#endif

	}


	#if CAN_USE_EDITOR_PREFS

	static class ACEditorPrefsIMGUIRegister
	{

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider ()
		{
			var provider = new SettingsProvider ("Project/AdventureCreator", SettingsScope.Project)
			{
				label = "Adventure Creator",

				guiHandler = (searchContext) =>
				{
					var settings = ACEditorPrefs.GetSerializedSettings ();
					if (settings != null)
					{
						EditorGUILayout.LabelField ("Gizmo colours", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("hotspotGizmoColor"), new GUIContent ("Hotspots:", "The colour to tint Hotspot gizmos with"));
						EditorGUILayout.PropertyField (settings.FindProperty ("triggerGizmoColor"), new GUIContent ("Triggers:", "The colour to tint Trigger gizmos with"));
						EditorGUILayout.PropertyField (settings.FindProperty ("collisionGizmoColor"), new GUIContent ("Collisions:", "The colour to tint Collision gizmos with"));
						EditorGUILayout.PropertyField (settings.FindProperty ("pathGizmoColor"), new GUIContent ("Paths:", "The colour to draw Path gizmos with"));

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Hierarchy icons", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("showHierarchyIcons"), new GUIContent ("Show icons?", "If True, save and node icons will appear in the Hierarchy window"));
						if (settings.FindProperty ("showHierarchyIcons") == null || settings.FindProperty ("showHierarchyIcons").boolValue)
						{
							EditorGUILayout.PropertyField (settings.FindProperty ("hierarchyIconOffset"), new GUIContent ("Horizontal offset:", "A horizontal offset to apply to AC icons in the Hierarchy"));
						}

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Editor settings", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("installPath"), new GUIContent ("Install path:", "The filepath to AC's root folder"));
						EditorGUILayout.PropertyField (settings.FindProperty ("editorLabelWidth"), new GUIContent ("Label widths:", "How wide to render labels in Managers and other editors"));
						EditorGUILayout.PropertyField (settings.FindProperty ("menuItemsBeforeScroll"), new GUIContent ("Items before scrolling:", "How many Menus, Inventory items, Variables etc can be listed in the AC Game Editor before scrolling becomes necessary"));
						EditorGUILayout.PropertyField (settings.FindProperty ("disableInstaller"), new GUIContent ("Bypass install checks?", "If True, checks for AC's required Input and Layer settings will be bypassed"));

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("ActionList settings", EditorStyles.boldLabel);
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.PropertyField (settings.FindProperty ("autosaveActionListsInterval"), new GUIContent ("Autosave interval (m):", "How often - in minutes - scene-based ActionList data should be backed-up (set to 0 to disable)"));
						if (GUILayout.Button ("Autosave all now", GUILayout.MaxWidth (160f)))
						{
							AutosaveAll ();
						}
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.PropertyField (settings.FindProperty ("actionNodeWidth"), new GUIContent ("Node width:", "How wide Actions are when rendered as nodes in the ActionList Editor window"));

						EditorGUILayout.Space ();
						EditorGUILayout.LabelField ("Import / export", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField (settings.FindProperty ("csvFormat"), new GUIContent ("CSV format:", "The formatting method to apply to CSV files"));

						settings.ApplyModifiedProperties ();
					}
					else
					{
						EditorGUILayout.HelpBox ("Cannot create AC editor prefs - does the folder '/Assets/AdventureCreator/Editor' exist?", MessageType.Warning);
					}
				},
			};

			return provider;
		}


		private static void AutosaveAll ()
		{
			bool canProceed = EditorUtility.DisplayDialog ("Auto-save all ActionLists", "AC will now go through your game's build settings, and backup the data for all scene-based ActionLists. It is recommended to back up your project beforehand.", "OK", "Cancel");
			if (!canProceed) return;

			if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
			{
				string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
				string[] sceneFiles = AdvGame.GetSceneFiles ();

				// First look for lines that already have an assigned lineID
				foreach (string sceneFile in sceneFiles)
				{
					AutosaveActions (sceneFile);
				}

				ACDebug.Log ("Process complete - " + sceneFiles.Length + " scenes processed.");

				UnityVersionHandler.OpenScene (originalScene);
			}
		}


		private static void AutosaveActions (string sceneFile)
		{
			UnityVersionHandler.OpenScene (sceneFile);

			ActionList[] actionLists = UnityEngine.Object.FindObjectsOfType<ActionList> ();
			if (actionLists.Length == 0)
			{
				return;
			}

			JsonAction.CacheSceneObjectReferences ();
			foreach (ActionList actionList in actionLists)
			{
				actionList.BackupData ();
			}
			JsonAction.ClearSceneObjectReferencesCache ();

			UnityVersionHandler.SaveScene ();
		}

	}

	#endif

}