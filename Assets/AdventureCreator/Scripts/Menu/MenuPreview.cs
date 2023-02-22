using UnityEngine;

namespace AC
{

	/**
	 * Allows Menus (see: Menu) made in MenuManager to be shown in the Game Window when the game is not running, allowing for real-time previews as they are built.
	 */
	[ExecuteInEditMode]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_menu_preview.html")]
	public class MenuPreview : MonoBehaviour
	{

		#if UNITY_EDITOR

		private Menu previewSpeechMenu;
		private int previewTrackID;

		private MenuManager menuManager;
		private GUIStyle normalStyle;
		private GUIStyle highlightedStyle;
		private Vector2 screenSize;


		private void SetStyles (MenuElement element)
		{
			normalStyle = new GUIStyle();
			normalStyle.normal.textColor = element.fontColor;
			normalStyle.font = element.font;
			normalStyle.fontSize = element.GetFontSize ();
			normalStyle.alignment = TextAnchor.MiddleCenter;

			highlightedStyle = new GUIStyle();
			highlightedStyle.font = element.font;
			highlightedStyle.fontSize = element.GetFontSize ();
			highlightedStyle.normal.textColor = element.fontHighlightColor;
			highlightedStyle.normal.background = element.highlightTexture;
			highlightedStyle.alignment = TextAnchor.MiddleCenter;
		}

		
		private void Update ()
		{
			if (!Application.isPlaying)
			{
				if (AdvGame.GetReferences ())
				{
					menuManager = AdvGame.GetReferences ().menuManager;
					
					if (menuManager)
					{
						if (previewSpeechMenu != null)
						{
							UpdatePreviewMenu (previewSpeechMenu, true);
						}
						if (menuManager.GetSelectedMenu () != null)
						{
							Menu menu = menuManager.GetSelectedMenu ();
							UpdatePreviewMenu (menu);
						}
					}
				}
			}
		}
		
		
		private void OnGUI ()
		{
			if (!Application.isPlaying)
			{
				if (AdvGame.GetReferences ())
				{
					menuManager = AdvGame.GetReferences ().menuManager;

					if (menuManager && menuManager.drawInEditor && KickStarter.mainCamera)
					{
						if (previewSpeechMenu != null)
						{
							DrawPreviewMenu (previewSpeechMenu);
						}

						if (menuManager.GetSelectedMenu () != null && AdvGame.GetReferences ().viewingMenuManager)
						{
							Menu menu = menuManager.GetSelectedMenu ();
							DrawPreviewMenu (menu);
						}
					}
				}
			}
		}


		private void UpdatePreviewMenu (Menu menu, bool isSubtitlesPreview = false)
		{
			if (menu == null || menu.IsUnityUI ())
			{
				return;
			}

			foreach (MenuElement element in menu.visibleElements)
			{
				if (element == null) continue;

				for (int i=0; i<element.GetNumSlots (); i++)
				{
					if (menuManager.GetSelectedElement () == element && element.isClickable && i == 0)
					{
						element.PreDisplay (i, 0, false);
					}
					else
					{
						element.PreDisplay (i, 0, false);
					}

					if (isSubtitlesPreview && element is MenuLabel)
					{
						MenuLabel menuLabel = element as MenuLabel;
						menuLabel.UpdateLabelText ();
					}
				}
			}
		}


		private void DrawPreviewMenu (Menu menu)
		{
			if (menu == null || menu.IsUnityUI ())
			{
				return;
			}

			if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
			{
				// Required to get MouseDown event
				UnityEditor.EditorUtility.SetDirty (menu);
			}
			
			if (KickStarter.mainCamera)
			{
				KickStarter.mainCamera.DrawBorders ();
			}

			CheckScreenSize (menu);

			if (menu.CanPause () && menu.pauseWhenEnabled && menuManager.pauseTexture)
			{
				if (KickStarter.mainCamera)
				{
					GUI.DrawTexture (KickStarter.mainCamera.GetPlayableScreenArea (false), menuManager.pauseTexture);
				}
				else
				{
					GUI.DrawTexture (new Rect (0, 0, ACScreen.width, ACScreen.height), menuManager.pauseTexture);
				}
			}
			
			if ((menu.positionType == AC_PositionType.FollowCursor || menu.positionType == AC_PositionType.AppearAtCursorAndFreeze || menu.positionType == AC_PositionType.OnHotspot || menu.positionType == AC_PositionType.AboveSpeakingCharacter || menu.positionType == AC_PositionType.AbovePlayer) && AdvGame.GetReferences ().cursorManager && AdvGame.GetReferences ().cursorManager.pointerIcon.texture)
			{
				CursorIconBase icon = AdvGame.GetReferences ().cursorManager.pointerIcon;
				GUI.DrawTexture (AdvGame.GUIBox (KickStarter.mainCamera.GetPlayableScreenArea (false).center, icon.size), icon.texture, ScaleMode.ScaleToFit, true, 0f);
			}
			
			menu.StartDisplay ();
							
			foreach (MenuElement element in menu.visibleElements)
			{
				SetStyles (element);
				
				for (int i=0; i<element.GetNumSlots (); i++)
				{
					if (menuManager.GetSelectedElement () == element && element.isClickable && i == 0)
					{
						element.Display (highlightedStyle, i, 1f, true);
					}
					
					else
					{
						element.Display (normalStyle, i, 1f, false);
					}
				}

				if (Event.current.type == EventType.MouseDown && UnityEditor.EditorWindow.mouseOverWindow != null && UnityEditor.EditorWindow.mouseOverWindow.ToString () != null && UnityEditor.EditorWindow.mouseOverWindow.ToString ().Contains ("(UnityEditor.GameView)"))
				{
					if (menu.IsPointerOverSlot (element, 0, Event.current.mousePosition + new Vector2 (menu.GetRect ().x, menu.GetRect ().y)))
					{
						menuManager.SelectElementFromPreview (menu, element);
					}
				}
			}
	
			menu.EndDisplay ();
			
			if (menuManager.drawOutlines)
			{
				menu.DrawOutline (menuManager.GetSelectedElement ());
			}
		}


		private void CheckScreenSize (Menu menu)
		{
			menu.Recalculate ();

			if (!Mathf.Approximately (screenSize.x, ACScreen.width) || !Mathf.Approximately (screenSize.y, ACScreen.height))			
			{
				screenSize = new Vector2 ( ACScreen.width, ACScreen.height);
				menu.Recalculate ();
			}
		}


		/**
		 * <summary>Begins the preview of a subtitle</summary>
		 * <param name = "speech">The Speech to preview</param>
		 * <param name = "trackInstanceID">The Instance ID of the TrackAsset that is playing the speech</param>
		 */
		public void SetPreviewSpeech (Speech speech, int trackInstanceID)
		{
			if (speech != null && KickStarter.menuManager != null && KickStarter.speechManager)
			{
				if (previewSpeechMenu == null)
				{
					previewSpeechMenu = KickStarter.menuManager.CreatePreviewMenu (KickStarter.speechManager.previewMenuName);
				}

				if (previewSpeechMenu != null)
				{
					if (trackInstanceID != previewTrackID)
					{
						RemovePreviewSpeechMenu ();
						previewSpeechMenu = KickStarter.menuManager.CreatePreviewMenu (KickStarter.speechManager.previewMenuName);
					}

					if (previewSpeechMenu != null)
					{
						previewSpeechMenu.SetSpeech (speech);
						previewTrackID = trackInstanceID;
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot preview Timeline speech track because a suitable preview menu has not been named in the Speech Manager's 'Subtitle preview menu' field.");
				}
			}
		}


		/**
		 * <summary>Ends the preview of a subtitle</summary>
		 * <param name = "trackInstanceID">The Instance ID of the TrackAsset that is playing the speech</param>
		 */
		public void ClearPreviewSpeech (int trackInstanceID)
		{
			if (previewTrackID == trackInstanceID)
			{
				RemovePreviewSpeechMenu ();
			}
		}


		private void RemovePreviewSpeechMenu ()
		{
			if (previewSpeechMenu != null)
			{
				DestroyImmediate (previewSpeechMenu);
				previewSpeechMenu = null;
			}
		}

		#endif

	}

}