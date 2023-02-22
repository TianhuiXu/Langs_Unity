/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuJournal.cs"
 * 
 *	This MenuElement provides an array of labels, used to make a book.
 * 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides an array of labels, each one representing a page, that collectively form a bork.
	 * "Pages" can be added to the journal mid-game, and changes made to it will be saved in save games.
	 */
	public class MenuJournal : MenuElement, ITranslatable
	{

		/** The Unity UI Text this is linked to (Unity UI Menus only) */
		#if TextMeshProIsPresent
		public TMPro.TextMeshProUGUI uiText;
		#else
		public Text uiText;
		#endif

		/** A List of JournalPage instances that make up the pages within */
		public List<JournalPage> pages = new List<JournalPage>();
		/** The initial number of pages when the game begins */
		public int numPages = 1;
		/** The index number of the current page being shown */
		public int showPage = 1;
		/** If True, then the "Preview page" set in the Editor will be the first page open when the game begins */
		public bool startFromPage = false;
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** An ActionList to run whenever a new page is added */
		public ActionListAsset actionListOnAddPage;
		/** What type of journal this is (NewJournal, DisplayExistingJournal, DisplayActiveDocument) */
		public JournalType journalType = JournalType.NewJournal;
		/** The page offset, if journalType = JournalType.DisplayExistingJournal) */
		public int pageOffset;
		/** The name of the Journal element within the same Menu that is used as reference, if journalType = JournalType.DisplayExistingJournal) */
		public string otherJournalTitle;

		private string fullText;
		private MenuJournal otherJournal;
		private Document ownDocument;

		#if UNITY_EDITOR
		private int sideMenu;
		#endif


		public override void Declare ()
		{
			uiText = null;

			pages = new List<JournalPage>();
			pages.Add (new JournalPage ());
			numPages = 1;
			showPage = 1;
			isVisible = true;
			isClickable = false;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			fullText = "";
			actionListOnAddPage = null;
			journalType = JournalType.NewJournal;
			pageOffset = 0;
			otherJournalTitle = "";

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuJournal newElement = CreateInstance <MenuJournal>();
			newElement.Declare ();
			newElement.CopyJournal (this, fromEditor, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyJournal (MenuJournal _element, bool fromEditor, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiText = null;
			}
			else
			{
				uiText = _element.uiText;
			}

			pages = new List<JournalPage>();
			foreach (JournalPage page in _element.pages)
			{
				JournalPage newPage = new JournalPage (page);
				if (fromEditor)
				{
					newPage.lineID = -1;
				}

				pages.Add (newPage);
			}

			numPages = _element.numPages;
			startFromPage = _element.startFromPage;
			if (startFromPage)
			{
				showPage = _element.showPage;
			}
			else
			{
				showPage = 1;
			}
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			fullText = "";
			actionListOnAddPage = _element.actionListOnAddPage;
			journalType = _element.journalType;
			pageOffset = _element.pageOffset;
			otherJournalTitle = _element.otherJournalTitle;;

			base.Copy (_element);
		}


		public override void Initialise (AC.Menu _menu)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				MenuElement sharedElement = _menu.GetElementWithName (otherJournalTitle);
				if (sharedElement != null && sharedElement is MenuJournal)
				{
					otherJournal = (MenuJournal) sharedElement;
				}
			}

			base.Initialise (_menu);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			#if TextMeshProIsPresent
			uiText = LinkUIElement <TMPro.TextMeshProUGUI> (canvas);
			#else
			uiText = LinkUIElement <Text> (canvas);
			#endif
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiText)
			{
				return uiText.rectTransform;
			}
			return null;
		}


		/**
		 * <summary>Gets the currently-viewed page number.</summary>
		 * <returns>The currently-viewed page number</returms>
		 */
		public int GetCurrentPageNumber ()
		{
			return showPage;
		}


		/**
		 * <summary>Gets the total number of pages.</summary>
		 * <returns>The total number of pages</returns>
		 */
		public int GetTotalNumberOfPages ()
		{
			if (pages != null)
			{
				return pages.Count;
			}
			return 0;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuJournal)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			journalType = (JournalType) CustomGUILayout.EnumPopup ("Journal type:", journalType, apiPrefix + ".journalType", "What type of journal this is");
			if (journalType == JournalType.DisplayExistingJournal || journalType == JournalType.DisplayActiveDocument)
			{
				if (journalType == JournalType.DisplayExistingJournal)
				{
					EditorGUILayout.HelpBox ("This Journal will share pages from another Journal element in the same Menu.", MessageType.Info);
					otherJournalTitle = CustomGUILayout.TextField ("Existing element name:", otherJournalTitle, apiPrefix + ".otherJournalTitle", "The name of the Journal element within the same Menu that is used as reference");
					pageOffset = CustomGUILayout.IntField ("Page offset #:", pageOffset, apiPrefix + ".pageOffset", "The difference in page index between this and the reference Journal");
				}

				if (pages == null || pages.Count != 1)
				{
					pages.Clear ();
					pages.Add (new JournalPage ());
				}

				showPage = 1;

				if (source == MenuSource.AdventureCreator)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Placeholder text:", GUILayout.Width (146f));
					pages[0].text = CustomGUILayout.TextArea (pages[0].text, GUILayout.MaxWidth (370f), apiPrefix + ".pages[0].text");
					EditorGUILayout.EndHorizontal ();
				}
			}
			else if (journalType == JournalType.NewJournal)
			{
				if (pages == null)
				{
					pages = new List<JournalPage>();
					pages.Clear ();
					pages.Add (new JournalPage ());
				}
				numPages = pages.Count;

				for (int i=0; i<pages.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();

					if (pages[i].lineID >= 0)
					{
						CustomGUILayout.LabelField ("Page #" + (i+1).ToString () + ", Text ID #" + pages[i].lineID + ":", apiPrefix + ".pages[" + i.ToString () + "].text");
					}
					else
					{
						CustomGUILayout.LabelField ("Page #" + (i+1).ToString () + ":", apiPrefix + ".pages[" + i.ToString () + "].text");
					}

					if (GUILayout.Button ("", CustomStyles.IconCog))
					{
						sideMenu = i;
						SideMenu ();
					}
					EditorGUILayout.EndHorizontal ();

					pages[i].text = CustomGUILayout.TextArea (pages[i].text, GUILayout.MaxWidth (370f), apiPrefix + ".pages[" + i.ToString () + "].text");
					GUILayout.Box ("", GUILayout.ExpandWidth (true), GUILayout.Height(1));
				}

				if (GUILayout.Button ("Create new page", EditorStyles.miniButton))
				{
					Undo.RecordObject (this, "Create journal page");
					pages.Add (new JournalPage ());
				}

				numPages = pages.Count;

				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();

				if (numPages > 1)
				{
					showPage = CustomGUILayout.IntSlider ("Preview page #:", showPage, 1, numPages, apiPrefix + ".showPage", "The index number of the current page being shown ");
					startFromPage = CustomGUILayout.Toggle ("Start from this page?", startFromPage, apiPrefix + ".startFromPage", "If True, then the page index above will be the first open when the game begins");
				}
				else if (numPages == 1)
				{
					showPage = 1;
				}
				else
				{
					showPage = 0;
				}
			}

			if (source == MenuSource.AdventureCreator)
			{
				anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
				textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
				if (textEffects != TextEffects.None)
				{
					outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The effect thickness");
					effectColour = CustomGUILayout.ColorField ("Effect colour:", effectColour, apiPrefix + ".effectColour", "The effect colour");
				}
			}
			else
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();

				#if TextMeshProIsPresent
				uiText = LinkedUiGUI <TMPro.TextMeshProUGUI> (uiText, "Linked Text:", source);
				#else
				uiText = LinkedUiGUI <Text> (uiText, "Linked Text:", source);
				#endif
			}

			if (journalType == JournalType.NewJournal)
			{
				actionListOnAddPage = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on add page:", actionListOnAddPage, false, apiPrefix + ".actionListOnAddPage", "An ActionList to run whenever a new page is added");
			}

			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		private void SideMenu ()
		{
			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Insert after"), false, MenuCallback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, MenuCallback, "Delete");

			menu.AddSeparator ("");

			if (sideMenu > 0 || sideMenu <pages.Count-1)
			{
				menu.AddSeparator ("");
				if (sideMenu > 0)
				{
					menu.AddItem (new GUIContent ("Move to top"), false, MenuCallback, "Move to top");
					menu.AddItem (new GUIContent ("Move up"), false, MenuCallback, "Move up");
				}
				if (sideMenu < pages.Count-1)
				{
					menu.AddItem (new GUIContent ("Move down"), false, MenuCallback, "Move down");
					menu.AddItem (new GUIContent ("Move to bottom"), false, MenuCallback, "Move to bottom");
				}
			}
			
			menu.ShowAsContext ();
		}


		private void MenuCallback (object obj)
		{
			if (sideMenu >= 0)
			{
				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert journal page");
					pages.Insert (sideMenu+1, new JournalPage ());
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete journal page");
					pages.RemoveAt (sideMenu);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move page up");
					SwapPages (sideMenu, sideMenu-1);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move page down");
					SwapPages (sideMenu, sideMenu+1);
					break;

				case "Move to top":
					Undo.RecordObject (this, "Move page to top");
					MovePageToTop (sideMenu);
					break;
				
				case "Move to bottom":
					Undo.RecordObject (this, "Move page to bottom");
					MovePageToBottom (sideMenu);
					break;
				}
			}
			
			sideMenu = -1;
		}


		private void MovePageToTop (int a1)
		{
			JournalPage tempPage = pages[a1];
			pages.Insert (0, tempPage);
			pages.RemoveAt (a1+1);
		}


		private void MovePageToBottom (int a1)
		{
			JournalPage tempPage = pages[a1];
			pages.Add (tempPage);
			pages.RemoveAt (a1);
		}
		

		private void SwapPages (int a1, int a2)
		{
			JournalPage tempPage = pages[a1];
			pages[a1] = pages[a2];
			pages[a2] = tempPage;
		}
	

		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			if (pages != null)
			{
				foreach (JournalPage page in pages)
				{
					string newPageText = AdvGame.ConvertGlobalVariableTokenToLocal (page.text, oldGlobalID, newLocalID);
					if (page.text != newPageText)
					{
						return true;
					}
				}
			}
			return false;
		}


		public override int GetVariableReferences (int _varID)
		{
			int numFound = 0;
			string tokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, _varID);
			if (journalType == JournalType.NewJournal)
			{
				foreach (JournalPage page in pages)
				{
					if (page.text.Contains (tokenText))
					{
						numFound ++;
					}
				}
			}

			return numFound;
		}


		public override int UpdateVariableReferences (int oldVarID, int newVarID)
		{
			int numFound = 0;
			string oldTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, oldVarID);
			string newTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, newVarID);
			if (journalType == JournalType.NewJournal)
			{
				foreach (JournalPage page in pages)
				{
					if (page.text.Contains (oldTokenText))
					{
						page.text = page.text.Replace (oldTokenText, newTokenText);
						numFound++;
					}
				}
			}

			return numFound;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (journalType == JournalType.NewJournal && actionListOnAddPage == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiText && uiText.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (uiText && uiText.gameObject == gameObject)
			{
				return 0;
			}
			return base.GetSlotIndex (gameObject);
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			base.OnMenuTurnOn (menu);

			if (journalType == JournalType.DisplayActiveDocument)
			{
				if (KickStarter.runtimeDocuments.ActiveDocument != null)
				{
					ownDocument = KickStarter.runtimeDocuments.ActiveDocument;
					pages = ownDocument.pages;
					showPage = KickStarter.runtimeDocuments.GetLastOpenPage (ownDocument);
				}
			}
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (Application.isPlaying && journalType == JournalType.DisplayExistingJournal)
			{
				if (otherJournal != null)
				{
					int index = otherJournal.showPage + pageOffset - 1;
					if (otherJournal.pages.Count > index)
					{
						fullText = TranslatePage (otherJournal.pages[index], languageNumber);
					}
					else
					{
						fullText = string.Empty;
					}
					fullText = AdvGame.ConvertTokens (fullText, languageNumber);
				}
			}
			else
			{
				if (Application.isPlaying && journalType == JournalType.DisplayActiveDocument)
				{
					if (ownDocument != KickStarter.runtimeDocuments.ActiveDocument && KickStarter.runtimeDocuments.ActiveDocument != null)
					{
						ownDocument = KickStarter.runtimeDocuments.ActiveDocument;
						pages = ownDocument.pages;
						showPage = KickStarter.runtimeDocuments.GetLastOpenPage (ownDocument);
					}
				}

				if (pages.Count == 0)
				{
					fullText = string.Empty;
				}
				else if (pages.Count >= showPage && showPage > 0)
				{
					fullText = TranslatePage (pages[showPage - 1], languageNumber);
					fullText = AdvGame.ConvertTokens (fullText, languageNumber);
				}
			}

			if (uiText)
			{
				UpdateUIElement (uiText);
				uiText.text = fullText;
			}
		}
		

		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			
			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (pages.Count >= showPage)
			{
				if (textEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
				}
				else
				{
					GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
				}
			}
		}


		public override string GetLabel (int slot, int languageNumber)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				if (otherJournal != null)
				{
					int index = otherJournal.showPage + pageOffset - 1;
					if (index >= 0 && otherJournal.pages.Count > index)
					{
						return TranslatePage (otherJournal.pages [index], languageNumber);
					}
				}
				return "";
			}

			int i = showPage - 1;
			if (i >= 0 && pages.Count > i)
			{
				return TranslatePage (pages [i], languageNumber);
			}

			return "";
		}


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift pages in (Left, Right)</param>
		 * <param name = "doLoop">If True, then shifting right beyond the last page will display the first page, and vice-versa</param>
		 * <param name = "amount">The amount to shift pages by</param>
		 */
		public void Shift (AC_ShiftInventory shiftType, bool doLoop, int amount)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be shifted - instead its linked journal (" + otherJournalTitle + ") must be shifted instead.");
				return;
			}

			if (shiftType == AC_ShiftInventory.ShiftNext)
			{
				showPage += amount;
			}
			else if (shiftType == AC_ShiftInventory.ShiftPrevious)
			{
				showPage -= amount;
			}

			if (showPage < 1)
			{
				if (doLoop)
				{
					showPage = pages.Count;
				}
				else
				{
					showPage = 1;
				}
			}
			else if (showPage > pages.Count)
			{
				if (doLoop)
				{
					showPage = 1;
				}
				else
				{
					showPage = pages.Count;
				}
			}

			if (journalType == JournalType.DisplayActiveDocument)
			{
				if (ownDocument != null)
				{
					KickStarter.runtimeDocuments.SetLastOpenPage (ownDocument, showPage);
				}
			}

			KickStarter.eventManager.Call_OnMenuElementShift (this, shiftType);
		}


		private string TranslatePage (JournalPage page, int languageNumber)
		{
			if (Application.isPlaying)
			{
				return KickStarter.runtimeLanguages.GetTranslation (page.text, page.lineID, languageNumber, AC_TextType.JournalEntry);
			}
			return page.text;
		}

		
		protected override void AutoSize ()
		{
			string pageText = "";
			if (Application.isPlaying && journalType == JournalType.DisplayExistingJournal)
			{
				if (otherJournal != null)
				{
					int index = otherJournal.showPage + pageOffset - 1;
					if (index >= 0 && otherJournal.pages.Count > index)
					{
						pageText = otherJournal.pages [index].text;
					}
				}
			}
			else
			{
				int index = showPage - 1;
				if (index >= 0 && pages.Count > index)
				{
					pageText = pages [index].text;
				}
			}

			if (string.IsNullOrEmpty (pageText) && backgroundTexture)
			{
				GUIContent content = new GUIContent (backgroundTexture);
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (pageText);
				AutoSize (content);
			}
		}


		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (journalType == JournalType.DisplayExistingJournal || pages.Count == 0)
			{
				return false;
			}

			if (shiftType == AC_ShiftInventory.ShiftPrevious)
			{
				if (showPage == 1)
				{
					return false;
				}
			}
			else
			{
				if (pages.Count <= showPage)
				{
					return false;
				}
			}
			return true;
		}


		/**
		 * <summary>Adds a page to the journal.</summary>
		 * <param name = "newPage">The page to add</param>
		 * <param name = "onlyAddNew">If True, then the page will not be added if its lineID number matches that of any page already in the journal</param>
		 * <param name = "index">The index number to insert the page into. A value of -1 will cause it to be added at the end.<param>
		 */
		public void AddPage (JournalPage newPage, bool onlyAddNew, int index = -1)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be added to - instead its linked journal (" + otherJournalTitle + ") must be modified instead.");
				return;
			}

			if (journalType == JournalType.DisplayActiveDocument)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be added to.");
				return;
			}

			if (onlyAddNew && newPage.lineID >= 0 && pages != null && pages.Count > 0)
			{
				// Check for existing to avoid duplicates
				foreach (JournalPage page in pages)
				{
					if (page.lineID == newPage.lineID)
					{
						return;
					}
				}
			}

			if (index == -1)
			{
				index = pages.Count;
			}

			if (index < 0 || index >= pages.Count)
			{
				pages.Add (newPage);
				index = pages.IndexOf (newPage);
			}
			else
			{
				pages.Insert (index, newPage);
			}

			if (showPage > index || showPage == 0)
			{
				showPage ++;
			}

			KickStarter.eventManager.Call_OnModifyJournalPage (this, newPage, index, true);

			AdvGame.RunActionListAsset (actionListOnAddPage);
		}


		/**
		 * <summary>Removes a page from the journal.</summary>
		 * <param name = "index">The page number to remove. A value of -1 will cause the last page to be removed.<param>
		 */
		public void RemovePage (int index = -1)
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be modified - instead its linked journal (" + otherJournalTitle + ") must be modified instead.");
				return;
			}

			if (pages.Count == 0)
			{
				return;
			}

			if (index == -1)
			{
				index = pages.Count - 1;
			}

			if (index < 0)
			{
				index = pages.Count-1;
			}
			else if (index >= pages.Count)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot have it's " + index + " page removed, as it only has " + pages.Count + " pages!");
				return;
			}

			if (pages[index].lineID == -1)
			{
				ACDebug.LogWarning ("The removed Journal page has no ID number, and the change will not be included in save game files - this can be corrected by clicking 'Gather text' in the Speech Manager.");
			}

			JournalPage removedPage = pages[index];
			pages.RemoveAt (index);

			if (showPage > index)// && showPage > 1)
			{
				showPage --;
			}

			KickStarter.eventManager.Call_OnModifyJournalPage (this, removedPage, index, false);
		}


		/** Removes all page from the journal. */
		public void RemoveAllPages ()
		{
			if (journalType == JournalType.DisplayExistingJournal)
			{
				ACDebug.LogWarning ("The journal '" + title + "' cannot be modified - instead its linked journal (" + otherJournalTitle + ") must be modified instead.");
				return;
			}

			if (pages.Count == 0)
			{
				return;
			}

			pages.Clear ();
			showPage = 0;
		}


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return pages[index].text;
		}


		public int GetTranslationID (int index)
		{
			return pages[index].lineID;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			if (index < pages.Count)
			{
				pages[index].text = updatedText;
			}
		}


		public int GetNumTranslatables ()
		{
			if (pages != null) return pages.Count;
			return 0;
		}


		public bool HasExistingTranslation (int index)
		{
			return (pages[index].lineID > 0);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			pages[index].lineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return title;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.JournalEntry;
		}


		public bool CanTranslate (int index)
		{
			if (journalType == JournalType.NewJournal)
			{
				return (!string.IsNullOrEmpty (pages[index].text));
			}
			return false;
		}

		#endif

		#endregion

	}


	/** A data container for the contents of each page in a MenuJournal. */
	[System.Serializable]
	public class JournalPage
	{

		/** The translation ID, as set by SpeechManager */
		public int lineID = -1;
		/** The page text, in its original language */
		public string text = "";


		/**
		 * The default Constructor.
		 */
		public JournalPage ()
		{ }


		public JournalPage (JournalPage journalPage)
		{
			lineID = journalPage.lineID;
			text = journalPage.text;
		}


		public JournalPage (int _lineID, string _text)
		{
			lineID = _lineID;
			text = _text;
		}

	}

}