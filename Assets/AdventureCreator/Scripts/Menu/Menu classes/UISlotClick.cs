/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"UISlotClick.cs"
 * 
 *	This component acts as a click handler for Unity UI Buttons, and is added automatically by UISlot.
 * 
 */

using UnityEngine;
using UnityEngine.EventSystems;

namespace AC
{

	/** This component acts as a click handler for Unity UI Buttons, and is added automatically by UISlot. */
	public class UISlotClick : MonoBehaviour, ISelectHandler
	{

		#region Variables

		protected AC.Menu menu;
		protected MenuElement menuElement;
		protected int slot;
		private float menuLastTurnOnTime;
		
		#endregion


		#region UnityStandards

		/** Implementation of ISelectHandler */
		public void OnSelect (BaseEventData eventData)
		{
			if (menuElement == null) return;

			if (menu.CanCurrentlyKeyboardControl (KickStarter.stateHandler.gameState))
			{
				if (Time.unscaledTime > menuLastTurnOnTime + 0.3f)
				{
					KickStarter.sceneSettings.PlayDefaultSound (menuElement.hoverSound, false);
				}
				KickStarter.eventManager.Call_OnMouseOverMenuElement (menu, menuElement, slot);
			}
		}


		private void OnEnable ()
		{
			EventManager.OnMenuTurnOn += OnMenuTurnOn;
		}


		private void OnDisable ()
		{
			EventManager.OnMenuTurnOn -= OnMenuTurnOn;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Syncs the component to a slot within a menu.</summary>
		 * <param name = "_menu">The Menu that the Button is linked to</param>
		 * <param name = "_element">The MenuElement within _menu that the Button is linked to</param>
		 * <param name = "_slot">The index number of the slot within _element that the Button is linked to</param>
		 */
		public void Setup (AC.Menu _menu, MenuElement _element, int _slot)
		{
			if (_menu == null)
			{
				return;
			}

			menu = _menu;
			menuElement = _element;
			slot = _slot;
		}

		#endregion


		#region CustomEvents

		private void OnMenuTurnOn (Menu _menu, bool isInstant)
		{
			if (menu == _menu)
			{
				menuLastTurnOnTime = Time.unscaledTime;
			}
		}

		#endregion

	}

}