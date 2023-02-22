/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMenuJournal.cs"
 * 
 *	This Action allows you to set the page number of a MenuJournal.
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
	public class ActionMenuJournal : Action, IMenuReferencer
	{

		public string menuToChange = "";
		public int menuToChangeParameterID = -1;
		
		public string elementToChange = "";
		public int elementToChangeParameterID = -1;

		public SetJournalPage setJournalPage = SetJournalPage.FirstPage;

		public int pageNumber;
		public int pageNumberParameterID = -1;

		
		public override ActionCategory Category { get { return ActionCategory.Menu; }}
		public override string Title { get { return "Set Journal page"; }}
		public override string Description { get { return "Set which page of a Journal is currently open."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			menuToChange = AssignString (parameters, menuToChangeParameterID, menuToChange);
			elementToChange = AssignString (parameters, elementToChangeParameterID, elementToChange);
			pageNumber = AssignInteger (parameters, pageNumberParameterID, pageNumber);
		}
		
		
		public override float Run ()
		{
			MenuElement _element = PlayerMenus.GetElementWithName (menuToChange, elementToChange);
			if (_element != null)
			{
				if (_element is MenuJournal)
				{
					MenuJournal journal = (MenuJournal) _element;

					if (journal.pages.Count > 0)
					{
						if (setJournalPage == SetJournalPage.FirstPage)
						{
							journal.showPage = 1;
						}
						else if (setJournalPage == SetJournalPage.LastPage)
						{
							journal.showPage = journal.pages.Count;
						}
						else if (setJournalPage == SetJournalPage.SetHere)
						{
							journal.showPage = Mathf.Min (journal.pages.Count, pageNumber);
						}
					}
				}
				else
				{
					LogWarning (_element.title + " is not a journal!");
				}
			}
			else
			{
				LogWarning ("Could not find menu element of name '" + elementToChange + "' inside '" + menuToChange + "'");
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			menuToChangeParameterID = Action.ChooseParameterGUI ("Menu containing element:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
			if (menuToChangeParameterID < 0)
			{
				menuToChange = EditorGUILayout.TextField ("Menu containing element:", menuToChange);
			}
			
			elementToChangeParameterID = Action.ChooseParameterGUI ("Journal element:", parameters, elementToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
			if (elementToChangeParameterID < 0)
			{
				elementToChange = EditorGUILayout.TextField ("Journal element:", elementToChange);
			}

			setJournalPage = (SetJournalPage) EditorGUILayout.EnumPopup ("Page to set to:", setJournalPage);
			if (setJournalPage == SetJournalPage.SetHere)
			{
				pageNumberParameterID = Action.ChooseParameterGUI ("Page #:", parameters, pageNumberParameterID, ParameterType.Integer);
				if (pageNumberParameterID < 0)
				{
					pageNumber = EditorGUILayout.IntField ("Page #:", pageNumber);
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			return setJournalPage.ToString ();
		}


		public int GetNumMenuReferences (string _menuName, string _elementName = "")
		{
			if (menuToChangeParameterID < 0 && menuToChange == _menuName)
			{
				if (string.IsNullOrEmpty (elementToChange))
				{
					return 1;
				}

				if (elementToChangeParameterID < 0 && _elementName == elementToChange)
				{
					return 1;
				}
			}

			return 0;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Menu: Set journal page' Action</summary>
		 * <param name = "menuName">The name of the menu containing the Journal element</param>
		 * <param name = "journalElementName">The name of the Journal element to update</param>
		 * <param name = "pageIndexNumber">The index number of the page to open.  If -1, the last page will be opened</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuJournal CreateNew (string menuName, string journalElementName, int pageIndexNumber)
		{
			ActionMenuJournal newAction = CreateNew<ActionMenuJournal> ();
			newAction.menuToChange = menuName;
			newAction.elementToChange = journalElementName;
			newAction.pageNumber = pageIndexNumber;

			if (pageIndexNumber == 0) newAction.setJournalPage = SetJournalPage.FirstPage;
			else if (pageIndexNumber == -1) newAction.setJournalPage = SetJournalPage.LastPage;
			else newAction.setJournalPage = SetJournalPage.SetHere;

			return newAction;
		}
		
	}
	
}