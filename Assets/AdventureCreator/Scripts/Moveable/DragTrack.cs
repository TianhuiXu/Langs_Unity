/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"DragTrack.cs"
 * 
 *	The base class for "tracks", which are used to
 *	constrain Moveable_Drag objects along set paths
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * The base class for "tracks", which are used to contrain Moveable_Drag objects along a pre-determined path
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_drag_track.html")]
	public abstract class DragTrack : MonoBehaviour
	{

		#region Variables

		/** The Physics Material to give the track's end colliders */
		public PhysicMaterial colliderMaterial;
		/** The size of the track's end colliders, as seen in the Scene window */
		public float discSize = 0.2f;
		/** The colour of Scene window Handles */
		public Color handleColour = Color.white;
		/** How input movement is calculated (DragVector, CursorPosition) */
		public DragMovementCalculation dragMovementCalculation = DragMovementCalculation.DragVector;
		/** If True, then snapping is enabled and any object attached to the track can snap to pre-set points along it when let go by the player */
		public bool doSnapping = false;
		/** A list of all the points along the track that attached objects can snap to, if doSnapping = True */
		public List<TrackSnapData> allTrackSnapData = new List<TrackSnapData>();
		/** The speed to move by when attached objects snap */
		public float snapSpeed = 100f;
		/** If True, then snapping will only occur when the player releases the object - and not when moving on its own accord */
		public bool onlySnapOnPlayerRelease;
		/** If True, and the track doesn't loop, then the dragged object will be prevented from jumping from one end to the other without first moving somewhere in between */
		public bool preventEndToEndJumping = false;
		/** Where to locate interactions */
		public ActionListSource actionListSource = ActionListSource.InScene;
		private Transform _transform;

		#endregion


		#region PublicFunctions

		/** Returns true if this type of track supports connections with other tracks via snapping */
		public virtual bool TypeSupportsSnapConnections ()
		{
			return false;
		}


		/**
		 * <summary>Gets the proportion along the track that an object is positioned.</summary>
		 * <param name = "draggable">The Moveable_Drag object to check the position of</param>
		 * <returns>The proportion along the track that the Moveable_Drag object is positioned (0 to 1)</returns>
		 */
		public virtual float GetDecimalAlong (Moveable_Drag draggable)
		{
			return 0f;
		}


		/**
		 * <summary>Positions an object on a specific point along the track.</summary>
		 * <param name = "proportionAlong">The proportion along which to place the Moveable_Drag object (0 to 1)</param>
		 * <param name = "draggable">The Moveable_Drag object to reposition</param>
		 */
		public virtual void SetPositionAlong (float proportionAlong, Moveable_Drag draggable)
		{
			draggable.trackValue = proportionAlong;
		}


		/**
		 * <summary>Connects an object to the track when the game begins.</summary>
		 * <param name = "draggable">The Moveable_Drag object to connect to the track</param>
		 */
		public virtual void Connect (Moveable_Drag draggable)
		{}


		/**
		 * <summary>Called when an object attached to the track is disconnected from it.</summary>
		 * <param name = "draggable">The Moveable_Drag object being disconnected from the track</param>
		 */
		public void OnDisconnect (Moveable_Drag draggable)
		{
			if (draggable.maxCollider)
			{
				Destroy (draggable.maxCollider.gameObject);
			}

			if (draggable.minCollider)
			{
				Destroy (draggable.minCollider.gameObject);
			}
		}


		/**
		 * <summary>Applies a force to an object connected to the track.</summary>
		 * <param name = "force">The drag force vector input by the player</param>
		 * <param name = "draggable">The Moveable_Drag object to apply the force to</param>
		 */
		public virtual void ApplyDragForce (Vector3 force, Moveable_Drag draggable)
		{}


		/**
		 * <summary>Gets the proportion along the track closest to a given position in screen-space</summary>
		 * <param name = "point">The position in screen-space</param>
		 * <param name = "grabRelativePosition">The grab position relative to the draggable's centre</param>
		 * <param name = "dragm">The object being dragged</param>
		 * <returns>The proportion along the track closest to a given position in screen-space</returns>
		 */
		public virtual float GetScreenPointProportionAlong (Vector2 point, Vector3 grabRelativePosition, Moveable_Drag drag)
		{
			return 0f;
		}


		/**
		 * <summary>Gets the smallest distance, in screen-space, between a given position in screen space, and the point on the track that it is closest to.</summary>
		 * <param name="point">The point, in screen space</param>
		 * <returns>The smallest distance, in screen-space, between a given position in screen space, and the point on the track that it is closest to.</returns>
		 */
		public float GetMinDistanceToScreenPoint (Vector2 point)
		{
			float proportionAlong = GetScreenPointProportionAlong (point, Vector3.zero, null);
			Vector3 trackPointWorldPosition = GetGizmoPosition(proportionAlong);
			Vector2 trackPointScreenPosition = KickStarter.CameraMain.WorldToScreenPoint(trackPointWorldPosition);

			return Vector2.Distance(point, trackPointScreenPosition);
		}


		/**
		 * <summary>Applies a force that, when applied every frame, pushes an object connected to the track towards a specific point along it.</summary>
		 * <param name = "_position">The proportion along which to place the Moveable_Drag object (0 to 1)</param>
		 * <param name = "_speed">The speed to move by</param>
		 * <param name = "draggable">The draggable object to move</param>
		 * <param name = "ignoreMaxSpeed">If False, the object's maxSpeed will limit the speed</param>
		 */
		public virtual void ApplyAutoForce (float _position, float _speed, Moveable_Drag draggable, bool ignoreMaxSpeed)
		{}


		/**
		 * <summary>Updates the position of an object connected to the track. This is called every frame.</summary>
		 * <param name = "draggable">The Moveable_Drag object to update the position of</param>
		 */
		public virtual void UpdateDraggable (Moveable_Drag draggable)
		{
			draggable.trackValue = GetDecimalAlong (draggable);
			
			DoRegionAudioCheck (draggable);
			if (!onlySnapOnPlayerRelease)
			{
				DoSnapCheck (draggable);
			}
			DoConnectionCheck (draggable);
		}


		/**
		 * <summary>Called whenever an object attached to the track is let go by the player</summary>
		 * <param name = "draggable">The draggable object</param>
		 */
		public void OnLetGo (Moveable_Drag draggable)
		{
			DoSnapCheck (draggable);
		}


		/**
		 * <summary>Corrects the position of an object so that it is placed along the track.</summary>
		 * <param name = "draggable">The Moveable_Drag object to snap onto the track</param>
		 * <param name = "onStart">Is True if the game has just begun (i.e. this function is being run for the first time)</param>
		 */
		public virtual void SnapToTrack (Moveable_Drag draggable, bool onStart)
		{}


		/**
		 * <summary>Checks if the icon that can display when an object is moved along the track remains in the same place as the object moves.</summary>
		 * <returns>True if the icon remains in the same place (always False unless overridden by subclasses)</returns>
		 */
		public virtual bool IconIsStationary ()
		{
			return false;
		}


		/**
		 * <summary>Gets the position of gizmos at a certain position along the track</summary>
		 * <param name = "proportionAlong">The proportio along the track to get the gizmo position of</param>
		 * <returns>The position of the gizmo</returns>
		 */
		public virtual Vector3 GetGizmoPosition (float proportionAlong)
		{
			return Transform.position;
		}


		/**
		 * <summary>Calculates a force to get a draggable object to a given point along the track</summary>
		 * <param name = "draggable">The draggable object</param>
		 * <param name = "targetProportionAlong">How far along the track to calculate a force for</param>
		 * <returns>The force vector, in world space</returns>
		 */
		public virtual Vector3 GetForceToPosition (Moveable_Drag draggable, float targetProportionAlong)
		{
			return Vector3.zero;
		}



		/*
		 * <summary>Gets the current intensity of a draggable object's movement sound</summary>
		 * <param name = "deltaTrackPosition">The change in the draggable object's track position in the last frame</param>
		 * <returns>The current intensity of a draggable object's movement sound</summary>
		 */
		public virtual float GetMoveSoundIntensity (float deltaTrackPosition)
		{
			return 0f;
		}


		/**
		 * <summary>Gets TrackSnapData for a snap point</summary>
		 * <param name="regionID">The ID of the region to get data for</param>
		 * <returns>The TrackSnapData associated with the given ID</returns>
		 */
		public TrackSnapData GetSnapData (int regionID)
		{
			foreach (TrackSnapData trackSnapData in allTrackSnapData)
			{
				if (trackSnapData.ID == regionID)
				{
					return trackSnapData;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the position along the track for the centre of a given region</summary>
		 * <param name="regionID">The ID of the region to get the position of</param>
		 * <returns>The centre-point position along the track of the region</returns>
		 */
		public float GetRegionPositionAlong (int regionID)
		{
			if (allTrackSnapData != null)
			{
				foreach (TrackSnapData trackSnapData in allTrackSnapData)
				{
					if (trackSnapData.ID == regionID)
					{
						return trackSnapData.PositionAlong;
					}
				}
			}

			ACDebug.LogWarning ("Could not find snap point with ID " + regionID + " on Track " + this, this);
			return 0f;
		}


		/**
		 * <summary>Checks if a position along the track is within a given track region</summary>
		 * <param name = "trackValue">The distance along the track, as a decimal of its total length</param>
		 * <param name = "snapID">The ID number of the snap region</param>
		 * <returns>True if the position along the track is within the region</region>
		 */
		public bool IsWithinTrackRegion (float trackValue, int regionID)
		{
			foreach (TrackSnapData trackSnapData in allTrackSnapData)
			{
				if (trackSnapData.ID == regionID)
				{
					return trackSnapData.IsWithinRegion (trackValue);
				}
			}
			return false;
		}


		public virtual float GetForceDotProduct (Vector3 force, Moveable_Drag draggable)
		{
			return 0f;
		}

		#endregion


		#region ProtectedFunctions

		protected void DoRegionAudioCheck (Moveable_Drag draggable)
		{
			TrackSnapData trackSnapData = null;
			for (int i = 0; i < allTrackSnapData.Count; i++)
			{
				if (IsWithinTrackRegion (draggable.trackValue, allTrackSnapData[i].ID))
				{
					trackSnapData = allTrackSnapData[i];
					break;
				}
			}

			if (trackSnapData != null)
			{
				if (draggable.regionID != trackSnapData.ID)
				{
					if (trackSnapData.SoundOnEnter)
					{
						AudioSource.PlayClipAtPoint (trackSnapData.SoundOnEnter, trackSnapData.GetWorldPosition (this));
					}
					draggable.regionID = trackSnapData.ID;
				}
			}
			else
			{
				draggable.regionID = -1;
			}
		}


		protected virtual void AssignColliders (Moveable_Drag draggable)
		{
			if (UsesEndColliders && draggable.minCollider && draggable.maxCollider)
			{
				draggable.maxCollider.transform.rotation = Quaternion.AngleAxis (90f, draggable.maxCollider.transform.right) * draggable.maxCollider.transform.rotation;
				draggable.minCollider.transform.rotation = Quaternion.AngleAxis (90f, draggable.minCollider.transform.right) * draggable.minCollider.transform.rotation;

				if (colliderMaterial)
				{
					draggable.maxCollider.material = colliderMaterial;
					draggable.minCollider.material = colliderMaterial;
				}

				draggable.maxCollider.transform.parent = Transform;
				draggable.minCollider.transform.parent = Transform;

				draggable.maxCollider.name = draggable.name + "_UpperLimit";
				draggable.minCollider.name = draggable.name + "_LowerLimit";
			}

			LimitCollisions (draggable);
		}


		protected void DoSnapCheck (Moveable_Drag draggable)
		{
			if (doSnapping && (!draggable.UsesRigidbody || !draggable.IsAutoMoving ()) && !draggable.IsHeld)
			{
				SnapToNearest (draggable);
			}
		}


		protected void DoConnectionCheck (Moveable_Drag draggable)
		{
			if (!TypeSupportsSnapConnections ())
			{
				return;
			}

			foreach (TrackSnapData trackSnapData in allTrackSnapData)
			{
				float dist = trackSnapData.GetDistanceFrom (draggable.GetPositionAlong ());
				if (Mathf.Abs (dist) < 0.01f)
				{
					trackSnapData.EvaluateConnectionPoints (this, draggable, KickStarter.playerInput.GetDragForce (draggable));
				}
			}
		}


		protected void SnapToNearest (Moveable_Drag draggable)
		{
			int bestIndex = -1;
			float minDistanceFrom = Mathf.Infinity;

			for (int i=0; i<allTrackSnapData.Count; i++)
			{
				float thisDistanceFrom = allTrackSnapData[i].GetDistanceFrom (draggable.trackValue);
				if (thisDistanceFrom < minDistanceFrom)
				{
					bestIndex = i;
					minDistanceFrom = thisDistanceFrom;
				}
			}

			if (bestIndex >= 0)
			{
				allTrackSnapData[bestIndex].MoveTo (draggable, snapSpeed);
			}
		}


		protected void LimitCollisions (Moveable_Drag draggable)
		{
			Collider[] allColliders = FindObjectsOfType (typeof(Collider)) as Collider[];
			Collider[] dragColliders = draggable.GetComponentsInChildren <Collider>();

			// Disable all collisions on max/min colliders
			if (draggable.minCollider && draggable.maxCollider)
			{
				foreach (Collider _collider in allColliders)
				{
					if (_collider.enabled)
					{
						if (_collider != draggable.minCollider && draggable.minCollider.enabled)
						{
							Physics.IgnoreCollision (_collider, draggable.minCollider, true);
						}
						if (_collider != draggable.maxCollider && draggable.maxCollider.enabled)
						{
							Physics.IgnoreCollision (_collider, draggable.maxCollider, true);
						}
					}
				}
			}

			// Set collisions on draggable's colliders
			foreach (Collider _collider in allColliders)
			{
				if (_collider.GetComponent <AC_Trigger>()) continue;

				foreach (Collider dragCollider in dragColliders)
				{
					if (_collider == dragCollider)
					{
						continue;
					}

					bool result = true;

					if ((draggable.minCollider && draggable.minCollider == _collider) || (draggable.maxCollider && draggable.maxCollider == _collider))
					{
						result = false;
					}
					else if (KickStarter.player && _collider.gameObject == KickStarter.player.gameObject)
					{
						result = draggable.ignorePlayerCollider;
					}
					else if (_collider.GetComponent <Rigidbody>() && _collider.gameObject != draggable.gameObject)
					{
						if (_collider.GetComponent <Moveable>())
						{
							result = draggable.ignoreMoveableRigidbodies;
						}
						else
						{
							result = false;
						}
					}

					if (_collider.enabled && dragCollider.enabled)
					{
						Physics.IgnoreCollision (_collider, dragCollider, result);
					}
				}
			}

			// Enable collisions between max/min collisions and draggable's colliders
			if (draggable.minCollider && draggable.maxCollider)
			{
				foreach (Collider _collider in dragColliders)
				{
					if (_collider.enabled && draggable.minCollider.enabled)
					{
						Physics.IgnoreCollision (_collider, draggable.minCollider, false);
					}
					if (_collider.enabled && draggable.maxCollider.enabled)
					{
						Physics.IgnoreCollision (_collider, draggable.maxCollider, false);
					}
				}
			}
		}


		protected Vector3 RotatePointAroundPivot (Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			return rotation * (point - pivot) + pivot;
		}

		#endregion


		#region GetSet		

		/** Checks if the track is on a loop */
		public virtual bool Loops
		{
			get
			{
				return false;
			}
		}


		/**
		 * If True, end-colliders are generated to prevent draggable objects from leaving the track's boundaries
		 */
		public virtual bool UsesEndColliders
		{
			get
			{
				return false;
			}
		}


		/** A cache of the tracks's transform component */
		public Transform Transform
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