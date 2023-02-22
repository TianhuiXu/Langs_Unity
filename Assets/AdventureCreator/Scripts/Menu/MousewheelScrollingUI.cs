/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MousewheelScrollingUI.cs"
 * 
 *	This script, when attached to the root canvas of a Unity UI linked to AC's Menu Manager, allows you to scroll through the slots of a given element with an input (by default, the mouse scrollwheel)
 * 
 */
 
using UnityEngine;

namespace AC
{

	/** This script, when attached to the root canvas of a Unity UI linked to AC's Menu Manager, allows you to scroll through the slots of a given element with an input (by default, the mouse scrollwheel) */
	[AddComponentMenu ("Adventure Creator/UI/Mousewheel scrolling UI")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_mousewheel_scrolling_ui.html")]
	public class MousewheelScrollingUI : MonoBehaviour
	{

		#region Variables

		[SerializeField] private string inputName = "Mouse ScrollWheel";
		[SerializeField] private string elementToScroll = "";

		private MenuElement element;

		private bool allowScrollWheel;
		private float inputThreshold = 0.05f;

		#endregion


		#region UnityStandards

		private void OnEnable ()
		{
			if (element == null && KickStarter.playerMenus)
			{
				Menu menu = KickStarter.playerMenus.GetMenuWithCanvas (GetComponent <Canvas>());
				if (menu != null && !string.IsNullOrEmpty (elementToScroll))
				{
					element = menu.GetElementWithName (elementToScroll);
				}
			}
		}


		private void Update ()
		{
			if (element == null) return;

			float scrollInput = 0f;
			try
			{
				scrollInput = Input.GetAxisRaw (inputName);
			}
			catch
			{ }

			if (scrollInput > inputThreshold)
			{
				if (allowScrollWheel)
				{
					element.Shift (AC_ShiftInventory.ShiftPrevious, 1);
					allowScrollWheel = false;
				}
			}
			else if (scrollInput < -inputThreshold)
			{
				if (allowScrollWheel)
				{
					element.Shift (AC_ShiftInventory.ShiftNext, 1);
					allowScrollWheel = false;
				}
			}
			else
			{
				allowScrollWheel = true;
			}
		}

		#endregion
	}

}