/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerInteraction.cs"
 * 
 *	This script processes cursor clicks over hotspots and NPCs
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This script processes Hotspot interactions.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_interaction.html")]
	public class PlayerInteraction : MonoBehaviour
	{

		protected bool inPreInteractionCutscene = false;
		protected string interactionLabel;

		protected Hotspot hotspotMovingTo;
		protected Hotspot hotspot;
		protected Hotspot lastHotspot = null;
		protected Button button = null;
		protected int interactionIndex = -1;
		protected Hotspot manualHotspot;
		protected string movingToHotspotLabel = "";
		protected bool ignoreInputThisFrame = false;
		protected int lastClickedCursorID;

		private const int MaxRaycastHits = 5;
		private readonly RaycastHit[] results = new RaycastHit[MaxRaycastHits];


		protected void OnEnable ()
		{
			EventManager.OnInitialiseScene += OnInitialiseScene;
			EventManager.OnInventoryInteract += OnInventoryInteract;
			EventManager.OnInventoryCombine += OnInventoryCombine;
			EventManager.OnEnterGameState += OnEnterGameState;
		}

		
		protected void OnDisable ()
		{
			EventManager.OnInitialiseScene -= OnInitialiseScene;
			EventManager.OnInventoryInteract -= OnInventoryInteract;
			EventManager.OnInventoryCombine -= OnInventoryCombine;
			EventManager.OnEnterGameState -= OnEnterGameState;
		}


		/** Updates the interaction handler. This is called every frame by StateHandler. */
		public void UpdateInteraction ()
		{
			if (KickStarter.stateHandler.IsInGameplay ())	
			{
				if (KickStarter.playerInput.GetDragState () == DragState.Moveable)
				{
					DeselectHotspot (true);
					ignoreInputThisFrame = false;
					return;
				}
				
				if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.CustomScript && KickStarter.playerInput.GetMouseState () == MouseState.RightClick && InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && !KickStarter.playerMenus.IsMouseOverMenu ())
				{
					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.settingsManager.cycleInventoryCursors)
					{
						// Don't respond to right-clicks
					}
					else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.cycleInventoryCursors)
					{
						KickStarter.playerInput.ResetMouseClick ();
						KickStarter.runtimeInventory.SetNull ();
					}
					else if (KickStarter.settingsManager.RightClickInventory == RightClickInventory.DeselectsItem)
					{
						KickStarter.playerInput.ResetMouseClick ();
						KickStarter.runtimeInventory.SetNull ();
					}
					else if (KickStarter.settingsManager.RightClickInventory == RightClickInventory.ExaminesItem && KickStarter.cursorManager.lookUseCursorAction != LookUseCursorAction.RightClickCyclesModes && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
					{
						KickStarter.playerInput.ResetMouseClick ();
						KickStarter.runtimeInventory.SelectedInstance.Examine ();
					}
					else if (KickStarter.settingsManager.RightClickInventory == RightClickInventory.ExaminesHotspot && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
					{
						if (hotspot)
						{
							hotspot.RunExamineInteraction ();
						}
					}
				}
				
				if (KickStarter.playerInput.IsCursorLocked () && KickStarter.settingsManager.onlyInteractWhenCursorUnlocked && KickStarter.settingsManager.IsInFirstPerson ())
				{
					DeselectHotspot (true);
					ignoreInputThisFrame = false;
					return;
				}

				if (UnityUIBlocksClick ())
				{
					DeselectHotspot (true);
					ignoreInputThisFrame = false;
					return;
				}
				
				if (!KickStarter.playerInput.IsCursorReadable ())
				{
					if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity &&
						KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen &&
						KickStarter.player &&
						KickStarter.player.hotspotDetector)
					{
						// Special case: Highlight hotspots here, because they don't rely on the mouse position to do so
						KickStarter.player.hotspotDetector.HighlightAll ();
					}
					ignoreInputThisFrame = false;
					return;
				}

				HandleInteractionMenu ();
				
				if (KickStarter.settingsManager.playerFacesHotspots && KickStarter.player)
				{
					if (KickStarter.settingsManager.interactionMethod != AC_InteractionMethod.ChooseHotspotThenInteraction || !KickStarter.settingsManager.onlyFaceHotspotOnSelect)
					{
						if (hotspot && hotspot.playerTurnsHead)
						{
							KickStarter.player.SetHeadTurnTarget (hotspot.transform, hotspot.GetFacingPosition (true), false, HeadFacing.Hotspot);
						}
						else if (button == null)
						{
							KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
						}
					}
					else if (button == null && hotspot == null && !KickStarter.playerMenus.IsInteractionMenuOn ())
					{
						KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
					}
				}
			}
			else if (KickStarter.stateHandler.gameState == GameState.Paused)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot && KickStarter.playerMenus.IsPausingInteractionMenuOn ())
				{
					HandleInteractionMenu ();
				}
			}

			ignoreInputThisFrame = false;
		}


		/** Updates the internal 'Hotspot label' according to what, if any, Hotspot is currently selected, and the currently-selected icon or inventory item. */
		public void UpdateInteractionLabel ()
		{
			interactionLabel = GetInteractionLabel (Options.GetLanguage ());
		}


		protected void HandleInteractionMenu ()
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
			{
				CustomScriptMethod ();
				return;
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.LetGo && !KickStarter.playerMenus.IsMouseOverInteractionMenu () && KickStarter.settingsManager.ReleaseClickInteractions ())
			{
				KickStarter.playerMenus.CloseInteractionMenus ();
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.LetGo && !KickStarter.playerMenus.IsMouseOverInteractionMenu () && KickStarter.settingsManager.ReleaseClickInteractions ())
			{
				KickStarter.playerMenus.CloseInteractionMenus ();
			}

			if (!KickStarter.playerMenus.IsMouseOverMenu () && KickStarter.CameraMain && !KickStarter.playerInput.ActiveArrowsDisablingHotspots () &&
				(KickStarter.mainCamera == null || KickStarter.mainCamera.IsPointInCamera (KickStarter.playerInput.GetMousePosition ())))
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
					{
						ContextSensitiveClick ();
					}
					else if (!KickStarter.playerMenus.IsMouseOverInteractionMenu ())
					{
						ChooseHotspotThenInteractionClick ();
					}
				}
				else
				{
					ContextSensitiveClick ();
				}
			}
			else 
			{
				if (KickStarter.playerMenus.IsMouseOverInteractionMenu () && !InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
				{
					// Don't deselect Hotspot
					return;
				}

				DeselectHotspot (false);
			}
		}
		

		/**
		 * De-selects the current inventory item, if appropriate.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateInventory ()
		{
			if (hotspot == null && button == null && IsDroppingInventory ())
			{
				if (KickStarter.playerMenus.EventSystem && KickStarter.playerMenus.EventSystem.IsPointerOverGameObject () && !KickStarter.settingsManager.InventoryDragDrop)
				{
					// Don't null if over interactive Menu Element (Unity UI issue)
					return;
				}
				KickStarter.runtimeInventory.SetNull ();
			}
		}


		/**
		 * <summary>Sets the active Hotspot, provided that the chosen hotspot detection method in Settings Manager is CustomScript.</summary>
		 * <param name = "_hotspot">The Hotspot to make active</param>
		 */
		public void SetActiveHotspot (Hotspot _hotspot)
		{
			hotspot = manualHotspot = _hotspot;

			if (hotspot)
			{
				lastHotspot = hotspot;
			}

			if (KickStarter.settingsManager.hotspotDetection != HotspotDetection.CustomScript)
			{
				ACDebug.LogWarning ("The 'Hotspot detection method' setting must be set to 'Custom Script' in order for Hotspots to be set active manually.");
			}
		}

		
		protected Hotspot CheckForHotspots ()
		{
			if (!KickStarter.playerInput.IsMouseOnScreen ())
			{
				return null;
			}

			if (KickStarter.settingsManager.InventoryDragDrop && KickStarter.playerInput.GetMousePosition () == Vector2.zero)
			{
				return null;
			}

			if (KickStarter.playerInput.GetDragState () == DragState._Camera)
			{
				return null;
			}

			if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.CustomScript)
			{
				return manualHotspot;
			}
			else if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity)
			{
				if (KickStarter.player && KickStarter.player.hotspotDetector)
				{
					if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct || KickStarter.settingsManager.IsInFirstPerson ())
					{
						if (KickStarter.settingsManager.hotspotsInVicinity == HotspotsInVicinity.ShowAll)
						{
							// Just highlight the nearest hotspot, but don't make it the "active" one
							KickStarter.player.hotspotDetector.HighlightAll ();
						}
						else
						{
							return CheckHotspotValid (KickStarter.player.hotspotDetector.GetSelected ());
						}
					}
					else
					{
						// Just highlight the nearest hotspot, but don't make it the "active" one
						KickStarter.player.hotspotDetector.HighlightAll ();
					}
				}
				else
				{
					ACDebug.LogWarning ("Both a Player and a Hotspot Detector on that Player are required for Hotspots to be detected by 'Player Vicinity'");
					return null;
				}
			}

			if (SceneSettings.IsUnity2D ())
			{
				RaycastHit2D hit;
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					hit = UnityVersionHandler.Perform2DRaycast (
						KickStarter.CameraMain.ScreenToWorldPoint (KickStarter.playerInput.GetMousePosition ()),
						Vector3.zero,
						KickStarter.settingsManager.hotspotRaycastLength,
						HotspotLayerMask
						);
				}
				else
				{
					Vector3 pos = KickStarter.playerInput.GetMousePosition ();
					pos.z = -KickStarter.CameraMainTransform.position.z;

					hit = UnityVersionHandler.Perform2DRaycast (
						KickStarter.CameraMain.ScreenToWorldPoint (pos),
						Vector2.zero,
						KickStarter.settingsManager.hotspotRaycastLength,
						HotspotLayerMask
						);
				}

				if (hit.collider)
				{
					Hotspot hitHotspot = hit.collider.gameObject.GetComponent <Hotspot>();
					if (hitHotspot)
					{
						if (KickStarter.settingsManager.hotspotDetection != HotspotDetection.PlayerVicinity)
						{
							return (CheckHotspotValid (hitHotspot));
						}
						else if (KickStarter.player.hotspotDetector && KickStarter.player.hotspotDetector.IsHotspotInTrigger (hitHotspot))
						{
							return (CheckHotspotValid (hitHotspot));
						}
					}
				}
			}
			else
			{
				Camera _camera = KickStarter.CameraMain;
				if (_camera)
				{
					Ray ray = _camera.ScreenPointToRay (KickStarter.playerInput.GetMousePosition ());
					RaycastHit hit;
					
					if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.hotspotRaycastLength, HotspotLayerMask))
					{
						Hotspot hitHotspot = hit.collider.gameObject.GetComponent <Hotspot>();
						if (hitHotspot)
						{
							if (KickStarter.settingsManager.hotspotDetection != HotspotDetection.PlayerVicinity)
							{
								return (CheckHotspotValid (hitHotspot));
							}
							else if (KickStarter.player && KickStarter.player.hotspotDetector && KickStarter.player.hotspotDetector.IsHotspotInTrigger (hitHotspot))
							{
								return (CheckHotspotValid (hitHotspot));
							}
						}
					}
				}
			}
			
			return null;
		}


		protected Hotspot CheckHotspotValid (Hotspot hotspot)
		{
			if (hotspot == null || !hotspot.enabled) return null;

			if (!hotspot.PlayerIsWithinBoundary ())
			{
				return null;
			}

			if (KickStarter.settingsManager.AutoDisableUnhandledHotspots)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (!hotspot.HasInventoryInteraction (KickStarter.runtimeInventory.SelectedInstance.InvItem))
					{
						return null;
					}
				}
			}

			return hotspot;
		}
		
		
		protected bool CanDoDoubleTap ()
		{
			if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.InventoryDragDrop)
				return false;
			
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.settingsManager.doubleTapHotspots)
				return true;
			
			return false;
		}
		
		
		protected void ChooseHotspotThenInteractionClick ()
		{
			if (CanDoDoubleTap ())
			{
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
				{
					ChooseHotspotThenInteractionClick_Process (true);
				}
			}
			else
			{
				ChooseHotspotThenInteractionClick_Process (false);
			}
		}
		
		
		protected void ChooseHotspotThenInteractionClick_Process (bool doubleTap)
		{
			Hotspot newHotspot = CheckForHotspots ();
			if (hotspot && newHotspot == null)
			{
				DeselectHotspot (false);
			}
			else if (newHotspot)
			{
				if (newHotspot.IsSingleInteraction ())
				{
					ContextSensitiveClick ();
					return;
				}

				bool clickedNew = false;
				if (newHotspot != hotspot)
				{
					clickedNew = true;
						
					if (hotspot)
					{
						hotspot.Deselect ();
						KickStarter.playerMenus.DisableHotspotMenus ();
					}
						
					if (KickStarter.settingsManager.cancelInteractions != CancelInteractions.ViaScriptOnly)
					{
						if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || !KickStarter.settingsManager.CanClickOffInteractionMenu ())
						{
							if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
							{
								if (hotspot == null)
								{
									KickStarter.playerMenus.CloseInteractionMenus ();
								}
							}
							if (hotspot)
							{
								KickStarter.playerMenus.CloseInteractionMenus ();
							}
						}
					}

					lastHotspot = hotspot = newHotspot;
			
					hotspot.Select ();
				}

				if (hotspot)
				{
					if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick ||
						(KickStarter.settingsManager.InventoryDragDrop && IsDroppingInventory ()) ||
						(KickStarter.settingsManager.MouseOverForInteractionMenu () && !InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && clickedNew && !IsDroppingInventory ()))
					{
						if (!InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && 
							KickStarter.settingsManager.MouseOverForInteractionMenu () && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu &&
							KickStarter.settingsManager.cancelInteractions != CancelInteractions.ClickOffMenu &&
							!(InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && !KickStarter.settingsManager.cycleInventoryCursors))
						{
							return;
						}
						if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
						{
							if (! KickStarter.settingsManager.InventoryDragDrop && clickedNew && doubleTap)
							{
								return;
							} 
							else
							{
								HandleInteraction ();
							}
						}
						else if (KickStarter.playerMenus)
						{
							if (KickStarter.settingsManager.playerFacesHotspots && KickStarter.player && KickStarter.settingsManager.onlyFaceHotspotOnSelect)
							{
								if (hotspot && hotspot.playerTurnsHead)
								{
									KickStarter.player.SetHeadTurnTarget (hotspot.transform, hotspot.GetFacingPosition (true), false, HeadFacing.Hotspot);
								}
							}

							if (KickStarter.playerMenus.IsInteractionMenuOn () && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
							{
								if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
								{
									ClickHotspotToInteract (hotspot);
									return;
								}
							}
								
							if (clickedNew && doubleTap)
							{
								return;
							}

							if (KickStarter.settingsManager.SeeInteractions != SeeInteractions.ViaScriptOnly)
							{
								KickStarter.playerMenus.EnableInteractionMenus (hotspot);
								
								if (KickStarter.settingsManager.SeeInteractions == SeeInteractions.ClickOnHotspot)
								{
									if (KickStarter.settingsManager.stopPlayerOnClickHotspot && KickStarter.player)
									{
										StopMovingToHotspot ();
									}
										
									StopInteraction ();
									KickStarter.runtimeInventory.SetNull ();
								}
							}
						}
					}
					else if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
					{
						hotspot.Deselect ();
					}
				}
			}
		}


		protected bool IsInvokingDefaultInteraction ()
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot &&
				KickStarter.settingsManager.allowDefaultinteractions &&
				KickStarter.playerInput.InputGetButtonDown ("DefaultInteraction"))
			{
				return true;
			}
			return false;
		}


		protected void ContextSensitiveClick ()
		{
			if (hotspot != null &&
				IsInvokingDefaultInteraction () &&
				hotspot.provideUseInteraction)
			{
				UseHotspot (hotspot);
				return;
			}

			if (CanDoDoubleTap ())
			{
				// Detect Hotspots only on mouse click
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick ||
					KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick)
				{
					// Check Hotspots only when click/tap
					ContextSensitiveClick_Process (true, CheckForHotspots ());
				}
				else if (KickStarter.playerInput.GetMouseState () == MouseState.RightClick)
				{
					HandleInteraction ();
				}
			}
			else
			{
				// Always detect Hotspots
				ContextSensitiveClick_Process (false, CheckForHotspots ());

				if (!KickStarter.playerMenus.IsMouseOverMenu () && hotspot)
				{
					bool requireDoubleClick = (hotspot.doubleClickingHotspot == DoubleClickingHotspot.IsRequiredToUse &&
												(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive ||
												(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && hotspot.IsSingleInteraction ())));

					if ((KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && !requireDoubleClick) || KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick || KickStarter.playerInput.GetMouseState () == MouseState.RightClick || IsDroppingInventory ())
					{
						if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot &&
							(!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) || (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.cycleInventoryCursors)))
						{
							if (KickStarter.playerInput.GetMouseState () != MouseState.RightClick)
							{
								ClickHotspotToInteract (hotspot);
							}
						}
						else
						{
							HandleInteraction ();
						}
					}
				}
			}
		}
		

		protected void CustomScriptMethod ()
		{
			if (KickStarter.settingsManager.hotspotDetection != HotspotDetection.CustomScript)
			{
				Hotspot newHotspot = CheckForHotspots ();
				if (hotspot && newHotspot == null)
				{
					DeselectHotspot (false);
				}
				else if (newHotspot && newHotspot != hotspot)
				{
					DeselectHotspot (false);
					lastHotspot = hotspot = newHotspot;
					hotspot.Select ();
				}
			}
		}

		
		protected void ContextSensitiveClick_Process (bool doubleTap, Hotspot newHotspot)
		{
			if (hotspot && newHotspot == null)
			{
				DeselectHotspot (false);
			}
			else if (newHotspot)
			{
				if (newHotspot != hotspot)
				{
					DeselectHotspot (false); 
					
					lastHotspot = hotspot = newHotspot;

					hotspot.Select ();

					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
					{
						hotspot.RestoreInteraction ();
					}
				}
				else if (hotspot && doubleTap)
				{
					// Still work if not clicking on the active Hotspot
					HandleInteraction ();
				}
			}
		}


		public void PreAutoCycle ()
		{
			int originalIndex = interactionIndex;
			SetNextInteraction ();
			interactionIndex = originalIndex;
		}
		

		/**
		 * <summary>De-selects the active Hotspot.</summary>
		 * <param name = "isInstant">If True, then any highlight effects being applied to the Hotspot will be instantly removed</param>
		 */
		public void DeselectHotspot (bool isInstant = false)
		{
			if (hotspot)
			{
				if (isInstant)
				{
					hotspot.DeselectInstant ();
				}
				else
				{
					hotspot.Deselect ();
				}
				hotspot = null;
			}
		}
		

		/**
		 * <summary>Checks if the active Hotspot has an enabled inventory interaction that matches the currently-selected inventory item.</summary>
		 * <returns>True if the active Hotspot has an an enabled inventory interaction that matches the currently-selected inventory item</returns>
		 */
		public bool DoesHotspotHaveInventoryInteraction ()
		{
			if (hotspot && KickStarter.runtimeInventory && InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
			{
				for (int i=0; i<hotspot.invButtons.Count; i++)
				{
					if (hotspot.invButtons[i].invID == KickStarter.runtimeInventory.SelectedInstance.ItemID && !hotspot.invButtons[i].isDisabled)
					{
						return true;
					}
				}
			}
			
			return false;
		}
		
		
		protected void HandleInteraction ()
		{
			if (hotspot)
			{
				switch (KickStarter.settingsManager.interactionMethod)
				{
					case AC_InteractionMethod.ContextSensitive:
						{
							if (KickStarter.playerInput.GetMouseState() == MouseState.SingleClick || KickStarter.playerInput.GetMouseState() == MouseState.DoubleClick)
							{
								if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.RightClickCyclesModes)
								{
									if (KickStarter.playerCursor.ContextCycleExamine && hotspot.HasContextLook())
									{
										// Perform "Look" interaction
										ClickButton (InteractionType.Examine, -1);
									}
									else if (hotspot.HasContextUse())
									{
										// Perform "Use" interaction
										ClickButton (InteractionType.Use, -1);
									}
									return;
								}

								if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && hotspot.HasContextUse())
								{
									// Perform "Use" interaction
									ClickButton (InteractionType.Use, -1);
								}
								else if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
								{
									// Perform "Use Inventory" interaction
									ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.SelectedInstance);
								}
								else if (hotspot.HasContextLook() && KickStarter.cursorManager.leftClickExamine)
								{
									// Perform "Look" interaction
									ClickButton (InteractionType.Examine, -1);
								}
								else
								{
									if (hotspot.walkToMarker)
									{
										ClickHotspotToWalk (hotspot.walkToMarker);
									}
								}

							}
							else if (KickStarter.playerInput.GetMouseState() == MouseState.RightClick)
							{
								if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && hotspot.HasContextLook() && KickStarter.cursorManager.lookUseCursorAction != LookUseCursorAction.RightClickCyclesModes)
								{
									// Perform "Look" interaction
									ClickButton (InteractionType.Examine, -1);
								}
							}
							else if (KickStarter.settingsManager.InventoryDragDrop && IsDroppingInventory ())
							{
								// Perform "Use Inventory" interaction (Drag n' drop mode)
								ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.SelectedInstance);
								KickStarter.runtimeInventory.SetNull ();
							}
						}
						break;

					case AC_InteractionMethod.ChooseInteractionThenHotspot:
						{
							if (KickStarter.playerInput.GetMouseState() == MouseState.SingleClick)
							{
								if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && hotspot.provideUseInteraction)
								{
									// Perform "Use" interaction
									if (hotspot.IsSingleInteraction ())
									{
										ClickButton (InteractionType.Use, -1);
									}
									else if (KickStarter.playerCursor.IsInWalkMode () && hotspot && hotspot.walkToMarker)
									{
										ClickHotspotToWalk (hotspot.walkToMarker);
									}
									else if (KickStarter.playerCursor.GetSelectedCursor() >= 0)
									{
										ClickButton (InteractionType.Use, KickStarter.cursorManager.cursorIcons[KickStarter.playerCursor.GetSelectedCursor()].id, null, GetActiveHotspot());
									}
									else
									{
										if (KickStarter.cursorManager.allowWalkCursor && hotspot && hotspot.walkToMarker)
										{
											ClickHotspotToWalk (hotspot.walkToMarker);
										}
									}
								}
								else if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.playerCursor.GetSelectedCursor () == -2)
								{
									// Perform "Use Inventory" interaction
									InvInstance invInstance = KickStarter.runtimeInventory.SelectedInstance;

									bool isDefined = hotspot.HasInventoryInteraction (KickStarter.runtimeInventory.SelectedItem);
									if (KickStarter.settingsManager.InventoryDragDrop || (KickStarter.settingsManager.inventoryDisableDefined && isDefined) || (KickStarter.settingsManager.inventoryDisableUnhandled && !isDefined))
									{
										KickStarter.playerCursor.ResetSelectedCursor ();
									}

									ClickButton (InteractionType.Inventory, -1, invInstance);
								}
							}
							else if (KickStarter.settingsManager.InventoryDragDrop && IsDroppingInventory())
							{
								// Perform "Use Inventory" interaction (Drag n' drop mode)
								ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.SelectedInstance);
							}
						}
						break;

					case AC_InteractionMethod.ChooseHotspotThenInteraction:
						{
							if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.CanSelectItems (false))
							{
								if (KickStarter.playerInput.GetMouseState() == MouseState.SingleClick || KickStarter.playerInput.GetMouseState() == MouseState.DoubleClick)
								{
									// Perform "Use Inventory" interaction
									ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.SelectedInstance);
									return;
								}
								else if (KickStarter.settingsManager.InventoryDragDrop && IsDroppingInventory())
								{
									// Perform "Use Inventory" interaction
									ClickButton (InteractionType.Inventory, -1, KickStarter.runtimeInventory.SelectedInstance);

									KickStarter.runtimeInventory.SetNull();
									return;
								}
							}
							else if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && hotspot.IsSingleInteraction())
							{
								// Perform "Use" interaction
								ClickButton (InteractionType.Use, -1);
							}
						}
						break;

					default:
						break;
				}
			}
		}


		protected void ClickHotspotToWalk (Marker walkToMarker)
		{
			if (!KickStarter.settingsManager.walkToHotspotMarkers)
			{
				return;
			}

			StopInteraction ();
			//StopMovingToHotspot ();

			KickStarter.playerInput.ResetMouseClick ();
			KickStarter.playerInput.ResetClick ();

			if (KickStarter.player)
			{
				KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
				Vector3[] pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.Transform.position, walkToMarker.Position, KickStarter.player);
				KickStarter.player.MoveAlongPoints (pointArray, false);
			}
		}


		/**
		 * <summary>Runs a Hotspot's 'use' interaction.</summary>
		 * <param name = "_hotspot">The Hotspot to use</param>
		 * <param name = "selectedCursorID">The ID number of the current cursor. If -1, the Hotspot's first available 'use' interaction will be triggered</param>
		 */
		public void UseHotspot (Hotspot _hotspot, int selectedCursorID = -1)
		{
			ClickButton (InteractionType.Use, selectedCursorID, null, _hotspot);
		}


		/**
		 * <summary>Runs a Hotspot's 'look' interaction.</summary>
		 * <param name = "_hotspot">The Hotspot to examine</param>
		 */
		public void ExamineHotspot (Hotspot _hotspot)
		{
			ClickButton (InteractionType.Examine, -1, null, _hotspot);
		}


		/**
		 * <summary>Runs a Hotspot's 'use inventory' interaction.</summary>
		 * <param name = "_hotspot">The Hotspot to examine</param>
		 * <param name = "inventoryItemID">The ID number of the inventory item (see InvItem)</param>
		 * <param name = "requireCarry">If the SettingsManager's interactionMethod is CustomScript, the item must be carried by the player for the interaction to trigger</param>
		 */
		public void UseInventoryOnHotspot (Hotspot _hotspot, InvInstance invInstance, bool requireCarry = true)
		{
			if (!InvInstance.IsValid (invInstance)) return;

			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript && requireCarry && !KickStarter.runtimeInventory.IsCarryingItem (invInstance))
			{
				ACDebug.Log ("Cannot use item " + invInstance.InvItem.label + " as the player is not carrying it.");
				return;
			}

			ClickButton (InteractionType.Inventory, -1, invInstance, _hotspot);
		}
		

		protected void ClickButton (InteractionType _interactionType, int selectedCursorID, InvInstance selectedInvInstance = null, Hotspot clickedHotspot = null)
		{
			if (ignoreInputThisFrame)
			{
				return;
			}

			inPreInteractionCutscene = false;
			StopAllCoroutines ();

			lastClickedCursorID = selectedCursorID;
		
			if (clickedHotspot)
			{
				lastHotspot = hotspot = clickedHotspot;
			}

			if (hotspot == null)
			{
				ACDebug.LogWarning ("Cannot process Hotspot interaction, because no Hotspot was set!");
				return;
			}
			
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (KickStarter.settingsManager.autoCycleWhenInteract)
				{
					SetNextInteraction ();
				}
				else
				{
					ResetInteractionIndex ();
				}
			}
			else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.autoCycleWhenInteract)
			{
				KickStarter.playerCursor.ResetSelectedCursor ();
			}

			KickStarter.playerInput.ResetMouseClick ();
			KickStarter.playerInput.ResetClick ();
			button = null;

			switch (_interactionType)
			{
				case InteractionType.Use:
					if (selectedCursorID == -1)
					{
						button = hotspot.GetFirstUseButton ();
					}
					else
					{
						foreach (Button _button in hotspot.useButtons)
						{
							if (_button.iconID == selectedCursorID && !_button.isDisabled)
							{
								button = _button;
								break;
							}
						}

						if (button == null && KickStarter.cursorManager.AllowUnhandledIcons ())
						{
							if (hotspot.provideUnhandledUseInteraction && !hotspot.unhandledUseButton.isDisabled)
							{
								button = hotspot.unhandledUseButton;
								button.iconID = selectedCursorID;
							}
						}

						if (button == null && KickStarter.cursorManager.AllowUnhandledIcons ())
						{
							ActionListAsset _actionListAsset = KickStarter.cursorManager.GetUnhandledInteraction (selectedCursorID);
							RunUnhandledHotspotInteraction (_actionListAsset, clickedHotspot, KickStarter.cursorManager.passUnhandledHotspotAsParameter);

							KickStarter.runtimeInventory.SetNull ();
							if (KickStarter.player)
							{
								KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
							}
							return;
						}
					}
					break;

				case InteractionType.Examine:
					button = hotspot.lookButton;
					break;

				case InteractionType.Inventory:
					if (InvInstance.IsValid (selectedInvInstance))
					{
						foreach (Button invButton in hotspot.invButtons)
						{
							if (invButton.invID == selectedInvInstance.ItemID && !invButton.isDisabled)
							{
								if (invButton.selectItemMode == selectedInvInstance.SelectItemMode || !KickStarter.settingsManager.CanGiveItems ())
								{
									button = invButton;
									break;
								}
							}
						}

						if (button == null && KickStarter.settingsManager.CanGiveItems ())
						{
							foreach (Button invButton in hotspot.invButtons)
							{
								if (invButton.invID == selectedInvInstance.ItemID && !invButton.isDisabled && invButton.selectItemMode != selectedInvInstance.SelectItemMode)
								{
									ACDebug.LogWarning ("Can't run Hotspot " + hotspot.name + "'s Inventory interaction because the Item Selection Mode does not match.");
									break;
								}
							}
						}

						if (button == null && hotspot.provideUnhandledInvInteraction && hotspot.unhandledInvButton != null)
						{
							button = hotspot.unhandledInvButton;
						}
					}
					break;

				default:
					break;
			}

			if (button != null && button.isDisabled)
			{
				button = null;

				if (_interactionType != InteractionType.Inventory)
				{
					if (KickStarter.player)
					{
						KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
					}
					return;
				}
			}

			KickStarter.eventManager.Call_OnInteractHotspot (hotspot, button);
			StartCoroutine (UseObject (selectedInvInstance));
		}
		
		
		protected IEnumerator UseObject (InvInstance selectedInvInstance)
		{
			bool doRun = false;
			bool doSnap = false;

			if (hotspotMovingTo == hotspot && KickStarter.playerInput.LastClickWasDouble ())
			{
				KickStarter.eventManager.Call_OnDoubleClickHotspot (hotspot, button);

				if (hotspot.oneClick || 
					(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive) || 
					(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && hotspot.oneClick))
				{
					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
					{ }
					else
					{
						switch (hotspotMovingTo.doubleClickingHotspot)
						{
							case DoubleClickingHotspot.TriggersInteractionInstantly:
								doSnap = true;
								break;

							case DoubleClickingHotspot.MakesPlayerRun:
								doRun = true;
								break;

							default:
								break;
						}
					}
				}
			}
			
			if (KickStarter.player)
			{
				switch (KickStarter.player.runningLocked)
				{
					case PlayerMoveLock.AlwaysWalk:
						doRun = false;
						break;

					case PlayerMoveLock.AlwaysRun:
						doRun = true;
						break;

					default:
						break;
				}
			}
			
			if (KickStarter.player)
			{
				if (button != null && (button.playerAction == PlayerAction.DoNothing || button.playerAction == PlayerAction.TurnToFace))
				{
					KickStarter.player.EndPath ();
				}

				if (button != null && (button.playerAction == PlayerAction.WalkToMarker || button.playerAction == PlayerAction.WalkTo))
				{
					if (!KickStarter.player.AllDirectionsLocked ())
					{
						if (button.isBlocking)
						{
							inPreInteractionCutscene = true;
						}

						hotspotMovingTo = hotspot;
						movingToHotspotLabel = button.GetFullLabel (hotspot, selectedInvInstance, Options.GetLanguage ());
					}
				}
				else
				{
					if (button != null && button.playerAction != PlayerAction.DoNothing)
					{
						inPreInteractionCutscene = true;
					}
					hotspotMovingTo = null;
				}
			}
			
			Hotspot _hotspot = hotspot;
			if (KickStarter.player == null || inPreInteractionCutscene || (button != null && button.playerAction == PlayerAction.DoNothing))
			{
				DeselectHotspot ();
			}

			if (KickStarter.player)
			{
				if (button != null && button.playerAction != PlayerAction.DoNothing)
				{
					Vector3 lookVector = Vector3.zero;
					Vector3 targetPos = _hotspot.Transform.position;

					Vector3 _hotspotCentre = (_hotspot.centrePoint && _hotspot.centrePointOverrides != CentrePointOverrides.IconPositionOnly) ? _hotspot.centrePoint.position : _hotspot.Transform.position;
					if (SceneSettings.ActInScreenSpace ())
					{
						lookVector = AdvGame.GetScreenDirection (KickStarter.player.Transform.position, _hotspotCentre);
					}
					else
					{
						lookVector = _hotspotCentre - KickStarter.player.Transform.position;
						lookVector.y = 0;
					}
					
					KickStarter.player.SetLookDirection (lookVector, false);
					
					if (button.playerAction == PlayerAction.TurnToFace)
					{
						while (KickStarter.player.IsTurning ())
						{
							yield return new WaitForFixedUpdate ();			
						}
					}
					
					if (button.playerAction == PlayerAction.WalkToMarker && _hotspot.walkToMarker)
					{
						bool skipSnapping = false;
						if (button.playerAction == PlayerAction.WalkToMarker && !button.isBlocking && doSnap)
						{
							skipSnapping = button.doubleClickDoesNotSnapPlayerToMarker;
						}

						if (!KickStarter.player.AllDirectionsLocked () && Vector3.Distance (KickStarter.player.Transform.position, _hotspot.walkToMarker.Position) > KickStarter.settingsManager.GetDestinationThreshold ())
						{
							if (KickStarter.navigationManager)
							{
								Vector3[] pointArray;
								Vector3 targetPosition = _hotspot.walkToMarker.Position;
								
								if (SceneSettings.ActInScreenSpace ())
								{
									targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
								}
								
								pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.Transform.position, targetPosition, KickStarter.player);

								if (pointArray.Length > 0)
								{
									KickStarter.player.MoveAlongPoints (pointArray, doRun);
									targetPos = pointArray [pointArray.Length - 1];
								}
								else
								{
									ACDebug.LogWarning ("Cannot calculate path to Hotspot " + _hotspot.name + "'s marker.  Moving without pathfinding!", _hotspot.walkToMarker);
									KickStarter.player.MoveToPoint (targetPosition, doRun);
									targetPos = targetPosition;
								}

								if (KickStarter.player.retroPathfinding)
								{
									// Update the speed on the same frame so that we don't have a frame of zero moveSpeed
									KickStarter.player._Update ();
								}
							}
							
							while (KickStarter.player.GetPath ())
							{
								if (doSnap)
								{
									if (!skipSnapping)
									{
										KickStarter.player.Teleport (targetPos);
									}
									break;
								}
								yield return new WaitForFixedUpdate ();
							}
						}
						
						if (button.faceAfter)
						{
							lookVector = _hotspot.walkToMarker.ForwardDirection;
							lookVector.y = 0;

							KickStarter.player.EndPath ();
							KickStarter.player.SetLookDirection (lookVector, false);
							
							while (KickStarter.player.IsTurning ())
							{
								if (doSnap)
								{
									if (!skipSnapping)
									{
										KickStarter.player.SetLookDirection (lookVector, true);
									}
									break;
								}

								yield return new WaitForEndOfFrame ();
							}
						}
					}
					
					else if (button.playerAction == PlayerAction.WalkTo)
					{
						float dist = Vector3.Distance (KickStarter.player.Transform.position, targetPos);
						if (_hotspot.walkToMarker)
						{
							dist = Vector3.Distance (KickStarter.player.Transform.position, _hotspot.walkToMarker.Position);
						}

						if (!KickStarter.player.AllDirectionsLocked ())
						{
							if ((button.setProximity && dist > button.proximity) ||
								(!button.setProximity && dist > 2f))
							{
								if (KickStarter.navigationManager)
								{
									Vector3[] pointArray;
									Vector3 targetPosition = _hotspot.Transform.position;
									if (_hotspot.walkToMarker)
									{
										targetPosition = _hotspot.walkToMarker.Position;
									}
									
									if (SceneSettings.ActInScreenSpace ())
									{
										targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
									}
									
									pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.Transform.position, targetPosition, KickStarter.player);
									KickStarter.player.MoveAlongPoints (pointArray, doRun);

									if (pointArray.Length > 0)
									{
										targetPos = pointArray [pointArray.Length - 1];
									}
									else
									{
										targetPos = KickStarter.player.Transform.position;
									}

									if (KickStarter.player.retroPathfinding)
									{
										// Update the speed on the same frame so that we don't have a frame of zero moveSpeed
										KickStarter.player._Update ();
									}
								}
								
								if (button.setProximity)
								{
									float proxSqrd = Mathf.Pow (button.proximity, 2);

									if (SceneSettings.IsUnity2D ())
									{
										targetPos.z = KickStarter.player.Transform.position.z;
									}
									else
									{
										targetPos.y = KickStarter.player.Transform.position.y;
									}
									
									while ((KickStarter.player.Transform.position - targetPos).sqrMagnitude > proxSqrd && KickStarter.player.GetPath ())
									{
										if (doSnap)
										{
											break;
										}
										yield return new WaitForFixedUpdate ();
									}
								}
								else
								{
									if (!doSnap)
									{
										yield return new WaitForSeconds (0.6f);
									}
								}
							}
						}

						if (button.faceAfter)
						{
							KickStarter.player.EndPath ();
							KickStarter.player.SetLookDirection (lookVector, false);
							while (KickStarter.player.IsTurning ())
							{
								if (doSnap)
								{
									KickStarter.player.SetLookDirection (lookVector, true);
									break;
								}
								yield return new WaitForEndOfFrame ();
							}
						}
					}

					KickStarter.eventManager.Call_OnHotspotReach (hotspotMovingTo, button);
				}
				else
				{
					if (KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor || KickStarter.settingsManager.movementMethod == MovementMethod.None)
					{
						KickStarter.player.StartDecelerating ();
					}
					else
					{
						KickStarter.player.charState = CharState.Decelerate;
					}
				}

				if (KickStarter.player)
				{
					KickStarter.player.EndPath (null, button == null || button.playerAction == PlayerAction.DoNothing);
				}
				hotspotMovingTo = null;
			}
			
			DeselectHotspot ();
			inPreInteractionCutscene = false;

			if (KickStarter.settingsManager.alwaysCloseInteractionMenus)
			{
				KickStarter.playerMenus.CloseInteractionMenus ();
			}
			
			if (KickStarter.player)
			{
				KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
			}
			
			if (button == null)
			{
				// Unhandled event

				if (InvInstance.IsValid (selectedInvInstance))
				{
					if (selectedInvInstance.InvItem.unhandledActionList && selectedInvInstance.SelectItemMode == SelectItemMode.Use)
					{
						ActionListAsset unhandledActionList = selectedInvInstance.InvItem.unhandledActionList;
						RunUnhandledHotspotInteraction (unhandledActionList, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
					}
					else if (selectedInvInstance.InvItem.unhandledGiveActionList && selectedInvInstance.SelectItemMode == SelectItemMode.Give)
					{
						ActionListAsset unhandledGiveActionList = selectedInvInstance.InvItem.unhandledGiveActionList;
						RunUnhandledHotspotInteraction (unhandledGiveActionList, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
					}
					else if (KickStarter.inventoryManager.unhandledGive && selectedInvInstance.SelectItemMode == SelectItemMode.Give)
					{
						RunUnhandledHotspotInteraction (KickStarter.inventoryManager.unhandledGive, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
					}
					else if (KickStarter.inventoryManager.unhandledHotspot && selectedInvInstance.SelectItemMode == SelectItemMode.Use)
					{
						RunUnhandledHotspotInteraction (KickStarter.inventoryManager.unhandledHotspot, _hotspot, KickStarter.inventoryManager.passUnhandledHotspotAsParameter);
					}
					else
					{
						if (KickStarter.settingsManager.InventoryDragDrop || (KickStarter.settingsManager.CanSelectItems (false) && KickStarter.settingsManager.inventoryDisableUnhandled))
						{
							KickStarter.runtimeInventory.SetNull ();
						}
					}
				}
				else
				{
					if (KickStarter.settingsManager.InventoryDragDrop || (KickStarter.settingsManager.CanSelectItems (false) && KickStarter.settingsManager.inventoryDisableLeft))
					{
						KickStarter.runtimeInventory.SetNull ();
					}
				}
			}
			else
			{
				if (KickStarter.settingsManager.InventoryDragDrop || KickStarter.settingsManager.inventoryDisableDefined)
				{
					KickStarter.runtimeInventory.SetNull ();
				}
				
				if (_hotspot.interactionSource == InteractionSource.AssetFile)
				{
					if (button.assetFile)
					{
						if (button.invParameterID >= 0)
						{
							ActionParameter parameter = button.assetFile.GetParameter (button.invParameterID);
							if (parameter != null && parameter.parameterType == ParameterType.InventoryItem)
							{
								parameter.intValue = (InvInstance.IsValid (selectedInvInstance)) ? selectedInvInstance.ItemID : -1;
							}
						}

						if (button.parameterID >= 0)
						{
							ActionParameter parameter = button.assetFile.GetParameter (button.parameterID);
							if (parameter != null && parameter.parameterType == ParameterType.GameObject)
							{
								parameter.gameObject = _hotspot.gameObject;
								if (_hotspot.gameObject.GetComponent <ConstantID>())
								{
									parameter.intValue = _hotspot.gameObject.GetComponent <ConstantID>().constantID;
								}
								else
								{
									ACDebug.LogWarning ("Cannot set the value of parameter " + button.parameterID + " ('" + parameter.label + "') as " + _hotspot.gameObject.name + " has no Constant ID component.", _hotspot);
								}
							}
							else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
							{
								parameter.variables = _hotspot.gameObject.GetComponent <Variables>();
							}
						}

						AdvGame.RunActionListAsset (button.assetFile);
					}
					else
					{
						if (_hotspot.GetButtonInteractionType (button) == HotspotInteractionType.UnhandledUse && KickStarter.cursorManager.AllowUnhandledIcons ())
						{
							// Special case: Unhandled use interaction with no interaction defined
							ActionListAsset _actionListAsset = KickStarter.cursorManager.GetUnhandledInteraction (lastClickedCursorID);
							RunUnhandledHotspotInteraction (_actionListAsset, _hotspot, KickStarter.cursorManager.passUnhandledHotspotAsParameter);
						}
					}
				}
				else if (_hotspot.interactionSource == InteractionSource.CustomScript)
				{
					if (button.customScriptObject && !string.IsNullOrEmpty (button.customScriptFunction))
					{
						if (InvInstance.IsValid (selectedInvInstance))
						{
							button.customScriptObject.SendMessage (button.customScriptFunction, selectedInvInstance.ItemID);
						}
						else
						{
							button.customScriptObject.SendMessage (button.customScriptFunction);
						}
					}
				}
				else if (_hotspot.interactionSource == InteractionSource.InScene)
				{
					if (button.interaction)
					{
						if (button.parameterID >= 0 && _hotspot)
						{
							ActionParameter parameter = button.interaction.GetParameter (button.parameterID);
							if (parameter != null && parameter.parameterType == ParameterType.GameObject)
							{
								parameter.gameObject = _hotspot.gameObject;
							}
							else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
							{
								parameter.variables = _hotspot.gameObject.GetComponent <Variables>();
							}
						}

						if (button.invParameterID >= 0)
						{
							ActionParameter parameter = button.interaction.GetParameter (button.invParameterID);
							if (parameter != null && parameter.parameterType == ParameterType.InventoryItem)
							{
								parameter.intValue = (InvInstance.IsValid (selectedInvInstance)) ? selectedInvInstance.ItemID : -1;
							}
						}

						button.interaction.Interact ();
					}
					else
					{
						if (_hotspot.GetButtonInteractionType (button) == HotspotInteractionType.UnhandledUse && KickStarter.cursorManager.AllowUnhandledIcons ())
						{
							// Special case: Unhandled use interaction with no interaction defined
							ActionListAsset _actionListAsset = KickStarter.cursorManager.GetUnhandledInteraction (lastClickedCursorID);
							RunUnhandledHotspotInteraction (_actionListAsset, _hotspot, KickStarter.cursorManager.passUnhandledHotspotAsParameter);
						}
					}
				}
			}
			
			button = null;

			if (KickStarter.stateHandler.IsInGameplay ())
			{
				// Prevent cursor reverting
				IgnoreInputThisFrame ();
				UpdateInteraction ();
			}
			else if (!KickStarter.settingsManager.alwaysCloseInteractionMenus)
			{
				KickStarter.playerMenus.CloseInteractionMenus ();
			}
		}


		protected void RunUnhandledHotspotInteraction (ActionListAsset _actionListAsset, Hotspot _hotspot, bool optionValue)
		{
			if (KickStarter.settingsManager.inventoryDisableUnhandled)
			{
				KickStarter.runtimeInventory.SetNull ();
			}

			if (_actionListAsset)
			{
				if (optionValue && _hotspot)
				{
					AdvGame.RunActionListAsset (_actionListAsset, _hotspot.gameObject);
				}
				else
				{
					AdvGame.RunActionListAsset (_actionListAsset);	
				}
			}
		}
		

		protected void StopInteraction ()
		{
			button = null;
			inPreInteractionCutscene = false;
			StopAllCoroutines ();
			hotspotMovingTo = null;
		}
		

		/**
		 * <summary>Gets the centre of the active Hotspot in screen space</summary>
		 * <returns>The centre of the active Hotspot in screen space</returns>
		 */
		public Vector2 GetHotspotScreenCentre ()
		{
			if (hotspot)
			{
				Vector2 screenPos = hotspot.GetIconScreenPosition ();
				return new Vector2 (screenPos.x / ACScreen.width, 1f - (screenPos.y / ACScreen.height));
			}
			return Vector2.zero;
		}


		/**
		 * <summary>Gets the centre of the last-active Hotspot in screen space</summary>
		 * <returns>The centre of the last-active Hotspot in screen space</returns>
		 */
		public Vector2 GetLastHotspotScreenCentre ()
		{
			if (GetLastOrActiveHotspot ())
			{
				Vector2 screenPos = GetLastOrActiveHotspot ().GetIconScreenPosition ();
				return new Vector2 (screenPos.x / ACScreen.width, 1f - (screenPos.y / ACScreen.height));
			}
			return Vector2.zero;
		}
		

		/**
		 * <summary>Checks if the cursor is currently over a Hotspot.</summary>
		 * <returs>True if the cursor is currently over a Hotspot</returns>
		 */
		public bool IsMouseOverHotspot ()
		{
			// Return false if we're in "Walk mode" anyway
			if (KickStarter.settingsManager && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot
				&& KickStarter.playerCursor && KickStarter.playerCursor.IsInWalkMode ())
			{
				return false;
			}
			
			if (SceneSettings.IsUnity2D ())
			{
				RaycastHit2D hit = new RaycastHit2D ();
				
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					hit = UnityVersionHandler.Perform2DRaycast (
						KickStarter.CameraMain.ScreenToWorldPoint (KickStarter.playerInput.GetMousePosition ()),
						Vector2.zero,
						KickStarter.settingsManager.hotspotRaycastLength,
						HotspotLayerMask
						);
				}
				else
				{
					Vector3 pos = KickStarter.playerInput.GetMousePosition ();
					pos.z = -KickStarter.CameraMainTransform.position.z;

					hit = UnityVersionHandler.Perform2DRaycast (
						KickStarter.CameraMain.ScreenToWorldPoint (pos),
						Vector2.zero,
						KickStarter.settingsManager.hotspotRaycastLength,
						HotspotLayerMask
						);
				}
				
				if (hit.collider && hit.collider.gameObject.GetComponent <Hotspot>())
				{
					return true;
				}
			}
			else
			{
				Ray ray = KickStarter.CameraMain.ScreenPointToRay (KickStarter.playerInput.GetMousePosition ());
				
				int numHits = Physics.RaycastNonAlloc (ray, results, KickStarter.settingsManager.hotspotRaycastLength, HotspotLayerMask);
				for (int i = 0; i < numHits; i++)
				{
					Hotspot hotspot = results[i].collider.GetComponent<Hotspot> ();
					if (hotspot)
					{
						if (!hotspot.PlayerIsWithinBoundary ())
						{
							continue;
						}
						return true;
					}
					break;
				}

				// Include moveables in query
				RaycastHit hit = new RaycastHit ();
				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.moveableRaycastLength, HotspotLayerMask))
				{
					if (hit.collider.gameObject.GetComponent <DragBase>())
					{
						return true;
					}
				}
			}
			
			return false;
		}
		

		/**
		 * <summary>Checks if the player is de-selecting or dropping the inventory in this frame.</summary>
		 * <returns>True if the player is de-selecting or dropping the inventory in this frame</returns>
		 */
		public bool IsDroppingInventory ()
		{
			if (!KickStarter.settingsManager.CanSelectItems (false))
			{
				return false;
			}
			
			if (KickStarter.stateHandler.gameState == GameState.Cutscene || KickStarter.stateHandler.gameState == GameState.DialogOptions)
			{
				return false;
			}
			
			if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
			{
				return false;
			}

			MouseState mouseState = KickStarter.playerInput.GetMouseState (false);

			if (KickStarter.settingsManager.InventoryDragDrop && mouseState == MouseState.Normal && KickStarter.playerInput.GetDragState () == DragState.Inventory)
			{
				return true;
			}
			
			if ( KickStarter.settingsManager.InventoryDragDrop && KickStarter.playerInput.CanClick () && mouseState == MouseState.Normal && KickStarter.playerInput.GetDragState () == DragState.None)
			{
				return true;
			}
			
			if (mouseState == MouseState.SingleClick && KickStarter.settingsManager.inventoryDisableLeft)
			{
				return true;
			}
			
			if (mouseState == MouseState.RightClick && KickStarter.settingsManager.RightClickInventory == RightClickInventory.DeselectsItem && KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Single)
			{
				return true;
			}

			if (mouseState == MouseState.RightClick && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.cycleInventoryCursors)
			{
				return true;
			}

			return false;
		}
		

		/**
		 * <summary>Gets the active Hotspot.</summary>
		 * <returns>The active Hotspot</returns>
		 */
		public Hotspot GetActiveHotspot ()
		{
			return hotspot;
		}


		/**
		 * <summary>Gets the last Hotspot to be active, even if none is currently active.</summary>
		 * <returns>The last Hotspot to be active</returns>
		 */
		public Hotspot GetLastOrActiveHotspot ()
		{
			if (hotspot)
			{
				lastHotspot = hotspot;
				return hotspot;
			}
			return lastHotspot;
		}
		

		/**
		 * <summary>Gets the ID number of the current "Use" Button when the interface allows for cursors being cycled when over Hotspots or inventory items.</summary>
		 * <returns>The ID number of the current "Use" Button when the interface allows for cursors being cycled when over Hotspots or inventory items.</returns>
		 */
		public int GetActiveUseButtonIconID ()
		{
			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && KickStarter.settingsManager.InventoryInteractions == AC.InventoryInteractions.Multiple)
				{
					if (interactionIndex == -1)
					{
						if (KickStarter.runtimeInventory.HoverInstance.Interactions == null || KickStarter.runtimeInventory.HoverInstance.Interactions.Length == 0)
						{
							return -1;
						}
						else
						{
							interactionIndex = 0;
							return 0;
						}
					}
					
					if (KickStarter.runtimeInventory.HoverInstance.Interactions != null && interactionIndex < KickStarter.runtimeInventory.HoverInstance.Interactions.Length)
					{
						return KickStarter.runtimeInventory.HoverInstance.Interactions [interactionIndex].icon.id;
					}
				}
				else if (GetActiveHotspot ())
				{
					if (interactionIndex == -1)
					{
						if (GetActiveHotspot ().GetFirstUseButton () == null)
						{
							return -1;
						}
						else
						{
							interactionIndex = GetActiveHotspot ().FindFirstEnabledInteraction ();
							return interactionIndex;
						}
					}
					
					if (interactionIndex < GetActiveHotspot ().useButtons.Count)
					{
						if (!GetActiveHotspot ().useButtons [interactionIndex].isDisabled)
						{
							return GetActiveHotspot ().useButtons [interactionIndex].iconID;
						}
						else
						{
							interactionIndex = -1;
							if (GetActiveHotspot ().GetFirstUseButton () == null)
							{
								return -1;
							}
							else
							{
								interactionIndex = GetActiveHotspot ().FindFirstEnabledInteraction ();
								return interactionIndex;
							}
						}
					}
				}
			}
			else if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && KickStarter.settingsManager.InventoryInteractions == AC.InventoryInteractions.Multiple)
				{
					if (interactionIndex == -1)
					{
						return -1;
					}
					
					if (KickStarter.runtimeInventory.HoverInstance.Interactions != null && interactionIndex < KickStarter.runtimeInventory.HoverInstance.Interactions.Length)
					{
						return KickStarter.runtimeInventory.HoverInstance.Interactions [interactionIndex].icon.id;
					}
				}
				else if (GetActiveHotspot ())
				{
					if (interactionIndex == -1)
					{
						if (GetActiveHotspot ().GetFirstUseButton () == null)
						{
							//return -1;
							return GetActiveHotspot ().FindFirstEnabledInteraction ();
						}
						else
						{
							interactionIndex = 0;
							return 0;
						}
					}
					
					if (interactionIndex < GetActiveHotspot ().useButtons.Count)
					{
						return GetActiveHotspot ().useButtons [interactionIndex].iconID;
					}
				}
			}
			return -1;
		}
		

		/** Cycles forward to the next available interaction for the active Hotspot or inventory item. */
		public void SetNextInteraction ()
		{
			OffsetInteraction (true);
		}


		/** Cycles backward to the previous available interaction for the active Hotspot or inventory item. */
		public void SetPreviousInteraction ()
		{
			OffsetInteraction (false);
		}


		protected void OffsetInteraction (bool goForward)
		{
			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && !InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && hotspot == null)
				{
					return;
				}
				
				if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
				{
					if (KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Single)
					{
						return;
					}

					if (goForward)
					{
						interactionIndex = KickStarter.runtimeInventory.HoverInstance.GetNextInteraction (interactionIndex);
					}
					else
					{
						interactionIndex = KickStarter.runtimeInventory.HoverInstance.GetPreviousInteraction (interactionIndex);
					}

					int activeInvButtonID = KickStarter.runtimeInventory.HoverInstance.GetActiveInvButtonID ();

					if (activeInvButtonID >= 0)
					{
						if (KickStarter.settingsManager.cycleInventoryCursors)
						{
							KickStarter.runtimeInventory.SelectItemByID (activeInvButtonID, SelectItemMode.Use);
						}
					}
					else
					{
						KickStarter.runtimeInventory.SetNull ();
					}

					KickStarter.runtimeInventory.HoverInstance.LastInteractionIndex = interactionIndex;
				}
				else if (GetActiveHotspot ())
				{
					if (goForward)
					{
						interactionIndex = GetActiveHotspot ().GetNextInteraction (interactionIndex);
					}
					else
					{
						interactionIndex = GetActiveHotspot ().GetPreviousInteraction (interactionIndex);
					}

					int activeInvButtonID = GetActiveHotspot ().GetActiveInvButtonID ();

					if (activeInvButtonID >= 0)
					{
						if (KickStarter.settingsManager.cycleInventoryCursors)
						{
							KickStarter.runtimeInventory.SelectItemByID (activeInvButtonID, SelectItemMode.Use);
						}
					}
					else
					{
						KickStarter.runtimeInventory.SetNull ();
					}

					GetActiveHotspot ().lastInteractionIndex = interactionIndex;
				}
			}
			else
			{
				// Cycle menus
				if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
				{
					if (goForward)
					{
						interactionIndex = KickStarter.runtimeInventory.HoverInstance.GetNextInteraction (interactionIndex);
					}
					else
					{
						interactionIndex = KickStarter.runtimeInventory.HoverInstance.GetPreviousInteraction (interactionIndex);
					}
				}
				else if (GetActiveHotspot ())
				{
					if (KickStarter.settingsManager.cycleInventoryCursors)
					{
						if (goForward)
						{
							interactionIndex = GetActiveHotspot ().GetNextInteraction (interactionIndex);
							
						}
						else
						{
							interactionIndex = GetActiveHotspot ().GetPreviousInteraction (interactionIndex);
						}
					}
					else
					{
						if (goForward)
						{
							interactionIndex = GetActiveHotspot ().GetNextInteraction (interactionIndex, 0);
						}
						else
						{
							interactionIndex = GetActiveHotspot ().GetPreviousInteraction (interactionIndex, 0);
						}
					}
				}
			}
		}
		

		/**
		 * Resets the active Hotspot or inventory item's selected interaction index.
		 * The interaction index is the position inside a combined List of the Hotspot or inventory item's enabled Use and Inventory Buttons.
		 */
		public void ResetInteractionIndex ()
		{
			interactionIndex = -1;
			
			if (GetActiveHotspot ())
			{
				interactionIndex = GetActiveHotspot ().FindFirstEnabledInteraction ();
			}
			else if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
			{
				interactionIndex = 0;
			}
		}
		

		/** The global interaction index. */
		public int InteractionIndex
		{
			get
			{
				return interactionIndex;
			}
			set
			{
				interactionIndex = value;
			}
		}
		

		protected void ClickHotspotToInteract (Hotspot _hotspot)
		{
			int invID = _hotspot.GetActiveInvButtonID ();
			if (invID == -1)
			{
				ClickButton (InteractionType.Use, GetActiveUseButtonIconID ());
			}
			else
			{
				ClickButton (InteractionType.Inventory, -1, new InvInstance (invID));
			}
		}


		/**
		 * <summary>Runs the appropriate interaction after the clicking of a MenuInteraction element.</summary>
		 * <param name = "_menu">The Menu that contains the MenuInteraction element</param>
		 * <param name = "iconID">The ID number of the "Use" icon, defined in CursorManager, that was clicked on</param>
		 */
		public void ClickInteractionIcon (AC.Menu _menu, int iconID)
		{
			switch (KickStarter.settingsManager.interactionMethod)
			{ 
				case AC_InteractionMethod.ContextSensitive:
					ACDebug.LogWarning ("This element is not compatible with the Context-Sensitive interaction method.");
					break;

				case AC_InteractionMethod.ChooseInteractionThenHotspot:
					KickStarter.playerCursor.SetCursorFromID (iconID);
					break;

				case AC_InteractionMethod.ChooseHotspotThenInteraction:
					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.ClickingMenu ||
						(KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && _menu.IsUnityUI () && _menu.ignoreMouseClicks))
					{
						// The second OR lets us use 'Submit' to trigger Interaction elements in Unity UI
						if (InvInstance.IsValid (_menu.TargetInvInstance))
						{
							//_menu.TurnOff ();
							_menu.TargetInvInstance.Use (iconID);
						}
						else if (_menu.TargetHotspot)
						{
							//_menu.TurnOff ();
							ClickButton (InteractionType.Use, iconID, null, _menu.TargetHotspot);
						}

						if (KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.alwaysCloseInteractionMenus)
						{
							_menu.TurnOff ();
						}
					}
					break;

				default:
					break;
			}
		}


		/**
		 * <summary>Gets the Hotspot that the Player is moving towards.</summary>
		 * <returns>The Hotspot that the Player is moving towards</returns>
		 */
		public Hotspot GetHotspotMovingTo ()
		{
			return hotspotMovingTo;
		}


		/** The Hotspot label while the player is moving towards a Hotspot in order to run an interaction */
		public string MovingToHotspotLabel
		{
			get
			{
				return movingToHotspotLabel;
			}
		}


		/** Cancels the interaction process, that involves the Player prefab moving towards the Hotspot before the Interaction itself is run. */
		public void StopMovingToHotspot ()
		{
			if (KickStarter.player)
			{
				KickStarter.player.EndPath ();
				KickStarter.player.ClearHeadTurnTarget (false, HeadFacing.Hotspot);
			}

			KickStarter.eventManager.Call_OnHotspotStopMovingTo (hotspotMovingTo);

			StopInteraction ();
		}


		protected string GetInteractionLabel (int _language)
		{
			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && !KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return string.Empty;
			}

			if (KickStarter.stateHandler.IsInCutscene ())
			{
				return string.Empty;
			}

			if (hotspot)
			{
				return hotspot.GetFullLabel (_language);
			}
			else
			{
				// No Hotspot

				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (KickStarter.cursorManager.onlyShowInventoryLabelOverHotspots)
					{
						return string.Empty;
					}

					switch (KickStarter.cursorManager.inventoryHandling)
					{
						case InventoryHandling.ChangeHotspotLabel:
						case InventoryHandling.ChangeCursorAndHotspotLabel:
							return KickStarter.runtimeInventory.SelectedInstance.GetHotspotPrefixLabel (_language, true);

						default:
							break;
					}
				}
				else
				{
					// No selected item
					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
					{
						int cursorID = KickStarter.playerCursor.GetSelectedCursorID ();
						if (cursorID >= 0 && !KickStarter.cursorManager.onlyShowCursorLabelOverHotspots)
						{
							return KickStarter.cursorManager.GetLabelFromID (cursorID, _language);
						}
					}

					if (KickStarter.playerCursor.IsInWalkMode () && KickStarter.cursorManager.addWalkPrefix)
					{
						// 'Walk to'
						return KickStarter.runtimeLanguages.GetTranslation (KickStarter.cursorManager.walkPrefix.label, KickStarter.cursorManager.walkPrefix.lineID, _language, KickStarter.cursorManager.walkPrefix.GetTranslationType (0));
					}
				}
			}
			return string.Empty;
		}


		/** Checks if the Player is currently walking to a Hotspot in order to run an Interaction, and doing blocks gameplay. */
		public bool InPreInteractionCutscene
		{
			get
			{
				return inPreInteractionCutscene;
			}
		}


		/** Causes all input to be ignored until the next update loop */
		public void IgnoreInputThisFrame ()
		{
			ignoreInputThisFrame = true;
		}


		/** The internal 'Hotspot label' according to what, if any, Hotspot is currently selected, and the currently-selected icon or inventory item.
			Note that this does not account for 'label overrides', such as when accessing a menu */
		public string InteractionLabel
		{
			get
			{
				return interactionLabel;
			}
		}


		protected virtual bool UnityUIBlocksClick ()
		{
			if (KickStarter.settingsManager.unityUIClicksAlwaysBlocks && KickStarter.playerMenus.EventSystem)
			{
				if (KickStarter.settingsManager.hotspotDetection == HotspotDetection.MouseOver)
				{
					#if !UNITY_EDITOR
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
					{
						if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
						{
							if (KickStarter.playerMenus.EventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
							{
								return true;
							}
						}
						return false;
					}
					#endif

					if (KickStarter.playerMenus.EventSystem.IsPointerOverGameObject ())
					{
						return true;
					}
				}
			}
			return false;
		}


		protected void OnInitialiseScene ()
		{
			StopMovingToHotspot ();
		}


		protected void OnInventoryInteract (InvItem invItem, int iconID)
		{
			OnUseInventory ();
		}


		protected void OnInventoryCombine (InvItem invItem1, InvItem invItem2)
		{
			OnUseInventory ();
		}


		protected void OnEnterGameState (GameState gameState)
		{
			if (gameState == GameState.Normal && hotspot)
			{
				hotspot.Select ();
			}
		}


		protected void OnUseInventory ()
		{
			if (GetHotspotMovingTo ())
			{
				if (KickStarter.settingsManager.inventoryInteractionsHaltPlayer)
				{
					StopMovingToHotspot ();
				}
				else
				{
					StopInteraction ();
				}
			}
			else if (KickStarter.settingsManager.inventoryInteractionsHaltPlayer && KickStarter.player)
			{
				KickStarter.player.EndPath ();
			}
		}


		protected LayerMask hotspotLayerMask;
		protected LayerMask HotspotLayerMask
		{
			set
			{
				hotspotLayerMask = value;
			}
			get
			{
				if (hotspotLayerMask.value == 0)
				{
					hotspotLayerMask = 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
				}
				return hotspotLayerMask;
			}
		}

	}
	
}