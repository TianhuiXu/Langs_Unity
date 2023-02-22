/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"OptionalMouseInputModule.cs"
 * 
 *	This script is an alternative to the Standalone Input Module that makes mouse input optional.
 *  Code adapted from Vodolazz: http://answers.unity3d.com/questions/1197380/make-standalone-input-module-ignore-mouse-input.html
 *  and OpticalOverride: https://forum.unity.com/threads/fake-mouse-position-in-4-6-ui-answered.283748
 */

using UnityEngine.EventSystems;
using UnityEngine;

namespace AC
{

	/**
	 * <summary>This script is an alternative to the Standalone Input Module that makes mouse input optional.
 	 * Code adapted from Vodolazz: http://answers.unity3d.com/questions/1197380/make-standalone-input-module-ignore-mouse-input.html
 	 * and OpticalOverride: https://forum.unity.com/threads/fake-mouse-position-in-4-6-ui-answered.283748/</summary>
	 */
	public class OptionalMouseInputModule : StandaloneInputModule
	{

		private bool allowMouseInput = true;
		private readonly MouseState m_MouseState = new MouseState ();


		public bool AllowMouseInput
		{
			get
			{
				return allowMouseInput;
			}
			set
			{
				allowMouseInput = value;
			}
		}


		protected void Update ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.inputMethod != InputMethod.TouchScreen)
			{
				AllowMouseInput = !CanDirectlyControlMenus ();
			}
			else
			{
				AllowMouseInput = true;
			}
		}


		protected virtual bool CanDirectlyControlMenus ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				return false;
			}

			if ((KickStarter.stateHandler.gameState == GameState.Paused && KickStarter.menuManager.keyboardControlWhenPaused) ||
				(KickStarter.stateHandler.gameState == GameState.DialogOptions && KickStarter.menuManager.keyboardControlWhenDialogOptions) ||
				(KickStarter.stateHandler.IsInGameplay () && KickStarter.playerInput.canKeyboardControlMenusDuringGameplay))
			{
				return true;
			}
			return false;
		}


		protected override MouseState GetMousePointerEventData (int id = 0)
		{
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard)
			{
				return base.GetMousePointerEventData (id);
			}

			PointerEventData leftData;
			var created = GetPointerData (kMouseLeftId, out leftData, true );
	 
			leftData.Reset ();
	 
			Vector2 pos = KickStarter.playerInput.GetMousePosition ();
			if (created)
			{
				leftData.position = pos;
			}

			leftData.delta = pos - leftData.position;
			leftData.position = pos;
			leftData.scrollDelta = Input.mouseScrollDelta;
			leftData.button = PointerEventData.InputButton.Left;
			eventSystem.RaycastAll (leftData, m_RaycastResultCache);
			RaycastResult raycast = FindFirstRaycast (m_RaycastResultCache);
			leftData.pointerCurrentRaycast = raycast;
			m_RaycastResultCache.Clear ();

			if (raycast.isValid && KickStarter.menuManager.autoSelectValidRaycasts && !CanDirectlyControlMenus ())
			{
				KickStarter.playerMenus.EventSystem.SetSelectedGameObject (raycast.gameObject);
			}

			PointerEventData rightData;
			GetPointerData (kMouseRightId, out rightData, true);
			CopyFromTo (leftData, rightData);
			rightData.button = PointerEventData.InputButton.Right;
	 
			PointerEventData middleData;
			GetPointerData (kMouseMiddleId, out middleData, true);
			CopyFromTo (leftData, middleData);
			middleData.button = PointerEventData.InputButton.Middle;
	 
			PointerEventData.FramePressState leftClickState = PointerEventData.FramePressState.NotChanged;
			if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
			{
				if (Input.touchCount == 1)
				{
					TouchPhase phase = Input.GetTouch (0).phase;
					switch (phase)
					{
						case TouchPhase.Began:
							leftClickState = PointerEventData.FramePressState.Pressed;
							break;

						case TouchPhase.Canceled:
							leftClickState = PointerEventData.FramePressState.Released;
							break;

						case TouchPhase.Ended:
							leftClickState = PointerEventData.FramePressState.PressedAndReleased;
							break;

						default:
							
							break;
					}
				}
			}
			else
			{
				if (KickStarter.playerInput.InputGetButtonDown ("InteractionA"))
				{
					leftClickState = PointerEventData.FramePressState.Pressed;
				}
				else if (KickStarter.playerInput.InputGetButtonUp ("InteractionA"))
				{
					leftClickState = PointerEventData.FramePressState.Released;
				}
			}

			PointerEventData.FramePressState rightClickState = PointerEventData.FramePressState.NotChanged;
			if (KickStarter.playerInput.InputGetButtonDown ("InteractionB"))
			{
				rightClickState = PointerEventData.FramePressState.Pressed;
			}
			else if (KickStarter.playerInput.InputGetButtonUp ("InteractionB"))
			{
				rightClickState = PointerEventData.FramePressState.Released;
			}
	 
			m_MouseState.SetButtonState (PointerEventData.InputButton.Left, leftClickState, leftData);
			m_MouseState.SetButtonState (PointerEventData.InputButton.Right, rightClickState, rightData);
			m_MouseState.SetButtonState (PointerEventData.InputButton.Middle, StateForMouseButton (2), middleData);

			return m_MouseState;
		}


		public override void Process ()
		{
			//if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen) {base.Process (); return;}

			bool usedEvent = SendUpdateEventToSelectedObject ();
	 
			if (eventSystem.sendNavigationEvents)
			{
				if (!usedEvent)
				{
					usedEvent |= SendMoveEventToSelectedObject ();
				}
	 
				if (!usedEvent)
				{
					SendSubmitEventToSelectedObject ();
				}
			}

			if (allowMouseInput)
			{
				ProcessMouseEvent ();
			}
		}

	}

}