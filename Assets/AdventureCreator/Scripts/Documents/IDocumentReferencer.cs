using System.Collections.Generic;

namespace AC
{

	public interface IDocumentReferencer
	{

		#if UNITY_EDITOR

		int GetNumDocumentReferences (int documentID);
		int UpdateDocumentReferences (int oldDocumentID, int newDocumentID);

		#endif

	}


	public interface IDocumentReferencerAction
	{

		#if UNITY_EDITOR

		int GetNumDocumentReferences (int documentID, List<ActionParameter> actionParameters);
		int UpdateDocumentReferences (int oldDocumentID, int newDocumentID, List<ActionParameter> actionParameters);

		#endif

	}

}