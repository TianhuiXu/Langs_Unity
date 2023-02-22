/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuInventoryBox.cs"
 * 
 *	This MenuElement lists all inventory items held by the player.
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
	 * A MenuElement that lists inventory items (see: InvItem).
	 * It can be used to display all inventory items held by the player, those that are stored within a Container, or as part of an Interaction Menu.
	 */
	public class MenuInventoryBox : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;
		/** What pointer state registers as a 'click' for Unity UI Menus (PointerClick, PointerDown, PointerEnter) */
		public UIPointerState uiPointerState = UIPointerState.PointerClick;

		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The text alignement, if displayType = ConversationDisplayType.TextOnly */
		public TextAnchor anchor = TextAnchor.MiddleCenter;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** How the items to display are chosen (Default, HotspotBased, CustomScript, DisplaySelected, DisplayLastSelected, Container, CollectedDocuments, Objectives) */
		public AC_InventoryBoxType inventoryBoxType;
		/** An ActionList to run when a slot is clicked (if inventoryBoxType = CollectedDocuments or Objectives) */
		public ActionListAsset actionListOnClick = null;
		/** The maximum number of inventory items that can be shown at once */
		public int maxSlots;
		/** If True, only inventory items (InvItem) with a specific category will be displayed */
		public bool limitToCategory;
		/** If True, then only inventory items that are listed in a Hotspot's / InvItem's interactions will be listed if inventoryBoxType = AC_InventoryBoxType.HotspotBased */
		public bool limitToDefinedInteractions = true;
		/** The category ID to limit the display of inventory items by, if limitToCategory = True (Deprecated) */
		public int categoryID;
		/** The category IDs to limit the display of inventory items by, if limitToCategory = True */
		public List<int> categoryIDs = new List<int>();
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;
		/** If True, then no interactions will work */
		public bool preventInteractions = false;
		/** If True, then items cannot be selected */
		public bool preventSelection = false;
		/** If True, and the element is scrolled by an offset larger than the number of new items to show, then the offset amount will be reduced to only show those new items. */
		public bool limitMaxScroll = true;
		/** If True, then slots with no item in them will have highlighting effects applied as well */
		public bool highlightEmptySlots = false;

		/** DisplayType != ConversationDisplayType.IconOnly, Hotspot labels will only update when hovering over items if this is True */
		public bool updateHotspotLabelWhenHover = false;
		/** If True, then the hover sound will play even when over an empty slot */
		public bool hoverSoundOverEmptySlots = true;

		private List<InvInstance> invInstances = new List<InvInstance>();
		/** If inventoryBoxType = AC_InventoryBoxType.Container, what happens to items when they are removed from the container */
		public ContainerSelectMode containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
		/** How items are displayed (IconOnly, TextOnly, IconAndText) */
		public ConversationDisplayType displayType = ConversationDisplayType.IconOnly;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** If True, and inventoryBoxType = AC_InventoryBoxType.CollectedDocuments, then clicking an element slot will open the chosen Document */
		public bool autoOpenDocument = true;
		/** The texture to display when a slot is empty */
		public Texture2D emptySlotTexture = null;
		/** How the item count is displayed */
		public InventoryItemCountDisplay inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;

		/** What Objectives to display, if inventoryBoxType = AC_InventoryBoxType.Objectives */
		public ObjectiveDisplayType objectiveDisplayType = ObjectiveDisplayType.All;

		private Container overrideContainer;
		private Container pendingCloseContainer;
		private string[] labels = null;
		private int numDocuments = 0;
		private Texture[] textures;

		#if UNITY_EDITOR
		private Texture2D testIcon;
		#endif


		public override void Declare ()
		{
			uiSlots = null;
			uiPointerState = UIPointerState.PointerClick;

			isVisible = true;
			isClickable = true;
			inventoryBoxType = AC_InventoryBoxType.Default;
			actionListOnClick = null;
			anchor = TextAnchor.MiddleCenter;
			numSlots = 0;
			SetSize (new Vector2 (6f, 10f));
			maxSlots = 10;
			limitToCategory = false;
			limitToDefinedInteractions = true;
			containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
			categoryID = -1;
			displayType = ConversationDisplayType.IconOnly;
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			uiHideStyle = UIHideStyle.DisableObject;
			emptySlotTexture = null;
			objectiveDisplayType = ObjectiveDisplayType.All;
			invInstances = new List<InvInstance>();
			categoryIDs = new List<int>();
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			autoOpenDocument = true;
			updateHotspotLabelWhenHover = false;
			hoverSoundOverEmptySlots = true;
			preventInteractions = false;
			preventSelection = false;
			limitMaxScroll = true;
			inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;
			highlightEmptySlots = false;
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuInventoryBox newElement = CreateInstance <MenuInventoryBox>();
			newElement.Declare ();
			newElement.CopyInventoryBox (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyInventoryBox (MenuInventoryBox _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlots = null;
			}
			else
			{
				uiSlots = (_element.uiSlots != null) ? new UISlot[_element.uiSlots.Length] : new UISlot[0];
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i] = new UISlot (_element.uiSlots[i]);
				}
			}

			uiPointerState = _element.uiPointerState;

			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			anchor = _element.anchor;
			inventoryBoxType = _element.inventoryBoxType;
			actionListOnClick = _element.actionListOnClick;
			numSlots = _element.numSlots;
			maxSlots = _element.maxSlots;
			limitToCategory = _element.limitToCategory;
			limitToDefinedInteractions = _element.limitToDefinedInteractions;
			categoryID = _element.categoryID;
			containerSelectMode = _element.containerSelectMode;
			displayType = _element.displayType;
			uiHideStyle = _element.uiHideStyle;
			emptySlotTexture = _element.emptySlotTexture;
			objectiveDisplayType = _element.objectiveDisplayType;

			categoryIDs = new List<int> ();
			if (_element.categoryIDs != null)
			{
				foreach (int _categoryID in _element.categoryIDs)
				{
					categoryIDs.Add (_categoryID);
				}
			}

			linkUIGraphic = _element.linkUIGraphic;
			autoOpenDocument = _element.autoOpenDocument;
			updateHotspotLabelWhenHover = _element.updateHotspotLabelWhenHover;
			hoverSoundOverEmptySlots = _element.hoverSoundOverEmptySlots;
			preventInteractions = _element.preventInteractions;
			preventSelection = _element.preventSelection;
			limitMaxScroll = _element.limitMaxScroll;
			inventoryItemCountDisplay = _element.inventoryItemCountDisplay;
			highlightEmptySlots = _element.highlightEmptySlots;

			UpdateLimitCategory ();

			base.Copy (_element);
			invInstances = GetItemList ();


			if (Application.isPlaying)
			{
				if (!(inventoryBoxType == AC_InventoryBoxType.HotspotBased && maxSlots == 1))
				{
					alternativeInputButton = string.Empty;
				}
			}

			Upgrade ();
		}


		private void Upgrade ()
		{
			if (limitToCategory && categoryID >= 0)
			{
				categoryIDs.Add (categoryID);
				categoryID = -1;

				if (Application.isPlaying)
				{
					ACDebug.Log ("The inventory box element '" + title + "' has been upgraded - please view it in the Menu Manager and Save.");
				}
			}
		}


		private void UpdateLimitCategory ()
		{
			if (Application.isPlaying && AdvGame.GetReferences ().inventoryManager && AdvGame.GetReferences ().inventoryManager.bins != null)
			{
				foreach (InvBin invBin in KickStarter.inventoryManager.bins)
				{
					if (categoryIDs.Contains (invBin.id))
					{
						// Fine!
					}
					else
					{
						categoryIDs.Remove (invBin.id);
					}
				}
			}
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			int i=0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements (canvas, linkUIGraphic, (inventoryBoxType != AC_InventoryBoxType.CollectedDocuments) ? emptySlotTexture : null);
				if (uiSlot != null && uiSlot.uiButton)
				{
					int j=i;

					if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
					{
						if (KickStarter.settingsManager && KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple)
						{}
						else
						{
							uiPointerState = UIPointerState.PointerClick;
						}
					}

					if (addEventListeners)
					{
						CreateUIEvent (uiSlot.uiButton, _menu, uiPointerState, j, false);
					}

					uiSlot.AddClickHandler (_menu, this, j);
				}
				i++;
			}
		}


		/**
		 * <summary>Gets the UI Button associated with an inventory item, provided that the Menus' Source is UnityUiPrefab or UnityUiInScene.</summary>
		 * <param name = "itemID">The ID number of the inventory item (InvItem) to search for</param>
		 * <returns>The UI Button associated with an inventory item, or null if a suitable Button cannot be found.</returns>
		 */
		public UnityEngine.UI.Button GetUIButtonWithItem (int itemID)
		{
			for (int i=0; i< invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].ItemID == itemID)
				{
					if (uiSlots != null && uiSlots.Length > i && uiSlots[i] != null)
					{
						return uiSlots[i].uiButton;
					}
					return null;
				}
			}
			return null;
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
			if (uiSlots != null && _slot >= 0 && _slot < uiSlots.Length)
			{
				return uiSlots[_slot].GetRectTransform ();
			}
			return null;
		}
		

		protected override void ProcessClickUI (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.playerInput.GetDragState () == DragState.PreInventory && uiPointerState == UIPointerState.PointerClick)
			{
				// In this case, click handling is handled by PlayerMenus, so ignore here
				return;
			}
			
			base.ProcessClickUI (_menu, _slot, _mouseState);
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuInventoryBox)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			inventoryBoxType = (AC_InventoryBoxType) CustomGUILayout.EnumPopup ("Inventory box type:", inventoryBoxType, apiPrefix + ".inventoryBoxType", "How the items to display are chosen");
			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				maxSlots = CustomGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30, apiPrefix + ".maxSlots", "The maximum number of inventory items that can be shown at once");
				isClickable = true;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.DisplaySelected)
			{
				isClickable = false;
				maxSlots = 1;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected)
			{
				isClickable = true;
				maxSlots = 1;
			}
			else if (inventoryBoxType == AC_InventoryBoxType.Container)
			{
				isClickable = true;
				maxSlots = CustomGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30, apiPrefix + ".maxSlots", "The maximum number of inventory items that can be shown at once");
				containerSelectMode = (ContainerSelectMode) CustomGUILayout.EnumPopup ("Click behaviour:", containerSelectMode, apiPrefix + ".containerSelectMode", "What happens to items when they are removed from the Container");
			}
			else
			{
				isClickable = true;
				if (source == MenuSource.AdventureCreator)
				{
					numSlots = CustomGUILayout.IntField ("Test slots:", numSlots, apiPrefix + ".numSlots");
				}
				maxSlots = CustomGUILayout.IntSlider ("Max number of slots:", maxSlots, 1, 30, apiPrefix + ".maxSlots", "The maximum number of inventory items that can be shown at once");
			}

			if (inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected &&
				inventoryBoxType != AC_InventoryBoxType.DisplaySelected)
			{
				limitMaxScroll = CustomGUILayout.Toggle ("Limit maximum scroll?", limitMaxScroll, apiPrefix + ".limitMaxScroll", "If True, and the element is scrolled by an offset larger than the number of new items to show, then the offset amount will be reduced to only show those new items.");
			}

			switch (inventoryBoxType)
			{
				case AC_InventoryBoxType.Default:
					preventInteractions = CustomGUILayout.Toggle ("Prevent interactions?", preventInteractions, apiPrefix + ".preventInteractions", "If True, inventory interactions cannot be run");
					preventSelection = CustomGUILayout.Toggle ("Prevent selection?", preventSelection, apiPrefix + ".preventSelection", "If True, then items cannot be selected");
					break;

				case AC_InventoryBoxType.Container:
					preventInteractions = CustomGUILayout.Toggle ("Prevent interactions?", preventInteractions, apiPrefix + ".preventInteractions", "If True, inventory interactions cannot be run");
					preventSelection = CustomGUILayout.Toggle ("Prevent selection?", preventSelection, apiPrefix + ".preventSelection", "If True, then items cannot be selected");
					break;

				case AC_InventoryBoxType.HotspotBased:
					if (!ForceLimitByReference ())
					{
						limitToDefinedInteractions = CustomGUILayout.ToggleLeft ("Only show items referenced in Interactions?", limitToDefinedInteractions, apiPrefix + ".limitToDefinedInteractions", "If True, then only inventory items that are listed in a Hotspot's / InvItem's interactions will be listed");
					}
					if (maxSlots == 1)
					{
						alternativeInputButton = CustomGUILayout.TextField ("Alternative input button:", alternativeInputButton, apiPrefix + ".alternativeInputButton", "The name of the input button that triggers the element when pressed");
					}
					break;

				case AC_InventoryBoxType.CollectedDocuments:
					autoOpenDocument = CustomGUILayout.ToggleLeft ("Auto-open Document when clicked?", autoOpenDocument, apiPrefix + ".autoOpenDocument", "If True, then clicking a slot will open the chosen Document");
					actionListOnClick = ActionListAssetMenu.AssetGUI ("ActionList when click:", actionListOnClick, title + "_Click", apiPrefix + ".actionListOnClick", "The ActionList asset to run whenever a slot is clicked");
					break;

				case AC_InventoryBoxType.Objectives:
					objectiveDisplayType = (ObjectiveDisplayType) CustomGUILayout.EnumPopup ("Objectives to display:", objectiveDisplayType, apiPrefix + ".objectiveDisplayType", "What Objectives to display");
					autoOpenDocument = CustomGUILayout.ToggleLeft ("Auto-select Objective when clicked?", autoOpenDocument, apiPrefix + ".autoOpenDocument", "If True, then clicking a slot will select the chosen Objective");
					actionListOnClick = ActionListAssetMenu.AssetGUI ("ActionList when click:", actionListOnClick, title + "_Click", apiPrefix + ".actionListOnClick", "The ActionList asset to run whenever a slot is clicked");
					break;
			}

			displayType = (ConversationDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How items are displayed");
			if (displayType == ConversationDisplayType.IconAndText && source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("'Icon And Text' mode is only available for Unity UI-based Menus.", MessageType.Warning);
			}

			if (inventoryBoxType != AC_InventoryBoxType.CollectedDocuments && 
				inventoryBoxType != AC_InventoryBoxType.Objectives)
			{
				inventoryItemCountDisplay = (InventoryItemCountDisplay) CustomGUILayout.EnumPopup ("Display item amounts:", inventoryItemCountDisplay, apiPrefix + ".inventoryItemCountDisplay", "How item counts are drawn");
			}

			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (displayType != ConversationDisplayType.IconOnly)
				{
					updateHotspotLabelWhenHover = CustomGUILayout.ToggleLeft ("Update Hotspot label when hovering?", updateHotspotLabelWhenHover, apiPrefix + ".updateHotspotLabelWhenHover", "If True, Hotspot labels will only update when hovering over items");
				}
			}

			if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected && source == MenuSource.AdventureCreator)
			{
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
			
			if (inventoryBoxType == AC_InventoryBoxType.CustomScript)
			{
				ShowClipHelp ();
			}

			uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When slot is empty:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");

			if (inventoryBoxType != AC_InventoryBoxType.CollectedDocuments && inventoryBoxType != AC_InventoryBoxType.Objectives && uiHideStyle == UIHideStyle.ClearContent && displayType != ConversationDisplayType.TextOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Empty slot texture:", "The texture to display when a slot is empty"), GUILayout.Width (145f));
				emptySlotTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (emptySlotTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".emptySlotTexture");
				EditorGUILayout.EndHorizontal ();

			}

			if (source == MenuSource.AdventureCreator && ((KickStarter.settingsManager && KickStarter.settingsManager.canReorderItems) || uiHideStyle == UIHideStyle.ClearContent) && highlightTexture)
			{
				highlightEmptySlots = CustomGUILayout.Toggle ("Highlight empty slots?", highlightEmptySlots, apiPrefix + ".highlightEmptySlots", "If True, then the highlight texture will display for empty slots as well as those with items.");
			}

			hoverSoundOverEmptySlots = CustomGUILayout.Toggle ("Hover sound when empty?", hoverSoundOverEmptySlots, apiPrefix + ".hoverSoundOverEmptySlots", "If True, then the hover sound will play even when over an empty slot");

			if (source != MenuSource.AdventureCreator)
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, maxSlots);
				
				for (int i=0; i<uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");

				// Don't show if Single and Default or Custom Script
				if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.CustomScript)
				{
					if (KickStarter.settingsManager != null &&
						KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple)
					{
						uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
					}
				}
				else
				{
					uiPointerState = (UIPointerState) CustomGUILayout.EnumPopup ("Responds to:", uiPointerState, apiPrefix + ".uiPointerState", "What pointer state registers as a 'click' for Unity UI Menus");
				}
			}

			ChangeCursorGUI (menu);
			CustomGUILayout.EndVertical ();

			if (CanBeLimitedByCategory ())
			{
				ShowCategoriesUI (apiPrefix);
			}

			base.ShowGUI (menu);
		}


		protected override void ShowTextureGUI (string apiPrefix)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments && displayType == ConversationDisplayType.IconOnly)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Test icon:", GUILayout.Width (145f));
				testIcon = (Texture2D) EditorGUILayout.ObjectField (testIcon, typeof (Texture2D), false, GUILayout.Width (70f), GUILayout.Height (30f));
				EditorGUILayout.EndHorizontal ();
			}
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			if (displayType == ConversationDisplayType.TextOnly)
			{
				anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			}
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The effect thickness");
				effectColour = CustomGUILayout.ColorField ("Effect colour:", effectColour, apiPrefix + ".effectColour", "The effect colour");
			}
		}


		private void ShowCategoriesUI (string apiPrefix)
		{
			CustomGUILayout.BeginVertical ();
		
			limitToCategory = CustomGUILayout.Toggle ("Limit by category?", limitToCategory, apiPrefix + ".limitToCategory", "If True, only items with a specific category will be displayed");
			if (limitToCategory)
			{
				Upgrade ();

				if (AdvGame.GetReferences ().inventoryManager)
				{
					List<InvBin> bins = AdvGame.GetReferences ().inventoryManager.bins;

					if (bins == null || bins.Count == 0)
					{
						categoryIDs.Clear ();
						EditorGUILayout.HelpBox ("No categories defined!", MessageType.Warning);
					}
					else
					{
						for (int i=0; i<bins.Count; i++)
						{
							bool include = (categoryIDs.Contains (bins[i].id)) ? true : false;
							include = EditorGUILayout.ToggleLeft (" " + i.ToString () + ": " + bins[i].label, include);

							if (include)
							{
								if (!categoryIDs.Contains (bins[i].id))
								{
									categoryIDs.Add (bins[i].id);
								}
							}
							else
							{
								if (categoryIDs.Contains (bins[i].id))
								{
									categoryIDs.Remove (bins[i].id);
								}
							}
						}

						if (categoryIDs.Count == 0)
						{
							EditorGUILayout.HelpBox ("At least one category must be checked for this to take effect.", MessageType.Info);
						}
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("No Inventory Manager defined!", MessageType.Warning);
					categoryIDs.Clear ();
				}
			}
			CustomGUILayout.EndVertical ();
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if ((inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives) && actionListOnClick == actionListAsset)
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


		public override void HideAllUISlots ()
		{
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle, emptySlotTexture);
		}


		public override void SetUIInteractableState (bool state)
		{
			SetUISlotsInteractableState (uiSlots, state);
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].CanOverrideHotspotLabel) return string.Empty;

			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (displayType == ConversationDisplayType.IconOnly || updateHotspotLabelWhenHover)
				{
					return labels [_slot];
				}
				return string.Empty;
			}

			InvInstance slotInvInstance = GetInstance (_slot);
			if (!InvInstance.IsValid (slotInvInstance))
			{
				return string.Empty;
			}

			string slotItemLabel = slotInvInstance.InvItem.GetLabel (_language);

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
			{
				if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
					KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu)
				{
					// Don't override, refer to the clicked InventoryBox
					return string.Empty;
				}

				if (KickStarter.cursorManager.addHotspotPrefix)
				{
					string prefix = slotInvInstance.GetHotspotPrefixLabel (_language, true);
					if (InvInstance.IsValid (parentMenu.TargetInvInstance))
					{
						// Combine two items, i.e. "Use worm on apple"
						string itemName = parentMenu.TargetInvInstance.InvItem.GetLabel (_language);
						if (parentMenu.TargetInvInstance.InvItem.canBeLowerCase && !string.IsNullOrEmpty (prefix))
						{
							itemName = itemName.ToLower ();
						}

						return AdvGame.CombineLanguageString (prefix, itemName, _language);
					}

					if (parentMenu.TargetHotspot)
					{
						// Use item on hotspot, i.e. "Use worm on bench"
						string hotspotName = parentMenu.TargetHotspot.GetName (_language);
						if (parentMenu.TargetHotspot.canBeLowerCase && !string.IsNullOrEmpty (prefix))
						{
							hotspotName = hotspotName.ToLower ();
						}

						return AdvGame.CombineLanguageString (prefix, hotspotName, _language);
					}
				}
				else
				{
					if (InvInstance.IsValid (parentMenu.TargetInvInstance))
					{
						// Parent menu's item label only
						return parentMenu.TargetInvInstance.InvItem.GetLabel (_language);
					}
				}

				return string.Empty;
			}

			InvInstance selectedInstance = KickStarter.runtimeInventory.SelectedInstance;

			switch (KickStarter.settingsManager.interactionMethod)
			{
				case AC_InteractionMethod.ContextSensitive:
					{
						if (InvInstance.IsValid (selectedInstance) &&
							(KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeHotspotLabel || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel))
						{
							if (selectedInstance == slotInvInstance)
							{
								return slotItemLabel;
							}

							// Combine two items, i.e. "Use worm on apple"
							string prefix = selectedInstance.GetHotspotPrefixLabel (_language);
							if (slotInvInstance.InvItem.canBeLowerCase && !string.IsNullOrEmpty (prefix))
							{
								slotItemLabel = slotItemLabel.ToLower ();
							}

							return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
						}

						if (InvInstance.IsValid (selectedInstance) && selectedInstance == slotInvInstance && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu)
						{
							// Over hidden item
							return string.Empty;
						}

						// Just the item, i.e. "Worm"
						return slotItemLabel;
					}

				case AC_InteractionMethod.ChooseHotspotThenInteraction:
				case AC_InteractionMethod.ChooseInteractionThenHotspot:
					{
						if (InvInstance.IsValid (selectedInstance))
						{
							if (KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeHotspotLabel || KickStarter.cursorManager.inventoryHandling == InventoryHandling.ChangeCursorAndHotspotLabel)
							{
								if (selectedInstance == slotInvInstance)
								{
									return slotItemLabel;
								}

								// Combine two items, i.e. "Use worm on apple"
								string prefix = selectedInstance.GetHotspotPrefixLabel (_language);
								if (slotInvInstance.InvItem.canBeLowerCase && !string.IsNullOrEmpty (prefix))
								{
									slotItemLabel = slotItemLabel.ToLower ();
								}

								return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
							}

							if (selectedInstance == slotInvInstance && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu)
							{
								// Over hidden item
								return string.Empty;
							}
						}
						else
						{
							// None selected

							if (KickStarter.cursorManager.addHotspotPrefix)
							{
								if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
									KickStarter.settingsManager.SelectInteractionMethod () != SelectInteractions.ClickingMenu &&
									KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple)
								{
									string prefix = slotInvInstance.GetLabelPrefix (_language);
									return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
								}

								if (KickStarter.playerCursor.GetSelectedCursor () >= 0)
								{
									// Use an item, i.e. "Look at worm"
									string prefix = KickStarter.cursorManager.GetLabelFromID (KickStarter.playerCursor.GetSelectedCursorID (), _language);
									if (slotInvInstance.InvItem.canBeLowerCase && !string.IsNullOrEmpty (prefix))
									{
										slotItemLabel = slotItemLabel.ToLower ();
									}

									return AdvGame.CombineLanguageString (prefix, slotItemLabel, _language);
								}
							}
						}

						// Just the item, i.e. "Worm"
						return slotItemLabel;
					}

				default:
					break;
			}

			return string.Empty;
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (Application.isPlaying)
				{
					if (uiSlots != null && uiSlots.Length > _slot)
					{
						LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

						if (displayType == ConversationDisplayType.IconOnly || displayType == ConversationDisplayType.IconAndText)
						{
							uiSlots[_slot].SetImage (textures [_slot]);
						}
						if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
						{
							uiSlots[_slot].SetText (labels [_slot]);
						}
					}
				}
				else
				{
					string fullText = string.Empty;
					if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
					{
						fullText = "Document #" + _slot.ToString ();
					}
					else
					{
						fullText = "Objective #" + _slot.ToString ();
					}

					if (labels == null || labels.Length != numSlots)
					{
						labels = new string[numSlots];
					}
					labels [_slot] = fullText;
				}
				return;
			}

			if (invInstances.Count > 0 && invInstances.Count > (_slot+offset) && InvInstance.IsValid (invInstances[_slot+offset]))
			{
				string fullText = string.Empty;

				if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
				{
					if (InvInstance.IsValid (invInstances[_slot+offset]))
					{
						fullText = (Application.isPlaying)
							? invInstances[_slot+offset].InvItem.GetLabel (languageNumber)
							: invInstances[_slot+offset].InvItem.label;
					}
					string countText = GetCount (_slot);
					if (!string.IsNullOrEmpty (countText))
					{
						fullText += " (" + countText + ")";
					}
				}
				else
				{
					string countText = GetCount (_slot);
					if (!string.IsNullOrEmpty (countText))
					{
						fullText = countText;
					}
				}

				if (labels == null || labels.Length != numSlots)
				{
					labels = new string [numSlots];
				}
				labels [_slot] = fullText;
			}

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle, emptySlotTexture);

					uiSlots[_slot].SetText (labels [_slot]);
					if (displayType == ConversationDisplayType.IconOnly || displayType == ConversationDisplayType.IconAndText)
					{
						Texture tex = null;
						if ((_slot + offset) < invInstances.Count && InvInstance.IsValid (invInstances[_slot+offset]))
						{
							if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected)
							{
								if (KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (_slot+offset))
								{
									if (!invInstances[_slot+offset].IsPartialTransfer ())
									{
										// Display as normal if we are only doing a partial transfer
										uiSlots[_slot].SetImage (null);
										labels [_slot] = string.Empty;
										uiSlots[_slot].SetText (labels [_slot]);
										return;
									}
								}
								tex = GetTexture (_slot+offset, isActive);
							}

							if (tex == null)
							{
								tex = invInstances [_slot+offset].Tex;
							}
						}

						if (IsVisible || uiHideStyle == UIHideStyle.DisableObject)
						{
							uiSlots[_slot].SetImage (tex);
						}
					}

					if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingMenuAndClickingHotspot &&
						inventoryBoxType == AC_InventoryBoxType.HotspotBased &&
						parentMenu != null &&
						((parentMenu.TargetHotspot && parentMenu.TargetHotspot.GetActiveInvButtonID () == invInstances[_slot+offset].ItemID) ||
						(KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple && InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance) && KickStarter.runtimeInventory.HoverInstance.GetActiveInvButtonID () == invInstances[_slot+offset].ItemID)))
					{
						// Select through script, not by mouse-over
						if (uiSlots[_slot].uiButton)
						{
							uiSlots[_slot].uiButton.Select ();
						}
					}
				}
			}
			return;
		}


		private bool ItemIsSelected (int index)
		{
			if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance)) return false;

			if (InvInstance.IsValid (invInstances[index]) && (!KickStarter.settingsManager.InventoryDragDrop || KickStarter.playerInput.GetDragState () == DragState.Inventory))
			{
				return (invInstances[index] == KickStarter.runtimeInventory.SelectedInstance);
			}
			return false;
		}


		/**
		 * <summary>Draws the element using OnGUI</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive If True, then the element will be drawn as though highlighted</param>
		 */
		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			_style.wordWrap = true;

			if (displayType == ConversationDisplayType.TextOnly)
			{
				_style.alignment = anchor;
			}

			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (zoom < 1f)
				{
					_style.fontSize = (int) ((float) _style.fontSize * zoom);
				}

				if (displayType == ConversationDisplayType.TextOnly)
				{
					if (textEffects != TextEffects.None)
					{
						AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels [_slot], _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
					}
					else
					{
						GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels [_slot], _style);
					}
				}
				else
				{
					if (Application.isPlaying && textures[_slot])
					{
						GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), textures[_slot], ScaleMode.StretchToFill, true, 0f);
					}
					#if UNITY_EDITOR
					else if (testIcon != null)
					{
						GUI.DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), testIcon, ScaleMode.StretchToFill, true, 0f);
					}
					#endif
					
					GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), string.Empty, _style);
				}
				return;
			}

			if (invInstances.Count > 0 && invInstances.Count > (_slot+offset) && InvInstance.IsValid (invInstances[_slot+offset]))
			{
				if (Application.isPlaying && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (_slot+offset))
				{
					if (!invInstances[_slot + offset].IsPartialTransfer ())
					{
						// Display as normal if we only have one selected from many
						return;
					}
				}

				Rect slotRect = GetSlotRectRelative (_slot);

				switch (displayType)
				{
					case ConversationDisplayType.IconOnly:
						GUI.Label (slotRect, string.Empty, _style);
						DrawTexture (ZoomRect (slotRect, zoom), _slot + offset, isActive);
						_style.normal.background = null;

						if (textEffects != TextEffects.None)
						{
							AdvGame.DrawTextEffect (ZoomRect (slotRect, zoom), GetCount (_slot), _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
						}
						else
						{
							GUI.Label (ZoomRect (slotRect, zoom), GetCount (_slot), _style);
						}
						break;

					case ConversationDisplayType.TextOnly:
						if (textEffects != TextEffects.None)
						{
							AdvGame.DrawTextEffect (ZoomRect (slotRect, zoom), labels[_slot], _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
						}
						else
						{
							GUI.Label (ZoomRect (slotRect, zoom), labels[_slot], _style);
						}
						break;

					default:
						break;
				}
				return;
			}

			if (displayType == ConversationDisplayType.IconOnly)
			{
				Rect slotRect = GetSlotRectRelative (_slot);
				Texture2D _tex = emptySlotTexture;

				if (((KickStarter.settingsManager && KickStarter.settingsManager.canReorderItems) || uiHideStyle == UIHideStyle.ClearContent) && highlightEmptySlots)
				{
					if (emptySlotTexture == null)
					{
						_tex = _style.normal.background;
					}
				}
				else
				{	
					_style.normal.background = null;
				}

				if (_tex)
				{
					GUI.Label (slotRect, string.Empty, _style);
					GUI.DrawTexture (ZoomRect (slotRect, zoom), _tex, ScaleMode.StretchToFill, true, 0f);
				}
			}
		}


		private bool AllowInteractions ()
		{
			switch (inventoryBoxType)
			{
				case AC_InventoryBoxType.Default:
				case AC_InventoryBoxType.Container:
					return !preventInteractions;

				default:
					return true;
			}
		}


		private bool AllowSelection ()
		{
			if (inventoryBoxType == AC_InventoryBoxType.Default || inventoryBoxType == AC_InventoryBoxType.Container)
			{
				return !preventSelection;
			}
			return true;
		}


		protected void ClickInvItemToInteract (InvInstance invInstance)
		{
			if (!InvInstance.IsValid (invInstance)) return;

			int invID = invInstance.GetActiveInvButtonID ();
			if (invID == -1)
			{
				invInstance.Use (KickStarter.playerInteraction.GetActiveUseButtonIconID ());
			}
			else
			{
				invInstance.Combine (new InvInstance (invID));
			}
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on, if inventoryBoxType = AC_InventoryBoxType.Default.</summary>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 * <returns>True if the click had an effect and should be consumed</returns>
		 */
		public bool HandleDefaultClick (MouseState _mouseState, int _slot)
		{
			InvInstance selectedInstance = KickStarter.runtimeInventory.SelectedInstance;

			if (KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple && KickStarter.playerMenus.IsInteractionMenuOn ())
			{
				KickStarter.playerMenus.CloseInteractionMenus ();
				ClickInvItemToInteract (GetInstance (_slot));
				return true;
			}
			else if (KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Multiple && KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot)
			{
				if (KickStarter.settingsManager.autoCycleWhenInteract && _mouseState == MouseState.SingleClick && (!InvInstance.IsValid (selectedInstance) || KickStarter.settingsManager.cycleInventoryCursors))
				{
					KickStarter.playerInteraction.PreAutoCycle ();
				}

				if (!KickStarter.settingsManager.cycleInventoryCursors && InvInstance.IsValid (selectedInstance))
				{}
				else if (_mouseState != MouseState.RightClick)
				{
					KickStarter.playerMenus.CloseInteractionMenus ();
					ClickInvItemToInteract (GetInstance (_slot));
					return true;
				}

				if (KickStarter.settingsManager.autoCycleWhenInteract && _mouseState == MouseState.SingleClick)
				{
					if (InvInstance.IsValid (KickStarter.runtimeInventory.HoverInstance))
					{
						KickStarter.runtimeInventory.HoverInstance.RestoreInteraction ();
						return true;
					}
					return false;
				}

				if (KickStarter.settingsManager.cycleInventoryCursors || _mouseState == MouseState.RightClick)
				{
					return false;
				}
			}

			AC_InteractionMethod interactionMethod = (KickStarter.settingsManager.InventoryInteractions == InventoryInteractions.Single)
													? AC_InteractionMethod.ContextSensitive
													: KickStarter.settingsManager.interactionMethod;

			if (KickStarter.stateHandler.gameState == GameState.DialogOptions && !KickStarter.settingsManager.allowInventoryInteractionsDuringConversations && !KickStarter.settingsManager.allowGameplayDuringConversations)
			{
				return false;
			}

			KickStarter.playerMenus.CloseInteractionMenus ();
			KickStarter.runtimeInventory.HighlightItemOffInstant ();
				
			int trueIndex = _slot + offset;

			bool notFromPlayerInventory = (selectedInstance != null) ? KickStarter.runtimeInventory.PlayerInvCollection != selectedInstance.GetSource () : false;

			if (inventoryBoxType == AC_InventoryBoxType.Default)
			{
				Container selectedInstanceContainer = (selectedInstance != null) ? selectedInstance.GetSourceContainer () : null;
				if (selectedInstanceContainer && !KickStarter.runtimeInventory.CanTransferContainerItemsToInventory (selectedInstanceContainer, selectedInstance))
				{
					return false;
				}

				if (trueIndex >= invInstances.Count || !InvInstance.IsValid (invInstances[trueIndex]) || notFromPlayerInventory)
				{
					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && (KickStarter.settingsManager.canReorderItems || notFromPlayerInventory))
					{
						if (limitToCategory && categoryIDs != null && categoryIDs.Count > 0)
						{
							// Need to change index because we want to affect the actual inventory, not the percieved one shown in the restricted menu
							List<InvInstance> trueItemList = GetItemList (false);
							LimitedItemList limitedItemList = LimitByCategory (trueItemList, trueIndex);
							trueIndex += limitedItemList.Offset;
						}

						KickStarter.runtimeInventory.PlayerInvCollection.Insert (KickStarter.runtimeInventory.SelectedInstance, trueIndex);
						KickStarter.runtimeInventory.SetNull ();
						return true;
					}
					else if (InvInstance.IsValid (selectedInstance) && notFromPlayerInventory)
					{
						trueIndex = KickStarter.runtimeInventory.localItems.Count;
						KickStarter.runtimeInventory.PlayerInvCollection.Insert (KickStarter.runtimeInventory.SelectedInstance, trueIndex);
						KickStarter.runtimeInventory.SetNull ();
						return true;
					}

					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
					{
						KickStarter.runtimeInventory.SetNull ();
						return true;
					}
					return false;
				}
			}

			if (InvInstance.IsValid (selectedInstance))
			{
				if (inventoryBoxType == AC_InventoryBoxType.Container)
				{
					if (selectedInstance.GetSourceContainer () && (selectedInstance.GetSourceContainer () == OverrideContainer) || (OverrideContainer == null && selectedInstance.GetSourceContainer () == KickStarter.playerInput.activeContainer))
					{
						// Combines OK
					}
					else
					{
						return false;
					}
				}
				else
				{
					if (notFromPlayerInventory)
					{
						return false;
					}
				}
			}
				
			switch (interactionMethod)
			{
				case AC_InteractionMethod.ContextSensitive:
					if (_mouseState == MouseState.SingleClick)
					{
						if (invInstances.Count <= trueIndex)
						{
							return false;
						}

						if (InvInstance.IsValid (selectedInstance))
						{
							if (AllowInteractions ())
							{
								selectedInstance.Combine (invInstances[trueIndex]);
								return true;
							}
							else if (AllowSelection ())
							{ 
								if (invInstances[trueIndex] == selectedInstance)
								{
									KickStarter.runtimeInventory.SelectItem (selectedInstance);
									return true;
								}
								else if (KickStarter.settingsManager.canReorderItems && InvInstance.IsValid (invInstances[trueIndex]) && invInstances[trueIndex].InvItem == selectedInstance.InvItem)
								{ 
									KickStarter.runtimeInventory.PlayerInvCollection.Insert (selectedInstance, trueIndex, OccupiedSlotBehaviour.FailTransfer);
									KickStarter.runtimeInventory.SetNull ();
									return true;
								}
							}
						}
						else
						{
							if (AllowInteractions ())
							{
								if (KickStarter.cursorManager.lookUseCursorAction == LookUseCursorAction.RightClickCyclesModes && KickStarter.playerCursor.ContextCycleExamine)
								{
									invInstances[trueIndex].Examine ();
								}
								else
								{
									invInstances[trueIndex].Use ();
								}
								return true;
							}
							else if (AllowSelection ())
							{
								KickStarter.runtimeInventory.SelectItem (invInstances[trueIndex], SelectItemMode.Use);
								return true;
							}
						}
					}
					else if (_mouseState == MouseState.RightClick)
					{
						if (!InvInstance.IsValid (selectedInstance))
						{
							if (trueIndex < invInstances.Count && KickStarter.cursorManager.lookUseCursorAction != LookUseCursorAction.RightClickCyclesModes && AllowInteractions ())
							{
								invInstances[trueIndex].Examine ();
								return true;
							}
						}
						else if (AllowSelection ())
						{
							if (selectedInstance == invInstances[trueIndex] && selectedInstance.ItemStackingMode == ItemStackingMode.Stack)
							{
								selectedInstance.RemoveStack ();
							}
							else
							{
								KickStarter.runtimeInventory.SetNull ();
							}
							return true;
						}
					}
					return false;

				case AC_InteractionMethod.ChooseInteractionThenHotspot:
					if (trueIndex >= invInstances.Count) return false;

					if (_mouseState == MouseState.SingleClick)
					{
						int cursorID = KickStarter.playerCursor.GetSelectedCursorID ();
						int cursor = KickStarter.playerCursor.GetSelectedCursor ();

						if (cursor == -2 && InvInstance.IsValid (selectedInstance))
						{
							if (invInstances[trueIndex] == selectedInstance)
							{
								if (AllowSelection ())
								{
									KickStarter.runtimeInventory.SelectItem (invInstances[trueIndex], SelectItemMode.Use);
									return true;
								}
							}
							else if (AllowInteractions ())
							{
								if (InvInstance.IsValid (selectedInstance))
								{
									selectedInstance.Combine (invInstances[trueIndex]);
									return true;
								}
							}
						}
						else if ((cursor == -1 && !KickStarter.settingsManager.selectInvWithUnhandled) || !AllowInteractions ())
						{
							if (AllowSelection ())
							{
								KickStarter.runtimeInventory.SelectItem (invInstances[trueIndex], SelectItemMode.Use);
								return true;
							}
						}
						else if (cursorID > -1)
						{
							invInstances[trueIndex].Use (cursorID);
							return true;
						}
					}
					return false;

				case AC_InteractionMethod.ChooseHotspotThenInteraction:
					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
					{
						if (_mouseState == MouseState.SingleClick)
						{
							if (trueIndex >= invInstances.Count)
							{
								return false;
							}
							if (AllowInteractions ())
							{
								if (InvInstance.IsValid (selectedInstance))
								{
									selectedInstance.Combine (invInstances[trueIndex]);
									return true;
								}
							}
							else
							{
								if (invInstances[trueIndex] == selectedInstance && AllowSelection ())
								{
									KickStarter.runtimeInventory.SetNull ();
									return true;
								}
							}
						}
						else if (_mouseState == MouseState.RightClick)
						{
							if (AllowSelection ())
							{
								KickStarter.runtimeInventory.SetNull ();
								return true;
							}
						}
					}
					else
					{
						if (trueIndex >= invInstances.Count)
						{
							return false;
						}

						if (!AllowInteractions ())
						{
							if (AllowSelection ())
							{
								KickStarter.runtimeInventory.SelectItem (invInstances[trueIndex], SelectItemMode.Use);
								return true;
							}
						}
						else
						{
							if (KickStarter.settingsManager.dragThreshold > 0f && KickStarter.settingsManager.InventoryDragDrop && KickStarter.settingsManager.inventoryDropLookNoDrag && AllowSelection ())
							{
								KickStarter.runtimeInventory.SelectItem (invInstances[trueIndex], SelectItemMode.Use);
								return true;
							}

							KickStarter.runtimeInventory.ShowInteractions (invInstances[trueIndex]);
							return true;
						}
					}
					return false;

				default:
					break;
			}

			return true;
		}


		protected override int MaxSlotsForOffset
		{
			get
			{
				switch (inventoryBoxType)
				{
					case AC_InventoryBoxType.CollectedDocuments:
						int[] documentIDs = KickStarter.runtimeDocuments.GetCollectedDocumentIDs ((limitToCategory) ? categoryIDs.ToArray () : null);
						return documentIDs.Length;

					case AC_InventoryBoxType.Objectives:
						ObjectiveInstance[] objectiveInstances = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType);
						return objectiveInstances.Length;

					default:
						invInstances = GetItemList ();
						return invInstances.Count;
				}
			}
		}


		public override void RecalculateSize (MenuSource source)
		{
			switch (inventoryBoxType)
			{
				case AC_InventoryBoxType.CollectedDocuments:
				{
					if (Application.isPlaying)
					{
						int[] documentIDs = KickStarter.runtimeDocuments.GetCollectedDocumentIDs ((limitToCategory) ? categoryIDs.ToArray () : null);

						numDocuments = documentIDs.Length;
						numSlots = numDocuments;
						if (numSlots > maxSlots)
						{
							numSlots = maxSlots;
						}

						LimitOffset ();

						labels = new string[numSlots];
						textures = new Texture[numSlots];

						int languageNumber = Options.GetLanguage ();
						for (int i=0; i<numSlots; i++)
						{
							int documentID = documentIDs [i + offset];
							Document document = KickStarter.inventoryManager.GetDocument (documentID);

							labels[i] = KickStarter.runtimeLanguages.GetTranslation (document.title,
																						document.titleLineID,
																						languageNumber,
																						document.GetTranslationType (0));

							textures[i] = document.texture;
						}

						if (uiHideStyle == UIHideStyle.DisableObject)
						{
							if (numSlots > numDocuments)
							{
								offset = 0;
								numSlots = numDocuments;
							}
						}
					}
					break;
				}

				case AC_InventoryBoxType.Objectives:
				{
					if (Application.isPlaying)
					{
						ObjectiveInstance[] objectiveInstances = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType);

						numDocuments = objectiveInstances.Length;
						numSlots = numDocuments;
						if (numSlots > maxSlots)
						{
							numSlots = maxSlots;
						}

						LimitOffset();

						labels = new string[numSlots];
						textures = new Texture[numSlots];

						int languageNumber = Options.GetLanguage ();
						for (int i = 0; i < numSlots; i++)
						{
							labels[i] = objectiveInstances[i + offset].Objective.GetTitle(languageNumber);
							textures[i] = objectiveInstances[i + offset].Objective.texture;
						}

						if (uiHideStyle == UIHideStyle.DisableObject)
						{
							if (numSlots > numDocuments)
							{
								offset = 0;
								numSlots = numDocuments;
							}
						}
					}
					break;
				}

				default:
				{
					invInstances = GetItemList ();

					if (inventoryBoxType == AC_InventoryBoxType.HotspotBased)
					{
						if (Application.isPlaying)
						{
							numSlots = Mathf.Clamp (invInstances.Count, 0, maxSlots);
						}
						else
						{
							numSlots = Mathf.Clamp (numSlots, 0, maxSlots);
						}
					}
					else
					{
						numSlots = maxSlots;
					}

					if (uiHideStyle == UIHideStyle.DisableObject)
					{
						if (numSlots > invInstances.Count)
						{
							offset = 0;
							numSlots = invInstances.Count;
						}
					}

					LimitOffset();

					if (Application.isPlaying || labels == null || labels.Length != numSlots)
					{
						labels = new string[numSlots];
					}

					if (Application.isPlaying && uiSlots != null)
					{
						ClearSpriteCache(uiSlots);
					}
					break;
				}
			}

			if (!isVisible)
			{
				LimitUISlotVisibility (uiSlots, 0, uiHideStyle, emptySlotTexture);
			}
			base.RecalculateSize (source);
		}
		
		
		private List<InvInstance> GetItemList (bool doLimit = true)
		{
			List<InvInstance> newItemList = new List<InvInstance>();

			if (Application.isPlaying)
			{
				switch (inventoryBoxType)
				{
					case AC_InventoryBoxType.HotspotBased:
						if (limitToDefinedInteractions || ForceLimitByReference ())
						{
							if (parentMenu)
							{
								if (InvInstance.IsValid (parentMenu.TargetInvInstance) && KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple)
								{
									newItemList = parentMenu.TargetInvInstance.GetMatchingInvInteractionData (true).InvInstances;
								}
								else if (parentMenu.TargetHotspot)
								{
									newItemList = parentMenu.TargetHotspot.GetMatchingInvInteractionData (true).InvInstances;
								}
							}
						}
						else
						{
							foreach (InvInstance invInstance in KickStarter.runtimeInventory.PlayerInvCollection.InvInstances)
							{
								if (InvInstance.IsValid (invInstance))
								{
									if (parentMenu &&
										InvInstance.IsValid (parentMenu.TargetInvInstance) &&
										KickStarter.settingsManager.inventoryInteractions == InventoryInteractions.Multiple &&
										parentMenu.TargetInvInstance == invInstance &&
										!invInstance.InvItem.DoesHaveInventoryInteraction (invInstance.InvItem))
									{
										continue;
									}
									newItemList.Add (invInstance);
								}
							}
						}
						break;

					case AC_InventoryBoxType.DisplaySelected:
						if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
						{
							newItemList.Add (KickStarter.runtimeInventory.SelectedInstance);
						}
						break;

					case AC_InventoryBoxType.DisplayLastSelected:
						if (InvInstance.IsValid (KickStarter.runtimeInventory.LastSelectedInstance) && KickStarter.runtimeInventory.PlayerInvCollection.Contains (KickStarter.runtimeInventory.LastSelectedInstance))
						{
							newItemList.Add (KickStarter.runtimeInventory.LastSelectedInstance);
						}
						break;

					case AC_InventoryBoxType.Container:
						if (overrideContainer)
						{
							newItemList = overrideContainer.InvCollection.InvInstances;
						}
						else if (KickStarter.playerInput.activeContainer)
						{
							newItemList = KickStarter.playerInput.activeContainer.InvCollection.InvInstances;
						}
						else if (pendingCloseContainer)
						{
							newItemList = pendingCloseContainer.InvCollection.InvInstances;
						}
						break;

					case AC_InventoryBoxType.CustomScript:
						if (overrideContainer)
						{
							newItemList = overrideContainer.InvCollection.InvInstances;
						}
						else
						{
							newItemList = KickStarter.runtimeInventory.PlayerInvCollection.InvInstances;
						}
						break;

					default:
						newItemList = KickStarter.runtimeInventory.PlayerInvCollection.InvInstances;
						break;
				}

				newItemList = AddExtraNulls (newItemList);
			}
			else
			{
				newItemList = new List<InvInstance>();
				if (AdvGame.GetReferences ().inventoryManager)
				{
					foreach (InvItem _item in AdvGame.GetReferences ().inventoryManager.items)
					{
						newItemList.Add (new InvInstance (_item));

						if (newItemList.Count >= maxSlots)
						{
							break;
						}
					}
				}

				if (newItemList.Count > 0 && newItemList.Count < maxSlots)
				{
					InvItem lastItem = newItemList[newItemList.Count-1].InvItem;
					while (newItemList.Count < maxSlots)
					{
						newItemList.Add (new InvInstance (lastItem));
					}
				}
			}

			if (doLimit && CanBeLimitedByCategory ())
			{
				newItemList = LimitByCategory (newItemList, 0).LimitedInvInstances;
			}

			return newItemList;
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			if (inventoryBoxType == AC_InventoryBoxType.Container &&
				menu.appearType == AppearType.OnContainer)
			{
				ClearEvents ();

				EventManager.OnMenuTurnOff += OnMenuTurnOff;
				EventManager.OnAfterChangeScene += OnAfterChangeScene;
				EventManager.OnFinishLoading += OnFinishLoading;
			}
		}


		private void OnAfterChangeScene (LoadingGame loadingGame)
		{
			pendingCloseContainer = null;
		}


		private void OnFinishLoading ()
		{
			pendingCloseContainer = null;
		}

		
		private void OnMenuTurnOff (Menu menu, bool isInstant)
		{
			if (menu.elements.Contains (this))
			{
				pendingCloseContainer = KickStarter.playerInput.activeContainer;
				ClearEvents ();
			}
		}


		private void ClearEvents ()
		{
			EventManager.OnMenuTurnOff -= OnMenuTurnOff;
			EventManager.OnAfterChangeScene -= OnAfterChangeScene;
			EventManager.OnFinishLoading -= OnFinishLoading;
		}


		private List<InvInstance> AddExtraNulls (List<InvInstance> _invInstances)
		{
			if (inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected &&
				inventoryBoxType != AC_InventoryBoxType.DisplaySelected &&
				!limitMaxScroll &&
				_invInstances.Count > 0 &&
				_invInstances.Count % maxSlots != 0)
			{
				while (_invInstances.Count % maxSlots != 0)
				{
					_invInstances.Add (null);
				}
			}
			return _invInstances;
		}


		private bool CanBeLimitedByCategory ()
		{
			if (inventoryBoxType == AC_InventoryBoxType.Default ||
				inventoryBoxType == AC_InventoryBoxType.CustomScript ||
				inventoryBoxType == AC_InventoryBoxType.DisplaySelected ||
				inventoryBoxType == AC_InventoryBoxType.DisplayLastSelected ||
				inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
			{
				return true;
			}

			if (inventoryBoxType == AC_InventoryBoxType.HotspotBased && !ForceLimitByReference ())
			{
				return true;
			}

			return false;
		}


		public override bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (numSlots == 0)
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
					if ((maxSlots + offset) >= numDocuments)
					{
						return false;
					}
				}
				return true;
			}

			if (invInstances.Count == 0)
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
				if ((maxSlots + offset) >= invInstances.Count)
				{
					return false;
				}
			}
			return true;
		}


		private LimitedItemList LimitByCategory (List<InvInstance> invInstancesToLimit, int reverseItemIndex)
		{
			int offset = 0;

			List<InvInstance> nonLinkedInvInstancesToLimit = new List<InvInstance> ();
			foreach (InvInstance invInstanceToLimit in invInstancesToLimit)
			{
				nonLinkedInvInstancesToLimit.Add (invInstanceToLimit);
			}

			if (limitToCategory && categoryIDs.Count > 0)
			{
				for (int i=0; i<nonLinkedInvInstancesToLimit.Count; i++)
				{
					if (InvInstance.IsValid (nonLinkedInvInstancesToLimit[i]) && !categoryIDs.Contains (nonLinkedInvInstancesToLimit[i].InvItem.binID))
					{
						if (i <= reverseItemIndex)
						{
							offset ++;
						}

						nonLinkedInvInstancesToLimit.RemoveAt (i);
						i = -1;
					}
				}

				// Bugfix: Remove extra nulls at end in case some where added as a result of re-ordering another menu
				if (nonLinkedInvInstancesToLimit != null && Application.isPlaying)
				{
					nonLinkedInvInstancesToLimit = KickStarter.runtimeInventory.RemoveEmptySlots (nonLinkedInvInstancesToLimit);
				}

				nonLinkedInvInstancesToLimit = AddExtraNulls (nonLinkedInvInstancesToLimit);
			}

			return new LimitedItemList (nonLinkedInvInstancesToLimit, offset);
		}
		

		public override void Shift (AC_ShiftInventory shiftType, int amount)
		{
			if (numSlots >= maxSlots)
			{
				switch (inventoryBoxType)
				{
					case AC_InventoryBoxType.CollectedDocuments:
					case AC_InventoryBoxType.Objectives:
						Shift (shiftType, maxSlots, numDocuments, amount);
						break;

					default:
						Shift (shiftType, maxSlots, invInstances.Count, amount);
						break;
				}
			}
		}


		private Texture GetTexture (int itemIndex, bool isActive)
		{
			if (ItemIsSelected (itemIndex))
			{
				switch (KickStarter.settingsManager.selectInventoryDisplay)
				{
					case SelectInventoryDisplay.ShowSelectedGraphic:
						return invInstances[itemIndex].SelectedTex;

					case SelectInventoryDisplay.ShowHoverGraphic:
						return invInstances[itemIndex].ActiveTex;

					default:
						break;
				}
			}
			else if (isActive && KickStarter.settingsManager.activeWhenHover)
			{
				return invInstances[itemIndex].ActiveTex;
			}
			return invInstances[itemIndex].Tex;
		}
		
		
		private void DrawTexture (Rect rect, int itemIndex, bool isActive)
		{
			InvInstance invInstance = invInstances[itemIndex];
			if (!InvInstance.IsValid (invInstance)) return;

			Texture tex = null;
			if (Application.isPlaying && KickStarter.runtimeInventory && inventoryBoxType != AC_InventoryBoxType.DisplaySelected)
			{
				if (invInstance == KickStarter.runtimeInventory.HighlightInstance && invInstance.ActiveTex)
				{
					KickStarter.runtimeInventory.DrawHighlighted (rect);
					return;
				}

				if (inventoryBoxType != AC_InventoryBoxType.DisplaySelected && inventoryBoxType != AC_InventoryBoxType.DisplayLastSelected)
				{
					tex = GetTexture (itemIndex, isActive);
				}

				if (tex == null)
				{
					tex = invInstance.Tex;
				}
			}
			else
			{
				tex = invInstance.Tex;
			}

			if (tex)
			{
				GUI.DrawTexture (rect, tex, ScaleMode.StretchToFill, true, 0f);
			}
		}


		public override string GetLabel (int i, int languageNumber)
		{
			switch (inventoryBoxType)
			{
				case AC_InventoryBoxType.CollectedDocuments:
				case AC_InventoryBoxType.Objectives:
					if (labels.Length > i)
					{
						return labels[i];
					}
					return string.Empty;

				default:
					if ((i + offset) >= invInstances.Count || !InvInstance.IsValid (invInstances[i+offset]))
					{
						return string.Empty;
					}
					return invInstances[i+offset].InvItem.GetLabel (languageNumber);
			}
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


		public override AudioClip GetHoverSound (int slot)
		{
			if (!hoverSoundOverEmptySlots && !InvInstance.IsValid (GetInstance (slot))) return null;

			return base.GetHoverSound (slot);
		}


		/**
		 * <summary>Gets the inventory item shown in a specific slot</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The inventory item shown in the slot</returns>
		 */
		public InvItem GetItem (int i)
		{
			if ((i + offset) >= invInstances.Count || !InvInstance.IsValid (invInstances[i+offset]))
			{
				return null;
			}

			return invInstances[i+offset].InvItem;
		}


		/**
		 * <summary>Gets the inventory item instance shown in a specific slot</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The inventory item instance shown in the slot</returns>
		 */
		public InvInstance GetInstance (int i)
		{
			if ((i + offset) >= invInstances.Count || !InvInstance.IsValid (invInstances[i + offset]))
			{
				return null;
			}

			return invInstances[i + offset];
		}


		private string GetCount (int i)
		{
			if (inventoryItemCountDisplay == InventoryItemCountDisplay.Never) return string.Empty;

			if (Application.isPlaying)
			{
				if ((i + offset) >= invInstances.Count || !InvInstance.IsValid (invInstances[i+offset]))
				{
					return string.Empty;
				}

				if (inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfMultiple && invInstances[i + offset].Count < 2)
				{
					return string.Empty;
				}

				if (inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfStackable && (!invInstances[i + offset].InvItem.canCarryMultiple || invInstances[i + offset].InvItem.maxCount <= 1))
				{
					return string.Empty;
				}

				string customText = KickStarter.eventManager.Call_OnRequestInventoryCountText (invInstances[i + offset], false);
				if (!string.IsNullOrEmpty (customText))
				{
					return customText;
				}

				if (ItemIsSelected (i+offset))
				{
					int displayCount = invInstances[i + offset].GetInventoryDisplayCount ();
					return (displayCount > 0) ? displayCount.ToString () : string.Empty;
				}
				
				int count = invInstances[i + offset].Count;
				return (count > 0) ? count.ToString () : string.Empty;
			}

			if (invInstances[i+offset].InvItem.canCarryMultiple && invInstances[i+offset].InvItem.maxCount > 1)
			{
				if (invInstances[i+offset].Count > 1 || inventoryItemCountDisplay == InventoryItemCountDisplay.Always || inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfStackable)
				{
					return invInstances[i+offset].Count.ToString ();
				}
				if (invInstances[i + offset].InvItem.count > 1)
				{
					return invInstances[i + offset].InvItem.count.ToString ();
				}
			}
			return string.Empty;
		}


		/** Re-sets the "shift" offset, so that the first InvItem shown is the first InvItem in items. */
		public void ResetOffset ()
		{
			offset = 0;
		}
		
		
		protected override void AutoSize ()
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments || inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				if (!Application.isPlaying)
				{
					#if UNITY_EDITOR
					if (displayType == ConversationDisplayType.IconOnly)
					{
						AutoSize (new GUIContent (testIcon));
					}
					else
					{
						if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
						{
							AutoSize (new GUIContent ("Document 0"));
						}
						else
						{
							AutoSize (new GUIContent ("Objective 0"));
						}
					}
					return;
					#endif
				}

				if (numDocuments > 0)
				{
					if (displayType == ConversationDisplayType.IconOnly)
					{
						AutoSize (new GUIContent (textures[0]));
					}
					else
					{
						AutoSize (new GUIContent (labels[0]));
					}
					return;
				}
			}
			else if (invInstances.Count > 0)
			{
				foreach (InvInstance invInstance in invInstances)
				{
					if (InvInstance.IsValid (invInstance))
					{
						if (displayType == ConversationDisplayType.IconOnly)
						{
							AutoSize (new GUIContent (invInstance.Tex));
						}
						else if (displayType == ConversationDisplayType.TextOnly)
						{
							AutoSize (new GUIContent (invInstance.InvItem.label));
						}
						return;
					}
				}
			}
			else if (emptySlotTexture)
			{
				AutoSize (new GUIContent (emptySlotTexture));
			}
			else
			{
				AutoSize (GUIContent.none);
			}
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on, if inventoryBoxType = AC_InventoryBoxType.Container.</summary>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 * <returns>True if the click had an effect and should be consumed</returns>
		 */
		public bool ClickContainer (MouseState _mouseState, int _slot)
		{
			Container container = (overrideContainer != null) ? overrideContainer : KickStarter.playerInput.activeContainer;

			if (container == null || KickStarter.runtimeInventory == null) return false;

			InvInstance selectedInstance = KickStarter.runtimeInventory.SelectedInstance;
			InvInstance containerInstance = container.InvCollection.GetInstanceAtIndex (_slot+offset);
			
			if (_mouseState == MouseState.SingleClick)
			{
				if (!InvInstance.IsValid (selectedInstance))
				{
					// No item selected, so take an item from the Container
					if (InvInstance.IsValid (containerInstance))
					{
						switch (containerSelectMode)
						{
							case ContainerSelectMode.MoveToInventory:
							case ContainerSelectMode.MoveToInventoryAndSelect:
								bool selectItem = (containerSelectMode == ContainerSelectMode.MoveToInventoryAndSelect);

								ItemStackingMode itemStackingMode = containerInstance.ItemStackingMode;
								if (itemStackingMode != ItemStackingMode.All)
								{
									containerInstance.TransferCount = 1;
								}

								while (InvInstance.IsValid (containerInstance))
								{
									InvInstance newInstance = KickStarter.runtimeInventory.PlayerInvCollection.Add (containerInstance);
								
									if (selectItem && InvInstance.IsValid (newInstance))
									{
										KickStarter.runtimeInventory.SelectItem (newInstance);
									}
									
									if (InvInstance.IsValid (containerInstance) && !containerInstance.InvItem.canCarryMultiple && KickStarter.runtimeInventory.PlayerInvCollection.Contains (containerInstance.ItemID))
									{
										break;
									}
								}
								break;

							case ContainerSelectMode.SelectItemOnly:
								if (AllowInteractions ())
								{
									return HandleDefaultClick (_mouseState, _slot);
								}
								else
								{
									KickStarter.runtimeInventory.SelectItem (containerInstance);
								}
								break;

							default:
								break;
						}

						return true;
					}
					return false;
				}
				else
				{
					// Placing an item inside the container

					if (selectedInstance == containerInstance && selectedInstance.CanStack ())
					{
						selectedInstance.AddStack ();
						return true;
					}

					if (AllowInteractions () && InvInstance.IsValid (containerInstance))
					{
						bool clickConsumed = HandleDefaultClick (_mouseState, _slot);
						if (clickConsumed) return true;
					}

					KickStarter.runtimeInventory.SetNull ();

					int index = _slot + offset;
					if (container.maxSlots > 0 && index >= container.maxSlots)
					{
						index = -1;
					}

					bool doSwap = (container.maxSlots > 0 && container.swapIfFull);

					if (doSwap)
					{
						container.InvCollection.Insert (selectedInstance, index, OccupiedSlotBehaviour.SwapItems);
					}
					else
					{
						container.InvCollection.Insert (selectedInstance, index);
					}

					return true;
				}
			}

			else if (_mouseState == MouseState.RightClick)
			{
				if (AllowInteractions () && InvInstance.IsValid (containerInstance))
				{
					return HandleDefaultClick (_mouseState, _slot);
				}

				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (selectedInstance == containerInstance && selectedInstance.ItemStackingMode == ItemStackingMode.Stack)
					{
						selectedInstance.RemoveStack ();
					}
					else
					{
						KickStarter.runtimeInventory.SetNull ();
					}

					return true;
				}
			}

			return false;
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}

			bool clickConsumed = true;

			if (_mouseState == MouseState.SingleClick)
			{
				KickStarter.runtimeInventory.LastClickedInstance = GetInstance (_slot);
			}

			switch (inventoryBoxType)
			{
				case AC_InventoryBoxType.CollectedDocuments:
					if (autoOpenDocument)
					{
						Document document = GetDocument (_slot);
						KickStarter.runtimeDocuments.OpenDocument (document);
					}
					if (actionListOnClick)
					{
						actionListOnClick.Interact ();
					}
					break;

				case AC_InventoryBoxType.CustomScript:
					MenuSystem.OnElementClick (_menu, this, _slot, (int)_mouseState);
					break;

				case AC_InventoryBoxType.Objectives:
					if (autoOpenDocument)
					{
						ObjectiveInstance objectiveInstance = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType)[_slot + offset];
						KickStarter.runtimeObjectives.SelectedObjective = objectiveInstance;
					}
					if (actionListOnClick)
					{
						actionListOnClick.Interact ();
					}
					break;

				case AC_InventoryBoxType.HotspotBased:
					clickConsumed = KickStarter.runtimeInventory.ProcessInventoryBoxClick (_menu, this, _slot, _mouseState);
					if (clickConsumed && KickStarter.stateHandler.IsInGameplay () && KickStarter.settingsManager.alwaysCloseInteractionMenus)
					{
						KickStarter.playerMenus.CloseInteractionMenus ();
					}
					break;

				default:
					if (KickStarter.settingsManager.InventoryDragDrop && uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].uiButton.IsInteractable ())
					{
						return false;
					}
					clickConsumed = KickStarter.runtimeInventory.ProcessInventoryBoxClick (_menu, this, _slot, _mouseState);
					break;
			}

			if (clickConsumed && clickSound)
			{
				if (KickStarter.playerInput.GetDragState () == DragState.PreInventory && InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					// Bypass click sound in this case
				}
				else
				{
					KickStarter.sceneSettings.PlayDefaultSound (clickSound, false);
				}
			}

			KickStarter.eventManager.Call_OnMenuElementClick (_menu, this, _slot, (int)_mouseState);
			return clickConsumed;
		}


		/**
		 * <summary>Gets the Document associated with a given slot</summary>
		 * <param name = "slotIndex">The element's slot index number</param>
		 * <returns>The Document assoicated with the slot</returns>
		 */
		public Document GetDocument (int slotIndex)
		{
			if (inventoryBoxType == AC_InventoryBoxType.CollectedDocuments)
			{
				int documentID = KickStarter.runtimeDocuments.GetCollectedDocumentIDs ((limitToCategory) ? categoryIDs.ToArray () : null) [slotIndex + offset];
				return KickStarter.inventoryManager.GetDocument (documentID);
			}
			return null;
		}


		/**
		 * <summary>Gets the Document associated with a given slot</summary>
		 * <param name = "slotIndex">The element's slot index number</param>
		 * <returns>The Document assoicated with the slot</returns>
		 */
		public ObjectiveInstance GetObjective (int slotIndex)
		{
			if (inventoryBoxType == AC_InventoryBoxType.Objectives)
			{
				ObjectiveInstance[] allObjectives = KickStarter.runtimeObjectives.GetObjectives (objectiveDisplayType);
				return allObjectives [slotIndex+offset];
			}
			return null;
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "itemID">The ID number of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (int itemID)
		{
			for (int i=0; i< invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].ItemID == itemID)
				{
					return i - offset;
				}
			}
			return 0;
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "invInstance">The instance of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (InvInstance invInstance)
		{
			for (int i = 0; i < invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i] == invInstance)
				{
					return i - offset;
				}
			}
			return 0;
		}


		private bool ForceLimitByReference ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
				KickStarter.settingsManager.cycleInventoryCursors &&
				(KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingCursorAndClickingHotspot || KickStarter.settingsManager.selectInteractions == SelectInteractions.CyclingMenuAndClickingHotspot))
			{
				return true;
			}
			return false;
		}


		/** If set, and inventoryBoxType = AC_InventoryBoxType.Container, then this Container will be used instead of the global 'active' one.  Note that its Menu's 'Appear type' should not be set to 'On Container'. */
		public Container OverrideContainer
		{
			get
			{
				return overrideContainer;
			}
			set
			{
				if (overrideContainer != value)
				{
					overrideContainer = value;
					PlayerMenus.ResetInventoryBoxes ();
				}
			}
		}


		/** The items listed in the element */
		public List<InvInstance> InvInstances
		{
			get
			{
				return invInstances;
			}
		}


		private struct LimitedItemList
		{

			private List<InvInstance> limitedInvInstances;
			private int offset;

	
			public LimitedItemList (List<InvInstance> _limitedInvInstances, int _offset)
			{
				limitedInvInstances = _limitedInvInstances;
				offset = _offset;
			}


			public List<InvInstance> LimitedInvInstances
			{
				get
				{
					return limitedInvInstances;
				}
			}


			public int Offset
			{
				get
				{
					return offset;
				}
			}

		}

	}

}