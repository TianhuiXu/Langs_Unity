/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Moveable.cs"
 * 
 *	This script is attached to any gameObject that is to be transformed
 *	during gameplay via the action ActionTransform.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script provides functions to move or transform the GameObject it is attached to.
	 * It is used by the "Object: Transform" Action to move objects without scripting.
	 */
	[AddComponentMenu ("Adventure Creator/Misc/Moveable")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_moveable.html")]
	public class Moveable : MonoBehaviour
	{

		#region Variables

		protected float positionChangeTime;
		protected float positionStartTime;
		protected AnimationCurve positionTimeCurve;
		protected MoveMethod positionMethod;

		protected Vector3 startPosition;
		protected Vector3 endPosition;
		protected bool inWorldSpace;

		protected float rotateChangeTime;
		protected float rotateStartTime;
		protected AnimationCurve rotateTimeCurve;
		protected MoveMethod rotateMethod;
		protected bool doEulerRotation = false;

		protected Vector3 startEulerRotation;
		protected Vector3 endEulerRotation;

		protected Quaternion startRotation;
		protected Quaternion endRotation;

		protected float scaleChangeTime;
		protected float scaleStartTime;
		protected AnimationCurve scaleTimeCurve;
		protected MoveMethod scaleMethod;

		protected Vector3 startScale;
		protected Vector3 endScale;

		protected Char character;
		protected Rigidbody _rigidbody;
		protected Rigidbody2D _rigidbody2D;

		[SerializeField] private bool _moveWithRigidbody = false;
		/** If True, then movement will not occur if it results in a collision */
		public bool predictCollisions;
		private Transform _transform;
		private RaycastHit2D[] raycastHit2Ds = new RaycastHit2D[5];

		#endregion


		#region UnityStandards

		protected virtual void Awake ()
		{
			_rigidbody = GetComponent<Rigidbody> ();
			_rigidbody2D = GetComponent<Rigidbody2D> ();
		}


		protected virtual void OnEnable ()
		{
			EventManager.OnManuallyTurnACOff += StopMoving;
		}


		protected virtual void OnDisable ()
		{
			EventManager.OnManuallyTurnACOff -= StopMoving;
		}


		protected void Update ()
		{
			if (positionChangeTime > 0f)
			{
				if (Time.time < positionStartTime + positionChangeTime)
				{
					if (inWorldSpace)
					{
						SetPosition ((positionMethod == MoveMethod.Curved)
							? Vector3.Slerp (startPosition, endPosition, AdvGame.Interpolate (positionStartTime, positionChangeTime, positionMethod, positionTimeCurve))
							: AdvGame.Lerp (startPosition, endPosition, AdvGame.Interpolate (positionStartTime, positionChangeTime, positionMethod, positionTimeCurve)));
					}
					else
					{
						SetLocalPosition ((positionMethod == MoveMethod.Curved)
							? Vector3.Slerp (startPosition, endPosition, AdvGame.Interpolate (positionStartTime, positionChangeTime, positionMethod, positionTimeCurve))
							: AdvGame.Lerp (startPosition, endPosition, AdvGame.Interpolate (positionStartTime, positionChangeTime, positionMethod, positionTimeCurve)));
					}
				}
				else
				{
					if (inWorldSpace)
					{
						SetPosition (endPosition);
					}
					else
					{
						SetLocalPosition (endPosition);
					}

					positionChangeTime = 0f;
				}
			}

			if (rotateChangeTime > 0f)
			{
				if (Time.time < rotateStartTime + rotateChangeTime)
				{
					if (doEulerRotation)
					{
						if (inWorldSpace)
						{
							Transform.eulerAngles = (rotateMethod == MoveMethod.Curved)
								? Vector3.Slerp (startEulerRotation, endEulerRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve))
								: AdvGame.Lerp (startEulerRotation, endEulerRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve));
						}
						else
						{
							Transform.localEulerAngles = (rotateMethod == MoveMethod.Curved)
								? Vector3.Slerp (startEulerRotation, endEulerRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve))
								: AdvGame.Lerp (startEulerRotation, endEulerRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve));
						}
					}
					else
					{
						if (inWorldSpace)
						{
							SetRotation ((rotateMethod == MoveMethod.Curved)
								? Quaternion.Slerp (startRotation, endRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve))
								: AdvGame.Lerp (startRotation, endRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve)));
						}
						else
						{
							SetLocalRotation ((rotateMethod == MoveMethod.Curved)
								? Quaternion.Slerp (startRotation, endRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve))
								: AdvGame.Lerp (startRotation, endRotation, AdvGame.Interpolate (rotateStartTime, rotateChangeTime, rotateMethod, rotateTimeCurve)));
						}
					}
				}
				else
				{
					if (doEulerRotation)
					{
						if (inWorldSpace)
						{
							Transform.eulerAngles = endEulerRotation;
						}
						else
						{
							Transform.localEulerAngles = endEulerRotation;
						}
					}
					else
					{
						if (inWorldSpace)
						{
							SetRotation (endRotation);
						}
						else
						{
							SetLocalRotation (endRotation);
						}
					}

					if (character == null)
					{
						character = GetComponent<Char> ();
					}

					if (character)
					{
						character.SetLookDirection (character.TransformRotation * Vector3.forward, true);
						character.StopTurning ();
					}

					rotateChangeTime = 0f;
				}
			}

			if (scaleChangeTime > 0f)
			{
				if (Time.time < scaleStartTime + scaleChangeTime)
				{
					if (scaleMethod == MoveMethod.Curved)
					{
						Transform.localScale = Vector3.Slerp (startScale, endScale, AdvGame.Interpolate (scaleStartTime, scaleChangeTime, scaleMethod, scaleTimeCurve));
					}
					else
					{
						Transform.localScale = AdvGame.Lerp (startScale, endScale, AdvGame.Interpolate (scaleStartTime, scaleChangeTime, scaleMethod, scaleTimeCurve));
					}
				}
				else
				{
					Transform.localScale = endScale;
					scaleChangeTime = 0f;
				}
			}
		}

		#endregion


		#region PublicFunctions

		/** Halts the GameObject, if it is being moved by this script. */
		public void StopMoving ()
		{
			positionChangeTime = rotateChangeTime = scaleChangeTime = 0f;
		}


		public bool IsMoving (TransformType transformType)
		{
			switch (transformType)
			{
				case TransformType.Translate:
				case TransformType.CopyMarker:
					return positionChangeTime > 0f;

				case TransformType.Rotate:
					return rotateChangeTime > 0f;

				case TransformType.Scale:
					return scaleChangeTime > 0f;

				default:
					return false;
			}
		}


		public bool IsMoving ()
		{
			return positionChangeTime > 0f || rotateChangeTime > 0f || scaleChangeTime > 0f;
		}


		/** Halts the GameObject, and sets its Transform to its target values, if it is being moved by this script. */
		public void EndMovement ()
		{
			if (positionChangeTime > 0f)
			{
				Transform.localPosition = endPosition;
			}

			if (rotateChangeTime > 0f)
			{
				if (doEulerRotation)
				{
					Transform.localEulerAngles = endEulerRotation;
				}
				else
				{
					Transform.localRotation = endRotation;
				}
			}

			if (scaleChangeTime > 0f)
			{
				Transform.localScale = endScale;
			}

			StopMoving ();
		}


		/**
		 * <summary>Moves the GameObject by referencing a Vector3 as its target Transform.</summary>
		 * <param name = "_newVector">The target values of either the GameObject's position, rotation or scale</param>
		 * <param name = "_moveMethod">The interpolation method by which the GameObject moves (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_inWorldSpace">If True, the movement will use world-space co-ordinates</param>
		 * <param name = "_transitionTime">The time, in seconds, that the movement should take place over</param>
		 * <param name = "_transformType">The way in which the GameObject should be transformed (Translate, Rotate, Scale)</param>
		 * <param name = "_doEulerRotation">If True, then the GameObject's eulerAngles will be directly manipulated. Otherwise, the rotation as a Quaternion will be affected.</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the movement speed will follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 * <param name = "clearExisting">If True, then existing transforms will be stopped before new transforms will be made</param>
		 */
		public void Move (Vector3 _newVector, MoveMethod _moveMethod, bool _inWorldSpace, float _transitionTime, TransformType _transformType, bool _doEulerRotation, AnimationCurve _timeCurve, bool clearExisting)
		{
			if (_transformType == TransformType.Translate && predictCollisions)
			{
				Vector3 _endPosition = inWorldSpace ? _newVector : Transform.TransformVector (_newVector);
				Vector3 _direction = _endPosition - Transform.position;
				float _distance = (_endPosition - Transform.position).magnitude;

				if (_rigidbody)
				{
					RaycastHit hitInfo;
					if (_rigidbody.SweepTest (_direction, out hitInfo, _distance, QueryTriggerInteraction.Ignore))
					{
						ACDebug.LogWarning ("Cannot move " + this.name + " to " + _endPosition + " as it will collide with " + hitInfo.collider + " at " + hitInfo.point, this);
						return;
					}
				}
				else if (_rigidbody2D)
				{
					int numHits = _rigidbody2D.Cast (_direction, raycastHit2Ds, _distance);
					for (int i = 0; i < numHits; i++)
					{
						if (!raycastHit2Ds[i].collider.isTrigger)
						{
							ACDebug.LogWarning ("Cannot move " + this.name + " to " + _endPosition + " as it will collide with " + raycastHit2Ds[i].collider + " at " + raycastHit2Ds[i].point, this);
							return;
						}
					}
				}
				else
				{
					ACDebug.LogWarning ("A Rigidbody component is required on " + this.name + " to prevent collisions as a movement command is issued", this);
				}
			}

			if (_rigidbody && !_rigidbody.isKinematic)
			{
				_rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
			}

			inWorldSpace = _inWorldSpace;

			if (_transitionTime <= 0f)
			{
				if (clearExisting)
				{
					positionChangeTime = rotateChangeTime = scaleChangeTime = 0f;
				}

				if (_transformType == TransformType.Translate)
				{
					if (inWorldSpace)
					{
						SetPosition (_newVector);
					}
					else
					{
						SetLocalPosition (_newVector);
					}
					positionChangeTime = 0f;
				}
				else if (_transformType == TransformType.Rotate)
				{
					if (inWorldSpace)
					{
						Transform.eulerAngles = _newVector;
					}
					else
					{
						Transform.localEulerAngles = _newVector;
					}
					rotateChangeTime = 0f;
				}
				else if (_transformType == TransformType.Scale)
				{
					if (inWorldSpace)
					{
						Transform oldParent = Transform.parent;
						Transform.SetParent (null, true);
						Transform.localScale = _newVector;
						if (oldParent) transform.SetParent (oldParent, true);
					}
					else
					{
						Transform.localScale = _newVector;
					}
					scaleChangeTime = 0f;
				}
			}
			else
			{
				if (_transformType == TransformType.Translate)
				{
					startPosition = endPosition = (inWorldSpace) ? Transform.position : Transform.localPosition;
					endPosition = _newVector;

					positionMethod = _moveMethod;

					positionChangeTime = _transitionTime;
					positionStartTime = Time.time;

					positionMethod = _moveMethod;
					if (positionMethod == MoveMethod.CustomCurve)
					{
						positionTimeCurve = _timeCurve;
					}
					else
					{
						positionTimeCurve = null;
					}

					if (startPosition == endPosition)
					{
						Move (_newVector, _moveMethod, _inWorldSpace, 0f, _transformType, _doEulerRotation, _timeCurve, clearExisting);
						return;
					}

					if (clearExisting)
					{
						rotateChangeTime = scaleChangeTime = 0f;
					}
				}
				else if (_transformType == TransformType.Rotate)
				{
					startEulerRotation = endEulerRotation = (inWorldSpace) ? Transform.eulerAngles : Transform.localEulerAngles;
					startRotation = endRotation = (inWorldSpace) ? Transform.rotation : Transform.localRotation;
					endRotation = Quaternion.Euler (_newVector);
					endEulerRotation = _newVector;

					doEulerRotation = _doEulerRotation;
					rotateMethod = _moveMethod;

					rotateChangeTime = _transitionTime;
					rotateStartTime = Time.time;

					rotateMethod = _moveMethod;
					if (rotateMethod == MoveMethod.CustomCurve)
					{
						rotateTimeCurve = _timeCurve;
					}
					else
					{
						rotateTimeCurve = null;
					}

					if ((doEulerRotation && startEulerRotation == endEulerRotation) ||
						(!doEulerRotation && startRotation == endRotation))
					{
						Move (_newVector, _moveMethod, _inWorldSpace, 0f, _transformType, _doEulerRotation, _timeCurve, clearExisting);
						return;
					}

					if (clearExisting)
					{
						positionChangeTime = scaleChangeTime = 0f;
					}

				}
				else if (_transformType == TransformType.Scale)
				{
					if (inWorldSpace)
					{
						ACDebug.LogWarning ("Cannot change the world-space scale value of " + gameObject.name + " over time.", gameObject);
					}

					startScale = endScale = Transform.localScale;
					endScale = _newVector;

					scaleMethod = _moveMethod;

					scaleChangeTime = _transitionTime;
					scaleStartTime = Time.time;

					scaleMethod = _moveMethod;
					if (scaleMethod == MoveMethod.CustomCurve)
					{
						scaleTimeCurve = _timeCurve;
					}
					else
					{
						scaleTimeCurve = null;
					}

					if (startScale == endScale)
					{
						Move (_newVector, _moveMethod, _inWorldSpace, 0f, _transformType, _doEulerRotation, _timeCurve, clearExisting);
						return;
					}

					if (clearExisting)
					{
						positionChangeTime = rotateChangeTime = 0f;
					}
				}
			}
		}


		/**
		 * <summary>Moves the GameObject by referencing a Marker component as its target Transform.</summary>
		 * <param name = "_marker">A Marker whose position, rotation and scale will be the target values of the GameObject</param>
		 * <param name = "_moveMethod">The interpolation method by which the GameObject moves (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_inWorldSpace">If True, the movement will use world-space co-ordinates</param>
		 * <param name = "_transitionTime">The time, in seconds, that the movement should take place over</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the movement speed will follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void Move (Marker _marker, MoveMethod _moveMethod, bool _inWorldSpace, float _transitionTime, AnimationCurve _timeCurve)
		{
			if (predictCollisions)
			{
				Vector3 _endPosition = inWorldSpace ? _marker.Position : Transform.TransformVector (_marker.Transform.localPosition);
				Vector3 _direction = _endPosition - Transform.position;
				float _distance = (_endPosition - Transform.position).magnitude;

				if (_rigidbody)
				{
					RaycastHit hitInfo;
					if (_rigidbody.SweepTest (_direction, out hitInfo, _distance, QueryTriggerInteraction.Ignore))
					{
						ACDebug.LogWarning ("Cannot move " + this.name + " to " + _endPosition + " as it will collide with " + hitInfo.collider + " at " + hitInfo.point, this);
						return;
					}
				}
				else if (_rigidbody2D)
				{
					RaycastHit2D[] raycastHit2Ds = new RaycastHit2D[0];
					int numHits = _rigidbody2D.Cast (_direction, raycastHit2Ds, _distance);
					for (int i = 0; i < numHits; i++)
					{
						if (!raycastHit2Ds[i].collider.isTrigger)
						{
							ACDebug.LogWarning ("Cannot move " + this.name + " to " + _endPosition + " as it will collide with " + raycastHit2Ds[i].collider + " at " + raycastHit2Ds[i].point, this);
							return;
						}
					}
				}
				else
				{
					ACDebug.LogWarning ("A Rigidbody component is required on " + this.name + " to prevent collisions as a movement command is issued", this);
				}
			}

			if (_rigidbody && !_rigidbody.isKinematic)
			{
				_rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
			}

			inWorldSpace = _inWorldSpace;

			if (_transitionTime <= 0f)
			{
				positionChangeTime = rotateChangeTime = scaleChangeTime = 0f;

				if (inWorldSpace)
				{
					Transform oldParent = Transform.parent;
					Transform.SetParent (null, true);
					Transform.localScale = _marker.Transform.lossyScale;
					SetPosition (_marker.Position);
					SetRotation (_marker.Rotation);
					if (oldParent) Transform.SetParent (oldParent, true);
				}
				else
				{
					SetLocalPosition (_marker.Transform.localPosition);
					Transform.localEulerAngles = _marker.Transform.localEulerAngles;
					Transform.localScale = _marker.Transform.localScale;
				}
			}
			else
			{
				doEulerRotation = false;
				positionMethod = rotateMethod = scaleMethod = _moveMethod;

				if (inWorldSpace)
				{
					startPosition = Transform.position;
					startRotation = Transform.rotation;
					startScale = Transform.localScale;

					endPosition = _marker.Position;
					endRotation = _marker.Rotation;
					endScale = _marker.Transform.localScale;
				}
				else
				{
					startPosition = Transform.localPosition;
					startRotation = Transform.localRotation;
					startScale = Transform.localScale;

					endPosition = _marker.Transform.localPosition;
					endRotation = _marker.Transform.localRotation;
					endScale = _marker.Transform.localScale;
				}

				if (startPosition == endPosition && startRotation == endRotation && startScale == endScale)
				{
					Move (_marker, _moveMethod, _inWorldSpace, 0f, _timeCurve);
					return;
				}

				positionChangeTime = rotateChangeTime = scaleChangeTime = _transitionTime;
				positionStartTime = rotateStartTime = scaleStartTime = Time.time;

				if (_moveMethod == MoveMethod.CustomCurve)
				{
					positionTimeCurve = _timeCurve;
					rotateTimeCurve = _timeCurve;
					scaleTimeCurve = _timeCurve;
				}
				else
				{
					positionTimeCurve = rotateTimeCurve = scaleTimeCurve = null;
				}
			}
		}


		/**
		 * <summary>Updates a MoveableData class with its own variables that need saving.</summary>
		 * <param name = "saveData">The original MoveableData class</param>
		 * <returns>The updated MoveableData class</returns>
		 */
		public MoveableData SaveData (MoveableData saveData)
		{
			if (positionChangeTime > 0f)
			{
				saveData.LocX = endPosition.x;
				saveData.LocY = endPosition.y;
				saveData.LocZ = endPosition.z;
			}

			if (rotateChangeTime > 0f)
			{
				saveData.doEulerRotation = doEulerRotation;

				if (doEulerRotation)
				{
					saveData.LocX = endEulerRotation.x;
					saveData.LocY = endEulerRotation.y;
					saveData.LocZ = endEulerRotation.z;
				}
				else
				{
					saveData.RotW = endRotation.w;
					saveData.RotX = endRotation.x;
					saveData.RotY = endRotation.y;
					saveData.RotZ = endRotation.z;
				}
			}
			else
			{
				saveData.doEulerRotation = true;
			}

			if (scaleChangeTime > 0f)
			{
				saveData.ScaleX = endScale.x;
				saveData.ScaleY = endScale.y;
				saveData.ScaleZ = endScale.z;
			}

			saveData.inWorldSpace = inWorldSpace;

			return saveData;
		}


		/**
		 * <summary>Updates its own variables from a MoveableData class.</summary>
		 * <param name = "saveData">The MoveableData class to load from</param>
		 */
		public void LoadData (MoveableData saveData)
		{
			inWorldSpace = saveData.inWorldSpace;

			if (!saveData.doEulerRotation)
			{
				if (inWorldSpace)
				{
					SetRotation (new Quaternion (saveData.RotW, saveData.RotX, saveData.RotY, saveData.RotZ));
				}
				else
				{
					Transform.localRotation = new Quaternion (saveData.RotW, saveData.RotX, saveData.RotY, saveData.RotZ);
				}
			}

			StopMoving ();
		}


		public Vector3 GetTargetPosition ()
		{
			if (positionChangeTime > 0f)
			{
				if (inWorldSpace)
				{
					return endPosition;
				}
				else
				{
					return Transform.TransformVector (endPosition);
				}
			}
			return Vector3.zero;
		}

		#endregion


		#region ProtectedFunctions

		protected void Kill ()
		{
			StopMoving ();
		}

		#endregion


		#region PrivateFunctions

		private void SetLocalPosition (Vector3 localPosition)
		{
			if (_moveWithRigidbody)
			{
				if (_rigidbody)
				{
					_rigidbody.MovePosition (Transform.TransformVector (localPosition));
					return;
				}

				if (_rigidbody2D)
				{
					_rigidbody2D.MovePosition (Transform.TransformVector (localPosition));
					return;
				}
			}

			Transform.localPosition = localPosition;
		}


		private void SetPosition (Vector3 position)
		{
			if (_moveWithRigidbody)
			{
				if (_rigidbody)
				{
					_rigidbody.MovePosition (position);
					return;
				}

				if (_rigidbody2D)
				{
					_rigidbody2D.MovePosition (position);
					return;
				}
			}

			Transform.position = position;
		}


		private void SetRotation (Quaternion rotation)
		{
			if (_moveWithRigidbody)
			{
				if (_rigidbody)
				{
					_rigidbody.MoveRotation (rotation);
					return;
				}

				#if UNITY_2019_OR_LATER
				if (_rigidbody2D)
				{
					_rigidbody2D.MoveRotation (rotation);
					return;
				}
				#endif
			}

			Transform.rotation = rotation;
		}


		private void SetLocalRotation (Quaternion localRotation)
		{
			if (_moveWithRigidbody)
			{
				Quaternion rotation = Transform.parent ? transform.parent.rotation * localRotation : localRotation;

				if (_rigidbody)
				{
					_rigidbody.MoveRotation (rotation);
					return;
				}

				#if UNITY_2019_OR_LATER
				if (_rigidbody2D)
				{
					_rigidbody2D.MoveRotation (rotation);
					return;
				}
				#endif
			}

			Transform.localRotation = localRotation;
		}

		#endregion


		#region GetSet

		/** The attached Rigidbody */
		public Rigidbody Rigidbody
		{
			get
			{
				return _rigidbody;
			}
		}


		/** A cache of the Hotspot's transform component */
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