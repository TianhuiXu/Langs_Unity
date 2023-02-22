/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerMovement.cs"
 * 
 *	This script analyses the variables in PlayerInput, and moves the character
 *	based on the control style, defined in the SettingsManager.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script analyses the variables in PlayerInput, and moves the character based on the control style, defined in the SettingsManager.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_movement.html")]
	public class PlayerMovement : MonoBehaviour
	{

		/** The LayerMask used when raycasting for point-and-click movement */
		public LayerMask pointClickLayerMask = -1;

		protected float moveStraightToCursorUpdateTime;
		protected GameObject clickPrefabInstance;


		/** Updates the movement handler. This is called every frame by StateHandler. */
		public void UpdatePlayerMovement ()
		{
			if (KickStarter.player)
			{
				UpdateMoveStraightToCursorTime ();

				if (KickStarter.playerInput.activeArrows)
				{
					return;
				}

				if (KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick || KickStarter.settingsManager.movementMethod == MovementMethod.Drag || KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor)
				{
					if (!KickStarter.playerInput.IsMouseOnScreen ())
					{
						return;
					}
				}
				
				if (KickStarter.settingsManager.disableMovementWhenInterationMenusAreOpen && KickStarter.stateHandler.IsInGameplay ())
				{
					if (KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick &&
						KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction &&
						KickStarter.settingsManager.selectInteractions != SelectInteractions.CyclingCursorAndClickingHotspot &&
						KickStarter.playerMenus.IsInteractionMenuOn ())
					{
						KickStarter.player.Halt ();
						return;
					}
				}

				if (UnityUIBlocksClick ())
				{
					return;
				}
				
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && !KickStarter.playerMenus.IsInteractionMenuOn () && !KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerInteraction.IsMouseOverHotspot ())
				{
					if (KickStarter.playerInteraction.GetHotspotMovingTo ())
					{
						KickStarter.playerInteraction.StopMovingToHotspot ();
					}

					KickStarter.playerInteraction.DeselectHotspot (false);
				}

				if (KickStarter.playerInteraction.GetHotspotMovingTo () && KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick && KickStarter.playerInput.GetMoveKeys ().sqrMagnitude > 0f)
				{
					KickStarter.playerInteraction.StopMovingToHotspot ();
				}

				switch (KickStarter.settingsManager.movementMethod)
				{
					case MovementMethod.None:
						break;

					case MovementMethod.Direct:
						if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen && KickStarter.settingsManager.directTouchScreen == DirectTouchScreen.DragBased)
						{
							DragPlayer (true, KickStarter.playerInput.GetMoveKeys ());
						}
						else
						{
							if (KickStarter.player.GetPath () == null || !KickStarter.player.IsLockedToPath ())
							{
								// Normal gameplay
								DirectControlPlayer (false, KickStarter.playerInput.GetMoveKeys ());
							}
							else
							{
								// Move along pre-determined path
								DirectControlPlayerPath (KickStarter.playerInput.GetMoveKeys ());
							}
						}
						break;

					case MovementMethod.Drag:
						DragPlayer (true, KickStarter.playerInput.GetMoveKeys ());
						break;
						
					case MovementMethod.StraightToCursor:
						MoveStraightToCursor ();
						break;
						
					case MovementMethod.PointAndClick:
						PointControlPlayer ();
						break;

					case MovementMethod.FirstPerson:
						if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
						{
							switch (KickStarter.settingsManager.firstPersonTouchScreen)
							{
								case FirstPersonTouchScreen.OneTouchToTurnAndTwoTouchesToMove:
									if (Input.touchCount == 1)
									{
										FirstPersonControlPlayer ();
										DragPlayerLook ();
									}
									else
									{
										DragPlayerTouch (KickStarter.playerInput.GetMoveKeys ());
									}
									break;

								case FirstPersonTouchScreen.OneTouchToMoveAndTurn:
									FirstPersonControlPlayer ();
									DragPlayer (false, KickStarter.playerInput.GetMoveKeys ());
									break;

								case FirstPersonTouchScreen.TouchControlsTurningOnly:
									FirstPersonControlPlayer ();
									DragPlayerLook ();
									break;

								case FirstPersonTouchScreen.CustomInput:
									FirstPersonControlPlayer ();
									DirectControlPlayer (true, KickStarter.playerInput.GetMoveKeys ());
									break;

								default:
									break;
							}
						}
						else
						{
							FirstPersonControlPlayer ();
							DirectControlPlayer (true, KickStarter.playerInput.GetMoveKeys ());
						}
						break;
				}
			}
		}


		// Straight to cursor functions
		protected float moveStraightToCursorHoldTime;
		protected void UpdateMoveStraightToCursorTime ()
		{
			if (moveStraightToCursorUpdateTime > 0f)
			{
				moveStraightToCursorUpdateTime -= Time.deltaTime;
				if (moveStraightToCursorUpdateTime < 0f)
				{
					moveStraightToCursorUpdateTime = 0f;
				}
			}

			if (KickStarter.settingsManager.clickHoldSeparationStraight > 0f && KickStarter.settingsManager.singleTapStraight)
			{
				if (KickStarter.playerInput.GetMouseState () == MouseState.Normal)
				{
					moveStraightToCursorHoldTime = KickStarter.settingsManager.clickHoldSeparationStraight;
				}

				if (moveStraightToCursorHoldTime > 0f)
				{
					moveStraightToCursorHoldTime -= Time.deltaTime;
					if (moveStraightToCursorHoldTime < 0f)
					{
						moveStraightToCursorHoldTime = 0f;
					}
				}
			}
			else
			{
				moveStraightToCursorHoldTime = 0f;
			}
		}


		protected bool movingFromHold;
		protected void MoveStraightToCursor ()
		{
			if (KickStarter.player.AllDirectionsLocked ())
			{
				if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.StartDecelerating ();
				}
				return;
			}

			if (KickStarter.playerInput.GetDragState () == DragState.None)
			{
				KickStarter.playerInput.ResetDragMovement ();
				
				if (KickStarter.player.charState == CharState.Move && KickStarter.player.GetPath () == null)
				{
					KickStarter.player.StartDecelerating ();
				}
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick && KickStarter.settingsManager.singleTapStraight && KickStarter.settingsManager.doubleClickMovement != DoubleClickMovement.Disabled && KickStarter.settingsManager.singleTapStraightPathfind)
			{
				movingFromHold = false;
				PointControlPlayer ();
				return;
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick && KickStarter.settingsManager.singleTapStraight)
			{
				movingFromHold = false;

				if (KickStarter.settingsManager.singleTapStraightPathfind)
				{
					PointControlPlayer ();
					return;
				}

				Vector3 clickPoint = GetStraightToCursorClickPoint ();
				Vector3 moveDirection = clickPoint - KickStarter.player.Transform.position;
				
				if (clickPoint != Vector3.zero)
				{
					if (moveDirection.magnitude > KickStarter.settingsManager.GetDestinationThreshold ())
					{
						if (SceneSettings.IsUnity2D ())
						{
							moveDirection = new Vector3 (moveDirection.x, 0f, moveDirection.y);
						}
						
						bool run = (moveDirection.magnitude > KickStarter.settingsManager.dragRunThreshold);

						switch (KickStarter.player.runningLocked)
						{ 
							case PlayerMoveLock.AlwaysRun:
								run = true;
								break;

							case PlayerMoveLock.AlwaysWalk:
								run = false;
								break;

							default:
								break;
						}

						List<Vector3> pointArray = new List<Vector3>();
						pointArray.Add (clickPoint);
						PointMovePlayer (pointArray.ToArray (), run);
					}
					else
					{
						if (KickStarter.player.charState == CharState.Move)
						{
							KickStarter.player.StartDecelerating ();
						}
					}
				}
			}
			else if (KickStarter.playerInput.GetDragState () == DragState.Player && moveStraightToCursorHoldTime <= 0f && (!KickStarter.settingsManager.singleTapStraight || KickStarter.playerInput.CanClick ()))
			{
				Vector3 clickPoint = GetStraightToCursorClickPoint ();
				Vector3 moveDirection = clickPoint - KickStarter.player.Transform.position;

				if (clickPoint != Vector3.zero)
				{
					if (moveDirection.magnitude > KickStarter.settingsManager.GetDestinationThreshold ())
					{
						if (SceneSettings.IsUnity2D ())
						{
							moveDirection = new Vector3 (moveDirection.x, 0f, moveDirection.y);
						}

						bool run;
						if (KickStarter.settingsManager.singleTapStraight && KickStarter.settingsManager.doubleClickMovement == DoubleClickMovement.MakesPlayerRun && KickStarter.settingsManager.singleTapStraightPathfind && KickStarter.player.isRunning)
						{
							run = true;
						}
						else
						{
							run = (moveDirection.magnitude > KickStarter.settingsManager.dragRunThreshold);
						}

						switch (KickStarter.player.runningLocked)
						{
							case PlayerMoveLock.AlwaysRun:
								run = true;
								break;

							case PlayerMoveLock.AlwaysWalk:
								run = false;
								break;

							default:
								break;
						}

						if (KickStarter.settingsManager.pathfindUpdateFrequency > 0f)
						{
							if (moveStraightToCursorUpdateTime <= 0f)
							{
								if (movingFromHold && KickStarter.player.IsPathfinding () && (clickPoint - KickStarter.player.GetTargetPosition (true)).magnitude < KickStarter.settingsManager.GetDestinationThreshold ())
								{
									// Too close, don't update
								}
								else
								{
									Vector3[] pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.Transform.position, clickPoint, KickStarter.player);
									PointMovePlayer (pointArray, run);
									moveStraightToCursorUpdateTime = KickStarter.settingsManager.pathfindUpdateFrequency;

									movingFromHold = true;
									return;
								}
							}
						}
						else
						{
							KickStarter.player.isRunning = run;
							KickStarter.player.charState = CharState.Move;
							
							KickStarter.player.SetLookDirection (moveDirection, false);
							KickStarter.player.SetMoveDirectionAsForward ();

							movingFromHold = true;
						}
					}
					else
					{
						if (KickStarter.player.charState == CharState.Move)
						{
							KickStarter.player.StartDecelerating ();
							movingFromHold = false;
						}
					}

					if (KickStarter.player.GetPath () &&
					   (KickStarter.settingsManager.pathfindUpdateFrequency <= 0f || KickStarter.playerInput.GetMouseState () != MouseState.HeldDown))
					{
						KickStarter.player.EndPath ();
						movingFromHold = false;
					}
				}
				else
				{
					if (KickStarter.player.charState == CharState.Move)
					{
						KickStarter.player.StartDecelerating ();
						movingFromHold = false;
					}

					if (KickStarter.player.GetPath ())
					{
						KickStarter.player.EndPath ();
						movingFromHold = false;
					}
				}
			}
			else
			{
				if (KickStarter.player.charState == CharState.Move || KickStarter.player.IsPathfinding ())
				{
					if (movingFromHold && moveStraightToCursorHoldTime > 0f)
					{
						if (KickStarter.player.charState == CharState.Move)
						{
							KickStarter.player.StartDecelerating ();
						}

						if (KickStarter.player.GetPath ())
						{
							KickStarter.player.EndPath ();
						}
					}
				}
				else
				{
					movingFromHold = false;
				}
			}
		}


		protected Vector3 GetStraightToCursorClickPoint ()
		{
			Vector2 simulatedMouse = KickStarter.playerInput.GetMousePosition ();

			Vector3 clickPoint = ClickPoint (simulatedMouse);
			
			if (clickPoint == Vector3.zero)
			{
				// Move Ray down screen until we hit something
				if (KickStarter.settingsManager.walkableClickRange > 0f && ((int) ACScreen.height * KickStarter.settingsManager.walkableClickRange) > 1)
				{
					float maxIterations = 100f;
					float stepSize = ACScreen.height / maxIterations; // was fixed at 4f

					if (KickStarter.settingsManager.navMeshSearchDirection == NavMeshSearchDirection.StraightDownFromCursor)
					{
						for (float i=1f; i< ACScreen.height * KickStarter.settingsManager.walkableClickRange; i+=stepSize)
						{
							// Down
							clickPoint = ClickPoint (new Vector2 (simulatedMouse.x, simulatedMouse.y - i));
							if (clickPoint != Vector3.zero) return clickPoint;
						}
					}

					for (float i=1f; i< ACScreen.height * KickStarter.settingsManager.walkableClickRange; i+=stepSize)
					{
						// Up
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x, simulatedMouse.y + i));
						if (clickPoint != Vector3.zero) return clickPoint;

						// Down
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x, simulatedMouse.y - i));
						if (clickPoint != Vector3.zero) return clickPoint;

						// Left
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x - i, simulatedMouse.y));
						if (clickPoint != Vector3.zero) return clickPoint;

						// Right
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x + i, simulatedMouse.y));
						if (clickPoint != Vector3.zero) return clickPoint;

						// UpLeft
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x - i, simulatedMouse.y - i));
						if (clickPoint != Vector3.zero) return clickPoint;

						// UpRight
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x + i, simulatedMouse.y - i));
						if (clickPoint != Vector3.zero) return clickPoint;

						// DownLeft
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x - i, simulatedMouse.y + i));
						if (clickPoint != Vector3.zero) return clickPoint;

						// DownRight
						clickPoint = ClickPoint (new Vector2 (simulatedMouse.x + i, simulatedMouse.y + i));
						if (clickPoint != Vector3.zero) return clickPoint;
					}
				}
			}
			else
			{
				return clickPoint;
			}

			return Vector3.zero;
		}


		/**
		 * <summary>Gets the point in world space that a point in screen space is above.</summary>
		 * <param name = "screenPosition">The position in screen space</returns>
		 * <param name = "onNavMesh">If True, then only objects placed on the NavMesh layer will be detected.</param>
		 * <returns>The point in world space that a point in screen space is above</returns>
		 */
		public Vector3 ClickPoint (Vector2 screenPosition, bool onNavMesh = false)
		{
			if (KickStarter.navigationManager.Is2D ())
			{
				RaycastHit2D hit;
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					if (onNavMesh)
					{
						hit = UnityVersionHandler.Perform2DRaycast (
							KickStarter.CameraMain.ScreenToWorldPoint (new Vector2 (screenPosition.x, screenPosition.y)),
							Vector2.zero,
							KickStarter.settingsManager.navMeshRaycastLength,
							1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer)
							);
					}
					else
					{
						hit = UnityVersionHandler.Perform2DRaycast (
							KickStarter.CameraMain.ScreenToWorldPoint (new Vector2 (screenPosition.x, screenPosition.y)),
							Vector2.zero,
							KickStarter.settingsManager.navMeshRaycastLength,
							pointClickLayerMask
							);
					}
				}
				else
				{
					Vector3 pos = screenPosition;
					pos.z = KickStarter.player.Transform.position.z - KickStarter.CameraMainTransform.position.z;

					if (onNavMesh)
					{
						hit = UnityVersionHandler.Perform2DRaycast (
							KickStarter.CameraMain.ScreenToWorldPoint (pos),
							Vector2.zero,
							KickStarter.settingsManager.navMeshRaycastLength,
							1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer)
							);
					}
					else
					{
						hit = UnityVersionHandler.Perform2DRaycast (
							KickStarter.CameraMain.ScreenToWorldPoint (pos),
							Vector2.zero,
							KickStarter.settingsManager.navMeshRaycastLength,
							pointClickLayerMask
							);
					}
				}
				
				if (hit.collider)
				{
					return hit.point;
				}
			}
			else
			{
				Ray ray = KickStarter.CameraMain.ScreenPointToRay (screenPosition);
				RaycastHit hit = new RaycastHit();

				if (onNavMesh)
				{
					if (KickStarter.settingsManager && KickStarter.sceneSettings && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer)))
					{
						return hit.point;
					}
				}
				else
				{
					if (KickStarter.settingsManager && KickStarter.sceneSettings && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, pointClickLayerMask))
					{
						return hit.point;
					}
				}
			}
			
			return Vector3.zero;
		}
		
		
		// Drag functions

		protected void DragPlayer (bool doRotation, Vector2 moveKeys)
		{
			if (KickStarter.playerInput.GetDragState () == DragState.None)
			{
				KickStarter.playerInput.ResetDragMovement ();
				
				if (KickStarter.player.charState == CharState.Move)
				{
					if (KickStarter.playerInteraction.GetHotspotMovingTo () == null)
					{
						KickStarter.player.charState = CharState.Decelerate;
					}
				}
			}

			if (KickStarter.playerInput.GetDragState () == DragState.Player)
			{
				Vector3 moveDirectionInput = Vector3.zero;
				
				if (SceneSettings.IsTopDown ())
				{
					moveDirectionInput = (moveKeys.y * Vector3.forward) + (moveKeys.x * Vector3.right);
				}
				else
				{
					moveDirectionInput = (moveKeys.y * MainCamera.ForwardVector ()) + (moveKeys.x * MainCamera.RightVector ());
				}
				
				if (KickStarter.playerInput.IsDragMoveSpeedOverWalkThreshold ())
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
				
					if (doRotation)
					{
						KickStarter.player.SetLookDirection (moveDirectionInput, false);
						KickStarter.player.SetMoveDirectionAsForward ();
					}
					else
					{
						if (KickStarter.playerInput.GetDragVector ().y < 0f)
						{
							KickStarter.player.SetMoveDirectionAsForward ();
						}
						else
						{
							KickStarter.player.SetMoveDirectionAsBackward ();
						}
					}
				}
				else
				{
					if (KickStarter.player.charState == CharState.Move && KickStarter.playerInteraction.GetHotspotMovingTo () == null)
					{
						KickStarter.player.StartDecelerating ();
					}
				}
			}
		}


		protected void DragPlayerTouch (Vector2 moveKeys)
		{
			if (KickStarter.playerInput.GetDragState () == DragState.None)
			{
				KickStarter.playerInput.ResetDragMovement ();
				
				if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;
				}
			}
			
			if (KickStarter.playerInput.GetDragState () == DragState.Player)
			{
				Vector3 moveDirectionInput = (moveKeys.y * MainCamera.ForwardVector ()) + (moveKeys.x * MainCamera.RightVector ());

				if (KickStarter.playerInput.IsDragMoveSpeedOverWalkThreshold ())
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;

					KickStarter.player.SetMoveDirection (KickStarter.player.Transform.position + moveDirectionInput);
				}
				else
				{
					if (KickStarter.player.charState == CharState.Move && KickStarter.playerInteraction.GetHotspotMovingTo () == null)
					{
						KickStarter.player.StartDecelerating ();
					}
				}
			}
		}


		// Direct-control functions

		private Vector2 lastFrameMoveKeys;
		private Vector3 cameraAlignedInput;

		protected void DirectControlPlayer (bool isFirstPerson, Vector2 moveKeys)
		{
			KickStarter.player.CancelPathfindRecalculations ();
			if (KickStarter.settingsManager.directMovementType == DirectMovementType.RelativeToCamera)
			{
				if (moveKeys.sqrMagnitude > 0f)
				{
					// When using input keys, releasing both simultaneously sometimes has a 1-frame lag.
					// Here, this is countered by detecting a change and skipping the input
					bool releasedOneAxis = false;
					if ((moveKeys.x == 0f && lastFrameMoveKeys.x != 0f) ||
						(moveKeys.y == 0f && lastFrameMoveKeys.y != 0f))
					{
						releasedOneAxis = true;
					}

					lastFrameMoveKeys = moveKeys;
					if (releasedOneAxis)
					{
						return;
					}

					if (!KickStarter.playerInput.IsCameraLockSnapped ())
					{
						if (SceneSettings.IsTopDown ())
						{
							cameraAlignedInput = (moveKeys.y * Vector3.forward) + (moveKeys.x * Vector3.right);
						}
						else
						{
							if (!isFirstPerson && KickStarter.settingsManager.directMovementPerspective && SceneSettings.CameraPerspective == CameraPerspective.ThreeD)
							{
								Vector3 forwardVector = (KickStarter.player.Transform.position - KickStarter.CameraMainTransform.position).normalized;
								Vector3 rightVector = -Vector3.Cross (forwardVector, KickStarter.CameraMainTransform.up);
								cameraAlignedInput = (moveKeys.y * forwardVector) + (moveKeys.x * rightVector);
							}
							else
							{
								cameraAlignedInput = (moveKeys.y * MainCamera.ForwardVector ()) + (moveKeys.x * MainCamera.RightVector ());
							}
						}
					}

					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					if (isFirstPerson && moveKeys.y < 0f && !KickStarter.player.canRunInReverse) KickStarter.player.isRunning = false;
					KickStarter.player.charState = CharState.Move;

					if (isFirstPerson)
					{
						KickStarter.player.SetMoveDirection (cameraAlignedInput, AC.KickStarter.settingsManager.firstPersonMovementSmoothing);
					}
					else
					{
						KickStarter.player.SetLookDirection (cameraAlignedInput, KickStarter.settingsManager.directTurnsInstantly);
						KickStarter.player.SetMoveDirectionAsForward ();
					}
				}
				else if (KickStarter.player.charState == CharState.Move && KickStarter.playerInteraction.GetHotspotMovingTo () == null && !KickStarter.player.IsPathfinding ())
				{
					if (KickStarter.settingsManager.stopTurningWhenReleaseInput && !isFirstPerson)
					{
						KickStarter.player.SetLookDirection (KickStarter.player.TransformForward, false);
						KickStarter.player.SetMoveDirectionAsForward ();
					}

					KickStarter.player.charState = CharState.Decelerate;
				}
			}
			
			else if (KickStarter.settingsManager.directMovementType == DirectMovementType.TankControls)
			{
				if (KickStarter.settingsManager.magnitudeAffectsDirect || isFirstPerson)
				{
					if (moveKeys.x < 0f)
					{
						KickStarter.player.TankTurnLeft (-moveKeys.x);
					}
					else if (moveKeys.x > 0f)
					{
						KickStarter.player.TankTurnRight (moveKeys.x);
					}
					else
					{
						KickStarter.player.StopTankTurning ();
					}
				}
				else
				{
					if (moveKeys.x < -0.3f)
					{
						KickStarter.player.TankTurnLeft ();
					}
					else if (moveKeys.x > 0.3f)
					{
						KickStarter.player.TankTurnRight ();
					}
					else
					{
						KickStarter.player.StopTankTurning ();
					}
				}
				
				if (moveKeys.y > 0f)
				{
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
					KickStarter.player.SetMoveDirectionAsForward ();
				}
				else if (moveKeys.y < 0f)
				{
					KickStarter.player.isRunning = KickStarter.player.canRunInReverse && KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
					KickStarter.player.SetMoveDirectionAsBackward ();
				}
				else if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.charState = CharState.Decelerate;

					if (KickStarter.player.IsReversing ())
					{
						KickStarter.player.SetMoveDirectionAsBackward ();
					}
					else
					{
						KickStarter.player.SetMoveDirectionAsForward ();
					}
				}
			}
		}


		protected void DirectControlPlayerPath (Vector2 moveKeys)
		{
			if (moveKeys != Vector2.zero)
			{
				Vector3 moveDirectionInput;

				if (SceneSettings.IsTopDown ())
				{
					moveDirectionInput = (moveKeys.y * Vector3.forward) + (moveKeys.x * Vector3.right);
				}
				else
				{
					moveDirectionInput = (moveKeys.y * MainCamera.ForwardVector ()) + (moveKeys.x * MainCamera.RightVector ());

					if (SceneSettings.IsUnity2D ())
					{
						moveDirectionInput = new Vector3 (moveDirectionInput.x, moveDirectionInput.z, moveDirectionInput.y);
					}
				}

				Vector3 direction = KickStarter.player.GetPath ().nodes[KickStarter.player.GetTargetNode ()] - KickStarter.player.Transform.position;
				if (SceneSettings.IsUnity2D ())
				{
					direction.z = 0f;
				}

				float dotProduct = Vector3.Dot (moveDirectionInput, direction.normalized);
				if (dotProduct > 0.5f)
				{
					// Move along path, because movement keys are in the path's forward direction
					KickStarter.player.isRunning = KickStarter.playerInput.IsPlayerControlledRunning ();
					KickStarter.player.charState = CharState.Move;
				}
				else if (dotProduct < -0.5f)
				{
					KickStarter.player.ReverseDirectPathDirection ();
				}
			}
			else
			{
				if (KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.StartDecelerating ();
				}
			}
		}


		// Point/click functions
		
		protected void PointControlPlayer ()
		{
			if (KickStarter.playerInput.IsCursorLocked ())
			{
				return;
			}

			if (KickStarter.mainCamera && !KickStarter.mainCamera.IsPointInCamera (KickStarter.playerInput.GetMousePosition ()))
			{
				return;
			}

			if (KickStarter.player.AllDirectionsLocked ())
			{
				if (KickStarter.player.GetPath () == null && KickStarter.player.charState == CharState.Move)
				{
					KickStarter.player.StartDecelerating ();
				}
				return;
			}
			
			if ((KickStarter.playerInput.GetMouseState () == MouseState.SingleClick || KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick) && !KickStarter.playerMenus.IsInteractionMenuOn () && !KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerInteraction.IsMouseOverHotspot () && KickStarter.playerCursor)
			{
				if (KickStarter.playerCursor.GetSelectedCursor () < 0 || KickStarter.playerCursor.IsInWalkMode ())
				{
					if (KickStarter.settingsManager.doubleClickMovement == DoubleClickMovement.RequiredToWalk && KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
					{
						return;
					}

					if (KickStarter.playerInput.GetDragState () == DragState.Moveable)
					{
						return;
					}

					if (InvInstance.IsValid (KickStarter.runtimeInventory.SelectedInstance) && !KickStarter.settingsManager.canMoveWhenActive && KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick && !KickStarter.settingsManager.inventoryDisableLeft)
					{
						return;
					}

					bool doubleClick = false;
					if (KickStarter.playerInput.GetMouseState () == MouseState.DoubleClick && KickStarter.settingsManager.doubleClickMovement == DoubleClickMovement.MakesPlayerRun)
					{
						doubleClick = true;
					}

					if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction && KickStarter.playerMenus)
					{
						KickStarter.playerMenus.CloseInteractionMenus ();
					}

					#if UNITY_2019_1_OR_NEWER
					if (SceneSettings.IsUnity2D () && KickStarter.settingsManager.navMeshSearchDirection == NavMeshSearchDirection.RadiallyOutwardsFromCursor && KickStarter.sceneSettings.navMesh)//&& KickStarter.sceneSettings.navMesh.gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer))
					{
						int numPolys = KickStarter.sceneSettings.navMesh.PolygonCollider2Ds.Length;

						if (numPolys > 0)
						{
							Vector3 bestClickPoint = Vector3.zero;
							float bestDist = Mathf.Infinity;

							Vector3 screenPoint = KickStarter.playerInput.GetMousePosition ();
							float depth = Mathf.Abs (KickStarter.sceneSettings.navMesh.transform.position.z - KickStarter.mainCamera.Transform.position.z);
							screenPoint.z = depth;
							Vector3 worldPoint = KickStarter.CameraMain.ScreenToWorldPoint (screenPoint);

							for (int i = 0; i < numPolys; i++)
							{
								PolygonCollider2D polygonCollider2D = KickStarter.sceneSettings.navMesh.PolygonCollider2Ds[i];
								if (polygonCollider2D)
								{
									Vector3 clickPoint = polygonCollider2D.ClosestPoint (worldPoint);
									
									if (numPolys == 1)
									{
										ProcessHit (clickPoint, null, doubleClick);
										return;
									}

									float dist = (clickPoint - worldPoint).sqrMagnitude;
									if (i == 0 || dist < bestDist)
									{
										bestDist = dist;
										bestClickPoint = clickPoint;
									}
								}
							}

							ProcessHit (bestClickPoint, null, doubleClick);
							return;
						}
					}
					#endif

					Vector3 simulatedMouse = KickStarter.playerInput.GetMousePosition ();

					// In Unity 5.6+, 'Ignore Raycast' layers are included in raycast checks so we need to specify the layer if in 2D
					if (
						(SceneSettings.IsUnity2D () && !SearchForNavMesh2D (simulatedMouse, Vector2.zero, doubleClick))
						||
						(!SceneSettings.IsUnity2D () && !RaycastNavMesh (simulatedMouse, doubleClick))
						)
					{
						// Move Ray down screen until we hit something

						if (KickStarter.settingsManager.walkableClickRange > 0f && ((int) ACScreen.height * KickStarter.settingsManager.walkableClickRange) > 1)
						{
							float maxIterations = 100f;
							float stepSize = ACScreen.height / maxIterations; // was fixed at 4f

							if (KickStarter.settingsManager.navMeshSearchDirection == NavMeshSearchDirection.StraightDownFromCursor)
							{
								if (SceneSettings.IsUnity2D ())
								{
									// Down
									if (SearchForNavMesh2D (simulatedMouse, -Vector2.up, doubleClick))
									{
										return;
									}
								}
								else
								{
									for (float i=1f; i< ACScreen.height * KickStarter.settingsManager.walkableClickRange; i+=stepSize)
									{
										// Down
										if (RaycastNavMesh (new Vector2 (simulatedMouse.x, simulatedMouse.y - i), doubleClick))
										{
											return;
										}
									}
								}
							}

							if (SceneSettings.IsUnity2D ())
							{
								// Up
								if (SearchForNavMesh2D (simulatedMouse, Vector2.up, doubleClick))
								{
									return;
								}

								if (KickStarter.settingsManager.navMeshSearchDirection == NavMeshSearchDirection.RadiallyOutwardsFromCursor)
								{
									// Down
									if (SearchForNavMesh2D (simulatedMouse, -Vector2.up, doubleClick))
									{
										return;
									}
								}
								// Left
								if (SearchForNavMesh2D (simulatedMouse, -Vector2.right, doubleClick))
								{
									return;
								}
								// Right
								if (SearchForNavMesh2D (simulatedMouse, Vector2.right, doubleClick))
								{
									return;
								}
								// DownLeft
								if (SearchForNavMesh2D (simulatedMouse, -Vector2.one, doubleClick))
								{
									return;
								}
								// DownRight
								if (SearchForNavMesh2D (simulatedMouse, new Vector2 (1f, -1f), doubleClick))
								{
									return;
								}
								// UpLeft
								if (SearchForNavMesh2D (simulatedMouse, new Vector2 (-1f, 1f), doubleClick))
								{
									return;
								}
								// UpRight
								if (SearchForNavMesh2D (simulatedMouse, Vector2.one, doubleClick))
								{
									return;
								}
							}
							else
							{
								for (float i=1f; i< ACScreen.height * KickStarter.settingsManager.walkableClickRange; i+=stepSize)
								{
									// Up
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x, simulatedMouse.y + i), doubleClick))
									{
										return;
									}

									if (KickStarter.settingsManager.navMeshSearchDirection == NavMeshSearchDirection.RadiallyOutwardsFromCursor)
									{
										// Down
										if (RaycastNavMesh (new Vector2 (simulatedMouse.x, simulatedMouse.y - i), doubleClick))
										{
											return;
										}
									}
									// Left
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x - i, simulatedMouse.y), doubleClick))
									{
										return;
									}
									// Right
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x + i, simulatedMouse.y), doubleClick))
									{
										return;
									}
									// DownLeft
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x - i, simulatedMouse.y - i), doubleClick))
									{
										return;
									}
									// DownRight
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x + i, simulatedMouse.y - i), doubleClick))
									{
										return;
									}
									// UpLeft
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x - i, simulatedMouse.y + i), doubleClick))
									{
										return;
									}
									// UpRight
									if (RaycastNavMesh (new Vector2 (simulatedMouse.x + i, simulatedMouse.y + i), doubleClick))
									{
										return;
									}
								}
							}
						}
					}
				}
				else if (KickStarter.settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot && KickStarter.settingsManager.autoCycleWhenInteract)
				{
					KickStarter.playerCursor.ResetSelectedCursor ();
				}

			}
			else if (KickStarter.player.GetPath () == null && KickStarter.player.charState == CharState.Move)
			{
				KickStarter.player.StartDecelerating ();
			}
		}


		protected bool ProcessHit (Vector3 hitPoint, GameObject hitObject, bool run)
		{
			if (hitObject && hitObject.layer != LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer))
			{
				return false;
			}

			if (Vector3.Distance (hitPoint, KickStarter.player.Transform.position) < KickStarter.settingsManager.GetDestinationThreshold ())
			{
				return true;
			}

			bool canShowClick = !run;
			
			if (KickStarter.player.runningLocked == PlayerMoveLock.AlwaysRun)
			{
				run = true;
			}
			else if (KickStarter.player.runningLocked == PlayerMoveLock.AlwaysWalk)
			{
				run = false;
			}
			else if (Vector3.Distance (hitPoint, KickStarter.player.Transform.position) < KickStarter.player.runDistanceThreshold)
			{
				run = false;
			}

			Vector3[] pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (KickStarter.player.Transform.position, hitPoint, KickStarter.player);
			PointMovePlayer (pointArray, run);

			if (canShowClick)
			{
				switch (KickStarter.settingsManager.clickMarkerPosition)
				{
					case ClickMarkerPosition.ColliderContactPoint:
						ShowClick (hitPoint);
						break;

					case ClickMarkerPosition.PlayerDestination:
						if (pointArray.Length > 0)
							ShowClick (pointArray[pointArray.Length - 1]);
						break;
				}
			}

			return true;
		}


		protected void PointMovePlayer (Vector3[] pointArray, bool run)
		{
			if (KickStarter.player.AllDirectionsLocked ())
			{
				ACDebug.LogWarning ("Cannot move the Player because their Movement has been locked.");
				return;
			}

			KickStarter.eventManager.Call_OnPointAndClick (pointArray, run);
			KickStarter.player.MoveAlongPoints (pointArray, run);
		}


		protected bool SearchForNavMesh2D (Vector2 mousePosition, Vector2 direction, bool run)
		{
			RaycastHit2D hit;
			if (KickStarter.mainCamera.IsOrthographic ())
			{
				hit = UnityVersionHandler.Perform2DRaycast (
					KickStarter.CameraMain.ScreenToWorldPoint (mousePosition),
					direction,
					KickStarter.settingsManager.navMeshRaycastLength,
					1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer)
					);
			}
			else
			{
				Vector3 pos = mousePosition;
				pos.z = KickStarter.player.Transform.position.z - KickStarter.CameraMainTransform.position.z;

				hit = UnityVersionHandler.Perform2DRaycast (
					KickStarter.CameraMain.ScreenToWorldPoint (pos),
					direction,
					KickStarter.settingsManager.navMeshRaycastLength,
					1 << LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer)
					);
			}

			if (hit.collider)
			{
				return ProcessHit (hit.point, hit.collider.gameObject, run);
			}
			
			return false;
		}


		protected bool RaycastNavMesh (Vector3 mousePosition, bool run)
		{
			if (KickStarter.settingsManager.ignoreOffScreenNavMesh)
			{
				if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > ACScreen.width || mousePosition.y > ACScreen.height)
				{
					// Out of camera bounds, ignore
					return false;
				}
			}

			if (KickStarter.navigationManager.Is2D ())
			{
				RaycastHit2D hit;
				if (KickStarter.mainCamera.IsOrthographic ())
				{
					hit = UnityVersionHandler.Perform2DRaycast (
						KickStarter.CameraMain.ScreenToWorldPoint (new Vector2 (mousePosition.x, mousePosition.y)),
						Vector2.zero,
						KickStarter.settingsManager.navMeshRaycastLength,
						pointClickLayerMask);
				}
				else
				{
					Vector3 pos = mousePosition;
					pos.z = KickStarter.player.Transform.position.z - KickStarter.CameraMainTransform.position.z;

					hit = UnityVersionHandler.Perform2DRaycast (
						KickStarter.CameraMain.ScreenToWorldPoint (pos),
						Vector2.zero,
						KickStarter.settingsManager.navMeshRaycastLength,
						pointClickLayerMask);
				}

				if (hit.collider)
				{
					return ProcessHit (hit.point, hit.collider.gameObject, run);
				}
			}
			else
			{
				Ray ray = KickStarter.CameraMain.ScreenPointToRay (mousePosition);
				RaycastHit hit;

				if (Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, pointClickLayerMask))
				{
					return ProcessHit (hit.point, hit.collider.gameObject, run);
				}
			}
			return false;
		}


		protected void ShowClick (Vector3 clickPoint)
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.clickPrefab)
			{
				if (clickPrefabInstance && clickPrefabInstance.activeSelf)
				{
					KickStarter.sceneChanger.ScheduleForDeletion (clickPrefabInstance);
				}
				Transform clickPrefabTransform = Instantiate (KickStarter.settingsManager.clickPrefab, clickPoint, Quaternion.identity) as Transform;
				clickPrefabInstance = clickPrefabTransform.gameObject;
			}
		}

		
		// First-person functions

		protected void FirstPersonControlPlayer ()
		{
			Vector2 freeAim = KickStarter.playerInput.GetFreeAim ();
			if (freeAim.magnitude > KickStarter.settingsManager.dragWalkThreshold * 30f * Time.deltaTime)
			{
				freeAim.Normalize ();
				freeAim *= KickStarter.settingsManager.dragWalkThreshold * 30f * Time.deltaTime;
			}
			
			float rotationX = KickStarter.player.TransformRotation.eulerAngles.y;
			if (KickStarter.player.FirstPersonCameraComponent)
			{
				rotationX += (freeAim.x * KickStarter.player.FirstPersonCameraComponent.sensitivity.x);
				KickStarter.player.FirstPersonCameraComponent.IncreasePitch (-freeAim.y);
			}
			else
			{
				rotationX += (freeAim.x * 15f);
			}

			Quaternion rot = Quaternion.AngleAxis (rotationX, Vector3.up);
			KickStarter.player.SetRotation (rot);
			KickStarter.player.ForceTurnFloat (freeAim.x * 2f);
		}


		protected void DragPlayerLook ()
		{
			if (KickStarter.player.AllDirectionsLocked ())
			{
				return;
			}

			if (KickStarter.playerInput.GetMouseState () == MouseState.Normal)
			{
				return;
			}
			
			else if (!KickStarter.playerMenus.IsMouseOverMenu () && !KickStarter.playerMenus.IsInteractionMenuOn () && (KickStarter.playerInput.GetMouseState () == MouseState.RightClick || !KickStarter.playerInteraction.IsMouseOverHotspot ()))
			{
				if (KickStarter.playerInput.GetMouseState () == MouseState.SingleClick)
				{
					KickStarter.playerInteraction.DeselectHotspot (false);
				}
			}
		}


		protected virtual bool UnityUIBlocksClick ()
		{
			if (KickStarter.settingsManager.unityUIClicksAlwaysBlocks && KickStarter.playerMenus.EventSystem)
			{
				#if !UNITY_EDITOR
				if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
				{
					if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
					{
						if (KickStarter.playerMenus.EventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
						{
							return true;
						}
					}
					return false;
				}
				#endif

				if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen ||
					KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick || 
					KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor || 
					KickStarter.settingsManager.movementMethod == MovementMethod.Drag)
				{
					return KickStarter.playerMenus.EventSystem.IsPointerOverGameObject ();
				}
			}
			return false;
		}


		public GameObject ClickPrefabInstance
		{
			get
			{
				return clickPrefabInstance;
			}
		}

	}

}