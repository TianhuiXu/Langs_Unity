/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ClickByHoveringCursorExample.cs"
 * 
 *	This script serves as an example of how you can override AC's input system to invoke Hotspot and Menu clicks by hovering over them for a set period of time.
 *	It works by calling custom events whenever a Hotspot or Menu is hovered over that begin an internal countdown to a manual "mouse click".
 *
 *	To use it, add it to any GameObject in the scene, and set the "Hover Duration" value in the Inspector to suit.
 *
 *	If you wish to modify or extend the feature (for example, to add a UI that shows the countdown), you can duplicated this script and amend it to suit your needs. 
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script serves as an example of how you can override AC's input system to invoke Hotspot and Menu clicks by hovering over them for a set period of time.
	 * It works by calling custom events whenever a Hotspot or Menu is hovered over that begin an internal countdown to a manual "mouse click".
	 *
	 * It is compatible with the "World Space Cursor Example" script as well.
	 *
	 * To use it, add it to any GameObject in the scene, and set the "Hover Duration" value in the Inspector to suit.
	 *
	 * If you wish to modify or extend the feature (for example, to add a UI that shows the countdown), you can duplicated this script and amend it to suit your needs.
	 */
	[AddComponentMenu("Adventure Creator/3rd-party/Click by hovering cursor example")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_click_by_hovering_cursor_example.html")]
	public class ClickByHoveringCursorExample : MonoBehaviour
	{

		/** The duration to keep the Cursor over a Hotspot or Menu Element to invoke it automatically */
		public float hoverDuration = 1f;

		private float hotspotHoverDuration = 0f;
		private float menuHoverDuration = 0f;
		private string elementOverIdentifier;

		private Hotspot hoverHotspot;

		private Menu hoverMenu;
		private MenuElement hoverElement;
		private int hoverSlot;


		private void OnEnable ()
		{
			/*
			 * First, we'll hook up our own functions to the Event Manager, so that we can run our own code when a Hotspot
			 * is selected and deselected, when the Cursor hovers over a Menu element, and when a Menu is turned off.
			 */

			EventManager.OnHotspotSelect += OnSelectHotspot;
			EventManager.OnHotspotDeselect += OnDeselectHotspot;
			EventManager.OnMouseOverMenu += OnMouseOverMenu;
			EventManager.OnMenuTurnOff += OnMenuTurnOff;
		}


		private void Update ()
		{
			/**
			 * Every frame, we update the Hotspot and Menu countdowns, and check if they've reached zero.
			 */

			if (hotspotHoverDuration > 0.0f)
			{
				hotspotHoverDuration -= Time.fixedDeltaTime;

				if (hotspotHoverDuration <= 0f)
				{
					/**
					 * The Hotspot countdown has reached zero, so we'll simulate the "InteractionA" input button so long as the original Hotspot is still selected.
					 */

					hotspotHoverDuration = 0f;
					if (hoverHotspot != null && KickStarter.playerInteraction.GetActiveHotspot () == hoverHotspot)
					{
						KickStarter.playerInput.SimulateInputButton ("InteractionA");
						elementOverIdentifier = "";
					}
				}
			}

			if (menuHoverDuration > 0.0f)
			{
				menuHoverDuration -= Time.fixedDeltaTime;

				if (menuHoverDuration <= 0f)
				{
					/**
					 * The Menu countdown has reached zero, so we'll call ProcessClick on the original MenuElement, provided that the cursor is still hovering over it.
					 */

					menuHoverDuration = 0f;

					if (hoverElement != null && hoverMenu != null && hoverMenu.IsPointerOverSlot (hoverElement, hoverSlot, KickStarter.playerInput.GetInvertedMouse ()))
					{
						hoverElement.ProcessClick (hoverMenu, hoverSlot, MouseState.SingleClick);
						elementOverIdentifier = "";
					}
				}
			}
		}


		private void OnSelectHotspot (Hotspot hotspot)
		{
			/**
			 * Whenever a Hotspot is selected, begin the countdown.
			 */

			hoverHotspot = hotspot;
			hotspotHoverDuration = hoverDuration;
		}


		private void OnDeselectHotspot (Hotspot hotspot)
		{
			/**
			 * Whenever a Hotspot is deselected, end the countdown.
			 */

			hoverHotspot = null;
			hotspotHoverDuration = 0f;
		}


		private void OnMouseOverMenu (AC.Menu _menu, MenuElement _element, int _slot)
		{
			/**
			 * Whenever a new Menu Element slot is hovered over, begin the countdown.
			 * Since this is called every frame that the mouse is over an Element slot, we'll construct an "elementOverIdentifer" string
			 * from the arguments to determine if this is the first frame that the hover-over occurs.
			 */

			string newElementOverIdentifier = "";
			if (_element != null)
			{
				newElementOverIdentifier = _menu.id.ToString () + " " + _element.ID.ToString () + " " + _slot.ToString ();
			}

			if (newElementOverIdentifier != "" && newElementOverIdentifier != elementOverIdentifier)
			{
				hotspotHoverDuration = 0f;
				menuHoverDuration = hoverDuration;

				hoverMenu = _menu;
				hoverElement = _element;
				hoverSlot = _slot;
			}

			elementOverIdentifier = newElementOverIdentifier;
		}


		private void OnMenuTurnOff (AC.Menu _menu, bool isInstant)
		{
			/**
			 * This function isn't strictly necessary, but it we're using Choose Hotspot Then Interaction mode, we can ensure the Hotspot
			 * can be clicked again if we cancel the Interaction menu by moving the Cursor away from it.
			 */

			if (_menu.title == "Interaction")
			{
				if (hoverHotspot != null && KickStarter.playerInteraction.GetActiveHotspot () == hoverHotspot)
				{
					OnSelectHotspot (hoverHotspot);
				}
			}
		}


		private void OnDisable ()
		{
			/*
			 * To prevent unwanted affects, we'll remove the Event Manager hooks when the component is disabled.
			 */

			EventManager.OnHotspotSelect -= OnSelectHotspot;
			EventManager.OnHotspotDeselect -= OnDeselectHotspot;
			EventManager.OnMouseOverMenu -= OnMouseOverMenu;
			EventManager.OnMenuTurnOff -= OnMenuTurnOff;
		}

	}

}