/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"IKLimbController.cs"
 * 
 *	This processes all IKCommand classes assigned to a specific AvatarIKGoal.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** This processes all IKCommand classes assigned to a specific AvatarIKGoal. */
	[System.Serializable]
	public class IKLimbController
	{

		#region Variables

		private bool isControlling;
		private List<IKCommand> ikCommands;
		
		#endregion


		#region Constructors

		/** The default Constructor */
		public IKLimbController ()
		{
			ikCommands = new List<IKCommand> ();
			isControlling = false;
		}

		#endregion


		#region PublicFunctions

		/** Updates the IK */
		public void Update ()
		{
			if (!isControlling) return;

			for (int i = 0; i < ikCommands.Count; i++)
			{
				ikCommands[i].Update (Time.deltaTime);
			}
		}


		/** Applies the effect to an Animator */
		public void OnAnimatorIK (Animator animator, AvatarIKGoal avatarIKGoal)
		{
			if (!isControlling) return;

			if (ikCommands.Count == 0)
			{
				animator.SetIKPositionWeight (avatarIKGoal, 0f);
				isControlling = false;
				return;
			}

			if (ikCommands.Count == 1)
			{
				ikCommands[0].OnAnimatorIK (animator, avatarIKGoal);
				return;
			}

			if (ikCommands[0].IsReadyToDestroy)
			{
				ikCommands.RemoveAt (0);
				return;
			}

			ikCommands[1].OnAnimatorIK (animator, avatarIKGoal, ikCommands[0]);
		}


		/**
		 * <summary>Adds a new IK target</summary>
		 * <param name = "targetTransform">The transform to target</param>
		 * <param name = "transitionCurve">A curve to describe the motion</param>
		 * <param name = "isInstant">If True, the full weight will be applied instantly</param>
		 */
		public void AddTarget (Transform targetTransform, AnimationCurve transitionCurve, bool isInstant)
		{
			if (targetTransform == null || transitionCurve == null) return;

			while (ikCommands.Count > 1)
			{
				ikCommands.RemoveAt (0);
			}

			if (ikCommands.Count == 1)
			{
				if (ikCommands[0].HasTarget (targetTransform) && !ikCommands[0].IsInTransition)
				{
					return;
				}

				if (isInstant || ikCommands[0].IsInTransition)
				{
					ikCommands.RemoveAt (0);
				}
				else
				{
					ikCommands[0].Deactivate (transitionCurve);
				}
			}

			ikCommands.Add (new IKCommand (targetTransform, transitionCurve, isInstant));
			isControlling = true;
		}

		
		/**
		 * <summary></summary>Clears all IK commands</summary>
		 * <param name = "isInstant">If True, the effects will be disabled instantly, as opposed to transitioned out smoothly</param>
		 */
		public void Clear (bool isInstant)
		{
			if (isInstant)
			{
				ikCommands.Clear ();
				return;
			}

			for (int i=0; i< ikCommands.Count; i++)
			{
				ikCommands[i].Deactivate ();
			}
		}


		/** Serializes the current state into a single string */
		public string CreateSaveData ()
		{
			if (!isControlling) return string.Empty;

			IKCommand commandToSave = GetLastActiveCommand ();
			if (commandToSave != null)
			{
				return commandToSave.GetSaveData ();
			}
			
			return string.Empty;
		}


		/** Restores a state from a serialized string */
		public void LoadData (string data)
		{
			Clear (true);
			
			if (string.IsNullOrEmpty (data)) return;
			
			IKCommand newCommand = IKCommand.LoadData (data);
			if (newCommand != null)
			{
				ikCommands.Add (newCommand);
				isControlling = true;
			}
		}

		#endregion


		#region PrivateFunctions

		private IKCommand GetLastActiveCommand ()
		{
			if (ikCommands.Count == 0) return null;

			IKCommand lastCommand = ikCommands[ikCommands.Count - 1];
			if (lastCommand.IsActive)
			{
				return lastCommand;
			}
			return null;
		}

		#endregion

	}

}
 