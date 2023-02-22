/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuCrafting.cs"
 * 
 *	This MenuElement stores multiple Inventory Items to be combined.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A MenuElement that stores multiple inventory items to be combined to create new ones. */
	public class MenuCrafting : MenuElement
	{

		/** A List of UISlot classes that reference the linked Unity UI GameObjects (Unity UI Menus only) */
		public UISlot[] uiSlots;

		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The outline thickness, if textEffects != TextEffects.None */
		public float outlineSize = 2f;
		/** The outline colour */
		public Color effectColour = Color.black;
		/** What part of the crafting process this element is used for (Ingredients, Output) */
		public CraftingElementType craftingType = CraftingElementType.Ingredients;
		/** The List of InvItem instances that are currently on display */
		private List<InvInstance> invInstances = new List<InvInstance> ();
		/** How items are displayed (IconOnly, TextOnly, IconAndText) */
		public ConversationDisplayType displayType = ConversationDisplayType.IconOnly;
		/** The method by which this element (or slots within it) are hidden from view when made invisible (DisableObject, ClearContent) */
		public UIHideStyle uiHideStyle = UIHideStyle.DisableObject;
		/** If craftingType = CraftingElementType.Output, the ActionList to run if a crafting attempt is made but no succesful recipe is possible. This only works if crafting is performed manually via the Inventory: Crafting Action. */
		public ActionListAsset actionListOnWrongIngredients;
		/** What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic) */
		public LinkUIGraphic linkUIGraphic = LinkUIGraphic.ImageComponent;
		/** If True, and craftingType = CraftingElementYpe.Output, then outputs will appear automatically when the correct ingredients are used. If False, then the player will have to run the "Inventory: Crafting" Action as an additional step. */
		public bool autoCreate = true;
		/** How the item count is displayed */
		public InventoryItemCountDisplay inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;
		/** If craftingType = CraftingElementType.Ingredients, what happens to items when they are removed from the container */
		public ContainerSelectMode containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
		/** If craftingType = CraftingElementType.Output, default click behaiour is disabled */
		public bool preventDefaultClicks = false;
		/** If True, only inventory items (InvItem) with a specific category will be allowed */
		public bool limitToCategory;
		/** The category IDs to limit the display of inventory items by, if limitToCategory = True */
		public List<int> categoryIDs = new List<int> ();
		/** The Crafting element of type 'Ingredients' that this is linked to, if craftingType = CraftingElementType.Output. If blank, it will be auto-set to the first-found Ingredient box in the same menu */
		public string linkedIngredients = "";

		private Recipe activeRecipe;
		private string[] labels = null;


		public override void Declare ()
		{
			uiSlots = null;
			isVisible = true;
			isClickable = true;
			numSlots = 4;
			SetSize (new Vector2 (6f, 10f));
			textEffects = TextEffects.None;
			outlineSize = 2f;
			effectColour = Color.black;
			craftingType = CraftingElementType.Ingredients;
			displayType = ConversationDisplayType.IconOnly;
			uiHideStyle = UIHideStyle.DisableObject;
			actionListOnWrongIngredients = null;
			linkUIGraphic = LinkUIGraphic.ImageComponent;
			invInstances = new List<InvInstance> ();
			autoCreate = true;
			preventDefaultClicks = false;
			inventoryItemCountDisplay = InventoryItemCountDisplay.OnlyIfMultiple;
			containerSelectMode = ContainerSelectMode.MoveToInventoryAndSelect;
			limitToCategory = false;
			categoryIDs = new List<int> ();
			linkedIngredients = string.Empty;
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuCrafting newElement = CreateInstance<MenuCrafting> ();
			newElement.Declare ();
			newElement.CopyCrafting (this, ignoreUnityUI);
			return newElement;
		}


		private void CopyCrafting (MenuCrafting _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlots = null;
			}
			else
			{
				uiSlots = new UISlot[_element.uiSlots.Length];
				for (int i = 0; i < uiSlots.Length; i++)
				{
					uiSlots[i] = new UISlot (_element.uiSlots[i]);
				}
			}

			isClickable = _element.isClickable;
			textEffects = _element.textEffects;
			outlineSize = _element.outlineSize;
			effectColour = _element.effectColour;
			numSlots = _element.numSlots;
			craftingType = _element.craftingType;
			displayType = _element.displayType;
			uiHideStyle = _element.uiHideStyle;
			actionListOnWrongIngredients = _element.actionListOnWrongIngredients;
			linkUIGraphic = _element.linkUIGraphic;
			autoCreate = _element.autoCreate;
			inventoryItemCountDisplay = _element.inventoryItemCountDisplay;
			containerSelectMode = _element.containerSelectMode;
			preventDefaultClicks = _element.preventDefaultClicks;
			linkedIngredients = _element.linkedIngredients;

			limitToCategory = _element.limitToCategory;
			categoryIDs = new List<int> ();
			if (_element.categoryIDs != null)
			{
				foreach (int _categoryID in _element.categoryIDs)
				{
					categoryIDs.Add (_categoryID);
				}
			}

			UpdateLimitCategory ();
			PopulateList ();

			base.Copy (_element);
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
			int i = 0;
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.LinkUIElements (canvas, linkUIGraphic);

				if (addEventListeners)
				{
					if (uiSlot != null && uiSlot.uiButton)
					{
						int j = i;

						uiSlot.uiButton.onClick.AddListener (() => {
							ProcessClickUI (_menu, j, MouseState.SingleClick);
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
			if (uiSlots != null && _slot >= 0 && _slot < uiSlots.Length)
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
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuCrafting)";

			MenuSource source = menu.menuSource;

			CustomGUILayout.BeginVertical ();

			craftingType = (CraftingElementType) CustomGUILayout.EnumPopup ("Crafting element type:", craftingType, apiPrefix + ".craftingType", "What part of the crafting process this element is used for");

			if (craftingType == CraftingElementType.Ingredients)
			{
				numSlots = CustomGUILayout.IntSlider ("Number of slots:", numSlots, 1, 12);
				if (source == MenuSource.AdventureCreator && numSlots > 1)
				{
					slotSpacing = EditorGUILayout.Slider (new GUIContent ("Slot spacing:", "The distance between slots"), slotSpacing, 0f, 20f);
					orientation = (ElementOrientation) CustomGUILayout.EnumPopup ("Slot orientation:", orientation, apiPrefix + ".orientation", "The slot orientation");
					if (orientation == ElementOrientation.Grid)
					{
						gridWidth = CustomGUILayout.IntSlider ("Grid size:", gridWidth, 1, 10, apiPrefix + ".gridWidth");
					}
				}
				containerSelectMode = (ContainerSelectMode) CustomGUILayout.EnumPopup ("Behaviour after taking?", containerSelectMode, apiPrefix + ".containerSelectMode", "What happens to items when they are taken");
			}
			else
			{
				autoCreate = CustomGUILayout.Toggle ("Result is automatic?", autoCreate, apiPrefix + ".autoCreate", "If True, then the output ingredient will appear automatically when the correct ingredients are used. If False, then the player will have to run the 'Inventory: Crafting' Action as an additional step.");
				preventDefaultClicks = CustomGUILayout.Toggle ("Prevent default clicks?", preventDefaultClicks, apiPrefix + ".preventDefaultClicks", "If True, then default behavior when clicked is disabled.");

				numSlots = 1;
				actionListOnWrongIngredients = ActionListAssetMenu.AssetGUI ("ActionList on fail:", actionListOnWrongIngredients, menu.title + "_OnFailRecipe", apiPrefix + ".actionListOnWrongIngredients", "Ahe ActionList asset to run if a crafting attempt is made but no succesful recipe is possible. This only works if crafting is performed manually via the Inventory: Crafting Action.");
				if (actionListOnWrongIngredients != null)
				{
					EditorGUILayout.HelpBox ("This ActionList will only be run if the result is calculated manually via the 'Inventory: Crafting' Action.", MessageType.Info);
				}

				linkedIngredients = CustomGUILayout.TextField ("Linked 'Ingredients' box:", linkedIngredients, apiPrefix + ".linkedIngredients", "The Crafting element of type 'Ingredients' that this is linked to in the same Menu. If blank, it will be auto-set to the first-found Ingredient box in the same Menu.");
			}

			displayType = (ConversationDisplayType) CustomGUILayout.EnumPopup ("Display type:", displayType, apiPrefix + ".displayType", "How items are displayed");
			if (displayType == ConversationDisplayType.IconAndText && source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("'Icon And Text' mode is only available for Unity UI-based Menus.", MessageType.Warning);
			}

			inventoryItemCountDisplay = (InventoryItemCountDisplay) CustomGUILayout.EnumPopup ("Display item amounts:", inventoryItemCountDisplay, apiPrefix + ".inventoryItemCountDisplay", "How item counts are drawn");

			if (source != MenuSource.AdventureCreator)
			{
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
				uiHideStyle = (UIHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiHideStyle, apiPrefix + ".uiHideStyle", "The method by which this element (or slots within it) are hidden from view when made invisible");
				EditorGUILayout.LabelField ("Linked button objects", EditorStyles.boldLabel);

				uiSlots = ResizeUISlots (uiSlots, numSlots);

				for (int i = 0; i < uiSlots.Length; i++)
				{
					uiSlots[i].LinkedUiGUI (i, source);
				}

				linkUIGraphic = (LinkUIGraphic) CustomGUILayout.EnumPopup ("Link graphics to:", linkUIGraphic, "", "What Image component the element's graphics should be linked to");
			}

			isClickable = true;
			CustomGUILayout.EndVertical ();

			ShowCategoriesUI (apiPrefix);

			PopulateList ();
			base.ShowGUI (menu);
		}


		protected override void ShowTextGUI (string apiPrefix)
		{
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");
			if (textEffects != TextEffects.None)
			{
				outlineSize = CustomGUILayout.Slider ("Effect size:", outlineSize, 1f, 5f, apiPrefix + ".outlineSize", "The effect thickness");
				effectColour = CustomGUILayout.ColorField ("Effect colour:", effectColour, apiPrefix + ".effectColour", "The effect colour");
			}
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (craftingType != CraftingElementType.Ingredients && actionListOnWrongIngredients == actionListAsset)
				return true;
			return false;
		}


		private void ShowCategoriesUI (string apiPrefix)
		{
			CustomGUILayout.BeginVertical ();

			limitToCategory = CustomGUILayout.Toggle ("Limit by category?", limitToCategory, apiPrefix + ".limitToCategory", "If True, only items with a specific category will be displayed");
			if (limitToCategory)
			{
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
						for (int i = 0; i < bins.Count; i++)
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
			LimitUISlotVisibility (uiSlots, 0, uiHideStyle);
		}


		public override string GetHotspotLabelOverride (int _slot, int _language)
		{
			if (uiSlots != null && _slot < uiSlots.Length && !uiSlots[_slot].CanOverrideHotspotLabel) return string.Empty;

			InvItem invItem = GetItem (_slot);
			if (invItem != null)
			{
				return invItem.GetLabel (_language);
			}

			return string.Empty;
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			string fullText = string.Empty;
			if (displayType == ConversationDisplayType.TextOnly || displayType == ConversationDisplayType.IconAndText)
			{
				InvItem invItem = GetItem (_slot);
				if (invItem != null)
				{
					fullText = invItem.GetLabel (languageNumber);
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
				labels = new string[numSlots];
			}
			labels[_slot] = fullText;

			if (Application.isPlaying)
			{
				if (uiSlots != null && uiSlots.Length > _slot)
				{
					LimitUISlotVisibility (uiSlots, numSlots, uiHideStyle);

					uiSlots[_slot].SetText (labels[_slot]);

					switch (displayType)
					{
						case ConversationDisplayType.IconOnly:
						case ConversationDisplayType.TextOnly:
							if ((craftingType == CraftingElementType.Ingredients && GetItem (_slot) != null) || (craftingType == CraftingElementType.Output && invInstances.Count > 0))
							{
								uiSlots[_slot].SetImage (GetTexture (_slot));
							}
							else
							{
								uiSlots[_slot].SetImage (null);
							}
							break;

						default:
							break;
					}
				}
			}
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			if (craftingType == CraftingElementType.Ingredients)
			{
				if (Application.isPlaying && KickStarter.settingsManager.selectInventoryDisplay == SelectInventoryDisplay.HideFromMenu && ItemIsSelected (_slot))
				{
					if (!invInstances[_slot].IsPartialTransfer ())
					{
						// Display as normal if we only have one selected from many
						return;
					}
				}

				if (displayType == ConversationDisplayType.IconOnly)
				{
					GUI.Label (GetSlotRectRelative (_slot), string.Empty, _style);

					if (Application.isPlaying && GetItem (_slot) == null)
					{
						return;
					}
					DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), _slot);
					_style.normal.background = null;
				}
				else
				{
					if (GetItem (_slot) == null && Application.isPlaying)
					{
						GUI.Label (GetSlotRectRelative (_slot), string.Empty, _style);
					}
				}

				DrawText (_style, _slot, zoom);
			}
			else if (craftingType == CraftingElementType.Output)
			{
				GUI.Label (GetSlotRectRelative (_slot), string.Empty, _style);
				if (invInstances.Count > 0)
				{
					if (displayType == ConversationDisplayType.IconOnly)
					{
						DrawTexture (ZoomRect (GetSlotRectRelative (_slot), zoom), _slot);
					}
					DrawText (_style, _slot, zoom);
				}
			}
		}


		private void DrawText (GUIStyle _style, int _slot, float zoom)
		{
			if (_slot >= labels.Length) return;
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style, effectColour, _style.normal.textColor, outlineSize, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (GetSlotRectRelative (_slot), zoom), labels[_slot], _style);
			}
		}


		private bool HandleDefaultClick (MouseState _mouseState, int _slot)
		{
			InvInstance clickedInstance = GetInstance (_slot);

			if (_mouseState == MouseState.SingleClick)
			{
				if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (InvInstance.IsValid (clickedInstance))
					{
						// Clicked on an item while nothing selected

						switch (containerSelectMode)
						{
							case ContainerSelectMode.MoveToInventory:
							case ContainerSelectMode.MoveToInventoryAndSelect:
								bool selectItem = (containerSelectMode == ContainerSelectMode.MoveToInventoryAndSelect);

								ItemStackingMode itemStackingMode = clickedInstance.ItemStackingMode;
								if (itemStackingMode != ItemStackingMode.All)
								{
									// Only take one
									clickedInstance.TransferCount = 1;
								}

								InvInstance newInstance = KickStarter.runtimeInventory.PlayerInvCollection.Add (clickedInstance);
								if (selectItem && InvInstance.IsValid (newInstance))
								{
									KickStarter.runtimeInventory.SelectItem (newInstance);
								}
								break;

							case ContainerSelectMode.SelectItemOnly:
								KickStarter.runtimeInventory.SelectItem (clickedInstance);
								break;
						}

						return true;
					}
				}
				else
				{
					// Clicked while selected

					if (KickStarter.runtimeInventory.SelectedInstance == clickedInstance && KickStarter.runtimeInventory.SelectedInstance.CanStack ())
					{
						KickStarter.runtimeInventory.SelectedInstance.AddStack ();
					}
					else
					{
						if (limitToCategory && categoryIDs.Count > 0 && !categoryIDs.Contains (KickStarter.runtimeInventory.SelectedInstance.InvItem.binID))
						{
							return false;
						}

						IngredientsInvCollection.Insert (KickStarter.runtimeInventory.SelectedInstance, _slot, OccupiedSlotBehaviour.FailTransfer);
						KickStarter.runtimeInventory.SetNull ();
					}

					return true;
				}
			}
			else if (_mouseState == MouseState.RightClick)
			{
				if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (KickStarter.runtimeInventory.SelectedInstance == clickedInstance && KickStarter.runtimeInventory.SelectedInstance.ItemStackingMode == ItemStackingMode.Stack)
					{
						KickStarter.runtimeInventory.SelectedInstance.RemoveStack ();
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


		private bool ClickOutput (AC.Menu _menu, MouseState _mouseState)
		{
			if (invInstances.Count > 0)
			{
				if (_mouseState == MouseState.SingleClick && !InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance))
				{
					if (!preventDefaultClicks)
					{
						// Pick up created item
						switch (activeRecipe.onCreateRecipe)
						{
							case OnCreateRecipe.SelectItem:
								KickStarter.runtimeInventory.PerformCrafting (IngredientsInvCollection, activeRecipe, true);
								break;

							case OnCreateRecipe.JustMoveToInventory:
								KickStarter.runtimeInventory.PerformCrafting (IngredientsInvCollection, activeRecipe, false);
								break;

							case OnCreateRecipe.RunActionList:
								ActionListAsset actionList = activeRecipe.invActionList;
								KickStarter.runtimeInventory.PerformCrafting (IngredientsInvCollection, activeRecipe, false);
								if (actionList)
								{
									AdvGame.RunActionListAsset (actionList);
								}
								break;

							default:
								break;
						}
					}

					return true;
				}
			}

			return false;
		}


		public override void RecalculateSize (MenuSource source)
		{
			PopulateList ();

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


		private void PopulateList ()
		{
			if (Application.isPlaying)
			{
				switch (craftingType)
				{
					case CraftingElementType.Ingredients:
						invInstances = IngredientsInvCollection.InvInstances;
						return;

					case CraftingElementType.Output:
						if (autoCreate)
						{
							SetOutput ();
						}
						else if (activeRecipe != null)
						{
							Recipe recipe = KickStarter.runtimeInventory.CalculateRecipe (IngredientsInvCollection);
							if (recipe != activeRecipe)
							{
								activeRecipe = null;
								invInstances = new List<InvInstance> ();
							}
						}
						return;

					default:
						break;
				}
			}
			else
			{
				invInstances = new List<InvInstance> ();
				if (AdvGame.GetReferences ().inventoryManager)
				{
					foreach (InvItem _item in AdvGame.GetReferences ().inventoryManager.items)
					{
						invInstances.Add (new InvInstance (_item));

						if (craftingType == CraftingElementType.Output)
						{
							return;
						}
						else if (numSlots <= invInstances.Count)
						{
							return;
						}
					}
				}
				return;
			}
		}


		/** Creates and displays the correct InvItem, based on the current Recipe, provided craftingType = CraftingElementType.Output. */
		public void SetOutput ()
		{
			if (craftingType != CraftingElementType.Output)
			{
				return;
			}

			int existingItemID = (invInstances != null && invInstances.Count > 0 && invInstances[0] != null) ? invInstances[0].ItemID : -1;

			invInstances = new List<InvInstance> ();

			activeRecipe = KickStarter.runtimeInventory.CalculateRecipe (IngredientsInvCollection);
			if (activeRecipe != null)
			{
				InvItem invItem = KickStarter.inventoryManager.GetItem (activeRecipe.resultID);
				if (invItem == null)
				{
					activeRecipe = null;
					return;
				}
				else if (limitToCategory && categoryIDs.Count > 0 && !categoryIDs.Contains (invItem.binID))
				{
					activeRecipe = null;
					return;
				}

				if (activeRecipe.actionListOnCreate && !KickStarter.actionListAssetManager.IsListRunning (activeRecipe.actionListOnCreate))
				{
					AdvGame.RunActionListAsset (activeRecipe.actionListOnCreate);
				}

				InvItem resultingItem = KickStarter.inventoryManager.GetItem (activeRecipe.resultID);
				InvInstance resultingItemInstance = new InvInstance (resultingItem, 1);
				invInstances.Add (resultingItemInstance);

				if (activeRecipe.resultID != existingItemID)
				{
					KickStarter.eventManager.Call_OnCraftingSucceed (activeRecipe, resultingItemInstance);
				}
			}
			else
			{
				if (!autoCreate && actionListOnWrongIngredients)
				{
					actionListOnWrongIngredients.Interact ();
				}
			}
		}


		private Texture GetTexture (int i)
		{
			InvInstance invInstance = GetInstance (i);
			if (InvInstance.IsValid (invInstance))
			{
				return invInstance.Tex;
			}
			return null;
		}


		private void DrawTexture (Rect rect, int i)
		{
			Texture tex = GetTexture (i);

			if (tex)
			{
				GUI.DrawTexture (rect, tex, ScaleMode.StretchToFill, true, 0f);
			}
		}


		public override string GetLabel (int i, int languageNumber)
		{
			InvItem invItem = GetItem (i);
			if (invItem == null)
			{
				return string.Empty;
			}

			return invItem.GetLabel (languageNumber);
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


		/**
		 * <summary>Gets the InvInstance displayed in a specific slot.</summary>
		 * <param name = "i">The index number of the slot</param>
		 * <returns>The InvInstance displayed in the slot</returns>
		 */
		public InvInstance GetInstance (int i)
		{
			if (craftingType == CraftingElementType.Ingredients && !Application.isPlaying)
			{
				i = 0;
			}

			if (i >= 0 && i < invInstances.Count)
			{
				return invInstances[i];
			}
			return null;
		}


		public InvItem GetItem (int i)
		{
			if (craftingType == CraftingElementType.Ingredients && !Application.isPlaying)
			{
				i = 0;
			}

			if (i >= 0 && i < invInstances.Count)
			{
				if (InvInstance.IsValid (invInstances[i]))
				{
					return invInstances[i].InvItem;
				}
			}
			return null;
		}


		private string GetCount (int i)
		{
			if (inventoryItemCountDisplay == InventoryItemCountDisplay.Never) return string.Empty;

			InvInstance invInstance = GetInstance (i);
			if (InvInstance.IsValid (invInstance))
			{
				if (inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfMultiple && invInstance.Count < 2)
				{
					return string.Empty;
				}

				if (inventoryItemCountDisplay == InventoryItemCountDisplay.OnlyIfStackable && (!invInstance.InvItem.canCarryMultiple || invInstance.InvItem.maxCount <= 1))
				{
					return string.Empty;
				}

				string customText = KickStarter.eventManager.Call_OnRequestInventoryCountText (invInstance, false);
				if (!string.IsNullOrEmpty (customText))
				{
					return customText;
				}

				if (ItemIsSelected (i))
				{
					return invInstance.GetInventoryDisplayCount ().ToString ();
				}
				return invInstance.Count.ToString ();
			}
			return string.Empty;
		}


		private bool ItemIsSelected (int index)
		{
			if (!InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance)) return false;

			if (index > 0 && index < invInstances.Count && (!KickStarter.settingsManager.InventoryDragDrop || KickStarter.playerInput.GetDragState () == DragState.Inventory))
			{
				return (invInstances[index] == KickStarter.runtimeInventory.SelectedInstance);
			}
			return false;
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (KickStarter.stateHandler.gameState == GameState.Cutscene)
			{
				return false;
			}

			bool clickConsumed = false;

			switch (craftingType)
			{
				case CraftingElementType.Ingredients:
					clickConsumed = HandleDefaultClick (_mouseState, _slot);
					break;

				case CraftingElementType.Output:
					clickConsumed = ClickOutput (_menu, _mouseState);
					break;

				default:
					break;
			}

			PlayerMenus.ResetInventoryBoxes ();
			_menu.Recalculate ();

			if (clickConsumed)
			{
				base.ProcessClick (_menu, _slot, _mouseState);
				return true;
			}

			return false;
		}


		protected override void AutoSize ()
		{
			if (invInstances.Count > 0)
			{
				foreach (InvInstance invInstance in invInstances)
				{
					if (InvInstance.IsValid (invInstance))
					{
						switch (displayType)
						{
							case ConversationDisplayType.IconOnly:
								AutoSize (new GUIContent (invInstance.Tex));
								break;

							case ConversationDisplayType.TextOnly:
								AutoSize (new GUIContent (invInstance.InvItem.label));
								break;

							default:
								break;
						}
						return;
					}
				}
			}
			else
			{
				AutoSize (GUIContent.none);
			}
		}


		/**
		 * <summary>Gets the slot index number that a given InvItem (inventory item) appears in.</summary>
		 * <param name = "itemID">The ID number of the InvItem to search for</param>
		 * <returns>The slot index number that the inventory item appears in</returns>
		 */
		public int GetItemSlot (int itemID)
		{
			for (int i = 0; i < invInstances.Count; i++)
			{
				if (InvInstance.IsValid (invInstances[i]) && invInstances[i].ItemID == itemID)
				{
					if (craftingType == CraftingElementType.Ingredients)
					{
						return i;
					}
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


		/** The List of inventory item instances that are currently on display */
		public List<InvInstance> InvInstances
		{
			get
			{
				return invInstances;
			}
		}


		public Recipe ActiveRecipe
		{
			get
			{
				return activeRecipe;
			}
		}

		
		public InvCollection IngredientsInvCollection
		{
			get
			{
				switch (craftingType)
				{
					case CraftingElementType.Ingredients:
					default:
						return KickStarter.runtimeInventory.GetIngredientsInvCollection (ParentMenu ? ParentMenu.title : string.Empty, title);

					case CraftingElementType.Output:
						{
							if (string.IsNullOrEmpty (linkedIngredients) && ParentMenu)
							{
								foreach (MenuElement element in ParentMenu.elements)
								{
									if (element != this && element is MenuCrafting)
									{
										MenuCrafting craftingElement = element as MenuCrafting;
										if (craftingElement.craftingType == CraftingElementType.Ingredients)
										{
											linkedIngredients = craftingElement.title;
											break;
										}
									}
								}

								if (string.IsNullOrEmpty (linkedIngredients))
								{
									ACDebug.LogWarning ("Crafting Output element '" + title + "' cannot find an associated Ingredients element in the same Menu '" + ParentMenu.title + "'");
								}
							}

							return KickStarter.runtimeInventory.GetIngredientsInvCollection (ParentMenu ? ParentMenu.title : string.Empty, linkedIngredients);
						}
				}
			}
		}

	}

}