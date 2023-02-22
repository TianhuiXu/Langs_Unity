
/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuCycle.cs"
 * 
 *	This MenuElement is like a label, only its text cycles through an array when clicked on.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that displays different text each time it is clicked on.
	 * The index number of the text array it displays can be linked to a Global Variable (GVar), a custom script, or the game's current language.
	 */
	public class MenuCycle : MenuElement, ITranslatable
	{

		/** The Unity UI Button this is linked to (Unity UI Menus only) */
		public UnityEngine.UI.Button uiButton;

		/** The Unity UI Dropdown this is linked to (Unity UI Menus only) */
		public Dropdown uiDropdown;

		/** The ActionListAsset to run when the element is clicked on */
		public ActionListAsset actionListOnClick = null;
		/** The text that's displayed on-screen, which prefixes the varying text */
		public string label = "Element";
		/** A string to append to the label, before the value */
		public string labelSuffix = defaultLabelSuffix;
		private const string defaultLabelSuffix = " : ";
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** The text alignment */
		public TextAnchor anchor;
		/** The index number of the currently-shown text in optionsArray */
		public int selected;
		/** An array of texts that the element can show one at a time */
		public List<string> optionsArray = new List<string>();
		/** What the text links to (CustomScript, GlobalVariable, Language) */
		public AC_CycleType cycleType;
		/** What kind of language to affect, if cycleType = AC_CycleType.Language and SpeechManager.separateVoiceAndTextLanguages = True */
		public SplitLanguageType splitLanguageType = SplitLanguageType.TextAndVoice;
		/** The ID number of the linked GlobalVariable, if cycleType = AC_CycleType.GlobalVariable */
		public int varID;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** What kind of UI element the Cycle is linked to, if the Menu's Source is UnityUiPrefab or UnityUiInScene (Button, Dropdown) */
		public CycleUIBasis cycleUIBasis = CycleUIBasis.Button;

		/** An array of textures to replace the background according to the current choice (optional) */
		public Texture2D[] optionTextures = new Texture2D[0];
		private RawImage rawImage;

		/** If True, then a right-click will cycle backwards */
		public bool rightClickGoesBack = false;

		private GVar linkedVariable;
		private string cycleText;

		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif


		public override void Declare ()
		{
			uiText = null;
			uiButton = null;
			label = "Cycle";
			labelSuffix = defaultLabelSuffix;
			selected = 0;
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			SetSize (new Vector2 (15f, 5f));
			anchor = TextAnchor.MiddleLeft;
			cycleType = AC_CycleType.CustomScript;
			splitLanguageType = SplitLanguageType.TextAndVoice;
			varID = 0;
			optionsArray = new List<string>();
			cycleText = string.Empty;
			actionListOnClick = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			cycleUIBasis = CycleUIBasis.Button;
			optionTextures = new Texture2D[0];
			rawImage = null;
			linkedVariable = null;
			uiDropdown = null;
			rightClickGoesBack = false;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuCycle newElement = CreateInstance <MenuCycle>();
			newElement.Declare ();
			newElement.CopyCycle (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyCycle (MenuCycle _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiButton = null;
			}
			else
			{
				uiButton = _element.uiButton;
			}
			uiText = null;

			label = _element.label;
			labelSuffix = _element.labelSuffix;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			anchor = _element.anchor;
			selected = _element.selected;
			
			optionsArray = new List<string>();
			foreach (string option in _element.optionsArray)
			{
				optionsArray.Add (option);
			}
			
			cycleType = _element.cycleType;
			splitLanguageType = _element.splitLanguageType;
			varID = _element.varID;
			cycleText = string.Empty;
			actionListOnClick = _element.actionListOnClick;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			cycleUIBasis = _element.cycleUIBasis;
			optionTextures = _element.optionTextures;
			linkedVariable = null;
			uiDropdown = _element.uiDropdown;
			rightClickGoesBack = _element.rightClickGoesBack;

			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			rawImage = null;

			if (_menu.menuSource != MenuSource.AdventureCreator)
			{
				if (cycleUIBasis == CycleUIBasis.Button)
				{
					uiButton = LinkUIElement <UnityEngine.UI.Button> (canvas);
					if (uiButton)
					{
						rawImage = uiButton.GetComponentInChildren <RawImage>();

						#if TextMeshProIsPresent
						uiText = uiButton.GetComponentInChildren <TMPro.TextMeshProUGUI>();
						#else
						uiText = uiButton.GetComponentInChildren <Text>();
						#endif

						if (addEventListeners)
						{
							uiButton.onClick.AddListener (() => {
								ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
							});

							if (rightClickGoesBack)
							{
								UISlotClickRight uiSlotRightClick = uiButton.gameObject.GetComponent<UISlotClickRight> ();
								if (uiSlotRightClick == null)
								{
									uiSlotRightClick = uiButton.gameObject.AddComponent<UISlotClickRight> ();
								}
								uiSlotRightClick.Setup (_menu, this, 0);
							}
						}

						CreateHoverSoundHandler (uiButton, _menu, 0);
					}
				}
				else if (cycleUIBasis == CycleUIBasis.Dropdown)
				{
					uiDropdown = LinkUIElement <Dropdown> (canvas);
					if (uiDropdown)
					{
						uiDropdown.value = selected;

						if (addEventListeners)
						{
							uiDropdown.onValueChanged.AddListener (delegate {
								uiDropdownValueChangedHandler (uiDropdown);
							});
						}

						CreateHoverSoundHandler (uiDropdown, _menu, 0);
		 			}
				}
			}
		}


		private void uiDropdownValueChangedHandler (Dropdown _dropdown)
		{
			ProcessClickUI (parentMenu, 0, KickStarter.playerInput.GetMouseState ());
		}


		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiButton)
			{
				return uiButton.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiButton)
			{
				uiButton.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuCycle)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			if (source != AC.MenuSource.AdventureCreator)
			{
				cycleUIBasis = (CycleUIBasis) CustomGUILayout.EnumPopup ("UI basis:", cycleUIBasis, apiPrefix + ".cycleUIBasis", "What kind of UI element the Cycle is linked to");

				if (cycleUIBasis == CycleUIBasis.Button)
				{
					uiButton = LinkedUiGUI <UnityEngine.UI.Button> (uiButton, "Linked Button:", source, "The Unity UI Button this is linked to");
				}
				else if (cycleUIBasis == CycleUIBasis.Dropdown)
				{
					uiDropdown = LinkedUiGUI <Dropdown> (uiDropdown, "Linked Dropdown:", source);
				}
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
			}

			cycleType = (AC_CycleType) CustomGUILayout.EnumPopup ("Cycle type:", cycleType, apiPrefix + ".cycleType", "What the value links to");

			if (cycleType == AC_CycleType.Language && KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages)
			{
				splitLanguageType = (SplitLanguageType) CustomGUILayout.EnumPopup ("Language type:", splitLanguageType, apiPrefix + ".splitLanguageType", "What kind of language this affects");
			}

			if (source == MenuSource.AdventureCreator || cycleUIBasis == CycleUIBasis.Button)
			{
				label = CustomGUILayout.TextField ("Label text:", label, apiPrefix + ".label", "The text that's displayed on-screen, which prefixes the varying text");
				if (!string.IsNullOrEmpty (label))
				{
					labelSuffix = CustomGUILayout.TextField ("Label suffix:", labelSuffix, apiPrefix + ".labelSuffix", "A string to append to the label, before the value");
				}
			}

			GVar popUpVariable = null;
			if (cycleType == AC_CycleType.CustomScript || cycleType == AC_CycleType.Variable)
			{
				bool showOptionsGUI = true;

				if (cycleType == AC_CycleType.Variable)
				{
					VariableType[] allowedVarTypes = new VariableType[2];
					allowedVarTypes[0] = VariableType.Integer;
					allowedVarTypes[1] = VariableType.PopUp;

					varID = AdvGame.GlobalVariableGUI ("Global variable:", varID, allowedVarTypes, "The Global PopUp or Integer variable that's value will be synced with the cycle");

					if (AdvGame.GetReferences ().variablesManager && AdvGame.GetReferences ().variablesManager.GetVariable (varID) != null && AdvGame.GetReferences ().variablesManager.GetVariable (varID).type == VariableType.PopUp)
					{
						popUpVariable = AdvGame.GetReferences ().variablesManager.GetVariable (varID);
						showOptionsGUI = false;
					}
				}

				if (showOptionsGUI)
				{
					int numOptions = optionsArray.Count;
					numOptions = EditorGUILayout.IntField ("Number of choices:", optionsArray.Count);
					if (numOptions < 0)
					{
						numOptions = 0;
					}
					
					if (numOptions < optionsArray.Count)
					{
						optionsArray.RemoveRange (numOptions, optionsArray.Count - numOptions);
					}
					else if (numOptions > optionsArray.Count)
					{
						if (numOptions > optionsArray.Capacity)
						{
							optionsArray.Capacity = numOptions;
						}
						for (int i=optionsArray.Count; i<numOptions; i++)
						{
							optionsArray.Add (string.Empty);
						}
					}
					
					for (int i=0; i<optionsArray.Count; i++)
					{
						optionsArray [i] = CustomGUILayout.TextField ("Choice #" + i.ToString () + ":", optionsArray [i], apiPrefix + ".optionsArray[" + i.ToString () + "]");
					}
				}
				
				if (cycleType == AC_CycleType.CustomScript)
				{
					if (optionsArray.Count > 0)
					{
						selected = CustomGUILayout.IntField ("Default option #:", selected, apiPrefix + ".selected");
					}
					ShowClipHelp ();
				}

				actionListOnClick = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on click:", actionListOnClick, false, apiPrefix + ".actionListOnClick", "The ActionList asset to run when the element is clicked on");
			}

			if (source == MenuSource.AdventureCreator || cycleUIBasis == CycleUIBasis.Button)
			{
				rightClickGoesBack = CustomGUILayout.Toggle ("Right-click cycles back?", rightClickGoesBack, apiPrefix + ".rightClickGoesBack", "If True, then right-clicks or InteractionB button presses will cycle the element backwards.");
			}
			alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");

			bool showOptionTextures = (optionTextures.Length > 0);
			if (menu.menuSource != MenuSource.AdventureCreator && cycleUIBasis == CycleUIBasis.Dropdown)
			{
				showOptionTextures = false;
			}
			showOptionTextures = EditorGUILayout.Toggle ("Per-option textures?", showOptionTextures);
			if (showOptionTextures)
			{
				int numOptions = (cycleType == AC_CycleType.Language) ? KickStarter.speechManager.Languages.Count : optionsArray.Count;
				if (cycleType == AC_CycleType.Language)
				{
					numOptions = 0;
					if (KickStarter.speechManager && KickStarter.speechManager.Languages != null)
					{
						numOptions = KickStarter.speechManager.Languages.Count;
					}
				}
				else if (popUpVariable != null)
				{
					numOptions = popUpVariable.GetNumPopUpValues ();
				}
				if (optionTextures.Length != numOptions)
				{
					optionTextures = new Texture2D[numOptions];
				}
			}
			else
			{
				optionTextures = new Texture2D[0];
			}
			ChangeCursorGUI (menu);
			CustomGUILayout.EndVertical ();

			if (showOptionTextures)
			{
				CustomGUILayout.BeginVertical ();
				for (int i=0; i<optionTextures.Length; i++)
				{
					string label = (popUpVariable != null) ? ("'" + popUpVariable.GetPopUpForIndex (i) + "' texture:") : ("Option #" + i + " texture:");
					optionTextures[i] = (Texture2D) EditorGUILayout.ObjectField (label, optionTextures[i], typeof (Texture2D), false);
				}
				if (menu.menuSource != MenuSource.AdventureCreator)
				{
					EditorGUILayout.HelpBox ("Per-option textures require a RawImage component on the linked Button.", MessageType.Info);
				}
				CustomGUILayout.EndVertical ();
			}

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


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			if (cycleType == AC_CycleType.Variable && varID == oldGlobalID)
			{
				return true;
			}
			return false;
		}


		public override int GetVariableReferences (int _varID)
		{
			int numFound = 0;
			string tokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, _varID);
			if (label.ToLower ().Contains (tokenText))
			{
				numFound ++;
			}

			switch (cycleType)
			{
				case AC_CycleType.Variable:
					if (varID == _varID)
					{
						numFound ++;
					}
					break;

				case AC_CycleType.Language:
				case AC_CycleType.CustomScript:
					foreach (string optionLabel in optionsArray)
					{
						if (optionLabel.Contains (tokenText))
						{
							numFound++;
						}
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
			if (label.ToLower ().Contains (oldTokenText))
			{
				label = label.Replace (oldTokenText, newTokenText);
				numFound++;
			}

			switch (cycleType)
			{
				case AC_CycleType.Variable:
					if (varID == oldVarID)
					{
						varID = newVarID;
						numFound++;
					}
					break;

				case AC_CycleType.Language:
				case AC_CycleType.CustomScript:
					for (int i = 0; i < optionsArray.Count; i++)
					{
						if (optionsArray[i].Contains (oldTokenText))
						{
							optionsArray[i] = optionsArray[i].Replace (oldTokenText, newTokenText);
							numFound++;
						}
					}
					break;

				default:
					break;
			}
			return numFound;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if ((cycleType == AC_CycleType.CustomScript || cycleType == AC_CycleType.Variable) && actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (cycleUIBasis == CycleUIBasis.Button && uiButton && uiButton.gameObject == gameObject) return true;
			if (cycleUIBasis == CycleUIBasis.Dropdown && uiDropdown && uiDropdown.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (cycleUIBasis == CycleUIBasis.Button && uiButton && uiButton.gameObject == gameObject)
			{
				return 0;
			}
			if (cycleUIBasis == CycleUIBasis.Dropdown && uiDropdown && uiDropdown.gameObject == gameObject)
			{
				return 0;
			}
			return base.GetSlotIndex (gameObject);
		}


		protected override string GetLabelToTranslate ()
		{
			return label;
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			//CalculateValue ();

			cycleText = TranslateLabel (languageNumber);
			if (!string.IsNullOrEmpty (cycleText))
			{
				cycleText += labelSuffix;
			}

			if (Application.isPlaying && uiDropdown)
			{
				cycleText = string.Empty;
			}

			cycleText += GetOptionLabel (selected);

			if (Application.isPlaying && optionTextures.Length > 0 && selected < optionTextures.Length && optionTextures[selected])
			{
				backgroundTexture = optionTextures[selected];
				if (rawImage)
				{
					rawImage.texture = backgroundTexture;
				}
			}

			if (uiButton)
			{
				if (uiText)
				{
					uiText.text = cycleText;
				}
				UpdateUISelectable (uiButton, uiSelectableHideStyle);
			}
			else
			{
				if (uiDropdown && Application.isPlaying)
				{
					uiDropdown.value = selected;
					UpdateUISelectable (uiDropdown, uiSelectableHideStyle);
				}
			}
		}


		private string GetOptionLabel (int index)
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying && cycleType == AC_CycleType.Language)
			{
				optionsArray = new List<string> ();
				for (int i = 0; i < AdvGame.GetReferences ().speechManager.Languages.Count; i++)
				{
					optionsArray.Add (AdvGame.GetReferences ().speechManager.Languages[i].name);
				}
			}
			#endif

			if (index >= 0 && index < GetNumOptions ())
			{
				if (cycleType == AC_CycleType.Variable && linkedVariable != null && linkedVariable.type == VariableType.PopUp)
				{
					return linkedVariable.GetPopUpForIndex (index, Options.GetLanguage ());
				}
				return optionsArray [index];
			}

			if (Application.isPlaying)
			{
				ACDebug.Log ("Could not gather options for MenuCycle " + label);
				return string.Empty;
			}
			return "Default option";
		}


		private int GetNumOptions ()
		{
			if (!Application.isPlaying && cycleType == AC_CycleType.Variable && (linkedVariable == null || linkedVariable.id != varID))
			{
				if (AdvGame.GetReferences ().variablesManager)
				{
					linkedVariable = AdvGame.GetReferences ().variablesManager.GetVariable (varID);
				}
			}

			if (cycleType == AC_CycleType.Variable && linkedVariable != null && linkedVariable.type == VariableType.PopUp)
			{
				return linkedVariable.GetNumPopUpValues ();
			}
			return optionsArray.Count;
		}


		private void CycleOption ()
		{
			selected ++;
			if (selected >= GetNumOptions ())
			{
				selected = 0;
			}
		}


		private void CycleOptionBack ()
		{
			selected--;
			if (selected < 0)
			{
				selected = GetNumOptions () - 1;
			}
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), cycleText, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), cycleText, _style);
			}
		}
		

		public override string GetLabel (int slot, int languageNumber)
		{
			string optionLabel = GetOptionLabel (selected);
			string prefixLabel = TranslateLabel (languageNumber);

			if (!string.IsNullOrEmpty (prefixLabel) && !string.IsNullOrEmpty (optionLabel))
			{
				return prefixLabel + labelSuffix + optionLabel;
			}
			else if (!string.IsNullOrEmpty (optionLabel))
			{
				return optionLabel;
			}
			return prefixLabel;
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiButton)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiButton.gameObject);
			}
			return false;
		}


		public override bool IsSelectableInteractable (int slotIndex)
		{
			if (uiButton)
			{
				return uiButton.IsInteractable ();
			}
			return false;
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return false;
			}

			if (uiDropdown)
			{
				selected = uiDropdown.value;
			}
			else if (_mouseState == MouseState.RightClick && rightClickGoesBack)
			{
				CycleOptionBack ();
			}
			else
			{
				CycleOption ();
			}

			switch (cycleType)
			{
				case AC_CycleType.Language:
					int trueIndex = KickStarter.runtimeLanguages.EnabledLanguageToTrueIndex (selected);
					if (KickStarter.speechManager && KickStarter.speechManager.separateVoiceAndTextLanguages)
					{
						switch (splitLanguageType)
						{
							case SplitLanguageType.TextAndVoice:
								Options.SetLanguage (trueIndex);
								Options.SetVoiceLanguage (trueIndex);
								break;

							case SplitLanguageType.TextOnly:
								Options.SetLanguage (trueIndex);
								break;

							case SplitLanguageType.VoiceOnly:
								Options.SetVoiceLanguage (trueIndex);
								break;
						}
					}
					else
					{
						Options.SetLanguage (trueIndex);
					}
					break;

				case AC_CycleType.Variable:
					if (linkedVariable != null)
					{
						linkedVariable.IntegerValue = selected;
						linkedVariable.Upload ();
					}
					break;

				case AC_CycleType.CustomScript:
					MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
					break;

				default:
					break;
			}
	
			if (actionListOnClick)
			{
				AdvGame.RunActionListAsset (actionListOnClick);
			}
			
			return base.ProcessClick (_menu, _slot, _mouseState);
		}


		public override void RecalculateSize (MenuSource source)
		{
			CalculateValue ();

			if (Application.isPlaying && uiDropdown)
			{
				if (uiDropdown.captionText)
				{
					string _label = GetOptionLabel (selected);
					if (!string.IsNullOrEmpty (_label))
					{
						uiDropdown.captionText.text = _label;
					}
				}

				int numOptions = GetNumOptions ();

				if (Application.isPlaying)
				{
					if (uiDropdown.options.Count < numOptions)
					{
						while (uiDropdown.options.Count < numOptions)
						{
							uiDropdown.options.Add (new Dropdown.OptionData ("New option"));
							Debug.Log ("Add " + uiDropdown.options.Count);
						}
						ACDebug.Log ("Cycle element '" + title + " is linked to a UI Dropdown with fewer options - adding them in automatically.");
					}
					else if (uiDropdown.options.Count > numOptions)
					{
						uiDropdown.options.RemoveRange (numOptions, uiDropdown.options.Count - numOptions);
					}
				}

				for (int i=0; i< numOptions; i++)
				{
					if (uiDropdown.options.Count > i && uiDropdown.options[i] != null)
					{
						uiDropdown.options[i].text = GetOptionLabel (i);
					}
				}
			}
			base.RecalculateSize (source);
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			if (cycleType == AC_CycleType.Variable)
			{
				linkedVariable = GlobalVariables.GetVariable (varID);
				if (linkedVariable != null)
				{
					if (linkedVariable.type != VariableType.Integer && linkedVariable.type != VariableType.PopUp)
					{
						ACDebug.LogWarning ("Cannot link the variable '" + linkedVariable.label + "' to Cycle element '" + title + "' because it is not an Integer or PopUp.");
						linkedVariable = null;
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot find the variable with ID=" + varID + " to link to the Cycle element '" + title + "'");
				}
			}

			base.OnMenuTurnOn (menu);
		}


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (cycleType == AC_CycleType.Language)
			{
				optionsArray = new List<string> ();
				if (Application.isPlaying)
				{
					for (int i = 0; i < KickStarter.runtimeLanguages.Languages.Count; i++)
					{
						if (!KickStarter.runtimeLanguages.Languages[i].isDisabled)
						{
							optionsArray.Add (KickStarter.runtimeLanguages.Languages[i].name);
						}
					}
				}
				else
				{
					for (int i = 0; i < AdvGame.GetReferences ().speechManager.Languages.Count; i++)
					{
						optionsArray.Add (AdvGame.GetReferences ().speechManager.Languages[i].name);
					}
				}

				if (Options.optionsData != null)
				{
					if (KickStarter.speechManager && KickStarter.speechManager.separateVoiceAndTextLanguages && splitLanguageType == SplitLanguageType.VoiceOnly)
					{
						int trueIndex = Options.optionsData.voiceLanguage;
						selected = KickStarter.runtimeLanguages.TrueLanguageIndexToEnabledIndex (trueIndex);
					}
					else
					{
						int trueIndex = Options.optionsData.language;
						selected = KickStarter.runtimeLanguages.TrueLanguageIndexToEnabledIndex (trueIndex);
					}
				}
			}
			else if (cycleType == AC_CycleType.Variable)
			{
				if (linkedVariable != null)
				{
					if (GetNumOptions () > 0)
					{
						selected = Mathf.Clamp (linkedVariable.IntegerValue, 0, GetNumOptions () - 1);
					}
					else
					{
						selected = 0;
					}
				}
			}
		}


		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (TranslateLabel (Options.GetLanguage ()) + " : Default option"));
		}


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return label;
		}


		public int GetTranslationID (int index)
		{
			return lineID;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			label = updatedText;
		}


		public int GetNumTranslatables ()
		{
			return 1;
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
			return title;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.MenuElement;
		}


		public bool CanTranslate (int index)
		{
			return !string.IsNullOrEmpty (label);
		}

		#endif

		#endregion

	}
	
}