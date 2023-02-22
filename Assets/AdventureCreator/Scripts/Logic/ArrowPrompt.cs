/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ArrowPrompt.cs"
 * 
 *	This script allows for "Walking Dead"-style on-screen arrows,
 *	which respond to player input.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This component provides the ability to display up to four arrows on-screen.
	 * Each arrow responds to player input, and can run an ActionList when the relevant input is detected.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_arrow_prompt.html")]
	public class ArrowPrompt : MonoBehaviour, iActionListAssetReferencer
	{

		#region Variables

		/** Where the Actions are stored when not being run (InScene, AssetFile) */
		public ActionListSource source;
		/** What kind of input the arrows respond to (KeyOnly, ClickOnly, KeyAndClick) */
		public ArrowPromptType arrowPromptType = ArrowPromptType.KeyAndClick;
		/** The "up" Arrow */
		public Arrow upArrow;
		/** The "down" Arrow */
		public Arrow downArrow;
		/** The "left" Arrow */
		public Arrow leftArrow;
		/** The "right" Arrow */
		public Arrow rightArrow;
		/** If True, then Hotspots will be disabled when the arrows are on screen */
		public bool disableHotspots = true;

		/** A factor for the arrow position */
		public float positionFactor = 1f;
		/** A factor for the arrow size */
		public float scaleFactor = 1f;

		protected bool isOn = false;
		
		protected AC_Direction directionToAnimate;
		protected float alpha = 0f;
		protected float arrowSize = 0.05f;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}

		#endregion


		#region PublicFunctions

		/**
		 * Draws the arrow(s) on screen, if appropriate.
		 * This function is called every frame by StateHandler.
		 */
		public void DrawArrows ()
		{
			if (alpha > 0f)
			{
				if (directionToAnimate != AC_Direction.None)
				{
					SetGUIAlpha (alpha);

					switch (directionToAnimate)
					{
						case AC_Direction.Up:
							upArrow.rect = GetUpRect (arrowSize);
							break;

						case AC_Direction.Down:
							downArrow.rect = GetDownRect (arrowSize);
							break;

						case AC_Direction.Left:
							leftArrow.rect = GetLeftRect (arrowSize);
							break;

						case AC_Direction.Right:
							rightArrow.rect = GetRightRect (arrowSize);
							break;
					}
				}
				
				else
				{
					SetGUIAlpha (alpha);
					
					if (upArrow.isPresent)
					{
						upArrow.rect = GetUpRect ();
					}
		
					if (downArrow.isPresent)
					{
						downArrow.rect = GetDownRect ();
					}
				
					if (leftArrow.isPresent)
					{
						leftArrow.rect = GetLeftRect ();
					}
					
					if (rightArrow.isPresent)
					{
						rightArrow.rect = GetRightRect ();
					}
				}
			
				upArrow.Draw ();
				downArrow.Draw ();
				leftArrow.Draw ();
				rightArrow.Draw ();
			}
		}


		/**
		 * <summary>Enables the ArrowPrompt.</summary>
		 */
		public void TurnOn ()
		{
			if (upArrow.isPresent || downArrow.isPresent || leftArrow.isPresent || rightArrow.isPresent)
			{
				if (KickStarter.playerInput)
				{
					KickStarter.playerInput.activeArrows = this;
				}
				
				StartCoroutine ("FadeIn");
				directionToAnimate = AC_Direction.None;
				arrowSize = 0.05f;
			}
		}


		/**
		 * <summary>Disables the ArrowPrompt.</summary>
		 */
		public void TurnOff ()
		{
			Disable ();
			StopCoroutine ("FadeIn");
			alpha = 0f;
		}
		

		/**
		 * Triggers the "up" arrow.
		 */
		public void DoUp ()
		{
			if (upArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Up));
				Disable ();
				upArrow.Run (source);
			}
		}
		

		/**
		 * Triggers the "down" arrow.
		 */
		public void DoDown ()
		{
			if (downArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Down));
				Disable ();
				downArrow.Run (source);
			}
		}
		

		/**
		 * Triggers the "left" arrow.
		 */
		public void DoLeft ()
		{
			if (leftArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Left));
				Disable ();
				leftArrow.Run (source);
			}
		}
		

		/**
		 * Triggers the "right" arrow.
		 */
		public void DoRight ()
		{
			if (rightArrow.isPresent && isOn && directionToAnimate == AC_Direction.None)
			{
				StartCoroutine (FadeOut (AC_Direction.Right));
				Disable ();
				rightArrow.Run (source);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected Rect GetUpRect (float scale = 0.05f)
		{
			return KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.5f, 0.1f * positionFactor, scale * 2f * scaleFactor, scale * scaleFactor));
		}


		protected Rect GetDownRect (float scale = 0.05f)
		{
			return KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.5f, 1f - (0.1f * positionFactor), scale * 2f * scaleFactor, scale * scaleFactor));
		}


		protected Rect GetLeftRect (float scale = 0.05f)
		{
			return KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (0.05f * positionFactor * 2f, 0.5f, scale * scaleFactor, scale * 2f * scaleFactor));
		}


		protected Rect GetRightRect (float scale = 0.05f)
		{
			return KickStarter.mainCamera.LimitMenuToAspect (AdvGame.GUIRect (1f - (0.05f * positionFactor * 2f), 0.5f, scale * scaleFactor, scale * 2f * scaleFactor));
		}

		
		protected void Disable ()
		{
			if (KickStarter.playerInput)
			{
				KickStarter.playerInput.activeArrows = null;
			}
			
			isOn = false;
		}
		

		protected IEnumerator FadeIn ()
		{
			alpha = 0f;
			
			if (alpha < 1f)
			{
				while (alpha < 0.95f)
				{
					alpha += 0.05f;
					alpha = Mathf.Clamp01 (alpha);
					yield return new WaitForFixedUpdate();
				}
				
				alpha = 1f;
				isOn = true;
			}
		}
		
		
		protected IEnumerator FadeOut (AC_Direction direction)
		{
			arrowSize = 0.05f;
			alpha = 1f;
			directionToAnimate = direction;
			
			if (alpha > 0f)
			{
				while (alpha > 0.05f)
				{
					arrowSize += 0.005f;
					
					alpha -= 0.05f;
					alpha = Mathf.Clamp01 (alpha);
					yield return new WaitForFixedUpdate();
				}
				alpha = 0f;

			}
		}
		
		
		protected void SetGUIAlpha (float alpha)
		{
			Color tempColor = GUI.color;
			tempColor.a = alpha;
			GUI.color = tempColor;
		}

		#endregion


		#region GetSet

		protected float LargeSize
		{
			get
			{
				return arrowSize*2 * scaleFactor;
			}
		}


		protected float SmallSize
		{
			get
			{
				return arrowSize * scaleFactor;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (source == ActionListSource.AssetFile)
			{
				if (upArrow.isPresent && upArrow.linkedActionList == actionListAsset) return true;
				if (leftArrow.isPresent && leftArrow.linkedActionList == actionListAsset) return true;
				if (rightArrow.isPresent && rightArrow.linkedActionList == actionListAsset) return true;
				if (downArrow.isPresent && downArrow.linkedActionList == actionListAsset) return true;
			}
			return false;
		}

		#endif

	}


	/**
	 * A data container for an arrow that is used in an ArrowPrompt.
	 */
	[System.Serializable]
	public class Arrow
	{
			
		/** If True, the Arrow is defined and used in the ArrowPrompt */
		public bool isPresent;
		/** The Cutscene to run when the Arrow is triggered */
		public Cutscene linkedCutscene;
		/** The ActionList to run when the Arrow is triggered */
		public ActionListAsset linkedActionList;
		/** The texture to draw on-screen */
		public Texture2D texture;
		/** The OnGUI Rect that defines the screen boundary */
		public Rect rect;
		

		/**
		 * The default Constructor.
		 */
		public Arrow ()
		{
			isPresent = false;
		}
		

		/**
		 * <summary>Runs the Arrow's linkedCutscene.</summary>
		 * <param name = "actionListSource">Where the Actions are stored when not being run</param>
		 */
		public void Run (ActionListSource actionListSource)
		{
			if (actionListSource == ActionListSource.AssetFile)
			{
				if (linkedActionList)
				{
					linkedActionList.Interact ();
				}
			}
			else if (actionListSource == ActionListSource.InScene)
			{
				if (linkedCutscene)
				{
					linkedCutscene.Interact ();
				}
			}
		}
		

		/**
		 * Draws the Arrow on screen.
		 * This is called every OnGUI call by StateHandler.
		 */
		public void Draw ()
		{
			if (texture)
			{
				GUI.DrawTexture (rect, texture, ScaleMode.StretchToFill, true);
			}
		}

	}

}