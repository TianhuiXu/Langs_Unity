/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionType.cs"
 * 
 *	This defines the variables needed by the ActionsManager Editor Window.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A data container for an Action's properties as displayed within ActionsManager.
	 */
	[System.Serializable]
	public class ActionType
	{

		#region Variables

		/** The Action's filename */
		public string fileName;
		/** The Action's category (ActionList, Camera, Character, Container, Dialogue, Engine, Hotspot, Input, Inventory, Menu, Moveable, Object, Player, Save, Sound, ThirdParty, Variable, Custom) */
		public ActionCategory category;
		/** The Action's title */
		public string title;
		/** A brief description about what the Action does */
		public string description;
		/** If True, the Action is enabled and can be used in ActionList objects */
		public bool isEnabled;
		/** The Action's colour, when displayed in the ActionList Editor window */
		public Color color;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_fileName">The Action's filename</param>
		 * <param name = "_action">The Action itself</param>
		 */
		public ActionType (string _fileName, Action _action)
		{
			fileName = _fileName;
			category = _action.Category;
			title = _action.Title;
			description = _action.Description;
			isEnabled = true;
			color = Color.white;
		}


		public ActionType (ActionType _actionType)
		{
			fileName = _actionType.fileName;
			category = _actionType.category;
			title = _actionType.title;
			description = _actionType.description;
			isEnabled = _actionType.isEnabled;
			color = _actionType.color;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Compares the values with that of another ActionType instance to see if they are all equal.</summary>
		 * <param name = "_actionType">The ActionType instance to compare</param>
		 * <summary>True if the values match</summary>
		 */
		public bool IsMatch (ActionType _actionType)
		{
			if (_actionType != null && description == _actionType.description && title == _actionType.title && category == _actionType.category)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the full name (category + title) of the Action.  Actions in the Custom category are prefixed with "ZZ" so that they appear at the bottom in an alphabetical order.</summary>
		 * <returns>The full name (category + title) of the Action</param>
		 */
		public string GetFullTitle (bool forSorting = false)
		{
			if (forSorting)
			{
				if (category == ActionCategory.Custom)
				{
					return ("ZZ" + title);
				}
			}
			return (category.ToString () + ": " + title);
		}

		#endregion
		
	}

}