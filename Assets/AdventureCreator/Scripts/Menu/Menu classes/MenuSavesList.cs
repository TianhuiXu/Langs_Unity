
/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuSavesList.cs"
 * 
 *	This MenuElement handles the display of any saved games recorded.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;	
#endif

namespace AC
{

	/**
	 * This MenuElement lists any save game files found on by SaveSystem.
	 * Clicking on slots can load or save the relevant file, and importing variables from another game is also possible.
	 */
	public class MenuSavesList : MenuElement, ITranslatable
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** The text alignment */
		public TextAnchor anchor;
		/** How this list behaves (Load, Save, Import) */
		public AC_SaveListType saveListType;
		/** The maximum number of slots that can be displayed at once */
		public int maxSlots = 5;
		/** An ActionListAsset that can run once a game is succesfully loaded/saved/imported */
		public ActionListAsset actionListOnSave;
		/** How save files are displayed (LabelOnly, ScreenshotOnly, LabelAndScreenshot) */
		public SaveDisplayType displayType = SaveDisplayType.LabelOnly;
		/** The default graphic to use if slots display save screenshots */
		public Texture2D blankSlotTexture;

		/** The name of the project to import files from, if saveListType = AC_SaveListType.Import */
		public string importProductName;
		/** The filename syntax of import files, if saveListType = AC_SaveListType.Import */
		public string importSaveFilename;
		/** If True, and saveListType = AC_SaveListType.Import, then a specific Boolean global variable must = True for an import file to be listed */
		public bool checkImportBool;
		/** If checkImportBool = True, the ID number of the Boolean global variable that must = True, for an import file to be listed */
		public int checkImportVar;

		/** If True, then all slots will be shown even if they are not already assigned a save file. */
		public bool allowEmptySlots;
		/** If True, then only one save slot will be shown */
		public bool fixedOption;
		/** The index number of the save slot to show, if fixedOption = true */
		public int optionToShow;
		/** If >=0, The ID number of the integer ActionParameter in actionListOnSave to set to the index number of the slot clicked */
		public int parameterID = -1;

		/** The display text when a slot represents a "new save" space */
		public string newSaveText = "New save";
		/** The display text when a slot represents an empty save space */
		public string emptySlotText = "";
		/** The translation ID associated with the emptySlotText */
		public int emptySlotTextLineID = -1;
		/** If True, a slot that represents a "new save" space can be displayed if appropriate */
		public bool showNewSaveOption = true;
		/** If True, and saveListType = AC_SaveListType.Load and fixedOption = True, then the element will be hidden if the slot ID it represents is not filled with a valid save */
		public bool hideIfNotValid = false;
		/** If True, then the save file will be loaded/saved once its slot is clicked on */
		public bool autoHandle = true;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;

		private string[] labels = null;
		private bool newSaveSlot = false;
		private int eventSlot;


		public override void Declare ()
		{
			uiSlots = null;

			newSaveText = "New save";
			emptySlotText = string.Empty;
			emptySlotTextLineID = -1;
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			maxSlots = 5;

			SetSize (new Vector2 (20f, 5f));
			anchor = TextAnchor.MiddleCenter;
			saveListType = AC_SaveListType.Save;

			actionListOnSave = null;
			newSaveSlot = false;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			displayType = SaveDisplayType.LabelOnly;
			blankSlotTexture = null;

			allowEmptySlots = false;
			fixedOption = false;
			optionToShow = 1;
			hideIfNotValid = false;

			importProductName = string.Empty;
			importSaveFilename = string.Empty;
			checkImportBool = false;
			checkImportVar = 0;

			showNewSaveOption = true;
			autoHandle = true;

			parameterID = -1;
			uiHideStyle = UIHideStyle.DisableObject;
			linkUIGraphic = LinkUIGraphic.ImageComponent;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuSavesList newElement = CreateInstance <MenuSavesList>();
			newElement.Declare ();
			newElement.CopySavesList (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopySavesList (MenuSavesList _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlots = null;
			}
			else
			{
				uiSlots = new UISlot[_element.uiSlots.Length];
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i] = new UISlot (_element.uiSlots[i]);
				}
			}

			newSaveText = _element.newSaveText;
			emptySlotText = _element.emptySlotText;
			emptySlotTextLineID = _element.emptySlotTextLineID;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			anchor = _element.anchor;
			saveListType = _element.saveListType;
			maxSlots = _element.maxSlots;
			actionListOnSave = _element.actionListOnSave;
			displayType = _element.displayType;
			blankSlotTexture = _element.blankSlotTexture;
			allowEmptySlots = _element.allowEmptySlots;
			fixedOption = _element.fixedOption;
			optionToShow = _element.optionToShow;
			hideIfNotValid = _element.hideIfNotValid;
			importProductName = _element.importProductName;
			importSaveFilename = _element.importSaveFilename;
			checkImportBool = _element.checkImportBool;
			checkImportVar = _element.checkImportVar;
			parameterID = _element.parameterID;
			showNewSaveOption = _element.showNewSaveOption;
			autoHandle = _element.autoHandle;
			uiHideStyle = _element.uiHideStyle;
			linkUIGraphic = _element.linkUIGraphic;
			
			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			int i=0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements (canvas, linkUIGraphic);

				if (addEventListeners)
				{
					if (uiSlot != null && uiSlot.uiButton)
					{
						int j=i;
						uiSlot.uiButton.onClick.AddListener (() => {
							ProcessClickUI (_menu, j, KickStarter.playerInput.GetMouseState ());
						});
					}
				}
				i++;
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton && numSlots > slotIndex)
			{
				return uiSlots[slotIndex].uiButton.gameObject;
			}
			return null;
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiSlots != null && uiSlots.Length > _slot)
			{
				return uiSlots[_slot].GetRectTransform ();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuSavesList)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			saveListType = (AC_SaveListType) CustomGUILayout.EnumPopup ("List type:", saveListType, apiPrefix + ".savesListType", "How this list behaves");
			if (saveListType == AC_SaveListType.Save)
			{
				if (fixedOption || !allowEmptySlots)
				{
					showNewSaveOption = CustomGUILayout.Toggle ("Show 'New save' option?", showNewSaveOption, apiPrefix + ".showNewSaveOption", "If True, a slot that represents a 'new save' space can be displayed if appropriate");
				}

				if ((!fixedOption && allowEmptySlots) || showNewSaveOption)
				{
					newSaveText = CustomGUILayout.TextField ("'New save' text:", newSaveText, apiPrefix + ".newSaveText", "The display text when a slot represents a 'new save' space");
				}
				autoHandle = CustomGUILayout.Toggle ("Save when click on?", autoHandle, apiPrefix + ".autoHandle");
				if (autoHandle)
				{
					ActionListGUI ("ActionList after saving:", menu.title, "AfterSaving", apiPrefix, "An ActionList asset that runs after the game is saved");
				}
				else
				{
					ActionListGUI ("ActionList when click:", menu.title, "OnClick", apiPrefix, "An ActionList asset that runs after the user clicks on a save file");
				}
			}
			else if (saveListType == AC_SaveListType.Load)
			{
				if (fixedOption || allowEmptySlots)
				{
					emptySlotText = CustomGUILayout.TextField ("'Empty slot' text:", emptySlotText, apiPrefix + ".emptySlotText", "The display text when a slot is empty");
				}

				autoHandle = CustomGUILayout.Toggle ("Load when click on?", autoHandle, apiPrefix + ".autoHandle");
				if (autoHandle)
				{
					ActionListGUI ("ActionList after loading:", menu.title, "AfterLoading", apiPrefix, "An ActionList asset that runs after the game is loaded");
				}
				else
				{
					ActionListGUI ("ActionList when click:", menu.title, "OnClick", apiPrefix, "An ActionList asset that runs after the user clicks on a save file");
				}
			}
			else if (saveListType == AC_SaveListType.Import)
			{
				autoHandle = true;
				#if UNITY_STANDALONE
				importProductName = CustomGUILayout.TextField ("Import product name:", importProductName, apiPrefix + ".importProductName", "The name of the project to import files from");
				importSaveFilename = CustomGUILayout.TextField ("Import save filename:", importSaveFilename, apiPrefix + ".importSaveFilename", "The filename syntax of import files");
				ActionListGUI ("ActionList after import:", menu.title, "After_Import", apiPrefix, "An ActionList asset that runs after a save file is imported");
				checkImportBool = CustomGUILayout.Toggle ("Require Bool to be true?", checkImportBool, apiPrefix + ".checkImportBool", "If True, then a specific Boolean global variable must = True for an import file to be listed");
				if (checkImportBool)
				{
					if (KickStarter.variablesManager)
					{
						ShowVarGUI (KickStarter.variablesManager.vars);
					}
					else
					{
						EditorGUILayout.HelpBox ("A Variables Manager is required.", MessageType.Warning);
					}
				}
				#else
				EditorGUILayout.HelpBox ("This feature is only available for standalone platforms (PC, Mac, Linux)", MessageType.Warning);
				#endif
			}

			displayType = (SaveDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How save files are displayed");

			fixedOption = CustomGUILayout.Toggle ("Fixed Save ID only?", fixedOption, apiPrefix + ".fixedOption", "If True, then only one save slot will be shown");
			if (fixedOption)
			{
				numSlots = 1;
				slotSpacing = 0f;
				optionToShow = CustomGUILayout.IntField ("ID to display:", optionToShow, apiPrefix + ".optionToShow", "The ID of the save slot to show");

				if (saveListType == AC_SaveListType.Load)
				{
					hideIfNotValid = CustomGUILayout.Toggle ("Hide if no save file found?", hideIfNotValid, apiPrefix + ".hideIfNotValid", "If True, then the element will be hidden if the slot ID it represents is not filled with a valid save");
				}
			}
			else
			{
				maxSlots = CustomGUILayout.IntField ("Maximum # of slots:", maxSlots, apiPrefix + ".maxSlots", "The maximum number of slots that can be displayed at once");
				if (maxSlots < 0) maxSlots = 0;
				allowEmptySlots = CustomGUILayout.Toggle ("Allow empty slots?", allowEmptySlots, apiPrefix + ".allowEmptySlots", "If True, then all slots will be shown even if they are not already assigned a save file.");

				if (source == MenuSource.AdventureCreator)
				{
					if (allowEmptySlots)
					{
						numSlots = maxSlots;
					}
					else if (maxSlots > 1)
					{
						numSlots = CustomGUILayout.IntSlider ("Test slots:", numSlots, 1, maxSlots, apiPrefix + ".numSlots");
					}

					if (maxSlots > 1)
					{
						slotSpacing = CustomGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 30f, apiPrefix + ".slotSpacing");
						orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation");
						if (orientation == ElementOrientation.Grid)
						{
							gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
						}
					}
				}
			}

			if (source != MenuSource.AdventureCreator)
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				if (fixedOption)
				{
					uiSlots = ResizeUISlots (uiSlots, 1);
				}
				else
				{
					uiSlots = ResizeUISlots (uiSlots, maxSlots);
				}
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");

				if (displayType != SaveDisplayType.LabelOnly)
				{
					ShowTextureGUI (apiPrefix);
				}
			}
				
			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The effect thickness");
				effectColour = CustomGUILayout.ColorField ("Effect colour:", effectColour, apiPrefix + ".effectColour", "The effect colour");
			}
		}


		protected override void ShowTextureGUI (string apiPrefix)
		{
			if (displayType != SaveDisplayType.LabelOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Empty slot texture:", "The default graphic to use if slots display save screenshots"), GUILayout.Width (145f));
				blankSlotTexture = (Texture2D) EditorGUILayout.ObjectField (blankSlotTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
		}


		private void ShowVarGUI (List<GVar> vars)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int variableNumber = -1;

			if (vars != null && vars.Count > 0)
			{
				foreach (GVar _var in vars)
				{
					labelList.Add (_var.label);
					
					// If a GlobalVar variable has been removed, make sure selected variable is still valid
					if (_var.id == checkImportVar)
					{
						variableNumber = i;
					}
					
					i ++;
				}
				
				if (variableNumber == -1)
				{
					// Wasn't found (variable was deleted?), so revert to zero
					if (checkImportVar > 0) ACDebug.LogWarning ("Previously chosen variable no longer exists!");
					variableNumber = 0;
					checkImportVar = 0;
				}

				variableNumber = EditorGUILayout.Popup ("Global Variable:", variableNumber, labelList.ToArray());
				checkImportVar = vars [variableNumber].id;
			}
		}


		private void ActionListGUI (string label, string menuTitle, string suffix, string apiPrefix, string tooltip)
		{
			actionListOnSave = ActionListAssetMenu.AssetGUI (label, actionListOnSave,  menuTitle + "_" + title + "_" + suffix, apiPrefix + ".actionListOnSave",tooltip);
			
			if (actionListOnSave && actionListOnSave.NumParameters > 0)
			{
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.BeginHorizontal ();
				parameterID = Action.ChooseParameterGUI (string.Empty, actionListOnSave.DefaultParameters, parameterID, ParameterType.Integer);

				bool found = false;
				foreach (ActionParameter _parameter in actionListOnSave.DefaultParameters)
				{
					if (_parameter.parameterType == ParameterType.Integer)
					{
						found = true;
					}
				}
				if (found)
				{
					if (fixedOption || allowEmptySlots)
					{
						EditorGUILayout.LabelField ("(= Save ID #)");
					}
					else
					{
						EditorGUILayout.LabelField ("(= Slot index)");
					}
				}
				EditorGUILayout.EndHorizontal ();
				CustomGUILayout.EndVertical ();
			}
		}


		public override int GetVariableReferences (int _varID)
		{
			int numFound = 0;
			string tokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, _varID);

			switch (saveListType)
			{
				case AC_SaveListType.Save:
					if (newSaveText.ToLower ().Contains (tokenText))
					{
						numFound++;
					}
					break;

				case AC_SaveListType.Load:
					if (emptySlotText.ToLower ().Contains (tokenText))
					{
						numFound++;
					}
					break;

				case AC_SaveListType.Import:
					if (checkImportBool && checkImportVar == _varID)
					{
						numFound++;
					}
					break;

				default:
					break;
			}

			return numFound;
		}


		public override int UpdateVariableReferences (int oldVarID, int newVarID)
		{
			int numFound = 0;
			string oldTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, oldVarID);
			string newTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, newVarID);

			switch (saveListType)
			{
				case AC_SaveListType.Save:
					if (newSaveText.ToLower ().Contains (oldTokenText))
					{
						newSaveText = newSaveText.Replace (oldTokenText, newTokenText);
						numFound++;
					}
					break;

				case AC_SaveListType.Load:
					if (emptySlotText.ToLower ().Contains (oldTokenText))
					{
						emptySlotText = emptySlotText.Replace (oldTokenText, newTokenText);
						numFound++;
					}
					break;

				case AC_SaveListType.Import:
					if (checkImportBool && checkImportVar == oldVarID)
					{
						checkImportVar = newVarID;
						numFound++;
					}
					break;

				default:
					break;
			}

			return numFound;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListOnSave == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			foreach (UISlot uiSlot in uiSlots)
			{
				if (uiSlot.uiButton && uiSlot.uiButton.gameObject == gameObject) return true;
				if (uiSlot.uiButtonID == id && id != 0) return true;
			}
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			for (int i = 0; i < uiSlots.Length; i++)
			{
				if (uiSlots[i].uiButton && uiSlots[i].uiButton == gameObject)
				{
					return 0;
				}
			}
			return base.GetSlotIndex (gameObject);
		}

		
		/**
		 * <summary>Gets the SaveFile associated with a given slot</summary>
		 * <param name = "_slot">The index of the slot</param>
		 * <returns>The SaveFile associated with the slot</returns>
		 */
		public SaveFile GetSaveFile (int _slot)
		{
			int saveID = GetOptionID (_slot);
			return KickStarter.saveSystem.GetSaveFile (saveID);
		}


		public override string GetLabel (int _slot, int languageNumber)
		{
			if (saveListType == AC_SaveListType.Save)
			{
				if (newSaveSlot)
				{
					if (fixedOption)
					{
						if (!SaveSystem.DoesSaveExist (optionToShow))
						{
							return TranslateLabel (newSaveText, lineID, languageNumber);
						}
					}
					else
					{
						if ((_slot + offset) == KickStarter.saveSystem.GetNumSaves ())
						{
							return TranslateLabel (newSaveText, lineID, languageNumber);
						}
					}
				}
				else if (!fixedOption && allowEmptySlots)
				{
					if (!SaveSystem.DoesSaveExist (_slot + offset))
					{
						return TranslateLabel (newSaveText, lineID, languageNumber);
					}
				}
			}
			else if (saveListType == AC_SaveListType.Load)
			{
				string label = SaveSystem.GetSaveSlotLabel (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
				if (string.IsNullOrEmpty (label) && (fixedOption || allowEmptySlots))
				{
					label = TranslateLabel (emptySlotText, emptySlotTextLineID, languageNumber);
				}
				return label;
			}
			return SaveSystem.GetSaveSlotLabel (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
		}


		private int GetOptionID (int _slot)
		{
			if (fixedOption)
			{
				return optionToShow;
			}
			return _slot + offset;
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiSlots != null && slotIndex >= 0 && uiSlots.Length > slotIndex && uiSlots[slotIndex] != null && uiSlots[slotIndex].uiButton)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiSlots[slotIndex].uiButton.gameObject);
			}
			return false;
		}


		public override bool IsSelectableInteractable (int slotIndex)
		{
			if (uiSlots != null && slotIndex >= 0 && uiSlots.Length > slotIndex && uiSlots[slotIndex] != null && uiSlots[slotIndex].uiButton)
			{
				return uiSlots[slotIndex].uiButton.IsInteractable ();
			}
			return false;
		}


		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (displayType != SaveDisplayType.ScreenshotOnly)
			{
				string fullText = string.Empty;

				if (newSaveSlot && saveListType == AC_SaveListType.Save)
				{
					if (!fixedOption && (_slot + offset) == KickStarter.saveSystem.GetNumSaves ())
					{
						fullText = TranslateLabel (newSaveText, lineID, languageNumber);
					}
					else if (fixedOption)
					{
						fullText = TranslateLabel (newSaveText, lineID, languageNumber);
					}
					else
					{
						fullText = SaveSystem.GetSaveSlotLabel (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
					}
				}
				else if (saveListType == AC_SaveListType.Save && !fixedOption && allowEmptySlots)
				{
					if (SaveSystem.DoesSaveExist (GetOptionID (_slot)))
					{
						fullText = SaveSystem.GetSaveSlotLabel (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
					}
					else
					{
						fullText = TranslateLabel (newSaveText, lineID, languageNumber);
					}
				}
				else
				{
					if (saveListType == AC_SaveListType.Import)
					{
						fullText = SaveSystem.GetImportSlotLabel (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
					}
					else
					{
						fullText = SaveSystem.GetSaveSlotLabel (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);

						if (string.IsNullOrEmpty (fullText) && saveListType == AC_SaveListType.Load && (fixedOption || allowEmptySlots))
						{
							fullText = TranslateLabel (emptySlotText, emptySlotTextLineID, languageNumber);
						}
					}
				}

				if (!Application.isPlaying)
				{
					if (labels == null || labels.Length != numSlots)
					{
						labels = new string [numSlots];
					}
				}

				if (_slot < labels.Length)
					labels [_slot] = fullText;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					if (saveListType == AC_SaveListType.Load && fixedOption && hideIfNotValid)
					{
						if (!SaveSystem.DoesSaveExist (optionToShow))
						{
							LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
							return;
						}
						else
						{
							LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);
						}
					}

					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);
					
					if (displayType != SaveDisplayType.LabelOnly)
					{
						Texture2D tex = null;
						if (saveListType == AC_SaveListType.Import)
						{
							tex = SaveSystem.GetImportSlotScreenshot (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
						}
						else
						{
							tex = SaveSystem.GetSaveSlotScreenshot (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
						}
						if (tex == null)
						{
							tex = blankSlotTexture;
						}
						uiSlots[_slot].SetImage (tex);
					}
					if (displayType != SaveDisplayType.ScreenshotOnly)
					{
						uiSlots[_slot].SetText (labels [_slot]);
					}
				}
			}
		}
		

		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			_style.wordWrap = true;

			if (displayType != SaveDisplayType.LabelOnly)
			{
				Texture2D tex = null;
				if (saveListType == AC_SaveListType.Import)
				{
					tex = SaveSystem.GetImportSlotScreenshot (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
				}
				else
				{
					tex = SaveSystem.GetSaveSlotScreenshot (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
				}
				if (tex == null && blankSlotTexture)
				{
					tex = blankSlotTexture;
				}

				if (tex)
				{
					GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), tex, ScaleMode.StretchToFill, true, 0f);
				}
			}

			if (displayType != SaveDisplayType.ScreenshotOnly)
			{
				_style.alignment = anchor;
				if (zoom < 1f)
				{
					_style.fontSize = (int) ((float) _style.fontSize * zoom);
				}

				#if UNITY_EDITOR
				if (!Application.isPlaying && labels == null) PreDisplay (_slot, 0, isActive);
				#endif
				
				if (textEffects != TextEffects.None)
				{
					AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
				}
				else
				{
					GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
				}
			}
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}

			eventSlot = _slot;
			ClearAllEvents ();

			switch (saveListType)
			{
				case AC_SaveListType.Save:
					if (autoHandle)
					{
						if (PlayerMenus.IsSavingLocked (null, true))
						{
							return false;
						}

						EventManager.OnFinishSaving += OnCompleteSave;
						EventManager.OnFailSaving += OnFailSaveLoad;

						if (newSaveSlot && _slot == (numSlots - 1))
						{
							SaveSystem.SaveNewGame ();

							if (KickStarter.settingsManager.orderSavesByUpdateTime)
							{
								offset = 0;
							}
							else
							{
								Shift (AC_ShiftInventory.ShiftNext, 1);
							}
						}
						else
						{
							SaveSystem.SaveGame (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
						}
					}
					else
					{
						RunActionList (_slot);
					}
					break;

				case AC_SaveListType.Load:
					if (autoHandle)
					{
						EventManager.OnFinishLoading += OnCompleteLoad;
						EventManager.OnFailLoading += OnFailSaveLoad;

						SaveSystem.LoadGame (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
					}
					else
					{
						RunActionList (_slot);
					}
					break;

				case AC_SaveListType.Import:
					EventManager.OnFinishImporting += OnCompleteImport;
					EventManager.OnFailImporting += OnFailImport;

					SaveSystem.ImportGame (_slot + offset, GetOptionID (_slot), fixedOption || allowEmptySlots);
					break;
			}
	
			return base.ProcessClick (_menu, _slot, _mouseState);
		}


		private void OnCompleteSave (SaveFile saveFile)
		{
			ClearAllEvents ();

			if (autoHandle)
			{
				parentMenu.TurnOff (true);
			}

			RunActionList (eventSlot);
		}


		private void OnCompleteLoad ()
		{
			ClearAllEvents ();
			if (autoHandle)
			{
				parentMenu.TurnOff (false);
			}

			RunActionList (eventSlot);
		}


		private void OnCompleteImport ()
		{
			ClearAllEvents ();

			RunActionList (eventSlot);
		}


		private void OnFailSaveLoad (int saveID)
		{
			ClearAllEvents ();

			if (!autoHandle)
			{
				RunActionList (eventSlot);
			}
		}


		private void OnFailImport ()
		{
			ClearAllEvents ();
		}


		private void ClearAllEvents ()
		{
			EventManager.OnFinishSaving -= OnCompleteSave;
			EventManager.OnFailSaving -= OnFailSaveLoad;

			EventManager.OnFinishLoading -= OnCompleteLoad;
			EventManager.OnFailLoading -= OnFailSaveLoad;

			EventManager.OnFinishImporting -= OnCompleteImport;
			EventManager.OnFailImporting -= OnFailImport;
		}


		private void RunActionList (int _slot)
		{
			if (fixedOption)
			{
				AdvGame.RunActionListAsset (actionListOnSave, parameterID, optionToShow);
			}
			else
			{
				AdvGame.RunActionListAsset (actionListOnSave, parameterID, _slot + offset);
			}
		}


		public override void RecalculateSize (MenuSource source)
		{
			newSaveSlot = false;
			if (Application.isPlaying)
			{
				if (saveListType == AC_SaveListType.Import)
				{
					if (checkImportBool)
					{
						KickStarter.saveSystem.GatherImportFiles (importProductName, importSaveFilename, checkImportVar);
					}
					else
					{
						KickStarter.saveSystem.GatherImportFiles (importProductName, importSaveFilename, -1);
					}
				}

				if (fixedOption)
				{
					numSlots = 1;

					if (saveListType == AC_SaveListType.Save)
					{
						newSaveSlot = !SaveSystem.DoesSaveExist (optionToShow);
					}
				}
				else if (allowEmptySlots)
				{
					numSlots = maxSlots;
					offset = Mathf.Min (offset, GetMaxOffset ());
				}
				else
				{
					if (saveListType == AC_SaveListType.Import)
					{
						numSlots = SaveSystem.GetNumImportSlots ();
					}
					else
					{
						numSlots = SaveSystem.GetNumSlots ();

						if (saveListType == AC_SaveListType.Save &&
							numSlots < KickStarter.settingsManager.maxSaves &&
							showNewSaveOption)
						{
							newSaveSlot = true;
							numSlots ++;
						}
					}

					if (numSlots > maxSlots)
					{
						numSlots = maxSlots;
					}

					offset = Mathf.Min (offset, GetMaxOffset ());
				}
			}

			if (Application.isPlaying || labels == null || labels.Length != numSlots)
			{
				labels = new string [numSlots];
			}

			if (Application.isPlaying && uiSlots != null)
			{
				ClearSpriteCache (uiSlots);
			}

			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}

			base.RecalculateSize (source);
		}
		
		
		protected override void AutoSize ()
		{
			if (displayType == SaveDisplayType.ScreenshotOnly)
			{
				if (blankSlotTexture)
				{
					AutoSize (new GUIContent (blankSlotTexture));
				}
				else
				{
					AutoSize (GUIContent.none);
				}
			}
			else if (displayType == SaveDisplayType.LabelAndScreenshot)
			{
				if (blankSlotTexture)
				{
					AutoSize (new GUIContent (blankSlotTexture));
				}
				else
				{
					AutoSize (new GUIContent (SaveSystem.GetSaveSlotLabel (0, optionToShow, fixedOption)));
				}
			}
			else
			{
				AutoSize (new GUIContent (SaveSystem.GetSaveSlotLabel (0, optionToShow, fixedOption)));
			}
		}


		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0 || fixedOption)
			{
				return false;
			}
			if (shiftType == AC_ShiftInventory.ShiftPrevious)
			{
				if (offset == 0)
				{
					return false;
				}
			}
			else
			{
				if (offset >= GetMaxOffset ())
				{
					return false;
				}
			}
			return true;
		}


		private int GetMaxOffset ()
		{
			if (numSlots == 0 || fixedOption)
			{
				return 0;
			}

			return Mathf.Max (0, GetNumFilledSlots () - maxSlots);
		}


		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (fixedOption) return;

			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, GetNumFilledSlots (), amount);
			}
		}


		private int GetNumFilledSlots ()
		{
			if (!fixedOption && allowEmptySlots)
			{
				return KickStarter.settingsManager.maxSaves;
			}
			if (saveListType == AC_SaveListType.Save && !fixedOption && newSaveSlot && showNewSaveOption)
			{
				return KickStarter.saveSystem.GetNumSaves () + 1;
			}
			return KickStarter.saveSystem.GetNumSaves ();
		}


		private string TranslateLabel (string label, int _lineID, int languageNumber)
		{
			if (KickStarter.runtimeLanguages == null)
			{
				return label;
			}
			return KickStarter.runtimeLanguages.GetTranslation (label, _lineID, languageNumber, GetTranslationType (0));
		}


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			if (index == 1)
			{
				return emptySlotText;
			}
			return newSaveText;
		}


		public int GetTranslationID (int index)
		{
			if (index == 1)
			{
				return emptySlotTextLineID;
			}
			return lineID;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC.AC_TextType.MenuElement;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			if (index == 1)
			{
				emptySlotText = updatedText;
			}
			else
			{
				newSaveText = updatedText;
			}
		}


		public int GetNumTranslatables ()
		{
			return 2;
		}


		public bool HasExistingTranslation (int index)
		{
			if (index == 1)
			{
				return (emptySlotTextLineID > 0);
			}
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			if (index == 1)
			{
				emptySlotTextLineID = _lineID;
			}
			else
			{
				lineID = _lineID;
			}
		}


		public string GetOwner (int index)
		{
			return title;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public bool CanTranslate (int index)
		{
			if (index == 1)
			{
				if (saveListType == AC_SaveListType.Load)
				{
					if (fixedOption || allowEmptySlots)
					{
						return !string.IsNullOrEmpty (emptySlotText);
					}
				}
				return false;
			}
			else
			{
				if (saveListType == AC_SaveListType.Save && showNewSaveOption)
				{
					if ((!fixedOption && allowEmptySlots) || showNewSaveOption)
					{
						return !string.IsNullOrEmpty (newSaveText);
					}
				}
				return false;
			}
		}

		#endif

		#endregion

	}

}