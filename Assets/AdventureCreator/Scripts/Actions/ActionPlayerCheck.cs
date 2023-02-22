/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionPlayerCheck.cs"
 * 
 *	This action checks to see which
 *	Player prefab is currently being controlled.
 * 
 */

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionPlayerCheck : ActionCheck
	{
		
		public int playerID;
		public int playerIDParameterID;

		#if UNITY_EDITOR
		private SettingsManager settingsManager;
		#endif

		
		public override ActionCategory Category { get { return ActionCategory.Player; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Queries which Player prefab is currently being controlled. This only applies to games for which 'Player switching' has been allowed in the Settings Manager."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			playerID = AssignInteger (parameters, playerIDParameterID, playerID);
		}
		

		public override bool CheckCondition ()
		{
			if (KickStarter.player && KickStarter.player.ID == playerID)
			{
				return true;
			}

			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (!settingsManager)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
			
			if (!settingsManager)
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
				playerIDParameterID = Action.ChooseParameterGUI ("Current Player ID:", parameters, playerIDParameterID, ParameterType.Integer);
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
							labelList.Add (playerPrefab.playerOb.name);
						}
						else
						{
							labelList.Add ("(Undefined prefab)");
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
					
					playerNumber = EditorGUILayout.Popup ("Current Player is:", playerNumber, labelList.ToArray());
					playerID = settingsManager.players[playerNumber].ID;
				}
			}
			else
			{
				EditorGUILayout.LabelField ("No players exist!");
				playerID = -1;
			}
		}


		public override string SetLabel ()
		{
			if (playerIDParameterID >= 0) return string.Empty;

			if (settingsManager != null &&
				settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
				if (playerPrefab != null && playerPrefab.playerOb != null)
				{
					return playerPrefab.playerOb.name;
				}
				else
				{
					return "Undefined prefab";
				}
			}
			
			return string.Empty;
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (_playerID < 0 || playerIDParameterID >= 0) return false;
			return (playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Player: Check' Action</summary>
		 * <param name = "playerIDToCheck">The ID number of the Player to check is active</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayerCheck CreateNew (int playerIDToCheck)
		{
			ActionPlayerCheck newAction = CreateNew<ActionPlayerCheck> ();
			newAction.playerID = playerIDToCheck;
			return newAction;
		}
		
	}

}