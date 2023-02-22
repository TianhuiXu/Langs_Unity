/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"DragTrack_Curved.cs"
 * 
 *	This track constrains Moveable_Drag objects to a circular ring.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A track that constrains Moveable_Drag objects to a circular ring.
	 * Unlike a hinge track (see DragTrack_Hinge), the object will be translated as well as rotated.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_drag_track___curved.html")]
	public class DragTrack_Curved : DragTrack
	{

		#region Variables

		/** The angle of the tracks's curve */
		public float maxAngle = 60f;
		/** The track's radius */
		public float radius = 2f;
		/** If True, then the track forms a complete loop */
		public bool doLoop = false;
		/** If True, then colliders will be auto-generated at the ends of the track, to add friction/bounce effects when the dragged objects reaches the limits */
		public bool generateColliders = true;

		protected Vector3 startPosition;

		#endregion


		#region PublicFunctions

		public override bool TypeSupportsSnapConnections ()
		{
			return true;
		}


		public override void Connect (Moveable_Drag draggable)
		{
			startPosition = Transform.position + (radius * Transform.right);
			
			AssignColliders (draggable);
		}


		public override void ApplyAutoForce (float _position, float _speed, Moveable_Drag draggable, bool ignoreMaxSpeed)
		{
			if (draggable.UsesRigidbody)
			{
				Vector3 deltaForce = GetForceToPosition (draggable, _position);
				deltaForce *= _speed / draggable.Rigidbody.mass;

				// Limit magnitude
				if (!ignoreMaxSpeed && deltaForce.magnitude > draggable.maxSpeed)
				{
					deltaForce *= draggable.maxSpeed / deltaForce.magnitude;
				}
			
				deltaForce -= draggable.Rigidbody.velocity;
				draggable.Rigidbody.AddForce (deltaForce, ForceMode.VelocityChange);
			}
			else
			{
				float newPosition = Mathf.Lerp(draggable.trackValue, _position, Time.deltaTime * _speed * 100f);
				SetPositionAlong(newPosition, draggable);
			}
		}


		public override float GetForceDotProduct (Vector3 force, Moveable_Drag draggable)
		{
			return Vector3.Dot (force, draggable.Transform.up);
		}


		public override void ApplyDragForce (Vector3 force, Moveable_Drag draggable)
		{
			float dotProduct = GetForceDotProduct (force, draggable);

			switch (draggable.dragTrackDirection)
			{
				case DragTrackDirection.ForwardOnly:
					if (dotProduct < 0f)
					{
						return;
					}
					break;

				case DragTrackDirection.BackwardOnly:
					if (dotProduct > 0f)
					{
						return;
					}
					break;

				default:
					break;
			}

			if (draggable.UsesRigidbody)
			{
				// Calculate the amount of force along the tangent
				Vector3 tangentForce = draggable.Transform.up * dotProduct;
				draggable.Rigidbody.AddForce (tangentForce);
			}
			else
			{
				float normalizedDotProduct = GetForceDotProduct(force.normalized, draggable);
				if (Mathf.Abs(normalizedDotProduct) < 0.3f)
				{
					return;
				}

				float newPosition = draggable.trackValue + (dotProduct);
				ApplyAutoForce (newPosition, 0.01f * Time.deltaTime / draggable.simulatedMass, draggable, false);
			}
		}


		public override float GetScreenPointProportionAlong (Vector2 point, Vector3 grabRelativePosition, Moveable_Drag drag)
		{
			Vector2 screen_gizmoStartPosition = KickStarter.CameraMain.WorldToScreenPoint (GetGizmoPosition (0f));
			Vector2 screen_gizmoEndPosition = KickStarter.CameraMain.WorldToScreenPoint (GetGizmoPosition (1f));
			Vector2 screen_origin = KickStarter.CameraMain.WorldToScreenPoint (Transform.position);

			Vector2 startToOrigin = screen_gizmoStartPosition - screen_origin;
			Vector2 endToOrigin = screen_gizmoEndPosition - screen_origin;

			Vector2 pointToOrigin = point - screen_origin;

			float startToPointAngle = AdvGame.SignedAngle (startToOrigin, pointToOrigin);
			float startToEndAngle = AdvGame.SignedAngle (startToOrigin, endToOrigin);

			bool isFlipped = (Vector3.Dot (Transform.forward, KickStarter.CameraMainTransform.forward) < 0f);
			if (isFlipped)
			{
				startToEndAngle *= -1f;
				startToPointAngle *= -1f;
			}

			if (startToEndAngle < 0f) startToEndAngle += 360f;
			if (startToPointAngle < 0f) startToPointAngle += 360f;

			if (Loops)
			{
				startToEndAngle = 360f;
			}

			float reversedMidAngle = 180f + (startToEndAngle / 2f);
			if (startToPointAngle > reversedMidAngle) startToPointAngle -= 360f;

			float result = startToPointAngle / startToEndAngle;

			if (Loops)
			{
				// Prevent turning a revolution when crossing over the maxangle
				float currentPositionAlong = drag.GetPositionAlong ();
				if ((currentPositionAlong - result) > 0.5f)
				{
					result += 1f;
				}
				else if ((result - currentPositionAlong) > 0.5f)
				{
					result -= 1f;
				}
			}

			return result;
		}


		public override void SetPositionAlong (float proportionAlong, Moveable_Drag draggable)
		{
			Connect (draggable);

			Quaternion rotation = Quaternion.AngleAxis (proportionAlong * MaxAngle, Transform.forward);
			draggable.Transform.position = RotatePointAroundPivot (startPosition, Transform.position, rotation);
			draggable.Transform.rotation = Quaternion.AngleAxis (proportionAlong * MaxAngle, Transform.forward) * Transform.rotation;

			if (UsesEndColliders)
			{
				UpdateColliders (proportionAlong, draggable);
			}
			
			base.SetPositionAlong (proportionAlong, draggable);
		}


		public override float GetDecimalAlong (Moveable_Drag draggable)
		{
			float reversedMidAngle = 360f - (360f - MaxAngle) / 2f;

			float angle = Vector3.Angle (-Transform.right, draggable.Transform.position - Transform.position);

			// Sign of angle?
			if (angle < 180f && Vector3.Dot (draggable.Transform.position - Transform.position, Transform.up) < 0f)
			{
				angle *= -1f;
			}

			angle = 180f - angle;

			if (!Loops && angle > reversedMidAngle)
			{
				// Clamp to start
				return 0f;
			}
			return (angle / MaxAngle);
		}


		public override void SnapToTrack (Moveable_Drag draggable, bool onStart)
		{
			// Limit velocity to just along track
			if (draggable.UsesRigidbody)
			{
				Vector3 localVelocity = draggable.Transform.InverseTransformDirection (draggable.Rigidbody.velocity);
				localVelocity.x = 0;
				localVelocity.z = 0;
				draggable.Rigidbody.velocity = draggable.Transform.TransformDirection (localVelocity);
			}

			float proportionAlong = Mathf.Clamp01 (GetDecimalAlong (draggable));
			draggable.Transform.rotation = Quaternion.AngleAxis (proportionAlong * MaxAngle, Transform.forward) * Transform.rotation;

			draggable.Transform.position = Transform.position + draggable.Transform.right * radius;

			if (onStart)
			{
				SetPositionAlong (proportionAlong, draggable);
			}			
		}


		public override void UpdateDraggable (Moveable_Drag draggable)
		{
			draggable.trackValue = GetDecimalAlong (draggable);

			SnapToTrack (draggable, false);

			if (UsesEndColliders)
			{
				UpdateColliders (draggable.trackValue, draggable);
			}

			DoRegionAudioCheck (draggable);

			if (!onlySnapOnPlayerRelease)
			{
				DoSnapCheck (draggable);
			}

			DoConnectionCheck (draggable);
		}


		public override Vector3 GetGizmoPosition (float proportionAlong)
		{
			Quaternion rot = Quaternion.AngleAxis (proportionAlong * MaxAngle, Transform.forward);
			Vector3 startPosition = Transform.position + (radius * Transform.right);
			return RotatePointAroundPivot (startPosition, Transform.position, rot);	
		}


		public override Vector3 GetForceToPosition (Moveable_Drag draggable, float targetProportionAlong)
		{
			float proportionalDifference = Mathf.Clamp01 (targetProportionAlong) - draggable.trackValue;

			if (Loops)
			{
				if (proportionalDifference > 0.5f)
				{
					proportionalDifference -= 1f;
				}
				else if (proportionalDifference < -0.5f)
				{
					proportionalDifference += 1f;
				}
			}

			return draggable.Transform.up * proportionalDifference * 1000f;
		}


		public override float GetMoveSoundIntensity (float deltaTrackPosition)
		{
			return Mathf.Abs (deltaTrackPosition) * Time.deltaTime * 2500f * MaxAngle;
		}

		#endregion


		#region ProtectedFunctions

		protected override void AssignColliders (Moveable_Drag draggable)
		{
			if (!UsesEndColliders || !draggable.UsesRigidbody)
			{
				base.AssignColliders (draggable);
				return;
			}

			if (draggable.maxCollider == null)
			{
				draggable.maxCollider = (Collider) Instantiate (Resource.DragCollider);
			}

			if (draggable.minCollider == null)
			{
				draggable.minCollider = (Collider) Instantiate (Resource.DragCollider);
			}

			float offsetAngle = Mathf.Asin (draggable.ColliderWidth / radius) * Mathf.Rad2Deg;

			draggable.maxCollider.transform.position = startPosition;
			draggable.maxCollider.transform.up = -Transform.up;
			draggable.maxCollider.transform.RotateAround (Transform.position, Transform.forward, maxAngle + offsetAngle);

			draggable.minCollider.transform.position = startPosition;
			draggable.minCollider.transform.up = Transform.up;
			draggable.minCollider.transform.RotateAround (Transform.position, Transform.forward, -offsetAngle);

			base.AssignColliders (draggable);
		}


		protected void UpdateColliders (float trackValue, Moveable_Drag draggable)
		{
			if (trackValue > 1f || !draggable.UsesRigidbody)
			{
				return;
			}

			if (trackValue > 0.5f)
			{
				draggable.minCollider.enabled = false;
				draggable.maxCollider.enabled = true;
			}
			else
			{
				draggable.minCollider.enabled = true;
				draggable.maxCollider.enabled = false;
			}
		}

		#endregion


		#region GetSet

		public override bool Loops
		{
			get
			{
				return (doLoop || maxAngle == 360f);
			}
		}


		public float MaxAngle
		{
			get
			{
				return (Loops) ? 360f : maxAngle;
			}
		}


		public override bool UsesEndColliders
		{
			get
			{
				return !Loops && generateColliders;
			}
		}

		#endregion

	}

}