/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuDrag.cs"
 * 
 *	This MenuElement can be used to drag around its parent Menu.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;	
#endif

namespace AC
{

	/**
	 * A MenuElement that can be used to drag around another element, or its parent Menu.
	 * This element type cannot be used in Unity UI-based Menus, because Unity UI has its own classes that perform the same functionality.
	 */
	[System.Serializable]
	public class MenuDrag : MenuElement, ITranslatable
	{

		/** The text that's displayed on-screen */
		public string label = "Element";
		/** The text alignment */
		public TextAnchor anchor;
		/** The special FX applied to the text (None, Outline, Shadow, OutlineAndShadow) */
		public TextEffects textEffects;
		/** The boundary that the draggable Menu or MenuElement can be moved within */
		public Rect dragRect;
		/** What the MenuDrag can be used to move (EntireMenu, SingleElement) */
		public DragElementType dragType = DragElementType.EntireMenu;
		/** The name of the MenuElement that can be dragged, if dragType = DragElementType.SingleElement */
		public string elementName;

		private Vector2 dragStartPosition;
		private AC.Menu menuToDrag;
		private MenuElement elementToDrag;
		private string fullText;


		public override void Declare ()
		{
			label = "Button";
			isVisible = true;
			isClickable = true;
			textEffects = TextEffects.None;
			numSlots = 1;
			anchor = TextAnchor.MiddleCenter;
			SetSize (new Vector2 (10f, 5f));
			dragRect = new Rect (0,0,0,0);
			dragType = DragElementType.EntireMenu;
			elementName = "";

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuDrag newElement = CreateInstance <MenuDrag>();
			newElement.Declare ();
			newElement.CopyDrag (this);
			return newElement;
		}
		
		
		private void CopyDrag (MenuDrag _element)
		{
			label = _element.label;
			anchor = _element.anchor;
			textEffects = _element.textEffects;
			dragRect = _element.dragRect;
			dragType = _element.dragType;
			elementName = _element.elementName;

			base.Copy (_element);
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuDrag)";

			MenuSource source = menu.menuSource;
			if (source != MenuSource.AdventureCreator)
			{
				EditorGUILayout.HelpBox ("This Element type is not necessary in Unity's UI, as it can be recreated using ScrollBars and ScrollRects.", MessageType.Info);
				return;
			}

			CustomGUILayout.BeginVertical ();
			label = CustomGUILayout.TextField ("Button text:", label, apiPrefix + ".label", "The text that's displayed on-screen");
			anchor = (TextAnchor) CustomGUILayout.EnumPopup ("Text alignment:", anchor, apiPrefix + ".anchor", "The text alignment");
			textEffects = (TextEffects) CustomGUILayout.EnumPopup ("Text effect:", textEffects, apiPrefix + ".textEffects", "The special FX applied to the text");

			dragType = (DragElementType) CustomGUILayout.EnumPopup ("Drag type:", dragType, apiPrefix + ".dragType", "What the MenuDrag can be used to move");
			if (dragType == DragElementType.SingleElement)
			{
				elementName = CustomGUILayout.TextField ("Element name:", elementName, apiPrefix + ".elementName", "The name of the element (within the same menu) that can be dragged");
			}

			dragRect = EditorGUILayout.RectField (new GUIContent ("Drag boundary:", "The boundary that the " + dragType.ToString ().ToLower () + " can be moved within"), dragRect);

			ChangeCursorGUI (menu);

			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}


		public override void DrawOutline (bool isSelected, AC.Menu _menu)
		{
			if (dragType == DragElementType.EntireMenu)
			{
				DrawStraightLine.DrawBox (_menu.GetRectAbsolute (GetDragRectRelative ()), Color.white, 1f, false, 1);
			}
			else
			{
				if (!string.IsNullOrEmpty (elementName))
				{
					MenuElement element = MenuManager.GetElementWithName (_menu.title, elementName);
					if (element != null)
					{
						Rect dragBox = _menu.GetRectAbsolute (GetDragRectRelative ());
						dragBox.x += element.GetSlotRectRelative (0).x;
						dragBox.y += element.GetSlotRectRelative (0).y;
						DrawStraightLine.DrawBox (dragBox, Color.white, 1f, false, 1);
					}
				}
			}
			
			base.DrawOutline (isSelected, _menu);
		}

#endif


		protected override string GetLabelToTranslate ()
		{
			return label;
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			fullText = TranslateLabel (languageNumber);
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);
			
			_style.alignment = anchor;
			if (zoom < 1f)
			{
				_style.fontSize = (int) ((float) _style.fontSize * zoom);
			}
			
			if (textEffects != TextEffects.None)
			{
				AdvGame.DrawTextEffect (ZoomRect (relativeRect, zoom), fullText, _style, Color.black, _style.normal.textColor, 2, textEffects);
			}
			else
			{
				GUI.Label (ZoomRect (relativeRect, zoom), fullText, _style);
			}
		}
		

		public override string GetLabel (int slot, int languageNumber)
		{
			return TranslateLabel (languageNumber);
		}
		
		
		protected override void AutoSize ()
		{
			if (string.IsNullOrEmpty (label) && backgroundTexture)
			{
				GUIContent content = new GUIContent (backgroundTexture);
				AutoSize (content);
			}
			else
			{
				GUIContent content = new GUIContent (TranslateLabel (Options.GetLanguage ()));
				AutoSize (content);
			}
		}


		private void StartDrag (AC.Menu _menu)
		{
			menuToDrag = _menu;

			if (dragType == DragElementType.SingleElement)
			{
				if (elementName != "")
				{
					MenuElement element = PlayerMenus.GetElementWithName (_menu.title, elementName);
					if (element == null)
					{
						ACDebug.LogWarning ("Cannot drag " + elementName + " as it cannot be found on " + _menu.title);
					}
					else if (element.positionType == AC_PositionType2.Aligned)
					{
						ACDebug.LogWarning ("Cannot drag " + elementName + " as its Position is set to Aligned");
					}
					else if (_menu.sizeType == AC_SizeType.Automatic)
					{
						ACDebug.LogWarning ("Cannot drag " + elementName + " as its parent Menu's Size is set to Automatic");
					}
					else
					{
						elementToDrag = element;
						dragStartPosition = elementToDrag.GetDragStart ();
					}
				}
			}
			else
			{
				dragStartPosition = _menu.GetDragStart ();
			}
		}


		/**
		 * <summary>Performs the drag.</summary>
		 * <param name = "_dragVector">The amount and direction to drag by</param>
		 * <returns>True if the drag effect was successful</returns>
		 */
		public bool DoDrag (Vector2 _dragVector)
		{
			if (dragType == DragElementType.EntireMenu)
			{
				if (menuToDrag == null)
				{
					return false;
				}
				
				if (!menuToDrag.IsEnabled () || menuToDrag.IsFading ())
				{
					return false;
				}
			}
			
			if (elementToDrag == null && dragType == DragElementType.SingleElement)
			{
				return false;
			}
			
			// Convert dragRect to screen co-ordinates
			Rect dragRectAbsolute = dragRect;
			if (sizeType != AC_SizeType.AbsolutePixels)
			{
				Vector2 sizeFactor = KickStarter.mainCamera.GetPlayableScreenArea (false).size;
				dragRectAbsolute = new Rect (dragRect.x * sizeFactor.x / 100f,
											 dragRect.y * sizeFactor.y / 100f,
											 dragRect.width * sizeFactor.x / 100f,
											 dragRect.height * sizeFactor.y / 100f);
			}
			
			if (dragType == DragElementType.EntireMenu)
			{
				menuToDrag.SetDragOffset (_dragVector + dragStartPosition, dragRectAbsolute);
			}
			else if (dragType == AC.DragElementType.SingleElement)
			{
				elementToDrag.SetDragOffset (_dragVector + dragStartPosition, dragRectAbsolute);
			}
			
			return true;
		}


		/**
		 * <summary>Checks if the dragging should be cancelled.</summary>
		 * <param name = "mousePosition">The position of the mouse cursor</param>
		 * <returns>True if the dragging should be cancelled</returns>
		 */
		public bool CheckStop (Vector2 mousePosition)
		{
			if (menuToDrag == null)
			{
				return false;
			}
			if (dragType == DragElementType.EntireMenu && !menuToDrag.IsPointerOverSlot (this, 0, mousePosition))
			{
				return true;
			}
			if (dragType == DragElementType.SingleElement && elementToDrag != null && !menuToDrag.IsPointerOverSlot (this, 0, mousePosition))
			{
				return true;
			}
			return false;
		}


		private Rect GetDragRectRelative ()
		{
			Rect positionRect = dragRect;

			if (sizeType != AC_SizeType.AbsolutePixels)
			{
				positionRect.x = dragRect.x / 100f * KickStarter.mainCamera.GetPlayableScreenArea (false).width;
				positionRect.y = dragRect.y / 100f * KickStarter.mainCamera.GetPlayableScreenArea (false).height;

				positionRect.width = dragRect.width / 100f * KickStarter.mainCamera.GetPlayableScreenArea (false).width;
				positionRect.height = dragRect.height / 100f * KickStarter.mainCamera.GetPlayableScreenArea (false).height;
			}

			return (positionRect);
		}


		public override bool ProcessClick (AC.Menu _menu, int _slot, MouseState _mouseState)
		{
			if (_mouseState == MouseState.SingleClick)
			{
				StartDrag (_menu);
				KickStarter.playerInput.SetActiveDragElement (this);
				base.ProcessClick (_menu, _slot, _mouseState);
				return true;
			}

			return false;
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