/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSceneAdd.cs"
 * 
 *	This action adds or removes a scene without affecting any other open scenes.
 * 
 */

using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionSceneAdd : Action
	{

		public enum SceneAddRemove { Add, Remove };
		public SceneAddRemove sceneAddRemove = SceneAddRemove.Add;
		public bool runCutsceneOnStart;
		public bool runCutsceneIfAlreadyOpen;
		
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		public int sceneNumber;
		public int sceneNumberParameterID = -1;
		public string sceneName;
		public int sceneNameParameterID = -1;

		protected bool awaitingCallback = false;
		private int numIterations;


		public override ActionCategory Category { get { return ActionCategory.Scene; }}
		public override string Title { get { return "Add or remove"; }}
		public override string Description { get { return "Adds or removes a scene without affecting any other open scenes."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			sceneNumber = AssignInteger (parameters, sceneNumberParameterID, sceneNumber);
			sceneName = AssignString (parameters, sceneNameParameterID, sceneName);
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				numIterations = 0;
				awaitingCallback = false;
				isRunning = true;
				AddEventHooks ();

				if (KickStarter.sceneSettings.OverridesCameraPerspective ())
				{
					ACDebug.LogError ("The current scene overrides the default camera perspective - this feature should not be used in conjunction with multiple-open scenes.");
				}

				switch (KickStarter.settingsManager.referenceScenesInSave)
				{
					case ChooseSceneBy.Name:
						return UpdateSceneByName ();

					case ChooseSceneBy.Number:
					default:
						return UpdateSceneByNumber ();
				}
			}
			else
			{
				numIterations++;
				if (awaitingCallback && numIterations < 50) // Failsafe
				{
					return defaultPauseTime;
				}
				
				isRunning = false;
			}

			RemoveEventHooks ();
			return 0f;
		}


		private float UpdateSceneByName ()
		{
			string runtimeSceneName = (chooseSceneBy == ChooseSceneBy.Name) ? sceneName : KickStarter.sceneChanger.IndexToName (sceneNumber);
			if (string.IsNullOrEmpty (runtimeSceneName)) return 0f;

			switch (sceneAddRemove)
			{
				case SceneAddRemove.Add:
					if (KickStarter.sceneChanger.AddSubScene (runtimeSceneName))
					{
						awaitingCallback = true;
						return defaultPauseTime;
					}

					if (runCutsceneIfAlreadyOpen && runCutsceneOnStart)
					{
						foreach (SubScene subScene in KickStarter.sceneChanger.SubScenes)
						{
							if (subScene.SceneName == runtimeSceneName)
							{
								PlayStartCutscene (subScene.SceneSettings);
								break;
							}
						}
					}
					break;

				case SceneAddRemove.Remove:
					KickStarter.sceneChanger.RemoveScene (runtimeSceneName);
					awaitingCallback = true;
					return defaultPauseTime;

				default:
					break;
			}

			return 0f;
		}


		private float UpdateSceneByNumber ()
		{
			int runtimeSceneIndex = (chooseSceneBy == ChooseSceneBy.Name) ? KickStarter.sceneChanger.NameToIndex (sceneName) : sceneNumber;
			if (runtimeSceneIndex < 0) return 0f;

			switch (sceneAddRemove)
			{
				case SceneAddRemove.Add:
					if (KickStarter.sceneChanger.AddSubScene (runtimeSceneIndex))
					{
						awaitingCallback = true;
						return defaultPauseTime;
					}

					if (runCutsceneIfAlreadyOpen && runCutsceneOnStart)
					{
						foreach (SubScene subScene in KickStarter.sceneChanger.SubScenes)
						{
							if (subScene.SceneIndex == runtimeSceneIndex)
							{
								PlayStartCutscene (subScene.SceneSettings);
								break;
							}
						}
					}
					break;

				case SceneAddRemove.Remove:
					KickStarter.sceneChanger.RemoveScene (runtimeSceneIndex);
					awaitingCallback = true;
					return defaultPauseTime;

				default:
					break;
			}

			return 0f;
		}


		private void AddEventHooks ()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
			UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
		}


		private void RemoveEventHooks ()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
			UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
		}


		private void OnSceneLoaded (Scene scene, LoadSceneMode loadSceneMode)
		{
			if (sceneAddRemove == SceneAddRemove.Add)
			{
				bool found = false;

				foreach (SubScene subScene in KickStarter.sceneChanger.SubScenes)
				{
					if (subScene.gameObject.scene == scene)
					{
						found = true;

						if (runCutsceneOnStart)
						{
							PlayStartCutscene (subScene.SceneSettings);
						}

						break;
					}
				}

				if (!found)
				{
					switch (KickStarter.settingsManager.referenceScenesInSave)
					{
						case ChooseSceneBy.Name:
							string runtimeSceneName = (chooseSceneBy == ChooseSceneBy.Name) ? sceneName : KickStarter.sceneChanger.IndexToName (sceneNumber);
							LogWarning ("Could not find SubScene class for scene " + runtimeSceneName + " - is it added to Unity's Build Settings?\nIf this is a non-AC scene, add a SubScene component to it and check 'Self Initialise'.");
							break;

						case ChooseSceneBy.Number:
						default:
							int runtimeSceneIndex = (chooseSceneBy == ChooseSceneBy.Name) ? KickStarter.sceneChanger.NameToIndex (sceneName) : sceneNumber;
							LogWarning ("Could not find SubScene class for scene " + runtimeSceneIndex + " - is it added to Unity's Build Settings?\nIf this is a non-AC scene, add a SubScene component to it and check 'Self Initialise'.");
							break;
					}
				}

				awaitingCallback = false;
			}
		}


		private void PlayStartCutscene (SceneSettings sceneSettings)
		{
			if (sceneSettings == null) return;

			switch (sceneSettings.actionListSource)
			{
				case ActionListSource.InScene:
					if (sceneSettings.cutsceneOnStart)
					{
						sceneSettings.cutsceneOnStart.Interact ();
					}
					break;

				case ActionListSource.AssetFile:
					if (sceneSettings.actionListAssetOnStart)
					{
						sceneSettings.actionListAssetOnStart.Interact ();
					}
					break;

				default:
					break;
			}
		}


		private void OnSceneUnloaded (Scene scene)
		{
			if (sceneAddRemove == SceneAddRemove.Remove)
			{
				awaitingCallback = false;
			}
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			sceneAddRemove = (SceneAddRemove) EditorGUILayout.EnumPopup ("Method:", sceneAddRemove);

			chooseSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);
			if (chooseSceneBy == ChooseSceneBy.Name)
			{
				sceneNameParameterID = Action.ChooseParameterGUI ("Scene name:", parameters, sceneNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (sceneNameParameterID < 0)
				{
					sceneName = EditorGUILayout.TextField ("Scene name:", sceneName);
				}
			}
			else
			{
				sceneNumberParameterID = Action.ChooseParameterGUI ("Scene number:", parameters, sceneNumberParameterID, ParameterType.Integer);
				if (sceneNumberParameterID < 0)
				{
					sceneNumber = EditorGUILayout.IntField ("Scene number:", sceneNumber);
				}
			}

			if (sceneAddRemove == SceneAddRemove.Add)
			{
				runCutsceneOnStart = EditorGUILayout.Toggle ("Run 'Cutscene on start'?", runCutsceneOnStart);
				if (runCutsceneOnStart)
				{
					runCutsceneIfAlreadyOpen = EditorGUILayout.Toggle ("Run if already open?", runCutsceneIfAlreadyOpen);
				}
			}
			else if (sceneAddRemove == SceneAddRemove.Remove && endings[0].resultAction != ResultAction.Stop)
			{
				if (isAssetFile)
				{
					EditorGUILayout.HelpBox ("If the active scene is removed, further Actions can only be run if the ActionList asset's 'Survive scene changes?' property is checked.", MessageType.Info);
				}
				else
				{
					EditorGUILayout.HelpBox ("If the active scene is removed, further Actions cannot be run - consider using an ActionList asset instead.", MessageType.Warning);
				}
			}
		}


		public override string SetLabel ()
		{
			if (chooseSceneBy == ChooseSceneBy.Name)
			{
				return sceneAddRemove.ToString () + " " + sceneName;
			}
			return sceneAddRemove.ToString () + " " + sceneNumber;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Scene: Add or remove' Action, set to add a new scene</summary>
		 * <param name = "newSceneInfo">Data about the scene to add</param>
		 * <param name = "runCutsceneOnStart">If True, the new scene's OnStart cutscene will be triggered</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneAdd CreateNew_Add (int newSceneIndex, bool runCutsceneOnStart)
		{
			ActionSceneAdd newAction = CreateNew<ActionSceneAdd> ();
			newAction.sceneAddRemove = SceneAddRemove.Add;
			newAction.sceneName = string.Empty;
			newAction.sceneNumber = newSceneIndex;
			newAction.chooseSceneBy = ChooseSceneBy.Number;
			newAction.runCutsceneOnStart = runCutsceneOnStart;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Scene: Add or remove' Action, set to remove an open scene</summary>
		 * <param name = "removeSceneIndex">Data about the scene to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneAdd CreateNew_Remove (int removeSceneIndex)
		{
			ActionSceneAdd newAction = CreateNew<ActionSceneAdd> ();
			newAction.sceneAddRemove = SceneAddRemove.Remove;
			newAction.sceneName = string.Empty;
			newAction.sceneNumber = removeSceneIndex;
			newAction.chooseSceneBy = ChooseSceneBy.Number;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Scene: Add or remove' Action, set to add a new scene</summary>
		 * <param name = "newSceneName">The scene to add</param>
		 * <param name = "runCutsceneOnStart">If True, the new scene's OnStart cutscene will be triggered</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneAdd CreateNew_Add (string newSceneName, bool runCutsceneOnStart)
		{
			ActionSceneAdd newAction = CreateNew<ActionSceneAdd> ();
			newAction.sceneAddRemove = SceneAddRemove.Add;
			newAction.sceneName = newSceneName;
			newAction.sceneNumber = -1;
			newAction.chooseSceneBy = ChooseSceneBy.Name;
			newAction.runCutsceneOnStart = runCutsceneOnStart;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Scene: Add or remove' Action, set to remove an open scene</summary>
		 * <param name = "removeSceneName">The scene to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneAdd CreateNew_Remove (string removeSceneName)
		{
			ActionSceneAdd newAction = CreateNew<ActionSceneAdd> ();
			newAction.sceneAddRemove = SceneAddRemove.Remove;
			newAction.sceneName = removeSceneName;
			newAction.sceneNumber = -1;
			newAction.chooseSceneBy = ChooseSceneBy.Name;
			return newAction;
		}

	}

}