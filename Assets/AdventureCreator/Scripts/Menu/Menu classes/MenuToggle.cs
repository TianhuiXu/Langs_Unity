/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuToggle.cs"
 * 
 *	This MenuElement toggles between On and Off when clicked on.
 *	It can be used for changing boolean options.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides an "on/off" toggle button.
	 * It can be used to change the value of a Boolean global variable, or the display of subtitles in Options.
	 */
	public class MenuToggle : MenuElement, ITranslatable
	{

		/** The Unity UI Toggle this is linked to (Unity UI Menus only) */
		public Toggle uiToggle;
		/** What the value of the toggle represents (Subtitles, Variable, CustomScript) */
		public AC_ToggleType toggleType;
		/** An ActionListAsset that will run when the element is clicked on */
		public ActionListAsset actionListOnClick = null;
		/** The text that's displayed on-screen */
		public string label;
		/** A string to append to the label, before the value */
		public string labelSuffix = defaultLabelSuffix;
		private const string defaultLabelSuffix = " : ";
		/** If True, then the toggle will be in its "on" state by default */
		public bool isOn;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** The text alignment */
		public TextAnchor anchor;
		/** The ID number of the Boolean global variable to link to, if toggleType = AC_ToggleType.Variable */
		public int varID;
		/** If True, then the state ("On"/"Off") will be added to the display label */
		public bool appendState = true;
		/** The background texture when in the "on" state (OnGUI Menus only) */
		public Texture2D onTexture = null;
		/** The background texture when in the "off" state (OnGUI Menus only) */
		public Texture2D offTexture = null;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		/** The text suffix when the toggle is 'on' */
		public string onText = "On";
		/** The translation ID of the 'off' text, as set within SpeechManager */
		public int onTextLineID = -1;
		/** The text suffix when the toggle is 'off' */
		public string offText = "Off";
		/** The translation ID of the 'off' text, as set within SpeechManager */
		public int offTextLineID = -1;

		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif
		private string fullText;


		public override void Declare ()
		{
			uiToggle = null;
			uiText = null;
			label = "Toggle";
			labelSuffix = defaultLabelSuffix;
			isOn = false;
			isVisible = true;
			isClickable = true;
			toggleType = AC_ToggleType.CustomScript;
			numSlots = 1;
			varID = 0;
			SetSize (new Vector2 (15f, 5f));
			anchor = TextAnchor.MiddleLeft;
			appendState = true;
			onTexture = null;
			offTexture = null;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			actionListOnClick = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			onText = "On";
			offText = "Off";
			onTextLineID = -1;
			offTextLineID = -1;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuToggle newElement = CreateInstance <MenuToggle>();
			newElement.Declare ();
			newElement.CopyToggle (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyToggle (MenuToggle _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiToggle = null;
			}
			else
			{
				uiToggle = _element.uiToggle;
			}

			uiText = null;
			label = _element.label;
			labelSuffix = _element.labelSuffix;
			isOn = _element.isOn;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			anchor = _element.anchor;
			toggleType = _element.toggleType;
			varID = _element.varID;
			appendState = _element.appendState;
			onTexture = _element.onTexture;
			offTexture = _element.offTexture;
			actionListOnClick = _element.actionListOnClick;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			onText = _element.onText;
			offText = _element.offText;
			onTextLineID = _element.onTextLineID;
			offTextLineID = _element.offTextLineID;
			isClickable = _element.isClickable;

			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			uiToggle = LinkUIElement <Toggle> (canvas);
			if (uiToggle)
			{
				#if TextMeshProIsPresent
				uiText = uiToggle.GetComponentInChildren <TMPro.TextMeshProUGUI>();
				#else
				uiText = uiToggle.GetComponentInChildren <Text>();
				#endif

				uiToggle.interactable = isClickable;
				if (isClickable)
				{
					if (addEventListeners)
					{
						uiToggle.onValueChanged.AddListener ((isOn) => {
						ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
						});
					}

					CreateHoverSoundHandler (uiToggle, _menu, 0);
				}
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiToggle)
			{
				return uiToggle.gameObject;
			}
			return null;
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiToggle)
			{
				return uiToggle.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiToggle)
			{
				uiToggle.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuToggle)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			if (source != MenuSource.AdventureCreator)
			{
				uiToggle = LinkedUiGUI <Toggle> (uiToggle, "Linked Toggle:", source, "The Unity UI Toggle this is linked to");
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
			}

			label = CustomGUILayout.TextField ("Label text:", label, apiPrefix + ".label", "The text that's displayed on-screen");
			if (!string.IsNullOrEmpty (label))
			{
				labelSuffix = CustomGUILayout.TextField ("Label suffix:", labelSuffix, apiPrefix + ".labelSuffix", "A string to append to the label, before the value");
			}
			appendState = CustomGUILayout.Toggle ("Append state to label?", appendState, apiPrefix + ".appendState", "If True, then the state (On/Off) will be added to the display label");
			if (appendState)
			{
				onText = CustomGUILayout.TextField ("'On' state text:", onText, apiPrefix + ".onText", "The text suffix when the toggle is 'on'");
				offText = CustomGUILayout.TextField ("'Off' state text:", offText, apiPrefix + ".offText", "The text suffix when the toggle is 'off'");
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
			
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("'On' texture:", "The background texture when in the 'on' state"), GUILayout.Width (145f));
				onTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (onTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".onTexture");
				EditorGUILayout.EndHorizontal ();
				
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("'Off' texture:", "The background texture when in the 'off' state"), GUILayout.Width (145f));
				offTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (offTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".offTexture");
				EditorGUILayout.EndHorizontal ();
			}

			toggleType = (AC_ToggleType) CustomGUILayout.EnumPopup ("Toggle type:", toggleType, apiPrefix + ".toggleType", "What the value of the toggle represents");
			if (toggleType == AC_ToggleType.CustomScript)
			{
				isOn = CustomGUILayout.Toggle ("On by default?", isOn, apiPrefix + ".isOn", "If True, then the toggle will be in its 'on' state by default");
				ShowClipHelp ();
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				varID = AdvGame.GlobalVariableGUI ("Global boolean var:", varID, VariableType.Boolean, "The global Boolean variable whose value is linked to the Toggle");
			}

			isClickable = CustomGUILayout.Toggle ("User can change value?", isClickable, apiPrefix + ".isClickable", "If True, the slider is interactive and can be modified by the user");
			if (isClickable)
			{
				if (toggleType != AC_ToggleType.Subtitles)
				{
					actionListOnClick = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on click:", actionListOnClick, false, apiPrefix + ".actionListOnClick", "An ActionList asset that will run when the element is clicked on");
				}
				alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
				ChangeCursorGUI (menu);
			}
			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			if (toggleType == AC_ToggleType.Variable && varID == oldGlobalID)
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

			if (toggleType == AC_ToggleType.Variable && varID == _varID)
			{
				numFound ++;
			}

			return numFound;
		}


		public override int UpdateVariableReferences (int oldVarID, int newVarID)
		{
			int numFound = 0;

			string oldTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, oldVarID);
			if (label.ToLower ().Contains (oldTokenText))
			{
				string newTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, newVarID);
				label = label.Replace (oldTokenText, newTokenText);
				numFound++;
			}

			if (toggleType == AC_ToggleType.Variable && varID == oldVarID)
			{
				numFound++;
				varID = newVarID;
			}

			return numFound;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (isClickable && toggleType != AC_ToggleType.Subtitles && actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiToggle && uiToggle.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (uiToggle && uiToggle.gameObject == gameObject)
			{
				return 0;
			}
			if (uiText && uiText.gameObject == gameObject)
			{
				return 0;
			}
			return base.GetSlotIndex (gameObject);
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			CalculateValue ();

			fullText = TranslateLabel (languageNumber);
			if (appendState)
			{
				if (!string.IsNullOrEmpty (fullText))
				{
					fullText += labelSuffix;
				}

				if (languageNumber == 0)
				{
					if (isOn)
					{
						fullText += onText;
					}
					else
					{
						fullText += offText;
					}
				}
				else
				{
					if (isOn)
					{
						fullText += KickStarter.runtimeLanguages.GetTranslation (onText, onTextLineID, languageNumber, GetTranslationType (0));
					}
					else
					{
						fullText += KickStarter.runtimeLanguages.GetTranslation (offText, offTextLineID, languageNumber, GetTranslationType (0));
					}
				}
			}

			if (uiToggle)
			{
				if (uiText)
				{
					uiText.text = fullText;
				}
				uiToggle.isOn = isOn;
				UpdateUISelectable (uiToggle, uiSelectableHideStyle);
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
			
			Rect rect = ZoomRect (relativeRect, zoom);
			if (isOn && onTexture)
			{
				GUI.DrawTexture (rect, onTexture, ScaleMode.StretchToFill, true, 0f);
			}
			else if (!isOn && offTexture)
			{
				GUI.DrawTexture (rect, offTexture, ScaleMode.StretchToFill, true, 0f);
			}
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (rect, fullText, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (rect, fullText, _style);
			}
		}


		protected override string GetLabelToTranslate ()
		{
			return label;
		}


		public override string GetLabel (int slot, int languageNumber)
		{
			string baseLabel = TranslateLabel (languageNumber);

			if (appendState)
			{
				if (!string.IsNullOrEmpty (baseLabel))
				{
					baseLabel += labelSuffix;
				}

				if (isOn)
				{
					return baseLabel + KickStarter.runtimeLanguages.GetTranslation (onText, onTextLineID, languageNumber, GetTranslationType (0));
				}
				
				return baseLabel + KickStarter.runtimeLanguages.GetTranslation (offText, offTextLineID, languageNumber, GetTranslationType (0));
			}
			return baseLabel;
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiToggle)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiToggle.gameObject);
			}
			return false;
		}


		public override bool IsSelectableInteractable (int slotIndex)
		{
			if (uiToggle)
			{
				return uiToggle.IsInteractable ();
			}
			return false;
		}
		

		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return false;
			}

			if (uiToggle)
			{
				isOn = uiToggle.isOn;
			}
			else
			{
				if (isOn)
				{
					isOn = false;
				}
				else
				{
					isOn = true;
				}
			}

			switch (toggleType)
			{
				case AC_ToggleType.Subtitles:
					Options.SetSubtitles (isOn);
					break;

				case AC_ToggleType.Variable:
					if (varID >= 0)
					{
						GVar var = GlobalVariables.GetVariable (varID);
						if (var.type == VariableType.Boolean)
						{
							var.IntegerValue = (isOn) ? 1 : 0;
							var.Upload (VariableLocation.Global);
						}
					}
					break;

				case AC_ToggleType.CustomScript:
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


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			if (toggleType == AC_ToggleType.Subtitles)
			{	
				if (Options.optionsData != null)
				{
					isOn = Options.optionsData.showSubtitles;
				}
			}
			else if (toggleType == AC_ToggleType.Variable)
			{
				if (varID >= 0)
				{
					GVar var = GlobalVariables.GetVariable (varID);
					if (var != null && var.type == VariableType.Boolean)
					{
						if (var.IntegerValue == 1)
						{
							isOn = true;
						}
						else
						{
							isOn = false;
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot link MenuToggle " + title + " to Variable " + varID + " as it is not a Boolean.");
					}
				}
			}
		}

		
		protected override void AutoSize ()
		{
			int languageNumber = Options.GetLanguage ();
			if (appendState)
			{
				AutoSize (new GUIContent (TranslateLabel (languageNumber) + " : Off"));
			}
			else
			{
				AutoSize (new GUIContent (TranslateLabel (languageNumber)));
			}
		}


		#region ITranslatable
		
		public string GetTranslatableString (int index)
		{
			if (index == 0)
			{
				return label;
			}
			else if (index == 1)
			{
				return onText;
			}
			else
			{
				return offText;
			}
		}
		

		public int GetTranslationID (int index)
		{
			if (index == 0)
			{
				return lineID;
			}
			else if (index == 1)
			{
				return onTextLineID;
			}
			else
			{
				return offTextLineID;
			}
		}

		
		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.MenuElement;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			if (index == 0)
			{
				label = updatedText;
			}
			else if (index == 1)
			{
				onText = updatedText;
			}
			else
			{
				offText = updatedText;
			}
		}


		public int GetNumTranslatables ()
		{
			return 3;
		}


		public bool HasExistingTranslation (int index)
		{
			if (index == 0)
			{
				return (lineID > -1);
			}
			else if (index == 1)
			{
				return (onTextLineID > -1);
			}
			else
			{
				return (offTextLineID > -1);
			}
		}


		public void SetTranslationID (int index, int _lineID)
		{
			if (index == 0)
			{
				lineID = _lineID;
			}
			else if (index == 1)
			{
				onTextLineID = _lineID;
			}
			else
			{
				offTextLineID = _lineID;
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
			if (index == 0)
			{
				return !string.IsNullOrEmpty (label);
			}
			else if (index == 1)
			{
				return !string.IsNullOrEmpty (onText);
			}
			else
			{
				return !string.IsNullOrEmpty (offText);
			}
		}
		
		#endif

		#endregion

	}
	
}