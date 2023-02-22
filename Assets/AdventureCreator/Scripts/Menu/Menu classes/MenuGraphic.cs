#if UNITY_STANDALONE && !UNITY_2018_2_OR_NEWER
#define ALLOW_MOVIETEXTURES
#endif

/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuGraphic.cs"
 * 
 *	This MenuElement provides a space for
 *	animated and static textures
 * 
 */

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A MenuElement that provides a space for animated or still images. */
	public class MenuGraphic : MenuElement
	{

		/** The Unity UI Image this is linked to (Unity UI Menus only) */
		public Image uiImage;
		/** The type of graphic that is shown (Normal, DialoguePortrait, DocumentTexture, ObjectiveTexture) */
		public AC_GraphicType graphicType = AC_GraphicType.Normal;
		/** The CursorIconBase that stores the graphic and animation data */
		public CursorIconBase graphic;

		public RawImage uiRawImage;
		[SerializeField] private UIImageType uiImageType = UIImageType.Image;
		private enum UIImageType { Image, RawImage };

		private Texture localTexture;
		private AC.Char portraitCharacterOverride;

		private Rect speechRect = new Rect ();
		private Sprite sprite;
		private Speech speech;
		private CursorIconBase portrait;
		private bool isDuppingSpeech;


		public override void Declare ()
		{
			uiImage = null;
			uiRawImage = null;
			
			graphicType = AC_GraphicType.Normal;
			isVisible = true;
			isClickable = false;
			graphic = new CursorIconBase ();
			numSlots = 1;
			SetSize (new Vector2 (10f, 5f));
			
			base.Declare ();
		}
		

		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuGraphic newElement = CreateInstance <MenuGraphic>();
			newElement.Declare ();
			newElement.CopyGraphic (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyGraphic (MenuGraphic _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiImage = null;
			}
			else
			{
				uiImage = _element.uiImage;
			}
			uiRawImage = _element.uiRawImage;
			uiImageType = _element.uiImageType;

			graphicType = _element.graphicType;
			graphic = new CursorIconBase ();
			graphic.Copy (_element.graphic);
			base.Copy (_element);
		}
		

		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			if (uiImageType == UIImageType.Image)
			{
				uiImage = LinkUIElement <Image> (canvas);
			}
			else if (uiImageType == UIImageType.RawImage)
			{
				uiRawImage = LinkUIElement <RawImage> (canvas);
			}
		}
		

		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiImageType == UIImageType.Image && uiImage)
			{
				return uiImage.rectTransform;
			}
			else if (uiImageType == UIImageType.RawImage && uiRawImage)
			{
				return uiRawImage.rectTransform;
			}
			return null;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuGraphic)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();
			
			if (source != MenuSource.AdventureCreator)
			{
				uiImageType = (UIImageType) EditorGUILayout.EnumPopup (new GUIContent ("UI image type:", "The type of UI component to link to"), uiImageType);
				if (uiImageType == UIImageType.Image)
				{
					uiImage = LinkedUiGUI <Image> (uiImage, "Linked Image:", source);
				}
				else if (uiImageType == UIImageType.RawImage)
				{
					uiRawImage = LinkedUiGUI <RawImage> (uiRawImage, "Linked Raw Image:", source);
				}
				CustomGUILayout.EndVertical ();
				CustomGUILayout.BeginVertical ();
			}
			
			graphicType = (AC_GraphicType) CustomGUILayout.EnumPopup ("Graphic type:", graphicType, apiPrefix + ".graphicType", "The type of graphic that is shown");
			if (graphicType == AC_GraphicType.Normal)
			{
				graphic.ShowGUI (false, false, "Texture:", CursorRendering.Software, apiPrefix + ".graphic", "The texture to display");
			}
			CustomGUILayout.EndVertical ();
			
			base.ShowGUI (menu);
		}

		#endif

		
		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiImageType == UIImageType.Image && uiImage && uiImage.gameObject == gameObject) return true;
			if (uiImageType == UIImageType.RawImage && uiRawImage && uiRawImage.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (uiImageType == UIImageType.Image && uiImage && uiImage.gameObject == gameObject)
			{
				return 0;
			}
			if (uiImageType == UIImageType.RawImage && uiRawImage && uiRawImage.gameObject == gameObject)
			{
				return 0;
			}
			return base.GetSlotIndex (gameObject);
		}


		/**
		 * <summary>Updates the element's texture, provided that its graphicType = AC_GraphicType.Normal</summary>
		 * <param name = "newTexture">The new texture to assign the element</param>
		 */
		public void SetNormalGraphicTexture (Texture newTexture)
		{
			if (graphicType == AC_GraphicType.Normal)
			{
				graphic.texture = newTexture;
				graphic.ClearCache ();
			}
		}


		private void UpdateSpeechLink ()
		{
			if (!isDuppingSpeech && KickStarter.dialog.GetLatestSpeech () != null)
			{
				speech = KickStarter.dialog.GetLatestSpeech ();

				if (parentMenu != null && !speech.MenuCanShow (parentMenu))
				{
					speech = null;
				}
			}
		}
		

		public override void SetSpeech (Speech _speech)
		{
			isDuppingSpeech = true;
			speech = _speech;
		}
		

		public override void ClearSpeech ()
		{
			if (graphicType == AC_GraphicType.DialoguePortrait)
			{
				localTexture = null;
			}
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			base.OnMenuTurnOn (menu);

			PreDisplay (0, Options.GetLanguage (), false);

			#if ALLOW_MOVIETEXTURE
			if (localTexture != null)
			{
				MovieTexture movieTexture = localTexture as MovieTexture;
				if (movieTexture != null)
				{
					movieTexture.Play ();
				}
			}
			#endif
		}


		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			switch (graphicType)
			{
				case AC_GraphicType.DialoguePortrait:
					if (portraitCharacterOverride)
					{
						portrait = portraitCharacterOverride.GetPortrait ();
						localTexture = portrait.texture;
					}
					else
					{
						UpdateSpeechLink ();
						if (speech != null && speech.GetSpeakingCharacter ())
						{
							portrait = speech.GetSpeakingCharacter ().GetPortrait ();
							localTexture = portrait.texture;
						}
					}
					break;

				case AC_GraphicType.DocumentTexture:
					if (Application.isPlaying && KickStarter.runtimeDocuments.ActiveDocument != null)
					{
						if (localTexture != KickStarter.runtimeDocuments.ActiveDocument.texture)
						{
							if (KickStarter.runtimeDocuments.ActiveDocument.texture)
							{
								Texture2D docTex = KickStarter.runtimeDocuments.ActiveDocument.texture;
								sprite = Sprite.Create (docTex, new Rect (0f, 0f, docTex.width, docTex.height), new Vector2 (0.5f, 0.5f));
							}
							else
							{
								sprite = null;
							}
						}
						localTexture = KickStarter.runtimeDocuments.ActiveDocument.texture;
					}
					break;

				case AC_GraphicType.ObjectiveTexture:
					if (Application.isPlaying && KickStarter.runtimeObjectives.SelectedObjective != null)
					{
						if (localTexture != KickStarter.runtimeObjectives.SelectedObjective.Objective.texture && KickStarter.runtimeObjectives.SelectedObjective.Objective.texture)
						{
							Texture2D objTex = KickStarter.runtimeObjectives.SelectedObjective.Objective.texture;
							sprite = UnityEngine.Sprite.Create (objTex, new Rect (0f, 0f, objTex.width, objTex.height), new Vector2 (0.5f, 0.5f));
						}
						localTexture = KickStarter.runtimeObjectives.SelectedObjective.Objective.texture;
					}
					break;

				default:
					break;
			}

			SetUIGraphic ();
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			base.Display (_style, _slot, zoom, isActive);

			switch (graphicType)
			{
				case AC_GraphicType.Normal:
					if (graphic != null)
					{
						graphic.DrawAsInteraction (ZoomRect (relativeRect, zoom), true);
					}
					break;

				case AC_GraphicType.DialoguePortrait:
					if (localTexture)
					{
						if (portrait.isAnimated)
						{
							Char character = portraitCharacterOverride;
							if (character == null && speech != null && speech.speaker)
							{
								character = speech.speaker;
							}

							if (character)
							{
								if (character.isLipSyncing)
								{
									speechRect = portrait.GetAnimatedRect (character.GetLipSyncFrame ());
								}
								else if (character.isTalking)
								{
									speechRect = portrait.GetAnimatedRect ();
								}
								else
								{
									speechRect = portrait.GetAnimatedRect (0);
								}

								GUI.DrawTextureWithTexCoords (ZoomRect (relativeRect, zoom), localTexture, speechRect);
							}
						}
						else
						{
							GUI.DrawTexture (ZoomRect (relativeRect, zoom), localTexture, ScaleMode.StretchToFill, true, 0f);
						}
					}
					break;

				case AC_GraphicType.DocumentTexture:
				case AC_GraphicType.ObjectiveTexture:
					if (localTexture)
					{
						GUI.DrawTexture (ZoomRect (relativeRect, zoom), localTexture, ScaleMode.StretchToFill, true, 0f);
					}
					break;
			}
		}
		

		public override void RecalculateSize (MenuSource source)
		{
			graphic.Reset ();
			SetUIGraphic ();
			base.RecalculateSize (source);
		}


		private void SetUIGraphic ()
		{
			if (!Application.isPlaying) return;

			if (uiImageType == UIImageType.Image && uiImage)
			{
				switch (graphicType)
				{
					case AC_GraphicType.Normal:
						uiImage.sprite = graphic.GetAnimatedSprite (true);
						break;

					case AC_GraphicType.DialoguePortrait:
						if (speech != null && portraitCharacterOverride == null)
						{
							uiImage.sprite = speech.GetPortraitSprite ();
						}
						else if (portraitCharacterOverride != null)
						{
							uiImage.sprite = portraitCharacterOverride.GetPortraitSprite ();
						}
						break;

					case AC_GraphicType.DocumentTexture:
					case AC_GraphicType.ObjectiveTexture:
						uiImage.sprite = sprite;
						break;

					default:
						break;
				}
				UpdateUIElement (uiImage);
			}
			if (uiImageType == UIImageType.RawImage && uiRawImage)
			{
				switch (graphicType)
				{
					case AC_GraphicType.Normal:
						if (graphic.texture && graphic.texture is RenderTexture)
						{
							uiRawImage.texture = graphic.texture;
						}
						else
						{
							uiRawImage.texture = graphic.GetAnimatedTexture (true);
						}
						break;

					case AC_GraphicType.DocumentTexture:
					case AC_GraphicType.ObjectiveTexture:
						uiRawImage.texture = localTexture;
						break;

					case AC_GraphicType.DialoguePortrait:
						if (speech != null)
						{
							uiRawImage.texture = speech.GetPortrait ();
						}
						break;
				}
				UpdateUIElement (uiRawImage);
			}
		}
		
		
		protected override void AutoSize ()
		{
			if (graphicType == AC_GraphicType.Normal && graphic.texture)
			{
				GUIContent content = new GUIContent (graphic.texture);
				AutoSize (content);
			}
		}


		public AC.Char PortraitCharacterOverride
		{
			set
			{
				portraitCharacterOverride = value;
			}
		}
		
	}
	
}