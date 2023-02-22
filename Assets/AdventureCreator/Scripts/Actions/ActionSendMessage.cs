/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSendMessage.cs"
 * 
 *	This action calls "SendMessage" on a GameObject.
 *	Both standard messages, and custom ones with paremeters, can be sent.
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
	public class ActionSendMessage : Action
	{
		
		public int constantID = 0;
		public int parameterID = -1;

		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;

		public GameObject linkedObject;
		protected GameObject runtimeLinkedObject;

		public bool affectChildren = false;
		
		public MessageToSend messageToSend;
		public enum MessageToSend { TurnOn, TurnOff, Interact, Kill, Custom };

		public int customMessageParameterID = -1;
		public string customMessage;

		public bool sendValue;

		public int customValueParameterID = -1;
		public int customValue;

		public bool ignoreWhenSkipping = false;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Send message"; }}
		public override string Description { get { return "Sends a given message to a GameObject. Can be either a message commonly-used by Adventure Creator (Interact, TurnOn, etc) or a custom one, with an integer argument."; }}
		
		
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

			customMessage = AssignString (parameters, customMessageParameterID, customMessage);
			customValue = AssignInteger (parameters, customValueParameterID, customValue);
		}
		
		
		public override float Run ()
		{
			if (runtimeLinkedObject != null)
			{
				if (messageToSend == MessageToSend.Custom)
				{
					if (affectChildren)
					{
						if (!sendValue)
						{
							runtimeLinkedObject.BroadcastMessage (customMessage, SendMessageOptions.DontRequireReceiver);
						}
						else
						{
							runtimeLinkedObject.BroadcastMessage (customMessage, customValue, SendMessageOptions.DontRequireReceiver);
						}
					}
					else
					{
						if (!sendValue)
						{
							runtimeLinkedObject.SendMessage (customMessage, SendMessageOptions.DontRequireReceiver);
						}
						else
						{
							runtimeLinkedObject.SendMessage (customMessage, customValue, SendMessageOptions.DontRequireReceiver);
						}
					}
				}
				else
				{
					if (affectChildren)
					{
						runtimeLinkedObject.BroadcastMessage (messageToSend.ToString (), SendMessageOptions.DontRequireReceiver);
					}
					else
					{
						runtimeLinkedObject.SendMessage (messageToSend.ToString (), SendMessageOptions.DontRequireReceiver);
					}
				}
			}
			else
			{
				LogWarning ("Cannot send message - no receiving object set!");
			}
			
			return 0f;
		}
		
		
		public override void Skip ()
		{
			if (!ignoreWhenSkipping)
			{
				Run ();
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Send to Player?", isPlayer);
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
				parameterID = Action.ChooseParameterGUI ("Object to affect:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					linkedObject = null;
				}
				else
				{
					linkedObject = (GameObject) EditorGUILayout.ObjectField ("Object to affect:", linkedObject, typeof(GameObject), true);
					
					constantID = FieldToID (linkedObject, constantID);
					linkedObject = IDToField  (linkedObject, constantID, false);
				}
			}

			messageToSend = (MessageToSend) EditorGUILayout.EnumPopup ("Message to send:", messageToSend);
			if (messageToSend == MessageToSend.Custom)
			{
				customMessageParameterID = Action.ChooseParameterGUI ("Method name:", parameters, customMessageParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (customMessageParameterID < 0)
				{
					customMessage = EditorGUILayout.TextField ("Method name:", customMessage);
				}
				
				sendValue = EditorGUILayout.Toggle ("Pass integer to method?", sendValue);
				if (sendValue)
				{
					customValueParameterID = Action.ChooseParameterGUI ("Integer to send:", parameters, customValueParameterID, ParameterType.Integer);
					if (customValueParameterID < 0)
					{
						customValue = EditorGUILayout.IntField ("Integer to send:", customValue);
					}
				}
			}
			
			affectChildren = EditorGUILayout.Toggle ("Send to children too?", affectChildren);
			ignoreWhenSkipping = EditorGUILayout.Toggle ("Ignore when skipping?", ignoreWhenSkipping);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				AssignConstantID (linkedObject, constantID, parameterID);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (linkedObject != null)
			{
				string labelAdd = string.Empty;
				if (messageToSend == MessageToSend.TurnOn)
				{
					labelAdd = "'Turn on' ";
				}
				else if (messageToSend == MessageToSend.TurnOff)
				{
					labelAdd = "'Turn off' ";
				}
				else if (messageToSend == MessageToSend.Interact)
				{
					labelAdd = "'Interact' ";
				}
				else if (messageToSend == MessageToSend.Kill)
				{
					labelAdd = "'Kill' ";
				}
				else
				{
					labelAdd = "'" + customMessage + "' ";
				}
				
				labelAdd += "to " + linkedObject.name;
				return labelAdd;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (linkedObject && linkedObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			if (isPlayer && gameObject && gameObject.GetComponent <Player>()) return true;
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
		 * <summary>Creates a new instance of the 'Object: Send message' Action</summary>
		 * <param name = "receivingObject">The GameObject to send the message to</param>
		 * <param name = "messageName">The message to send</param>
		 * <param name = "affectChildren">If True, the message will be broadcast to all child GameObjects as well</param>
		 * <param name = "ignoreWhenSkipping">If True, the message will not be send if the ActionList is being skipped</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSendMessage CreateNew (GameObject receivingObject, string messageName, bool affectChildren = false, bool ignoreWhenSkipping = false)
		{
			ActionSendMessage newAction = CreateNew<ActionSendMessage> ();
			newAction.linkedObject = receivingObject;
			newAction.TryAssignConstantID (newAction.linkedObject, ref newAction.constantID);
			newAction.messageToSend = MessageToSend.Custom;
			newAction.customMessage = messageName;
			newAction.sendValue = false;
			newAction.affectChildren = affectChildren;
			newAction.ignoreWhenSkipping = ignoreWhenSkipping;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Send message' Action</summary>
		 * <param name = "receivingObject">The GameObject to send the message to</param>
		 * <param name = "messageName">The message to send</param>
		 * <param name = "parameterValue">An integer value to pass as a parameter</param>
		 * <param name = "affectChildren">If True, the message will be broadcast to all child GameObjects as well</param>
		 * <param name = "ignoreWhenSkipping">If True, the message will not be send if the ActionList is being skipped</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSendMessage CreateNew (GameObject receivingObject, string messageName, int parameterValue, bool affectChildren = false, bool ignoreWhenSkipping = false)
		{
			ActionSendMessage newAction = CreateNew<ActionSendMessage> ();
			newAction.linkedObject = receivingObject;
			newAction.TryAssignConstantID (newAction.linkedObject, ref newAction.constantID);
			newAction.messageToSend = MessageToSend.Custom;
			newAction.customMessage = messageName;
			newAction.sendValue = true;
			newAction.customValue = parameterValue;
			newAction.affectChildren = affectChildren;
			newAction.ignoreWhenSkipping = ignoreWhenSkipping;
			return newAction;
		}
		
	}
	
}