namespace AC
{

	public interface IObjectiveReferencer
	{

		#if UNITY_EDITOR

		int GetNumObjectiveReferences (int objectiveID);
		int UpdateObjectiveReferences (int oldObjectiveID, int newObjectiveID);

		#endif

	}


	public interface IObjectiveReferencerAction
	{

		#if UNITY_EDITOR

		int GetNumObjectiveReferences (int objectiveID);
		int UpdateObjectiveReferences (int oldObjectiveID, int newObjectiveID);

		#endif

	}

}