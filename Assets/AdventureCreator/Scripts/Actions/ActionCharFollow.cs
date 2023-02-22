/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCharFollow.cs"
 * 
 *	This action causes NPCs to follow other characters.
 *	If they are moved in any other way, their following
 *	state will reset
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
	public class ActionCharFollow : Action
	{

		public int npcToMoveParameterID = -1;
		public int charToFollowParameterID = -1;

		public int npcToMoveID = 0;
		public int charToFollowID = 0;

		public NPC npcToMove;
		protected NPC runtimeNpcToMove;
		public Char charToFollow;
		protected Char runtimeCharToFollow;
		public bool followPlayer;
		public int followPlayerID = -1;

		public bool movePlayer;
		public int movePlayerID = 0;

		public bool faceWhenIdle;
		public float updateFrequency = 2f;
		public float followDistance = 1f;
		public float followDistanceMax = 15f;
		public enum FollowType { StartFollowing, StopFollowing };
		public FollowType followType;
		public bool randomDirection = false;
		public bool followAcrossScenes = false;


		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "NPC follow"; }}
		public override string Description { get { return "Makes an NPC follow another Character, whether it be a fellow NPC or the Player. If they exceed a maximum distance from their target, they will run towards them. Note that making an NPC move via another Action will make them stop following anyone."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && movePlayer)
			{
				runtimeNpcToMove = AssignPlayer (movePlayerID, parameters, npcToMoveParameterID);
			}
			else
			{
				runtimeNpcToMove = AssignFile<NPC> (parameters, npcToMoveParameterID, npcToMoveID, npcToMove);
			}

			if (followType == FollowType.StartFollowing)
			{
				if (followPlayer)
				{
					runtimeCharToFollow = AssignPlayer (followPlayerID, parameters, charToFollowParameterID);
				}
				else
				{
					runtimeCharToFollow = AssignFile<Char> (parameters, charToFollowParameterID, charToFollowID, charToFollow);
				}

				if (runtimeNpcToMove != null && runtimeNpcToMove == runtimeCharToFollow)
				{
					LogWarning ("The character " + runtimeNpcToMove.GetName () + " cannot follow themselves!", runtimeNpcToMove);
					runtimeNpcToMove = null;
				}

				if (runtimeNpcToMove != null && runtimeNpcToMove == KickStarter.player)
				{
					runtimeNpcToMove = null;
					LogWarning ("The active Player cannot follow another character.");
				}
			}
		}
		
		
		public override float Run ()
		{
			if (runtimeNpcToMove)
			{
				if (followType == FollowType.StopFollowing)
				{
					runtimeNpcToMove.StopFollowing ();
					return 0f;
				}

				if (runtimeCharToFollow != null)
				{
					bool _followPlayer = (runtimeCharToFollow == KickStarter.player);
					runtimeNpcToMove.FollowAssign (runtimeCharToFollow, _followPlayer, updateFrequency, followDistance, followDistanceMax, faceWhenIdle, randomDirection, followAcrossScenes);
				}
			}

			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			bool ignoreNPC = false;

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				movePlayer = EditorGUILayout.Toggle ("Move inactive Player?", movePlayer);
				if (movePlayer)
				{
					ignoreNPC = true;
					npcToMoveParameterID = ChooseParameterGUI ("Move Player ID:", parameters, npcToMoveParameterID, ParameterType.Integer);
					if (npcToMoveParameterID < 0)
						movePlayerID = ChoosePlayerGUI (movePlayerID, false);
				}
			}

			if (!ignoreNPC)
			{
				npcToMoveParameterID = Action.ChooseParameterGUI ("NPC to affect:", parameters, npcToMoveParameterID, ParameterType.GameObject);
				if (npcToMoveParameterID >= 0)
				{
					npcToMoveID = 0;
					npcToMove = null;
				}
				else
				{
					npcToMove = (NPC)EditorGUILayout.ObjectField ("NPC to affect:", npcToMove, typeof (NPC), true);

					npcToMoveID = FieldToID<NPC> (npcToMove, npcToMoveID);
					npcToMove = IDToField<NPC> (npcToMove, npcToMoveID, false);
				}
			}

			followType = (FollowType) EditorGUILayout.EnumPopup ("Follow type:", followType);
			if (followType == FollowType.StartFollowing)
			{
				EditorGUILayout.Space ();

				followPlayer = EditorGUILayout.Toggle ("Follow Player?", followPlayer);

				if (followPlayer)
				{
					if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
					{
						charToFollowParameterID = ChooseParameterGUI ("Follow Player ID:", parameters, charToFollowParameterID, ParameterType.Integer);
						if (charToFollowParameterID < 0)
						{
							followPlayerID = ChoosePlayerGUI (followPlayerID, true);

							if (movePlayer && npcToMoveParameterID < 0 && movePlayerID == followPlayerID)
							{
								EditorGUILayout.HelpBox ("A character cannot follow themselves.", MessageType.Warning);
							}
						}
					}
				}
				else
				{
					charToFollowParameterID = Action.ChooseParameterGUI ("Character to follow:", parameters, charToFollowParameterID, ParameterType.GameObject);
					if (charToFollowParameterID >= 0)
					{
						charToFollowID = 0;
						charToFollow = null;
					}
					else
					{
						charToFollow = (Char) EditorGUILayout.ObjectField ("Character to follow:", charToFollow, typeof(Char), true);
						
						if (charToFollow && charToFollow == (Char) npcToMove)
						{
							ACDebug.LogWarning ("An NPC cannot follow themselves!", charToFollow);
							charToFollow = null;
						}
						else
						{
							charToFollowID = FieldToID <Char> (charToFollow, charToFollowID);
							charToFollow = IDToField <Char> (charToFollow, charToFollowID, false);
						}
					}
				}

				randomDirection = EditorGUILayout.Toggle ("Randomise position?", randomDirection);
				updateFrequency = EditorGUILayout.FloatField ("Update frequency (s):", updateFrequency);
				if (updateFrequency <= 0f)
				{
					EditorGUILayout.HelpBox ("Update frequency must be greater than zero.", MessageType.Warning);
				}
				followDistance = EditorGUILayout.FloatField ("Minimum distance:", followDistance);
				if (followDistance <= 0f)
				{
					EditorGUILayout.HelpBox ("Minimum distance must be greater than zero.", MessageType.Warning);
				}
				followDistanceMax = EditorGUILayout.FloatField ("Maximum distance:", followDistanceMax);
				if (followDistanceMax <= 0f || followDistanceMax < followDistance)
				{
					EditorGUILayout.HelpBox ("Maximum distance must be greater than minimum distance.", MessageType.Warning);
				}

				faceWhenIdle = EditorGUILayout.Toggle ("Face when idle?", faceWhenIdle);

				if (movePlayer && followPlayer && followPlayerID < 0 && charToFollowParameterID < 0)
				{ 
					followAcrossScenes = EditorGUILayout.Toggle ("Follow across scenes?", followAcrossScenes);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				if (!followPlayer && charToFollow != null && !charToFollow.IsPlayer)
				{
					AddSaveScript <RememberNPC> (charToFollow);
				}
				if (!movePlayer || (KickStarter.settingsManager == null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow))
				{
					AddSaveScript<RememberNPC> (npcToMove);
				}
			}

			if (!followPlayer)
			{
				AssignConstantID <Char> (charToFollow, charToFollowID, charToFollowParameterID);
			}
			if (!movePlayer || (KickStarter.settingsManager == null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow))
			{
				AssignConstantID<NPC> (npcToMove, npcToMoveID, npcToMoveParameterID);
			}
		}

		
		public override string SetLabel ()
		{
			if (npcToMove != null)
			{
				if (followType == FollowType.StopFollowing)
				{
					return "Stop " + npcToMove;
				}
				else
				{
					if (followPlayer)
					{
						return npcToMove.name + " to Player";
					}
					else if (charToFollow != null)
					{
						return (npcToMove.name + " to " + charToFollow.name);
					}
				}
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (npcToMoveParameterID < 0)
			{
				if (npcToMove && npcToMove.gameObject == _gameObject) return true;
				if (npcToMoveID == id) return true;
			}
			if (!followPlayer && charToFollowParameterID < 0)
			{
				if (charToFollow && charToFollow.gameObject == _gameObject) return true;
				if (charToFollowID == id) return true;
			}
			if (followPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (movePlayer)
			{
				if (_playerID < 0) return false;
				if (movePlayerID < 0 && npcToMoveParameterID < 0) return true;
				if (npcToMoveParameterID < 0 && movePlayerID == _playerID) return true;
			}
			if (followPlayer)
			{
				if (_playerID < 0) return true;
				if (followPlayerID < 0 && charToFollowParameterID < 0) return true;
				if (charToFollowParameterID < 0 && followPlayerID == _playerID) return true;
			}
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: NPC follow' Action, set to command an NPC to follow a character</summary>
		 * <param name = "npcToMove>The NPC to affect</param>
		 * <param name = "characterToFollow">The character the NPC should follow</param>
		 * <param name = "minimumDistance">The minimum distance the NPC should be from the NPC</param>
		 * <param name = "maximumDistance">The maximum distance the NPC should be from the NPC</param>
		 * <param name = "updateFrequency">How often the NPC should move towards the character they're following</param>
		 * <param name = "randomisePosition">If True, then the NPC will move to some random point around the character they're following</param>
		 * <param name = "faceCharacterWhenIdle">If True, then the NPC will face the character they're following whenever idle</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFollow CreateNew_Start (NPC npcToMove, AC.Char characterToFollow, float minimumDistance, float maximumDistance, float updateFrequency = 2f, bool randomisePosition = false, bool faceCharacterWhenIdle = false)
		{
			ActionCharFollow newAction = CreateNew<ActionCharFollow> ();
			newAction.followType = FollowType.StartFollowing;
			newAction.npcToMove = npcToMove;
			newAction.TryAssignConstantID (newAction.npcToMove, ref newAction.npcToMoveID);
			newAction.charToFollow = characterToFollow;
			newAction.TryAssignConstantID (newAction.charToFollow, ref newAction.charToFollowID);
			newAction.followDistance = minimumDistance;
			newAction.followDistanceMax = maximumDistance;
			newAction.updateFrequency = updateFrequency;
			newAction.randomDirection = randomisePosition;
			newAction.faceWhenIdle = faceCharacterWhenIdle;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Face direction' Action, set to command an NPC to stop following anyone</summary>
		 * <param name = "npcToMove">The NPC to stop following anyone</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFollow CreateNew_Stop (NPC npcToMove)
		{
			ActionCharFollow newAction = CreateNew<ActionCharFollow> ();
			newAction.followType = FollowType.StopFollowing;
			newAction.npcToMove = npcToMove;
			newAction.TryAssignConstantID (newAction.npcToMove, ref newAction.npcToMoveID);
			return newAction;
		}
		
	}

}