/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMenuState.cs"
 * 
 *	This Action alters various variables of menus and menu elements.
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
	public class ActionMenuState : Action, ITranslatable, IMenuReferencer
	{
		
		public enum MenuChangeType { TurnOnMenu, TurnOffMenu, HideMenuElement, ShowMenuElement, LockMenu, UnlockMenu, AddJournalPage, RemoveJournalPage };
		public MenuChangeType changeType = MenuChangeType.TurnOnMenu;

		[SerializeField] protected RemoveJournalPageMethod removeJournalPageMethod = RemoveJournalPageMethod.RemoveSinglePage;
		public enum RemoveJournalPageMethod { RemoveSinglePage, RemoveAllPages };

		public string menuToChange = "";
		public int menuToChangeParameterID = -1;
		
		public string elementToChange = "";
		public int elementToChangeParameterID = -1;
		
		public string journalText = "";
		public bool onlyAddNewJournal = false;

		public bool doFade = false;
		public int lineID = -1;

		public int journalPageIndex = -1;
		public int journalPageIndexParameterID = -1;

		protected LocalVariables localVariables;
		protected string runtimeMenuToChange, runtimeElementToChange;

		public bool preprocessTextTokens = false;


		public override ActionCategory Category { get { return ActionCategory.Menu; }}
		public override string Title { get { return "Change state"; }}
		public override string Description { get { return "Provides various options to show and hide both menus and menu elements."; }}


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
			menuToChange = AssignString (parameters, menuToChangeParameterID, menuToChange);
			elementToChange = AssignString (parameters, elementToChangeParameterID, elementToChange);
			journalPageIndex = AssignInteger (parameters, journalPageIndexParameterID, journalPageIndex);

			runtimeMenuToChange = AdvGame.ConvertTokens (menuToChange, Options.GetLanguage (), localVariables, parameters);
			runtimeElementToChange = AdvGame.ConvertTokens (elementToChange, Options.GetLanguage (), localVariables, parameters);
		}
				
		
		public override float Run ()
		{
			if (!isRunning)
			{
				AC.Menu _menu = PlayerMenus.GetMenuWithName (runtimeMenuToChange);

				if (_menu == null)
				{
					return 0f;
				}

				isRunning = true;

				switch (changeType)
				{
					case MenuChangeType.TurnOnMenu:
						if (_menu.IsManualControlled ())
						{
							if (!_menu.TurnOn (doFade))
							{
								// Menu is already on
								isRunning = false;
								return 0f;
							}
							
							if (doFade && willWait)
							{
								return _menu.fadeSpeed;
							}
						}
						else
						{
							LogWarning ("Can only turn on Menus with an Appear Type of Manual, OnInputKey, OnContainer or OnViewDocument - did you mean 'Unlock Menu'?");
						}
						break;

					case MenuChangeType.TurnOffMenu:
						if (_menu.IsManualControlled () || _menu.appearType == AppearType.OnInteraction)
						{
							if (!_menu.TurnOff (doFade))
							{
								// Menu is already off
								isRunning = false;
								return 0f;
							}
							
							if (doFade && willWait)
							{
								return _menu.fadeSpeed;
							}
						}
						else
						{
							LogWarning ("Can only turn off Menus with an Appear Type of Manual, OnInputKey, OnContainer or OnViewDocument - did you mean 'Lock Menu'?");
						}
						break;

					case MenuChangeType.LockMenu:
						if (doFade)
						{
							_menu.TurnOff (true);
						}
						else
						{
							_menu.ForceOff ();
						}
						_menu.isLocked = true;
						
						if (doFade && willWait)
						{
							return _menu.fadeSpeed;
						}
						break;

					default:
						RunInstant (_menu);
						break;
				}
			}
			else
			{
				isRunning = false;
				return 0f;
			}
			
			return 0f;
		}
		
		
		public override void Skip ()
		{
			AC.Menu _menu = PlayerMenus.GetMenuWithName (runtimeMenuToChange);
			if (_menu == null)
			{
				if (!string.IsNullOrEmpty (runtimeMenuToChange))
				{
					ACDebug.LogWarning ("Could not find menu of name '" + runtimeMenuToChange + "'");
				}
				return;
			}

			switch (changeType)
			{
				case MenuChangeType.TurnOnMenu:
					if (_menu.IsManualControlled ())
					{
						_menu.TurnOn (false);
					}
					break;

				case MenuChangeType.TurnOffMenu:
					if (_menu.IsManualControlled () || _menu.appearType == AppearType.OnInteraction)
					{
						_menu.ForceOff ();
					}
					break;

				case MenuChangeType.LockMenu:
					_menu.isLocked = true;
					_menu.ForceOff ();
					break;

				default:
					RunInstant (_menu);
					break;
			}
		}


		protected void RunInstant (AC.Menu _menu)
		{
			switch (changeType)
			{
				case MenuChangeType.HideMenuElement:
				case MenuChangeType.ShowMenuElement:
					{
						MenuElement _element = PlayerMenus.GetElementWithName (runtimeMenuToChange, runtimeElementToChange);
						if (_element != null)
						{
							if (changeType == MenuChangeType.HideMenuElement)
							{
								_element.IsVisible = false;
								KickStarter.playerMenus.DeselectInputBox (_element);
							}
							else
							{
								_element.IsVisible = true;
							}

							_menu.ResetVisibleElements ();
							_menu.Recalculate ();

							KickStarter.playerMenus.FindFirstSelectedElement ();
						}
						else
						{
							LogWarning ("Could not find element of name '" + elementToChange + "' on menu '" + menuToChange + "'");
						}
					}
					break;

				case MenuChangeType.UnlockMenu:
					_menu.isLocked = false;
					break;

				case MenuChangeType.AddJournalPage:
					{
						MenuElement _element = PlayerMenus.GetElementWithName (runtimeMenuToChange, runtimeElementToChange);
						if (_element != null)
						{
							if (!string.IsNullOrEmpty (journalText))
							{
								if (_element is MenuJournal)
								{
									MenuJournal journal = (MenuJournal) _element;

									string processedPageText = preprocessTextTokens ? AdvGame.ConvertTokens (journalText, Options.GetLanguage ()) : journalText;

									JournalPage newPage = new JournalPage (lineID, processedPageText);
									journal.AddPage (newPage, preprocessTextTokens ? false : onlyAddNewJournal, journalPageIndex);

									if (lineID == -1)
									{
										LogWarning ("The new Journal page has no ID number, and will not be included in save game files - this can be corrected by clicking 'Gather text' in the Speech Manager");
									}
								}
								else
								{
									ACDebug.LogWarning (_element.title + " is not a journal!");
								}
							}
							else
							{
								ACDebug.LogWarning ("No journal text to add!");
							}
						}
						else
						{
							LogWarning ("Could not find menu element of name '" + elementToChange + "' inside '" + menuToChange + "'");
						}
						_menu.Recalculate ();
					}
					break;

				case MenuChangeType.RemoveJournalPage:
					{
						MenuElement _element = PlayerMenus.GetElementWithName (runtimeMenuToChange, runtimeElementToChange);
						if (_element != null)
						{
							if (_element is MenuJournal)
							{
								MenuJournal journal = (MenuJournal) _element;

								if (removeJournalPageMethod == RemoveJournalPageMethod.RemoveAllPages)
								{
									journal.RemoveAllPages ();
								}
								else if (removeJournalPageMethod == RemoveJournalPageMethod.RemoveSinglePage)
								{
									journal.RemovePage (journalPageIndex);
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
						_menu.Recalculate ();
					}
					break;

				default:
					break;
			}
		}

		
		#if UNITY_EDITOR

		public override void ClearIDs ()
		{
			lineID = -1;
		}

		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			changeType = (MenuChangeType) EditorGUILayout.EnumPopup ("Change type:", changeType);
			
			switch (changeType)
			{
				case MenuChangeType.TurnOnMenu:
					{
						menuToChangeParameterID = Action.ChooseParameterGUI ("Menu to turn on:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (menuToChangeParameterID < 0)
						{
							menuToChange = EditorGUILayout.TextField ("Menu to turn on:", menuToChange);
						}
						doFade = EditorGUILayout.Toggle ("Transition?", doFade);
					}
					break;
			
				case MenuChangeType.TurnOffMenu:
					{
						menuToChangeParameterID = Action.ChooseParameterGUI ("Menu to turn off:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (menuToChangeParameterID < 0)
						{
							menuToChange = EditorGUILayout.TextField ("Menu to turn off:", menuToChange);
						}
						doFade = EditorGUILayout.Toggle ("Transition?", doFade);
					}
					break;
			
				case MenuChangeType.HideMenuElement:
					{
						menuToChangeParameterID = Action.ChooseParameterGUI ("Menu containing element:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (menuToChangeParameterID < 0)
						{
							menuToChange = EditorGUILayout.TextField ("Menu containing element:", menuToChange);
						}
				
						elementToChangeParameterID = Action.ChooseParameterGUI ("Element to hide:", parameters, elementToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (elementToChangeParameterID < 0)
						{
							elementToChange = EditorGUILayout.TextField ("Element to hide:", elementToChange);
						}
					}
					break;
			
				case MenuChangeType.ShowMenuElement:
					{
						menuToChangeParameterID = Action.ChooseParameterGUI ("Menu containing element:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (menuToChangeParameterID < 0)
						{
							menuToChange = EditorGUILayout.TextField ("Menu containing element:", menuToChange);
						}
				
						elementToChangeParameterID = Action.ChooseParameterGUI ("Element to show:", parameters, elementToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (elementToChangeParameterID < 0)
						{
							elementToChange = EditorGUILayout.TextField ("Element to show:", elementToChange);
						}
					}
					break;
			
				case MenuChangeType.LockMenu:
					{
						menuToChangeParameterID = Action.ChooseParameterGUI ("Menu to lock:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (menuToChangeParameterID < 0)
						{
							menuToChange = EditorGUILayout.TextField ("Menu to lock:", menuToChange);
						}
						doFade = EditorGUILayout.Toggle ("Transition?", doFade);
					}
					break;
			
				case MenuChangeType.UnlockMenu:
					{
						menuToChangeParameterID = Action.ChooseParameterGUI ("Menu to unlock:", parameters, menuToChangeParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (menuToChangeParameterID < 0)
						{
							menuToChange = EditorGUILayout.TextField ("Menu to unlock:", menuToChange);
						}
					}
					break;
			
				case MenuChangeType.AddJournalPage:
					{
						if (lineID > -1)
						{
							EditorGUILayout.LabelField ("Speech Manager ID:", lineID.ToString ());
						}
				
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
				
						journalText = CustomGUILayout.TextArea ("New page text:", journalText);
						preprocessTextTokens = EditorGUILayout.Toggle ("Pre-process text tokens?", preprocessTextTokens);

						if (!preprocessTextTokens)
						{
							onlyAddNewJournal = EditorGUILayout.Toggle ("Only add if not already in?", onlyAddNewJournal);
							if (onlyAddNewJournal && lineID == -1)
							{
								EditorGUILayout.HelpBox ("The page text must be added to the Speech Manager by clicking the 'Gather text' button, in order for duplicates to be prevented.", MessageType.Warning);
							}
						}

						journalPageIndexParameterID = Action.ChooseParameterGUI ("Index to insert into:", parameters, journalPageIndexParameterID, ParameterType.Integer);
						if (journalPageIndexParameterID < 0)
						{
							journalPageIndex = EditorGUILayout.IntField ("Index to insert into:", journalPageIndex);
							EditorGUILayout.HelpBox ("An index value of -1 will add the page to the end of the Journal.", MessageType.Info);
						}
					}
					break;

				case MenuChangeType.RemoveJournalPage:
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

						removeJournalPageMethod = (RemoveJournalPageMethod) EditorGUILayout.EnumPopup ("Removal method:", removeJournalPageMethod);
						if (removeJournalPageMethod == RemoveJournalPageMethod.RemoveSinglePage)
						{
							journalPageIndexParameterID = Action.ChooseParameterGUI ("Page number to remove:", parameters, journalPageIndexParameterID, ParameterType.Integer);
							if (journalPageIndexParameterID < 0)
							{
								journalPageIndex = EditorGUILayout.IntField ("Page number to remove:", journalPageIndex);
								EditorGUILayout.HelpBox ("An index value of -1 will remove the last page of the Journal.", MessageType.Info);
							}
						}
					}
					break;

				default:
					break;
			}
			
			if (doFade && (changeType == MenuChangeType.TurnOnMenu || changeType == MenuChangeType.TurnOffMenu || changeType == MenuChangeType.LockMenu))
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
		}
		
		
		public override string SetLabel ()
		{
			string labelAdd = changeType.ToString () + " '" + menuToChange;
			if (changeType == MenuChangeType.HideMenuElement || changeType == MenuChangeType.ShowMenuElement)
			{
				labelAdd += " " + elementToChange;
			}
			return labelAdd;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			string updatedJournalText = AdvGame.ConvertLocalVariableTokenToGlobal (journalText, oldLocalID, newGlobalID);
			if (journalText != updatedJournalText)
			{
				wasAmended = true;
				journalText = updatedJournalText;
			}
			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			string updatedJournalText = AdvGame.ConvertGlobalVariableTokenToLocal (journalText, oldGlobalID, newLocalID);
			if (journalText != updatedJournalText)
			{
				isAffected = true;
				if (isCorrectScene)
				{
					journalText = updatedJournalText;
				}
			}
			return isAffected;
		}


		public override int GetNumVariableReferences (VariableLocation location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			string tokenText = AdvGame.GetVariableTokenText (location, varID, _variablesConstantID);
			if (!string.IsNullOrEmpty (tokenText) && journalText.Contains (tokenText))
			{
				thisCount ++;
			}
			thisCount += base.GetNumVariableReferences (location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;
			string oldTokenText = AdvGame.GetVariableTokenText (location, oldVarID, _variablesConstantID);
			if (!string.IsNullOrEmpty (oldTokenText) && journalText.Contains (oldTokenText))
			{
				string newTokenText = AdvGame.GetVariableTokenText (location, newVarID, _variablesConstantID);
				journalText = journalText.Replace (oldTokenText, newTokenText);
				thisCount++;
			}
			thisCount += base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public int GetNumMenuReferences (string menuName, string elementName = "")
		{
			if (menuToChangeParameterID < 0 && menuName == menuToChange)
			{
				switch (changeType)
				{
					case MenuChangeType.TurnOnMenu:
					case MenuChangeType.TurnOffMenu:
					case MenuChangeType.LockMenu:
					case MenuChangeType.UnlockMenu:
						if (string.IsNullOrEmpty (elementName))
						{
							return 1;
						}
						break;

					case MenuChangeType.ShowMenuElement:
					case MenuChangeType.HideMenuElement:
					case MenuChangeType.AddJournalPage:
					case MenuChangeType.RemoveJournalPage:
						if (elementToChangeParameterID < 0 && !string.IsNullOrEmpty (elementName) && elementToChange == elementName)
						{
							return 1;
						}
						break;
				}
			}
			
			return 0;
		}

		#endif


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return journalText;
		}


		public int GetTranslationID (int index)
		{
			return lineID;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			journalText = updatedText;
		}


		public int GetNumTranslatables ()
		{
			return 1;
		}


		public bool CanTranslate (int index)
		{
			if (changeType == ActionMenuState.MenuChangeType.AddJournalPage && !string.IsNullOrEmpty (journalText) && !preprocessTextTokens)
			{
				return true;
			}
			return false;
		}


		public bool HasExistingTranslation (int index)
		{
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			lineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.JournalEntry;
		}
		
		#endif

		#endregion


		/**
		 * <summary>Creates a new instance of the 'Menu: Change state' Action, set to turn a menu on</summary>
		 * <param name = "menuToTurnOn">The name of the menu to turn on</param>
		 * <param name = "unlockMenu">If True, the menu will be unlocked as well</param>
		 * <param name = "doTransition">If True, the menu will transition on - as opposed to turning on instantly</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the transition has completed</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuState CreateNew_TurnOnMenu (string menuToTurnOn, bool unlockMenu = true, bool doTransition = true, bool waitUntilFinish = false)
		{
			ActionMenuState newAction = CreateNew<ActionMenuState> ();
			newAction.changeType = (unlockMenu) ? MenuChangeType.UnlockMenu : MenuChangeType.TurnOnMenu;
			newAction.menuToChange = menuToTurnOn;
			newAction.doFade = doTransition;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Menu: Change state' Action, set to turn a menu off</summary>
		 * <param name = "menuToTurnOff">The name of the menu to turn off</param>
		 * <param name = "lockMenu">If True, the menu will be locked as well</param>
		 * <param name = "doTransition">If True, the menu will transition off - as opposed to turning off instantly</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the transition has completed</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuState CreateNew_TurnOffMenu (string menuToTurnOff, bool lockMenu = false, bool doTransition = true, bool waitUntilFinish = false)
		{
			ActionMenuState newAction = CreateNew<ActionMenuState> ();
			newAction.changeType = (lockMenu) ? MenuChangeType.LockMenu : MenuChangeType.TurnOffMenu;
			newAction.menuToChange = menuToTurnOff;
			newAction.doFade = doTransition;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Menu: Change state' Action, set to alter a menu element's visibility</summary>
		 * <param name = "menuName">The name of the menu with the element</param>
		 * <param name = "elementToAffect">The name of the element to affect</param>
		 * <param name = "makeVisible">If True, the element will be made visible. Otherwise, it will be made invisible</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuState CreateNew_SetElementVisibility (string menuName, string elementToAffect, bool makeVisible)
		{
			ActionMenuState newAction = CreateNew<ActionMenuState> ();
			newAction.changeType = (makeVisible) ? MenuChangeType.ShowMenuElement : MenuChangeType.HideMenuElement;
			newAction.menuToChange = menuName;
			newAction.elementToChange = elementToAffect;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Menu: Change state' Action, set to add a new page to a journal elemnt</summary>
		 * <param name = "menuName">The name of the menu with the journal</param>
		 * <param name = "journalElementName">The name of the journal element to update</param>
		 * <param name = "newPageText">The text of the new page to add</param>
		 * <param name = "newPageranslationID">The new page's translation ID number, as generated by the Speech Manager</param>
		 * <param name = "pageIndexToInsertInto">The index number of the journal's existing pages to insert the new page into. If negative, it will be inserted at the end</param>
		 * <param name = "avoidDuplicated">If True, the page will only be added if not already present in the journal</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMenuState CreateNew_AddJournalPage (string menuName, string journalElementName, string newPageText, int newPageTranslationID = -1, int pageIndexToInsertInto = -1, bool avoidDuplicates = true)
		{
			ActionMenuState newAction = CreateNew<ActionMenuState> ();
			newAction.changeType = MenuChangeType.AddJournalPage;
			newAction.menuToChange = menuName;
			newAction.elementToChange = journalElementName;
			newAction.journalText = newPageText;
			newAction.lineID = newPageTranslationID;
			newAction.journalPageIndex = pageIndexToInsertInto;
			newAction.onlyAddNewJournal = avoidDuplicates;
			return newAction;
		}


		public static ActionMenuState CreateNew_RemoveJournalPage (string menuName, string journalElementName, RemoveJournalPageMethod removeJournalPageMethod = RemoveJournalPageMethod.RemoveSinglePage, int pageIndexToRemove = -1)
		{
			ActionMenuState newAction = CreateNew<ActionMenuState> ();
			newAction.changeType = MenuChangeType.RemoveJournalPage;
			newAction.menuToChange = menuName;
			newAction.elementToChange = journalElementName;
			newAction.removeJournalPageMethod = removeJournalPageMethod;
			newAction.journalPageIndex = pageIndexToRemove;
			return newAction;
		}

	}
	
}