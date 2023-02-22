/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionPlayerLockPath.cs"
 * 
 *	In Direct control mode, the player can be assigned a path,
 *	and will only be able to move along that path during gameplay.
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
	public class ActionPlayerLockPath : Action
	{

		public bool unlock;

		public Paths movePath;
		public int movePathConstantID = 0;
		public int movePathParameterID = -1;
		protected Paths runtimeMovePath;

		public int snapNodeIndex;
		public int snapNodeIndexParameterID = -1;

		public bool lockedPathCanReverse;

		public PathSnapping pathSnapping = PathSnapping.SnapToStart;


		public override ActionCategory Category { get { return ActionCategory.Player; } }
		public override string Title { get { return "Lock to Path"; } }
		public override string Description { get { return "When using Direct or First Person control, constrains the Player's movement to a pre-defined Path."; } }


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (!unlock)
			{
				runtimeMovePath = AssignFile <Paths> (parameters, movePathParameterID, movePathConstantID, movePath);
				snapNodeIndex = AssignInteger (parameters, snapNodeIndexParameterID, snapNodeIndex);
			}
		}


		public override float Run ()
		{
			if (KickStarter.settingsManager.movementMethod != MovementMethod.Direct && KickStarter.settingsManager.movementMethod != MovementMethod.FirstPerson)
			{
				LogWarning ("The Player can only be locked to a Path when using Direct or First Person movement.");
				return 0f;
			}

			if (KickStarter.player == null)
			{
				LogWarning ("No Player found!");
				return 0f;
			}

			if (unlock)
			{
				if (KickStarter.player.IsLockedToPath ())
				{
					KickStarter.player.EndPath ();
				}
			}
			else if (runtimeMovePath)
			{
				KickStarter.player.SetLockedPath (runtimeMovePath, lockedPathCanReverse, pathSnapping, snapNodeIndex);
				KickStarter.player.SetMoveDirectionAsForward ();
			}

			return 0f;
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (KickStarter.settingsManager && KickStarter.settingsManager.movementMethod != MovementMethod.Direct && KickStarter.settingsManager.movementMethod != MovementMethod.FirstPerson)
			{
				EditorGUILayout.HelpBox ("The Player can only be locked to a Path when using Direct or First Person movement.", MessageType.Info);
				return;
			}

			unlock = EditorGUILayout.Toggle ("Unlock from Path?", unlock);
			if (unlock)
			{
				return;
			}

			movePathParameterID = Action.ChooseParameterGUI ("Path to lock to:", parameters, movePathParameterID, ParameterType.GameObject);
			if (movePathParameterID >= 0)
			{
				movePathConstantID = 0;
				movePath = null;
			}
			else
			{
				movePath = (Paths) EditorGUILayout.ObjectField ("Path to lock to:", movePath, typeof (Paths), true);

				movePathConstantID = FieldToID<Paths> (movePath, movePathConstantID);
				movePath = IDToField<Paths> (movePath, movePathConstantID, true);
			}

			if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct)
			{
				lockedPathCanReverse = EditorGUILayout.Toggle ("Can move both ways?", lockedPathCanReverse);
			}

			pathSnapping = (PathSnapping) EditorGUILayout.EnumPopup ("Path snapping:", pathSnapping);
			if (pathSnapping == PathSnapping.SnapToNode)
			{
				snapNodeIndexParameterID = Action.ChooseParameterGUI ("Snap to node:", parameters, snapNodeIndexParameterID, ParameterType.Integer);
				if (snapNodeIndexParameterID == -1)
				{
					snapNodeIndex = EditorGUILayout.IntField ("Snap to node:", snapNodeIndex);
				}
			}
		}


		public override bool ReferencesPlayer (int playerID = -1)
		{
			return true;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!unlock && movePathParameterID < 0)
			{
				if (movePath && movePath.gameObject == _gameObject) return true;
				if (movePathConstantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Player: Lock to Path' Action</summary>
		 * <param name = "limitToPath">If set, a Path to constrain movement along, if using Direct movement</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayerLockPath CreateNew (Paths limitToPath)
		{
			ActionPlayerLockPath newAction = CreateNew<ActionPlayerLockPath> ();
			newAction.movePath = limitToPath;
			return newAction;
		}

	}

}