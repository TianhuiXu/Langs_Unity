/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Enums.cs"
 * 
 *	This script containers any enum type used by more than one script.
 * 
 */

namespace AC
{

	public enum MouseState { Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo };
	public enum DragState { None, Player, Inventory, PreInventory, Menu, ScreenArrows, Moveable, _Camera };
	public enum GameState { Normal, Cutscene, DialogOptions, Paused };
	public enum FlagsGameState { Normal = 1 << 0, Cutscene = 1 << 1, DialogOptions = 1 << 2, Paused = 1 << 3 };
	public enum ActionListType { PauseGameplay, RunInBackground };
	public enum PlayerSwitching { Allow, DoNotAllow };
	public enum ResultAction { Continue, Stop, Skip, RunCutscene };
	public enum ActionListSource { InScene, AssetFile };
	public enum InteractionSource { InScene, AssetFile, CustomScript };
	
	public enum AppearType { Manual, MouseOver, DuringConversation, OnInputKey, OnInteraction, OnHotspot, WhenSpeechPlays, DuringGameplay, OnContainer, WhileLoading, DuringCutscene, WhileInventorySelected, ExceptWhenPaused, DuringGameplayAndConversations, OnViewDocument };
	public enum SpeechMenuType { All, CharactersOnly, NarrationOnly, SpecificCharactersOnly, AllExceptSpecificCharacters };
	public enum SpeechMenuLimit { All, BlockingOnly, BackgroundOnly };
	public enum MenuTransition { Fade, Pan, FadeAndPan, Zoom, None };
	public enum UITransition { None, CanvasGroupFade, CustomAnimationStates, CustomAnimationBlend };
	public enum PanDirection { Up, Down, Left, Right };
	public enum PanMovement { Linear, Smooth, CustomCurve };
	public enum MenuOrientation { Horizontal, Vertical };
	public enum ElementOrientation { Horizontal, Vertical, Grid };
	public enum AC_PositionType { Centred, Aligned, Manual, FollowCursor, AppearAtCursorAndFreeze, OnHotspot, AboveSpeakingCharacter, AbovePlayer };
	public enum UIPositionType { Manual, FollowCursor, AppearAtCursorAndFreeze,  OnHotspot, AboveSpeakingCharacter, AbovePlayer };
	public enum AC_PositionType2 { Aligned, AbsolutePixels, RelativeToMenuSize };
	public enum AC_ShiftInventory { ShiftPrevious, ShiftNext };
	public enum AC_SizeType { Automatic, Manual, AbsolutePixels };
	public enum AC_InputType { AlphaNumeric, NumbericOnly, AllowSpecialCharacters };
	public enum AC_LabelType { Normal, Hotspot, DialogueLine, DialogueSpeaker, GlobalVariable, ActiveSaveProfile, InventoryProperty, DocumentTitle, SelectedObjective, ActiveContainer };
	public enum AC_GraphicType { Normal, DialoguePortrait, DocumentTexture, ObjectiveTexture };
	public enum DragElementType { EntireMenu, SingleElement };
	public enum AC_SaveListType { Save, Load, Import };
	public enum AC_ButtonClickType { TurnOffMenu, Crossfade, OffsetElementSlot, RunActionList, CustomScript, OffsetJournal, SimulateInput };
	public enum SimulateInputType { Button, Axis };
	public enum SaveDisplayType { LabelOnly, ScreenshotOnly, LabelAndScreenshot };
	public enum AC_SliderType { Speech, Music, SFX, CustomScript, FloatVariable };
	public enum AC_CycleType { Language, CustomScript, Variable };
	public enum AC_ToggleType { Subtitles, CustomScript, Variable };
	public enum AC_TimerType { Conversation, QuickTimeEventProgress, QuickTimeEventRemaining, LoadingProgress, Timer };
	public enum AC_InventoryBoxType { Default, HotspotBased, CustomScript, DisplaySelected, DisplayLastSelected, Container, CollectedDocuments, Objectives };
	public enum CraftingElementType { Ingredients, Output };
	public enum ConversationDisplayType { TextOnly, IconOnly, IconAndText };
	public enum SliderDisplayType { FillBar, MoveableBlock };
	public enum AC_DisplayType { IconOnly, TextOnly, IconAndText };

	public enum AC_TextType { Speech, Hotspot, DialogueOption, InventoryItem, CursorIcon, MenuElement, HotspotPrefix, JournalEntry, InventoryItemProperty, Variable, Character, Document, Custom, Objective, Container };
	public enum TextTypeFilter { Speech, Hotspot, DialogueOption, InventoryItem, CursorIcon, MenuElement, HotspotPrefix, JournalEntry, InventoryItemProperty, Variable, Character, Document, Custom, Objective, Container, All };
	public enum AC_TextTypeFlags { Speech=1<<0, Hotspot=1<<1, DialogueOption=1<<2, InventoryItem=1<<3, CursorIcon=1<<4, MenuElement=1<<5, HotspotPrefix=1<<6, JournalEntry=1<<7, InventoryItemProperty=1<<8, Variable=1<<9, Character=1<<10, Document=1<<11, Custom=1<<12, Objective=1<<13 };
	public enum CursorDisplay { Always, OnlyWhenPaused, Never };
	public enum LookUseCursorAction { DisplayBothSideBySide, DisplayUseIcon, RightClickCyclesModes };
	
	public enum InteractionType { Use, Examine, Inventory };
	public enum AC_InteractionMethod { ContextSensitive, ChooseInteractionThenHotspot, ChooseHotspotThenInteraction, CustomScript };
	public enum HotspotDetection { MouseOver, PlayerVicinity, CustomScript };
	public enum HotspotsInVicinity { NearestOnly, CycleMultiple, ShowAll };
	public enum PlayerAction { DoNothing, TurnToFace, WalkTo, WalkToMarker };
	public enum CancelInteractions { CursorLeavesMenuOrHotspot, CursorLeavesMenu, ClickOffMenu, ViaScriptOnly };
	
	public enum InventoryInteractions { Multiple, Single };
	public enum InventoryActiveEffect { None, Simple, Pulse };
	
	public enum AnimationEngine { Legacy=0, Sprites2DToolkit=1, SpritesUnity=2, Mecanim=3, SpritesUnityComplex=4, Custom=5 };
	public enum MotionControl { Automatic, JustTurning, Manual };
	public enum TalkingAnimation { Standard, CustomFace };
	public enum MovementMethod { PointAndClick, Direct, FirstPerson, Drag, None, StraightToCursor };
	public enum InputMethod { MouseAndKeyboard, KeyboardOrController, TouchScreen };
	public enum DirectMovementType { RelativeToCamera, TankControls };
	public enum CameraPerspective { TwoD, TwoPointFiveD, ThreeD };
	public enum MovingTurning { WorldSpace, ScreenSpace, TopDown, Unity2D };

	public enum InteractionIcon { Use, Examine, Talk };
	public enum InventoryHandling { ChangeCursor, ChangeHotspotLabel, ChangeCursorAndHotspotLabel, DoNothing };
	
	public enum RenderLock { NoChange, Set, Release };
	public enum LockType { Enabled, Disabled, NoChange };
	public enum CharState { Idle, Custom, Move, Decelerate };
	public enum AC_2DFrameFlipping { None, LeftMirrorsRight, RightMirrorsLeft };
	public enum FadeType { fadeIn, fadeOut };
	public enum SortingMapType { SortingLayer, OrderInLayer };
	public enum SortingMapScaleType { Linear, AnimationCurve };

	public enum CameraLocConstrainType { TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, SideScrolling, TargetHeight };
	public enum CameraRotConstrainType { TargetX, TargetZ, TargetAcrossScreen, TargetIntoScreen, LookAtTarget };
	
	public enum MoveMethod { Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve };
	
	public enum AnimLayer {	Base=0, UpperBody=1, LeftArm=2, RightArm=3, Neck=4, Head=5, Face=6, Mouth=7 };
	public enum AnimStandard { Idle, Walk, Run, Talk };
	public enum AnimPlayMode { PlayOnce=0, PlayOnceAndClamp=1, Loop=2 };
	public enum AnimPlayModeBase { PlayOnceAndClamp=1, Loop=2 };
	public enum AnimMethodMecanim { ChangeParameterValue, PlayCustom, BlendShape };
	public enum AnimMethod { PlayCustom, StopCustom, BlendShape };
	public enum AnimMethodCharMecanim { ChangeParameterValue, SetStandard, PlayCustom };
	public enum MecanimCharParameter { MoveSpeedFloat, TalkBool, TurnFloat };
	public enum MecanimParameterType { Float, Int, Bool, Trigger };
	
	public enum PlayerMoveLock { Free=0, AlwaysWalk=1, AlwaysRun=2, NoChange=3 };
	public enum AC_OnOff { On, Off };
	public enum TransformType { Translate, Rotate, Scale, CopyMarker };
	
	public enum VariableLocation { Global, Local, Component };
	public enum VariableType { Boolean, Integer, String, Float, PopUp, Vector3, GameObject, UnityObject };
	public enum BoolValue { True=1, False=0 };
	public enum SetVarMethod { SetValue, IncreaseByValue, SetAsRandom, Formula };
	public enum SetVarMethodString { EnteredHere=0, SetAsMenuElementText=1, CombinedWithOtherString=2 };
	public enum SetVarMethodIntBool { EnteredHere=0, SetAsMecanimParameter=1 };
	public enum SetParamMethod { EnteredHere=0, Random=2, CopiedFromGlobalVariable =1, CopiedFromComponentVariable=5, CopiedFromParameter=3, CopiedFromAnimator=4 };
	public enum GetVarMethod { EnteredValue, GlobalVariable, LocalVariable, ComponentVariable };
	
	public enum AC_Direction { None, Up, Down, Left, Right };
	public enum CharDirection { Up, Down, Left, Right, UpLeft, DownLeft, UpRight, DownRight };
	public enum ArrowPromptType { KeyOnly, ClickOnly, KeyAndClick };
	
	public enum AC_NavigationMethod { UnityNavigation, meshCollider, PolygonCollider, Custom };
	public enum AC_PathType { Loop=0, PingPong=1, ForwardOnly=2, IsRandom=3, ReverseOnly=4 };
	public enum PathSpeed { Walk=0, Run=1 };
	
	public enum SoundType { SFX, Music, Other, Speech };
	
	public enum NewPlayerPosition { ReplaceCurrentPlayer, ReplaceNPC, AppearAtMarker, AppearInOtherScene, ReplaceAssociatedNPC };
	public enum OldPlayer { RemoveFromScene, ReplaceWithNPC, ReplaceWithAssociatedNPC };
	
	public enum SaveTimeDisplay { DateOnly, TimeAndDate, None, CustomFormat };
	public enum ConversationAction { ReturnToConversation, Stop, RunOtherConversation };
	
	public enum AutoManual { Automatic, Manual };
	public enum SceneSetting { DefaultNavMesh, DefaultPlayerStart, SortingMap, OnStartCutscene, OnLoadCutscene, TintMap };
	public enum AnimatedCameraType { PlayWhenActive, SyncWithTargetMovement };
	public enum VarLink { None, PlaymakerVariable, OptionsData, CustomScript };

	public enum HotspotIconDisplay { Never, Always, OnlyWhenHighlighting, OnlyWhenFlashing, ViaScriptOnly };
	public enum HotspotIcon { Texture, UseIcon };
	public enum OnCreateRecipe { JustMoveToInventory, SelectItem, RunActionList };
	public enum HighlightState { None, Normal, Flash, Pulse, On };
	public enum HighlightType { Enable, Disable, PulseOnce, PulseContinually };
	
	public enum HeadFacing { None, Hotspot, Manual };
	public enum CharFaceType { Body, Head };
	public enum InputCheckType { Button, Axis, SingleTapOrClick, DoubleTapOrClick };
	public enum IntCondition { EqualTo, NotEqualTo, LessThan, MoreThan };
	public enum RightClickInventory { DeselectsItem, ExaminesItem, DoesNothing, ExaminesHotspot };
	public enum ParameterType { GameObject, InventoryItem, GlobalVariable, LocalVariable, String, Float, Integer, Boolean, UnityObject, Vector3, Document, ComponentVariable, PopUp };
	
	public enum ChangeNavMeshMethod { ChangeNavMesh, ChangeNumberOfHoles };
	public enum InvAction { Add, Remove, Replace };
	public enum TextEffects { None, Outline, Shadow, OutlineAndShadow };
	public enum LoadingGame { No, InSameScene, InNewScene, JustSwitchingPlayer };
	
	public enum DragMode { LockToTrack, RotateOnly, MoveAlongPlane };
	public enum AlignDragMovement { AlignToCamera, AlignToPlane };
	public enum DragRotationType { None, Roll, Screw };
	public enum TriggerDetects { Player, AnyObjectWithComponent, AnyObject, SetObject, AnyObjectWithTag };
	public enum PositionRelativeTo { Nothing, RelativeToActiveCamera, RelativeToPlayer, RelativeToGameObject, EnteredValue, VectorVariable };

	public enum CursorRendering { Software, Hardware, UnityUI };
	public enum SeeInteractions { ClickOnHotspot, CursorOverHotspot, ViaScriptOnly };
	public enum SelectInteractions { ClickingMenu, CyclingMenuAndClickingHotspot, CyclingCursorAndClickingHotspot };
	public enum ChooseSceneBy { Number=0, Name=1 };
	public enum ChangeType { Enable, Disable };
	public enum LipSyncMode { Off, FromSpeechText, ReadPamelaFile, ReadSapiFile, ReadPapagayoFile, FaceFX, Salsa2D, RogoLipSync };
	public enum LipSyncOutput { Portrait, PortraitAndGameObject, GameObjectTexture };
	public enum LimitDirectMovement { NoLimit, FourDirections, EightDirections };

	public enum MenuSource { AdventureCreator, UnityUiPrefab, UnityUiInScene };
	public enum DisplayActionsInEditor { ArrangedHorizontally, ArrangedVertically };
	public enum ActionListEditorScrollWheel { PansWindow, ZoomsWindow };
	public enum SelectItemMode { Use, Give };
	public enum WizardMenu { Blank, DefaultAC, DefaultUnityUI };
	public enum QTEType { SingleKeypress, HoldKey, ButtonMash, SingleAxis, ThumbstickRotation };
	public enum QTEState { None, Win, Lose, Running };

	public enum FilterSpeechLine { Type, Text, Scene, Speaker, Description, ID, All };
	public enum ActionCategory { ActionList, Camera, Character, Container, Dialogue, Document, Engine, Hotspot, Input, Inventory, Menu, Moveable, Object, Objective, Player, Save, Scene, Sound, ThirdParty, Variable, Custom };
	public enum VolumeControl { AudioSources, AudioMixerGroups };
	public enum TurningStyle { Linear, Script, RootMotion };
	public enum DoubleClickingHotspot { MakesPlayerRun, TriggersInteractionInstantly, DoesNothing, IsRequiredToUse };
	public enum BoolCondition { EqualTo, NotEqualTo };
	public enum VectorCondition { EqualTo, MagnitudeGreaterThan };

	public enum ManageProfileType { CreateProfile, DeleteProfile, RenameProfile, SwitchActiveProfile };
	public enum DeleteProfileType { ActiveProfile, SetSlotIndex, SlotIndexFromVariable, SetProfileID };
	public enum SaveCheck { NumberOfSaveGames, NumberOfProfiles, IsSavingPossible, IsSlotEmpty, DoesProfileExist, DoesProfileNameExist };
	public enum ManageSaveType { DeleteSave, RenameSave };
	public enum SelectSaveType { Autosave, SetSlotIndex, SlotIndexFromVariable, SetSaveID };
	public enum SaveHandling { LoadGame, ContinueFromLastSave, OverwriteExistingSave, SaveNewGame };

	public enum PlatformType { Desktop, TouchScreen, WebGL, Windows, Mac, Linux, iOS, Android };
	public enum Coord { W, X, Y, Z };
	public enum RootMotionType { None, TwoD, ThreeD };
	public enum RotationLock { Free, Locked, Limited };

	public enum FirstPersonTouchScreen { TouchControlsTurningOnly, OneTouchToMoveAndTurn, OneTouchToTurnAndTwoTouchesToMove, CustomInput };
	public enum DirectTouchScreen { DragBased, CustomInput };
	public enum TintMapMethod { ChangeTintMap, ChangeIntensity };
	public enum VisState { Visible, Invisible };
	public enum CheckVisState { InScene, InCamera };

	public enum NavMeshSearchDirection { StraightDownFromCursor, RadiallyOutwardsFromCursor };
	public enum MovieClipType { FullScreen, OnMaterial, VideoPlayer };
	public enum SetJournalPage { FirstPage, LastPage, SetHere };
	public enum InventoryPropertyType { SelectedItem, LastClickedItem, MouseOverItem, CustomScript };
	public enum UIHideStyle { DisableObject, ClearContent };
	public enum UISelectableHideStyle { DisableObject, DisableInteractability };
	public enum Hand { Left, Right };

	public enum SelectInventoryDisplay { NoChange, ShowSelectedGraphic, ShowHoverGraphic, HideFromMenu };
	public enum RotateSprite3D { CameraFacingDirection, RelativePositionToCamera, FullCameraRotation };
	public enum ScreenWorld { ScreenSpace, WorldSpace };
	public enum ShowDebugLogs { Always, OnlyWarningsOrErrors, Never };
	public enum JournalType { NewJournal, DisplayExistingJournal, DisplayActiveDocument };
	public enum CharacterEvasion { None, OnlyStationaryCharacters, AllCharacters };
	public enum UIPointerState { PointerClick, PointerDown, PointerEnter };
	public enum InventoryEventType { Add, Remove, Select, Deselect };
	public enum CameraShakeEffect { Translate, Rotate, TranslateAndRotate };

	public enum FileAccessState { Before, After, Fail };
	public enum GameTextSorting { None, ByID, ByDescription }; 
	public enum CharacterEvasionPoints { Four, Eight, Sixteen };
	public enum CycleUIBasis { Button, Dropdown };
	public enum TriggerReacts { OnlyDuringGameplay, OnlyDuringCutscenes, DuringCutscenesAndGameplay };
	public enum MovieMaterialMethod { PlayMovie, PauseMovie, StopMovie };
	public enum FirstPersonHeadBobMethod { BuiltIn, CustomAnimation, CustomScript };
	public enum ForceGameplayCursor { None, KeepLocked, KeepUnlocked };
	public enum WhenReselectHotspot { RestoreHotspotIcon, ResetIcon };

	public enum LinkUIGraphic { ImageComponent, ButtonTargetGraphic };
	public enum AlignType { YAxisOnly, CopyFullRotation };
	public enum DoubleClickMovement { MakesPlayerRun = 0, RequiredToWalk = 1, Disabled = 2 };
	public enum MusicAction { Play, Stop, Crossfade, ResumeLastStopped };
	public enum AngleSnapping { None=0, NinetyDegrees=1, FortyFiveDegrees=2 };
	public enum ParallaxReactsTo { Camera, Cursor, Transform };
	public enum DebugWindowDisplays { Never, EditorOnly, EditorAndBuild };
	public enum SpeechProximityLimit { NoLimit, LimitByDistanceToPlayer, LimitByDistanceToCamera };
	public enum SpeechIDRecycling { NeverRecycle, AlwaysRecycle, RecycleHighestOnly };
	public enum GameObjectParameterReferences { ReferencePrefab=0, ReferenceSceneInstance=1 };

	public enum TriggerDetectionMethod { RigidbodyCollision, TransformPosition };
	public enum VarFilter { Label, Description, Type };
	public enum GlobalLocal { Global, Local };
	public enum CameraSplitOrientation { Horizontal, Vertical, Overlay };
	public enum SplitLanguageType { TextAndVoice=0, TextOnly=1, VoiceOnly=2 };

	public enum DragMovementCalculation { DragVector=0, CursorPosition=1 };
	public enum MainCameraForwardDirection { MainCameraComponent, CameraComponent };
	public enum ObjectiveStateType { Active=1, Complete=2, Fail=3 };
	public enum ObjectiveDisplayType { All, ActiveOnly, CompleteOnly, FailedOnly }
	public enum SelectedObjectiveLabelType { Title, Description, StateLabel, StateDescription, StateType };
	public enum HotspotInteractionType { NotFound, Use, Examine, Inventory, UnhandledInventory, UnhandledUse };

	public enum CSVFormat { Standard, Legacy };
	public enum InventoryItemCountDisplay { OnlyIfMultiple, Always, Never, OnlyIfStackable };
	public enum ClickMarkerPosition { ColliderContactPoint, PlayerDestination };
	public enum CameraFadePauseBehaviour { Cancel, Hold, Hide, Continue };
	public enum TeleportPlayerStartMethod { SceneDefault, BasedOnPrevious, EnteredHere };
	public enum ReferenceSpeechFiles { ByDirectReference, ByNamingConvention, ByAssetBundle, ByAddressable };
	public enum PlayerStartActiveOption { NoLimit, ActivePlayerOnly, InactivePlayersOnly };
	public enum DebugLogType { Info, Warning, Error };
	public enum OffScreenRelease { GrabPoint, TransformCentre, DoNotRelease };
	public enum MergeMatchingIDs { NoMerging, MergeSpeechOnly, MergeIfTypesMatch, AlwaysMerge };
	public enum SliderOrientation { Horizontal, Vertical };

	public enum DragTrackDirection { NoRestriction, ForwardOnly, BackwardOnly };
	public enum ContainerTransfer { DoNotTransfer, TransferToPlayer, TransferToOtherContainer };
	public enum OccupiedSlotBehaviour { ShiftItems=0, SwapItems=1, FailTransfer=2, Overwrite=3 };
	public enum ContainerSelectMode { MoveToInventory, MoveToInventoryAndSelect, SelectItemOnly, CustomScript };
	public enum ActionCommentLogging { Never, OnlyIfVisible, Always };
	public enum ItemStackingMode { All=0, Single=1, Stack=2 };
	public enum AspectRatioEnforcement { NoneEnforced, Fixed, Range };
	public enum SaveScreenshots { Never, Always, ExceptWhenAutosaving };
	public enum CentrePointOverrides { FacingAndIconPosition, FacingPositionOnly, IconPositionOnly };
	public enum IfSkipWhileScrolling { DisplayFullText, SkipToNextWaitToken, EndLine, DoNothing };
	public enum SpeechScrollAudioSource { SFX, Speech };
	public enum ElementSlotMapping { List, FixedSlotIndex, FixedOptionID };
	public enum PathSnapping { None, SnapToStart, SnapToNode, SnapToNearest }
	public enum IndexPrefixDisplay { None, GlobalOrder, DisplayOrder };

}