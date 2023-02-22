/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuSlider.cs"
 * 
 *	This MenuElement creates a slider for eg. volume control.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A MenuElement that provides a slider, whose value can represent either a Float global variable or the volume of an Options sound type. */
	public class MenuSlider : MenuElement, ITranslatable
	{

		/** The Unity UI Slider this is linked to (Unity UI Menus only) */
		public Slider uiSlider;
		/** The slider's default value */
		public float amount;
		/** The slider's minimum value */
		public float minValue = 0f;
		/** The slider's maximum value */
		public float maxValue = 1f;
		/** The text that's displayed on-screen */
		public string label;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** The text alignement */
		public TextAnchor anchor;
		/** The fill-bar texture, or moveable block texture (OnGUI Menus only) */
		public Texture2D sliderTexture;
		/** The display type of the slider (FillBar, MoveableBlock) (OnGUI Menus Only) */
		public SliderDisplayType sliderDisplayType = SliderDisplayType.FillBar;
		/** Which way the slider is orientated (Horizontal, Vertical) (OnGUI Menus Only) */
		public SliderOrientation sliderOrientation = SliderOrientation.Horizontal;
		/** What the slider's value represents (Speech, Music, SFX, CustomScript, FloatVariable) */
		public AC_SliderType sliderType;
		/** The dimensions of the block, if sliderDisplayType = SliderDisplayType.MoveableBlock */
		public Vector2 blockSize = new Vector2 (0.05f, 1f);
		/** If True, then the slider will be drawn across the whole width of the element.  Otherwise, it will be drawn across only half. */
		public bool useFullWidth = false;
		/** The ID number of the Float global variable to link the slider's value to, if sliderType = AC_SliderType.FloatVariable) */
		public int varID;
		/** If >0, The number of descrete values the slider can have (OnGUI Menus only) */
		public int numberOfSteps = 0;
		/** An ActionListAsset that will run when the value of the slider is changed */
		public ActionListAsset actionListOnChange = null;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif

		private float visualAmount;
		private string fullText;


		public override void Declare ()
		{
			uiSlider = null;
			uiText = null;

			label = "Slider";
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			amount = 1f;
			minValue = 0f;
			maxValue = 1f;
			anchor = TextAnchor.MiddleLeft;
			sliderType = AC_SliderType.CustomScript;
			sliderDisplayType = SliderDisplayType.FillBar;
			sliderOrientation = SliderOrientation.Horizontal;
			blockSize = new Vector2 (0.05f, 1f);
			useFullWidth = false;
			varID = 0;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			numberOfSteps = 0;
			actionListOnChange = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuSlider newElement = CreateInstance <MenuSlider>();
			newElement.Declare ();
			newElement.CopySlider (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopySlider (MenuSlider _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlider = null;
			}
			else
			{
				uiSlider = _element.uiSlider;
			}

			uiText = null;
			label = _element.label;
			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			amount = _element.amount;
			minValue = _element.minValue;
			maxValue = _element.maxValue;
			anchor = _element.anchor;
			sliderTexture = _element.sliderTexture;
			sliderType = _element.sliderType;
			sliderDisplayType = _element.sliderDisplayType;
			sliderOrientation = _element.sliderOrientation;
			blockSize = _element.blockSize;
			useFullWidth = _element.useFullWidth;
			varID = _element.varID;
			numberOfSteps = _element.numberOfSteps;
			actionListOnChange = _element.actionListOnChange;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;

			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			uiSlider = LinkUIElement <Slider> (canvas);
			if (uiSlider)
			{
				#if TextMeshProIsPresent
				uiText = uiSlider.GetComponentInChildren <TMPro.TextMeshProUGUI>();
				#else
				uiText = uiSlider.GetComponentInChildren <Text>();
				#endif

				uiSlider.interactable = isClickable;
				if (isClickable)
				{
					if (addEventListeners)
					{
						uiSlider.onValueChanged.AddListener ((amount) => {
							ProcessClickUI (_menu, 0, KickStarter.playerInput.GetMouseState ());
						});
					}

					CreateHoverSoundHandler (uiSlider, _menu, 0);
				}
			}
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (uiSlider)
			{
				return uiSlider.gameObject;
			}
			return null;
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiSlider)
			{
				return uiSlider.GetComponent <RectTransform>();
			}
			return null;
		}


		public override void SetUIInteractableState (bool state)
		{
			if (uiSlider)
			{
				uiSlider.interactable = state;
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuSlider)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			sliderType = (AC_SliderType) CustomGUILayout.EnumPopup ("Slider affects:", sliderType, apiPrefix + ".sliderType", "What the slider's value represents");
			label = CustomGUILayout.TextField ("Label text:", label, apiPrefix + ".label", "The text that's displayed on-screen");

			if (sliderType == AC_SliderType.CustomScript)
			{
				ShowClipHelp ();
				amount = CustomGUILayout.Slider ("Default value:", amount, minValue, maxValue, apiPrefix + ".amount", "The slider's default value");
			}
			else if (sliderType == AC_SliderType.FloatVariable)
			{
				varID = AdvGame.GlobalVariableGUI ("Global float var:", varID, VariableType.Float, "The global Float variable whose value is linked to the slider");
			}
			if (sliderType == AC_SliderType.CustomScript || sliderType == AC_SliderType.FloatVariable)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Min. value:", GUILayout.Width (80f));
				minValue = EditorGUILayout.FloatField (minValue);
				EditorGUILayout.LabelField ("Max. value:", GUILayout.Width (80f));
				maxValue = EditorGUILayout.FloatField (maxValue);
				EditorGUILayout.EndHorizontal ();
				maxValue = Mathf.Max (minValue, maxValue);
			}
			else
			{
				minValue = 0f;
				maxValue = 1f;
			}
			actionListOnChange = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on change:", actionListOnChange, false, apiPrefix + ".actionListOnChange");

			if (source == MenuSource.AdventureCreator)
			{
				numberOfSteps = CustomGUILayout.IntField ("Number of steps:", numberOfSteps, apiPrefix + ".numberOfSteps", "The number of descrete values the slider can have");
			}

			if (source == MenuSource.AdventureCreator)
			{
				sliderOrientation = (SliderOrientation) CustomGUILayout.EnumPopup ("Orientation:", sliderOrientation, apiPrefix + ".sliderOrientation", "Which way the slider is orientated");
				if (sliderOrientation == SliderOrientation.Horizontal)
				{
					useFullWidth = CustomGUILayout.Toggle ("Use full width?", useFullWidth, apiPrefix + ".useFullWidth", "If True, then the slider will be drawn across the whole width of the element. Otherwise, it will be drawn across only half.");
				}
				sliderDisplayType = (SliderDisplayType) CustomGUILayout.EnumPopup ("Display type:", sliderDisplayType, apiPrefix + ".sliderDisplayType", "The display type of the slider");
				
				if (sliderDisplayType == SliderDisplayType.MoveableBlock)
				{
					blockSize = EditorGUILayout.Vector2Field (new GUIContent ("Block size:", "The dimensions of the block"), blockSize);
				}
			}
			else
			{
				uiSlider = LinkedUiGUI <Slider> (uiSlider, "Linked Slider:", source, "The Unity UI Slider this is linked to");
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
			}
			

			isClickable = CustomGUILayout.Toggle ("User can change value?", isClickable, apiPrefix + ".isClickable", "If True, the slider is interactive and can be modified by the user");
			if (isClickable)
			{
				ChangeCursorGUI (menu);
			}
			else
			{
				changeCursor = false;
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
			EditorGUILayout.BeginHorizontal ();
			if (sliderDisplayType == SliderDisplayType.FillBar)
			{
				EditorGUILayout.LabelField ("Fill-bar texture:", GUILayout.Width (145f));
			}
			else if (sliderDisplayType == SliderDisplayType.MoveableBlock)
			{
				EditorGUILayout.LabelField ("Movable block texture:", GUILayout.Width (145f));
			}
			sliderTexture = (Texture2D) EditorGUILayout.ObjectField (sliderTexture, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
			EditorGUILayout.EndHorizontal ();
		}


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			if (sliderType == AC_SliderType.FloatVariable && varID == oldGlobalID)
			{
				return true;
			}

			string newLabel = AdvGame.ConvertGlobalVariableTokenToLocal (label, oldGlobalID, newLocalID);
			return (label != newLabel);
		}


		public override int GetVariableReferences (int _varID)
		{
			int numFound = 0;

			string tokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, _varID);
			if (label.ToLower ().Contains (tokenText))
			{
				numFound ++;
			}

			if (sliderType == AC_SliderType.FloatVariable && varID == _varID)
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
				numFound++;
				string newTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, newVarID);
				label = label.Replace (oldTokenText, newTokenText);
			}

			if (sliderType == AC_SliderType.FloatVariable && varID == oldVarID)
			{
				numFound++;
				varID = newVarID;
			}

			return numFound;
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListOnChange == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiSlider && uiSlider.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (uiSlider && uiSlider.gameObject == gameObject)
			{
				return 0;
			}
			if (uiText && uiText.gameObject == gameObject)
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
			CalculateValue ();

			fullText = AdvGame.ConvertTokens (TranslateLabel (languageNumber));

			if (uiSlider)
			{
				if (uiText)
				{
					uiText.text = fullText;
				}
				uiSlider.value = visualAmount;
				UpdateUISelectable (uiSlider, uiSelectableHideStyle);
			}
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			GUI.Label (ZoomRect (relativeRect, zoom), "", _style);
			
			if (sliderTexture)
			{
				switch (sliderOrientation)
				{
					case SliderOrientation.Horizontal:
						DrawHorizontalSlider (zoom);
						break;

					case SliderOrientation.Vertical:
						DrawVerticalSlider (zoom);
						break;

					default:
						break;
				}
			}
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}
			
			_style.normal.background = null;
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
			}
		}
		
		
		private void DrawHorizontalSlider (float zoom)
		{
			Rect sliderRect = relativeRect;

			switch (sliderDisplayType)
			{
				case SliderDisplayType.FillBar:
					if (useFullWidth)
					{
						sliderRect.x = relativeRect.x;
						sliderRect.width = slotSize.x * visualAmount;
					}
					else
					{
						sliderRect.x = relativeRect.x + (relativeRect.width / 2);
						sliderRect.width = slotSize.x * visualAmount * 0.5f;
					}
					
					if (sizeType != AC_SizeType.AbsolutePixels)
					{
						sliderRect.width *= KickStarter.mainCamera.GetPlayableScreenArea (false).size.x / 100f;
					}
					break;

				case SliderDisplayType.MoveableBlock:
					sliderRect.width *= blockSize.x;
					sliderRect.height *= blockSize.y;
					sliderRect.y += (relativeRect.height - sliderRect.height) / 2f;
					
					if (useFullWidth)
					{
						sliderRect.x += (relativeRect.width - sliderRect.width) * visualAmount;
					}
					else
					{
						sliderRect.x += (relativeRect.width - sliderRect.width) / 2f;
						sliderRect.x += (relativeRect.width - sliderRect.width) * visualAmount / 2f;
					}
					break;
			}

			GUI.DrawTexture (ZoomRect (sliderRect, zoom), sliderTexture, ScaleMode.StretchToFill, true, 0f);
		}


		private void DrawVerticalSlider (float zoom)
		{
			Rect sliderRect = relativeRect;

			switch (sliderDisplayType)
			{
				case SliderDisplayType.FillBar:
					sliderRect.y = relativeRect.y;
					sliderRect.height = slotSize.y * visualAmount;

					if (sizeType != AC_SizeType.AbsolutePixels)
					{
						sliderRect.height *= KickStarter.mainCamera.GetPlayableScreenArea (false).size.y / 100f;
					}

					sliderRect.y = relativeRect.y + relativeRect.height - sliderRect.height;
					break;

				case SliderDisplayType.MoveableBlock:
					sliderRect.width *= blockSize.y;
					sliderRect.height *= blockSize.x;
					sliderRect.x += (relativeRect.width - sliderRect.width) / 2f;
					
					sliderRect.y += (relativeRect.height - sliderRect.height) * (1f - visualAmount);
					break;
			}

			GUI.DrawTexture (ZoomRect (sliderRect, zoom), sliderTexture, ScaleMode.StretchToFill, true, 0f);
		}
		

		public override string GetLabel (int slot, int languageNumber)
		{
			return AdvGame.ConvertTokens (TranslateLabel (languageNumber));
		}


		public override bool IsSelectedByEventSystem (int slotIndex)
		{
			if (uiSlider)
			{
				return KickStarter.playerMenus.IsEventSystemSelectingObject (uiSlider.gameObject);
			}
			return false;
		}


		public override bool IsSelectableInteractable (int slotIndex)
		{
			if (uiSlider)
			{
				return uiSlider.IsInteractable ();
			}
			return false;
		}
		
		
		private void Change (float mousePos)
		{
			switch (sliderOrientation)
			{
				case SliderOrientation.Horizontal:
					if (useFullWidth)
					{
						mousePos = mousePos - relativeRect.x;
						visualAmount = mousePos / relativeRect.width;
					}
					else
					{
						mousePos = mousePos - relativeRect.x - (relativeRect.width / 2f);
						visualAmount = mousePos / (relativeRect.width / 2f);
					}
					break;

				case SliderOrientation.Vertical:
					mousePos = relativeRect.y + relativeRect.height - mousePos;
					visualAmount = mousePos / relativeRect.height;
					break;

				default:
					break;
			}

			UpdateValue ();
		}
		
		
		private void UpdateValue ()
		{
			if (uiSlider == null)
			{
				visualAmount = Mathf.Clamp (visualAmount, 0f, 1f);

				// Limit by steps
				if (numberOfSteps > 0)
				{
					visualAmount = Mathf.Round (visualAmount * numberOfSteps) / numberOfSteps;
				}

				amount = (visualAmount * (maxValue - minValue)) + minValue;
			}
			else
			{
				amount = visualAmount;
			}

			switch (sliderType)
			{
				case AC_SliderType.Speech:
					Options.SetSpeechVolume (amount);
					break;

				case AC_SliderType.Music:
					Options.SetMusicVolume (amount);
					break;

				case AC_SliderType.SFX:
					Options.SetSFXVolume (amount);
					break;

				case AC_SliderType.FloatVariable:
					if (varID >= 0)
					{
						GlobalVariables.SetFloatValue (varID, amount);
					}
					break;

				default:
					break;
			}

			if (!KickStarter.actionListAssetManager.IsListRunning (actionListOnChange))
			{
				AdvGame.RunActionListAsset (actionListOnChange);
			}
		}


		private void CalculateValue ()
		{
			if (!Application.isPlaying)
			{
				visualAmount = 0.5f;
				return;
			}

			if (sliderType == AC_SliderType.Speech || sliderType == AC_SliderType.SFX || sliderType == AC_SliderType.Music)
			{
				if (Options.optionsData != null)
				{
					if (sliderType == AC_SliderType.Speech)
					{
						amount = Options.optionsData.speechVolume;
					}
					else if (sliderType == AC_SliderType.Music)
					{
						amount = Options.optionsData.musicVolume;
					}
					else if (sliderType == AC_SliderType.SFX)
					{
						amount = Options.optionsData.sfxVolume;
					}
				}
			}
			else if (sliderType == AC_SliderType.FloatVariable)
			{
				if (varID >= 0)
				{
					GVar _variable = GlobalVariables.GetVariable (varID);
					if (_variable != null)
					{
						if (_variable.type != VariableType.Float)
						{
							ACDebug.LogWarning ("Cannot link MenuSlider " + title + " to Variable " + varID + " as it is not a Float.");
						}
						else
						{
							amount = Mathf.Clamp (_variable.FloatValue, minValue, maxValue);
							_variable.FloatValue = amount;
						}
					}
					else
					{
						ACDebug.LogWarning ("Slider " + this.label + " is referencing Gloval Variable " + varID + ", which does not exist.");
					}
				}
			}

			if (uiSlider)
			{
				visualAmount = amount;
			}
			else
			{
				visualAmount = (amount - minValue) / (maxValue - minValue);
			}
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (!_menu.IsClickable ())
			{
				return false;
			}

			if (uiSlider)
			{
				visualAmount = uiSlider.value;
				UpdateValue ();
			}
			else
			{
				if ((KickStarter.stateHandler.gameState == GameState.DialogOptions && KickStarter.menuManager.keyboardControlWhenDialogOptions) ||
					(KickStarter.stateHandler.gameState == GameState.Paused && KickStarter.menuManager.keyboardControlWhenPaused) ||
					(KickStarter.stateHandler.IsInGameplay () && KickStarter.playerInput.canKeyboardControlMenusDuringGameplay))
				{
					// Direct-controlling
				}
				else
				{
					switch (sliderOrientation)
					{
						case SliderOrientation.Horizontal:
							Change (KickStarter.playerInput.GetMousePosition ().x - _menu.GetRect ().x);
							break;

						case SliderOrientation.Vertical:
							Change (KickStarter.playerInput.GetInvertedMouse ().y - _menu.GetRect ().y);
							break;

						default:
							break;
					}
				}
			}

			if (sliderType == AC_SliderType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, _slot, (int) _mouseState);
			}

			return base.ProcessClick (_menu, _slot, _mouseState);
		}


		public bool KeyboardControl (Vector2 direction)
		{
			Vector2 increaseDirection = (sliderOrientation == SliderOrientation.Horizontal) ? Vector2.right : Vector2.up;
			Vector2 decreaseDirection = (sliderOrientation == SliderOrientation.Horizontal) ? Vector2.left : Vector2.down;

			if (direction == increaseDirection)
			{
				if (clickSound)
				{
					KickStarter.sceneSettings.PlayDefaultSound (clickSound, false, true);
				}

				visualAmount += 0.02f; 
				UpdateValue ();	
				return true;
			}
			else if (direction == decreaseDirection)
			{
				if (clickSound)
				{
					KickStarter.sceneSettings.PlayDefaultSound (clickSound, false, true);
				}

				visualAmount -= 0.02f;
				UpdateValue ();
				return true;
			}
			return false;
		}
		

		public override bool ProcessContinuousClick (AC.Menu _menu, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}

			float originalVisualAmount = visualAmount;

			if (uiSlider)
			{
				visualAmount = uiSlider.value;
				UpdateValue ();
			}
			else
			{
				if ((KickStarter.stateHandler.gameState == GameState.DialogOptions && KickStarter.menuManager.keyboardControlWhenDialogOptions) ||
					(KickStarter.stateHandler.gameState == GameState.Paused && KickStarter.menuManager.keyboardControlWhenPaused) ||
					(KickStarter.stateHandler.IsInGameplay () && KickStarter.playerInput.canKeyboardControlMenusDuringGameplay))
				{
					// Direct-controlling
				}
				else
				{
					switch (sliderOrientation)
					{
						case SliderOrientation.Horizontal:
							Change (KickStarter.playerInput.GetMousePosition ().x - _menu.GetRect ().x);
							break;

						case SliderOrientation.Vertical:
							Change (KickStarter.playerInput.GetInvertedMouse ().y - _menu.GetRect ().y);
							break;

						default:
							break;
					}
				}
			}

			if (sliderType == AC_SliderType.CustomScript)
			{
				MenuSystem.OnElementClick (_menu, this, 0, (int) _mouseState);
			}

			if (clickSound && originalVisualAmount != visualAmount)
			{
				KickStarter.sceneSettings.PlayDefaultSound(clickSound, false, true);
			}

			return true;
		}

		
		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (TranslateLabel (Options.GetLanguage ())));
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