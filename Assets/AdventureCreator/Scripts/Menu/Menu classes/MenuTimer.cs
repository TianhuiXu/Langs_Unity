/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MenuTimer.cs"
 * 
 *	This MenuElement can be used in conjunction with MenuDialogList to create
 *	timed conversations, "Walking Dead"-style.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A MenuElement that provides a "countdown" timer that can either show the time remaining to choose a Conversation's dialogue option or complete a QTE, or the progress made in the current QTE. */
	public class MenuTimer : MenuElement
	{

		/** The Unity UI Slider this is linked to (Unity UI Menus only) */
		public Slider uiSlider;
		/** If True, then the value will be inverted, and the timer will move in the opposite direction */
		public bool doInvert;
		/** The texture of the slider bar (OnGUI Menus only) */
		public Texture2D timerTexture;
		/** What the value of the timer represents (Conversation, QuickTimeEventProgress, QuickTimeEventRemaining) */
		public AC_TimerType timerType = AC_TimerType.Conversation;
		/** The ID of the Timer to represent, if timerType = AC_TimerType.Timer */
		public int timerID;
		/** The method by which this element is hidden from view when made invisible (DisableObject, DisableInteractability) */
		public UISelectableHideStyle uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
		/** The amount of smoothing to apply (disabled if <= 0) */
		public float smoothingFactor = 0f;
		/** If True, and timerType = AC_TimerType.Conversation, then the Timer will be hidden if the current Conversation is not timed */
		public bool autoSetVisibility = false;

		private Timer timer;
		private LerpUtils.FloatLerp progressSmoothing = new LerpUtils.FloatLerp ();
		private float progress;
		private Rect timerRect;


		public override void Declare ()
		{
			uiSlider = null;
			doInvert = false;
			isVisible = true;
			isClickable = false;
			timerType = AC_TimerType.Conversation;
			numSlots = 1;
			SetSize (new Vector2 (20f, 5f));
			uiSelectableHideStyle = UISelectableHideStyle.DisableObject;
			smoothingFactor = 0f;
			autoSetVisibility = false;
			timerID = 0;

			base.Declare ();
		}


		public override MenuElement DuplicateSelf (bool fromEditor, bool ignoreUnityUI)
		{
			MenuTimer newElement = CreateInstance <MenuTimer>();
			newElement.Declare ();
			newElement.CopyTimer (this, ignoreUnityUI);
			return newElement;
		}
		
		
		private void CopyTimer (MenuTimer _element, bool ignoreUnityUI)
		{
			if (ignoreUnityUI)
			{
				uiSlider = null;
			}
			else
			{
				uiSlider = _element.uiSlider;
			}

			doInvert = _element.doInvert;
			timerTexture = _element.timerTexture;
			timerType = _element.timerType;
			uiSelectableHideStyle = _element.uiSelectableHideStyle;
			smoothingFactor = _element.smoothingFactor;
			autoSetVisibility = _element.autoSetVisibility;
			timerID = _element.timerID;

			base.Copy (_element);
		}


		public override void LoadUnityUI (AC.Menu _menu, Canvas canvas, bool addEventListeners = true)
		{
			uiSlider = LinkUIElement <Slider> (canvas);
			if (uiSlider)
			{
				uiSlider.minValue = 0f;
				uiSlider.maxValue = 1f;
				uiSlider.wholeNumbers = false;
				uiSlider.value = 1f;
				uiSlider.interactable = false;
			}
		}


		public override RectTransform GetRectTransform (int _slot)
		{
			if (uiSlider)
			{
				return uiSlider.GetComponent <RectTransform>();
			}
			return null;
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (Menu menu)
		{
			string apiPrefix = "(AC.PlayerMenus.GetElementWithName (\"" + menu.title + "\", \"" + title + "\") as AC.MenuTimer)";

			MenuSource source = menu.menuSource;
			CustomGUILayout.BeginVertical ();

			timerType = (AC_TimerType) CustomGUILayout.EnumPopup ("Timer type:", timerType, apiPrefix + ".timerType", "What the value of the timer represents");
			if (timerType == AC_TimerType.LoadingProgress && AdvGame.GetReferences ().settingsManager && !AdvGame.GetReferences ().settingsManager.useAsyncLoading)
			{
				EditorGUILayout.HelpBox ("Loading progress cannot be displayed unless asynchonised loading is enabled within the Settings Manager.", MessageType.Warning);
			}
			else if (timerType == AC_TimerType.Conversation)
			{
				autoSetVisibility = CustomGUILayout.Toggle ("Auto-set visibility?", autoSetVisibility, apiPrefix + ".autoSetVisibility", "If True, the Timer will be hidden if the active Conversation is not timed");
			}
			else if (timerType == AC_TimerType.Timer)
			{
				if (GUILayout.Button ("Timers window"))
				{
					TimersEditor.Init ();
				}

				if (KickStarter.variablesManager != null && KickStarter.variablesManager.timers != null && KickStarter.variablesManager.timers.Count > 0)
				{
					int tempNumber = -1;
					string[] labelList = new string[KickStarter.variablesManager.timers.Count];
					for (int i = 0; i < KickStarter.variablesManager.timers.Count; i++)
					{
						labelList[i] = i.ToString () + ": " + KickStarter.variablesManager.timers[i].Label;

						if (KickStarter.variablesManager.timers[i].ID == timerID)
						{
							tempNumber = i;
						}
					}

					if (tempNumber == -1)
					{
						// Wasn't found (was deleted?), so revert to zero
						tempNumber = 0;
						timerID = 0;
					}

					tempNumber = EditorGUILayout.Popup ("Timer:", tempNumber, labelList);
					timerID = KickStarter.variablesManager.timers[tempNumber].ID;
				}
				else
				{
					EditorGUILayout.HelpBox ("No Timers exist!", MessageType.Warning);
				}
			}
			doInvert = CustomGUILayout.Toggle ("Invert value?", doInvert, apiPrefix + ".doInvert", "If True, then the value will be inverted, and the timer will move in the opposite direction");

			smoothingFactor = CustomGUILayout.Slider ("Value smoothing:", smoothingFactor, 0f, 1f, ".smoothingFactor", "The amount of smoothing to apply (0 = no smoothing)");

			if (source == MenuSource.AdventureCreator)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (new GUIContent ("Timer texture:", "The texture of the slider bar"), GUILayout.Width (145f));
				timerTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> (timerTexture, false, GUILayout.Width (70f), GUILayout.Height (30f), apiPrefix + ".timerTexture");
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				uiSlider = LinkedUiGUI <Slider> (uiSlider, "Linked Slider:", source, "The Unity UI Slider this is linked to");
				uiSelectableHideStyle = (UISelectableHideStyle) CustomGUILayout.EnumPopup ("When invisible:", uiSelectableHideStyle, apiPrefix + ".uiSelectableHideStyle", "The method by which this element is hidden from view when made invisible");
			}
			CustomGUILayout.EndVertical ();

			if (source == MenuSource.AdventureCreator)
			{
				EndGUI (apiPrefix);
			}
		}
		
		#endif


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (uiSlider && uiSlider.gameObject == gameObject) return true;
			if (linkedUiID == id && id != 0) return true;
			return false;
		}


		public override int GetSlotIndex (GameObject gameObject)
		{
			if (uiSlider && uiSlider.gameObject == gameObject)
			{
				return 0;
			}
			return base.GetSlotIndex (gameObject);
		}


		public override void OnMenuTurnOn (Menu menu)
		{
			progress = -1f;
			progressSmoothing.Reset ();

			if (timerType == AC_TimerType.Conversation && KickStarter.playerInput.activeConversation && autoSetVisibility)
			{
				IsVisible = KickStarter.playerInput.activeConversation.isTimed;
			}

			base.OnMenuTurnOn (menu);
		}
		

		public override void PreDisplay (int _slot, int languageNumber, bool isActive)
		{
			if (Application.isPlaying)
			{
				float newProgress = GetProgress ();
				if (progress <= 0f && timerType == AC_TimerType.LoadingProgress)
				{
					progress = 0f;
				}
				else if (progress < 0f || smoothingFactor <= 0f)
				{
					progress = newProgress;
				}
				else
				{
					float lerpSpeed = (-9.5f * smoothingFactor) + 10f;
					progress = progressSmoothing.Update (progress, newProgress, lerpSpeed);
				}

				if (doInvert)
				{
					progress = 1f - progress;
				}

				if (uiSlider)
				{
					uiSlider.value = progress;
					UpdateUISelectable (uiSlider, uiSelectableHideStyle);
				}
				else
				{
					timerRect = relativeRect;
					timerRect.width *= progress;
				}
			}
			else
			{
				timerRect = relativeRect;
			}
		}


		public override void Display (GUIStyle _style, int _slot, float zoom, bool isActive)
		{
			if (timerTexture)
			{
				GUI.DrawTexture (ZoomRect (timerRect, zoom), timerTexture, ScaleMode.StretchToFill, true, 0f);
			}
			
			base.Display (_style, _slot, zoom, isActive);
		}


		private float GetProgress ()
		{
			switch (timerType)
			{
				case AC_TimerType.Conversation:
					if (KickStarter.playerInput.activeConversation && KickStarter.playerInput.activeConversation.isTimed)
					{
						return KickStarter.playerInput.activeConversation.GetTimeRemaining ();
					}
					return 0f;

				case AC_TimerType.QuickTimeEventProgress:
					if (KickStarter.playerQTE.QTEIsActive ())
					{
						return KickStarter.playerQTE.GetProgress ();
					}
					return 0f;

				case AC_TimerType.QuickTimeEventRemaining:
					if (KickStarter.playerQTE.QTEIsActive ())
					{
						return KickStarter.playerQTE.GetRemainingTimeFactor ();
					}
					return 0f;

				case AC_TimerType.LoadingProgress:
					return KickStarter.sceneChanger.GetLoadingProgress ();

				case AC_TimerType.Timer:
					if (timer == null)
					{
						timer = KickStarter.variablesManager.GetTimer (timerID);
						if (timer == null)
						{
							ACDebug.LogWarning ("Timer element " + title + " cannot find Timer with ID " + timerID);
						}
					}
					if (timer != null)
					{
						return KickStarter.variablesManager.GetTimer (timerID).Progress;
					}
					return 0f;
			}
			return 0f;
		}

	}

}