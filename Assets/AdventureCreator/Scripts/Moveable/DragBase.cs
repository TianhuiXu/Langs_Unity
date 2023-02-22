/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"DragBase.cs"
 * 
 *	This the base class of draggable/pickup-able objects
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * The base class of objects that can be picked up and moved around with the mouse / touch.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_drag_base.html")]
	public abstract class DragBase : Moveable
	{

		#region Variables

		/** If assigned, then the draggable will only be interactive when the player is within this Trigger Collider's boundary */
		public InteractiveBoundary interactiveBoundary;
		/** If assigned, then the draggable will only be interactive when the assigned _Camera is active */
		public _Camera limitToCamera = null;
		
		protected bool isHeld = false;
		/** If True, input vectors will be inverted */
		public bool invertInput = false;
		/** The maximum force magnitude that can be applied to itself */
		public float maxSpeed = 200f;
		/** How much player movement is reduced by when the object is being dragged */
		public float playerMovementReductionFactor = 0f;
		/** The influence that player movement has on the drag force */
		public float playerMovementInfluence = 1f;

		/** If True, the object can be moved towards and away from the camera */
		public bool allowZooming = false;
		/** The speed at which the object can be moved towards and away from the camera (if allowZooming = True) */
		public float zoomSpeed = 60f;
		/** The minimum distance that there can be between the object and the camera (if allowZooming = True) */
		public float minZoom = 1f;
		/** The maxiumum distance that there can be between the object and the camera (if allowZooming = True) */
		public float maxZoom = 3f;
		/** The speed by which the object can be rotated */
		public float rotationFactor = 1f;

		/** If True, then an icon will be displayed at the "grab point" when the object is held */
		public bool showIcon = false;
		/** The ID number of the CursorIcon that gets shown if showIcon = true, as defined in CursorManager's cursorIcons List */
		public int iconID = -1;

		/** The sound to play when the object is moved */
		public AudioClip moveSoundClip;
		/** The sound to play when the object has a collision */
		public AudioClip collideSoundClip;

		/** The minimum speed that the object must be moving by for sound to play */
		public float slideSoundThreshold = 0.03f;
		/** The factor by which the movement sound's pitch is adjusted in relation to speed */
		public float slidePitchFactor = 1f;
		/** If True, then the collision sound will only play when the object collides with its lower boundary collider */
		public bool onlyPlayLowerCollisionSound = false;

		/** If True, then the Physics system will ignore collisions between this object and the boundary colliders of any DragTrack that this is not locked to */
		public bool ignoreMoveableRigidbodies;
		/** If True, then the Physics system will ignore collisions between this object and the player */
		public bool ignorePlayerCollider;
		/** If True, then this object's children will be placed on the same layer */
		public bool childrenShareLayer;
		/** What should cause the object to be automatically released if it leaves the screen */
		public OffScreenRelease offScreenRelease = OffScreenRelease.GrabPoint;

		protected Transform grabPoint;
		protected float distanceToCamera;
		
		protected float speedFactor = 0.16f;
		
		protected float originalDrag;
		protected float originalAngularDrag;

		protected int numCollisions = 0;

		protected CursorIconBase icon;
		protected Sound collideSound;

		/** The Sound component to play move sounds from */
		public Sound moveSound;
		protected bool isOn = true;

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			base.Awake ();

			GameObject newOb = new GameObject ();
			newOb.name = this.name + " (Grab point)";
			grabPoint = newOb.transform;
			grabPoint.parent = this.transform;

			if (moveSoundClip && moveSound == null)
			{
				GameObject newSoundOb = new GameObject ();
				newSoundOb.name = this.name + " (Move sound)";
				newSoundOb.transform.parent = this.transform;
				newSoundOb.AddComponent <Sound>();
				moveSound = newSoundOb.GetComponent <Sound>();
				moveSound.audioSource.playOnAwake = false;
				moveSound.audioSource.spatialBlend = SceneSettings.IsUnity2D () ? 0f : 1f;
			}

			icon = GetMainIcon ();

			collideSound = GetComponent <Sound>();
			if (collideSound == null)
			{
				collideSound = gameObject.AddComponent <Sound>();
			}
		}


		protected override void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
			base.OnEnable ();

			EventManager.OnSwitchCamera += OnSwitchCamera;
		}


		protected virtual void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected override void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
			base.OnDisable ();

			EventManager.OnSwitchCamera -= OnSwitchCamera;
		}


		/**
		 * Called every frame by StateHandler.
		 */
		public virtual void UpdateMovement ()
		{}


		public virtual void _FixedUpdate ()
		{}


		protected void OnCollisionExit (Collision collision)
		{
			if (KickStarter.player && collision.gameObject != KickStarter.player.gameObject)
			{
				numCollisions--;
			}
		}

		#endregion


		#region PublicFunctions

		public bool IsOn (bool accountForCamera = false)
		{
			if (this == null || gameObject == null) return false;

			if (gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer) && !isOn)
			{
				return false;
			}

			if (accountForCamera && limitToCamera && KickStarter.mainCamera && KickStarter.mainCamera.attachedCamera != limitToCamera)
			{
				return false;
			}

			return true;
		}


		/** Makes the object interactive.*/
		public void TurnOn ()
		{
			TurnOn (true);
      	}


		/**
		 * <summary>Makes the object interactive.</summary>
		 * <param name = "manualSet">If True, then the object will be considered 'On" when saving</param>
		 */
		public void TurnOn (bool manualSet)
		{
			gameObject.layer = LayerMask.NameToLayer(KickStarter.settingsManager.hotspotLayer);
			
			if (manualSet)
			{
				isOn = true;

				if (KickStarter.mainCamera)
				{
					LimitToActiveCamera (KickStarter.mainCamera.attachedCamera);
				}
			}
		}


		/** Makes the object non-interactive. */
		public void TurnOff ()
		{
			TurnOff (true);
		}


		/**
		 * <summary>Disables the Hotspot.</summary>
		 * <param name = "manualSet">If True, then the Hotspot will be considered 'Off" when saving</param>
		 */
		public void TurnOff (bool manualSet)
		{
			gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);

			if (manualSet)
			{
				isOn = false;
			}
		}


		/** If True, 'ToggleCursor' can be used while the object is held. */
		public virtual bool CanToggleCursor ()
		{
			return false;
		}


		/** Draws an icon at the point of contact on the object, if appropriate. */
		public virtual void DrawGrabIcon ()
		{
			if (isHeld && showIcon && KickStarter.CameraMain.WorldToScreenPoint (Transform.position).z > 0f && icon != null)
			{
				Vector3 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (grabPoint.position);
				icon.Draw (new Vector3 (screenPosition.x, screenPosition.y));
			}
		}


		/**
		 * <summary>Attaches the object to the player's control.</summary>
		 * <param name = "grabPosition">The point of contact on the object</param>
		 */
		public virtual void Grab (Vector3 grabPosition)
		{
			isHeld = true;
			grabPoint.position = grabPosition;
			originalDrag = _rigidbody.drag;
			originalAngularDrag = _rigidbody.angularDrag;
			_rigidbody.drag = 20f;
			_rigidbody.angularDrag = 20f;

			KickStarter.eventManager.Call_OnGrabMoveable (this);
		}


		/** Detaches the object from the player's control. */
		public virtual void LetGo (bool ignoreInteractions = false)
		{
			isHeld = false;
			KickStarter.eventManager.Call_OnDropMoveable (this);
		}


		/** Checks if the object can currently be grabbed */
		public bool CanGrab ()
		{
			if (IsOn (true) && PlayerIsWithinBoundary ())
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if the the point of contact is visible on-screen.</summary>
		 * <returns>True if the point of contact is visible on-screen.</returns>
		 */
		public bool IsOnScreen ()
		{
			switch (offScreenRelease)
			{
				case OffScreenRelease.GrabPoint:
					{
						Vector2 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (grabPoint.position);
						return KickStarter.mainCamera.GetPlayableScreenArea (false).Contains (screenPosition);
					}

				case OffScreenRelease.TransformCentre:
					{
						Vector2 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (Transform.position);
						return KickStarter.mainCamera.GetPlayableScreenArea (false).Contains (screenPosition);
					}

				case OffScreenRelease.DoNotRelease:
					return true;

				default:
					break;
			}
			
			return false;

			
		}


		/**
		 * <summary>Checks if the point of contact is close enough to the camera to continue being held.</summary>
		 * <param name = "maxDistance">The maximum-allowed distance between the point of contact and the camera</param>
		 */
		public bool IsCloseToCamera (float maxDistance)
		{
			if ((GetGrabPosition () - KickStarter.CameraMainTransform.position).magnitude < maxDistance)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Applies a drag force on the object, based on the movement of the cursor.</summary>
		 * <param name = "force">The force vector to apply</param>
		* <param name = "mousePosition">The position of the mouse</param>
		 * <param name = "distanceToCamera">The distance between the object's centre and the camera</param>
		 */
		public virtual void ApplyDragForce (Vector3 force, Vector3 mousePosition, float distanceToCamera)
		{}


		/**
		 * <summary>Checks if the Player is within the draggables's interactableBoundary, if assigned.</summary>
		 * <returns>True if the Player is within the draggables's interactableBoundary, if assigned.  If no InteractableBoundary is assigned, or there is no Player, then True will be returned.</returns>
		 */
		public bool PlayerIsWithinBoundary ()
		{
			if (interactiveBoundary == null || KickStarter.player == null)
			{
				return true;
			}

			return interactiveBoundary.PlayerIsPresent;
		}

		#endregion


		#region ProtectedFunctions

		protected void OnSwitchCamera (_Camera oldCamera, _Camera newCamera, float transitionTime)
		{
			if (limitToCamera == null) return;
			LimitToActiveCamera (newCamera);
		}


		protected void LimitToActiveCamera (_Camera _camera)
		{
			if (limitToCamera && _camera)
			{
				if (_camera == limitToCamera && isOn)
				{
					TurnOn (false);
				}
				else
				{
					TurnOff (false);
				}
			}
		}


		protected void PlaceOnLayer (int layerName)
		{
			gameObject.layer = layerName;
			if (childrenShareLayer)
			{
				foreach (Transform child in transform)
				{
					child.gameObject.layer = layerName;
				}
			}
		}


		protected void BaseOnCollisionEnter (Collision collision)
		{
			if (KickStarter.player && collision.gameObject != KickStarter.player.gameObject)
			{
				numCollisions ++;

				if (collideSound && collideSoundClip && Time.time > 0f)
				{
					collideSound.Play (collideSoundClip, false);
				}
			}
		}


		protected void PlayMoveSound (float speed)
		{
			if (slidePitchFactor > 0f)
			{
				float targetPitch = Mathf.Min (1f, speed * slidePitchFactor);
				moveSound.audioSource.pitch = Mathf.Lerp (moveSound.audioSource.pitch, targetPitch, Time.deltaTime * 3f);
			}

			if (speed > slideSoundThreshold)
			{
				if (!moveSound.IsPlaying ())
				{
					moveSound.Play (moveSoundClip, true);
				}
			}
			else if (moveSound.IsPlaying () && !moveSound.IsFading ())
			{
				moveSound.FadeOut (0.2f);
			}
		}


		protected void UpdateZoom ()
		{
			float zoom = Input.GetAxis ("ZoomMoveable");
			Vector3 moveVector = (Transform.position - KickStarter.CameraMainTransform.position).normalized;
			
			if (distanceToCamera - minZoom < 1f && zoom < 0f)
			{
				moveVector *= (distanceToCamera - minZoom);
			}
			else if (maxZoom - distanceToCamera < 1f && zoom > 0f)
			{
				moveVector *= (maxZoom - distanceToCamera);
			}

			if (_rigidbody)
			{
				if ((distanceToCamera < minZoom && zoom < 0f) || (distanceToCamera > maxZoom && zoom > 0f))
				{
					_rigidbody.AddForce (-moveVector * zoom * zoomSpeed);
					_rigidbody.velocity = Vector3.zero;
				}
				else
				{
					_rigidbody.AddForce (moveVector * zoom * zoomSpeed);
				}
			}
			else
			{
				if ((distanceToCamera < minZoom && zoom < 0f) || (distanceToCamera > maxZoom && zoom > 0f))
				{ }
				else
				{
					Transform.position += moveVector * zoom * zoomSpeed * Time.deltaTime;
				}
			}
		}


		protected void LimitZoom ()
		{
			if (distanceToCamera < minZoom)
			{
				Transform.position = KickStarter.CameraMainTransform.position + (Transform.position - KickStarter.CameraMainTransform.position) / (distanceToCamera / minZoom);
			}
			else if (distanceToCamera > maxZoom)
			{
				Transform.position = KickStarter.CameraMainTransform.position + (Transform.position - KickStarter.CameraMainTransform.position) / (distanceToCamera / maxZoom);
			}
		}


		protected CursorIconBase GetMainIcon ()
		{
			if (KickStarter.cursorManager == null || iconID < 0)
			{
				return null;
			}
			
			return KickStarter.cursorManager.GetCursorIconFromID (iconID);
		}


		protected void LimitCollisions ()
		{
			Collider[] ownColliders = GetComponentsInChildren<Collider> ();

			foreach (Collider _collider1 in ownColliders)
			{
				if (_collider1.isTrigger) continue;

				foreach (Collider _collider2 in ownColliders)
				{
					if (_collider2.isTrigger) continue;
					if (_collider1 == _collider2) continue;

					Physics.IgnoreCollision (_collider1, _collider2, true);
					Physics.IgnoreCollision (_collider1, _collider2, true);
				}

				if (ignorePlayerCollider && KickStarter.player)
				{
					Collider[] playerColliders = KickStarter.player.gameObject.GetComponentsInChildren<Collider> ();
					foreach (Collider playerCollider in playerColliders)
					{
						Physics.IgnoreCollision (playerCollider, _collider1, true);
					}
				}

				if (ignoreMoveableRigidbodies)
				{
					Collider[] allColliders = FindObjectsOfType (typeof (Collider)) as Collider[];
					foreach (Collider allCollider in allColliders)
					{
						if (allCollider == _collider1) continue;

						if (allCollider.GetComponent<Rigidbody> () && allCollider.gameObject != allCollider.gameObject)
						{
							if (allCollider.GetComponent<Moveable> ())
							{
								Physics.IgnoreCollision (allCollider, _collider1, true);
							}
						}
					}
				}
			}
		}

		#endregion


		#region GetSet

		/**
		 * <summary>Gets the point of contact on the object, once grabbed.</summary>
		 * <returns>The point of contact on the object, once grabbed</returns>
		 */
		public Vector3 GetGrabPosition ()
		{
			return grabPoint.position;
		}


		/** If True, the object is currently held by the player */
		public bool IsHeld
		{
			get
			{
				return isHeld;
			}
		}

		#endregion

	}

}