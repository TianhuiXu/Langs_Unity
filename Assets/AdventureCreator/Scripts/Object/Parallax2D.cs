/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Parallax2D.cs"
 * 
 *	Attach this script to a GameObject when making a 2D game,
 *	to make it scroll as the camera moves.
 * 
 */

using UnityEngine;

namespace AC
{

	/** When used in 2D games, this script can be attached to scene objects to make them scroll as the camera moves, creating a parallax effect. */
	[AddComponentMenu("Adventure Creator/Misc/Parallax 2D")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_parallax2_d.html")]
	public class Parallax2D : MonoBehaviour
	{

		#region Variables

		/** The intensity of the depth effect. Positive values will make the GameObject appear further away (i.e. in the background), negative values will make it appear closer to the camera (i.e. in the foreground). */
		public float depth;
		/** If True, then the GameObject will scroll in the X-direction */
		public bool xScroll;
		/** If True, then the GameObject will scroll in the Y-direction */
		public bool yScroll;
		/** An offset for the GameObject's initial position along the X-axis */
		public float xOffset;
		/** An offset for the GameObject's initial position along the Y-axis */
		public float yOffset;
		
		/** If True, scrolling in the X-direction will be constrained */
		public bool limitX;
		/** The minimum scrolling position in the X-direction, if limitX = True */
		public float minX;
		/** The maximum scrolling position in the X-direction, if limitX = True */
		public  float maxX;

		/** If True, scrolling in the Y-direction will be constrained */
		public bool limitY;
		/** The minimum scrolling position in the Y-direction, if limitY = True */
		public float minY;
		/** The maximum scrolling position in the Y-direction, if limitY = True */
		public float maxY;

		/** What entity affects the parallax behaviour (Camera, Cursor, Transform) */
		public ParallaxReactsTo reactsTo = ParallaxReactsTo.Camera;
		/** Which GameObject affects behaviour, if reactsTo = ParallaxReactsTo.Transform */
		public Transform transformToReactTo;

		protected float xStart;
		protected float yStart;
		protected float xDesired;
		protected float yDesired;
		protected Vector2 perspectiveOffset;

		public HorizontalParallaxConstraint horizontalConstraint;
		public VerticalParallaxConstraint verticalConstraint;
		public SpriteRenderer backgroundConstraint = null;
		public enum HorizontalParallaxConstraint { Left, Right };
		public enum VerticalParallaxConstraint { Top, Bottom };

		private Transform _transform;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			Initialise ();
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

		/** Updates the GameObject's position according to the camera.  This is called every frame by the StateHandler. */
		public void UpdateOffset ()
		{
			switch (reactsTo)
			{
				case ParallaxReactsTo.Camera:
					if (KickStarter.mainCamera.attachedCamera is GameCamera2D && !KickStarter.mainCamera.attachedCamera.Camera.orthographic)
					{
						perspectiveOffset = KickStarter.mainCamera.GetPerspectiveOffset ();
					}
					else
					{
						perspectiveOffset = new Vector2 (KickStarter.CameraMainTransform.position.x, KickStarter.CameraMainTransform.position.y);
					}
					break;

				case ParallaxReactsTo.Cursor:
					Vector2 screenCentre = ACScreen.safeArea.size / 2f;
					Vector2 mousePosition = KickStarter.playerInput.GetMousePosition ();
					perspectiveOffset = new Vector2 (((1f - mousePosition.x) / screenCentre.x) + 1f, ((1f - mousePosition.y) / screenCentre.y + 1f));
					break;

				case ParallaxReactsTo.Transform:
					if (transformToReactTo)
					{
						perspectiveOffset = transformToReactTo.position;
					}
					break;

				default:
					break;
			}

			if (limitX)
			{
				perspectiveOffset.x = Mathf.Clamp (perspectiveOffset.x, minX, maxX);
			}
			if (limitY)
			{
				perspectiveOffset.y = Mathf.Clamp (perspectiveOffset.y, minY, maxY);
			}

			xDesired = xStart;
			if (xScroll)
			{
				if (limitX && reactsTo == ParallaxReactsTo.Camera && backgroundConstraint)
				{
					xDesired = GetHorizontalBackgroundConstraintPosition ();
				}
				else
				{
					xDesired += perspectiveOffset.x * depth;
					xDesired += xOffset;
				}
			}

			yDesired = yStart;
			if (yScroll)
			{
				if (limitY && reactsTo == ParallaxReactsTo.Camera && backgroundConstraint)
				{
					yDesired = GetVerticalBackgroundConstraintPosition ();
				}
				else
				{
					yDesired += perspectiveOffset.y * depth;
					yDesired += yOffset;
				}
			}

			if (xScroll && yScroll)
			{
				Transform.localPosition = new Vector3 (xDesired, yDesired, Transform.localPosition.z);
			}
			else if (xScroll)
			{
				Transform.localPosition = new Vector3 (xDesired, Transform.localPosition.y, Transform.localPosition.z);
			}
			else if (yScroll)
			{
				Transform.localPosition = new Vector3 (Transform.localPosition.x, yDesired, Transform.localPosition.z);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected virtual void Initialise ()
		{
			xStart = Transform.localPosition.x;
			yStart = Transform.localPosition.y;

			xDesired = xStart;
			yDesired = yStart;
		}


		private float GetHorizontalBackgroundConstraintPosition ()
		{
			if (!KickStarter.CameraMain.orthographic)
			{
				Debug.LogWarning (gameObject.name + " cannot use background for parallax constraint unless the Main Camera uses Orthographic projection.");
				return 0f;
			}

			switch (horizontalConstraint)
			{
				case HorizontalParallaxConstraint.Left:
					{
						float cameraLeft = KickStarter.CameraMain.ViewportToWorldPoint (new Vector3 (0f, 0f, KickStarter.CameraMain.nearClipPlane)).x;
						float backgroundLeft = backgroundConstraint.bounds.min.x;
						float scroll = Mathf.Max (cameraLeft - backgroundLeft, 0f);
						return backgroundConstraint.bounds.min.x + (scroll * depth) + xOffset;
					}

				case HorizontalParallaxConstraint.Right:
					{
						float cameraRight = KickStarter.CameraMain.ViewportToWorldPoint (new Vector3 (1f, 1f, KickStarter.CameraMain.nearClipPlane)).x;
						float backgroundRight = backgroundConstraint.bounds.max.x;
						float scroll = Mathf.Min (cameraRight - backgroundRight, 0f);
						return backgroundConstraint.bounds.max.x + (scroll * depth) + xOffset;
					}

				default:
					break;
			}

			return 0f;
		}


		private float GetVerticalBackgroundConstraintPosition ()
		{
			if (!KickStarter.CameraMain.orthographic)
			{
				Debug.LogWarning (gameObject.name + " cannot use background for parallax constraint unless the Main Camera uses Orthographic projection.");
				return 0f;
			}

			switch (verticalConstraint)
			{
				case VerticalParallaxConstraint.Top:
					{
						float cameraTop = KickStarter.CameraMain.ViewportToWorldPoint (new Vector3 (0f, 1f, KickStarter.CameraMain.nearClipPlane)).y;
						float backgroundTop = backgroundConstraint.bounds.max.y;
						float offset = Mathf.Min (cameraTop - backgroundTop, 0f);
						return backgroundConstraint.bounds.max.y - (offset * depth) + yOffset;
					}

				case VerticalParallaxConstraint.Bottom:
					{
						float cameraBottom = KickStarter.CameraMain.ViewportToWorldPoint (new Vector3 (1f, 0f, KickStarter.CameraMain.nearClipPlane)).y;
						float backgroundBottom = backgroundConstraint.bounds.min.y;
						float offset = Mathf.Max (cameraBottom - backgroundBottom, 0f);
						return backgroundConstraint.bounds.min.y - (offset * depth) + yOffset;
					}

				default:
					break;
			}

			return 0f;
		}

		#endregion


		#region GetSet

		private Transform Transform
		{
			get
			{
				if (_transform == null) _transform = transform;
				return _transform;
			}
		}

		#endregion

	}

}