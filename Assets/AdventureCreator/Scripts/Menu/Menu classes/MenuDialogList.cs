/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuDialogList.cs"
 * 
 *	This MenuElement lists the available options of the active conversation,
 *	and runs them when clicked on.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	/** A MenuElement that lists the available options in the active Conversation, and runs their interactions when clicked on. */
	public class MenuDialogList : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** How the Conversation's dialogue options are displayed (IconOnly, TextOnly, IconAndText) */
		public ConversationDisplayType displayType = ConversationDisplayType.TextOnly;
		/** A temporary dialogue option icon, used for test purposes when the game is not running */
		public Texture2D testIcon = null;
		/** The text alignment */
		public TextAnchor anchor;
		/** Deprecated */
		public bool fixedOption;
		/** The slot index or ID of the dialogue option to show, if elementSlotMapping = FixedSlotIndex or FixedOptionID */
		public int optionToShow;
		/** The maximum number of dialogue options that can be shown at once */
		public int maxSlots = 10;
		/** If True, then options that have already been clicked can be displayed in a different colour */
		public bool markAlreadyChosen = false;
		/** The font colour for options already chosen (If markAlreadyChosen = True, OnGUI only) */
		public Color alreadyChosenFontColour = Color.white;
		/** The font colour when the option is highlighted but has already been chosen (OnGUI only) */
		public Color alreadyChosenFontHighlightedColour = Color.white;
		/** (Deprecated) */
		public bool showIndexNumbers = false;
		/** If displayType = ConversationDisplayType.TextOnly, how each option's index number is prefixed to the label */
		public IndexPrefixDisplay indexPrefixDisplay = IndexPrefixDisplay.None;

		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;
		/** If True, then the offset value will be reset when the parent menu is turned on for the same Conversation that it last displayed */
		public bool resetOffsetWhenRestart = true;
		/** If True, and the element is scrolled by an offset larger than the number of new options to show, then the offset amount will be reduced to only show those new options. */
		public bool limitMaxScroll = true;

		/** How to map this element to a Conversation's dialogue options */
		public ElementSlotMapping elementSlotMapping = ElementSlotMapping.List;
		
		private Conversation linkedConversation;
		private Conversation overrideConversation;
		private int numOptions = 0;
		private DialogueOptionReference[] optionReferences = new DialogueOptionReference[0];


		public override void Declare ()
		{
			uiSlots = null;

			isVisible = true;
			isClickable = true;
			fixedOption = false;
			elementSlotMapping = ElementSlotMapping.List;
			displayType = ConversationDisplayType.TextOnly;
			testIcon = null;
			optionToShow = 1;
			numSlots = 0;
			SetSize (new Vector2 (20f, 5f));
			maxSlots = 10;
			anchor = TextAnchor.MiddleLeft;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			markAlreadyChosen = false;
			alreadyChosenFontColour = Color.white;
			alreadyChosenFontHighlightedColour = Color.white;
			showIndexNumbers = false;
			uiHideStyle = UIHideStyle.DisableObject;
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			resetOffsetWhenRestart = true;
			limitMaxScroll = true;
			indexPrefixDisplay = IndexPrefixDisplay.None;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuDialogList newElement = CreateInstance <MenuDialogList>();
			newElement.Declare ();
			newElement.CopyDialogList (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyDialogList (MenuDialogList _element, bool ignoreUnityUI)
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

			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			displayType = _element.displayType;
			testIcon = _element.testIcon;
			anchor = _element.anchor;
			fixedOption = _element.fixedOption;
			elementSlotMapping = _element.elementSlotMapping;
			optionToShow = _element.optionToShow;
			maxSlots = _element.maxSlots;
			markAlreadyChosen = _element.markAlreadyChosen;
			alreadyChosenFontColour = _element.alreadyChosenFontColour;
			alreadyChosenFontHighlightedColour = _element.alreadyChosenFontHighlightedColour;
			showIndexNumbers = _element.showIndexNumbers;
			uiHideStyle = _element.uiHideStyle;
			linkUIGraphic = _element.linkUIGraphic;
			resetOffsetWhenRestart = _element.resetOffsetWhenRestart;
			limitMaxScroll = _element.limitMaxScroll;
			indexPrefixDisplay = _element.indexPrefixDisplay;

			base.Copy (_element);

			Upgrade ();
		}


		private void Upgrade ()
		{
			if (fixedOption)
			{
				fixedOption = false;
				elementSlotMapping = ElementSlotMapping.FixedSlotIndex;
			}
			if (showIndexNumbers)
			{
				showIndexNumbers = false;
				indexPrefixDisplay = IndexPrefixDisplay.GlobalOrder;
			}
		}


		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			int i=0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements (canvas, linkUIGraphic);

				if (displayType == ConversationDisplayType.TextOnly)
				{
					uiSlot.CanSetOriginalImage = true;
				}

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
			if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton)
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
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuDialogList)";

			Upgrade ();

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();
			elementSlotMapping = (ElementSlotMapping) CustomGUILayout.EnumPopup ("Map to:", elementSlotMapping, apiPrefix + ".elementSlotMapping", "How to map this element to a Conversation's dialogue options");
			switch (elementSlotMapping)
			{
				case ElementSlotMapping.List:
				default:
					{
						maxSlots = CustomGUILayout.IntField ("Maximum # of slots:", maxSlots, apiPrefix + ".maxSlots", "The maximum number of dialogue options that can be shown at once");
						if (maxSlots < 0) maxSlots = 0;
						resetOffsetWhenRestart = CustomGUILayout.Toggle ("Reset offset when turn on?", resetOffsetWhenRestart, apiPrefix + ".resetOffsetWhenRestart", "If True, then the offset value will be reset when the parent menu is turned on for the same Conversation that it last displayed");
						limitMaxScroll = CustomGUILayout.Toggle ("Limit maximum scroll?", limitMaxScroll, apiPrefix + ".limitMaxScroll", "If True, and the element is scrolled by an offset larger than the number of new options to show, then the offset amount will be reduced to only show those new options.");

						if (source == MenuSource.AdventureCreator)
						{
							if (maxSlots > 1)
							{
								numSlots = CustomGUILayout.IntSlider ("Test slots:", numSlots, 1, maxSlots, apiPrefix + ".numSlots");
								slotSpacing = CustomGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 30f, apiPrefix + ".slotSpacing");
								orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation");
								if (orientation == ElementOrientation.Grid)
								{
									gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
								}
							}
							else
							{
								numSlots = Mathf.Max (0, maxSlots);
							}
						}
					}
					break;

				case ElementSlotMapping.FixedSlotIndex:
					{
						numSlots = 1;
						slotSpacing = 0f;
						optionToShow = CustomGUILayout.IntSlider ("Slot index to display:", optionToShow, 1, 20, apiPrefix + ".optionToShow", "The slot index of the dialogue option to show");
					}
					break;

				case ElementSlotMapping.FixedOptionID:
					{
						numSlots = 1;
						slotSpacing = 0f;
						optionToShow = CustomGUILayout.IntSlider ("Option ID to display:", optionToShow, 1, 20, apiPrefix + ".optionToShow", "The ID of the dialogue option to show");
					}
					break;
			}
			

			displayType = (ConversationDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How the Conversation's dialogue options are displayed");
			if (displayType == ConversationDisplayType.IconAndText && source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("'Icon And Text' mode is only available for Unity UI-based Menus.", MessageType.Warning);
			}

			markAlreadyChosen = CustomGUILayout.Toggle ("Mark options already used?", markAlreadyChosen, apiPrefix + ".markAlreadyChosen", "If True, then options that have already been clicked can be displayed in a different colour");
			if (markAlreadyChosen)
			{
				alreadyChosenFontColour = CustomGUILayout.ColorField ("'Already chosen' colour:", alreadyChosenFontColour, apiPrefix + ".alreadyChosenFontColour", "The font colour for options already chosen");
				alreadyChosenFontHighlightedColour = CustomGUILayout.ColorField ("'Already chosen' highlighted colour:", alreadyChosenFontHighlightedColour, apiPrefix + ".alreadyChosenFontHighlightedColour", "The font colour when the option is highlighted but has already been chosen");
			}

			if (source != MenuSource.AdventureCreator)
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				if (elementSlotMapping == ElementSlotMapping.List)
				{
					uiSlots = ResizeUISlots (uiSlots, maxSlots);
				}
				else
				{
					uiSlots = ResizeUISlots (uiSlots, 1);
				}

				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}

			if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
			{
				indexPrefixDisplay = (IndexPrefixDisplay) CustomGUILayout.EnumPopup ("Index prefix display:", indexPrefixDisplay, apiPrefix + ".indexPrefixDisplay", "Allows an option's index number to be displayed at the front of its label");
			}

			ChangeCursorGUI (menu);
			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			if (displayType != ConversationDisplayType.IconOnly)
			{
				anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
				textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
				if (textEffects != TextEffects.None)
				{
					outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The effect thickness");
					effectColour = CustomGUILayout.ColorField ("Effect colour:", effectColour, apiPrefix + ".effectColour", "The effect colour");
				}
			}
		}


		protected override void ShowTextureGUI (string apiPrefix)
		{
			if (displayType == ConversationDisplayType.IconOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Test icon:", GUILayout.Width (145f));
				testIcon = (Texture2D) EditorGUILayout.ObjectField (testIcon, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
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


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].CanOverrideHotspotLabel) return string.Empty;

			if (displayType == ConversationDisplayType.IconOnly)
			{
				if (_slot <= optionReferences.Length && optionReferences[_slot] != null)
				{
					return optionReferences[_slot].Label;
				}
			}
			return string.Empty;
		}
		

		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (elementSlotMapping != ElementSlotMapping.List)
			{
				_slot = 0;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && _slot < uiSlots.Length)
				{
					int slotsToLimitTo = numSlots;
					if (elementSlotMapping == ElementSlotMapping.List &&
						!limitMaxScroll &&
						numSlots == maxSlots)
					{
						int dynamicOffset = numSlots + offset - linkedConversation.GetNumEnabledOptions ();
						if (dynamicOffset >= 0)
						{
							slotsToLimitTo = numSlots - dynamicOffset;
						}
					}
					LimitUISlotVisibility (uiSlots, slotsToLimitTo, uiHideStyle);
					
					DialogueOptionReference optionReference = optionReferences[_slot];
					if (optionReference != null)
					{
						if (displayType == ConversationDisplayType.IconOnly || displayType == ConversationDisplayType.IconAndText)
						{
							uiSlots[_slot].SetImageAsSprite (optionReference.Icon.GetAnimatedSprite (isActive));
						}
						if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
						{
							uiSlots[_slot].SetText (optionReference.Label);
						}

						uiSlots[_slot].ShowUIElement (uiHideStyle);
					}
				}
			}
			else
			{
				string fullText;

				switch (elementSlotMapping)
				{
					case ElementSlotMapping.List:
					default:
						fullText = "Dialogue option " + _slot.ToString ();
						fullText = AddIndexNumber (fullText, _slot + 1);
						break;

					case ElementSlotMapping.FixedSlotIndex:
					case ElementSlotMapping.FixedOptionID:
						fullText = "Dialogue option " + optionToShow.ToString ();
						fullText = AddIndexNumber (fullText, optionToShow);
						break;
				}
				
				if (optionReferences == null || optionReferences.Length != numSlots)
				{
					optionReferences = new DialogueOptionReference[numSlots];
				}
				optionReferences[_slot] = new DialogueOptionReference (fullText, null, false);
			}
		}


		private string AddIndexNumber (string _label, int _i)
		{
			switch (indexPrefixDisplay)
			{
				case IndexPrefixDisplay.GlobalOrder:
					return _i.ToString () + ". " + _label;

				case IndexPrefixDisplay.DisplayOrder:
					return (_i - offset).ToString () + ". " + _label;

				default:
					return _label;
			}
		}


		private DialogueOptionReference[] AddExtraNulls (DialogueOptionReference[] _optionReferences)
		{
			if (elementSlotMapping == ElementSlotMapping.List &&
				!limitMaxScroll &&
				_optionReferences.Length > 0 &&
				_optionReferences.Length % maxSlots != 0)
			{
				List<DialogueOptionReference> tempList = new List<DialogueOptionReference>();
				for (int i = 0; i < _optionReferences.Length; i++)
				{
					tempList.Add (_optionReferences[i]);
				}

				while (tempList.Count % maxSlots != 0)
				{
					tempList.Add (null);
				}

				return tempList.ToArray ();
			}
			return _optionReferences;
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			if (_slot >= optionReferences.Length || optionReferences[_slot] == null)
			{
				return;
			}

			if (elementSlotMapping != ElementSlotMapping.List)
			{
				_slot = 0;
			}

			if (markAlreadyChosen)
			{
				if (optionReferences[_slot].Chosen)
				{
					if (isActive)
					{
						_style.normal.textColor = alreadyChosenFontHighlightedColour;
					}
					else
					{
						_style.normal.textColor = alreadyChosenFontColour;
					}
				}
				else if (isActive)
				{
					_style.normal.textColor = fontHighlightColor;
				}
				else
				{
					_style.normal.textColor = fontColor;
				}
			}

			_style.wordWrap = true;
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) (_style.fontSize * zoom);
			}

			switch (displayType)
			{
				case ConversationDisplayType.TextOnly:
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), optionReferences[_slot].Label, _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), optionReferences[_slot].Label, _style);
					}
					break;

				default:
					if (Application.isPlaying && optionReferences[_slot].Icon != null)
					{
						optionReferences[_slot].Icon.DrawAsInteraction (ZoomRect (GetSlotRectRelative (_slot), zoom), isActive);
					}
					else if (testIcon != null)
					{
						GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), testIcon, ScaleMode.StretchToFill, true, 0f);
					}

					GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), string.Empty, _style);
					break;
			}
		}
		

		public override void RecalculateSize (MenuSource source)
		{
			if (Application.isPlaying)
			{
				if (linkedConversation)
				{
					numOptions = linkedConversation.GetCount ();
					
					switch (elementSlotMapping)
					{
						case ElementSlotMapping.List:
						default:
							{
								numSlots = numOptions;
								if (numSlots > maxSlots)
								{
									numSlots = maxSlots;
								}

								optionReferences = new DialogueOptionReference[numOptions];
								for (int i = 0; i < numSlots; i++)
								{
									if (linkedConversation.SlotIsAvailable (i + offset))
									{
										string label = linkedConversation.GetOptionName (i + offset);
										label = AddIndexNumber (label, i + offset + 1);

										CursorIconBase icon = new CursorIconBase ();
										icon.Copy (linkedConversation.GetOptionIcon (i + offset));

										bool chosen = linkedConversation.OptionHasBeenChosen (i + offset);
										optionReferences[i] = new DialogueOptionReference (label, icon, chosen);
									}
									else
									{
										optionReferences[i] = null;
									}
								}

								if (markAlreadyChosen && source != MenuSource.AdventureCreator)
								{
									for (int i = 0; i < optionReferences.Length; i++)
									{
										bool chosen = optionReferences[i] != null && optionReferences[i].Chosen;

										if (uiSlots.Length > i)
										{
											if (chosen)
											{
												uiSlots[i].SetColours (alreadyChosenFontColour, alreadyChosenFontHighlightedColour);
											}
											else
											{
												uiSlots[i].RestoreColour ();
											}
										}
									}
								}

								optionReferences = AddExtraNulls (optionReferences);
								LimitOffset ();
							}
							break;

						case ElementSlotMapping.FixedSlotIndex:
							{
								if (numOptions < optionToShow)
								{
									numSlots = 0;
									optionReferences = new DialogueOptionReference[0];
								}
								else
								{
									numSlots = 1;
									optionReferences = new DialogueOptionReference[1];

									string label = linkedConversation.GetOptionName (optionToShow - 1);
									label = AddIndexNumber (label, optionToShow);

									CursorIconBase icon = new CursorIconBase ();
									icon.Copy (linkedConversation.GetOptionIcon (optionToShow - 1));

									bool chosen = linkedConversation.OptionHasBeenChosen (optionToShow - 1);
									optionReferences[0] = new DialogueOptionReference (label, icon, chosen);
								}
							}
							break;
							
						case ElementSlotMapping.FixedOptionID:
							{
								if (linkedConversation.OptionWithIDIsActive (optionToShow))
								{
									numSlots = 1;
									optionReferences = new DialogueOptionReference[1];

									string label = linkedConversation.GetOptionNameWithID (optionToShow);
									label = AddIndexNumber (label, optionToShow);

									CursorIconBase icon = new CursorIconBase ();
									icon.Copy (linkedConversation.GetOptionIconWithID (optionToShow));

									bool chosen = linkedConversation.OptionWithIDHasBeenChosen (optionToShow);
									optionReferences[0] = new DialogueOptionReference (label, icon, chosen);
								}
								else
								{
									numSlots = 0;
									optionReferences = new DialogueOptionReference[0];
								}
							}
							break;
					}
				}
				else
				{
					numSlots = 0;
				}
			}
			else if (elementSlotMapping != ElementSlotMapping.List)
			{
				numSlots = 1;
				offset = 0;
				optionReferences = new DialogueOptionReference[numSlots];

				PreDisplay (0, 0, false);
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


		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, optionReferences.Length, amount);
			}
		}
		

		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0)
			{
				return false;
			}

			switch (shiftType)
			{
				case AC_ShiftInventory.ShiftPrevious:
					return offset > 0;

				case AC_ShiftInventory.ShiftNext:
					return (maxSlots + offset) < numOptions;

				default:
					return true;
			}
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			base.OnMenuTurnOn (menu);

			Conversation oldConversation = linkedConversation;
			linkedConversation = (overrideConversation) ? overrideConversation : KickStarter.playerInput.activeConversation;
			if (linkedConversation)
			{
				linkedConversation.LinkedDialogList = this;
			}

			if (oldConversation != linkedConversation || resetOffsetWhenRestart)
			{
				offset = 0;
			}

			if (linkedConversation && elementSlotMapping == ElementSlotMapping.List)
			{
				LimitOffset ();
			}
		}


		protected override int MaxSlotsForOffset
		{
			get
			{
				return optionReferences.Length;
				/*Conversation linkedConversation = (overrideConversation) ? overrideConversation : KickStarter.playerInput.activeConversation;
				if (linkedConversation && elementSlotMapping == ElementSlotMapping.List)
				{
					return (linkedConversation.GetCount());
				}
				return 0;*/
			}
		}


		public override string GetLabel (int slot, int languageNumber)
		{
			if (slot < optionReferences.Length && optionReferences[slot] != null)
			{
				return optionReferences[slot].Label;
			}
			return string.Empty;
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
		

		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState != GameState.DialogOptions)
			{
				return false;
			}

			if (_mouseState != MouseState.SingleClick && _menu.menuSource == MenuSource.AdventureCreator)
			{
				return false;
			}
			
			if (linkedConversation && 
				(linkedConversation == overrideConversation || (overrideConversation == null && KickStarter.playerInput.activeConversation)))
			{
				switch (elementSlotMapping)
				{
					case ElementSlotMapping.List:
						linkedConversation.RunOption (_slot + offset);
						break;

					case ElementSlotMapping.FixedSlotIndex:
						linkedConversation.RunOption (optionToShow - 1);
						break;

					case ElementSlotMapping.FixedOptionID:
						linkedConversation.RunOptionWithID (optionToShow);
						break;
				}
			}

			return base.ProcessClick (_menu, _slot, _mouseState);
		}


		/** If set, then this Conversation will be used instead of the global 'active' one.  This must be set either before the Menu is turned on, or within the OnMenuTurnOn custom event.  Note that its Menu's 'Appear type' should not be set to 'During Conversation', and that the Conversation's dialogue options should not be overridden with the 'Dialogue: Start conversation' Action. */
		public Conversation OverrideConversation
		{
			set
			{
				overrideConversation = value;
			}
		}

		
		protected override void AutoSize ()
		{
			if (displayType == ConversationDisplayType.IconOnly)
			{
				AutoSize (new GUIContent (testIcon));
			}
			else
			{
				AutoSize (new GUIContent ("Dialogue option 0"));
			}
		}


		/**
		 * <summary>Gets a ButtonDialog data-container class for a dialogue option within the active Conversation</summary>
		 * <param name = "slotIndex">The element's slot index number associated with the dialogue option</param>
		 * <returns>The ButtonDialog data-container class for a dialogue option within the active Conversation</returns>
		 */
		public ButtonDialog GetDialogueOption (int slotIndex)
		{
			slotIndex += offset;
			if (linkedConversation)
			{
				return linkedConversation.GetOption (slotIndex);
			}
			return null;
		}


		private class DialogueOptionReference
		{

			public string Label { get; private set; }
			public CursorIconBase Icon { get; private set; }
			public bool Chosen { get; private set; }


			public DialogueOptionReference (string label, CursorIconBase icon, bool chosen)
			{
				Label = label;
				Icon = icon;
				Chosen = chosen;
			}

		}

	}

}
