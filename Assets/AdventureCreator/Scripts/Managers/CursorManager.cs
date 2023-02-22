/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CursorManager.cs"
 * 
 *	This script handles the "Cursor" tab of the main wizard.
 *	It is used to define cursor icons and the method in which
 *	interactions are triggered by the player.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * Handles the "Cursor" tab of the Game Editor window.
	 * All possible cursors that the mouse can have (excluding inventory items) are defined here, as are the various ways in which these cursors are displayed.
	 */
	[System.Serializable]
	public class CursorManager : ScriptableObject
	{

		/** The rendering method of all cursors (Software, Hardware, UnityUI) */
		public CursorRendering cursorRendering = CursorRendering.Software;
		/** The cursor prefab to spawn if cursorRendering = CursorRendering.Hardware */
		public GameObject uiCursorPrefab = null;
		/** The rule that defines when the main cursor is shown (Always, Never, OnlyWhenPaused) */
		public CursorDisplay cursorDisplay = CursorDisplay.Always;
		/** If True, then the system's default hardware cursor will replaced with a custom one */
		public bool allowMainCursor = false;
		/** If True, and cursorRendering = CursorRendering.Software, the system cursor will be locked when the AC cursor is (this is always true when using Hardware cursor rendering) */
		public bool lockSystemCursor = true;
		/** If True, then the cursor will always be kept within the boundary of the game window */
		public bool keepCursorWithinScreen = true;
		#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		/** If True, then the system cursor will be confined to the game window */
		public bool confineSystemCursor = false;
		#endif

		/** If True, then a separate cursor will display when in "walk mode" */
		public bool allowWalkCursor = false;

		public bool syncWalkCursorWithInteraction = false;
		
		public int walkCursor_ID = 0;

		/** If True, then a prefix can be added to the Hotspot label when in "walk mode" */
		public bool addWalkPrefix = false;
		/** The prefix to add to the Hotspot label when in "walk mode", if addWalkPrefix = True */
		public HotspotPrefix walkPrefix = new HotspotPrefix ("Walk to");

		/** If True, then the Cursor's interaction verb will prefix the Hotspot label when hovering over Hotspots */
		public bool addHotspotPrefix = false;
		/** If True, then the cursor will be controlled by the current Interaction when hovering over a Hotspot */
		public bool allowInteractionCursor = false;
		/** If True, then the cursor will be controlled by the current Interaction when hovering over an inventory item (see InvItem) */
		public bool allowInteractionCursorForInventory = false;
		/** If True, then cursor modes can by clicked by right-clicking, if interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot in SettingsManager */
		public bool cycleCursors = false;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot in SettingsManager, then animated cursors will only animate if the cursor is over a Hotspot */
		public bool onlyAnimateOverHotspots = false;
		/** If True, then left-clicking a Hotspot will examine it if no "use" Interaction exists (if interactionMethod = AC_InteractionMethod.ContextSensitive in SettingsManager) */
		public bool leftClickExamine = false;
		/** If True, and allowWalkCursor = True, then the walk cursor will only show when the cursor is hovering over a NavMesh */
		public bool onlyWalkWhenOverNavMesh = false;
		/** If True, then Hotspot labels will not show when an inventory item is selected unless the cursor is over another inventory item or a Hotspot */
		public bool onlyShowInventoryLabelOverHotspots = false;
		/** The size of selected inventory item graphics when used as a cursor */
		public float inventoryCursorSize = 0.06f;
		/** If True, and interactionMethod = AC_InteractionMethod.ChooseInteractionThenHotspot in SettingsManager, then the player can switch the active "interaction" icon by invoking a specific input */
		public bool allowIconInput = true;

		/** The cursor while the game is running a gameplay-blocking cutscene */
		public CursorIconBase waitIcon = new CursorIcon ();
		/** The game's default cursor */
		public CursorIconBase pointerIcon = new CursorIcon ();
		/** The cursor when in "walk mode", if allowWalkCursor = True */
		public CursorIconBase walkIcon = new CursorIcon ();
		/** The cursor when hovering over a Hotspot */
		public CursorIconBase mouseOverIcon = new CursorIcon ();
		/** The cursor while the cursor is being used to manipulate a drag-controlled camera */
		public CursorIconBase cameraDragIcon = new CursorIcon ();

		/** What happens to the cursor when an inventory item is selected (ChangeCursor, ChangeHotspotLabel, ChangeCursorAndHotspotLabel) */
		public InventoryHandling inventoryHandling = InventoryHandling.ChangeCursor;
		/** The "Use" in the syntax "Use item on Hotspot" */
		public HotspotPrefix hotspotPrefix1 = new HotspotPrefix ("Use");
		/** The "on" in the syntax "Use item on Hotspot" */
		public HotspotPrefix hotspotPrefix2 = new HotspotPrefix ("on");
		/** The "Give" in the syntax "Give item to NPC" */
		public HotspotPrefix hotspotPrefix3 = new HotspotPrefix ("Give");
		/** The "to" in the syntax "Give item to NPC" */
		public HotspotPrefix hotspotPrefix4 = new HotspotPrefix ("to");

		/** How large to display the count of the selected item instance */
		public float displayCountSize = 1.6f;
		/** The font when displaying the count of the selected item instance */
		public Font displayCountFont = null;
		/** The text effects when displaying the count of the selected item instance */
		public TextEffects displayCountTextEffects = TextEffects.None;
		/** The colour to use when displaying the count of the selected item instance */
		public Color displayCountColor = Color.white;

		/** A List of all CursorIcon instances that represent the various Interaction types */
		public List<CursorIcon> cursorIcons = new List<CursorIcon>();
		/** A List of ActionListAsset files that get run when an unhandled Interaction is triggered */
		public List<ActionListAsset> unhandledCursorInteractions = new List<ActionListAsset>();
		/** If True, the Hotspot clicked on to initiate unhandledCursorInteractions will be sent as a parameter to the ActionListAsset */
		public bool passUnhandledHotspotAsParameter;
		/** If True, then Hotspot labels will not show when no inventory item is selected unless the cursor is over another inventory item or a Hotspot */
		public bool onlyShowCursorLabelOverHotspots = false;
		/** If True, then the cursor can be cycled while the game is paused, if the current interaction method supports it */
		public bool allowCursorCyclingWhenPaused = false;

		/** What happens when hovering over a Hotspot that has both a Use and Examine Interaction (DisplayUseIcon, DisplayBothSideBySide, RightClickCyclesModes) */
		public LookUseCursorAction lookUseCursorAction = LookUseCursorAction.DisplayBothSideBySide;
		/** The ID number of the CursorIcon (in cursorIcons) that represents the "Examine" Interaction */
		public int lookCursor_ID = 0;

		/** If True, the cursor will be hidden when manipulating draggable objects */
		public bool hideCursorWhenDraggingMoveables = true;

		#if UNITY_EDITOR
		public bool forceCursorInEditor = true;

		private bool showSettings = true;
		private bool showMainCursor = true;
		private bool showWalkCursor = true;
		private bool showHotspotCursor = true;
		private bool showInventoryCursor = true;
		private bool showInteractionIcons = true;
		private bool showCutsceneCursor = true;
		private bool showCameraDragCursor = true;
		#endif

		private SettingsManager settingsManager;
		
		
		#if UNITY_EDITOR

		/** Shows the GUI. */
		public void ShowGUI ()
		{
			settingsManager = AdvGame.GetReferences().settingsManager;

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSettings = CustomGUILayout.ToggleHeader (showSettings, "Global cursor settings");
			if (showSettings)
			{
				cursorRendering = (CursorRendering) CustomGUILayout.EnumPopup ("Cursor rendering:", cursorRendering, "AC.KickStarter.cursorManager.cursorRendering", "The rendering method of all cursors");

				switch (cursorRendering)
				{
					case CursorRendering.Software:
						lockSystemCursor = CustomGUILayout.ToggleLeft ("Lock system cursor when locking AC cursor?", lockSystemCursor, "AC.KickStarter.cursorManager.lockSystemCursor", "If True, the system cursor will be locked when the AC cursor is");
						keepCursorWithinScreen = CustomGUILayout.ToggleLeft ("Always keep cursor within screen boundary?", keepCursorWithinScreen, "AC.KickStarter.cursorManager.keepCursorWithinScreen", "If True, then the cursor will always be kept within the boundary of the game window");
						break;

					case CursorRendering.Hardware:
						keepCursorWithinScreen = CustomGUILayout.ToggleLeft ("Always keep perceived cursor within screen boundary?", keepCursorWithinScreen, "AC.KickStarter.cursorManager.keepCursorWithinScreen", "If True, then the cursor will always be kept within the boundary of the game window");
						break;

					case CursorRendering.UnityUI:
						uiCursorPrefab = (GameObject) CustomGUILayout.ObjectField <GameObject> ("Unity UI Cursor prefab:", uiCursorPrefab, false, "AC.KickStarter.cursorManager.uiCursorPrefab", "The cursor prefab to spawn at runtime");
						break;
				}
				
				forceCursorInEditor = CustomGUILayout.ToggleLeft ("Always show system cursor in Editor?", forceCursorInEditor, "AC.KickStarter.cursorManager.forceCursorInEditor");
				#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
				confineSystemCursor = CustomGUILayout.ToggleLeft ("Confine system cursor to game window?", confineSystemCursor, "AC.KickStarter.cursorManager.confineSystemCursor", "If True, then the system cursor will be confined to the game window");
				#endif

				hideCursorWhenDraggingMoveables = CustomGUILayout.ToggleLeft ("Hide cursor when manipulating Draggables?", hideCursorWhenDraggingMoveables, "AC.KickStarter.cursorManager.hideCursorWhenDraggingMoveables", "If True, the cursor will be hidden when manipulating Draggable objects");
			}
			CustomGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showMainCursor = CustomGUILayout.ToggleHeader (showMainCursor, "Main cursor settings");
			if (showMainCursor)
			{
				cursorDisplay = (CursorDisplay) CustomGUILayout.EnumPopup ("Display cursor:", cursorDisplay, "AC.KickStarter.cursorManager.cursorDisplay", "The rule that defines when the main cursor is shown");
				if (cursorDisplay != CursorDisplay.Never)
				{
					allowMainCursor = CustomGUILayout.Toggle ("Replace mouse cursor?", allowMainCursor, "AC.KickStarter.cursorManager.allowMainCursor", "If True, then the system's default hardware cursor will replaced with a custom one");
					if (allowMainCursor || (settingsManager && settingsManager.inputMethod == InputMethod.KeyboardOrController))
					{
						IconBaseGUI (string.Empty, pointerIcon, "AC.KickStarter.cursorManager.pointerIcon", "The game's default cursor", false);
					}
				}
			}
			CustomGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showWalkCursor = CustomGUILayout.ToggleHeader (showWalkCursor, "Walk cursor");
			if (showWalkCursor)
			{
				if (allowMainCursor)
				{
					allowWalkCursor = CustomGUILayout.Toggle ("Provide walk cursor?", allowWalkCursor, "AC.KickStarter.cursorManager.allowWalkCursor", "If True, then a separate cursor will display when in 'walk mode'");
					if (allowWalkCursor)
					{
						if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && allowIconInput)
						{
							EditorGUILayout.LabelField ("Input button:", "Icon_Walk");
						}
						IconBaseGUI (string.Empty, walkIcon, "AC.KickStarter.cursorManager.walkIcon", "The cursor when in 'walk mode'");
					}
					if (KickStarter.settingsManager && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
					{
						syncWalkCursorWithInteraction = CustomGUILayout.Toggle ("Sync with interaction?", syncWalkCursorWithInteraction, "AC.KickStarter.cursorManager.syncWalkCursorWithInteraction", "If True, then walking will be possible when the Interaction icon set below is the active cursor");
						if (syncWalkCursorWithInteraction)
						{
							WalkIconGUI ();
						}
					}
					if (allowWalkCursor || (KickStarter.settingsManager && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && syncWalkCursorWithInteraction))
					{
						onlyWalkWhenOverNavMesh = CustomGUILayout.ToggleLeft ("Only show 'Walk' Cursor when over NavMesh?", onlyWalkWhenOverNavMesh, "AC.KickStarter.cursorManager.onlyWalkWhenOverNavMesh", "If True, then the walk cursor will only show when the cursor is hovering over a NavMesh");
					}
				}
				addWalkPrefix = CustomGUILayout.Toggle ("Prefix cursor labels?", addWalkPrefix, "AC.KickStarter.cursorManager.addWalkPrefix", "If True, then a prefix can be added to the Hotspot label when in 'walk mode'");
				if (addWalkPrefix)
				{
					walkPrefix.label = CustomGUILayout.TextField ("Walk prefix:", walkPrefix.label, "AC.KickStarter.cursorManager.walkPrefix");
				}
			}
			CustomGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showHotspotCursor = CustomGUILayout.ToggleHeader (showHotspotCursor, "Hotspot cursor");
			if (showHotspotCursor)
			{
				addHotspotPrefix = CustomGUILayout.Toggle ("Prefix cursor labels?", addHotspotPrefix, "AC.KickStarter.cursorManager.addHotspotPrefix", "If True, then the Cursor's interaction verb will prefix the Hotspot label when hovering over Hotspots");
				IconBaseGUI (string.Empty, mouseOverIcon, "AC.KickStarter.cursorManager.mouseOverIcon");
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showInventoryCursor = CustomGUILayout.ToggleHeader (showInventoryCursor, "Inventory cursor");
			if (showInventoryCursor)
			{
				inventoryHandling = (InventoryHandling) CustomGUILayout.EnumPopup ("When inventory selected:", inventoryHandling, "AC.KickStarter.cursorManager.inventoryHandling", "What happens to the cursor when an inventory item is selected");

				if (inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel || inventoryHandling == InventoryHandling.ChangeHotspotLabel)
				{
					onlyShowInventoryLabelOverHotspots = CustomGUILayout.ToggleLeft ("Only show label when over Hotspots and Inventory?", onlyShowInventoryLabelOverHotspots, "AC.KickStarter.cursorManager.onlyShowInventoryLabelOverHotspots", "If True, then Hotspot labels will not show when an inventory item is selected unless the cursor is over another inventory item or a Hotspot");
				}
				if (inventoryHandling == InventoryHandling.ChangeCursor || inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel)
				{
					inventoryCursorSize = CustomGUILayout.FloatField ("Inventory cursor size:", inventoryCursorSize, "AC.KickStarter.cursorManager.inventoryCursorSize", "The size of selected inventory item graphics when used as a cursor");
				}
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Use syntax:", GUILayout.Width (100f));
				hotspotPrefix1.label = CustomGUILayout.TextField (hotspotPrefix1.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix1");
				EditorGUILayout.LabelField ("(item)", GUILayout.MaxWidth (40f));
				hotspotPrefix2.label = CustomGUILayout.TextField (hotspotPrefix2.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix2");
				EditorGUILayout.LabelField ("(hotspot)", GUILayout.MaxWidth (55f));
				EditorGUILayout.EndHorizontal ();
				if (AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.CanGiveItems ())
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Give syntax:", GUILayout.Width (100f));
					hotspotPrefix3.label = CustomGUILayout.TextField (hotspotPrefix3.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix3");
					EditorGUILayout.LabelField ("(item)", GUILayout.MaxWidth (40f));
					hotspotPrefix4.label = CustomGUILayout.TextField (hotspotPrefix4.label, GUILayout.MaxWidth (80f), "AC.KickStarter.cursorManager.hotspotPrefix4");
					EditorGUILayout.LabelField ("(hotspot)", GUILayout.MaxWidth (55f));
					EditorGUILayout.EndHorizontal ();
				}

				if ((inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel || inventoryHandling == InventoryHandling.ChangeCursor) || KickStarter.settingsManager.cycleInventoryCursors)
				{
					CustomGUILayout.LabelField ("Count display style:");
					displayCountSize = CustomGUILayout.FloatField ("Size:", displayCountSize, "AC.KickStarter.cursorManager.displayCountSize", "How large to display the selected item's count");
					if (displayCountSize > 0)
					{ 
						displayCountFont = (Font) CustomGUILayout.ObjectField<Font> ("Font:", displayCountFont, false, "AC.KickStarter.cursorManager.displayCountFont", "The font to use when displaying the selected item's count");
						displayCountColor = CustomGUILayout.ColorField ("Colour:", displayCountColor, "AC.KickStarter.cursorManager.displayCountColor", "What colour to use when displaying the selected item's count");
						displayCountTextEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", displayCountTextEffects, "AC.KickStarter.cursorManager.displayCountTextEffects", "The text effect to use when displaying the selected item's count");
					}
				}
			}
			CustomGUILayout.EndVertical ();
			
			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showInteractionIcons = CustomGUILayout.ToggleHeader (showInteractionIcons, "Interaction icons");
			if (showInteractionIcons)
			{
				if (settingsManager == null || settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					allowInteractionCursor = CustomGUILayout.ToggleLeft ("Change cursor based on Interaction?", allowInteractionCursor, "AC.KickStarter.cursorManager.allowInteractionCursor", "If True, then the cursor will be controlled by the current Interaction when hovering over a Hotspot");
					if (allowInteractionCursor && (settingsManager == null || settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive))
					{
						allowInteractionCursorForInventory = CustomGUILayout.ToggleLeft ("Change when over Inventory items too?", allowInteractionCursorForInventory, "AC.KickStarter.cursorManager.allowInteractionCursorForInventory", "If True, then the cursor will be controlled by the current Interaction when hovering over an inventory item (see InvItem)");
					}
					if (settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
					{
						cycleCursors = CustomGUILayout.ToggleLeft ("Cycle Interactions with right-click?", cycleCursors, "AC.KickStarter.cursorManager.cycleCursors", "If True, then cursor modes can by clicked by right-clicking");
						allowIconInput = CustomGUILayout.ToggleLeft ("Set Interaction with specific inputs?", allowIconInput, "AC.KickStarter.cursorManager.allowIconInput", "then the player can switch the active icon by invoking a specific input");
						onlyAnimateOverHotspots = CustomGUILayout.ToggleLeft ("Only animate icons when over Hotspots?", onlyAnimateOverHotspots, "AC.KickStarter.cursorManager.onlyAnimateOverHotspots", "If True, then animated cursors will only animate if the cursor is over a Hotspot");
						onlyShowCursorLabelOverHotspots = CustomGUILayout.ToggleLeft ("Only show label when over Hotspots and Inventory?", onlyShowCursorLabelOverHotspots, "AC.KickStarter.cursorManager.onlyShowCursorLabelOverHotspots", "If True, then Hotspot labels will not show when no inventory item is selected unless the cursor is over another inventory item or a Hotspot");
					}
				}
				if (settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					allowInteractionCursor = CustomGUILayout.ToggleLeft ("Change cursor when over single-use Interaction Hotspots?", allowInteractionCursor, "AC.KickStarter.cursorManager.allowInteractionCursor", "If True, then the cursor will be controlled by the current Interaction when hovering over a Hotspot");
				}

				if ((settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive && lookUseCursorAction == LookUseCursorAction.RightClickCyclesModes) ||
					(settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && settingsManager.inventoryInteractions == InventoryInteractions.Multiple) ||
					(settingsManager && settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && settingsManager.inventoryInteractions == InventoryInteractions.Multiple))
				{
					allowCursorCyclingWhenPaused = CustomGUILayout.ToggleLeft ("Allow cursor cycling when paused?", allowCursorCyclingWhenPaused, "AC.KickStarter.cursorManager.allowCursorCyclingWhenPaused", "If True, then the cursor can be cycled while the game is paused");
				}

				
				IconsGUI ();
			
				EditorGUILayout.Space ();
			
				if (settingsManager == null || settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					LookIconGUI ();
				}
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCutsceneCursor = CustomGUILayout.ToggleHeader (showCutsceneCursor, "Cutscene cursor");
			if (showCutsceneCursor)
			{
				IconBaseGUI (string.Empty, waitIcon, "AC.KickStarter.cursorManager.waitIcon", "The cursor while the game is running a gameplay-blocking cutscene");
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCameraDragCursor = CustomGUILayout.ToggleHeader (showCameraDragCursor, "Camera-drag cursor");
			if (showCameraDragCursor)
			{
				IconBaseGUI (string.Empty, cameraDragIcon, "AC.KickStarter.cursorManager.cameraDragIcon", "The cursor to show while dragging the camera");
			}
			CustomGUILayout.EndVertical ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);
			}
		}


		private int iconSideMenu;
		private void SideMenu (int i)
		{
			GenericMenu menu = new GenericMenu ();
			iconSideMenu = i;

			menu.AddItem (new GUIContent ("Insert after"), false, MenuCallback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, MenuCallback, "Delete");

			if (i > 0 || i < cursorIcons.Count - 1)
			{
				menu.AddSeparator (string.Empty);
			}
			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, MenuCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, MenuCallback, "Move up");
			}
			if (i < cursorIcons.Count - 1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, MenuCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, MenuCallback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}


		private void MenuCallback (object obj)
		{
			if (iconSideMenu >= 0)
			{
				int i = iconSideMenu;
				CursorIcon tempIcon = cursorIcons[i];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (this, "Add icon");
						cursorIcons.Insert (i+1, new CursorIcon (GetIDArray ()));
						unhandledCursorInteractions.Insert (i+1, null);
						break;

					case "Delete":
						Undo.RecordObject (this, "Delete icon");
						cursorIcons.RemoveAt (i);
						unhandledCursorInteractions.RemoveAt (i);
						break;

					case "Move up":
						Undo.RecordObject (this, "Move icon up");
						cursorIcons.RemoveAt (i);
						cursorIcons.Insert (i - 1, tempIcon);
						break;

					case "Move down":
						Undo.RecordObject (this, "Move icon down");
						cursorIcons.RemoveAt (i);
						cursorIcons.Insert (i + 1, tempIcon);
						break;

					case "Move to top":
						Undo.RecordObject (this, "Move icon to top");
						cursorIcons.RemoveAt (i);
						cursorIcons.Insert (0, tempIcon);
						break;

					case "Move to bottom":
						Undo.RecordObject (this, "Move icon to bottom");
						cursorIcons.Add (tempIcon);
						cursorIcons.RemoveAt (i);
						break;

					default:
						break;
				}
			}
			
			iconSideMenu = -1;
		}

		
		private void IconsGUI ()
		{
			// Make sure unhandledCursorInteractions is the same length as cursorIcons
			while (unhandledCursorInteractions.Count < cursorIcons.Count)
			{
				unhandledCursorInteractions.Add (null);
			}
			while (unhandledCursorInteractions.Count > cursorIcons.Count)
			{
				unhandledCursorInteractions.RemoveAt (unhandledCursorInteractions.Count + 1);
			}

			// List icons
			foreach (CursorIcon _cursorIcon in cursorIcons)
			{
				int i = cursorIcons.IndexOf (_cursorIcon);
				CustomGUILayout.DrawUILine ();

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Icon ID:", GUILayout.MaxWidth (145));
				EditorGUILayout.LabelField (_cursorIcon.id.ToString (), GUILayout.MaxWidth (120));

				GUILayout.FlexibleSpace ();

				if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
				{
					SideMenu (i);
				}

				EditorGUILayout.EndHorizontal ();

				_cursorIcon.label = CustomGUILayout.TextField ("Label:", _cursorIcon.label, "AC.KickStarter.cursorManager.GetCursorIconFromID (" + _cursorIcon.id.ToString () + ").label", "The display name of the icon");
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && allowIconInput)
				{
					EditorGUILayout.LabelField ("Input button:", _cursorIcon.GetButtonName ());
				}
				_cursorIcon.ShowGUI (true, true, "Texture:", cursorRendering, "AC.KickStarter.cursorManager.GetCursorIconFromID (" + i + ")", "The icon's texture");

				if (AllowUnhandledIcons ())
				{
					string autoName = _cursorIcon.label + "_Unhandled_Interaction";
					unhandledCursorInteractions[i] = ActionListAssetMenu.AssetGUI ("Unhandled interaction:", unhandledCursorInteractions[i], autoName, "AC.KickStarter.cursorManager.unhandledCursorInteractions[" + i + "]", "An ActionList asset that gets run when an unhandled Interaction is triggered");
				}

				if (settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					_cursorIcon.dontCycle = CustomGUILayout.Toggle ("Leave out of Cursor cycle?", _cursorIcon.dontCycle, "AC.KickStarter.cursorManager.GetCursorIconFromID (" + i + ").dontCycle", "If True, then the cursor will be left out of the cycle when right-clicking");
				}
			}

			EditorGUILayout.Space ();
			if (GUILayout.Button("Create new icon"))
			{
				Undo.RecordObject (this, "Add icon");
				cursorIcons.Add (new CursorIcon (GetIDArray ()));
			}

			if (AllowUnhandledIcons())
			{
				EditorGUILayout.Space ();
				passUnhandledHotspotAsParameter = CustomGUILayout.ToggleLeft ("Pass Hotspot as GameObject parameter?", passUnhandledHotspotAsParameter, "AC.KickStarter.cursorManager.passUnhandledHotspotAsParameter", "If True, the Hotspot clicked on to initiate unhandled Interactions will be sent as a parameter to the ActionList asset");
				if (passUnhandledHotspotAsParameter)
				{
					EditorGUILayout.HelpBox ("The Hotspot will be set as the Unhandled interaction's first parameter, which must be set to type 'GameObject'.", MessageType.Info);
				}
			}
		}


		private void LookIconGUI ()
		{
			if (cursorIcons.Count > 0)
			{
				int lookCursor_int = GetIntFromID (lookCursor_ID);
				lookCursor_int = CustomGUILayout.Popup ("Examine icon:", lookCursor_int, GetLabelsArray (), "AC.KickStarter.cursorManager.lookCursor_ID", "The Cursor that represents the 'Examine' Interaction");
				lookCursor_ID = cursorIcons[lookCursor_int].id;

				EditorGUILayout.LabelField (new GUIContent ("When Use and Examine interactions are both available:", "What happens when hovering over a Hotspot that has both a Use and Examine Interaction"));
				lookUseCursorAction = (LookUseCursorAction) CustomGUILayout.EnumPopup (" ", lookUseCursorAction, "AC.KickStarter.cursorManager.lookUseCursorAction");
				if (cursorRendering == CursorRendering.Hardware && lookUseCursorAction == LookUseCursorAction.DisplayBothSideBySide)
				{
					EditorGUILayout.HelpBox ("This mode is not available with Hardward cursor rendering.", MessageType.Warning);
				}

				if (lookUseCursorAction != LookUseCursorAction.RightClickCyclesModes)
				{
					leftClickExamine = CustomGUILayout.ToggleLeft ("Left-click to examine when no use interaction exists?", leftClickExamine, "AC.KickStarter.cursorManager.leftClickExamine", "If True, then left-clicking a Hotspot will examine it if no 'Use' Interaction exists");
				}
			}
		}


		private void WalkIconGUI ()
		{
			if (cursorIcons.Count > 0)
			{
				int walkCursor_int = GetIntFromID (walkCursor_ID);
				walkCursor_int = CustomGUILayout.Popup ("Walk interaction:", walkCursor_int, GetLabelsArray (), "AC.KickStarter.cursorManager.walkCursor_ID", "The Cursor that represents the 'Walk' Interaction");
				walkCursor_ID = cursorIcons[walkCursor_int].id;
			}
		}


		private void IconBaseGUI (string fieldLabel, CursorIconBase icon, string apiPrefix, string tooltip = "", bool includeAlwaysAnimate = true)
		{
			if (fieldLabel != "" && fieldLabel.Length > 0)
				EditorGUILayout.LabelField (fieldLabel,  CustomStyles.subHeader);

			icon.ShowGUI (true, includeAlwaysAnimate, "Texture:", cursorRendering, apiPrefix, tooltip);
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
		}

		#endif


		/**
		 * <summary>Checks if the current settings allow for unhandled variants of each cursor icon to be available.</summary>
		 * <returns>Tr if the current settings allow for unhandled variants of each cursor icon to be available.</returns>
		 */
		public bool AllowUnhandledIcons ()
		{
			if (KickStarter.settingsManager != null)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					return true;
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot && !KickStarter.settingsManager.autoHideInteractionIcons)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Gets an array of the CursorIcon labels defined in cursorIcons.</summary>
		 * <param name = "includeNone">If True, then the array will begin with a (none) option.</param>
		 * <returns>An array of the CursorIcon labels defined in cursorIcons</returns>
		 */
		public string[] GetLabelsArray (bool includeNone = false)
		{
			List<string> iconLabels = new List<string>();
			if (includeNone)
			{
				iconLabels.Add ("(None)");
			}
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				iconLabels.Add (cursorIcon.id.ToString () + ": " + cursorIcon.label);
			}
			return iconLabels.ToArray();
		}
		

		/**
		 * <summary>Gets a label of the CursorIcon defined in cursorIcons.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <param name = "languageNumber">The index number of the language to get the label in</param>
		 * <returns>The label of the CursorIcon</returns>
		 */
		public string GetLabelFromID (int _ID, int languageNumber)
		{
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				if (cursorIcon.id == _ID)
				{
					return cursorIcon.GetLabel (languageNumber);
				}
			}
			return string.Empty;
		}
		

		/**
		 * <summary>Gets a CursorIcon defined in cursorIcons.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <returns>The CursorIcon</returns>
		 */
		public CursorIcon GetCursorIconFromID (int _ID)
		{
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				if (cursorIcon.id == _ID)
				{
					return (cursorIcon);
				}
			}
			return null;
		}
		

		/**
		 * <summary>Gets the index number (in cursorIcons) of a CursorIcon.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <returns>The index number (in cursorIcons) of the CursorIcon</returns>
		 */
		public int GetIntFromID (int _ID)
		{
			int i = 0;
			int requestedInt = -1;
			
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				if (cursorIcon.id == _ID)
				{
					requestedInt = i;
				}
				
				i++;
			}
			
			if (requestedInt == -1)
			{
				// Wasn't found (icon was deleted?), so revert to zero
				requestedInt = 0;
			}
		
			return requestedInt;
		}


		/**
		 * <summary>Gets the ActionListAsset that is used as a CursorIcon's unhandled event.</summary>
		 * <param name = "_ID">The ID number of the CursorIcon to find</param>
		 * <returns>The ActionListAsset that is used as the CursorIcon's unhandled event</returns>
		 */
		public ActionListAsset GetUnhandledInteraction (int _ID)
		{
			if (AllowUnhandledIcons ())
			{
				foreach (CursorIcon cursorIcon in cursorIcons)
				{
					if (cursorIcon.id == _ID)
					{
						int i = cursorIcons.IndexOf (cursorIcon);
						if (unhandledCursorInteractions.Count > i)
						{
							return unhandledCursorInteractions [i];
						}
						return null;
					}
				}
			}
			return null;
		}


		public GUIStyle GetDisplayCountStyle ()
		{
			float xScale = (KickStarter.mainCamera) ? KickStarter.mainCamera.GetPlayableScreenArea (false).size.x : ACScreen.width;
			int fontSize = (int) (xScale * displayCountSize / 100f);

			GUIStyle countStyle = new GUIStyle ();
			countStyle.font = displayCountFont;
			countStyle.fontSize = fontSize;
			countStyle.normal.textColor = displayCountColor;
			countStyle.alignment = TextAnchor.MiddleCenter;
			return countStyle;
		}
		
		
		private int[] GetIDArray ()
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (CursorIcon cursorIcon in cursorIcons)
			{
				idArray.Add (cursorIcon.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}