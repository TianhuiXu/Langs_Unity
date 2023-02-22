/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RuntimeDocuments.cs"
 * 
 *	This script stores information about the currently-open Document, as well as any runtime-made changes to all Documents.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script stores information about the currently-open Document, as well as any runtime-made changes to all Documents.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_documents.html")]
	public class RuntimeDocuments : MonoBehaviour
	{

		#region Variables

		protected Document activeDocument;

		protected List<int> collectedDocuments = new List<int>(); 
		protected Dictionary<int, int> lastOpenPages = new Dictionary<int, int>();

		#endregion


		#region PublicFunctions

		/**
		 * This is called when the game begins, and sets up the initial state.
		 */
		public void OnInitPersistentEngine ()
		{
			activeDocument = null;
			collectedDocuments.Clear ();
			lastOpenPages.Clear ();

			GetDocumentsOnStart ();
		}


		/**
		 * <summary>Opens a Document.  To view it, a Menu with an Appear Type of OnViewDocument must be present in the Menu Manager.<summary>
		 * <param name = "document">The Document to open</param>
		 */
		public void OpenDocument (Document document)
		{
			if (document != null && activeDocument != document)
			{
				CloseDocument ();

				activeDocument = document;
				KickStarter.eventManager.Call_OnHandleDocument (activeDocument, true);
			}
		}


		/**
		 * <summary>Opens a Document.  To view it, a Menu with an Appear Type of OnViewDocument must be present in the Menu Manager.<summary>
		 * <param name = "documentID">The ID number of the Document to open</param>
		 */
		public void OpenDocument (int documentID)
		{
			if (documentID >= 0)
			{
				Document document = KickStarter.inventoryManager.GetDocument (documentID);
				OpenDocument (document);
			}
		}


		/**
		 * <summary>Closes the currently-viewed Document, if there is one</summary>
		 */
		public void CloseDocument ()
		{
			if (activeDocument != null)
			{
				KickStarter.eventManager.Call_OnHandleDocument (activeDocument, false);
				activeDocument = null;
			}
		}


		/**
		 * <summary>Checks if a particular Document is in the Player's collection</summary>
		 * <param name = "ID">The ID number of the Document to check for</param>
		 * <returns>True if the Document is in the Player's collection</returns>
		 */
		public bool DocumentIsInCollection (int ID)
		{
			if (collectedDocuments != null)
			{
				foreach (int documentID in collectedDocuments)
				{
					if (documentID == ID)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Adds a Document to the Player's own collection</summary>
		 * <param name = "document">The Document to add</param>
		 */
		public void AddToCollection (Document document)
		{
			if (!collectedDocuments.Contains (document.ID))
			{
				collectedDocuments.Add (document.ID);
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Removes a Document from the Player's own collection</summary>
		 * <param name = "document">The Document to remove</param>
		 */
		public void RemoveFromCollection (Document document)
		{
			if (collectedDocuments.Contains (document.ID))
			{
				collectedDocuments.Remove (document.ID);
				PlayerMenus.ResetInventoryBoxes ();
			}
		}


		/**
		 * <summary>Removes all Documents from the Player's own collection</summary>
		 */
		public void ClearCollection ()
		{
			collectedDocuments.Clear ();
			PlayerMenus.ResetInventoryBoxes ();
		}


		/**
		 * <summary>Gets the page number to return to when opening a previously-read Document</summary>
		 * <param name = "document">The Document in question</param>
		 * <returns>The page number to return to when opening a previously-read Document</returns>
		 */
		public int GetLastOpenPage (Document document)
		{
			if (document.rememberLastOpenPage)
			{
				int lastOpenPage = 0;
				if (lastOpenPages.TryGetValue (document.ID, out lastOpenPage))
				{
					return lastOpenPage;
				}
			}
			return 1;
		}


		/**
		 * <summary>Sets the page number to return to when a given Document is next opened</summary>
		 * <param name = "document">The Document in question</param>
		 * <param name = "page">The page number to return to next time</param>
		 */
		public void SetLastOpenPage (Document document, int page)
		{
			if (document.rememberLastOpenPage)
			{
				if (lastOpenPages.ContainsKey (document.ID))
				{
					lastOpenPages[document.ID] = page;
				}
				else
				{
					lastOpenPages.Add (document.ID, page);
				}
			}
		}


		/**
		 * <summary>Updates a PlayerData class with its own variables that need saving.</summary>
		 * <param name = "playerData">The original PlayerData class</param>
		 * <returns>The updated PlayerData class</returns>
		 */
		public PlayerData SavePlayerDocuments (PlayerData playerData)
		{
			playerData.activeDocumentID = (activeDocument != null) ? activeDocument.ID : -1;

			System.Text.StringBuilder collectedDocumentsData = new System.Text.StringBuilder ();
			foreach (int collectedDocument in collectedDocuments)
			{
				collectedDocumentsData.Append (collectedDocument.ToString ());
				collectedDocumentsData.Append (SaveSystem.pipe);
			}
			if (collectedDocuments.Count > 0)
			{
				collectedDocumentsData.Remove (collectedDocumentsData.Length-1, 1);
			}
			playerData.collectedDocumentData = collectedDocumentsData.ToString ();

			System.Text.StringBuilder lastOpenPagesData = new System.Text.StringBuilder ();
			foreach (KeyValuePair<int, int> lastOpenPage in lastOpenPages)
			{
				lastOpenPagesData.Append (lastOpenPage.Key.ToString ());
				lastOpenPagesData.Append (SaveSystem.colon);
				lastOpenPagesData.Append (lastOpenPage.Value.ToString ());
				lastOpenPagesData.Append (SaveSystem.pipe);
			}
			if (lastOpenPages.Count > 0)
			{
				lastOpenPagesData.Remove (lastOpenPagesData.Length-1, 1);
			}
			playerData.lastOpenDocumentPagesData = lastOpenPagesData.ToString ();
			
			return playerData;
		}


		/**
		 * <summary>Restores saved data from a PlayerData class</summary>
		 * <param name = "playerData">The PlayerData class to load from</param>
		 */
		public void AssignPlayerDocuments (PlayerData playerData)
		{
			collectedDocuments.Clear ();
			if (!string.IsNullOrEmpty (playerData.collectedDocumentData))
			{
				string[] collectedDocumentArray = playerData.collectedDocumentData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in collectedDocumentArray)
				{
					int _id = -1;
					if (int.TryParse (chunk, out _id))
					{
						if (_id >= 0)
						{
							collectedDocuments.Add (_id);
						}
					}
				}
			}

			lastOpenPages.Clear ();
			if (!string.IsNullOrEmpty (playerData.lastOpenDocumentPagesData))
			{
				string[] lastOpenPagesArray = playerData.lastOpenDocumentPagesData.Split (SaveSystem.pipe[0]);

				foreach (string chunk in lastOpenPagesArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					int documentID = -1;
					if (int.TryParse (chunkData[0], out documentID))
					{
						if (documentID >= 0)
						{
							int pageNumber = 1;
							if (int.TryParse (chunkData[1], out pageNumber))
							{
								if (pageNumber > 1)
								{
									lastOpenPages.Add (documentID, pageNumber);
								}
							}
						}
					}
				}
			}

			OpenDocument (playerData.activeDocumentID);
		}

		#endregion


		#region ProtectedFunctions

		protected void GetDocumentsOnStart ()
		{
			if (KickStarter.inventoryManager)
			{
				foreach (Document document in KickStarter.inventoryManager.documents)
				{
					if (document.carryOnStart)
					{
						collectedDocuments.Add (document.ID);
					}
				}
			}
			else
			{
				ACDebug.LogError ("No Inventory Manager found - please use the Adventure Creator window to create one.");
			}
		}

		#endregion


		#region GetSet

		/**
		 * The currently-active Document
		 */
		public Document ActiveDocument
		{
			get
			{
				return activeDocument;
			}
		}


		/**
		 * <summary>Gets an array of ID numbers that each represent a Document held by the Player</summary>
		 * <param name = "limitToCategoryIDs">If non-negative, ID numbers of inventory categories to limit results to</param>
		 * <returns>An array of ID numbers that each represent a Document held by the Player</returns>
		 */
		public int[] GetCollectedDocumentIDs (int[] limitToCategoryIDs = null)
		{
			if (limitToCategoryIDs != null && limitToCategoryIDs.Length >= 0)
			{
				List<int> limitedDocuments = new List<int>();
				foreach (int documentID in collectedDocuments)
				{
					if (documentID >= 0)
					{
						Document document = KickStarter.inventoryManager.GetDocument (documentID);
						bool canAdd = false;
						foreach (int limitToCategoryID in limitToCategoryIDs)
						{
							if (document.binID == limitToCategoryID)
							{
								canAdd = true;
							}
						}
						if (canAdd)
						{
							limitedDocuments.Add (documentID);
						}
					}
				}
				return limitedDocuments.ToArray ();
			}
			return collectedDocuments.ToArray ();
		}

		#endregion

	}

}