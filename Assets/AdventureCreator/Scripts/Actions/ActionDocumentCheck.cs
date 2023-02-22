/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionDocumentCheck.cs"
 * 
 *	This action checks to see if a Document is being carried
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
	public class ActionDocumentCheck : ActionCheck, IDocumentReferencerAction
	{

		public int documentID;
		public int parameterID = -1;

		
		public override ActionCategory Category { get { return ActionCategory.Document; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Checks to see if a particular Document is in the Player's possession."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			documentID = AssignDocumentID (parameters, parameterID, documentID);
		}


		public override bool CheckCondition ()
		{
			return KickStarter.runtimeDocuments.DocumentIsInCollection (documentID);
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Check carrying:", parameters, parameterID, ParameterType.Document);
			if (parameterID < 0)
			{
				documentID = InventoryManager.DocumentSelectorList (documentID, "Check carrying:");
			}
		}


		public override string SetLabel ()
		{
			Document document = KickStarter.inventoryManager.GetDocument (documentID);
			if (document != null)
			{
				return document.Title;
			}
			return string.Empty;
		}


		public int GetNumDocumentReferences (int _docID, List<ActionParameter> parameters)
		{
			if (parameterID < 0 && documentID == _docID)
			{
				return 1;
			}
			return 0;
		}


		public int UpdateDocumentReferences (int oldDocumentID, int newDocumentID, List<ActionParameter> actionParameters)
		{
			if (parameterID < 0 && documentID == oldDocumentID)
			{
				documentID = newDocumentID;
				return 1;
			}
			return 0;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Document: Check' Action</summary>
		 * <param name = "documentID">The ID number of the document to check if the player is carrying</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionDocumentCheck CreateNew (int documentID)
		{
			ActionDocumentCheck newAction = CreateNew<ActionDocumentCheck> ();
			newAction.documentID = documentID;
			return newAction;
		}
		
	}

}