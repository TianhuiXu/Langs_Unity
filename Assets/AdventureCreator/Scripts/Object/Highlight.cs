/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Highlight.cs"
 * 
 *	This script is attached to any gameObject that glows
 *	when a cursor is placed over its associated interaction
 *	object.  These are not always the same object.
 * 
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AC
{

	/**
	 * Allows GameObjects associated with Hotspots to glow when the Hotspots are made active.
	 * Attach it to a mesh renderer, and assign it as the Hotspot's highlight variable.
	 */
	[AddComponentMenu ("Adventure Creator/Hotspots/Highlight")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_highlight.html")]
	public class Highlight : MonoBehaviour
	{

		#region Variables

		/** If True, then the Highlight effect will be enabled automatically when the Hotspot is selected */
		public bool highlightWhenSelected = true;
		/** If True, then Materials associated with the GameObject's Renderer will be affected. Otherwise, their intended values will be calculated, but not applied, allowing for custom effects to be achieved. */
		public bool brightenMaterials = true;
		/** If True, then child Renderer GameObjects will be brightened as well */
		public bool affectChildren = true;
		/** The fade time for the highlight transition effect */
		public float fadeTime = 0.3f;
		/** The length of time that a flash will hold for */
		public float flashHoldTime = 0f;
		/** An animation curve that describes the effect's intensity over time */
		public AnimationCurve highlightCurve = new AnimationCurve (new Keyframe (0, 1, 1, 1), new Keyframe (1, 2, 1, 1));

		/** If True, then custom events can be called when highlighting the object */
		public bool callEvents;
		/** The UnityEvent to run when the highlight effect is enabled */
		public UnityEvent onHighlightOn;
		/** The UnityEvent to run when the highlight effect is disabled */
		public UnityEvent onHighlightOff;

		protected float highlight = 1f;
		protected int direction = 1;
		protected float currentTimer;
		protected HighlightState highlightState = HighlightState.None;
		protected List<Color> originalColors = new List<Color> ();
		protected Renderer _renderer;
		protected Renderer[] childRenderers;

		private string colorProperty = "_Color";

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


		protected void Awake ()
		{
			Renderer thisRenderer = GetComponent<Renderer> ();
			if (thisRenderer && thisRenderer.material && thisRenderer.material.HasProperty ("_BaseColor") && !thisRenderer.material.HasProperty ("_Color"))
			{
				colorProperty = "_BaseColor";
			}

			/*#if UNITY_2019_3_OR_NEWER
			if (GraphicsSettings.currentRenderPipeline)
			{
				string pipelineType = GraphicsSettings.currentRenderPipeline.GetType ().ToString ();
				if (pipelineType.Contains ("HighDefinition") || pipelineType.Contains ("UniversalRenderPipelineAsset"))
				{
					colorProperty = "_BaseColor";
				}
			}
			#endif*/

			if (affectChildren)
			{
				childRenderers = GetComponentsInChildren<Renderer> ();
				foreach (Renderer childRenderer in childRenderers)
				{
					foreach (Material material in childRenderer.materials)
					{
						if (material.HasProperty (ColorProperty))
						{
							originalColors.Add (material.color);
						}
					}
				}
			}
			else
			{
				_renderer = GetComponent<Renderer> ();
				if (_renderer)
				{
					foreach (Material material in _renderer.materials)
					{
						if (material.HasProperty (ColorProperty))
						{
							originalColors.Add (material.color);
						}
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets the intended intensity of the highlighting effect at the current point in time.</summary>
		 * <returns>The intended intensity of the highlight, ranging from 0 to 1.</returns>
		 */
		public float GetHighlightIntensity ()
		{
			return (highlight - 1f);
		}


		/**
		 * <summary>Gets the highlight effect's intensity, as an alpha value for associated icon textures.</summary>
		 * <returns>The alpha value of the highlight effect</returns>
		 */
		public float GetHighlightAlpha ()
		{
			return (highlight - 1f);
		}


		/** Turns the highlight effect on. The effect will occur over time. */
		public void HighlightOn ()
		{
			if (highlightState == HighlightState.On ||
			   (highlightState == HighlightState.Normal && direction == 1))
			{
				return;
			}

			if (direction == -1 && currentTimer > 0f)
			{
				currentTimer = fadeTime - currentTimer;
			}
			else if (direction != 1)
			{
				currentTimer = 0f;
			}

			highlightState = HighlightState.Normal;
			direction = 1;

			if (callEvents && onHighlightOn != null)
			{
				onHighlightOn.Invoke ();
			}
		}


		/** Instantly turns the highlight effect on, to its maximum intensity. */
		public void HighlightOnInstant ()
		{
			highlightState = HighlightState.On;
			currentTimer = fadeTime;

			UpdateMaterials ();

			if (callEvents && onHighlightOn != null)
			{
				onHighlightOn.Invoke ();
			}
		}


		/** Turns the highlight effect off. The effect will occur over time. */
		public void HighlightOff ()
		{
			if (GetHighlightIntensity () == 0f)
			{
				HighlightOffInstant ();
				return;
			}

			highlightState = HighlightState.Normal;
			
			if (direction == 1 && currentTimer > 0f)
			{
				currentTimer = fadeTime - currentTimer;
			}
			else if (direction != -1)
			{
				currentTimer = 0f;
			}

			direction = -1;

			if (callEvents && onHighlightOff != null)
			{
				onHighlightOff.Invoke ();
			}
		}


		/** Instantly turns the highlight effect off. */
		public void HighlightOffInstant ()
		{
			highlightState = HighlightState.None;
			currentTimer = fadeTime;

			UpdateMaterials ();

			if (callEvents && onHighlightOff != null)
			{
				onHighlightOff.Invoke ();
			}
		}


		/** Flashes the highlight effect on, and then off, once. */
		public void Flash ()
		{
			if (highlightState != HighlightState.Flash && (highlightState == HighlightState.None || direction == -1))
			{
				highlightState = HighlightState.Flash;
				direction = 1;
				currentTimer = 0f;

				if (callEvents && onHighlightOn != null)
				{
					onHighlightOn.Invoke ();
				}
			}
		}


		/**
		 * <summary>Gets the duration of the flash (i.e. turn on, then off) effect.</summary>
		 * <returns>The duration, in effect, that the flash effect will last</returns>
		 */
		public float GetFlashTime ()
		{
			return fadeTime * 2f;
		}


		/** Cancels the current flash effect */
		public void CancelFlash ()
		{
			if (direction >= 0 && highlightState == HighlightState.Flash)
			{
				direction = -1;
				currentTimer = 0f;

				if (callEvents && onHighlightOff != null)
				{
					onHighlightOff.Invoke ();
				}
			}
		}


		/**
		 * <summary>Gets the flash effect's intensity, as an alpha value for associated icon textures.</summary>
		 * <param name = "original">The original alpha value of the texture this is being called for</param>
		 * <returns>The flash effect's intensity, as an alpha value</returns>
		 */
		public float GetFlashAlpha (float original)
		{
			if (highlightState == HighlightState.Flash)
			{
				return (highlight - 1f);
			}
			return Mathf.Lerp (original, 0f, Time.deltaTime * 5f);
		}


		/**
		 * <summary>Sets the minimum intensity of the highlighting effect - i.e. the intensity when the effect is considered "off".</summary>
		 * <param name = "minHighlight">The minimum intensity of the highlighting effect</param>
		 */
		public void SetMinHighlight (float minHighlight)
		{
			MinHighlight = Mathf.Max (minHighlight, 0f) + 1f;
		}


		/**
		 * <summary>Gets the time that it will take to turn the highlight effect fully on or fully off.</summary>
		 * <returns>The time, in seconds, that it takes to turn the highlight effect fully on or fully off</returns>
		 */
		public float GetFadeTime ()
		{
			return fadeTime;
		}


		/** Pulses the highlight effect on, and then off, in a continuous cycle. */
		public void Pulse ()
		{
			highlightState = HighlightState.Pulse;
			//highlight = minHighlight;
			direction = 1;
			currentTimer = 0f;
		}


		/** Re-calculates the intensity value. This is public so that it can be called every frame by the StateHandler component. */
		public void _Update ()
		{
			if (highlightState != HighlightState.None)
			{
				if (direction > 0)
				{
					// Add highlight
					if (currentTimer < fadeTime)
					{
						currentTimer += Time.deltaTime;
					}
					else
					{
						currentTimer = fadeTime;
					}

					float timeProportion = currentTimer / fadeTime;
					highlight = highlightCurve.Evaluate (timeProportion);

					if (timeProportion >= 1f)
					{
						switch (highlightState)
						{
							case HighlightState.Flash:
								direction = 0;
								currentTimer = 0f;
								break;

							case HighlightState.Pulse:
								direction = -1;
								currentTimer = 0f;
								break;

							default:
								highlightState = HighlightState.On;
								break;
						}
					}
				}
				else if (direction < 0)
				{
					// Remove highlight
					if (currentTimer < fadeTime)
					{
						currentTimer += Time.deltaTime;
					}
					else
					{
						currentTimer = fadeTime;
					}

					float timeProportion = 1f - (currentTimer / fadeTime);
					highlight = highlightCurve.Evaluate (timeProportion);

					if (timeProportion <= 0f)
					{
						highlight = 1f;

						if (highlightState == HighlightState.Pulse)
						{
							direction = 1;
							currentTimer = 0f;
						}
						else
						{
							highlightState = HighlightState.None;
						}
					}
				}
				else
				{
					// Flash pause
					currentTimer += Time.deltaTime;
					if (currentTimer >= flashHoldTime)
					{
						CancelFlash ();
					}
				}

				UpdateMaterials ();
			}
			else
			{
				if (!Mathf.Approximately (highlight, MinHighlight))
				{
					highlight = MinHighlight;
					UpdateMaterials ();
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void UpdateMaterials ()
		{
			if (!brightenMaterials)
			{
				return;
			}

			int i = 0;
			float alpha;

			if (affectChildren)
			{
				foreach (Renderer childRenderer in childRenderers)
				{
					foreach (Material material in childRenderer.materials)
					{
						if (originalColors.Count <= i)
						{
							break;
						}

						if (material.HasProperty (ColorProperty))
						{
							alpha = material.color.a;
							Color newColor = originalColors[i] * highlight;
							newColor.a = alpha;
							material.SetColor (ColorProperty, newColor);
							i++;
						}
					}
				}
			}
			else if (_renderer)
			{
				foreach (Material material in _renderer.materials)
				{
					if (material.HasProperty (ColorProperty))
					{
						alpha = material.color.a;
						Color newColor = originalColors[i] * highlight;
						newColor.a = alpha;
						material.SetColor (ColorProperty, newColor);
						i++;
					}
				}
			}
			return;


		}

		#endregion


		#region GetSet

		private float MinHighlight
		{
			get
			{
				if (highlightCurve.keys.Length > 0)
				{
					return highlightCurve.keys[0].value;
				}
				return 1f;
			}
			set
			{
				if (highlightCurve.keys.Length > 0)
				{
					Keyframe[] keyframes = highlightCurve.keys;
					keyframes[0].value = value;
					highlightCurve.keys = keyframes;
				}
			}
		}


		private string ColorProperty
		{
			get
			{
				if (KickStarter.settingsManager && !string.IsNullOrEmpty (KickStarter.settingsManager.highlightMaterialPropertyOverride))
				{ 
					return KickStarter.settingsManager.highlightMaterialPropertyOverride;
				}
				return colorProperty;
			}
		}

		#endregion

	}

}