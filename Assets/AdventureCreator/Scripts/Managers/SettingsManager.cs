/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SettingsManager.cs"
 * 
 *	This script handles the "Settings" tab of the main wizard.
 *	It is used to define the player, and control methods of the game.
 * 
 */

#if !(UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII || UNITY_PS4)
#define ADVANCED_SAVING
#endif

#if UNITY_IOS || UNITY_ANDROID || UNITY_TVOS
#define MOBILE_PLATFORM
#endif

using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * Handles the "Settings" tab of the Game Editor window.
	 * Most game-wide settings, including those related to control, input and interactions, are stored here.
	 */
	[System.Serializable]
	public class SettingsManager : ScriptableObject
	{
		
		#if UNITY_EDITOR
		private bool showSave = true;
		private bool showCutscene = true;
		private bool showCharacter = true;
		private bool showInterface = true;
		private bool showInventory = true;
		private bool showMovement = true;
		private bool showTouchScreen = true;
		private bool showSceneLoading = true;
		private bool showCamera = true;
		private bool showHotspot = true;
		private bool showRaycast = true;
		private bool showRequiredInputs = true;
		private bool showSound = true;
		private bool showOptions = true;
		private bool showDebug = true;
		public bool separateEditorSaveFiles = false;
		#endif
		
		// Save settings

		/** The name to give save game files */
		public string saveFileName = "";			
		/** How the time of a save file should be displayed (None, DateOnly, TimeAndDate, CustomFormat) */
		public SaveTimeDisplay saveTimeDisplay = SaveTimeDisplay.DateOnly;
		/** The format of time display for save game labels, if saveTimeDisplay = SaveTimeDisplay.CustomFormat */
		public string customSaveFormat = "MMMM dd, yyyy";
		/** Deprecated */
		[SerializeField] private bool takeSaveScreenshots;
		/** Determines when save-game screenshots are recorded*/
		public SaveScreenshots saveScreenshots = SaveScreenshots.Never;
		/** If takeSaveSreenshots = True, the size of save-game screenshots, relative to the game window's actual resolution */
		public float screenshotResolutionFactor = 1f;
		/** If True, then multiple save profiles - each with its own save files and options data - can be created */
		public bool useProfiles = false;
		/** The maximum number of save files that can be created */
		public int maxSaves = 5;
		/** If True, then save files listed in MenuSaveList will be displayed in order of update time */
		public bool orderSavesByUpdateTime = false;
		/** If True, then the scene will reload when loading a saved game that takes place in the same scene that the player is already in */
		public bool reloadSceneWhenLoading = false;
		/** If True, then save operations will occur on a separate thread */
		public bool saveWithThreading = false;
		/** If True, then references to assets made in save game files will be based on their Addressable name, and not Resources folder presence */
		public bool saveAssetReferencesWithAddressables = false;
		/** A collection of save strings (Save, Import, Autosave) that can be translated */
		public SaveLabels saveLabels = new SaveLabels ();
		/** How to refer to scenes in save game files */
		public ChooseSceneBy referenceScenesInSave = ChooseSceneBy.Number;

		// Scene settings

		/** The game's full list of inventory item properties */
		public List<InvVar> sceneAttributes = new List<InvVar>();


		// Cutscene settings

		/** The ActionListAsset to run when the game begins */
		public ActionListAsset actionListOnStart;
		/** If True, then the game will turn black whenever the user triggers the "EndCutscene" input to skip a cutscene */
		public bool blackOutWhenSkipping = false;

		// Character settings

		/** The state of player-switching (Allow, DoNotAllow) */
		public PlayerSwitching playerSwitching = PlayerSwitching.DoNotAllow;
		/** The player prefab, if playerSwitching = PlayerSwitching.DoNotAllow */
		public Player player;
		/** All available player prefabs, if playerSwitching = PlayerSwitching.Allow */
		public List<PlayerPrefab> players = new List<PlayerPrefab>();

		// Interface settings

		/** How the player character is controlled (PointAndClick, Direct, FirstPerson, Drag, None, StraightToCursor) */
		public MovementMethod movementMethod = MovementMethod.PointAndClick;
		/** The main input method used to control the game with (MouseAndKeyboard, KeyboardOrController, TouchScreen) */
		public InputMethod inputMethod = InputMethod.MouseAndKeyboard;
		/** The movement speed of a keyboard or controller-controlled cursor */
		public float simulatedCursorMoveSpeed = 4f;
		/** How Hotspots are interacted with (ContextSensitive, ChooseInteractionThenHotspot, ChooseHotspotThenInteraction) */
		public AC_InteractionMethod interactionMethod = AC_InteractionMethod.ContextSensitive;
		/** How Interactions are triggered, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction (ClickingMenu, CyclingCursorAndClickingHotspot, CyclingMenuAndClickingHotspot) */
		public SelectInteractions selectInteractions = SelectInteractions.ClickingMenu;
		/** If True, then Interaction icons will be hidden if linked to a Hotspot or InvItem that has no interaction linked to that icon */
		public bool autoHideInteractionIcons = true;
		/** The method to close Interaction menus, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction (ClickOffMenu, CursorLeavesMenu, CursorLeavesMenuOrHotspot) */
		public CancelInteractions cancelInteractions = CancelInteractions.CursorLeavesMenuOrHotspot;
		/** How Interaction menus are opened, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction (ClickOnHotspot, CursorOverHotspot, ViaScriptOnly) */
		public SeeInteractions seeInteractions = SeeInteractions.ClickOnHotspot;
		/** If True, then Interaction Menus can be closed by tapping another Hotspot for which they are opened. */
		public bool closeInteractionMenuIfTapHotspot = true;
		/** If True, then the player will stop when a Hotspot is clicked on, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction */
		public bool stopPlayerOnClickHotspot = false;
		/** If True, then inventory items will be included in Interaction menus / cursor cycles, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction */
		public bool cycleInventoryCursors = true;
		/** If True, and inputMethod = InputMethod.KeyboardOrController, then the speed of the cursor will be proportional to the size of the screen */
		public bool scaleCursorSpeedWithScreen = true;
		/** If True, then triggering an Interaction will cycle the cursor mode, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction */
		public bool autoCycleWhenInteract = false;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot, then the Hotspot label will show the name of the interaction icon being hovered over */
		public bool showHoverInteractionInHotspotLabel = false;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot, then invoking the 'DefaultInteractions' input button will run the first-enabled 'Use' interaction of the active Hotspot */
		public bool allowDefaultinteractions = false;
		/* It True, Interaction menus will always close as the result of running an Interaction.  If False, they will only close if the resulting ActionList blocks gameplay. */
		public bool alwaysCloseInteractionMenus = true;
		/** What happens to the cursor icon when a hotspot (or inventory item, depending) is reselected and selectInteractions = SelectInteractions.CyclingMenuAndClickingHotspot */
		public WhenReselectHotspot whenReselectHotspot = WhenReselectHotspot.RestoreHotspotIcon;
		/** If True, then the cursor will be locked in the centre of the screen when the game begins */
		public bool lockCursorOnStart = false;
		/** If True, then the cursor will be hidden whenever it is locked */
		public bool hideLockedCursor = false;
		/** If True, and the game is in first-person, then free-aiming will be disabled while a Draggable object is manipulated */
		public bool disableFreeAimWhenDragging = false;
		/** If True, and the game is in first-person, then free-aiming will be disabled while a PickUp object is manipulated */
		public bool disableFreeAimWhenDraggingPickUp = false;
		/** If True, then Conversation dialogue options can be triggered with the number keys */
		public bool runConversationsWithKeys = false;
		/** If True, then interactions can be triggered by releasing the mouse cursor over an icon, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction */
		public bool clickUpInteractions = false;
		/** If True, and inputMethod = InputMethod.MouseAndKeyboard, then left and right mouse clicks will have default behaviour */
		public bool defaultMouseClicks = true;
		/** If True, then gameplay is allowed during Conversations */
		public bool allowGameplayDuringConversations = false;
		/** If True, then walking to Hotspots without running a particular Interaction will cause the Player to walk to the Hotspot's 'Walk-to Marker' */
		public bool walkToHotspotMarkers = true;
		/** The proportion of the screen that the mouse must be dragged for drag effects to kick in */
		public float dragThreshold = 0f;

		// Inventory settings

		/** If True, then all player prefabs will share the same inventory, if playerSwitching = PlayerSwitching.Allow */
		public bool shareInventory = false;
		/** If True, then inventory items can be drag-dropped (i.e. used on Hotspots and other items with a single mouse button press */
		public bool inventoryDragDrop = false;
		#if UNITY_EDITOR
		[SerializeField] private float dragDropThreshold = 0f;
		#endif
		/** If True, inventory can be interacted with while a Conversation is active (overridden by allowGameplayDuringConversations) */
		public bool allowInventoryInteractionsDuringConversations = false;
		/** If True, then drag-dropping an inventory item on itself will trigger its Examine interaction */
		public bool inventoryDropLook = false;
		/** If True, then drag-dropping an inventory item on itself will trigger its Examine interaction */
		public bool inventoryDropLookNoDrag = false;
		/** How many interactions an inventory item can have (Single, Multiple) */
		public InventoryInteractions inventoryInteractions = InventoryInteractions.Single;
		/** If True, then left-clicking will de-select an inventory item */
		public bool inventoryDisableLeft = true;
		/** If True, interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot and inventoryInteractions = InventoryInteractions.Multiple, then invoking the 'DefaultInteractions' input button will run the first-enabled 'Standard' interaction of the active Inventory item */
		public bool allowDefaultInventoryInteractions = false;
		/** If True, then triggering an unhandled Inventory interaction will de-select the active inventory item */
		public bool inventoryDisableUnhandled = true;
		/** If True, then triggering a defined Inventory interaction will-deselect the active inventory item */
		public bool inventoryDisableDefined = true;
		/** If True, then the player will stop pathfinding upon interacting with an inventory item */
		public bool inventoryInteractionsHaltPlayer = false;
		/** If True, then an inventory item will show its "active" texture when the mouse hovers over it */
		public bool activeWhenHover = false;
		/** The effect to apply to an active inventory item's icon (None, Pulse, Simple) */
		public InventoryActiveEffect inventoryActiveEffect = InventoryActiveEffect.Simple;
		/** The speed at which to pulse the active inventory item's icon, if inventoryActiveEffect = InventoryActiveEffect.Pulse */
		public float inventoryPulseSpeed = 1f;
		/** If True, then the inventory item will show its active effect when hovering over a Hotspot that has no matching Interaction */
		public bool activeWhenUnhandled = true;
		/** If True, then inventory items can be re-ordered in a MenuInventoryBox by the player */
		public bool canReorderItems = false;
		/** How the currently-selected inventory item should be displayed in InventoryBox elements */
		public SelectInventoryDisplay selectInventoryDisplay = SelectInventoryDisplay.NoChange;
		/** What happens when right-clicking while an inventory item is selected (ExaminesItem, DeselectsItem, DoesNothing) */
		public RightClickInventory rightClickInventory = RightClickInventory.DeselectsItem;
		/** If True, then invntory item combinations will also work in reverse */
		public bool reverseInventoryCombinations = false;
		/** If True, then the player can move while an inventory item is selected */
		public bool canMoveWhenActive = true;
		/** If True, and inventoryInteraction = InventoryInteractions.Multiple, then the item will be selected (in "use" mode) if a particular Interaction is unhandled */
		public bool selectInvWithUnhandled = false;
		/** The ID number of the CursorIcon interaction that selects the inventory item (in "use" mode) when unhandled, if selectInvWithUnhandled = True */
		public int selectInvWithIconID = 0;
		/** If True, and inventoryInteraction = InventoryInteractions.Multiple, then the item will be selected (in "give" mode) if a particular Interaction is unhandled */
		public bool giveInvWithUnhandled = false;
		/** The ID number of the CursorIcon interaction that selects the inventory item (in "give" mode) when unhandled, if selectInvWithUnhandled = True */
		public int giveInvWithIconID = 0;
		/** If True, Hotspots that have no interaction associated with a given inventory item will not be active while that item is selected */
		public bool autoDisableUnhandledHotspots = false;
	
		// Movement settings

		/** A prefab to instantiate whenever the user clicks to move the player, if movementMethod = AC_MovementMethod.PointAndClick */
		public Transform clickPrefab;
		/** If clickPrefab != null, where the click marker is spawned */
		public ClickMarkerPosition clickMarkerPosition = ClickMarkerPosition.ColliderContactPoint;
		/** How much of the screen will be searched for a suitable NavMesh, if the user doesn't click directly on one (it movementMethod = AC_MovementMethod.PointAndClick)  */
		public float walkableClickRange = 0.5f;
		/** How the nearest NavMesh to a cursor click is found, in screen space, if the user doesn't click directly on one */
		public NavMeshSearchDirection navMeshSearchDirection = NavMeshSearchDirection.RadiallyOutwardsFromCursor;
		/** If True, and navMeshSearchDirection = NavMeshSearchDirection.RadiallyOutwardsFromCursor, then off-NavMesh clicks will not detect NavMeshes that are off-screen */
		public bool ignoreOffScreenNavMesh = true;
		/** If movementMethod = AC_MovementMethod.PointAndClick or StraightToCursor, what effect double-clicking has on player movement */
		public DoubleClickMovement doubleClickMovement = DoubleClickMovement.MakesPlayerRun;
		/** If True, and movementMethod = AC_MovementMethod.Direct, then the magnitude of the input axis will affect the Player's speed */
		public bool magnitudeAffectsDirect = false;
		/** If True, and movementMethod = AC_MovementMethod.Direct, then the Player will turn instantly when moving during gameplay */
		public bool directTurnsInstantly = false;
		/** If True, and movementMethod = AC_MovementMethod.Direct, then the Player will cease turning when input is released */
		public bool stopTurningWhenReleaseInput = false;
		/** If True, and Interaction menus are used, movement will be prevented while they are on */
		public bool disableMovementWhenInterationMenusAreOpen = false;
		/** How the player moves, if movementMethod = AC_MovementMethod.Direct (RelativeToCamera, TankControls) */
		public DirectMovementType directMovementType = DirectMovementType.RelativeToCamera;
		/** How to limit the player's moement, if directMovementType = DirectMovementType.RelativeToCamera */
		public LimitDirectMovement limitDirectMovement = LimitDirectMovement.NoLimit;
		/** If greater than zero, player direction will be unchanged when the camera angle changes during gameplay if the input does not exceed this angle */
		public float cameraLockSnapAngleThreshold = 5f;
		/** If True, then the player's position on screen will be accounted for, if directMovementType = DirectMovementType.RelativeToCamera */
		public bool directMovementPerspective = false;
		/** How accurate characters will be when navigating to set points on a NavMesh */
		public float destinationAccuracy = 0.8f;
		/** If True, and destinationAccuracy = 1, then characters will lerp to their destination when very close, to ensure they end up at exactly the intended point */
		public bool experimentalAccuracy = false;
		/** If True, then movement and interaction clicks will be ignored if the cursor is over a Unity UI element - even those not linked to the Menu Manager */
		public bool unityUIClicksAlwaysBlocks = false;

		/** If >0, the time (in seconds) between pathfinding recalculations occur */
		public float pathfindUpdateFrequency = 0f;
		/** How much slower vertical movement is compared to horizontal movement, if the game is in 2D */
		public float verticalReductionFactor = 0.7f;
		/** If True, then rotations of 2D characters will be affected by the verticalReductionFactor value */
		public bool rotationsAffectedByVerticalReduction = true;
		/** If True, then 2D characters will move according to their sprite direction when moving along a Path / pathfinding, allowing for smooth movement at corners */
		public bool alwaysPathfindInSpriteDirection = false;
		/** The player's jump speed */
		public float jumpSpeed = 4f;
		/** If True, then single-clicking also moves the player, if movementMethod = AC_MovementMethod.StraightToCursor */
		public bool singleTapStraight = false;
		/** If True, then single-clicking will make the player pathfind, if singleTapStraight = True */
		public bool singleTapStraightPathfind = false;
		/** The duration in seconds that separates a single click/tap from a held click/tap when movementMethod = AC_MovementMethod.StraightToCursor */
		public float clickHoldSeparationStraight = 0.3f;

		// First-person settings

		/** If True, then first-person games will use the first-person camera during conversations (overridden by allowGameplayDuringConversations) */
		public bool useFPCamDuringConversations = true;
		/** If True, then Hotspot interactions are only allowed if the cursor is unlocked (first person-games only) */
		public bool onlyInteractWhenCursorUnlocked = false;
		/** The acceleration for free-aiming smoothing */
		public float freeAimSmoothSpeed = 50f;
		/* If True, then a smoothing factor will be applied to first-person movement */
		public bool firstPersonMovementSmoothing = true; 

		// Input settings

		/** If True, then try/catch statements used when checking for input will be bypassed - this results in better performance, but all available inputs must be defined. */
		public bool assumeInputsDefined = false;
		/** A List of active inputs that trigger ActionLists when an Input button is pressed */
		public List<ActiveInput> activeInputs = new List<ActiveInput>();

		// Drag settings

		/** The free-look speed when rotating a first-person camera, if inputMethod = AC_InputMethod.TouchScreen */
		public float freeAimTouchSpeed = 0.01f;
		/** If movementMethod = AC_MovementMethod.Drag, the minimum drag magnitude needed to move the player. If movementMethod = AC_MovementMethod.FirstPerson, this is the maximum free-aiming speed */
		public float dragWalkThreshold = 5f;
		/** The minimum drag magnitude needed to make the player run, if movementMethod = AC_MovementMethod.Drag */
		public float dragRunThreshold = 20f;
		/** If True, then a drag line will be drawn on screen if movementMethod = AC_MovementMethod.Drag */
		public bool drawDragLine = false;
		/** The width of the drag line, if drawDragLine = True */
		public float dragLineWidth = 3f;
		/** The colour of the drag line, if drawDragLine = True */
		public Color dragLineColor = Color.white;
	
		// Touch Screen settings

		/** If True, then the cursor is not set to the touch point, but instead is moved by dragging (if inputMethod = AC_InputMethod.TouchScreen) */
		public bool offsetTouchCursor = false;
		/** If True, then Hotspots are activated by double-tapping (if inputMethod = AC_InputMethod.TouchScreen) */
		public bool doubleTapHotspots = true;
		/** How First Person movement should work when using touch-screen controls (OneTouchToMoveAndTurn, OneTouchToTurnAndTwoTouchesToMove, TouchControlsTurningOnly, CustomInput) */
		public FirstPersonTouchScreen firstPersonTouchScreen = FirstPersonTouchScreen.OneTouchToMoveAndTurn;
		/** How Direct movement should work when using touch-screen controls (DragBased, CustomInput) */
		public DirectTouchScreen directTouchScreen = DirectTouchScreen.DragBased;
		/** If True, then menu clicks are performed by releasing a touch, rather than beginning one */
		public bool touchUpWhenPaused = false;
		/** If True, then scne clicks are performed by releasing a touch, rather than beginning one */
		public bool touchUpInteractScene = false;

		// Camera settings

		/** Deprecated */
		[SerializeField] private bool forceAspectRatio = false;
		/** What type of aspect ratio to enforce */
		public AspectRatioEnforcement aspectRatioEnforcement = AspectRatioEnforcement.NoneEnforced;
		/** The aspect ratio, as a decimal, to use if aspectRatioEnforcement = AspectRatioEnforcement.Fixed. If set to AspectRatioEnforcement.Range, this will be the minimum */
		public float wantedAspectRatio = 1.5f;
		/** The maximum aspect ratio, as a decimal, to use if aspectRatioEnforcement = AspectRatioEnforcement.Range */
		public float maxAspectRatio = 2.39f;
		/** If True, a second camera is used to render borders due to enforced aspect ratio.  This helps to prevent artefacts, but increases performance. */
		public bool renderBorderCamera = true;
		/** If True, then the game can only be played in landscape mode (iPhone only) */
		public bool landscapeModeOnly = true;
		/** The game's camera perspective (TwoD, TwoPointFiveD, ThreeD) */
		public CameraPerspective cameraPerspective = CameraPerspective.ThreeD;
		/** If True, Unity's Camera.main variable will be cached for a minor performance boost */
		public bool cacheCameraMain = false;
		/** If True, then textures created for crossfading and overlays will be saved in linear color space */
		public bool linearColorTextures = false;

		private int cameraPerspective_int;
		#if UNITY_EDITOR
		private string[] cameraPerspective_list = { "2D", "2.5D", "3D" };
		#endif

		#if MOBILE_PLATFORM
		/** If True, then the game's display will be limited to the device's "safe area" */
		public bool relyOnSafeArea = true;
		#endif


		/** The method of moving and turning in 2D games (Unity2D, TopDown, ScreenSpace, WorldSpace) */
		public MovingTurning movingTurning = MovingTurning.Unity2D;

		// Hotspot settings

		/** How Hotspots are detected (MouseOver, PlayerVicinity, CustomScript) */
		public HotspotDetection hotspotDetection = HotspotDetection.MouseOver;
		/** If True, and hotspotDetection = HotspotDetection.PlayerVicinity and interactionMethod = InteractionMethod.ChooseHotspotThenInteraction, then Interaction Menus will close if the Player is no longer in the active Hotspot's vicinity */
		public bool closeInteractionMenusIfPlayerLeavesVicinity = false;
		/** If True, and hotspotDetection = HotspotDetection.PlayerVicinity, then distant Hotspots will be placed on a different layer  */
		public bool placeDistantHotspotsOnSeparateLayer = true;
		/** What Hotspots gets detected, if hotspotDetection = HotspotDetection.PlayerVicinity (NearestOnly, CycleMultiple, ShowAll) */
		public HotspotsInVicinity hotspotsInVicinity = HotspotsInVicinity.NearestOnly;
		/** When Hotspot icons are displayed (Never, Always, OnlyWhenHighlighting, OnlyWhenFlashing) */
		public HotspotIconDisplay hotspotIconDisplay = HotspotIconDisplay.Never;
		/** The type of Hotspot icon to display, if hotspotIconDisplay != HotspotIconDisplay.Never (Texture, UseIcon) */
		public HotspotIcon hotspotIcon;
		/** Deprecated */
		public Texture2D hotspotIconTexture = null;
		/** The icon to use for Hotspot icons, if hotspotIcon = HotspotIcon.Texture */
		public CursorIconBase hotspotIconGraphic = new CursorIcon ();
		/** If set, this material property will be affected by Highlight components instead of the default */
		public string highlightMaterialPropertyOverride = "";

		/** The size of Hotspot icons */
		public float hotspotIconSize = 0.04f;
		/** If True, then 3D player prefabs will turn their head towards the active Hotspot */
		public bool playerFacesHotspots = false;
		/** If true, and playerFacesHotspots = True, and interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction, then players will only turn their heads once a Hotspot has been selected */
		public bool onlyFaceHotspotOnSelect = false;
		/** If True, then Hotspots will highlight according to how close the cursor is to them */
		public bool scaleHighlightWithMouseProximity = false;
		/** The factor by which distance affects the highlighting of Hotspots, if scaleHighlightWithMouseProximity = True */
		public float highlightProximityFactor = 4f;
		/** If True, then Hotspot icons will be hidden behind colldiers placed on hotspotLayer */
		public bool occludeIcons = false;
		/** If True, then Hotspot icons will be hidden if an Interaction Menu is visible */
		public bool hideIconUnderInteractionMenu = false;
		/** How to draw Hotspot icons (ScreenSpace, WorldSpace) */
		public ScreenWorld hotspotDrawing = ScreenWorld.ScreenSpace;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot, then Hotspots that do not have an interaction for the currently-selected icon will not be visible to the cursor */
		public bool hideUnhandledHotspots = false;

		// Raycast settings

		/** The length of rays cast to find NavMeshes */
		public float navMeshRaycastLength = 100f;
		/** The length of rays cast to find Hotspots */
		public float hotspotRaycastLength = 100f;
		/** The length of rays cast to find moveable objects (see DragBase) */
		public float moveableRaycastLength = 30f;
		
		// Layer names

		/** The layer to place active Hotspots on */
		public string hotspotLayer = "Default";
		/** The layer to place distant Hotspots on, if hotspotDetection = HotspotDetection.PlayerVicinity */
		public string distantHotspotLayer = "DistantHotspot";
		/** The layer to place active NavMeshes on */
		public string navMeshLayer = "NavMesh";
		/** The layer to place BackgroundImage prefabs on */
		public string backgroundImageLayer = "BackgroundImage";
		/** The layer to place deactivated objects on */
		public string deactivatedLayer = "Ignore Raycast";
		
		// Loading screen

		/** If True, then a specific scene will be loaded in-between scene transitions, to be used as a loading screen */
		public bool useLoadingScreen = false;
		/** If True, then scenes will not be automatically activated upon loading, and activation will be delayed until the SceneChanger script's ActivateLoadedScene function is called. */
		public bool manualSceneActivation = false;
		/** How the scene that acts as a loading scene is chosen (Number, Name) */
		public ChooseSceneBy loadingSceneIs = ChooseSceneBy.Number;
		/** If True, scenes will be loaded from Addressables, and names will be used for keys */
		public bool loadScenesFromAddressable = false;
		/** The name of the scene to act as a loading scene, if loadingScene = ChooseSceneBy.Name */
		public string loadingSceneName = "";
		/** The number of the scene to act as a loading scene, if loadingScene = ChooseSceneBy.Number */
		public int loadingScene = 0;
		/** If True, scenes will be loaded asynchronously */
		public bool useAsyncLoading = false;
		/** The delay, in seconds, before and after loading, if both useLoadingScreen = True and useAsyncLoading = True */
		public float loadingDelay = 0f;
		/** If True then the game will turn black while the scene initialises itself, which can be useful when restoring animation states */
		public bool blackOutWhenInitialising = true;
		/** If True, the required PersistentEngine will be created by spawning the prefab from Resources, as opposed to generating it from scratch */
		public bool spawnPersistentEnginePrefab = true;

		// Sound settings

		/** If True, then music can play when the game is paused */
		public bool playMusicWhilePaused = false;
		/** If True, then ambience can play when the game is paused */
		public bool playAmbienceWhilePaused = false;
		/** A list of all AudioClips that can be played as music using the "Sound: Play music" Action */
		public List<MusicStorage> musicStorages = new List<MusicStorage>();
		/** A list of all AudioClips that can be played as ambience using the "Sound: Play ambience" Action */
		public List<MusicStorage> ambienceStorages = new List<MusicStorage>();
		/** The fade-in duration when resuming ambience audio after loading a save game */
		public float loadAmbienceFadeTime = 0f;
		/** If True, and loadAmbienceFadeTime > 0, then previously-playing ambience audio will be crossfaded out upon loading */
		public bool crossfadeAmbienceWhenLoading = false;
		/** If True, then the ambience track at the time of saving will be resumed from the start upon loading */
		public bool restartAmbienceTrackWhenLoading = false;
		/** The fade-in duration when resuming music audio after loading a save game */
		public float loadMusicFadeTime = 0f;
		/** If True, and loadMusicFadeTime > 0, then previously-playing music audio will be crossfaded out upon loading */
		public bool crossfadeMusicWhenLoading = false;
		/** If True, then the music track at the time of saving will be resumed from the start upon loading */
		public bool restartMusicTrackWhenLoading = false;
		/** If True, then playing Music will force all other Sounds in the scene to stop if they are also playing Music */
		public bool autoEndOtherMusicWhenPlayed = true;
		/** A prefab override for the Music object */
		public Music musicPrefabOverride = null;
		/** A prefab override for the Ambience object */
		public Ambience ambiencePrefabOverride = null;

		/** How volume is controlled (AudioSources, AudioMixerGroups) (Unity 5 only) */
		public VolumeControl volumeControl = VolumeControl.AudioSources;
		/** The AudioMixerGroup for music audio, if volumeControl = VolumeControl.AudioSources */
		public AudioMixerGroup musicMixerGroup = null;
		/** The AudioMixerGroup for SF audio, if volumeControl = VolumeControl.AudioSources */
		public AudioMixerGroup sfxMixerGroup = null;
		/** The AudioMixerGroup for speech audio, if volumeControl = VolumeControl.AudioSources */
		public AudioMixerGroup speechMixerGroup = null;
		/** The name of the parameter in musicMixerGroup that controls attenuation */
		public string musicAttentuationParameter = "musicVolume";
		/** The name of the parameter in sfxMixerGroup that controls attenuation */
		public string sfxAttentuationParameter = "sfxVolume";
		/** The name of the parameter in speechMixerGroup that controls attenuation */
		public string speechAttentuationParameter = "speechVolume";

		// Options data

		/** The game's default language index */
		public int defaultLanguage = 0;
		/** The game's default voice language index (if SpeechManager.separateVoiceAndTextLanguages = True) */
		public int defaultVoiceLanguage = 0;
		/** The game's default subtitles state */
		public bool defaultShowSubtitles = false;
		/** The game's default SFX audio volume */
		public float defaultSfxVolume = 0.9f;
		/** The game's default music audio volume */
		public float defaultMusicVolume = 0.6f;
		/** The game's default speech audio volume */
		public float defaultSpeechVolume = 1f;

		/** Determines when logs are written to the Console (Always, OnlyInEditor, Never) */
		public ShowDebugLogs showDebugLogs = ShowDebugLogs.Always;
		/** (DEPRECATED) */
		public bool printActionCommentsInConsole = false;
		/** When comments attached to Actions should be printed in the Console when the Action is run */
		public ActionCommentLogging actionCommentLogging = ActionCommentLogging.Never;
		/** Used to show all currently-running ActionLists will be listed in the corner of the screen */
		public DebugWindowDisplays showActiveActionLists = DebugWindowDisplays.Never;


		#if UNITY_EDITOR
		
		/**
		 * Shows the GUI.
		 */
		public void ShowGUI ()
		{

			ShowSaveGameSettings ();

			EditorGUILayout.Space ();

			ShowCutsceneSettings ();

			EditorGUILayout.Space ();

			ShowPlayerSettings ();

			EditorGUILayout.Space ();

			ShowInterfaceSettings ();

			ShowTouchScreenSettings ();

			EditorGUILayout.Space ();

			ShowInventorySettings ();

			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			if (assumeInputsDefined)
			{
				showRequiredInputs = CustomGUILayout.ToggleHeader (showRequiredInputs, "Required inputs");
			}
			else
			{
				showRequiredInputs = CustomGUILayout.ToggleHeader (showRequiredInputs, "Available inputs");
			}
			if (showRequiredInputs)
			{
				EditorGUILayout.HelpBox ("The following inputs are available for the chosen interface settings:" + GetInputList (), MessageType.Info);
				assumeInputsDefined = CustomGUILayout.ToggleLeft ("Assume inputs are defined?", assumeInputsDefined, "AC.KickStarter.settingsManager.assumeInputsDefined");
				if (assumeInputsDefined)
				{
					EditorGUILayout.HelpBox ("Try/catch statements used when checking for input will be bypassed - this results in better performance, but all available inputs must be defined.", MessageType.Warning);
				}
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			ShowMovementSettings ();
			
			EditorGUILayout.Space ();
			ShowCameraSettings ();
			
			EditorGUILayout.Space ();
			ShowHotspotSettings ();

			ShowAudioSettings ();

			EditorGUILayout.Space ();
			ShowRaycastSettings ();

			EditorGUILayout.Space ();
			ShowSceneLoadingSettings ();

			EditorGUILayout.Space ();
			ShowOptionsSettings ();

			EditorGUILayout.Space ();
			ShowDebugSettings ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);
			}
		}


		private void ShowSaveGameSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSave = CustomGUILayout.ToggleHeader (showSave, "Save game settings");
			if (showSave)
			{
				if (string.IsNullOrEmpty (saveFileName))
				{
					string[] s = Application.dataPath.Split ('/');
					saveFileName = s[s.Length - 2];
				}
				maxSaves = CustomGUILayout.IntField ("Max. number of saves:", maxSaves, "AC.KickStarter.settingsManager.maxSaves", "The maximum number of save files that can be created.");

				saveFileName = CustomGUILayout.DelayedTextField ("Save filename:", saveFileName, "AC.KickStarter.settingsManager.saveFileName", "The name to give save game files.");
				if (!string.IsNullOrEmpty (saveFileName))
				{
					if (saveFileName.Contains (" "))
					{
						EditorGUILayout.HelpBox ("The save filename cannot contain 'space' characters - please remove them to prevent file-handling issues.", MessageType.Warning);
					}
					else
					{
						#if !(UNITY_WP8 || UNITY_WINRT)
						string newSaveFileName = System.Text.RegularExpressions.Regex.Replace (saveFileName, "[^\\w\\._]", "");
						if (saveFileName != newSaveFileName)
						{
							EditorGUILayout.HelpBox ("The save filename contains special characters - please remove them to prevent file-handling issues.", MessageType.Warning);
						}
						#endif
					}
					separateEditorSaveFiles = CustomGUILayout.ToggleLeft ("Use '_Editor' prefix for Editor save files?", separateEditorSaveFiles, string.Empty, "If True, then save files and PlayerPrefs keys will not be shared between Editor and Builds.");
				}

				useProfiles = CustomGUILayout.ToggleLeft ("Enable save game profiles?", useProfiles, "AC.KickStarter.settingsManager.useProfiles", "If True, then multiple save profiles - each with its own save files and options data - can be created");
				#if ADVANCED_SAVING
				saveTimeDisplay = (SaveTimeDisplay) CustomGUILayout.EnumPopup ("Time display:", saveTimeDisplay, "AC.KickStarter.settingsManager.saveTimeDisplay", "How the time of a save file should be displayed");
				if (saveTimeDisplay == SaveTimeDisplay.CustomFormat)
				{
					customSaveFormat = CustomGUILayout.TextField ("Time display format:", customSaveFormat, "AC.KickStarter.settingsManager.customSaveFormat", "The format of time display for save game labels");
				}

				if (takeSaveScreenshots)
				{
					saveScreenshots = SaveScreenshots.Always;
					takeSaveScreenshots = false;
				}

				saveScreenshots = (SaveScreenshots) CustomGUILayout.EnumPopup ("Save screenshots:", saveScreenshots, "AC.KickStarter.settingsManager.takeSaveScreenshots", "Determines when save-game screenshots are taken");
				if (saveScreenshots != SaveScreenshots.Never)
				{
					screenshotResolutionFactor = CustomGUILayout.Slider ("Screenshot size factor:", screenshotResolutionFactor, 0.1f, 1f, "AC.KickStarter.settingsManager.screenshotResolutionFactor", "The size of save-game screenshots, relative to the game window's actual resolution");
				}
				orderSavesByUpdateTime = CustomGUILayout.ToggleLeft ("Order save lists by update time?", orderSavesByUpdateTime, "AC.KickStarter.settingsManager.orderSavesByUpdateTime", "If True, then save files listed in SavesList menu elements will be displayed in order of update time");
				#else
				EditorGUILayout.HelpBox ("Save-game screenshots are disabled for the current platform.", MessageType.Info);
				takeSaveScreenshots = false;
				#endif

				saveWithThreading = CustomGUILayout.ToggleLeft ("Save using separate thread?", saveWithThreading, "AC.KickStarter.settingsManager.saveWithThreading", "If True, then game-saving will be handled by a separate CPU thread.");
				saveAssetReferencesWithAddressables = CustomGUILayout.ToggleLeft ("Save asset references with Addressables?", saveAssetReferencesWithAddressables, "AC.KickStarter.settingsManager.saveAssetReferencesWithAddressables", "If True, then references to assets made in save game files will be based on their Addressable name, and not Resources folder presence");

				if (saveAssetReferencesWithAddressables)
				{
					#if !AddressableIsPresent
					EditorGUILayout.HelpBox ("The 'AddressableIsPresent' preprocessor define must be declared in the Player Settings.", MessageType.Warning);
					#endif
				}

				referenceScenesInSave = (ChooseSceneBy) CustomGUILayout.EnumPopup ("Reference scenes by:", referenceScenesInSave, "AC.KickStarter.settingsManager.referenceScenesInSave", "How scenes are referenced in scene files (build index or filename)");

				if (GUILayout.Button ("Auto-add Save components to GameObjects"))
				{
					AssignSaveScripts ();
				}

				#if UNITY_EDITOR
				if (GUILayout.Button ("Manage save-game files"))
				{
					SaveFileManager.Init ();
				}
				#endif
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowCutsceneSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCutscene = CustomGUILayout.ToggleHeader (showCutscene, "Cutscene settings");
			if (showCutscene)
			{
				actionListOnStart = ActionListAssetMenu.AssetGUI ("ActionList on start game:", actionListOnStart, "OnStartGame", "AC.KickStarter.settingsManager.actionListOnStart", "The ActionListAsset to run when the game begins");
				blackOutWhenSkipping = CustomGUILayout.ToggleLeft ("Black out when skipping?", blackOutWhenSkipping, "AC.KickStarter.settingsManager.blackOutWhenSkipping", "If True, then the game will turn black whenever the user triggers the 'EndCutscene' input to skip a cutscene");
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowPlayerSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCharacter = CustomGUILayout.ToggleHeader (showCharacter, "Character settings");
			if (showCharacter)
			{
				playerSwitching = (PlayerSwitching) CustomGUILayout.EnumPopup ("Player switching:", playerSwitching, "AC.KickStarter.settingsManager.playerSwitching", "Whether or not the active Player can be swapped out or switched to at any time");
				if (playerSwitching == PlayerSwitching.DoNotAllow)
				{
					EditorGUILayout.BeginHorizontal ();
					player = (Player) CustomGUILayout.ObjectField <Player> ("Player prefab:", player, false, "AC.KickStarter.settingsManager.player", "The player prefab, to spawn in at runtime");
					if (player != null)
					{
						if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
						{
							GenericMenu menu = new GenericMenu ();
							menu.AddItem (new GUIContent ("Find references..."), false, PlayerCallback, "FindReferences");
							menu.ShowAsContext ();
						}
					}
					EditorGUILayout.EndHorizontal ();
				}
				else
				{
					for (int i = 0; i < players.Count; i++)
					{
						players[i].ShowGUI ("AC.KickStarter.settingsManager.players[" + i + "]");
					}

					if (GUILayout.Button ("Add new player"))
					{
						Undo.RecordObject (this, "Add player");
						
						PlayerPrefab newPlayer = new PlayerPrefab (GetPlayerIDArray ());
						players.Add (newPlayer);
					}
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private static void PlayerCallback (object obj)
		{
			switch (obj.ToString ())
			{
				case "FindReferences":
					PlayerPrefab.FindPlayerReferences (-1, KickStarter.settingsManager.player.GetName ());
					break;

				default:
					break;
			}
		}



		private void ShowInterfaceSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showInterface = CustomGUILayout.ToggleHeader (showInterface, "Interface settings");
			if (showInterface)
			{
				movementMethod = (MovementMethod) CustomGUILayout.EnumPopup ("Movement method:", movementMethod, "AC.KickStarter.settingsManager.movementMethod", "How the player character is controlled");
				inputMethod = (InputMethod) CustomGUILayout.EnumPopup ("Input method:", inputMethod, "AC.KickStarter.settingsManager.inputMethod", "The main input method used to control the game with");

				if (inputMethod == InputMethod.MouseAndKeyboard)
				{
					defaultMouseClicks = CustomGUILayout.ToggleLeft ("Mouse clicks have default functionality?", defaultMouseClicks, "AC.KickStarter.settingsManager.defaultMouseClicks", "If True, then left and right mouse clicks will have default behaviour");
				}
				else if (inputMethod == InputMethod.KeyboardOrController)
				{
					simulatedCursorMoveSpeed = CustomGUILayout.FloatField ("Simulated cursor speed:", simulatedCursorMoveSpeed, "AC.KickStarter.settingsManager.simulatedCursorMoveSpeed", "The movement speed of a keyboard or controller-controlled cursor");
				}
				interactionMethod = (AC_InteractionMethod) CustomGUILayout.EnumPopup ("Interaction method:", interactionMethod, "AC.KickStarter.settingsManager.interactionMethod", "How Hotspots are interacted with");

				if (interactionMethod == AC_InteractionMethod.CustomScript)
				{
					EditorGUILayout.HelpBox ("See the Manual's 'Custom interaction systems' section for information on how to trigger Hotspots and inventory items.", MessageType.Info);
				}

				if (CanUseCursor ())
				{
					if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
					{
						selectInteractions = (SelectInteractions) CustomGUILayout.EnumPopup ("Select Interactions by:", selectInteractions, "AC.KickStarter.settingsManager.selectInteractions", "How Interactions are triggered");
						if (selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot)
						{
							seeInteractions = (SeeInteractions) CustomGUILayout.EnumPopup ("See Interactions with:", seeInteractions, "AC.KickStarter.settingsManager.seeInteractions", "How Interaction menus are opened");

							if (inventoryInteractions == InventoryInteractions.Multiple)
							{
								autoHideInteractionIcons = CustomGUILayout.ToggleLeft ("Auto-hide Interaction icons based on Hotspot / item?", autoHideInteractionIcons, "AC.KickStarter.settingsManager.autoHideInteractionIcons", "If True, then Interaction icons will be hidden if linked to a Hotspot or InvItem that has no interaction linked to that icon");
							}
							else
							{
								autoHideInteractionIcons = CustomGUILayout.ToggleLeft ("Auto-hide Interaction icons based on Hotspot?", autoHideInteractionIcons, "AC.KickStarter.settingsManager.autoHideInteractionIcons", "If True, then Interaction icons will be hidden if linked to a Hotspot that has no interaction linked to that icon");
							}

							if (seeInteractions == SeeInteractions.ClickOnHotspot)
							{
								stopPlayerOnClickHotspot = CustomGUILayout.ToggleLeft ("Stop player moving when click Hotspot?", stopPlayerOnClickHotspot, "AC.KickStarter.settingsManager.stopPlayerOnClickHotspot", "If True, then the player will stop when a Hotspot is clicked on");
							}
							else if (seeInteractions == SeeInteractions.ViaScriptOnly)
							{
								EditorGUILayout.HelpBox ("Call 'AC.KickStarter.playerMenus.EnableInteractionMenus ();' to open Interaction Menus.", MessageType.Info);
							}
						}

						if (selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot)
						{
							autoCycleWhenInteract = CustomGUILayout.ToggleLeft ("Auto-cycle after an Interaction?", autoCycleWhenInteract, "AC.KickStarter.settingsManager.autoCycleWhenInteract", "If True, then triggering an Interaction will cycle the cursor mode");
							whenReselectHotspot = (WhenReselectHotspot) CustomGUILayout.EnumPopup ("When re-select Hotspot?", whenReselectHotspot, "AC.KickStarter.settingsManager.whenReselectHotspot", "What happens to the cursor icon when a hotspot (or inventory item, depending) is reselected");
						}
					
						if (SelectInteractionMethod () == SelectInteractions.ClickingMenu)
						{
							clickUpInteractions = CustomGUILayout.ToggleLeft ("Trigger interaction by releasing click?", clickUpInteractions, "AC.KickStarter.settingsManager.clickUpInteractions", "If True, then interactions can be triggered by releasing the mouse cursor over an icon");
							cancelInteractions = (CancelInteractions) CustomGUILayout.EnumPopup ("Close interactions with:", cancelInteractions, "AC.KickStarter.settingsManager.cancelInteractions", "The method to close Interaction menus");

							if (cancelInteractions == CancelInteractions.ViaScriptOnly)
							{
								EditorGUILayout.HelpBox ("Call 'AC.KickStarter.playerMenus.CloseInteractionMenus ();' to close Interaction Menus.", MessageType.Info);
							}
						}
						else
						{
							cancelInteractions = CancelInteractions.CursorLeavesMenu;
						}

						if (hotspotDetection == HotspotDetection.PlayerVicinity && SelectInteractionMethod () != SelectInteractions.CyclingCursorAndClickingHotspot)
						{
							closeInteractionMenusIfPlayerLeavesVicinity = CustomGUILayout.ToggleLeft ("Close interactions if Player leaves Hotspot's vicinity?", closeInteractionMenusIfPlayerLeavesVicinity, "AC.KickStarter.settingsManager.closeInteractionMenusIfPlayerLeavesVicinity", "If True, then Interaction Menus will close if the Player is no longer in the active Hotspot's vicinity");
						}
					}
				}
				else if (inputMethod == InputMethod.TouchScreen && 
				         interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (SelectInteractionMethod () == SelectInteractions.ClickingMenu)
					{
						clickUpInteractions = CustomGUILayout.ToggleLeft ("Trigger interaction by releasing tap?", clickUpInteractions, "AC.KickStarter.settingsManager.clickUpInteractions", "If True, then interactions can be triggered by releasing the touch over an icon");
						if (clickUpInteractions)
						{
							EditorGUILayout.HelpBox ("This option should be disabled if your Interaction Menu is built with Unity UI.", MessageType.Info);
						}
				
						closeInteractionMenuIfTapHotspot = CustomGUILayout.ToggleLeft ("Can close Interaction Menus by tapping another Hotspot?", closeInteractionMenuIfTapHotspot, "AC.KickStarter.settingsManager.closeInteractionMenuIfTapHotspot", "If True, then Interaction Menus can be closed by tapping another Hotspot for which they are opened.");
					}

					if (selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot)
					{
						if (inventoryInteractions == InventoryInteractions.Multiple)
						{
							autoHideInteractionIcons = CustomGUILayout.ToggleLeft ("Auto-hide Interaction icons based on Hotspot / item?", autoHideInteractionIcons, "AC.KickStarter.settingsManager.autoHideInteractionIcons", "If True, then Interaction icons will be hidden if linked to a Hotspot or InvItem that has no interaction linked to that icon");
						}
						else
						{
							autoHideInteractionIcons = CustomGUILayout.ToggleLeft ("Auto-hide Interaction icons based on Hotspot?", autoHideInteractionIcons, "AC.KickStarter.settingsManager.autoHideInteractionIcons", "If True, then Interaction icons will be hidden if linked to a Hotspot that has no interaction linked to that icon");
						}

						stopPlayerOnClickHotspot = CustomGUILayout.ToggleLeft ("Stop player moving when click Hotspot?", stopPlayerOnClickHotspot, "AC.KickStarter.settingsManager.stopPlayerOnClickHotspot", "If True, then the player will stop when a Hotspot is clicked on");
					}
				}

				if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					autoCycleWhenInteract = CustomGUILayout.ToggleLeft ("Reset cursor after an Interaction?", autoCycleWhenInteract, "AC.KickStarter.settingsManager.autoCycleWhenInteract", "If True, then triggering an Interaction will cycle the cursor mode");
					showHoverInteractionInHotspotLabel = CustomGUILayout.ToggleLeft ("Show hover Interaction icons in Hotspot label?", showHoverInteractionInHotspotLabel, "AC.KickStarter.settingsManager.showHoverInteractionInHotspotLabel", "If True, then the Hotspot label will show the name of the interaction icon being hovered over");
					allowDefaultinteractions = CustomGUILayout.ToggleLeft ("Set first 'Use' Hotspot interaction as default?", allowDefaultinteractions, "AC.KickStarter.settingsManager.allowDefaultinteractions", "If True, then invoking the 'DefaultInteractions' input button will run the first-enabled 'Use' interaction of the active Hotspot");
				}

				if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					alwaysCloseInteractionMenus = CustomGUILayout.ToggleLeft ("Close Interaction menus even if Interaction doesn't block gameplay?", alwaysCloseInteractionMenus, "AC.KickStarter.settingsManager.alwaysCloseInteractionMenus", "It True, Interaction menus will always close as the result of running an Interaction.  If False, they will only close if the resulting ActionList blocks gameplay.");
				}

				if (inputMethod == InputMethod.KeyboardOrController)
				{
					scaleCursorSpeedWithScreen = CustomGUILayout.ToggleLeft ("Scale cursor speed with screen size?", scaleCursorSpeedWithScreen, "AC.KickStarter.settingsManager.scaleCursorSpeedWithScreen", "If True, the cursor's movement speed will scale with the screen size, so that the time it takes to move across will be the same for all resolutions");
				}

				if (movementMethod == MovementMethod.FirstPerson && inputMethod == InputMethod.TouchScreen)
				{
					// First person dragging only works if cursor is unlocked
					lockCursorOnStart = false;
				}
				else
				{
					lockCursorOnStart = CustomGUILayout.ToggleLeft ("Lock cursor in screen's centre when game begins?", lockCursorOnStart, "AC.KickStarter.settingsManager.lockCursorOnStart", "If True, then the cursor will be locked in the centre of the screen when the game begins");
					hideLockedCursor = CustomGUILayout.ToggleLeft ("Hide cursor when locked in screen's centre?", hideLockedCursor, "AC.KickStarter.settingsManager.hideLockedCursor", "If True, then the cursor will be hidden whenever it is locked");
					if (movementMethod == MovementMethod.FirstPerson)
					{
						onlyInteractWhenCursorUnlocked = CustomGUILayout.ToggleLeft ("Disallow Interactions if cursor is locked?", onlyInteractWhenCursorUnlocked, "AC.KickStarter.settingsManager.onlyInteractWhenCursorUnlocked", "If True, then Hotspot interactions are only allowed if the cursor is unlocked");
					}
				}
				if (IsInFirstPerson ())
				{
					disableFreeAimWhenDragging = CustomGUILayout.ToggleLeft ("Disable free-aim when moving Draggables?", disableFreeAimWhenDragging, "AC.KickStarter.settingsManager.disableFreeAimWhenDragging", "If True, then free-aiming will be disabled while a Draggable object is manipulated");
					disableFreeAimWhenDraggingPickUp = CustomGUILayout.ToggleLeft ("Disable free-aim when moving PickUps?", disableFreeAimWhenDraggingPickUp, "AC.KickStarter.settingsManager.disableFreeAimWhenDraggingPickUp", "If True, then free-aiming will be disabled while a PickUp object is manipulated");

					if (movementMethod == MovementMethod.FirstPerson && !allowGameplayDuringConversations)
					{
						useFPCamDuringConversations = CustomGUILayout.ToggleLeft ("Run Conversations in first-person?", useFPCamDuringConversations, "AC.KickStarter.settingsManager.useFPCamDuringConversations", "If True, then first-person games will use the first-person camera during conversations");
					}
				}

				allowGameplayDuringConversations = CustomGUILayout.ToggleLeft ("Allow regular gameplay during Conversations?", allowGameplayDuringConversations, "AC.KickStarter.settingsManager.allowGameplayDuringConversations", "If True, then gameplay is allowed during Conversations");
				if (inputMethod != InputMethod.TouchScreen)
				{
					runConversationsWithKeys = CustomGUILayout.ToggleLeft ("Dialogue options can be selected with number keys?", runConversationsWithKeys, "AC.KickStarter.settingsManager.runConversationsWithKeys", "If True, then Conversation dialogue options can be triggered with the number keys");
				}

				unityUIClicksAlwaysBlocks = CustomGUILayout.ToggleLeft ("Unity UI blocks interaction and movement?", unityUIClicksAlwaysBlocks, "AC.KickStarter.settingsManager.unityUIClicksAlwaysBlocks", "If True, then movement and interaction clicks will be ignored if the cursor is over a Unity UI element - even those not linked to the Menu Manager");
				if (dragDropThreshold > 0f && Mathf.Approximately (dragThreshold, 0f))
				{
					dragThreshold = dragDropThreshold / 1080f;
					dragDropThreshold = 0f;
				}
				dragThreshold = CustomGUILayout.Slider ("Drag threshold:", dragThreshold, 0f, 0.1f, "AC.KickStarter.settingsManager.dragThreshold", "The proportion of the screen that the mouse must be dragged for drag effects to kick in");
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowInventorySettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showInventory = CustomGUILayout.ToggleHeader (showInventory, "Inventory settings");
			if (showInventory)
			{
				if (playerSwitching == PlayerSwitching.Allow)
				{
					shareInventory = CustomGUILayout.ToggleLeft ("All Players share same Inventory?", shareInventory, "AC.KickStarter.settingsManager.shareInventory", "If True, then all player prefabs will share the same inventory");
				}

				if (interactionMethod != AC_InteractionMethod.ContextSensitive)
				{
					inventoryInteractions = (InventoryInteractions) CustomGUILayout.EnumPopup ("Inventory interactions:", inventoryInteractions, "AC.KickStarter.settingsManager.inventoryInteractions", "How many interactions an inventory item can have");

					if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
					{
						if (selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot)
						{
							cycleInventoryCursors = CustomGUILayout.ToggleLeft ("Include Inventory items in Hotspot Interaction cycles?", cycleInventoryCursors, "AC.KickStarter.settingsManager.cycleInventoryCursors", "If True, then inventory items will be included in Interaction cursor cycles");
						}
						else
						{
							cycleInventoryCursors = CustomGUILayout.ToggleLeft ("Include Inventory items in Hotspot Interaction menus?", cycleInventoryCursors, "AC.KickStarter.settingsManager.cycleInventoryCursors", "If True, then inventory items will be included in Interaction menus cycles");
						}
					}
					else if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
					{
						cycleInventoryCursors = CustomGUILayout.ToggleLeft ("Include last-selected Inventory item in cursor cycles?", cycleInventoryCursors, "AC.KickStarter.settingsManager.cycleInventoryCursors", "If True, then the last-selected inventory item will be included in cursor cycles");

						if (inventoryInteractions == InventoryInteractions.Multiple)
						{
							allowDefaultInventoryInteractions = CustomGUILayout.ToggleLeft ("Set first 'Standard' Inventory interaction as default?", allowDefaultInventoryInteractions, "AC.KickStarter.settingsManager.allowDefaultInventoryInteractions", "If True, then invoking the 'DefaultInteractions' input button will run the first-enabled 'Standard' interaction of the active Inventory item");
						}
					}

					if (inventoryInteractions == InventoryInteractions.Multiple && CanSelectItems (false))
					{
						selectInvWithUnhandled = CustomGUILayout.ToggleLeft ("Select item if Interaction is unhandled?", selectInvWithUnhandled, "AC.KickStarter.settingsManager.selectInvWithUnhandled", "If True, then the item will be selected (in 'use' mode) if a particular Interaction is unhandled");
						if (selectInvWithUnhandled)
						{
							CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
							if (cursorManager != null && cursorManager.cursorIcons != null && cursorManager.cursorIcons.Count > 0)
							{
								selectInvWithIconID = GetIconID ("Select with unhandled:", selectInvWithIconID, cursorManager, "AC.KickStarter.settingsManager.selectInvWithIconID", "The Cursor interaction that selects the inventory item (in 'use' mode) when unhandled");
							}
							else
							{
								EditorGUILayout.HelpBox ("No Interaction cursors defined - please do so in the Cursor Manager.", MessageType.Info);
							}
						}

						giveInvWithUnhandled = CustomGUILayout.ToggleLeft ("Give item if Interaction is unhandled?", giveInvWithUnhandled, "AC.KickStarter.settingsManager.giveInvWithUnhandled", "If True, then the item will be selected (in 'give' mode) if a particular Interaction is unhandled");
						if (giveInvWithUnhandled)
						{
							CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
							if (cursorManager != null && cursorManager.cursorIcons != null && cursorManager.cursorIcons.Count > 0)
							{
								giveInvWithIconID = GetIconID ("Give with unhandled:", giveInvWithIconID, cursorManager, "AC.KickStarter.settingsManager.giveInvWithIconID", "The Cursor interaction that selects the inventory item (in 'give' mode) when unhandled");
							}
							else
							{
								EditorGUILayout.HelpBox ("No Interaction cursors defined - please do so in the Cursor Manager.", MessageType.Info);
							}
						}
					}
				}

				inventoryInteractionsHaltPlayer = CustomGUILayout.ToggleLeft ("Inventory interactions halt Player?", inventoryInteractionsHaltPlayer, "AC.KickStarter.settingsManager.inventoryInteractionsHaltPlayer", "If True, then the player will stop pathfinding upon interacting with an inventory item");

				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
				{}
				else
				{
					reverseInventoryCombinations = CustomGUILayout.ToggleLeft ("Combine interactions work in reverse?", reverseInventoryCombinations, "AC.KickStarter.settingsManager.reverseInventoryCombinations", "If True, then invntory item combinations will also work in reverse");
				}

				if (CanSelectItems (false))
				{
					if ((SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot || interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot) && cycleInventoryCursors)
					{}
					else if (SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && inventoryInteractions == InventoryInteractions.Multiple)
					{}
					else
					{
						inventoryDragDrop = CustomGUILayout.ToggleLeft ("Drag and drop Inventory interface?", inventoryDragDrop, "AC.KickStarter.settingsManager.InventoryDragDrop", "If True, then inventory items can be drag-dropped (i.e. used on Hotspots and other items with a single mouse button press");
					}
					if (InventoryDragDrop)
					{
						if (inventoryInteractions == AC.InventoryInteractions.Single || interactionMethod == AC_InteractionMethod.ContextSensitive)
						{
							inventoryDropLook = CustomGUILayout.ToggleLeft ("Can drop an Item onto itself to Examine it?", inventoryDropLook, "AC.KickStarter.settingsManager.inventoryDropLook", "If True, then using an inventory item on itself will trigger its Examine interaction");
							if (dragThreshold > 0f)
							{
								inventoryDropLookNoDrag = CustomGUILayout.ToggleLeft ("Clicking an Item without dragging Examines it?", inventoryDropLookNoDrag, "AC.KickStarter.settingsManager.inventoryDropLookNoDrag", "If True, then using an inventory item on itself, without first dragging it, will trigger its Examine interaction");
							}
						}
						else if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
						{
							if (dragThreshold > 0f)
							{
								inventoryDropLookNoDrag = CustomGUILayout.ToggleLeft ("Select item if drag before opening Interaction menu?", inventoryDropLookNoDrag, "AC.KickStarter.settingsManager.inventoryDropLookNoDrag", "If True, then Inventory interaction menus will only be shown when when releasing a click, so that they can be drag-dropped before showing.");
							}
						}
					}
					else
					{
						if (interactionMethod == AC_InteractionMethod.ContextSensitive || inventoryInteractions == InventoryInteractions.Single)
						{
							if (!(interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && cycleInventoryCursors))
							{
								rightClickInventory = (RightClickInventory) CustomGUILayout.EnumPopup ("Right-click active item:", rightClickInventory, "AC.KickStarter.settingsManager.rightClickInventory", "What happens when right-clicking while an inventory item is selected");

								if (rightClickInventory == RightClickInventory.ExaminesHotspot && interactionMethod != AC_InteractionMethod.ContextSensitive)
								{
									EditorGUILayout.HelpBox ("The above option is only available for the 'Context Sensitive' interaction method.", MessageType.Warning);
								}
							}
						}
						else if (interactionMethod != AC_InteractionMethod.CustomScript)
						{
							if (!(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.cycleInventoryCursors))
							{
								bool rightClickDeselect = (rightClickInventory == RightClickInventory.DeselectsItem);
								rightClickDeselect = CustomGUILayout.ToggleLeft ("Right-click deselects active item?", rightClickDeselect, "AC.KickStarter.settingsManager.rightClickInventory", "If True, right-clicking will de-select the active inventory item");
								rightClickInventory = (rightClickDeselect) ? RightClickInventory.DeselectsItem : RightClickInventory.DoesNothing;
							}
						}
					}
					autoDisableUnhandledHotspots = CustomGUILayout.ToggleLeft ("Auto-disable Hotspots with no interaction for selected item?", autoDisableUnhandledHotspots, "AC.KickStarter.settingsManager.autoDisableUnhandledHotspots", "If True, Hotspots that have no interaction associated with a given inventory item will not be active while that item is selected");
				}

				if (CanSelectItems (false) && !inventoryDragDrop)
				{
					inventoryDisableDefined = CustomGUILayout.ToggleLeft ("Defined interactions deselect active item?", inventoryDisableDefined, "AC.KickStarter.settingsManager.inventoryDisableDefined", "If True, then triggering a defined Inventory interaction will-deselect the active inventory item");
					inventoryDisableUnhandled = CustomGUILayout.ToggleLeft ("Unhandled interactions deselect active item?", inventoryDisableUnhandled, "AC.KickStarter.settingsManager.inventoryDisableUnhandled", "If True, then triggering an unhandled Inventory interaction will de-select the active inventory item");
					inventoryDisableLeft = CustomGUILayout.ToggleLeft ("Left-click deselects active item?", inventoryDisableLeft, "AC.KickStarter.settingsManager.inventoryDisableLeft", "If True, then left-clicking will de-select an inventory item");

					if (movementMethod == MovementMethod.PointAndClick && !inventoryDisableLeft)
					{
						canMoveWhenActive = CustomGUILayout.ToggleLeft ("Can move player if an Item is active?", canMoveWhenActive, "AC.KickStarter.settingsManager.canMoveWhenActive", "If True, then the player can move while an inventory item is selected");
					}
				}

				if (!allowGameplayDuringConversations)
				{
					allowInventoryInteractionsDuringConversations = CustomGUILayout.ToggleLeft ("Allow inventory interactions during Conversations?", allowInventoryInteractionsDuringConversations, "AC.KickStarter.settingsManager.allowInventoryInteractionsDuringConversations", "If True, inventory can be interacted with while a Conversation is active");
				}
				inventoryActiveEffect = (InventoryActiveEffect) CustomGUILayout.EnumPopup ("Active cursor FX:", inventoryActiveEffect, "AC.KickStarter.settingsManager.inventoryActiveEffect", "The effect to apply to an active inventory item's icon");
				if (inventoryActiveEffect == InventoryActiveEffect.Pulse)
				{
					inventoryPulseSpeed = CustomGUILayout.Slider ("Active FX pulse speed:", inventoryPulseSpeed, 0.5f, 2f, "AC.KickStarter.settingsManager.inventoryPulseSpeed", "The speed at which to pulse the active inventory item's icon");
				}

				activeWhenUnhandled = CustomGUILayout.ToggleLeft ("Show Active FX when an Interaction is unhandled?", activeWhenUnhandled, "AC.KickStarter.settingsManager.activeWhenUnhandled", "If True, then the inventory item will show its active effect when hovering over a Hotspot that has no matching Interaction");
				canReorderItems = CustomGUILayout.ToggleLeft ("Items can be re-ordered in Menus?", canReorderItems, "AC.KickStarter.settingsManager.canReorderItems", "If True, then inventory items can be re-ordered in an InventoryBox menu element by the player");
				selectInventoryDisplay = (SelectInventoryDisplay) CustomGUILayout.EnumPopup ("Selected item's display:", selectInventoryDisplay, "AC.KickStarter.settingsManager.selectInventoryDisplay", "How the currently-selected inventory item should be displayed in InventoryBox menu element");
				activeWhenHover = CustomGUILayout.ToggleLeft ("Show Active FX when Cursor hovers over Item in Menu?", activeWhenHover, "AC.KickStarter.settingsManager.activeWhenHover", "If True, then an inventory item will show its 'active' texture when the mouse hovers over it");
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowMovementSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showMovement = CustomGUILayout.ToggleHeader (showMovement, "Movement settings");
			if (showMovement)
			{
				if (movementMethod == MovementMethod.FirstPerson)
				{
					freeAimSmoothSpeed = CustomGUILayout.FloatField ("Free-aim acceleration:", freeAimSmoothSpeed, "AC.KickStarter.settingsManager.freeAimSmoothSpeed", "The acceleration for free-aiming smoothing");

					if (inputMethod != InputMethod.TouchScreen)
					{
						dragWalkThreshold = CustomGUILayout.FloatField ("Max free-aim speed:", dragWalkThreshold, "AC.KickStarter.settingsManager.dragWalkThreshold", "The maximum speed of free-aiming");
					}

					if (inputMethod != InputMethod.TouchScreen || firstPersonTouchScreen == FirstPersonTouchScreen.CustomInput)
					{
						firstPersonMovementSmoothing = CustomGUILayout.Toggle ("Movement smoothing?", firstPersonMovementSmoothing, "AC.KickStarter.settingsManager.firstPersonMovementSmoothing", "If True, then movement in first-person will be made smooth");
					}
				}

				if (movementMethod == MovementMethod.StraightToCursor)
				{
					dragRunThreshold = CustomGUILayout.FloatField ("Run threshold:", dragRunThreshold, "AC.KickStarter.settingsManager.dragRunThreshold", "If the cursor is this far from the player, the player will run");
					singleTapStraight = CustomGUILayout.ToggleLeft ("Single-clicking also moves player?", singleTapStraight, "AC.KickStarter.settingsManager.singleTapStraight", "If True, then single-clicking also moves the player");
					if (singleTapStraight)
					{
						singleTapStraightPathfind = CustomGUILayout.ToggleLeft ("Pathfind when single-clicking?", singleTapStraightPathfind, "AC.KickStarter.settingsManager.singleTapStraightPathfind", "If True, then single-clicking will make the player pathfind");
						if (singleTapStraightPathfind)
						{
							doubleClickMovement = (DoubleClickMovement) CustomGUILayout.EnumPopup ("Double-click movement:", doubleClickMovement, "AC.KickStarter.settingsManager.doubleClickMovement", "What effect double-clicking has on player movement");
						}
						clickHoldSeparationStraight = CustomGUILayout.Slider ("Click/hold separation (s):", clickHoldSeparationStraight, 0f, 2f, "AC.KickStarter.settingsManager.clickHoldSeparationStraight", "The duration in seconds that separates a single click/tap from a held click/tap");
					}
				}
				if ((inputMethod == InputMethod.TouchScreen && movementMethod != MovementMethod.PointAndClick) || movementMethod == MovementMethod.Drag)
				{
					dragWalkThreshold = CustomGUILayout.FloatField ("Walk threshold:", dragWalkThreshold, "AC.KickStarter.settingsManager.dragWalkThreshold", "The minimum drag magnitude needed to move the player");
					dragRunThreshold = CustomGUILayout.FloatField ("Run threshold:", dragRunThreshold, "AC.KickStarter.settingsManager.dragRunThreshold", "The minimum drag magnitude needed to make the player run");

					if (movementMethod == MovementMethod.FirstPerson && inputMethod == InputMethod.TouchScreen)
					{
						freeAimTouchSpeed = CustomGUILayout.FloatField ("Free-aim speed:", freeAimTouchSpeed, "AC.KickStarter.settingsManager.freeAimTouchSpeed", "The free-look speed when rotating a first-person camera");
					}

					drawDragLine = CustomGUILayout.ToggleLeft ("Draw drag line?", drawDragLine, "AC.KickStarter.settingsManager.drawDragLine", "If True, then a drag line will be drawn on screen");
					if (drawDragLine)
					{
						dragLineWidth = CustomGUILayout.FloatField ("Drag line width:", dragLineWidth, "AC.KickStarter.settingsManager.dragLineWidth", "The width of the drag line");
						dragLineColor = CustomGUILayout.ColorField ("Drag line colour:", dragLineColor, "AC.KickStarter.settingsManager.dragLineColor", "The colour of the drag line");
					}
					else
					{
						EditorGUILayout.HelpBox ("The 'OnUpdateDragLine' event can be used to display custom drag lines / joystick on-screen.", MessageType.Info);
					}

					if (inputMethod == InputMethod.TouchScreen && movementMethod == MovementMethod.FirstPerson && firstPersonTouchScreen == FirstPersonTouchScreen.CustomInput)
					{
						directMovementType = (DirectMovementType) CustomGUILayout.EnumPopup ("Turning type:", directMovementType, "AC.KickStarter.settingsManager.directMovementType", "How the player moves");
					}
				}
				else if (movementMethod == MovementMethod.Direct)
				{
					magnitudeAffectsDirect = CustomGUILayout.ToggleLeft ("Input magnitude affects speed?", magnitudeAffectsDirect, "AC.KickStarter.settingsManager.magnitudeAffectsDirect", "If True, then the magnitude of the input axis will affect the Player's speed");
					directTurnsInstantly = CustomGUILayout.ToggleLeft ("Turn instantly when under player control?", directTurnsInstantly, "AC.KickStarter.settingsManager.directTurnsInstantly", "If True, then the Player will turn instantly when moving during gameplay");
					if (!directTurnsInstantly)
					{
						stopTurningWhenReleaseInput = CustomGUILayout.ToggleLeft ("Stop turning when release input?", stopTurningWhenReleaseInput, "AC.KickStarter.settingsManager.stopTurningWhenReleaseInput", "If True, then the Player will stop turning when input is released");
					}
					directMovementType = (DirectMovementType) CustomGUILayout.EnumPopup ("Direct-movement type:", directMovementType, "AC.KickStarter.settingsManager.directMovementType", "How the player moves");
					if (directMovementType == DirectMovementType.RelativeToCamera)
					{
						limitDirectMovement = (LimitDirectMovement) CustomGUILayout.EnumPopup ("Movement limitation:", limitDirectMovement, "AC.KickStarter.settingsManager.limitDirectMovement", "How to limit the player's movement");
						if (cameraPerspective == CameraPerspective.ThreeD)
						{
							directMovementPerspective = CustomGUILayout.ToggleLeft ("Account for player's position on screen?", directMovementPerspective, "AC.KickStarter.settingsManager.directMovementPerspective", "If True, then the player's position on screen will be accounted for");
						}
						if (cameraPerspective != CameraPerspective.TwoD)
						{
							cameraLockSnapAngleThreshold = CustomGUILayout.Slider ("Max camera lock angle:", cameraLockSnapAngleThreshold, 0f, 20f, "AC.KickStarter.settingsManager.cameraLockSnapAngleThreshold", "If greater than zero, player direction will be unchanged when the camera angle changes during gameplay if the input does not exceed this angle");
						}
					}
				}
				else if (movementMethod == MovementMethod.PointAndClick ||
					(movementMethod == MovementMethod.StraightToCursor && 
					(singleTapStraight && singleTapStraightPathfind)))
				{
					clickPrefab = (Transform) CustomGUILayout.ObjectField <Transform> ("Click marker:", clickPrefab, false, "AC.KickStarter.settingsManager.clickPrefab", "A prefab to instantiate whenever the user clicks to move the player");
					if (clickPrefab != null)
					{
						clickMarkerPosition = (ClickMarkerPosition)CustomGUILayout.EnumPopup ("Click marker position:", clickMarkerPosition, "AC.KickStarter.settingsManager.clickMarkerPosition", "Where the spawned 'Click marker' is placed");
					}
					walkableClickRange = CustomGUILayout.Slider ("NavMesh search %:", walkableClickRange, 0f, 1f, "AC.KickStarter.settingsManager.walkableClickRange", "How much of the screen will be searched for a suitable NavMesh, if the user doesn't click directly on one");
					if (walkableClickRange > 0f)
					{
						navMeshSearchDirection = (NavMeshSearchDirection) CustomGUILayout.EnumPopup ("NavMesh search direction:", navMeshSearchDirection, "AC.KickStarter.settingsManager.navMeshSearchDirection", "How the nearest NavMesh to a cursor click is found, in screen space, if the user doesn't click directly on one");

						if (navMeshSearchDirection == NavMeshSearchDirection.RadiallyOutwardsFromCursor)
						{
							ignoreOffScreenNavMesh = CustomGUILayout.ToggleLeft ("Ignore NavMeshes off-screen?", ignoreOffScreenNavMesh, "AC.KickStarter.settingsManager.ignoreOffScreenNavMesh", "If True, then clicks will not detect NavMeshes that are off-screen");
						}
					}

					if (movementMethod == MovementMethod.PointAndClick)
					{
						doubleClickMovement = (DoubleClickMovement) CustomGUILayout.EnumPopup ("Double-click movement:", doubleClickMovement, "AC.KickStarter.settingsManager.doubleClickMovement", "What effect double-clicking has on player movement");
					}
				}
				else if (movementMethod == MovementMethod.FirstPerson)
				{
					directMovementType = (DirectMovementType) CustomGUILayout.EnumPopup ("Turning type:", directMovementType, "AC.KickStarter.settingsManager.directMovementType", "How the player moves");
				}

				if (movementMethod != MovementMethod.PointAndClick &&
					interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
					selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot)
				{
					disableMovementWhenInterationMenusAreOpen = CustomGUILayout.ToggleLeft ("Disable movement when Interaction menus are on?", disableMovementWhenInterationMenusAreOpen, "AC.KickStarter.settingsManager.disableMovementWhenInterationMenusAreOpen", "If True, and Interaction menus are used, movement will be prevented while they are on");
				}
				if (movementMethod == MovementMethod.Direct || movementMethod == MovementMethod.FirstPerson)
				{
					jumpSpeed = CustomGUILayout.Slider ("Jump speed:", jumpSpeed, 1f, 20f, "AC.KickStarter.settingsManager.jumpSpeed", "The player's jump speed");
				}
				
				destinationAccuracy = CustomGUILayout.Slider ("Destination accuracy:", destinationAccuracy, 0f, 1f, "AC.KickStarter.settingsManager.destinationAccuracy", "How accurate characters will be when navigating to set points on a NavMesh");
				if (destinationAccuracy >= 1f)
				{
					experimentalAccuracy = CustomGUILayout.ToggleLeft ("Attempt to be super-accurate? (Experimental)", experimentalAccuracy, "AC.KickStarter.settingsManager.experimentalAccuracy", "If True, then characters will lerp to their destination when very close, to ensure they end up at exactly the intended point");
				}
				pathfindUpdateFrequency = CustomGUILayout.Slider ("Pathfinding update time (s)", pathfindUpdateFrequency, 0f, 5f, "AC.KickStarter.settingsManager.pathfindUpdateFrequency", "If >0, the time (in seconds) between pathfinding recalculations occur");

				if (movementMethod == MovementMethod.StraightToCursor)
				{
					EditorGUILayout.HelpBox ("If the 'Pathfinding update time' is non-zero, the Player will pathfind to the cursor when moving.", MessageType.Info);
				}

				if (cameraPerspective == CameraPerspective.TwoD)
				{
					if (movingTurning == MovingTurning.TopDown || movingTurning == MovingTurning.Unity2D)
					{
						verticalReductionFactor = CustomGUILayout.Slider ("Vertical movement factor:", verticalReductionFactor, 0.1f, 1f, "AC.KickStarter.settingsManager.verticalReductionFactor", "How much slower vertical movement is compared to horizontal movement");
						rotationsAffectedByVerticalReduction = CustomGUILayout.ToggleLeft ("Character rotations affected by 'Vertical movement factor'?", rotationsAffectedByVerticalReduction, "AC.KickStarter.settingsManager.rotationsAffectedByVerticalReduction", "If True, then rotations of 2D characters will be affected by the verticalReductionFactor value");
					}
					if (movingTurning == MovingTurning.Unity2D)
					{
						alwaysPathfindInSpriteDirection = CustomGUILayout.ToggleLeft ("Always move along Paths in sprite direction?", alwaysPathfindInSpriteDirection, "AC.KickStarter.settingsManager.alwaysPathfindInSpriteDirection", "If True, then 2D characters will move according to their sprite direction when moving along a Path / pathfinding, allowing for smooth movement at corners");
					}
				}

				if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot || interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					walkToHotspotMarkers = CustomGUILayout.ToggleLeft ("Always move to Hotspot Markers?", walkToHotspotMarkers, "AC.KickStarter.settingsManager.walkToHotspotMarkers", "If True, then clicking Hotspots without running a particular Interaction will cause the Player to move to the Hotspot's 'Walk-to Marker'");
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowTouchScreenSettings ()
		{
			if (inputMethod == InputMethod.TouchScreen)
			{
				EditorGUILayout.Space ();

				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showTouchScreen = CustomGUILayout.ToggleHeader (showTouchScreen, "Touch-screen settings");
				if (showTouchScreen)
				{
					if (movementMethod != MovementMethod.FirstPerson)
					{
						offsetTouchCursor = CustomGUILayout.ToggleLeft ("Moving touch drags cursor?", offsetTouchCursor, "AC.KickStarter.settingsManager.offsetTouchCursor", "If True, then the cursor is not set to the touch point, but instead is moved by dragging");

						if (movementMethod == MovementMethod.Direct)
						{
							directTouchScreen = (DirectTouchScreen) CustomGUILayout.EnumPopup ("Direct movement:", directTouchScreen, "AC.KickStarter.settingsManager.directTouchScreen", "How Direct movement should work when using touch-screen controls");
							if (directTouchScreen == DirectTouchScreen.CustomInput)
							{
								EditorGUILayout.HelpBox ("Movement can be controlled by simulating/overriding the 'Horizontal', 'Vertical' and 'Run' inputs - see 'Remapping inputs' in the Manual.", MessageType.Info);
							}
						}
					}
					else
					{
						firstPersonTouchScreen = (FirstPersonTouchScreen) CustomGUILayout.EnumPopup ("First-person movement:", firstPersonTouchScreen, "AC.KickStarter.settingsManager.firstPersonTouchScreen", "How First Person movement should work when using touch-screen controls");
						if (firstPersonTouchScreen == FirstPersonTouchScreen.CustomInput)
						{
							EditorGUILayout.HelpBox ("Movement can be controlled by overriding the 'Horizontal' and 'Vertical' axes, and Free-aiming can be controlled by overriding InputGetFreeAimDelegate - see 'Remapping inputs' in the Manual.", MessageType.Info);
						}
					}
					doubleTapHotspots = CustomGUILayout.ToggleLeft ("Activate Hotspots with double-tap?", doubleTapHotspots, "AC.KickStarter.settingsManager.doubleTapHotspots", "If True, then Hotspots are activated by double-tapping");
					touchUpWhenPaused = CustomGUILayout.ToggleLeft ("Release touch to interact with AC Menus?", touchUpWhenPaused, "AC.KickStarter.settingsManager.touchUpWhenPaused", "If True, then menu interactions are performed by releasing a touch, rather than beginning one");

					if (movementMethod != MovementMethod.FirstPerson && offsetTouchCursor)
					{
						touchUpInteractScene = CustomGUILayout.ToggleLeft ("Release touch to interact with scene? (Experimental)", touchUpInteractScene, "AC.KickStarter.settingsManager.touchUpInteractScene", "If True, then scene interactions are performed by releasing a touch, rather than beginning one");
					}
				}
				CustomGUILayout.EndVertical ();
			}
		}


		private void ShowCameraSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCamera = CustomGUILayout.ToggleHeader (showCamera, "Camera settings");
			if (showCamera)
			{
				if (KickStarter.sceneSettings != null && KickStarter.sceneSettings.OverridesCameraPerspective ())
				{
					EditorGUILayout.HelpBox ("The current scene overrides the camera perspective - some fields only apply to the global perspective, below.", MessageType.Info);
				}

				if (movementMethod == MovementMethod.FirstPerson)
				{
					cameraPerspective = CameraPerspective.ThreeD;
					cameraPerspective_int = (int) cameraPerspective;
				}
				else
				{
					cameraPerspective_int = (int) cameraPerspective;
					cameraPerspective_int = CustomGUILayout.Popup ("Camera perspective:", cameraPerspective_int, cameraPerspective_list, "AC.KickStarter.settingsManager.cameraPerspective", "The game's default camera perspective, unless overridden in the scene");
					cameraPerspective = (CameraPerspective) cameraPerspective_int;
				}

				if (cameraPerspective == CameraPerspective.TwoD)
				{
					movingTurning = (MovingTurning) CustomGUILayout.EnumPopup ("Moving and turning:", movingTurning, "AC.KickStarter.settingsManager.movingTurning", "The method of moving and turning in 2D games");
					if (movingTurning == MovingTurning.TopDown)
					{
						EditorGUILayout.HelpBox ("This mode is now deprecated - use Unity 2D mode instead.", MessageType.Warning);
					}
				}
				
				if (forceAspectRatio)
				{ 
					forceAspectRatio = false;
					aspectRatioEnforcement = AspectRatioEnforcement.Fixed;
				}

				aspectRatioEnforcement = (AspectRatioEnforcement) CustomGUILayout.EnumPopup ("Aspect ratio:", aspectRatioEnforcement, "AC.KickStarter.settingsManager.aspectRatioEnforcement", "What type of aspect ratio to enforce.");
				if (aspectRatioEnforcement != AspectRatioEnforcement.NoneEnforced)
				{
					if (aspectRatioEnforcement == AspectRatioEnforcement.Fixed)
					{
						wantedAspectRatio = CustomGUILayout.FloatField ("Fixed aspect ratio:", wantedAspectRatio, "AC.KickStarter.settingsManager.wantedAspectRatio", "The aspect ratio, as a decimal");
					}
					else if (aspectRatioEnforcement == AspectRatioEnforcement.Range)
					{
						wantedAspectRatio = CustomGUILayout.FloatField ("Minimum aspect ratio:", wantedAspectRatio, "AC.KickStarter.settingsManager.wantedAspectRatio", "The minimum aspect ratio, as a decimal");
						maxAspectRatio = CustomGUILayout.FloatField ("Maximum aspect ratio:", maxAspectRatio, "AC.KickStarter.settingsManager.maxAspectRatio", "The maximum aspect ratio, as a decimal");
					}
					#if UNITY_IPHONE
					landscapeModeOnly = CustomGUILayout.Toggle ("Landscape-mode only?", landscapeModeOnly, "AC.KickStarter.settingsManager.landscapeModeOnly", "If True, then the game can only be played in landscape mode");
					#endif

					renderBorderCamera = CustomGUILayout.ToggleLeft ("Render border camera?", renderBorderCamera, "AC.KickStarter.settingsManager.renderBorderCamera", "If True, a second camera is used to render borders.  This helps to prevent artefacts, but increases performance.");
				}

				cacheCameraMain = CustomGUILayout.ToggleLeft ("Cache 'Camera.main' variable?", cacheCameraMain, "AC.KickStarter.settingsManager.cacheCameraMain", "If True, Unity's Camera.main variable will be cached for a minor performance boost");

				linearColorTextures = CustomGUILayout.ToggleLeft ("Generate textures in Linear color space?", linearColorTextures, string.Empty, "If True, then textures created for camera crossfading and overlay effects will be saved in linear color space");

				#if MOBILE_PLATFORM
				relyOnSafeArea = CustomGUILayout.ToggleLeft ("Limit display to 'safe area'?", relyOnSafeArea, "AC.KickStarter.settingsManager.relyOnSafeArea", "If True, then the game display will be limited to Unity's 'Screen.safeArea' property, which accounts for notches on mobile devices.");
				#endif
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowHotspotSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showHotspot = CustomGUILayout.ToggleHeader (showHotspot, "Hotspot settings");
			if (showHotspot)
			{
				hotspotDetection = (HotspotDetection) CustomGUILayout.EnumPopup ("Hotspot detection method", hotspotDetection, "AC.KickStarter.settingsManager.hotspotDetection", "How Hotspots are detected");
				if (hotspotDetection == HotspotDetection.PlayerVicinity)
				{
					placeDistantHotspotsOnSeparateLayer = CustomGUILayout.ToggleLeft ("Place distant Hotspots on separate layer?", placeDistantHotspotsOnSeparateLayer, "AC.KickStarter.settingsManager.placeDistantHotspotsOnSeparateLayer", "If True, then distant Hotspots will be placed on a different layer");
				}
				else if (hotspotDetection == HotspotDetection.CustomScript)
				{
					EditorGUILayout.HelpBox ("Hotspots must be assigned by calling 'AC.KickStarter.playerInteraction.SetActiveHotspot ()'", MessageType.Info);
				}

				if (hotspotDetection == HotspotDetection.PlayerVicinity && (movementMethod == MovementMethod.Direct || IsInFirstPerson ()))
				{
					hotspotsInVicinity = (HotspotsInVicinity) CustomGUILayout.EnumPopup ("Hotspots in vicinity:", hotspotsInVicinity, "AC.KickStarter.settingsManager.hotspotsInVicinity", "What Hotspots gets detected");
				}
				else if (hotspotDetection == HotspotDetection.MouseOver)
				{
					scaleHighlightWithMouseProximity = CustomGUILayout.ToggleLeft ("Highlight Hotspots based on cursor proximity?", scaleHighlightWithMouseProximity, "AC.KickStarter.settingsManager.scaleHighlightWithMouseProximity", "If True, then Hotspots will highlight according to how close the cursor is to them");
					if (scaleHighlightWithMouseProximity)
					{
						highlightProximityFactor = CustomGUILayout.FloatField ("Cursor proximity factor:", highlightProximityFactor, "AC.KickStarter.settingsManager.highlightProximityFactor", "The factor by which distance affects the highlighting of Hotspots");
					}
				}
				
				playerFacesHotspots = CustomGUILayout.ToggleLeft ("Player turns head to active Hotspot?", playerFacesHotspots, "AC.KickStarter.settingsManager.playerFacesHotspots", "If True, then player prefabs will turn their head towards the active Hotspot");
				if (playerFacesHotspots)
				{
					if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
					{
						onlyFaceHotspotOnSelect = CustomGUILayout.ToggleLeft ("Only turn head when select Hotspot?", onlyFaceHotspotOnSelect, "AC.KickStarter.settingsManager.onlyFaceHotspotOnSelect", "If true, then players will only turn their heads once a Hotspot has been selected");
					}
					if (cameraPerspective == CameraPerspective.TwoD)
					{
						EditorGUILayout.HelpBox ("2D head-turning can be achieved with Sprites Unity Complex animation, or with Sprites Unity animation's 'Head on separate layer?' option.", MessageType.Info);
					}
				}

				hotspotIconDisplay = (HotspotIconDisplay) CustomGUILayout.EnumPopup ("Display Hotspot icons:", hotspotIconDisplay, "AC.KickStarter.settingsManager.hotspotIconDisplay", "When Hotspot icons are displayed");
				if (hotspotIconDisplay != HotspotIconDisplay.Never)
				{
					if (hotspotIconDisplay == HotspotIconDisplay.ViaScriptOnly)
					{
						EditorGUILayout.HelpBox ("Call a Hotspot's 'SetIconVisibility' method to show or hide its icon.", MessageType.Info);
					}

					hotspotDrawing = (ScreenWorld) CustomGUILayout.EnumPopup ("Draw icons in:", hotspotDrawing, "AC.KickStarter.settingsManager.hotspotDrawing", "How to draw Hotspot icons");
					if (cameraPerspective != CameraPerspective.TwoD)
					{
						occludeIcons = CustomGUILayout.ToggleLeft ("Hide icons behind Colliders?", occludeIcons, "AC.KickStarter.settingsManager.occludeIcons", "If True, then Hotspot icons will be hidden behind colldiers placed on the same layer as Hotspots");
					}
					hotspotIcon = (HotspotIcon) CustomGUILayout.EnumPopup ("Hotspot icon type:", hotspotIcon, "AC.KickStarter.settingsManager.hotspotIcon", "The type of Hotspot icon to display");
					if (hotspotIcon == HotspotIcon.Texture)
					{
						if (hotspotIconTexture && hotspotIconGraphic.texture == null)
						{
							// Upgrade
							hotspotIconGraphic.texture = hotspotIconTexture;
							hotspotIconTexture = null;
						}
						hotspotIconGraphic.ShowGUI (true, true, "Hotspot icon texture:", (KickStarter.cursorManager) ? KickStarter.cursorManager.cursorRendering : CursorRendering.Software, "AC.KickStarter.settingsManager.hotspotIconGraphic", "The icon to use for Hotspots");
					}
					hotspotIconSize = CustomGUILayout.FloatField ("Hotspot icon size:", hotspotIconSize, "AC.KickStarter.settingsManager.hotspotIconSize", "The size of Hotspot icons");
					if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
					    selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot &&
					    hotspotIconDisplay != HotspotIconDisplay.OnlyWhenFlashing)
					{
						hideIconUnderInteractionMenu = CustomGUILayout.ToggleLeft ("Hide when Interaction Menus are visible?", hideIconUnderInteractionMenu, "AC.KickStarter.settingsManager.hideIconUnderInteractionMenu", "If True, then Hotspot icons will be hidden if an Interaction Menu is visible");
					}
				}

				if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					hideUnhandledHotspots = CustomGUILayout.ToggleLeft ("Hide Hotspots with no suitable interaction?", hideUnhandledHotspots, "AC.KickStarter.settingsManager.hideUnhandledHotspots", "If True, then Hotspots that do not have an interaction for the currently-selected icon will not be visible to the cursor");
				}
				
				highlightMaterialPropertyOverride = CustomGUILayout.TextField ("Highlight material override:", highlightMaterialPropertyOverride, "AC.KickStarter.settingsManager.highlightMaterialPropertyOverride", "By default, the Highlight component affects the '_Color' property for Materials. The value entered here will override that.");
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowAudioSettings ()
		{
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSound = CustomGUILayout.ToggleHeader (showSound, "Audio settings");
			if (showSound)
			{
				volumeControl = (VolumeControl) CustomGUILayout.EnumPopup ("Volume controlled by:", volumeControl, "AC.KickStarter.settingsManager.volumeControl", "How volume is controlled");
				if (volumeControl == VolumeControl.AudioMixerGroups)
				{
					musicMixerGroup = (AudioMixerGroup) CustomGUILayout.ObjectField <AudioMixerGroup> ("Music mixer:", musicMixerGroup, false, "AC.KickStarter.settingsManager.musicMixerGroup", "The AudioMixerGroup for music audio");
					sfxMixerGroup = (AudioMixerGroup) CustomGUILayout.ObjectField <AudioMixerGroup> ("SFX mixer:", sfxMixerGroup, false, "AC.KickStarter.settingsManager.sfxMixerGroup", "The AudioMixerGroup for SFX audio");
					speechMixerGroup = (AudioMixerGroup) CustomGUILayout.ObjectField <AudioMixerGroup> ("Speech mixer:", speechMixerGroup, false, "AC.KickStarter.settingsManager.speechMixerGroup", "The AudioMixerGroup for speech audio");
					musicAttentuationParameter = CustomGUILayout.TextField ("Music atten. parameter:", musicAttentuationParameter, "AC.KickStarter.settingsManager.musicAttentuationParameter", "The name of the parameter in the music MixerGroup that controls attenuation");
					sfxAttentuationParameter = CustomGUILayout.TextField ("SFX atten. parameter:", sfxAttentuationParameter, "AC.KickStarter.settingsManager.sfxAttentuationParameter", "The name of the parameter in the SFX MixerGroup that controls attenuation");
					speechAttentuationParameter = CustomGUILayout.TextField ("Speech atten. parameter:", speechAttentuationParameter, "AC.KickStarter.settingsManager.speechAttentuationParameter", "The name of the parameter in the speech MixerGroup that controls attenuation");
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowRaycastSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showRaycast = CustomGUILayout.ToggleHeader (showRaycast, "Raycast settings");
			if (showRaycast)
			{
				hotspotRaycastLength = CustomGUILayout.FloatField ("Hotspot ray length:", hotspotRaycastLength, "AC.KickStarter.settingsManager.hotspotRaycastLength", "The length of rays cast to find Hotspots");
				navMeshRaycastLength = CustomGUILayout.FloatField ("NavMesh ray length:", navMeshRaycastLength, "AC.KickStarter.settingsManager.navMeshRaycastLength", "The length of rays cast to find NavMeshes");
				moveableRaycastLength = CustomGUILayout.FloatField ("Moveable ray length:", moveableRaycastLength, "AC.KickStarter.settingsManager.moveableRaycastLength", "The length of rays cast to find moveable objects");
				
				EditorGUILayout.Space ();

				hotspotLayer = CustomGUILayout.TextField ("Hotspot layer:", hotspotLayer, "AC.KickStarter.settingsManager.hotspotLayer", "The layer to place active Hotspots on ");
				if (hotspotDetection == HotspotDetection.PlayerVicinity && placeDistantHotspotsOnSeparateLayer)
				{
					distantHotspotLayer = CustomGUILayout.TextField ("Distant hotspot layer:", distantHotspotLayer, "AC.KickStarter.settingsManager.distantHotspotLayer", "The layer to place distant Hotspots on");
				}
				navMeshLayer = CustomGUILayout.TextField ("NavMesh layer:", navMeshLayer, "AC.KickStarter.settingsManager.navMeshLayer", "The layer to place active NavMeshes on");
				if (cameraPerspective == CameraPerspective.TwoPointFiveD)
				{
					backgroundImageLayer = CustomGUILayout.TextField ("Background image layer:", backgroundImageLayer, "AC.KickStarter.settingsManager.backgroundImageLayer", "The layer to place BackgroundImage prefabs on ");
				}
				deactivatedLayer = CustomGUILayout.TextField ("Deactivated layer:", deactivatedLayer, "AC.KickStarter.settingsManager.deactivatedLayer", "The layer to place deactivated objects on");
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowSceneLoadingSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSceneLoading = CustomGUILayout.ToggleHeader (showSceneLoading, "Scene loading");
			if (showSceneLoading)
			{
				reloadSceneWhenLoading = CustomGUILayout.ToggleLeft ("Always reload scene when loading a save file?", reloadSceneWhenLoading, "AC.KickStarter.settingsManager.reloadSceneWhenLoading", "If True, then the scene will reload when loading a saved game that takes place in the same scene that the player is already in");
				blackOutWhenInitialising = CustomGUILayout.ToggleLeft ("Black out when initialising?", blackOutWhenInitialising, "AC.KickStarter.settingsManager.blackOutWhenInitialising", "If True then the game will turn black while the scene initialises itself, which can be useful when restoring animation states");

				useAsyncLoading = CustomGUILayout.ToggleLeft ("Load scenes asynchronously?", useAsyncLoading, "AC.KickStarter.settingsManager.useAsyncLoading", "If True, scenes will be loaded asynchronously");
				if (useAsyncLoading)
				{
					if (referenceScenesInSave == ChooseSceneBy.Name)
					{
						loadScenesFromAddressable = CustomGUILayout.ToggleLeft ("Load scenes from Addressables?", loadScenesFromAddressable, "AC.KickStarter.settingsManager.loadScenesFromAddressable", "If True, then scene names will be considered keys for Addressable scenes assets, and loaded via the Addressable system.");
						if (loadScenesFromAddressable)
						{
							#if !AddressableIsPresent
							EditorGUILayout.HelpBox ("The 'AddressableIsPresent' preprocessor define must be declared in the Player Settings.", MessageType.Warning);
							#endif
						}
					}
				
					manualSceneActivation = CustomGUILayout.ToggleLeft ("Scene loading requires manual activation?", manualSceneActivation, "AC.KickStarter.settingsManager.manualSceneActivation", "If True, then new scenes will not be activated upon loading until the SceneChanger's ActivateLoadedScene function has been called.");
				}
				useLoadingScreen = CustomGUILayout.ToggleLeft ("Use loading screen?", useLoadingScreen, "AC.KickStarter.settingsManager.useLoadingScreen", "If True, then a specific scene will be loaded in-between scene transitions, to be used as a loading screen");
				if (useLoadingScreen)
				{
					loadingSceneIs = (ChooseSceneBy) CustomGUILayout.EnumPopup ("Choose loading scene by:", loadingSceneIs, "AC.KickStarter.settingsManager.loadingSceneIs", "How the scene that acts as a loading scene is chosen");
					if (loadingSceneIs == ChooseSceneBy.Name)
					{
						loadingSceneName = CustomGUILayout.TextField ("Loading scene name:", loadingSceneName, "AC.KickStarter.settingsManager.loadingSceneName", "The name of the scene to act as a loading scene");
					}
					else
					{
						loadingScene = CustomGUILayout.IntField ("Loading screen scene:", loadingScene, "AC.KickStarter.settingsManager.loadingScene", "The number of the scene to act as a loading scene");
					}
					if (useAsyncLoading)
					{
						loadingDelay = CustomGUILayout.Slider ("Delay before and after (s):", loadingDelay, 0f, 1f, "AC.KickStarter.settingsManager.loadingDelay", "The delay, in seconds, before and after loading");
					}

					if (referenceScenesInSave == ChooseSceneBy.Name && loadScenesFromAddressable)
					{
						EditorGUILayout.HelpBox ("The loading scene must be present in Unity's Build Settings - it cannot be loaded via Addressables.", MessageType.Info);
					}
				}

				spawnPersistentEnginePrefab = CustomGUILayout.ToggleLeft ("Spawn PersistentEngine prefab from Resources?", spawnPersistentEnginePrefab, "AC.KickStarter.settingsManager.spawnPersistentEnginePrefab", "If True, the required PersistentEngine object will be created by spawning the 'Resources/PersistentEngine' prefab, as opposed to generating it from scratch");
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowOptionsSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showOptions = CustomGUILayout.ToggleHeader (showOptions, "Default Options");
			if (showOptions)
			{
				defaultSpeechVolume = CustomGUILayout.Slider ("Speech volume:", defaultSpeechVolume, 0f, 1f, "AC.KickStarter.settingsManager.defaultSpeechVolume", "The game's default speech audio volume");
				defaultMusicVolume = CustomGUILayout.Slider ("Music volume:", defaultMusicVolume, 0f, 1f, "AC.KickStarter.settingsManager.defaultMusicVolume", "The game's default music audio volume");
				defaultSfxVolume = CustomGUILayout.Slider ("SFX volume:", defaultSfxVolume, 0f, 1f, "AC.KickStarter.settingsManager.defaultSfxVolume", "The game's default SFX audio volume");
				defaultShowSubtitles = CustomGUILayout.Toggle ("Show subtitles?", defaultShowSubtitles, "AC.KickStarter.settingsManager.defaultShowSubtitles", "The game's default subtitles state");

				if (KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages)
				{
					defaultLanguage = CustomGUILayout.IntField ("Text language:", defaultLanguage, "AC.KickStarter.settingsManager.defaultLanguage", "The game's default text language index, where 0 is the game's original language");
					defaultVoiceLanguage = CustomGUILayout.IntField ("Speech audio language:", defaultVoiceLanguage, "AC.KickStarter.settingsManager.defaultVoiceLanguage", "The game's default voice language index");
				}
				else
				{
					defaultLanguage = CustomGUILayout.IntField ("Language:", defaultLanguage, "AC.KickStarter.settingsManager.defaultLanguage", "The game's default language index, where 0 is the game's original language");
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowDebugSettings ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showDebug = CustomGUILayout.ToggleHeader (showDebug, "Debug settings");
			if (showDebug)
			{
				showActiveActionLists = (DebugWindowDisplays) CustomGUILayout.EnumPopup ("Show 'AC Status' box:", showActiveActionLists, "AC.KickStarter.settingsManager.showActiveActionLists", "Used to show all currently-running ActionLists will be listed in the corner of the screen");
				showDebugLogs = (ShowDebugLogs) CustomGUILayout.EnumPopup ("Show logs in Console:", showDebugLogs, "AC.KickStarter.settingsManager.showDebugLogs", "Determines when logs are written to the Console");
				
				if (printActionCommentsInConsole)
				{
					printActionCommentsInConsole = false;
					actionCommentLogging = ActionCommentLogging.OnlyIfVisible;
				}
				actionCommentLogging = (ActionCommentLogging) CustomGUILayout.EnumPopup ("Action comment logging:", actionCommentLogging, "AC.KickStarter.settingsManager.actionCommentLogging", "If set, comments attached to Actions will be printed in the Console when the Action is run");
			}
			CustomGUILayout.EndVertical ();
		}
		
		#endif


		/** How Interaction menus are opened, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction (ClickOnHotspot, CursorOverHotspot) */
		public SeeInteractions SeeInteractions
		{
			get
			{
				if (CanUseCursor ())
				{
					return seeInteractions;
				}
				return SeeInteractions.ClickOnHotspot;
			}
		}


		/** What happens when right-clicking while an inventory item is selected (ExaminesItem, DeselectsItem, DoesNothing). */
		public RightClickInventory RightClickInventory
		{
			get
			{
				if (CanSelectItems (false) && !inventoryDragDrop)
				{
					if (interactionMethod == AC_InteractionMethod.ContextSensitive || inventoryInteractions == InventoryInteractions.Single)
					{
						if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && cycleInventoryCursors)
						{
							// Don't show
						}
						return rightClickInventory;
					}
					if (interactionMethod != AC_InteractionMethod.CustomScript)
					{
						if (!(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.cycleInventoryCursors))
						{
							return rightClickInventory;
						}
					}
				}
				return RightClickInventory.DoesNothing;
			}
		}


		/**
		 * The prefix of the project's save-game filenames, before the save / profile ID
		 */
		public string SavePrefix
		{
			get
			{
				if (string.IsNullOrEmpty (saveFileName))
				{
					string[] s = Application.dataPath.Split ('/');
					saveFileName = s[s.Length - 2];
				}
				#if UNITY_EDITOR
				if (separateEditorSaveFiles)
				{
					return saveFileName + "_Editor";
				}
				#endif
				return saveFileName;
			}
		}


		private string SmartAddInput (string existingResult, string newInput)
		{
			newInput = "\n" + newInput;
			if (!existingResult.Contains (newInput))
			{
				return existingResult + newInput;
			}
			return existingResult;
		}
		
		
		private string GetInputList ()
		{
			string result = "";
			
			if (inputMethod != InputMethod.TouchScreen)
			{
				result = SmartAddInput (result, "InteractionA (Button)");
				result = SmartAddInput (result, "InteractionB (Button)");
				result = SmartAddInput (result, "CursorHorizontal (Axis)");
				result = SmartAddInput (result, "CursorVertical (Axis)");
			}

			result = SmartAddInput (result, "ToggleCursor (Button)");

			if (movementMethod == MovementMethod.Direct || movementMethod == MovementMethod.FirstPerson || inputMethod == InputMethod.KeyboardOrController)
			{
				if (inputMethod != InputMethod.TouchScreen)
				{
					result = SmartAddInput (result, "Horizontal (Axis)");
					result = SmartAddInput (result, "Vertical (Axis)");

					if (movementMethod == MovementMethod.Direct || movementMethod == MovementMethod.FirstPerson)
					{
						result = SmartAddInput (result, "Run (Button/Axis)");
						result = SmartAddInput (result, "ToggleRun (Button)");
						result = SmartAddInput (result, "Jump (Button)");
					}
				}
				
				if (movementMethod == MovementMethod.FirstPerson && inputMethod == InputMethod.MouseAndKeyboard)
				{
					result = SmartAddInput (result, "Mouse ScrollWheel (Axis)");
					result = SmartAddInput (result, "CursorHorizontal (Axis)");
					result = SmartAddInput (result, "CursorVertical (Axis)");
				}
				
				if ((movementMethod == MovementMethod.Direct || movementMethod == MovementMethod.FirstPerson)
				    && (hotspotDetection == HotspotDetection.PlayerVicinity && hotspotsInVicinity == HotspotsInVicinity.CycleMultiple))
				{
					result = SmartAddInput (result, "CycleHotspotsLeft (Button)");
					result = SmartAddInput (result, "CycleHotspotsRight (Button)");
					result = SmartAddInput (result, "CycleHotspots (Axis)");
				}
			}
			
			if (SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
			{
				result = SmartAddInput (result, "CycleInteractionsLeft (Button)");
				result = SmartAddInput (result, "CycleInteractionsRight (Button)");
				result = SmartAddInput (result, "CycleInteractions (Axis)");
			}
			if (SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				result = SmartAddInput (result, "CycleCursors (Button)");
				result = SmartAddInput (result, "CycleCursorsBack (Button)");
			}
			else if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				result = SmartAddInput (result, "CycleCursors (Button)");
				result = SmartAddInput (result, "CycleCursorsBack (Button)");

				if (allowDefaultinteractions || (inventoryInteractions == InventoryInteractions.Multiple && CanSelectItems (false) && allowDefaultInventoryInteractions))
				{
					result = SmartAddInput (result, "DefaultInteraction (Button)");
				}

				if (KickStarter.cursorManager != null && KickStarter.cursorManager.allowIconInput)
				{
					if (KickStarter.cursorManager.allowMainCursor && KickStarter.cursorManager.allowWalkCursor)
					{
						result = SmartAddInput (result, "Icon_Walk (Button)");
					}
							
					if (KickStarter.cursorManager.cursorIcons != null)
					{
						foreach (CursorIcon cursorIcon in KickStarter.cursorManager.cursorIcons)
						{
							string buttonName = cursorIcon.GetButtonName ();
							if (!string.IsNullOrEmpty (buttonName))
							{
								result = SmartAddInput (result, buttonName + " (Button)");
							}
						}
					}
				}
			}

			result = SmartAddInput (result, "FlashHotspots (Button)");
			if (AdvGame.GetReferences ().speechManager != null &&
			   (AdvGame.GetReferences ().speechManager.allowSpeechSkipping || AdvGame.GetReferences ().speechManager.displayForever || AdvGame.GetReferences ().speechManager.displayNarrationForever))
			{
				result = SmartAddInput (result, "SkipSpeech (Button)");
			}
			result = SmartAddInput (result, "EndCutscene (Button)");
			result = SmartAddInput (result, "EndConversation (Button)");

			if (AdvGame.GetReferences ().menuManager != null && AdvGame.GetReferences ().menuManager.menus != null)
			{
				foreach (Menu menu in AdvGame.GetReferences ().menuManager.menus)
				{
					if (menu.appearType == AppearType.OnInputKey && menu.toggleKey != "")
					{
						result = SmartAddInput (result, menu.toggleKey + " (Button)");
					}
				}
			}

			if (activeInputs != null)
			{
				foreach (ActiveInput activeInput in activeInputs)
				{
					if (activeInput.inputName != "")
					{
						result = SmartAddInput (result, activeInput.inputName + " (Button)");
					}
				}
			}

			if (runConversationsWithKeys)
			{
				result = SmartAddInput (result, "DialogueOption[1-9] (Buttons)");
			}

			return result;
		}


		/**
		 * <summary>Checks if the movement settings are such that the player character is able to reverse<summary>
		 * <returns>True if the movement settings are such that the player character is able to reverse</returns>
		 */
		public bool PlayerCanReverse ()
		{
			if (movementMethod == MovementMethod.Direct && directMovementType == DirectMovementType.TankControls)
			{
				return true;
			}
			if (movementMethod == MovementMethod.FirstPerson)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the game is in first-person, on touch screen, and dragging affects only the camera rotation.</summary>
		 * <returns>True if the game is in first-person, on touch screen, and dragging affects only the camera rotation.</returns>
		 */
		public bool IsFirstPersonDragRotation ()
		{
			if (movementMethod == MovementMethod.FirstPerson && inputMethod == InputMethod.TouchScreen && firstPersonTouchScreen == FirstPersonTouchScreen.TouchControlsTurningOnly)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the game is in first-person, on touch screen, and dragging one finger affects camera rotation, and two fingers affects player movement.</summary>
		 * <returns>True if the game is in first-person, on touch screen, and dragging one finger affects camera rotation, and two fingers affects player movement.</returns>
		 */
		public bool IsFirstPersonDragComplex ()
		{
			if (movementMethod == MovementMethod.FirstPerson && inputMethod == InputMethod.TouchScreen && firstPersonTouchScreen == FirstPersonTouchScreen.OneTouchToTurnAndTwoTouchesToMove)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the game is in first-person, on touch screen, and dragging affects player movement and camera rotation.</summary>
		 * <returns>True if the game is in first-person, on touch screen, and dragging affects player movement and camera rotation.</returns>
		 */
		public bool IsFirstPersonDragMovement ()
		{
			if (movementMethod == MovementMethod.FirstPerson && inputMethod == InputMethod.TouchScreen && firstPersonTouchScreen == FirstPersonTouchScreen.OneTouchToMoveAndTurn)
			{
				return true;
			}
			return false;
		}
		
		
		
		#if UNITY_EDITOR
		
		private int GetIconID (string label, int iconID, CursorManager cursorManager, string api, string tooltip)
		{
			int iconInt = cursorManager.GetIntFromID (iconID);
			iconInt = CustomGUILayout.Popup (label, iconInt, cursorManager.GetLabelsArray (), api, tooltip);
			iconID = cursorManager.cursorIcons[iconInt].id;
			return iconID;
		}

		#endif
		
		
		private int[] GetPlayerIDArray ()
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (PlayerPrefab player in players)
			{
				idArray.Add (player.ID);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}
		

		/**
		 * <summary>Gets the ID number of the default Player prefab.</summary>
		 * <returns>The ID number of the default Player prefab</returns>
		 */
		public int GetDefaultPlayerID ()
		{
			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return 0;
			}
			
			foreach (PlayerPrefab _player in players)
			{
				if (_player.isDefault)
				{
					return _player.ID;
				}
			}
			
			return 0;
		}


		/**
		 * <summary>Gets a PlayerPrefab class with a given ID number, if player-switching is allowed.</summary>
		 * <param name = "ID">The ID number of the PlayerPrefab class to return</param>
		 * <returns>The PlayerPrefab class with the given ID number. This will return null if playerSwitching = PlayerSwitching.DoNotAllow</returns>
		 */
		public PlayerPrefab GetPlayerPrefab (int ID)
		{
			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return null;
			}
			
			foreach (PlayerPrefab _player in players)
			{
				if (_player.ID == ID)
				{
					return _player;
				}
			}
			
			return null;
		}


		/**
		 * <summary>Gets the ID number of the first-assigned Player prefab.</summary>
		 * <returns>The ID number of the first-assigned Player prefab</returns>
		 */
		public int GetEmptyPlayerID ()
		{
			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return 0;
			}
			
			foreach (PlayerPrefab _player in players)
			{
				if (_player.playerOb == null)
				{
					return _player.ID;
				}
			}
			
			return 0;
		}


		/**
		 * <summary>Gets the default Player prefab.</summary>
		 * <param name = "showError">If True, and no default Player is found, a warning message will be printed in the Console</param>
		 * <returns>The default player Player prefab</returns>
		 */
		public Player GetDefaultPlayer (bool showError = true)
		{
			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return player;
			}
			
			foreach (PlayerPrefab _player in players)
			{
				if (_player.isDefault)
				{
					if (_player.playerOb != null)
					{
						return _player.playerOb;
					}

					if (showError)
					{
						ACDebug.LogWarning ("Default Player has no prefab!");
					}
					return null;
				}
			}

			if (showError)
			{
				ACDebug.LogWarning ("Cannot find default player!");
			}
			return null;
		}


		public PlayerPrefab GetDefaultPlayerPrefab ()
		{
			foreach (PlayerPrefab _player in players)
			{
				if (_player.isDefault)
				{
					return _player;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets an array of all defined Player prefabs.</summary>
		 * <returns>An array of all defined Player prefabs</returns>
		 */
		public Player[] GetAllPlayerPrefabs ()
		{
			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return new Player[1] { player };
			}

			List<Player> playersList = new List<Player> ();

			foreach (PlayerPrefab _player in players)
			{
				if (_player.playerOb)
				{
					playersList.Add (_player.playerOb);
				}
			}

			return playersList.ToArray ();
		}


		/**
		 * <summary>Gets an array of all scene instances of the defined Player prefabs</summary>
		 * <returns>An array of all scene instances of the defined Player prefabs</returns>
		 */
		public Player[] GetAllPlayerInstances ()
		{
			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return new Player[1] { KickStarter.player };
			}

			List<Player> playersList = new List<Player> ();

			foreach (PlayerPrefab _player in players)
			{
				if (_player.playerOb)
				{
					Player sceneInstance = _player.GetSceneInstance ();
					if (sceneInstance)
					{
						playersList.Add (sceneInstance);
					}
				}
			}

			return playersList.ToArray ();
		}


		/**
		 * <summary>Sets the default Player prefab, when player-switching is not allowed</summary>
		 * <param name = "defaultPlayer">The Player prefab to assign as the default.</param>
		 */
		public void SetDefaultPlayer (Player defaultPlayer)
		{
			if (defaultPlayer == null) return;

			if (playerSwitching == PlayerSwitching.DoNotAllow)
			{
				player = defaultPlayer;
				return;
			}

			bool found = false;
			foreach (PlayerPrefab _player in players)
			{
				if (_player.playerOb == defaultPlayer)
				{
					_player.isDefault = true;
					found = true;
				}
				else
				{
					_player.isDefault = false;
				}
			}

			if (!found)
			{
				PlayerPrefab newPlayer = new PlayerPrefab (GetPlayerIDArray ());
				newPlayer.playerOb = defaultPlayer;
				players.Add (newPlayer);
			}
		}


		/**
		 * <summary>Checks if the player can click off Interaction menus to disable them.</summary>
		 * <returns>True if the player can click off Interaction menus to disable them.</returns>
		 */
		public bool CanClickOffInteractionMenu ()
		{
			if (cancelInteractions == CancelInteractions.ClickOffMenu || !CanUseCursor ())
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the player brings up the Interaction Menu by hovering the mouse over a Hotspot.</summary>
		 * <returns>True if the player brings up the Interaction Menu by hovering the mouse over a Hotspot.</returns>
		 */
		public bool MouseOverForInteractionMenu ()
		{
			if (seeInteractions == SeeInteractions.CursorOverHotspot && CanUseCursor ())
			{
				return true;
			}
			return false;
		}


		private bool CanUseCursor ()
		{
			if (inputMethod != InputMethod.TouchScreen || CanDragCursor ())
			{
				return true;
			}
			return false;
		}
		

		private bool DoPlayerAnimEnginesMatch ()
		{
			AnimationEngine animationEngine = AnimationEngine.Legacy;
			bool foundFirst = false;
			
			foreach (PlayerPrefab _player in players)
			{
				if (_player.playerOb != null)
				{
					if (!foundFirst)
					{
						foundFirst = true;
						animationEngine = _player.playerOb.animationEngine;
					}
					else
					{
						if (_player.playerOb.animationEngine != animationEngine)
						{
							return false;
						}
					}
				}
			}
			
			return true;
		}
		

		/**
		 * <summary>Gets the method of selecting Interactions, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction.</summary>
		 * <returns>The method of selecting Interactions, if interactionMethod = AC_InteractionMethod.ChooseHotspotThenInteraction.</returns>
		 */
		public SelectInteractions SelectInteractionMethod ()
		{
			if (inputMethod != InputMethod.TouchScreen && interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
			{
				return selectInteractions;
			}
			return SelectInteractions.ClickingMenu;
		}


		/**
		 * <summary>Checks if the interaction method is ChooseInteractionThenHotspot, and if the Hotspot label should change when hovering over an Interaction icon.</summary>
		 * <returns>True if the interaction method is ChooseInteractionThenHotspot, and if the Hotspot label should change when hovering over an Interaction icon.</returns>
		 */
		public bool ShowHoverInteractionInHotspotLabel ()
		{
			if (interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && showHoverInteractionInHotspotLabel)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the game is currently in a "loading" scene.<summary>
		 * <returns>True it the game is currently in a "loading" scene</returns>
		 */
		public bool IsInLoadingScene ()
		{
			if (useLoadingScreen)
			{
				switch (loadingSceneIs)
				{
					case ChooseSceneBy.Name:
						return SceneChanger.CurrentScene.name == loadingSceneName;

					case ChooseSceneBy.Number:
						return SceneChanger.CurrentScene.buildIndex == loadingScene;
				}
			}
			return false;
		}
		
		
		/**
		 * <summary>Checks if the game is played in first-person.</summary>
		 * <returns>True if the game is played in first-person</returns>
		 */
		public bool IsInFirstPerson ()
		{
			if (movementMethod == MovementMethod.FirstPerson)
			{
				return true;
			}
			if (KickStarter.player != null && KickStarter.player.FirstPersonCamera != null && KickStarter.mainCamera != null && KickStarter.mainCamera.attachedCamera != null && KickStarter.mainCamera.attachedCamera.transform == KickStarter.player.FirstPersonCamera)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the player is able to "give" inventory items to NPCs.</summary>
		 * <returns>True if the player is able to "give" inventory items to NPCs.</returns>
		 */
		public bool CanGiveItems ()
		{
			if (interactionMethod != AC_InteractionMethod.ContextSensitive && CanSelectItems (false))
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if inventory items can be selected and then used on Hotspots or other items.</summary>
		 * <param name = "showError">If True, then a warning will be sent to the Console if this function returns False</param>
		 * <returns>Checks if inventory items can be selected and then used on Hotspots or other items</returns>
		 */
		public bool CanSelectItems (bool showError)
		{
			if (interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				return true;
			}
			if (!cycleInventoryCursors)
			{
				return true;
			}
			if (showError)
			{
				ACDebug.LogWarning ("Inventory items cannot be selected with this combination of settings - they are included in Interaction cycles instead.");
			}
			return false;
		}


		/**
		 * <summary>Checks if the cursor can be dragged on a touch-screen.</summary>
		 * <returns>True if the cursor can be dragged on a touch-screen</returns>
		 */
		public bool CanDragCursor ()
		{
			if (offsetTouchCursor && inputMethod == InputMethod.TouchScreen && movementMethod != MovementMethod.FirstPerson)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if Interactions are triggered by "clicking up" over a MenuInteraction element.</summary>
		 * <returns>True if Interactions are triggered by "clicking up" over a MenuInteraction element</returns>
		 */
		public bool ReleaseClickInteractions ()
		{
			if (inputMethod == InputMethod.TouchScreen)
			{
				return clickUpInteractions;
			}

			if (interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
				SelectInteractionMethod () == SelectInteractions.ClickingMenu &&
			    clickUpInteractions)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the minimum distance that a character can be to its target to be considered "close enough".</summary>
		 * <param name = "offset">The calculation is 1 + offset - destinationAccuracy, so having a non-zero offset prevents the result ever being zero.</param>
		 * <returns>The minimum distance that a character can be to its target to be considered "close enough".</returns>
		 */
		public float GetDestinationThreshold (float offset = 0.1f)
		{
			return (1f + offset - destinationAccuracy);
		}


		/** The desired aspect ratio, if forced.  If not forced, -1 will be returned */
		public float AspectRatio
		{
			get
			{
				return (AspectRatioEnforcement != AspectRatioEnforcement.NoneEnforced) ? wantedAspectRatio : -1f;
			}
		}


		public AspectRatioEnforcement AspectRatioEnforcement
		{
			get
			{
				if (forceAspectRatio)
				{
					forceAspectRatio = false;
					aspectRatioEnforcement = AspectRatioEnforcement.Fixed;
				}
				return aspectRatioEnforcement;
			}
		}


		/** If True, then inventory items can be drag-dropped (i.e. used on Hotspots and other items with a single mouse button press */
		public bool InventoryDragDrop
		{
			get
			{
				if (CanSelectItems (false))
				{
					if ((SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot || interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot) && 
						cycleInventoryCursors)
					{
						return false;
					}
					if (SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && inventoryInteractions == InventoryInteractions.Multiple)
					{
						return false;
					}
					return inventoryDragDrop;
				}
				return false;
			}
			set
			{
				inventoryDragDrop = value;
			}
		}


		public bool CanDragPlayer
		{
			get
			{
				if ((KickStarter.settingsManager.movementMethod == MovementMethod.Drag
				|| KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor
				|| (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen))
					&& KickStarter.settingsManager.movementMethod != MovementMethod.None)
				{
					return true;
				}
				return false;
			}
		}


		public InventoryInteractions InventoryInteractions
		{
			get
			{
				if (interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					return InventoryInteractions.Single;
				}
				return inventoryInteractions;
			}
		}


		/** If True, Hotspots that have no interaction associated with a given inventory item will not be active while that item is selected */
		public bool AutoDisableUnhandledHotspots
		{
			get
			{
				if (CanSelectItems (false))
				{
					return autoDisableUnhandledHotspots;
				}
				return false;
			}
		}


		#if UNITY_EDITOR

		private void AssignSaveScripts ()
		{
			bool canProceed = EditorUtility.DisplayDialog ("Add save scripts", "AC will now go through your game, and attempt to add 'Remember' components where appropriate.\n\nThese components are required for saving to function, and are covered in Section 9.1.1 of the Manual.\n\nAs this process cannot be undone without manually removing each script, it is recommended to back up your project beforehand.", "OK", "Cancel");
			if (!canProceed) return;

			string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();

			if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
			{
				Undo.RecordObject (this, "Update speech list");
				
				string[] sceneFiles = AdvGame.GetSceneFiles ();

				// First look for lines that already have an assigned lineID
				foreach (string sceneFile in sceneFiles)
				{
					AssignSaveScriptsInScene (sceneFile);
				}

				AssignSaveScriptsInManagers ();

				if (string.IsNullOrEmpty (originalScene))
				{
					UnityVersionHandler.NewScene ();
				}
				else
				{
					UnityVersionHandler.OpenScene (originalScene);
				}

				ACDebug.Log ("Process complete.");
			}
		}


		private void AssignSaveScriptsInScene (string sceneFile)
		{
			UnityVersionHandler.OpenScene (sceneFile);
			
			// Speech lines and journal entries
			ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
			foreach (ActionList list in actionLists)
			{
				if (list.source == ActionListSource.AssetFile)
				{
					SaveActionListAsset (list.assetFile);
				}
				else
				{
					SaveActionList (list);
				}
			}

			// Cameras
			PlayerStart[] playerStarts = GameObject.FindObjectsOfType (typeof (PlayerStart)) as PlayerStart[];
			foreach (PlayerStart playerStart in playerStarts)
			{
				if (playerStart.cameraOnStart != null && playerStart.cameraOnStart.GetComponent <ConstantID>() == null)
				{
					ConstantID newConstantID = playerStart.cameraOnStart.gameObject.AddComponent <ConstantID>();
					newConstantID.AssignInitialValue ();

					ACDebug.Log ("Added '" + newConstantID.GetType ().ToString () + "' component to " + playerStart.cameraOnStart.gameObject.name);
				}
			}
				
			// Hotspots
			Hotspot[] hotspots = GameObject.FindObjectsOfType (typeof (Hotspot)) as Hotspot[];
			foreach (Hotspot hotspot in hotspots)
			{
				if (hotspot.interactionSource == InteractionSource.AssetFile)
				{
					SaveActionListAsset (hotspot.lookButton.assetFile);
					SaveActionListAsset (hotspot.unhandledInvButton.assetFile);
					
					foreach (Button _button in hotspot.useButtons)
					{
						SaveActionListAsset (_button.assetFile);
					}
					
					foreach (Button _button in hotspot.invButtons)
					{
						SaveActionListAsset (_button.assetFile);
					}
				}
			}

			// Triggers
			AC_Trigger[] triggers = GameObject.FindObjectsOfType (typeof (AC_Trigger)) as AC_Trigger[];
			foreach (AC_Trigger trigger in triggers)
			{
				if (trigger.GetComponent <RememberTrigger>() == null)
				{
					UnityVersionHandler.AddConstantIDToGameObject <RememberTrigger> (trigger.gameObject);
				}
			}

			// Dialogue options
			Conversation[] conversations = GameObject.FindObjectsOfType (typeof (Conversation)) as Conversation[];
			foreach (Conversation conversation in conversations)
			{
				foreach (ButtonDialog dialogOption in conversation.options)
				{
					SaveActionListAsset (dialogOption.assetFile);
				}
			}
			
			// Save the scene
			UnityVersionHandler.SaveScene ();
			EditorUtility.SetDirty (this);
		}


		private void AssignSaveScriptsInManagers ()
		{
			// Settings
			SaveActionListAsset (actionListOnStart);
			if (activeInputs != null)
			{
				foreach (ActiveInput activeInput in activeInputs)
				{
					SaveActionListAsset (activeInput.actionListAsset);
				}
			}

			// Inventory
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager)
			{
				SaveActionListAsset (inventoryManager.unhandledCombine);
				SaveActionListAsset (inventoryManager.unhandledHotspot);
				SaveActionListAsset (inventoryManager.unhandledGive);

				// Item-specific events
				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem item in inventoryManager.items)
					{
						SaveActionListAsset (item.useActionList);
						SaveActionListAsset (item.lookActionList);
						SaveActionListAsset (item.unhandledActionList);
						SaveActionListAsset (item.unhandledGiveActionList);
						SaveActionListAsset (item.unhandledCombineActionList);

						foreach (InvCombineInteraction invCombineInteraction in item.combineInteractions)
						{
							SaveActionListAsset (invCombineInteraction.actionList);
						}
					}
				}
				
				foreach (Recipe recipe in inventoryManager.recipes)
				{
					SaveActionListAsset (recipe.invActionList);
				}
			}

			// Cursor
			CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
			if (cursorManager && cursorManager.AllowUnhandledIcons ())
			{
				foreach (ActionListAsset actionListAsset in cursorManager.unhandledCursorInteractions)
				{
					SaveActionListAsset (actionListAsset);
				}
			}

			// Menu
			MenuManager menuManager = AdvGame.GetReferences ().menuManager;
			if (menuManager)
			{
				// Gather elements
				if (menuManager.menus.Count > 0)
				{
					foreach (AC.Menu menu in menuManager.menus)
					{
						SaveActionListAsset (menu.actionListOnTurnOff);
						SaveActionListAsset (menu.actionListOnTurnOn);
						
						foreach (MenuElement element in menu.elements)
						{
							if (element is MenuButton)
							{
								MenuButton menuButton = (MenuButton) element;
								if (menuButton.buttonClickType == AC_ButtonClickType.RunActionList)
								{
									SaveActionListAsset (menuButton.actionList);
								}
							}
							else if (element is MenuSavesList)
							{
								MenuSavesList menuSavesList = (MenuSavesList) element;
								SaveActionListAsset (menuSavesList.actionListOnSave);
							}
						}
					}
				}
			}
		}


		private void SaveActionListAsset (ActionListAsset actionListAsset)
		{
			if (actionListAsset != null)
			{
				SaveActions (actionListAsset.actions, true);
			}
		}
		
		
		private void SaveActionList (ActionList actionList)
		{
			if (actionList != null)
			{
				SaveActions (actionList.actions, false);
			}
		}
		
		
		private void SaveActions (List<Action> actions, bool fromAsset)
		{
			foreach (Action action in actions)
			{
				if (action == null)
				{
					continue;
				}

				action.AssignConstantIDs (true, fromAsset);
				action.Upgrade ();

				foreach (ActionEnd ending in action.endings)
				{
					if (ending.resultAction == ResultAction.RunCutscene)
					{
						SaveActionListAsset (ending.linkedAsset);
					}
				}
			}
		}

		#endif

	}


	/**
	 * \mainpage Adventure Creator: Scripting guide
	 *
	 * Welcome to Adventure Creator's scripting guide. You can use this guide to get detailed descriptions on all of ACs public functions and variables.<br><b>Please read this page before delving in!</b>
	 * 
	 * Adventure Creator's scripts are written in C#, and use the 'AC' namespace, so you'll need to add the following at the top of any script that accesses them:
	 * 
	 * \code
	 * using AC;
	 * \endcode
	 * 
	 * Accessing ACs scripts is very simple: each component on the GameEngine and PersistentEngine prefabs, as well as all Managers, can be accessed by referencing their associated static variable in the KickStarter class, e.g.:
	 * 
	 * \code
	 * AC.KickStarter.settingsManager;
	 * AC.KickStarter.playerInput;
	 * \endcode
	 * 
	 * Additionally, the Player and MainCamera can also be accessed in this way:
	 * 
	 * \code
	 * AC.KickStarter.player;
	 * AC.KickStarter.mainCamera;
	 * \endcode
	 * 
	 * The KickStarter class also has functions that can be used to turn AC off or on completely:
	 * 
	 * \code
	 * AC.KickStarter.TurnOffAC ();
	 * AC.KickStarter.TurnOnAC ();
	 * \endcode
	 * 
	 * You can detect the game's current state (cutscene, gameplay, or paused) from the StateHandler:
	 *
	 * \code
	 * AC.KickStarter.stateHandler.IsInCutscene ();
	 * AC.KickStarter.stateHandler.IsInGameplay ();
	 * AC.KickStarter.stateHandler.IsPaused ();
	 * \endcode
	 * 
	 * If you want to place the game in a scripted cutscene, the StateHandler has functions for that, too:
	 * 
	 * \code
	 * AC.KickStarter.stateHandler.StartCutscene ();
	 * AC.KickStarter.stateHandler.EndCutscene ();
	 * \endcode
	 * 
	 * All-scene based ActionLists, inculding Cutscenes and Triggers, derive from the ActionList class.  Action List assets rely on the ActionListAsset class.  Both classes have an Interact function, which will cause their Actions to run.
	 * 
	 * Global and Local variables can be read and written to with static functions in the GlobalVariables and LocalVariables classes respectively:
	 * 
	 * \code
	 * AC.GlobalVariables.GetBooleanValue (int _id);
	 * AC.LocalVariables.SetStringValue (int _id, string _value);
	 * \endcode
	 *
	 * The best way to hook up custom code with AC is to use the EventManager.  Custom events allow you to hook up your own code whenever AC performs common tasks, such as playing speech or changing the camera.
	 * A tutorial on writing custom events can be found <a href="https://www.adventurecreator.org/tutorials/calling-custom-events">here</a>.
	 * 
	 * If you're using this guide to help write an integration script with another Unity asset, check out the <a href="http://adventure-creator.wikia.com/wiki/Category:Integrations">Integrations page</a> of the <a href="http://adventure-creator.wikia.com/wiki/">AC wiki</a> - it may have what you're looking for!
	 * 
	 * More common functions and variables can be found under Section 12.7 of the <a href="https://www.adventurecreator.org/files/Manual.pdf">AC Manual</a>.  Happy scripting!
	 */
	
}