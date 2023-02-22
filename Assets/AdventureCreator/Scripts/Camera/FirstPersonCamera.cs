/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"FirstPersonCamera.cs"
 * 
 *	An optional script that allows First Person control.
 *	This is attached to a camera which is a child of the player.
 *	Only one First Person Camera should ever exist in the scene at runtime.
 *	Only the yaw is affected here: the pitch is determined by the player parent object.
 *
 *	Headbobbing code adapted from Mr. Animator's code: http://wiki.unity3d.com/index.php/Headbobber
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A camera used for first-person games. To use it, attach it to a child object of your Player prefab, as well as a Camera.
	 * It will then be used during gameplay if SettingsManager's movementMethod = MovementMethod.FirstPerson.
	 * This script only affects the pitch rotation - yaw rotation occurs by rotating the base object.
	 * Headbobbing code adapted from Mr. Animator's code: http://wiki.unity3d.com/index.php/Headbobber
	 */
	[AddComponentMenu("Adventure Creator/Camera/First-person camera")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_first_person_camera.html")]
	public class FirstPersonCamera : _Camera
	{

		#region Variables

		/** The sensitivity of free-aiming */
		public Vector2 sensitivity = new Vector2 (15f, 15f);

		/** The minimum pitch angle */
		public float minY = -60f;
		/** The maximum pitch angle */
		public float maxY = 60f;

		/** If True, the mousewheel can be used to zoom the camera's FOV */
		public bool allowMouseWheelZooming = false;
		/** The minimum FOV, if allowMouseWheelZooming = True */
		public float minimumZoom = 13f;
		/** The maximum FOV, if allowMouseWheelZooming = True */
		public float maximumZoom = 65f;

		/** If True, then the camera will bob up and down as the Player moves */
		public bool headBob = true;
		/** The method of head-bobbing to employ, if headBob = True (BuiltIn, CustomAnimation, CustomScript) */
		public FirstPersonHeadBobMethod headBobMethod = FirstPersonHeadBobMethod.BuiltIn;
		/** The bobbing speed, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.BuiltIn */
		public float builtInSpeedFactor = 1f;
		/** The bobbing magnitude, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.BuiltIn */
		public float bobbingAmount = 0.2f;
		private Animator headBobAnimator;
		/** The name of the float parameter in headBobAnimator to set as the forward head-bob speed, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.CustomAnimation */
		public string headBobSpeedParameter;
		/** The name of the float parameter in headBobAnimator to set as the side head-bob speed, if headBob = True and headBobMethod = FirstPersonHeadBobMethod.CustomAnimation */
		public string headBobSpeedSideParameter;
		
		protected float actualTilt = 0f;
		protected float bobTimer = 0f;
		protected float height = 0f;
		protected float deltaHeight = 0f;
		protected Player player;

		protected LerpUtils.FloatLerp tiltLerp = new LerpUtils.FloatLerp ();
		protected float targetTilt;

		#endregion


		#region UnityStandards

		protected override void Awake ()
		{
			if (player == null)
			{
				height = transform.localPosition.y;
				player = GetComponentInParent<Player> ();
				headBobAnimator = GetComponent<Animator> ();
			}
		}


		protected override void OnEnable ()
		{
			base.OnEnable ();
			EventManager.OnInitialiseScene += Awake;
		}


		protected override void OnDisable ()
		{
			base.OnDisable ();
			EventManager.OnInitialiseScene -= Awake;
		}

		#endregion


		#region PublicFunctions

		/**
		 * Overrides the default in _Camera to do nothing.
		 */
		new public void ResetTarget ()
		{}
		

		/**
		 * Updates the camera's transform.
		 * This is called every frame by StateHandler.
		 */
		public void _UpdateFPCamera ()
		{
			if (actualTilt != targetTilt)
			{
				if (player)
				{
					actualTilt = tiltLerp.Update (actualTilt, targetTilt, player.turnSpeed);
				}
				else
				{
					actualTilt = tiltLerp.Update (actualTilt, targetTilt, 7f);
				}
			}

			ApplyTilt ();

			if (headBob)
			{
				switch (headBobMethod)
				{
					case FirstPersonHeadBobMethod.BuiltIn:
						{
							deltaHeight = 0f;

							float bobSpeed = GetHeadBobSpeed ();
							float waveSlice = Mathf.Sin (bobTimer);
					
							bobTimer += Mathf.Abs (player.GetMoveSpeed ()) * Time.deltaTime * 5f * builtInSpeedFactor;

							if (bobTimer > Mathf.PI * 2)
							{
								bobTimer = bobTimer - (2f * Mathf.PI);
							}

							float totalAxes = Mathf.Clamp (bobSpeed, 0f, 1f);
					
							deltaHeight = totalAxes * waveSlice * bobbingAmount;

							transform.localPosition = new Vector3 (transform.localPosition.x, height + deltaHeight, transform.localPosition.z);
						}
						break;

					case FirstPersonHeadBobMethod.CustomAnimation:
						if (headBobAnimator)
						{
							bool isGrounded = (player && player.IsGrounded (true));

							if (!string.IsNullOrEmpty (headBobSpeedParameter))
							{
								if (isGrounded)
								{
									float forwardDot = Vector3.Dot (player.TransformForward, player.GetMoveDirection ());
									headBobAnimator.SetFloat (headBobSpeedParameter, player.GetMoveSpeed () * forwardDot);
								}
								else
								{
									headBobAnimator.SetFloat (headBobSpeedParameter, 0f);
								}
															   
							//	headBobAnimator.SetFloat (headBobSpeedParameter, GetHeadBobSpeed ());
							}
							if (!string.IsNullOrEmpty (headBobSpeedSideParameter))
							{
								if (isGrounded)
								{
									float rightDot = Vector3.Dot (player.TransformRight, player.GetMoveDirection ());
									headBobAnimator.SetFloat (headBobSpeedSideParameter, player.GetMoveSpeed () * rightDot);
								}
								else
								{ 
									headBobAnimator.SetFloat (headBobSpeedSideParameter, 0f);
								}
								//headBobAnimator.SetFloat (headBobSpeedSideParameter, GetHeadBobSpeed (true));
							}
						}
						break;

					default:
						break;
				}
			}

			if (KickStarter.stateHandler.gameState != GameState.Normal)
			{
				return;
			}

			if (allowMouseWheelZooming && Camera && KickStarter.mainCamera && KickStarter.mainCamera.attachedCamera == this)
			{
				float scrollWheelInput = KickStarter.playerInput.InputGetAxis ("Mouse ScrollWheel");
				if (scrollWheelInput > 0f)
				{
					Camera.fieldOfView = Mathf.Max (Camera.fieldOfView - 3, minimumZoom);
				}
				else if (scrollWheelInput < 0f)
				{
					Camera.fieldOfView = Mathf.Min (Camera.fieldOfView + 3, maximumZoom);
				}
			}
		}


		/**
		 * <summary>Gets the desired head-bobbing speed, to be manipulated via a custom script if headBobMethod = FirstPersonHeadBobMethod.CustomScript.</summary>
		 * <returns>The desired head-bobbing speed. Returns zero if the player is idle.</returns>
		 */
		public float GetHeadBobSpeed ()
		{
			if (player && player.IsGrounded (true))
			{
				return Mathf.Abs (player.GetMoveSpeed ());
			}
			return 0f;
		}


		/**
		 * <summary>Sets the pitch to a specific angle.</summary>
		 * <param name = "angle">The new pitch angle</param>
		 * <param nae = "isInstant">If True, then the pitch will be set instantly. Otherwise, it will move over time according to the Player's turnSpeed</param>
		 */
		public void SetPitch (float angle, bool isInstant = true)
		{
			if (isInstant)
			{
				actualTilt = targetTilt = angle;
			}
			else
			{
				targetTilt = angle;
			}
		}


		/**
		 * <summary>Increases the pitch, accounting for sensitivity</summary>
		 * <param name = "increase">The amount to increase sensitivity by</param>
		 */
		public void IncreasePitch (float increase)
		{
			actualTilt += increase * sensitivity.y;
			targetTilt = actualTilt;
		}


		/** Checks if the camera is looking up or down. */
		public bool IsTilting ()
		{
			return (actualTilt != 0f);
		}


		/** Gets the angle by which the camera is looking up or down, with negative values looking upward. */
		public float GetTilt ()
		{
			return actualTilt;
		}
		
		
		/** Gets the intended angle by which the camera wants to look up or down, with negative values looking upward. */
		public float GetTargetTilt ()
		{
			return targetTilt;
		}

		#endregion


		#region ProtectedFunctions

		protected void ApplyTilt ()
		{
			actualTilt = Mathf.Clamp (actualTilt, minY, maxY);
			transform.localEulerAngles = new Vector3 (actualTilt, 0f, 0f);
		}
		
		#endregion

	}

}
