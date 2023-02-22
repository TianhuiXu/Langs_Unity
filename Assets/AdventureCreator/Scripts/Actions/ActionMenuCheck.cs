/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMenuCheck.cs"
 * 
 *	This Action checks the visibility states of menus and elements
 * 
 */

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionMenuCheck : ActionCheck, IMenuReferencer
	{
		
		public enum MenuCheckType { MenuIsVisible, MenuIsLocked, ElementIsVisible };
		public MenuCheckType checkType = MenuCheckType.MenuIsVisible;

		public string menuToCheck = "";
		public int menuToCheckParameterID = -1;
		
		public string elementToCheck = "";
		public int elementToCheckParameterID = -1;

		protected LocalVariables localVariables;
		protected string _menuToCheck, _elementToCheck;

		
		public override ActionCategory Category { get { return ActionCategory.Menu; }}
		public override string Title { get { return "Check state"; }}
		public override string Description { get { return "Queries the visibility of menu elements, and the enabled or locked state of menus."; }}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			menuToCheck = AssignString (parameters, menuToCheckParameterID, menuToCheck);
			elementToCheck = AssignString (parameters, elementToCheckParameterID, elementToCheck);

			_menuToCheck = AdvGame.ConvertTokens (menuToCheck, Options.GetLanguage (), localVariables, parameters);
			_elementToCheck = AdvGame.ConvertTokens (elementToCheck, Options.GetLanguage (), localVariables, parameters);
		}


		public override bool CheckCondition ()
		{
			AC.Menu _menu = PlayerMenus.GetMenuWithName (_menuToCheck);
			if (_menu != null)
			{
				if (checkType == MenuCheckType.MenuIsVisible)
				{
					return _menu.IsVisible ();
				}
				else if (checkType == MenuCheckType.MenuIsLocked)
				{
					return _menu.isLocked;
				}
				else if (checkType == MenuCheckType.ElementIsVisible)
				{
					MenuElement _element = PlayerMenus.GetElementWithName (_menuToCheck, _elementToCheck);
					if (_element != null)
					{
						return _element.IsVisible;
					}
				}
			}

			return false;
		}
		

		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			checkType = (MenuCheckType) EditorGUILayout.EnumPopup ("State to check:", checkType);
			
			if (checkType == MenuCheckType.MenuIsVisible || checkType == MenuCheckType.MenuIsLocked)
			{
				menuToCheckParameterID = Action.ChooseParameterGUI ("Menu to check:", parameters, menuToCheckParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (menuToCheckParameterID < 0)
				{
					menuToCheck = EditorGUILayout.TextField ("Menu to check:", menuToCheck);
				}
			}
			else if (checkType == MenuCheckType.ElementIsVisible)
			{
				menuToCheckParameterID = Action.ChooseParameterGUI ("Menu containing element:", parameters, menuToCheckParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (menuToCheckParameterID < 0)
				{
					menuToCheck = EditorGUILayout.TextField ("Menu containing element:", menuToCheck);
				}

				elementToCheckParameterID = Action.ChooseParameterGUI ("Element to check:", parameters, elementToCheckParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (elementToCheckParameterID < 0)
				{
					elementToCheck = EditorGUILayout.TextField ("Element to check:", elementToCheck);
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			string labelAdd = checkType.ToString () + " '" + menuToCheck;
			if (checkType == MenuCheckType.ElementIsVisible)
			{
				labelAdd += " " + elementToCheck;
			}
			return labelAdd;
		}


		public int GetNumMenuReferences (string menuName, string elementName = "")
		{
			if (menuToCheckParameterID < 0 && menuName == menuToCheck)
			{
				switch (checkType)
				{
					case MenuCheckType.MenuIsLocked:
					case MenuCheckType.MenuIsVisible:
						if (string.IsNullOrEmpty (elementName))
						{
							return 1;
						}
						break;

					case MenuCheckType.ElementIsVisible:
						if (elementToCheckParameterID < 0 && !string.IsNullOrEmpty (elementName) && elementToCheck == elementName)
						{
							return 1;
						}
						break;
				}
			}
			
			return 0;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Menu: Check' Action, set to check if a menu is locked</summary>
		 * <param name = "menuName">The name of the menu to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuCheck CreateNew_MenuIsLocked (string menuName)
		{
			ActionMenuCheck newAction = CreateNew<ActionMenuCheck> ();
			newAction.checkType = MenuCheckType.MenuIsLocked;
			newAction.menuToCheck = menuName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Menu: Check state' Action, set to check if a menu is turned on</summary>
		 * <param name = "menuName">The name of the menu to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuCheck CreateNew_MenuIsOn (string menuName)
		{
			ActionMenuCheck newAction = CreateNew<ActionMenuCheck> ();
			newAction.checkType = MenuCheckType.MenuIsVisible;
			newAction.menuToCheck = menuName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Menu: Check state' Action, set to check if a menu element is visible</summary>
		 * <param name = "menuName">The name of the menu with the element</param>
		 * <param name = "elementName">The name of the element to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuCheck CreateNew_ElementIsVisible (string menuName, string elementName)
		{
			ActionMenuCheck newAction = CreateNew<ActionMenuCheck> ();
			newAction.checkType = MenuCheckType.ElementIsVisible;
			newAction.menuToCheck = menuName;
			newAction.elementToCheck = elementName;
			return newAction;
		}
		
	}

}