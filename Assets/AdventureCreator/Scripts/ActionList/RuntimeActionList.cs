/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RuntimeActionList.cs"
 * 
 *	This is a special derivative of ActionList.
 *	It is used to run ActionList assets, which are assets defined outside of the scene.
 *	This type of asset's actions are copied here and run locally.
 *	When a ActionList asset is copied is copied from a menu, the menu it is called from is recorded, so that the game returns
 *	to the appropriate state after running.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * An ActionList subclass used to run ActionListAssets, which exist in asset files outside of the scene.
	 * When an ActionListAsset is run, its Actions are copied to a new RuntimeActionList and run locally.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_action_list.html")]
	public class RuntimeActionList : ActionList
	{

		#region Variables

		/** The ActionListAsset that this ActionList's Actions are copied from */
		public ActionListAsset assetSource;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			EventManager.OnBeforeChangeScene += OnBeforeChangeScene;
			EventManager.OnAfterChangeScene += OnAfterChangeScene;
		}


		protected void OnDisable ()
		{
			EventManager.OnBeforeChangeScene -= OnBeforeChangeScene;
			EventManager.OnAfterChangeScene -= OnAfterChangeScene;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Downloads and runs the settings and Actions stored within an ActionListAsset.</summary>
		 * <param name = "actionListAsset">The ActionListAsset to copy Actions from and run</param>
		 * <param name = "endConversation">If set, the supplied Conversation will be run when the AcionList ends</param>
		 * <param name = "i">The index number of the first Action to run</param>
		 * <param name = "doSkip">If True, then the Actions will be skipped, instead of run normally</param>
		 * <param name = "addToSkipQueue">If True, the ActionList will be skippable when the user presses 'EndCutscene'</param>
		 * <param name = "dontRun">If True, the Actions will not be run once transferred from the ActionListAsset</param>
		 */
		public void DownloadActions (ActionListAsset actionListAsset, Conversation endConversation, int i, bool doSkip, bool addToSkipQueue, bool dontRun = false)
		{
			assetSource = actionListAsset;
			useParameters = actionListAsset.useParameters;

			parameters = new List<ActionParameter>();

			List<ActionParameter> assetParameters = actionListAsset.GetParameters ();
			if (assetParameters != null)
			{
				foreach (ActionParameter assetParameter in assetParameters)
				{
					parameters.Add (new ActionParameter (assetParameter, true));
				}
			}

			unfreezePauseMenus = actionListAsset.unfreezePauseMenus;

			actionListType = actionListAsset.actionListType;
			if (actionListAsset.actionListType == ActionListType.PauseGameplay)
			{
				isSkippable = actionListAsset.isSkippable;
			}
			else
			{
				isSkippable = false;
			}

			conversation = endConversation;
			actions.Clear ();
			
			foreach (Action action in actionListAsset.actions)
			{
				if (action != null)
				{
					int lastResultIndex = action.LastRunOutput;

					#if AC_ActionListPrefabs
					Action newAction = JsonAction.CreateCopy (action);
					#else
					// Really we should re-instantiate all Actions, but this is 'safer'
					Action newAction = (actionListAsset.canRunMultipleInstances)
										? Instantiate (action)
										: action;
					#endif

					if (doSkip)
					{
						newAction.SetLastResult (lastResultIndex);
					}

					actions.Add (newAction);
				}
				else
				{
					actions.Add (null);
				}
			}

			actionListAsset.AfterDownloading ();

			if (!dontRun)
			{
				if (doSkip)
				{
					Skip (i);
				}
				else
				{
					Interact (i, addToSkipQueue);
				}
			}

			if (actionListAsset.canSurviveSceneChanges && !actionListAsset.IsSkippable ())
			{
				DontDestroyOnLoad (gameObject);
			}
		}


		/**
		 * Stops the Actions from running and sets the gameState in StateHandler to the correct value.
		 */
		public override void Kill ()
		{
			StopAllCoroutines ();

			KickStarter.actionListAssetManager.EndAssetList (this);
			KickStarter.eventManager.Call_OnEndActionList (this, assetSource, isSkipping);
		}


		/**
		 * Destroys itself.
		 */
		public void DestroySelf ()
		{
			Destroy (this.gameObject);
		}

		#endregion


		#region ProtectedFunctions

		protected override void BeginActionList (int i, bool addToSkipQueue)
		{
			if (KickStarter.actionListAssetManager != null)
			{
				KickStarter.actionListAssetManager.AddToList (this, assetSource, addToSkipQueue, i, isSkipping);
				KickStarter.eventManager.Call_OnBeginActionList (this, assetSource, i, isSkipping);

				if (KickStarter.actionListManager.IsListRegistered (this))
				{
					ProcessAction (i);
				}
			}
			else
			{
				ACDebug.LogWarning ("Cannot run " + this.name + " because no ActionListManager was found.", this);
			}
		}


		protected override void AddResumeToManager (int startIndex)
		{
			if (KickStarter.actionListAssetManager == null)
			{
				ACDebug.LogWarning ("Cannot run " + this.name + " because no ActionListAssetManager was found.", this);
				return;
			}
			// If resuming, ActiveList should already be present - no need to add to the list
			// KickStarter.actionListAssetManager.AddToList (this, assetSource, true, startIndex);
		}


		protected override void ReturnLastResultToSource (int index, int i)
		{
			assetSource.actions[i].SetLastResult (index);
		}


		protected override void FinishPause ()
		{
			KickStarter.actionListAssetManager.AssignResumeIndices (assetSource, resumeIndices.ToArray ());
			CheckEndCutscene ();
		}


		protected override void PrintActionComment (Action action)
		{
			switch (KickStarter.settingsManager.actionCommentLogging)
			{
				case ActionCommentLogging.Always:
					action.PrintComment (this, assetSource);
					break;

				case ActionCommentLogging.OnlyIfVisible:
					if (action.showComment)
					{
						action.PrintComment (this, assetSource);
					}
					break;

				default:
					break;
			}
		}


		protected void OnBeforeChangeScene (string nextSceneName)
		{
			if (assetSource.canSurviveSceneChanges && !assetSource.IsSkippable ())
			{
				isChangingScene = true;
			}
			else
			{
				Kill ();
			}
		}


		protected void OnAfterChangeScene (LoadingGame loadingGame)
		{
			isChangingScene = false;
		}

		#endregion

	}

}
