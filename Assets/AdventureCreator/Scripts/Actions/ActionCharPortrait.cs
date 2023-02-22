/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCharPortrait.cs"
 * 
 *	This action picks a new portrait for the chosen Character.
 *	Written for the AC community by Guran.
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
	public class ActionCharPortrait : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		public bool isPlayer;
		public int playerID = -1;
		public Char _char;
		protected Char runtimeChar;
		public Texture newPortraitGraphic;


		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "Switch Portrait"; }}
		public override string Description { get { return "Changes the 'speaking' graphic used by Characters. To display this graphic in a Menu, place a Graphic element of type Dialogue Portrait in a Menu of Appear type: When Speech Plays. If the new graphic is placed in a Resources folder, it will be stored in saved game files."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				runtimeChar = AssignPlayer (playerID, parameters, parameterID);
				playerID = AssignInteger (parameters, parameterID, playerID);
			}
			else
			{
				runtimeChar = AssignFile<Char> (parameters, parameterID, constantID, _char);
			}
		}

		
		public override float Run ()
		{
			if (newPortraitGraphic == null)
			{
				return 0f;
			}

			if (runtimeChar != null)
			{
				runtimeChar.portraitIcon.ReplaceTexture (newPortraitGraphic);
			}
			else if (playerID >= 0 && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && KickStarter.saveSystem.CurrentPlayerID != playerID)
			{
				// Special case: Player is not in the scene, so manually update their PlayerData
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (playerID);
				if (playerData != null)
				{
					playerData.playerPortraitGraphic = AssetLoader.GetAssetInstanceID (newPortraitGraphic);
				}
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					parameterID = ChooseParameterGUI ("Player ID:", parameters, parameterID, ParameterType.Integer);
					if (parameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("Character:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					_char = null;
				}
				else
				{
					_char = (Char) EditorGUILayout.ObjectField ("Character:", _char, typeof (Char), true);
					
					constantID = FieldToID <Char> (_char, constantID);
					_char = IDToField <Char> (_char, constantID, false);
				}
			}
			
			newPortraitGraphic = (Texture) EditorGUILayout.ObjectField ("New Portrait graphic:", newPortraitGraphic, typeof (Texture), true);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				if (saveScriptsToo)
				{
					if (_char != null && !_char.IsPlayer)
					{
						AddSaveScript <RememberNPC> (_char);
					}
				}

				AssignConstantID <Char> (_char, constantID, parameterID);
			}
		}


		public override string SetLabel ()
		{
			if (isPlayer)
			{
				return "Player";
			}
			else if (_char != null)
			{
				return _char.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0 && playerID < 0)
			{
				if (_char && _char.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>()) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && parameterID < 0) return true;
			return (parameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Character: Switch portrait' Action with key variables already set.</summary>
		 * <param name = "characterToUpdate">The character to update</param>
		 * <param name = "newPortraitGraphic">The texture to assign as the character's portrait graphic</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharPortrait CreateNew (Char characterToUpdate, Texture newPortraitGraphic)
		{
			ActionCharPortrait newAction = CreateNew<ActionCharPortrait> ();
			newAction._char = characterToUpdate;
			newAction.TryAssignConstantID (newAction._char, ref newAction.constantID);
			newAction.newPortraitGraphic = newPortraitGraphic;
			return newAction;
		}
		
	}

}