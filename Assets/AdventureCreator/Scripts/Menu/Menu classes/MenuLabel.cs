/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuLabel.cs"
 * 
 *	This MenuElement provides a basic label.
 * 
 */

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A MenuElement that provides a basic label.
	 * The label can be used to display fixed text, or a number of pre-programmed string types, such as the active verb and Hotspot, subtitles, and more.
	 * Variable tokens of the form [var:ID] and [localvar:ID] can also be inserted to display the values of global and local variables respectively.
	 */
	public class MenuLabel : MenuElement, ITranslatable
	{

		/** The Unity UI Text this is linked to (Unity UI Menus only) */
		#if TextMeshProIsPresent
		public TMPro.TextMeshProUGUI uiText;
		public bool hideScrollingCharacters = false;
		#else
		public Text uiText;
		#endif

		[SerializeField] [FormerlySerializedAs ("label")] private string _label = "Element";
		/** The text alignement */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects = TextEffects.None;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** What kind of text the label displays (Normal, Hotspot, DialogueLine, DialogueSpeaker, GlobalVariable, ActiveSaveProfile, JournalPageNumber, InventoryProperty, DocumentTitle, SelectedObjective) */
		public AC_LabelType labelType;

		/** The ID number of the global variable to show (if labelType = AC_LabelType.GlobalVariable) */
		public int variableID;
		/** If True, and labelType = AC_LabelType.DialogueLine, then the displayed subtitle text will use the speaking character's subtitle text colour */
		public bool useCharacterColour = false;
		/** If True, and sizeType = AC_SizeType.Manual, then the label's height will adjust itself to fit the text within it */
		public bool autoAdjustHeight = true;
		/** If True, and labelType = AC_LabelType.Hotspot, DialogueSpeaker or DialogueLine, then the display text buffer can be empty */
		public bool updateIfEmpty = false;
		/** If True, and labelType = AC_LabelType.Hotspot, then the label will not change while the player is moving towards a Hotspot in order to run an interaction */
		public bool showPendingWhileMovingToHotspot = false;

		/** The ID number of the inventory property to show, if labelType = AC_LabelType.InventoryProperty */
		public int itemPropertyID;
		private InvInstance overrideInventoryInstance;
		/** If True, and labelType = AC_LabelType.InventoryProperty, then the total property value will be multipled by the count associated with the item */
		public bool multiplyByItemCount = false;
		/** What kind of item to retrieve properties for, if labelType = AC_LabelType.InventoryProperty (SelectedItem, ItemInInventoryBox, LastClickedItem, MouseOverItem) */
		public InventoryPropertyType inventoryPropertyType;
		/** What Objective text to display, if labelType = AC_LabelType.SelectedObjective */
		public SelectedObjectiveLabelType selectedObjectiveLabelType = SelectedObjectiveLabelType.Title;
		/** The InventoryBox slot number to retrieve properties for, if itemInInventoryBox = ItemInInventoryBox.ItemInSlot */
		public int itemSlotNumber;

		private string newLabel = "";
		private Speech speech;
		private Color speechColour;
		private bool isDuppingSpeech;


		public override void Declare ()
		{
			uiText = null;

			_label = "Label";
			isVisible = true;
			isClickable = false;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			labelType = AC_LabelType.Normal;
			variableID = 0;
			useCharacterColour = false;
			autoAdjustHeight = true;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			newLabel = "";
			updateIfEmpty = false;
			showPendingWhileMovingToHotspot = false;
			inventoryPropertyType = InventoryPropertyType.SelectedItem;
			selectedObjectiveLabelType = SelectedObjectiveLabelType.Title;
			itemPropertyID = 0;
			itemSlotNumber = 0;
			multiplyByItemCount = false;
			#if TextMeshProIsPresent
			hideScrollingCharacters = false;
			#endif

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuLabel newElement = CreateInstance <MenuLabel>();
			newElement.Declare ();
			newElement.CopyLabel (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyLabel (MenuLabel _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiText = null;
			}
			else
			{
				uiText = _element.uiText;
			}

			_label = _element._label;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			labelType = _element.labelType;
			variableID = _element.variableID;
			useCharacterColour = _element.useCharacterColour;
			autoAdjustHeight = _element.autoAdjustHeight;
			updateIfEmpty = _element.updateIfEmpty;
			showPendingWhileMovingToHotspot = _element.showPendingWhileMovingToHotspot;
			newLabel = string.Empty;
			inventoryPropertyType = _element.inventoryPropertyType;
			selectedObjectiveLabelType = _element.selectedObjectiveLabelType;
			itemPropertyID = _element.itemPropertyID;
			itemSlotNumber = _element.itemSlotNumber;
			multiplyByItemCount = _element.multiplyByItemCount;
			#if TextMeshProIsPresent
			hideScrollingCharacters = _element.hideScrollingCharacters;
			#endif

			base.Copy (_element);
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
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuLabel)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			if (source != MenuSource.AdventureCreator)
			{
				#if TextMeshProIsPresent
				uiText = LinkedUiGUI <TMPro.TextMeshProUGUI> (uiText, "Linked Text:", source);
				#else
				uiText = LinkedUiGUI <Text> (uiText, "Linked Text:", source);
				#endif

				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
			}

			labelType = (AC_LabelType) CustomGUILayout.EnumPopup ("Label type:", labelType, apiPrefix + ".labelType", "What kind of text the label displays");
			if (labelType == AC_LabelType.Normal)
			{
				_label = CustomGUILayout.TextArea ("Label text:", _label, apiPrefix + ".label", "The display text");
			}
			else if (source == MenuSource.AdventureCreator)
			{
				_label = CustomGUILayout.TextArea ("Placeholder text:", _label, apiPrefix + ".label");
			}

			if (labelType == AC_LabelType.GlobalVariable)
			{
				variableID = AdvGame.GlobalVariableGUI ("Global Variable:", variableID, "The Global Variable whose value will be displayed");
			}
			else if (labelType == AC_LabelType.DialogueLine)
			{
				useCharacterColour = CustomGUILayout.Toggle ("Use speaker text colour?", useCharacterColour, apiPrefix + ".useCharacterColour", "If True, then the displayed subtitle text will use the speaking character's subtitle text colour");
				if (sizeType == AC_SizeType.Manual && source == MenuSource.AdventureCreator)
				{
					autoAdjustHeight = CustomGUILayout.Toggle ("Auto-adjust height to fit?", autoAdjustHeight, apiPrefix + ".autoAdjustHeight", "If True, then the label's height will adjust itself to fit the text within it");
				}
			}
			else if (labelType == AC_LabelType.DialogueSpeaker)
			{
				useCharacterColour = CustomGUILayout.Toggle ("Use Character text colour?", useCharacterColour, apiPrefix + ".useCharacterColour", "If True, then the displayed text will use the speaking character's subtitle text colour");
			}
			else if (labelType == AC_LabelType.SelectedObjective)
			{
				selectedObjectiveLabelType = (SelectedObjectiveLabelType) CustomGUILayout.EnumPopup ("Objective text:", selectedObjectiveLabelType, apiPrefix + ".selectedObjectiveLabelType", "Which associated text of the selected Objective to display");
			}
			if (labelType == AC_LabelType.Hotspot || labelType == AC_LabelType.DialogueLine || labelType == AC_LabelType.DialogueSpeaker)
			{
				updateIfEmpty = CustomGUILayout.Toggle ("Update if string is empty?", updateIfEmpty, apiPrefix + ".updateIfEmpty", "If True, then the display text buffer can be empty ");

				if (labelType == AC_LabelType.Hotspot)
				{
					showPendingWhileMovingToHotspot = CustomGUILayout.ToggleLeft ("Show pending Interaction while moving to Hotspot?", showPendingWhileMovingToHotspot, apiPrefix + ".showPendingWhileMovingToHotspot", "If True, then the label will not change while the player is moving towards a Hotspot in order to run an interaction");
				}
				#if TextMeshProIsPresent
				else if (labelType == AC_LabelType.DialogueLine && (KickStarter.speechManager.scrollSubtitles || KickStarter.speechManager.scrollNarration))
				{
					hideScrollingCharacters = CustomGUILayout.Toggle ("TMPro Typewriter effect?", hideScrollingCharacters, apiPrefix + ".hideScrollingCharacters", "If True, all speech text will be fed to the TMPro Text component, and shown as speech scrolls. Otherwise, scrolling text will be fed character-by-character.");
				}
				#endif
			}
			else if (labelType == AC_LabelType.InventoryProperty)
			{
				if (AdvGame.GetReferences ().inventoryManager)
				{
					if (AdvGame.GetReferences ().inventoryManager.invVars != null && AdvGame.GetReferences ().inventoryManager.invVars.Count > 0)
					{
						InvVar[] invVars = AdvGame.GetReferences ().inventoryManager.invVars.ToArray ();
						List<string> invVarNames = new List<string>();
						invVarNames.Add ("Item amount");

						int itemPropertyNumber = 0;
						for (int i=0; i<invVars.Length; i++)
						{
							if (invVars[i].id == itemPropertyID)
							{
								itemPropertyNumber = i + 1;
							}
							invVarNames.Add (invVars[i].id + ": " + invVars[i].label);
						}

						itemPropertyNumber = CustomGUILayout.Popup ("Inventory property:", itemPropertyNumber, invVarNames.ToArray (), apiPrefix + ".itemPropertyNumber", "The inventory property to show");
						itemPropertyID = (itemPropertyNumber > 0)
										? invVars[itemPropertyNumber - 1].id
										: -1;

						if (invVars[itemPropertyNumber-1].type == VariableType.Float || invVars[itemPropertyNumber-1].type == VariableType.Integer)
						{
							multiplyByItemCount = CustomGUILayout.Toggle ("Multiply by item count?", multiplyByItemCount, apiPrefix + ".multiplyByItemCount", "If True, then the property's value will be multipled by the item's count.");
						}

						inventoryPropertyType = (InventoryPropertyType) CustomGUILayout.EnumPopup ("Inventory item source:", inventoryPropertyType, apiPrefix + ".inventoryPropertyType", "What kind of item to display properties for");
						if (inventoryPropertyType == InventoryPropertyType.CustomScript)
						{
							EditorGUILayout.HelpBox ("The Inventory Item can be set through this element's OverrideInventoryInstance property", MessageType.Info);
						}
					}
					else
					{
						EditorGUILayout.HelpBox ("No Inventory properties defined!", MessageType.Warning);
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No Inventory Manager assigned!", MessageType.Warning);
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


		public override bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			string newLabel = AdvGame.ConvertGlobalVariableTokenToLocal (label, oldGlobalID, newLocalID);
			return (label != newLabel);
		}


		public override int GetVariableReferences (int _varID)
		{
			int numFound = 0;

			switch (labelType)
			{
				case AC_LabelType.Normal:
					string tokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, _varID);
					if (label.ToLower ().Contains (tokenText))
					{
						numFound++;
					}
					break;

				case AC_LabelType.GlobalVariable:
					if (variableID == _varID)
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

			switch (labelType)
			{
				case AC_LabelType.Normal:
					string oldTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, oldVarID);
					if (label.ToLower ().Contains (oldTokenText))
					{
						string newTokenText = AdvGame.GetVariableTokenText (VariableLocation.Global, newVarID);
						label = label.Replace (oldTokenText, newTokenText);
						numFound++;
					}
					break;

				case AC_LabelType.GlobalVariable:
					if (variableID == oldVarID)
					{
						variableID = newVarID;
						numFound++;
					}
					break;

				default:
					break;
			}

			return numFound;
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


		public override void SetSpeech (Speech _speech)
		{
			isDuppingSpeech = true;
			speech = _speech;
		}


		public override void ClearSpeech ()
		{
			if (labelType == AC_LabelType.DialogueLine || labelType == AC_LabelType.DialogueSpeaker)
			{
				newLabel = string.Empty;
			}
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			base.OnMenuTurnOn (menu);

			PreDisplay (0, Options.GetLanguage (), false);
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (Application.isPlaying)
			{
				UpdateLabelText (languageNumber);
			}
			else
			{
				newLabel = label;
			}
			
			newLabel = AdvGame.ConvertTokens (newLabel, languageNumber);

			if (uiText && Application.isPlaying)
			{
				uiText.text = newLabel;
				UpdateUIElement (uiText);
			}
		}


		protected override string GetLabelToTranslate ()
		{
			return (labelType == AC_LabelType.Normal) ? label : string.Empty;
		}


		/** Updates the label's text buffer.  This is normally done internally at runtime, but can be called manually to update it in Edit mode. */
		public void UpdateLabelText (int languageNumber = 0)
		{
			string _oldLabel = newLabel;

			switch (labelType)
			{
				case AC_LabelType.Normal:
					newLabel = TranslateLabel (languageNumber);
					break;

				case AC_LabelType.Hotspot:
					string _newLabel = string.Empty;

					if (showPendingWhileMovingToHotspot &&
						KickStarter.playerInteraction.GetHotspotMovingTo () && 
						KickStarter.playerCursor.GetSelectedCursorID () == -1 &&
						!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
					{
						_newLabel = KickStarter.playerInteraction.MovingToHotspotLabel;
					}

					if (parentMenu != null && parentMenu.appearType == AppearType.OnInteraction)
					{
						if (parentMenu.TargetHotspot && parentMenu.TargetHotspot != KickStarter.playerInteraction.GetActiveHotspot ())
						{
							return;
						}
					}

					if (string.IsNullOrEmpty (_newLabel))
					{
						_newLabel = KickStarter.playerMenus.GetHotspotLabel ();
					}

					if (!string.IsNullOrEmpty (_newLabel) || updateIfEmpty)
					{
						newLabel = _newLabel;
					}
					break;

				case AC_LabelType.GlobalVariable:
					GVar variable = GlobalVariables.GetVariable (variableID);
					if (variable != null)
					{
						newLabel = variable.GetValue (languageNumber);
					}
					else
					{
						ACDebug.LogWarning ("Label element '" + title + "' cannot display Global Variable " + variableID + " as it does not exist!");
					}
					break;

				case AC_LabelType.ActiveSaveProfile:
					newLabel = KickStarter.options.GetProfileName ();
					break;

				case AC_LabelType.InventoryProperty:
					newLabel = GetPropertyDisplayValue (languageNumber);
					break;

				case AC_LabelType.DialogueLine:
				case AC_LabelType.DialogueSpeaker:
					if (parentMenu != null && parentMenu.IsFadingOut ())
					{
						return;
					}

					UpdateSpeechLink ();

					if (labelType == AC_LabelType.DialogueLine)
					{
						if (speech != null)
						{
							string line = speech.displayText;

							#if TextMeshProIsPresent
							if (uiText && hideScrollingCharacters)
							{
								if (KickStarter.runtimeLanguages.LanguageReadsRightToLeft (Options.GetLanguageName ()))
								{
									ACDebug.LogWarning ("Cannot use TMPro Typewriter effect for RTL speech text.");
								}
								else
								{
									line = speech.log.textWithRichTextTags;
									uiText.maxVisibleCharacters = speech.CurrentCharIndex;
								}
							}
							#endif
							
							if (line != string.Empty || updateIfEmpty)
							{
								newLabel = line;
							}

							if (useCharacterColour)
							{
								speechColour = speech.GetColour ();
								if (uiText)
								{
									uiText.color = speechColour;
								}
							}
						}
						else if (!KickStarter.speechManager.keepTextInBuffer)
						{
							newLabel = string.Empty;
						}
					}
					else if (labelType == AC_LabelType.DialogueSpeaker)
					{
						if (speech != null)
						{
							string line = speech.GetSpeaker (languageNumber);

							if (line != string.Empty || updateIfEmpty || speech.GetSpeakingCharacter () == null)
							{
								newLabel = line;
							}

							if (useCharacterColour)
							{
								speechColour = speech.GetColour ();
								if (uiText)
								{
									uiText.color = speechColour;
								}
							}
						}
						else if (!KickStarter.speechManager.keepTextInBuffer)
						{
							newLabel = string.Empty;
						}
					}
					break;

				case AC_LabelType.DocumentTitle:
					if (Document != null)
					{
						newLabel = KickStarter.runtimeLanguages.GetTranslation (Document.title,
																				Document.titleLineID,
																				languageNumber,
																				Document.GetTranslationType (0));
					}
					break;

				case AC_LabelType.SelectedObjective:
					if (KickStarter.runtimeObjectives.SelectedObjective != null)
					{
						switch (selectedObjectiveLabelType)
						{
							case SelectedObjectiveLabelType.Title:
								newLabel = KickStarter.runtimeObjectives.SelectedObjective.Objective.GetTitle (languageNumber);
								break;

							case SelectedObjectiveLabelType.Description:
								newLabel = KickStarter.runtimeObjectives.SelectedObjective.Objective.GetDescription (languageNumber);
								break;

							case SelectedObjectiveLabelType.StateLabel:
								newLabel = KickStarter.runtimeObjectives.SelectedObjective.CurrentState.GetLabel (languageNumber);
								break;

							case SelectedObjectiveLabelType.StateDescription:
								newLabel = KickStarter.runtimeObjectives.SelectedObjective.CurrentState.GetDescription (languageNumber);
								break;

							case SelectedObjectiveLabelType.StateType:
								newLabel = KickStarter.runtimeObjectives.SelectedObjective.CurrentState.stateType.ToString ();
								break;
						}
					}
					else
					{
						newLabel = string.Empty;
					}
					break;

				case AC_LabelType.ActiveContainer:
					if (KickStarter.playerInput.activeContainer)
					{
						newLabel = KickStarter.playerInput.activeContainer.GetLabel (languageNumber);
					}
					else
					{
						newLabel = string.Empty;
					}
					break;

				default:
					break;
			}

			if (newLabel != _oldLabel && sizeType == AC_SizeType.Automatic && parentMenu != null && parentMenu.menuSource == MenuSource.AdventureCreator)
			{
				parentMenu.Recalculate ();
			}
		}


		private string GetPropertyDisplayValue (int languageNumber)
		{
			switch (inventoryPropertyType)
			{
				case InventoryPropertyType.SelectedItem:
					return GetPropertyDisplayValue (languageNumber, KickStarter.runtimeInventory.SelectedInstance);

				case InventoryPropertyType.LastClickedItem:
					return GetPropertyDisplayValue (languageNumber, KickStarter.runtimeInventory.LastClickedInstance);
			
				case InventoryPropertyType.MouseOverItem:
					return GetPropertyDisplayValue (languageNumber, KickStarter.runtimeInventory.HoverInstance);

				case InventoryPropertyType.CustomScript:
					return GetPropertyDisplayValue (languageNumber, overrideInventoryInstance);

				default:
					return string.Empty;
			}
		}


		/** The Inventory instance to rely on if inventoryPropertyType = InventoryPropertyType.CustomScript */
		public InvInstance OverrideInventoryInstance
		{
			set
			{
				overrideInventoryInstance = value;
			}
		}


		private Document Document
		{
			get
			{
				return KickStarter.runtimeDocuments.ActiveDocument;
			}
		}


		private string GetPropertyDisplayValue (int languageNumber, InvInstance invInstance)
		{
			if (InvInstance.IsValid (invInstance))
			{
				if (itemPropertyID < 0)
				{
					return invInstance.Count.ToString ();
				}

				InvVar invVar = invInstance.GetProperty (itemPropertyID);
				if (invVar != null)
				{
					if (multiplyByItemCount)
					{
						return invVar.GetDisplayValue (languageNumber, invInstance.Count);
					}
					return invVar.GetDisplayValue (languageNumber);
				}
				ACDebug.LogWarning ("Inventory Property with ID " + itemPropertyID + " not found");
			}
			return string.Empty;
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			if (Application.isPlaying)
			{
				switch (labelType)
				{
					case AC_LabelType.DialogueLine:
						if (useCharacterColour)
						{
							_style.normal.textColor = speechColour;
						}

						if (updateIfEmpty || !string.IsNullOrEmpty (newLabel))
						{
							if (autoAdjustHeight && sizeType == AC_SizeType.Manual)
							{
								GUIContent content = new GUIContent (newLabel);
								relativeRect.height = _style.CalcHeight (content, relativeRect.width);
							}
						}
						break;

					case AC_LabelType.DialogueSpeaker:
						if (useCharacterColour)
						{
							_style.normal.textColor = speechColour;
						}
						break;

					default:
						break;
				}
			}

			base.Display (_style, _slot, zoom, isActive);

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}

			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), newLabel, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), newLabel, _style);
			}
		}


		public override string GetLabel (int slot, int languageNumber)
		{
			switch (labelType)
			{
				case AC_LabelType.Normal:
					return TranslateLabel (languageNumber);

				case AC_LabelType.DialogueSpeaker:
					return KickStarter.dialog.GetSpeaker (languageNumber);

				case AC_LabelType.GlobalVariable:
					return GlobalVariables.GetVariable (variableID).GetValue (languageNumber);

				case AC_LabelType.Hotspot:
					return newLabel;

				case AC_LabelType.ActiveSaveProfile:
					if (Application.isPlaying)
					{
						return KickStarter.options.GetProfileName ();
					}
					else
					{
						return label;
					}

				default:
					return string.Empty;
			}
		}


		private void UpdateSpeechLink ()
		{
			if (!isDuppingSpeech)
			{
				speech = KickStarter.dialog.GetLatestSpeech ();
			}
		}


		protected override void AutoSize ()
		{
			int languageNumber = Options.GetLanguage ();

			string _newLabel = (Application.isPlaying) ? newLabel : label;

			if (labelType == AC_LabelType.DialogueLine)
			{
				GUIContent content = new GUIContent (_newLabel);

				#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					if (speech != null && !string.IsNullOrEmpty (speech.displayText))
					{
						// Timeline preview
						content = new GUIContent (speech.displayText);
					}

					AutoSize (content);
					return;
				}
				#endif

				GUIStyle normalStyle = new GUIStyle();
				normalStyle.font = font;
				normalStyle.fontSize = (int) (KickStarter.mainCamera.GetPlayableScreenArea (false).size.x * fontScaleFactor / 100);

				UpdateSpeechLink ();
				if (speech != null)
				{
					string line = " " + speech.FullText + " ";
					content = new GUIContent (line);	//
					AutoSize (content);
				}
			}
			else if (labelType == AC_LabelType.ActiveSaveProfile)
			{
				GUIContent content = new GUIContent (GetLabel (0, 0));
				AutoSize (content);
			}
			else if (string.IsNullOrEmpty (_newLabel) && backgroundTexture)
			{
				GUIContent content = new GUIContent (backgroundTexture);
				AutoSize (content);
			}
			else if (labelType == AC_LabelType.Normal)
			{
				GUIContent content = new GUIContent (TranslateLabel (languageNumber));
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (_newLabel);
				AutoSize (content);
			}
		}


		/** The display text, if labelType = AC_LabelType.Normal */
		public string label
		{
			get
			{
				return _label;
			}
			set
			{
				_label = value;
				if (Application.isPlaying)
				{
					ClearCache ();
				}
			}
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
			return AC.AC_TextType.MenuElement;
		}


		public bool CanTranslate (int index)
		{
			if (labelType == AC_LabelType.Normal)
			{
				return !string.IsNullOrEmpty (label);
			}
			return false;
		}

		#endif

		#endregion

	}

}