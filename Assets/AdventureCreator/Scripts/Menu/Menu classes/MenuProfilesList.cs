/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuProfilesList.cs"
 * 
 *	This MenuElement handles the display of any save profiles recorded.
 * 
 */

using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;	
#endif

namespace AC
{

	/** This MenuElement lists any save profiles found on by SaveSystem. */
	public class MenuProfilesList : MenuElement
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
		/** The maximum number of profiles that can be displayed at once */
		public int maxSlots = 5;
		/** An ActionListAsset that can be run when a profile is clicked on */
		public ActionListAsset actionListOnClick;
		/** If True, then the current active profile will also be listed */
		public bool showActive = true;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** If True, then the profile will be switched to once its slot is clicked on */
		public bool autoHandle = true;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;

		/** If True, then only one profile slot will be shown */
		public bool fixedOption;
		/** The index number of the profile to show, if fixedOption = true */
		public int optionToShow = 0;
		/** If >=0, The ID number of the integer ActionParameter in actionListOnClick to set to the index number of the slot clicked */
		public int parameterID = -1;

		private string[] labels = null;


		public override void Declare ()
		{
			uiSlots = null;
			
			isVisible = true;
			isClickable = true;
			numSlots = 1;
			maxSlots = 5;
			showActive = true;

			SetSize (new Vector2 (20f, 5f));
			anchor = TextAnchor.MiddleCenter;

			actionListOnClick = null;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			uiHideStyle = UIHideStyle.DisableObject;

			fixedOption = false;
			optionToShow = 0;
			autoHandle = true;
			parameterID = -1;
			linkUIGraphic = LinkUIGraphic.ImageComponent;

			base.Declare ();
		}
		

		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuProfilesList newElement = CreateInstance <MenuProfilesList>();
			newElement.Declare ();
			newElement.CopyProfilesList (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyProfilesList (MenuProfilesList _element, bool ignoreUnityUI)
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
			anchor = _element.anchor;
			maxSlots = _element.maxSlots;
			actionListOnClick = _element.actionListOnClick;
			showActive = _element.showActive;
			uiHideStyle = _element.uiHideStyle;
			autoHandle = _element.autoHandle;
			parameterID = _element.parameterID;
			fixedOption = _element.fixedOption;
			optionToShow = _element.optionToShow;
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
			if (uiSlots != null && uiSlots.Length > slotIndex && uiSlots[slotIndex].uiButton)
			{
				return uiSlots[0].uiButton.gameObject;
			}
			return null;
		}


		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
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
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuProfilesList)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			fixedOption = CustomGUILayout.Toggle ("Fixed Profile ID only?", fixedOption, apiPrefix + ".fixedOption", "If True, then only one profile slot will be shown");
			if (fixedOption)
			{
				numSlots = 1;
				slotSpacing = 0f;
				optionToShow = CustomGUILayout.IntField ("ID to display:", optionToShow, apiPrefix + ".optionToShow", "The index number of the profile to show");
			}
			else
			{
				showActive = CustomGUILayout.Toggle ("Include active?", showActive, apiPrefix + ".showActive", "If True, then the current active profile will also be listed");
				maxSlots = CustomGUILayout.IntField ("Maximum number of slots:", maxSlots, apiPrefix + ".maxSlots", "The maximum number of profiles that can be displayed at once");
				if (maxSlots < 0) maxSlots = 0;

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

			autoHandle = CustomGUILayout.Toggle ("Switch profile when click?", autoHandle, apiPrefix + ".autoHandle", "If True, then the profile will be switched to once its slot is clicked on");

			if (autoHandle)
			{
				ActionListGUI ("ActionList after selecting:", menu.title, "After_Selecting", apiPrefix, "The ActionList asset to run once a profile has been switched to");
			}
			else
			{
				ActionListGUI ("ActionList when click:", menu.title, "When_Click", apiPrefix, "The ActionList asset to run once a profile has been clicked on");
			}

			if (source != MenuSource.AdventureCreator)
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, maxSlots);
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}
			
			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".anchor");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The effect thickness");
				effectColour = CustomGUILayout.ColorField ("Effect colour:", effectColour, apiPrefix + ".effectColour", "The effect colour");
			}
		}


		private void ActionListGUI (string label, string menuTitle, string suffix, string apiPrefix, string tooltip)
		{
			actionListOnClick = ActionListAssetMenu.AssetGUI (label, actionListOnClick, menuTitle + "_" + title + "_" + suffix, apiPrefix + ".actionListOnClick", tooltip);
			
			if (actionListOnClick && actionListOnClick.NumParameters > 0)
			{
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.BeginHorizontal ();
				parameterID = Action.ChooseParameterGUI (string.Empty, actionListOnClick.DefaultParameters, parameterID, ParameterType.Integer);
				if (parameterID >= 0)
				{
					if (fixedOption)
					{
						EditorGUILayout.LabelField ("(= Profile ID #)");
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


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListOnClick == actionListAsset)
				return true;
			return false;
		}

		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			foreach (UISlot uiSlot in uiSlots)
			{
				if (uiSlot.uiButton != null && uiSlot.uiButton.gameObject == gameObject) return true;
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


		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (fixedOption) return;

			if (isVisible && numSlots >= maxSlots)
			{
				Shift (shiftType, maxSlots, KickStarter.options.GetNumProfiles (), amount);
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
			if (fixedOption)
			{
				return 0;
			}

			if (!showActive)
			{
				return Mathf.Max (0, KickStarter.options.GetNumProfiles () - 1 - maxSlots);
			}
			return Mathf.Max (0, KickStarter.options.GetNumProfiles () - maxSlots);
		}
		

		public override string GetLabel (int slot, int languageNumber)
		{
			if (Application.isPlaying)
			{
				if (fixedOption)
				{
					return KickStarter.options.GetProfileIDName (optionToShow);
				}
				else
				{
					return KickStarter.options.GetProfileName (slot + offset, showActive);
				}
			}

			if (fixedOption)
			{
				return ("Profile ID " + optionToShow.ToString ());
			}
			return ("Profile " + slot.ToString ());
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
		

		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			string fullText = GetLabel (_slot, languageNumber);

			if (!Application.isPlaying)
			{
				if (labels == null || labels.Length != numSlots)
				{
					labels = new string [numSlots];
				}
			}
			
			labels [_slot] = fullText;
			
			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);
					uiSlots[_slot].SetText (labels [_slot]);
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

			#if UNITY_EDITOR
			if (!Application.isPlaying && (labels == null || labels.Length <= _slot || string.IsNullOrEmpty (labels[_slot]))) PreDisplay (_slot, 0, isActive);
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
		

		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}
			
			if (autoHandle)
			{
				bool isSuccess = (fixedOption)
								 ? Options.SwitchProfileID (optionToShow)
								 : KickStarter.options.SwitchProfile (_slot + offset, showActive);

				if (isSuccess)
				{
					RunActionList (_slot);
				}
			}
			else
			{
				RunActionList (_slot);
			}

			return base.ProcessClick (_menu, _slot, _mouseState);
		}


		private void RunActionList (int _slot)
		{
			if (fixedOption)
			{
				AdvGame.RunActionListAsset (actionListOnClick, parameterID, optionToShow);
			}
			else
			{
				AdvGame.RunActionListAsset (actionListOnClick, parameterID, _slot + offset);
			}
		}


		public override void RecalculateSize (MenuSource source)
		{
			if (Application.isPlaying)
			{
				if (fixedOption)
				{
					numSlots = 1;
				}
				else
				{
					numSlots = KickStarter.options.GetNumProfiles ();

					if (!showActive)
					{
						numSlots --;
					}

					if (numSlots > maxSlots)
					{
						numSlots = maxSlots;
					}

					offset = Mathf.Min (offset, GetMaxOffset ());
				}
			}
			
			labels = new string [numSlots];
			
			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
			}
			
			base.RecalculateSize (source);
		}
		
		
		protected override void AutoSize ()
		{
			AutoSize (new GUIContent (GetLabel (0, 0)));
		}
		
	}
	
}