/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"GameCamera2D.cs"
 * 
 *	This GameCamera allows scrolling horizontally and vertically without altering perspective.
 *	Based on the work by Eric Haines (Eric5h5) at http://wiki.unity3d.com/index.php?title=OffsetVanishingPoint
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * The standard 2D camera. It can be scrolled horizontally and vertically without altering perspective (causing a "Ken Burns effect" if the camera uses Perspective projection.
	 * Based on the work by Eric Haines (Eric5h5) at http://wiki.unity3d.com/index.php?title=OffsetVanishingPoint
	 */
	[RequireComponent (typeof (Camera))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera2_d.html")]
	public class GameCamera2D : CursorInfluenceCamera
	{

		#region Variables

		/** If True, then horizontal panning is prevented */
		public bool lockHorizontal = true;
		/** If True, then vertical panning is prevented */
		public bool lockVertical = true;

		/** If True, then horizontal panning will be limited to minimum and maximum values */
		public bool limitHorizontal;
		/** If True, then vertical panning will be limited to minimum and maximum values */
		public bool limitVertical;

		/** If set, then the sprite's bounds will be used to set the horizontal and vertical limits, overriding constrainHorizontal and constrainVertical */
		public SpriteRenderer backgroundConstraint = null;
		/** If True, and backgroundConstraint is set, then the camera will zoom in to fit the background if it is too zoomed out to fit */
		public bool autoScaleToFitBackgroundConstraint = false;

		/** The lower and upper horizontal limits, if limitHorizontal = True */
		public Vector2 constrainHorizontal;
		/** The lower and upper vertical limits, if limitVertical = True */
		public Vector2 constrainVertical;

		/** The amount of freedom when tracking a target. Higher values will result in looser tracking */
		public Vector2 freedom = Vector2.zero;
		/** The follow speed when tracking a target */
		public float dampSpeed = 0.9f;

		/** The influence that the target's facing direction has on the tracking position */
		public Vector2 directionInfluence = Vector2.zero;
		/** The intended horizontal and vertical panning offsets */
		public Vector2 afterOffset = Vector2.zero;

		/** If True, the camera will only move in steps, as if snapping to a grid */
		public bool doSnapping = false;
		/** The step size when doSnapping is True */
		public float unitSnap = 0.1f;

		protected Vector2 perspectiveOffset = Vector2.zero;
		protected Vector3 originalPosition = Vector3.zero;
		protected Vector2 desiredOffset = Vector2.zero;
		protected bool haveSetOriginalPosition = false;
		private float lastOrthographicSize = 0f;

		protected LerpUtils.FloatLerp xLerp = new LerpUtils.FloatLerp ();
		protected LerpUtils.FloatLerp yLerp = new LerpUtils.FloatLerp ();

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			SetOriginalPosition ();
			base.Awake ();
		}


		protected override void OnEnable ()
		{
			EventManager.OnTeleport += OnTeleport;
			EventManager.OnUpdatePlayableScreenArea += OnUpdatePlayableScreenArea;
			base.OnEnable ();
		}


		protected override void OnDisable ()
		{
			EventManager.OnTeleport -= OnTeleport;
			EventManager.OnUpdatePlayableScreenArea -= OnUpdatePlayableScreenArea;
			base.OnDisable ();
		}


		protected override void Start ()
		{
			base.Start ();

			ResetTarget ();
			if (Target)
			{
				MoveCameraInstant ();
			}
		}


		public override void _Update ()
		{
			if (Camera && Camera.orthographicSize != lastOrthographicSize)
			{
				UpdateBackgroundConstraint ();
			}

			MoveCamera ();
		}

		#endregion


		#region PublicFunctions

		/** Force-sets the current position as its original position. This should not normally need to be called externally. */
		public void ForceRecordOriginalPosition ()
		{
			if (!haveSetOriginalPosition && backgroundConstraint && Camera.orthographic && ((limitHorizontal && !lockHorizontal) || (limitVertical && !lockVertical)) && Target)
			{
				bool clearX = limitHorizontal && !lockHorizontal;
				bool clearY = limitVertical && !lockVertical;
				Transform.position = new Vector3 (clearX ? 0f : Transform.position.x, clearY ? 0f : Transform.position.y, Transform.position.z);
			}

			originalPosition = Transform.position;
			haveSetOriginalPosition = true;
		}


		public override bool Is2D ()
		{
			return true;
		}


		public override void MoveCameraInstant ()
		{
			SetOriginalPosition ();

			if (!lockHorizontal || !lockVertical)
			{
				if (Target)
				{
					SetDesired ();
			
					if (!lockHorizontal)
					{
						perspectiveOffset.x = xLerp.Update (desiredOffset.x, desiredOffset.x, dampSpeed);
					}
				
					if (!lockVertical)
					{
						perspectiveOffset.y = yLerp.Update (desiredOffset.y, desiredOffset.y, dampSpeed);
					}
				}
				else if ((limitHorizontal || limitVertical) && Camera.orthographic)
				{
					Vector3 position = originalPosition;
					if (limitHorizontal && !lockHorizontal)
					{
						position.x = Mathf.Clamp (position.x, constrainHorizontal.x, constrainHorizontal.y);
					}
					if (limitVertical && !lockVertical)
					{
						position.y = Mathf.Clamp (position.y, constrainVertical.x, constrainVertical.y);
					}
					transform.position = position;
				}
			}

			SetProjection ();
		}


		/** Snaps the camera to its offset values and recalculates the camera's projection matrix. */
		public void SnapToOffset ()
		{
			perspectiveOffset = afterOffset;
			SetProjection ();
		}


		/** Sets the camera's rotation and projection according to the chosen settings in SettingsManager. */
		public void SetCorrectRotation ()
		{
			if (KickStarter.settingsManager)
			{
				if (SceneSettings.IsTopDown ())
				{
					Transform.rotation = Quaternion.Euler (90f, 0, 0);
					return;
				}

				if (SceneSettings.IsUnity2D ())
				{
					Camera.orthographic = true;
				}
			}

			Transform.rotation = Quaternion.Euler (0, 0, 0);
		}


		/**
		 * <summary>Checks if the GameObject's rotation matches the intended rotation, according to the chosen settings in SettingsManager.</summary>
		 * <returns>True if the GameObject's rotation matches the intended rotation<returns>
		 */
		public bool IsCorrectRotation ()
		{
			if (SceneSettings.IsTopDown ())
			{
				if (Transform.rotation == Quaternion.Euler (90f, 0f, 0f))
				{
					return true;
				}

				return false;
			}

			if (SceneSettings.CameraPerspective != CameraPerspective.TwoD)
			{
				return true;
			}

			if (Transform.rotation == Quaternion.Euler (0f, 0f, 0f))
			{
				return true;
			}

			return false;
		}


		public override Vector2 GetPerspectiveOffset ()
		{
			return GetSnapOffset ();
		}


		/**
		 * <summary>Sets the actual horizontal and vertical panning offsets. Be aware that the camera will still be subject to the movement set by the target, so it will move back to its original position afterwards unless you also change the target.</summary>
		 * <param name = "_perspectiveOffset">The new offsets</param>
		 */
		public void SetPerspectiveOffset (Vector2 _perspectiveOffset)
		{
			perspectiveOffset = _perspectiveOffset;
		}

		#endregion


		#region CustomEvents

		protected void OnTeleport (GameObject _gameObject)
		{
			if (gameObject == _gameObject)
			{
				ForceRecordOriginalPosition ();
			}
		}


		protected void OnUpdatePlayableScreenArea ()
		{
			UpdateBackgroundConstraint ();
		}

		#endregion


		#region ProtectedFunctions
		
		protected void UpdateBackgroundConstraint ()
		{
			lastOrthographicSize = Camera.orthographicSize;
			if (backgroundConstraint == null || Camera == null || !Camera.orthographic) return;
			if (!limitHorizontal && !limitVertical) return;
			if (lockHorizontal && lockVertical) return;

			Camera.enabled = true;

			Rect originalRect = Camera.pixelRect;
			if (KickStarter.CameraMain)
			{
				Camera.pixelRect = KickStarter.CameraMain.pixelRect;
			}

			Vector3 bottomLeftWorldPosition = Camera.ViewportToWorldPoint (new Vector3 (0f, 0f, Camera.nearClipPlane));
			Vector3 topRightWorldPosition = Camera.ViewportToWorldPoint (new Vector3 (1f, 1f, Camera.nearClipPlane));
			Camera.pixelRect = originalRect;
			
			Vector2 bottomLeftOffset = new Vector2 (Transform.position.x - bottomLeftWorldPosition.x, Transform.position.y - bottomLeftWorldPosition.y);
			Vector2 topRightOffset = new Vector2 (Transform.position.x - topRightWorldPosition.x, Transform.position.y - topRightWorldPosition.y);

			if (limitHorizontal)
			{
				Vector2 hLimits = new Vector2 (bottomLeftOffset.x + backgroundConstraint.bounds.min.x, topRightOffset.x + backgroundConstraint.bounds.max.x);
				constrainHorizontal = hLimits;
				float scaleFactor = (topRightWorldPosition.x - bottomLeftWorldPosition.x) / backgroundConstraint.bounds.size.x;
				if (scaleFactor > 1f)
				{
					constrainHorizontal.x = constrainHorizontal.y = backgroundConstraint.bounds.center.x;
					if (autoScaleToFitBackgroundConstraint)
					{
						ACDebug.Log ("GameCamera2D '" + gameObject.name + "' is zoomed out to much to fit the Horizontal background constraint - zooming in to compensate.", this);
						Camera.orthographicSize /= scaleFactor;
						lastOrthographicSize = Camera.orthographicSize;

						if (KickStarter.CameraMain)
						{
							Camera.pixelRect = KickStarter.CameraMain.pixelRect;
						}

						bottomLeftWorldPosition = Camera.ViewportToWorldPoint (new Vector3 (0f, 0f, Camera.nearClipPlane));
						topRightWorldPosition = Camera.ViewportToWorldPoint (new Vector3 (1f, 1f, Camera.nearClipPlane));
						Camera.pixelRect = originalRect;

						bottomLeftOffset = new Vector2 (Transform.position.x - bottomLeftWorldPosition.x, Transform.position.y - bottomLeftWorldPosition.y);
						topRightOffset = new Vector2 (Transform.position.x - topRightWorldPosition.x, Transform.position.y - topRightWorldPosition.y);
					}
					else
					{
						ACDebug.LogWarning ("Cannot properly set Horizontal constraint for GameCamera2D '" + gameObject.name + "' because the assigned background's width is less than the screen's width.", this);
					}
				}
			}

			if (limitVertical)
			{
				Vector2 vLimits = new Vector2 (bottomLeftOffset.y + backgroundConstraint.bounds.min.y, topRightOffset.y + backgroundConstraint.bounds.max.y);
				constrainVertical = vLimits;

				float scaleFactor = (topRightWorldPosition.y - bottomLeftWorldPosition.y) / backgroundConstraint.bounds.size.y;
				if (scaleFactor > 1f)
				{
					constrainVertical.x = constrainVertical.y = backgroundConstraint.bounds.center.y;
					if (autoScaleToFitBackgroundConstraint)
					{
						ACDebug.Log ("GameCamera2D '" + gameObject.name + "' is zoomed out to much to fit the Vertical background constraint - zooming in to compensate.", this);
						Camera.orthographicSize /= scaleFactor;
						lastOrthographicSize = Camera.orthographicSize;
					}
					else
					{
						ACDebug.LogWarning ("Cannot properly set Vertical constraint for GameCamera2D '" + gameObject.name + "' because the assigned background's height is less than the screen's height.", this);
					}
				}
			}
		
			MoveCameraInstant ();
			Camera.enabled = false;
		}


		protected void SetDesired ()
		{
			Vector2 targetOffset = GetOffsetForPosition (Target.position);
			if (targetOffset.x < (perspectiveOffset.x - freedom.x))
			{
				desiredOffset.x = targetOffset.x + freedom.x;
			}
			else if (targetOffset.x > (perspectiveOffset.x + freedom.x))
			{
				desiredOffset.x = targetOffset.x - freedom.x;
			}

			desiredOffset.x += afterOffset.x;
			if (!Mathf.Approximately (directionInfluence.x, 0f))
			{
				desiredOffset.x += Vector3.Dot (TargetForward, Transform.right) * directionInfluence.x;
			}

			if (limitHorizontal)
			{
				desiredOffset.x = ConstrainAxis (desiredOffset.x, constrainHorizontal);
			}
			
			if (targetOffset.y < (perspectiveOffset.y - freedom.y))
			{
				desiredOffset.y = targetOffset.y + freedom.y;
			}
			else if (targetOffset.y > (perspectiveOffset.y + freedom.y))
			{
				desiredOffset.y = targetOffset.y - freedom.y;
			}
			
			desiredOffset.y += afterOffset.y;
			if (!Mathf.Approximately (directionInfluence.y, 0f))
			{
				if (SceneSettings.IsTopDown ())
				{
					desiredOffset.y += Vector3.Dot (TargetForward, Transform.up) * directionInfluence.y;
				}
				else
				{
					desiredOffset.y += Vector3.Dot (TargetForward, Transform.forward) * directionInfluence.y;
				}
			}

			if (limitVertical)
			{
				desiredOffset.y = ConstrainAxis (desiredOffset.y, constrainVertical);
			}
		}	
		

		protected void MoveCamera ()
		{
			if (Target && (!lockHorizontal || !lockVertical))
			{
				SetDesired ();

				if (!lockHorizontal)
				{
					perspectiveOffset.x = (dampSpeed > 0f)
											? xLerp.Update (perspectiveOffset.x, desiredOffset.x, dampSpeed)
											: desiredOffset.x;
				}
				
				if (!lockVertical)
				{
					perspectiveOffset.y = (dampSpeed > 0f)
											? yLerp.Update (perspectiveOffset.y, desiredOffset.y, dampSpeed)
											: desiredOffset.y;
				}

			}
			else if (!Camera.orthographic)
			{
				SnapToOffset ();
			}
			
			SetProjection ();
		}


		protected void SetOriginalPosition ()
		{
			if (!haveSetOriginalPosition)
			{
				ForceRecordOriginalPosition ();
			}
		}
		

		protected void SetProjection ()
		{
			if (Target == null) return;

			Vector2 snapOffset = GetSnapOffset ();

			if (Camera.orthographic)
			{
				Transform.position = originalPosition + (Transform.right * snapOffset.x) + (Transform.up * snapOffset.y);
			}
			else
			{
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, snapOffset);
			}
		}


		protected Vector2 GetOffsetForPosition (Vector3 targetPosition)
		{
			Vector2 targetOffset = new Vector2 ();
			float forwardOffsetScale = 93 - (299 * Camera.nearClipPlane);

			if (SceneSettings.IsTopDown ())
			{
				if (Camera.orthographic)
				{
					targetOffset.x = Transform.position.x;
					targetOffset.y = Transform.position.z;
				}
				else
				{
					targetOffset.x = - (targetPosition.x - Transform.position.x) / (forwardOffsetScale * (targetPosition.y - Transform.position.y));
					targetOffset.y = - (targetPosition.z - Transform.position.z) / (forwardOffsetScale * (targetPosition.y - Transform.position.y));
				}
			}
			else
			{
				if (Camera.orthographic)
				{
					targetOffset = Transform.TransformVector (new Vector3 (targetPosition.x, targetPosition.y, -targetPosition.z));
				}
				else
				{
					float rightDot = Vector3.Dot (Transform.right, targetPosition - Transform.position);
					float forwardDot = Vector3.Dot (Transform.forward, targetPosition - Transform.position);
					float upDot = Vector3.Dot (Transform.up, targetPosition - Transform.position);

					targetOffset.x = rightDot / (forwardOffsetScale * forwardDot);
					targetOffset.y = upDot / (forwardOffsetScale * forwardDot);
				}
			}

			return targetOffset;
		}


		protected Vector2 GetSnapOffset ()
		{
			if (doSnapping)
			{
				Vector2 snapOffset = perspectiveOffset;
				snapOffset /= unitSnap;
				snapOffset.x = Mathf.Round (snapOffset.x);
				snapOffset.y = Mathf.Round (snapOffset.y);
				snapOffset *= unitSnap;
				return snapOffset;
			}
			return perspectiveOffset;
		}

		#endregion


		#if UNITY_EDITOR

		[ContextMenu ("Make active")]
		private void LocalMakeActive ()
		{
			MakeActive ();
		}

		#endif


		#region GetSet

		public override TransparencySortMode TransparencySortMode
		{
			get
			{
				return TransparencySortMode.Orthographic;
			}
		}

		#endregion

	}

}