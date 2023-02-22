/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"UISlot.cs"
 * 
 *	This is a class for Unity UI elements that contain both
 *	Image and Text components that must be linked to AC's Menu system.
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
	 * A data container that links a Unity UI Button to AC's own Menu system.
	 */
	[System.Serializable]
	public class UISlot
	{

		#region Variables

		/** The Unity UI Button this is linked to */
		public UnityEngine.UI.Button uiButton;
		/** The ConstantID number of the linked Unity UI Button */
		public int uiButtonID;
		/** The sprite to set in the Button's Image */
		public UnityEngine.Sprite sprite;
		
		#if TextMeshProIsPresent
		private TMPro.TextMeshProUGUI uiText;
		#else
		private Text uiText;
		#endif

		private Image uiImage;
		private RawImage uiRawImage;

		private Color originalNormalColour;
		private Color originalHighlightedColour;
		private UnityEngine.Sprite emptySprite;
		private Texture cacheTexture;
		private Sprite originalSprite;
		private bool canSetOriginalImage;

		#endregion


		#region Constructors

		/** The default Constructor. */
		public UISlot ()
		{
			uiButton = null;
			uiButtonID = 0;
			uiText = null;
			uiImage = null;
			uiRawImage = null;
			sprite = null;
		}


		/** A Constructor that gets its values by copying another */
		public UISlot (UISlot uiSlot)
		{
			uiButton = uiSlot.uiButton;
			uiButtonID = uiSlot.uiButtonID;
			sprite = uiSlot.sprite;
			uiImage = null;
			uiRawImage = null;
		}

		#endregion

		#if UNITY_EDITOR

		public void LinkedUiGUI (int i, MenuSource source)
		{
			uiButton = (UnityEngine.UI.Button) EditorGUILayout.ObjectField ("Linked Button (" + (i+1).ToString () + "):", uiButton, typeof (UnityEngine.UI.Button), true);

			if (Application.isPlaying && source == MenuSource.UnityUiPrefab)
			{}
			else
			{
				uiButtonID = Menu.FieldToID <UnityEngine.UI.Button> (uiButton, uiButtonID);
				uiButton = Menu.IDToField <UnityEngine.UI.Button> (uiButton, uiButtonID, source);
			}
		}

		#endif


		#region PublicFunctions

		/**
		 * <summary>Gets the boundary of the UI Button.</summary>
		 * <returns>The boundary Rect of the UI Button</returns>
		 */
		public RectTransform GetRectTransform ()
		{
			if (uiButton)
			{
				RectTransform rectTransform = uiButton.GetComponent <RectTransform>();
				if (rectTransform)
				{
					return rectTransform;
				}
			}
			return null;
		}


		/**
		 * <summary>Links the UI GameObjects to the class, based on the supplied uiButtonID.</summary>
		 * <param name = "canvas">The Canvas that contains the UI GameObjects</param>
		 * <param name = "linkUIGraphic">What Image component the Element's Graphics should be linked to (ImageComponent, ButtonTargetGraphic)</param>
		 * <param name = "emptySlotTexture">If set, the texture to use when a slot is considered empty</param>
		 */
		public void LinkUIElements (Canvas canvas, LinkUIGraphic linkUIGraphic, Texture2D emptySlotTexture = null)
		{
			if (canvas)
			{
				uiButton = Serializer.GetGameObjectComponent <UnityEngine.UI.Button> (uiButtonID, canvas.gameObject);
			}
			else
			{
				uiButton = null;
			}

			if (uiButton)
			{
				#if TextMeshProIsPresent
				uiText = uiButton.GetComponentInChildren <TMPro.TextMeshProUGUI>();
				#else
				uiText = uiButton.GetComponentInChildren <Text>();
				#endif
				uiRawImage = uiButton.GetComponentInChildren <RawImage>();

				if (linkUIGraphic == LinkUIGraphic.ImageComponent)
				{
					uiImage = uiButton.GetComponentInChildren <Image>();
				}
				else if (linkUIGraphic == LinkUIGraphic.ButtonTargetGraphic)
				{
					if (uiButton.targetGraphic)
					{
						if (uiButton.targetGraphic is Image)
						{
							uiImage = uiButton.targetGraphic as Image;
						}
						else
						{
							ACDebug.LogWarning ("Cannot assign UI Image for " + uiButton.name + "'s target graphic as " + uiButton.targetGraphic + " is not an Image component.", canvas);
						}
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign UI Image for " + uiButton.name + "'s target graphic because it has none.", canvas);
					}
				}

				originalNormalColour = uiButton.colors.normalColor;
				originalHighlightedColour = uiButton.colors.highlightedColor;
				originalSprite = (uiImage) ? uiImage.sprite : null;
			}

			if (emptySlotTexture)
			{
				emptySprite = Sprite.Create (emptySlotTexture, new Rect (0f, 0f, emptySlotTexture.width, emptySlotTexture.height), new Vector2 (0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
			}
		}


		/**
		 * <summary>Sets the text of the UI Button.</summary>
		 * <param name = "_text">The text to assign the Button</param>
		 */
		public void SetText (string _text)
		{
			if (uiText)
			{
				uiText.text = _text;
			}
		}


		/**
		 * <summary>Sets the image of the UI Button using a Texture.</summary>
		 * <param name = "_texture">The texture to assign the Button</param>
		 */
		public void SetImage (Texture _texture)
		{
			if (uiRawImage)
			{
				if (_texture == null)
				{
					_texture = EmptySprite.texture;
				}

				uiRawImage.texture = _texture;
			}
			else if (uiImage)
			{
				if (_texture == null)
				{
					sprite = EmptySprite;
				}
				else if (sprite == null || sprite == emptySprite || cacheTexture != _texture)
				{
					if (_texture is Texture2D)
					{
						Texture2D texture2D = (Texture2D) _texture;
						sprite = Sprite.Create (texture2D, new Rect (0f, 0f, texture2D.width, texture2D.height), new Vector2 (0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
					}
					else
					{
						ACDebug.LogWarning ("Cannot show texture " + _texture.name + " in UI Image " + uiImage.name + " because it is not a 2D Texture. Use a UI RawImage component instead.", uiImage);
					}
				}

				if (_texture)
				{
					cacheTexture = _texture;
				}
				uiImage.sprite = sprite;
			}
		}


		/**
		 * <summary>Sets the image of the UI Button using a Sprite.</summary>
		 * <param name = "_sprite">The sprite to assign the Button</param>
		 */
		public void SetImageAsSprite (Sprite _sprite)
		{
			if (uiImage)
			{
				if (_sprite == null)
				{
					sprite = EmptySprite;
				}
				else if (sprite == null || sprite == EmptySprite || sprite != _sprite)
				{
					sprite = _sprite;
				}

				uiImage.sprite = sprite;
			}
		}


		/**
		 * <summary>Enables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, ClearContent) </param>
		 */
		public void ShowUIElement (UIHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton && uiButton.gameObject)
			{
				if (uiHideStyle == UIHideStyle.DisableObject && !uiButton.gameObject.activeSelf)
				{
					uiButton.gameObject.SetActive (true);
				}
				else if (uiHideStyle == UIHideStyle.ClearContent)
				{
					if (originalSprite && canSetOriginalImage) SetImageAsSprite (originalSprite);
				}
			}
		}


		public void ShowUIElement (UISelectableHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton && uiButton.gameObject)
			{
				if (uiHideStyle == UISelectableHideStyle.DisableObject && !uiButton.gameObject.activeSelf)
				{
					uiButton.gameObject.SetActive (true);
				}
				else if (uiHideStyle == UISelectableHideStyle.DisableInteractability)
				{
					uiButton.interactable = true;
				}
			}
		}


		/**
		 * <summary>Disables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, ClearContent) </param>
		 * <returns>True if the object being hidden is the EventSystem's currently-selected object</returns>
		 */
		public bool HideUIElement (UIHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton && uiButton.gameObject && uiButton.gameObject.activeSelf)
			{
				bool isSelected = KickStarter.playerMenus.EventSystem.currentSelectedGameObject == uiButton.gameObject;
				if (uiHideStyle == UIHideStyle.DisableObject)
				{
					uiButton.gameObject.SetActive (false);
				}
				else if (uiHideStyle == UIHideStyle.ClearContent)
				{
					SetImage (null);
					SetText (string.Empty);
				}
				return isSelected;
			}
			return false;
		}


		/**
		 * <summary>Disables the visibility of the linked UI Button.</summary>
		 * <param name = "uiHideStyle">The method by which the UI element is hidden (DisableObject, DisableInteractability) </param>
		 * <returns>True if the object being hidden is the EventSystem's currently-selected object</returns>
		 */
		public bool HideUIElement (UISelectableHideStyle uiHideStyle)
		{
			if (Application.isPlaying && uiButton && uiButton.gameObject && uiButton.gameObject.activeSelf)
			{
				bool isSelected = KickStarter.playerMenus.EventSystem.currentSelectedGameObject == uiButton.gameObject;
				if (uiHideStyle == UISelectableHideStyle.DisableObject)
				{
					uiButton.gameObject.SetActive (false);
				}
				else if (uiHideStyle == UISelectableHideStyle.DisableInteractability)
				{
					uiButton.interactable = false;
				}
				return isSelected;
			}
			return false;
		}


		/**
		 * <summary>Adds a UISlotClick component to the Button, which acts as a click-handler.</summary>
		 * <param name = "_menu">The Menu that the Button is linked to</param>
		 * <param name = "_element">The MenuElement within _menu that the Button is linked to</param>
		 * <param name = "_slot">The index number of the slot within _element that the Button is linked to</param>
		 */
		public void AddClickHandler (AC.Menu _menu, MenuElement _element, int _slot)
		{
			UISlotClickRight uiSlotRightClick = uiButton.gameObject.GetComponent <UISlotClickRight>();
			if (uiSlotRightClick == null)
			{
				UISlotClick uiSlotClick = uiButton.GetComponent <UISlotClick>();
				if (uiSlotClick)
				{
					Object.Destroy (uiSlotClick);
				}

				uiSlotRightClick = uiButton.gameObject.AddComponent <UISlotClickRight> ();
				uiSlotRightClick.Setup (_menu, _element, _slot);
			}
		}


		/**
		 * <summary>Changes the colours of the linked UI Button.</summary>
		 * <param name = "newNormalColour">The new 'normal' colour to set</param>
		 * * <param name = "newHighlightedColour">The new 'highlighted' colour to set</param>
		 */
		public void SetColours (Color newNormalColour, Color newHighlightedColour)
		{
			if (uiButton)
			{
				ColorBlock colorBlock = uiButton.colors;
				colorBlock.normalColor = newNormalColour;
				colorBlock.highlightedColor = newHighlightedColour;
				uiButton.colors = colorBlock;
			}
		}


		/**
		 * <summary>Reverts the 'normal' colour of the linked UI Button, if it was changed using SetColour.</summary>
		 */
		public void RestoreColour ()
		{
			if (uiButton)
			{
				ColorBlock colorBlock = uiButton.colors;
				colorBlock.normalColor = originalNormalColour;
				colorBlock.highlightedColor = originalHighlightedColour;
				uiButton.colors = colorBlock;
			}
		}

		#endregion


		#region GetSet

		/** Checks if the associated UI components can set a Hotspot label when selected */
		public bool CanOverrideHotspotLabel
		{
			get
			{
				if (uiButton)
				{
					return uiButton.interactable;
				}
				return true;
			}
		}


		private Sprite EmptySprite
		{
			get
			{
				if (emptySprite == null)
				{
					emptySprite = Resource.EmptySlot;
				}
				return emptySprite;
			}
		}


		public bool CanSetOriginalImage
		{
			get
			{
				return canSetOriginalImage;
			}
			set
			{
				canSetOriginalImage = value;
			}
		}

		#endregion

	}

}