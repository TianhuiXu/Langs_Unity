/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"PlayerPrefab.cs"
 * 
 *	A data container for a Player that is spawned automatically at runtime, and whose data is tracked automatically.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A data container for a Player that is spawned automatically at runtime, and whose data is tracked automatically.
	 */
	[System.Serializable]
	public class PlayerPrefab
	{

		#region Variables

		/** The Player prefab */
		public Player playerOb;
		/** A unique identifier */
		public int ID;
		/** If True, this Player is the game's default */
		public bool isDefault;
		/** The scene index to start in, if the Player is not the default, and chooseSceneBy = ChooseSceneBy.Number */
		public int startingSceneIndex = 0;
		/** How to reference the Player's starting scene, if not the default (Name, Number) */
		public ChooseSceneBy chooseSceneBy = ChooseSceneBy.Number;
		/** The name of the scene to start in, if the Player is not the default, and chooseSceneBy = ChooseSceneBy.Name */
		public string startingSceneName = "";
		/** If True, then the Player will appear at their initial scene's Default PlayerStart - as opposed to one specified here */
		public bool useSceneDefaultPlayerStart = true;
		/** The ConstantID value of the PlayerStart to appear at, if not the default Player */
		public int startingPlayerStartID;

		#endregion


		#region Constructors

		/**
		 * The default Constructor.
		 * An array of ID numbers is required, to ensure its own ID is unique.
		 */
		public PlayerPrefab (int[] idArray)
		{
			ID = 0;
			playerOb = null;

			if (idArray.Length > 0)
			{
				isDefault = false;

				foreach (int _id in idArray)
				{
					if (ID == _id)
						ID++;
				}
			}
			else
			{
				isDefault = true;
			}

			startingSceneIndex = 0;
			startingSceneName = string.Empty;
			chooseSceneBy = ChooseSceneBy.Number;
			startingPlayerStartID = 0;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets the runtime scene instance of the Player</summary>
		 * <param name = "spawnIfNotPresent">If True, the Player will be spawned if no scene instance was found.</param>
		 * <returns>The scene instance of the Player</returns>
		 */
		public Player GetSceneInstance (bool spawnIfNotPresent = false)
		{
			Player[] scenePlayers = Object.FindObjectsOfType<Player> ();
			foreach (Player scenePlayer in scenePlayers)
			{
				if (scenePlayer.ID == ID)
				{
					return scenePlayer;
				}
			}

			if (spawnIfNotPresent && playerOb)
			{
				return playerOb.SpawnFromPrefab (ID);
			}

			return null;
		}


		/**
		 * <summary>Spawns a new instance of the Player if one is not currently present.</summary>
		 * <param name = "makeActivePlayer">If True, the Player will be made the active Player afterwards</param>
		 */
		public void SpawnInScene (bool makeActivePlayer)
		{
			if (playerOb)
			{
				Player sceneInstance = GetSceneInstance (true);

				if (makeActivePlayer)
				{
					KickStarter.player = sceneInstance;
				}

				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (ID);
				if (playerData != null)
				{
					sceneInstance.LoadData (playerData);
				}
			}
		}


		/**
		 * <summary>Spawns a new instance of the Player if one is not currently present, and places them in a specific scene.  This will not be the active Player.</summary>
		 * <param name = "scene">The scene to place the instance in.</param>
		 */
		public void SpawnInScene (Scene scene)
		{
			if (playerOb)
			{
				Player sceneInstance = GetSceneInstance (true);

				UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (sceneInstance.gameObject, scene);

				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (ID);
				if (playerData != null)
				{
					sceneInstance.LoadData (playerData);
				}
			}
		}


		public void SetInitialPosition (PlayerData playerData)
		{
			TeleportPlayerStartMethod teleportPlayerStartMethod = (useSceneDefaultPlayerStart) ? TeleportPlayerStartMethod.SceneDefault : TeleportPlayerStartMethod.EnteredHere;

			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					playerData.UpdatePosition (StartingSceneName, teleportPlayerStartMethod, startingPlayerStartID);
					break;

				case ChooseSceneBy.Number:
				default:
					playerData.UpdatePosition (StartingSceneIndex, teleportPlayerStartMethod, startingPlayerStartID);
					break;
			}
		}


		/** Removes any runtime instance of the Player from the scene */
		public void RemoveFromScene ()
		{
			if (playerOb)
			{
				Player sceneInstance = GetSceneInstance ();
				if (sceneInstance)
				{
					sceneInstance.RemoveFromScene ();
				}
			}
		}

		#endregion


		#region GetSet

		private int StartingSceneIndex
		{
			get
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.GetDefaultPlayerPrefab () == this)
				{
					return -1;
				}

				if (chooseSceneBy == ChooseSceneBy.Name) return KickStarter.sceneChanger.NameToIndex (startingSceneName);
				return startingSceneIndex;
			}
			set
			{
				startingSceneIndex = value;
				chooseSceneBy = ChooseSceneBy.Number;
			}
		}


		private string StartingSceneName
		{
			get
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.GetDefaultPlayerPrefab () == this)
				{
					return string.Empty;
				}

				if (chooseSceneBy == ChooseSceneBy.Name) return startingSceneName;
				return KickStarter.sceneChanger.IndexToName (startingSceneIndex);
			}
			set
			{
				startingSceneName = value;
				chooseSceneBy = ChooseSceneBy.Name;
			}
		}

		#endregion


#if UNITY_EDITOR

		public void ShowGUI (string apiPrefix)
		{
			EditorGUILayout.BeginHorizontal ();

			string label = "Player " + ID + ":";
			if (isDefault)
			{
				label += " (DEFAULT)";
			}
			playerOb = (Player) CustomGUILayout.ObjectField<Player> (label, playerOb, false, "AC.KickStarter.settingsManager.players");

			if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
			{
				SideMenu (this);
			}

			EditorGUILayout.EndHorizontal ();
		}


		public void ShowStartDataGUI (string apiPrefix)
		{
			GUILayout.Label ("Starting point data for Player " + ID.ToString () + ": " + ((playerOb) ? playerOb.name : "(EMPTY)"), EditorStyles.boldLabel);

			chooseSceneBy = (ChooseSceneBy)CustomGUILayout.EnumPopup ("Choose scene by:", chooseSceneBy);
			switch (chooseSceneBy)
			{
				case ChooseSceneBy.Name:
					startingSceneName = CustomGUILayout.TextField ("Scene name:", startingSceneName);
					break;

				case ChooseSceneBy.Number:
					startingSceneIndex = CustomGUILayout.IntField ("Scene index:", startingSceneIndex);
					break;
			}

			useSceneDefaultPlayerStart = EditorGUILayout.Toggle ("Use default PlayerStart?", useSceneDefaultPlayerStart);
			if (!useSceneDefaultPlayerStart)
			{
				PlayerStart playerStart = ConstantID.GetComponent <PlayerStart> (startingPlayerStartID);
				playerStart = (PlayerStart)CustomGUILayout.ObjectField<PlayerStart> ("PlayerStart:", playerStart, true, apiPrefix + ".startingPlayerStartID", "The PlayerStart that this character starts from.");
				startingPlayerStartID = FieldToID<PlayerStart> (playerStart, startingPlayerStartID);

				if (startingPlayerStartID != 0)
				{
					CustomGUILayout.BeginVertical ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + startingPlayerStartID.ToString (), EditorStyles.miniLabel);
					CustomGUILayout.EndVertical ();
				}
			}
		}


		private int FieldToID<T> (T field, int _constantID) where T : Component
		{
			if (field == null)
			{
				return _constantID;
			}

			if (field.GetComponent<ConstantID> ())
			{
				if (!field.gameObject.activeInHierarchy && field.GetComponent<ConstantID> ().constantID == 0)
				{
					UnityVersionHandler.AddConstantIDToGameObject<ConstantID> (field.gameObject, true);
				}
				_constantID = field.GetComponent<ConstantID> ().constantID;
			}
			else
			{
				UnityVersionHandler.AddConstantIDToGameObject<ConstantID> (field.gameObject, true);
				AssetDatabase.SaveAssets ();
				_constantID = field.GetComponent<ConstantID> ().constantID;
			}

			return _constantID;
		}


		private static int sidePlayerPrefab = -1;
		private static void SideMenu (PlayerPrefab playerPrefab)
		{
			GenericMenu menu = new GenericMenu ();
			sidePlayerPrefab = KickStarter.settingsManager.players.IndexOf (playerPrefab);

			if (!playerPrefab.isDefault)
			{
				menu.AddItem (new GUIContent ("Set as default"), false, Callback, "SetAsDefault");
				menu.AddItem (new GUIContent ("Edit start data..."), false, Callback, "EditStartData");
				menu.AddSeparator (string.Empty);
			}

			menu.AddItem (new GUIContent ("Find references..."), false, Callback, "FindReferences");
			menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			
			menu.ShowAsContext ();
		}


		private static void Callback (object obj)
		{
			if (sidePlayerPrefab >= 0)
			{
				switch (obj.ToString ())
				{
					case "Delete":
						Undo.RecordObject (KickStarter.settingsManager, "Delete player reference");
						KickStarter.settingsManager.players.RemoveAt (sidePlayerPrefab);
						break;

					case "SetAsDefault":
						for (int i=0; i<KickStarter.settingsManager.players.Count; i++)
						{
							KickStarter.settingsManager.players[i].isDefault = (i == sidePlayerPrefab);
						}
						break;

					case "EditStartData":
						PlayerStartDataEditor.CreateNew (sidePlayerPrefab);
						break;

					case "FindReferences":
						PlayerPrefab playerPrefab = KickStarter.settingsManager.players[sidePlayerPrefab];
						FindPlayerReferences (playerPrefab.ID, (playerPrefab != null) ? playerPrefab.playerOb.GetName () : "(Unnamed)");
						break;

					default:
						break;
				}
			}
		}


		public static void FindPlayerReferences (int playerID, string playerName)
		{
			if (EditorUtility.DisplayDialog ("Search Player '" + playerName + "' references?", "The Editor will search ActionList assets, and scenes listed in the Build Settings, for references to this Player.  The current scene will need to be saved and listed to be included in the search process. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					// ActionList assets
					if (AdvGame.GetReferences ().speechManager != null)
					{
						ActionListAsset[] allActionListAssets = AdvGame.GetReferences ().speechManager.GetAllActionListAssets ();
						foreach (ActionListAsset actionListAsset in allActionListAssets)
						{
							SearchActionListAssetForPlayerReferences (playerID, playerName, actionListAsset);
						}
					}

					// Scenes
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
					string[] sceneFiles = AdvGame.GetSceneFiles ();

					foreach (string sceneFile in sceneFiles)
					{
						UnityVersionHandler.OpenScene (sceneFile);

						string suffix = " in scene '" + sceneFile + "'";
						SearchSceneForPlayerReferences (playerID, playerName, suffix);
					}

					UnityVersionHandler.OpenScene (originalScene);
				}
			}
		}


		private static void SearchSceneForPlayerReferences (int playerID, string playerName, string suffix)
		{
			ActionList[] localActionLists = GameObject.FindObjectsOfType<ActionList> ();
			foreach (ActionList actionList in localActionLists)
			{
				if (actionList.source == ActionListSource.InScene)
				{
					foreach (Action action in actionList.actions)
					{
						if (action != null)
						{
							if (action.ReferencesPlayer (playerID))
							{
								string actionLabel = (KickStarter.actionsManager != null) ? (" (" + KickStarter.actionsManager.GetActionTypeLabel (action) + ")") : "";
								Debug.Log ("'" + playerName + "' is referenced by Action #" + actionList.actions.IndexOf (action) + actionLabel + " in ActionList '" + actionList.gameObject.name + "'" + suffix, actionList);
							}
						}
					}
				}
				else if (actionList.source == ActionListSource.AssetFile)
				{
					SearchActionListAssetForPlayerReferences (playerID, playerName, actionList.assetFile);
				}
			}
		}


		private static void SearchActionListAssetForPlayerReferences (int playerID, string playerName, ActionListAsset actionListAsset)
		{
			if (actionListAsset == null) return;

			foreach (Action action in actionListAsset.actions)
			{
				if (action != null)
				{
					if (action.ReferencesPlayer (playerID))
					{
						string actionLabel = (KickStarter.actionsManager != null) ? (" (" + KickStarter.actionsManager.GetActionTypeLabel (action) + ")") : "";
						Debug.Log ("'" + playerName + "' is referenced by Action #" + actionListAsset.actions.IndexOf (action) + actionLabel + " in ActionList asset '" + actionListAsset.name + "'", actionListAsset);
					}
				}
			}
		}

		#endif

	}

}