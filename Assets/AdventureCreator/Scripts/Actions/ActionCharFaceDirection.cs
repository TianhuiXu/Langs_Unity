/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCharDirection.cs"
 * 
 *	This action is used to make characters turn to face fixed directions relative to the camera.
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
	public class ActionCharFaceDirection : Action
	{
		
		public int charToMoveParameterID = -1;
		public int charToMoveID = 0;

		public bool isInstant;
		public CharDirection direction;
		public int directionParameterID = -1;

		public Char charToMove;
		protected Char runtimeCharToMove;

		public bool isPlayer;
		public int playerID = -1;
		
		[SerializeField] protected RelativeTo relativeTo = RelativeTo.Camera;
		public enum RelativeTo { Camera, Character };


		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "Face direction"; }}
		public override string Description { get { return "Makes a Character turn, either instantly or over time, to face a direction relative to the camera – i.e. up, down, left or right."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				runtimeCharToMove = AssignPlayer (playerID, parameters, charToMoveParameterID);
			}
			else
			{
				runtimeCharToMove = AssignFile <Char> (parameters, charToMoveParameterID, charToMoveID, charToMove);
			}

			if (directionParameterID >= 0)
			{
				int _directionInt = AssignInteger (parameters, directionParameterID, 0);
				direction = (CharDirection) _directionInt;
			}
		}


		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				
				if (runtimeCharToMove != null)
				{
					if (!isInstant && runtimeCharToMove.IsMovingAlongPath ())
					{
						runtimeCharToMove.EndPath ();
					}

					Vector3 lookVector = AdvGame.GetCharLookVector (direction, (relativeTo == RelativeTo.Character) ? runtimeCharToMove : null);
					runtimeCharToMove.SetLookDirection (lookVector, isInstant);

					if (!isInstant)
					{
						if (willWait)
						{
							return (defaultPauseTime);
						}
					}
				}
				
				return 0f;
			}
			else
			{
				if (runtimeCharToMove.IsTurning ())
				{
					return defaultPauseTime;
				}
				else
				{
					isRunning = false;
					return 0f;
				}
			}
		}
		
		
		public override void Skip ()
		{
			if (runtimeCharToMove != null)
			{
				Vector3 lookVector = AdvGame.GetCharLookVector (direction, (relativeTo == RelativeTo.Character) ? runtimeCharToMove : null);
				runtimeCharToMove.SetLookDirection (lookVector, true);
			}
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Affect Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					charToMoveParameterID = ChooseParameterGUI ("Player ID:", parameters, charToMoveParameterID, ParameterType.Integer);
					if (charToMoveParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				charToMoveParameterID = ChooseParameterGUI ("Character to turn:", parameters, charToMoveParameterID, ParameterType.GameObject);
				if (charToMoveParameterID >= 0)
				{
					charToMoveID = 0;
					charToMove = null;
				}
				else
				{
					charToMove = (Char) EditorGUILayout.ObjectField ("Character to turn:", charToMove, typeof(Char), true);
					
					charToMoveID = FieldToID <Char> (charToMove, charToMoveID);
					charToMove = IDToField <Char> (charToMove, charToMoveID, false);
				}
			}

			directionParameterID = Action.ChooseParameterGUI ("Direction to face:", parameters, directionParameterID, ParameterType.Integer);
			if (directionParameterID < 0)
			{
				direction = (CharDirection) EditorGUILayout.EnumPopup ("Direction to face:", direction);
			}

			relativeTo = (RelativeTo) EditorGUILayout.EnumPopup ("Direction is relative to:", relativeTo);
			isInstant = EditorGUILayout.Toggle ("Is instant?", isInstant);
			if (!isInstant)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				if (saveScriptsToo && charToMove != null && !charToMove.IsPlayer)
				{
					AddSaveScript <RememberNPC> (charToMove);
				}

				AssignConstantID <Char> (charToMove, charToMoveID, charToMoveParameterID);
			}
		}

		
		public override string SetLabel ()
		{
			if (charToMove != null)
			{
				return charToMove.name + " - " + direction;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && charToMoveParameterID < 0)
			{
				if (charToMove != null && charToMove.gameObject == _gameObject) return true;
				if (charToMoveID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && charToMoveParameterID < 0) return true;
			return (charToMoveParameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Face direction' Action</summary>
		 * <param name = "characterToTurn">The character to affect</param>
		 * <param name = "directionToFace">The direction to face</param>
		 * <param name = "relativeTo">What the supplied direction is relative to</param>
		 * <param name = "isInstant">If True, the character will stop turning their head instantly</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharFaceDirection CreateNew (AC.Char characterToTurn, CharDirection directionToFace, RelativeTo relativeTo = RelativeTo.Camera, bool isInstant = false, bool waitUntilFinish = false)
		{
			ActionCharFaceDirection newAction = CreateNew<ActionCharFaceDirection> ();
			newAction.charToMove = characterToTurn;
			newAction.TryAssignConstantID (newAction.charToMove, ref newAction.charToMoveID);
			newAction.direction = directionToFace;
			newAction.relativeTo = relativeTo;
			newAction.isInstant = isInstant;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}

}