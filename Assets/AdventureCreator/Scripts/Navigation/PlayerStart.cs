/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerStart.cs"
 * 
 *	This script defines a possible starting position for the
 *	player when the scene loads, based on what the previous
 *	scene was.  If no appropriate PlayerStart is found, the
 *	one define in SceneSettings is used as the default.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Defines a possible starting position for the Player when the scene loads, based on what the previous scene was
	 * If no appropriate PlayerStart is found, then the defaultPlayerStart defined in SceneSettings will be used instead.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_start.html")]
	public class PlayerStart : Marker
	{

		#region Variables

		/** If autoActivateFromPrevious = True, the way in which the previous scene is identified by (Number, Name) */
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		/** The number of the previous scene to check for */
		public int previousScene;
		/** The name of the previous scene to check for */
		public string previousSceneName;
		/** If True, then the MainCamera will fade in when the Player starts the scene from this point */
		public bool fadeInOnStart;
		/** The speed of the fade, if the MainCamera fades in when the Player starts the scene from this point */
		public float fadeSpeed = 0.5f;
		/** The _Camera that should be made active when the Player starts the scene from this point */
		public _Camera cameraOnStart;
		/** If >= 0, and player-switching is allowed, then this will only be used to automatically place the Player with the same ID value */
		public List<int> playerIDs = new List<int> ();
		/** If True, and player-switching is allowed, then only specific Players can use this from previous scenes */
		public bool limitByPlayer = false;
		/** Whether to limit activation by active / inactive Players */
		public PlayerStartActiveOption limitByActive = PlayerStartActiveOption.NoLimit;

		protected GameObject playerOb;

		#endregion


		#region PublicFunctions

		/**
		 * Places the Player at the GameObject's position, and activates the assigned cameraOnStart.
		 */
		public void PlacePlayerAt ()
		{
			if (KickStarter.mainCamera)
			{
				if (fadeInOnStart)
				{
					KickStarter.mainCamera.FadeIn (fadeSpeed);
				}
				
				if (KickStarter.settingsManager)
				{
					if (KickStarter.player)
					{
						KickStarter.player.SetLookDirection (ForwardDirection, true);
						KickStarter.player.Teleport (KickStarter.sceneChanger.GetStartPosition (Position));

						if (SceneSettings.ActInScreenSpace ())
						{
							KickStarter.player.Transform.position = AdvGame.GetScreenNavMesh (KickStarter.player.Transform.position);
						}
					}
				
					if (KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
					{
						KickStarter.mainCamera.SetFirstPerson ();
					}
					else if (cameraOnStart)
					{
						SetCameraOnStart ();
					}
					else
					{
						if (!KickStarter.settingsManager.IsInFirstPerson ())
						{
							ACDebug.LogWarning ("PlayerStart '" + gameObject.name + "' has no Camera On Start", this);

							if (KickStarter.sceneSettings != null &&
								this != KickStarter.sceneSettings.defaultPlayerStart)
							{
								KickStarter.sceneSettings.defaultPlayerStart.SetCameraOnStart ();
							}
						}
					}

					KickStarter.eventManager.Call_OnOccupyPlayerStart (KickStarter.player, this);
				}
			}
		}


		public bool MatchesPreviousScene (int _playerID)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				if (limitByPlayer && !playerIDs.Contains (_playerID))
				{
					return false;
				}

				switch (limitByActive)
				{
					case PlayerStartActiveOption.ActivePlayerOnly:
						if (KickStarter.saveSystem.CurrentPlayerID != _playerID)
						{
							return false;
						}
						break;

					case PlayerStartActiveOption.InactivePlayersOnly:
						if (KickStarter.saveSystem.CurrentPlayerID == _playerID)
						{
							return false;
						}
						break;

					default:
						break;
				}
			}

			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					string _previousSceneName = GetPlayerPreviousSceneName (_playerID);
					if (chooseSceneBy == ChooseSceneBy.Name && !string.IsNullOrEmpty (previousSceneName))
					{
						return (_previousSceneName == previousSceneName);
					}
					if (chooseSceneBy == ChooseSceneBy.Number && previousScene >= 0)
					{
						return (KickStarter.sceneChanger.IndexToName (previousScene) == _previousSceneName);
					}
					break;

				case ChooseSceneBy.Number:
				default:
					int previousSceneIndex = GetPlayerPreviousSceneIndex (_playerID);
					if (chooseSceneBy == ChooseSceneBy.Name && !string.IsNullOrEmpty (previousSceneName))
					{
						return (KickStarter.sceneChanger.NameToIndex (previousSceneName) == previousSceneIndex);
					}
					if (chooseSceneBy == ChooseSceneBy.Number && previousScene >= 0)
					{
						return previousScene == previousSceneIndex;
					}
					break;
			}

			return false;
		}


		private int GetPlayerPreviousSceneIndex (int _playerID)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return KickStarter.sceneChanger.PreviousSceneIndex;
			}

			PlayerData playerData = KickStarter.saveSystem.GetPlayerData (_playerID);
			return (playerData != null) ? playerData.previousScene : -1;
		}


		private string GetPlayerPreviousSceneName (int _playerID)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return KickStarter.sceneChanger.PreviousSceneName;
			}

			PlayerData playerData = KickStarter.saveSystem.GetPlayerData (_playerID);
			return (playerData != null) ? playerData.previousSceneName : string.Empty;
		}


		/** Makes the assigned cameraOnStart the active _Camera. */
		public void SetCameraOnStart ()
		{
			if (cameraOnStart && KickStarter.mainCamera)
			{
				cameraOnStart.MoveCameraInstant ();
				KickStarter.mainCamera.SetGameCamera (cameraOnStart);
				KickStarter.mainCamera.lastNavCamera = cameraOnStart;
			}
		}

		#endregion


		#if UNITY_EDITOR

		public override void DrawGizmos ()
		{
			Renderer _renderer = GetComponent<Renderer> ();
			if (_renderer && KickStarter.sceneSettings && !Application.isPlaying)
			{
				_renderer.enabled = KickStarter.sceneSettings.visibilityPlayerStarts;
			}
		}

		#endif
		
	}

}