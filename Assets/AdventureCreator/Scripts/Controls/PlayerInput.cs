/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerInput.cs"
 * 
 *	This script records all input and processes it for other scripts.
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
	 * This script recieves and processes all input, for use by other scripts.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_input.html")]
	public class PlayerInput : MonoBehaviour
	{

		protected AnimationCurve timeCurve;
		protected float changeTimeStart;

		protected MouseState mouseState = MouseState.Normal;
		protected DragState dragState = DragState.None;

		protected Vector2 moveKeys = new Vector2 (0f, 0f);
		protected bool playerIsControlledRunning = false;

		public float directMovementResponsiveness = 50f;

		/** The game's current Time.timeScale value */
		[HideInInspector] public float timeScale = 1f;
		
		/** If True, Menus can be controlled via the keyboard or controller during gameplay */
		[HideInInspector] public bool canKeyboardControlMenusDuringGameplay = false;
		/** The name of the Input button that skips movies played with ActionMove */
		[HideInInspector] public string skipMovieKey = "";
		/** The minimum duration, in seconds, that can elapse between mouse clicks */
		public float clickDelay = 0.3f;
		/** The maximum duration, in seconds, between two successive mouse clicks to register a "double-click" */
		public float doubleClickDelay = 1f;
		/** The name of the Input Axis that controls dragging effects. If empty, the default inputs (LMB / "InteractionA") will be used */
		public string dragOverrideInput = "";

		public float directMenuThreshold = 0.05f;

		protected float clickTime = 0f;
		protected float doubleClickTime = 0;
		protected MenuDrag activeDragElement;
		protected bool hasUnclickedSinceClick = false;
		protected bool lastClickWasDouble = false;
		protected float lastclickTime = 0f;
		
		// Menu input override
		protected string menuButtonInput;
		protected float menuButtonValue;
		protected SimulateInputType menuInput;
		
		// Controller movement
		private bool cameraLockSnap = false;
		protected Vector2 xboxCursor;
		protected Vector2 mousePosition;
		protected bool scrollingLocked = false;
		protected bool canCycleInteractionInput = true;

		// Touch-Screen movement
		protected Vector2 dragStartPosition = Vector2.zero;
		protected Vector2 dragEndPosition = Vector2.zero;
		protected float dragSpeed = 0f;
		protected Vector2 dragVector;
		protected float touchTime = 0f;
		protected float touchThreshold = 0.2f;
		
		// 1st person movement
		protected Vector2 freeAim;
		protected bool toggleCursorOn = false;
		protected bool cursorIsLocked = false;
		public ForceGameplayCursor forceGameplayCursor = ForceGameplayCursor.None;

		// Draggable
		protected bool canDragMoveable = false;
		protected List<HeldObjectData> heldObjectDatas = new List<HeldObjectData>();
		protected bool pickUpIsHeld;
		protected bool draggableIsHeld;
		protected Vector2 lastMousePosition, unconstrainedMousePosition;
		protected bool resetMouseDelta = false;
		protected Vector3 lastCameraPosition;
		protected Vector2 deltaDragMouse;

		/** The active Conversation */
		public Conversation activeConversation = null;
		protected Conversation pendingOptionConversation = null;
		/** The active ArrowPrompt */
		[HideInInspector] public ArrowPrompt activeArrows = null;
		/** The active Container */
		[HideInInspector] public Container activeContainer = null;
		protected bool mouseIsOnScreen = true;

		// Delegates
		/** A delegate template for overriding input button detection */
		public delegate bool InputButtonDelegate (string buttonName);
		/** A delegate template for overriding input axis detection */
		public delegate float InputAxisDelegate (string axisName);
		/** A delegate template for overriding mouse position detection */
		public delegate Vector2 InputMouseDelegate (bool cusorIsLocked = false);
		/** A delegate template for overriding mouse button detection */
		public delegate bool InputMouseButtonDelegate (int button);
		/** A delegate template for overriding touch position detection */
		public delegate Vector2 InputTouchDelegate (int index);
		/** A delegate template for overriding touch phase */
		public delegate TouchPhase InputTouchPhaseDelegate (int index);
		/** A delegate template for overriding touch count */
		public delegate int _InputTouchCountDelegate ();
		/** A delegate template for overriding the drag state calculation */
		public delegate DragState _InputGetDragStateDelegate (DragState currentDragState);

		/** A delegate for the InputGetButtonDown function, used to detect when a button is first pressed */
		public InputButtonDelegate InputGetButtonDownDelegate = null;
		/** A delegate for the InputGetButtonUp function, used to detect when a button is released */
		public InputButtonDelegate InputGetButtonUpDelegate = null;
		/** A delegate for the InputGetButton function, used to detect when a button is held down */
		public InputButtonDelegate InputGetButtonDelegate = null;
		/** A delegate for the InputGetAxis function, used to detect the value of an input axis */
		public InputAxisDelegate InputGetAxisDelegate = null;
		/** A delegate for the InputGetMouseButton function, used to detect mouse clicks */
		public InputMouseButtonDelegate InputGetMouseButtonDelegate;
		/** A delegate for the InputGetMouseDownButton function, used to detect when a mouse button is first clicked */
		public InputMouseButtonDelegate InputGetMouseButtonDownDelegate;
		/** A delegate for the InputMousePosition function, used to detect the mouse position */
		public InputMouseDelegate InputMousePositionDelegate;
		/** A delegate for the InputTouchPosition function, used to detect the touch position */
		public InputTouchDelegate InputTouchPositionDelegate;
		/** A delegate for the InputTouchPosition function, used to detect the touch deltaPosition */
		public InputTouchDelegate InputTouchDeltaPositionDelegate;
		/** A delegate for the InputTouchPhase function, used to detect a touch index's phase */
		public InputTouchPhaseDelegate InputGetTouchPhaseDelegate;
		/** A delegate for the InputGetFreeAim function, used to get the free-aiming vector */
		public InputMouseDelegate InputGetFreeAimDelegate;
		/** A delegate for _InputTouchCountDelegate, used to get the number of touches */
		public _InputTouchCountDelegate InputTouchCountDelegate;
		/** A delegate for _InputGetDragStateDelegate, used to update the drag state */
		public _InputGetDragStateDelegate InputGetDragStateDelegate;

		protected LerpUtils.Vector2Lerp freeAimLerp = new LerpUtils.Vector2Lerp ();
		private LerpUtils.Vector2Lerp directMoveLerp = new LerpUtils.Vector2Lerp (true);
		private Vector2 lockedCursorPositionOverride;
		private bool overrideLockedCursorPosition;
		private bool resetMouseClickThisFrame;


		private void OnEnable ()
		{
			EventManager.OnGrabMoveable += OnGrabMoveable;
			EventManager.OnDropMoveable += OnDropMoveable;
		}


		private void OnDisable ()
		{
			EventManager.OnGrabMoveable -= OnGrabMoveable;
			EventManager.OnDropMoveable -= OnDropMoveable;
		}


		public void OnInitGameEngine ()
		{
			InitialiseCursorLock (KickStarter.settingsManager.movementMethod);
		
			ResetClick ();
			ResetMouseClick ();

			xboxCursor = LockedCursorPosition;

			if (KickStarter.settingsManager && KickStarter.settingsManager.CanDragCursor ())
			{
				mousePosition = xboxCursor;
			}
		}


		/**
		 * Updates the input handler.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateInput ()
		{
			if (timeCurve != null && timeCurve.length > 0)
			{
				float timeIndex = Time.time - changeTimeStart;
				if (timeCurve [timeCurve.length -1].time < timeIndex)
				{
					SetTimeScale (timeCurve [timeCurve.length -1].value);
					timeCurve = null;
				}
				else
				{
					SetTimeScale (timeCurve.Evaluate (timeIndex));
				}
			}

			if (clickTime > 0f)
			{
				clickTime -= 4f * GetDeltaTime ();
			}
			if (clickTime < 0f)
			{
				clickTime = 0f;
			}

			if (doubleClickTime > 0f)
			{
				doubleClickTime -= 4f * GetDeltaTime ();
			}
			if (doubleClickTime < 0f)
			{
				doubleClickTime = 0f;
			}

			bool isSkippingMovie = false;
			if (!string.IsNullOrEmpty (skipMovieKey) && InputGetButtonDown (skipMovieKey) && KickStarter.stateHandler.gameState != GameState.Paused)
			{
				skipMovieKey = string.Empty;
				isSkippingMovie = true;
			}
			
			if (KickStarter.stateHandler && KickStarter.settingsManager)
			{
				lastMousePosition = unconstrainedMousePosition;

				if (InputGetButtonDown ("ToggleCursor") && KickStarter.stateHandler.IsInGameplay ())
				{
					ToggleCursor ();
				}

				if (KickStarter.stateHandler.gameState == GameState.Cutscene && InputGetButtonDown ("EndCutscene") && !isSkippingMovie)
				{
					KickStarter.actionListManager.EndCutscene ();
				}

				#if UNITY_EDITOR
				if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard || KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				#else
				if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
				#endif
				{
					// Cursor lock state
					if (KickStarter.stateHandler.gameState == GameState.Paused ||
						(KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowGameplayDuringConversations))
					{
						cursorIsLocked = false;
					}
					else if (draggableIsHeld && 
							 KickStarter.settingsManager.IsInFirstPerson () && 
							 KickStarter.settingsManager.disableFreeAimWhenDragging)
					{
						cursorIsLocked = false;
					}
					else if (pickUpIsHeld &&
							 KickStarter.settingsManager.IsInFirstPerson () &&
							 KickStarter.settingsManager.disableFreeAimWhenDraggingPickUp)
					{
						cursorIsLocked = false;
					}
					else
					{
						if (forceGameplayCursor == ForceGameplayCursor.KeepLocked)
						{
							cursorIsLocked = true;
						}
						else if (forceGameplayCursor == ForceGameplayCursor.KeepUnlocked)
						{
							cursorIsLocked = false;
						}
						else
						{
							if (KickStarter.player && KickStarter.player.freeAimLocked && KickStarter.settingsManager.IsInFirstPerson ())
							{
								cursorIsLocked = false;
							}
							else
							{
								cursorIsLocked = toggleCursorOn;
							}
						}
					}

					UnityVersionHandler.CursorLock = cursorIsLocked;

					// Cursor position
					mousePosition = InputMousePosition (cursorIsLocked);
					freeAim = GetSmoothFreeAim (InputGetFreeAim (cursorIsLocked));

					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}

					resetMouseClickThisFrame = false;
					if (InputGetMouseButtonDown (0) || InputGetButtonDown ("InteractionA"))
					{
						if (mouseState == MouseState.Normal)
						{
							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								mouseState = MouseState.SingleClick;
								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (InputGetButtonDown (dragOverrideInput))
					{
						if (KickStarter.stateHandler.IsInGameplay () && mouseState == MouseState.Normal && !CanDoubleClick () && CanClick ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (InputGetMouseButtonDown (1) || InputGetButtonDown ("InteractionB"))
					{
						mouseState = MouseState.RightClick;
					}
					else if (!string.IsNullOrEmpty (dragOverrideInput) && InputGetButton (dragOverrideInput))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (string.IsNullOrEmpty (dragOverrideInput) && (InputGetMouseButton (0) || InputGetButton ("InteractionA")))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else
					{
						if (mouseState == MouseState.HeldDown && dragState == DragState.None && CanClick ())
						{
							mouseState = MouseState.LetGo;
						}
						else if (mouseState != MouseState.Normal)
						{
							ResetMouseClick ();

							if (!CanClick ())
							{
								resetMouseClickThisFrame = true;
							}
						}
					}

					SetDoubleClickState ();
					
					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
					{
						if (InputGetFreeAimDelegate != null)
						{
							freeAim = GetSmoothFreeAim (InputGetFreeAim (dragState == DragState.Player));
						}
						else
						{
							if (dragState == DragState.Player)
							{
								if (KickStarter.settingsManager.IsFirstPersonDragMovement ())
								{
									freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, 0f));
								}
								else
								{
									freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, -dragVector.y * KickStarter.settingsManager.freeAimTouchSpeed));
								}
							}
							else
							{
								freeAim = GetSmoothFreeAim (Vector2.zero);
							}
						}
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				{
					int touchCount = InputTouchCount ();

					// Cursor lock state
					if (forceGameplayCursor == ForceGameplayCursor.KeepLocked)
					{
						cursorIsLocked = true;
					}
					else if (forceGameplayCursor == ForceGameplayCursor.KeepUnlocked)
					{
						cursorIsLocked = false;
					}
					else
					{
						cursorIsLocked = toggleCursorOn;
					}

					// Cursor position
					if (cursorIsLocked)
					{
						mousePosition = LockedCursorPosition;
					}
					else if (touchCount > 0)
					{
						if (KickStarter.settingsManager.CanDragCursor ())
						{
							if (touchTime > touchThreshold)
							{
								if (InputTouchPhase (0) == TouchPhase.Moved && touchCount == 1)
								{
									mousePosition += InputTouchDeltaPosition (0);

									if (mousePosition.x < 0f)
									{
										mousePosition.x = 0f;
									}
									else if (mousePosition.x > ACScreen.width)
									{
										mousePosition.x = ACScreen.width;
									}
									if (mousePosition.y < 0f)
									{
										mousePosition.y = 0f;
									}
									else if (mousePosition.y > ACScreen.height)
									{
										mousePosition.y = ACScreen.height;
									}
								}
							}
						}
						else
						{
							mousePosition = InputTouchPosition (0);
						}
					}

					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}
					
					if (touchTime > 0f && touchTime < touchThreshold)
					{
						dragStartPosition = GetInvertedMouse ();
					}

					resetMouseClickThisFrame = false;
					if ((touchCount == 1 && KickStarter.stateHandler.gameState == GameState.Cutscene && InputTouchPhase (0) == TouchPhase.Began)
						|| (touchCount == 1 && !KickStarter.settingsManager.CanDragCursor () && InputTouchPhase (0) == TouchPhase.Began)
						|| Mathf.Approximately (touchTime, -1f))
					{
						if (mouseState == MouseState.Normal)
						{
							dragStartPosition = GetInvertedMouse (); //

							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								
								mouseState = MouseState.SingleClick;

								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (touchCount == 2 && InputTouchPhase (1) == TouchPhase.Began)
					{
						mouseState = MouseState.RightClick;

						if (KickStarter.settingsManager.IsFirstPersonDragComplex ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (touchCount == 1 && (InputTouchPhase (0) == TouchPhase.Stationary || InputTouchPhase (0) == TouchPhase.Moved))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (touchCount == 2 && (InputTouchPhase (0) == TouchPhase.Stationary || InputTouchPhase (0) == TouchPhase.Moved) && KickStarter.settingsManager.IsFirstPersonDragComplex ())
					{
						mouseState = MouseState.HeldDown;
						SetDragState (true);
					}
					else
					{
						if (mouseState == MouseState.HeldDown && dragState == DragState.None && CanClick ())
						{
							mouseState = MouseState.LetGo;
						}
						else if (mouseState != MouseState.Normal)
						{
							ResetMouseClick ();

							if (!CanClick ())
							{
								resetMouseClickThisFrame = true;
							}
						}
					}

					SetDoubleClickState ();
					
					if (KickStarter.settingsManager.CanDragCursor ())
					{
						if (touchCount > 0)
						{
							touchTime += GetDeltaTime ();
						}
						else
						{
							if (touchTime > 0f && touchTime < touchThreshold)
							{
								touchTime = -1f;
							}
							else
							{
								touchTime = 0f;
							}
						}
					}

					if (InputGetFreeAimDelegate != null)
					{
						freeAim = GetSmoothFreeAim (InputGetFreeAim (dragState == DragState.Player));
					}
					else
					{
						if (dragState == DragState.Player)
						{
							if (KickStarter.settingsManager.IsFirstPersonDragMovement ())
							{
								freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, 0f));
							}
							else
							{
								freeAim = GetSmoothFreeAim (new Vector2 (dragVector.x * KickStarter.settingsManager.freeAimTouchSpeed, -dragVector.y * KickStarter.settingsManager.freeAimTouchSpeed));
							}
						}
						else
						{
							freeAim = GetSmoothFreeAim (Vector2.zero);
						}
					}
				}
				else if (KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
				{
					// Cursor lock
					
					if (draggableIsHeld && 
						KickStarter.settingsManager.IsInFirstPerson () && 
						KickStarter.settingsManager.disableFreeAimWhenDragging)
					{
						cursorIsLocked = false;
					}
					else if (pickUpIsHeld &&
						KickStarter.settingsManager.IsInFirstPerson () &&
						KickStarter.settingsManager.disableFreeAimWhenDraggingPickUp)
					{
						cursorIsLocked = false;
					}
					else if (KickStarter.stateHandler.IsInGameplay ())
					{
						if (forceGameplayCursor == ForceGameplayCursor.KeepLocked)
						{
							cursorIsLocked = true;
						}
						else if (forceGameplayCursor == ForceGameplayCursor.KeepUnlocked)
						{
							cursorIsLocked = false;
						}
						else
						{
							if (KickStarter.player && KickStarter.player.freeAimLocked && KickStarter.settingsManager.IsInFirstPerson ())
							{
								cursorIsLocked = false;
							}
							else
							{
								cursorIsLocked = toggleCursorOn;
							}
						}
					}
					else
					{
						cursorIsLocked = false;
					}

					// Cursor position
					if (cursorIsLocked)
					{
						mousePosition = LockedCursorPosition;
					}
					else
					{
						float speedFactor = (KickStarter.settingsManager.scaleCursorSpeedWithScreen)
											? KickStarter.settingsManager.simulatedCursorMoveSpeed * GetDeltaTime () * KickStarter.mainCamera.PlayableScreenDiagonalLength * 0.5f
											: KickStarter.settingsManager.simulatedCursorMoveSpeed * GetDeltaTime () * 300f;

						xboxCursor.x += InputGetAxis ("CursorHorizontal") * speedFactor;
						xboxCursor.y += InputGetAxis ("CursorVertical") * speedFactor;
					
						xboxCursor.x = Mathf.Clamp (xboxCursor.x, 0f, ACScreen.width);
						xboxCursor.y = Mathf.Clamp (xboxCursor.y, 0f, ACScreen.height);
						
						mousePosition = xboxCursor;
						freeAim = Vector2.zero;
					}

					freeAim = GetSmoothFreeAim (InputGetFreeAim (cursorIsLocked, 50f));
					
					// Cursor state
					if (mouseState == MouseState.Normal)
					{
						dragState = DragState.None;
					}
					
					if (InputGetButtonDown ("InteractionA"))
					{
						if (mouseState == MouseState.Normal)
						{
							if (CanDoubleClick ())
							{
								mouseState = MouseState.DoubleClick;
								ResetClick ();
							}
							else if (CanClick ())
							{
								dragStartPosition = GetInvertedMouse ();
								mouseState = MouseState.SingleClick;

								ResetClick ();
								ResetDoubleClick ();
							}
						}
					}
					else if (InputGetButtonDown (dragOverrideInput))
					{
						if (mouseState == MouseState.Normal && !CanDoubleClick () && CanClick ())
						{
							dragStartPosition = GetInvertedMouse ();
						}
					}
					else if (InputGetButtonDown ("InteractionB"))
					{
						mouseState = MouseState.RightClick;
					}
					else if (!string.IsNullOrEmpty (dragOverrideInput) && InputGetButton (dragOverrideInput))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else if (string.IsNullOrEmpty (dragOverrideInput) && InputGetButton ("InteractionA"))
					{
						mouseState = MouseState.HeldDown;
						SetDragState ();
					}
					else
					{
						ResetMouseClick ();
					}

					SetDoubleClickState ();
				}

				if (KickStarter.playerInteraction.GetHotspotMovingTo ())
				{
					ClearFreeAimInput ();
				}

				if (KickStarter.stateHandler.IsInGameplay ())
				{
					DetectCursorInputs ();
				}

				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot &&
					InputGetButtonDown ("DefaultInteraction") &&
					KickStarter.settingsManager.allowDefaultInventoryInteractions &&
					KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple &&
					KickStarter.settingsManager.CanSelectItems (false) &&
					InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && 
					KickStarter.playerInteraction.GetActiveHotspot () == null)
				{
					KickStarter.runtimeInventory.HoverInstance.RunDefaultInteraction ();
					ResetMouseClick ();
					ResetClick ();
					return;
				}

				if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot && KickStarter.playerMenus.IsInteractionMenuOn ())
				{
					float cycleInteractionsInput = InputGetAxis ("CycleInteractions");

					if (InputGetButtonDown ("CycleInteractionsRight"))
					{
						KickStarter.playerInteraction.SetNextInteraction ();
					}
					else if (InputGetButtonDown ("CycleInteractionsLeft"))
					{
						KickStarter.playerInteraction.SetPreviousInteraction ();
					}

					if (cycleInteractionsInput > 0.1f)
					{
						if (canCycleInteractionInput)
						{
							canCycleInteractionInput = false;
							KickStarter.playerInteraction.SetNextInteraction ();
						}
					}
					else if (cycleInteractionsInput < -0.1f)
					{
						if (canCycleInteractionInput)
						{
							canCycleInteractionInput = false;
							KickStarter.playerInteraction.SetPreviousInteraction ();
						}
					}
					else
					{
						canCycleInteractionInput = true;
					}
				}

				unconstrainedMousePosition = mousePosition;

				if (KickStarter.mainCamera)
				{
					mousePosition = KickStarter.mainCamera.LimitToAspect (mousePosition);
				}

				if (resetMouseDelta)
				{
					lastMousePosition = unconstrainedMousePosition;
					resetMouseDelta = false;
				}

				if (mouseState == MouseState.Normal && !hasUnclickedSinceClick)
				{
					hasUnclickedSinceClick = true;
				}
				
				if (mouseState == MouseState.Normal)
				{
					canDragMoveable = true;
				}
				
				UpdateDrag ();
				
				if (dragState != DragState.None)
				{
					dragVector = GetInvertedMouse () - dragStartPosition;
					dragSpeed = dragVector.magnitude;
				}
				else
				{
					dragVector = Vector2.zero;
					dragSpeed = 0f;
				}

				UpdateActiveInputs ();

				if (mousePosition.x < 0f || mousePosition.x > ACScreen.width || mousePosition.y < 0f || mousePosition.y > ACScreen.height)
				{
					mouseIsOnScreen = false;
				}
				else
				{
					mouseIsOnScreen = true;
				}
			}

			UpdateDragLine ();
		}


		protected void SetDoubleClickState ()
		{
			if (mouseState == MouseState.DoubleClick)
			{
				lastClickWasDouble = true;
			}
			else if (mouseState == MouseState.SingleClick || mouseState == MouseState.RightClick || mouseState == MouseState.LetGo)
			{
				lastClickWasDouble = false;
			}

			if (mouseState == MouseState.DoubleClick || mouseState == MouseState.RightClick || mouseState == MouseState.SingleClick)
			{
				lastclickTime = clickDelay;
			}
			else if (lastclickTime > 0f)
			{
				lastclickTime -= Time.deltaTime;
			}
		}


		/** Resets the free-aim input instantly */
		public void ClearFreeAimInput ()
		{
			freeAim = Vector2.zero;
		}


		/**
		 * <summary>Checks if the Player can be directly-controlled during gameplay.</summary>
		 * <returns>True if the Player can be directly-controlled during gameplay.</returns>
		 */
		public bool CanDirectControlPlayer ()
		{
			if (KickStarter.player)
			{
				return !KickStarter.player.AllDirectionsLocked ();
			}
			return false;
		}


		/**
		 * <summary>Checks if the player clicked within the last few frames. This is useful when checking for input in Actions, because Actions do not run every frame.</summary>
		 * <param name = "checkForDouble">If True, then the check will be made for a double-click, rather than a single-click.</param>
		 * <returns>True if the player recently clicked.</returns>
		 */
		public bool ClickedRecently (bool checkForDouble = false)
		{
			if (lastclickTime > 0f)
			{
				if (checkForDouble == lastClickWasDouble)
				{
					return true;
				}
			}
			return false;
		}


		protected void UpdateActiveInputs ()
		{
			if (KickStarter.settingsManager.activeInputs != null)
			{
				for (int i=0; i<KickStarter.settingsManager.activeInputs.Count; i++)
				{
					bool responded = KickStarter.settingsManager.activeInputs[i].TestForInput ();
					if (responded) return;
				}
			}
		}


		protected void DetectCursorInputs ()
		{
			if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.cursorManager.allowIconInput)
			{
				if (KickStarter.cursorManager.allowWalkCursor)
				{
					if (InputGetButtonDown ("Icon_Walk"))
					{
						KickStarter.runtimeInventory.SetNull ();
						KickStarter.playerCursor.ResetSelectedCursor ();
						return;
					}
				}

				foreach (CursorIcon icon in KickStarter.cursorManager.cursorIcons)
				{
					if (InputGetButtonDown (icon.GetButtonName ()))
					{
						KickStarter.runtimeInventory.SetNull ();
						KickStarter.playerCursor.SetCursor (icon);
						return;
					}
				}
			}
		}


		/**
		 * <summary>Gets the cursor's position in screen space.</summary>
		 * <returns>The cursor's position in screen space</returns>
		 */
		public Vector2 GetMousePosition ()
		{
			return mousePosition;
		}


		/**
		 * <summary>Gets the y-inverted cursor position. This is useful because Menu Rects are drawn upwards, while screen space is measured downwards.</summary>
		 * <returns>Gets the y-inverted cursor position. This is useful because Menu Rects are drawn upwards, while screen space is measured downwards.</returns>
		 */
		public Vector2 GetInvertedMouse ()
		{
			return new Vector2 (GetMousePosition ().x, ACScreen.height - GetMousePosition ().y);
		}


		/**
		 * <summary>Sets the position of the simulated cursor, which is used when the game's Input method is set to Keyboard Or Controller</summary>
		 * <param name = "newPosition">The position, in screen-space co-ordinates, to move the simulated cursor to<param>
		 */
		public void SetSimulatedCursorPosition (Vector2 newPosition)
		{
			xboxCursor = newPosition;

			if (!cursorIsLocked)
			{
				mousePosition = xboxCursor;
			}
		}


		/**
		 * <summary>Initialises the cursor lock based on a given movement method.</summary>
		 * <param name = "movementMethod">The new movement method</param>
		 */
		public void InitialiseCursorLock (MovementMethod movementMethod)
		{
			if (KickStarter.settingsManager.IsInFirstPerson () && movementMethod != MovementMethod.FirstPerson)
			{
				toggleCursorOn = false;
			}
			else
			{
				toggleCursorOn = KickStarter.settingsManager.lockCursorOnStart;

				if (toggleCursorOn && !KickStarter.settingsManager.IsInFirstPerson () && KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && KickStarter.settingsManager.hotspotDetection == HotspotDetection.MouseOver)
				{
					ACDebug.Log ("Starting a non-First Person game with a locked cursor - is this correct?"); 
				}
			}
		}


		/**
		 * <summary>Checks if the cursor's position can be read. This is only ever False if the cursor cannot be dragged on a touch-screen.</summary>
		 * <returns>True if the cursor's position can be read</returns>
		 */
		public bool IsCursorReadable ()
		{
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				if (mouseState == MouseState.Normal)
				{
					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.InventoryDragDrop)
					{
						return true;
					}
					return KickStarter.settingsManager.CanDragCursor ();
				}
			}
			return true;
		}


		/**
		 * Detects the pressing of the numeric keys if they can be used to trigger a Conversation's dialogue options.
		 */
		public void DetectConversationNumerics ()
		{		
			if (activeConversation && KickStarter.settingsManager.runConversationsWithKeys)
			{
				int offset = 0;
				if (activeConversation.LinkedDialogList && activeConversation.LinkedDialogList.elementSlotMapping == ElementSlotMapping.List && activeConversation.LinkedDialogList.indexPrefixDisplay != IndexPrefixDisplay.GlobalOrder)
				{
					offset = activeConversation.LinkedDialogList.GetOffset ();
				}

				Event e = Event.current;
				if (e.isKey && e.type == EventType.KeyDown)
				{
					if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1)
					{
						activeConversation.RunOption (0 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2)
					{
						activeConversation.RunOption (1 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3)
					{
						activeConversation.RunOption (2 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha4 || e.keyCode == KeyCode.Keypad4)
					{
						activeConversation.RunOption (3 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha5 || e.keyCode == KeyCode.Keypad5)
					{
						activeConversation.RunOption (4 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha6 || e.keyCode == KeyCode.Keypad6)
					{
						activeConversation.RunOption (5 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha7 || e.keyCode == KeyCode.Keypad7)
					{
						activeConversation.RunOption (6 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha8 || e.keyCode == KeyCode.Keypad8)
					{
						activeConversation.RunOption (7 + offset);
						return;
					}
					else if (e.keyCode == KeyCode.Alpha9 || e.keyCode == KeyCode.Keypad9)
					{
						activeConversation.RunOption (8 + offset);
						return;
					}
				}
			}
		}


		/**
		 * Detects the pressing of the defined input buttons if they can be used to trigger a Conversation's dialogue options.
		 */
		public void DetectConversationInputs ()
		{		
			if (activeConversation && KickStarter.settingsManager.runConversationsWithKeys)
			{
				int offset = 0;
				if (activeConversation.LinkedDialogList && activeConversation.LinkedDialogList.elementSlotMapping == ElementSlotMapping.List && activeConversation.LinkedDialogList.indexPrefixDisplay != IndexPrefixDisplay.GlobalOrder)
				{
					offset = activeConversation.LinkedDialogList.GetOffset ();
				}

				if (InputGetButtonDown ("DialogueOption1"))
				{
					activeConversation.RunOption (0 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption2"))
				{
					activeConversation.RunOption (1 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption3"))
				{
					activeConversation.RunOption (2 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption4"))
				{
					activeConversation.RunOption (3 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption5"))
				{
					activeConversation.RunOption (4 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption6"))
				{
					activeConversation.RunOption (5 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption7"))
				{
					activeConversation.RunOption (6 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption8"))
				{
					activeConversation.RunOption (7 + offset);
				}
				else if (InputGetButtonDown ("DialogueOption9"))
				{
					activeConversation.RunOption (8 + offset);
				}
			}
			
		}
		
		
		/**
		 * Draws a drag-line on screen if the chosen movement method allows for one.
		 */
		public void DrawDragLine ()
		{
			if (KickStarter.settingsManager.drawDragLine && dragEndPosition != Vector2.zero)
			{
				DrawStraightLine.Draw (dragStartPosition, dragEndPosition, KickStarter.settingsManager.dragLineColor, KickStarter.settingsManager.dragLineWidth, true);
			}
		}


		protected void UpdateDragLine ()
		{
			dragEndPosition = Vector2.zero;

			if (dragState == DragState.Player && KickStarter.settingsManager.movementMethod != MovementMethod.StraightToCursor)
			{
				dragEndPosition = GetInvertedMouse ();
				KickStarter.eventManager.Call_OnUpdateDragLine (dragStartPosition, dragEndPosition);
			}
			else
			{
				KickStarter.eventManager.Call_OnUpdateDragLine (Vector2.zero, Vector2.zero);
			}

			if (activeDragElement != null)
			{
				if (mouseState == MouseState.HeldDown)
				{
					if (!activeDragElement.DoDrag (GetDragVector ()))
					{
						activeDragElement = null;
					}
				}
				else if (mouseState == MouseState.Normal)
				{
					if (activeDragElement.CheckStop (GetInvertedMouse ()))
					{
						activeDragElement = null;
					}
				}
			}
		}


		/**
		 * Updates the input variables needed for Direct movement.
		 * This is called every frame by StateHandler.
		 */
		public void UpdateDirectInput (bool isInGameplay)
		{
			if (!isInGameplay)
			{
				moveKeys = Vector2.zero;
				return;
			}

			if (activeArrows)
			{
				if (activeArrows.arrowPromptType == ArrowPromptType.KeyOnly || activeArrows.arrowPromptType == ArrowPromptType.KeyAndClick)
				{
					Vector2 normalizedVector = new Vector2 (InputGetAxis ("Horizontal"), -InputGetAxis ("Vertical"));

					if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && dragState == DragState.ScreenArrows)
					{
						normalizedVector = GetDragVector () / KickStarter.settingsManager.dragRunThreshold / KickStarter.settingsManager.dragWalkThreshold;
					}

					if (normalizedVector.sqrMagnitude > 0f)
					{
						float threshold = 0.95f;
						if (KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
						{
							threshold = 0.05f;
						}

						if (normalizedVector.x > threshold)
						{
							activeArrows.DoRight ();
						}
						else if (normalizedVector.x < -threshold)
						{
							activeArrows.DoLeft ();
						}
						else if (normalizedVector.y < -threshold)
						{
							activeArrows.DoUp();
						}
						else if (normalizedVector.y > threshold)
						{
							activeArrows.DoDown ();
						}
					}
				}
					
				if (activeArrows && (activeArrows.arrowPromptType == ArrowPromptType.ClickOnly || activeArrows.arrowPromptType == ArrowPromptType.KeyAndClick))
				{
					// Arrow Prompt is displayed: respond to mouse clicks
					Vector2 invertedMouse = GetInvertedMouse ();
					if (mouseState == MouseState.SingleClick)
					{
						if (activeArrows.upArrow.rect.Contains (invertedMouse))
						{
							activeArrows.DoUp ();
						}
							
						else if (activeArrows.downArrow.rect.Contains (invertedMouse))
						{
							activeArrows.DoDown ();
						}
							
						else if (activeArrows.leftArrow.rect.Contains (invertedMouse))
						{
							activeArrows.DoLeft ();
						}
							
						else if (activeArrows.rightArrow.rect.Contains (invertedMouse))
						{
							activeArrows.DoRight ();
						}
					}
				}
			}
				
			if (activeArrows == null && KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick)
			{
				float h = 0f;
				float v = 0f;
				bool run;
					
				if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
				{
					if (KickStarter.settingsManager.IsInFirstPerson () && KickStarter.settingsManager.firstPersonTouchScreen == FirstPersonTouchScreen.CustomInput)
					{
						h = InputGetAxisRaw ("Horizontal");
						v = InputGetAxisRaw ("Vertical");
					}
					else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.directTouchScreen == DirectTouchScreen.CustomInput)
					{
						h = InputGetAxisRaw ("Horizontal");
						v = InputGetAxisRaw ("Vertical");
					}
					else if (dragState != DragState.None)
					{
						h = dragVector.x;
						v = -dragVector.y;
					}
				}
				else
				{
					h = InputGetAxisRaw ("Horizontal");
					v = InputGetAxisRaw ("Vertical");
				}

				if (KickStarter.player)
				{
					if ((KickStarter.player.upMovementLocked && v > 0f) || (KickStarter.player.downMovementLocked && v < 0f))
					{
						v = 0f;
					}
					
					if ((KickStarter.player.leftMovementLocked && h > 0f) || (KickStarter.player.rightMovementLocked && h < 0f))
					{
						h = 0f;
					}

					bool customTouchInput = KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && 
						((KickStarter.settingsManager.movementMethod != MovementMethod.FirstPerson && KickStarter.settingsManager.directTouchScreen == DirectTouchScreen.CustomInput) ||
						(KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && KickStarter.settingsManager.firstPersonTouchScreen == FirstPersonTouchScreen.CustomInput));
				
					switch (KickStarter.player.runningLocked)
					{
						case PlayerMoveLock.Free:
							if (!customTouchInput && (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || KickStarter.settingsManager.movementMethod == MovementMethod.Drag))
							{
								if (dragStartPosition != Vector2.zero && dragSpeed > KickStarter.settingsManager.dragRunThreshold * 10f)
								{
									run = true;
								}
								else
								{
									run = false;
								}
							}
							else
							{
								if (InputGetAxis ("Run") > 0.1f)
								{
									run = true;
								}
								else
								{
									run = InputGetButton ("Run");
								}

								if (InputGetButtonDown ("ToggleRun") && KickStarter.player)
								{
									KickStarter.player.toggleRun = !KickStarter.player.toggleRun;
								}
							}
							break;

						case PlayerMoveLock.AlwaysWalk:
							run = false;
							break;

						default:
							run = true;
							break;
					}
				
					if (KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen && (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson || KickStarter.settingsManager.movementMethod == MovementMethod.Direct) && KickStarter.player.runningLocked == PlayerMoveLock.Free && KickStarter.player.toggleRun)
					{
						playerIsControlledRunning = !run;
					}
					else
					{
						playerIsControlledRunning = run;
					}
				}

				moveKeys = CreateMoveKeys (h, v);
			}

			if (InputGetButtonDown ("FlashHotspots"))
			{
				FlashHotspots ();
			}
		}


		protected Vector2 CreateMoveKeys (float h, float v)
		{
			if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen && KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera)
			{
				switch (KickStarter.settingsManager.limitDirectMovement)
				{
					case LimitDirectMovement.NoLimit:
					default:
						break;

					case LimitDirectMovement.FourDirections:
						if (Mathf.Abs (h) > Mathf.Abs (v))
						{
							v = 0f;
						}
						else
						{
							h = 0f;
						}
						break;

					case LimitDirectMovement.EightDirections:
						float signedAngle = Vector2.SignedAngle (Vector2.up, new Vector2 (h, v));
						h = Mathf.Abs (h);
						v = Mathf.Abs (v);

						if (signedAngle < 0f) signedAngle += 360f;

						if (signedAngle < 45f || signedAngle > 337.5f)
						{
							// UP
							h = 0f;
						}
						else if (signedAngle < 67.5f)
						{
							// UP LEFT
							v = h;
							h = -h;
						}
						else if (signedAngle < 112.5f)
						{
							// LEFT
							v = 0f;
							h = -h;
						}
						else if (signedAngle < 157.5f)
						{
							// DOWN LEFT
							v = -h;
							h = -h;
						}
						else if (signedAngle < 202.5f)
						{
							// DOWN
							h = 0f;
							v = -v;
						}
						else if (signedAngle < 247.5f)
						{
							// DOWN RIGHT
							v = -h;
						}
						else if (signedAngle < 292.5f)
						{
							// RIGHT
							v = 0f;
						}
						else
						{
							// UP RIGHT
							v = h;
						}
						break;
				}
			}

			if (cameraLockSnap)
			{
				Vector2 newMoveKeys = new Vector2 (h, v);
				if (newMoveKeys.sqrMagnitude < 0.01f || Vector2.Angle (newMoveKeys, moveKeys) > KickStarter.settingsManager.cameraLockSnapAngleThreshold)
				{
					cameraLockSnap = false;
					return newMoveKeys;
				}
				return moveKeys;
			}

			if (directMovementResponsiveness > 0f)
			{
				return directMoveLerp.Update (moveKeys, new Vector2 (h, v), directMovementResponsiveness);
			}
			return new Vector2 (h, v);
		}


		public void BeginCameraLockSnap ()
		{
			if (Application.isPlaying && !SceneSettings.IsUnity2D () && KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera)
			{
				if (KickStarter.settingsManager.cameraLockSnapAngleThreshold > 0f && KickStarter.player && !KickStarter.player.IsTurning (90f) &&
					(KickStarter.player.GetPath () == null || !KickStarter.player.IsLockedToPath ()))
				{
					cameraLockSnap = true;
				}
			}
		}


		/** If True, and Direct movement is used to control the Player, then the Player will not change direction. This is to avoid the Player moving in unwanted directions when the camera cuts. */
		public bool IsCameraLockSnapped ()
		{
			return cameraLockSnap;
		}


		protected virtual void FlashHotspots ()
		{
			foreach (Hotspot hotspot in KickStarter.stateHandler.Hotspots)
			{
				if (hotspot.highlight)
				{
					if (hotspot.IsOn () && hotspot.PlayerIsWithinBoundary () && hotspot != KickStarter.playerInteraction.GetActiveHotspot ())
					{
						hotspot.Flash ();
					}
				}
			}
		}
		

		/** Disables the active ArrowPrompt. */
		public void RemoveActiveArrows ()
		{
			if (activeArrows)
			{
				activeArrows.TurnOff ();
			}
		}
		

		/** Records the current click time, so that another click will not register for the duration of clickDelay. */
		public void ResetClick ()
		{
			clickTime = clickDelay;
			hasUnclickedSinceClick = false;
		}
		
		
		protected void ResetDoubleClick ()
		{
			doubleClickTime = doubleClickDelay;
		}
		

		/**
		 * <summary>Checks if a mouse click will be registered.</summary>
		 * <returns>True if a mouse click will be registered</returns>
		 */
		public bool CanClick ()
		{
			if (clickTime <= 0f)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * <summary>Checks if a mouse double-click will be registered.</summary>
		 * <returns>True if a mouse double-click will be registered</returns>
		 */
		public bool CanDoubleClick ()
		{
			if (doubleClickTime > 0f && clickTime <= 0f)
			{
				return true;
			}
			
			return false;
		}


		/**
		 * <summary>Simulates the pressing of an Input button.</summary>
		 * <param name = "button">The name of the Input button</param>
		 */
		public void SimulateInputButton (string button)
		{
			SimulateInput (SimulateInputType.Button, button, 1f);
		}
		

		/**
		 * <summary>Simulates the pressing of an Input axis.</summary>
		 * <param name = "axis">The name of the Input axis</param>
		 * <param name = "value">The value to assign the Input axis</param>
		 */
		public void SimulateInputAxis (string axis, float value)
		{
			SimulateInput (SimulateInputType.Axis, axis, value);
		}


		/**
		 * <summary>Simulates the pressing of an Input button or axis.</summary>
		 * <param name = "input">The type of Input this is simulating (Button, Axis)</param>
		 * <param name = "axis">The name of the Input button or axis</param>
		 * <param name = "value">The value to assign the Input axis, if input = SimulateInputType.Axis</param>
		 */
		public void SimulateInput (SimulateInputType input, string axis, float value)
		{
			if (!string.IsNullOrEmpty (axis))
			{
				menuInput = input;
				menuButtonInput = axis;
				
				if (input == SimulateInputType.Button)
				{
					menuButtonValue = 1f;
				}
				else
				{
					menuButtonValue = value;
				}

				CancelInvoke ();
				Invoke ("StopSimulatingInput", 0.1f);
			}
		}


		/**
		 * <summary>Checks if the cursor is locked.</summary>
		 * <returns>True if the cursor is locked</returns>
		 */
		public bool IsCursorLocked ()
		{
			return cursorIsLocked;
		}


		protected void StopSimulatingInput ()
		{
			menuButtonInput = string.Empty;
		}


		/**
		 * <summary>Checks if any input button is currently being pressed, simulated or otherwise.</summary>
		 * <returns>True if any input button is currently being pressed, simulated or otherwise.</returns>
		 */
		public bool InputAnyKey ()
		{
			if (menuButtonInput != null && !string.IsNullOrEmpty (menuButtonInput))
			{
				return true;
			}
			return Input.anyKey;
		}


		protected float InputGetAxisRaw (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return 0f;
			}

			if (InputGetAxisDelegate != null)
			{
				return InputGetAxisDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (!Mathf.Approximately (Input.GetAxisRaw (axis), 0f))
				{
					return Input.GetAxisRaw (axis);
				}
			}
			else
			{
				try
				{
					if (!Mathf.Approximately (Input.GetAxisRaw (axis), 0f))
					{
						return Input.GetAxisRaw (axis);
					}
				}
				catch {}
			}
			
			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Axis)
			{
				return menuButtonValue;
			}
			
			return 0f;
		}
		

		/**
		 * <summary>Replaces "Input.GetAxis", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input axis to detect</param>
		 * <returns>The Input axis' value</returns>
		 */
		public float InputGetAxis (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return 0f;
			}

			if (InputGetAxisDelegate != null)
			{
				return InputGetAxisDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (!Mathf.Approximately (Input.GetAxis (axis), 0f))
				{
					return Input.GetAxis (axis);
				}
			}
			else
			{
				try
				{
					if (!Mathf.Approximately (Input.GetAxis (axis), 0f))
					{
						return Input.GetAxis (axis);
					}
				}
				catch {}
			}

			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Axis)
			{
				return menuButtonValue;
			}
			
			return 0f;
		}
		
		
		protected bool InputGetMouseButton (int button)
		{
			if (InputGetMouseButtonDelegate != null)
			{
				return InputGetMouseButtonDelegate (button);
			}

			if (KickStarter.settingsManager.inputMethod != InputMethod.MouseAndKeyboard || KickStarter.settingsManager.defaultMouseClicks)
			{
				return Input.GetMouseButton (button);
			}
			return false;
		}
		
		
		protected Vector2 InputMousePosition (bool _cursorIsLocked)
		{
			if (InputMousePositionDelegate != null)
			{
				return InputMousePositionDelegate (_cursorIsLocked);
			}

			if (_cursorIsLocked)
			{
				return LockedCursorPosition;
			}
			return Input.mousePosition;
		}


		protected Vector2 InputTouchPosition (int index)
		{
			if (InputTouchPositionDelegate != null)
			{
				return InputTouchPositionDelegate (index);
			}

			if (InputTouchCount () > index)
			{
				return Input.GetTouch (index).position;
			}
			return Vector2.zero;			
		}


		protected Vector2 InputTouchDeltaPosition (int index)
		{
			if (InputTouchPositionDelegate != null)
			{
				return InputTouchDeltaPositionDelegate (index);
			}

			if (InputTouchCount () > index)
			{
				Touch t = Input.GetTouch (0);
				if (KickStarter.stateHandler.gameState == GameState.Paused)
				{
					return t.deltaPosition * 1.7f;
				}
				return t.deltaPosition * Time.deltaTime / t.deltaTime;
			}
			return Vector2.zero;
		}


		protected TouchPhase InputTouchPhase (int index)
		{
			if (InputGetTouchPhaseDelegate != null)
			{
				return InputGetTouchPhaseDelegate (index);
			}

			return Input.GetTouch (index).phase;
		}


		protected int InputTouchCount ()
		{
			if (InputTouchCountDelegate != null)
			{
				return InputTouchCountDelegate ();
			}

			return Input.touchCount;
		}


		protected Vector2 InputGetFreeAim (bool _cursorIsLocked, float scaleFactor = 1f)
		{
			if (InputGetFreeAimDelegate != null)
			{
				return InputGetFreeAimDelegate (_cursorIsLocked);
			}

			if (_cursorIsLocked)
			{
				return new Vector2 (InputGetAxis ("CursorHorizontal") * scaleFactor, InputGetAxis ("CursorVertical") * scaleFactor);
			}
			return Vector2.zero;
		}
		
		
		protected bool InputGetMouseButtonDown (int button)
		{
			if (InputGetMouseButtonDownDelegate != null)
			{
				return InputGetMouseButtonDownDelegate (button);
			}

			if (KickStarter.settingsManager.inputMethod != InputMethod.MouseAndKeyboard || KickStarter.settingsManager.defaultMouseClicks)
			{
				return Input.GetMouseButtonDown (button);
			}
			return false;
		}
		

		/**
		 * <summary>Replaces "Input.GetButton", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <returns>True if the Input button is being held down this frame</returns>
		 */
		public bool InputGetButton (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return false;
			}

			if (InputGetButtonDelegate != null)
			{
				return InputGetButtonDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButton (axis))
				{
					return true;
				}
			}
			else
			{
				try
				{
					if (Input.GetButton (axis))
					{
						return true;
					}
				}
				catch {}
			}

			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Button)
			{
				if (menuButtonValue > 0f)
				{
					//ResetClick ();
					StopSimulatingInput ();	
					return true;
				}
				
				StopSimulatingInput ();
			}

			return false;
		}
		

		/**
		 * <summary>Replaces "Input.GetButton", allowing for custom overrides.</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <param name = "showError">If True, then an error message will appear in the Console window if the button is not defined in the Input manager</param>
		 * <returns>True if the Input button was first pressed down this frame</returns>
		 */
		public bool InputGetButtonDown (string axis, bool showError = false)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return false;
			}

			if (InputGetButtonDownDelegate != null)
			{
				return InputGetButtonDownDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButtonDown (axis))
				{
					return true;
				}
			}
			else
			{
				try
				{
					if (Input.GetButtonDown (axis))
					{
						return true;
					}
				}
				catch
				{
					if (showError)
					{
						ACDebug.LogWarning ("Cannot find Input button '" + axis + "' - please define it in Unity's Input Manager (Edit -> Project settings -> Input).");
					}
				}
			}

			if (!string.IsNullOrEmpty (menuButtonInput) && menuButtonInput == axis && menuInput == SimulateInputType.Button)
			{
				if (menuButtonValue > 0f)
				{
					//ResetClick ();
					StopSimulatingInput ();	
					return true;
				}
				
				StopSimulatingInput ();
			}
			
			return false;
		}


		/**
		 * <summary>Replaces "Input.GetButtonUp".</summary>
		 * <param name = "axis">The Input button to detect</param>
		 * <returns>True if the Input button is released</returns>
		 */
		public bool InputGetButtonUp (string axis)
		{
			if (string.IsNullOrEmpty (axis))
			{
				return false;
			}

			if (InputGetButtonUpDelegate != null)
			{
				return InputGetButtonUpDelegate (axis);
			}

			if (KickStarter.settingsManager.assumeInputsDefined)
			{
				if (Input.GetButtonUp (axis))
				{
					return true;
				}
			}
			else
			{
				try
				{
					if (Input.GetButtonUp (axis))
					{
						return true;
					}
				}
				catch {}
			}
			return false;
		}


		public void EnforcePreInventoryDragState ()
		{
			if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) &&
				KickStarter.settingsManager.InventoryDragDrop &&
				KickStarter.settingsManager.dragThreshold > 0 &&
				(KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
			{
				dragState = DragState.PreInventory;
				dragStartPosition = GetInvertedMouse ();
			}
		}


		protected void SetDragState (bool twoTouches = false)
		{
			DragState oldDragState = dragState;

			if (InputGetDragStateDelegate != null)
			{
				dragState = InputGetDragStateDelegate (oldDragState);
			}
			else if (twoTouches)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.InventoryDragDrop && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
				{ }
				else if (activeDragElement != null && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
				{ }
				else if (activeArrows && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				{ }
				else if (IsDragObjectHeld ())
				{ }
				else if (KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled)
				{ }
				else if ((KickStarter.settingsManager.movementMethod == MovementMethod.Drag || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor ||
						  (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen))
						  && KickStarter.settingsManager.movementMethod != MovementMethod.None && KickStarter.stateHandler.IsInGameplay ())
				{
					if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn ())
					{
						if (KickStarter.playerInteraction.IsMouseOverHotspot ())
						{ }
						else
						{
							dragState = DragState.Player;
						}
					}
				}
				else
				{
					dragState = DragState.None;
				}
			}
			else
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && KickStarter.settingsManager.InventoryDragDrop && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
				{
					if (dragVector.magnitude / ACScreen.LongestDimension >= KickStarter.settingsManager.dragThreshold)
					{
						dragState = DragState.Inventory;
					}
					else if (dragState != DragState.Inventory)
					{
						dragState = DragState.PreInventory;
					}
				}
				else if (activeDragElement != null && (KickStarter.stateHandler.IsInGameplay () || KickStarter.stateHandler.gameState == GameState.Paused))
				{
					dragState = DragState.Menu;
				}
				else if (activeArrows && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				{
					dragState = DragState.ScreenArrows;
				}
				else if (IsDragObjectHeld ())
				{
					if (dragState == DragState.None && !cursorIsLocked && (deltaDragMouse.magnitude * Time.deltaTime <= 1f) && (GetInvertedMouse () - dragStartPosition).magnitude / ACScreen.LongestDimension < KickStarter.settingsManager.dragThreshold)
					{
						return;
					}

					dragState = DragState.Moveable;
				}
				else if (KickStarter.mainCamera && KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera.attachedCamera.isDragControlled && !KickStarter.stateHandler.AreCamerasDisabled ())
				{
					if (dragState == DragState.Moveable)
					{
						return;
					}

					if (!KickStarter.playerInteraction.IsMouseOverHotspot () ||
						!KickStarter.stateHandler.IsInGameplay () ||
						(KickStarter.playerInteraction.GetActiveHotspot () && 
							(KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive || 
							(KickStarter.playerInteraction.GetActiveHotspot ().IsSingleInteraction () && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction))))
					{
						if (dragState == DragState.None && !cursorIsLocked && (deltaDragMouse.magnitude * Time.deltaTime <= 1f) && (GetInvertedMouse () - dragStartPosition).magnitude / ACScreen.LongestDimension < KickStarter.settingsManager.dragThreshold)
						{
							return;
						}

						dragState = DragState._Camera;
					}
				}
				else if (KickStarter.settingsManager.CanDragPlayer && KickStarter.stateHandler.IsInGameplay () && !KickStarter.stateHandler.MovementIsOff)
				{
					if (!KickStarter.playerMenus.IsMouseOverMenu ())
					{
						if (KickStarter.playerInteraction.IsMouseOverHotspot ())
						{}
						else
						{
							dragState = DragState.Player;
						}
					}
				}
				else
				{
					dragState = DragState.None;
				}
			}

			if (oldDragState == DragState.None && dragState != DragState.None)
			{
				resetMouseDelta = true;
				lastMousePosition = unconstrainedMousePosition;
			}
		}


		protected void UpdateDrag ()
		{
			if (dragState != DragState.None)
			{
				// Calculate change in mouse position
				if (freeAim.sqrMagnitude > 0f)
				{
					deltaDragMouse = freeAim * 500f / Time.deltaTime;
				}
				else
				{
					deltaDragMouse = (unconstrainedMousePosition - lastMousePosition) / Time.deltaTime;
				}
			}

			bool isInGameplay = KickStarter.stateHandler.IsInGameplay ();
			
			if (mouseState == MouseState.HeldDown && dragState == DragState.None && KickStarter.stateHandler.CanInteractWithDraggables () && !KickStarter.playerMenus.IsMouseOverMenu () && isInGameplay)
			{
				AttemptGrab ();
			}
			else
			{
				for (int i=0; i<heldObjectDatas.Count; i++)
				{
					heldObjectDatas[i].AttemptRelease (!isInGameplay);
				}
			}
		}


		public void _FixedUpdate ()
		{
			if (KickStarter.CameraMainTransform)
			{
				Vector3 cameraPosition = KickStarter.CameraMainTransform.position;

				Vector3 deltaCamera = cameraPosition - lastCameraPosition;

				foreach (HeldObjectData heldObjectData in heldObjectDatas)
				{
					if (!heldObjectData.IgnoreBuiltInDragInput)
					{
						heldObjectData.Drag (deltaCamera, deltaDragMouse, unconstrainedMousePosition);
					}
				}

				lastCameraPosition = cameraPosition;
			}
		}


		/** Forces the letting-go of the currently-held DragBase, if set. */
		public void LetGo ()
		{
			for (int i=0; i<heldObjectDatas.Count; i++)
			{
				heldObjectDatas[i].DragObject.LetGo ();
			}
		}
		
		
		protected void AttemptGrab ()
		{
			if (heldObjectDatas.Count > 0)
			{
				for (int i=0; i<heldObjectDatas.Count; i++)
				{
					heldObjectDatas[i].DragObject.LetGo ();
				}
				return;
			}

			if (canDragMoveable)
			{
				canDragMoveable = false;
				
				Ray ray = KickStarter.CameraMain.ScreenPointToRay (mousePosition); 
				RaycastHit hit = new RaycastHit ();
				
				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.moveableRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer)))
				{
					Hotspot hotspot = hit.collider.GetComponent<Hotspot> ();
					if (hotspot)
					{
						Button button = hotspot.GetFirstUseButton ();
						if (button != null && 
							((hotspot.interactionSource == InteractionSource.InScene && button.interaction) || (hotspot.interactionSource == InteractionSource.AssetFile && button.assetFile)))
						{
							return;
						}
					}

					DragBase dragBase = hit.collider.GetComponent <DragBase>();
					if (dragBase == null || !dragBase.CanGrab ())
					{
						dragBase = hit.transform.GetComponent <DragBase>();
					}
					if (dragBase && dragBase.CanGrab ())
					{
						dragBase.Grab (hit.point);
						lastCameraPosition = KickStarter.CameraMainTransform.position;
					}
				}
			}
		}


		/**
		 * <summary>Gets the data related to the holding of a draggable object</summary>
		 * <param name="dragBase">The draggable object to get data for</param>
		 * <returns>Data related to the holding of the draggable object</returns>
		 */
		public HeldObjectData GetHeldObjectData (DragBase dragBase)
		{
			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (heldObjectData.DragObject == dragBase)
				{
					return heldObjectData;
				}
			}
			return null;
		}


		/** Gets all data related to the draggable objects currently being held. */
		public HeldObjectData[] GetHeldObjectData ()
		{
			return heldObjectDatas.ToArray ();
		}


		protected void OnGrabMoveable (DragBase dragBase)
		{
			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (heldObjectData.DragObject == dragBase)
				{
					return;
				}
			}

			heldObjectDatas.Add (new HeldObjectData (dragBase));

			if (dragBase is Moveable_PickUp)
			{
				pickUpIsHeld = true;
			}
			else if (dragBase is Moveable_Drag)
			{
				draggableIsHeld = true;
			}
		}


		protected void OnDropMoveable (DragBase dragBase)
		{
			if (dragBase == null) return;

			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (heldObjectData.DragObject == dragBase)
				{
					heldObjectDatas.Remove (heldObjectData);
					break;
				}
			}

			pickUpIsHeld = false;
			draggableIsHeld = false;
			
			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (heldObjectData.DragObject is Moveable_PickUp)
				{
					pickUpIsHeld = true;
				}
				else if (heldObjectData.DragObject is Moveable_Drag)
				{
					draggableIsHeld = true;
				}
			}
		}


		/**
		 * <summary>Gets the drag vector.</summary>
		 * <returns>The drag vector</returns>
		 */
		public Vector2 GetDragVector ()
		{
			if (dragState == AC.DragState._Camera)
			{
				return deltaDragMouse;
			}
			return dragVector;
		}


		/**
		 * <summary>Checks if the active ArrowPrompt prevents Hotspots from being interactive.</summary>
		 * <returns>True if the active ArrowPrompt prevents Hotspots from being interactive</returns>
		 */
		public bool ActiveArrowsDisablingHotspots ()
		{
			if (activeArrows && activeArrows.disableHotspots)
			{
				return true;
			}
			return false;
		}
		

		protected void ToggleCursor ()
		{
			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (!heldObjectData.DragObject.CanToggleCursor ())
				{
					return;
				}
			}
			toggleCursorOn = !toggleCursorOn;
		}


		/**
		 * <summary>Sets the lock state of the in-game cursor manually. When locked, the cursor will be placed in the centre of the screen during gameplay.</summary>
		 * <param name = "lockState">If True, the cursor will be locked during gameplay</param>
		 */
		public void SetInGameCursorState (bool lockState)
		{
			toggleCursorOn = lockState;

			if (lockState)
			{
				freeAimLerp.Update (Vector2.zero, Vector2.zero, 0f);
			}
		}


		/**
		 * <summary>Gets the locked state of the cursor during gameplay (i.e. when the game is not paused).</summary>
		 * <returns>True if the in-game cursor is locked in the centre of the screen</returns>
		 */
		public bool GetInGameCursorState ()
		{
			return toggleCursorOn;
		}
		

		/**
		 * <summary>Checks if a specific DragBase object is being held by the player.</summary>
		 * <param name "_dragBase">The DragBase to check for</param>
		 * <returns>True if the DragBase object is being held by the Player</returns>
		 */
		public bool IsDragObjectHeld (DragBase _dragBase)
		{
			if (_dragBase == null)
			{
				return false;
			}

			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (heldObjectData.DragObject == _dragBase)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if any DragBase object is being held by the player.</summary>
		 * <returns>True if any DragBase object is being held by the Player</returns>
		 */
		public bool IsDragObjectHeld ()
		{
			return (heldObjectDatas.Count > 0);
		}


		/**
		 * <summary>Gets the factor by which Player movement is slowed when holding a DragBase object.</summary>
		 * <returns>The factor by which Player movement is slowed when holding a DragBase object</returns>
		 */
		public float GetDragMovementSlowDown ()
		{
			if (heldObjectDatas.Count > 0)
			{
				return (1f - heldObjectDatas[0].DragObject.playerMovementReductionFactor);
			}
			return 1f;
		}
		
		
		protected float GetDeltaTime ()
		{
			return Time.unscaledDeltaTime;
		}


		/**
		 * <summary>Sets the timeScale.</summary>
		 * <param name = "_timeScale">The new timeScale. A value of 0 will have no effect<param>
		 */
		public void SetTimeScale (float _timeScale)
		{
			if (_timeScale > 0f)
			{
				timeScale = _timeScale;
				if (KickStarter.stateHandler.gameState != GameState.Paused)
				{
					Time.timeScale = _timeScale;
				}
			}
		}


		/**
		 * <summary>Assigns an AnimationCurve that controls the timeScale over time.</summary>
		 * <param name = "_timeCurve">The AnimationCurve to use</param>
		 */
		public void SetTimeCurve (AnimationCurve _timeCurve)
		{
			timeCurve = _timeCurve;
			changeTimeStart = Time.time;
		}


		/**
		 * <summary>Checks if time is being controlled by an AnimationCurve.</summary>
		 * <returns>True if time is being controlled by an AnimationCurve.</returns>
		 */
		public bool HasTimeCurve ()
		{
			if (timeCurve != null)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Get what kind of object is currently being dragged (None, Player, Inventory, Menu, ScreenArrows, Moveable, _Camera).</summary>
		 * <returns>What kind of object is currently being dragged (None, Player, Inventory, Menu, ScreenArrows, Moveable, _Camera).</returns>
		 */
		public DragState GetDragState ()
		{
			return dragState;
		}


		/**
		 * <summary>Gets the current state of the mouse buttons (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo).</summary>
		 * <param name = "forScene">If True, then SingleClick will swapped with LetGo, allowing for clicks to register upon release, should the current input settings allow for it</param>
		 * <returns>The current state of the mouse buttons (Normal, SingleClick, RightClick, DoubleClick, HeldDown, LetGo).</returns>
		 */
		public MouseState GetMouseState (bool forScene = true)
		{
			if (forScene)
			{
				if (!(KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.settingsManager.offsetTouchCursor && KickStarter.settingsManager.touchUpInteractScene && KickStarter.settingsManager.movementMethod != MovementMethod.FirstPerson))
				{
					forScene = false;
				}
			}
			if (forScene && KickStarter.settingsManager.InventoryDragDrop)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) || InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					// Disallow when drag-dropping
					forScene = false;
				}
			}

			if (forScene)
			{
				if (mouseState == MouseState.SingleClick)
				{
					return MouseState.Normal;
				}
				if (resetMouseClickThisFrame)
				{
					return MouseState.SingleClick;
				}
			}

			return mouseState;
		}


		/** Resets the mouse click so that nothing else will be affected by it this frame. */
		public void ResetMouseClick ()
		{
			mouseState = MouseState.Normal;
		}


		/**
		 * <summary>Gets the input movement as a vector</summary>
		 * <returns>The input movement as a vector</returns>
		 */
		public Vector2 GetMoveKeys ()
		{
			return moveKeys;
		}


		/**
		 * <summary>Checks if the Player is running due to user-controlled input.</summary>
		 * <returns>True if the Player is running due to user-controller input</returns>
		 */
		public bool IsPlayerControlledRunning ()
		{
			return playerIsControlledRunning;
		}


		/**
		 * <summary>Assigns a MenuDrag element as the one to drag.</summary>
		 * <param name = "menuDrag">The MenuDrag to begin dragging</param>
		 */
		public void SetActiveDragElement (MenuDrag menuDrag)
		{
			activeDragElement = menuDrag;
		}


		/**
		 * <summary>Checks if the last mouse click made was a double-click.</summary>
		 * <returns>True if the last mouse click made was a double-click</returns>
		 */
		public bool LastClickWasDouble ()
		{
			return lastClickWasDouble;
		}


		/**
		 * Resets the speed of "Drag" Player input.
		 */
		public void ResetDragMovement ()
		{
			dragSpeed = 0f;
		}


		/**
		 * <summary>Checks if the magnitude of "Drag" Player input is above the minimum needed to move the Player.</summary>
		 * <returns>True if the magnitude of "Drag" Player input is above the minimum needed to move the Player.</returns>
		 */
		public bool IsDragMoveSpeedOverWalkThreshold ()
		{
			if (dragSpeed > KickStarter.settingsManager.dragWalkThreshold * 10f)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the cursor's position is within the boundary of the screen.</summary>
		 * <returns>True if the cursor's position is within the boundary of the screen</returns>
		 */
		public bool IsMouseOnScreen ()
		{
			return mouseIsOnScreen;
		}


		/**
		 * <summary>Gets the free-aim input vector.</summary>
		 * <returns>The free-aim input vector</returns>
		 */
		public Vector2 GetFreeAim ()
		{
			if (KickStarter.player && KickStarter.player.freeAimLocked)
			{
				return Vector2.zero;
			}
			return freeAim;
		}


		protected virtual Vector2 GetSmoothFreeAim (Vector2 targetFreeAim)
		{
			if (KickStarter.settingsManager.freeAimSmoothSpeed <= 0f)
			{
				return targetFreeAim;
			}

			float factor = 1f;
			if (heldObjectDatas.Count > 0)
			{
				factor = 1f - heldObjectDatas[0].DragObject.playerMovementReductionFactor;
			}

			return freeAimLerp.Update (freeAim, targetFreeAim * factor, KickStarter.settingsManager.freeAimSmoothSpeed);
		}
		

		/** Resets the mouse and assigns the correct gameState in StateHandler after loading a save game. */
		public void OnLoad ()
		{
			pendingOptionConversation = null;
			ResetMouseClick ();
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			mainData.timeScale = KickStarter.playerInput.timeScale;
			mainData.activeArrows = (activeArrows) ? Serializer.GetConstantID (activeArrows.gameObject) : 0;
			mainData.activeConversation = (activeConversation) ? Serializer.GetConstantID (activeConversation.gameObject) : 0;
			mainData.canKeyboardControlMenusDuringGameplay = canKeyboardControlMenusDuringGameplay;

			return mainData;
		}
		
		
		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public void LoadMainData (MainData mainData)
		{
			LetGo ();

			// Active screen arrows
			RemoveActiveArrows ();
			ArrowPrompt loadedArrows = ConstantID.GetComponent <ArrowPrompt> (mainData.activeArrows);
			if (loadedArrows)
			{
				loadedArrows.TurnOn ();
			}
			
			// Active conversation
			activeConversation = ConstantID.GetComponent <Conversation> (mainData.activeConversation);
			pendingOptionConversation = null;
			timeScale = mainData.timeScale;

			canKeyboardControlMenusDuringGameplay = mainData.canKeyboardControlMenusDuringGameplay;

			if (mainData.toggleCursorState > 0)
			{
				toggleCursorOn = (mainData.toggleCursorState == 1) ? true : false;
			}
		}

		
		/**
		 * <summary>Controls an OnGUI-based Menu with keyboard or Controller inputs.</summary>
		 * <param name = "menu">The Menu to control</param>
		 */
		public virtual void InputControlMenu (Menu menu)
		{
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen || menu.menuSource != MenuSource.AdventureCreator)
			{
				return;
			}

			if (menu.IsOn () && menu.CanCurrentlyKeyboardControl (KickStarter.stateHandler.gameState))
			{
				menu.AutoSelect ();

				// Menu option changing
				if (!KickStarter.playerMenus.IsCyclingInteractionMenu ())
				{
					if (KickStarter.stateHandler.gameState == GameState.DialogOptions ||
						KickStarter.stateHandler.gameState == GameState.Paused ||
						(KickStarter.stateHandler.gameState == GameState.Cutscene && menu.CanClickInCutscenes ()) ||
						(KickStarter.stateHandler.IsInGameplay () && canKeyboardControlMenusDuringGameplay))
					{
						Vector2 rawInput = new Vector2 (InputGetAxisRaw (KickStarter.menuManager.horizontalInputAxis), InputGetAxisRaw (KickStarter.menuManager.verticalInputAxis));
						scrollingLocked = menu.GetNextSlot (rawInput, scrollingLocked);
						
						if (rawInput.y < directMenuThreshold && rawInput.y > -directMenuThreshold && rawInput.x < directMenuThreshold && rawInput.x > -directMenuThreshold)
						{
							scrollingLocked = false;
						}
					}
				}
			}
		}


		/**
		 * <summary>Ends the active Conversation.</summary>
		 */
		public void EndConversation ()
		{
			if (activeConversation)
			{
				KickStarter.eventManager.Call_OnEndConversation (activeConversation);
				activeConversation = null;
			}
		}


		/** Displays input-related information for the AC Status window */
		public void DrawStatus ()
		{
			if (activeConversation)
			{
				if (GUILayout.Button ("Conversation: " + activeConversation.name))
				{
					#if UNITY_EDITOR
					EditorGUIUtility.PingObject (activeConversation);
					#endif
				}
			}
			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{ 
				GUILayout.Label ("Dragging: " + heldObjectData.DragObject.name);
			}
		}

		/**
		 * <summary>Checks if a Conversation is currently active</summary>
		 * <param name = "alsoPendingOption">If True, then the method will return True if a Conversation is not active, but is in the delay gap between choosing an option and running it</param>
		 * <returns>True if a Conversation is currently active</returns>
		 */
		public bool IsInConversation (bool alsoPendingOption = false)
		{
			if (activeConversation)
			{
				return true;
			}
			if (pendingOptionConversation && alsoPendingOption)
			{
				return true;
			}
			return false;
		}


		/** A Conversation that has ended, but has yet to run the response */
		public Conversation PendingOptionConversation
		{
			get
			{
				return pendingOptionConversation;
			}
			set
			{
				pendingOptionConversation = value;
			}
		}


		/** 
		 * <summary>Enforces a custom position (in screen coordinates) to apply to the cursor when it is locked</summary>
		 * <param name="position">The position (in screen coordinates)</param>
		 */
		public void OverrideLockedCursorPosition (Vector2 position)
		{
			overrideLockedCursorPosition = true;
			lockedCursorPositionOverride = position;
		}


		/** Releases the custom locked cursor position set with OverrideLockedCursorPosition */
		public void ReleaseLockedCursorPositionOverride ()
		{
			overrideLockedCursorPosition = false;
			mousePosition = InputMousePosition (cursorIsLocked);
		}


		/** The position of the cursor when it is locked */
		public virtual Vector2 LockedCursorPosition
		{
			get
			{
				if (overrideLockedCursorPosition)
				{
					return lockedCursorPositionOverride;
				}
				return new Vector2 (ACScreen.width / 2f, ACScreen.height / 2f);
			}
		}


		/** If True, Menus can be controlled via the keyboard or controller during gameplay */
		public bool CanKeyboardControlMenusDuringGameplay
		{
			get
			{
				return canKeyboardControlMenusDuringGameplay;
			}
			set
			{
				if (canKeyboardControlMenusDuringGameplay && !value)
				{
					List<Menu> allMenus = PlayerMenus.GetMenus (true);
					foreach (Menu menu in allMenus)
					{
						if (menu.CanCurrentlyKeyboardControl (GameState.Normal))
						{
							if (KickStarter.playerMenus.DeselectEventSystemMenu (menu))
							{
								break;
							}
						}
					}
				}

				canKeyboardControlMenusDuringGameplay = value;
			}
		}


		public Vector3 GetDragForce (DragBase dragBase)
		{
			foreach (HeldObjectData heldObjectData in heldObjectDatas)
			{
				if (heldObjectData.DragObject == dragBase)
				{
					return heldObjectData.DragForce;
				}
			}
			return Vector3.zero;
		}

	}

}