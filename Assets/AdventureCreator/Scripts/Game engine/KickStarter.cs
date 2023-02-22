/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"KickStarter.cs"
 * 
 *	This script will make sure that PersistentEngine and the Player gameObjects are always created,
 *	regardless of which scene the game is begun from.  It will also check the key gameObjects for
 *	essential scripts and references.
 * 
 */

using UnityEngine;

namespace AC
{
	
	/**
	 * This component instantiates the PersistentEngine and Player prefabs when the game beings.
	 * It also provides static references to each of Adventure Creator's main components.
	 * It should be attached to the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_kick_starter.html")]
	public class KickStarter : MonoBehaviour
	{
		
		private static Player playerPrefab = null;
		private static MainCamera mainCameraPrefab = null;
		private static Camera cameraMain = null;
		private static GameObject persistentEnginePrefab = null;
		private static GameObject gameEnginePrefab = null;
		
		// Managers
		private static SceneManager sceneManagerPrefab = null;
		private static SettingsManager settingsManagerPrefab = null;
		private static ActionsManager actionsManagerPrefab = null;
		private static VariablesManager variablesManagerPrefab = null;
		private static InventoryManager inventoryManagerPrefab = null;
		private static SpeechManager speechManagerPrefab = null;
		private static CursorManager cursorManagerPrefab = null;
		private static MenuManager menuManagerPrefab = null;
		
		// PersistentEngine components
		private static Options optionsComponent = null;
		private static RuntimeInventory runtimeInventoryComponent = null;
		private static RuntimeVariables runtimeVariablesComponent = null;
		private static PlayerMenus playerMenusComponent = null;
		private static StateHandler stateHandlerComponent = null;
		private static SceneChanger sceneChangerComponent = null;
		private static SaveSystem saveSystemComponent = null;
		private static LevelStorage levelStorageComponent = null;
		private static RuntimeLanguages runtimeLanguagesComponent = null;
		private static RuntimeDocuments runtimeDocumentsComponent = null;
		private static RuntimeObjectives runtimeObjectivesComponent = null;
		private static ActionListAssetManager actionListAssetManagerComponent = null;
		
		// GameEngine components
		private static MenuSystem menuSystemComponent = null;
		private static Dialog dialogComponent = null;
		private static PlayerInput playerInputComponent = null;
		private static PlayerInteraction playerInteractionComponent = null;
		private static PlayerMovement playerMovementComponent = null;
		private static PlayerCursor playerCursorComponent = null;
		private static PlayerQTE playerQTEComponent = null;
		private static SceneSettings sceneSettingsComponent = null;
		private static NavigationManager navigationManagerComponent = null;
		private static ActionListManager actionListManagerComponent = null;
		private static LocalVariables localVariablesComponent = null;
		private static MenuPreview menuPreviewComponent = null;
		private static EventManager eventManagerComponent = null;
		private static KickStarter kickStarterComponent = null;


		protected void Awake ()
		{
			if (GetComponent <MultiSceneChecker>() == null)
			{
				ACDebug.LogError ("A 'MultiSceneChecker' component must be attached to the GameEngine prefab - please re-import AC.", gameObject);
			}
		}


		public static void SetGameEngine (GameObject _gameEngine = null)
		{
			if (_gameEngine)
			{
				gameEnginePrefab = _gameEngine;

				menuSystemComponent = null;
				playerCursorComponent = null;
				playerInputComponent = null;
				playerInteractionComponent = null;
				playerMovementComponent = null;
				playerMenusComponent = null;
				playerQTEComponent = null;
				kickStarterComponent = null;
				sceneSettingsComponent = null;
				dialogComponent = null;
				menuPreviewComponent = null;
				navigationManagerComponent = null;
				actionListManagerComponent = null;
				localVariablesComponent = null;
				eventManagerComponent = null;

				return;
			}

			if (gameEnginePrefab == null)
			{
				SceneSettings sceneSettings = UnityVersionHandler.GetKickStarterComponent <SceneSettings>();
				if (sceneSettings)
				{
					gameEnginePrefab = sceneSettings.gameObject;
				}
			}
		}


		private static bool SetPersistentEngine ()
		{
			if (persistentEnginePrefab == null)
			{
				StateHandler stateHandler = UnityVersionHandler.GetKickStarterComponent <StateHandler>();
				
				if (stateHandler)
				{
					persistentEnginePrefab = stateHandler.gameObject;
				}
				else
				{
					GameObject newPersistentEngine = null;

					if (settingsManager == null || settingsManager.spawnPersistentEnginePrefab)
					{
						try
						{
							newPersistentEngine = (GameObject) Instantiate (Resources.Load (Resource.persistentEngine));
							if (newPersistentEngine)
							{
								newPersistentEngine.name = Resource.persistentEngine;
							}
						}
						catch
						{}

						if (newPersistentEngine == null)
						{
							ACDebug.LogWarning ("Could not locate Resources/PersistentEngine prefab - generating from scratch.");
						}
					}
					
					if (newPersistentEngine == null)
					{
						newPersistentEngine = new GameObject ("PersistentEngine");
						optionsComponent = newPersistentEngine.AddComponent <Options>();
						runtimeInventoryComponent = newPersistentEngine.AddComponent <RuntimeInventory>();
						runtimeVariablesComponent = newPersistentEngine.AddComponent <RuntimeVariables>();
						stateHandlerComponent = newPersistentEngine.AddComponent <StateHandler>();
						sceneChangerComponent = newPersistentEngine.AddComponent <SceneChanger>();
						saveSystemComponent = newPersistentEngine.AddComponent <SaveSystem>();
						levelStorageComponent = newPersistentEngine.AddComponent <LevelStorage>();
						playerMenusComponent = newPersistentEngine.AddComponent <PlayerMenus>();
						runtimeLanguagesComponent = newPersistentEngine.AddComponent <RuntimeLanguages>();
						actionListAssetManagerComponent = newPersistentEngine.AddComponent <ActionListAssetManager>();
						runtimeDocumentsComponent = newPersistentEngine.AddComponent <RuntimeDocuments>();
						runtimeObjectivesComponent = newPersistentEngine.AddComponent <RuntimeObjectives>();
					}

		 			if (newPersistentEngine)
		 			{
						#if UNITY_EDITOR
						if (!TestPersistentEngine (newPersistentEngine))
						{
							return false;
						}
						#endif

						persistentEnginePrefab = newPersistentEngine;

						stateHandler = persistentEnginePrefab.GetComponent <StateHandler>();
						stateHandler.Initialise ();
						return true;
					}
				}
			}

			if (stateHandler)
			{
				stateHandler.RegisterInitialConstantIDs ();
			}
			return true;
		}


		public void CheckRequiredManagerPackage (ManagerPackage requiredManagerPackage)
		{
			if (requiredManagerPackage == null)
			{
				return;
			}

			#if UNITY_EDITOR

			if ((requiredManagerPackage.sceneManager && requiredManagerPackage.sceneManager != KickStarter.sceneManager) ||
				(requiredManagerPackage.settingsManager && requiredManagerPackage.settingsManager != KickStarter.settingsManager) ||
				(requiredManagerPackage.actionsManager && requiredManagerPackage.actionsManager != KickStarter.actionsManager) ||
				(requiredManagerPackage.variablesManager && requiredManagerPackage.variablesManager != KickStarter.variablesManager) ||
				(requiredManagerPackage.inventoryManager && requiredManagerPackage.inventoryManager != KickStarter.inventoryManager) ||
				(requiredManagerPackage.speechManager && requiredManagerPackage.speechManager != KickStarter.speechManager) ||
				(requiredManagerPackage.cursorManager && requiredManagerPackage.cursorManager != KickStarter.cursorManager) ||
				(requiredManagerPackage.menuManager && requiredManagerPackage.menuManager != KickStarter.menuManager))
			{
				if (requiredManagerPackage.settingsManager)
				{
					if (requiredManagerPackage.settingsManager.name == "Demo_SettingsManager" && UnityVersionHandler.GetCurrentSceneName () == "Basement")
					{
						ACDebug.LogWarning ("The demo scene's required Manager asset files are not all loaded - please stop the game, and choose 'Adventure Creator -> Getting started -> Load 3D Demo managers from the top toolbar, and re-load the scene.", requiredManagerPackage);
						return;
					}
					else if (requiredManagerPackage.settingsManager.name == "Demo2D_SettingsManager" && UnityVersionHandler.GetCurrentSceneName () == "Park")
					{
						ACDebug.LogWarning ("The 2D demo scene's required Manager asset files are not all loaded - please stop the game, and choose 'Adventure Creator -> Getting started -> Load 2D Demo managers from the top toolbar, and re-load the scene.", requiredManagerPackage);
						return;
					}
				}

				ACDebug.LogWarning ("This scene's required Manager asset files are not all loaded - please find the asset file '" + requiredManagerPackage.name + "' and click 'Assign managers' in its Inspector.", requiredManagerPackage);
			}

			#endif

		}


		#if UNITY_EDITOR

		private static bool TestPersistentEngine (GameObject _persistentEngine)
		{
			bool testResult = true;

			if (_persistentEngine == null)
			{
				ACDebug.LogError ("No PersistentEngine found - please place one in the Resources directory");
				testResult = false;
			}
			else
			{
				if (_persistentEngine.GetComponent<Options> () == null)
				{
					ACDebug.LogError (persistentEnginePrefab.name + " has no Options component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeInventory> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeInventory component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeVariables> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeVariables component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<PlayerMenus> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no PlayerMenus component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeObjectives> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeObjectives component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<StateHandler> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no StateHandler component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<SceneChanger> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no SceneChanger component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<SaveSystem> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no SaveSystem component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<LevelStorage> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no LevelStorage component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeLanguages> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeLanguages component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<RuntimeDocuments> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no RuntimeDocuments component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
				if (_persistentEngine.GetComponent<ActionListAssetManager> () == null)
				{
					ACDebug.LogError (_persistentEngine.name + " has no ActionListAssetManager component attached. It can be found in /Assets/AdventureCreator/Resources", _persistentEngine);
					testResult = false;
				}
			}

			return testResult;
		}

		#endif


		/**
		 * Clears the internal Manager references.  Call this when changing the assigned Managers, so that other Inspectors/Editors get updated to reflect this
		 */
		public static void ClearManagerCache ()
		{
			sceneManagerPrefab = null;
			settingsManagerPrefab = null;
			actionsManagerPrefab = null;
			variablesManagerPrefab = null;
			inventoryManagerPrefab = null;
			speechManagerPrefab = null;
			cursorManagerPrefab = null;
			menuManagerPrefab = null;
		}
		
		
		public static SceneManager sceneManager
		{
			get
			{
				if (sceneManagerPrefab) return sceneManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().sceneManager)
				{
					sceneManagerPrefab = AdvGame.GetReferences ().sceneManager;
					return sceneManagerPrefab;
				}
				return null;
			}
			set
			{
				sceneManagerPrefab = value;
			}
		}
		
		
		public static SettingsManager settingsManager
		{
			get
			{
				if (settingsManagerPrefab) return settingsManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager)
				{
					settingsManagerPrefab = AdvGame.GetReferences ().settingsManager;
					return settingsManagerPrefab;
				}
				return null;
			}
			set
			{
				settingsManagerPrefab = value;
			}
		}
		
		
		public static ActionsManager actionsManager
		{
			get
			{
				if (actionsManagerPrefab) return actionsManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().actionsManager)
				{
					actionsManagerPrefab = AdvGame.GetReferences ().actionsManager;
					return actionsManagerPrefab;
				}
				return null;
			}
			set
			{
				actionsManagerPrefab = value;
			}
		}
		
		
		public static VariablesManager variablesManager
		{
			get
			{
				if (variablesManagerPrefab) return variablesManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
				{
					variablesManagerPrefab = AdvGame.GetReferences ().variablesManager;
					return variablesManagerPrefab;
				}
				return null;
			}
			set
			{
				variablesManagerPrefab = value;
			}
		}
		
		
		public static InventoryManager inventoryManager
		{
			get
			{
				if (inventoryManagerPrefab) return inventoryManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().inventoryManager)
				{
					inventoryManagerPrefab = AdvGame.GetReferences ().inventoryManager;
					return inventoryManagerPrefab;
				}
				return null;
			}
			set
			{
				inventoryManagerPrefab = value;
			}
		}
		
		
		public static SpeechManager speechManager
		{
			get
			{
				if (speechManagerPrefab) return speechManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager)
				{
					speechManagerPrefab = AdvGame.GetReferences ().speechManager;
					return speechManagerPrefab;
				}
				return null;
			}
			set
			{
				speechManagerPrefab = value;
			}
		}
		
		
		public static CursorManager cursorManager
		{
			get
			{
				if (cursorManagerPrefab) return cursorManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().cursorManager)
				{
					cursorManagerPrefab = AdvGame.GetReferences ().cursorManager;
					return cursorManagerPrefab;
				}
				return null;
			}
			set
			{
				cursorManagerPrefab = value;
			}
		}
		
		
		public static MenuManager menuManager
		{
			get
			{
				if (menuManagerPrefab) return menuManagerPrefab;
				else if (AdvGame.GetReferences () && AdvGame.GetReferences ().menuManager)
				{
					menuManagerPrefab = AdvGame.GetReferences ().menuManager;
					return menuManagerPrefab;
				}
				return null;
			}
			set
			{
				menuManagerPrefab = value;
			}
		}
		
		
		public static Options options
		{
			get
			{
				if (optionsComponent) return optionsComponent;
				else if (persistentEnginePrefab)
				{
					optionsComponent = persistentEnginePrefab.GetComponent <Options>();
					return optionsComponent;
				}
				return null;
			}
		}
		
		
		public static RuntimeInventory runtimeInventory
		{
			get
			{
				if (runtimeInventoryComponent) return runtimeInventoryComponent;
				else if (persistentEnginePrefab)
				{
					runtimeInventoryComponent = persistentEnginePrefab.GetComponent <RuntimeInventory>();
					return runtimeInventoryComponent;
				}
				return null;
			}
		}
		
		
		public static RuntimeVariables runtimeVariables
		{
			get
			{
				if (runtimeVariablesComponent) return runtimeVariablesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeVariablesComponent = persistentEnginePrefab.GetComponent <RuntimeVariables>();
					return runtimeVariablesComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerMenus playerMenus
		{
			get
			{
				if (playerMenusComponent) return playerMenusComponent;
				else if (persistentEnginePrefab)
				{
					playerMenusComponent = persistentEnginePrefab.GetComponent <PlayerMenus>();
					return playerMenusComponent;
				}
				return null;
			}
		}
		
		
		public static StateHandler stateHandler
		{
			get
			{
				if (stateHandlerComponent) return stateHandlerComponent;
				else if (persistentEnginePrefab)
				{
					stateHandlerComponent = persistentEnginePrefab.GetComponent <StateHandler>();
					return stateHandlerComponent;
				}
				return null;
			}
		}
		
		
		public static SceneChanger sceneChanger
		{
			get
			{
				if (sceneChangerComponent) return sceneChangerComponent;
				else if (persistentEnginePrefab)
				{
					sceneChangerComponent = persistentEnginePrefab.GetComponent <SceneChanger>();
					return sceneChangerComponent;
				}
				return null;
			}
		}
		
		
		public static SaveSystem saveSystem
		{
			get
			{
				if (saveSystemComponent) return saveSystemComponent;
				else if (persistentEnginePrefab)
				{
					saveSystemComponent = persistentEnginePrefab.GetComponent <SaveSystem>();
					return saveSystemComponent;
				}
				return null;
			}
		}
		
		
		public static LevelStorage levelStorage
		{
			get
			{
				if (levelStorageComponent) return levelStorageComponent;
				else if (persistentEnginePrefab)
				{
					levelStorageComponent = persistentEnginePrefab.GetComponent <LevelStorage>();
					return levelStorageComponent;
				}
				return null;
			}
		}


		public static RuntimeLanguages runtimeLanguages
		{
			get
			{
				if (runtimeLanguagesComponent) return runtimeLanguagesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeLanguagesComponent = persistentEnginePrefab.GetComponent <RuntimeLanguages>();
					return runtimeLanguagesComponent;
				}
				return null;
			}
		}


		public static RuntimeDocuments runtimeDocuments
		{
			get
			{
				if (runtimeDocumentsComponent) return runtimeDocumentsComponent;
				else if (persistentEnginePrefab)
				{
					runtimeDocumentsComponent = persistentEnginePrefab.GetComponent <RuntimeDocuments>();
					return runtimeDocumentsComponent;
				}
				return null;
			}
		}


		public static RuntimeObjectives runtimeObjectives
		{
			get
			{
				if (runtimeObjectivesComponent) return runtimeObjectivesComponent;
				else if (persistentEnginePrefab)
				{
					runtimeObjectivesComponent = persistentEnginePrefab.GetComponent <RuntimeObjectives>();
					return runtimeObjectivesComponent;
				}
				return null;
			}
		}


		public static ActionListAssetManager actionListAssetManager
		{
			get
			{
				if (actionListAssetManagerComponent) return actionListAssetManagerComponent;
				else if (persistentEnginePrefab)
				{
					actionListAssetManagerComponent = persistentEnginePrefab.GetComponent <ActionListAssetManager>();
					return actionListAssetManagerComponent;
				}
				return null;
			}
		}
		
		
		public static MenuSystem menuSystem
		{
			get
			{
				if (menuSystemComponent) return menuSystemComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					menuSystemComponent = gameEnginePrefab.GetComponent <MenuSystem>();
					return menuSystemComponent;
				}
				return null;
			}
		}
		
		
		public static Dialog dialog
		{
			get
			{
				if (dialogComponent) return dialogComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					dialogComponent = gameEnginePrefab.GetComponent <Dialog>();
					return dialogComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerInput playerInput
		{
			get
			{
				if (playerInputComponent) return playerInputComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerInputComponent = gameEnginePrefab.GetComponent <PlayerInput>();
					return playerInputComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerInteraction playerInteraction
		{
			get
			{
				if (playerInteractionComponent) return playerInteractionComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerInteractionComponent = gameEnginePrefab.GetComponent <PlayerInteraction>();
					return playerInteractionComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerMovement playerMovement
		{
			get
			{
				if (playerMovementComponent) return playerMovementComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerMovementComponent = gameEnginePrefab.GetComponent <PlayerMovement>();
					return playerMovementComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerCursor playerCursor
		{
			get
			{
				if (playerCursorComponent) return playerCursorComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerCursorComponent = gameEnginePrefab.GetComponent <PlayerCursor>();
					return playerCursorComponent;
				}
				return null;
			}
		}
		
		
		public static PlayerQTE playerQTE
		{
			get
			{
				if (playerQTEComponent) return playerQTEComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					playerQTEComponent = gameEnginePrefab.GetComponent <PlayerQTE>();
					return playerQTEComponent;
				}
				return null;
			}
		}
		
		
		public static SceneSettings sceneSettings
		{
			get
			{
				if (sceneSettingsComponent && Application.isPlaying) return sceneSettingsComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					sceneSettingsComponent = gameEnginePrefab.GetComponent <SceneSettings>();
					return sceneSettingsComponent;
				}
				return null;
			}
		}
		
		
		public static NavigationManager navigationManager
		{
			get
			{
				if (navigationManagerComponent) return navigationManagerComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					navigationManagerComponent = gameEnginePrefab.GetComponent <NavigationManager>();
					return navigationManagerComponent;
				}
				return null;
			}
		}
		
		
		public static ActionListManager actionListManager
		{
			get
			{
				if (actionListManagerComponent) 
				{
					return actionListManagerComponent;
				}
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					actionListManagerComponent = gameEnginePrefab.GetComponent <ActionListManager>();
					return actionListManagerComponent;
				}
				return null;
			}
		}
		
		
		public static LocalVariables localVariables
		{
			get
			{
				if (localVariablesComponent) return localVariablesComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					localVariablesComponent = gameEnginePrefab.GetComponent <LocalVariables>();
					return localVariablesComponent;
				}
				return null;
			}
		}
		
		
		public static MenuPreview menuPreview
		{
			get
			{
				if (menuPreviewComponent) return menuPreviewComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					menuPreviewComponent = gameEnginePrefab.GetComponent <MenuPreview>();
					return menuPreviewComponent;
				}
				return null;
			}
		}


		public static EventManager eventManager
		{
			get
			{
				if (eventManagerComponent) return eventManagerComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					eventManagerComponent = gameEnginePrefab.GetComponent <EventManager>();
					return eventManagerComponent;
				}
				return null;
			}
		}


		public static KickStarter kickStarter
		{
			get
			{
				if (kickStarterComponent) return kickStarterComponent;
				else
				{
					SetGameEngine ();
				}
				
				if (gameEnginePrefab)
				{
					kickStarterComponent = gameEnginePrefab.GetComponent <KickStarter>();
					return kickStarterComponent;
				}
				return null;
			}
		}


		public static Music music
		{
			get
			{
				if (stateHandler)
				{
					return stateHandler.GetMusicEngine ();
				}
				return null;
			}
		}
		
		
		public static Player player
		{
			get
			{
				return playerPrefab;
			}
			set
			{
				if (playerPrefab != value)
				{
					if (playerPrefab && playerPrefab.transform.parent == null)
					{
						UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (playerPrefab.gameObject, SceneChanger.CurrentScene);
					}

					playerPrefab = value;
					
					if (playerPrefab)
					{
						if (playerPrefab.IsLocalPlayer ())
						{
							// Remove others
							Player[] allPlayers = FindObjectsOfType<Player> ();
							foreach (Player allPlayer in allPlayers)
							{
								if (allPlayer != playerPrefab)
								{
									allPlayer.RemoveFromScene ();
								}
							}
							
							if (settingsManager.GetDefaultPlayer () != null)
							{
								ACDebug.Log ("Local player " + playerPrefab.GetName () + " found - this will override the default, " + settingsManager.GetDefaultPlayer ().GetName () + ", for the duration of scene " + playerPrefab.gameObject.scene.name, player);
							}
						}
						else if (settingsManager.playerSwitching == PlayerSwitching.Allow)
						{
							PlayerData playerData = saveSystem.GetPlayerData (playerPrefab.ID);

							if (!settingsManager.shareInventory)
							{
								runtimeInventory.SetNull ();
								runtimeInventory.RemoveRecipes ();
								runtimeObjectives.ClearUniqueToPlayer ();

								runtimeInventory.localItems.Clear ();
								runtimeDocuments.ClearCollection ();

								if (playerData != null)
								{
									runtimeInventory.AssignPlayerInventory (InvCollection.LoadData (playerData.inventoryData));
									runtimeDocuments.AssignPlayerDocuments (playerData);
									runtimeObjectives.AssignPlayerObjectives (playerData);
								}
								
								// Menus
								foreach (AC.Menu menu in PlayerMenus.GetMenus ())
								{
									foreach (MenuElement element in menu.elements)
									{
										if (element is MenuInventoryBox)
										{
											MenuInventoryBox invBox = (MenuInventoryBox) element;
											invBox.ResetOffset ();
										}
									}
								}
							}

							if (playerData == null)
							{
								ACDebug.LogWarning ("No PlayerData found for new Player " + playerPrefab, playerPrefab);
							}
							else if (mainCamera)
							{
								mainCamera.LoadData (playerData, false);
							}

							DontDestroyOnLoad (playerPrefab);
						}
						else
						{
							DontDestroyOnLoad (playerPrefab);
						}
						
						stateHandler.IgnoreNavMeshCollisions ();
						stateHandler.UpdateAllMaxVolumes ();
						foreach (_Camera camera in stateHandler.Cameras)
						{
							camera.ResetTarget ();
						}

						saveSystem.CurrentPlayerID = playerPrefab.ID;

						if (eventManager) eventManager.Call_OnSetPlayer (playerPrefab);
					}
				}
			}
		}
		
		
		public static MainCamera mainCamera
		{
			get
			{
				if (mainCameraPrefab)
				{
					return mainCameraPrefab;
				}
				else
				{
					MainCamera _mainCamera = (MainCamera) FindObjectOfType (typeof (MainCamera));
					if (_mainCamera)
					{
						mainCameraPrefab = _mainCamera;
					}
					return mainCameraPrefab;
				}
			}
			set
			{
				if (value)
				{
					mainCameraPrefab = value;
				}
			}
		}


		/**
		 * A cache of Unity's own Camera.main
		 */
		public static Camera CameraMain
		{
			get
			{
				if (KickStarter.settingsManager.cacheCameraMain)
				{
					if (cameraMain == null)
					{
						cameraMain = Camera.main;
						_cameraMainTransform = null;
					}
					return cameraMain;
				}
				return Camera.main;
			}
			set
			{
				if (value)
				{
					cameraMain = value;
					_cameraMainTransform = null;
				}
			}
		}


		private static Transform _cameraMainTransform;
		public static Transform CameraMainTransform
		{
			get
			{
				if (_cameraMainTransform == null && CameraMain)
				{
					_cameraMainTransform = CameraMain.transform;
				}
				return _cameraMainTransform;
			}
		}


		public void Initialise ()
		{
			if (settingsManager.IsInLoadingScene ())
			{
				ACDebug.Log ("Bypassing regular AC startup because the current scene is the 'Loading' scene.");
				return;
			}

			ClearVariables ();
			SetGameEngine (gameObject);

			bool havePersistentEngine = SetPersistentEngine ();
			if (!havePersistentEngine)
			{
				return;
			}

			CheckRequiredManagerPackage (sceneSettings.requiredManagerPackage);

			PreparePlayer ();

			if (mainCamera)
			{
				mainCamera.OnInitGameEngine ();
			}
			else
			{
				ACDebug.LogWarning ("No MainCamera found - please organise the scene at the top of the Scene Manager to create one.");
			}

			playerInput.OnInitGameEngine ();
			localVariables.OnInitGameEngine ();
			sceneSettings.OnInitGameEngine ();
		}


		public static void PreparePlayer ()
		{
			saveSystem.SpawnAllPlayers ();

			Player[] localPlayers = FindObjectsOfType<Player> ();

			if (settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				// Local players are ignored
				foreach (Player localPlayer in localPlayers)
				{
					if (localPlayer.ID <= -1)
					{
						ACDebug.LogWarning ("Local Player " + localPlayer.GetName () + " found in scene " + localPlayer.gameObject.scene.name + ". This is not allowed when Player Switching is enabled - in this mode, Players can only be spawned in.", localPlayer);
					}
				}
				
				PlayerPrefab playerPrefab = settingsManager.GetPlayerPrefab (saveSystem.CurrentPlayerID);
				if (playerPrefab != null) playerPrefab.SpawnInScene (true);
			}
			else
			{
				// Local players take priority
				foreach (Player localPlayer in localPlayers)
				{
					if (localPlayer.ID == -1)
					{
						switch (KickStarter.settingsManager.referenceScenesInSave)
						{
							case ChooseSceneBy.Name:
								localPlayer.ID = -2 - Mathf.Abs (SceneChanger.CurrentSceneName.GetHashCode ()); // Always unique to the same, but not needing building index
								break;

							case ChooseSceneBy.Number:
							default:
								localPlayer.ID = -2 - SceneChanger.CurrentSceneIndex; // Always unique to the scene
								break;
						}

						player = localPlayer;
						return;
					}
				}

				foreach (Player localPlayer in localPlayers)
				{
					if (localPlayer.ID == 0)
					{
						player = localPlayer;
						return;
					}
				}

				if (settingsManager.GetDefaultPlayer () != null)
				{
					player = settingsManager.GetDefaultPlayer ().SpawnFromPrefab (0);
				}
			}

			if (player == null && settingsManager.movementMethod != MovementMethod.None)
			{
				ACDebug.LogWarning ("No Player found - this can be assigned in the Settings Manager.");
			}

			if (player)
			{
				player.EndPath ();
				player.Halt (false);
			}

			KickStarter.saveSystem.SpawnFollowingPlayers ();
		}


		/**
		 * Turns Adventure Creator off.
		 */
		public static void TurnOnAC ()
		{
			if (stateHandler)
			{
				stateHandler.SetACState (true);
				eventManager.Call_OnManuallySwitchAC (true);
				ACDebug.Log ("Adventure Creator has been turned on.");
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn AC on because the PersistentEngine and GameEngine are not present!");
			}
		}
		
		
		/**
		 * Turns Adventure Creator on.
		 */
		public static void TurnOffAC ()
		{
			if (stateHandler)
			{
				eventManager.Call_OnManuallySwitchAC (false);
				stateHandler.SetACState (false);
				ACDebug.Log ("Adventure Creator has been turned off.");
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn AC off because it is not on!");
			}
		}


		/**
		 * <summary>Unsets the values of all script variables, so that they can be re-assigned to the correct scene if multiple scenes are open.</summary>
		 */
		public void ClearVariables ()
		{
			playerPrefab = null;
			mainCameraPrefab = null;
			persistentEnginePrefab = null;
			gameEnginePrefab = null;

			// Managers
			sceneManagerPrefab = null;
			settingsManagerPrefab = null;
			actionsManagerPrefab = null;
			variablesManagerPrefab = null;
			inventoryManagerPrefab = null;
			speechManagerPrefab = null;
			cursorManagerPrefab = null;
			menuManagerPrefab = null;

			// PersistentEngine components
			optionsComponent = null;
			runtimeInventoryComponent = null;
			runtimeVariablesComponent = null;
			playerMenusComponent = null;
			stateHandlerComponent = null;
			sceneChangerComponent = null;
			saveSystemComponent = null;
			levelStorageComponent = null;
			runtimeLanguagesComponent = null;
			actionListAssetManagerComponent = null;

			// GameEngine components
			menuSystemComponent = null;
			dialogComponent = null;
			playerInputComponent = null;
			playerInteractionComponent = null;
			playerMovementComponent = null;
			playerCursorComponent = null;
			playerQTEComponent = null;
			sceneSettingsComponent = null;
			navigationManagerComponent = null;
			actionListManagerComponent = null;
			localVariablesComponent = null;
			menuPreviewComponent = null;
			eventManagerComponent = null;

			SetGameEngine ();
		}


		/**
		 * <summary>Restarts the game, resetting the game to its original state.  Save game files and options data will not be affected</summary>
		 * <param name = "resetMenus">If True, Menus will be rebuilt based on their original settings in the Menu Manager</param>
		 * <param name = "newSceneIndex">The build index number of the scene to switch to</param>
		 * <param name = "killActionLists">If True, then all ActionLists currently running will be killed</param>
		 */
		public static void RestartGame (bool rebuildMenus, int newSceneIndex, bool killActionLists = false)
		{
			OnRestart (rebuildMenus, killActionLists);
			KickStarter.sceneChanger.ChangeScene (newSceneIndex, false, true);
		}


		/**
		 * <summary>Restarts the game, resetting the game to its original state.  Save game files and options data will not be affected</summary>
		 * <param name = "resetMenus">If True, Menus will be rebuilt based on their original settings in the Menu Manager</param>
		 * <param name = "newSceneName">The name of the scene to switch to</param>
		 * <param name = "killActionLists">If True, then all ActionLists currently running will be killed</param>
		 */
		public static void RestartGame (bool rebuildMenus, string newSceneName, bool killActionLists = false)
		{
			OnRestart (rebuildMenus, killActionLists);
			KickStarter.sceneChanger.ChangeScene (newSceneName, false, true);
		}


		private static void OnRestart (bool rebuildMenus, bool killActionLists)
		{
			if (killActionLists)
			{
				KickStarter.actionListManager.KillAllLists ();
			}

			KickStarter.runtimeInventory.SetNull ();
			KickStarter.runtimeInventory.RemoveRecipes ();

			if (KickStarter.settingsManager.blackOutWhenInitialising)
			{
				KickStarter.mainCamera.ForceOverlayForFrames (6);
			}

			if (KickStarter.player && !KickStarter.player.IsLocalPlayer ())
			{
				DestroyImmediate (KickStarter.player.gameObject);
			}

			KickStarter.saveSystem.ClearAllData ();
			KickStarter.levelStorage.ClearAllLevelData ();

			KickStarter.stateHandler.Initialise (rebuildMenus);

			KickStarter.eventManager.Call_OnRestartGame ();

			KickStarter.stateHandler.CanGlobalOnStart ();
		}

	}
	
}