/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionListStarter.cs"
 * 
 *	A component used to run ActionLists when a scene begins or loads, optionally setting their parameters as well.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A component used to run ActionLists when a scene begins or loads, optionally setting their parameters as well. */
	[AddComponentMenu ("Adventure Creator/Logic/ActionList starter")]
	[HelpURL ("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_starter.html")]
	public class ActionListStarter : SetParametersBase, iActionListAssetReferencer
	{

		#region Variables

		[SerializeField] protected ActionListSource actionListSource = ActionListSource.InScene;
		[SerializeField] protected ActionList actionList = null;
		[SerializeField] protected ActionListAsset actionListAsset = null;
		[SerializeField] protected bool runOnStart = false;
		[SerializeField] protected bool runOnLoad = false;
		protected enum SceneLoadCondition { All, DueToLoadingSave, DueToSwitchingPlayer };
		[SerializeField] protected SceneLoadCondition sceneLoadCondition = SceneLoadCondition.All;
		[SerializeField] protected bool setParameters = false;
		[SerializeField] protected bool runMultipleTimes = false;
		[SerializeField] protected bool runInstantly = false;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			EventManager.OnStartScene += OnStartScene;
			EventManager.OnAfterChangeScene += OnAfterChangeScene;
			EventManager.OnAddSubScene += OnAddSubScene;
		}


		protected void OnDisable ()
		{
			EventManager.OnStartScene -= OnStartScene;
			EventManager.OnAfterChangeScene -= OnAfterChangeScene;
			EventManager.OnAddSubScene -= OnAddSubScene;
		}

		#endregion


		#region PublicFunctions

		public override List<ActionListAsset> GetReferencedActionListAssets ()
		{
			List<ActionListAsset> foundAssets = base.GetReferencedActionListAssets ();
			if (actionListSource == ActionListSource.AssetFile && actionListAsset != null)
			{
				foundAssets.Add (actionListAsset);
			}
			return foundAssets;
		}


		/** Runs the referenced ActionList or ActionListAsset */
		[ContextMenu ("Run now")]
		public void RunActionList ()
		{
			if (Application.isPlaying)
			{
				RunActionLists ();
			}
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			runOnStart = EditorGUILayout.Toggle ("Run on scene start?", runOnStart);
			runOnLoad = EditorGUILayout.Toggle ("Run on scene load?", runOnLoad);

			if (runOnLoad && KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				sceneLoadCondition = (SceneLoadCondition) EditorGUILayout.EnumPopup ("Scene load condition:", sceneLoadCondition);
			}
			
			EditorGUILayout.Space ();

			actionListSource = (ActionListSource)EditorGUILayout.EnumPopup ("ActionList source:", actionListSource);
			switch (actionListSource)
			{
				case ActionListSource.InScene:
					actionList = (ActionList)EditorGUILayout.ObjectField ("ActionList to run:", actionList, typeof (ActionList), true);

					if (actionList == null && GetComponent <ActionList>())
					{
						actionList = GetComponent <ActionList>();
					}

					if (actionList && actionList.IsSkippable ())
					{
						runInstantly = EditorGUILayout.Toggle ("Run instantly?", runInstantly);
					}
					EditorGUILayout.Space ();
					ShowParametersGUI (actionList);
					break;

				case ActionListSource.AssetFile:
					actionListAsset = (ActionListAsset)EditorGUILayout.ObjectField ("Asset to run:", actionListAsset, typeof (ActionListAsset), false);

					if (actionListAsset && actionListAsset.IsSkippable ())
					{
						runInstantly = EditorGUILayout.Toggle ("Run instantly?", runInstantly);
					}
					EditorGUILayout.Space ();
					ShowParametersGUI (actionListAsset);
					break;
			}

			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox ("The linked ActionList can also be run by invoking this script's RunActionList() method.", MessageType.Info);
		}


		private void ShowParametersGUI (ActionList _actionList)
		{
			if (_actionList != null)
			{
				if (_actionList.source == ActionListSource.AssetFile && _actionList.assetFile && _actionList.assetFile.NumParameters > 0)
				{
					setParameters = EditorGUILayout.Toggle ("Set parameters?", setParameters);

					if (setParameters)
					{
						ShowParametersGUI (_actionList.assetFile.DefaultParameters, _actionList.syncParamValues);
					}
				}
				else if (_actionList.source == ActionListSource.InScene && _actionList.NumParameters > 0)
				{
					setParameters = EditorGUILayout.Toggle ("Set parameters:", setParameters);

					if (setParameters)
					{
						ShowParametersGUI (_actionList.parameters, false);
					}
				}
			}
		}


		private void ShowParametersGUI (ActionListAsset _actionListAsset)
		{
			if (_actionListAsset != null)
			{
				if (_actionListAsset.NumParameters > 0)
				{
					setParameters = EditorGUILayout.Toggle ("Set parameters?", setParameters);

					if (setParameters && _actionListAsset.canRunMultipleInstances)
					{
						runMultipleTimes = EditorGUILayout.Toggle ("Run multiple times?", runMultipleTimes);
					}

					if (setParameters)
					{
						ShowParametersGUI (_actionListAsset.DefaultParameters, true, (_actionListAsset.canRunMultipleInstances && runMultipleTimes));
					}
				}
			}
		}

		#endif


		#region ProtectedFunctions

		protected void OnStartScene ()
		{
			if (runOnStart)
			{
				RunActionLists ();
			}
		}


		protected void OnAfterChangeScene (LoadingGame loadingGame)
		{
			if (runOnLoad && loadingGame != LoadingGame.No)
			{
				if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					switch (sceneLoadCondition)
					{
						case SceneLoadCondition.DueToLoadingSave:
							if (loadingGame == LoadingGame.JustSwitchingPlayer) return;
							break;

						case SceneLoadCondition.DueToSwitchingPlayer:
							if (loadingGame != LoadingGame.JustSwitchingPlayer) return;
							break;

						default:
							break;
					}
				}

				RunActionLists ();
			}
		}


		protected void OnAddSubScene (SubScene subScene)
		{
			if (subScene.gameObject.scene == gameObject.scene)
			{
				if (KickStarter.saveSystem.loadingGame == LoadingGame.No)
				{
					OnStartScene ();
				}
				else
				{
					OnAfterChangeScene (KickStarter.saveSystem.loadingGame);
				}
			}
		}


		protected void RunActionLists ()
		{
			switch (actionListSource)
			{
				case ActionListSource.InScene:
					if (actionList != null)
					{
						if (setParameters)
						{
							AssignParameterValues (actionList);
						}

						if (actionList.IsSkippable () && runInstantly)
						{
							actionList.Skip ();
						}
						else
						{
							actionList.Interact ();
						}
					}
					break;

				case ActionListSource.AssetFile:
					if (actionListAsset)
					{
						if (setParameters && runMultipleTimes)
						{
							if (actionListAsset.canRunMultipleInstances)
							{
								for (int i = 0; i < successiveGUIData.Length + 1; i++)
								{
									AssignParameterValues (actionListAsset, i);

									if (actionListAsset.IsSkippable () && runInstantly)
									{
										AdvGame.SkipActionListAsset (actionListAsset);
									}
									else
									{
										actionListAsset.Interact ();
									}
								}
							}
							else
							{
								ACDebug.LogWarning ("Cannot set run multiple parameter configurations because the ActionList asset '" + actionListAsset + "' has 'Can run multiple instances?' unchecked.", actionListAsset);
							}
							return;
						}

						if (setParameters)
						{
							AssignParameterValues (actionListAsset);
						}

						if (actionListAsset.IsSkippable () && runInstantly)
						{
							AdvGame.SkipActionListAsset (actionListAsset);
						}
						else
						{
							actionListAsset.Interact ();
						}
					}
					break;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset _actionListAsset)
		{
			if (actionListSource == ActionListSource.InScene && actionListAsset == _actionListAsset) return true;
			return false;
		}

		#endif

	}

}