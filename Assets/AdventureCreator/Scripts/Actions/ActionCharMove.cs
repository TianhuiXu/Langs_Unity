/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCharMove.cs"
 * 
 *	This action moves characters by assinging them a Paths object.
 *	If a player is moved, the game will automatically pause.
 * 
*/

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionCharMove : Action
	{

		public enum MovePathMethod { MoveOnNewPath, StopMoving, ResumeLastSetPath };
		public MovePathMethod movePathMethod = MovePathMethod.MoveOnNewPath;

		public int charToMoveParameterID = -1;
		public int movePathParameterID = -1;

		public int charToMoveID = 0;
		public int movePathID = 0;

		public bool stopInstantly;
		public Paths movePath;
		protected Paths runtimeMovePath;

		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;
		public Char charToMove;

		public bool doTeleport;
		public bool startRandom = false;

		protected Char runtimeChar;

		
		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "Move along path"; }}
		public override string Description { get { return "Moves the Character along a pre-determined path. Will adhere to the speed setting selected in the relevant Paths object. Can also be used to stop a character from moving, or resume moving along a path if it was previously stopped."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeMovePath = AssignFile <Paths> (parameters, movePathParameterID, movePathID, movePath);

			if (isPlayer)
			{
				runtimeChar = AssignPlayer (playerID, parameters, playerParameterID);
			}
			else
			{
				runtimeChar = AssignFile<Char> (parameters, charToMoveParameterID, charToMoveID, charToMove);
			}
		}


		public override float Run ()
		{
			if (runtimeMovePath && runtimeMovePath.GetComponent <Char>())
			{
				LogWarning ("Can't follow a Path attached to a Character!");
				return 0f;
			}

			if (!isRunning)
			{
				isRunning = true;

				if (runtimeChar)
				{
					if (!runtimeChar.IsActivePlayer ())
					{
						NPC npcToMove = (NPC) runtimeChar;
						npcToMove.StopFollowing ();
					}

					switch (movePathMethod)
					{
						case MovePathMethod.StopMoving:
							runtimeChar.EndPath ();
							if (runtimeChar.IsActivePlayer () && KickStarter.playerInteraction.GetHotspotMovingTo () != null)
							{
								KickStarter.playerInteraction.StopMovingToHotspot ();
							}

							if (stopInstantly)
							{
								runtimeChar.Halt ();
							}
							break;

						case MovePathMethod.MoveOnNewPath:
							if (runtimeMovePath)
							{
								int randomIndex = -1;
								if (runtimeMovePath.pathType == AC_PathType.IsRandom && startRandom)
								{
									if (runtimeMovePath.nodes.Count > 1)
									{
										randomIndex = Random.Range (0, runtimeMovePath.nodes.Count);
									}
								}

								PrepareCharacter (randomIndex);

								if (willWait && runtimeMovePath.pathType != AC_PathType.ForwardOnly && runtimeMovePath.pathType != AC_PathType.ReverseOnly)
								{
									willWait = false;
									LogWarning ("Cannot pause while character moves along a linear path, as this will create an indefinite cutscene.");
								}

								if (randomIndex >= 0)
								{
									runtimeChar.SetPath (runtimeMovePath, randomIndex, 0);
								}
								else
								{
									runtimeChar.SetPath (runtimeMovePath);
								}

								if (willWait)
								{
									return defaultPauseTime;
								}
							}
							break;

						case MovePathMethod.ResumeLastSetPath:
							runtimeChar.ResumeLastPath ();
							break;

						default:
							break;
					}
				}

				return 0f;
			}
			else
			{
				if (runtimeChar.GetPath () != runtimeMovePath)
				{
					isRunning = false;
					return 0f;
				}
				else
				{
					return (defaultPauseTime);
				}
			}
		}


		public override void Skip ()
		{
			if (runtimeChar)
			{
				runtimeChar.EndPath (runtimeMovePath);

				if (!runtimeChar.IsActivePlayer ())
				{
					NPC npcToMove = (NPC) runtimeChar;
					npcToMove.StopFollowing ();
				}

				if (movePathMethod == MovePathMethod.StopMoving)
				{
					return;
				}
				else if (movePathMethod == MovePathMethod.ResumeLastSetPath)
				{
					runtimeChar.ResumeLastPath ();
					runtimeMovePath = runtimeChar.GetPath ();
				}
				
				if (runtimeMovePath != null)
				{
					int randomIndex = -1;

					switch (runtimeMovePath.pathType)
					{
						case AC_PathType.ForwardOnly:
							{
								// Place at end
								int i = runtimeMovePath.nodes.Count - 1;
								runtimeChar.Teleport (runtimeMovePath.nodes[i]);
								if (i > 0)
								{
									runtimeChar.SetLookDirection (runtimeMovePath.nodes[i] - runtimeMovePath.nodes[i - 1], true);
								}
								return;
							}

						case AC_PathType.ReverseOnly:
							{
								// Place at start
								runtimeChar.Teleport (runtimeMovePath.transform.position);
								if (runtimeMovePath.nodes.Count > 1)
								{
									runtimeChar.SetLookDirection (runtimeMovePath.nodes[0] - runtimeMovePath.nodes[1], true);
								}
								return;
							}

						case AC_PathType.IsRandom:
							if (startRandom && runtimeMovePath.nodes.Count > 1)
							{
								randomIndex = Random.Range (0, runtimeMovePath.nodes.Count);
							}
							break;

						default:
							break;
					}

					PrepareCharacter (randomIndex);

					if (!isPlayer)
					{
						if (randomIndex >= 0)
						{
							runtimeChar.SetPath (runtimeMovePath, randomIndex, 0);
						}
						else
						{
							runtimeChar.SetPath (runtimeMovePath);
						}
					}
				}
			}
		}


		protected void PrepareCharacter (int randomIndex)
		{
			if (doTeleport)
			{
				if (randomIndex >= 0)
				{
					runtimeChar.Teleport (runtimeMovePath.nodes[randomIndex]);
				}
				else
				{
					int numNodes = runtimeMovePath.nodes.Count;

					if (runtimeMovePath.pathType == AC_PathType.ReverseOnly)
					{
						runtimeChar.Teleport (runtimeMovePath.nodes[numNodes-1]);

						// Set rotation if there is more than two nodes
						if (numNodes > 2)
						{
							runtimeChar.SetLookDirection (runtimeMovePath.nodes[numNodes-2] - runtimeMovePath.nodes[numNodes-1], true);
						}
					}
					else
					{
						runtimeChar.Teleport (runtimeMovePath.transform.position);
						
						// Set rotation if there is more than one node
						if (numNodes > 1)
						{
							runtimeChar.SetLookDirection (runtimeMovePath.nodes[1] - runtimeMovePath.nodes[0], true);
						}
					}
				}
			}
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);

			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerParameterID = ChooseParameterGUI ("Player ID:", parameters, playerParameterID, ParameterType.Integer);
					if (playerParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				charToMoveParameterID = ChooseParameterGUI ("Character to move:", parameters, charToMoveParameterID, ParameterType.GameObject);
				if (charToMoveParameterID >= 0)
				{
					charToMoveID = 0;
					charToMove = null;
				}
				else
				{
					charToMove = (Char) EditorGUILayout.ObjectField ("Character to move:", charToMove, typeof (Char), true);
					
					charToMoveID = FieldToID <Char> (charToMove, charToMoveID);
					charToMove = IDToField <Char> (charToMove, charToMoveID, false);
				}
			}

			movePathMethod = (MovePathMethod) EditorGUILayout.EnumPopup ("Method:", movePathMethod);

			switch (movePathMethod)
			{
				case MovePathMethod.MoveOnNewPath:
				{
					movePathParameterID = Action.ChooseParameterGUI ("Path to follow:", parameters, movePathParameterID, ParameterType.GameObject);
					if (movePathParameterID >= 0)
					{
						movePathID = 0;
						movePath = null;
					}
					else
					{
						movePath = (Paths) EditorGUILayout.ObjectField ("Path to follow:", movePath, typeof(Paths), true);
					
						movePathID = FieldToID <Paths> (movePath, movePathID);
						movePath = IDToField <Paths> (movePath, movePathID, false);
					}

					if (movePath != null && movePath.pathType == AC_PathType.IsRandom)
					{
						startRandom = EditorGUILayout.Toggle ("Start at random node?", startRandom);
					}

					doTeleport = EditorGUILayout.Toggle ("Teleport to start?", doTeleport);
					if (movePath != null && movePath.pathType != AC_PathType.ForwardOnly && movePath.pathType != AC_PathType.ReverseOnly)
					{
						willWait = false;
					}
					else
					{
						willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
					}

					if (movePath != null && movePath.GetComponent <Char>())
					{
						EditorGUILayout.HelpBox ("Can't follow a Path attached to a Character!", MessageType.Warning);
					}
					break;
				}

				case MovePathMethod.StopMoving:
					stopInstantly = EditorGUILayout.Toggle ("Stop instantly?", stopInstantly);
					break;

				default:
					break;
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <ConstantID> (movePath);
				if (!isPlayer && charToMove != null && !charToMove.IsPlayer)
				{
					AddSaveScript <RememberNPC> (charToMove);
				}
			}

			if (!isPlayer)
			{
				AssignConstantID <Char> (charToMove, charToMoveID, charToMoveParameterID);
			}
			AssignConstantID <Paths> (movePath, movePathID, movePathParameterID);
		}
				
		
		public override string SetLabel ()
		{
			if (movePath != null)
			{
				if (charToMove != null)
				{
					return charToMove.name + " to " + movePath.name;
				}
				else if (isPlayer)
				{
					return "Player to " + movePath.name;
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && charToMoveParameterID < 0)
			{
				if (charToMove && charToMove.gameObject == _gameObject) return true;
				if (charToMoveID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>()) return true;
			if (movePathMethod == MovePathMethod.MoveOnNewPath && movePathParameterID < 0)
			{
				if (movePath && movePath.gameObject == _gameObject) return true;
				if (movePathID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && playerParameterID < 0) return true;
			return (playerParameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Move along path' Action, set to command a character to move along a new path</summary>
		 * <param name = "characterToMove">The character to affect</param>
		 * <param name = "pathToFollow">The Path that the character should follow</param>
		 * <param name = "teleportToStart">If True, the character will teleport to the first node on the Path</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharMove CreateNew_NewPath (AC.Char characterToMove, Paths pathToFollow, bool teleportToStart = false)
		{
			ActionCharMove newAction = CreateNew<ActionCharMove> ();
			newAction.movePathMethod = MovePathMethod.MoveOnNewPath;
			newAction.charToMove = characterToMove;
			newAction.TryAssignConstantID (newAction.charToMove, ref newAction.charToMoveID);
			newAction.movePath = pathToFollow;
			newAction.TryAssignConstantID (newAction.movePath, ref newAction.movePathID);
			newAction.doTeleport = teleportToStart;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Move along path' Action, set to command a character to resume moving along their last-assigned path</summary>
		 * <param name = "characterToMove">The character to affect</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharMove CreateNew_ResumeLastPath (AC.Char characterToMove)
		{
			ActionCharMove newAction = CreateNew<ActionCharMove> ();
			newAction.movePathMethod = MovePathMethod.ResumeLastSetPath;
			newAction.charToMove = characterToMove;
			newAction.TryAssignConstantID (newAction.charToMove, ref newAction.charToMoveID);
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Move along path' Action, set to command a character to stop moving</summary>
		 * <param name = "characterToStop">The character to affect</param>
		 * <param name = "stopInstantly">If True, the character will stop in one frame, as opposed to more naturally through deceleration</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharMove CreateNew_StopMoving (AC.Char characterToStop, bool stopInstantly = false)
		{
			ActionCharMove newAction = CreateNew<ActionCharMove> ();
			newAction.movePathMethod = MovePathMethod.StopMoving;
			newAction.charToMove = characterToStop;
			newAction.TryAssignConstantID (newAction.charToMove, ref newAction.charToMoveID);
			newAction.stopInstantly = stopInstantly;
			return newAction;
		}
		
	}

}