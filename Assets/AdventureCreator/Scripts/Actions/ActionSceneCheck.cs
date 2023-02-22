/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSceneCheck.cs"
 * 
 *	This action checks the player's last-visited scene,
 *	useful for running specific "player enters the room" cutscenes.
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
	public class ActionSceneCheck : ActionCheck
	{
		
		public enum IntCondition { EqualTo, NotEqualTo };
		public enum SceneToCheck { Current, Previous };
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		public SceneToCheck sceneToCheck = SceneToCheck.Current;
		[SerializeField] protected int chooseSceneByPlayerSwitching = -1;
		protected enum ChooseSceneByPlayerSwitching { Number=0, Name=1, CurrentMain=2 };

		public int sceneNumberParameterID = -1;
		public int sceneNumber;

		public int sceneNameParameterID = -1;
		public string sceneName;

		public int playerID = -1;
		public int playerParameterID = -1;

		private int runtimePlayerID;
		public IntCondition intCondition;


		public override ActionCategory Category { get { return ActionCategory.Scene; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Queries either the current scene, or the last one visited."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			sceneNumber = AssignInteger (parameters, sceneNumberParameterID, sceneNumber);
			sceneName = AssignString (parameters, sceneNameParameterID, sceneName);
			runtimePlayerID = AssignInteger (parameters, playerParameterID, playerID);
		}

		
		public override bool CheckCondition ()
		{
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				runtimePlayerID = -1;
			}

			// Check if first scene
			if (sceneToCheck == SceneToCheck.Previous && chooseSceneBy == ChooseSceneBy.Name && string.IsNullOrEmpty (sceneName))
			{
				if (runtimePlayerID >= 0)
				{
					ChooseSceneByPlayerSwitching csbps = (ChooseSceneByPlayerSwitching) chooseSceneByPlayerSwitching;
					if (csbps != ChooseSceneByPlayerSwitching.CurrentMain)
					{
						PlayerData playerData = KickStarter.saveSystem.GetPlayerData (runtimePlayerID);
						if (playerData != null)
						{
							switch (intCondition)
							{
								case IntCondition.EqualTo:
									return playerData.previousSceneName == string.Empty;

								case IntCondition.NotEqualTo:
									return playerData.previousSceneName != string.Empty;
							}
						}
					}
				}
				else
				{
					switch (intCondition)
					{
						case IntCondition.EqualTo:
							return KickStarter.sceneChanger.PreviousSceneName == string.Empty;

						case IntCondition.NotEqualTo:
							return KickStarter.sceneChanger.PreviousSceneName != string.Empty;
					}
				}
			}

			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					return CheckCondition_Name ();

				case ChooseSceneBy.Number:
				default:
					return CheckCondition_Number ();
			}
		}


		private bool CheckCondition_Name ()
		{
			string actualSceneName = string.Empty;

			if (runtimePlayerID >= 0)
			{
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (runtimePlayerID);
				if (playerData != null)
				{
					actualSceneName = (sceneToCheck == SceneToCheck.Previous) ? playerData.previousSceneName : playerData.currentSceneName;

					ChooseSceneByPlayerSwitching csbps = (ChooseSceneByPlayerSwitching) chooseSceneByPlayerSwitching;
					if (csbps == ChooseSceneByPlayerSwitching.CurrentMain)
					{
						return (actualSceneName == SceneChanger.CurrentSceneName);
					}
					chooseSceneBy = (ChooseSceneBy) chooseSceneByPlayerSwitching;
				}
				else
				{
					LogWarning ("Could not find scene data for Player ID = " + playerID);
				}
			}
			else
			{
				actualSceneName = (sceneToCheck == SceneToCheck.Previous) ? KickStarter.sceneChanger.PreviousSceneName : SceneChanger.CurrentSceneName;
			}

			if (sceneToCheck == SceneToCheck.Previous)
			{
				if (string.IsNullOrEmpty (actualSceneName))
				{
					LogWarning ("The " + sceneToCheck + " scene's name is currently empty - is this the game's first scene?");
					return false;
				}
			}

			int actualSceneNumber = KickStarter.sceneChanger.NameToIndex (actualSceneName);
			return DoSceneCheck (actualSceneName, actualSceneNumber);
		}


		private bool CheckCondition_Number ()
		{
			int actualSceneNumber = 0;

			if (runtimePlayerID >= 0)
			{
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (runtimePlayerID);
				if (playerData != null)
				{
					actualSceneNumber = (sceneToCheck == SceneToCheck.Previous) ? playerData.previousScene : playerData.currentScene;

					ChooseSceneByPlayerSwitching csbps = (ChooseSceneByPlayerSwitching) chooseSceneByPlayerSwitching;
					if (csbps == ChooseSceneByPlayerSwitching.CurrentMain)
					{
						return (actualSceneNumber == SceneChanger.CurrentSceneIndex);
					}
					chooseSceneBy = (ChooseSceneBy) chooseSceneByPlayerSwitching;
				}
				else
				{
					LogWarning ("Could not find scene data for Player ID = " + playerID);
				}
			}
			else
			{
				actualSceneNumber = (sceneToCheck == SceneToCheck.Previous) ? KickStarter.sceneChanger.PreviousSceneIndex : SceneChanger.CurrentSceneIndex;
			}

			if (sceneToCheck == SceneToCheck.Previous)
			{
				if (actualSceneNumber == -1)
				{
					LogWarning ("The " + sceneToCheck + " scene's Build Index is currently " + actualSceneNumber + " - is this the game's first scene?");
					return false;
				}
			}

			string actualSceneName = KickStarter.sceneChanger.IndexToName (actualSceneNumber);
			return DoSceneCheck (actualSceneName, actualSceneNumber);
		}


		private bool DoSceneCheck (string actualSceneName, int actualSceneNumber)
		{
			switch (chooseSceneBy)
			{
				case ChooseSceneBy.Name:
					if ((intCondition == IntCondition.EqualTo) == (actualSceneName == AdvGame.ConvertTokens (sceneName)))
					{
						return true;
					}
					break;

				case ChooseSceneBy.Number:
				default:
					if ((intCondition == IntCondition.EqualTo) == (actualSceneNumber == sceneNumber))
					{
						return true;
					}
					break;
			}

			return false;
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			bool showPlayerOptions = false;

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				playerParameterID = ChooseParameterGUI ("Player:", parameters, playerParameterID, ParameterType.Integer);
				if (playerParameterID < 0)
				{
					playerID = ChoosePlayerGUI (playerID, true);
					showPlayerOptions = (playerID >= 0);
				}
				else
				{
					showPlayerOptions = true;
				}
			}

			if (showPlayerOptions)
			{
				sceneToCheck = (SceneToCheck) EditorGUILayout.EnumPopup ("Check type:", sceneToCheck);
				if (chooseSceneByPlayerSwitching == -1)
				{
					chooseSceneByPlayerSwitching = (int) chooseSceneBy;
				}
				ChooseSceneByPlayerSwitching csbps = (ChooseSceneByPlayerSwitching) chooseSceneByPlayerSwitching;
				csbps = (ChooseSceneByPlayerSwitching) EditorGUILayout.EnumPopup ("Choose scene by:", csbps);
				chooseSceneByPlayerSwitching = (int) csbps;
				chooseSceneBy = (ChooseSceneBy) chooseSceneByPlayerSwitching;

				EditorGUILayout.BeginHorizontal ();
				switch (csbps)
				{
					case ChooseSceneByPlayerSwitching.Name:
						EditorGUILayout.LabelField ("Scene name is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNameParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (sceneNameParameterID < 0)
						{
							sceneName = EditorGUILayout.TextField (sceneName);
						}
						break;

					case ChooseSceneByPlayerSwitching.Number:
						EditorGUILayout.LabelField ("Scene number is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNumberParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNumberParameterID, ParameterType.Integer);
						if (sceneNumberParameterID < 0)
						{
							sceneNumber = EditorGUILayout.IntField (sceneNumber);
						}
						break;

					default:
						break;
				}
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				sceneToCheck = (SceneToCheck) EditorGUILayout.EnumPopup ("Check type:", sceneToCheck);
				chooseSceneBy = (ChooseSceneBy) EditorGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);

				EditorGUILayout.BeginHorizontal ();
				switch (chooseSceneBy)
				{
					case ChooseSceneBy.Name:
						EditorGUILayout.LabelField ("Scene name is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNameParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (sceneNameParameterID < 0)
						{
							sceneName = EditorGUILayout.TextField (sceneName);
						}
						break;

					case ChooseSceneBy.Number:
						EditorGUILayout.LabelField ("Scene number is:", GUILayout.Width (100f));
						intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

						sceneNumberParameterID = ChooseParameterGUI (string.Empty, parameters, sceneNumberParameterID, ParameterType.Integer);
						if (sceneNumberParameterID < 0)
						{
							sceneNumber = EditorGUILayout.IntField (sceneNumber);
						}
						break;

					default:
						break;
				}
				EditorGUILayout.EndHorizontal ();
			}
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Scene: Check' Action</summary>
		 * <param name = "sceneName">The name of the scene to check for</param>
		 * <param name = "sceneToCheck">Which scene type to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneCheck CreateNew (string sceneName, SceneToCheck sceneToCheck = SceneToCheck.Current)
		{
			ActionSceneCheck newAction = CreateNew<ActionSceneCheck> ();
			newAction.sceneToCheck = sceneToCheck;
			newAction.chooseSceneBy = ChooseSceneBy.Name;
			newAction.sceneName = sceneName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Scene: Check' Action</summary>
		 * <param name = "sceneName">The build index number of the scene to check for</param>
		 * <param name = "sceneToCheck">Which scene type to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneCheck CreateNew (int sceneNumber, SceneToCheck sceneToCheck = SceneToCheck.Current)
		{
			ActionSceneCheck newAction = CreateNew<ActionSceneCheck> ();
			newAction.sceneToCheck = sceneToCheck;
			newAction.chooseSceneBy = ChooseSceneBy.Number;
			newAction.sceneNumber = sceneNumber;
			return newAction;
		}

	}

}