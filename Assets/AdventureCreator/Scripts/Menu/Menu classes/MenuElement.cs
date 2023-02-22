/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuElement.cs"
 * 
 *	This is the base class for all menu elements.  It should never
 *	be added itself to a menu, as it is only a container of shared data.
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * The base class for all elements that sit inside a Menu.
	 * It should never be added itself to a menu, as it is only a container of shared data.
	 * Like Menus, MenuElements can be drawn either through OnGUI calls, or with Unity's UI system.
	 * Elements can consist multiple "slots" that are arranged together.  If an element has only one slot, then the slot and element are interchangeable so far as display goes.
	 */
	[System.Serializable]
	public class MenuElement : ScriptableObject
	{

		/** A unique identifier */
		public int ID;
		/** A text identifier, used by PlayerMenus */
		public string title = "Element";
		/** The size of a single OnGUI slot */
		public Vector2 slotSize;
		/** How an OnGUI element is scaled (AbsolutePixels, Automatic, Manual) */
		public AC_SizeType sizeType;
		/** How an OnGUI element is positioned (AbsolutePixels, Aligned, RelativeToMenuSize) */
		public AC_PositionType2 positionType;
		/** The spacing between slots (OnGUI only) */
		public float slotSpacing = 0f;
		/** The translation ID, as set within SpeechManager */
		public int lineID = -1;
		/** The name of the input button that triggers the element when pressed */
		public string alternativeInputButton = "";

		/** The text font (OnGUI only) */
		public Font font;
		/** The font size (OnGUI only) */
		public float fontScaleFactor = 60f;
		/** The font colour (OnGUI only) */
		public Color fontColor = Color.white;
		/** The font colour when the element is highlighted (OnGUI only) */
		public Color fontHighlightColor = Color.white;

		/** If True, the element is enabled and visible */
		[SerializeField] protected bool isVisible = true;

		/** If True, then the element is interactive */
		public bool isClickable;
		/** How slots are arranged, if there are multiple (Horizontal, Vertical, Grid) */
		public ElementOrientation orientation = ElementOrientation.Vertical;
		/** The number of columns in a grid, if orientation = ElementOrientation.Grid */
		public int gridWidth = 3;

		/** A texture to display underneath the element text */
		public Texture2D backgroundTexture;
		/** If True, and the element has more than one slot, then each slot will have its own background texture - as opposed to a single background texture that spans the whole element */
		public bool singleSlotBackgrounds = false;
		/** The texture to overlay when the element is highlighted (OnGUI only) */
		public Texture2D highlightTexture;

		/** The sound to play when the mouse cursor hovers over the element */
		public AudioClip hoverSound;
		/** The sound to play when the element is clicked on */
		public AudioClip clickSound;

		/** If True, then the mouse cursor will change when it hovers over the element */
		public bool changeCursor = false;
		/** The ID number of the cursor (in CursorManager's cursorIcons) to display when the mouse hovers of the element, if changeCursor = True */
		public int cursorID = 0;
		/** The ConstantID number of its GameObject counterpart (Unity UI only) */
		public int linkedUiID;

		protected int offset = 0;
		private Vector2 dragOffset;

		/** If an AC element and set to scale automatically, how much of the width of the screen it can cover */
		public float maxAutoWidthFactor = 0.5f;

		#if UNITY_EDITOR
		private bool doProportionalScaling = false;
		#endif

		private string cachedLabel;
		protected Menu parentMenu;

		[SerializeField] protected Rect relativeRect;
		[SerializeField] protected Vector2 relativePosition;
		[SerializeField] protected int numSlots;
		

		/** Initialises the MenuElement when it is created within MenuManager. */
		public virtual void Declare ()
		{
			linkedUiID = 0;
			fontScaleFactor = 2f;
			fontColor = Color.white;
			fontHighlightColor = Color.white;
			highlightTexture = null;
			orientation = ElementOrientation.Vertical;
			positionType = AC_PositionType2.Aligned;
			sizeType = AC_SizeType.Automatic;
			gridWidth = 3;
			lineID = -1;
			hoverSound = null;
			clickSound = null;
			dragOffset = Vector2.zero;
			changeCursor = false;
			cursorID = 0;
			alternativeInputButton = "";
			maxAutoWidthFactor = 0.5f;
		}


		/**
		 * <summary>Creates and returns a new MenuElement that has the same values as itself.</summary>
		 * <param name = "fromEditor">If True, the duplication was done within the Menu Manager and not as part of the gameplay initialisation.</param>
		 * <param name = "ignoreUnityUI">If True, variables associated with Unity UI will not be transferred</param>
		 * <returns>A new MenuElement with the same values as itself</returns>
		 */
		public virtual MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			return null;
		}


		/**
		 * <summary>Copies the values of another MenuElement onto itself.</summary>
		 * <param name = "_element">The MenuElement to copy values from</param>
		 */
		public virtual void Copy (MenuElement _element)
		{
			linkedUiID = _element.linkedUiID;
			ID = _element.ID;
			title = _element.title;
			slotSize = _element.slotSize;
			sizeType = _element.sizeType;
			positionType = _element.positionType;
			relativeRect = _element.relativeRect;
			numSlots = _element.numSlots;
			lineID = _element.lineID;
			slotSpacing = _element.slotSpacing;
		
			font = _element.font;
			fontScaleFactor = _element.fontScaleFactor;
			fontColor = _element.fontColor;
			fontHighlightColor = _element.fontHighlightColor;
			highlightTexture = _element.highlightTexture;

			isVisible = _element.isVisible;
			isClickable = _element.isClickable;
			orientation = _element.orientation;
			gridWidth = _element.gridWidth;

			backgroundTexture = _element.backgroundTexture;
			singleSlotBackgrounds = _element.singleSlotBackgrounds;

			hoverSound = _element.hoverSound;
			clickSound = _element.clickSound;

			relativePosition = _element.relativePosition;
			dragOffset = Vector2.zero;

			changeCursor = _element.changeCursor;
			cursorID = _element.cursorID;
			alternativeInputButton = _element.alternativeInputButton;
			maxAutoWidthFactor = _element.maxAutoWidthFactor;
		}


		/**
		 * <summary>Performs any initialisation that can only be done once the element has been instantiated at runtime.</summary>
		 * <param name = "_menu">The Menu that the elemnt is a part of.</param>
		 */
		public virtual void Initialise (AC.Menu _menu)
		{
			parentMenu = _menu;
		}


		/**
		 * <summary>Initialises the linked Unity UI GameObject.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name="canvas">The runtime Canvas associated with the Menu</param>
		 * <param name="addEventListeners">If True, then event listeners should be added to the UI's Interactive component(s)</param>
		 */
		public virtual void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{}


		protected void CreateUIEvent (UnityEngine.UI.Button uiButton, AC.Menu _menu, UIPointerState uiPointerState = UIPointerState.PointerClick, int _slotIndex = 0, bool liveState = true)
		{
			liveState = false; // Causing issues, just use Event System

			if (uiPointerState == UIPointerState.PointerClick)
			{
				uiButton.onClick.AddListener (() => {
					ProcessClickUI (_menu, _slotIndex, liveState ? KickStarter.playerInput.GetMouseState () : MouseState.SingleClick);
				});
			}
			else
			{
				EventTrigger eventTrigger = uiButton.gameObject.GetComponent <EventTrigger>();
				if (eventTrigger == null)
				{
					eventTrigger = uiButton.gameObject.AddComponent <EventTrigger>();
				}
				EventTrigger.Entry entry = new EventTrigger.Entry ();

				if (uiPointerState == UIPointerState.PointerDown)
				{
					entry.eventID = EventTriggerType.PointerDown;
				}
				else if (uiPointerState == UIPointerState.PointerEnter)
				{
					entry.eventID = EventTriggerType.PointerEnter;
				}

				entry.callback.AddListener ((eventData) => {
					ProcessClickUI (_menu, _slotIndex, liveState ? KickStarter.playerInput.GetMouseState () : MouseState.SingleClick);
				} );

				eventTrigger.triggers.Add (entry);
			}
		}


		protected void CreateHoverSoundHandler (Selectable selectable, AC.Menu _menu, int _slotIndex = 0)
		{
			if (selectable == null)
			{
				ACDebug.LogWarning ("No linked Selectable found for element " + title + " inside menu " + _menu, _menu.RuntimeCanvas);
				return;
			}

			UISlotClick uiSlotClick = selectable.gameObject.GetComponent <UISlotClick>();
			if (uiSlotClick == null)
			{
				uiSlotClick = selectable.gameObject.AddComponent<UISlotClick>();
				uiSlotClick.Setup (_menu, this, _slotIndex);
			}
		}


		protected virtual void ProcessClickUI (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (_menu.ignoreMouseClicks)
			{
				return;
			}

			KickStarter.playerInput.ResetClick ();
			ProcessClick (_menu, _slot, _mouseState);
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_slot">The index number of the slot that was clicked on</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <returns>True if the click had an effect, and so should be consumed</returns>
		 */
		public virtual bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (clickSound)
			{
				KickStarter.sceneSettings.PlayDefaultSound (clickSound, false);
			}

			KickStarter.eventManager.Call_OnMenuElementClick (_menu, this, _slot, (int) _mouseState);
			return true;
		}


		/**
		 * <summary>Performs what should happen when the element is clicked on continuously.</summary>
		 * <param name = "_menu">The element's parent Menu</param>
		 * <param name = "_mouseState">The state of the mouse button</param>
		 * <returns>True if the click has an effect</returns>
		 */
		public virtual bool ProcessContinuousClick (AC.Menu _menu, MouseState _mouseState)
		{
			return false;
		}


		/**
		 * <summary>Gets the linked Unity UI GameObject associated with this element.</summary>
		 * <param name = "slotIndex">The slot index, if the element has multiple slots</param>
		 * <returns>The Unity UI GameObject associated with the element</returns>
		 */
		public virtual GameObject GetObjectToSelect (int slotIndex = 0)
		{
			return null;
		}


		/**
		 * <summary>Gets the boundary of the element, or a slot within it.</summary>
		 * <param name = "_slot">The index number of the slot to get the boundary of</param>
		 * <returns>The boundary Rect of the slot.  If the element doesn't have multiple slots, the boundary of the whole element will be returned.</returns>
		 */
		public virtual RectTransform GetRectTransform (int _slot)
		{
			return null;
		}


		/**
		 * <summary>Assigns the element to a specific Speech line.</summary>
		 * <param name = "_speech">The Speech line to assign the element to</param>
		 */
		public virtual void SetSpeech (Speech _speech)
		{}


		/**
		 * Clears any speech text on display.
		 */
		public virtual void ClearSpeech ()
		{}


		/**
		 * <summary>Updates the ID number to something unique.</summary>
		 * <param name = "idArray">An array of existing ID numbers, used to determine a new unique one</param>
		 */
		public void UpdateID (int[] idArray)
		{
			foreach (int _id in idArray)
			{
				if (ID == _id)
				{
					ID ++;
				}
			}
		}


		protected virtual string GetLabelToTranslate ()
		{
			return string.Empty;
		}


		public void UpdateLabel (int languageNumber)
		{
			string label = GetLabelToTranslate ();
			if (!string.IsNullOrEmpty (label))
			{
				cachedLabel = KickStarter.runtimeLanguages.GetTranslation (label, lineID, languageNumber, AC_TextType.MenuElement);
			}
		}


		protected void ClearCache ()
		{
			cachedLabel = string.Empty;
		}


		protected string TranslateLabel (int languageNumber)
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return GetLabelToTranslate ();
			}
			#endif

			if (languageNumber == Options.GetLanguage ())
			{
				if (string.IsNullOrEmpty (cachedLabel))
				{
					UpdateLabel (languageNumber);
				}
				return cachedLabel;
			}

			return KickStarter.runtimeLanguages.GetTranslation (GetLabelToTranslate (), lineID, languageNumber, AC_TextType.MenuElement);
		}


		/**
		 * <summary>Gets the display text of the element, or a slot within it.</summary>
		 * <param name = "slot">The index number of the slot to get text for</param>
		 * <param name = "languageNumber">The index number of the language number to get the text in</param>
		 * <returns>The display text of the element's slot, or the whole element if it only has one slot</returns>
		 */
		public virtual string GetLabel (int slot, int languageNumber)
		{
			return string.Empty;
		}


		/**
		 * <summary>Checks if the element is selected by Unity UI's EventSystem (if the Menu is Unity UI-based).</summary>
		 * <param name = "slotIndex">The element's slot index, if it has multiple slots</param>
		 * <returns>True if the element is selected by Unity UI's EventSystem.</returns>
		 */
		public virtual bool IsSelectedByEventSystem (int slotIndex)
		{
			return false;
		}


		/**
		 * <summary>Checks if the element's linked Selectable is currently Interactable (if the Menu is Unity UI-based).</summary>
		 * <param name = "slotIndex">The element's slot index, if it has multiple slots</param>
		 * <returns>True if the element's linked Selectable is currently Interactable</returns>
		 */
		public virtual bool IsSelectableInteractable (int slotIndex)
		{
			return false;
		}


		/** If True, then the element is visible */
		public virtual bool IsVisible
		{
			get
			{
				return isVisible;
			}
			set
			{
				if (isVisible != value)
				{
					isVisible = value;
					KickStarter.eventManager.Call_OnMenuElementChangeVisibility (this);
				}
			}
		}

		
		#if UNITY_EDITOR
		
		public void ShowGUIStart (Menu menu)
		{
			string apiPrefix = "AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\")";

			EditorGUILayout.Space ();	
			title = CustomGUILayout.TextField ("Element name:", title, apiPrefix + ".title", "A text identifier for use in Actions and custom scripts");
			isVisible = CustomGUILayout.Toggle ("Is visible?", isVisible, apiPrefix + ".IsVisible", "If True, the element is enabled and visible");
			CustomGUILayout.EndVertical ();

			ShowGUI (menu);
		}


		protected virtual void ShowTextGUI (string apiPrefix)
		{
		}


		protected virtual void ShowTextureGUI (string apiPrefix)
		{
		}
		
		
		public virtual void ShowGUI (Menu menu)
		{
			string apiPrefix = "AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\")";

			if (menu.menuSource != MenuSource.AdventureCreator)
			{
				if (isClickable)
				{
					EditorGUILayout.BeginVertical (CustomStyles.thinBox);
					hoverSound = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Hover sound:", hoverSound, false, apiPrefix + ".hoverSound", "The sound to play when the cursor hovers over the element");
					clickSound = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Click sound:", clickSound, false, apiPrefix + ".clickSound", "The sound to play when the element is clicked on");
					CustomGUILayout.EndVertical ();
				}
				return;
			}

			if (!(this is MenuGraphic))
			{
				CustomGUILayout.BeginVertical ();
				font = (Font) CustomGUILayout.ObjectField <Font> ("Font:", font, false, apiPrefix + ".font", "The text font");
				fontScaleFactor = CustomGUILayout.Slider ("Text size:", fontScaleFactor, 1f, 4f, apiPrefix + ".fontScaleFactor", "The font size");
				fontColor = CustomGUILayout.ColorField ("Text colour:", fontColor, apiPrefix + ".fontColor", "The font colour");

				ShowTextGUI (apiPrefix);

				if (isClickable)
				{
					fontHighlightColor = CustomGUILayout.ColorField ("Text colour (highlighted):", fontHighlightColor, apiPrefix + ".fontHighlightColor", "The font colour when the element is highlighted");
				}
				CustomGUILayout.EndVertical ();
			}

			CustomGUILayout.BeginVertical ();

			ShowTextureGUI (apiPrefix);

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Background texture:", "A texture to display underneath the element text"), GUILayout.Width (145f));
			backgroundTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (backgroundTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".backgroundTexture");
			EditorGUILayout.EndHorizontal ();

			if (numSlots > 1 && backgroundTexture)
			{
				singleSlotBackgrounds = CustomGUILayout.ToggleLeft ("Background texture is per slot?", singleSlotBackgrounds, apiPrefix + ".singleSlotBackgrounds", "If True, then each slot will have its own background texture - as opposed to a single background texture that spans the whole element");
			}

			if (isClickable)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Highlight texture:", "The texture to overlay when the element is highlighted"), GUILayout.Width (145f));
				highlightTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (highlightTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".highlightTexture");
				EditorGUILayout.EndHorizontal ();

				hoverSound = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Hover sound:", hoverSound, false, apiPrefix + ".hoverSound", "The sound to play when the cursor hovers over the element");
				clickSound = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Click sound:", clickSound, false, apiPrefix + ".clickSound", "The sound to play when the element is clicked on");
			}

			CustomGUILayout.EndVertical ();
			
			EndGUI (apiPrefix);
		}
		
		
		protected void EndGUI (string apiPrefix)
		{
			CustomGUILayout.BeginVertical ();
			positionType = (AC_PositionType2) CustomGUILayout.EnumPopup ("Position:", positionType, apiPrefix + ".positionType", "How the element is positioned");
			if (positionType == AC_PositionType2.AbsolutePixels)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("X:", GUILayout.Width (15f));
				relativeRect.x = EditorGUILayout.FloatField (relativeRect.x);
				EditorGUILayout.LabelField ("Y:", GUILayout.Width (15f));
				relativeRect.y = EditorGUILayout.FloatField (relativeRect.y);
				EditorGUILayout.EndHorizontal ();
			}
			else if (positionType == AC_PositionType2.RelativeToMenuSize)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("X:", GUILayout.Width (15f));
				relativePosition.x = EditorGUILayout.Slider (relativePosition.x, 0f, 100f);
				EditorGUILayout.LabelField ("Y:", GUILayout.Width (15f));
				relativePosition.y = EditorGUILayout.Slider (relativePosition.y, 0f, 100f);
				EditorGUILayout.EndHorizontal ();
			}
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			sizeType = (AC_SizeType) CustomGUILayout.EnumPopup ("Size:", sizeType, apiPrefix + ".sizeType", "How the element is scaled");
			if (sizeType == AC_SizeType.Manual)
			{
				Vector2 originalSlotSize = slotSize;

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("W:", GUILayout.Width (17f));
				originalSlotSize.x = EditorGUILayout.Slider (slotSize.x, 0f, 100f);
				EditorGUILayout.LabelField ("H:", GUILayout.Width (15f));
				originalSlotSize.y = EditorGUILayout.Slider (slotSize.y, 0f, 100f);

				if (GUILayout.Button ("", (doProportionalScaling) ? CustomStyles.IconLock : CustomStyles.IconUnlock))
				{
					doProportionalScaling = !doProportionalScaling;
				}
				EditorGUILayout.EndHorizontal ();

				if (doProportionalScaling)
				{
					if (!Mathf.Approximately (originalSlotSize.x, slotSize.x))
					{
						float proportion = slotSize.y / slotSize.x;
						originalSlotSize.y = proportion * originalSlotSize.x;
					}
					else if (!Mathf.Approximately (originalSlotSize.y, slotSize.y))
					{
						float proportion = slotSize.x / slotSize.y;
						originalSlotSize.x = proportion * originalSlotSize.y;
					}
				}

				slotSize = originalSlotSize;
			}
			else if (sizeType == AC_SizeType.AbsolutePixels)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Width:", GUILayout.Width (50f));
				slotSize.x = EditorGUILayout.FloatField (slotSize.x);
				EditorGUILayout.LabelField ("Height:", GUILayout.Width (50f));
				slotSize.y = EditorGUILayout.FloatField (slotSize.y);
				EditorGUILayout.EndHorizontal ();
			}
			else if (sizeType == AC_SizeType.Automatic)
			{
				if (font == null && !(this is MenuGraphic))
				{
					EditorGUILayout.HelpBox ("Automatic sizing may not appear correct without a Font assigned above.", MessageType.Warning);
				}

				if (this is MenuLabel)
				{
					maxAutoWidthFactor = CustomGUILayout.Slider ("Max auto-width factor:", maxAutoWidthFactor, 0f, 1f, apiPrefix + ".maxAutoWidthFactor", "How much width, as a proportion of the total screen width, the element can take up when re-sizing itself automatically.");
				}
			}
			CustomGUILayout.EndVertical ();
		}
		
		
		protected void ShowClipHelp ()
		{
			EditorGUILayout.HelpBox ("The OnMenuElementClick custom event will be run when this element is clicked.", MessageType.Info);
		}


		protected T LinkedUiGUI <T> (T field, string label, MenuSource source, string tooltip = "") where T : Component
		{
			field = (T) EditorGUILayout.ObjectField (new GUIContent (label, tooltip), field, typeof (T), true);
			linkedUiID = Menu.FieldToID <T> (field, linkedUiID);
			return Menu.IDToField <T> (field, linkedUiID, source);
		}


		protected UISlot[] ResizeUISlots (UISlot[] uiSlots, int maxSlots)
		{
			List<UISlot> uiSlotsList = new List<UISlot>();
			if (uiSlots == null)
			{
				return uiSlotsList.ToArray ();
			}

			if (maxSlots < 0)
			{
				maxSlots = 0;
			}

			if (uiSlots.Length == maxSlots)
			{
				return uiSlots;
			}

			// Convert to list
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlotsList.Add (uiSlot);
			}

			if (maxSlots < uiSlotsList.Count)
			{
				uiSlotsList.RemoveRange (maxSlots, uiSlotsList.Count - maxSlots);
			}
			else if (maxSlots > uiSlotsList.Count)
			{
				if (maxSlots > uiSlotsList.Capacity)
				{
					uiSlotsList.Capacity = maxSlots;
				}
				for (int i=uiSlotsList.Count; i<maxSlots; i++)
				{
					UISlot newUISlot = new UISlot ();
					uiSlotsList.Add (newUISlot);
				}
			}

			return uiSlotsList.ToArray ();
		}


		protected void ChangeCursorGUI (Menu menu)
		{
			string apiPrefix = "AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\")";

			changeCursor = CustomGUILayout.Toggle ("Change cursor when over?", changeCursor, apiPrefix + ".changeCursor", "If True, then the mouse cursor will change when it hovers over the element");
			if (changeCursor)
			{
				CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
				if (cursorManager)
				{
					int cursorIndex = cursorManager.GetIntFromID (cursorID);
					cursorIndex = CustomGUILayout.Popup ("Cursor:", cursorIndex, cursorManager.GetLabelsArray (), apiPrefix + ".cursorID", "The Cursor to display when the mouse hovers of the element");
					cursorID = cursorManager.cursorIcons[cursorIndex].id;
				}
				else
				{
					EditorGUILayout.HelpBox ("No Cursor Manager found!", MessageType.Warning);
				}
			}
		}


		/**
		 * <summary>Checks if the Element makes references from a given global variable to a given local variable</summary>
		 * <param name = "oldGlobalID">The ID number of the old global variable</param>
		 * <param name = "newLocalID">The ID number of the new local variable</param>
		 * <returns>True if the Element was affected</returns>
		 */
		public virtual bool CheckConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID)
		{
			return false;
		}


		/**
		 * <summary>Gets the number of references the MenuElement makes to a global variable</summary>
		 * <param name = "varID">The global variable's ID number</param>
		 * <returns>The number of references the MenuElement makes to the variable</returns>
		 */
		public virtual int GetVariableReferences (int varID)
		{
			return 0;
		}


		/**
		 * <summary>Updates references the MenuElement makes to a global variable</summary>
		 * <param name = "varID">The global variable's original ID number</param>
		 * <param name = "varID">The global variable's new ID number</param>
		 * <returns>The number of references the MenuElement makes to the variable</returns>
		 */
		public virtual int UpdateVariableReferences (int oldVarID, int newVarID)
		{
			return 0;
		}


		/**
		 * <summary>Checks if the Menu makes reference to a particular ActionList asset</summary>
		 * <param name = "actionListAsset">The ActionList to check for</param>
		 * <returns>True if the Menu references the ActionList</param>
		 */
		public virtual bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			return false;
		}

		#endif


		/**
		 * <summary>Checks if the Element makes reference to a particular GameObject</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <param name = "id">The GameObject's associated ConstantID value</param>
		 * <returns>True if the Element references the GameObject</param>
		 */
		public virtual bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			return false;
		}


		/**
		 * <summary>Gets the slot index that reference a particular GameObject</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <returns>The slot index that references the GameObject</param>
		 */
		public virtual int GetSlotIndex (GameObject gameObject)
		{
			return -1;
		}


		/** Hides all linked Unity UI GameObjects associated with the element. */
		public virtual void HideAllUISlots ()
		{}


		protected void LimitUISlotVisibility (UISlot[] uiSlots, int _numSlots, UIHideStyle uiHideStyle, Texture emptyTexture = null)
		{
			if (uiSlots == null)
			{
				return;
			}

			if (!IsVisible && _numSlots > 0)
			{
				return;
			}

			for (int i=0; i<uiSlots.Length; i++)
			{
				if (i < _numSlots)
				{
					uiSlots[i].ShowUIElement (uiHideStyle);
				}
				else
				{
					bool wasSelected = uiSlots[i].HideUIElement (uiHideStyle);
					if (wasSelected) KickStarter.eventManager.Call_OnHideSelectedElement (parentMenu, this, i);
				}
			}
		}


		protected void LimitUISlotVisibility (UISlot[] uiSlots, int _numSlots, UISelectableHideStyle uiHideStyle)
		{
			if (uiSlots == null)
			{
				return;
			}

			if (!IsVisible && _numSlots > 0)
			{
				return;
			}

			for (int i=0; i<uiSlots.Length; i++)
			{
				if (i < _numSlots)
				{
					uiSlots[i].ShowUIElement (uiHideStyle);
				}
				else
				{
					bool wasSelected = uiSlots[i].HideUIElement (uiHideStyle);
					if (wasSelected) KickStarter.eventManager.Call_OnHideSelectedElement (parentMenu, this, i);
				}
			}
		}


		/**
		 * <summary>Gets a string that overrides the default 'hotspot label'.</summary>
		 * <param name = "_slot">The index number of the slot to get an override label for</param>
		 * <param name = "_language">The index number of the language to display text in</param>
		 * <returns>A string that overrides the default 'hotspot label'. If empty, the default will not be overridden.</returns>
		 */
		public virtual string GetHotspotLabelOverride (int _slot, int _language)
		{
			return string.Empty;
		}


		/**
		 * <summary>Performs all calculations necessary to display the element.</summary>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "languageNumber">The index number of the language to display text in</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public virtual void PreDisplay (int _slot, int languageNumber, bool isActive)
		{}


		/**
		 * <summary>Draws the element using OnGUI.</summary>
		 * <param name = "_style">The GUIStyle to draw with</param>
		 * <param name = "_slot">The index number of the slot to display</param>
		 * <param name = "zoom">The zoom factor</param>
		 * <param name = "isActive">If True, then the element will be drawn as though highlighted</param>
		 */
		public virtual void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			if (backgroundTexture)
			{
				if (singleSlotBackgrounds)
				{
					Rect outlineRect = GetSlotRectRelative (_slot);
					GUI.DrawTexture (ZoomRect (outlineRect, zoom), backgroundTexture, ScaleMode.StretchToFill, true, 0f);
				}
				else if (_slot == 0)
				{
					GUI.DrawTexture (ZoomRect (relativeRect, zoom), backgroundTexture, ScaleMode.StretchToFill, true, 0f);
				}
			}
		}


		/**
		 * <summary>Draws an outline around the element.</summary>
		 * <param name = "isSelected">If True, a different-coloured outline will be used to differentiate it from others</param>
		 * <param name = "_menu">The element's parent Menu</param>
		 */
		public virtual void DrawOutline (bool isSelected, AC.Menu _menu)
		{
			Color boxColor = Color.yellow;
			if (isSelected)
			{
				boxColor = Color.red;
			}
			for (int i=0; i<GetNumSlots (); i++)
			{
				if (i > 0)
				{
					boxColor = Color.blue;
				}
				Rect outlineRect = _menu.GetRectAbsolute (GetSlotRectRelative (i));
				DrawStraightLine.DrawBox (outlineRect, boxColor, 1f, false, 0);
			}
		}


		/**
		 * <summary>Gets the element's slot centres, as an array of Vector2s.  This is used when keyboard-navigating menus</summary>
		 * <param name = "_menu">The parent Menu</param>
		 * <returns>The element's slot centres, as an array of Vector2s.</returns>
		 */
		public Vector2[] GetSlotCentres (AC.Menu _menu)
		{
			List<Vector2> slotCentres = new List<Vector2>();

			if (isClickable)
			{
				for (int i=0; i<GetNumSlots (); i++)
				{
					Vector2 slotCentre = _menu.GetRectAbsolute (GetSlotRectRelative (i)).center;
					slotCentres.Add (slotCentre);
				}
			}

			return slotCentres.ToArray ();
		}


		protected Rect ZoomRect (Rect rect, float zoom)
		{
			if (Mathf.Approximately (zoom, 1f))
			{
				if (!Application.isPlaying)
				{
					dragOffset = Vector2.zero;
				}

				if (dragOffset != Vector2.zero)
				{
					rect.x += dragOffset.x;
					rect.y += dragOffset.y;
				}

				return rect;
			}

			return (new Rect (rect.x * zoom, rect.y * zoom, rect.width * zoom, rect.height * zoom));
		}


		protected virtual int MaxSlotsForOffset
		{
			get
			{
				return numSlots;
			}
		}


		protected void LimitOffset ()
		{
			if (offset > 0 && (numSlots + offset) > MaxSlotsForOffset)
			{
				offset = MaxSlotsForOffset - numSlots;
			}
			if (offset < 0)
			{
				offset = 0;
			}
		}


		protected void Shift (AC_ShiftInventory shiftType, int maxSlots, int arraySize, int amount)
		{
			int newOffset = offset;

			switch (shiftType)
			{ 
				case AC_ShiftInventory.ShiftPrevious:
					if (offset > 0)
					{
						newOffset -= amount;
						if (newOffset < 0)
						{
							newOffset = 0;
						}
					}
					break;

				case AC_ShiftInventory.ShiftNext:
					{
						newOffset += amount;
						if ((maxSlots + newOffset) >= arraySize)
						{
							newOffset = arraySize - maxSlots;
						}
					}
					break;

				default:
					break;
			}

			if (newOffset != offset)
			{
				offset = newOffset;
				KickStarter.eventManager.Call_OnMenuElementShift (this, shiftType);
				if (parentMenu != null) parentMenu.ResetVisibleElements ();
			}			
		}


		/**
		 * <summary>Shifts which slots are on display, if the number of slots the element has exceeds the number of slots it can show at once.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <param name = "amount">The amount to shift slots by</param>
		 */
		public virtual void Shift (AC_ShiftInventory shiftType, int amount)
		{
			ACDebug.LogWarning ("The MenuElement " + this.title + " cannot be 'Shifted'");
		}


		/**
		 * <summary>Checks if the element's slots can be shifted in a particular direction.</summary>
		 * <param name = "shiftType">The direction to shift slots in (Left, Right)</param>
		 * <returns>True if the element's slots can be shifted in the particular direction</returns>
		 */
		public virtual bool CanBeShifted (AC_ShiftInventory shiftType)
		{
			return true;
		}
		

		/**
		 * <summary>Gets the size of the whole element.</summary>
		 * <returns>The size of the whole element</returns>
		 */
		public Vector2 GetSize ()
		{
			Vector2 size = new Vector2 (relativeRect.width, relativeRect.height);
			return (size);
		}
		

		/**
		 * <summary>Gets the Vector2 from the top-left corner of the parent Menu to the bottom-right corner of the element.</summary>
		 * <returns>The Vector2 from the top-left corner of the parent Menu to the bottom-right corner of the element.</returns>
		 */
		public Vector2 GetSizeFromCorner ()
		{
			Vector2 size = new Vector2 (relativeRect.width + relativeRect.x, relativeRect.height + relativeRect.y);
			return (size);
		}
		

		/**
		 * <summary>Sets the size of an individual slot.</summary>
		 * <param name = "_size">The new size of an individual slot</param>
		 */
		public void SetSize (Vector2 _size)
		{
			slotSize = new Vector2 (_size.x, _size.y);
		}
		

		protected void SetAbsoluteSize (Vector2 _size)
		{
			Vector2 playableAreaSize = (KickStarter.mainCamera) ? KickStarter.mainCamera.GetPlayableScreenArea (false).size : new Vector2 ( ACScreen.width, ACScreen.height);
			SetSize (new Vector2 (_size.x * 100f / playableAreaSize.x, _size.y * 100f / playableAreaSize.y));
		}


		/**
		 * <summary>Gets the number of display slots the element has.
		 * This is not the maximum number of slots that can be shown by shifting - it is the number of slots that are shown at any one time.</summary>
		 * <returns>The number of display slots the element has</returns>
		 */
		public int GetNumSlots ()
		{
			return numSlots;
		}
		

		/**
		 * <summary>Gets the boundary of a slot, as a proportion of the screen size.</summary>
		 * <param name = "_slot">The slot to get the boundary for</param>
		 * <returns>The boundary of a slot, as a proportion of the screen size.</returns>
		 */
		public Rect GetSlotRectRelative (int _slot)
		{
			Vector2 screenFactor = Vector2.one;
			if (sizeType != AC_SizeType.AbsolutePixels)
			{
				if (KickStarter.mainCamera)
				{
					screenFactor = new Vector2 (KickStarter.mainCamera.GetPlayableScreenArea (false).size.x / 100f, KickStarter.mainCamera.GetPlayableScreenArea (false).size.y / 100f);
				}
				else
				{
					screenFactor = new Vector2 (ACScreen.safeArea.size.x / 100f, ACScreen.safeArea.size.y / 100f);
				}
			}

			Rect positionRect = relativeRect;
			positionRect.width = slotSize.x * screenFactor.x;
			positionRect.height = slotSize.y * screenFactor.y;

			if (_slot > numSlots)
			{
				_slot = numSlots;
			}
			
			if (orientation == ElementOrientation.Horizontal)
			{
				positionRect.x += (slotSize.x + slotSpacing) * _slot * screenFactor.x;
			}
			else if (orientation == ElementOrientation.Vertical)
			{
				positionRect.y += (slotSize.y + slotSpacing) * _slot * screenFactor.y;
			}
			else if (orientation == ElementOrientation.Grid)
			{
				int xOffset = _slot + 1;
				float numRows = Mathf.CeilToInt ((float) xOffset / gridWidth) - 1;
				while (xOffset > gridWidth)
				{
					xOffset -= gridWidth;
				}
				xOffset -= 1;

				positionRect.x += (slotSize.x + slotSpacing) * (float) xOffset * screenFactor.x;
				positionRect.y += (slotSize.y + slotSpacing) * numRows * screenFactor.y;
			}

			return (positionRect);
		}
		

		/**
		 * <summary>Recalculates the element's size.
		 * This should be called whenever a Menu's shape is changed.</summary>
		 * <param name = "source">How the parent Menu is displayed (AdventureCreator, UnityUiPrefab, UnityUiInScene)</param>
		 */
		public virtual void RecalculateSize (MenuSource source)
		{
			if (source != MenuSource.AdventureCreator)
			{
				return;
			}

			dragOffset = Vector2.zero;
			Vector2 screenSize = Vector2.one;

			if (sizeType == AC_SizeType.Automatic)
			{
				AutoSize ();
			}

			if (sizeType != AC_SizeType.AbsolutePixels)
			{
				if (KickStarter.mainCamera)
				{
					screenSize = new Vector2 (KickStarter.mainCamera.GetPlayableScreenArea (false).size.x / 100f, KickStarter.mainCamera.GetPlayableScreenArea (false).size.y / 100f);
				}
				else
				{
					screenSize = new Vector2 ( ACScreen.width / 100f, ACScreen.height / 100f);
				}
			}

			switch (orientation)
			{
				case ElementOrientation.Horizontal:
					relativeRect.width = slotSize.x * screenSize.x * numSlots;
					relativeRect.height = slotSize.y * screenSize.y;
					if (numSlots > 1)
					{
						relativeRect.width += slotSpacing * screenSize.x * (numSlots - 1);
					}
					break;

				case ElementOrientation.Vertical:
					relativeRect.width = slotSize.x * screenSize.x;
					relativeRect.height = slotSize.y * screenSize.y * numSlots;
					if (numSlots > 1)
					{
						relativeRect.height += slotSpacing * screenSize.y * (numSlots - 1);
					}
					break;

				case ElementOrientation.Grid:
					if (numSlots < gridWidth)
					{
						relativeRect.width = (slotSize.x + slotSpacing) * screenSize.x * numSlots;
						relativeRect.height = slotSize.y * screenSize.y;
					}
					else
					{
						float numRows = Mathf.CeilToInt ((float) numSlots / gridWidth);

						relativeRect.width = slotSize.x * screenSize.x * gridWidth;
						relativeRect.height = slotSize.y * screenSize.y * numRows;

						if (numSlots > 1)
						{
							relativeRect.width += slotSpacing * screenSize.x * (gridWidth - 1);
							relativeRect.height += slotSpacing * screenSize.y * (numRows - 1);
						}
					}
					break;
			}
		}


		/**
		 * <summary>Gets the size of the font.</summary>
		 * <returns>The size of the font</returns>
		 */
		public int GetFontSize ()
		{
			if (sizeType == AC_SizeType.AbsolutePixels)
			{
				return (int) (fontScaleFactor * 10f);
			}

			float xScale = (KickStarter.mainCamera) ? KickStarter.mainCamera.GetPlayableScreenArea (false).size.x : ACScreen.width;
			return (int) (xScale * fontScaleFactor / 100);
		}

		
		protected void AutoSize (GUIContent content)
		{
			GUIStyle normalStyle = new GUIStyle();

			normalStyle.font = font;
			normalStyle.fontSize = GetFontSize ();

			if (string.IsNullOrEmpty (content.text))
			{
				Vector2 oldSize = normalStyle.CalcSize (content);
				SetAbsoluteSize (oldSize);
				return;
			}

			// Calculate with no word-wrapping
			Vector2 singleLineSize = normalStyle.CalcSize (content);
			
			// Calculate with word-wrapping
			normalStyle.wordWrap = true;
			float maxWidth = ACScreen.safeArea.width * maxAutoWidthFactor;
			float height = normalStyle.CalcHeight (content, maxWidth);
			Vector2 wrappedSize = new Vector2 (maxWidth, height);

			// Compare the two sizes
			if (wrappedSize.y == singleLineSize.y && singleLineSize.x < wrappedSize.x)
			{
				wrappedSize.x = singleLineSize.x;
			}
			wrappedSize.x += 2;
			SetAbsoluteSize (wrappedSize);
		}


		protected virtual void AutoSize ()
		{
			GUIContent content = new GUIContent (backgroundTexture);
			AutoSize (content);
		}
		

		/**
		 * <summary>Sets the element's position.</summary>
		 * <param name = "_position">The new position</param>
		 */
		public void SetPosition (Vector2 _position)
		{
			relativeRect.x = _position.x;
			relativeRect.y = _position.y;
		}


		/**
		 * Sets the element's position, if positionType = AC_PositionType2.RelativeToMenuSize.
		 * <param name = "_size">The size of the parent Menu</param>
		 */
		public void SetRelativePosition (Vector2 _size)
		{
			relativeRect.x = relativePosition.x * _size.x;
			relativeRect.y = relativePosition.y * _size.y;
		}


		/**
		 * Resets the offset by which an element has been moved by dragging.
		 */
		public void ResetDragOffset ()
		{
			dragOffset = Vector2.zero;
		}


		/**
		 * <summary>Offsets an OnGUI MenuElement's position when dragged by a MenuDrag element.</summary>
		 * <param name = "pos">The amoung to offset the position by</param>
		 * <param name = "dragRect">The boundary limit to keep the MenuElement within</param>
		 */
		public void SetDragOffset (Vector2 pos, Rect dragRect)
		{
			if (pos.x < dragRect.x)
			{
				pos.x = dragRect.x;
			}
			else if (pos.x > (dragRect.x + dragRect.width - relativeRect.width))
			{
				pos.x = dragRect.x + dragRect.width - relativeRect.width;
			}
			
			if (pos.y < dragRect.y)
			{
				pos.y = dragRect.y;
			}
			else if (pos.y > (dragRect.y + dragRect.height - relativeRect.height))
			{
				pos.y = dragRect.y + dragRect.height - relativeRect.height;
			}

			dragOffset = pos;
		}


		/**
		 * <summary>Gets the drag offset.</summary>
		 * <returns>The drag offset</returns>
		 */
		public Vector2 GetDragStart ()
		{
			return new Vector2 (-dragOffset.x, dragOffset.y);
		}


		/**
		 * Hides any elements that have zero slots.
		 */
		public void AutoSetVisibility ()
		{
			if (numSlots == 0)
			{
				IsVisible = false;
			}
			else
			{
				IsVisible = true;
			}
		}


		protected T LinkUIElement <T> (Canvas canvas) where T : Behaviour
		{
			if (canvas)
			{
				T field = Serializer.GetGameObjectComponent <T> (linkedUiID, canvas.gameObject);

				if (field == null)
				{
					ACDebug.LogWarning ("Cannot find " + typeof (T) + " for menu element " + title + " in Canvas " + canvas.name, canvas);
				}
				return field;
			}
			return null;
		}


		protected void UpdateUISelectable <T> (T field, UISelectableHideStyle uiSelectableHideStyle) where T : Selectable
		{
			if (Application.isPlaying && field)
			{
				if (uiSelectableHideStyle == UISelectableHideStyle.DisableObject)
				{
					field.gameObject.SetActive (IsVisible);
				}
				else if (uiSelectableHideStyle == UISelectableHideStyle.DisableInteractability)
				{
					field.interactable = IsVisible;
				}
			}
		}


		protected void UpdateUIElement <T> (T field) where T : Behaviour
		{
			if (Application.isPlaying && field && field.gameObject.activeSelf != IsVisible)
			{
				field.gameObject.SetActive (IsVisible);
			}
		}


		protected void ClearSpriteCache (UISlot[] uiSlots)
		{
			foreach (UISlot uiSlot in uiSlots)
			{
				uiSlot.sprite = null;
			}
		}


		/**
		 * <summary>Sets the interactive state of any linked Unity UI gameobjects.</summary>
		 * <param name = "state">If True, linked UI gameobjects will be made interactive. If False, they will be made non-interactive</param>
		 */
		public virtual void SetUIInteractableState (bool state)
		{}


		protected void SetUISlotsInteractableState (UISlot[] uiSlots, bool state)
		{
			foreach (UISlot uiSlot in uiSlots)
			{
				if (uiSlot.uiButton)
				{
					uiSlot.uiButton.interactable = state;
				}
			}
		}


		/**
		 * <summary>Gets the hover sound for the element slot.</summary>
		 * <param name = "slot">The slots index number</param>
		 * <returns>Tover sound for the element slot.</returns>
		 */
		public virtual AudioClip GetHoverSound (int slot)
		{
			return hoverSound;
		}


		/**
		 * <summary>Gets the amount by which the slots have been offset, if the number that can be shown exceeds the number that can be display at once.</summary>
		 * <returns>The amount by which the slots have been offset</returns>
		 */
		public int GetOffset ()
		{
			return offset;
		}


		/**
		 * <summary>Sets the amount by which the slots have been offset, if the number that can be shown exceeds the number that can be display at once.</summary>
		 * <param name = "value">The amount by which the slots have been offset</returns>
		 */
		public void SetOffset (int value)
		{
			offset = value;
			LimitOffset ();
		}


		/**
		 * <summary>Called whenever the Menu this is attached to is turned on.</summary>
		 * <param name = "menu">The Menu this is attached to</param>
		 */
		public virtual void OnMenuTurnOn (Menu menu)
		{
			parentMenu = menu;
		}


		/** Gets the data related to the element's visiblity as a serialized string */
		public string GetVisibilitySaveData ()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			sb.Append (ID.ToString ());
			sb.Append ("=");
			sb.Append (isVisible.ToString ());
			sb.Append ("+");
			return sb.ToString ();
		}


		public Menu ParentMenu
		{
			get
			{
				return parentMenu;
			}
		}

	}

}