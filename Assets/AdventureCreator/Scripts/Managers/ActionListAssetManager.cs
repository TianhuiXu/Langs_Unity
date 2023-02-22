/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionListAssetManager.cs"
 * 
 *	This script keeps track of which ActionListAssets are running.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component keeps track of which ActionListAssets are running.
	 * It should be placed on the PersistentEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_asset_manager.html")]
	public class ActionListAssetManager : MonoBehaviour
	{

		#region Variables

		protected List<ActiveList> activeLists = new List<ActiveList>();

		#endregion

		
		#region PublicFunctions

		/**
		 * <summary>Checks if a particular ActionListAsset file is running.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to search for</param>
		 * <returns>True if the ActionListAsset file is currently running</returns>
		 */
		public bool IsListRunning (ActionListAsset actionListAsset)
		{
			if (actionListAsset == null) return false;

			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionListAsset))
				{
					if (activeList.IsRunning ())
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Adds a new ActionListAsset, assumed to already be running, to the internal record of currently-running ActionListAssets, and sets the correct GameState in StateHandler.</summary>
		 * <param name = "runtimeActionList">The RuntimeActionList associated with the ActionListAsset to run</param>
		 * <param name = "actionListAsset">The ActionListAsset that is the runtimeActionList's source, if it has one.</param>
		 * <param name = "addToSkipQueue">If True, then the ActionList will be added to the list of ActionLists to skip</param>
		 * <param name = "_startIndex">The index number of the Action to start skipping from, if addToSkipQueue = True</param>
		 */
		public void AddToList (RuntimeActionList runtimeActionList, ActionListAsset actionListAsset, bool addToSkipQueue, int _startIndex, bool removeMultipleInstances = false)
		{
			if (!actionListAsset.canRunMultipleInstances || removeMultipleInstances)
			{
				for (int i=0; i<activeLists.Count; i++)
				{
					if (activeLists[i].IsFor (actionListAsset))
					{
						activeLists[i].Reset (false);
						activeLists.RemoveAt (i);
					}
				}
			}

			addToSkipQueue = KickStarter.actionListManager.CanAddToSkipQueue (runtimeActionList, addToSkipQueue);
			activeLists.Add (new ActiveList (runtimeActionList, addToSkipQueue, _startIndex));
		}
		
		
		/**
		 * <summary>Destroys the RuntimeActionList scene object that is running Actions from an ActionListAsset.</summary>
		 * <param name = "asset">The asset file that the RuntimeActionList has sourced its Actions from</param>
		 */
		public void DestroyAssetList (ActionListAsset asset)
		{
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (asset))
				{
					activeList.Reset (true);
				}
			}
		}
		
		
		/**
		 * <summary>Stops an ActionListAsset from running.</summary>
		 * <param name = "asset">The ActionListAsset file to stop></param>
		 * <param name = "_action">An Action that, if present within 'asset', will prevent the ActionListAsset from ending prematurely</param>
		 * <param name = "forceEndAll">If True, then all will be stopped - even if the ActionListAsset's canRunMultipleInstances is True</param>
		 * <returns>The number of instances of the ActionListAsset that were stopped</returns>
		 */
		public int EndAssetList (ActionListAsset asset, Action _action = null, bool forceEndAll = false)
		{
			int numRemoved = 0;

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (asset))
				{
					if (_action == null || !activeLists[i].actionList.actions.Contains (_action))
					{
						activeLists[i].ClearNecessity ();
						int originalCount = activeLists.Count;
						KickStarter.actionListManager.EndList (activeLists[i]);

						if (originalCount == activeLists.Count)
						{
							ACDebug.LogWarning ("Ended asset " + asset.name + ", but ActiveList data is retained.", asset);
						}
						else
						{
							numRemoved ++;
							i=-1;

							if (asset.canRunMultipleInstances && !forceEndAll)
							{
								return numRemoved;
							}
						}
					}
				}
			}

			return numRemoved;
		}


		/**
		 * <summary>Stops an ActionListAsset from running.</summary>
		 * <param name = "runtimeActionList">The RuntimeActionList associated with the ActionListAsset file to stop></param>
		 */
		public void EndAssetList (RuntimeActionList runtimeActionList)
		{
			if (runtimeActionList != null)
			{
				for (int i=0; i<activeLists.Count; i++)
				{
					if (activeLists[i].IsFor (runtimeActionList))
					{
						KickStarter.actionListManager.EndList (activeLists[i]);
						return;
					}
				}
			}
		}


		/**
		 * <summary>Records the Action indices that the associated ActionListAsset was running before being paused. This data is sent to the ActionListAsset's associated ActiveList</summary>
		 * <param name = "actionListAsset">The ActionListAsset that is being paused</param>
		 * <param name = "resumeIndices">An array of Action indices to run when the ActionListAsset is resumed</param>
		 */
		public void AssignResumeIndices (ActionListAsset actionListAsset, int[] resumeIndices)
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					activeLists[i].SetResumeIndices (resumeIndices);
				}
			}
		}
		

		/**
		 * <summary>Pauses an ActionListAsset, provided that it is currently running.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to pause</param>
		 * <returns>All RuntimeActionLists that are in the scene, associated with the ActionListAsset</returns>
		 */
		public RuntimeActionList[] Pause (ActionListAsset actionListAsset)
		{
			List<RuntimeActionList> runtimeActionLists = new List<RuntimeActionList>();

			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					RuntimeActionList runtimeActionList = (RuntimeActionList) activeLists[i].actionList;
					runtimeActionList.Pause ();
					runtimeActionLists.Add (runtimeActionList);
					activeLists[i].UpdateParameterData ();
				}
			}
			return runtimeActionLists.ToArray ();
		}
		

		/**
		 * <summary>Resumes a previously-paused ActionListAsset. If the ActionListAsset is already running, nothing will happen.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to pause</param>
		 * <param name = "rerunPausedActions">If True, then any Actions that were midway-through running when the ActionList was paused will be restarted. Otherwise, the Actions that follow them will be reun instead.</param>
		 */
		public void Resume (ActionListAsset actionListAsset, bool rerunPausedActions)
		{
			if (IsListRunning (actionListAsset) && !actionListAsset.canRunMultipleInstances)
			{
				return;
			}

			bool foundInstance = false;
			for (int i=0; i<activeLists.Count; i++)
			{
				if (activeLists[i].IsFor (actionListAsset))
				{
					int numInstances = 0;
					foreach (ActiveList activeList in activeLists)
					{
						if (activeList.IsFor (actionListAsset) && activeList.IsRunning ())
						{
							numInstances ++;
						}
					}

					GameObject runtimeActionListObject = new GameObject ();
					runtimeActionListObject.name = actionListAsset.name;
					if (numInstances > 0) runtimeActionListObject.name += " " + numInstances.ToString ();

					RuntimeActionList runtimeActionList = runtimeActionListObject.AddComponent <RuntimeActionList>();
					runtimeActionList.DownloadActions (actionListAsset, activeLists[i].GetConversationOnEnd (), activeLists[i].startIndex, false, activeLists[i].inSkipQueue, true);
					activeLists[i].Resume (runtimeActionList, rerunPausedActions);
					foundInstance = true;
					if (!actionListAsset.canRunMultipleInstances)
					{
						return;
					}
				}
			}

			if (!foundInstance)
			{
				ACDebug.LogWarning ("No resume data found for '" + actionListAsset + "' - running from start.", actionListAsset);
				AdvGame.RunActionListAsset (actionListAsset);
			}
		}

		
		/**
		 * <summary>Generates a save-able string out of the ActionList resume data.<summary>
		 * <returns>A save-able string out of the ActionList resume data<returns>
		 */
		public string GetSaveData ()
		{
			PurgeLists ();
			string assetResumeData = string.Empty;
			for (int i=0; i<activeLists.Count; i++)
			{
				string thisResumeData = activeLists[i].GetSaveData (null);
				if (!string.IsNullOrEmpty (thisResumeData))
				{
					assetResumeData += thisResumeData;

					if (i < (activeLists.Count - 1))
					{
						assetResumeData += SaveSystem.pipe;
					}
				}
			}
			return assetResumeData;
		}
		
		
		/**
		 * <summary>Recreates ActionList resume data from a saved data string.</summary>
		 * <param name = "_dataString">The saved data string</param>
		 */
		public void LoadData (string _dataString)
		{
			activeLists.Clear ();

			if (!string.IsNullOrEmpty (_dataString))
			{
				string[] dataArray = _dataString.Split (SaveSystem.pipe[0]);
				foreach (string chunk in dataArray)
				{
					ActiveList activeList = new ActiveList ();
					activeList.LoadData (chunk);
				}
			}
		}


		public void AddToList (ActiveList activeList)
		{
			activeLists.Add (activeList);
		}


		public void DrawStatus ()
		{
			bool anyRunning = false;
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsRunning ())
				{
					anyRunning = true;
					break;
				}
			}

			if (anyRunning)
			{
				GUILayout.Label ("ActionList Assets running:");

				foreach (ActiveList activeList in activeLists)
				{
					activeList.ShowGUI ();
				}
			}
		}


		public int GetNumInstances (ActionListAsset actionListAsset)
		{
			int numInstances = 0;
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (actionListAsset) && activeList.IsRunning ())
				{
					numInstances++;
				}
			}
			return numInstances;
		}

		#endregion


		#region ProtectedFunctions

		protected void PurgeLists ()
		{
			for (int i=0; i<activeLists.Count; i++)
			{
				if (!activeLists[i].IsNecessary ())
				{
					activeLists.RemoveAt (i);
					i--;
				}
			}
		}

		#endregion


		#region GetSet

		/** Data about any ActionListAsset that has been run and we need to store information about */
		public List<ActiveList> ActiveLists
		{
			get
			{
				if (activeLists == null) activeLists = new List<ActiveList> ();
				return activeLists;
			}
		}

		#endregion

	}

}