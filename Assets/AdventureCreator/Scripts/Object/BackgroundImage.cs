/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"BackgroundImage.cs"
 * 
 *	The BackgroundImage prefab is used to store a GUITexture for use in background images for 2.5D games.
 * 
 */

#if UNITY_STANDALONE && !UNITY_2018_2_OR_NEWER
#define ALLOW_MOVIETEXTURES
#endif

//#if !UNITY_SWITCH
#define ALLOW_VIDEO
//#endif

using UnityEngine;
using System.Collections;
#if ALLOW_VIDEO
using UnityEngine.Video;
#endif

namespace AC
{

	/**
	 * Controls a GUITexture for use in background images in 2.5D games.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_background_image.html")]
	public class BackgroundImage : MonoBehaviour
	{

		#region Variables

		#if ALLOW_VIDEO

		public enum BackgroundImageSource { Texture, VideoClip };
		/** What type of asset is used as a background (Texture, VideoClip) */
		public BackgroundImageSource backgroundImageSource = BackgroundImageSource.Texture;
		/** The VideoClip to use as a background, if animated */
		public VideoClip backgroundVideo;
		protected VideoPlayer videoPlayer;

		#endif

		/** The Texture to use as a background, if static */
		public Texture backgroundTexture;


		#if ALLOW_MOVIETEXTURES

		/** If True, then any MovieTexture set as the background will be looped */
		public bool loopMovie = true;
		/** If True, then any MovieTexture set as the background will start from the beginning when the associated Camera is activated */
		public bool restartMovieWhenTurnOn = false;

		#endif


		protected float shakeDuration;
		protected float startTime;
		protected float startShakeIntensity;
		protected float shakeIntensity;
		protected Rect originalPixelInset;
		protected AnimationCurve shakeCurve;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			#if ALLOW_VIDEO
			PrepareVideo ();
			#endif
		}


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
		 * <summary>Sets the background image to a supplied texture</summary>
		 * <param name = "_texture">The texture to set the background image to</param>
		 */
		public void SetImage (Texture2D _texture)
		{
			SetBackgroundTexture (_texture);
		}


		/**
		 * Displays the background image full-screen.
		 */
		public void TurnOn ()
		{
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) == -1)
			{
				ACDebug.LogWarning ("No '" + KickStarter.settingsManager.backgroundImageLayer + "' layer exists - please define one in the Tags Manager.");
			}
			else
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer);
			}

			TurnOnUI ();

			#if ALLOW_MOVIETEXTURES

			if (backgroundTexture != null && backgroundTexture is MovieTexture)
			{
				MovieTexture movieTexture = (MovieTexture) backgroundTexture;
				if (restartMovieWhenTurnOn)
				{
					movieTexture.Stop ();
				}
				movieTexture.loop = loopMovie;
				movieTexture.Play ();
			}

			#endif
		}


		/**
		 * Hides the background image from view.
		 */
		public void TurnOff ()
		{
			gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);

			TurnOffUI ();
		}


		/**
		 * <summary>Shakes the background image (within the GUITexture) for an earthquake-like effect.</summary>
		 * <param name = "_shakeIntensity">How intense the shake effect should be</param>
		 * <param name = "_duration">How long the shake effect should last, in seconds</param>
		 */
		public void Shake (float _shakeIntensity, float _duration, AnimationCurve _shakeCurve = null)
		{
			shakeDuration = _duration;
			startTime = Time.time;
			shakeIntensity = _shakeIntensity;
			shakeCurve = _shakeCurve;

			startShakeIntensity = shakeIntensity;

			StopCoroutine (UpdateShake ());
			StartCoroutine (UpdateShake ());
		}

		
		#if ALLOW_VIDEO

		public void CancelVideoPlayback ()
		{
			if (videoPlayer)
			{
				videoPlayer.Stop ();
			}
			StopCoroutine ("PlayVideoCoroutine");
		}

		#endif

		#endregion


		#region ProtectedFunctions

		protected void TurnOnUI ()
		{
			SetBackgroundCameraFarClipPlane (0.02f);
			BackgroundImageUI.Instance.SetTexture (backgroundTexture);

			#if ALLOW_VIDEO

			if (Application.isPlaying && backgroundImageSource == BackgroundImageSource.VideoClip)
			{
				StartCoroutine (PlayVideoCoroutine ());
			}

			#endif
		}


		protected void TurnOffUI ()
		{
			#if ALLOW_VIDEO

			if (backgroundImageSource == BackgroundImageSource.VideoClip)
			{
				if (Application.isPlaying)
				{
				
					videoPlayer.Stop ();
					if (videoPlayer.isPrepared)
					{
						if (videoPlayer.texture)
						{
							BackgroundImageUI.Instance.ClearTexture (videoPlayer.texture);
						}
						return;
					}
				}
			}

			#endif

			Texture texture = backgroundTexture;
			if (texture)
			{
				BackgroundImageUI.Instance.ClearTexture (texture);
			}
		}


		protected void SetBackgroundCameraFarClipPlane (float value)
		{
			BackgroundCamera backgroundCamera = Object.FindObjectOfType <BackgroundCamera>();
			if (backgroundCamera)
			{
				backgroundCamera.GetComponent <Camera>().farClipPlane = value;
			}
			else
			{
				ACDebug.LogWarning ("Cannot find BackgroundCamera");
			}
		}


		protected IEnumerator UpdateShake ()
		{
			while (shakeIntensity > 0f)
			{
				float _size = Random.Range (0, shakeIntensity) * 0.2f;

				BackgroundImageUI.Instance.SetShakeIntensity (_size);

				float lerpAmount = AdvGame.Interpolate (startTime, shakeDuration, MoveMethod.Linear, null);
				if (lerpAmount >= 1f)
				{
					shakeIntensity = 0f;
				}
				else if (shakeCurve != null)
				{
					shakeIntensity = startShakeIntensity * shakeCurve.Evaluate (lerpAmount);
					shakeIntensity = Mathf.Max (shakeIntensity, 0.001f);
				}
				else
				{
					shakeIntensity = Mathf.Lerp (startShakeIntensity, 0f, lerpAmount);
					shakeIntensity = Mathf.Max (shakeIntensity, 0.001f);
				}
				
				yield return new WaitForEndOfFrame ();
			}
			
			shakeIntensity = 0f;

			BackgroundImageUI.Instance.SetShakeIntensity (0f);
		}


		protected void SetBackgroundTexture (Texture _texture)
		{
			backgroundTexture = _texture;
		}


		#if ALLOW_VIDEO

		protected IEnumerator PlayVideoCoroutine ()
		{
			foreach (BackgroundImage backgroundImage in KickStarter.stateHandler.BackgroundImages)
			{
				if (backgroundImage)
				{
					backgroundImage.CancelVideoPlayback ();
				}
			}
			yield return new WaitForEndOfFrame ();
			
			videoPlayer.Prepare ();
			while (!videoPlayer.isPrepared)
			{
				yield return new WaitForEndOfFrame ();
			}

			videoPlayer.Play ();
			yield return new WaitForEndOfFrame ();
			BackgroundImageUI.Instance.SetTexture (videoPlayer.texture);
		}


		protected void PrepareVideo ()
		{
			if (backgroundImageSource == BackgroundImageSource.VideoClip)
			{
				videoPlayer = GetComponent <VideoPlayer>();
				if (videoPlayer == null)
				{
					videoPlayer = gameObject.AddComponent <VideoPlayer>();
					videoPlayer.isLooping = true;
				}
				videoPlayer.playOnAwake = false;
				videoPlayer.renderMode = VideoRenderMode.APIOnly;
				videoPlayer.clip = backgroundVideo;
			}
		}

		#endif

		#endregion

	}

}