/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MainCamera.cs"
 * 
 *	This is attached to the Main Camera, and must be tagged as "MainCamera" to work.
 *	Only one Main Camera should ever exist in the scene.
 *
 *	Shake code adapted from Mike Jasper's code: http://www.mikedoesweb.com/2012/camera-shake-in-unity/
 *
 *  Aspect-ratio code adapated from Eric Haines' code: http://wiki.unity3d.com/index.php?title=AspectRatioEnforcer
 * 
 */

#if !UNITY_2020_2_OR_NEWER && (UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS)
#define ALLOW_VR
#endif

#if UNITY_2018_2_OR_NEWER
#define ALLOW_PHYSICAL_CAMERA
#endif

using UnityEngine;
using System.Collections;
#if ALLOW_VR
using UnityEngine.VR;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * This is attached to the scene's Main Camera, and must be tagged as "MainCamera".
	 * The camera system works by having the MainCamera attach itself to the "active" _Camera component.
	 * Each _Camera component is merely used for reference - only the MainCamera actually performs any rendering.
	 * Shake code adapted from Mike Jasper's code: http://www.mikedoesweb.com/2012/camera-shake-in-unity/
	 * Aspect-ratio code adapated from Eric Haines' code: http://wiki.unity3d.com/index.php?title=AspectRatioEnforcer
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_main_camera.html")]
	[ExecuteInEditMode]
	public class MainCamera : MonoBehaviour
	{

		#region Variables

		[SerializeField] protected Texture2D fadeTexture;
		protected Texture2D tempFadeTexture = null;

		protected _Camera _attachedCamera;
		/** The last active camera during gameplay */
		[System.NonSerialized] public _Camera lastNavCamera;
		protected _Camera lastNavCamera2;

		/** If True, the fade texture will be rendered on the script automatically.  If False, the user can read this script's GetFadeTexture and GetFadeAlpha values to render it with a custom technique. */
		[SerializeField] protected bool renderFading = true;
		/** If True, borders will be drawn outside of the playable screen area. */
		[SerializeField] protected bool renderBorders = true;

		protected _Camera transitionFromCamera;

		protected enum MainCameraMode { NormalSnap, NormalTransition };
		protected MainCameraMode mainCameraMode = MainCameraMode.NormalSnap;

		protected bool isCrossfading;
		protected Texture2D crossfadeTexture;
		
		protected Vector2 perspectiveOffset = new Vector2 (0f, 0f);

		// Fading
		protected float fadeDuration, fadeTimer;
		protected int drawDepth = -1000;
		protected float alpha = 0f; 
		protected FadeType fadeType;
		protected CameraFadePauseBehaviour cameraFadePauseBehaviour = CameraFadePauseBehaviour.Cancel;
		protected AnimationCurve fadeCurve;
		
		protected GameCameraData currentFrameCameraData;
		
		protected MoveMethod moveMethod;
		protected float transitionDuration, transitionTimer;
		protected AnimationCurve timeCurve;

		protected _Camera previousAttachedCamera = null;
		protected GameCameraData oldCameraData;
		protected bool retainPreviousSpeed = false;

		protected Texture2D actualFadeTexture = null;
		protected float shakeStartTime;
		protected float shakeDuration;
		protected float shakeStartIntensity;
		protected CameraShakeEffect shakeEffect;
		protected float shakeIntensity;
		protected Vector3 shakePosition;
		protected Vector3 shakeRotation;
		protected AnimationCurve shakeCurve;
		
		// Aspect ratio
		protected Camera borderCam;
		protected float borderWidth;
		protected MenuOrientation borderOrientation;
		protected Rect borderRect1 = new Rect (0f, 0f, 0f, 0f);
		protected Rect borderRect2 = new Rect (0f, 0f, 0f, 0f);
		protected Rect midBorderRect = new Rect (0f, 0f, 0f, 0f);

		protected Vector2 aspectRatioScaleCorrection = Vector2.zero;
		protected Vector2 aspectRatioOffsetCorrection = Vector2.zero;

		// Split-screen
		protected bool isSplitScreen;
		protected bool isTopLeftSplit;
		protected CameraSplitOrientation splitOrientation;
		protected _Camera splitCamera;
		protected float splitAmountMain = 0.49f;
		protected float splitAmountOther = 0.49f;
		
		private Rect overlayRect;
		private float overlayDepthBackup;
		
		// Custom FX
		protected float focalDistance = 10f;
		
		protected Camera ownCamera;
		protected AudioListener _audioListener;

		// Timeline
		protected bool timelineOverride;
		protected bool timelineFadeOverride;
		protected Texture2D timelineFadeTexture;
		protected float timelineFadeWeight;

		// Internal rects
		protected Rect safeScreenRectInverted;
		protected Rect playableScreenRect;
		protected Rect playableScreenRectRelative;
		protected Rect playableScreenRectInverted;
		protected Rect playableScreenRectRelativeInverted;
		protected float playableScreenDiagonalLength;

		private Rect lastSafeRect;
		private float lastAspectRatio;

		protected int overlayFrames;
		private Transform _transform;

		#if ALLOW_VR
		/** If True, the camera's position and rotation will be restored when loading (VR only) */
		public bool restoreTransformOnLoadVR = false;
		#endif
		/** How the MainCamera's "forward" direction is calculated, which is used for various camera and movement behaviours */
		public MainCameraForwardDirection forwardDirection = MainCameraForwardDirection.MainCameraComponent;

		#endregion


		#region UnityStandards

		public void OnInitGameEngine (bool hideWhileLoading = true)
		{
			if (gameObject.tag != Tags.mainCamera)
			{
				ACDebug.LogWarning ("The AC MainCamera does not have the 'MainCamera' tag.  It may not be recognised as the same MainCamera by custom scripts or third-party assets.", this);
			}

			RecalculateRects ();

			AssignFadeTexture ();
			if (KickStarter.sceneChanger && !KickStarter.settingsManager.useLoadingScreen)
			{
				SetFadeTexture (KickStarter.sceneChanger.GetAndResetTransitionTexture ());
			}
		}


		private void Start ()
		{
			RecalculateRects ();
		}


		private void OnEnable ()
		{
			EventManager.OnAfterChangeScene += OnAfterChangeScene;
			EventManager.OnEnterGameState += OnEnterGameState;
		}


		private void OnDisable ()
		{
			EventManager.OnAfterChangeScene -= OnAfterChangeScene;
			EventManager.OnEnterGameState -= OnEnterGameState;
		}


		/**
		 * Updates the Camera's position.
		 * This is called every frame by StateHandler.
		 */
		public void _LateUpdate ()
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.IsInLoadingScene ())
			{
				return;
			}

			if (overlayFrames > 0) overlayFrames--;

			if (lastSafeRect != ACScreen.safeArea || lastAspectRatio != KickStarter.settingsManager.AspectRatio)
			{
				if (lastSafeRect.width > 0)
				{
					KickStarter.playerMenus.RecalculateAll ();
				}
				lastSafeRect = ACScreen.safeArea;
				lastAspectRatio = KickStarter.settingsManager.AspectRatio;
			}

			UpdateCameraFade ();
			
			bool attachedIs25D = (attachedCamera is GameCamera25D);
			if (attachedCamera && !attachedIs25D)
			{
				switch (mainCameraMode)
				{
					case MainCameraMode.NormalSnap:
						currentFrameCameraData = new GameCameraData (attachedCamera);
						break;

					case MainCameraMode.NormalTransition:
						UpdateCameraTransition ();
						break;

					default:
						break;
				}

				if (!timelineOverride)
				{
					ApplyCameraData (currentFrameCameraData);
				}
			}
			
			else if (attachedCamera && attachedIs25D)
			{
				Transform.position = attachedCamera.CameraTransform.position;
				Transform.rotation = attachedCamera.CameraTransform.rotation;

				perspectiveOffset = attachedCamera.GetPerspectiveOffset ();
				if (AllowProjectionShifting (Camera))
				{
					Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
				}

				currentFrameCameraData = new GameCameraData (this);
			}

			// Shake
			if (KickStarter.stateHandler.gameState != GameState.Paused)
			{
				if (shakeIntensity > 0f)
				{
					if (shakeEffect != CameraShakeEffect.Rotate)
					{
						shakePosition = Random.insideUnitSphere * shakeIntensity * 0.5f;
					}

					if (shakeEffect != CameraShakeEffect.Translate)
					{
						shakeRotation = new Vector3
						(
							Random.Range (-shakeIntensity, shakeIntensity) * 0.2f,
							Random.Range (-shakeIntensity, shakeIntensity) * 0.2f,
							Random.Range (-shakeIntensity, shakeIntensity) * 0.2f
						);
					}
					
					float lerpAmount = AdvGame.Interpolate (shakeStartTime, shakeDuration, MoveMethod.Linear, null);
					if (lerpAmount >= 1f)
					{
						shakeIntensity = 0f;
					}
					else if (shakeCurve != null)
					{
						shakeIntensity = shakeStartIntensity * shakeCurve.Evaluate (lerpAmount);
						shakeIntensity = Mathf.Max (shakeIntensity, 0.001f);
					}
					else
					{
						shakeIntensity = Mathf.Lerp (shakeStartIntensity, 0f, lerpAmount);
						shakeIntensity = Mathf.Max (shakeIntensity, 0.001f);
					}

					Transform.position += shakePosition;
					Transform.localEulerAngles += shakeRotation;
				}
				else if (shakeIntensity < 0f)
				{
					StopShaking ();
				}
			}
		}


		protected virtual void OnDestroy ()
		{
			crossfadeTexture = null;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Forces an overlay for a number of frames. This is used to mask loading and intialisation, and the game will considered a 'cutscene' during this time.</summary>
		 * <param name = "numFrames">The number of frames to enforce the overlay for.  This is frame-rate independent, so it will still apply while the game is paused.</param>
		 */
		public void ForceOverlayForFrames (int numFrames)
		{
			overlayFrames = numFrames;
		}


		/**
		 * <summary>Checks if an overlay is being enforced via a ForceOverlayForFrames call</summary>
		 * <returns>True if an overlay is being enforced</returns>>
		 */
		public bool IsShowingForcedOverlay ()
		{
			return (overlayFrames > 0);
		}


		/**
		 * <summary>Sets the camera's default fade texture, which is used when not temporarily set to something else.</summary>
		 * <param name = "_fadeTexture">The new fadeTexture to use, if not null</param>
		 */
		public void SetDefaultFadeTexture (Texture2D _fadeTexture)
		{
			if (_fadeTexture)
			{
				fadeTexture = _fadeTexture;
			}
		}


		public void RecalculateRects ()
		{
			if (SetAspectRatio ())
			{
				CreateBorderCamera ();
			}
			CalculatePlayableScreenArea ();
			SetCameraRect ();
			CalculateUnityUIAspectRatioCorrection ();

			if (isSplitScreen && splitCamera)
				splitCamera.SetSplitScreen ();

			if (Application.isPlaying && KickStarter.eventManager)
				KickStarter.eventManager.Call_OnUpdatePlayableScreenArea ();
		}
		

		/**
		 * <summary>Shakes the Camera, creating an "earthquake" effect.</summary>
		 * <param name = "_shakeIntensity">The shake intensity</param>
		 * <param name = "_duration">The duration of the effect, in sectonds</param>
		 * <param name = "_shakeEffect">The type of shaking to make (Translate, Rotate, TranslateAndRotate)</param>
		 */
		public void Shake (float _shakeIntensity, float _duration, CameraShakeEffect _shakeEffect, AnimationCurve _shakeCurve = null)
		{
			shakeCurve = _shakeCurve;

			shakePosition = Vector3.zero;
			shakeRotation = Vector3.zero;
			
			shakeEffect = _shakeEffect;
			shakeDuration = _duration;
			shakeStartTime = Time.time;
			shakeIntensity = _shakeIntensity;
			
			shakeStartIntensity = shakeIntensity;

			KickStarter.eventManager.Call_OnShakeCamera (shakeIntensity, shakeDuration);
		}
		

		/**
		 * <summary>Checks if the Camera is shaking.</summary>
		 * <returns>True if the Camera is shaking</returns>
		 */
		public bool IsShaking ()
		{
			if (shakeIntensity > 0f)
			{
				return true;
			}
			
			return false;
		}
		

		/**
		 * Ends the "earthquake" shake effect.
		 */
		public void StopShaking ()
		{
			shakeIntensity = 0f;
			shakePosition = Vector3.zero;
			shakeRotation = Vector3.zero;

			KickStarter.eventManager.Call_OnShakeCamera (0f, 0f);
		}
		

		/**
		 * Prepares the Camera for being able to render a BackgroundImage underneath scene objects.
		 */
		public virtual void PrepareForBackground ()
		{
			Camera.clearFlags = CameraClearFlags.Depth;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) != -1)
			{
				Camera.cullingMask = Camera.cullingMask & ~(1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
			}
		}
		

		/**
		 * Activates the FirstPersonCamera found in the Player prefab.
		 */
		public void SetFirstPerson ()
		{
			if (KickStarter.player == null)
			{
				ACDebug.LogWarning ("Cannot set first-person camera because no Player can be found!");
				return;
			}

			FirstPersonCamera firstPersonCamera = KickStarter.player.FirstPersonCameraComponent;
			if (firstPersonCamera)
			{
				SetGameCamera (firstPersonCamera);
			}

			UpdateLastGameplayCamera ();
		}


		#if UNITY_EDITOR
		private void OnGUI ()
		{
			if (timelineFadeOverride && !Application.isPlaying)
			{
				DrawCameraFade ();
			}
		}
		#endif


		/**
		 * <summary>Draws the Camera's fade texture. This is called every OnGUI call by StateHandler.</summary>
		 */
		public void DrawCameraFade ()
		{
			if (timelineFadeOverride)
			{
				Color originalColor = GUI.color;
				Color tempColor = GUI.color;
				tempColor.a = timelineFadeWeight;
				GUI.color = tempColor;
				GUI.depth = drawDepth;
				GUI.DrawTexture (new Rect (0, 0,  ACScreen.width,  ACScreen.height), timelineFadeTexture);
				GUI.color = originalColor;
				return;
			}

			if (overlayFrames > 0 && renderFading && actualFadeTexture)
			{
				GUI.DrawTexture (new Rect (0, 0, ACScreen.width, ACScreen.height), actualFadeTexture);
				return;
			}

			if (alpha > 0f)
			{
				Color tempColor = GUI.color;
				tempColor.a = alpha;
				GUI.color = tempColor;
				GUI.depth = drawDepth;
				
				if (KickStarter.stateHandler.IsPaused ())
				{
					switch (cameraFadePauseBehaviour)
					{
						case CameraFadePauseBehaviour.Cancel:
							FadeIn (0f);
							break;

						case CameraFadePauseBehaviour.Continue:
							break;

						case CameraFadePauseBehaviour.Hide:
							return;

						default:
							break;
					}
				}

				if (isCrossfading)
				{
					if (crossfadeTexture)
					{
						GUI.DrawTexture (new Rect (0, 0, ACScreen.width, ACScreen.height), crossfadeTexture);
					}
					else
					{
						ACDebug.LogWarning ("Cannot crossfade as the crossfade texture was not succesfully generated.");
					}
				}
				else if (renderFading)
				{
					if (actualFadeTexture)
					{
						GUI.DrawTexture (new Rect (0, 0, ACScreen.width, ACScreen.height), actualFadeTexture);
					}
					else
					{
						ACDebug.LogWarning ("Cannot fade camera as no fade texture has been assigned.");
					}
				}
			}
			else if (actualFadeTexture != fadeTexture && !isFading())
			{
				ReleaseFadeTexture ();
			}
		}


		/** The alpha value of the current fade effect (0 = not visible, 1 = fully visible) */
		public float GetFadeAlpha ()
		{
			return alpha;
		}


		/** The texture to display full-screen for the fade effect */
		public Texture2D GetFadeTexture ()
		{
			AssignFadeTexture ();
			return actualFadeTexture;
		}
		

		/** Resets the Camera's projection matrix. */
		public void ResetProjection ()
		{
			if (Camera)
			{
				perspectiveOffset = Vector2.zero;
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
				Camera.ResetProjectionMatrix ();
			}
		}
		

		/** Resets the transition effect when moving from one _Camera to another. */
		public void ResetMoving ()
		{
			mainCameraMode = MainCameraMode.NormalSnap;
			transitionTimer = 0f;
			transitionDuration = 0f;
		}


		/** Snaps the Camera to the attachedCamera instantly. */
		public void SnapToAttached ()
		{
			if (attachedCamera && attachedCamera.Camera)
			{
				ResetMoving ();
				transitionFromCamera = null;

				bool changedOrientation = (previousAttachedCamera && previousAttachedCamera.Transform.rotation != attachedCamera.Transform.rotation);

				currentFrameCameraData = new GameCameraData (attachedCamera);
				ApplyCameraData (currentFrameCameraData);

				if (changedOrientation)
				{
					KickStarter.playerInput.BeginCameraLockSnap ();
				}
			}
		}
		

		/**
		 * <summary>Crossfades to a new _Camera over time.</summary>
		 * <param name = "_transitionDuration">The duration, in seconds, of the crossfade</param>
		 * <param name = "_linkedCamera">The _Camera to crossfade to</param>
		 * <param name = "_fadeCurve">An animation curve used to set the transition behaviour over time</param>
		 */
		public void Crossfade (float _transitionDuration, _Camera _linkedCamera, AnimationCurve _fadeCurve)
		{
			object[] parms = new object[3] { _transitionDuration, _linkedCamera, _fadeCurve};
			StartCoroutine ("StartCrossfade", parms);
		}
		

		/**
		 * Instantly ends the crossfade effect.
		 */
		public void StopCrossfade ()
		{
			StopCoroutine ("StartCrossfade");
			if (isCrossfading)
			{
				isCrossfading = false;
				alpha = 0f;
			}

			#if UNITY_2018_1_OR_NEWER
			Destroy (crossfadeTexture);
			#else
			DestroyObject (crossfadeTexture);
			#endif
			crossfadeTexture = null;
		}


		/** Places a full-screen texture of the current game window over the screen, allowing for a scene change to have no visible transition. */
		public void TakeOverlayScreenshot ()
		{
			Texture2D screenTex = new Texture2D (ACScreen.width, ACScreen.height);
			
			#if UNITY_EDITOR
			screenTex.ReadPixels (new Rect (0f, 0f, ACScreen.width-1, ACScreen.height-1), 0, 0, false);
			#else
			screenTex.ReadPixels (new Rect (0f, 0f, ACScreen.width, ACScreen.height), 0, 0, false);
			#endif

			if (KickStarter.settingsManager.linearColorTextures)
			{
				for (int y = 0; y < screenTex.height; y++)
				{
					for (int x = 0; x < screenTex.width; x++)
					{
						Color color = screenTex.GetPixel(x, y);
						screenTex.SetPixel(x, y, color.linear);
					}
				}
			}

			screenTex.Apply ();
			screenTex.name = "OverlayTexture";
			screenTex.Apply ();
			SetFadeTexture (screenTex);
			KickStarter.sceneChanger.SetTransitionTexture (screenTex);
			FadeOut (0f);
		}


		/**
		 * <summary>Gets the _Camera being transitioned from, if the MainCamera is transitioning between two _Cameras.</summary>
		 * <returns>The _Camera being transitioned from, if the MainCamera is transitioning between two _Cameras.</returns>
		 */
		public _Camera GetTransitionFromCamera ()
		{
			if (mainCameraMode == MainCameraMode.NormalTransition)
			{
				return transitionFromCamera;
			}
			return null;
		}
		

		/**
		 * <summary>Sets a _Camera as the new attachedCamera to follow.</summary>
		 * <param name = "newCamera">The new _Camera to follow</param>
		 * <param name = "transitionTime">The time, in seconds, that it will take to move towards the new _Camera</param>
		 * <param name = "_moveMethod">How the Camera should move towards the new _Camera, if transitionTime > 0f (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_animationCurve">The AnimationCurve that dictates movement over time, if _moveMethod = MoveMethod.CustomCurve</param>
		 * <param name = "_retainPreviousSpeed">If True, and transitionTime > 0, then the previous _Camera's speed will influence the transition, allowing for a smoother effect</param>
		 * <param name = "snapCamera">If True, the new camera will snap to its target position instantly</param>
		 */
		public void SetGameCamera (_Camera newCamera, float transitionTime = 0f, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _animationCurve = null, bool _retainPreviousSpeed = false, bool snapCamera = true)
		{
			if (newCamera == null)
			{
				return;
			}
			
			if (KickStarter.eventManager) KickStarter.eventManager.Call_OnSwitchCamera (attachedCamera, newCamera, transitionTime);
			
			if (attachedCamera && attachedCamera is GameCamera25D)
			{
				transitionTime = 0f;
				
				if (newCamera is GameCamera25D)
				{ }
				else
				{
					RemoveBackground ();
				}
			}

			previousAttachedCamera = attachedCamera;

			oldCameraData = currentFrameCameraData;
			if (oldCameraData == null)
			{
				oldCameraData = new GameCameraData (this);
			}

			retainPreviousSpeed = (mainCameraMode == MainCameraMode.NormalSnap) ? _retainPreviousSpeed : false;

			Camera.ResetProjectionMatrix ();

			if (newCamera != attachedCamera && transitionTime > 0f)
			{
				transitionFromCamera = attachedCamera;
			}
			else
			{
				transitionFromCamera = null;
			}

			_attachedCamera = newCamera;

			if (KickStarter.stateHandler)
			{
				if (KickStarter.stateHandler.IsInGameplay ())
				{
					UpdateLastGameplayCamera ();
				}
				else
				{
					StartCoroutine (CheckGameStateNextFrame ());
				}
			}

			if (attachedCamera && attachedCamera.Camera)
			{
				Camera.farClipPlane = attachedCamera.Camera.farClipPlane;
				Camera.nearClipPlane = attachedCamera.Camera.nearClipPlane;
			}
			
			// Set background
			if (attachedCamera is GameCamera25D)
			{
				GameCamera25D cam25D = (GameCamera25D) attachedCamera;
				cam25D.SetActiveBackground ();
			}
			
			// TransparencySortMode
			if (UnityEngine.Rendering.GraphicsSettings.transparencySortMode == TransparencySortMode.Default)
			{
				Camera.transparencySortMode = attachedCamera.TransparencySortMode;
			}
			
			if (transitionTime > 0f)
			{
				SmoothChange (transitionTime, _moveMethod, _animationCurve);
			}
			else if (attachedCamera)
			{
				if (snapCamera)
				{
					attachedCamera.MoveCameraInstant ();
				}
				SnapToAttached ();
			}
		}
		

		/**
		 * <summary>Sets the MainCamera's fade texture</summary>
		 * <param name = "tex">The MainCamera's new fade Texture</param>
		 */
		public void SetFadeTexture (Texture2D tex)
		{
			if (tex)
			{
				tempFadeTexture = tex;
				AssignFadeTexture ();
			}
			else
			{
				ReleaseFadeTexture ();
			}
		}


		/**
		 * <summary>Fades the camera out with a custom texture.</summary>
		 * <param name = "_fadeDuration">The duration, in seconds, of the fade effect</param>
		 * <param name = "tempTex">The texture to display full-screen</param>
		 * <param name = "forceCompleteTransition">If True, the camera will be faded in instantly before beginning</param>
		 * <param name = "cameraFadePauseBehaviour">How to react to the game being paused (Cancel, Hide, Continue)</param>
		 * <param name="_fadeCurve">An animation curve used to describe the progress of the fade effect over time. The curve will be processed in normalised time - the _fadeDuration parameter is still used to determine the fade's duration</param>
		 */
		public void FadeOut (float _fadeDuration, Texture2D tempTex, bool forceCompleteTransition = true, CameraFadePauseBehaviour _cameraFadePauseBehaviour = CameraFadePauseBehaviour.Cancel, AnimationCurve _fadeCurve = null)
		{
			if (tempTex)
			{
				SetFadeTexture (tempTex);
			}
			FadeOut (_fadeDuration, forceCompleteTransition, _cameraFadePauseBehaviour, _fadeCurve);
		}


		/**
		 * <summary>Fades the camera in.</summary>
		 * <param name = "_fadeDuration">The duration, in seconds, of the fade effect</param>
		 * <param name = "forceCompleteTransition">If True, the camera will be faded out instantly before beginning</param>
		 * <param name = "cameraFadePauseBehaviour">How to react to the game being paused (Cancel, Hide, Continue)</param>
		 * * <param name="_fadeCurve">An animation curve used to describe the progress of the fade effect over time. The curve will be processed in normalised time - the _fadeDuration parameter is still used to determine the fade's duration</param>
		 */
		public virtual void FadeIn (float _fadeDuration, bool forceCompleteTransition = true, CameraFadePauseBehaviour _cameraFadePauseBehaviour = CameraFadePauseBehaviour.Cancel, AnimationCurve _fadeCurve = null)
		{
			AssignFadeTexture ();

			if (_fadeDuration <= 0) forceCompleteTransition = false;

			if ((forceCompleteTransition || alpha > 0f) && _fadeDuration > 0f)
			{
				fadeDuration = _fadeDuration;

				if (forceCompleteTransition)
				{
					alpha = 1f;
					fadeTimer = _fadeDuration;
				}
				else 
				{
					fadeTimer = _fadeDuration * alpha;
				}

				fadeType = FadeType.fadeIn;
				cameraFadePauseBehaviour = _cameraFadePauseBehaviour;

				fadeCurve = (_fadeCurve != null && forceCompleteTransition) ? _fadeCurve : new AnimationCurve (new Keyframe(0, 0, 1, 1), new Keyframe(1, 1, 1, 1));
			}
			else
			{
				alpha = 0f;
				fadeTimer = fadeDuration = 0f;
				ReleaseFadeTexture ();
			}
		}


		/**
		 * <summary>Fades the camera out.</summary>
		 * <param name = "_fadeDuration">The duration, in seconds, of the fade effect</param>
		 * <param name = "forceCompleteTransition">If True, the camera will be faded in instantly before beginning</param>
		 * <param name = "cameraFadePauseBehaviour">How to react to the game being paused (Cancel, Hide, Continue)</param>
		 * * <param name="_fadeCurve">An animation curve used to describe the progress of the fade effect over time. The curve will be processed in normalised time - the _fadeDuration parameter is still used to determine the fade's duration</param>
		 */
		public virtual void FadeOut (float _fadeDuration, bool forceCompleteTransition = true, CameraFadePauseBehaviour _cameraFadePauseBehaviour = CameraFadePauseBehaviour.Cancel, AnimationCurve _fadeCurve = null)
		{
			AssignFadeTexture ();
			
			if (alpha <= 0f)
			{
				alpha = 0.01f;
			}
			if ((forceCompleteTransition || alpha < 1f) && _fadeDuration > 0f)
			{
				if (forceCompleteTransition)
				{
					alpha = 0.01f;
					fadeTimer = _fadeDuration;
				}
				else
				{
					alpha = Mathf.Clamp01 (alpha);
					fadeTimer = _fadeDuration * (1f - alpha);
				}
				fadeDuration = _fadeDuration;
				fadeType = FadeType.fadeOut;
				cameraFadePauseBehaviour = _cameraFadePauseBehaviour;

				fadeCurve = (_fadeCurve != null && forceCompleteTransition) ? _fadeCurve : new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
			}
			else
			{
				alpha = 1f;
				fadeTimer = fadeDuration = 0f;
			}
		}
		

		/**
		 * <summary>Checks if the Camera is fading in our out.</summary>
		 * <returns>True if the Camera is fading in or out</returns>
		 */
		public virtual bool isFading ()
		{
			if (fadeTimer > 0f)
			{
				switch (fadeType)
				{
					case FadeType.fadeIn:
						return (alpha > 0f);

					case FadeType.fadeOut:
						return (alpha < 1f);

					default:
						break;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Converts a point in world space to one relative to the Camera's forward vector.</summary>
		 * <returns>A point in world space to one relative to the Camera's forward vector.</returns>
		 */
		public Vector3 PositionRelativeToCamera (Vector3 _position)
		{
			return (_position.x * ForwardVector ()) + (_position.z * RightVector ());
		}
		

		/**
		 * <summary>Gets the Camera's right vector.</summary>
		 * <returns>The Camera's right vector</returns>
		 */
		public static Vector3 RightVector ()
		{
			if (KickStarter.mainCamera)
			{
				return (KickStarter.mainCamera.forwardDirection == MainCameraForwardDirection.CameraComponent) ? KickStarter.mainCamera.ownCamera.transform.right : KickStarter.mainCamera.Transform.right;
			}
			return Camera.main.transform.right;
		}
		

		/**
		 * <summary>Gets the Camera's forward vector, not accounting for pitch.</summary>
		 * <returns>The Camera's forward vector, not accounting for pitch</returns>
		 */
		public static Vector3 ForwardVector ()
		{
			if (KickStarter.mainCamera)
			{
				Vector3 camForward = (KickStarter.mainCamera.forwardDirection == MainCameraForwardDirection.CameraComponent) ? KickStarter.mainCamera.ownCamera.transform.forward : KickStarter.mainCamera.Transform.forward;
				camForward.y = 0;
				return (camForward);
			}
			else
			{
				Vector3 camForward = Camera.main.transform.forward;
				camForward.y = 0;
				return (camForward);
			}
		}


		/**
		 * Updates the camera's rect values according to the aspect ratio and split-screen settings.
		 */
		public void SetCameraRect ()
		{
			if (!Application.isPlaying) return;

			if (SetAspectRatio () && Application.isPlaying)
			{
				CreateBorderCamera ();
			}

			if (isSplitScreen)
			{
				Camera.rect = GetSplitScreenRect (true);
			}
			else
			{
				Rect newRect = new Rect ();
				Rect safeRectRelative = new Rect (ACScreen.safeArea.x /  ACScreen.width,
												  ACScreen.safeArea.y /  ACScreen.height,
												  (ACScreen.safeArea.width) /  ACScreen.width,
												  (ACScreen.safeArea.height) /  ACScreen.height);
				if (borderOrientation == MenuOrientation.Vertical)
				{
					float scaledBorder = borderWidth * safeRectRelative.width;
					newRect = new Rect (scaledBorder + safeRectRelative.x, safeRectRelative.y, safeRectRelative.width - (2 * scaledBorder), safeRectRelative.height);
				}
				else if (borderOrientation == MenuOrientation.Horizontal)
				{
					float scaledBorder = borderWidth * safeRectRelative.height;
					newRect = new Rect (safeRectRelative.x, safeRectRelative.y + scaledBorder, safeRectRelative.width, safeRectRelative.height - (2 * scaledBorder));
				}
				
				Camera.rect = newRect;
			}

			if (KickStarter.stateHandler)
			{
				foreach (BackgroundCamera backgroundCamera in KickStarter.stateHandler.BackgroundCameras)
				{
					backgroundCamera.UpdateRect ();
				}
			}
		}


		/**
		 * <summary>Corrects a screen position Vector to account for the MainCamera's viewport Rect. This is necessary when positioning Unity UI RectTransforms while an aspect ratio is enforced, because the original screen position assumes a default Rect.</summary>
		 * <param name = "screenPosition">The screen position to correct.</param>
		 * <returns>The corrected screen position</returns>
		 */
		public Vector2 CorrectScreenPositionForUnityUI (Vector2 screenPosition)
		{
			return new Vector2 ((screenPosition.x * aspectRatioScaleCorrection.x) + aspectRatioOffsetCorrection.x, (screenPosition.y * aspectRatioScaleCorrection.y) + aspectRatioOffsetCorrection.y);
		}


		/**
		 * <summary>Gets the difference between the window size and the game's viewport.</summary>
		 * <returns>The difference between the window size and the game's viewport.</returns>
		 */
		public Vector2 GetWindowViewportDifference ()
		{
			return aspectRatioOffsetCorrection;
		}


		/**
		 * Draws any borders generated by a fixed aspect ratio, as set with forceAspectRatio in SettingsManager.
		 * This will be called every OnGUI call by StateHandler.
		 */
		public void DrawBorders ()
		{
			if (!Application.isPlaying)
			{
				if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().settingsManager == null || AdvGame.GetReferences ().settingsManager.AspectRatioEnforcement == AspectRatioEnforcement.NoneEnforced)
				{
					return;
				}
				SetAspectRatio ();
			}

			if (!renderBorders)
			{
				return;
			}

			if (borderWidth > 0f)
			{
				if (fadeTexture == null)
				{
					ACDebug.LogWarning ("Cannot draw camera borders because no Fade texture is assigned in the MainCamera!");
					return;
				}

				GUI.depth = 10;
				GUI.DrawTexture (borderRect1, fadeTexture);
				GUI.DrawTexture (borderRect2, fadeTexture);

			}
			else if (isSplitScreen)
			{
				if (splitOrientation != CameraSplitOrientation.Overlay)
				{
					if (fadeTexture == null)
					{
						ACDebug.LogWarning ("Cannot draw camera borders because no Fade texture is assigned in the MainCamera!", gameObject);
						return;
					}

					GUI.depth = 10;
					GUI.DrawTexture (midBorderRect, fadeTexture);
				}
			}

			// Safe area
			if (Application.isPlaying)
			{
				if (ACScreen.safeArea.x > 0f)
				{
					GUI.DrawTexture (new Rect (0f, 0f, ACScreen.safeArea.x,  ACScreen.height), fadeTexture);
				}
				if (ACScreen.safeArea.y > 0f)
				{
					GUI.DrawTexture (new Rect (0f, ACScreen.height - ACScreen.safeArea.y,  ACScreen.width, ACScreen.safeArea.y), fadeTexture);
				}
				if (ACScreen.safeArea.width < ( ACScreen.width - ACScreen.safeArea.x))
				{
					GUI.DrawTexture (new Rect (ACScreen.safeArea.x + ACScreen.safeArea.width, 0f,  ACScreen.width - ACScreen.safeArea.width,  ACScreen.height), fadeTexture);
				}
				if (ACScreen.safeArea.height < ( ACScreen.height - ACScreen.safeArea.y))
				{
					GUI.DrawTexture (new Rect (0f, 0f,  ACScreen.width,  ACScreen.height - ACScreen.safeArea.height - ACScreen.safeArea.y), fadeTexture);
				}
			}
		}
		

		/**
		 * <summary>Checks if the Camera uses orthographic perspective.</summary>
		 * <returns>True if the Camera uses orthographic perspective</returns>
		 */
		public bool IsOrthographic ()
		{
			if (Camera == null)
			{
				return false;
			}
			return Camera.orthographic;
		}

	
		/**
		 * <summary>Limits a point in screen-space to stay within the Camera's rect boundary, if forceAspectRatio in SettingsManager = True.</summary>
		 * <param name = "position">The original position in screen-space</param>
		 * <returns>The point, repositioned to stay within the Camera's rect boundary</returns>
		 */
		public Vector2 LimitToAspect (Vector2 position)
		{
			if (!KickStarter.cursorManager.keepCursorWithinScreen && KickStarter.playerCursor.LimitCursorToMenu == null)
			{
				return position;
			}

			if (KickStarter.settingsManager.AspectRatioEnforcement != AspectRatioEnforcement.NoneEnforced)
			{
				switch (borderOrientation)
				{
					case MenuOrientation.Horizontal:
						return LimitVector (position, 0f, borderWidth);

					case MenuOrientation.Vertical:
						return LimitVector (position, borderWidth, 0f);
				}
			}

			return LimitVector (position, 0f, 0f);
		}


		/**
		 * <summary>Checks if a point in screen-space is within the Camera's viewport</summary>
		 * <param name = "point">The point to check the position of</param>
		 * <returns>True if the point is within the Camera's viewport</returns>
		 */
		public bool IsPointInCamera (Vector2 point)
		{
			if (isSplitScreen)
			{
				point = new Vector2 (point.x /  ACScreen.width, point.y /  ACScreen.height);
				return Camera.rect.Contains (point);
			}
			return true;
		}


		/**
		 * <summary>Returns the amount by which the screen dimensions are offset as a result of an enforced aspect ratio.</param>
		 * <returns>The screen dimensions</returns>
		 */
		public Vector2 GetMainGameViewOffset ()
		{
			Vector2 safeOffset = new Vector2 (ACScreen.safeArea.x,  ACScreen.height - ACScreen.safeArea.height - ACScreen.safeArea.y);

			if (borderWidth > 0f)
			{
				if (borderOrientation == MenuOrientation.Horizontal)
				{
					// Letterbox
					safeOffset.y += ACScreen.safeArea.height * borderWidth;
				}
				else
				{
					// Pillarbox
					safeOffset.x += ACScreen.safeArea.width * borderWidth;
				}
			}
			return safeOffset;
		}
		

		/**
		 * <summary>Resizes an OnGUI Rect so that it fits within the Camera's rect, if forceAspectRatio = True in SettingsManager.</summary>
		 * <param name = "rect">The OnGUI Rect to resize</param>
		 * <returns>The resized OnGUI Rect</returns>
		 */
		public Rect LimitMenuToAspect (Rect rect)
		{
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.AspectRatioEnforcement == AspectRatioEnforcement.NoneEnforced)
			{
				return rect;
			}

			rect.position += ACScreen.safeArea.position;

			if (borderOrientation == MenuOrientation.Horizontal)
			{
				// Letterbox
				int yOffset = (int) ( ACScreen.height * borderWidth);
				
				if (rect.y < yOffset)
				{
					rect.y = yOffset;
					
					if (rect.height > ( ACScreen.height - yOffset - yOffset))
					{
						rect.height =  ACScreen.height - yOffset - yOffset;
					}
				}
				else if (rect.y + rect.height > ( ACScreen.height - yOffset))
				{
					rect.y =  ACScreen.height - yOffset - rect.height;
				}
			}
			else
			{
				// Pillarbox
				int xOffset = (int) ( ACScreen.width * borderWidth);
				
				if (rect.x < xOffset)
				{
					rect.x = xOffset;
					
					if (rect.width > ( ACScreen.width - xOffset - xOffset))
					{
						rect.width =  ACScreen.width - xOffset - xOffset;
					}
				}
				else if (rect.x + rect.width > ( ACScreen.width - xOffset))
				{
					rect.x =  ACScreen.width - xOffset - rect.width;
				}
			}
			
			return rect;
		}


		/**
		 * <summary>Creates a new split-screen effect.</summary>
		 * <param name = "_camera1">The first _Camera to use in the effect</param>
		 * <param name = "_camera2">The second _Camera to use in the effect</param>
		 * <param name = "_splitOrientation">How the two _Cameras are arranged (Horizontal, Vertical)</param>
		 * <param name = "_isTopLeft">If True, the MainCamera will take the position of _camera1</param>
		 * <param name = "_splitAmountMain">The proportion of the screen taken up by this Camera</param>
		 * <param name = "_splitAmountOther">The proportion of the screen take up by the other _Camera</param>
		 */
		public void SetSplitScreen (_Camera _camera1, _Camera _camera2, CameraSplitOrientation _splitOrientation, bool _isTopLeft, float _splitAmountMain, float _splitAmountOther)
		{
			splitCamera = _camera2;
			isSplitScreen = true;
			splitOrientation = _splitOrientation;
			isTopLeftSplit = _isTopLeft;
			
			SetGameCamera (_camera1);
			StartSplitScreen (_splitAmountMain, _splitAmountOther);
		}


		public void SwapSplitScreenMainCamera ()
		{
			SetSplitScreen (splitCamera, attachedCamera, splitOrientation, !isTopLeftSplit, splitAmountMain, splitAmountOther);
		}
		

		/**
		 * <summary>Adjusts the screen ratio of any active split-screen effect.</summary>
		 * <param name = "_splitAmountMain">The proportion of the screen taken up by this Camera</param>
		 * <param name = "_splitAmountOther">The proportion of the screen take up by the other _Camera</param>
		 */
		public void StartSplitScreen (float _splitAmountMain, float _splitAmountOther)
		{
			splitAmountMain = _splitAmountMain;
			splitAmountOther = _splitAmountOther;
			
			splitCamera.SetSplitScreen ();
			SetCameraRect ();
			SetMidBorder ();

			KickStarter.eventManager.Call_OnCameraSplitScreenStart (splitCamera, splitOrientation, splitAmountMain, splitAmountOther, isTopLeftSplit);
		}


		/**
		 * <summary>Create a new box-overlay effect</summary>
		 * <param name = "underlayCamera">The full-screen camera to go underneath the overlay</param>
		 * <param name = "overlayCamera">The camera to overlay</param>
		 * <param name = "_overlayRect">A Rect that describes the position and size of the overlay effect</param>
		 */
		public void SetBoxOverlay (_Camera underlayCamera, _Camera overlayCamera, Rect _overlayRect, bool useRectCentre = true)
		{
			if (underlayCamera == null)
			{
				ACDebug.LogWarning ("Cannot set box overlay because no underlay camera was set.");
				return;
			}

			if (overlayCamera == null)
			{
				ACDebug.LogWarning ("Cannot set box overlay because no overlay camera was set.");
				return;
			}

			splitCamera = underlayCamera;
			isSplitScreen = true;
			splitOrientation = CameraSplitOrientation.Overlay;
			midBorderRect = new Rect (0f, 0f, 0f, 0f);

			SetGameCamera (overlayCamera);

			overlayDepthBackup = Camera.depth;
			Camera.depth = underlayCamera.Camera.depth + 1f;
			overlayRect = _overlayRect;
			if (useRectCentre)
			{
				overlayRect.center = new Vector2 (_overlayRect.x, _overlayRect.y);
			}
			
			splitCamera.SetSplitScreen ();
			SetCameraRect ();
		}


		/** Ends any active split-screen effect. */
		public void RemoveSplitScreen ()
		{
			_Camera _splitCamera = isSplitScreen ? splitCamera : null;

			if (isSplitScreen && splitOrientation == CameraSplitOrientation.Overlay)
			{
				Camera.depth = overlayDepthBackup;
			}

			isSplitScreen = false;
			SetCameraRect ();
			
			if (splitCamera)
			{
				splitCamera.RemoveSplitScreen ();

				if (splitOrientation == CameraSplitOrientation.Overlay)
				{
					SetGameCamera (splitCamera);
				}

				splitCamera = null;
			}

			if (_splitCamera) 
			{
				KickStarter.eventManager.Call_OnCameraSplitScreenStop (_splitCamera);
			}
		}


		/**
		 * <summary>Gets a screen Rect of the split-screen camera.</summary>
		 * <param name = "isMainCamera">If True, then the Rect of the MainCamera's view will be returned. Otherwise, the Rect of the other split-screen _Camera's view will be returned</param>
		 * <returns>A screen Rect of the split-screen camera</returns>
		 */
		public Rect GetSplitScreenRect (bool isMainCamera)
		{
			Rect playableArea = GetPlayableScreenArea (true);

			if (splitOrientation == CameraSplitOrientation.Overlay)
			{
				if (isMainCamera)
				{
					return new Rect (playableArea.x + (playableArea.width * overlayRect.x),
									 playableArea.y + (playableArea.height * overlayRect.y),
									 playableArea.width * overlayRect.width,
									 playableArea.height * overlayRect.height);
				}

				return playableArea;
			}

			bool _isTopLeftSplit = (isMainCamera) ? isTopLeftSplit : !isTopLeftSplit;
			float split = (isMainCamera) ? splitAmountMain : splitAmountOther;

			Vector2 splitPosition = playableArea.position;
			if (splitOrientation == CameraSplitOrientation.Horizontal && _isTopLeftSplit)
			{
				splitPosition.y += (1f - split) * playableArea.height;
			}
			else if (splitOrientation == CameraSplitOrientation.Vertical && !_isTopLeftSplit)
			{
				splitPosition.x += (1f - split) * playableArea.width;
			}

			Vector2 splitSize = (splitOrientation == CameraSplitOrientation.Horizontal)
								? new Vector2 (playableArea.width, split * playableArea.height)
								: new Vector2 (split * playableArea.width, playableArea.height);

			return new Rect (splitPosition, splitSize);
		}


		/**
		 * <summary>Gets the current focal distance.</summary>
		 * <returns>The current focal distance</returns>
		 */
		public float GetFocalDistance ()
		{
			return focalDistance;
		}


		/**
		 * Disables the Camera and AudioListener.
		 */
		public void Disable ()
		{
			if (Camera)
			{
				Camera.enabled = false;
			}
			if (AudioListener)
			{
				AudioListener.enabled = false;
			}
		}
		

		/**
		 * Enables the Camera and AudioListener.
		 */
		public void Enable ()
		{
			if (Camera)
			{
				Camera.enabled = true;
			}
			if (AudioListener)
			{
				AudioListener.enabled = true;
			}
		}


		/**
		 * <summary>Checks if the Camera is enabled.</summary>
		 * returns>True if the Camera is enabled</returns>
		 */
		public bool IsEnabled ()
		{
			if (Camera)
			{
				return Camera.enabled;
			}
			return false;
		}
		

		/**
		 * <summary>Sets the GameObject's tag.</summary>
		 * <param name = "_tag">The tag to give the GameObject</param>
		 */
		public void SetCameraTag (string _tag)
		{
			if (Camera)
			{
				Camera.gameObject.tag = _tag;
			}
		}
		

		/**
		 * <summary>Sets the state of the AudioListener component.</summary>
		 * <param name = "state">If True, the AudioListener will be enabled. If False, it will be disabled.</param>
		 */
		public void SetAudioState (bool state)
		{
			if (AudioListener)
			{
				AudioListener.enabled = state;
			}
		}


		/**
		 * <summary>Gets the previously-used gameplay _Camera.</summary>
		 * <returns>The previously-used gameplay _Camera</returns>
		 */
		public _Camera GetLastGameplayCamera ()
		{
			if (lastNavCamera)
			{
				if (lastNavCamera2 && attachedCamera == lastNavCamera)
				{
					return lastNavCamera2;
				}
				else
				{
					return lastNavCamera;
				}
			}
			ACDebug.LogWarning ("Could not get the last gameplay camera - was it previously set?");
			return null;
		}


		/**
		 * <summary>Gets the current perspective offset, as set by a GameCamera2D.</summary>
		 * <returns>The current perspective offset, as set by a GameCamera2D.</returns>
		 */
		public Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}


		/**
		 * <summary>Saves data related to the camera.</summary>
		 * <param name = "playerData">The PlayerData class to update with current data</param>
		 * <returns>A PlayerData class, updated with current camera data</returns>
		 */
		public PlayerData SaveData (PlayerData playerData)
		{
			if (lastNavCamera)
			{
				bool ignoreReportMissing = (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && lastNavCamera is FirstPersonCamera);
				playerData.lastNavCamera = Serializer.GetConstantID (lastNavCamera.gameObject, !ignoreReportMissing);
			}
			if (lastNavCamera2)
			{
				bool ignoreReportMissing = (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && lastNavCamera2 is FirstPersonCamera);
				playerData.lastNavCamera2 = Serializer.GetConstantID (lastNavCamera2.gameObject, !ignoreReportMissing);
			}

			if (shakeIntensity > 0f)
			{
				playerData.shakeIntensity = shakeIntensity;
				playerData.shakeDuration = shakeDuration;
				playerData.shakeEffect = (int) shakeEffect;
			}
			else
			{
				playerData.shakeIntensity = 0f;
				playerData.shakeDuration = 0f;
				playerData.shakeEffect = 0;
				StopShaking ();
			}

			if (attachedCamera)
			{
				bool ignoreReportMissing = (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && attachedCamera is FirstPersonCamera);
				playerData.gameCamera = Serializer.GetConstantID (attachedCamera.gameObject, !ignoreReportMissing);

				if (attachedCamera.gameObject.scene != gameObject.scene && !attachedCamera.gameObject.IsPersistent ())
				{
					ACDebug.LogWarning ("Cannot save the active camera '" + attachedCamera.gameObject.name + "' as it is not in the active scene.", attachedCamera.gameObject);
				}

				playerData.mainCameraLocX = attachedCamera.Transform.position.x;
				playerData.mainCameraLocY = attachedCamera.Transform.position.y;
				playerData.mainCameraLocZ = attachedCamera.Transform.position.z;

				playerData.mainCameraRotX = attachedCamera.Transform.eulerAngles.x;
				playerData.mainCameraRotY = attachedCamera.Transform.eulerAngles.y;
				playerData.mainCameraRotZ = attachedCamera.Transform.eulerAngles.z;
			}
			else
			{
				playerData.mainCameraLocX = Transform.position.x;
				playerData.mainCameraLocY = Transform.position.y;
				playerData.mainCameraLocZ = Transform.position.z;

				playerData.mainCameraRotX = Transform.eulerAngles.x;
				playerData.mainCameraRotY = Transform.eulerAngles.y;
				playerData.mainCameraRotZ = Transform.eulerAngles.z;
			}

			playerData.isSplitScreen = isSplitScreen;
			if (isSplitScreen)
			{
				switch (splitOrientation)
				{
					case CameraSplitOrientation.Horizontal:
						playerData.splitIsVertical = false;
						playerData.overlayRectX = 0f;
						playerData.overlayRectY = 0f;
						playerData.overlayRectWidth = 0f;
						playerData.overlayRectHeight = 0f;
						playerData.isTopLeftSplit = isTopLeftSplit;
						playerData.splitAmountMain = splitAmountMain;
						playerData.splitAmountOther = splitAmountOther;
						break;

					case CameraSplitOrientation.Vertical:
						playerData.splitIsVertical = true;
						playerData.overlayRectX = 0f;
						playerData.overlayRectY = 0f;
						playerData.overlayRectWidth = 0f;
						playerData.overlayRectHeight = 0f;
						playerData.isTopLeftSplit = isTopLeftSplit;
						playerData.splitAmountMain = splitAmountMain;
						playerData.splitAmountOther = splitAmountOther;
						break;

					case CameraSplitOrientation.Overlay:
						playerData.splitIsVertical = false;
						playerData.overlayRectX = overlayRect.x;
						playerData.overlayRectY = overlayRect.y;
						playerData.overlayRectWidth = overlayRect.width;
						playerData.overlayRectHeight = overlayRect.height;
						playerData.isTopLeftSplit = false;
						playerData.splitAmountMain = overlayDepthBackup;
						playerData.splitAmountOther = 0f;
						break;
				}
				
				if (splitCamera && splitCamera.GetComponent <ConstantID>())
				{
					playerData.splitCameraID = splitCamera.GetComponent <ConstantID>().constantID;
				}
				else
				{
					playerData.splitCameraID = 0;
				}
			}

			return playerData;
		}


		/**
		 * <summary>Restores the camera state from saved data</summary>
		 * <param name = "playerData">The data class to load from</param>
		 * <param name = "snapCamera">If True, the active camera will be snapped to</param>
		 */
		public void LoadData (PlayerData playerData, bool snapCamera = true)
		{
			_Camera oldTransitionCamera = (IsInTransition ()) ? attachedCamera : null;

			if (isSplitScreen)
			{
				RemoveSplitScreen ();
			}

			StopShaking ();
			if (playerData.shakeIntensity > 0f && playerData.shakeDuration > 0f)
			{
				Shake (playerData.shakeIntensity, playerData.shakeDuration, (CameraShakeEffect) playerData.shakeEffect);
			}

			_Camera _attachedCamera = ConstantID.GetComponent <_Camera> (playerData.gameCamera);
			if (_attachedCamera)
			{
				if (attachedCamera != _attachedCamera)
				{
					snapCamera = true;
				}

				if (snapCamera)
				{
					_attachedCamera.MoveCameraInstant ();
					SetGameCamera (_attachedCamera);
				}
				else
				{
					SetGameCamera (_attachedCamera, 0f, MoveMethod.Linear, null, false, false);
				}
			}
			else if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && KickStarter.settingsManager.IsInFirstPerson ())
			{
				SetFirstPerson ();
			}
			else if (attachedCamera)
			{
				_attachedCamera = null;
			}

			lastNavCamera = ConstantID.GetComponent <_Camera> (playerData.lastNavCamera);
			lastNavCamera2 = ConstantID.GetComponent <_Camera> (playerData.lastNavCamera2);

			if (KickStarter.saveSystem.loadingGame == LoadingGame.No && !snapCamera && oldTransitionCamera && oldTransitionCamera == attachedCamera)
			{
				// Don't snap in this situation, caused when swapping player
			}
			else
			{
				ResetMoving ();
			}

			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				// When player-switching, must use GameCameras to set position

				#if ALLOW_VR
				if (!UnityEngine.XR.XRSettings.enabled || restoreTransformOnLoadVR) {
				#endif
					Transform.position = new Vector3 (playerData.mainCameraLocX, playerData.mainCameraLocY, playerData.mainCameraLocZ);
					Transform.eulerAngles = new Vector3 (playerData.mainCameraRotX, playerData.mainCameraRotY, playerData.mainCameraRotZ);
					ResetProjection ();
				#if ALLOW_VR
				}
				#endif
			}

			if (KickStarter.saveSystem.loadingGame == LoadingGame.No && !snapCamera && oldTransitionCamera && oldTransitionCamera == attachedCamera)
			{
				// Don't snap in this situation, caused when swapping player
			}
			else
			{
				SnapToAttached ();
			}

			isSplitScreen = playerData.isSplitScreen;
			if (isSplitScreen)
			{
				if (playerData.splitCameraID != 0)
				{
					_Camera _splitCamera = ConstantID.GetComponent <_Camera> (playerData.splitCameraID);
					if (_splitCamera)
					{
						splitCamera = _splitCamera;
					}
				}

				if (!Mathf.Approximately (playerData.overlayRectX, 0f) ||
					!Mathf.Approximately (playerData.overlayRectY, 0f) ||
					!Mathf.Approximately (playerData.overlayRectWidth, 0f) ||
					!Mathf.Approximately (playerData.overlayRectHeight, 0f))
				{
					overlayRect = new Rect (playerData.overlayRectX, playerData.overlayRectY, playerData.overlayRectWidth, playerData.overlayRectHeight);
					SetBoxOverlay (splitCamera, attachedCamera, overlayRect, false);
				}
				else
				{
					isTopLeftSplit = playerData.isTopLeftSplit;
					if (playerData.splitIsVertical)
					{
						splitOrientation = CameraSplitOrientation.Vertical;
					}
					else
					{
						splitOrientation = CameraSplitOrientation.Horizontal;
					}

					StartSplitScreen (playerData.splitAmountMain, playerData.splitAmountOther);
				}
			}
		}


		/** Displays information about the MainCamera section of the 'AC Status' box. */
		public void DrawStatus ()
		{
			if (IsEnabled ())
			{
				if (timelineOverride)
				{
					GUILayout.Label ("Current camera: Set by Timeline");
				}
				else if (attachedCamera)
				{
					if (GUILayout.Button ("Current camera: " + attachedCamera.gameObject.name))
					{
						#if UNITY_EDITOR
						EditorGUIUtility.PingObject (attachedCamera.gameObject);
						#endif
					}
				}
			}
			else
			{
				GUILayout.Label ("MainCamera: Disabled");
			}
		}


		/**
		 * <summary>Overrides the MainCamera's normal behaviour, using Timeline to control it.</summary>
		 * <param name = "cam1">The first AC camera in the Timeline track mix</param>
		 * <param name = "cam2">The second AC camera in the Timeline track mix</param>
		 * <param name = "cam2Weight">The weight of the second AC camera in the Timeline track mix</param>
		 * <param name = "shakeIntensity">The intensity of a camera shaking effect</param>
		 */
		public void SetTimelineOverride (_Camera cam1, _Camera cam2, float cam2Weight, float _shakeIntensity = 0f)
		{
			#if UNITY_EDITOR
			if ((!timelineOverride && !Application.isPlaying) || currentFrameCameraData == null)
			#else
			if (currentFrameCameraData == null)
			#endif
			{
				currentFrameCameraData = new GameCameraData (this);
			}
			
			if (!timelineOverride)
			{
				if (Application.isPlaying)
				{
					if (cam1) cam1.MoveCameraInstant ();
					if (cam2) cam2.MoveCameraInstant ();
				}
				timelineOverride = true;
			}

			if (_shakeIntensity > 0f)
			{
				shakeEffect = CameraShakeEffect.TranslateAndRotate;
				shakeIntensity = _shakeIntensity * 0.2f;
			}
			else
			{
				shakeIntensity = 0f;
			}
			shakeCurve = null;

			if (cam1 == null)
			{
				if (cam2 == null)
				{
					ReleaseTimelineOverride ();
					return;
				}

				// Blending in/out of a clip with no mix
				GameCameraData cameraData2 = new GameCameraData (cam2);
				ApplyCameraData (currentFrameCameraData, cameraData2, cam2Weight);
			}
			else
			{
				ApplyCameraData (cam1, cam2, cam2Weight);
			}
		}


		/**
		 * <summary>Ends Timeline's overriding of normal behaviour, so that the Main Camera resumes normal behaviour.</summary>
		 */
		public void ReleaseTimelineOverride ()
		{
			timelineOverride = false;

			shakeIntensity = 0f;

			#if UNITY_EDITOR
			if (!Application.isPlaying && currentFrameCameraData != null)
			{
				ApplyCameraData (currentFrameCameraData);
			}
			#endif
		}


		public void SetTimelineFadeOverride (Texture2D _timelineFadeTexture, float _timelineFadeWeight)
		{
			if (_timelineFadeTexture == null)
			{
				ReleaseTimelineFadeOverride ();
				return;
			}

			timelineFadeOverride = true;
			timelineFadeTexture = _timelineFadeTexture;
			timelineFadeWeight = _timelineFadeWeight;
		}


		public void ReleaseTimelineFadeOverride ()
		{
			timelineFadeOverride = false;
		}


		/**
		 * <summary>Gets a Rect that describes the area of the screen that the game is played in.  This accounts both for the "safe" area of the screen, as well as borders added to due an enforced aspect ratio.</summary>
		 * <param name = "relativeToScreenSize">If True, the returned Rect will be scaled relative to the screen's size</param>
		 * <param name = "invertY">If True, the Rect will be flipped along the Y-axis</param>
		 * <returns>The Rect that describes the area of the screen that the game is played in.</returns>
		 */
		public Rect GetPlayableScreenArea (bool relativeToScreenSize, bool invertY = false)
		{
			if (!Application.isPlaying)
			{
				RecalculateRects ();
			}
			else
			{
				#if UNITY_WEBGL && !UNITY_EDITOR
				if (playableScreenRect.width == 0 && Time.time > 0f) RecalculateRects ();
				#else
				if (playableScreenRect.width == 0) RecalculateRects ();
				#endif
			}
			if (relativeToScreenSize)
			{
				return (invertY) ? playableScreenRectRelativeInverted : playableScreenRectRelative;
			}
			return (invertY) ? playableScreenRectInverted : playableScreenRect;
		}


		/**
		 * <summary>Converts a screen-space position (e.g. the cursor position) into one in the same co-ordinate system as the AC menu system.</summary>
		 * <param name="point">The position to convert</param>
		 * <returns>The position, converted to use the same co-ordinate system as the AC menu system.  This will be relative to the screen's actual size.</returns>
		 */
		public static Vector2 ConvertToMenuSpace (Vector2 point)
		{
			Vector2 playablePoint = point;

			if (KickStarter.mainCamera)
			{
				playablePoint -= KickStarter.mainCamera.safeScreenRectInverted.position;
			}
			playablePoint.x /= ACScreen.safeArea.width /  ACScreen.width;
			playablePoint.y /= ACScreen.safeArea.height /  ACScreen.height;

			return new Vector2 (playablePoint.x /  ACScreen.width, playablePoint.y /  ACScreen.height);
		}


		/**
		 * <summary>Converts a screen-space position (relative to the screen's actual size) into one in the same co-ordinate system that Unity UI uses.</summary>
		 * <param name="point">The position to convert</param>
		 * <returns>The position, converted to use the same co-ordinate system as Unity UI.</returns>
		 */
		public static Vector2 ConvertRelativeScreenSpaceToUI (Vector2 point)
		{
			Vector2 uiPoint = new Vector2 (point.x, 1f - point.y);

			if (KickStarter.mainCamera)
			{
				uiPoint.x *= KickStarter.mainCamera.GetPlayableScreenArea (false).width;
				uiPoint.y *= KickStarter.mainCamera.GetPlayableScreenArea (false).height;
				uiPoint += KickStarter.mainCamera.GetPlayableScreenArea (false).position;
			}
			else
			{
				uiPoint.x *= ACScreen.safeArea.width;
				uiPoint.y *= ACScreen.safeArea.height;
				uiPoint += ACScreen.safeArea.position;
			}

			return uiPoint;
		}


		/** Checks if the camera is currently mid-transition between two GameCameras */
		public bool IsInTransition ()
		{
			return (transitionTimer > 0f);
		}

		#endregion


		#region ProtectedFunctions

		protected virtual void OnAfterChangeScene (LoadingGame loadingGame)
		{
			if (KickStarter.settingsManager.blackOutWhenInitialising)
			{
				ForceOverlayForFrames (4);
			}
		}


		protected virtual void RemoveBackground ()
		{
			Camera.clearFlags = CameraClearFlags.Skybox;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) != -1)
			{
				Camera.cullingMask = Camera.cullingMask & ~(1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
			}
		}


		protected void UpdateCameraFade ()
		{
			if (fadeTimer > 0f && overlayFrames <= 0)
			{
				fadeTimer -= (cameraFadePauseBehaviour == CameraFadePauseBehaviour.Continue) ? Time.unscaledDeltaTime : Time.deltaTime;
				float progress = 1f - (fadeTimer / fadeDuration);
				alpha = (fadeCurve != null && fadeCurve.length > 0) ? fadeCurve.Evaluate (progress * fadeCurve[fadeCurve.length - 1].time) : progress;
				
				if (fadeType == FadeType.fadeIn)
				{
					alpha = 1f - alpha;
				}
				
				alpha = Mathf.Clamp01 (alpha);
				
				if (fadeTimer <= 0f)
				{
					if (fadeType == FadeType.fadeIn)
					{
						alpha = 0f;
					}
					else
					{
						alpha = 1f;
					}
					
					fadeDuration = fadeTimer = 0f;
					StopCrossfade ();
				}
			}
		}


		protected void OnEnterGameState (GameState gameState)
		{
			if (gameState == GameState.Normal ||
				(gameState == GameState.DialogOptions && KickStarter.settingsManager.allowGameplayDuringConversations))
			{
				UpdateLastGameplayCamera ();
			}

			if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
			{
				if (gameState == GameState.Normal ||
					(gameState == GameState.DialogOptions && KickStarter.settingsManager.useFPCamDuringConversations))
				{
					SetFirstPerson ();
				}
			}
		}


		protected IEnumerator CheckGameStateNextFrame ()
		{
			yield return null;
			OnEnterGameState (KickStarter.stateHandler.gameState);
		}


		protected void UpdateLastGameplayCamera ()
		{ 
			if (attachedCamera)
			{
				if (lastNavCamera != attachedCamera)
				{
					lastNavCamera2 = lastNavCamera;
				}
					
				lastNavCamera = attachedCamera;
			}
		}


		protected void UpdateCameraTransition ()
		{
			if (transitionTimer > 0f)
			{
				transitionTimer -= Time.deltaTime;

				if (transitionTimer <= 0f)
				{
					ResetMoving ();
					return;
				}

				float transitionProgress = 1f - (transitionTimer / transitionDuration);
				
				if (retainPreviousSpeed && previousAttachedCamera)
				{
					oldCameraData = new GameCameraData (previousAttachedCamera);
				}
				
				GameCameraData attachedCameraData = new GameCameraData (attachedCamera);
				float timeValue = AdvGame.Interpolate (transitionProgress, moveMethod, timeCurve);
				currentFrameCameraData = oldCameraData.CreateMix (attachedCameraData, timeValue, (moveMethod == MoveMethod.Curved));
			}
		}


		protected IEnumerator StartCrossfade (object[] parms)
		{
			float _transitionDuration = (float) parms[0];

			_Camera _linkedCamera = (_Camera) parms[1];
			AnimationCurve _fadeCurve = (AnimationCurve) parms[2];
			
			yield return new WaitForEndOfFrame ();

			crossfadeTexture = new Texture2D (ACScreen.width, ACScreen.height);

			#if UNITY_EDITOR
			crossfadeTexture.ReadPixels (new Rect(0f, 0f, ACScreen.width-1, ACScreen.height-1), 0, 0, false);
			#else
			crossfadeTexture.ReadPixels (new Rect(0f, 0f, ACScreen.width, ACScreen.height), 0, 0, false);
			#endif

			if (KickStarter.settingsManager.linearColorTextures)
			{
				for (int y = 0; y < crossfadeTexture.height; y++)
				{
					for (int x = 0; x < crossfadeTexture.width; x++)
					{
						Color color = crossfadeTexture.GetPixel(x, y);
						crossfadeTexture.SetPixel (x, y, color.linear);
					}
				}
			}

			crossfadeTexture.Apply ();

			ResetMoving ();
			isCrossfading = true;
			SetGameCamera (_linkedCamera);
			FadeOut (0f);
			FadeIn (_transitionDuration, true, CameraFadePauseBehaviour.Cancel, _fadeCurve);
		}
		
		
		protected void SmoothChange (float _transitionDuration, MoveMethod method, AnimationCurve _timeCurve = null)
		{
			moveMethod = method;
			mainCameraMode = MainCameraMode.NormalTransition;
			StopCrossfade ();
			
			transitionTimer = transitionDuration = _transitionDuration;

			if (method == MoveMethod.CustomCurve)
			{
				timeCurve = _timeCurve;
			}
			else
			{
				timeCurve = null;
			}
		}


		protected void ReleaseFadeTexture ()
		{
			tempFadeTexture = null;
			AssignFadeTexture ();
		}
		
		
		protected void AssignFadeTexture ()
		{
			if (tempFadeTexture)
			{
				actualFadeTexture = tempFadeTexture;
			}
			else
			{
				actualFadeTexture = fadeTexture;
			}
		}


		protected void CalculatePlayableScreenArea ()
		{
			Rect trueSafeRect = new Rect (ACScreen.safeArea);
			safeScreenRectInverted = new Rect (new Vector2 (ACScreen.safeArea.x,  ACScreen.height - ACScreen.safeArea.y - ACScreen.safeArea.height), ACScreen.safeArea.size);

			if (borderWidth > 0f)
			{
				if (borderOrientation == MenuOrientation.Horizontal)
				{
					// Letterbox
					float yShift = borderWidth * ACScreen.safeArea.height;
					trueSafeRect.y += yShift;
					trueSafeRect.height -= (2f * yShift);
				}
				else if (borderOrientation == MenuOrientation.Vertical)
				{
					// Pillarbox
					float xShift = borderWidth * ACScreen.safeArea.width;
					trueSafeRect.x += xShift;
					trueSafeRect.width -= (2f * xShift);
				}
			}

			playableScreenRect = new Rect (trueSafeRect);
			playableScreenRectInverted = new Rect (new Vector2 (trueSafeRect.x,  ACScreen.height - trueSafeRect.y - trueSafeRect.height), trueSafeRect.size);

			playableScreenRectRelative = new Rect (playableScreenRect.x / ACScreen.width, playableScreenRect.y / ACScreen.height, playableScreenRect.width /  ACScreen.width, playableScreenRect.height /  ACScreen.height);
			playableScreenRectRelativeInverted = new Rect (playableScreenRectInverted.x /  ACScreen.width, playableScreenRectInverted.y /  ACScreen.height, playableScreenRectInverted.width /  ACScreen.width, playableScreenRectInverted.height /  ACScreen.height);

			playableScreenDiagonalLength = Mathf.Sqrt ((playableScreenRect.width * playableScreenRect.width) + (playableScreenRect.height * playableScreenRect.height));
		}


		protected bool SetAspectRatio ()
		{
			float currentAspectRatio = 0f;
			Vector2 screenSize = new Vector2 ( ACScreen.width,  ACScreen.height);
			Vector2 safeScreenSize = ACScreen.safeArea.size;

			Vector2 safeRectOffset = new Vector2 (ACScreen.safeArea.x, screenSize.y - ACScreen.safeArea.height - ACScreen.safeArea.y);
			
			if (Screen.orientation == ScreenOrientation.LandscapeRight || Screen.orientation == ScreenOrientation.LandscapeLeft)
			{
				currentAspectRatio = safeScreenSize.x / safeScreenSize.y;
			}
			else
			{
				#if UNITY_IPHONE
				if (safeScreenSize.y > safeScreenSize.x && KickStarter.settingsManager.landscapeModeOnly)
				{
					currentAspectRatio = safeScreenSize.y / safeScreenSize.x;
				}
				else
				#endif
				{
					currentAspectRatio = safeScreenSize.x / safeScreenSize.y;
				}
			}

			// If the current aspect ratio is already approximately equal to the desired aspect ratio, use a full-screen Rect (in case it was set to something else previously)
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.AspectRatioEnforcement == AspectRatioEnforcement.NoneEnforced || Mathf.Approximately (currentAspectRatio, KickStarter.settingsManager.wantedAspectRatio))
			{
				borderWidth = 0f;
				borderOrientation = MenuOrientation.Horizontal;
				
				if (borderCam) 
				{
					Destroy (borderCam.gameObject);
				}
				return false;
			}

			if (KickStarter.settingsManager.AspectRatioEnforcement == AspectRatioEnforcement.Range && currentAspectRatio > KickStarter.settingsManager.maxAspectRatio)
			{
				borderWidth = 1f - KickStarter.settingsManager.maxAspectRatio / currentAspectRatio;

				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Vertical;
				borderRect1 = new Rect (0, 0, borderWidth * ACScreen.safeArea.width, ACScreen.safeArea.height);
				borderRect2 = new Rect (ACScreen.safeArea.width * (1f - borderWidth), 0f, borderWidth * ACScreen.safeArea.width, ACScreen.safeArea.height);
			}
			else if (currentAspectRatio > KickStarter.settingsManager.wantedAspectRatio)
			{
				// Pillarbox
					
				if (KickStarter.settingsManager.AspectRatioEnforcement == AspectRatioEnforcement.Range)
				{
					borderWidth = 0f;
				}
				else
				{
					borderWidth = 1f - KickStarter.settingsManager.wantedAspectRatio / currentAspectRatio;
				}

				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Vertical;
				borderRect1 = new Rect (0, 0, borderWidth * ACScreen.safeArea.width, ACScreen.safeArea.height);
				borderRect2 = new Rect (ACScreen.safeArea.width * (1f - borderWidth), 0f, borderWidth * ACScreen.safeArea.width, ACScreen.safeArea.height);
			}
			else
			{
				// Letterbox

				borderWidth = 1f - currentAspectRatio / KickStarter.settingsManager.wantedAspectRatio;
				borderWidth /= 2f;
				borderOrientation = MenuOrientation.Horizontal;
				borderRect1 = new Rect (0, 0, ACScreen.safeArea.width, borderWidth * ACScreen.safeArea.height);
				borderRect2 = new Rect (0, ACScreen.safeArea.height * (1f - borderWidth), ACScreen.safeArea.width, borderWidth * ACScreen.safeArea.height);
			}

			borderRect1.position += safeRectOffset;
			borderRect2.position += safeRectOffset;

			return true;
		}
		

		protected void CalculateUnityUIAspectRatioCorrection ()
		{
			if (!Application.isPlaying) return;

			Vector2 _screenSize = GetPlayableScreenArea (false).size;
			Vector2 windowSize = ACScreen.safeArea.size;

			aspectRatioScaleCorrection = new Vector2 (_screenSize.x / windowSize.x, _screenSize.y / windowSize.y);
			aspectRatioOffsetCorrection = new Vector2 ((windowSize.x - _screenSize.x) / 2f, (windowSize.y - _screenSize.y) / 2f);
		}


		protected void CreateBorderCamera ()
		{
			if (!borderCam && Application.isPlaying && KickStarter.settingsManager.renderBorderCamera)
			{
				// Make a new camera behind the normal camera which displays black; otherwise the unused space is undefined
				borderCam = new GameObject ("BorderCamera", typeof (Camera)).GetComponent <Camera>();
				borderCam.transform.parent = Transform;
				borderCam.depth = int.MinValue;
				borderCam.clearFlags = CameraClearFlags.SolidColor;
				borderCam.backgroundColor = Color.black;
				borderCam.cullingMask = 0;
			}
		}
		

		protected Vector2 LimitVector (Vector2 point, float xBorder, float yBorder)
		{
			if (KickStarter.playerCursor.LimitCursorToMenu != null)
			{
				if (KickStarter.playerCursor.LimitCursorToMenu.IsUnityUI ())
				{
					if (KickStarter.playerCursor.LimitCursorToMenu.RuntimeCanvas)
					{
						if (KickStarter.playerCursor.LimitCursorToMenu.RuntimeCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
						{
							RectTransform rectTransform = KickStarter.playerCursor.LimitCursorToMenu.rectTransform;
							Vector2 size = Vector2.Scale (rectTransform.rect.size, rectTransform.lossyScale);
							Rect menuRect = new Rect (rectTransform.position.x, ACScreen.height - rectTransform.position.y, size.x, size.y);
							menuRect.x -= (rectTransform.pivot.x * size.x);
							menuRect.y -= ((1.0f - rectTransform.pivot.y) * size.y);

							point.x = Mathf.Clamp (point.x, menuRect.x, menuRect.x + menuRect.width);
							point.y = Mathf.Clamp (point.y, ACScreen.height - menuRect.y - menuRect.height, ACScreen.height - menuRect.y);
						}
						else
						{
							ACDebug.LogWarning ("Cannot limit the cursor's position within the boundary of Menu " + KickStarter.playerCursor.LimitCursorToMenu.RuntimeCanvas + " as it is not set to the 'Screen Space Overlay' render mode!", KickStarter.playerCursor.LimitCursorToMenu.RuntimeCanvas);
						}
					}
				}
				else
				{
					Rect menuRect = KickStarter.playerCursor.LimitCursorToMenu.GetRect ();
					point.x = Mathf.Clamp (point.x, menuRect.x, menuRect.x + menuRect.width);
					point.y = Mathf.Clamp (point.y, ACScreen.height - menuRect.y - menuRect.height,  ACScreen.height - menuRect.y);
				}
			}

			// Pillarbox
			int xOffset = (int) (ACScreen.safeArea.width * xBorder);

			if (point.x <= xOffset + ACScreen.safeArea.x)
			{
				point.x = xOffset + 1 + ACScreen.safeArea.x;
			}
			else if (point.x >= (ACScreen.safeArea.width - xOffset + ACScreen.safeArea.x))
			{
				point.x = ACScreen.safeArea.width - xOffset - 1 + ACScreen.safeArea.x;
			}

			// Letterbox
			int yOffset = (int) (ACScreen.safeArea.height * yBorder);

			if (point.y <= yOffset + ACScreen.safeArea.y)
			{
				point.y = yOffset + 1 + ACScreen.safeArea.y;
			}
			else if (point.y >= (ACScreen.safeArea.height - yOffset + ACScreen.safeArea.y))
			{
				point.y = ACScreen.safeArea.height - yOffset - 1 + ACScreen.safeArea.y;
			}

			return point;
		}


		protected void SetMidBorder ()
		{
			if (borderWidth <= 0f && (splitAmountMain + splitAmountOther) < 1f)
			{
				Vector2 screenSize = ACScreen.safeArea.size;

				if (splitOrientation == CameraSplitOrientation.Horizontal)
				{
					if (isTopLeftSplit)
					{
						midBorderRect = new Rect (0f, screenSize.y * splitAmountMain, screenSize.x, screenSize.y * (1f - splitAmountOther - splitAmountMain));
					}
					else
					{
						midBorderRect = new Rect (0f, screenSize.y * splitAmountOther, screenSize.x, screenSize.y * (1f - splitAmountOther - splitAmountMain));
					}
				}
				else
				{
					if (isTopLeftSplit)
					{
						midBorderRect = new Rect (screenSize.x * splitAmountMain, 0f, screenSize.x * (1f - splitAmountOther - splitAmountMain), screenSize.y);
					}
					else
					{
						midBorderRect = new Rect (screenSize.x * splitAmountOther, 0f, screenSize.x * (1f - splitAmountOther - splitAmountMain), screenSize.y);
					}
				}
			}
			else
			{
				midBorderRect = new Rect (0f, 0f, 0f, 0f);
			}
		}


		protected void ApplyCameraData (_Camera _camera1, _Camera _camera2, float camera2Weight, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _timeCurve = null)
		{
			if (_camera1 == null)
			{
				ApplyCameraData (new GameCameraData (_camera2));
				return;
			}

			if (_camera2 == null)
			{
				ApplyCameraData (new GameCameraData (_camera1));
				return;
			}

			GameCameraData cameraData1 = new GameCameraData (_camera1);
			GameCameraData cameraData2 = new GameCameraData (_camera2);

			ApplyCameraData (cameraData1, cameraData2, camera2Weight, _moveMethod, _timeCurve);
		}


		protected void ApplyCameraData (GameCameraData cameraData1, GameCameraData cameraData2, float camera2Weight, MoveMethod _moveMethod = MoveMethod.Linear, AnimationCurve _timeCurve = null)
		{
			float timeValue = AdvGame.Interpolate (camera2Weight, _moveMethod, _timeCurve);

			GameCameraData mixedData = cameraData1.CreateMix (cameraData2, timeValue, _moveMethod == MoveMethod.Curved);
			ApplyCameraData (mixedData);
		}


		protected void ApplyCameraData (GameCameraData cameraData)
		{
			if (cameraData.is2D)
			{
				perspectiveOffset = cameraData.perspectiveOffset;
				Camera.ResetProjectionMatrix ();
			}

			Transform.position = cameraData.position;
			Transform.rotation = cameraData.rotation;
			Camera.orthographic = cameraData.isOrthographic;
			Camera.fieldOfView = cameraData.fieldOfView;
			Camera.orthographicSize = cameraData.orthographicSize;
			focalDistance = cameraData.focalDistance;

			if (cameraData.is2D && !cameraData.isOrthographic)
			{
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
			}

			#if ALLOW_PHYSICAL_CAMERA
			Camera.usePhysicalProperties = cameraData.usePhysicalProperties;
			Camera.sensorSize = cameraData.sensorSize;
			Camera.lensShift = cameraData.lensShift;
			#endif
		}

		#endregion


		#region StaticFunctions

		public static bool AllowProjectionShifting (Camera _camera)
		{
			#if ALLOW_PHYSICAL_CAMERA
			return false;
			#else
			return (!_camera.orthographic);
			#endif
		}

		#endregion


		#region GetSet

		/**
		 * The MainCamera's Camera component.
		 */
		public Camera Camera
		{
			get
			{
				if (ownCamera == null)
				{
					ownCamera = GetComponent <Camera>();

					if (ownCamera == null)
					{
						ownCamera = GetComponentInChildren <Camera>();

						if (ownCamera == null)
						{
							ACDebug.LogError ("The MainCamera script requires a Camera component.", gameObject);
						}
					}
				}
				return ownCamera;
			}
		}


		/** A cache of the MainCamera's transform component */
		public Transform Transform
		{
			get
			{
				if (_transform == null) _transform = transform;
				return _transform;
			}
		}


		/** The current active camera, i.e. the one that the MainCamera is attaching itself to */
		public _Camera attachedCamera
		{
			get
			{
				return _attachedCamera;
			}
		}


		protected AudioListener AudioListener
		{
			get
			{
				if (_audioListener == null)
				{
					_audioListener = GetComponent <AudioListener>();

					if (_audioListener == null && Camera)
					{
						_audioListener = Camera.GetComponent <AudioListener>();
					}

					if (_audioListener == null)
					{
						ACDebug.LogWarning ("No AudioListener found on the MainCamera!", gameObject);
					}
				}
				return _audioListener;
			}
		}


		/** The distance between opposing corners of the playable screen area */
		public float PlayableScreenDiagonalLength
		{
			get
			{
				return playableScreenDiagonalLength;
			}
		}


		/** Data related to the MainCamera's currently-active camera and transition */
		public GameCameraData CurrentFrameCameraData
		{
			get
			{
				return currentFrameCameraData;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			CustomGUILayout.BeginVertical ();
			fadeTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> ("Fade texture:", fadeTexture, false, string.Empty, "The texture to display fullscreen when fading");
			renderFading = CustomGUILayout.Toggle ("Draw fade?", renderFading, string.Empty, "If True, the fade effect will be drawn automatically.");
			if (!renderFading)
			{
				EditorGUILayout.HelpBox ("A custom fade effect can be written by hooking into this component's GetFadeTexture and GetFadeAlpha functions.", MessageType.Info);
			}
			renderBorders = CustomGUILayout.Toggle ("Draw borders?", renderBorders, string.Empty, "If True, borders will be drawn outside of the playable screen area.");

			#if ALLOW_VR
			if (UnityEngine.XR.XRSettings.enabled)
			{
				restoreTransformOnLoadVR = CustomGUILayout.ToggleLeft ("Restore transform when loading?", restoreTransformOnLoadVR, string.Empty, "If True, the camera's position and rotation will be restored when loading (Hand for VR)");
			}
			#endif

			if (GetComponent <Camera>() == null)
			{
				forwardDirection = (MainCameraForwardDirection) EditorGUILayout.EnumPopup ("Facing direction:", forwardDirection);
			}

			CustomGUILayout.EndVertical ();

			if (Application.isPlaying)
			{
				CustomGUILayout.BeginVertical ();
				if (_attachedCamera)
				{
					_attachedCamera = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Attached camera:", attachedCamera, true, string.Empty, "The current active camera, i.e. the one that the MainCamera is attaching itself to");
				}
				else
				{
					EditorGUILayout.LabelField ("Attached camera: None");
				}
				CustomGUILayout.EndVertical ();
			}
		}

		#endif

	}
	
}