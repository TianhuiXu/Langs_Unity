/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavMeshAgentIntegration.cs"
 * 
 *	This script serves as a bridge between Adventure Creator and Unity's NavMeshAgent component.
 *	To use it, add it to your Player / NPC, as well as the NavMeshAgent component.
 *	You can then use the fields in the NavMeshAgent Inspector to control the character's movement.
 *
 *	You will also need to make sure your scene's 'Pathfinding method' is set to 'Unity Navigation'.
 *
 *	This script will override AC's movement code with that in the NavMeshAgent.
 *	While it is good for most purposes, it is more intended to demonstrate how such a bridge can be built.
 *	If you wish to build upon it for more custom gameplay, duplicate the script and make such changes to the copy.
 *	You can then add your new script to the character instead.
 * 
 */

using UnityEngine;
using UnityEngine.AI;

namespace AC
{

	/**
	 * This script serves as a bridge between Adventure Creator and Unity's NavMeshAgent component.
	 * To use it, add it to your Player / NPC, as well as the NavMeshAgent component.
	 * You can then use the fields in the NavMeshAgent Inspector to control the character's movement.
	 *
	 * You will also need to make sure your scene's 'Pathfinding method' is set to 'Unity Navigation'.
	 *
	 * This script will override AC's movement code with that in the NavMeshAgent.
	 * While it is good for most purposes, it is more intended to demonstrate how such a bridge can be built.
	 * If you wish to build upon it for more custom gameplay, duplicate the script and make such changes to the copy.
	 * You can then add your new script to the character instead.
	 */
	[AddComponentMenu("Adventure Creator/Navigation/NavMeshAgent Integration")]
	[RequireComponent (typeof (NavMeshAgent))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_nav_mesh_agent_integration.html")]
	public class NavMeshAgentIntegration : MonoBehaviour
	{

		public bool useACForSpeedValues;
		public float runSpeedFactor = 2f;
		public bool useACForTurning = true;

		private float originalSpeed;
		private NavMeshAgent navMeshAgent;
		private Char _char;
		private bool disableDuringGameplay;
		private Vector3 targetPosition;
		private float directSpeed;
		

		private void Awake ()
		{
			/*
			 * First, we'll assign our private variables.
			 */

			navMeshAgent = GetComponent <NavMeshAgent>();
			originalSpeed = navMeshAgent.speed;
			_char = GetComponent <AC.Char>();
		}


		private void Start ()
		{
			/*
			 * Next, we'll correct the character's motionControl value and give warnings if things are not as expected.
			 */

			if (_char == null)
			{
				ACDebug.LogWarning ("A 'Player' or 'NPC' component must be attached to " + gameObject.name + " for the NavMeshAgentIntegration script to work.", gameObject);
			}
			else
			{
				SetMotionControl ();

				/*
				 * If this controls a Player, and the game's Movement method is not Point And Click,
				 * we'll allow regular AC control over movement during gameplay.
				 */
				if (_char.IsPlayer && KickStarter.settingsManager && KickStarter.settingsManager.movementMethod != MovementMethod.PointAndClick)
				{
					disableDuringGameplay = true;
				}
			}

			if (KickStarter.sceneSettings && KickStarter.sceneSettings.navigationMethod != AC_NavigationMethod.UnityNavigation)
			{
				ACDebug.LogWarning ("For the NavMeshAgentIntegration script to work, your scene's pathfinding method must be set to 'Unity Navigation'");
			}
		}


		private void OnTeleport ()
		{
			/*
			 * This function is called by the Char script whenever the character has been teleported.
			 */

			targetPosition = _char.GetTargetPosition ();
			navMeshAgent.Warp (transform.position);
		}


		private void Update ()
		{
			if (disableDuringGameplay)
			{
				/*
				 * This code block will be run if the character is a Player, and not controlled during regular gameplay.
				 */

				if (KickStarter.stateHandler && KickStarter.stateHandler.gameState == GameState.Cutscene)
				{
					/*
					 * We are not in regular gameplay, so we can override the character's movement with the NavMeshAgent.
					 */

					navMeshAgent.enabled = true;
					SetMotionControl ();
					SetCharacterPosition ();
				}
				else if (_char.IsPlayer && KickStarter.settingsManager && KickStarter.settingsManager.movementMethod == MovementMethod.Direct)
				{
					/*
					 * Move with the NavMeshAgent, so as to do without colliders
					 */

					float targetSpeed = 0f;
					if (_char.charState == CharState.Move)
					{
						if (useACForSpeedValues)
						{
							targetSpeed = (_char.isRunning) ? (_char.runSpeedScale) : _char.walkSpeedScale;
						}
						else
						{
							targetSpeed = (_char.isRunning) ? (originalSpeed * runSpeedFactor) : originalSpeed;
						}
					}

					navMeshAgent.enabled = true;
					navMeshAgent.ResetPath ();

					directSpeed = Mathf.Lerp (directSpeed, targetSpeed, Time.deltaTime * _char.acceleration);
					_char.motionControl = MotionControl.JustTurning;
					navMeshAgent.Move (_char.TransformForward * directSpeed * Time.deltaTime);
				}
				else
				{
					/*
					 * We are in regular gameplay, so disable the NavMeshAgent and let AC take full control.
					 */

					navMeshAgent.enabled = false;
					_char.motionControl = MotionControl.Automatic;
				}
			}
			else
			{
				/*
				 * This code block will be run if we can override the character's movement at all times.
				 */

				SetMotionControl ();
				SetCharacterPosition ();
			}
		}


		private void SetCharacterPosition ()
		{
			/*
			 * Move the character, unless they are spot-turning.
			 */
			if (_char && !_char.IsTurningBeforeWalking ())
			{
				/* 
				 * We could just set the destination as _char.GetTargetPosition(), but this function will return the character's position if they
				 * are not on a path. This is normally fine, but the path is also removed once the character begins decelerating.
				 * Therefore, we record the "last known" targetPosition for as long as the character is on a Path.
				 * Note: If the character overshoots and starts 'sliding' backward, try increasing the NavMeshAgent's stopping distance.
				 */
				if (_char.GetPath () || _char.charState == CharState.Idle)
				{
					targetPosition = _char.GetTargetPosition ();
				}

				/**
				 * Stop the character from running if they are closer than the "Stopping distance" to from their target.
				 */
				if (_char.isRunning && Vector3.Distance (targetPosition, transform.position) < navMeshAgent.stoppingDistance)
				{
					if (_char.WillStopAtNextNode ())
					{
						_char.isRunning = false;
					}
				}

				/* 
				 * Scale the speed if we are running. Note that the original speed is recorded in Start(), so you will need to replay the scene
				 * to see changes made to the NavMeshAgent's "speed" variable take effect.
				 */
				if (useACForSpeedValues)
				{
					navMeshAgent.speed = (_char.isRunning) ? (_char.runSpeedScale) : _char.walkSpeedScale;
				}
				else
				{
					navMeshAgent.speed = (_char.isRunning) ? (originalSpeed * runSpeedFactor) : originalSpeed;
				}

				/*
				 * Provided the NavMeshAgent is on a NavMesh, set the destination point
				 */
				if (navMeshAgent.isOnNavMesh)
				{
					navMeshAgent.SetDestination (targetPosition);
				}
			}
		}


		/*
		 * We could also set the character's motionControl to MotionControl.Manual,
		 * but this way we can make use of AC's "Turn before walking" feature.
		 */
		private void SetMotionControl ()
		{
			_char.motionControl = (useACForTurning) ? MotionControl.JustTurning : MotionControl.Manual;
		}

	}

}