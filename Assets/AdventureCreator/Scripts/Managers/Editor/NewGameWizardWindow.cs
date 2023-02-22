#if UNITY_EDITOR

#if UNITY_ANDROID || UNITY_IOS
#define ON_MOBILE
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AC
{

	public class NewGameWizardWindow : EditorWindow
	{

		private string gameName = "";

		private int cameraPerspective_int;
		private readonly string[] cameraPerspective_list = { "2D", "2.5D", "3D" };
		private bool screenSpace = false;
		private bool oneScenePerBackground = false;
		private MovementMethod movementMethod = MovementMethod.PointAndClick;

		private InputMethod inputMethod = InputMethod.MouseAndKeyboard;
		private AC_InteractionMethod interactionMethod = AC_InteractionMethod.ContextSensitive;
		private HotspotDetection hotspotDetection = HotspotDetection.MouseOver;

		public bool directControl;
		public bool touchScreen;
		public WizardMenu wizardMenu = WizardMenu.DefaultAC;

		private int pageNumber = 0;
		private References references;

		private int numPages = 6;

		// To process
		private CameraPerspective cameraPerspective = CameraPerspective.ThreeD;
		private MovingTurning movingTurning = MovingTurning.Unity2D;

		private readonly Rect pageRect = new Rect (350, 335, 150, 25);


		[MenuItem ("Adventure Creator/Getting started/New Game wizard", false, 4)]
		public static void Init ()
		{
			NewGameWizardWindow window = EditorWindow.GetWindowWithRect <NewGameWizardWindow> (new Rect (0, 0, 420, 360), true, "New Game Wizard", true);
			window.GetReferences ();
			window.titleContent.text = "New Game wizard";
			window.position = new Rect (300, 200, 420, 360);
		}
		
		
		private void GetReferences ()
		{
			references = Resource.References;
		}


		public void OnInspectorUpdate ()
		{
			Repaint ();
		}


		private void OnGUI ()
		{
			GUILayout.BeginVertical (CustomStyles.thinBox, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));

			GUILayout.Label (GetTitle (), CustomStyles.managerHeader);
			if (!string.IsNullOrEmpty (GetTitle ()))
			{
				EditorGUILayout.Separator ();
				GUILayout.Space (10f);
			}

			if (references == null)
			{
				GetReferences ();
			}
			if (references == null)
			{
				AdventureCreator.MissingReferencesGUI ();
				GUILayout.EndHorizontal ();
				return;
			}

			ShowPage ();

			GUILayout.Space (15f);
			GUILayout.BeginHorizontal ();
			if (pageNumber < 1)
			{
				if (pageNumber < 0)
				{
					pageNumber = 0;
				}
				GUI.enabled = false;
			}
			if (pageNumber < numPages)
			{
				if (GUILayout.Button ("Previous", EditorStyles.miniButtonLeft))
				{
					pageNumber --;
				}
			}
			else
			{
				if (GUILayout.Button ("Restart", EditorStyles.miniButtonLeft))
				{
					pageNumber = 0;
					gameName = string.Empty;
				}
			}
			GUI.enabled = true;
			if (pageNumber < numPages - 1)
			{
				if (pageNumber == 1 && string.IsNullOrEmpty (gameName))
				{
					GUI.enabled = false;
				}
				if (GUILayout.Button ("Next", EditorStyles.miniButtonRight))
				{
					pageNumber ++;
					if (pageNumber == numPages - 1)
					{
						Process ();
						return;
					}
				}
				GUI.enabled = true;
			}
			else
			{
				if (pageNumber == numPages)
				{
					if (GUILayout.Button ("Close", EditorStyles.miniButtonRight))
					{
						NewGameWizardWindow window = (NewGameWizardWindow) EditorWindow.GetWindow (typeof (NewGameWizardWindow));
						pageNumber = 0;
						window.Close ();
						return;
					}
				}
				else
				{
					if (GUILayout.Button ("Finish", EditorStyles.miniButtonRight))
					{
						pageNumber ++;
						Finish ();
						return;
					}
				}
			}
			GUILayout.EndHorizontal ();

			GUI.Label (pageRect, "Page " + (pageNumber + 1) + " of " + (numPages + 1));

			GUILayout.FlexibleSpace ();
			CustomGUILayout.EndVertical ();
		}


		private string GetTitle ()
		{
			if (pageNumber == 1)
			{
				return "Game name";
			}
			else if (pageNumber == 2)
			{
				return "Camera perspective";
			}
			else if (pageNumber == 3)
			{
				return "Interface";
			}
			else if (pageNumber == 4)
			{
				return "GUI system";
			}
			else if (pageNumber == 5)
			{
				return "Confirm choices";
			}
			else if (pageNumber == 6)
			{
				return "Complete";
			}

			return string.Empty;
		}


		private void Finish ()
		{
			if (!references)
			{
				GetReferences ();
			}
			
			if (!references)
			{
				return;
			}

			string managerPath = gameName + "/Managers";
			try
			{
				System.IO.Directory.CreateDirectory (Application.dataPath + "/" + managerPath);
			}
			catch (System.Exception e)
			{
				ACDebug.LogError ("Wizard aborted - Could not create directory: " + Application.dataPath + "/" + managerPath + ". Please make sure the Assets direcrory is writeable, and that the intended game name contains no special characters.");
				Debug.LogException (e, this);
				pageNumber --;
				return;
			}

			try
			{
				ShowProgress (0f);

				SceneManager newSceneManager = CustomAssetUtility.CreateAsset<SceneManager> ("SceneManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/SceneManager.asset", gameName + "_SceneManager");
				references.sceneManager = newSceneManager;

				ShowProgress (0.1f);

				SettingsManager newSettingsManager = CustomAssetUtility.CreateAsset<SettingsManager> ("SettingsManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/SettingsManager.asset", gameName + "_SettingsManager");

				newSettingsManager.saveFileName = gameName;
				newSettingsManager.separateEditorSaveFiles = true;
				newSettingsManager.cameraPerspective = cameraPerspective;
				newSettingsManager.movingTurning = movingTurning;
				newSettingsManager.movementMethod = movementMethod;
				newSettingsManager.inputMethod = inputMethod;
				newSettingsManager.interactionMethod = interactionMethod;
				newSettingsManager.hotspotDetection = hotspotDetection;
				if (cameraPerspective == CameraPerspective.TwoPointFiveD)
				{
					newSettingsManager.aspectRatioEnforcement = AspectRatioEnforcement.Fixed;
				}
				references.settingsManager = newSettingsManager;

				ShowProgress (0.2f);

				ActionsManager newActionsManager = CustomAssetUtility.CreateAsset<ActionsManager> ("ActionsManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/ActionsManager.asset", gameName + "_ActionsManager");
				ActionsManager defaultActionsManager = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Default/Default_ActionsManager.asset", typeof(ActionsManager)) as ActionsManager;
				if (defaultActionsManager != null)
				{
					newActionsManager.defaultClass = defaultActionsManager.defaultClass;
					newActionsManager.defaultClassName = defaultActionsManager.defaultClassName;
				}
				references.actionsManager = newActionsManager;
				AdventureCreator.RefreshActions ();

				ShowProgress (0.3f);

				VariablesManager newVariablesManager = CustomAssetUtility.CreateAsset<VariablesManager> ("VariablesManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/VariablesManager.asset", gameName + "_VariablesManager");
				references.variablesManager = newVariablesManager;

				ShowProgress (0.4f);

				InventoryManager newInventoryManager = CustomAssetUtility.CreateAsset<InventoryManager> ("InventoryManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/InventoryManager.asset", gameName + "_InventoryManager");
				references.inventoryManager = newInventoryManager;

				ShowProgress (0.5f);

				SpeechManager newSpeechManager = CustomAssetUtility.CreateAsset<SpeechManager> ("SpeechManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/SpeechManager.asset", gameName + "_SpeechManager");
				newSpeechManager.ClearLanguages ();
				references.speechManager = newSpeechManager;

				ShowProgress (0.6f);

				CursorManager newCursorManager = CustomAssetUtility.CreateAsset<CursorManager> ("CursorManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/CursorManager.asset", gameName + "_CursorManager");
				references.cursorManager = newCursorManager;

				ShowProgress (0.7f);

				MenuManager newMenuManager = CustomAssetUtility.CreateAsset<MenuManager> ("MenuManager", managerPath);
				AssetDatabase.RenameAsset ("Assets/" + managerPath + "/MenuManager.asset", gameName + "_MenuManager");
				references.menuManager = (MenuManager) newMenuManager;

				CursorManager defaultCursorManager = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Default/Default_CursorManager.asset", typeof(CursorManager)) as CursorManager;
				if (wizardMenu == WizardMenu.Blank)
				{
					if (defaultCursorManager != null)
					{
						CursorIcon useIcon = new CursorIcon ();
						useIcon.Copy (defaultCursorManager.cursorIcons[0], false);
						newCursorManager.cursorIcons.Add (useIcon);

						if (defaultCursorManager.uiCursorPrefab)
						{
							CreateUICursorPrefab (defaultCursorManager.uiCursorPrefab, newCursorManager, "Assets/" + gameName + "/" + defaultCursorManager.uiCursorPrefab.name + ".prefab");
						}

						EditorUtility.SetDirty (newCursorManager);
					}
				}
				else
				{
					System.IO.Directory.CreateDirectory (Application.dataPath + "/" + gameName + "/UI");

					if (defaultCursorManager)
					{
						foreach (CursorIcon defaultIcon in defaultCursorManager.cursorIcons)
						{
							CursorIcon newIcon = new CursorIcon ();
							newIcon.Copy (defaultIcon, false);
							newCursorManager.cursorIcons.Add (newIcon);
						}

						if (defaultCursorManager.uiCursorPrefab)
						{
							CreateUICursorPrefab (defaultCursorManager.uiCursorPrefab, newCursorManager, "Assets/" + gameName + "/UI/" + defaultCursorManager.uiCursorPrefab.name + ".prefab");
						}

						CursorIconBase pointerIcon = new CursorIconBase ();
						pointerIcon.Copy (defaultCursorManager.pointerIcon);
						newCursorManager.pointerIcon = pointerIcon;

						newCursorManager.lookCursor_ID = defaultCursorManager.lookCursor_ID;
					}
					else
					{
						ACDebug.LogWarning ("Cannot find Default_CursorManager asset to copy from!");
					}
						
					newCursorManager.allowMainCursor = true;
					EditorUtility.SetDirty (newCursorManager);

					MenuManager defaultMenuManager = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Default/Default_MenuManager.asset", typeof(MenuManager)) as MenuManager;
					if (defaultMenuManager != null)
					{
						#if UNITY_EDITOR
						newMenuManager.drawOutlines = defaultMenuManager.drawOutlines;
						newMenuManager.drawInEditor = defaultMenuManager.drawInEditor;
						#endif
						newMenuManager.pauseTexture = defaultMenuManager.pauseTexture;

						foreach (Menu defaultMenu in defaultMenuManager.menus)
						{
							float progress = (float) defaultMenuManager.menus.IndexOf (defaultMenu) / (float) defaultMenuManager.menus.Count;
							ShowProgress ((progress * 0.3f) + 0.7f);

							Menu newMenu = ScriptableObject.CreateInstance <Menu>();
							newMenu.Copy (defaultMenu, true, true);
							newMenu.Recalculate ();

							if (wizardMenu == WizardMenu.DefaultAC)
							{
								newMenu.menuSource = MenuSource.AdventureCreator;
							}
							else if (wizardMenu == WizardMenu.DefaultUnityUI)
							{
								newMenu.menuSource = MenuSource.UnityUiPrefab;
							}

							if (newMenu.pauseWhenEnabled)
							{
								bool autoSelectUI = (inputMethod == InputMethod.KeyboardOrController);
								newMenu.autoSelectFirstVisibleElement = autoSelectUI;
							}

							if (defaultMenu.PrefabCanvas)
							{
								string oldPath = AssetDatabase.GetAssetPath (defaultMenu.PrefabCanvas.gameObject);
								string newPath = "Assets/" + gameName + "/UI/" + defaultMenu.PrefabCanvas.name + ".prefab";

								if (AssetDatabase.CopyAsset (oldPath, newPath))
								{
									AssetDatabase.ImportAsset (newPath);
									GameObject canvasObNewPrefab = (GameObject) AssetDatabase.LoadAssetAtPath (newPath, typeof(GameObject));
									newMenu.PrefabCanvas = canvasObNewPrefab.GetComponent <Canvas>();
								}
								else
								{
									newMenu.PrefabCanvas = null;
									ACDebug.LogWarning ("Could not copy asset " + oldPath + " to " + newPath, defaultMenu.PrefabCanvas.gameObject);
								}
								newMenu.rectTransform = null;
							}

							foreach (MenuElement newElement in newMenu.elements)
							{
								if (newElement != null)
								{
									AssetDatabase.AddObjectToAsset (newElement, newMenuManager);
									newElement.hideFlags = HideFlags.HideInHierarchy;
								}
								else
								{
									ACDebug.LogWarning ("Null element found in " + newMenu.title + " - the interface may not be set up correctly.");
								}
							}

							if (newMenu != null)
							{
								AssetDatabase.AddObjectToAsset (newMenu, newMenuManager);
								newMenu.hideFlags = HideFlags.HideInHierarchy;

								newMenuManager.menus.Add (newMenu);
							}
							else
							{
								ACDebug.LogWarning ("Unable to create new Menu from original '" + defaultMenu.title + "'");
							}
						}

						EditorUtility.SetDirty (newMenuManager);

						if (newMenuManager.menus.Count != defaultMenuManager.menus.Count)
						{
							ACDebug.LogWarning ("Menu mismatch detected - not all Menus were created by the New Game Wizard - you may wish to delete the new Managers and run the Wizard again.");
						}

						if (newSpeechManager != null)
						{
							newSpeechManager.previewMenuName = "Subtitles";
							EditorUtility.SetDirty (newSpeechManager);
						}

						if (wizardMenu != WizardMenu.Blank)
						{
							System.IO.Directory.CreateDirectory (Application.dataPath + "/" + gameName + "/UI/ActionLists");
						}

						string actionListPath = gameName + "/UI/ActionLists";

						ActionListAsset asset_quitButton = CreateActionList_QuitButton (actionListPath);
						ActionListAsset asset_deselectInventory = CreateActionList_DeselectInventory (actionListPath);
						Menu pauseMenu = newMenuManager.GetMenuWithName ("Pause");
						if (pauseMenu)
						{
							pauseMenu.actionListOnTurnOn = asset_deselectInventory;

							MenuElement quitElement = pauseMenu.GetElementWithName ("QuitButton");
							if (quitElement && quitElement is MenuButton)
							{
								MenuButton quitButton = quitElement as MenuButton;
								quitButton.actionList = asset_quitButton;
							}
						}

						ActionListAsset asset_createNewProfile = CreateActionList_CreateNewProfile (actionListPath);
						ActionListAsset asset_deleteActiveProfile = CreateActionList_DeleteActiveProfile (actionListPath);
						ActionListAsset asset_setupProfilesMenu = CreateActionList_SetupProfilesMenu (actionListPath);
						Menu profilesMenu = newMenuManager.GetMenuWithName ("Profiles");
						if (profilesMenu)
						{
							profilesMenu.actionListOnTurnOn = asset_setupProfilesMenu;

							MenuElement newElement = profilesMenu.GetElementWithName ("NewButton");
							if (newElement && newElement is MenuButton)
							{
								MenuButton newButton = newElement as MenuButton;
								newButton.actionList = asset_createNewProfile;
							}

							MenuElement deleteElement = profilesMenu.GetElementWithName ("DeleteActiveProfileButton");
							if (deleteElement && deleteElement is MenuButton)
							{
								MenuButton deleteButton = deleteElement as MenuButton;
								deleteButton.actionList = asset_deleteActiveProfile;
							}
						}

						ActionListAsset asset_closeCrafting = CreateActionList_CloseCrafting (actionListPath);
						ActionListAsset asset_doCrafting = CreateActionList_DoCrafting (actionListPath);
						Menu craftingMenu = newMenuManager.GetMenuWithName ("Crafting");
						if (craftingMenu)
						{
							MenuElement closeElement = craftingMenu.GetElementWithName ("CloseButton");
							if (closeElement && closeElement is MenuButton)
							{
								MenuButton closeButton = closeElement as MenuButton;
								closeButton.actionList = asset_closeCrafting;
							}

							MenuElement createElement = craftingMenu.GetElementWithName ("CreateButton");
							if (createElement && createElement is MenuButton)
							{
								MenuButton createButton = createElement as MenuButton;
								createButton.actionList = asset_doCrafting;
							}
						}

						ActionListAsset asset_hideSelectedObjective = CreateActionList_HideSelectedObjective (actionListPath);
						ActionListAsset asset_showSelectedObjective = CreateActionList_ShowSelectedObjective (actionListPath);
						Menu objectivesMenu = newMenuManager.GetMenuWithName ("Objectives");
						if (objectivesMenu)
						{
							objectivesMenu.actionListOnTurnOn = asset_hideSelectedObjective;

							MenuElement objectivesElement = objectivesMenu.GetElementWithName ("ObjectivesList");
							if (objectivesElement && objectivesElement is MenuInventoryBox)
							{
								MenuInventoryBox objectivesList = objectivesElement as MenuInventoryBox;
								objectivesList.actionListOnClick = asset_showSelectedObjective;
							}
						}

						ActionListAsset asset_takeAllContainerItems = CreateActionList_TakeAllContainerItems (actionListPath);
						Menu containerMenu = newMenuManager.GetMenuWithName ("Container");
						if (containerMenu)
						{
							MenuElement takeElement = containerMenu.GetElementWithName ("TakeAllButton");
							if (takeElement && takeElement is MenuButton)
							{
								MenuButton takeButton = takeElement as MenuButton;
								takeButton.actionList = asset_takeAllContainerItems;
							}
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot find Default_MenuManager asset to copy from!");
					}
				}

				EditorUtility.ClearProgressBar ();
				ManagerPackage newManagerPackage = CreateManagerPackage (gameName, newSceneManager, newSettingsManager, newActionsManager, newVariablesManager, newInventoryManager, newSpeechManager, newCursorManager, newMenuManager);

				AssetDatabase.SaveAssets ();

				if (newManagerPackage == null || !newManagerPackage.IsFullyAssigned ())
				{
					EditorUtility.DisplayDialog ("Wizard failed", "The New Game Wizard failed to generate a new 'Manager Package' file with all eight Managers assigned. Check your '/Assets/" + gameName + "/Managers' directory - the Managers may have been created, and just need assigning in the ManagerPackage asset Inspector, found in '/Assets/" + gameName + "'.", "OK");
				}
				else if (GameObject.FindObjectOfType <KickStarter>() == null)
				{
					bool initScene = EditorUtility.DisplayDialog ("Organise scene?", "Process complete.  Would you like to organise the scene objects to begin working?  This can be done at any time within the Scene Manager.", "Yes", "No");
					if (initScene)
					{
						newSceneManager.InitialiseObjects ();
					}
				}
			}
			catch (System.Exception e)
			{
				ACDebug.LogWarning ("Could not create Manager. Does the subdirectory " + managerPath + " exist?");
				Debug.LogException (e, this);
				pageNumber --;
			}
		}


		private void CreateUICursorPrefab (GameObject uiCursorPrefab, CursorManager newCursorManager, string newPath)
		{
			if (uiCursorPrefab)
			{
				string oldPath = AssetDatabase.GetAssetPath (uiCursorPrefab.gameObject);

				if (AssetDatabase.CopyAsset (oldPath, newPath))
				{
					AssetDatabase.ImportAsset(newPath);
					GameObject uiCursorNewPrefab = (GameObject) AssetDatabase.LoadAssetAtPath (newPath, typeof (GameObject));
					newCursorManager.uiCursorPrefab = uiCursorNewPrefab;
				}
				else
				{
					newCursorManager.uiCursorPrefab = null;
					ACDebug.LogWarning("Could not copy asset " + oldPath + " to " + newPath);
				}
			}
		}


		private void ShowProgress (float progress)
		{
			EditorUtility.DisplayProgressBar ("Generating Managers", "Please wait while your Manager asset files are created.", progress);
		}


		private void Process ()
		{
			if (cameraPerspective_int == 0)
			{
				cameraPerspective = CameraPerspective.TwoD;
				if (screenSpace)
				{
					movingTurning = MovingTurning.ScreenSpace;
				}
				else
				{
					movingTurning = MovingTurning.Unity2D;
				}

				movementMethod = MovementMethod.PointAndClick;
				inputMethod = InputMethod.MouseAndKeyboard;
				hotspotDetection = HotspotDetection.MouseOver;
			}
			else if (cameraPerspective_int == 1)
			{
				if (oneScenePerBackground)
				{
					cameraPerspective = CameraPerspective.TwoD;
					movingTurning = MovingTurning.ScreenSpace;
					movementMethod = MovementMethod.PointAndClick;
					inputMethod = InputMethod.MouseAndKeyboard;
					hotspotDetection = HotspotDetection.MouseOver;
				}
				else
				{
					cameraPerspective = CameraPerspective.TwoPointFiveD;

					if (directControl)
					{
						movementMethod = MovementMethod.Direct;
						inputMethod = InputMethod.KeyboardOrController;
						hotspotDetection = HotspotDetection.PlayerVicinity;
					}
					else
					{
						movementMethod = MovementMethod.PointAndClick;
						inputMethod = InputMethod.MouseAndKeyboard;
						hotspotDetection = HotspotDetection.MouseOver;
					}
				}
			}
			else if (cameraPerspective_int == 2)
			{
				cameraPerspective = CameraPerspective.ThreeD;
				hotspotDetection = HotspotDetection.MouseOver;

				inputMethod = InputMethod.MouseAndKeyboard;
				if (movementMethod == MovementMethod.Drag)
				{
					if (touchScreen)
					{
						inputMethod = InputMethod.TouchScreen;
					}
					else
					{
						inputMethod = InputMethod.MouseAndKeyboard;
					}
				}
			}

			#if ON_MOBILE
			inputMethod = InputMethod.TouchScreen;
			#endif
		}


		private void ShowPage ()
		{
			GUI.skin.label.wordWrap = true;

			if (pageNumber == 0)
			{
				if (Resource.ACLogo)
				{
					GUI.DrawTexture (new Rect (82, 25, 256, 128), Resource.ACLogo);
				}
				GUILayout.Space (140f);
				GUILayout.Label ("New Game Wizard", CustomStyles.managerHeader);

				GUILayout.Space (5f);
				GUILayout.Label ("This window can help you get started with making a new Adventure Creator game.");
				GUILayout.Label ("To begin, click 'Next'. Changes will not be implemented until you are finished.");
			}

			else if (pageNumber == 1)
			{
				GUILayout.Label ("Enter a name for your game. This will be used for filenames, so alphanumeric characters only.");
				gameName = GUILayout.TextField (gameName);
			}
			
			else if (pageNumber == 2)
			{
				GUILayout.Label ("What kind of perspective will your game have?");
				cameraPerspective_int = EditorGUILayout.Popup (cameraPerspective_int, cameraPerspective_list);

				if (cameraPerspective_int == 0)
				{
					GUILayout.Space (5f);
					GUILayout.Label ("By default, 2D games are built entirely in the X-Y plane, and characters are scaled to achieve a depth effect.\nIf you prefer, you can position your characters in 3D space, so that they scale accurately due to camera perspective.");
					screenSpace = EditorGUILayout.ToggleLeft ("I'll position my characters in 3D space", screenSpace);
				}
				else if (cameraPerspective_int == 1)
				{
					GUILayout.Space (5f);
					GUILayout.Label ("2.5D games mixes 3D characters with 2D backgrounds. By default, 2.5D games group several backgrounds into one scene, and swap them out according to the camera angle.\nIf you prefer, you can work with just one background in a scene, to create a more traditional 2D-like adventure.");
					oneScenePerBackground = EditorGUILayout.ToggleLeft ("I'll work with one background per scene", oneScenePerBackground);
				}
				else if (cameraPerspective_int == 2)
				{
					GUILayout.Label ("3D games can still have sprite-based Characters, but having a true 3D environment is more flexible so far as Player control goes. How should your Player character be controlled?");
					movementMethod = (MovementMethod) EditorGUILayout.EnumPopup (movementMethod);
				}
			}

			else if (pageNumber == 3)
			{
				if (cameraPerspective_int == 1 && !oneScenePerBackground)
				{
					GUILayout.Label ("Do you want to play the game ONLY with a keyboard or controller?");
					directControl = EditorGUILayout.ToggleLeft ("Yes", directControl);
					GUILayout.Space (5f);
				}
				else if (cameraPerspective_int == 2 && movementMethod == MovementMethod.Drag)
				{
					GUILayout.Label ("Is your game designed for Touch-screen devices?");
					touchScreen = EditorGUILayout.ToggleLeft ("Yes", touchScreen);
					GUILayout.Space (5f);
				}

				GUILayout.Label ("How do you want to interact with Hotspots?");
				interactionMethod = (AC_InteractionMethod) EditorGUILayout.EnumPopup (interactionMethod);
				if (interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					EditorGUILayout.HelpBox ("This method simplifies interactions to either Use, Examine, or Use Inventory. Hotspots can be interacted with in just one click.", MessageType.Info);
				}
				else if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					EditorGUILayout.HelpBox ("This method emulates the classic 'Sierra-style' interface, in which the player chooses from a list of verbs, and then the Hotspot they wish to interact with.", MessageType.Info);
				}
				else if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					EditorGUILayout.HelpBox ("This method involves first choosing a Hotspot, and then from a range of available interactions, which can be customised in the Editor.", MessageType.Info);
				}
				else if (interactionMethod == AC_InteractionMethod.CustomScript)
				{
					EditorGUILayout.HelpBox ("See the Manual's 'Custom interaction systems' section for information on how to trigger Hotspots and inventory items.", MessageType.Info);
				}
			}

			else if (pageNumber == 4)
			{
				GUILayout.Label ("Please choose what interface you would like to start with. It can be changed at any time - this is just to help you get started.");
				wizardMenu = (WizardMenu) EditorGUILayout.EnumPopup (wizardMenu);

				if (wizardMenu == WizardMenu.DefaultAC || wizardMenu == WizardMenu.DefaultUnityUI)
				{
					MenuManager defaultMenuManager = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Default/Default_MenuManager.asset", typeof(MenuManager)) as MenuManager;
					CursorManager defaultCursorManager = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Default/Default_CursorManager.asset", typeof(CursorManager)) as CursorManager;
					ActionsManager defaultActionsManager = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Default/Default_ActionsManager.asset", typeof(ActionsManager)) as ActionsManager;

					if (defaultMenuManager == null || defaultCursorManager == null || defaultActionsManager == null)
					{
						EditorGUILayout.HelpBox ("Unable to locate the default Manager assets in '" + Resource.MainFolderPath + "/Default'. These assets must be imported in order to start with the default interface.", MessageType.Warning);
					}
				}

				if (wizardMenu == WizardMenu.Blank)
				{
					EditorGUILayout.HelpBox ("Your interface will be completely blank - no cursor icons will exist either.\r\n\r\nThis option is not recommended for those still learning how to use AC.", MessageType.Info);
				}
				else if (wizardMenu == WizardMenu.DefaultAC)
				{
					EditorGUILayout.HelpBox ("This mode uses AC's built-in Menu system and not Unity UI.\r\n\r\nUnity UI prefabs will also be created for each Menu, however, so that you can make use of them later if you choose.", MessageType.Info);
				}
				else if (wizardMenu == WizardMenu.DefaultUnityUI)
				{
					EditorGUILayout.HelpBox ("This mode relies on Unity UI to handle the interface.\r\n\r\nCopies of the UI prefabs will be stored in a UI subdirectory, for you to edit.", MessageType.Info);
				}
			}

			else if (pageNumber == 5)
			{
				GUILayout.Label ("The following values have been set based on your choices. Please review them and amend if necessary, then click 'Finish' to create your game template.");
				GUILayout.Space (5f);

				gameName = EditorGUILayout.TextField ("Game name:", gameName);
				cameraPerspective_int = (int) cameraPerspective;
				cameraPerspective_int = EditorGUILayout.Popup ("Camera perspective:", cameraPerspective_int, cameraPerspective_list);
				cameraPerspective = (CameraPerspective) cameraPerspective_int;

				if (cameraPerspective == CameraPerspective.TwoD)
				{
					movingTurning = (MovingTurning) EditorGUILayout.EnumPopup ("Moving and turning:", movingTurning);
				}

				movementMethod = (MovementMethod) EditorGUILayout.EnumPopup ("Movement method:", movementMethod);
				inputMethod = (InputMethod) EditorGUILayout.EnumPopup ("Input method:", inputMethod);
				interactionMethod = (AC_InteractionMethod) EditorGUILayout.EnumPopup ("Interaction method:", interactionMethod);
				hotspotDetection = (HotspotDetection) EditorGUILayout.EnumPopup ("Hotspot detection:", hotspotDetection);

				wizardMenu = (WizardMenu) EditorGUILayout.EnumPopup ("GUI type:", wizardMenu);
			}

			else if (pageNumber == 6)
			{
				GUILayout.Label ("Your game's Managers have been set up!");
				GUILayout.Space (5f);
				GUILayout.Label ("Now you can use the AC Game Editor to start building your game.  For a step-by-step guide, follow the link below:");

				if (GUILayout.Button ("Tutorial: The Game Editor window", CustomStyles.linkCentre))
				{
					Application.OpenURL (Resource.introTutorialLink);
				}
			}
		}


		private ManagerPackage CreateManagerPackage (string folder, SceneManager sceneManager, SettingsManager settingsManager, ActionsManager actionsManager, VariablesManager variablesManager, InventoryManager inventoryManager, SpeechManager speechManager, CursorManager cursorManager, MenuManager menuManager)
		{
			ManagerPackage managerPackage = CustomAssetUtility.CreateAsset<ManagerPackage> ("ManagerPackage", folder);
			AssetDatabase.RenameAsset ("Assets/" + folder + "/ManagerPackage.asset", folder + "_ManagerPackage");

			managerPackage.sceneManager = sceneManager;
			managerPackage.settingsManager = settingsManager;
			managerPackage.actionsManager = actionsManager;
			managerPackage.variablesManager = variablesManager;

			managerPackage.inventoryManager = inventoryManager;
			managerPackage.speechManager = speechManager;
			managerPackage.cursorManager = cursorManager;
			managerPackage.menuManager = menuManager;

			managerPackage.AssignManagers ();
			EditorUtility.SetDirty (managerPackage);
			AssetDatabase.SaveAssets ();

			AdventureCreator.Init ();
			
			return managerPackage;
		}


		private ActionListAsset CreateActionList_CloseCrafting (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionInventoryCrafting.CreateNew (ActionInventoryCrafting.ActionCraftingMethod.ClearRecipe),
				ActionMenuState.CreateNew_TurnOffMenu ("Crafting"),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("CloseCrafting", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;
			
			return newAsset;
		}


		private ActionListAsset CreateActionList_CreateNewProfile (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionManageProfiles.CreateNew_CreateProfile (),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("CreateNewProfile", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_DeleteActiveProfile (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionManageProfiles.CreateNew_DeleteProfile (DeleteProfileType.ActiveProfile, string.Empty, string.Empty, 0),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("DeleteActiveProfile", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_DeselectInventory (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionInventorySelect.CreateNew_DeselectActive (),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("DeselectInventory", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_DoCrafting (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionInventoryCrafting.CreateNew (ActionInventoryCrafting.ActionCraftingMethod.CreateRecipe),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("DoCrafting", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_HideSelectedObjective (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedTitle", false),
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedStateType", false),
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedDescription", false),
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedTexture", false),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("HideSelectedObjective", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_QuitButton (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionEndGame.CreateNew_QuitGame (),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("QuitButton", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_SetupProfilesMenu (string folderPath)
		{
			ActionSaveCheck saveCheck1 = ActionSaveCheck.CreateNew_NumberOfProfiles (1, IntCondition.MoreThan);

			ActionMenuState hideDeleteButton = ActionMenuState.CreateNew_SetElementVisibility ("Profiles", "DeleteActiveProfileButton", false);

			ActionMenuState showDeleteButton = ActionMenuState.CreateNew_SetElementVisibility ("Profiles", "DeleteActiveProfileButton", true);
			ActionSaveCheck saveCheck2 = ActionSaveCheck.CreateNew_NumberOfProfiles (10, IntCondition.LessThan);

			ActionMenuState hideNewButton = ActionMenuState.CreateNew_SetElementVisibility ("Profiles", "NewButton", false);

			ActionMenuState showNewButton = ActionMenuState.CreateNew_SetElementVisibility ("Profiles", "NewButton", true);

			List<Action> actions = new List<Action>
			{
				saveCheck1,
				hideDeleteButton,
				showDeleteButton,
				saveCheck2,
				hideNewButton,
				showNewButton,
			};

			saveCheck1.SetOutputs (new ActionEnd (showDeleteButton), new ActionEnd (hideDeleteButton));
			hideDeleteButton.SetOutput (new ActionEnd (true));
			showDeleteButton.SetOutput (new ActionEnd (saveCheck2));
			saveCheck2.SetOutputs (new ActionEnd (showNewButton), new ActionEnd (hideNewButton));
			hideNewButton.SetOutput (new ActionEnd (true));
			showNewButton.SetOutput (new ActionEnd (true));

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("SetupProfilesMenu", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_ShowSelectedObjective (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedTitle", true),
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedStateType", true),
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedDescription", true),
				ActionMenuState.CreateNew_SetElementVisibility ("Objectives", "SelectedTexture", true),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("ShowSelectedObjective", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}


		private ActionListAsset CreateActionList_TakeAllContainerItems (string folderPath)
		{
			List<Action> actions = new List<Action>
			{
				ActionContainerSet.CreateNew_RemoveAll (null, true),
				ActionMenuState.CreateNew_TurnOffMenu ("Crafting"),
			};

			ActionListAsset newAsset = ActionListAsset.CreateFromActions ("TakeAllContainerItems", folderPath, actions);
			newAsset.actionListType = ActionListType.RunInBackground;

			return newAsset;
		}

	}

}

#endif