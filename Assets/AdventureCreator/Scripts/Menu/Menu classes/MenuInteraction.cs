/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuInteraction.cs"
 * 
 *	This MenuElement displays a cursor icon inside a menu.
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
	 * A MenuElement that displays available interactions inside a Menu.
	 * It is used to allow the player to run Hotspot interactions from within a UI.
	 */
	public class MenuInteraction : MenuElement
	{

		/** The Unity UI Button this is linked to (Unity UI Menus only) */
		public UnityEngine.UI.Button uiButton;
		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** What pointer state registers as a 'click' for Unity UI Menus (PointerClick, PointerDown, PointerEnter) */
		public UIPointerState uiPointerState = UIPointerState.PointerClick;

		/** If True, then only one icon will be shown */
		public bool fixedIcon = true;
		/** The maximum number of icons that can be shown at once, if fixedIcon = false */
		public int maxSlots = 5;
		/** How interactions are displayed (IconOnly, TextOnly, IconAndText) */
		public AC_DisplayType displayType = AC_DisplayType.IconOnly;
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** The ID number of the interaction's associated CursorIcon, if fixedIcon = true */
		public int iconID;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** If True, the element's texture can be set independently of the associated interaction icon set within the Cursor Manager (OnGUI only) */
		public bool overrideTexture;
		/** The element's texture (OnGUI only) */
		public Texture activeTexture;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;

		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif

		private Image uiImage;
		private CursorIcon icon;
		private string[] labels = null;
		private bool isDefaultIcon = false;

		private int[] iconIDs;
		private CursorManager cursorManager;


		public override void Declare ()
		{
			uiButton = null;
			uiSlots = null;
			uiPointerState = UIPointerState.PointerClick;
			uiImage = null;
			uiText = null;
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			maxSlots = 5;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (5f, 5f));
			iconID = -1;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			overrideTexture = false;
			activeTexture = null;
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			fixedIcon = true;
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			
			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuInteraction newElement = CreateInstance <MenuInteraction>();
			newElement.Declare ();
			newElement.CopyInteraction (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyInteraction (MenuInteraction _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiButton = null;
				uiSlots = null;
			}
			else
			{
				uiButton = _element.uiButton;
				uiSlots = (_element.uiSlots != null) ? new UISlot[_element.uiSlots.Length] : new UISlot[0];
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i] = new UISlot (_element.uiSlots[i]);
				}
			}

			uiPointerState = _element.uiPointerState;
			uiText = null;
			uiImage = null;

			maxSlots = _element.maxSlots;
			displayType = _element.displayType;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			iconID = _element.iconID;
			overrideTexture = _element.overrideTexture;
			activeTexture = _element.activeTexture;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			linkUIGraphic = _element.linkUIGraphic;
			fixedIcon = _element.fixedIcon;
			
			base.Copy (_element);

			if (!fixedIcon)
			{
				alternativeInputButton = string.Empty;
			}
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			if (fixedIcon)
			{
				uiButton = LinkUIElement <UnityEngine.UI.Button> (canvas);
				if (uiButton)
				{
					#if TextMeshProIsPresent
					uiText = uiButton.GetComponentInChildren <TMPro.TextMeshProUGUI>();
					#else
					uiText = uiButton.GetComponentInChildren <Text>();
					#endif

					uiImage = uiButton.GetComponentInChildren <Image>();

					if (addEventListeners)
					{
						CreateUIEvent (uiButton, _menu, uiPointerState);
					}
				}
			}
			else
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
		}


		public override GameObject GetObjectToSelect (int slotIndex = 0)
		{
			if (fixedIcon)
			{
				if (uiButton)
				{
					return uiButton.gameObject;
				}
			}
			else
			{
				if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton)
				{
					return uiSlots[slotIndex].uiButton.gameObject;
				}
			}
			return null;
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (fixedIcon)
			{
				if (uiButton)
				{
					return uiButton.GetComponent <RectTransform>();
				}
			}
			else
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					return uiSlots[_slot].GetRectTransform ();
				}
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
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuInteraction)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			GetCursorGUI (apiPrefix, source);
			displayType = (AC_DisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How interactions are displayed");

			if (fixedIcon)
			{ 
				alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
			}

			if (source == MenuSource.AdventureCreator)
			{
				if (displayType != AC_DisplayType.TextOnly && fixedIcon)
				{
					overrideTexture = CustomGUILayout.Toggle ("Override icon texture?", overrideTexture, apiPrefix + ".overrideTexture", "If True, the element's texture can be set independently of the associated interaction icon set within the Cursor Manager");
				}
			}
			else
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();

				if (fixedIcon)
				{
					uiButton = LinkedUiGUI <UnityEngine.UI.Button> (uiButton, "Linked Button:", source, "The Unity UI Button this is linked to");
				}
				else
				{
					uiSlots = ResizeUISlots (uiSlots, maxSlots);
					for (int i=0; i<uiSlots.Length; i++)
					{
						uiSlots[i].LinkedUiGUI (i, source);
					}
				}

				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
				uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}
			CustomGUILayout.EndVertical ();

			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			if (displayType != AC_DisplayType.IconOnly)
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
			if (displayType != AC_DisplayType.TextOnly && overrideTexture)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Active texture:", GUILayout.Width (145f));
				activeTexture = (Texture) EditorGUILayout.ObjectField (activeTexture, typeof (Texture), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
		}
		
		
		private void GetCursorGUI (string apiPrefix, MenuSource source)
		{
			fixedIcon = CustomGUILayout.Toggle ("For fixed icon?", fixedIcon, apiPrefix + ".fixedIcon", "If True, the element will always be associated with the same single icon. If False, the element will represent all available icons.");

			if (fixedIcon)
			{
				numSlots = 1;

				if (AdvGame.GetReferences ().cursorManager && AdvGame.GetReferences().cursorManager.cursorIcons.Count > 0)
				{
					int iconInt = AdvGame.GetReferences ().cursorManager.GetIntFromID (iconID);
					iconInt = EditorGUILayout.Popup ("Cursor:", iconInt, AdvGame.GetReferences ().cursorManager.GetLabelsArray ());
					iconID = AdvGame.GetReferences ().cursorManager.cursorIcons [iconInt].id;
				}
				else
				{
					iconID = -1;
				}
			}
			else
			{
				maxSlots = CustomGUILayout.IntField ("Maximum # of icons:", maxSlots, apiPrefix + ".maxSlots", "The maximum number of icons that can be shown at once");
				if (KickStarter.cursorManager && maxSlots > KickStarter.cursorManager.cursorIcons.Count)
				{
					maxSlots = KickStarter.cursorManager.cursorIcons.Count;
				}

				if (source == MenuSource.AdventureCreator)
				{
					numSlots = CustomGUILayout.IntSlider ("Test slots:", numSlots, 1, maxSlots, apiPrefix + ".numSlots");
					slotSpacing = CustomGUILayout.Slider ("Slot spacing:", slotSpacing, 0f, 30f, apiPrefix + ".slotSpacing");
					orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation");
					if (orientation == ElementOrientation.Grid)
					{
						gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
					}
				}
			}
		}
		
		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (fixedIcon)
			{
				if (uiButton && uiButton.gameObject == gameObject) return true;
				if (linkedUiID == id && id != 0) return true;
			}
			else
			{
				foreach (UISlot uiSlot in uiSlots)
				{
					if (uiSlot.uiButton && uiSlot.uiButton == gameObject) return true;
					if (uiSlot.uiButtonID == id && id != 0) return true;
				}
			}

			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (fixedIcon)
			{
				if (uiButton && uiButton.gameObject == gameObject)
				{
					return 0;
				}
				if (uiText && uiText.gameObject == gameObject)
				{
					return 0;
				}
			}
			else
			{
				for (int i = 0; i < uiSlots.Length; i++)
				{
					if (uiSlots[i].uiButton && uiSlots[i].uiButton == gameObject)
					{
						return 0;
					}
				}
			}
			return base.GetSlotIndex (gameObject);
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			isDefaultIcon = false;

			if (fixedIcon)
			{
				if (Application.isPlaying && KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					if (KickStarter.settingsManager.allowDefaultinteractions &&
						parentMenu != null &&
						parentMenu.TargetHotspot &&
						parentMenu.TargetHotspot.GetFirstUseIcon () == iconID)
					{
						isActive = true;
						isDefaultIcon = true;
					}
					else if (KickStarter.settingsManager.allowDefaultInventoryInteractions &&
							 KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple &&
							 KickStarter.settingsManager.CanSelectItems (false) &&
							 parentMenu.TargetHotspot == null &&
							 InvInstance.IsValid (parentMenu.TargetInvInstance) &&
							 parentMenu.TargetInvInstance.GetFirstStandardIcon () == iconID)
					{
						isActive = true;
						isDefaultIcon = true;
					}
				}

				if (uiButton)
				{
					UpdateUISelectable (uiButton, uiSelectableHideStyle);
					
					if (displayType != AC_DisplayType.IconOnly && uiText)
					{
						uiText.text = labels[0];
					}
					if (displayType == AC_DisplayType.IconOnly && uiImage && icon != null && icon.isAnimated)
					{
						uiImage.sprite = icon.GetAnimatedSprite (isActive);
					}

					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot &&
						iconID == KickStarter.playerInteraction.GetActiveUseButtonIconID ())
					{
						// Select through script, not by mouse-over
						uiButton.Select ();
					}
				}
			}
			else
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiSelectableHideStyle);

					CursorIcon cursorIcon = GetIconAtSlot (_slot);
					if (cursorIcon != null)
					{
						if (displayType == AC_DisplayType.IconOnly || displayType == AC_DisplayType.IconAndText)
						{
							uiSlots[_slot].SetImageAsSprite (cursorIcon.GetAnimatedSprite (isActive));
						}
						if (displayType == AC_DisplayType.TextOnly || displayType == AC_DisplayType.IconAndText)
						{
							uiSlots[_slot].SetText (labels[_slot]);
						}
					}
				}
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

			if (fixedIcon)
			{
				if (displayType != AC_DisplayType.IconOnly)
				{
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), labels[0], _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (relativeRect, zoom), labels[0], _style);
					}
				}
				else
				{
					GUI.Label (ZoomRect (relativeRect, zoom), string.Empty, _style);
				}

				if (overrideTexture)
				{
					if (iconID >= 0 && KickStarter.playerCursor.GetSelectedCursorID () == iconID && activeTexture)
					{
						GUI.DrawTexture (ZoomRect (relativeRect, zoom), activeTexture, ScaleMode.StretchToFill, true, 0f);
					}
				}
				else
				{
					if (displayType != AC_DisplayType.TextOnly && icon != null)
					{
						icon.DrawAsInteraction (ZoomRect (relativeRect, zoom), isActive);
					}
				}
			}
			else
			{
				if (displayType != AC_DisplayType.IconOnly)
				{
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
					}
				}
				else
				{
					GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), string.Empty, _style);
				}

				CursorIcon _icon = GetIconAtSlot (_slot);
				if (displayType != AC_DisplayType.TextOnly && _icon != null)
				{
					_icon.DrawAsInteraction (ZoomRect (GetSlotRectRelative (_slot), zoom), isActive);
				}
			}
		}


		/**
		 * <summary>Recalculates display for a particular inventory item.</summary>
		 * <param name = "invInstance">The inventory item instance to recalculate the Menus's display for</param>
		 */
		public void MatchInteractions (InvInstance invInstance)
		{
			if (!fixedIcon) return;

			bool match = false;
			foreach (InvInteraction interaction in invInstance.Interactions)
			{
				if (interaction.icon.id == iconID)
				{
					match = true;
					break;
				}
			}

			IsVisible = match;
		}
		

		/**
		 * <summary>Recalculates display for a particular set of Hotspot Buttons.</summary>
		 * <param name = "parentMenu">The Menu that the element is a part of</param>
		 * <param name = "buttons">A List of Button classes to recalculate the Menus's display for</param>
		 */
		public void MatchInteractions (List<AC.Button> buttons)
		{
			if (!fixedIcon) return;

			bool match = false;
			
			foreach (AC.Button button in buttons)
			{
				if (button.iconID == iconID && !button.isDisabled)
				{
					match = true;
					break;
				}
			}

			IsVisible = match;
		}


		/**
		 * <summary>Recalculates display for an "Use" Hotspot Button.</summary>
		  * <param name = "button">A Button class to recalculate the Menus's display for</param>
		 */
		public void MatchUseInteraction (AC.Button button)
		{
			if (fixedIcon && button.iconID == iconID && !button.isDisabled)
			{
				IsVisible = true;
			}
		}


		/**
		 * <summary>Recalculates display for a given cursor icon ID.</summary>
		 * <param name = "parentMenu">The Menu that the element is a part of</param>
		 * <param name = "_iconID">The ID number of the CursorIcon in CursorManager</param>
		 */
		public void MatchInteraction (int _iconID)
		{
			if (fixedIcon && _iconID == iconID)
			{
				IsVisible = true;
			}
		}


		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, iconIDs.Length, amount);
			}
		}
		

		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (numSlots == 0 || fixedIcon)
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
				if ((maxSlots + offset) >= iconIDs.Length)
				{
					return false;
				}
			}
			return true;
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			base.OnMenuTurnOn (menu);

			if (!fixedIcon)
			{
				LimitOffset ();
			}
		}


		protected override int MaxSlotsForOffset
		{
			get
			{
				if (!fixedIcon)
				{
					return iconIDs.Length;
				}
				return 0;
			}
		}


		public override string GetLabel (int slot, int languageNumber)
		{
			if (fixedIcon)
			{
				slot = 0;
			}
			if (labels != null && slot < labels.Length)
			{
				return labels[slot];
			}
			return string.Empty;
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


		public override void RecalculateSize (MenuSource source)
		{
			if (AdvGame.GetReferences ().cursorManager)
			{
				if (fixedIcon)
				{
					CursorIcon _icon = AdvGame.GetReferences ().cursorManager.GetCursorIconFromID (iconID);
					if (_icon != null)
					{
						labels = new string[1];
						icon = _icon;
						if (Application.isPlaying)
						{
							labels[0] = KickStarter.runtimeLanguages.GetTranslation (_icon.label, _icon.lineID, Options.GetLanguage (), _icon.GetTranslationType (0));
						}
						else
						{
							labels[0] = _icon.label;
						}
						icon.Reset ();
					}
				}
				else
				{
					List<int> _iconIDs = new List<int>();
					if (!KickStarter.settingsManager.autoHideInteractionIcons || !Application.isPlaying)
					{
						foreach (CursorIcon icon in KickStarter.cursorManager.cursorIcons)
						{
							_iconIDs.Add (icon.id);
						}
					}
					else if (parentMenu != null)
					{
						if (parentMenu.TargetHotspot)
						{
							foreach (Button button in parentMenu.TargetHotspot.useButtons)
							{
								if (!button.isDisabled)
								{
									_iconIDs.Add (button.iconID);
								}
							}
						}
						else if (InvInstance.IsValid (parentMenu.TargetInvInstance))
						{
							foreach (InvInteraction interaction in parentMenu.TargetInvInstance.Interactions)
							{
								_iconIDs.Add (interaction.icon.id);
							}
						}
					}
					
					iconIDs = _iconIDs.ToArray ();

					labels = new string[iconIDs.Length];
					for (int i=0; i<iconIDs.Length; i++)
					{
						CursorIcon _icon = KickStarter.cursorManager.GetCursorIconFromID (iconIDs[i]);
						if (Application.isPlaying)
						{
							labels[i] = (_icon != null) ? KickStarter.runtimeLanguages.GetTranslation (_icon.label, _icon.lineID, Options.GetLanguage (), _icon.GetTranslationType (0)) : string.Empty;
						}
						else
						{
							labels[i] = (icon != null) ? _icon.label : string.Empty;
						}
					}

					if (Application.isPlaying)
					{
						numSlots = iconIDs.Length;
						if (numSlots > maxSlots)
						{
							numSlots = maxSlots;
						}
					}

					LimitOffset ();
				}
			}

			base.RecalculateSize (source);
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}
			
			if (_mouseState != MouseState.SingleClick && _menu.menuSource == MenuSource.AdventureCreator)
			{
				return false;
			}

			KickStarter.playerInteraction.ClickInteractionIcon (_menu, GetIconIDAtSlot (_slot));

			return base.ProcessClick (_menu, _slot, _mouseState);
		}
		
		
		protected override void AutoSize ()
		{
			CursorIcon _icon = GetIconAtSlot (0);
			if (_icon == null)
			{
				return;
			}

			if (displayType == AC_DisplayType.IconOnly && _icon.texture)
			{
				GUIContent content = new GUIContent (_icon.texture);
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (_icon.GetLabel (Options.GetLanguage ()));
				AutoSize (content);
			}
		}


		/** If using Choose Interaction Then Hotspot mode, and default interactions are enabled, then this is True if the active Hotspot's first-enabled Use interaction uses this icon */
		public bool IsDefaultIcon
		{
			get
			{
				return (fixedIcon && isDefaultIcon);
			}
		}


		/**
		 * <summary>Gets the ID of the cursor icon associated with a given slot</summary>
		 * <param name="_slot">The ID of the icon at this slot. If fixedIcon = true, this parameter is irrelevant</param>
		 * <returns>The ID of the cursor icon.</returns>
		 */
		public int GetIconIDAtSlot (int _slot)
		{
			if (fixedIcon)
			{
				return iconID;
			}

			if ((_slot + offset) < iconIDs.Length)
				return iconIDs[_slot+offset];
			return -1;
		}


		private CursorIcon GetIconAtSlot (int _slot)
		{
			int iconID = GetIconIDAtSlot (_slot);
			return KickStarter.cursorManager.GetCursorIconFromID (iconID);
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (fixedIcon && uiButton && !uiButton.interactable) return string.Empty;

			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return string.Empty;
			}
			#endif

			int slotIconID = GetIconIDAtSlot (_slot);
			if (KickStarter.cursorManager.addHotspotPrefix)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction)
				{
					if (KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
					{
						// Don't override, refer to the clicked InventoryBox or Hotspot
						return string.Empty;
					}

					if (InvInstance.IsValid (parentMenu.TargetInvInstance))
					{
						string prefix = KickStarter.cursorManager.GetLabelFromID (slotIconID, _language);
						string itemName = parentMenu.TargetInvInstance.InvItem.GetLabel (_language);
						if (parentMenu.TargetInvInstance.InvItem.canBeLowerCase && !string.IsNullOrEmpty (prefix))
						{
							itemName = itemName.ToLower ();
						}

						return AdvGame.CombineLanguageString (prefix, itemName, _language);
					}

					if (KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
					{
						return string.Empty;
					}

					if (parentMenu.TargetHotspot)
					{
						string prefix = KickStarter.cursorManager.GetLabelFromID (slotIconID, _language);
						string hotspotName = parentMenu.TargetHotspot.GetName (_language);
						if (parentMenu.TargetHotspot.canBeLowerCase && !string.IsNullOrEmpty (prefix))
						{
							hotspotName = hotspotName.ToLower ();
						}

						return AdvGame.CombineLanguageString (prefix, hotspotName, _language);
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot)
				{
					if (KickStarter.settingsManager.ShowHoverInteractionInHotspotLabel ())
					{
						if (KickStarter.playerCursor.GetSelectedCursor () == -1)
						{
							return KickStarter.cursorManager.GetLabelFromID (slotIconID, _language);
						}
					}
				}
			}

			return string.Empty;
		}

	}

}