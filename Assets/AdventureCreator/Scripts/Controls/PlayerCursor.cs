/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerCursor.cs"
 * 
 *	This script displays a cursor graphic on the screen.
 *	PlayerInput decides if this should be at the mouse position,
 *	or a position based on controller input.
 *	The cursor graphic changes based on what hotspot is underneath it.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script displays the cursor on screen.
	 * The available cursors are defined in CursorManager.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_cursor.html")]
	public class PlayerCursor : MonoBehaviour
	{
		
		protected Menu limitCursorToMenu;

		protected int selectedCursor = -10; // -2 = inventory, -1 = pointer, 0+ = cursor array
		protected bool showCursor = false;
		protected bool canShowHardwareCursor = false;
		protected float pulse = 0f;
		protected int pulseDirection = 0; // 0 = none, 1 = in, -1 = out
		
		// Animation variables
		protected CursorIconBase activeIcon = null;
		protected CursorIconBase activeLookIcon = null;
		protected bool lastIconLook;
		protected string lastCursorName;

		protected Texture2D currentCursorTexture2D;
		protected Texture currentCursorTexture;
		protected bool contextCycleExamine = false;
		protected int manualCursorID = -1;
		protected bool isDrawingHiddenCursor = false;

		protected bool forceOffCursor;


		protected void Start ()
		{
			if (KickStarter.cursorManager)
			{
				if (KickStarter.cursorManager.cursorDisplay != CursorDisplay.Never && KickStarter.cursorManager.allowMainCursor && (KickStarter.cursorManager.pointerIcon == null || KickStarter.cursorManager.pointerIcon.texture == null))
				{
					ACDebug.LogWarning ("Main cursor has no texture - please assign one in the Cursor Manager.");
				}

				if (KickStarter.cursorManager.cursorRendering == CursorRendering.UnityUI)
				{
					if (KickStarter.cursorManager.uiCursorPrefab)
					{
						GameObject newInstance = Instantiate (KickStarter.cursorManager.uiCursorPrefab);
						newInstance.name = KickStarter.cursorManager.uiCursorPrefab.name;
					}
					else
					{
						ACDebug.LogWarning ("No UI cursor prefab assigned in the Cursor Manager - Unity UI-based cursor rendering will not function correctly.");
					}
				}
			}
			if (KickStarter.settingsManager)
			{
				SelectedCursor = -1;
			}
		}


		private void OnEnable ()
		{
			EventManager.OnChangeLanguage += OnChangeLanguage;
		}


		private void OnDisable ()
		{
			EventManager.OnChangeLanguage -= OnChangeLanguage;
		}


		/** Updates the cursor. This is called every frame by StateHandler. */
		public void UpdateCursor ()
		{
			if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software)
			{
				bool shouldShowCursor = false;

				if (!canShowHardwareCursor)
				{
					shouldShowCursor = false;
				}
				else if (KickStarter.playerInput.GetDragState () == DragState.Moveable && KickStarter.cursorManager.hideCursorWhenDraggingMoveables)
				{
					shouldShowCursor = false;
				}
				else if (KickStarter.settingsManager && 
					KickStarter.cursorManager && 
					(!KickStarter.cursorManager.allowMainCursor || KickStarter.cursorManager.pointerIcon.texture == null) && 
					(!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeHotspotLabel || KickStarter.cursorManager.inventoryHandling == InventoryHandling.DoNothing) && 
					KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard &&
					KickStarter.stateHandler.gameState != GameState.Cutscene)
				{
					shouldShowCursor = true;
				}
				else if (KickStarter.cursorManager == null)
				{
					shouldShowCursor = true;
				}
				else
				{
					shouldShowCursor = false;
				}

				SetCursorVisibility (shouldShowCursor);
			}
			
			if (KickStarter.settingsManager && KickStarter.stateHandler)
			{
				if (forceOffCursor)
				{
					showCursor = false;
				}
				else if (KickStarter.stateHandler.gameState == GameState.Cutscene)
				{
					if (KickStarter.cursorManager.waitIcon.texture)
					{
						showCursor = true;
					}
					else
					{
						showCursor = false;
					}
				}
				else if (KickStarter.stateHandler.gameState != GameState.Normal && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen)
				{
					if (KickStarter.stateHandler.gameState == GameState.Paused && !KickStarter.menuManager.keyboardControlWhenPaused)
					{
						showCursor = true;
					}
					else if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.menuManager.keyboardControlWhenDialogOptions)
					{
						showCursor = true;
					}
					else
					{
						showCursor = false;
					}
				}
				else if (KickStarter.cursorManager)
				{
					if (KickStarter.stateHandler.gameState == GameState.Paused && (KickStarter.cursorManager.cursorDisplay == CursorDisplay.OnlyWhenPaused || KickStarter.cursorManager.cursorDisplay == CursorDisplay.Always))
					{
						showCursor = true;
					}
					else if (KickStarter.playerInput.GetDragState () == DragState.Moveable && KickStarter.cursorManager.hideCursorWhenDraggingMoveables)
					{
						showCursor = false;
					}
					else if (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.DialogOptions)
					{
						showCursor = true;
					}
					else
					{
						showCursor = false;
					}
				}
				else
				{
					showCursor = true;
				}

				switch (KickStarter.settingsManager.interactionMethod)
				{
					case AC_InteractionMethod.ContextSensitive:
					{
						if (CanCycleContextSensitiveMode () && KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
						{
							if (CanCurrentlyCycleCursor ())
							{
								Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
								if (hotspot)
								{
									if (hotspot.HasContextUse () && hotspot.HasContextLook ())
									{
										KickStarter.playerInput.ResetMouseClick ();
										contextCycleExamine = !contextCycleExamine;
									}
								}
								else if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
								{
									if (KickStarter.runtimeInventory.HoverInstance.InvItem.lookActionList)
									{
										KickStarter.playerInput.ResetMouseClick ();
										contextCycleExamine = !contextCycleExamine;
									}
								}
							}
						}
						break;
					}

					case AC_InteractionMethod.ChooseInteractionThenHotspot:
						if (CanCurrentlyCycleCursor ())
						{
							if ((KickStarter.cursorManager.cycleCursors && KickStarter.playerInput.GetMouseState () == MouseState.RightClick) || KickStarter.playerInput.InputGetButtonDown ("CycleCursors"))
							{
								CycleCursors ();
							}
							else if (KickStarter.playerInput.InputGetButtonDown ("CycleCursorsBack"))
							{
								CycleCursorsBack ();
							}
						}
						break;

					case AC_InteractionMethod.ChooseHotspotThenInteraction:
					{
						if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
						{
							if (CanCurrentlyCycleCursor ())
							{
								if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick || KickStarter.playerInput.InputGetButtonDown ("CycleCursors"))
								{
									KickStarter.playerInteraction.SetNextInteraction ();
								}
								else if (KickStarter.playerInput.InputGetButtonDown ("CycleCursorsBack"))
								{
									KickStarter.playerInteraction.SetPreviousInteraction ();
								}
							}
						}
						break;
					}

					default:
						break;
				}
			}
			
			switch (KickStarter.cursorManager.cursorRendering)
			{
				case CursorRendering.Hardware:
					SetCursorVisibility (showCursor);
					DrawCursor ();
					break;

				case CursorRendering.UnityUI:
					DrawCursor ();
					break;

				default:
					break;
			}
		}


		private bool CanCurrentlyCycleCursor ()
		{
			if (KickStarter.stateHandler.IsInGameplay ())
			{
				return true;
			}
			if (KickStarter.cursorManager.allowCursorCyclingWhenPaused && KickStarter.stateHandler.IsPaused ())
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
				{
					return true;
				}
				return (KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple);
			}
			return false;
		}


		/**
		 * If True, the cursor will always be hidden.
		 */
		public bool ForceOffCursor
		{
			set
			{
				forceOffCursor = value;
			}
		}


		/**
		 * <summary>Sets the active cursor ID, provided that the interaction method is CustomScript.</summary>
		 * <param name = "_cursorID">The ID number of the cursor defined in the Cursor Manager. If set to -1, the current cursor will be deselected main cursor will be displayed.</param>
		 */
		public void SetSelectedCursorID (int _cursorID)
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
			{
				manualCursorID = _cursorID;
			}
			else
			{
				ACDebug.LogWarning ("The cursor ID can only be set manually if the 'Interaction method' is set to 'Custom Script'");
			}
		}


		/** Draws the cursor. This is called from StateHandler's OnGUI() function. */
		public void DrawCursor ()
		{
			if (!showCursor)
			{
				if (!isDrawingHiddenCursor)
				{
					activeIcon = activeLookIcon = null;

					if (KickStarter.cursorManager.cursorRendering == CursorRendering.UnityUI)
					{
						SetHardwareCursor (null, Vector2.zero);
					}
					else
					{
						SetUICursor (null, Vector2.zero);
					}

					isDrawingHiddenCursor = true;
				}
				return;
			}
			isDrawingHiddenCursor = false;

			if (KickStarter.playerInput.IsCursorLocked () && KickStarter.settingsManager.hideLockedCursor)
			{
				if (KickStarter.cursorManager.cursorRendering == CursorRendering.UnityUI)
				{
					activeIcon = null;
					activeLookIcon = null;
					SetHardwareCursor (null, Vector2.zero);
				}
				canShowHardwareCursor = false;
				return;
			}

			GUI.depth = -1;
			canShowHardwareCursor = true;

			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
			{
				ShowCycleCursor (manualCursorID);
				return;
			}

			if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
			{
				// Cursor becomes selected inventory
				SelectedCursor = -2;
				canShowHardwareCursor = false;
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
			{
				if (KickStarter.stateHandler.gameState == GameState.DialogOptions)
				{
					SelectedCursor = -1;
				}
				else if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.cursorManager.cycleCursors && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
					{ }
					else
					{
						SelectedCursor = -1;
					}
				}
				else if (KickStarter.playerInteraction.GetActiveHotspot () && !KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction () && (selectedCursor == -1 || !KickStarter.cursorManager.allowInteractionCursor) && KickStarter.cursorManager.mouseOverIcon.texture)
				{
					DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
					return;
				}
			}
			else
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive && KickStarter.cursorManager.allowInteractionCursorForInventory && InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
				{
					ShowContextIcons (KickStarter.runtimeInventory.HoverInstance.InvItem);
					return;
				}
				else if (KickStarter.playerInteraction.GetActiveHotspot () && KickStarter.stateHandler.IsInGameplay () && (KickStarter.playerInteraction.GetActiveHotspot ().HasContextUse () || (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.playerInteraction.GetActiveHotspot ().HasContextLook ())))
				{
					SelectedCursor = 0;
					
					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
					{
						Button useButton = KickStarter.playerInteraction.GetActiveHotspot ().GetFirstUseButton ();
						if (useButton != null) SelectedCursor = useButton.iconID;

						if (KickStarter.cursorManager.allowInteractionCursor)
						{
							canShowHardwareCursor = false;
							ShowContextIcons ();
						}
						else if (KickStarter.cursorManager.mouseOverIcon.texture)
						{
							DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
						}
						else
						{
							DrawMainCursor ();
						}
					}
				}
				else
				{
					SelectedCursor = -1;
				}
			}

			GameState gameState = KickStarter.stateHandler.gameState;
			if (gameState == GameState.Cutscene && KickStarter.cursorManager.waitIcon.texture)
			{
				// Wait
				int elementOverCursorID = KickStarter.playerMenus.GetElementOverCursorID ();
				if (elementOverCursorID >= 0)
				{
					DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (elementOverCursorID), false);
					return;
				}

				DrawIcon (KickStarter.cursorManager.waitIcon, false);
			}
			else if (gameState == GameState.Normal && KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled && KickStarter.cursorManager.cameraDragIcon.texture &&
				(KickStarter.playerInput.GetDragState () == DragState._Camera || (KickStarter.playerInput.GetDragState () == DragState.None && KickStarter.playerInput.GetMouseState () == MouseState.HeldDown && KickStarter.playerInteraction.GetActiveHotspot () == null)))
			{
				// Camera drag
				DrawIcon (KickStarter.cursorManager.cameraDragIcon, false);
			}
			else if (selectedCursor == -2 && InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
			{
				// Inventory
				canShowHardwareCursor = false;
				
				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.cycleInventoryCursors)
				{
					if (KickStarter.playerInteraction.GetActiveHotspot () == null && !InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
					{
						if (KickStarter.playerInteraction.InteractionIndex >= 0)
						{
							// Item was selected due to cycling icons
							KickStarter.playerInteraction.ResetInteractionIndex ();
							KickStarter.runtimeInventory.SetNull ();
							return;
						}
					}
				}
				if (KickStarter.settingsManager.InventoryDragDrop && KickStarter.playerInput.GetDragState () != DragState.Inventory)
				{
					DrawMainCursor ();
				}
				else if (KickStarter.settingsManager.inventoryActiveEffect != InventoryActiveEffect.None && KickStarter.runtimeInventory.SelectedInstance.CanBeAnimated && !string.IsNullOrEmpty (KickStarter.playerMenus.GetHotspotLabel ()) &&
						(KickStarter.settingsManager.activeWhenUnhandled || 
						KickStarter.playerInteraction.DoesHotspotHaveInventoryInteraction () || 
						(InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && KickStarter.runtimeInventory.HoverInstance.InvItem.DoesHaveInventoryInteraction (KickStarter.runtimeInventory.SelectedInstance.InvItem))))
				{
					if (KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeHotspotLabel || KickStarter.cursorManager.inventoryHandling == InventoryHandling.DoNothing)
					{
						DrawMainCursor ();
					}
					else
					{
						DrawActiveInventoryCursor ();
					}
				}
				else
				{
					if ((KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursor || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel) && KickStarter.runtimeInventory.SelectedInstance.InvItem.HasCursorIcon ())
					{
						DrawInventoryCursor ();
					}
					else
					{
						#if UNITY_EDITOR
						if (!KickStarter.runtimeInventory.SelectedInstance.InvItem.HasCursorIcon () &&
							(KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursor || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel))
						{
							ACDebug.LogWarning ("Cannot change cursor to display the selected Inventory item because the item '" + KickStarter.runtimeInventory.SelectedInstance.InvItem.label + "' has no associated graphic.");
						}
						#endif

						if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
						{
							if (KickStarter.playerInteraction.GetActiveHotspot () == null)
							{
								DrawMainCursor ();
							}
							else if (KickStarter.cursorManager.allowInteractionCursor)
							{
								canShowHardwareCursor = false;
								ShowContextIcons ();
							}
							else if (KickStarter.cursorManager.mouseOverIcon.texture)
							{
								DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
							}
							else
							{
								DrawMainCursor ();
							}
						}
						else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
						{
							if (KickStarter.stateHandler.gameState == GameState.DialogOptions || KickStarter.stateHandler.gameState == GameState.Paused)
							{}
							else if (KickStarter.playerInteraction.GetActiveHotspot () && !KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction () && !KickStarter.cursorManager.allowInteractionCursor && KickStarter.cursorManager.mouseOverIcon.texture)
							{
								DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
							}
							else
							{
								DrawMainCursor ();
							}
						}
						else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
						{
							if (KickStarter.settingsManager.CanSelectItems (false) && KickStarter.stateHandler.gameState == GameState.Normal)
							{
								DrawMainCursor ();
							}
						}
					}
				}
				
				if (KickStarter.cursorManager.cursorRendering == CursorRendering.Software)
				{ 
					KickStarter.runtimeInventory.DrawSelectedInventoryCount ();
				}
			}
			else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				ShowCycleCursor (KickStarter.playerInteraction.GetActiveUseButtonIconID ());
			}
			else if (KickStarter.cursorManager.allowMainCursor || KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
			{
				// Pointer
				pulseDirection = 0;

				switch (KickStarter.settingsManager.interactionMethod)
				{
					case AC_InteractionMethod.ChooseHotspotThenInteraction:
						{
							if (!InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && KickStarter.playerInteraction.GetActiveHotspot () && (!KickStarter.playerMenus.IsInteractionMenuOn () || KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot))
							{
								if (KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction () && KickStarter.cursorManager.allowInteractionCursor)
								{
									ShowContextIcons ();
								}
								else if (KickStarter.cursorManager.mouseOverIcon.texture)
								{
									DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
								}
								else
								{
									DrawMainCursor ();
								}
							}
							else
							{
								DrawMainCursor ();
							}
						}
						break;

					case AC_InteractionMethod.ContextSensitive:
						{
							if (selectedCursor == -1)
							{
								DrawMainCursor ();
							}
							else if (selectedCursor == -2 && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
							{
								SelectedCursor = -1;
							}
						}
						break;

					case AC_InteractionMethod.ChooseInteractionThenHotspot:
						{
							if (KickStarter.playerInteraction.GetActiveHotspot () && KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction ())
							{
								//SelectedCursor = -1;

								if (KickStarter.cursorManager.allowInteractionCursor)
								{
									ShowContextIcons ();
								}
								else if (KickStarter.cursorManager.mouseOverIcon.texture)
								{
									DrawIcon (KickStarter.cursorManager.mouseOverIcon, false);
								}
								else
								{
									DrawMainCursor ();
								}
							}
							else if (selectedCursor >= 0)
							{
								if (KickStarter.cursorManager.allowInteractionCursor)
								{
									//	Custom icon
									pulseDirection = 0;
									canShowHardwareCursor = false;

									bool canAnimate = false;
									if (!KickStarter.cursorManager.onlyAnimateOverHotspots ||
										 KickStarter.playerInteraction.GetActiveHotspot () != null ||
										(KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple && InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance)))
									{
										canAnimate = true;
									}

									DrawIcon (KickStarter.cursorManager.cursorIcons[selectedCursor], false, canAnimate);
								}
								else
								{
									DrawMainCursor ();
								}
							}
							else if (selectedCursor == -1)
							{
								DrawMainCursor ();
							}
							else if (selectedCursor == -2 && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
							{
								SelectedCursor = -1;
							}
						}
						break;

					default:
						break;
				}
			}
		}
		
		
		protected void DrawMainCursor ()
		{
			if (!showCursor)
			{
				return;
			}

			if (KickStarter.cursorManager.cursorDisplay == CursorDisplay.Never || !KickStarter.cursorManager.allowMainCursor)
			{
				return;
			}
			
			if (KickStarter.stateHandler.gameState != GameState.Paused && KickStarter.cursorManager.cursorDisplay == CursorDisplay.OnlyWhenPaused)
			{
				return;
			}
			
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return;
			}

			bool showWalkCursor = false;
			int elementOverCursorID = KickStarter.playerMenus.GetElementOverCursorID ();

			if (elementOverCursorID >= 0)
			{
				DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (elementOverCursorID), false);
				return;
			}

			if (KickStarter.cursorManager.allowWalkCursor && KickStarter.playerInput && !KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn () && KickStarter.stateHandler.IsInGameplay ())
			{
				if (KickStarter.cursorManager.onlyWalkWhenOverNavMesh)
				{
					if (KickStarter.playerMovement.ClickPoint (KickStarter.playerInput.GetMousePosition (), true) != Vector3.zero)
					{
						showWalkCursor = true;
					}
				}
				else
				{
					showWalkCursor = true;
				}
			}

			if (showWalkCursor)
			{
				DrawIcon (KickStarter.cursorManager.walkIcon, false);
			}
			else if (KickStarter.cursorManager.pointerIcon.texture)
			{
				DrawIcon (KickStarter.cursorManager.pointerIcon, false);
			}
		}
		
		
		protected void ShowContextIcons ()
		{
			Hotspot hotspot = KickStarter.playerInteraction.GetActiveHotspot ();
			if (hotspot == null)
			{
				return;
			}

			if (hotspot.HasContextUse ())
			{
				if (!hotspot.HasContextLook ())
				{
					DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (hotspot.GetFirstUseButton ().iconID), false);
					return;
				}
				else
				{
					Button _button = hotspot.GetFirstUseButton ();
					
					if (hotspot.HasContextUse () && hotspot.HasContextLook () && CanDisplayIconsSideBySide ())
					{
						CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (_button.iconID);
						DrawIcon (new Vector2 (-icon.size * ACScreen.width / 2f, 0f), icon, false);
					}
					else if (CanCycleContextSensitiveMode () && contextCycleExamine && hotspot.HasContextLook ())
					{
						CursorIcon lookIcon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);
						DrawIcon (Vector2.zero, lookIcon, true);
					}
					else
					{
						DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (_button.iconID), false);
					}
				}
			}
			
			if (hotspot.HasContextLook () &&
			    (!hotspot.HasContextUse () ||
			 (hotspot.HasContextUse () && CanDisplayIconsSideBySide ())))
			{
				if (KickStarter.cursorManager.cursorIcons.Count > 0)
				{
					CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);
					
					if (hotspot.HasContextUse () && hotspot.HasContextLook () && CanDisplayIconsSideBySide ())
					{
						DrawIcon (new Vector2 (icon.size * ACScreen.width / 2f, 0f), icon, true);
					}
					else
					{
						DrawIcon (icon, true);
					}
				}
			}	
		}
		
		
		protected void ShowContextIcons (InvItem invItem)
		{
			if (KickStarter.cursorManager.cursorIcons.Count > 0)
			{
				if (invItem.lookActionList && CanDisplayIconsSideBySide ())
				{
					if (invItem.useIconID < 0)
					{
						// Hide use
						if (invItem.lookActionList)
						{
							CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);
							DrawIcon (icon, true);
						}
						return;
					}
					else
					{
						CursorIcon icon = KickStarter.cursorManager.GetCursorIconFromID (invItem.useIconID);
						DrawIcon (new Vector2 (-icon.size * ACScreen.width / 2f, 0f), icon, false);
					}
				}
				else if (CanCycleContextSensitiveMode () && contextCycleExamine && invItem.lookActionList)
				{}
				else
				{
					DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (invItem.useIconID), false);
					return;
				}
				
				if (invItem.lookActionList)
				{
					CursorIcon lookIcon = KickStarter.cursorManager.GetCursorIconFromID (KickStarter.cursorManager.lookCursor_ID);

					if (invItem.lookActionList && CanDisplayIconsSideBySide ())
					{
						DrawIcon (new Vector2 (lookIcon.size * ACScreen.width / 2f, 0f), lookIcon, true);
					}
					else if (CanCycleContextSensitiveMode ())
					{
						if (contextCycleExamine)
						{
							DrawIcon (Vector2.zero, lookIcon, true);
						}
					}
					else
					{
						DrawIcon (lookIcon, true);
					}
				}
			}	
		}
		
		
		protected void ShowCycleCursor (int useCursorID)
		{
			if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
			{
				SelectedCursor = -2;

				switch (KickStarter.cursorManager.inventoryHandling)
				{
					case InventoryHandling.ChangeCursor:
					case InventoryHandling.ChangeCursorAndHotspotLabel:
						DrawActiveInventoryCursor ();
						break;

					default:
						DrawMainCursor ();
						break;
				}
			}
			else if (useCursorID >= 0)
			{
				SelectedCursor = useCursorID;
				DrawIcon (KickStarter.cursorManager.GetCursorIconFromID (selectedCursor), false);
			}
			else if (useCursorID == -1)
			{
				SelectedCursor = -1;
				DrawMainCursor ();
			}
		}


		protected void DrawInventoryCursor ()
		{
			InvInstance invInstance = KickStarter.runtimeInventory.SelectedInstance;
			if (!InvInstance.IsValid (invInstance))
			{
				return;
			}

			if (invInstance.CursorIcon.texture)
			{
				if (KickStarter.settingsManager.inventoryActiveEffect != InventoryActiveEffect.None)
				{
					// Only animate when active
					DrawIcon (invInstance.CursorIcon, false, false);
				}
				else
				{
					DrawIcon (invInstance.CursorIcon, false, true);
				}
			}
			else
			{
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invInstance.Tex);
			}
			pulseDirection = 0;
		}
		
		
		protected void DrawActiveInventoryCursor ()
		{	
			InvInstance invInstance = KickStarter.runtimeInventory.SelectedInstance;
			if (!InvInstance.IsValid (invInstance))
			{
				return;
			}

			if (invInstance.CursorIcon.texture)
			{
				DrawIcon (invInstance.CursorIcon, false, true);
			}
			else if (invInstance.ActiveTex == null)
			{
				DrawInventoryCursor ();
			}
			else if (KickStarter.settingsManager.inventoryActiveEffect == InventoryActiveEffect.Simple)
			{
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invInstance.ActiveTex);
			}
			else if (KickStarter.settingsManager.inventoryActiveEffect == InventoryActiveEffect.Pulse && invInstance.Tex)
			{
				if (pulseDirection == 0)
				{
					pulse = 0f;
					pulseDirection = 1;
				}
				else if (pulse > 1f)
				{
					pulse = 1f;
					pulseDirection = -1;
				}
				else if (pulse < 0f)
				{
					pulse = 0f;
					pulseDirection = 1;
				}
				else if (pulseDirection == 1)
				{
					pulse += KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
				}
				else if (pulseDirection == -1)
				{
					pulse -= KickStarter.settingsManager.inventoryPulseSpeed * Time.deltaTime;
				}
				
				Color backupColor = GUI.color;
				Color tempColor = GUI.color;
				
				tempColor.a = pulse;
				GUI.color = tempColor;
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invInstance.ActiveTex);
				GUI.color = backupColor;
				DrawIcon (AdvGame.GUIBox (KickStarter.playerInput.GetMousePosition (), KickStarter.cursorManager.inventoryCursorSize), invInstance.Tex);
			}
		}
		
		
		protected void DrawIcon (Rect _rect, Texture _tex)
		{
			if (_tex)
			{
				RecordCursorTexture (_tex);

				switch (KickStarter.cursorManager.cursorRendering)
				{
					case CursorRendering.Software:
					default:
						GUI.DrawTexture (_rect, currentCursorTexture, ScaleMode.ScaleToFit, true, 0f);
						break;

					case CursorRendering.Hardware:
						lastCursorName = string.Empty;
						activeIcon = activeLookIcon = null;

						SetHardwareCursor (currentCursorTexture2D, Vector2.zero);
						break;

					case CursorRendering.UnityUI:
						lastCursorName = string.Empty;
						activeIcon = activeLookIcon = null;

						SetUICursor (currentCursorTexture2D, Vector2.zero);
						break;
				}
			}
		}


		protected void SetHardwareCursor (Texture2D texture2D, Vector2 clickOffset)
		{
			Cursor.SetCursor (texture2D, clickOffset, CursorMode.Auto);
			KickStarter.eventManager.Call_OnSetHardwareCursor (texture2D, clickOffset);
		}


		private void SetUICursor (Texture2D texture2D, Vector2 clickOffset)
		{
			SetCursorVisibility (false);
			KickStarter.eventManager.Call_OnSetHardwareCursor (texture2D, clickOffset);
		}
		
		
		protected void DrawIcon (Vector2 offset, CursorIconBase icon, bool isLook, bool canAnimate = true)
		{
			if (icon != null)
			{
				bool isNew = (lastIconLook != isLook);
				if (isLook && activeLookIcon != icon)
				{
					activeLookIcon = icon;
					isNew = true;
					icon.Reset ();
				}
				else if (!isLook && activeIcon != icon)
				{
					activeIcon = icon;
					isNew = true;
					icon.Reset ();
				}
				lastIconLook = isLook;

				switch (KickStarter.cursorManager.cursorRendering)
				{
					case CursorRendering.Software:
						Texture tex = icon.Draw (KickStarter.playerInput.GetMousePosition () + offset, canAnimate);
						RecordCursorTexture (tex);
						break;

					case CursorRendering.Hardware:
						if (icon.isAnimated)
						{
							Texture2D animTex = icon.GetAnimatedTexture (canAnimate);

							if (icon.GetName () != lastCursorName)
							{
								lastCursorName = icon.GetName ();
								RecordCursorTexture (animTex);
								SetHardwareCursor (currentCursorTexture2D, icon.clickOffset);
							}
						}
						else if (isNew)
						{
							RecordCursorTexture (icon.texture);
							SetHardwareCursor (currentCursorTexture2D, icon.clickOffset);
						}
						break;

					case CursorRendering.UnityUI:
						if (icon.isAnimated)
						{
							Texture2D animTex = icon.GetAnimatedTexture (canAnimate);

							if (icon.GetName () != lastCursorName)
							{
								lastCursorName = icon.GetName ();
								RecordCursorTexture (animTex);
								SetUICursor (currentCursorTexture2D, icon.clickOffset);
							}
						}
						else if (isNew)
						{
							RecordCursorTexture (icon.texture);
							SetUICursor (currentCursorTexture2D, icon.clickOffset);
						}
						break;
				}
			}
		}
		
		
		protected void DrawIcon (CursorIconBase icon, bool isLook, bool canAnimate = true)
		{
			if (icon != null)
			{
				DrawIcon (new Vector2 (0f, 0f), icon, isLook, canAnimate);
			}
		}


		protected void RecordCursorTexture (Texture newCursorTexture)
		{
			if (newCursorTexture)
			{
				if (currentCursorTexture != newCursorTexture)
				{
					currentCursorTexture = newCursorTexture;

					if (newCursorTexture is Texture2D)
					{
						Texture2D newCursorTexture2D = (Texture2D) newCursorTexture;
						currentCursorTexture2D = newCursorTexture2D;
					}
				}
			}
		}


		/**
		 * <summary>Gets the current cursor texture.</summary>
		 * <returns>The current cursor texture. If the cursor is hidden or showing no texture, the last-assigned texture will be returned instead.</returns>
		 */
		public Texture GetCurrentCursorTexture ()
		{
			return currentCursorTexture;
		}


		protected void CycleCursors ()
		{
			if (KickStarter.playerInteraction.GetActiveHotspot () && KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction ())
			{
				return;
			}

			int newSelectedCursor = selectedCursor;
			if (KickStarter.cursorManager.cursorIcons.Count > 0)
			{
				newSelectedCursor ++;

				if (newSelectedCursor >= KickStarter.cursorManager.cursorIcons.Count)
				{
					newSelectedCursor = -1;
				}
				else if (newSelectedCursor >= 0 && newSelectedCursor < KickStarter.cursorManager.cursorIcons.Count && KickStarter.cursorManager.cursorIcons [newSelectedCursor].dontCycle)
				{
					while (KickStarter.cursorManager.cursorIcons [newSelectedCursor].dontCycle)
					{
						newSelectedCursor ++;

						if (newSelectedCursor >= KickStarter.cursorManager.cursorIcons.Count)
						{
							newSelectedCursor = -1;
							break;
						}
					}
				}
			}
			else
			{
				// Pointer
				newSelectedCursor = -1;
			}

			if (newSelectedCursor == -1 && selectedCursor >= 0)
			{
				// Ended icon cycle
				if (KickStarter.settingsManager.cycleInventoryCursors)
				{
					KickStarter.runtimeInventory.ReselectLastItem ();
					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
					{
						KickStarter.playerInput.ResetMouseClick ();
						return;
					}
    			}
			}

			SelectedCursor = newSelectedCursor;
		}


		protected void CycleCursorsBack ()
		{
			if (KickStarter.playerInteraction.GetActiveHotspot () && KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction ())
			{
				return;
			}

			int newSelectedCursor = selectedCursor;

			newSelectedCursor --;

			if (newSelectedCursor < -1)
			{
				if (selectedCursor == -1 && KickStarter.settingsManager.cycleInventoryCursors)
				{
					KickStarter.runtimeInventory.ReselectLastItem ();
					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
					{
						KickStarter.playerInput.ResetMouseClick ();
						SelectedCursor = -2;
						return;
					}
				}

				newSelectedCursor = KickStarter.cursorManager.cursorIcons.Count - 1;
			}

			if (newSelectedCursor >= 0 && newSelectedCursor < KickStarter.cursorManager.cursorIcons.Count && KickStarter.cursorManager.cursorIcons [newSelectedCursor].dontCycle)
			{
				while (newSelectedCursor >= 0 && KickStarter.cursorManager.cursorIcons [newSelectedCursor].dontCycle)
				{
					newSelectedCursor --;
				}
			}
			else if (newSelectedCursor < -1)
			{
				newSelectedCursor = -1;
			}

			SelectedCursor = newSelectedCursor;
		}
		

		/**
		 * <summary>Gets the index number of the currently-selected cursor within CursorManager's cursorIcons List.</summary>
		 * <returns>If = -2, the inventory cursor is showing.
		 * If = -1, the main pointer is showing.
		 * If > 0, the index number of the currently-selected cursor within CursorManager's cursorIcons List</returns>
		 */
		public int GetSelectedCursor ()
		{
			return selectedCursor;
		}


		/** Returns True if the cursor is currently set to walk mode */
		public virtual bool IsInWalkMode ()
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.cursorManager.syncWalkCursorWithInteraction)
			{
				if (KickStarter.cursorManager.walkCursor_ID == GetSelectedCursorID ())
				{
					return true;
				}
			}

			return selectedCursor == -1;
		}
		

		/**
		 * <summary>Gets the ID number of the currently-selected cursor, within CursorManager's cursorIcons List.</summary>
		 * <returns>The ID number of the currently-selected cursor, within CursorManager's cursorIcons List</returns>
		 */
		public int GetSelectedCursorID ()
		{
			if (KickStarter.cursorManager && selectedCursor >= 0 && selectedCursor < KickStarter.cursorManager.cursorIcons.Count)
			{
				return KickStarter.cursorManager.cursorIcons [selectedCursor].id;
			}
			return -1;
		}
		

		/**
		 * <summary>Resets the currently-selected cursor</summary>
		 */
		public void ResetSelectedCursor ()
		{
			SelectedCursor = -1;
		}
		

		/**
		 * <summary>Sets the cursor to an icon defined in CursorManager.</summary>
		 * <param name = "ID">The ID number of the cursor, within CursorManager's cursorIcons List, to select</param>
		 */
		public void SetCursorFromID (int ID)
		{
			if (KickStarter.cursorManager && KickStarter.cursorManager.cursorIcons.Count > 0)
			{
				foreach (CursorIcon cursor in KickStarter.cursorManager.cursorIcons)
				{
					if (cursor.id == ID)
					{
						SetCursor (cursor);
					}
				}
			}
		}


		/**
		 * <summary>Sets the cursor to an icon defined in CursorManager.</summary>
		 * <param name = "_icon">The cursor, within CursorManager's cursorIcons List, to select</param>
		 */
		public void SetCursor (CursorIcon _icon)
		{
			KickStarter.runtimeInventory.SetNull ();
			SelectedCursor = KickStarter.cursorManager.cursorIcons.IndexOf (_icon);
		}


		public bool ContextCycleExamine
		{
			get
			{
				return contextCycleExamine;
			}
		}


		protected bool CanDisplayIconsSideBySide ()
		{
			if (KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.DisplayBothSideBySide &&
			    KickStarter.cursorManager.cursorRendering == CursorRendering.Software &&
			    KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				return true;
			}
			return false;
		}


		protected bool CanCycleContextSensitiveMode ()
		{
			if (KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.RightClickCyclesModes &&
			    KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				return true;
			}
			return false;
		}


		protected void OnApplicationQuit ()
		{
			if (KickStarter.cursorManager)
			{
				KickStarter.cursorManager.waitIcon.ClearCache ();
				KickStarter.cursorManager.pointerIcon.ClearCache ();
				KickStarter.cursorManager.walkIcon.ClearCache ();
				KickStarter.cursorManager.mouseOverIcon.ClearCache ();

				foreach (CursorIcon cursorIcon in KickStarter.cursorManager.cursorIcons)
				{
					cursorIcon.ClearCache ();
				}
			}
		}


		protected int SelectedCursor
		{
			set
			{
				if (selectedCursor != value)
				{
					selectedCursor = value;

					if (value >= -1 && KickStarter.runtimeInventory && InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.CanSelectItems (false))
					{
						KickStarter.runtimeInventory.SetNull ();
					}

					if (KickStarter.eventManager)
					{
						KickStarter.eventManager.Call_OnChangeCursorMode (selectedCursor);
					}
				}
			}
		}


		/** If set, the cursor's range of movement will be limited to this Menu's boundary */
		public Menu LimitCursorToMenu
		{
			get
			{
				return limitCursorToMenu;
			}
			set
			{
				limitCursorToMenu = value;
			}
		}


		/**
		 * <summary>Sets the visiblity of the cursor.</summary>
		 * <param name = "state">If True, the cursor will be shown. If False, the cursor will be hidden."</param>
		 */
		public void SetCursorVisibility (bool state)
		{
			#if UNITY_EDITOR
			if (KickStarter.cursorManager && KickStarter.cursorManager.forceCursorInEditor)
			{
				state = true;
			}
			#endif

			Cursor.visible = state;
		}


		private void OnChangeLanguage (int language)
		{
			foreach (CursorIcon cursorIcon in KickStarter.cursorManager.cursorIcons)
			{
				cursorIcon.UpdateLabel (language);
			}
		}

	}
	
}