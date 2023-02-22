/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RuntimeObjectives.cs"
 * 
 *	This script keeps track of all active Objectives.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/** This script keeps track of all active Objectives. */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_objectives.html")]
	public class RuntimeObjectives : MonoBehaviour
	{

		#region Variables

		protected List<ObjectiveInstance> playerObjectiveInstances = new List<ObjectiveInstance>();
		protected List<ObjectiveInstance> globalObjectiveInstances = new List<ObjectiveInstance>();
		protected ObjectiveInstance selectedObjectiveInstance;

		#endregion


		#region PublicFunctions

		public void OnInitPersistentEngine ()
		{
			ClearAll ();
		}


		/** 
		 * <summary>Updates the state of an Objective</summary>
		 * <param name = "objectiveID">The ID of the Objective to update</param>
		 * <param name = "newStateID">The ID of the Objective's new state</param>
		 * <param name = "selectAfter">If True, the Objective will be considered 'selected' upon being updated</param>
		 */
		public void SetObjectiveState (int objectiveID, int newStateID, bool selectAfter = false)
		{
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					objectiveInstance.CurrentStateID = newStateID;
					if (selectAfter)
					{
						SelectedObjective = objectiveInstance;
					}
					return;
				}
			}

			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					objectiveInstance.CurrentStateID = newStateID;
					if (selectAfter)
					{
						SelectedObjective = objectiveInstance;
					}
					return;
				}
			}

			ObjectiveInstance newObjectiveInstance = new ObjectiveInstance (objectiveID, newStateID);
			if (newObjectiveInstance.Objective != null)
			{
				if (newObjectiveInstance.Objective.perPlayer && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerObjectiveInstances.Add (newObjectiveInstance);
				}
				else
				{
					globalObjectiveInstances.Add (newObjectiveInstance);
				}
				if (selectAfter)
				{
					SelectedObjective = newObjectiveInstance;
				}
				KickStarter.eventManager.Call_OnObjectiveUpdate (newObjectiveInstance);
			}
			else
			{
				ACDebug.LogWarning ("Cannot set the state of objective " + objectiveID + " because that ID does not exist!");
			}
		}


		/** 
		 * <summary>Updates the state of an Objective</summary>
		 * <param name = "objectiveID">The ID of the Objective to update</param>
		 * <param name = "newStateType">The ObjectiveStateType of the Objective's new state. If not states of this type are found, no change will be made.  If multiple states of this type are found, the first will be set.</param>
		 * <param name = "selectAfter">If True, the Objective will be considered 'selected' upon being updated</param>
		 */
		public void SetObjectiveState (int objectiveID, ObjectiveStateType newStateType, bool selectAfter = false)
		{
			int newStateID = -1;
			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective == null)
			{
				return;
			}

			foreach (ObjectiveState state in objective.states)
			{
				if (state.stateType == newStateType)
				{
					newStateID = state.ID;
				}
			}

			if (newStateID < 0)
			{
				return;
			}

			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					objectiveInstance.CurrentStateID = newStateID;
					if (selectAfter)
					{
						SelectedObjective = objectiveInstance;
					}
					return;
				}
			}

			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					objectiveInstance.CurrentStateID = newStateID;
					if (selectAfter)
					{
						SelectedObjective = objectiveInstance;
					}
					return;
				}
			}

			ObjectiveInstance newObjectiveInstance = new ObjectiveInstance (objectiveID, newStateID);
			if (newObjectiveInstance.Objective != null)
			{
				if (newObjectiveInstance.Objective.perPlayer && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerObjectiveInstances.Add (newObjectiveInstance);
				}
				else
				{
					globalObjectiveInstances.Add (newObjectiveInstance);
				}
				if (selectAfter)
				{
					SelectedObjective = newObjectiveInstance;
				}
				KickStarter.eventManager.Call_OnObjectiveUpdate (newObjectiveInstance);
			}
			else
			{
				ACDebug.LogWarning ("Cannot set the state of objective " + objectiveID + " because that ID does not exist!");
			}
		}


		/** 
		 * <summary>Updates the state of an Objective</summary>
		 * <param name = "objectiveID">The ID of the Objective to update</param>
		 * <param name = "newStateID">The ID of the Objective's new state</param>
		 * <param name = "playerID">The ID of the Player whose Objectives to update, if the Objective is set per-Player</param>
		 */
		public void SetObjectiveState (int objectiveID, int newStateID, int playerID)
		{
			if (!KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID))
			{
				SetObjectiveState (objectiveID, newStateID);
				return;
			}

			if (playerID < 0 || (KickStarter.player && KickStarter.player.ID == playerID))
			{
				SetObjectiveState (objectiveID, newStateID);
				return;
			}

			// Inactive player
			PlayerData playerData = KickStarter.saveSystem.GetPlayerData (playerID);
			if (playerData != null)
			{
				ObjectiveInstance[] extractedObjectives = ExtractPlayerObjectiveData (playerData).ToArray ();
				foreach (ObjectiveInstance objectiveInstance in extractedObjectives)
				{
					if (objectiveInstance.Objective.ID == objectiveID)
					{
						objectiveInstance.CurrentStateID = newStateID;
						string dataString = RecordPlayerObjectiveData (extractedObjectives);
						KickStarter.saveSystem.AssignObjectivesToPlayer (dataString, playerID);
						return;
					}
				}
			}

			// No data found
			List<ObjectiveInstance> tempObjectives = new List<ObjectiveInstance>();
			ObjectiveInstance newObjectiveInstance = new ObjectiveInstance (objectiveID, newStateID);
			if (newObjectiveInstance.Objective != null)
			{
				tempObjectives.Add (newObjectiveInstance);
				string dataString = RecordPlayerObjectiveData (tempObjectives.ToArray ());
				KickStarter.saveSystem.AssignObjectivesToPlayer (dataString, playerID);
			}
		}


		/**
		 * <summary>Gets the state of an active Objective.<summary>
		 * <param name = "objectiveID">The ID of the Objective</param>
		 * <returns>The Objective's state, if active. If inactive, null is returned</returns>
		 */
		public ObjectiveState GetObjectiveState (int objectiveID)
		{
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					return objectiveInstance.CurrentState;
				}
			}
			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					return objectiveInstance.CurrentState;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the state of an active Objective, held by a specific Player.<summary>
		 * <param name = "objectiveID">The ID of the Objective</param>
		 * <param name = "playerID">The ID of the Player</param>
		 * <returns>The Objective's state, if active. If inactive, or no player data is found, null is returned</returns>
		 */
		public ObjectiveState GetObjectiveState (int objectiveID, int playerID)
		{
			if (!KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID))
			{
				return GetObjectiveState (objectiveID);
			}

			if (playerID < 0 || (KickStarter.player && KickStarter.player.ID == playerID))
			{
				return GetObjectiveState (objectiveID);
			}

			// Inactive player
			PlayerData playerData = KickStarter.saveSystem.GetPlayerData (playerID);
			if (playerData != null)
			{
				ObjectiveInstance[] extractedObjectives = ExtractPlayerObjectiveData (playerData).ToArray ();
				foreach (ObjectiveInstance objectiveInstance in extractedObjectives)
				{
					if (objectiveInstance.Objective.ID == objectiveID)
					{
						return objectiveInstance.CurrentState;
					}
				}
				return null;
			}

			// No data found
			return null;
		}


		/**
		 * <summary>Marks an Objective as inactive</summary>
		 * <param name = "objectiveID">The ID of the Objective</param>
		 */
		public void CancelObjective (int objectiveID)
		{
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					playerObjectiveInstances.Remove (objectiveInstance);
					return;
				}
			}

			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					globalObjectiveInstances.Remove (objectiveInstance);
					return;
				}
			}
		}


		/**
		 * <summary>Gets an instance of an active Objective</summary>
		 * <param name = "objectiveID">The ID of the Objective to search for</param>
		 * <returns>The ObjectiveInstance if active, or null if not</returns>
		 */
		public ObjectiveInstance GetObjective (int objectiveID)
		{
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					return objectiveInstance;
				}
			}
			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.Objective.ID == objectiveID)
				{
					return objectiveInstance;
				}
			}
			return null;
		}


		/**
		 * <summary>Gets all active Objective instances</summary>
		 * <returns>All active Objective instances</returns>
		 */
		public ObjectiveInstance[] GetObjectives ()
		{
			List<ObjectiveInstance> completedObjectives = new List <ObjectiveInstance>();
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				completedObjectives.Add (objectiveInstance);
			}
			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				completedObjectives.Add (objectiveInstance);
			}
			return completedObjectives.ToArray ();
		}


		/**
		 * <summary>Gets all active Objective instances currently set to a particular type of state</summary>
		 * <param name = "objectiveStateType">The type of state to search for</param>
		 * <returns>All active Objective instances set to the type of state</returns>
		 */
		public ObjectiveInstance[] GetObjectives (ObjectiveStateType objectiveStateType)
		{
			List<ObjectiveInstance> completedObjectives = new List <ObjectiveInstance>();
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.CurrentState.stateType == objectiveStateType)
				{
					completedObjectives.Add (objectiveInstance);
				}
			}
			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.CurrentState.stateType == objectiveStateType)
				{
					completedObjectives.Add (objectiveInstance);
				}
			}
			return completedObjectives.ToArray ();
		}


		/**
		 * <summary>Gets all active Objective instances currently set to a particular display type of state</summary>
		 * <param name = "objectiveDisplayType">The type of display state to search for</param>
		 * <returns>All active Objective instances set to the type of display state</returns>
		 */
		public ObjectiveInstance[] GetObjectives (ObjectiveDisplayType objectiveDisplayType)
		{
			List<ObjectiveInstance> completedObjectives = new List <ObjectiveInstance>();
			foreach (ObjectiveInstance objectiveInstance in playerObjectiveInstances)
			{
				if (objectiveInstance.CurrentState.DisplayTypeMatches (objectiveDisplayType))
				{
					completedObjectives.Add (objectiveInstance);
				}
			}
			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				if (objectiveInstance.CurrentState.DisplayTypeMatches (objectiveDisplayType))
				{
					completedObjectives.Add (objectiveInstance);
				}
			}
			return completedObjectives.ToArray ();
		}


		/**
		 * <summary>Selects an Objective, so that it can be displayed in a Menu</summary>
		 * <param name = "objectiveID">The ID of the Objective to select</param>
		 */
		public void SelectObjective (int objectiveID)
		{
			SelectedObjective = GetObjective (objectiveID);
		}


		/** De-selects the selected Objective */
		public void DeselectObjective ()
		{
			SelectedObjective = null;
		}


		/** Clears all Objective data */
		public void ClearAll ()
		{
			ClearUniqueToPlayer ();
			globalObjectiveInstances.Clear ();
		}


		/** Clears all Objective data that's per-player, and not global */
		public void ClearUniqueToPlayer ()
		{
			playerObjectiveInstances.Clear ();
		}


		/**
		 * <summary>Updates a PlayerData class with Objectives that are unique to the current Player.</summary>
		 * <param name = "playerData">The original PlayerData class</param>
		 * <returns>The updated PlayerData class</returns>
		 */
		public PlayerData SavePlayerObjectives (PlayerData playerData)
		{
			playerData.playerObjectivesData = RecordPlayerObjectiveData (playerObjectiveInstances.ToArray ());
			return playerData;
		}


		/**
		 * <summary>Restores saved data from a PlayerData class</summary>
		 * <param name = "playerData">The PlayerData class to load from</param>
		 */
		public void AssignPlayerObjectives (PlayerData playerData)
		{
			playerObjectiveInstances.Clear ();
			SelectedObjective = null;

			playerObjectiveInstances = ExtractPlayerObjectiveData (playerData);
		}


		/**
		 * <summary>Updates a MainData class with Objectives that are shared by all Players.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveGlobalObjectives (MainData mainData)
		{
			System.Text.StringBuilder globalObjectivesData = new System.Text.StringBuilder ();
			foreach (ObjectiveInstance objectiveInstance in globalObjectiveInstances)
			{
				globalObjectivesData.Append (objectiveInstance.SaveData);
				globalObjectivesData.Append (SaveSystem.pipe);
			}
			if (globalObjectiveInstances.Count > 0)
			{
				globalObjectivesData.Remove (globalObjectivesData.Length-1, 1);
			}
			mainData.globalObjectivesData = globalObjectivesData.ToString ();

			return mainData;
		}


		/**
		 * <summary>Restores saved data from a MainData class</summary>
		 * <param name = "playerData">The MainData class to load from</param>
		 */
		public void AssignGlobalObjectives (MainData mainData)
		{
			globalObjectiveInstances.Clear ();
			SelectedObjective = null;

			if (!string.IsNullOrEmpty (mainData.globalObjectivesData))
			{
				string[] globalObjectivesArray = mainData.globalObjectivesData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in globalObjectivesArray)
				{
					ObjectiveInstance objectiveInstance = new ObjectiveInstance (chunk);
					if (objectiveInstance.Objective != null)
					{
						globalObjectiveInstances.Add (objectiveInstance);
					}
				}
			}
		}

		#endregion


		#region PrivateFunctions

		private List<ObjectiveInstance> ExtractPlayerObjectiveData (PlayerData playerData)
		{
			List<ObjectiveInstance> objectiveInstances = new List<ObjectiveInstance>();

			if (!string.IsNullOrEmpty (playerData.playerObjectivesData))
			{
				string[] playerObjectivesArray = playerData.playerObjectivesData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in playerObjectivesArray)
				{
					ObjectiveInstance objectiveInstance = new ObjectiveInstance (chunk);
					if (objectiveInstance.Objective != null)
					{
						objectiveInstances.Add (objectiveInstance);
					}
				}
			}

			return objectiveInstances;
		}


		private string RecordPlayerObjectiveData (ObjectiveInstance[] objectivesInstances)
		{
			System.Text.StringBuilder dataString = new System.Text.StringBuilder ();
			foreach (ObjectiveInstance objectiveInstance in objectivesInstances)
			{
				dataString.Append (objectiveInstance.SaveData);
				dataString.Append (SaveSystem.pipe);
			}
			if (objectivesInstances.Length > 0)
			{
				dataString.Remove (dataString.Length-1, 1);
			}

			return dataString.ToString ();
		}

		#endregion


		#region GetSet

		/** The instance of the currently-selected Objective */
		public ObjectiveInstance SelectedObjective
		{
			get
			{
				return selectedObjectiveInstance;
			}
			set
			{
				selectedObjectiveInstance = value;

				if (selectedObjectiveInstance != null)
				{
					KickStarter.eventManager.Call_OnObjectiveSelect (selectedObjectiveInstance);
				}
			}
		}

		#endregion

	}

}