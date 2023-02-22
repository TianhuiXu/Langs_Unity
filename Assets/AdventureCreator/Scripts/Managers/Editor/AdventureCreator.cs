#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace AC
{

	public class AdventureCreator : EditorWindow
	{
		
		public const string version = "1.76.1";
	 
		private bool showScene = true;
		private bool showSettings = false;
		private bool showActions = false;
		private bool showGVars = false;
		private bool showInvItems = false;
		private bool showSpeech = false;
		private bool showCursor = false;
		private bool showMenu = false;
		
		private Vector2 scroll;
		

		[MenuItem ("Adventure Creator/Editors/Game Editor")]
		public static void Init ()
		{
			// Get existing open window or if none, make a new one:
			AdventureCreator window = (AdventureCreator) GetWindow (typeof (AdventureCreator));
			window.titleContent.text = "AC Game Editor";
		}


		private void OnEnable ()
		{
			RefreshActions ();
		}
		
		
		private void OnInspectorUpdate ()
		{
			Repaint ();
		}
		
		
		private void OnGUI ()
		{
			if (!ACInstaller.IsInstalled ())
			{
				ACInstaller.DoInstall ();
			}
			
			if (Resource.References)
			{
				GUILayout.Space (10);
				GUILayoutOption tabWidth = GUILayout.Width (this.position.width / 4f);

				GUILayout.BeginHorizontal ();
				
				if (GUILayout.Toggle (showScene, "Scene", "toolbarbutton", tabWidth))
				{
					SetTab (0);
				}
				if (GUILayout.Toggle (showSettings, "Settings", "toolbarbutton", tabWidth)) 
				{
					SetTab (1);
				}
				if (GUILayout.Toggle (showActions, "Actions", "toolbarbutton", tabWidth))
				{
					SetTab (2);
				}
				if (GUILayout.Toggle (showGVars, "Variables", "toolbarbutton", tabWidth))
				{
					SetTab (3);
				}
				
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				
				if (GUILayout.Toggle (showInvItems, "Inventory", "toolbarbutton", tabWidth))
				{
					SetTab (4);
				}
				if (GUILayout.Toggle (showSpeech, "Speech", "toolbarbutton", tabWidth))
				{
					SetTab (5);
				}
				if (GUILayout.Toggle (showCursor, "Cursor", "toolbarbutton", tabWidth))
				{
					SetTab (6);
				}
				if (GUILayout.Toggle (showMenu, "Menu", "toolbarbutton", tabWidth))
				{
					SetTab (7);
				}
		
				GUILayout.EndHorizontal ();
				GUILayout.Space (5);

				if (showScene)
				{
					GUILayout.Label ("Scene manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.sceneManager = (SceneManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.sceneManager, typeof (SceneManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.sceneManager)
					{
						AskToCreate <SceneManager> ("SceneManager");
					}
					else
					{
						if (Resource.References.sceneManager.name == "Demo_SceneManager" || Resource.References.sceneManager.name == "Demo2D_SceneManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.sceneManager.ShowGUI (this.position);
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showSettings)
				{
					GUILayout.Label ("Settings manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.settingsManager = (SettingsManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.settingsManager, typeof (SettingsManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.settingsManager)
					{
						AskToCreate <SettingsManager> ("SettingsManager");
					}
					else
					{
						if (Resource.References.settingsManager.name == "Demo_SettingsManager" || Resource.References.settingsManager.name == "Demo2D_SettingsManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.settingsManager.ShowGUI ();
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showActions)
				{
					GUILayout.Label ("Actions manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.actionsManager = (ActionsManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.actionsManager, typeof (ActionsManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.actionsManager)
					{
						AskToCreate <ActionsManager> ("ActionsManager");
					}
					else
					{
						if (Resource.References.actionsManager.name == "Demo_ActionsManager" || Resource.References.actionsManager.name == "Demo2D_ActionsManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.actionsManager.ShowGUI (this.position);
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showGVars)
				{
					GUILayout.Label ("Variables manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.variablesManager = (VariablesManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.variablesManager, typeof (VariablesManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();
					
					if (!Resource.References.variablesManager)
					{
						AskToCreate <VariablesManager> ("VariablesManager");
					}
					else
					{
						if (Resource.References.variablesManager.name == "Demo_VariablesManager" || Resource.References.variablesManager.name == "Demo2D_VariablesManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.variablesManager.ShowGUI ();
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showInvItems)
				{
					GUILayout.Label ("Inventory manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.inventoryManager = (InventoryManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.inventoryManager, typeof (InventoryManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.inventoryManager)
					{
						AskToCreate <InventoryManager> ("InventoryManager");
					}
					else
					{
						if (Resource.References.inventoryManager.name == "Demo_InventoryManager" || Resource.References.inventoryManager.name == "Demo2D_InventoryManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.inventoryManager.ShowGUI (this.position);
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showSpeech)
				{
					GUILayout.Label ("Speech manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.speechManager = (SpeechManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.speechManager, typeof (SpeechManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.speechManager)
					{
						AskToCreate <SpeechManager> ("SpeechManager");
					}
					else
					{
						if (Resource.References.speechManager.name == "Demo_SpeechManager" || Resource.References.speechManager.name == "Demo2D_SpeechManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.speechManager.ShowGUI (this.position);
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showCursor)
				{
					GUILayout.Label ("Cursor manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.cursorManager = (CursorManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.cursorManager, typeof (CursorManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.cursorManager)
					{
						AskToCreate <CursorManager> ("CursorManager");
					}
					else
					{
						if (Resource.References.cursorManager.name == "Demo_CursorManager" || Resource.References.cursorManager.name == "Demo2D_CursorManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.cursorManager.ShowGUI ();
						GUILayout.EndScrollView ();
					}
				}
				
				else if (showMenu)
				{
					GUILayout.Label ("Menu manager",  CustomStyles.managerHeader);
					EditorGUI.BeginChangeCheck ();
					Resource.References.menuManager = (MenuManager) EditorGUILayout.ObjectField ("Asset file: ", Resource.References.menuManager, typeof (MenuManager), false);
					if (EditorGUI.EndChangeCheck ())
					{
						KickStarter.ClearManagerCache ();
					}
					DrawManagerSpace ();

					if (!Resource.References.menuManager)
					{
						AskToCreate <MenuManager> ("MenuManager");
					}
					else
					{
						if (Resource.References.menuManager.name == "Demo_MenuManager" || Resource.References.menuManager.name == "Demo2D_MenuManager")
						{
							EditorGUILayout.HelpBox ("The Demo Managers are for demonstration purposes only.  Modifying them to create your game may result in data loss upon upgrading - instead, use the New Game Wizard to create a new set of Managers.", MessageType.Warning);
						}

						scroll = GUILayout.BeginScrollView (scroll);
						Resource.References.menuManager.ShowGUI ();
						GUILayout.EndScrollView ();
					}
				}

				Resource.References.viewingMenuManager = showMenu;

				EditorGUILayout.Separator ();
				GUILayout.Box (string.Empty, GUILayout.ExpandWidth (true), GUILayout.Height(1));
				GUILayout.Label ("Adventure Creator - Version " + AdventureCreator.version, EditorStyles.miniLabel);

			}
			else
			{
				MissingReferencesGUI ();
			}
			
			if (GUI.changed)
			{
				if (showActions)
				{
					RefreshActions ();
				}

				EditorUtility.SetDirty (this);
				if (Resource.References != null)
				{
					EditorUtility.SetDirty (Resource.References);
				}
			}
		}


		public static void MissingReferencesGUI ()
		{
			EditorStyles.label.wordWrap = true;
			GUILayout.Label ("Error - missing References",  CustomStyles.managerHeader);
			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox ("A 'References' file must be present in the directory '" + Resource.DefaultReferencesPath + "' - please click to create one.", MessageType.Warning);

			if (GUILayout.Button ("Create 'References' file"))
			{
				CustomAssetUtility.CreateAsset<References> ("References", Resource.DefaultReferencesPath);
			}
		}


		private void DrawManagerSpace ()
		{
			EditorGUILayout.Space ();
			EditorGUILayout.Separator ();
			GUILayout.Box (string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1));
		}
		
		
		private void SetTab (int tab)
		{
			showScene = false;
			showSettings = false;
			showActions = false;
			showGVars = false;
			showInvItems = false;
			showSpeech = false;
			showCursor = false;
			showMenu = false;
			
			if (tab == 0)
			{
				showScene = true;
			}
			else if (tab == 1)
			{
				showSettings = true;
			}
			else if (tab == 2)
			{
				showActions = true;
			}
			else if (tab == 3)
			{
				showGVars = true;
			}
			else if (tab == 4)
			{
				showInvItems = true;
			}
			else if (tab == 5)
			{
				showSpeech = true;
			}
			else if (tab == 6)
			{
				showCursor = true;
			}
			else if (tab == 7)
			{
				showMenu = true;
			}
		}
		
		
		private void AskToCreate<T> (string obName) where T : ScriptableObject
		{
			EditorStyles.label.wordWrap = true;
			EditorGUILayout.HelpBox ("A '" + obName + "' asset is required for the game to run correctly. Please either click the button below to create one, or use the New Game Wizard to create a set of Managers.", MessageType.Warning);
			
			if (GUILayout.Button ("Create new " + obName + " asset file"))
			{
				try {
					ScriptableObject t = CustomAssetUtility.CreateAsset<T> (obName);
					
					Undo.RecordObject (Resource.References, "Assign " + obName);
					
					if (t is SceneManager)
					{
						Resource.References.sceneManager = (SceneManager) t;
					}
					else if (t is SettingsManager)
					{
						Resource.References.settingsManager = (SettingsManager) t;
					}
					else if (t is ActionsManager)
					{
						Resource.References.actionsManager = (ActionsManager) t;
						RefreshActions ();
					}
					else if (t is VariablesManager)
					{
						Resource.References.variablesManager = (VariablesManager) t;
					}
					else if (t is InventoryManager)
					{
						Resource.References.inventoryManager = (InventoryManager) t;
					}
					else if (t is SpeechManager)
					{
						Resource.References.speechManager = (SpeechManager) t;
					}
					else if (t is CursorManager)
					{
						Resource.References.cursorManager = (CursorManager) t;
					}
					else if (t is MenuManager)
					{
						Resource.References.menuManager = (MenuManager) t;
					}
				}
				catch
				{
					ACDebug.LogWarning ("Could not create " + obName + ".");
				}
			}
		}


		public static void RefreshActions ()
		{
			if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().actionsManager == null)
			{
				return;
			}
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;

			// Collect data to transfer
			List<ActionType> oldActionTypes = new List<ActionType>();
			foreach (ActionType actionType in actionsManager.AllActions)
			{
				oldActionTypes.Add (actionType);
			}

			actionsManager.AllActions.Clear ();

			// Load default Actions
			AddActionsFromFolder (actionsManager, actionsManager.FolderPath, oldActionTypes);

			for (int i=0; i<actionsManager.customFolderPaths.Count; i++)
			{
				string customFolderPath = actionsManager.customFolderPaths[i];

				// Discount duplicates
				bool ignoreMe = false;
				for (int j=0; j<i; j++)
				{
					if (actionsManager.customFolderPaths[j] == customFolderPath)
					{
						ignoreMe = true;
					}
				}

				if (ignoreMe) continue;

				if (!string.IsNullOrEmpty (customFolderPath) && actionsManager.FolderPath != ("Assets/" + customFolderPath))
				{
					try
					{
						AddActionsFromFolder (actionsManager, "Assets/" + customFolderPath, oldActionTypes);
					}
					catch (System.Exception e)
					{
						ACDebug.LogWarning ("Can't access directory " + "Assets/" + customFolderPath + " - does it exist?\n\nException: " + e);
					}
				}
			}
			
			actionsManager.AllActions.Sort (delegate(ActionType i1, ActionType i2) { return i1.GetFullTitle (true).CompareTo(i2.GetFullTitle (true)); });
		}


		private static void AddActionsFromFolder (ActionsManager actionsManager, string folderPath, List<ActionType> oldActionTypes)
		{
			DirectoryInfo dir = new DirectoryInfo (folderPath);

			if (!dir.Exists)
			{
				Debug.LogWarning ("Cannot add Actions from folder '" + folderPath + "', because the directory does not exist!");
				return;
			}

			FileInfo[] info = dir.GetFiles ("*.cs");
			foreach (FileInfo f in info)
			{
				if (f.Name.StartsWith ("._")) continue;

				try
				{
					int extentionPosition = f.Name.IndexOf (".cs");
					string className = f.Name.Substring (0, extentionPosition);

					StreamReader streamReader = new StreamReader (f.FullName);
					string fileContents = streamReader.ReadToEnd ();
					streamReader.Close ();
					
					fileContents = fileContents.Replace (" ", "");
					
					if (fileContents.Contains ("class" + className + ":Action") ||
					    fileContents.Contains ("class" + className + ":AC.Action"))
					{
						MonoScript script = AssetDatabase.LoadAssetAtPath <MonoScript> (folderPath + "/" + f.Name);
						if (script == null) continue;

						#if AC_ActionListPrefabs
						System.Runtime.Remoting.ObjectHandle handle = System.Activator.CreateInstance ("Assembly-CSharp", "AC." + script.name);
						Action tempAction = (Action) handle.Unwrap();
						#else
						Action tempAction = (Action) CreateInstance (script.GetClass ());
						#endif

						if (tempAction != null && tempAction is Action)
						{
							ActionType newActionType = new ActionType (className, tempAction);
							
							// Transfer back data
							foreach (ActionType oldActionType in oldActionTypes)
							{
								if (newActionType.IsMatch (oldActionType))
								{
									newActionType.color = oldActionType.color;
									newActionType.isEnabled = oldActionType.isEnabled;
									if (newActionType.color == new Color (0f, 0f, 0f, 0f)) newActionType.color = Color.white;
									if (newActionType.color.a < 1f) newActionType.color = new Color (newActionType.color.r, newActionType.color.g, newActionType.color.b, 1f);
								}
							}
							
							actionsManager.AllActions.Add (newActionType);
						}
					}
					else
					{
						ACDebug.LogError ("The script '" + f.FullName + "' must derive from AC's Action class in order to be available as an Action.");
					}
				}
				catch {}
			}
		}

	}

}

#endif