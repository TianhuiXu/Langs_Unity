/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMenuSlotCheck.cs"
 * 
 *	This Action checks the number of slots on a menu element
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionMenuSlotCheck : ActionCheck, IMenuReferencer
	{
		
		public string menuToCheck = "";
		public int menuToCheckParameterID = -1;
		
		public string elementToCheck = "";
		public int elementToCheckParameterID = -1;

		public int numToCheck;
		public int numToCheckParameterID = -1;
		public IntCondition intCondition;

		
		public override ActionCategory Category { get { return ActionCategory.Menu; }}
		public override string Title { get { return "Check num slots"; }}
		public override string Description { get { return "Queries the number of slots on a given menu element."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			menuToCheck = AssignString (parameters, menuToCheckParameterID, menuToCheck);
			elementToCheck = AssignString (parameters, elementToCheckParameterID, elementToCheck);
			numToCheck = AssignInteger (parameters, numToCheckParameterID, numToCheck);
		}
		
		
		public override bool CheckCondition ()
		{
			MenuElement menuElement = PlayerMenus.GetElementWithName (menuToCheck, elementToCheck);
			if (menuElement != null)
			{
				int numSlots = menuElement.GetNumSlots ();

				switch (intCondition)
				{
					case IntCondition.EqualTo:
						return (numSlots == numToCheck);

					case IntCondition.NotEqualTo:
						return (numSlots != numToCheck);

					case IntCondition.LessThan:
						return (numSlots < numToCheck);

					case IntCondition.MoreThan:
						return (numSlots > numToCheck);

					default:
						return false;
				}
			}
			
			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
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

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Number of slots is:", GUILayout.Width (145f));
			intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);
			EditorGUILayout.EndHorizontal ();

			numToCheckParameterID = Action.ChooseParameterGUI ("Value:", parameters, numToCheckParameterID, ParameterType.Integer);
			if (numToCheckParameterID < 0)
			{
				numToCheck = EditorGUILayout.IntField ("Value:", numToCheck);
			}
		}
		
		
		public override string SetLabel ()
		{
			return menuToCheck + " " + elementToCheck;
		}


		public int GetNumMenuReferences (string menuName, string elementName = "")
		{
			if (menuToCheckParameterID < 0 && menuName == menuToCheck)
			{
				if (string.IsNullOrEmpty (elementName))
				{
					return 1;
				}

				if (elementToCheckParameterID < 0 && elementToCheck == elementName)
				{
					return 1;
				}
			}

			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Menu: Check num slots' Action</summary>
		 * <param name = "menuName">The name of the Menu</param>
		 * <param name = "elementName">The name of the element</param>
		 * <param name = "numSlots">The number of slots to check for</param>
		 * <param name = "condition">The condition to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuSlotCheck CreateNew (string menuName, string elementName, int numSlots, IntCondition condition = IntCondition.EqualTo)
		{
			ActionMenuSlotCheck newAction = CreateNew<ActionMenuSlotCheck> ();
			newAction.menuToCheck = menuName;
			newAction.elementToCheck = elementName;
			newAction.intCondition = condition;
			newAction.numToCheck = numSlots;

			return newAction;
		}
		
	}
	
}