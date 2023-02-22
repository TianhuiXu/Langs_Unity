/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"_Camera.cs"
 * 
 *	This is the base class for all other cameras.
 * 
 */


using UnityEngine;

namespace AC
{

	/**
	 * The base class for all Adventure Creator cameras.
	 * To integrate a custom camera script to AC, just add this component to the same object as the Camera component, and it will be visible to AC's fields, functions and Actions.
	 */
	[AddComponentMenu("Adventure Creator/Camera/Basic camera")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1___camera.html")]
	public class _Camera : MonoBehaviour
	{

		#region Variables

		/** If True, the camera will move according to the Player prefab */
		public bool targetIsPlayer = true;
		/** The Transform that affects the camera's movement */
		public Transform target;
		/** If True, then the camera can be drag-controlled (used for GameCameraThirdPerson only) */
		public bool isDragControlled = false;
		/** The camera's focal distance */
		public float focalDistance = 10f;

		private Transform _transform;
		private Transform _cameraTransform;

		protected Char targetChar;
		protected Camera _camera;
		protected Vector2 inputMovement;

		[SerializeField] [HideInInspector] protected bool is2D = false;

		#endregion


		#region UnityStandards

		protected virtual void Awake ()
		{
			if (Camera /*&& Camera == GetComponent <Camera>()*/)
			{
				if (KickStarter.mainCamera)
				{
					Camera.enabled = false;
				}
			}

			SwitchTarget (target);
		}


		protected virtual void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected virtual void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected virtual void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Returns a vector by which to tweak the camera's rotation. The x-axis will control the spin, and the y-axis will control the pitch.</summary>
		 * <return>A vector by which to tweak the camera's rotation.</returns>
		 */
		public virtual Vector2 CreateRotationOffset ()
		{
			return Vector2.zero;
		}


		/**
		 * <summary>Switches the camera's target.</summary>
		 * <param name = "_target">The new target</param>
		 */
		public virtual void SwitchTarget (Transform _target)
		{
			target = _target;

			if (target)
			{
				targetChar = _target.GetComponent <Char>();
			}
			else
			{
				targetChar = null;
			}
		}


		/**
		 * <summary>Checks if the camera is for 2D games.  This is necessary for working out if the MainCamera needs to change its projection matrix.</summary>
		 * <returns>True if the camera is for 2D games</returns>
		 */
		public virtual bool Is2D ()
		{
			return is2D;
		}


		/**
		 * Updates the camera.
		 * This is called every frame by StateHandler.
		 */
		public virtual void _Update ()
		{}


		/** Auto-assigns "target" as the Player prefab Transform if targetIsPlayer = True. */
		public virtual void ResetTarget ()
		{
			if (targetIsPlayer && KickStarter.player)
			{
				SwitchTarget (KickStarter.player.Transform);
			}
		}


		/** Moves the camera instantly to its destination. */
		public virtual void MoveCameraInstant ()
		{ }



		/** Enables the camera for split-screen, using the MainCamera as the "main" part of the split, with all the data. */
		public void SetSplitScreen ()
		{
			Camera.enabled = true;
			Camera.rect = KickStarter.mainCamera.GetSplitScreenRect (false);
		}


		/** Removes the split-screen effect on this camera. */
		public void RemoveSplitScreen ()
		{
			if (Camera.enabled)
			{
				Camera.rect = new Rect (0f, 0f, 1f, 1f);
				Camera.enabled = false;
			}
		}


		/**
		 * <summary>Gets the actual horizontal and vertical panning offsets.</summary>
		 * <returns>The actual horizontal and vertical panning offsets</returns>
		 */
		public virtual Vector2 GetPerspectiveOffset ()
		{
			return Vector2.zero;
		}


		/**
		 * <summary>Checks if the Camera is currently the MainCamera's active camera (attachedCamera)</summary>
		 * <returns>True if the Camera is currently the MainCamera's active camera (attachedCamera)</returns>
		 */
		public bool IsActive ()
		{
			if (KickStarter.mainCamera)
			{
				return (KickStarter.mainCamera.attachedCamera == this);
			}
			return false;
		}

		#endregion


		#region ProtectedFunctions

		protected Vector3 PositionRelativeToCamera (Vector3 _position)
		{
			return (_position.x * ForwardVector ()) + (_position.z * RightVector ());
		}
		
		
		protected Vector3 RightVector ()
		{
			return (Transform.right);
		}
		
		
		protected Vector3 ForwardVector ()
		{
			Vector3 camForward;
			
			camForward = Transform.forward;
			camForward.y = 0;
			
			return (camForward);
		}
		

		protected float ConstrainAxis (float desired, Vector2 range, bool isAngle = false)
		{
			if (range.x > range.y)
			{
				range = new Vector2 (range.y, range.x);
			}

			if (isAngle)
			{
				if (desired > range.y + 180f)
				{
					desired -= 360f;
				}
				else if (desired < range.x - 180f)
				{
					desired += 360f;
				}
			}

			return Mathf.Clamp (desired, range.x, range.y);
		}

		#endregion


		#region GetSet

		/** The Transform that affects the camera's movement.  If targetIsPlayer = True, this will return the Player's Transform. */
		public Transform Target
		{
			get
			{
				if (targetIsPlayer)
				{
					if (KickStarter.player)
					{
						return KickStarter.player.Transform;
					}
					return null;
				}
				return target;
			}
		}


		/** The Unity Camera's Transform */
		public Transform CameraTransform
		{
			get
			{
				if (_cameraTransform == null) _cameraTransform = Camera.transform;
				return _cameraTransform;
			}
		}


		/** A cache of the object's transform component */
		public Transform Transform
		{
			get
			{
				if (_transform == null) _transform = transform;
				return _transform;
			}
		}


		/** The Unity Camera component */
		public Camera Camera
		{
			get
			{
				if (_camera == null)
				{
					_camera = GetComponent <Camera>();
					if (_camera == null)
					{
						_camera = GetComponentInChildren <Camera>();
					}
					if (_camera == null)
					{
						ACDebug.LogWarning (this.name + " has no Camera component!", this);
					}
				}
				return _camera;
			}
		}


		/**
		 * True if the game plays in 2D, making use of 2D colliders and raycasts
		 */
		public bool isFor2D
		{
			get
			{
				return is2D;
			}
			set
			{
				is2D = value;
			}
		}


		/** The camera's transparency sort mode.  This will be copied over to the MainCamera when switched to */
		public virtual TransparencySortMode TransparencySortMode
		{
			get
			{
				if (Camera.orthographic)
				{
					return TransparencySortMode.Orthographic;
				}
				return TransparencySortMode.Perspective;
			}
		}


		protected Vector3 TargetForward
		{
			get
			{
				if (targetChar)
				{
					return targetChar.TransformForward;
				}
				if (target)
				{
					return target.forward;
				}
				return Vector3.zero;
			}
		}


		/** If True, then the influence of the cursor (if any) will cause a change in position, rather than rotation */
		public virtual bool CursorOffsetForcesTranslation
		{
			get
			{
				return false;
			}
		}

		#endregion


		#if UNITY_EDITOR

		[ContextMenu ("Make active")]
		protected void MakeActive ()
		{
			if (Application.isPlaying)
			{
				if (KickStarter.mainCamera)
				{
					KickStarter.mainCamera.SetGameCamera (this);
				}
				else
				{
					ACDebug.LogWarning ("Cannot find a MainCamera in the scene!");
				}
			}
			else
			{
				ACDebug.Log ("Cannot switch active camera outside of Play mode.");
			}
		}

		#endif

	}

}
