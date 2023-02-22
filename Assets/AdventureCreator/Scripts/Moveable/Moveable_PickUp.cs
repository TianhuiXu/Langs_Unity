/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Moveable_PickUp.cs"
 * 
 *	Attaching this script to a GameObject allows it to be
 *	picked up and manipulated freely by the player.
 * 
 */

using UnityEngine;

namespace AC
{

	/** Attaching this component to a GameObject allows it to be picked up and manipulated freely by the player. */
	[RequireComponent (typeof (Rigidbody))]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable___pick_up.html")]
	[AddComponentMenu ("Adventure Creator/Misc/PickUp")]
	public class Moveable_PickUp : DragBase, iActionListAssetReferencer
	{

		#region Variables

		/** If True, the object can be rotated */
		public bool allowRotation = false;
		/** The maximum force magnitude that can be applied by the player - if exceeded, control will be removed */
		public float breakForce = 300f;
		/** If True, the object can be thrown */
		public bool allowThrow = false;
		/** How long a "charge" takes, if the object cen be thrown */
		public float chargeTime = 0.5f;
		/** How far the object is pulled back while chargine, if the object can be thrown */
		public float pullbackDistance = 0.6f;
		/** How far the object can be thrown */
		public float throwForce = 400f;
		/** If True, then Rigidbody constraints will be set automatically based on the interaction state */
		public bool autoSetConstraints = true;
		/** The maximum angular velocity of the Rigidbody, set if allowRotation = true */
		public float maxAngularVelocity = 7f;
		/** The minimum distance to keep from the camera */
		public float maxDistance = 1f;
		/** The minimum distance to keep from the camera */
		public float minDistance = 0.2f;

		/** Where to locate interactions */
		public ActionListSource actionListSource = ActionListSource.InScene;
		/** The Interaction to run whenever the object is picked up by the player */
		public Interaction interactionOnGrab;
		/** The Interaction to run whenever the object is let go by the player (and actionListSource = ActionListSource.InScene) */
		public Interaction interactionOnDrop = null;
		/** The ActionListAsset to run whenever the object is grabbed by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnGrab = null;
		/** The ActionListAsset to run whenever the object is let go by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnDrop = null;
		/** The parameter ID to set as this object in the interactionOnGrab / actionListAssetOnGrab ActionLists */
		public int moveParameterID = -1;
		/** The parameter ID to set as this object in the interactionOnDrop / actionListAssetOnDrop ActionLists */
		public int dropParameterID = -1;

		/** The lift to give objects picked up, so that they aren't touching the ground when initially held */
		public float initialLift = 0.05f;

		private const float movementFactor = 10f;

		protected bool isChargingThrow = false;
		protected float throwCharge = 0f;
		protected float chargeStartTime;
		protected bool inRotationMode = false;
		protected float originalDistanceToCamera;
		private Vector3 currentTorque;

		private Vector3 screenMousePosition;
		protected Vector3 worldMousePosition;
		protected Vector3 deltaMovement;
		protected LerpUtils.Vector3Lerp fixedJointLerp = new LerpUtils.Vector3Lerp ();

		private bool cursorUnlockedWhenRotate;
		private Vector2 lockedScreenOffset;
		private Vector2 startRotationMousePosition;
		private bool overrideMoveToPosition;
		private Vector3 moveToPositionOverride;

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			base.Awake ();

			if (_rigidbody == null)
			{
				ACDebug.LogWarning ("A Rigidbody component is required for " + name, this);
			}
			else if (allowRotation)
			{
				_rigidbody.maxAngularVelocity = maxAngularVelocity;
			}
		}


		protected override void Start ()
		{
			LimitCollisions ();
			base.Start ();
		}


		protected new void Update ()
		{
			if (!isHeld) return;

			if (allowThrow)
			{
				if (KickStarter.playerInput.InputGetButton ("ThrowMoveable"))
				{
					ChargeThrow ();
				}
				else if (isChargingThrow)
				{
					ReleaseThrow ();
				}
			}

			if (allowRotation)
			{
				if (KickStarter.playerInput.InputGetButton ("RotateMoveable"))
				{
					SetRotationMode (true);
				}
				else if (KickStarter.playerInput.InputGetButtonUp ("RotateMoveable"))
				{
					SetRotationMode (false);
					return;
				}

				if (KickStarter.playerInput.InputGetButtonDown ("RotateMoveableToggle"))
				{
					SetRotationMode (!inRotationMode);
					if (!inRotationMode)
					{
						return;
					}
				}
			}

			if (allowZooming)
			{
				UpdateZoom ();
			}
		}


		protected void LateUpdate ()
		{
			if (!isHeld) return;

			worldMousePosition = GetWorldMousePosition ();

			Vector3 deltaPositionRaw = (worldMousePosition - _rigidbody.position) * 100f;
			deltaMovement = Vector3.Lerp (deltaMovement, deltaPositionRaw, Time.deltaTime * 6f);
		}


		protected void OnCollisionEnter (Collision collision)
		{
			BaseOnCollisionEnter (collision);
		}

		#endregion


		#region PublicFunctions

		public override void UpdateMovement ()
		{
			base.UpdateMovement ();

			if (_rigidbody && moveSound && moveSoundClip && !inRotationMode)
			{
				if (numCollisions > 0)
				{
					PlayMoveSound (_rigidbody.velocity.magnitude);
				}
				else if (moveSound.IsPlaying ())
				{
					moveSound.Stop ();
				}
			}
		}


		public override void Grab (Vector3 grabPosition)
		{
			Vector3 originalPosition = grabPosition;
			lockedScreenOffset = Vector2.zero;
			currentTorque = Vector3.zero;

			inRotationMode = false;
			isChargingThrow = false;
			throwCharge = 0f;

			float distToCentre = (KickStarter.CameraMainTransform.position - _rigidbody.position).magnitude;
			distToCentre = Mathf.Clamp (distToCentre, minDistance, maxDistance);
			grabPosition = KickStarter.CameraMainTransform.position + ((grabPosition - KickStarter.CameraMainTransform.position).normalized * distToCentre);

			//FixedJointPosition = grabPosition;
			deltaMovement = Vector3.zero;

			_rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
			_rigidbody.useGravity = false;
			originalDistanceToCamera = (grabPosition - KickStarter.CameraMainTransform.position).magnitude;

			if (autoSetConstraints)
			{
				_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
			}

			base.Grab (grabPosition);

			grabPoint.position = originalPosition;

			RunInteraction (true);
		}


		public override void LetGo (bool ignoreInteractions = false)
		{
			if (inRotationMode)
			{
				SetRotationMode (false);
			}

			if (autoSetConstraints)
			{
				_rigidbody.constraints = RigidbodyConstraints.None;
			}

			_rigidbody.drag = originalDrag;
			_rigidbody.angularDrag = originalAngularDrag;

			if (inRotationMode)
			{
				_rigidbody.velocity = Vector3.zero;
			}
			else if (!isChargingThrow && !ignoreInteractions)
			{
				if (deltaMovement.magnitude > 3f)
				{
					deltaMovement = deltaMovement.normalized * 3f;
				}
				_rigidbody.AddForce (deltaMovement * _rigidbody.mass * 0.5f / Time.fixedDeltaTime);
			}

			_rigidbody.useGravity = true;

			base.LetGo (ignoreInteractions);

			RunInteraction (false);
		}


		public override bool CanToggleCursor ()
		{
			if (isChargingThrow || inRotationMode)
			{
				return false;
			}
			return true;
		}


		public override void ApplyDragForce (Vector3 force, Vector3 _screenMousePosition, float _distanceToCamera)
		{
			distanceToCamera = _distanceToCamera;

			if (inRotationMode)
			{
				// Scale force
				force *= speedFactor * _rigidbody.drag * distanceToCamera * Time.deltaTime;

				// Limit magnitude
				if (force.magnitude > maxSpeed)
				{
					force *= maxSpeed / force.magnitude;
				}

				Vector3 newRot = Vector3.Cross (force, KickStarter.CameraMainTransform.forward);
				newRot /= Mathf.Sqrt ((grabPoint.position - Transform.position).magnitude) * 2.4f * rotationFactor;

				currentTorque = newRot;
			}
			else
			{
				currentTorque = Vector3.Lerp (currentTorque, Vector3.zero, Time.deltaTime * 50f);
			}

			_rigidbody.AddTorque (currentTorque);

			UpdateFixedJoint ();
		}


		/** Unsets the FixedJoint used to hold the object in place */
		public void UnsetFixedJoint ()
		{
			isHeld = false;
		}


		/**
		 * <summary>Causes the object's target position to be overridden when held</summary>
		 * <param name = "newPosition">The new position</param>
		 */
		public void OverrideMoveToPosition (Vector3 newPosition)
		{
			overrideMoveToPosition = true;
			moveToPositionOverride = newPosition;
		}


		/** Clears any override set by the OverrideMoveToPosition function */
		public void ClearMoveOverride ()
		{
			overrideMoveToPosition = false;
		}

		#endregion


		#region ProtectedFunctions

		protected void RunInteraction (bool onGrab)
		{
			int parameterID = (onGrab) ? moveParameterID : dropParameterID;

			switch (actionListSource)
			{
				case ActionListSource.InScene:
					Interaction interaction = (onGrab) ? interactionOnGrab : interactionOnDrop;
					if (interaction && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onGrab || !KickStarter.actionListManager.IsListRunning (interaction))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = interaction.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
								}
							}

							interaction.Interact ();
						}
					}
					break;

				case ActionListSource.AssetFile:
					ActionListAsset actionListAsset = (onGrab) ? actionListAssetOnGrab : actionListAssetOnDrop;
					if (actionListAsset && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onGrab || !KickStarter.actionListAssetManager.IsListRunning (actionListAsset))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = actionListAsset.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
									if (GetComponent<ConstantID> ())
									{
										parameter.intValue = GetComponent<ConstantID> ().constantID;
									}
									else
									{
										ACDebug.LogWarning ("Cannot set the value of parameter " + parameterID + " ('" + parameter.label + "') as " + gameObject.name + " has no Constant ID component.", gameObject);
									}
								}
							}

							actionListAsset.Interact ();
						}
					}
					break;
			}
		}


		protected void ChargeThrow ()
		{
			if (!isChargingThrow)
			{
				isChargingThrow = true;
				chargeStartTime = Time.time;
				throwCharge = 0f;
			}
			else if (throwCharge < 1f)
			{
				throwCharge = (Time.time - chargeStartTime) / chargeTime;
			}

			if (throwCharge > 1f)
			{
				throwCharge = 1f;
			}
		}


		protected void ReleaseThrow ()
		{
			LetGo ();

			_rigidbody.useGravity = true;
			_rigidbody.drag = originalDrag;
			_rigidbody.angularDrag = originalAngularDrag;

			Vector3 moveVector = (Transform.position - KickStarter.CameraMainTransform.position).normalized;
			_rigidbody.AddForce (throwForce * throwCharge * moveVector);

			KickStarter.eventManager.Call_OnPickUpThrow (this);
		}


		protected void SetRotationMode (bool on)
		{
			_rigidbody.velocity = Vector3.zero;

			if (inRotationMode != on)
			{
				if (on)
				{
					startRotationMousePosition = KickStarter.playerInput.GetMousePosition ();
					cursorUnlockedWhenRotate = !KickStarter.playerInput.IsCursorLocked ();
					KickStarter.playerInput.forceGameplayCursor = ForceGameplayCursor.KeepUnlocked;

					if (autoSetConstraints)
					{
						_rigidbody.constraints = RigidbodyConstraints.None;
					}
				}
				else
				{
					KickStarter.playerInput.forceGameplayCursor = ForceGameplayCursor.None;

					if (autoSetConstraints)
					{
						_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
					}

					if (cursorUnlockedWhenRotate)
					{
						lockedScreenOffset += startRotationMousePosition - KickStarter.playerInput.GetMousePosition ();
					}
					else
					{
						lockedScreenOffset = Vector2.zero;
					}
				}
			}

			inRotationMode = on;
		}


		protected void UpdateFixedJoint ()
		{
			FixedJointPosition = Vector3.MoveTowards (FixedJointPosition, MoveToPosition, movementFactor * Time.fixedDeltaTime);
		}


		protected new void UpdateZoom ()
		{
			float zoom = Input.GetAxis ("ZoomMoveable");

			if ((originalDistanceToCamera <= minZoom && zoom < 0f) || (originalDistanceToCamera >= maxZoom && zoom > 0f))
			{ }
			else
			{
				originalDistanceToCamera += (zoom * zoomSpeed / 10f * Time.deltaTime);
			}

			originalDistanceToCamera = Mathf.Clamp (originalDistanceToCamera, minZoom, maxZoom);
		}


		/** Returns the position of the cursor that's dragging the PickUp in world-space */
		public Vector3 GetWorldMousePosition ()
		{
			if (!inRotationMode)
			{
				screenMousePosition = KickStarter.playerInput.GetMousePosition ();
				if (KickStarter.playerInput.IsCursorLocked ())
				{
					screenMousePosition = KickStarter.playerInput.LockedCursorPosition;
				}

				screenMousePosition += new Vector3 (lockedScreenOffset.x, lockedScreenOffset.y);
			}

			float alignedDistance = GetAlignedDistance (screenMousePosition);

			screenMousePosition.z = alignedDistance - (throwCharge * pullbackDistance);

			Vector3 pos = KickStarter.CameraMain.ScreenToWorldPoint (screenMousePosition);
			pos += Vector3.up * initialLift;

			return pos;
		}


		protected float GetAlignedDistance (Vector3 screenMousePosition)
		{
			screenMousePosition.z = 1f;
			Vector3 tempWorldMousePosition = KickStarter.CameraMain.ScreenToWorldPoint (screenMousePosition);

			float angle = Vector3.Angle (KickStarter.CameraMainTransform.forward, tempWorldMousePosition - KickStarter.CameraMainTransform.position);

			return originalDistanceToCamera * Mathf.Cos (angle * Mathf.Deg2Rad);
		}

		#endregion


		#region GetSet

		private Vector3 FixedJointPosition
		{
			get
			{
				return _rigidbody.position;
			}
			set
			{
				Vector3 origin = _rigidbody.position;
				Vector3 direction = value - origin;
				RaycastHit hit;
				if (Physics.Raycast (origin, direction, out hit, direction.magnitude))
				{
					if (hit.collider.gameObject != gameObject)
					{
						value = hit.point;
					}
				}

				_rigidbody.MovePosition (value);
			}
		}

		
		private Vector3 MoveToPosition
		{
			get
			{
				if (overrideMoveToPosition)
				{
					return moveToPositionOverride;
				}
				return worldMousePosition;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListSource == ActionListSource.AssetFile)
			{
				if (actionListAssetOnGrab == actionListAsset) return true;
				if (actionListAssetOnDrop == actionListAsset) return true;
			}
			return false;
		}

		#endif

	}

}