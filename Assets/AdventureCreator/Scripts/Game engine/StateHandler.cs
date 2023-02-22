/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"StateHandler.cs"
 * 
 *	This script stores the gameState variable, which is used by
 *	other scripts to determine if the game is running normal gameplay,
 *	in a cutscene, paused, or displaying conversation options.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script stores the all-important gameState variable, which determines if the game is running normal gameplay, is in a cutscene, or is paused.
	 * It also runs the various "Update", "LateUpdate", "FixedUpdate" and "OnGUI" functions that are within Adventure Creator's main scripts - by running them all from here, performance is drastically improved.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_state_handler.html")]
	public class StateHandler : MonoBehaviour
	{

		#region Variables

		protected Music music;
		protected Ambience ambience;
		protected bool inScriptedCutscene;
		protected bool inScriptedPause;
		protected GameState previousUpdateState = GameState.Normal;
		protected bool isACDisabled = false;

		protected bool cursorIsOff = false;
		protected bool inputIsOff = false;
		protected bool interactionIsOff = false;
		protected bool draggablesIsOff = false;
		protected bool menuIsOff = false;
		protected bool movementIsOff = false;
		protected bool cameraIsOff = false;
		protected bool triggerIsOff = false;
		protected bool playerIsOff = false;
		protected bool applicationIsInFocus = true;
		protected bool applicationIsPaused = false;

		protected bool runAtLeastOnce = false;
		protected KickStarter activeKickStarter = null;

		protected HashSet<ArrowPrompt> arrowPrompts = new HashSet<ArrowPrompt>();
		protected HashSet<DragBase> dragBases = new HashSet<DragBase>();
		protected HashSet<Parallax2D> parallax2Ds = new HashSet<Parallax2D>();
		protected HashSet<Hotspot> hotspots = new HashSet<Hotspot>();
		protected HashSet<Highlight> highlights = new HashSet<Highlight>();
		protected HashSet<AC_Trigger> triggers = new HashSet<AC_Trigger>();
		protected HashSet<_Camera> cameras = new HashSet<_Camera>();
		protected HashSet<Sound> sounds = new HashSet<Sound>();
		protected HashSet<Char> characters = new HashSet<Char>();
		protected HashSet<FollowSortingMap> followSortingMaps = new HashSet<FollowSortingMap>();
		protected HashSet<NavMeshBase> navMeshBases = new HashSet<NavMeshBase>();
		protected HashSet<SortingMap> sortingMaps = new HashSet<SortingMap>();
		protected HashSet<BackgroundCamera> backgroundCameras = new HashSet<BackgroundCamera>();
		protected HashSet<BackgroundImage> backgroundImages = new HashSet<BackgroundImage>();
		protected HashSet<Container> containers = new HashSet<Container> ();

		protected ConstantIDManager constantIDManager;

		#endregion


		#region UnityStandards

		private void OnEnable ()
		{
			EventManager.OnInitialiseScene += OnInitialiseScene;
			EventManager.OnAddSubScene += OnAddSubScene;
			EventManager.OnEnterGameState += OnEnterGameState;

			#if UNITY_EDITOR
			UnityEditor.EditorApplication.pauseStateChanged += OnPauseStateChange;
			#endif
		}


		private void OnDisable ()
		{
			EventManager.OnInitialiseScene -= OnInitialiseScene;
			EventManager.OnAddSubScene -= OnAddSubScene;
			EventManager.OnEnterGameState -= OnEnterGameState;

			#if UNITY_EDITOR
			UnityEditor.EditorApplication.pauseStateChanged -= OnPauseStateChange;
			#endif
		}
		

		public void Initialise (bool rebuildMenus = true)
		{
			RegisterInitialConstantIDs ();

			Time.timeScale = 1f;
			DontDestroyOnLoad (this);

			KickStarter.sceneChanger.OnInitPersistentEngine ();
			KickStarter.runtimeInventory.OnInitPersistentEngine ();

			KickStarter.saveSystem.SetInitialPlayerID ();

			KickStarter.runtimeLanguages.OnInitPersistentEngine ();
			KickStarter.runtimeVariables.TransferFromManager ();
			KickStarter.options.OnInitPersistentEngine ();
			KickStarter.levelStorage.OnInitPersistentEngine ();
			KickStarter.runtimeVariables.OnInitPersistentEngine ();
			KickStarter.runtimeDocuments.OnInitPersistentEngine ();
			KickStarter.runtimeObjectives.OnInitPersistentEngine ();

			if (rebuildMenus)
			{
				KickStarter.playerMenus.OnInitPersistentEngine ();
			}

			KickStarter.playerMenus.RecalculateAll ();
		}


		protected void Update ()
		{
			#if UNITY_EDITOR
			ACScreen.UpdateCache ();
			#endif
			
			if (!CanRun ())
			{
				return;
			}
			
			if (KickStarter.settingsManager.IsInLoadingScene () || KickStarter.sceneChanger.IsLoading ())
			{
				if (!menuIsOff)
				{
					KickStarter.playerMenus.UpdateLoadingMenus ();
				}
				return;
			}

			for (int i = 0; i < KickStarter.variablesManager.timers.Count; i++)
			{
				KickStarter.variablesManager.timers[i].Update ();
			}

			if (!inputIsOff)
			{
				if (gameState == GameState.DialogOptions)
				{
					KickStarter.playerInput.DetectConversationInputs ();
				}
				KickStarter.playerInput.UpdateInput ();

				KickStarter.playerInput.UpdateDirectInput (IsInGameplay ());
			
				if (gameState != GameState.Paused)
				{
					KickStarter.playerQTE.UpdateQTE ();
				}
			}

			KickStarter.dialog._Update ();

			KickStarter.playerInteraction.UpdateInteractionLabel ();

			if (!cursorIsOff)
			{
				KickStarter.playerCursor.UpdateCursor ();
			
				bool canHideHotspots = KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.hideUnhandledHotspots;
				bool canDrawHotspotIcons = (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never);
				bool canUpdateProximity = (KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity && KickStarter.settingsManager.placeDistantHotspotsOnSeparateLayer && KickStarter.player);

				foreach (Hotspot hotspot in hotspots)
				{
					bool showing = (canHideHotspots) ? hotspot.UpdateUnhandledVisibility () : true;
					if (showing)
					{
						if (canDrawHotspotIcons)
						{
							if (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never)
							{
								hotspot.UpdateIcon ();
								if (KickStarter.settingsManager.hotspotDrawing == ScreenWorld.WorldSpace)
								{
									hotspot.DrawHotspotIcon (true);
								}
							}
						}

						if (canUpdateProximity)
						{
							hotspot.UpdateProximity (KickStarter.player.hotspotDetector);
						}
					}
				}
			}
			
			if (!menuIsOff)
			{
				KickStarter.playerMenus.CheckForInput ();
				
				if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.playerInput.GetMouseState () != MouseState.Normal)
				{
					KickStarter.playerMenus.UpdateAllMenus ();
				}
			}

			if (!interactionIsOff)
			{
				KickStarter.playerInteraction.UpdateInteraction ();

				foreach (Highlight highlight in highlights)
				{
					highlight._Update ();
				}

				if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.MouseOver && KickStarter.settingsManager.scaleHighlightWithMouseProximity)
				{
					bool isInGameplay = IsInGameplay ();
					foreach (Hotspot hotspot in hotspots)
					{
						hotspot.SetProximity (isInGameplay);
					}
				}
			}

			if (!triggerIsOff)
			{
				foreach (AC_Trigger trigger in triggers)
				{
					trigger._Update ();
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.UpdateAllMenus ();
			}

			foreach (DragBase dragBase in dragBases)
			{
				dragBase.UpdateMovement ();
			}

			if (!movementIsOff)
			{
				if (IsInGameplay () && KickStarter.settingsManager && KickStarter.settingsManager.movementMethod != MovementMethod.None)
				{
					KickStarter.playerMovement.UpdatePlayerMovement ();
				}
			}

			if (!interactionIsOff)
			{
				KickStarter.playerInteraction.UpdateInventory ();
			}
			
			foreach (Sound sound in sounds)
			{
				sound._Update ();
			}
			
			foreach (AC.Char character in characters)
			{
				if (character && (!playerIsOff || !(character.IsPlayer)))
				{
					character._Update ();
				}
			}

			if (!cameraIsOff)
			{
				foreach (_Camera _camera in cameras)
				{
					_camera._Update ();
				}
			}
		}


		protected void LateUpdate ()
		{
			if (!CanRun ())
			{
				return;
			}

			if (KickStarter.settingsManager && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			foreach (AC.Char character in characters)
			{
				if (!playerIsOff || !(character.IsPlayer))
				{
					character._LateUpdate ();
				}
			}

			if (!cameraIsOff && KickStarter.mainCamera)
			{
				KickStarter.mainCamera._LateUpdate ();
			}

			foreach (Parallax2D parallax2D in parallax2Ds)
			{
				parallax2D.UpdateOffset ();
			}

			foreach (SortingMap sortingMap in sortingMaps)
			{
				sortingMap.UpdateSimilarFollowers ();
			}

			KickStarter.dialog._LateUpdate ();

			GameState currentGameState = gameState;
			if (previousUpdateState != currentGameState)
			{
				KickStarter.eventManager.Call_OnChangeGameState (previousUpdateState, currentGameState);
				previousUpdateState = currentGameState;
			}
		}


		protected void FixedUpdate ()
		{
			if (!CanRun ())
			{
				return;
			}

			if (KickStarter.settingsManager && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			foreach (AC.Char character in characters)
			{
				if (!playerIsOff || !(character.IsPlayer))
				{
					character._FixedUpdate ();
				}
			}

			foreach (DragBase dragBase in dragBases)
			{
				dragBase._FixedUpdate ();
			}

			KickStarter.playerInput._FixedUpdate ();
		}


		private void OnApplicationFocus (bool focus)
		{
			applicationIsInFocus = focus;
		}


		private void OnApplicationPause (bool pause)
		{
			applicationIsPaused = pause;
		}


		#if ACIgnoreOnGUI
		#else

		protected void OnGUI ()
		{
			if (!isACDisabled)
			{
				_OnGUI ();
			}
		}

		#endif


		/**
		 * Runs all of AC's OnGUI code.
		 * This is called automatically from within StateHandler, unless 'ACIgnoreOnGUI' is listed in Unity's Scripting Define Symbols box in the Player settings.
		 */
		public void _OnGUI ()
		{
			if (!CanRun ())
			{
				return;
			}

			if (KickStarter.settingsManager.IsInLoadingScene () || KickStarter.sceneChanger.IsLoading ())
			{
				if (!cameraIsOff && !KickStarter.settingsManager.IsInLoadingScene ())
				{
					KickStarter.mainCamera.DrawCameraFade ();
				}
				if (!menuIsOff)
				{
					if (KickStarter.settingsManager.IsInLoadingScene ())
					{
						KickStarter.playerMenus.DrawLoadingMenus ();
					}
					else
					{
						KickStarter.playerMenus.DrawMenus ();
					}
				}
				if (!cameraIsOff)
				{
					KickStarter.mainCamera.DrawBorders ();
				}

				StatusBox.DrawDebugWindow ();
				return;
			}

			if (!cursorIsOff && !KickStarter.saveSystem.IsTakingSaveScreenshot)
			{
				if (KickStarter.settingsManager.hotspotIconDisplay != HotspotIconDisplay.Never &&
				   KickStarter.settingsManager.hotspotDrawing == ScreenWorld.ScreenSpace)
				{
					foreach (Hotspot hotspot in hotspots)
					{
						hotspot.DrawHotspotIcon ();
					}
				}

				if (IsInGameplay ())
				{
					foreach (DragBase dragBase in dragBases)
					{
						dragBase.DrawGrabIcon ();
					}
				}
			}

			if (!inputIsOff)
			{
				if (gameState == GameState.DialogOptions)
				{
					KickStarter.playerInput.DetectConversationNumerics ();
				}
				KickStarter.playerInput.DrawDragLine ();

				foreach (ArrowPrompt arrowPrompt in arrowPrompts)
				{
					arrowPrompt.DrawArrows ();
				}
			}

			if (!menuIsOff)
			{
				KickStarter.playerMenus.DrawMenus ();
			}

			if (!cursorIsOff)
			{
				if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software)
				{
					KickStarter.playerCursor.DrawCursor ();
				}
				else if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					KickStarter.runtimeInventory.DrawSelectedInventoryCount ();
				}
			}

			if (!cameraIsOff && KickStarter.mainCamera)
			{
				KickStarter.mainCamera.DrawCameraFade ();
				KickStarter.mainCamera.DrawBorders ();
			}

			StatusBox.DrawDebugWindow ();
		}

		#endregion


		#region PublicFunctions

		/** Checks if the application is currently in focus or not */
		public bool ApplicationIsInFocus ()
		{
			return applicationIsInFocus;
		}


		/** Checks if the application is currently paused */
		public bool ApplicationIsPaused ()
		{
			return applicationIsPaused;
		}


		/** The current state of the game (Normal, Cutscene, Paused, DialogOptions) */
		public GameState gameState
		{
			get
			{
				if (inScriptedPause) return GameState.Paused;

				if (KickStarter.playerMenus.ArePauseMenusOn ())
				{
					if (KickStarter.actionListManager.IsGameplayBlockedAndUnfrozen ())
					{
						return GameState.Cutscene;
					}
					return GameState.Paused;
				}

				if (inScriptedCutscene) return GameState.Cutscene;
				if (KickStarter.mainCamera && KickStarter.mainCamera.IsShowingForcedOverlay ()) return GameState.Cutscene;
				if (KickStarter.playerInteraction.InPreInteractionCutscene) return GameState.Cutscene;

				if (KickStarter.actionListManager.IsGameplayBlocked ())
				{
					return GameState.Cutscene;
				}

				if (KickStarter.playerInput.IsInConversation (true))
				{
					return GameState.DialogOptions;
				}
				return GameState.Normal;
			}
		}



		/**
		 * Alerts the StateHandler that a Game Engine prefab is present in the scene.
		 * This is called from KickStarter when the game begins - the StateHandler will not run until this is done.
		 */
		public void Register (KickStarter kickStarter)
		{
			activeKickStarter = kickStarter;
		}


		/**
		 * Alerts the StateHandler that a Game Engine prefab is no longer present in the scene.
		 * This is called from KickStarter's OnDestroy function.
		 */
		public void Unregister (KickStarter kickStarter)
		{
			if (kickStarter != null && activeKickStarter == kickStarter)
			{
				activeKickStarter = null;
			}
		}


		/**
		 * <summary>Runs the ActionListAsset defined in SettingsManager's actionListOnStart when the game begins.</summary>
		 * <returns>True if an ActionListAsset was run</returns>
		 */
		public bool PlayGlobalOnStart ()
		{
			if (runAtLeastOnce)
			{
				return false;
			}

			runAtLeastOnce = true;

			KickStarter.playerMenus.ShowEnabledOnStartMenus ();

			ActiveInput.Upgrade ();
			if (KickStarter.settingsManager.activeInputs != null)
			{
				foreach (ActiveInput activeInput in KickStarter.settingsManager.activeInputs)
				{
					activeInput.SetDefaultState ();
				}
			}
			if (KickStarter.variablesManager.timers != null)
			{
				foreach (Timer timer in KickStarter.variablesManager.timers)
				{
					timer.SetDefaultState ();
				}
			}

			if (gameState != GameState.Paused)
			{
				// Fix for audio pausing on start
				AudioListener.pause = false;
			}

			if (KickStarter.settingsManager.actionListOnStart)
			{
				AdvGame.RunActionListAsset (KickStarter.settingsManager.actionListOnStart);
				return true;
			}

			return false;
		}


		/** Allows the ActionListAsset defined in SettingsManager's actionListOnStart to be run again. */
		public void CanGlobalOnStart ()
		{
			runAtLeastOnce = false;
		}


		/** Calls Physics.IgnoreCollision on all appropriate Collider combinations (Unity 5 only). */
		public void IgnoreNavMeshCollisions ()
		{
			Collider[] allColliders = FindObjectsOfType (typeof(Collider)) as Collider[];
			foreach (NavMeshBase navMeshBase in navMeshBases)
			{
				navMeshBase.IgnoreNavMeshCollisions (allColliders);
			}
		}


		/** Sets the maximum volume of all Sound objects in the scene. */
		public void UpdateAllMaxVolumes ()
		{
			foreach (Sound sound in sounds)
			{
				sound.SetMaxVolume ();
			}
		}


		/** The state of enforced cutscene mode.  This is used to block gameplay etc through custom scripting, as opposed to ActionLists */
		public bool EnforceCutsceneMode
		{
			get
			{
				return inScriptedCutscene;
			}
			set
			{
				inScriptedCutscene = value;
			}
		}


		/** The state of enforced pause mode.  This is used to pause the game without requiring a pausing menu to be enabled */
		public bool EnforcePauseMode
		{
			get
			{
				return inScriptedPause;
			}
			set
			{
				inScriptedPause = value;
			}
		}


		/**
		 * <summary>Checks if the game is currently in a cutscene, scripted or otherwise.</summary>
		 * <returns>True if the game is currently in a cutscene</returns>
		 */
		public bool IsInCutscene ()
		{
			return (!isACDisabled && gameState == GameState.Cutscene);
		}


		/**
		 * <summary>Checks if the game is currently paused.</summary>
		 * <returns>True if the game is currently paused</returns>
		 */
		public bool IsPaused ()
		{
			return (!isACDisabled && gameState == GameState.Paused);
		}


		/**
		 * <summary>Checks if the game is currently in regular gameplay.</summary>
		 * <returns>True if the game is currently in regular gameplay</returns>
		 */
		public bool IsInGameplay ()
		{
			if (isACDisabled)
			{
				return false;
			}
			if (gameState == GameState.Normal)
			{
				return true;
			}
			if (gameState == GameState.DialogOptions && KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Enables or disables Adventure Creator completely.</summary>
		 * <param name = "state">If True, then Adventure Creator will be enabled. If False, then Adventure Creator will be disabled.</param>
		 */
		public void SetACState (bool state)
		{
			isACDisabled = !state;
		}


		/**
		 * <summary>Checks if AC is currently enabled.<summary>
		 * <returns>Trye if AC is currently enabled.<returns>
		 */
		public bool IsACEnabled ()
		{
			return !isACDisabled;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerCursor system.</summary>
		 * <param name = "state">If True, the PlayerCursor system will be enabled</param>
		 */
		public void SetCursorSystem (bool state)
		{
			cursorIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerInput system.</summary>
		 * <param name = "state">If True, the PlayerInput system will be enabled</param>
		 */
		public void SetInputSystem (bool state)
		{
			inputIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the Interaction system.</summary>
		 * <param name = "state">If True, the Interaction system will be enabled</param>
		 */
		public void SetInteractionSystem (bool state)
		{
			interactionIsOff = !state;

			if (!state)
			{
				KickStarter.playerInteraction.DeselectHotspot (true);
			}
		}


		/**
		 * <summary>Sets the enabled state of the Draggable system.</summary>
		 * <param name = "state">If True, the Draggable system will be enabled</param>
		 */
		public void SetDraggableSystem (bool state)
		{
			draggablesIsOff = !state;

			if (!state)
			{
				KickStarter.playerInput.LetGo ();
			}
		}


		/**
		 * <summary>Checks if the interaction system is enabled.</summary>
		 * <returns>True if the interaction system is enabled</returns>
		 */
		public bool CanInteract ()
		{
			return !interactionIsOff;
		}


		/**
		 * <summary>Checks if the draggables system is enabled.</summary>
		 * <returns>True if the draggables system is enabled</returns>
		 */
		public bool CanInteractWithDraggables ()
		{
			return !draggablesIsOff;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerMenus system.</summary>
		 * <param name = "state">If True, the PlayerMenus system will be enabled</param>
		 */
		public void SetMenuSystem (bool state)
		{
			menuIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the PlayerMovement system.</summary>
		 * <param name = "state">If True, the PlayerMovement system will be enabled</param>
		 */
		public void SetMovementSystem (bool state)
		{
			movementIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the MainCamera system.</summary>
		 * <param name = "state">If True, the MainCamera system will be enabled</param>
		 */
		public void SetCameraSystem (bool state)
		{
			cameraIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the trigger system.</summary>
		 * <param name = "state">If True, the trigger system will be enabled</param>
		 */
		public void SetTriggerSystem (bool state)
		{
			triggerIsOff = !state;
		}


		/**
		 * <summary>Sets the enabled state of the Player system.</summary>
		 * <param name = "state">If True, the Player system will be enabled</param>
		 */
		public void SetPlayerSystem (bool state)
		{
			playerIsOff = !state;
		}


		/**
		 * <summary>Checks if the trigger system is disabled.</summary>
		 * <returns>True if the trigger system is disabled</returns>
		 */
		public bool AreTriggersDisabled ()
		{
			return triggerIsOff;
		}


		/**
		 * <summary>Checks if the camera system is disabled.</summary>
		 * <returns>True if the camera system is disabled</returns>
		 */
		public bool AreCamerasDisabled ()
		{
			return cameraIsOff;
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.cursorIsOff = cursorIsOff;
			mainData.inputIsOff = inputIsOff;
			mainData.interactionIsOff = interactionIsOff;
			mainData.menuIsOff = menuIsOff;
			mainData.movementIsOff = movementIsOff;
			mainData.cameraIsOff = cameraIsOff;
			mainData.triggerIsOff = triggerIsOff;
			mainData.playerIsOff = playerIsOff;

			if (music)
			{
				mainData = music.SaveMainData (mainData);
			}

			if (ambience)
			{
				mainData = ambience.SaveMainData (mainData);
			}

			mainData = KickStarter.runtimeObjectives.SaveGlobalObjectives (mainData);

			return mainData;
		}


		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			cursorIsOff = mainData.cursorIsOff;
			inputIsOff = mainData.inputIsOff;
			interactionIsOff = mainData.interactionIsOff;
			menuIsOff = mainData.menuIsOff;
			movementIsOff = mainData.movementIsOff;
			cameraIsOff = mainData.cameraIsOff;
			triggerIsOff = mainData.triggerIsOff;
			playerIsOff = mainData.playerIsOff;

			if (music == null)
			{
				CreateMusicEngine ();
			}
			music.LoadMainData (mainData);

			if (ambience == null)
			{
				CreateAmbienceEngine ();
			}
			ambience.LoadMainData (mainData);
			KickStarter.runtimeObjectives.AssignGlobalObjectives (mainData);
		}


		/**
		 * <summary>Gets the Music component used to handle AudioClips played using the 'Sound: Play music' Action.</summary>
		 * <returns>The Music component used to handle AudioClips played using the 'Sound: Play music' Action.</returns>
		 */
		public Music GetMusicEngine ()
		{
			if (music == null)
			{
				CreateMusicEngine ();
			}
			return music;
		}


		/**
		 * <summary>Gets the Ambience component used to handle AudioClips played using the 'Sound: Play ambience' Action.</summary>
		 * <returns>The Ambience component used to handle AudioClips played using the 'Sound: Play ambience' Action.</returns>
		 */
		public Ambience GetAmbienceEngine ()
		{
			if (ambience == null)
			{
				CreateAmbienceEngine ();
			}
			return ambience;
		}


		/** Creates an initial record of all ConstantID components in the Hierarchy. More may be added through OnEnable / Start functions, but this way those that are initially present are ensured to be included in initialisation processes */
		public void RegisterInitialConstantIDs ()
		{
			ConstantID[] allConstantIDs = Object.FindObjectsOfType <ConstantID>();
			foreach (ConstantID constantID in allConstantIDs)
			{
				Register(constantID);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void OnAddSubScene (SubScene subScene)
		{
			IgnoreNavMeshCollisions ();
		}


		protected void OnInitialiseScene ()
		{
			if (previousUpdateState != gameState)
			{
				KickStarter.eventManager.Call_OnChangeGameState (previousUpdateState, gameState);
				previousUpdateState = gameState;
			}

			EnforceCutsceneMode = false;
		}


		protected void OnEnterGameState (GameState gameState)
		{
			StopAllCoroutines ();

			if (gameState == GameState.Paused)
			{
				if (Time.time > 0f)
				{
					AudioListener.pause = true;
					Time.timeScale = 0f;
				}
				else
				{
					StartCoroutine (PauseNextFrame ());
				}
			}
			else
			{
				if (Time.timeScale <= 0f)
				{
					AudioListener.pause = false;
					Time.timeScale = KickStarter.playerInput.timeScale;
				}
			}
		}


		private System.Collections.IEnumerator PauseNextFrame ()
		{
			yield return null;
			AudioListener.pause = true;
			Time.timeScale = 0f;
		}


		protected void CreateMusicEngine ()
		{
			if (music == null)
			{
				if (KickStarter.settingsManager.musicPrefabOverride)
				{
					music = Instantiate (KickStarter.settingsManager.musicPrefabOverride);
					music.audioSource.playOnAwake = false;
					return;
				}

				GameObject newMusicOb = new GameObject ("_Music");
				AudioSource audioSource = newMusicOb.AddComponent <AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f;

				music = newMusicOb.AddComponent <Music>();
			}
		}


		protected void CreateAmbienceEngine ()
		{
			if (ambience == null)
			{
				if (KickStarter.settingsManager.ambiencePrefabOverride)
				{
					ambience = Instantiate (KickStarter.settingsManager.ambiencePrefabOverride);
					ambience.audioSource.playOnAwake = false;
					return;
				}

				GameObject newAmbienceOb = new GameObject ("_Ambience");
				AudioSource audioSource = newAmbienceOb.AddComponent <AudioSource>();
				audioSource.playOnAwake = false;
				audioSource.spatialBlend = 0f;

				ambience = newAmbienceOb.AddComponent<Ambience>();
			}
		}


		protected bool CanRun ()
		{
			return (!isACDisabled && activeKickStarter);
		}

		
		#if UNITY_EDITOR
		protected void OnPauseStateChange (UnityEditor.PauseState state)
		{
			applicationIsPaused = (state == UnityEditor.PauseState.Paused);
		}
		#endif


		#endregion


		#region GetSet

		/** A HashSet of all Char components found in the scene */
		public HashSet<Char> Characters
		{
			get
			{
				return characters;
			}
		}


		/** A HashSet of all Sound components found in the scene */
		public HashSet<Sound> Sounds
		{
			get
			{
				return sounds;
			}
		}


		/** A HashSet of all ConstantID components found in the scene */
		public HashSet<ConstantID> ConstantIDs
		{
			get
			{
				return constantIDManager.ConstantIDs;
			}
		}


		/** A HashSet of all Hotspot components found in the scene */
		public HashSet<Hotspot> Hotspots
		{
			get
			{
				return hotspots;
			}
		}


		/** A HashSet of all FollowSortingMap components found in the scene */
		public HashSet<FollowSortingMap> FollowSortingMaps
		{
			get
			{
				return followSortingMaps;
			}
		}


		/** A HashSet of all SortingMap components found in the scene */
		public HashSet<SortingMap> SortingMaps
		{
			get
			{
				return sortingMaps;
			}
		}


		/** A HashSet of all BackgroundCamera components found in the scene */
		public HashSet<BackgroundCamera> BackgroundCameras
		{
			get
			{
				return backgroundCameras;
			}
		}


		/** A HashSet of all BackgroundImage components found in the scene */
		public HashSet<BackgroundImage> BackgroundImages
		{
			get
			{
				return backgroundImages;
			}
		}


		/** A HashSet of all Container components found in the scene */
		public HashSet<Container> Containers
		{
			get
			{
				return containers;
			}
		}


		/** A HashSet of all _Camera components found in the scene */
		public HashSet<_Camera> Cameras
		{
			get
			{
				return cameras;
			}
		}


		/** The ConstantIDManager used to record all ConstantID components in the Hierarchy */
		public ConstantIDManager ConstantIDManager
		{
			get
			{
				return constantIDManager;
			}
		}


		/** True if the Movement system has been disabled */
		public bool MovementIsOff
		{
			get
			{
				return movementIsOff;
			}
		}

		#endregion


		#region ObjectRecordKeeping

		/**
		 * <summary>Registers an ArrowPrompt, so that it can be updated</summary>
		 * <param name = "_object">The ArrowPrompt to register</param>
		 */
		public void Register (ArrowPrompt _object)
		{
			arrowPrompts.Add (_object);
		}


		/**
		 * <summary>Unregisters an ArrowPrompt, so that it is no longer updated</summary>
		 * <param name = "_object">The ArrowPrompt to unregister</param>
		 */
		public void Unregister (ArrowPrompt _object)
		{
			arrowPrompts.Remove (_object);
		}


		/**
		 * <summary>Registers a DragBase, so that it can be updated</summary>
		 * <param name = "_object">The DragBase to register</param>
		 */
		public void Register (DragBase _object)
		{
			dragBases.Add (_object);
		}


		/**
		 * <summary>Unregisters a DragBase, so that it is no longer updated</summary>
		 * <param name = "_object">The DragBase to unregister</param>
		 */
		public void Unregister (DragBase _object)
		{
			dragBases.Remove (_object);
		}


		/**
		 * <summary>Registers a Parallax2D, so that it can be updated</summary>
		 * <param name = "_object">The Parallax2D to register</param>
		 */
		public void Register (Parallax2D _object)
		{
			parallax2Ds.Add (_object);
		}


		/**
		 * <summary>Unregisters a Parallax2D, so that it is no longer updated</summary>
		 * <param name = "_object">The Parallax2D to unregister</param>
		 */
		public void Unregister (Parallax2D _object)
		{
			parallax2Ds.Remove (_object);
		}


		/**
		 * <summary>Registers a Hotspot, so that it can be updated</summary>
		 * <param name = "_object">The Hotspot to register</param>
		 */
		public void Register (Hotspot _object)
		{
			if (!hotspots.Contains (_object))
			{
				hotspots.Add (_object);

				if (KickStarter.eventManager)
				{
					KickStarter.eventManager.Call_OnRegisterHotspot (_object, true);
				}
			}
		}


		/**
		 * <summary>Unregisters a Hotspot, so that it is no longer updated</summary>
		 * <param name = "_object">The Hotspot to unregister</param>
		 */
		public void Unregister (Hotspot _object)
		{
			if (hotspots.Contains (_object))
			{
				hotspots.Remove (_object);

				if (KickStarter.eventManager)
				{
					KickStarter.eventManager.Call_OnRegisterHotspot (_object, false);
				}
			}
		}


		/**
		 * <summary>Registers a Highlight, so that it can be updated</summary>
		 * <param name = "_object">The Highlight to register</param>
		 */
		public void Register (Highlight _object)
		{
			highlights.Add (_object);
		}


		/**
		 * <summary>Unregisters a Highlight, so that it is no longer updated</summary>
		 * <param name = "_object">The Highlight to unregister</param>
		 */
		public void Unregister (Highlight _object)
		{
			highlights.Remove (_object);
		}


		/**
		 * <summary>Registers a AC_Trigger, so that it can be updated</summary>
		 * <param name = "_object">The AC_Trigger to register</param>
		 */
		public void Register (AC_Trigger _object)
		{
			triggers.Add (_object);
		}


		/**
		 * <summary>Unregisters a AC_Trigger, so that it is no longer updated</summary>
		 * <param name = "_object">The AC_Trigger to unregister</param>
		 */
		public void Unregister (AC_Trigger _object)
		{
			triggers.Remove (_object);
		}


		/**
		 * <summary>Registers a _Camera, so that it can be updated</summary>
		 * <param name = "_object">The _Camera to register</param>
		 */
		public void Register (_Camera _object)
		{
			cameras.Add (_object);
		}


		/**
		 * <summary>Unregisters a _Camera, so that it is no longer updated</summary>
		 * <param name = "_object">The _Camera to unregister</param>
		 */
		public void Unregister (_Camera _object)
		{
			cameras.Remove (_object);
		}


		/**
		 * <summary>Registers a Sound, so that it can be updated</summary>
		 * <param name = "_object">The Sound to register</param>
		 */
		public void Register (Sound _object)
		{
			sounds.Add (_object);
		}


		/**
		 * <summary>Unregisters a Sound, so that it is no longer updated</summary>
		 * <param name = "_object">The Sound to unregister</param>
		 */
		public void Unregister (Sound _object)
		{
			sounds.Remove (_object);
		}


		/**
		 * <summary>Registers a Char, so that it can be updated</summary>
		 * <param name = "_object">The Char to register</param>
		 */
		public void Register (Char _object)
		{
			characters.Add (_object);
		}


		/**
		 * <summary>Unregisters a Char, so that it is no longer updated</summary>
		 * <param name = "_object">The Char to unregister</param>
		 */
		public void Unregister (Char _object)
		{
			characters.Remove (_object);
		}


		/**
		 * <summary>Registers a FollowSortingMap, so that it can be updated</summary>
		 * <param name = "_object">The FollowSortingMap to register</param>
		 */
		public void Register (FollowSortingMap _object)
		{
			followSortingMaps.Add (_object);
			_object.UpdateSortingMap ();
		}


		/**
		 * <summary>Unregisters a FollowSortingMap, so that it is no longer updated</summary>
		 * <param name = "_object">The FollowSortingMap to unregister</param>
		 */
		public void Unregister (FollowSortingMap _object)
		{
			followSortingMaps.Remove (_object);
		}


		/**
		 * <summary>Registers a NavMeshBase, so that it can be updated</summary>
		 * <param name = "_object">The NavMeshBase to register</param>
		 */
		public void Register (NavMeshBase _object)
		{
			if (!navMeshBases.Contains (_object))
			{
				navMeshBases.Add (_object);
				_object.IgnoreNavMeshCollisions ();
			}
		}


		/**
		 * <summary>Unregisters a NavMeshBase, so that it is no longer updated</summary>
		 * <param name = "_object">The NavMeshBase to unregister</param>
		 */
		public void Unregister (NavMeshBase _object)
		{
			navMeshBases.Remove (_object);
		}


		/**
		 * <summary>Registers a SortingMap, so that it can be updated</summary>
		 * <param name = "_object">The SortingMap to register</param>
		 */
		public void Register (SortingMap _object)
		{
			sortingMaps.Add (_object);
		}


		/**
		 * <summary>Unregisters a SortingMap, so that it is no longer updated</summary>
		 * <param name = "_object">The SortingMap to unregister</param>
		 */
		public void Unregister (SortingMap _object)
		{
			sortingMaps.Remove (_object);
		}


		/**
		 * <summary>Registers a BackgroundCamera, so that it can be updated</summary>
		 * <param name = "_object">The BackgroundCamera to register</param>
		 */
		public void Register (BackgroundCamera _object)
		{
			if (!backgroundCameras.Contains (_object))
			{
				backgroundCameras.Add (_object);
				_object.UpdateRect ();
			}
		}


		/**
		 * <summary>Unregisters a BackgroundCamera, so that it is no longer updated</summary>
		 * <param name = "_object">The BackgroundCamera to unregister</param>
		 */
		public void Unregister (BackgroundCamera _object)
		{
			backgroundCameras.Remove (_object);
		}


		/**
		 * <summary>Registers a BackgroundImage, so that it can be updated</summary>
		 * <param name = "_object">The BackgroundImage to register</param>
		 */
		public void Register (BackgroundImage _object)
		{
			backgroundImages.Add (_object);
		}


		/**
		 * <summary>Unregisters a BackgroundImage, so that it is no longer updated</summary>
		 * <param name = "_object">The BackgroundImage to unregister</param>
		 */
		public void Unregister (BackgroundImage _object)
		{
			backgroundImages.Remove (_object);
		}


		/**
		 * <summary>Registers a Container, so that it can be updated</summary>
		 * <param name = "_object">The Container to register</param>
		 */
		public void Register (Container _object)
		{
			containers.Add (_object);
		}


		/**
		 * <summary>Unregisters a Container, so that it is no longer updated</summary>
		 * <param name = "_object">The Container to unregister</param>
		 */
		public void Unregister (Container _object)
		{
			containers.Remove (_object);
		}


		/**
		 * <summary>Registers a ConstantID, so that it can be updated</summary>
		 * <param name = "_object">The ConstantID to register</param>
		 */
		public void Register (ConstantID _object)
		{
			constantIDManager.Register (_object);
		}


		/**
		 * <summary>Unregisters a ConstantID, so that it is no longer updated</summary>
		 * <param name = "_object">The ConstantID to unregister</param>
		 */
		public void Unregister (ConstantID _object)
		{
			constantIDManager.Unregister (_object);
		}

		#endregion

	}

}