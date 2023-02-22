/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Player.cs"
 * 
 *	This is attached to the Player GameObject, which must be tagged as Player.
 * 
 */

using UnityEngine;

namespace AC
{

	/** Attaching this component to a GameObject and tagging it "Player" will make it an Adventure Creator Player. */
	[AddComponentMenu("Adventure Creator/Characters/Player")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player.html")]
	public class Player : NPC
	{

		#region Variables

		/** The Player's jump animation, if using Legacy animation */
		public AnimationClip jumpAnim;
		/** The DetectHotspots component used if SettingsManager's hotspotDetection = HotspotDetection.PlayerVicinity */
		public DetectHotspots hotspotDetector;
		protected int id = -1;
		protected bool lockedPath;
		protected bool lockedPathCanReverse;
		protected float tankTurnFloat;
		/** True if running has been toggled */
		public bool toggleRun;

		protected bool lockHotspotHeadTurning = false;
		protected Transform firstPersonCameraTransform;
		protected FirstPersonCamera firstPersonCamera;
		protected bool prepareToJump;
		/** If True, and player-switching is enabled, then the enabled state of attached Hotspots will be synced with the player's active state */
		public bool autoSyncHotspotState = true;

		protected SkinnedMeshRenderer[] skinnedMeshRenderers;

		public PlayerMoveLock runningLocked = PlayerMoveLock.Free;
		public bool upMovementLocked = false;
		public bool downMovementLocked = false;
		public bool leftMovementLocked = false;
		public bool rightMovementLocked = false;
		public bool freeAimLocked = false;
		public bool jumpingLocked = false;

		#endregion


		#region UnityStandards

		protected new void Awake ()
		{
			if (soundChild && soundChild.audioSource)
			{
				audioSource = soundChild.audioSource;
			}

			skinnedMeshRenderers = GetComponentsInChildren <SkinnedMeshRenderer>();

			if (KickStarter.playerMovement)
			{
				firstPersonCamera = GetComponentInChildren <FirstPersonCamera>();
				if (firstPersonCamera == null && KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson && KickStarter.player && KickStarter.player.FirstPersonCamera == null)
				{
					ACDebug.LogWarning ("Could not find a FirstPersonCamera script on the Player - one is necessary for first-person movement.", KickStarter.player);
				}
				if (firstPersonCamera)
				{
					firstPersonCameraTransform = firstPersonCamera.transform;
				}
			}

			_Awake ();

			if (GetAnimEngine () != null && KickStarter.settingsManager && KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity && GetAnimEngine ().isSpriteBased && hotspotDetector)
			{
				if (spriteChild && hotspotDetector.transform == spriteChild) {} // OK
				else if (turn2DCharactersIn3DSpace)
				{
					if (hotspotDetector.transform == transform)
					{
						ACDebug.LogWarning ("The Player '" + name + "' has a Hotspot Detector assigned, but it is on the root.  Either parent it to the 'sprite child' instead, or check 'Turn root object in 3D space?' in the Player Inspector.", this);
					}
					else if (hotspotDetector.transform.parent == transform)
					{
						ACDebug.LogWarning ("The Player '" + name + "' has a Hotspot Detector assigned, but it is a direct child of a 2D root.  Either parent it to the 'sprite child' instead, or check 'Turn root object in 3D space?' in the Player Inspector.", this);
					}
				}
			}
		}


		protected override void OnEnable ()
		{
			base.OnEnable ();
			EventManager.OnSetPlayer += OnSetPlayer;
			
			AutoSyncHotspot ();
		}


		protected override void OnDisable ()
		{
			base.OnDisable ();
			EventManager.OnSetPlayer -= OnSetPlayer;
		}


		/** The Player's "Update" function, called by StateHandler. */
		public override void _Update ()
		{
			if (!IsActivePlayer ())
			{
				base._Update ();
				return;
			}

			if (firstPersonCamera && !KickStarter.stateHandler.MovementIsOff)
			{
				firstPersonCamera._UpdateFPCamera ();
			}

			bool jumped = false;
			if (KickStarter.playerInput.InputGetButtonDown ("Jump") && KickStarter.stateHandler.IsInGameplay () && motionControl == MotionControl.Automatic && !KickStarter.stateHandler.MovementIsOff)
			{
				if (!jumpingLocked)
				{
					jumped = Jump ();
				}
			}

			if (hotspotDetector)
			{
				hotspotDetector._Update ();
			}
			
			if (activePath && !pausePath)
			{
				if (IsTurningBeforeWalking ())
				{
					if (charState == CharState.Move)
					{
						StartDecelerating ();
					}
					else if (charState == CharState.Custom)
					{
						charState = CharState.Idle;
					}
				}
				else if ((KickStarter.stateHandler.gameState == GameState.Cutscene && !lockedPath) || 
						(KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick) ||
						(KickStarter.settingsManager.movementMethod == MovementMethod.None) ||
						(KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor && (KickStarter.settingsManager.singleTapStraight || KickStarter.settingsManager.pathfindUpdateFrequency > 0f)) || 
						IsMovingToHotspot ())
				{
					charState = CharState.Move;
				}
			}
			else if (activePath == null && charState == CharState.Move && !KickStarter.stateHandler.IsInGameplay () && KickStarter.stateHandler.gameState != GameState.Paused)
			{
				StartDecelerating ();
			}

			if (isJumping && !jumped)
			{
				if (IsGrounded ())
				{
					isJumping = false;
				}
			}

			BaseUpdate ();
		}


		public override void _FixedUpdate ()
		{
			if (prepareToJump)
			{
				prepareToJump = false;
				_rigidbody.AddForce (Vector3.up * KickStarter.settingsManager.jumpSpeed, ForceMode.Impulse);
			}

			base._FixedUpdate ();
		}

		#endregion


		#region PublicFunctions
	
		/**
		 * <summary>Makes the Player spot-turn left during gameplay. This needs to be called every frame of the turn.</summary>
		 * <param name = "intensity">The relative speed of the turn. Set this to the value of the input axis for smooth movement.</param>
		 */
		public void TankTurnLeft (float intensity = 1f)
		{
			lookDirection = -(intensity * TransformRight) + ((1f - intensity) * TransformForward);
			tankTurning = true;
			turnFloat = tankTurnFloat = -intensity;
		}
		

		/**
		 * <summary>Makes the Player spot-turn right during gameplay. This needs to be called every frame of the turn.</summary>
		 * <param name = "intensity">The relative speed of the turn. Set this to the value of the input axis for smooth movement.</param>
		 */
		public void TankTurnRight (float intensity = 1f)
		{
			lookDirection = (intensity * TransformRight) + ((1f - intensity) * TransformForward);
			tankTurning = true;
			turnFloat = tankTurnFloat = intensity;
		}


		/** Stops the Player from re-calculating pathfinding calculations. */
		public void CancelPathfindRecalculations ()
		{
			pathfindUpdateTime = 0f;
		}


		public override void StopTankTurning ()
		{
			lookDirection = TransformForward;
			tankTurning = false;
		}


		public override float GetTurnFloat ()
		{
			if (tankTurning)
			{
				return tankTurnFloat;
			}
			return base.GetTurnFloat ();
		}


		public void ForceTurnFloat (float _value)
		{
			turnFloat = _value;
		}


		/**
		 * <summary>Causes the Player to jump, so long as a Rigidbody component is attached.</summary>
		 * <return>True if the attempt to jump was succesful</returns>
		 */
		public bool Jump ()
		{
			if (isJumping)
			{
				return false;
			}

			bool isGrounded = IsGrounded ();

			if (activePath == null)
			{
				if (_characterController)
				{
					if (!isGrounded)
					{
						RaycastHit hitDownInfo;
						bool hitGround = Physics.Raycast (Transform.position + Vector3.up * _characterController.stepOffset, Vector3.down, out hitDownInfo, _characterController.stepOffset * 2f, groundCheckLayerMask);
						if (!hitGround)
						{
							return false;
						}
					}

					simulatedVerticalSpeed = KickStarter.settingsManager.jumpSpeed * 0.1f;
					isJumping = true;
					_characterController.Move (simulatedVerticalSpeed * Time.deltaTime * Vector3.up);
					KickStarter.eventManager.Call_OnPlayerJump (this);
					return true;
				}
				else if (_rigidbody && !_rigidbody.isKinematic && isGrounded)
				{
					if (useRigidbodyForMovement)
					{	
						prepareToJump = true;
					}
					else
					{
						_rigidbody.velocity = Vector3.up * KickStarter.settingsManager.jumpSpeed;
					}
					isJumping = true;

					if (ignoreGravity)
					{
						ACDebug.LogWarning (gameObject.name + " is jumping - but 'Ignore gravity?' is enabled in the Player Inspector. Is this correct?", gameObject);
					}
					KickStarter.eventManager.Call_OnPlayerJump (this);
					return true;
				}
				else if (isGrounded)
				{
					if (motionControl == MotionControl.Automatic)
					{
						if (_rigidbody && _rigidbody.isKinematic)
						{
							ACDebug.Log ("Player cannot jump without a non-kinematic Rigidbody component.", gameObject);
						}
						else
						{
							ACDebug.Log ("Player cannot jump without a Rigidbody component.", gameObject);
						}
						KickStarter.eventManager.Call_OnPlayerJump (this);
					}
				}
			}
			else if (_collider == null)
			{
				ACDebug.Log (gameObject.name + " has no Collider component", gameObject);
			}

			return false;
		}

		
		public override void EndPath ()
		{
			if (lockedPath)
			{
				if (activePath) activePath.pathType = lockedPathType;
				lockedPath = false;
			}
			base.EndPath ();
		}


		/** Reverses which way the Player is moving along a constrained Path during gameplay */
		public void ReverseDirectPathDirection ()
		{
			if (lockedPath && lockedPathCanReverse)
			{
				switch (activePath.pathType)
				{
					case AC_PathType.ForwardOnly:
					case AC_PathType.Loop:
						activePath.pathType = AC_PathType.ReverseOnly;
						targetNode --;
						if (targetNode < 0) targetNode = activePath.nodes.Count - 1;
						PathUpdate ();
						break;

					case AC_PathType.ReverseOnly:
						activePath.pathType = AC_PathType.ForwardOnly;
						targetNode ++;
						if (targetNode >= activePath.nodes.Count) targetNode = 0;
						PathUpdate ();
						break;

					default:
						break;
				}
			}
		}


		/**
		 * <summary>Locks the Player to a Paths object during gameplay, if using Direct movement.
		 * This allows the designer to constrain the Player's movement to a Path, even though they can move freely along it.</summary>
		 * <param name = "pathOb">The Paths to lock the Player to</param>
		 * <param name="canReverse">If True, the Player can move in both directions along the Path</param>
		 * <param name="pathSnapping">The type of snapping to enforce when first placing the Player over the Path</param>
		 * <param name="startingNode">If pathSnapping = PathSnapping.SnapToNode, the node index to snap to</param>
		 */
		public void SetLockedPath (Paths pathOb, bool canReverse = false, PathSnapping pathSnapping = PathSnapping.SnapToStart, int startingNode = 0)
		{
			// Ignore if using "point and click" or first person methods
			if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct || KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
			{
				lockedPath = true;
				lockedPathType = pathOb.pathType;

				if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct)
				{
					lockedPathCanReverse = canReverse;
				}
				else
				{
					lockedPathCanReverse = false;
				}

				if (pathOb.pathSpeed == PathSpeed.Run)
				{
					isRunning = true;
				}
				else
				{
					isRunning = false;
				}

				switch (pathSnapping)
				{
					default:
						startingNode = pathOb.GetNearestNode (Transform.position);
						break;

					case PathSnapping.SnapToStart:
						startingNode = 0;
						break;

					case PathSnapping.SnapToNode:
						break;
				}

				if (pathOb.nodes == null || pathOb.nodes.Count == 0 || startingNode >= pathOb.nodes.Count)
				{
					lockedPath = false;
					ACDebug.LogWarning ("Cannot lock Player to path '" + pathOb + "' - invalid node index " + startingNode, pathOb);
					return;
				}

				Vector3 pathPosition = pathOb.nodes[startingNode];

				if (pathSnapping != PathSnapping.None)
				{
					if (pathOb.affectY)
					{
						Teleport (pathPosition);
					}
					else if (SceneSettings.IsUnity2D ())
					{
						Teleport (new Vector3 (pathPosition.x, pathPosition.y, Transform.position.z));
					}
					else
					{
						Teleport (new Vector3 (pathPosition.x, Transform.position.y, pathPosition.z));
					}
				}
					
				activePath = pathOb;

				if (startingNode == pathOb.nodes.Count - 1 && lockedPathType == AC_PathType.Loop)
				{
					targetNode = 0;
				}
				else
				{
					targetNode = startingNode + 1;
				}

				if (startingNode == pathOb.nodes.Count - 1)
				{
					if (lockedPathCanReverse)
					{
						activePath.pathType = AC_PathType.ReverseOnly;
						targetNode = startingNode - 1;
					}
					else
					{
						ACDebug.LogWarning ("Cannot lock Player to path '" + pathOb + "' - node index " + startingNode + " is the end of the path, and bi-directional movement is disabled.", pathOb);

						Vector3 direction = Transform.position - pathOb.nodes[targetNode-2];
						Vector3 lookDir = new Vector3 (direction.x, 0f, direction.z);
						SetLookDirection (lookDir, true);

						lockedPath = false;
						activePath = null;
					}
				}

				if (activePath)
				{
					Vector3 direction = activePath.nodes[targetNode] - Transform.position;
					Vector3 lookDir = new Vector3 (direction.x, 0f, direction.z);
					SetLookDirection (lookDir, true);
				}

				charState = CharState.Idle;
			}
			else
			{
				ACDebug.LogWarning ("Path-constrained player movement is only available with Direct control.", gameObject);
			}
		}


		/**
		 * <summary>Checks if the Player is constrained to move along a Paths object during gameplay.</summary>
		 * <returns>True if the Player is constrained to move along a Paths object during gameplay</summary>
		 */
		public bool IsLockedToPath ()
		{
			return lockedPath;
		}


		/**
		 * <summary>Checks if the Player is prevented from being moved directly in all four directions.</summary>
		 * <returns>True if the Player is prevented from being moved directly in all four direction</returns>
		 */
		public bool AllDirectionsLocked ()
		{
			if (downMovementLocked && upMovementLocked && leftMovementLocked && rightMovementLocked)
			{
				return true;
			}
			return false;
		}

				
		/**
		 * Checks if the Player's FirstPersonCamera is looking up or down.
		 */
		public bool IsTilting ()
		{
			if (firstPersonCamera)
			{
				return firstPersonCamera.IsTilting ();
			}
			return false;
		}


		/**
		 * Gets the angle by which the Player's FirstPersonCamera is looking up or down, with negative values looking upward.
		 */
		public float GetTilt ()
		{
			if (firstPersonCamera)
			{
				return firstPersonCamera.GetTilt ();;
			}
			return 0f;
		}
		

		/**
		 * <summary>Sets the tilt of a first-person camera.</summary>
		 * <param name = "lookAtPosition">The point in World Space to tilt the camera towards</param>
		 * <param name = "isInstant">If True, the camera will be rotated instantly</param>
		 */
		public void SetTilt (Vector3 lookAtPosition, bool isInstant)
		{
			if (firstPersonCameraTransform == null || firstPersonCamera == null)
			{
				return;
			}
			
			if (isInstant)
			{
				Vector3 lookDirection = (lookAtPosition - firstPersonCameraTransform.position).normalized;
				float angle = Mathf.Asin (lookDirection.y) * Mathf.Rad2Deg;
				firstPersonCamera.SetPitch (-angle);
			}
			else
			{
				// Base the speed of tilt change on how much horizontal rotation is needed
				
				Quaternion oldRotation = firstPersonCameraTransform.rotation;
				firstPersonCameraTransform.LookAt (lookAtPosition);
				float targetTilt = firstPersonCameraTransform.localEulerAngles.x;
				firstPersonCameraTransform.rotation = oldRotation;
				if (targetTilt > 180)
				{
					targetTilt = targetTilt - 360;
				}
				firstPersonCamera.SetPitch (targetTilt, false);
			}
		}


		/**
		 * <summary>Sets the tilt of a first-person camera.</summary>
		 * <param name = "pitchAngle">The angle to tilt the camera towards, with 0 being horizontal, positive looking downard, and negative looking upward</param>
		 * <param name = "isInstant">If True, the camera will be rotated instantly</param>
		 */
		public void SetTilt (float pitchAngle, bool isInstant)
		{
			if (firstPersonCamera == null)
			{
				return;
			}
			
			firstPersonCamera.SetPitch (pitchAngle, isInstant);
		}


		/**
		 * <summary>Controls the head-facing position.</summary>
		 * <param name = "_headTurnTarget">The Transform to face</param>
		 * <param name = "_headTurnTargetOffset">The position offset of the Transform</param>
		 * <param name = "isInstant">If True, the head will turn instantly</param>
		 * <param name = "_headFacing">What the head should face (Manual, Hotspot, None)</param>
		 */
		public override void SetHeadTurnTarget (Transform _headTurnTarget, Vector3 _headTurnTargetOffset, bool isInstant, HeadFacing _headFacing = HeadFacing.Manual)
		{
			if (!IsActivePlayer ())
			{
				base.SetHeadTurnTarget (_headTurnTarget, _headTurnTargetOffset, isInstant, _headFacing);
				return;
			}

			if (_headFacing == HeadFacing.Hotspot && lockHotspotHeadTurning)
			{
				ClearHeadTurnTarget (false, HeadFacing.Hotspot);
			}
			else
			{
				base.SetHeadTurnTarget (_headTurnTarget, _headTurnTargetOffset, isInstant, _headFacing);
			}
		}


		/**
		 * <summary>Sets the enabled state of Player's ability to head-turn towards Hotspots.</summary>
		 * <param name = "state">If True, the Player's head will unable to face Hotspots</param>
		 */
		public void SetHotspotHeadTurnLock (bool state)
		{
			lockHotspotHeadTurning = state;
		}


		/**
		 * <summary>Updates a PlayerData class with its own variables that need saving.</summary>
		 * <param name = "playerData">The original PlayerData class</param>
		 * <returns>The updated PlayerData class</returns>
		 */
		public PlayerData SaveData (PlayerData playerData)
		{
			playerData.playerID = ID;
			
			playerData.playerLocX = Transform.position.x;
			playerData.playerLocY = Transform.position.y;
			playerData.playerLocZ = Transform.position.z;
			playerData.playerRotY = TransformRotation.eulerAngles.y;

			playerData.inCustomCharState = (charState == CharState.Custom && GetAnimator () && GetAnimator ().GetComponent <RememberAnimator>());
			
			playerData.playerWalkSpeed = walkSpeedScale;
			playerData.playerRunSpeed = runSpeedScale;

			playerData.playerUpLock = upMovementLocked;
			playerData.playerDownLock = downMovementLocked;
			playerData.playerLeftlock = leftMovementLocked;
			playerData.playerRightLock = rightMovementLocked;
			playerData.playerRunLock = (int) runningLocked;
			playerData.playerFreeAimLock = freeAimLocked;

			// Animation clips
			if (GetAnimEngine () != null)
			{
				playerData = GetAnimEngine ().SavePlayerData (playerData, this);
			}
						
			// Sound
			playerData.playerWalkSound = AssetLoader.GetAssetInstanceID (walkSound);
			playerData.playerRunSound = AssetLoader.GetAssetInstanceID (runSound);
			
			// Portrait graphic
			playerData.playerPortraitGraphic = AssetLoader.GetAssetInstanceID (portraitIcon.texture);

			// Speech label
			playerData.playerSpeechLabel = GetName ();
			playerData.playerDisplayLineID = displayLineID;

			// Rendering
			playerData.playerLockDirection = lockDirection;
			playerData.playerLockScale = lockScale;
			if (spriteChild && spriteChild.GetComponent <FollowSortingMap>())
			{
				playerData.playerLockSorting = spriteChild.GetComponent <FollowSortingMap>().lockSorting;
			}
			else if (GetComponent <FollowSortingMap>())
			{
				playerData.playerLockSorting = GetComponent <FollowSortingMap>().lockSorting;
			}
			else
			{
				playerData.playerLockSorting = false;
			}

			playerData.playerSpriteDirection = GetSpriteDirectionToSave ();

			playerData.playerSpriteScale = spriteScale;
			if (spriteChild && spriteChild.GetComponent <Renderer>())
			{
				playerData.playerSortingOrder = spriteChild.GetComponent <Renderer>().sortingOrder;
				playerData.playerSortingLayer = spriteChild.GetComponent <Renderer>().sortingLayerName;
			}
			else if (GetComponent <Renderer>())
			{
				playerData.playerSortingOrder = GetComponent <Renderer>().sortingOrder;
				playerData.playerSortingLayer = GetComponent <Renderer>().sortingLayerName;
			}
			
			playerData.playerActivePath = 0;
			playerData.lastPlayerActivePath = 0;
			if (GetPath ())
			{
				playerData.playerTargetNode = GetTargetNode ();
				playerData.playerPrevNode = GetPreviousNode ();
				playerData.playerIsRunning = isRunning;
				playerData.playerPathAffectY = activePath.affectY;

				if (GetPath () == ownPath)
				{
					playerData.playerPathData = Serializer.CreatePathData (ownPath);
					playerData.playerLockedPath = false;
				}
				else
				{
					playerData.playerPathData = string.Empty;
					playerData.playerActivePath = Serializer.GetConstantID (GetPath ().gameObject);
					playerData.playerLockedPath = lockedPath;
					playerData.playerLockedPathReversing = lockedPathCanReverse;
					playerData.playerLockedPathType = (int) lockedPathType;
				}
			}
			
			if (GetLastPath ())
			{
				playerData.lastPlayerTargetNode = GetLastTargetNode ();
				playerData.lastPlayerPrevNode = GetLastPrevNode ();
				playerData.lastPlayerActivePath = Serializer.GetConstantID (GetLastPath ().gameObject);
			}
			
			playerData.playerIgnoreGravity = ignoreGravity;
			
			// Head target
			playerData.playerLockHotspotHeadTurning = lockHotspotHeadTurning;
			if (headFacing == HeadFacing.Manual && headTurnTarget)
			{
				playerData.isHeadTurning = true;
				playerData.headTargetID = Serializer.GetConstantID (headTurnTarget);
				if (playerData.headTargetID == 0)
				{
					ACDebug.LogWarning ("The Player's head-turning target Transform, " + headTurnTarget + ", was not saved because it has no Constant ID", gameObject);
				}
				playerData.headTargetX = headTurnTargetOffset.x;
				playerData.headTargetY = headTurnTargetOffset.y;
				playerData.headTargetZ = headTurnTargetOffset.z;
			}
			else
			{
				playerData.isHeadTurning = false;
				playerData.headTargetID = 0;
				playerData.headTargetX = 0f;
				playerData.headTargetY = 0f;
				playerData.headTargetZ = 0f;
			}

			FollowSortingMap followSortingMap = GetComponentInChildren <FollowSortingMap>();
			if (followSortingMap)
			{
				playerData.followSortingMap = followSortingMap.followSortingMap;
				if (!playerData.followSortingMap && followSortingMap.GetSortingMap ())
				{
					ConstantID followSortingMapConstantID = followSortingMap.GetSortingMap ().GetComponent <ConstantID>();

					if (followSortingMapConstantID)
					{
						playerData.customSortingMapID = followSortingMapConstantID.constantID;
					}
					else
					{
						ACDebug.LogWarning ("The Player's SortingMap, " + followSortingMap.GetSortingMap ().name + ", was not saved because it has no Constant ID", gameObject);
						playerData.customSortingMapID = 0;
					}
				}
				else
				{
					playerData.customSortingMapID = 0;
				}
			}
			else
			{
				playerData.followSortingMap = false;
				playerData.customSortingMapID = 0;
			}

			// Inactive Player follow
			if (followTarget && !IsActivePlayer ())
			{
				if (!followTargetIsPlayer)
				{
					if (followTarget.GetComponent<ConstantID> ())
					{
						playerData.followTargetID = followTarget.GetComponent<ConstantID> ().constantID;
						playerData.followTargetIsPlayer = followTargetIsPlayer;
						playerData.followFrequency = followFrequency;
						playerData.followDistance = followDistance;
						playerData.followDistanceMax = followDistanceMax;
						playerData.followFaceWhenIdle = followFaceWhenIdle;
						playerData.followRandomDirection = followRandomDirection;
						playerData.followAcrossScenes = false;
					}
					else
					{
						ACDebug.LogWarning ("Want to save follow data for " + name + " but " + followTarget.name + " has no ID!", gameObject);
					}
				}
				else
				{
					playerData.followTargetID = 0;
					playerData.followTargetIsPlayer = followTargetIsPlayer;
					playerData.followFrequency = followFrequency;
					playerData.followDistance = followDistance;
					playerData.followDistanceMax = followDistanceMax;
					playerData.followFaceWhenIdle = followFaceWhenIdle;
					playerData.followRandomDirection = followRandomDirection;
					playerData.followAcrossScenes = followAcrossScenes;
				}
			}
			else
			{
				playerData.followTargetID = 0;
				playerData.followTargetIsPlayer = false;
				playerData.followFrequency = 0f;
				playerData.followDistance = 0f;
				playerData.followDistanceMax = 0f;
				playerData.followFaceWhenIdle = false;
				playerData.followRandomDirection = false;
				playerData.followAcrossScenes = false;
			}

			playerData.leftHandIKState = LeftHandIKController.CreateSaveData ();
			playerData.rightHandIKState = RightHandIKController.CreateSaveData ();

			playerData.spriteDirectionData = spriteDirectionData.SaveData ();

			// Remember scripts
			if (!IsLocalPlayer () && gameObject.activeInHierarchy)
			{
				playerData = KickStarter.levelStorage.SavePlayerData (this, playerData);
			}
			
			return playerData;
		}


		/** 
		 * <summary>Checks if the player is local to the scene, and not from a prefab.</summary>
		 * <returns>True if the player is local to the scene</returns>
		 */
		public bool IsLocalPlayer ()
		{
			return (ID <= -2);
		}


		/**
		 * <summary>Updates its own variables from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData class to load from</param>
		 */
		public void LoadData (PlayerData playerData)
		{
			upMovementLocked = playerData.playerUpLock;
			downMovementLocked = playerData.playerDownLock;
			leftMovementLocked = playerData.playerLeftlock;
			rightMovementLocked = playerData.playerRightLock;
			runningLocked = (PlayerMoveLock) playerData.playerRunLock;
			freeAimLocked = playerData.playerFreeAimLock;

			charState = (playerData.inCustomCharState) ? CharState.Custom : CharState.Idle;

			playerData.UpdateFromTempPosition (this);
				
			Teleport (new Vector3 (playerData.playerLocX, playerData.playerLocY, playerData.playerLocZ));
			SetRotation (playerData.playerRotY);
			SetMoveDirectionAsForward ();

			walkSpeedScale = playerData.playerWalkSpeed;
			runSpeedScale = playerData.playerRunSpeed;
			
			// Animation clips
			GetAnimEngine ().LoadPlayerData (playerData, this);

			/*#if AddressableIsPresent
			if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
			{
				StartCoroutine (LoadDataFromAddressables (playerData));
			}
			else
			#endif
			{
				walkSound = AssetLoader.RetrieveAsset (walkSound, playerData.playerWalkSound);
				runSound = AssetLoader.RetrieveAsset (runSound, playerData.playerRunSound);
				portraitIcon.ReplaceTexture (AssetLoader.RetrieveAsset (portraitIcon.texture, playerData.playerPortraitGraphic));
			}*/

			// Speech label
			SetName (playerData.playerSpeechLabel, playerData.playerDisplayLineID);
			
			// Rendering
			lockDirection = playerData.playerLockDirection;
			lockScale = playerData.playerLockScale;
			if (spriteChild && spriteChild.GetComponent <FollowSortingMap>())
			{
				spriteChild.GetComponent <FollowSortingMap>().lockSorting = playerData.playerLockSorting;
			}
			else if (GetComponent <FollowSortingMap>())
			{
				GetComponent <FollowSortingMap>().lockSorting = playerData.playerLockSorting;
			}
			else
			{
				ReleaseSorting ();
			}
			
			if (playerData.playerLockDirection)
			{
				spriteDirection = playerData.playerSpriteDirection;
				UpdateFrameFlipping (true);
			}
			if (playerData.playerLockScale)
			{
				spriteScale = playerData.playerSpriteScale;
			}
			if (playerData.playerLockSorting)
			{
				if (spriteChild && spriteChild.GetComponent <Renderer>())
				{
					spriteChild.GetComponent <Renderer>().sortingOrder = playerData.playerSortingOrder;
					spriteChild.GetComponent <Renderer>().sortingLayerName = playerData.playerSortingLayer;
				}
				else if (GetComponent <Renderer>())
				{
					GetComponent <Renderer>().sortingOrder = playerData.playerSortingOrder;
					GetComponent <Renderer>().sortingLayerName = playerData.playerSortingLayer;
				}
			}

			// Inactive following
			AC.Char charToFollow = null;
			if (playerData.followTargetID != 0)
			{
				RememberNPC followNPC = ConstantID.GetComponent <RememberNPC> (playerData.followTargetID);
				if (followNPC.GetComponent<AC.Char> ())
				{
					charToFollow = followNPC.GetComponent<AC.Char> ();
				}
			}

			if (charToFollow != null || (playerData.followTargetIsPlayer && KickStarter.player))
			{
				FollowAssign (charToFollow, playerData.followTargetIsPlayer, playerData.followFrequency, playerData.followDistance, playerData.followDistanceMax, playerData.followFaceWhenIdle, playerData.followRandomDirection, playerData.followAcrossScenes);
			}
			else
			{
				StopFollowing ();
			}

			// Active path
			Halt ();
			ForceIdle ();

			if (!string.IsNullOrEmpty (playerData.playerPathData) && ownPath)
			{
				Paths savedPath = ownPath;
				savedPath = Serializer.RestorePathData (savedPath, playerData.playerPathData);
				SetPath (savedPath, playerData.playerTargetNode, playerData.playerPrevNode, playerData.playerPathAffectY);
				isRunning = playerData.playerIsRunning;
				lockedPath = false;
			}
			else if (playerData.playerActivePath != 0)
			{
				Paths savedPath = ConstantID.GetComponent <Paths> (playerData.playerActivePath);
				if (savedPath)
				{
					lockedPath = playerData.playerLockedPath;
					
					if (lockedPath)
					{
						savedPath.pathType = AC_PathType.ForwardOnly;
						SetLockedPath (savedPath, playerData.playerLockedPathReversing);
						lockedPathType = (AC_PathType) playerData.playerLockedPathType;
						
						Teleport (new Vector3 (playerData.playerLocX, playerData.playerLocY, playerData.playerLocZ));
						SetRotation (playerData.playerRotY);
						targetNode = playerData.playerTargetNode;
						prevNode = playerData.playerPrevNode;
					}
					else
					{
						SetPath (savedPath, playerData.playerTargetNode, playerData.playerPrevNode);
					}
				}
				else
				{
					Halt ();
					ForceIdle ();
				}
			}
			else
			{
				Halt ();
				ForceIdle ();
			}
			
			// Previous path
			if (playerData.lastPlayerActivePath != 0)
			{
				Paths savedPath = ConstantID.GetComponent <Paths> (playerData.lastPlayerActivePath);
				if (savedPath)
				{
					SetLastPath (savedPath, playerData.lastPlayerTargetNode, playerData.lastPlayerPrevNode);
				}
			}
			
			// Head target
			lockHotspotHeadTurning = playerData.playerLockHotspotHeadTurning;
			if (playerData.isHeadTurning)
			{
				ConstantID _headTargetID = ConstantID.GetComponent <ConstantID> (playerData.headTargetID);
				if (_headTargetID)
				{
					SetHeadTurnTarget (_headTargetID.transform, new Vector3 (playerData.headTargetX, playerData.headTargetY, playerData.headTargetZ), true);
				}
				else
				{
					ClearHeadTurnTarget (true);
				}
			}
			else
			{
				ClearHeadTurnTarget (true);
			}
			
			ignoreGravity = playerData.playerIgnoreGravity;

			FollowSortingMap[] followSortingMaps = GetComponentsInChildren <FollowSortingMap>();
			if (followSortingMaps != null && followSortingMaps.Length > 0)
			{
				SortingMap customSortingMap = ConstantID.GetComponent <SortingMap> (playerData.customSortingMapID);
				
				foreach (FollowSortingMap followSortingMap in followSortingMaps)
				{
					followSortingMap.followSortingMap = playerData.followSortingMap;
					if (!playerData.followSortingMap && customSortingMap)
					{
						followSortingMap.SetSortingMap (customSortingMap);
					}
					else
					{
						followSortingMap.SetSortingMap (KickStarter.sceneSettings.sortingMap);
					}
				}
			}

			ignoreGravity = playerData.playerIgnoreGravity;

			if (GetAnimEngine () != null && GetAnimEngine ().IKEnabled)
			{
				LeftHandIKController.LoadData (playerData.leftHandIKState);
				RightHandIKController.LoadData (playerData.rightHandIKState);
			}

			_spriteDirectionData.LoadData (playerData.spriteDirectionData);

			// Remember scripts
			if (!IsLocalPlayer ())
			{
				KickStarter.levelStorage.LoadPlayerData (this, playerData);
			}

			if (GetAnimator ())
			{
				GetAnimator ().Update (0f);
			}
		}


		/**
		 * Hides the player's SkinnedMeshRenderers, if any exist
		 */
		public virtual void Hide ()
		{
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
			{
				skinnedMeshRenderer.enabled = false;
			}
		}


		/** Shows the player's SkinnedMeshRenderers, if any exist */
		public virtual void Show ()
		{
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
			{
				skinnedMeshRenderer.enabled = true;
			}
		}


		/**
		 * <summary>Spawns a new instance of the Player, when this function is called on a prefab</summary>
		 * <param name = "ID">The ID number to give the instance of the Player prefab</param>
		 * <returns>The spawned instance of this Player prefab</returns>
		 */
		public Player SpawnFromPrefab (int _ID)
		{
			Player newInstance = Instantiate (this);
			newInstance.gameObject.name = this.gameObject.name;
			newInstance.ID = _ID;

			if (_ID >= 0)
			{
				ACDebug.Log ("Spawned instance of Player '" + newInstance.GetName () + "'.", newInstance);
			}
			else
			{
				ACDebug.Log ("Spawned instance of Player '" + newInstance.GetName () + "' into scene " + newInstance.gameObject.scene.name + ".", newInstance);
			}

			if (KickStarter.eventManager)
			{
				KickStarter.eventManager.Call_OnPlayerSpawn (newInstance);
			}

			return newInstance;
		}


		/** Removes the Player GameObject from the scene */
		public void RemoveFromScene ()
		{
			if (KickStarter.eventManager)
			{
				KickStarter.eventManager.Call_OnPlayerRemove (this);
			}

			KickStarter.dialog.EndSpeechByCharacter (this);
			ReleaseHeldObjects ();

			Renderer[] playerObRenderers = gameObject.GetComponentsInChildren<Renderer> ();
			foreach (Renderer renderer in playerObRenderers)
			{
				renderer.enabled = false;
			}

			Collider[] playerObColliders = gameObject.GetComponentsInChildren<Collider> ();
			foreach (Collider collider in playerObColliders)
			{
				if (collider is CharacterController) continue;
				collider.isTrigger = true;
			}

			KickStarter.sceneChanger.ScheduleForDeletion (gameObject);
		}


		/** Returns True if the Player is inactive, but following the active Player across scenes */
		public bool IsFollowingActivePlayerAcrossScenes ()
		{
			if (followAcrossScenes && followTargetIsPlayer)
			{ 
				return true;
			}
			return false;
		}

		#endregion


		#region ProtectedFunctions

		protected override bool CanBeDirectControlled ()
		{
			if (KickStarter.stateHandler.gameState == GameState.Normal)
			{
				if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct || KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
				{
					return !upMovementLocked && !downMovementLocked && !leftMovementLocked && !rightMovementLocked;
				}
			}
			return false;
		}
		

		protected bool IsMovingToHotspot ()
		{
			if (KickStarter.playerInteraction && KickStarter.playerInteraction.GetHotspotMovingTo ())
			{
				return true;
			}
			
			return false;
		}


		protected void OnSetPlayer (Player player)
		{
			AutoSyncHotspot ();
		}


		protected void AutoSyncHotspot ()
		{
			bool enable = (KickStarter.player == null || KickStarter.player != this);

			if (autoSyncHotspotState && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				Hotspot[] hotspots = GetComponentsInChildren<Hotspot> ();
				foreach (Hotspot hotspot in hotspots)
				{
					if (enable) hotspot.TurnOn ();
					else hotspot.TurnOff ();
				}
			}
		}


		protected override void Accelerate ()
		{
			if (!IsActivePlayer ())
			{
				base.Accelerate ();
				return;
			}

			float targetSpeed = GetTargetSpeed ();

			if (AccurateDestination () && WillStopAtNextNode ())
			{
				AccurateAcc (GetTargetSpeed (), false);
			}
			else
			{
				if (KickStarter.settingsManager.magnitudeAffectsDirect && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.stateHandler.IsInGameplay () && !IsMovingToHotspot ())
				{
					targetSpeed -= (1f - KickStarter.playerInput.GetMoveKeys ().magnitude) / 2f;
				}

				moveSpeed = moveSpeedLerp.Update (moveSpeed, targetSpeed, acceleration);
			}
		}

		#endregion


		#region GetSet

		/** Assigns or sets the FirstPersonCamera Transform. This is done automatically in regular First Person mode, but must be set manually if using a custom controller, eg. Ultimate FPS. */
		public Transform FirstPersonCamera
		{
			get
			{
				return firstPersonCameraTransform;
			}
			set
			{
				firstPersonCameraTransform = value;
			}
		}


		public FirstPersonCamera FirstPersonCameraComponent
		{
			get
			{
				return firstPersonCamera;
			}
		}


		/** The Player's collection of Inventory items */
		public InvCollection Inventory
		{
			get
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && KickStarter.player != this)
				{
					return KickStarter.saveSystem.GetItemsFromPlayer (ID);
				}
				return KickStarter.runtimeInventory.PlayerInvCollection;
			}
		}


		public override bool IsPlayer
		{
			get
			{
				return true;
			}
		}


		public override bool IsActivePlayer ()
		{
			return this == KickStarter.player;
		}


		/** The Player's ID number, used to keep track of which Player is currently controlled */
		public int ID
		{
			get
			{
				return id;
			}
			set
			{
				id = value;

				if (id < -1 && KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					ACDebug.LogWarning ("The use of 'in-scene' local Players is not recommended when Player-switching is enabled - consider using the 'Player: Switch' Action to change Player instead.");
				}

				KickStarter.saveSystem.AssignPlayerData (this);
			}
		}

		#endregion

	}
	
}