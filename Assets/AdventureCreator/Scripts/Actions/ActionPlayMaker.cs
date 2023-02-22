/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionPlayMaker.cs"
 * 
 *	This action interacts with the popular
 *	PlayMaker FSM-manager.
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
	public class ActionPlayMaker : Action
	{

		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;

		public int constantID = 0;
		public int parameterID = -1;
		public GameObject linkedObject;
		protected GameObject runtimeLinkedObject;

		public string fsmName;
		public int fsmNameParameterID = -1;
		public string eventName;
		public int eventNameParameterID = -1;


		public override ActionCategory Category { get { return ActionCategory.ThirdParty; }}
		public override string Title { get { return "PlayMaker"; }}
		public override string Description { get { return "Calls a specified Event within a PlayMaker FSM. Note that PlayMaker is a separate Unity Asset, and the 'PlayMakerIsPresent' preprocessor must be defined for this to work."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				Player player = AssignPlayer (playerID, parameters, playerParameterID);
				runtimeLinkedObject = (player != null) ? player.gameObject : null;
			}
			else
			{
				runtimeLinkedObject = AssignFile (parameters, parameterID, constantID, linkedObject);
			}

			fsmName = AssignString (parameters, fsmNameParameterID, fsmName);
			eventName = AssignString (parameters, eventNameParameterID, eventName);
		}


		public override float Run ()
		{
			if (isPlayer && KickStarter.player == null)
			{
				LogWarning ("Cannot use Player's FSM since no Player was found!");
			}

			if (runtimeLinkedObject != null && !string.IsNullOrEmpty (eventName))
			{
				if (fsmName != "")
				{
					PlayMakerIntegration.CallEvent (runtimeLinkedObject, eventName, fsmName);
				}
				else
				{
					PlayMakerIntegration.CallEvent (runtimeLinkedObject, eventName);
				}
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (PlayMakerIntegration.IsDefinePresent ())
			{
				isPlayer = EditorGUILayout.Toggle ("Use Player's FSM?", isPlayer);
				if (isPlayer)
				{
					if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
					{
						playerParameterID = ChooseParameterGUI ("Player ID:", parameters, playerParameterID, ParameterType.Integer);
						if (playerParameterID < 0)
							playerID = ChoosePlayerGUI (playerID, true);
					}
				}
				else
				{
					parameterID = ChooseParameterGUI ("Playmaker FSM:", parameters, parameterID, ParameterType.GameObject);
					if (parameterID >= 0)
					{
						constantID = 0;
						linkedObject = null;
					}
					else
					{
						linkedObject = (GameObject) EditorGUILayout.ObjectField ("Playmaker FSM:", linkedObject, typeof (GameObject), true);
						
						constantID = FieldToID (linkedObject, constantID);
						linkedObject = IDToField (linkedObject, constantID, false);
					}
				}

				fsmNameParameterID = Action.ChooseParameterGUI ("FSM to call (optional):", parameters, fsmNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (fsmNameParameterID < 0)
				{
					fsmName = EditorGUILayout.TextField ("FSM to call (optional):", fsmName);
				}
				eventNameParameterID = Action.ChooseParameterGUI ("Event to call:", parameters, eventNameParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (eventNameParameterID < 0)
				{
					eventName = EditorGUILayout.TextField ("Event to call:", eventName);
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("The 'PlayMakerIsPresent' Scripting Define Symbol must be listed in the\nPlayer Settings. Please set it from Edit -> Project Settings -> Player", MessageType.Warning);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				AssignConstantID (linkedObject, constantID, parameterID);
			}
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0 && !isPlayer)
			{
				if (linkedObject && linkedObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && playerParameterID < 0) return true;
			return (playerParameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Third Party: PlayMaker' Action</summary>
		 * <param name = "playMakerFSM">The GameObject with the Playmaker FSM to trigger</param>
		 * <param name = "eventToCall">The name of the event to trigger</param>
		 * <param name = "fsmName">The name of the FSM to call</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPlayMaker CreateNew (GameObject playmakerFSM, string eventToCall, string fsmName = "")
		{
			ActionPlayMaker newAction = CreateNew<ActionPlayMaker> ();
			newAction.linkedObject = playmakerFSM;
			newAction.TryAssignConstantID (newAction.linkedObject, ref newAction.constantID);
			newAction.eventName = eventToCall;
			newAction.fsmName = fsmName;
			return newAction;
		}

	}

}