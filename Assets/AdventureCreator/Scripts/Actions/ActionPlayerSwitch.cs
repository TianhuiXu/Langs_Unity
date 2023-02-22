/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionPlayerSwitch.cs"
 * 
 *	This action causes a different Player prefab
 *	to be controlled.  Note that only one Player prefab
 *  can exist in a scene at any one time - for two player
 *  "characters" to be present, one must be a swapped-out
 * 	NPC instead.
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
	public class ActionPlayerSwitch : Action
	{
		
		public int playerID;
		public int playerIDParameterID = -1;

		public bool keepInventory = false;
		
		public bool alwaysSnapCamera = true;
		public bool assignScreenOverlay;

		#if UNITY_EDITOR
		private SettingsManager settingsManager;
		#endif

		public bool stopOldMoving, stopNewMoving;
		public bool takeOldPlayerPosition;

		// Deprecated variables:
		public NewPlayerPosition newPlayerPosition = NewPlayerPosition.ReplaceNPC;
		public OldPlayer oldPlayer = OldPlayer.RemoveFromScene;
		
		public bool restorePreviousData = false;
		public ChooseSceneBy chooseNewSceneBy = ChooseSceneBy.Number;
		public int newPlayerScene;
		public string newPlayerSceneName;
		
		public int oldPlayerNPC_ID;
		public NPC oldPlayerNPC;
		protected NPC runtimeOldPlayerNPC;
		
		public int newPlayerNPC_ID;
		public NPC newPlayerNPC;
		protected NPC runtimeNewPlayerNPC;
		
		public int newPlayerMarker_ID;
		public Marker newPlayerMarker;
		protected Marker runtimeNewPlayerMarker;

		
		public override ActionCategory Category { get { return ActionCategory.Player; }}
		public override string Title { get { return "Switch"; }}
		public override string Description { get { return "Swaps out the Player prefab mid-game. If the new prefab has been used before, you can restore that prefab's position data – otherwise you can set the position or scene of the new player. This Action only applies to games for which 'Player switching' has been allowed in the Settings Manager."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			playerID = AssignInteger (parameters, playerIDParameterID, playerID);
		}


		public override float Run ()
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				PlayerPrefab newPlayerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);

				if (newPlayerPrefab != null)
				{
					if (KickStarter.player != null && KickStarter.player.ID == playerID)
					{
						Log ("Cannot switch Player - already controlling the desired character.");
						return 0f;
					}

					if (newPlayerPrefab.playerOb != null)
					{
						KickStarter.playerInteraction.StopMovingToHotspot ();
						KickStarter.saveSystem.SaveCurrentPlayerData ();

						if (takeOldPlayerPosition)
						{
							if (KickStarter.player != null)
							{
								if (keepInventory)
								{
									KickStarter.saveSystem.AssignItemsToPlayer (KickStarter.runtimeInventory.PlayerInvCollection, playerID);
								}

								_Camera oldCamera = KickStarter.mainCamera.attachedCamera;
								KickStarter.player.RemoveFromScene ();

								// Transfer open sub-scene data since both are in the same scene
								PlayerData tempPlayerData = new PlayerData ();
								tempPlayerData = KickStarter.sceneChanger.SavePlayerData (tempPlayerData);
								string openSubSceneData = tempPlayerData.openSubScenes;
								string openSubSceneNameData = tempPlayerData.openSubSceneNames;
								PlayerData newPlayerData = KickStarter.saveSystem.GetPlayerData (playerID);
								newPlayerData.openSubScenes = openSubSceneData;
								newPlayerData.openSubSceneNames = openSubSceneNameData;
								
								// Force-set position / scene
								PlayerData oldPlayerData = KickStarter.saveSystem.GetPlayerData (KickStarter.saveSystem.CurrentPlayerID);
								newPlayerData.CopyPosition (oldPlayerData);
								newPlayerData.UpdatePresenceInScene ();
								oldPlayerData.currentScene = -1;
								oldPlayerData.currentSceneName = string.Empty;

								Player newPlayer = newPlayerPrefab.GetSceneInstance (true);
								KickStarter.player = newPlayer;

								if (KickStarter.player)
								{
									KickStarter.player.EndPath ();
									KickStarter.player.StopFollowing ();
								}

								// Same camera
								if (oldCamera != null && KickStarter.mainCamera.attachedCamera != oldCamera)
								{
									oldCamera.MoveCameraInstant ();
									KickStarter.mainCamera.SetGameCamera (oldCamera);
								}

								return 0f;
							}
							else
							{
								LogWarning ("Cannot take the old Player's position because no Player is currently active!");
							}
						}
						else
						{
							if (KickStarter.player != null)
							{
								if (stopOldMoving || !KickStarter.player.IsPathfinding ())
								{
									KickStarter.player.EndPath ();
								}
							}

							if (keepInventory)
							{
								KickStarter.saveSystem.AssignItemsToPlayer (KickStarter.runtimeInventory.PlayerInvCollection, playerID);
							}

							switch (KickStarter.settingsManager.referenceScenesInSave)
							{
								case ChooseSceneBy.Name:
									{
										string sceneNameToLoad = KickStarter.saveSystem.GetPlayerSceneName (playerID);
										if (string.IsNullOrEmpty (sceneNameToLoad))
										{
											bool hasData = (KickStarter.saveSystem.GetPlayerData (playerID) != null);
											if (hasData)
												LogWarning ("Cannot switch to Player ID " + playerID + " because their current scene name = " + sceneNameToLoad);
											else
												LogWarning ("Cannot switch to Player ID " + playerID + " because no save data was found for them.");
											return 0f;
										}

										SubScene subScene = KickStarter.sceneChanger.GetSubScene (sceneNameToLoad);
										if (subScene == null && SceneChanger.CurrentSceneName != sceneNameToLoad)
										{
											// Different scene, and not open as a sub-scene
											if (KickStarter.player != null)
											{
												KickStarter.player.Halt ();
											}

											KickStarter.saveSystem.SwitchToPlayerInDifferentScene (playerID, sceneNameToLoad, assignScreenOverlay);
											return 0f;
										}
									}
									break;

								case ChooseSceneBy.Number:
								default:
									{
										int sceneIndexToLoad = KickStarter.saveSystem.GetPlayerSceneIndex (playerID);
										if (sceneIndexToLoad < 0)
										{
											bool hasData = (KickStarter.saveSystem.GetPlayerData (playerID) != null);
											if (hasData)
												LogWarning ("Cannot switch to Player ID " + playerID + " because their current scene index = " + sceneIndexToLoad);
											else
												LogWarning ("Cannot switch to Player ID " + playerID + " because no save data was found for them.");
											return 0f;
										}

										SubScene subScene = KickStarter.sceneChanger.GetSubScene (sceneIndexToLoad);
										if (subScene == null && SceneChanger.CurrentSceneIndex != sceneIndexToLoad)
										{
											// Different scene, and not open as a sub-scene
											if (KickStarter.player != null)
											{
												KickStarter.player.Halt ();
											}

											KickStarter.saveSystem.SwitchToPlayerInDifferentScene (playerID, sceneIndexToLoad, assignScreenOverlay);
											return 0f;
										}
									}
									break;
							}

							

							// Same scene
						
							// Transfer open sub-scene data since both are in the same scene
							PlayerData tempPlayerData = new PlayerData ();
							tempPlayerData = KickStarter.sceneChanger.SavePlayerData (tempPlayerData);
							string openSubSceneData = tempPlayerData.openSubScenes;
							string openSubSceneNameData = tempPlayerData.openSubSceneNames;
							PlayerData newPlayerData = KickStarter.saveSystem.GetPlayerData (playerID);
							newPlayerData.openSubScenes = openSubSceneData;
							newPlayerData.openSubSceneNames = openSubSceneNameData;
						
							Player newPlayer = newPlayerPrefab.GetSceneInstance (true);
							KickStarter.player = newPlayer;

							if (stopNewMoving && KickStarter.player != null)
							{
								KickStarter.player.EndPath ();
								KickStarter.player.StopFollowing ();
							}
						
							if (alwaysSnapCamera && KickStarter.mainCamera.attachedCamera && KickStarter.mainCamera)
							{
								KickStarter.mainCamera.attachedCamera.MoveCameraInstant ();
							}
						}
					}
					else
					{
						LogWarning ("Cannot switch to an empty Player - no Player prefab is defined for ID " + playerID + ".");
					}
				}
				else
				{
					LogWarning ("Cannot switch Player - no Player prefab is defined.");
				}
			}

			return 0f;
		}


		public override void Upgrade ()
		{
			if (newPlayerPosition == NewPlayerPosition.ReplaceCurrentPlayer)
			{
				takeOldPlayerPosition = true;
				newPlayerPosition = NewPlayerPosition.ReplaceNPC;
			}
			base.Upgrade ();
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (settingsManager == null)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (settingsManager == null)
			{
				return;
			}
			
			if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				EditorGUILayout.HelpBox ("This Action requires Player Switching to be allowed, as set in the Settings Manager.", MessageType.Info);
				return;
			}

			if (settingsManager.players.Count > 0)
			{
				playerIDParameterID = Action.ChooseParameterGUI ("New Player ID:", parameters, playerIDParameterID, ParameterType.Integer);
				if (playerIDParameterID == -1)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					
					int i = 0;
					int playerNumber = -1;

					foreach (PlayerPrefab playerPrefab in settingsManager.players)
					{
						if (playerPrefab.playerOb != null)
						{
							labelList.Add (playerPrefab.ID + ": " + playerPrefab.playerOb.name);
						}
						else
						{
							labelList.Add (playerPrefab.ID + ": " + "(Undefined prefab)");
						}
						
						// If a player has been removed, make sure selected player is still valid
						if (playerPrefab.ID == playerID)
						{
							playerNumber = i;
						}
						
						i++;
					}
					
					if (playerNumber == -1)
					{
						// Wasn't found (item was possibly deleted), so revert to zero
						if (playerID > 0) LogWarning ("Previously chosen Player no longer exists!");
						
						playerNumber = 0;
						playerID = 0;
					}
				
					playerNumber = EditorGUILayout.Popup ("New Player:", playerNumber, labelList.ToArray());
					playerID = settingsManager.players[playerNumber].ID;
				}

				//
				takeOldPlayerPosition = EditorGUILayout.ToggleLeft ("Replace old Player's position?", takeOldPlayerPosition);

				if (takeOldPlayerPosition)
				{
					EditorGUILayout.HelpBox ("The old Player will be moved to scene index -1.", MessageType.Info);
				}
				else
				{
					stopNewMoving = EditorGUILayout.Toggle ("Stop new Player moving?", stopNewMoving);
					stopOldMoving = EditorGUILayout.Toggle ("Stop old Player moving?", stopOldMoving);

					alwaysSnapCamera = EditorGUILayout.Toggle ("Snap camera if shared?", alwaysSnapCamera);
					assignScreenOverlay = EditorGUILayout.ToggleLeft ("Overlay current screen if switch scene?", assignScreenOverlay);
				}
				//

				if (AdvGame.GetReferences ().settingsManager == null || !AdvGame.GetReferences ().settingsManager.shareInventory)
				{
					keepInventory = EditorGUILayout.Toggle ("Transfer inventory?", keepInventory);
				}
			}
			else
			{
				EditorGUILayout.LabelField ("No players exist!");
				playerID = -1;
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberNPC> (oldPlayerNPC);
				AddSaveScript <RememberNPC> (newPlayerNPC);
			}

			AssignConstantID <NPC> (oldPlayerNPC, oldPlayerNPC_ID, -1);
			AssignConstantID <NPC> (newPlayerNPC, newPlayerNPC_ID, -1);
			AssignConstantID <Marker> (newPlayerMarker, newPlayerMarker_ID, -1);
		}

		
		public override string SetLabel ()
		{
			if (playerIDParameterID >= 0) return string.Empty;

			if (settingsManager == null)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (settingsManager != null && settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				PlayerPrefab newPlayerPrefab = settingsManager.GetPlayerPrefab (playerID);
				if (newPlayerPrefab != null)
				{
					if (newPlayerPrefab.playerOb != null)
					{
						return newPlayerPrefab.playerOb.name;
					}
					else
					{
						return "Undefined prefab";
					}
				}
			}
			
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			return false;
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (_playerID < 0 || playerIDParameterID >= 0) return false;
			return (playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Player: Switch' Action</summary>
		 * <param name = "newPlayerID">The ID number of the Player to switch to</param>
		 * <param name = "takeOldPlayerPosition">If True, the old Player will be removed and the new Player will be spawned in their place</param>
		 * <param name = "transferInventory">If True, the previous Player's inventory will be transferred to the new one</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayerSwitch CreateNew (int newPlayerID, bool takeOldPlayerPosition = false, bool transferInventory = false)
		{
			ActionPlayerSwitch newAction = CreateNew<ActionPlayerSwitch> ();
			newAction.playerID = newPlayerID;
			newAction.keepInventory = transferInventory;
			newAction.takeOldPlayerPosition = takeOldPlayerPosition;
			
			return newAction;
		}
		
	}
	
}