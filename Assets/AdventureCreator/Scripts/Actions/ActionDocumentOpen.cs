/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionDocumentOpen.cs"
 * 
 *	This action makes a Document active for display in a Menu.
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
	public class ActionDocumentOpen : Action, IDocumentReferencerAction
	{

		public int documentID;
		public int parameterID = -1;
		public bool addToCollection = false;

		protected Document runtimeDocument;

		
		public override ActionCategory Category { get { return ActionCategory.Document; }}
		public override string Title { get { return "Open"; }}
		public override string Description { get { return "Opens a document, causing any Menu of 'Appear type: On View Document' to open."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			int runtimeDocumentID = AssignDocumentID (parameters, parameterID, documentID);
			runtimeDocument = KickStarter.inventoryManager.GetDocument (runtimeDocumentID);
		}


		public override float Run ()
		{
			if (runtimeDocument == null)
			{
				return 0f;
			}

			if (!isRunning)
			{
				if (addToCollection)
				{
					KickStarter.runtimeDocuments.AddToCollection (runtimeDocument);
				}
				KickStarter.runtimeDocuments.OpenDocument (runtimeDocument);

				if (willWait)
				{
					isRunning = true;
					return defaultPauseTime;
				}
			}
			else
			{
				if (KickStarter.runtimeDocuments.ActiveDocument == runtimeDocument)
				{
					return defaultPauseTime;
				}
			}

			isRunning = false;
			return 0f;
		}


		public override void Skip ()
		{
			if (runtimeDocument == null)
			{
				return;
			}

			if (addToCollection)
			{
				KickStarter.runtimeDocuments.AddToCollection (runtimeDocument);
			}
			if (willWait)
			{
				if (KickStarter.runtimeDocuments.ActiveDocument == runtimeDocument)
				{
					KickStarter.runtimeDocuments.CloseDocument ();
				}
			}
			else
			{
				KickStarter.runtimeDocuments.OpenDocument (runtimeDocument);
			}
		}
		

		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Document:", parameters, parameterID, ParameterType.Document);
			if (parameterID < 0)
			{
				documentID = InventoryManager.DocumentSelectorList (documentID);
			}
			addToCollection = EditorGUILayout.Toggle ("Add to collection?", addToCollection);
			willWait = EditorGUILayout.Toggle ("Wait until close?", willWait);
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
		 * <summary>Creates a new instance of the 'Document: Open' Action</summary>
		 * <param name = "documentID">The ID number of the document to open</param>
		 * <param name = "addToCollection">If True, the document will be added to the player's collection if not there already</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionDocumentOpen CreateNew (int documentID, bool addToCollection)
		{
			ActionDocumentOpen newAction = CreateNew<ActionDocumentOpen> ();
			newAction.documentID = documentID;
			newAction.addToCollection = addToCollection;
			return newAction;
		}
		
	}

}