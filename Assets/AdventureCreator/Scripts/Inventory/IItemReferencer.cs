using System.Collections.Generic;

namespace AC
{

	public interface IItemReferencer
	{

		#if UNITY_EDITOR

		int GetNumItemReferences (int itemID);
		int UpdateItemReferences (int oldItemID, int newItemID);

		#endif

	}


	public interface IItemReferencerAction
	{

		#if UNITY_EDITOR

		int GetNumItemReferences (int itemID, List<ActionParameter> actionParameters);
		int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> actionParameters);

		#endif

	}

}