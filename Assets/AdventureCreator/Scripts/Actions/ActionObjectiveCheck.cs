#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionObjectiveCheck : Action, IObjectiveReferencerAction
	{

		public int objectiveID;
		public int playerID;
		public bool setPlayer;

		public int numSockets = 2;

		public override ActionCategory Category { get { return ActionCategory.Objective; }}
		public override string Title { get { return "Check state"; }}
		public override string Description { get { return "Queries the current state of an objective."; }}
		public override int NumSockets { get { return numSockets; }}


		public override int GetNextOutputIndex ()
		{
			if (numSockets < 1)
			{
				return -1;
			}

			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				int _playerID = (setPlayer && KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID)) ? playerID : -1;

				ObjectiveState currentObjectiveState = KickStarter.runtimeObjectives.GetObjectiveState (objectiveID, _playerID);
				if (currentObjectiveState != null)
				{
					int stateIndex = objective.states.IndexOf (currentObjectiveState);
					return stateIndex + 1;
				}
				else
				{
					return 0;
				}
			}
			return -1;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			if (KickStarter.inventoryManager == null)
			{
				numSockets = 0;
				EditorGUILayout.HelpBox ("An Inventory Manager must be defined to use this Action", MessageType.Warning);
				return;
			}

			objectiveID = InventoryManager.ObjectiveSelectorList (objectiveID);

			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				numSockets = objective.NumStates + 1;

				if (KickStarter.inventoryManager.ObjectiveIsPerPlayer (objectiveID))
				{
					setPlayer = EditorGUILayout.Toggle ("Check specific Player?", setPlayer);
					if (setPlayer)
					{
						playerID = ChoosePlayerGUI (playerID, false);
					}
				}
			}
			else
			{
				numSockets = 1;
			}
		}
		

		public override string SetLabel ()
		{
			Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
			if (objective != null)
			{
				return objective.Title;
			}			
			return string.Empty;
		}


		protected override string GetSocketLabel (int i)
		{
			if (i == 0)
			{
				return "If inactive:";
			}

			if (KickStarter.inventoryManager)
			{
				Objective objective = KickStarter.inventoryManager.GetObjective (objectiveID);
				if (objective != null)
				{
					string[] popUpLabels = objective.GenerateEditorStateLabels ();
					return "If = '" + popUpLabels[i - 1] + "':";
				}
				else
				{
					return "If ID = '" + objectiveID.ToString () + "':";
				}
			}
			return string.Empty;
		}


		public int GetNumObjectiveReferences (int _objectiveID)
		{
			return (objectiveID == _objectiveID) ? 1 : 0;
		}


		public int UpdateObjectiveReferences (int oldObjectiveID, int newObjectiveID)
		{
			if (objectiveID == oldObjectiveID)
			{
				objectiveID = newObjectiveID;
				return 1;
			}
			return 0;
		}

		#endif

	}

}