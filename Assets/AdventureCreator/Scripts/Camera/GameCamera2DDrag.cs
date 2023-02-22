/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"GameCamera2DDrag.cs"
 * 
 *	This GameCamera allows for panning in 2D space by clicking and dragging.
 *	It is best used in games without Player movement, as the player will still move to the click point otherwise.
 * 
 */

using UnityEngine;

namespace AC
{

	/*
	 * This GameCamera allows for panning in 2D space by clicking and dragging.
	 * It is best used in games without Player movement, as the player will still move to the click point otherwise.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera2_d_drag.html")]
	public class GameCamera2DDrag : _Camera
	{

		#region Variables

		/** How X movement is affected (Free, Limited, Locked) */
		public RotationLock xLock;
		/** How Y movement is affected (Free, Limited, Locked) */
		public RotationLock yLock;

		/** The speed of X movement */
		public float xSpeed = 5f;
		/** The speed of Y movement */
		public float ySpeed = 5f;

		/** The acceleration of X movement */
		public float xAcceleration = 5f;
		/** The deceleration of X movement */
		public float xDeceleration = 5f;

		/** The acceleration of Y movement */
		public float yAcceleration = 5f;
		/** The deceleration of Y movement */
		public float yDeceleration = 5f;

		/** If True, then X movement will be inverted */
		public bool invertX;
		/** If True, then Y movement will be inverted */
		public bool invertY;

		/** The minimum X value, if xLock = RotationLock.Limited */
		public float minX;
		/** The maximum X value, if xLock = RotationLock.Limited */
		public float maxX;
		/** The minimum Y value, if yLock = RotationLock.Limited */
		public float minY;
		/** The maximum Y value, if yLock = RotationLock.Limited */
		public float maxY;

		/** The X offset */
		public float xOffset;
		/** The Y offset */
		public float yOffset;

		protected float deltaX;
		protected float deltaY;
		protected float xPos;
		protected float yPos;
		protected Vector2 perspectiveOffset;
		protected Vector3 originalPosition;

		protected bool _is2D;
		protected Vector2 lastMousePosition;
		protected Vector2 noInput = Vector2.zero;

		/** If set, then the sprite's bounds will be used to set the horizontal and vertical limits, overriding constrainHorizontal and constrainVertical */
		public SpriteRenderer backgroundConstraint = null;
		/** If True, and backgroundConstraint is set, then the camera will zoom in to fit the background if it is too zoomed out to fit */
		public bool autoScaleToFitBackgroundConstraint = false;
		private float lastOrthographicSize = 0f;

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			isDragControlled = true;
			targetIsPlayer = false;
			SetOriginalPosition ();

			if (KickStarter.settingsManager)
			{
				_is2D = SceneSettings.IsUnity2D ();
			}

			base.Awake ();
		}


		protected override void OnEnable ()
		{
			base.OnEnable ();
			EventManager.OnUpdatePlayableScreenArea += OnUpdatePlayableScreenArea;
		}


		protected override void OnDisable ()
		{
			EventManager.OnUpdatePlayableScreenArea -= OnUpdatePlayableScreenArea;
			base.OnDisable ();
		}


		public override void _Update ()
		{
			if (Camera && Camera.orthographicSize != lastOrthographicSize)
			{
				UpdateBackgroundConstraint ();
			}

			inputMovement = GetInputVector ();

			if (xLock != RotationLock.Locked)
			{
				if (Mathf.Approximately (inputMovement.x, 0f))
				{
					deltaX = Mathf.Lerp (deltaX, 0f, xDeceleration * Time.deltaTime);
				}
				else
				{
					float scaleFactor = Mathf.Abs (inputMovement.x) / 1000f;

					if (inputMovement.x > 0f)
					{
						deltaX = Mathf.Lerp (deltaX, xSpeed * scaleFactor, xAcceleration * Time.deltaTime * inputMovement.x);
					}
					else if (inputMovement.x < 0f)
					{
						deltaX = Mathf.Lerp (deltaX, -xSpeed * scaleFactor, xAcceleration * Time.deltaTime * -inputMovement.x);
					}
				}
				
				if (xLock == RotationLock.Limited)
				{
					if ((invertX && deltaX > 0f) || (!invertX && deltaX < 0f))
					{
						if (maxX - xPos < 5f)
						{
							deltaX *= (maxX - xPos) / 5f;
						}
					}
					else if ((invertX && deltaX < 0f) || (!invertX && deltaX > 0f))
					{
						if (minX - xPos > -5f)
						{
							deltaX *= (minX - xPos) / -5f;
						}
					}
				}
				
				if (invertX)
				{
					xPos += deltaX / 100f;
				}
				else
				{
					xPos -= deltaX / 100f;
				}
				
				if (xLock == RotationLock.Limited)
				{
					xPos = Mathf.Clamp (xPos, minX, maxX);
				}
			}

			if (yLock != RotationLock.Locked)
			{
				if (Mathf.Approximately (inputMovement.y, 0f))
				{
					deltaY = Mathf.Lerp (deltaY, 0f, yDeceleration * Time.deltaTime);
				}
				else
				{
					float scaleFactor = Mathf.Abs (inputMovement.y) / 1000f;

					if (inputMovement.y > 0f)
					{
						deltaY = Mathf.Lerp (deltaY, ySpeed * scaleFactor, yAcceleration * Time.deltaTime * inputMovement.y);
					}
					else if (inputMovement.y < 0f)
					{
						deltaY = Mathf.Lerp (deltaY, -ySpeed * scaleFactor, yAcceleration * Time.deltaTime * -inputMovement.y);
					}
				}
				
				if (yLock == RotationLock.Limited)
				{
					if ((invertY && deltaY > 0f) || (!invertY && deltaY < 0f))
					{
						if (maxY - yPos < 5f)
						{
							deltaY *= (maxY - yPos) / 5f;
						}
					}
					else if ((invertY && deltaY < 0f) || (!invertY && deltaY > 0f))
					{
						if (minY - yPos > -5f)
						{
							deltaY *= (minY - yPos) / -5f;
						}
					}
				}
				
				if (invertY)
				{
					yPos += deltaY / 100f;
				}
				else
				{
					yPos -= deltaY / 100f;
				}
				
				if (yLock == RotationLock.Limited)
				{
					yPos = Mathf.Clamp (yPos, minY, maxY);
				}
			}

			if (xLock != RotationLock.Locked)
			{
				perspectiveOffset.x = xPos + xOffset;
			}
			if (yLock != RotationLock.Locked)
			{
				perspectiveOffset.y = yPos + yOffset;
			}

			SetProjection ();
		}

		#endregion


		#region PublicFunctions

		public override bool Is2D ()
		{
			return _is2D;
		}


		public override Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}


		/**
		 * <summary>Sets the position to a specific point. This does not account for the offset, minimum or maximum values.</summary>
		 * <param name = "_position">The new position for the camera</param>
		 */
		public void SetPosition (Vector2 _position)
		{
			xPos = _position.x;
			yPos = _position.y;
		}


		/**
		 * <summary>Gets the camera's position, relative to its original position.</summary>
		 * <returns>The camera's position, relative to its original position</returns>
		 */
		public Vector2 GetPosition ()
		{
			return new Vector2 (xPos, yPos);
		}

		#endregion


		#region ProtectedFunctions

		protected virtual Vector2 GetInputVector ()
		{
			if (KickStarter.mainCamera && KickStarter.mainCamera.attachedCamera != this)
			{
				return noInput;
			}

			if (KickStarter.stateHandler.gameState != GameState.Normal)
			{
				return noInput;
			}
			else if (KickStarter.playerInput.GetDragState () == DragState._Camera)
			{
				return KickStarter.playerInput.GetDragVector () * Time.deltaTime * 50f;
			}
			else
			{
				return noInput;
			}
		}


		protected void SetProjection ()
		{
			if (!Camera.orthographic && _is2D)
			{
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
			}
			else
			{
				Transform.position = new Vector3 (originalPosition.x + perspectiveOffset.x, originalPosition.y + perspectiveOffset.y, originalPosition.z);
			}
		}


		protected void SetOriginalPosition ()
		{
			originalPosition = Transform.position;
		}


		protected void UpdateBackgroundConstraint ()
		{
			lastOrthographicSize = Camera.orthographicSize;
			if (backgroundConstraint == null || Camera == null || !Camera.orthographic) return;
			if (xLock != RotationLock.Limited && yLock != RotationLock.Limited) return;

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

			if (xLock == RotationLock.Limited)
			{
				Vector2 hLimits = new Vector2 (bottomLeftOffset.x + backgroundConstraint.bounds.min.x, topRightOffset.x + backgroundConstraint.bounds.max.x);
				minX = hLimits.x; maxX = hLimits.y;

				float scaleFactor = (topRightWorldPosition.x - bottomLeftWorldPosition.x) / backgroundConstraint.bounds.size.x;
				if (scaleFactor > 1f)
				{
					minX = maxX = backgroundConstraint.bounds.center.x;
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

			if (yLock == RotationLock.Limited)
			{
				Vector2 vLimits = new Vector2 (bottomLeftOffset.y + backgroundConstraint.bounds.min.y, topRightOffset.y + backgroundConstraint.bounds.max.y);
				minY = vLimits.x; maxY = vLimits.y;

				float scaleFactor = (topRightWorldPosition.y - bottomLeftWorldPosition.y) / backgroundConstraint.bounds.size.y;
				if (scaleFactor > 1f)
				{
					minY = maxY = backgroundConstraint.bounds.center.y;
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

		#endregion


		#region CustomEvents

		protected void OnUpdatePlayableScreenArea ()
		{
			UpdateBackgroundConstraint ();
		}

		#endregion

	}

}