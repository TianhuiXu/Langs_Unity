/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionEndGame.cs"
 * 
 *	This Action will force the game to either
 *	restart an autosave, or quit.
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
	public class ActionEndGame : Action
	{
		
		public enum AC_EndGameType { QuitGame, LoadAutosave, ResetScene, RestartGame };
		public AC_EndGameType endGameType;
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		public int sceneNumber;
		public string sceneName;
		public bool resetMenus;
		public bool killActionLists;
		
		
		public override ActionCategory Category { get { return ActionCategory.Engine; }}
		public override string Title { get { return "End game"; }}
		public override string Description { get { return "Ends the current game, either by loading an autosave, restarting or quitting the game executable."; }}
		public override int NumSockets { get { return 0; }}


		public override float Run ()
		{
			switch (endGameType)
			{
				case AC_EndGameType.QuitGame:
					#if UNITY_EDITOR
					EditorApplication.isPlaying = false;
					#else
					Application.Quit ();
					#endif
					break;

				case AC_EndGameType.LoadAutosave:
					SaveSystem.LoadAutoSave ();
					break;

				case AC_EndGameType.RestartGame:
					if (KickStarter.settingsManager.referenceScenesInSave == ChooseSceneBy.Name)
					{
						string _sceneName = (chooseSceneBy == ChooseSceneBy.Name) ? sceneName : KickStarter.sceneChanger.IndexToName (sceneNumber);
						KickStarter.RestartGame (resetMenus, _sceneName, killActionLists);
					}
					else if (KickStarter.settingsManager.referenceScenesInSave == ChooseSceneBy.Number)
					{
						int _sceneIndex = (chooseSceneBy == ChooseSceneBy.Name) ? KickStarter.sceneChanger.NameToIndex (sceneName) : sceneNumber;
						KickStarter.RestartGame (resetMenus, _sceneIndex, killActionLists);
					}
					break;

				case AC_EndGameType.ResetScene:
					KickStarter.sceneChanger.ResetCurrentScene ();
					break;

				default:
					break;
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			endGameType = (AC_EndGameType) EditorGUILayout.EnumPopup ("Command:", endGameType);

			if (endGameType == AC_EndGameType.RestartGame)
			{
				chooseSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);
				if (chooseSceneBy == ChooseSceneBy.Name)
				{
					sceneName = EditorGUILayout.TextField ("Scene to restart to:", sceneName);
				}
				else
				{
					sceneNumber = EditorGUILayout.IntField ("Scene to restart to:", sceneNumber);
				}

				resetMenus = EditorGUILayout.Toggle ("Reset all Menus?", resetMenus);
				killActionLists = EditorGUILayout.Toggle ("End all ActionLists?", killActionLists);
			}
		}
		

		public override string SetLabel ()
		{
			return endGameType.ToString ();
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to quit the game</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_QuitGame ()
		{
			ActionEndGame newAction = CreateNew<ActionEndGame> ();
			newAction.endGameType = AC_EndGameType.QuitGame;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to reset the current scene</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_ResetScene ()
		{
			ActionEndGame newAction = CreateNew<ActionEndGame> ();
			newAction.endGameType = AC_EndGameType.ResetScene;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to load the autosave</summary>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_LoadAutosave ()
		{
			ActionEndGame newAction = CreateNew<ActionEndGame> ();
			newAction.endGameType = AC_EndGameType.LoadAutosave;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: End game' Action, set to restart the game</summary>
		 * <param name = "newSceneBuildIndex">The build index number of the scene to load</param>
		 * <param name = "resetMenus">If True, then the state of all menus (e.g. visibility) will be reset</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionEndGame CreateNew_RestartGame (int newSceneBuildIndex, bool resetMenus = true)
		{
			ActionEndGame newAction = CreateNew<ActionEndGame> ();
			newAction.endGameType = AC_EndGameType.RestartGame;
			newAction.chooseSceneBy = ChooseSceneBy.Number;
			newAction.resetMenus = resetMenus;
			return newAction;
		}
		
	}

}