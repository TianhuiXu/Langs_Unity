/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSpeechWait.cs"
 * 
 *	This Action waits until a particular character has stopped speaking.
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
	public class ActionSpeechWait : Action
	{

		[SerializeField] private SpeechWaitMethod speechWaitMethod = SpeechWaitMethod.Speaker;
		private enum SpeechWaitMethod { Speaker, LineID, AnySpeech };
		public int lineID = 0;
		public int lineIDParameterID = -1;

		public int constantID = 0;
		public int parameterID = -1;

		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;

		public Char speaker;
		protected Char runtimeSpeaker;


		public override ActionCategory Category { get { return ActionCategory.Dialogue; }}
		public override string Title { get { return "Wait for speech"; }}
		public override string Description { get { return "Waits until a particular character has stopped speaking."; }}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			switch (speechWaitMethod)
			{
				case SpeechWaitMethod.Speaker:
					runtimeSpeaker = isPlayer
									? AssignPlayer (playerID, parameters, playerParameterID)
									: AssignFile<Char> (parameters, parameterID, constantID, speaker);
					break;

				case SpeechWaitMethod.LineID:
					lineID = AssignInteger (parameters, lineIDParameterID, lineID);
					break;

				default:
					break;
			}
		}


		public override float Run ()
		{
			if (!isRunning)
			{
				if (speechWaitMethod == SpeechWaitMethod.Speaker && runtimeSpeaker == null)
				{
					Log ("No speaker set - checking for narration");
				}

				if (LineIsPlaying ())
				{
					isRunning = true;
					return defaultPauseTime;
				}
			}
			else
			{
				if (LineIsPlaying ())
				{
					return defaultPauseTime;
				}
				else
				{
					isRunning = false;
				}
			}
			
			return 0f;
		}


		private bool LineIsPlaying ()
		{
			switch (speechWaitMethod)
			{
				case SpeechWaitMethod.Speaker:
					if (runtimeSpeaker == null)
					{
						return KickStarter.dialog.NarrationIsPlaying ();
					}
					return KickStarter.dialog.CharacterIsSpeaking (runtimeSpeaker);

				case SpeechWaitMethod.LineID:
					return KickStarter.dialog.LineIsPlaying (lineID);

				case SpeechWaitMethod.AnySpeech:
					return KickStarter.dialog.IsAnySpeechPlaying ();

				default:
					return false;
			}
		}


		public override void Skip ()
		{
			return;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			speechWaitMethod = (SpeechWaitMethod) EditorGUILayout.EnumPopup ("Reference speech by:", speechWaitMethod);

			switch (speechWaitMethod)
			{
				case SpeechWaitMethod.Speaker:
					{
						isPlayer = EditorGUILayout.Toggle ("Player line?",isPlayer);
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
							parameterID = Action.ChooseParameterGUI ("Speaker:", parameters, parameterID, ParameterType.GameObject);
							if (parameterID >= 0)
							{
								constantID = 0;
								speaker = null;
							}
							else
							{
								speaker = (Char) EditorGUILayout.ObjectField ("Speaker:", speaker, typeof(Char), true);
							
								constantID = FieldToID <Char> (speaker, constantID);
								speaker = IDToField <Char> (speaker, constantID, false);

								if (speaker == null && constantID == 0)
								{
									EditorGUILayout.HelpBox ("If no speaker is assigned, the Action will wait for narration.", MessageType.Info);
								}
							}
						}
						break;
					}

				case SpeechWaitMethod.LineID:
					{
						lineIDParameterID = Action.ChooseParameterGUI ("Line ID:", parameters, lineIDParameterID, ParameterType.Integer);
						if (lineIDParameterID < 0)
						{
							lineID = EditorGUILayout.IntField ("Line ID:", lineID);
						}
						break;
					}

				default:
					break;
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				AssignConstantID<Char> (speaker, constantID, parameterID);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (parameterID == -1)
			{
				if (isPlayer)
				{
					return "Player";
				}
				else if (speaker != null)
				{
					return speaker.gameObject.name;
				}
			}
			return string.Empty;
		}


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Wait for speech' Action</summary>
		 * <param name = "speakingCharacter">The speaking character to wait for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpeechWait CreateNew (AC.Char speakingCharacter)
		{
			ActionSpeechWait newAction = CreateNew<ActionSpeechWait> ();
			newAction.speaker = speakingCharacter;
			newAction.TryAssignConstantID (newAction.speaker, ref newAction.constantID);
			return newAction;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (speaker && speaker.gameObject == gameObject) return true;
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

	}
	
}