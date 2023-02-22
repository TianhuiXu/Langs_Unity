#if UNITY_EDITOR

using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(PlayerStart))]
	public class PlayerStartEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			PlayerStart _target = (PlayerStart) target;

			if (KickStarter.sceneSettings != null && KickStarter.sceneSettings.defaultPlayerStart == _target)
			{
				EditorGUILayout.HelpBox ("This PlayerStart is the scene's default, and will be used if a more appropriate one is not found.", MessageType.Info);
			}

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Previous scene activation", EditorStyles.boldLabel);
			_target.chooseSceneBy = (ChooseSceneBy)CustomGUILayout.EnumPopup ("Choose scene by:", _target.chooseSceneBy, "", "The way in which the previous scene is identified by");
			if (_target.chooseSceneBy == ChooseSceneBy.Name)
			{
				_target.previousSceneName = CustomGUILayout.TextField ("Previous scene:", _target.previousSceneName, "", "The name of the previous scene to check for");
			}
			else
			{
				_target.previousScene = CustomGUILayout.IntField ("Previous scene:", _target.previousScene, "", "The build-index number of the previous scene to check for");
			}

			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				_target.limitByActive = (PlayerStartActiveOption) CustomGUILayout.EnumPopup ("Limit by active:", _target.limitByActive, "", "Lets you limit activation to active or inactive Players only");

				_target.limitByPlayer = CustomGUILayout.Toggle ("Limit by Player?", _target.limitByPlayer, "", "If True, then only specific Players can use this when entering from a previous scene");
				if (_target.limitByPlayer)
				{
					_target.playerIDs = ChoosePlayerGUI (_target.playerIDs);
				}
			}

			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Camera settings", EditorStyles.boldLabel);
			_target.cameraOnStart = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Camera on start:", _target.cameraOnStart, true, "", "The AC _Camera that should be made active when the Player starts the scene from this point");
			_target.fadeInOnStart = CustomGUILayout.Toggle ("Fade in on activate?", _target.fadeInOnStart, "", "If True, then the MainCamera will fade in when the Player starts the scene from this point");
			if (_target.fadeInOnStart)
			{
				_target.fadeSpeed = CustomGUILayout.FloatField ("Fade speed:", _target.fadeSpeed, "", "The speed of the fade");
			}
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private List<int> ChoosePlayerGUI (List<int> playerIDs)
		{
			CustomGUILayout.LabelField ("Players:");

			foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
			{
				string playerName = "    " + playerPrefab.ID + ": " + ((playerPrefab.playerOb) ? playerPrefab.playerOb.GetName () : "(Unnamed)");
				bool isActive = false;
				foreach (int playerID in playerIDs)
				{
					if (playerID == playerPrefab.ID) isActive = true;
				}

				bool wasActive = isActive;
				isActive = EditorGUILayout.Toggle (playerName, isActive);
				if (isActive != wasActive)
				{
					if (isActive)
					{
						playerIDs.Add (playerPrefab.ID);
					}
					else
					{
						playerIDs.Remove (playerPrefab.ID);
					}
				}
			}
			return playerIDs;
		}

	}

}

#endif