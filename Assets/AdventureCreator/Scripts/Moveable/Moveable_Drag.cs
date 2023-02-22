/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Moveable_Drag.cs"
 * 
 *	Attach this script to a GameObject to make it
 *	moveable according to a set method, either
 *	by the player or through Actions.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attaching this component to a GameObject allows it to be dragged, through physics, according to a set method.
	 */
	[AddComponentMenu ("Adventure Creator/Misc/Draggable")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable___drag.html")]
	public class Moveable_Drag : DragBase, iActionListAssetReferencer
	{

		#region Variables

		/** The way in which the object can be dragged (LockedToTrack, MoveAlongPlane, RotateOnly) */
		public DragMode dragMode = DragMode.LockToTrack;
		/** The DragTrack the object is locked to (if dragMode = DragMode.LockToTrack */
		public DragTrack track;
		/** Which direction the object can be dragged along the track in */
		public DragTrackDirection dragTrackDirection = DragTrackDirection.NoRestriction;
		/** If True, and dragMode = DragMode.LockToTrack, then the position and rotation of all child objects will be maintained when the object is attached to the track */
		public bool retainOriginalTransform = false;

		/** If True, and the object is locked to a DragTrack, then the object will be placed at a specific point along the track when the game begins */
		public bool setOnStart = true;
		/** How far along its DragTrack that the object should be placed at when the game begins */
		public float trackValueOnStart = 0f;
		/** If True, the object will recieve an additional gravity force when not held by the Player or being moved automatically */
		public bool applyGravity = false;

		/** Where to locate interactions */
		public ActionListSource actionListSource = ActionListSource.InScene;
		/** The Interaction to run whenever the object is moved by the player (and actionListSource = ActionListSource.InScene) */
		public Interaction interactionOnMove = null;
		/** The Interaction to run whenever the object is let go by the player (and actionListSource = ActionListSource.InScene) */
		public Interaction interactionOnDrop = null;
		/** The ActionListAsset to run whenever the object is moved by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnMove = null;
		/** The ActionListAsset to run whenever the object is let go by the player (and actionListSource = ActionListSource.AssetFile) */
		public ActionListAsset actionListAssetOnDrop = null;
		/** The parameter ID to set as this object in the interactionOnMove / actionListAssetOnMove ActionLists */
		public int moveParameterID = -1;
		/** The parameter ID to set as this object in the interactionOnDrop / actionListAssetOnDrop ActionLists */
		public int dropParameterID = -1;

		/** What movement is aligned to, if dragMode = DragMode.MoveAlongPlane (AlignToCamera, AlignToPlane) */
		public AlignDragMovement alignMovement = AlignDragMovement.AlignToCamera;
		/** The plane to align movement to, if alignMovement = AlignDragMovement.AlignToPlane */
		public Transform plane;
		/** If True, then gravity will be disabled on the object while it is held by the player */
		public bool noGravityWhenHeld = true;
		/** If True, then movement will occur by affecting the Rigidbody, as opposed to directly manipulating the Transform */
		public bool moveWithRigidbody = true;

		protected Vector3 grabPositionRelative;

		/** The object's simulated mass, if not using a Rigidbody */
		public float simulatedMass = 0.2f;

		protected float colliderRadius = 0.5f;
		protected float grabDistance = 0.5f;
		
		protected float _trackValue;
		protected Vector3 _dragVector;

		protected Collider _maxCollider;
		protected Collider _minCollider;
		protected int _revolutions = 0;

		protected bool canPlayCollideSound = false;
		protected float screenToWorldOffset;

		protected float lastFrameTotalCursorPositionAlong;
		protected bool endLocked = false;

		protected AutoMoveTrackData activeAutoMove;
		public bool canCallSnapEvents = true;
		[System.NonSerialized] public int regionID = 0;

		private Vector3 thisFrameTorque;
		/** The amount of damping to apply when rotating an object without a Rigidbody */
		public float toruqeDamping = 10f;
		private LerpUtils.Vector3Lerp torqueDampingLerp = new LerpUtils.Vector3Lerp (true);

		private Vector3 lastFrameForce;
		private float lastFrameTrackValue;
		private float lastFrameTotalPositionAlong;
		private float heldIntensity = 0f;
		
		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			base.Awake ();

			if (_rigidbody && moveWithRigidbody)
			{
				SetGravity (true);

				if (dragMode == DragMode.RotateOnly)
				{
					if (_rigidbody.constraints == RigidbodyConstraints.FreezeRotation ||
						_rigidbody.constraints == RigidbodyConstraints.FreezeRotationX ||
						_rigidbody.constraints == RigidbodyConstraints.FreezeRotationY ||
						_rigidbody.constraints == RigidbodyConstraints.FreezeRotationZ)
					{
						ACDebug.LogWarning ("Draggable " + gameObject.name + " has a Drag Mode of 'RotateOnly', but its rigidbody rotation is constrained. This may lead to inconsistent behaviour, and using a HingeTrack is advised instead.", gameObject);
					}
				}
			}

			SphereCollider sphereCollider = GetComponent <SphereCollider>();
			if (sphereCollider)
			{
				colliderRadius = sphereCollider.radius * Transform.localScale.x;
			}
			else if (dragMode == DragMode.LockToTrack && track && track.UsesEndColliders)
			{
				ACDebug.LogWarning ("Cannot calculate collider radius for Draggable object '" + gameObject.name + "' - it should have either a SphereCollider attached, even if it's disabled.", this);
			}

			if (dragMode == DragMode.LockToTrack)
			{
				StartCoroutine (InitToTrack ());
			}

			CheckGameplayBlockers ();
		}


		protected override void Start ()
		{
			if (dragMode != DragMode.LockToTrack)
			{
				LimitCollisions ();
			}

			base.Start ();
		}


		public override void _FixedUpdate ()
		{
			if (activeAutoMove != null)
			{
				activeAutoMove.Update (track, this);
			}
			else if (dragMode == DragMode.RotateOnly && !UsesRigidbody)
			{
				Transform.Rotate (thisFrameTorque, Space.World);

				if (!isHeld)
				{
					thisFrameTorque = torqueDampingLerp.Update (thisFrameTorque, Vector3.zero, toruqeDamping);
				}
			}

			if (UsesRigidbody && !IsHeld && applyGravity && !IsAutoMoving ())
			{
				_rigidbody.AddForceAtPosition (-Physics.gravity * Time.deltaTime, _rigidbody.position, ForceMode.Force);
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Snaps the object to a track at a given position along it</summary>
		 * <param name="newTrack">The new DragTrack to snap to</param>
		 * <param name="positionAlong">How far along the track, as a decimal of its length, to snap to</param>
		 */
		public void SnapToTrack (DragTrack newTrack, float positionAlong)
		{
			if (newTrack == null) return;

			if (track && newTrack != track)
			{
				track.OnDisconnect (this);
			}

			dragMode = DragMode.LockToTrack;
			
			if (IsAutoMoving())
			{
				activeAutoMove.Stop(track, this, true);
				activeAutoMove = null;
			}

			track = newTrack;
			track.SetPositionAlong (positionAlong, this);

			if (_rigidbody)
			{
				_rigidbody.velocity = Vector3.zero;
				_rigidbody.angularVelocity = Vector3.zero;
			}
		}


		/**
		 * <summary>Snaps the object to a track at a given region along it</summary>
		 * <param name="newTrack">The new DragTrack to snap to</param>
		 * <param name="regionID">The ID of the region to snap to. Regions are defined in the track itself</param>
		 */
		public void SnapToTrack (DragTrack newTrack, int regionID)
		{
			if (newTrack == null) return;
			SnapToTrack (newTrack, newTrack.GetRegionPositionAlong (regionID));
		}


		/**
		 * <summary>Gets how far the object is along its DragTrack.</summary>
		 * <returns>How far the object is along its DragTrack. This is normally 0 to 1, but if the object is locked to a looped DragTrack_Hinge, then the number of revolutions will be added to the result.</returns>
		 */
		public float GetPositionAlong ()
		{
			if (dragMode == DragMode.LockToTrack && track && track is DragTrack_Hinge)
			{
				return trackValue + (float) revolutions;
			}
			return trackValue;
		}


		public override void UpdateMovement ()
		{
			base.UpdateMovement ();
			if (dragMode == DragMode.LockToTrack && track)
			{
				track.UpdateDraggable (this);
				
				if (UsesRigidbody && (_rigidbody.angularVelocity != Vector3.zero || _rigidbody.velocity != Vector3.zero))
				{
					RunInteraction (true);
				}

				if (IsAutoMoving ())
				{
					if (activeAutoMove.CheckForEnd (this))
					{
						StopAutoMove (true);
					}
				}
				else if (!UsesRigidbody && dragMode == DragMode.LockToTrack && track)
				{
					if (IsHeld)
					{
						heldIntensity = 1f;
					}
					else
					{
						if (heldIntensity > 0.01f && trackValue > 0f && trackValue < 1f)
						{
							switch (track.dragMovementCalculation)
							{
								case DragMovementCalculation.DragVector:
									track.ApplyDragForce (lastFrameForce * heldIntensity, this);
									break;

								case DragMovementCalculation.CursorPosition:
									if (simulatedMass > 0)
									{
										track.ApplyAutoForce (lastFrameTotalPositionAlong, heldIntensity * speedFactor * 0.02f / simulatedMass, this, false);
									}
									break;

								default:
									break;
							}
						}
						heldIntensity = Mathf.Lerp (heldIntensity, 0f, Time.deltaTime * (2f + simulatedMass));
					}
				}

				if (collideSound && !track.UsesEndColliders)
				{
					if (trackValue > 0.03f && trackValue < 0.97f)
					{
						canPlayCollideSound = true;
					}
					else if ((Mathf.Approximately (trackValue, 0f) || (!onlyPlayLowerCollisionSound && Mathf.Approximately (trackValue, 1f))) && canPlayCollideSound)
					{
						canPlayCollideSound = false;
						collideSound.Play (collideSoundClip, false);
					}
				}
			}
			else if (isHeld)
			{
				if (dragMode == DragMode.RotateOnly && allowZooming && distanceToCamera > 0f)
				{
					LimitZoom ();
				}
			}

			if (moveSoundClip && moveSound)
			{
				if (dragMode == DragMode.LockToTrack && track)
				{
					PlayMoveSound (track.GetMoveSoundIntensity (trackValue - lastFrameTrackValue));
					
				}
				else if (_rigidbody)
				{
					PlayMoveSound (_rigidbody.velocity.magnitude);
				}
			}

			lastFrameTrackValue = trackValue;
		}
		

		public override void DrawGrabIcon ()
		{
			if (isHeld && showIcon && KickStarter.CameraMain.WorldToScreenPoint (Transform.position).z > 0f && icon != null)
			{
				if (dragMode == DragMode.LockToTrack && track && track.IconIsStationary ())
				{
					Vector3 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (grabPositionRelative + Transform.position);
					icon.Draw (new Vector3 (screenPosition.x, screenPosition.y));
				}
				else
				{
					Vector3 screenPosition = KickStarter.CameraMain.WorldToScreenPoint (grabPoint.position);
					icon.Draw (new Vector3 (screenPosition.x, screenPosition.y));
				}
			}
		}

		
		public override void ApplyDragForce (Vector3 force, Vector3 mousePosition, float _distanceToCamera)
		{
			distanceToCamera = _distanceToCamera;
			
			// Scale force
			force *= speedFactor * distanceToCamera / 50f;

			// Limit magnitude
			if (force.magnitude > maxSpeed)
			{
				force *= maxSpeed / force.magnitude;
			}

			switch (dragMode)
			{
				case DragMode.LockToTrack:
					if (track)
					{
						switch (track.dragMovementCalculation)
						{
							case DragMovementCalculation.DragVector:
								if (!UsesRigidbody)
								{
									force = Vector3.Slerp (lastFrameForce, force, Time.deltaTime * 10f);
								}

								track.ApplyDragForce (force, this);
								lastFrameForce = force;
								if (!UsesRigidbody)
								{
									RunInteraction (true);
								}
								break;

							case DragMovementCalculation.CursorPosition:
								float mousePositionAlong = track.GetScreenPointProportionAlong (mousePosition, grabPositionRelative, this);
								
								float totalPositionAlong = mousePositionAlong + screenToWorldOffset;
								
								if (track.preventEndToEndJumping && !track.Loops)
								{
									bool inDeadZone = (totalPositionAlong >= 1f || totalPositionAlong <= 0f);
									if (endLocked)
									{
										if (!inDeadZone)
										{
											endLocked = false;
										}
									}
									else
									{
										if (inDeadZone)
										{
											endLocked = true;
										}
									}

									if (track.Loops || !endLocked)
									{
										lastFrameTotalCursorPositionAlong = totalPositionAlong;
									}
									else
									{
										totalPositionAlong = lastFrameTotalCursorPositionAlong;
									}
								}

								switch (dragTrackDirection)
								{
									case DragTrackDirection.ForwardOnly:
										if (totalPositionAlong < GetPositionAlong ())
										{
											return;
										}
										break;

									case DragTrackDirection.BackwardOnly:
										if (totalPositionAlong > GetPositionAlong ())
										{
											return;
										}
										break;

									default:
										break;
								}

								RunInteraction (true);
								lastFrameTotalPositionAlong = totalPositionAlong;
								track.ApplyAutoForce (totalPositionAlong, speedFactor * 0.02f / simulatedMass, this, false);
								break;
						}
					}
					break;

				case DragMode.MoveAlongPlane:
					{
						Vector3 newRot = Vector3.Cross (force, KickStarter.CameraMainTransform.forward);
						if (alignMovement == AlignDragMovement.AlignToPlane)
						{
							if (plane)
							{
								_rigidbody.AddForceAtPosition (Vector3.Cross(newRot, plane.up), Transform.position + (plane.up * grabDistance));
							}
							else
							{
								ACDebug.LogWarning("No alignment plane assigned to " + this.name, this);
							}
						}
						else
						{
							_rigidbody.AddForceAtPosition (force, Transform.position - (KickStarter.CameraMainTransform.forward * grabDistance));
						}
					}
					break;

				case DragMode.RotateOnly:
					{
						Vector3 newRot = Vector3.Cross (force, KickStarter.CameraMainTransform.forward);
						newRot /= Mathf.Sqrt((grabPoint.position - Transform.position).magnitude) * 2.4f * rotationFactor;

						if (UsesRigidbody)
						{
							_rigidbody.AddTorque (newRot);
						}
						else
						{
							Vector3 rawTorque = newRot;
							thisFrameTorque = torqueDampingLerp.Update (thisFrameTorque, rawTorque, toruqeDamping);
						}

						if (allowZooming)
						{
							UpdateZoom ();
						}
					}
					break;
			}
		}


		public override void LetGo (bool ignoreInteractions = false)
		{
			lastFrameForce = Vector3.zero;
			canCallSnapEvents = true;

			SetGravity (true);

			if (dragMode == DragMode.RotateOnly && UsesRigidbody)
			{
				_rigidbody.velocity = Vector3.zero;
			}

			if (!ignoreInteractions)
			{
				RunInteraction (false);
			}

			base.LetGo (ignoreInteractions);
			
			if (dragMode == DragMode.LockToTrack && track)
			{
				track.OnLetGo (this);
			}
		}


		public override void Grab (Vector3 grabPosition)
		{
			isHeld = true;
			grabPoint.position = grabPosition;
			grabPositionRelative = grabPosition - Transform.position;
			grabDistance = grabPositionRelative.magnitude;

			if (dragMode == DragMode.LockToTrack)
			{
				if (track)
				{
					StopAutoMove (false);

					if (track.dragMovementCalculation == DragMovementCalculation.CursorPosition)
					{
						screenToWorldOffset = trackValue - track.GetScreenPointProportionAlong (KickStarter.playerInput.GetMousePosition (), grabPositionRelative, this);
						endLocked = false;

						lastFrameTotalCursorPositionAlong = GetPositionAlong ();

						if (track.Loops)
						{
							if (trackValue < 0.5f && screenToWorldOffset < -0.5f)
							{
								// Other side
								screenToWorldOffset += 1f;
							}
							else if (trackValue > 0.5f && screenToWorldOffset > 0.5f)
							{
								// Other side #2
								screenToWorldOffset -= 1f;
							}
						}
					}
				
					if (track is DragTrack_Straight)
					{
						UpdateScrewVector ();
					}
					else if (track is DragTrack_Hinge)
					{
						_dragVector = grabPosition;
					}
				}
			}
			else
			{
				RunInteraction (true);
			}

			SetGravity (false);

			if (dragMode == DragMode.RotateOnly && UsesRigidbody)
			{
				_rigidbody.velocity = Vector3.zero;
			}

			KickStarter.eventManager.Call_OnGrabMoveable (this);
		}


		/** If the object rotates like a screw along a DragTrack_Straight, this updates the correct drag vector. */
		public void UpdateScrewVector ()
		{
			float forwardDot = Vector3.Dot (grabPoint.position - Transform.position, Transform.forward);
			float rightDot = Vector3.Dot (grabPoint.position - Transform.position, Transform.right);
			
			_dragVector = (Transform.forward * -rightDot) + (Transform.right * forwardDot);
		}


		/**
		 * <summary>Stops the object from moving without the player's direct input (i.e. through Actions).</summary>
		 * <param name = "snapToTarget">If True, then the object will snap instantly to the intended target position</param>
		 */
		public void StopAutoMove (bool snapToTarget = true)
		{
			if (IsAutoMoving ())
			{
				lastFrameForce = Vector3.zero;

				activeAutoMove.Stop (track, this, snapToTarget);
				activeAutoMove = null;

				if (UsesRigidbody)
				{
					_rigidbody.velocity = Vector3.zero;
					_rigidbody.angularVelocity = Vector3.zero;
				}
			}
		}


		/**
		 * <summary>Checks if the object is moving without the player's direct input.</summary>
		 * <returns>True if the object is moving without the player's direct input (gravity doesn't count).</returns>
		 */
		public bool IsAutoMoving (bool beAccurate = true)
		{
			if (activeAutoMove != null)
			{
				if (!beAccurate)
				{
					if (activeAutoMove.CheckForEnd (this, false))
					{
						// Special case: waiting for action to complete, so don't worry about being too accurate
						return false;
					}
				}
				return true;
			}
			return false;
		}


		/**
		 * <summary>Forces the object to move along a DragTrack without the player's direct input.</summary>
		 * <param name = "_targetTrackValue">The intended proportion along the track to send the object to</param>
		 * <param name = "_targetTrackSpeed">The intended speed to move the object by</param>
		 * <param name = "removePlayerControl">If True and the player is currently moving the object, then player control will be removed</param>
		 */
		public void AutoMoveAlongTrack (float _targetTrackValue, float _targetTrackSpeed, bool removePlayerControl)
		{
			AutoMoveAlongTrack (_targetTrackValue, _targetTrackSpeed, removePlayerControl, 1 << 0);
		}


		/**
		 * <summary>Forces the object to move along a DragTrack without the player's direct input.</summary>
		 * <param name = "_targetTrackValue">The intended proportion along the track to send the object to</param>
		 * <param name = "_targetTrackSpeed">The intended speed to move the object by</param>
		 * <param name = "removePlayerControl">If True and the player is currently moving the object, then player control will be removed</param>
		 * <param name = "layerMask">A LayerMask that determines what collisions will cause the automatic movement to cease</param>
		 * <param name = "snapID">The ID number of the associated snap, if snapping</param>
		 */
		public void AutoMoveAlongTrack (float _targetTrackValue, float _targetTrackSpeed, bool removePlayerControl, LayerMask layerMask, int snapID = -1)
		{
			if (dragMode == DragMode.LockToTrack && track)
			{
				if (snapID < 0)
				{
					canCallSnapEvents = true;
				}

				if (_targetTrackSpeed <= 0f)
				{
					activeAutoMove = null;
					track.SetPositionAlong (_targetTrackValue, this);
					return;
				}

				if (removePlayerControl)
				{
					isHeld = false;
				}
				
				activeAutoMove = new AutoMoveTrackData (_targetTrackValue, _targetTrackSpeed / 6000f, layerMask, snapID);
			}
			else
			{
				ACDebug.LogWarning ("Cannot move " + this.name + " along a track, because no track has been assigned to it", this);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void CheckGameplayBlockers ()
		{
			switch (actionListSource)
			{
				case ActionListSource.InScene:
					if (interactionOnMove && interactionOnMove.actionListType == ActionListType.PauseGameplay)
					{
						ACDebug.LogWarning ("The Draggable " + gameObject.name + "'s 'Interaction on move' is set to Pause Gameplay - this should be set to Run In Background to prevent issues.", this);
					}
					break;

				case ActionListSource.AssetFile:
					if (actionListAssetOnMove && actionListAssetOnMove.actionListType == ActionListType.PauseGameplay)
					{
						ACDebug.LogWarning ("The Draggable " + gameObject.name + "'s 'ActionList on move' is set to Pause Gameplay - this should be set to Run In Background to prevent issues.", this);
					}
					break;
			}
		}


		protected IEnumerator InitToTrack ()
		{
			if (track)
			{
				ChildTransformData[] childTransformData = GetChildTransforms ();

				track.Connect (this);

				if (retainOriginalTransform)
				{
					track.SnapToTrack (this, true);
					SetChildTransforms (childTransformData);
					yield return new WaitForEndOfFrame ();
				}

				if (setOnStart)
				{
					track.SetPositionAlong (trackValueOnStart, this);
				}
				else
				{
					track.SnapToTrack (this, true);
				}
				trackValue = track.GetDecimalAlong (this);
			}
		}


		protected ChildTransformData[] GetChildTransforms ()
		{
			List<ChildTransformData> childTransformData = new List<ChildTransformData>();
			for (int i=0; i<Transform.childCount; i++)
			{
				Transform childTransform = Transform.GetChild (i);
				childTransformData.Add (new ChildTransformData (childTransform.position, childTransform.rotation));
			}
			return childTransformData.ToArray ();
		}


		protected void SetChildTransforms (ChildTransformData[] childTransformData)
		{
			for (int i=0; i<Transform.childCount; i++)
			{
				Transform childTransform = Transform.GetChild (i);
				childTransformData[i].UpdateTransform (childTransform);
			}
		}


		protected void RunInteraction (bool onMove)
		{
			int parameterID = (onMove) ? moveParameterID : dropParameterID;

			switch (actionListSource)
			{
				case ActionListSource.InScene:
					Interaction interaction = (onMove) ? interactionOnMove : interactionOnDrop;
					if (interaction && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onMove || !KickStarter.actionListManager.IsListRunning (interaction))
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
					ActionListAsset actionListAsset = (onMove) ? actionListAssetOnMove : actionListAssetOnDrop;
					if (actionListAsset && gameObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer))
					{
						if (!onMove || !KickStarter.actionListAssetManager.IsListRunning (actionListAsset))
						{
							if (parameterID >= 0)
							{
								ActionParameter parameter = actionListAsset.GetParameter (parameterID);
								if (parameter != null && parameter.parameterType == ParameterType.GameObject)
								{
									parameter.gameObject = gameObject;
									if (GetComponent <ConstantID>())
									{
										parameter.intValue = GetComponent <ConstantID>().constantID;
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

				default:
						break;
			}
		}


		protected void OnCollisionEnter (Collision collision)
		{
			if (IsAutoMoving ())
			{
				activeAutoMove.ProcessCollision (collision, this);
			}

			BaseOnCollisionEnter (collision);
		}


		protected void SetGravity (bool value)
		{
			if (dragMode != DragMode.LockToTrack)
			{
				if (noGravityWhenHeld && _rigidbody)
				{
					_rigidbody.useGravity = value;
				}
			}
		}

		#endregion


		#region GetSet

		public float ColliderWidth
		{
			get
			{
				return colliderRadius;
			}
		}


		public bool UsesRigidbody
		{
			get
			{
				if (Application.isPlaying)
				{
					if (_rigidbody)
					{
						return moveWithRigidbody;
					}
				}
				else
				{
					if (GetComponent <Rigidbody>())
					{
						return moveWithRigidbody;
					}
				}
				return false;
			}
		}


		public float trackValue
		{
			get
			{
				return _trackValue;
			}
			set
			{
				_trackValue = value;
			}
		}
		

		public Vector3 dragVector
		{
			get
			{
				return _dragVector;
			}
			set
			{
				_dragVector = value;
			}
		}
		

		public Collider maxCollider
		{
			get
			{
				return _maxCollider;
			}
			set
			{
				_maxCollider = value;
			}
		}


		public Collider minCollider
		{
			get
			{
				return _minCollider;
			}
			set
			{
				_minCollider = value;
			}
		}


		public int revolutions
		{
			get
			{
				return _revolutions;
			}
			set
			{
				_revolutions = value;
			}
		}

		#endregion


		#region ProtectedClasses

		protected class AutoMoveTrackData
		{

			protected float targetValue;
			protected float speed;
			protected LayerMask blockLayerMask;
			protected int snapID = -1;


			public AutoMoveTrackData (float _targetValue, float _speed, LayerMask _blockLayerMask, int _snapID = -1)
			{
				targetValue = _targetValue;
				speed = _speed;
				blockLayerMask = _blockLayerMask;
				snapID = _snapID;
			}

			
			public void Update (DragTrack track, Moveable_Drag draggable)
			{
				track.ApplyAutoForce (targetValue, speed, draggable, true);
			}


			public bool CheckForEnd (Moveable_Drag draggable, bool beAccurate = true)
			{
				float currentValue = draggable.trackValue;

				if (draggable.track.Loops)
				{
					if (targetValue - currentValue > 0.5f)
					{
						currentValue += 1f;
					}
					else if (currentValue - targetValue > 0.5f)
					{
						currentValue -= 1f;
					}
				}

				float diff = Mathf.Abs (targetValue - currentValue);

				if (diff < 0.01f)
				{
					if (draggable.canCallSnapEvents && snapID >= 0)
					{
						TrackSnapData snapData = draggable.track.GetSnapData (snapID);
						if (snapData.IsEnabled)
						{
							snapData.RunSnapCutscene (draggable.track.actionListSource);

							KickStarter.eventManager.Call_OnDraggableSnap (draggable, draggable.track, snapData);
							draggable.canCallSnapEvents = false;
						}
					}

					if (!beAccurate)
					{
						return true;
					}
				}
				if (diff < 0.001f)
				{
					return true;
				}
				return false;
			}


			public void Stop (DragTrack track, Moveable_Drag draggable, bool snapToTarget)
			{
				if (snapToTarget)
				{
					track.SetPositionAlong (targetValue, draggable);
				}
			}


			public void ProcessCollision (Collision collision, Moveable_Drag draggable)
			{
				if ((blockLayerMask.value & 1 << collision.gameObject.layer) != 0)
				{
					draggable.StopAutoMove (false);
				}
			}

		}


		protected class ChildTransformData
		{

			protected Vector3 originalPosition;
			protected Quaternion originalRotation;


			public ChildTransformData (Vector3 _originalPosition, Quaternion _originalRotation)
			{
				originalPosition = _originalPosition;
				originalRotation = _originalRotation;
			}


			public void UpdateTransform (Transform transform)
			{
				transform.position = originalPosition;
				transform.rotation = originalRotation;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListSource == ActionListSource.AssetFile)
			{
				if (actionListAssetOnMove == actionListAsset) return true;
				if (actionListAssetOnDrop == actionListAsset) return true;
			}
			return false;
		}

		#endif

	}
	
}