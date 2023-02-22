/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"GameCameraThirdPerson.cs"
 * 
 *	This is attached to a scene-based camera, similar to GameCamera and GameCamera2D.
 *	It should not be a child of the Player, but instead scene-specific.
 * 
 */

using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[AddComponentMenu ("Adventure Creator/Camera/Third-person camera")]
	public class GameCameraThirdPerson : _Camera
	{

		#region Variables

		/** The name of the Input axis that controls spin rotation, if isDragControlled = False */
		public string spinAxis = "CursorHorizontal";
		/** The name of the Input axis that controls pitch rotation, if isDragControlled = False */
		public string pitchAxis = "CursorVertical";
		/** If true, then the cursor must be locked before input will take effect */
		public bool requireLockedCursor = false;
		/** If True, then spin and pitch can be altered when gameplay is blocked */
		[FormerlySerializedAs ("canRotateDuringCutscenes")] public bool allowCutsceneControl = false;
		/** If True, then spin and pitch can be altered when a Conversation is active */
		[FormerlySerializedAs ("canRotateDuringConversations")] public bool allowConversationControl = false;
		/** The direction to face by default */
		public Vector2 intialDirectionAngles = Vector2.zero;

		public enum InitialDirection { BehindTarget, PreviousCameraSpin, PreviousCameraDirection, PreviousCameraRelativePosition, SetAngles, None };
		/** How to set the original direction (BehindTarget, PreviousCameraSpin, PreviousCameraDirection, PreviousCameraRelativePosition, SetAngles, None) */
		public InitialDirection initialDirection = InitialDirection.BehindTarget;
		/** If True, the camera will revert to its initial direction each time it is switched to */
		public bool initialOnSwitch = false;

		/** The influence that input has on motion (X = spin, Y = pitch) */
		public Vector2 inputInfluence = new Vector2 (3f, 3f);
		/** The maximum amount of input per-frame (X = spin, Y = pitch) */
		public Vector2 maxInput = new Vector2 (3f, 3f);

		[SerializeField] private RegionInfluence topRegion = new RegionInfluence (1.5f, 2f);
		[SerializeField] private RegionInfluence midRegion = new RegionInfluence (1.5f, 2f);
		[SerializeField] private RegionInfluence bottomRegion = new RegionInfluence (1.5f, 2f);

		private float normalDistance = 2f;
		/** How much that target's speed influences distance */
		public float fastDistanceFactor = 2f;
		/** Acceleration when changing distance */
		public float distanceAcceleration = 5f;
		/** The acceleration when changing distance due to the target's speed */
		public float distanceInfluenceAcceleration = 10f;
		private float actualDistance;
		private LerpUtils.FloatLerp distanceLerp = new LerpUtils.FloatLerp ();
		private float distanceInfluence;
		private LerpUtils.FloatLerp distanceInfluenceLerp = new LerpUtils.FloatLerp ();

		/** If True, the target's movement influenced the spin angle */
		public bool targetMovementInfluencesSpin = true;
		public float targetSpinAcceleration = 2f;
		/** The amount by which the target's movement influences the spin angle */
		public float targetSpinInfluence = 1f;
		/** How long the target can move before they influence the spin angle */
		public float targetSpinDelay = 2f;
		/** The amount of curvature in the corners when the spin angle is constrained */
		public float edgeCurvature = 0f;

		private readonly AnimationCurve edgeCurve = new AnimationCurve (new Keyframe (0, 0),
																			new Keyframe (0.2f, 0.003f),
																			new Keyframe (0.715f, 0.263f, 1f, 1f),
																			new Keyframe (0.9363f, 0.648f, 2.5f, 2.5f), 
																			new Keyframe (1, 1, 11, 0));

		/** If True, the target's movement influenced the pitch angle */
		public bool targetMovementInfluencesPitch = true;
		/** The amount by which the target's movement influences the pitch angle */
		public float targetPitchInfluence = 1f;
		/** The pitch angle to move towards when influenced by the target's movement */
		public float targetPitchRestAngle = 0f;
		private float targetSpinTimer;
		private Vector3 lastTargetPoint;

		/** The acceleration of pitch rotations */
		[FormerlySerializedAs ("pitchAccleration")] public float pitchAcceleration = 8f;
		/** The minimum pitch angle */
		public float minPitch = -80f;
		/** The maximum pitch angle */
		public float maxPitch = 80f;
		private float actualPitchAngle = 0f;
		private float targetPitchAngle = 0f;
		private LerpUtils.FloatLerp pitchLerp = new LerpUtils.FloatLerp ();

		/** If True, then the spin angle is constrained */
		public bool doLimitSpin = false;
		/** The minimum spin angle, if doLimitSpin = True */
		public float minSpin = -80f;
		/** The maximum spin angle, if doLimitSpin = True */
		public float maxSpin = 80f;
		/* An offset angle to apply to all spin angles, if doLimitSpin = True */
		public float spinLimitOffset = 0f;
		/** The acceleration of spin rotations */
		[FormerlySerializedAs ("spinAccleration")] public float spinAcceleration = 8f;
		private readonly LerpUtils.FloatLerp spinLerp = new LerpUtils.FloatLerp ();
		private float actualSpinAngle = 0f;
		private float targetSpinAngle = 0f;

		private LerpUtils.Vector3Lerp targetVelocityLerp = new LerpUtils.Vector3Lerp ();
		private Vector3 targetVelocity;

		private readonly AnimationCurve heightOffsetCurve = new AnimationCurve (new Keyframe (-1, -1), new Keyframe (1, 1));
		/** The global offset to apply to all height values */
		public float heightOffset = 0f;
		private float pivotHeightOffset;
		/** An offset to apply to the camera's horizontal position. The Camera must be a child of this component for the effect to work */
		public float horizontalOffset = 0f;

		/** If True, then the camera will detect Colliders to try to avoid clipping through walls */
		public bool detectCollisions = true;
		/** The LayerMask used to detect collisions, if detectCollisions = True */
		public LayerMask collisionLayerMask = new LayerMask ();
		/** The minimum distance to keep from its target, if detectCollisions = True */
		public float minimumDistance = 0.2f;
		/** The distance to keep from walls, if detectCollisions = True; */
		[FormerlySerializedAs ("collisionRadius")] public float wallSeparator = 0.1f;
		private RaycastHit collisionHit;

		private Player player;

		/** The limit to which changes in the target's height influence the pitch angle */
		public float heightChangeInfluenceLimit = 10f;
		private float heightChangeInfluence;
		/** The distance of rays used to detect collisions */
		public float maxRaycastDistance = 5f;

		private Transform lookAtOverride = null;
		private float lookAtTransitionTime;
		private float lookAtTransitionTotalTime;
		private readonly AnimationCurve lookAtCurve = new AnimationCurve (new Keyframe (0, 0), new Keyframe (1, 1));
		private float lookAtInfluence = 0f;

		/** The lower relative limit to zooming */
		public float zoomLimit = 0.5f;
		/** The axis that controls zooming */
		public string zoomAxis = "Mouse ScrollWheel";
		/** The speed of zooming */
		public float zoomSpeed = 1f;
		/** The method by which to allow zooming (None, AxisControlsDistance, ButtonControlsFOV) */
		public ZoomMethod zoomMethod = ZoomMethod.None;
		private float originalFOV = 0f;
		public enum ZoomMethod { None, AxisControlsDistance, ButtonControlsFOV };
		private LerpUtils.FloatLerp zoomLerp = new LerpUtils.FloatLerp ();
		private float zoomAmount = 1f;

		private float autoMoveSpeed = 0f;
		private Vector2 autoMoveTargetAngles = new Vector2 (0f, 0f);
		private LerpUtils.FloatLerp autoMoveSpinLerp = new LerpUtils.FloatLerp ();
		private LerpUtils.FloatLerp autoMovePitchLerp = new LerpUtils.FloatLerp ();

		/** The factor by which the camera accelerates due to input */
		public Vector2 inputSmoothing = new Vector2 (0.3f, 0.3f);
		[SerializeField] private Color gizmoColor = Color.red;
		private Vector2 lastFrameInput;

		/** If True, then focalDistance will match the distance to target */
		public bool focalPointIsTarget = true;

		#endregion


		#region UnityStandards

		protected override void OnEnable ()
		{
			EventManager.OnSetPlayer += OnSetPlayer;
			EventManager.OnSwitchCamera += OnSwitchCamera;
			EventManager.OnOccupyPlayerStart += OnOccupyPlayerStart;
			EventManager.OnFinishLoading += OnFinishLoading;

			base.OnEnable ();
		}


		protected override void OnDisable ()
		{
			EventManager.OnFinishLoading -= OnFinishLoading;
			EventManager.OnOccupyPlayerStart -= OnOccupyPlayerStart;
			EventManager.OnSwitchCamera -= OnSwitchCamera;
			EventManager.OnSetPlayer -= OnSetPlayer;

			base.OnDisable ();
		}


		protected override void Start ()
		{
			base.Start ();

			if (originalFOV == 0f)
			{
				if (Camera)
				{
					if (Camera.transform == transform && !Mathf.Approximately (horizontalOffset, 0f))
					{
						ACDebug.LogWarning ("Camera '" + name + "' shares a GameObject with its Camera component - the Camera component should be a child of the Third-person Camera object", this);
					}

					originalFOV = Camera.fieldOfView;
				}
				else
				{
					ACDebug.LogWarning ("Camera '" + name + "' has no camera component!", this);
				}
			}

			if (KickStarter.player) OnSetPlayer (KickStarter.player);
		}


		private void LateUpdate ()
		{
			lookAtInfluence = GetOverrideInfluence ();

			Vector2 frameInput = Vector2.zero;
			if (CanAcceptInput ())
			{
				UpdateZoom ();
				frameInput = FrameInput * (1f - lookAtInfluence);
			}

			AddOverrideInfluence (lookAtInfluence);

			CalculateTargetVelocity ();

			AddTargetInfluence (frameInput);
			CalculateDistance ();

			AddInputInfluence (frameInput);
			CalculatePitchInfluence ();

			UpdateActualAngles ();
			UpdatePosition ();
			UpdateRotation (lookAtInfluence);
		}


		private void OnDrawGizmosSelected ()
		{
			if (target != null)
			{
				Color backupColor = Gizmos.color;
				Gizmos.color = gizmoColor;
				Gizmos.DrawWireSphere (TargetPoint + Vector3.up * (heightOffset + midRegion.HeightOffset), midRegion.Distance * 0.05f);
				Gizmos.color = backupColor;

				midRegion.OnDrawGizmos (-Vector3.forward, TargetPoint, (minPitch + maxPitch) / 2f, doLimitSpin, OffsetSpinLimits, heightOffset, 0f, gizmoColor);
				if (Mathf.Abs (minPitch - maxPitch) <= 0f)
				{
					return;
				}

				backupColor = Gizmos.color;
				Gizmos.color = gizmoColor;
				Gizmos.DrawLine (TargetPoint + Vector3.up * (heightOffset + midRegion.HeightOffset), TargetPoint + Vector3.up * (heightOffset + topRegion.HeightOffset));
				Gizmos.DrawLine (TargetPoint + Vector3.up * (heightOffset + midRegion.HeightOffset), TargetPoint + Vector3.up * (heightOffset + bottomRegion.HeightOffset));
				Gizmos.color = backupColor;

				topRegion.OnDrawGizmos (-Vector3.forward, TargetPoint, maxPitch, doLimitSpin, OffsetSpinLimits, heightOffset, edgeCurvature, Color.Lerp (gizmoColor, Color.white, 0.3f));
				bottomRegion.OnDrawGizmos (-Vector3.forward, TargetPoint, minPitch, doLimitSpin, OffsetSpinLimits, heightOffset, edgeCurvature, Color.Lerp (gizmoColor, Color.black, 0.3f));
			}
		}

		#endregion


		#region PublicFunctions

		public void SetTarget (Transform target)
		{
			this.target = target;
		}


		public void SetLookAtOverride (Transform lookAtOverride, float transitionTime)
		{
			if (lookAtOverride != null)
			{
				if (lookAtOverride == this.lookAtOverride) return;

				lookAtInfluence = 0f;
				this.lookAtOverride = lookAtOverride;

				if (transitionTime > 0f)
				{
					lookAtTransitionTime = lookAtTransitionTotalTime = transitionTime;
				}
				else
				{
					lookAtTransitionTime = lookAtTransitionTotalTime = 0f;
				}
			}
			else
			{
				ACDebug.LogWarning ("No LookAt transform set!");
			}
		}


		public void ClearLookAtOverride (float transitionTime = 0f)
		{
			if (lookAtOverride == null) return;

			lookAtInfluence = 0f;
			targetSpinAngle = actualSpinAngle;
			targetPitchAngle = actualPitchAngle;

			if (transitionTime > 0f)
			{
				lookAtTransitionTime = lookAtTransitionTotalTime = -transitionTime;
			}
			else
			{
				lookAtOverride = null;
				lookAtTransitionTime = lookAtTransitionTotalTime = 0f;

				UpdateRotation (0f);
			}
		}


		public bool IsLookAtInfluenceComplete ()
		{
			return (lookAtInfluence >= 1f);
		}


		public void SnapToCurrentCamera (bool fromRelativePosition = false)
		{
			ClearLookAtOverride ();

			if (fromRelativePosition)
			{
				targetSpinAngle = GetSpinAngle (FocalPosition - KickStarter.CameraMain.transform.position, KickStarter.CameraMain.transform.right);
			}
			else
			{
				targetSpinAngle = GetSpinAngle (KickStarter.CameraMain.transform.forward, KickStarter.CameraMain.transform.right);
			}
			targetPitchAngle = GetPitchAngle (KickStarter.CameraMain.transform);
			SnapAngle ();
		}


		public void SnapToCurrentCameraSpin (float newPitchAngle = 10f)
		{
			ClearLookAtOverride ();

			targetSpinAngle = GetSpinAngle (KickStarter.CameraMain.transform.forward, KickStarter.CameraMain.transform.right);

			targetPitchAngle = newPitchAngle;
			SnapAngle ();
		}


		public void SnapToDirection (Vector3 forward, Vector3 right)
		{
			float spin = GetSpinAngle (forward, right);
			float pitch = GetPitchAngle (forward, right);

			SnapToRotation (spin, pitch);
			SnapAngle ();
		}


		public void SnapToRotation (float newSpinAngle, float newPitchAngle, bool spinIsRelativeToTarget = false)
		{
			ClearLookAtOverride ();

			if (spinIsRelativeToTarget)
			{
				newSpinAngle += GetSpinAngle (target.forward, target.right);
			}

			Vector2 newAngles = ClampAngles (newPitchAngle, newSpinAngle);

			targetPitchAngle = actualPitchAngle = newAngles.x;
			targetSpinAngle = actualSpinAngle = newAngles.y;

			autoMoveSpeed = 0f;
		}


		public void SnapBehindTarget (float newDistance = 0f)
		{
			if (target == null) return;

			targetSpinAngle = GetSpinAngle (target.forward, target.right);
			targetPitchAngle = 0f;
			SnapAngle ();

			if (newDistance < actualDistance && newDistance > 0f)
			{
				actualDistance = newDistance;
			}
		}


		public Vector2 GenerateTargetAngles (Transform transform)
		{
			float spin = GetSpinAngle (transform.forward, transform.right);
			float pitch = GetPitchAngle (transform);
			return new Vector2 (spin, pitch);
		}


		public void BeginAutoMove (float speed, Vector2 targetAngles, bool relativeToTarget)
		{
			autoMoveSpeed = speed;
			autoMoveTargetAngles = targetAngles;

			if (relativeToTarget && target != null)
			{
				autoMoveTargetAngles.x += GetSpinAngle (target.forward, target.right);
			}

			autoMoveSpinLerp.Reset ();
			autoMovePitchLerp.Reset ();

			if (speed <= 0f)
			{
				SnapToRotation (targetAngles.x, targetAngles.y, relativeToTarget);
			}
		}


		public bool IsAutoMoving ()
		{
			return (autoMoveSpeed > 0f);
		}


		public void SetInitialDirection ()
		{
			switch (initialDirection)
			{
				case InitialDirection.BehindTarget:
					SnapBehindTarget ();
					break;

				case InitialDirection.PreviousCameraSpin:
					SnapToCurrentCameraSpin (0f);
					break;

				case InitialDirection.PreviousCameraDirection:
					SnapToCurrentCamera ();
					break;

				case InitialDirection.PreviousCameraRelativePosition:
					SnapToCurrentCamera (true);
					break;

				case InitialDirection.SetAngles:
					SnapToRotation (intialDirectionAngles.x, intialDirectionAngles.y);
					SnapAngle ();
					break;

				default:
					SnapAngle ();
					break;
			}
		}

		#endregion


		#region CustomEvents

		private void OnSwitchCamera (_Camera oldCamera, _Camera newCamera, float transitionTime)
		{
			if (newCamera == this && initialOnSwitch)
			{
				SetInitialDirection ();
			}
		}


		private void OnSetPlayer (Player _player)
		{
			player = _player;
			if (targetIsPlayer && Player != null)
			{
				target = player.transform;
			}

			SetInitialDirection ();
		}


		private void OnOccupyPlayerStart (Player _player, PlayerStart playerStart)
		{
			SnapAngle ();
		}


		private void OnFinishLoading ()
		{
			SnapAngle ();
		}

		#endregion


		#region PrivateFunctions

		private float GetOverrideInfluence ()
		{
			if (lookAtTransitionTotalTime != 0f)
			{
				if (lookAtTransitionTime > 0f)
				{
					lookAtTransitionTime -= Time.deltaTime;
					if (lookAtTransitionTime <= 0f)
					{
						lookAtTransitionTime = 0f;
					}
				}
				else if (lookAtTransitionTime < 0f)
				{
					lookAtTransitionTime += Time.deltaTime;
					if (lookAtTransitionTime >= 0f)
					{
						lookAtTransitionTime = 0f;
						lookAtOverride = null;
					}
				}

				float lookAtProportion = lookAtTransitionTime / lookAtTransitionTotalTime;
				if (lookAtTransitionTime >= 0f && lookAtOverride != null)
				{
					lookAtProportion = 1f - lookAtProportion;
				}

				if (lookAtTransitionTime == 0f)
				{
					lookAtTransitionTotalTime = 0f;
				}

				return lookAtCurve.Evaluate (Mathf.Abs (lookAtProportion));

			}
			else if (lookAtOverride != null)
			{
				return 1f;
			}

			return 0f;
		}


		private void AddOverrideInfluence (float proportion)
		{
			if (lookAtOverride != null)
			{
				Vector3 direction = lookAtOverride.position - transform.position;
				Vector3 right = Vector3.Cross (direction, Vector3.down);

				float overrideSpinAngle = GetSpinAngle (direction, right);
				float mergedSpinAngle = (targetSpinAngle * (1f - proportion)) + (overrideSpinAngle * proportion);
				actualSpinAngle = mergedSpinAngle;

				float overridePitchAngle = GetPitchAngle (direction, right);
				float mergedPitchAngle = (targetPitchAngle * (1f - proportion)) + (overridePitchAngle * proportion);
				actualPitchAngle = mergedPitchAngle;
			}
		}


		private void CalculateTargetVelocity ()
		{
			if (DeltaTime > 0f && target)
			{
				Vector3 deltaTargetPosition = (TargetPoint - lastTargetPoint) / DeltaTime / 50f;
				lastTargetPoint = TargetPoint;
				targetVelocity = targetVelocityLerp.Update (targetVelocity, deltaTargetPosition, targetSpinAcceleration);
			}
		}


		private void AddTargetInfluence (Vector2 frameInput)
		{
			if (!KickStarter.stateHandler || KickStarter.stateHandler.IsInCutscene () || lookAtOverride != null)
			{
				return;
			}

			bool hasSpinInput = !Mathf.Approximately (frameInput.x, 0f);

			if (hasSpinInput)
			{
				targetSpinTimer = targetSpinDelay;
			}
			else
			{
				if (targetSpinTimer > 0f)
				{
					targetSpinTimer -= Time.deltaTime;
				}
				else
				{
					if (targetMovementInfluencesSpin)
					{
						Vector3 ownDirection = transform.right;
						ownDirection.y = 0f;
						float dotProduct = Vector3.Dot (ownDirection, targetVelocity);
						if (Time.deltaTime > 0f)
						{
							targetSpinAngle += dotProduct * targetSpinInfluence * 20f / Time.deltaTime;
						}
					}

					if (targetMovementInfluencesPitch)
					{
						targetPitchAngle = Mathf.Lerp (targetPitchAngle, targetPitchRestAngle, targetPitchInfluence * Time.deltaTime);
					}
				}
			}
		}


		private void CalculateDistance ()
		{
			float targetDistanceInfluence = targetVelocity.sqrMagnitude / 0.0000024f;
			targetDistanceInfluence = Mathf.Clamp (targetDistanceInfluence, 0f, 1f);
			distanceInfluence = distanceInfluenceLerp.Update (distanceInfluence, targetDistanceInfluence, distanceInfluenceAcceleration);

			float targetDistance;
			if (fastDistanceFactor > 0f)
			{
				float fastDistance = normalDistance * Mathf.Max (0f, fastDistanceFactor);
				targetDistance = (distanceInfluence * fastDistance) + ((1f - distanceInfluence) * normalDistance * zoomAmount);
			}
			else
			{
				targetDistance = normalDistance * zoomAmount;
			}

			actualDistance = distanceAcceleration > 0f ? distanceLerp.Update (actualDistance, targetDistance, distanceAcceleration) : targetDistance;
			if (focalPointIsTarget)
			{
				focalDistance = actualDistance;
			}
		}


		private void AddInputInfluence (Vector2 inputVector)
		{
			if (doLimitSpin)
			{
				// Smooth spin edges
				float xAmount = targetSpinAngle / OffsetSpinLimits.y;
				if (xAmount > 0.9f && inputVector.x < 0f)
				{
					inputVector.y = Mathf.Lerp (inputVector.y, 0f, 10f * xAmount - 9f);
				}
				else
				{
					xAmount = targetSpinAngle / OffsetSpinLimits.x;
					if (xAmount < -0.9f && inputVector.x > 0f)
					{
						inputVector.y = Mathf.Lerp (inputVector.y, 0f, 10f * xAmount - 9f);
					}
				}

				targetSpinAngle += inputVector.x;
				targetSpinAngle = Mathf.Clamp (targetSpinAngle, OffsetSpinLimits.x, OffsetSpinLimits.y);
			}
			else
			{
				targetSpinAngle += inputVector.x;
			}

			// Smooth pitch edges
			float yAmount = (targetPitchAngle > 0f)
							? targetPitchAngle / maxPitch
							: -targetPitchAngle / minPitch;

			if (yAmount * inputVector.y < 0f && Mathf.Abs (yAmount) > 0.9f)
			{
				inputVector.y = Mathf.Lerp (inputVector.y, 0f, 10f * (Mathf.Abs (yAmount) - 0.9f));
			}

			targetPitchAngle -= inputVector.y;

			Vector2 clampedAngles = ClampAngles (targetPitchAngle, targetSpinAngle);
			targetPitchAngle = clampedAngles.x;
			targetSpinAngle = clampedAngles.y;
		}


		private Vector2 ClampAngles (float pitchAngle, float spinAngle)
		{
			pitchAngle = Mathf.Clamp (pitchAngle, minPitch, maxPitch);

			if (!doLimitSpin || edgeCurvature <= 0f)
			{
				return new Vector2 (pitchAngle, spinAngle);
			}

			float midSpin = (OffsetSpinLimits.x + OffsetSpinLimits.y) / 2f;
			float lowerBound = Mathf.Lerp (OffsetSpinLimits.x, midSpin, edgeCurvature);
			float upperBound = Mathf.Lerp (OffsetSpinLimits.y, midSpin, edgeCurvature);
			float midPitchAngle = (minPitch + maxPitch) / 2f;

			float proportion = 0f;
			if (spinAngle < lowerBound)
			{
				proportion = (lowerBound - spinAngle) / (lowerBound - OffsetSpinLimits.x);
			}
			else if (spinAngle > upperBound)
			{
				proportion = (upperBound - spinAngle) / (upperBound - OffsetSpinLimits.y);
			}

			if (proportion > 0f)
			{
				proportion = edgeCurve.Evaluate (proportion);

				float _maxPitch = Mathf.Lerp (maxPitch, midPitchAngle, proportion);
				float _minPitch = Mathf.Lerp (minPitch, midPitchAngle, proportion);
				float diff = 0f;

				if (pitchAngle > _maxPitch)
				{
					diff = pitchAngle - _maxPitch;
					pitchAngle -= diff;
				}
				else if (pitchAngle < _minPitch)
				{
					diff = _minPitch - pitchAngle;
					pitchAngle += diff;
				}

				if (spinAngle > upperBound)
				{
					spinAngle -= diff;
				}
				else
				{
					spinAngle += diff;
				}
			}

			return new Vector2 (pitchAngle, spinAngle);
		}


		private void UpdateActualAngles ()
		{
			if (autoMoveSpeed > 0f)
			{
				float _spinTarget = autoMoveTargetAngles.x;
				if (_spinTarget < actualSpinAngle)
				{
					while (Mathf.Abs (_spinTarget - actualSpinAngle) > 180f)
					{
						_spinTarget += 360f;
					}
				}
				else
				{
					while (Mathf.Abs (_spinTarget - actualSpinAngle) > 180f)
					{
						_spinTarget -= 360f;
					}
				}

				actualSpinAngle = targetSpinAngle = autoMoveSpinLerp.Update (actualSpinAngle, _spinTarget, autoMoveSpeed);
				actualPitchAngle = targetPitchAngle = autoMovePitchLerp.Update (actualPitchAngle, autoMoveTargetAngles.y, autoMoveSpeed);

				if (Mathf.Approximately (actualPitchAngle, autoMoveTargetAngles.y) &&
					Mathf.Approximately (actualSpinAngle, _spinTarget))
				{
					autoMoveSpeed = 0f;
					spinLerp.Reset ();
					pitchLerp.Reset ();
				}
				return;
			}

			if (lookAtOverride == null)
			{
				actualSpinAngle = (spinAcceleration > 0f)
									? spinLerp.Update (actualSpinAngle, targetSpinAngle, spinAcceleration)
									: targetSpinAngle;

				if (doLimitSpin)
				{
					actualSpinAngle = Mathf.Clamp (actualSpinAngle, OffsetSpinLimits.x, OffsetSpinLimits.y);
				}

				heightChangeInfluence = Mathf.Lerp (heightChangeInfluence, -targetVelocity.y * 20000f, Time.deltaTime * 5f);
				heightChangeInfluence = Mathf.Clamp (-heightChangeInfluenceLimit, heightChangeInfluence, heightChangeInfluenceLimit);

				actualPitchAngle = (pitchAcceleration > 0f)
									? pitchLerp.Update (actualPitchAngle, targetPitchAngle + heightChangeInfluence, pitchAcceleration)
									: targetPitchAngle + heightChangeInfluence;

				Vector2 clampedAngles = ClampAngles (actualPitchAngle, actualSpinAngle);

				actualPitchAngle = clampedAngles.x;
				actualSpinAngle = clampedAngles.y;
			}
		}


		private void UpdatePosition ()
		{
			try
			{
				Quaternion rotation = Quaternion.Euler (actualPitchAngle, actualSpinAngle, 0f);

				if (detectCollisions)
				{
					Vector3 trialRelativePosition = rotation * Vector3.forward * maxRaycastDistance;

					float collisionDistance = CalculateCollisionInfluence (-trialRelativePosition);
					if (actualDistance > collisionDistance)
					{
						actualDistance = collisionDistance;
					}
				}

				Vector3 correctedRelativePosition = rotation * Vector3.forward * actualDistance;
				transform.position = FocalPosition - correctedRelativePosition;

				if (!Mathf.Approximately (horizontalOffset, 0f))
				{
					if (detectCollisions)
					{
						Vector3 cameraRelative = (transform.position + rotation * Vector3.right * horizontalOffset) - FocalPosition;
						if (Physics.SphereCast (FocalPosition, 0.2f, cameraRelative.normalized, out collisionHit, cameraRelative.magnitude, collisionLayerMask, QueryTriggerInteraction.Ignore))
						{
							float collisionDistance = collisionHit.distance - wallSeparator;
							collisionDistance = Mathf.Max (collisionDistance, minimumDistance);
							_camera.transform.position = FocalPosition + (collisionDistance * cameraRelative.normalized);
						}
						else
						{
							_camera.transform.localPosition = Vector3.right * horizontalOffset;
						}
					}
					else
					{
						_camera.transform.localPosition = Vector3.right * horizontalOffset;
					}
				}
			}
			catch { }
		}


		private void UpdateRotation (float proportion)
		{
			if (normalDistance < 0f)
			{
				transform.LookAt (transform.position + transform.position - FocalPosition);
			}
			else if (target)
			{
				transform.LookAt (FocalPosition);
			}

			if (lookAtOverride != null)
			{
				Vector3 normalForward = transform.forward;

				transform.LookAt (lookAtOverride);
				Vector3 overrideForward = transform.forward;

				Vector3 mergedForward = (normalForward * (1f - proportion)) + (overrideForward * proportion);
				transform.forward = mergedForward;
			}
		}


		private float GetSpinAngle (Vector3 direction, Vector3 right)
		{
			direction.y = 0f;
			float spinAngle = Vector3.Angle (direction, Vector3.forward);

			float dotProduct = Vector3.Dot (right, Vector3.forward);
			if (dotProduct > 0f)
			{
				spinAngle = 360f - spinAngle;
			}

			if (spinAngle > 180f)
			{
				spinAngle -= 360f;
			}

			if (doLimitSpin)
			{
				float spinReverse = spinAngle - 360f;
				if (OffsetSpinLimits.x < spinReverse && spinReverse < OffsetSpinLimits.y)
				{
					return spinReverse;
				}

				spinReverse = spinAngle + 360f;
				if (OffsetSpinLimits.x < spinReverse && spinReverse < OffsetSpinLimits.y)
				{
					return spinReverse;
				}

				return Mathf.Clamp (spinAngle, OffsetSpinLimits.x, OffsetSpinLimits.y);
			}

			return spinAngle;
		}


		private float GetPitchAngle (Transform _transform)
		{
			var right = _transform.right;
			right.y = 0;
			right *= Mathf.Sign (_transform.up.y);
			var fwd = Vector3.Cross (right, Vector3.up).normalized;
			float pitchAngle = Vector3.Angle (fwd, _transform.forward) * Mathf.Sign (_transform.forward.y);

			pitchAngle *= -1f;
			return Mathf.Clamp (pitchAngle, minPitch, maxPitch);
		}


		private float GetPitchAngle (Vector3 direction, Vector3 right)
		{
			float pitchAngle = Vector3.Angle (direction, Vector3.up) - 90f;

			if (pitchAngle > 180f)
			{
				pitchAngle -= 360f;
			}

			pitchAngle *= -1f;
			return Mathf.Clamp (pitchAngle, minPitch, maxPitch);
		}


		private void SnapAngle ()
		{
			autoMoveSpeed = 0f;

			actualSpinAngle = targetSpinAngle;
			actualPitchAngle = targetPitchAngle;

			spinLerp.Update (targetSpinAngle, targetSpinAngle, spinAcceleration);
			pitchLerp.Update (targetPitchAngle, targetPitchAngle, pitchAcceleration);

			CalculatePitchInfluence (true);

			actualDistance = normalDistance;
			distanceInfluence = 0f;
			targetVelocity = Vector3.zero;
			targetVelocityLerp.Update (Vector3.zero, Vector3.zero, 0f);

			if (target != null)
			{
				lastTargetPoint = target.position;
			}

			heightChangeInfluence = 0f;
		}


		private void CalculatePitchInfluence (bool isInstant = false)
		{
			float targetPivotHeightOffset;
			float targetNormalDistance;

			if (Mathf.Approximately (minPitch, maxPitch))
			{
				targetPivotHeightOffset = midRegion.HeightOffset;
				targetNormalDistance = midRegion.Distance;
			}
			else
			{
				float proportion = ((-targetPitchAngle + maxPitch) / (minPitch - maxPitch) + 0.5f) * 2f;

				float curveValue = (Mathf.Abs (minPitch - maxPitch) > 0f)
									? heightOffsetCurve.Evaluate (proportion)
									: 0f;

				if (curveValue <= 0f)
				{
					targetPivotHeightOffset = Mathf.Lerp (midRegion.HeightOffset, bottomRegion.HeightOffset, -curveValue);
					targetNormalDistance = Mathf.Lerp (midRegion.Distance, bottomRegion.Distance, -curveValue);
				}
				else
				{
					targetPivotHeightOffset = Mathf.Lerp (midRegion.HeightOffset, topRegion.HeightOffset, curveValue);
					targetNormalDistance = Mathf.Lerp (midRegion.Distance, topRegion.Distance, curveValue);
				}
			}

			if (isInstant)
			{
				pivotHeightOffset = targetPivotHeightOffset;
				normalDistance = targetNormalDistance;
			}
			else
			{
				pivotHeightOffset = Mathf.Lerp (pivotHeightOffset, targetPivotHeightOffset, Time.deltaTime * 10f);
				normalDistance = Mathf.Lerp (normalDistance, targetNormalDistance, Time.deltaTime * 10f);
			}
		}


		private float CalculateCollisionInfluence (Vector3 relativePosition)
		{
			if (Physics.SphereCast (FocalPosition, 0.2f, relativePosition, out collisionHit, relativePosition.magnitude, collisionLayerMask, QueryTriggerInteraction.Ignore))
			{
				float collisionDistance = collisionHit.distance - wallSeparator;
				collisionDistance = Mathf.Max (collisionDistance, minimumDistance);

				return collisionDistance;
			}

			if (targetIsPlayer && player != null)
			{
				player.Show ();
			}

			return relativePosition.magnitude;
		}


		private bool CanAcceptInput ()
		{
			return (KickStarter.mainCamera == null || KickStarter.mainCamera.attachedCamera == this);
		}


		private void UpdateZoom ()
		{
			switch (zoomMethod)
			{
				case ZoomMethod.None:
					return;

				case ZoomMethod.AxisControlsDistance:
					if (KickStarter.stateHandler.gameState == GameState.Normal ||
						(KickStarter.stateHandler.gameState == GameState.Cutscene && allowCutsceneControl) ||
						(KickStarter.stateHandler.gameState == GameState.DialogOptions && allowConversationControl))
					{
						zoomAmount -= Input.GetAxis (zoomAxis) * zoomSpeed * Time.deltaTime;// * 50f;
						zoomAmount = Mathf.Clamp (zoomAmount, zoomLimit, 1f);
					}
					break;

				case ZoomMethod.ButtonControlsFOV:
					if (Input.GetButton (zoomAxis) &&
						(KickStarter.stateHandler.gameState == GameState.Normal ||
						(KickStarter.stateHandler.gameState == GameState.Cutscene && allowCutsceneControl) ||
						(KickStarter.stateHandler.gameState == GameState.DialogOptions && allowConversationControl)))
					{
						Camera.fieldOfView = zoomLerp.Update (Camera.fieldOfView, originalFOV * zoomLimit, zoomSpeed);
					}
					else
					{
						Camera.fieldOfView = zoomLerp.Update (Camera.fieldOfView, originalFOV, zoomSpeed);
					}
					break;
			}
		}


		private void SnapToOwnCameraAngle ()
		{
			ClearLookAtOverride ();

			targetSpinAngle = GetSpinAngle (transform.forward, transform.right);
			targetPitchAngle = GetPitchAngle (transform);
			SnapAngle ();
		}

		#endregion


		#region GetSet

		private Vector3 TargetPoint
		{
			get
			{
				return target.position;
			}
		}


		public float SpinAngle
		{
			get
			{
				return actualSpinAngle;
			}
		}


		public float PitchAngle
		{
			get
			{
				return actualPitchAngle;
			}
		}


		public Transform LookAtOverride
		{
			get
			{
				return lookAtOverride;
			}
		}


		private Vector3 FocalPosition
		{
			get
			{
				return target.position + Vector3.up * (pivotHeightOffset + heightOffset);
			}
		}


		private Vector2 OffsetSpinLimits
		{
			get
			{
				return new Vector2 (minSpin + spinLimitOffset, maxSpin + spinLimitOffset);
			}
		}


		private Vector2 FrameInput
		{
			get
			{
				if (!KickStarter.stateHandler) return Vector2.zero;

				GameState gameState = KickStarter.stateHandler.gameState;

				if (gameState == GameState.Normal ||
					(gameState == GameState.Cutscene && allowCutsceneControl) ||
					(gameState == GameState.DialogOptions && allowConversationControl))
				{
					if (KickStarter.playerInput.IsCursorLocked () || !requireLockedCursor)
					{
						Vector2 inputVector = Vector2.zero;

						if (!isDragControlled)
						{
							inputVector = new Vector2 (KickStarter.playerInput.InputGetAxis (spinAxis), KickStarter.playerInput.InputGetAxis (pitchAxis));
						}
						else
						{
							if (KickStarter.playerInput.GetDragState () == DragState._Camera)
							{
								if (KickStarter.playerInput.IsCursorLocked ())
								{
									inputVector = KickStarter.playerInput.GetFreeAim ();
								}
								else
								{
									inputVector = KickStarter.playerInput.GetDragVector () / 500f;
								}
							}
						}

						float inputX = (inputVector.x > 0f) ? Mathf.Min (inputVector.x, maxInput.x * DeltaTime) : Mathf.Max (inputVector.x, -maxInput.x * DeltaTime);
						float inputY = (inputVector.y > 0f) ? Mathf.Min (inputVector.y, maxInput.y * DeltaTime) : Mathf.Max (inputVector.y, -maxInput.y * DeltaTime);

						if (inputSmoothing.x > 0f)
						{
							inputX = Mathf.Lerp (lastFrameInput.x, inputX, DeltaTime * inputSmoothing.x);
						}
						if (inputSmoothing.y > 0f)
						{
							inputY = Mathf.Lerp (lastFrameInput.y, inputY, DeltaTime * inputSmoothing.y);
						}

						lastFrameInput = new Vector2 (inputX, inputY);

						return new Vector2 (inputX * inputInfluence.x, inputY * inputInfluence.y);
					}
				}
				return Vector2.zero;
			}
		}


		private float DeltaTime
		{
			get
			{
				return Time.deltaTime / 0.02f;
			}
		}


		private Player Player
		{
			get
			{
				if (player == null)
				{
					player = KickStarter.player;
				}
				return player;
			}
		}

		#endregion


		#region PrivateClasses

		[System.Serializable]
		private class RegionInfluence
		{

			[SerializeField] private float heightOffset = 1.5f;
			[SerializeField] private float distance = 2f;


			public RegionInfluence (float heightOffset, float distance)
			{
				this.heightOffset = heightOffset;
				this.distance = distance;
			}


			public float HeightOffset
			{
				get
				{
					return heightOffset;
				}
			}


			public float Distance
			{
				get
				{
					return distance;
				}
			}


			public void OnDrawGizmos (Vector3 forward, Vector3 focalPoint, float angle, bool doLimitSpin, Vector2 spinLimits, float globalHeightOffset, float edgeCurvature, Color color)
			{
				if (!doLimitSpin)
				{
					spinLimits.x = -180f;
					spinLimits.y = 180f;
				}
				else
				{
					edgeCurvature = Mathf.Min (edgeCurvature, 0.99f);
					float midSpin = (spinLimits.x + spinLimits.y) / 2f;
					spinLimits.x = Mathf.Lerp (spinLimits.x, midSpin, edgeCurvature);
					spinLimits.y = Mathf.Lerp (spinLimits.y, midSpin, edgeCurvature);
				}

				float angleRad = Mathf.Deg2Rad * angle;
				float yDist = Mathf.Abs (distance) * Mathf.Sin (angleRad);
				float xDist = Mathf.Abs (distance) * Mathf.Cos (angleRad);

				Vector3 centre = focalPoint + (Vector3.up * (yDist + heightOffset + globalHeightOffset));
				DrawCircle (forward, centre, xDist, spinLimits.x, spinLimits.y, color);
			}


			private void DrawCircle (Vector3 forward, Vector3 centre, float radius, float minSpinAngle, float maxSpinAngle, Color color)
			{
				Color backupColor = Gizmos.color;
				Gizmos.color = color;
				Vector3 pos = centre + (Quaternion.AngleAxis (minSpinAngle, Vector3.up) * forward.normalized * radius);

				for (float angle = minSpinAngle; angle < maxSpinAngle; angle += 5f)
				{
					Vector3 newPos = centre + (Quaternion.AngleAxis (angle, Vector3.up) * forward.normalized * radius);
					Gizmos.DrawLine (pos, newPos);
					pos = newPos;
				}
				{
					Vector3 newPos = centre + (Quaternion.AngleAxis (maxSpinAngle, Vector3.up) * forward.normalized * radius);
					Gizmos.DrawLine (pos, newPos);
				}

				Gizmos.color = backupColor;
			}


			#if UNITY_EDITOR

			public void ShowGUI (string label)
			{
				if (!string.IsNullOrEmpty (label))
				{
					EditorGUILayout.LabelField (label, EditorStyles.boldLabel);
				}
				heightOffset = EditorGUILayout.FloatField ("Height offset", heightOffset);
				distance = EditorGUILayout.FloatField ("Distance:", distance);
			}

			#endif

		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Target", EditorStyles.largeLabel);
			targetIsPlayer = EditorGUILayout.Toggle ("Is player?", targetIsPlayer);
			if (!targetIsPlayer)
			{
				target = (Transform) EditorGUILayout.ObjectField ("Target transform:", target, typeof (Transform), true);
			}

			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Spin", EditorStyles.largeLabel);
			if (!isDragControlled)
			{
				spinAxis = EditorGUILayout.TextField ("Spin input axis:", spinAxis);
			}
			inputInfluence.x = EditorGUILayout.FloatField ("Input influence:", inputInfluence.x);
			inputSmoothing.x = EditorGUILayout.Slider ("Input acceleration:", inputSmoothing.x, 0f, 1f);
			maxInput.x = EditorGUILayout.FloatField ("Max speed:", maxInput.x);
			spinAcceleration = EditorGUILayout.Slider ("Spin acceleration:", spinAcceleration, 0f, 20f);
			doLimitSpin = EditorGUILayout.Toggle ("Limit spin?", doLimitSpin);
			if (doLimitSpin)
			{
				minSpin = EditorGUILayout.Slider ("Minimum spin:", minSpin, -180f, 0f);
				maxSpin = EditorGUILayout.Slider ("Maximum spin:", maxSpin, 0f, 180f);
				spinLimitOffset = EditorGUILayout.Slider ("Spin limit offset:", spinLimitOffset, -180f, 180f);
				edgeCurvature = EditorGUILayout.Slider ("Edge curvature:", edgeCurvature, 0f, 1f);
			}
			if (maxSpin < minSpin) maxSpin = minSpin;
			targetMovementInfluencesSpin = EditorGUILayout.Toggle ("Target speed influences spin?", targetMovementInfluencesSpin);
			if (targetMovementInfluencesSpin)
			{
				targetSpinDelay = EditorGUILayout.FloatField ("Delay before influence (s):", targetSpinDelay);
				targetSpinAcceleration = EditorGUILayout.FloatField ("Target spin acceleration:", targetSpinAcceleration);
				targetSpinInfluence = EditorGUILayout.FloatField ("Target spin influence:", targetSpinInfluence);
			}
			horizontalOffset = EditorGUILayout.FloatField ("Horizontal offset:", horizontalOffset);

			if (Application.isPlaying)
			{
				EditorGUILayout.LabelField ("Current spin:", actualSpinAngle.ToString ());
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Pitch", EditorStyles.largeLabel);
			if (!isDragControlled)
			{
				pitchAxis = EditorGUILayout.TextField ("Pitch input axis:", pitchAxis);
			}
			inputInfluence.y = EditorGUILayout.FloatField ("Input influence:", inputInfluence.y);
			inputSmoothing.y = EditorGUILayout.Slider ("Input acceleration:", inputSmoothing.y, 0f, 1f);
			maxInput.y = EditorGUILayout.FloatField ("Max speed:", maxInput.y);
			pitchAcceleration = EditorGUILayout.Slider ("Pitch acceleration:", pitchAcceleration, 0f, 20f);
			minPitch = EditorGUILayout.Slider ("Minimum pitch:", minPitch, -90f, 90f);
			maxPitch = EditorGUILayout.Slider ("Maximum pitch:", maxPitch, -90f, 90f);
			if (maxPitch < minPitch) maxPitch = minPitch;

			targetMovementInfluencesPitch = EditorGUILayout.Toggle ("Target speed influences pitch?", targetMovementInfluencesPitch);
			if (targetMovementInfluencesPitch)
			{
				targetPitchInfluence = EditorGUILayout.FloatField ("Target pitch influence:", targetPitchInfluence);
				targetPitchRestAngle = EditorGUILayout.FloatField ("Target pitch rest angle:", targetPitchRestAngle);
			}
			if (Application.isPlaying)
			{
				EditorGUILayout.LabelField ("Current pitch:", actualPitchAngle.ToString ());
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Regions", EditorStyles.largeLabel);

			if (Mathf.Approximately (minPitch, maxPitch))
			{
				midRegion.ShowGUI (string.Empty);
			}
			else
			{
				topRegion.ShowGUI ("Top");
				midRegion.ShowGUI ("Mid");
				bottomRegion.ShowGUI ("Bottom");
			}

			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Distance", EditorStyles.largeLabel);
			distanceAcceleration = EditorGUILayout.FloatField ("Distance acceleration:", distanceAcceleration);
			fastDistanceFactor = EditorGUILayout.FloatField ("Target speed influence on distance:", fastDistanceFactor);
			if (fastDistanceFactor > 0f)
			{
				distanceInfluenceAcceleration = EditorGUILayout.FloatField ("Speed influence on distance acceleration:", distanceInfluenceAcceleration);
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Height", EditorStyles.largeLabel);
			heightOffset = EditorGUILayout.FloatField ("Target height offset:", heightOffset);
			heightChangeInfluenceLimit = EditorGUILayout.FloatField ("Height change influence limit:", heightChangeInfluenceLimit);
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Collision", EditorStyles.largeLabel);
			detectCollisions = EditorGUILayout.Toggle ("Do collisions?", detectCollisions);
			if (detectCollisions)
			{
				collisionLayerMask = AdvGame.LayerMaskField ("Collision layer(s):", collisionLayerMask);
				wallSeparator = EditorGUILayout.FloatField ("Wall separator:", wallSeparator);
				maxRaycastDistance = EditorGUILayout.FloatField ("Raycast distance:", maxRaycastDistance);
				minimumDistance = EditorGUILayout.FloatField ("Minimum distance:", minimumDistance);
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Zooming", EditorStyles.largeLabel);
			zoomMethod = (ZoomMethod) EditorGUILayout.EnumPopup ("Zoom method:", zoomMethod);
			switch (zoomMethod)
			{
				case ZoomMethod.AxisControlsDistance:
					{
						zoomLimit = EditorGUILayout.Slider ("Min relative distance:", zoomLimit, 0.1f, 1f);
						zoomAxis = EditorGUILayout.TextField ("Input axis:", zoomAxis);
						zoomSpeed = EditorGUILayout.Slider ("Zoom speed:", zoomSpeed, 0f, 15f);
						break;
					}

				case ZoomMethod.ButtonControlsFOV:
					{
						zoomLimit = EditorGUILayout.Slider ("Min relative FOV:", zoomLimit, 0.1f, 1f);
						zoomAxis = EditorGUILayout.TextField ("Input button:", zoomAxis);
						zoomSpeed = EditorGUILayout.Slider ("Zoom speed:", zoomSpeed, 0f, 15f);
						break;
					}

				default:
					break;
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Misc", EditorStyles.largeLabel);
			initialDirection = (InitialDirection) EditorGUILayout.EnumPopup ("Initial direction:", initialDirection);
			if (initialDirection == InitialDirection.SetAngles)
			{
				intialDirectionAngles.x = EditorGUILayout.FloatField ("Initial spin angle:", intialDirectionAngles.x);
				intialDirectionAngles.y = EditorGUILayout.FloatField ("Initial pitch angle:", intialDirectionAngles.y);
			}
			initialOnSwitch = EditorGUILayout.Toggle ("Set initial direction when active?", initialOnSwitch);
			isDragControlled = EditorGUILayout.Toggle ("Is drag-controlled?", isDragControlled);
			requireLockedCursor = EditorGUILayout.Toggle ("Input requires locked cursor?", requireLockedCursor);
			allowCutsceneControl = EditorGUILayout.Toggle ("Allow control in cutscenes?", allowCutsceneControl);
			allowConversationControl = EditorGUILayout.Toggle ("Allow control in conversations?", allowConversationControl);
			focalPointIsTarget = EditorGUILayout.Toggle ("Focal point is target?", focalPointIsTarget);
			if (!focalPointIsTarget)
			{
				focalDistance = EditorGUILayout.FloatField ("Focal distance:", focalDistance);
			}
			gizmoColor = EditorGUILayout.ColorField ("Gizmo colour:", gizmoColor);
			EditorGUILayout.EndVertical ();
		}

		#endif

	}

}