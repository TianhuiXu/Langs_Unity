/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuInput.cs"
 * 
 *	This MenuElement acts like a label, whose text can be changed with keyboard input.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A MenuElement that provides an input box that the player can enter text into. */
	public class MenuInput : MenuElement, ITranslatable
	{

		
		/** The text that's displayed on-screen */
		public string label = "Element";
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** What kind of characters can be entered in by the player (AlphaNumeric, NumericOnly, AllowSpecialCharacters) */
		public AC_InputType inputType;
		/** The character limit on text that can be entered */
		public int characterLimit = 10;
		/** The name of the MenuButton element that is synced with the 'Return' key when this element is active */
		public string linkedButton = "";
		/** If True, and inputType = AC_InputType.NumericOnly, then decimal points can be entered */
		public bool allowDecimals = false;
		/** If True, then spaces are recognised */
		public bool allowSpaces = false;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** If True, then the element will need to be selected before it receives input */
		public bool requireSelection = false;

		#if TextMeshProIsPresent
		public TMPro.TMP_InputField uiInput;
		#else
		/** The Unity UI InputField this is linked to (Unity UI Menus only) */
		public InputField uiInput;
		#endif

		private bool isSelected = false;


		/**
		 * Initialises the element when it is created within MenuManager.
		 */
		public override void Declare ()
		{
			uiInput = null;
			label = "Input";
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			inputType = AC_InputType.AlphaNumeric;
			characterLimit = 10;
			linkedButton = string.Empty;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			allowSpaces = false;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			requireSelection = false;
			allowDecimals = false;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuInput newElement = CreateInstance <MenuInput>();
			newElement.Declare ();
			newElement.CopyInput (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyInput (MenuInput _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiInput = null;
			}
			else
			{
				uiInput = _element.uiInput;
			}

			label = _element.label;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			inputType = _element.inputType;
			characterLimit = _element.characterLimit;
			linkedButton = _element.linkedButton;
			allowSpaces = _element.allowSpaces;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			requireSelection = _element.requireSelection;
			allowDecimals = _element.allowDecimals;

			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			#if TextMeshProIsPresent
			uiInput = LinkUIElement <TMPro.TMP_InputField> (canvas);
			#else
			uiInput = LinkUIElement <InputField> (canvas);
			#endif

			CreateHoverSoundHandler (uiInput, _menu, 0);
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiInput)
			{
				return uiInput.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiInput)
			{
				uiInput.interactable = state;
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiInput)
			{
				return uiInput.gameObject;
			}
			return null;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuInput)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();
			if (source == MenuSource.AdventureCreator)
			{
				inputType = (AC_InputType) CustomGUILayout.EnumPopup ("Input type:", inputType, apiPrefix + ".inputType", "What kind of characters can be entered in by the player");
				label = EditorGUILayout.TextField ("Default text:", label);
				if (inputType == AC_InputType.AlphaNumeric)
				{
					allowSpaces = CustomGUILayout.Toggle ("Allow spaces?", allowSpaces, apiPrefix + ".allowSpace", "If True, then spaces are recognised");
				}
				else if (inputType == AC_InputType.NumbericOnly)
				{
					allowDecimals = CustomGUILayout.Toggle ("Allow decimals?", allowDecimals, apiPrefix + ".allowDecimals", "If True, then decimals are recognised");
				}
				characterLimit = CustomGUILayout.IntField ("Character limit:", characterLimit, apiPrefix + ".characterLimit", "The character limit on text that can be entered");

				#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_2018_3_OR_NEWER
				EditorGUILayout.HelpBox ("For the character limit to be obeyed on Android and iOS, Unity 2018.3 or later must be used.", MessageType.Info);
				#endif

				linkedButton = CustomGUILayout.TextField ("'Enter' key's linked Button:", linkedButton, apiPrefix + ".linkedPrefab", "The name of the MenuButton element that is synced with the 'Return' key when this element is active");
				requireSelection = CustomGUILayout.ToggleLeft ("Require selection to accept input?", requireSelection, apiPrefix + ".requireSelection", "If True, then the element will need to be selected before it receives input");
			}
			else
			{
				#if TextMeshProIsPresent
				uiInput = LinkedUiGUI <TMPro.TMP_InputField> (uiInput, "Linked InputField:", source);
				#else
				uiInput = LinkedUiGUI <InputField> (uiInput, "Linked InputField:", source);
				#endif
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
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

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiInput && uiInput.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (uiInput && uiInput.gameObject == gameObject)
			{
				return 0;
			}
			return base.GetSlotIndex (gameObject);
		}


		/**
		 * <summary>Gets the contents of the text box.</summary>
		 * <returns>The contents of the text box.</returns>
		 */
		public string GetContents ()
		{
			if (uiInput)
			{
				return uiInput.text;
			}
			return label;
		}


		/**
		 * <summary>Set the contents of the text box manually.</summary>
		 * <param name = "_label">The new label for the text box.</param>
		 */
		public void SetLabel (string _label)
		{
			label = _label;

			if (uiInput)
			{
				uiInput.text = _label;
			}
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (uiInput)
			{
				UpdateUISelectable (uiInput, uiSelectableHideStyle);
			}
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			string fullText = label;
			if (Application.isPlaying && (isSelected || isActive))
			{
				fullText = AdvGame.CombineLanguageString (fullText, "|", Options.GetLanguage (), false);
			}

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
			}
		}


		protected override string GetLabelToTranslate ()
		{
			return label;
		}


		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslateLabel (languageNumber);
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiInput)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiInput.gameObject);
			}
			return false;
		}


		public override bool IsSelectableInteractable (int slotIndex)
		{
			if (uiInput)
			{
				return uiInput.IsInteractable ();
			}
			return false;
		}


		private void ProcessReturn (string input, string menuName)
		{
			if (input == "KeypadEnter" || input == "Return" || input == "Enter")
			{
				if (linkedButton != "" && menuName != "")
				{
					PlayerMenus.SimulateClick (menuName, PlayerMenus.GetElementWithName (menuName, linkedButton), 1);
				}
			}
		}


		/**
		 * <summary>Processes input entered by the player, and applies it to the text box (OnGUI-based Menus only).</summary>
		 * <param name = "keycode">The keycode of the Event that recorded input</param>
		 * <param name = "character">The character of the Event that recorded input</param>
		 * <param name = "shift">If True, shift was held down</param>
		 * <param name = "menuName">The name of the Menu that stores this element</param>
		 */
		public void CheckForInput (string keycode, string character, bool shift, string menuName)
		{
			if (uiInput)
			{
				return;
			}

			string input = keycode;


			if (inputType == AC_InputType.AllowSpecialCharacters)
			{
				if (!(input == "KeypadEnter" || input == "Return" || input == "Enter" || input == "Backspace"))
				{
					input = character;
				}
			}

			bool rightToLeft = KickStarter.runtimeLanguages.LanguageReadsRightToLeft (Options.GetLanguage ());
			
			isSelected = true;
			if (input == "Backspace")
			{
				if (label.Length > 1)
				{
					if (rightToLeft)
					{
						label = label.Substring (1, label.Length - 1);
					}
					else
					{
						label = label.Substring (0, label.Length - 1);
					}
				}
				else if (label.Length == 1)
				{
					label = string.Empty;
				}
			}
			else if (input == "KeypadEnter" || input == "Return" || input == "Enter")
			{
				ProcessReturn (input, menuName);
			}
			else if ((inputType == AC_InputType.AlphaNumeric && (input.Length == 1 || input.Contains ("Alpha"))) ||
					(inputType == AC_InputType.AlphaNumeric && allowSpaces && input == "Space") ||
					(inputType == AC_InputType.NumbericOnly && input.Contains ("Alpha")) ||
					(inputType == AC_InputType.NumbericOnly && allowDecimals && input == "Period" && !label.Contains (".")) ||
					(inputType == AC_InputType.NumbericOnly && allowDecimals && input == "KeypadPeriod" && !label.Contains (".")) ||
					(inputType == AC_InputType.AllowSpecialCharacters && (input.Length == 1 || input == "Space")))
			{
				if (inputType == AC_InputType.AllowSpecialCharacters && keycode != "None") return;
				
				input = input.Replace ("Alpha", "");
				input = input.Replace ("Space", " ");

				input = input.Replace ("KeypadPeriod", ".");
				input = input.Replace ("Period", ".");

				if (inputType != AC_InputType.AllowSpecialCharacters)
				{
					if (shift)
					{
						input = input.ToUpper ();
					}
					else
					{
						input = input.ToLower ();
					}
				}

				if (characterLimit == 1)
				{
					label = input;
				}
				else if (label.Length < characterLimit)
				{
					if (rightToLeft)
					{
						label = input + label;
					}
					else
					{
						label += input;
					}
				}
			}
			else if (input != "None")
			{
				Debug.LogWarning ("Invalid character: '" + input + "'");
			}
		}


		public override void RecalculateSize (MenuSource source)
		{
			if (source == MenuSource.AdventureCreator)
			{
				Deselect ();
			}

			base.RecalculateSize (source);
		}


		/** De-selects the text box (OnGUI-based Menus only). */
		public void Deselect ()
		{
			isSelected = false;
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return false;
			}

			KickStarter.playerMenus.SelectInputBox (this);

			return base.ProcessClick (_menu, _slot, _mouseState);
		}

		
		protected override void AutoSize ()
		{
			GUIContent content = new GUIContent (TranslateLabel (Options.GetLanguage ()) + "|");
			AutoSize (content);
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