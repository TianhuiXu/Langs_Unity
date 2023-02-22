/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ObjectiveInstance.cs"
 * 
 *	A runtime instance of an active Objective
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/** A runtime instance of an active Objective */
	public class ObjectiveInstance
	{

		#region Variables

		protected Objective linkedObjective;
		protected int currentStateID;

		#endregion


		#region Constructors

		public ObjectiveInstance (int objectiveID)
		{
			if (KickStarter.inventoryManager)
			{
				linkedObjective = KickStarter.inventoryManager.GetObjective (objectiveID);
				currentStateID = 0;
			}
		}


		public ObjectiveInstance (int objectiveID, int startingStateID)
		{
			if (KickStarter.inventoryManager)
			{
				linkedObjective = KickStarter.inventoryManager.GetObjective (objectiveID);;
				currentStateID = startingStateID;
			}
		}


		public ObjectiveInstance (string saveData)
		{
			if (KickStarter.inventoryManager)
			{
				string[] chunkData = saveData.Split (SaveSystem.colon[0]);
				if (chunkData.Length == 2)
				{
					int objectiveID = -1;
					if (int.TryParse (chunkData[0], out objectiveID))
					{
						linkedObjective = KickStarter.inventoryManager.GetObjective (objectiveID);
					}

					int.TryParse (chunkData[1], out currentStateID);
				}
			}
		}

		#endregion


		#region GetSet

		/** The Objective this instance is linked to */
		public Objective Objective
		{
			get
			{
				return linkedObjective;
			}
		}


		/** The ID number of the instance's current objective state */
		public int CurrentStateID
		{
			get
			{
				return currentStateID;
			}
			set
			{
				if (CurrentState.stateType == ObjectiveStateType.Complete && linkedObjective.lockStateWhenComplete)
				{
					if (currentStateID != value)
					{
						ACDebug.Log ("Cannot update the state of completed Objective " + linkedObjective.Title + " as it is locked.");
					}
					return;
				}
				if (CurrentState.stateType == ObjectiveStateType.Fail && linkedObjective.lockStateWhenFail)
				{
					if (currentStateID != value)
					{
						ACDebug.Log ("Cannot update the state of failed Objective " + linkedObjective.Title + " as it is locked.");
					}
					return;
				}

				ObjectiveState newState = linkedObjective.GetState (value);
				if (newState != null)
				{
					int oldStateID = currentStateID;
					currentStateID = value;

					if (oldStateID != currentStateID)
					{
						KickStarter.eventManager.Call_OnObjectiveUpdate (this);
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot set the state of objective " + linkedObjective.ID + " to " + value + " because it does not exist!");
				}
			}
		}


		/** The instance's current objective state */
		public ObjectiveState CurrentState
		{
			get
			{
				return linkedObjective.GetState (currentStateID);
			}
		}


		/** A data string containing all saveable data */
		public string SaveData
		{
			get
			{
				return linkedObjective.ID.ToString ()
						+ SaveSystem.colon
						+ currentStateID.ToString ();
			}
		}

		#endregion

	}

}