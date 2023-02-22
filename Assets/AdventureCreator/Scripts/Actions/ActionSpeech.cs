/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSpeech.cs"
 * 
 *	This action handles the displaying of messages, and talking of characters.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if AddressableIsPresent
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionSpeech : Action, ITranslatable
	{
		
		public int constantID = 0;
		public int parameterID = -1;
		
		public int messageParameterID = -1;
		
		public bool isPlayer;
		public int playerID = -1;
		
		public Char speaker;
		public string messageText;
		public int lineID = -1;
		public int[] multiLineIDs;
		public bool isBackground = false;
		public bool noAnimation = false;
		public AnimationClip headClip;
		public AnimationClip mouthClip;
		
		public bool play2DHeadAnim = false;
		public string headClip2D = "";
		public int headLayer;
		public bool play2DMouthAnim = false;
		public string mouthClip2D = "";
		public int mouthLayer;
		
		public float waitTimeOffset = 0f;
		protected bool stopAction = false;
		
		protected int splitIndex = 0;
		protected bool splitDelay = false;

		protected Char runtimeSpeaker;
		protected Speech speech;
		protected LocalVariables localVariables;
		protected bool runActionListInBackground;
		protected List<ActionParameter> ownParameters = new List<ActionParameter>();

		protected AudioClip addressableAudio;
		protected TextAsset addressableLipSync;

		public static string[] stringSeparators = new string[] {"\n", "\\n"};

		#if AddressableIsPresent
		protected bool isAwaitingAddressableAudio = false;
		protected bool isAwaitingAddressableLipsync = false;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Dialogue; }}
		public override string Title { get { return "Play speech"; }}
		public override string Description { get { return "Makes a Character talk, or – if no Character is specified – displays a message.Subtitles only appear if they are enabled from the Options menu.A 'thinking' effect can be produced by opting to not play any animation."; }}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (parameters != null) ownParameters = parameters;

			messageText = AssignString (parameters, messageParameterID, messageText);

			if (isPlayer)
			{
				runtimeSpeaker = AssignPlayer (playerID, parameters, parameterID);
			}
			else
			{
				runtimeSpeaker = AssignFile<Char> (parameters, parameterID, constantID, speaker);
			}

			if (runtimeSpeaker && !runtimeSpeaker.gameObject.activeInHierarchy && runtimeSpeaker.displayLineID == -1 && runtimeSpeaker.lineID >= 0)
			{ 
				runtimeSpeaker.displayLineID = runtimeSpeaker.lineID;
			}

			#if AddressableIsPresent
			isAwaitingAddressableAudio = false;
			isAwaitingAddressableLipsync = false;
			addressableAudio = null;
			addressableLipSync = null;
			#endif

			splitIndex = 0;
		}


		#if AddressableIsPresent

		private void OnCompleteLoadAudio (AsyncOperationHandle<AudioClip> obj)
		{
			isAwaitingAddressableAudio = false;
			addressableAudio = obj.Result;

			if (!isAwaitingAddressableLipsync)
			{
				StartSpeech (addressableAudio, addressableLipSync);
			}
		}


		private void OnCompleteLoadLipsync (AsyncOperationHandle<TextAsset> obj)
		{
			isAwaitingAddressableLipsync = false;
			addressableLipSync = obj.Result;

			if (!isAwaitingAddressableAudio)
			{
				StartSpeech (addressableAudio, addressableLipSync);
			}
		}

		#endif


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
				runActionListInBackground = (actionList.actionListType == ActionListType.RunInBackground);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}
		
		
		public override float Run ()
		{
			#if AddressableIsPresent
			if (isAwaitingAddressableAudio || isAwaitingAddressableLipsync)
			{
				return defaultPauseTime;
			}
			#endif

			if (KickStarter.speechManager.referenceSpeechFiles == ReferenceSpeechFiles.ByAddressable && lineID >= 0 && splitIndex == 0)
			{
				#if AddressableIsPresent
				if (!isRunning && StartAddressableSpeech ())
				{
					isRunning = true;
					return defaultPauseTime;
				}
				if (isBackground)
				{
					isRunning = false;
					return 0f;
				}
				#else
				LogWarning ("Cannot use addressables system for speech audio because 'AddressableIsPresent' has not been added as a Scripting Define Symbol.  This can be added in Unity's Player settings.");
				#endif
			}

			if (!isRunning)
			{
				stopAction = false;
				isRunning = true;
				splitDelay = false;
				splitIndex = 0;

				StartSpeech ();

				if (isBackground)
				{
					if (KickStarter.speechManager.separateLines)
					{
						string[] textArray = messageText.Split (stringSeparators, System.StringSplitOptions.None);
						if (textArray != null && textArray.Length > 1)
						{
							LogWarning ("Cannot separate multiple speech lines when 'Play in background?' is checked - will only play '" + textArray[0] + "'");
						}
					}

					isRunning = false;
					return 0f;
				}
				return defaultPauseTime;
			}
			else
			{
				if (stopAction || (speech != null && speech.continueState == Speech.ContinueState.Pending))
				{
					if (speech != null)
					{
						speech.continueState = Speech.ContinueState.Continued;
					}
					isRunning = false;
					stopAction = false;
					return 0;
				}

				if (speech == null || !speech.isAlive)
				{
					if (KickStarter.speechManager.separateLines)
					{
						if (!splitDelay)
						{
							// Begin pause if more lines are present
							splitIndex ++;

							string[] textArray = messageText.Split (stringSeparators, System.StringSplitOptions.None);
								
							if (textArray.Length > splitIndex)
							{
								if (KickStarter.speechManager.separateLinePause > 0f)
								{
									// Still got more to go
									splitDelay = true;
									return KickStarter.speechManager.separateLinePause;
								}
								else
								{
									// Show next line
									splitDelay = false;

									#if AddressableIsPresent
									if (KickStarter.speechManager.referenceSpeechFiles == ReferenceSpeechFiles.ByAddressable)
									{
										StartAddressableSpeech ();
									}
									else
									{
										StartSpeech ();
									}
									#else
									StartSpeech ();
									#endif

									return defaultPauseTime;
								}
							}
							// else finished
						}
						else
						{
							// Show next line
							splitDelay = false;
								
							#if AddressableIsPresent
							if (KickStarter.speechManager.referenceSpeechFiles == ReferenceSpeechFiles.ByAddressable)
							{
								StartAddressableSpeech ();
							}
							else
							{
								StartSpeech ();
							}
							#else
							StartSpeech ();
							#endif

							return defaultPauseTime;
						}
					}

					float totalWaitTimeOffset = waitTimeOffset + KickStarter.speechManager.waitTimeOffset;
					if (totalWaitTimeOffset <= 0f)
					{
						isRunning = false;
						return 0f;
					}
					else
					{
						stopAction = true;
						return totalWaitTimeOffset;
					}
				}
				else
				{
					return defaultPauseTime;
				}
			}
		}
		
		
		public override void Skip ()
		{
			KickStarter.dialog.KillDialog (true, true);

			SpeechLog log = new SpeechLog ();
			log.lineID = lineID;
			log.fullText = messageText;

			if (runtimeSpeaker)
			{
				log.speakerName = runtimeSpeaker.name;
				if (!noAnimation)
				{
					if (runtimeSpeaker.GetAnimEngine () != null)
					{
						runtimeSpeaker.GetAnimEngine ().ActionSpeechSkip (this);
					}
				}
			}

			KickStarter.runtimeVariables.AddToSpeechLog (log);
		}


		public AC.Char Speaker
		{
			get
			{
				if (Application.isPlaying)
				{
					return runtimeSpeaker;
				}
				return speaker;
			}
		}
		
		
		#if UNITY_EDITOR

		public override void ClearIDs ()
		{
			lineID = -1;
		}

		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (lineID > -1)
			{
				if (multiLineIDs != null && multiLineIDs.Length > 0 && AdvGame.GetReferences ().speechManager != null && AdvGame.GetReferences ().speechManager.separateLines)
				{
					string IDs = lineID.ToString ();
					foreach (int multiLineID in multiLineIDs)
					{
						IDs += ", " + multiLineID;
					}

					EditorGUILayout.LabelField ("Speech Manager IDs:", IDs);

				}
				else
				{
					EditorGUILayout.LabelField ("Speech Manager ID:", lineID.ToString ());
				}
			}

			if (Application.isPlaying && runtimeSpeaker == null)
			{
				AssignValues (parameters);
			}

			isPlayer = EditorGUILayout.Toggle ("Player line?", isPlayer);
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
				if (Application.isPlaying)
				{
					if (runtimeSpeaker)
					{
						EditorGUILayout.LabelField ("Speaker: " + runtimeSpeaker.name);
					}
					else
					{
						EditorGUILayout.HelpBox ("The speaker cannot be assigned while the game is running.", MessageType.Info);
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
					}
				}
			}
			
			messageParameterID = Action.ChooseParameterGUI ("Line text:", parameters, messageParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
			if (messageParameterID < 0)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Line text:", GUILayout.Width (65f));
				EditorStyles.textField.wordWrap = true;
				messageText = EditorGUILayout.TextArea (messageText, GUILayout.MaxWidth (400f));
				EditorGUILayout.EndHorizontal ();
			}

			Char _speaker = null;
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					if (parameterID < 0 && playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						_speaker = (playerPrefab != null) ? playerPrefab.playerOb : null;
					}
					else
					{
						_speaker = KickStarter.settingsManager.GetDefaultPlayer ();
					}
				}
				else if (Application.isPlaying)
				{
					_speaker = KickStarter.player;
				}
				else if (AdvGame.GetReferences ().settingsManager)
				{
					_speaker = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
			}
			else
			{
				_speaker = (Application.isPlaying) ? runtimeSpeaker : speaker;
			}

			if (_speaker != null)
			{
				noAnimation = EditorGUILayout.Toggle ("Don't animate speaker?", noAnimation);
				if (!noAnimation)
				{
					if (_speaker.GetAnimEngine ())
					{
						_speaker.GetAnimEngine ().ActionSpeechGUI (this, _speaker);

						#if !AC_ActionListPrefabs
						if (GUI.changed && this) EditorUtility.SetDirty (this);
						#endif
					}
				}
			}
			else if (!isPlayer && parameterID < 0)
			{
				EditorGUILayout.HelpBox ("If no Character is set, this line will be considered to be a Narration.", MessageType.Info);
			}
			
			isBackground = EditorGUILayout.Toggle ("Play in background?", isBackground);
			if (!isBackground)
			{
				waitTimeOffset = EditorGUILayout.Slider ("Wait time offset (s):", waitTimeOffset, 0f, 4f);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <Char> (speaker, constantID, parameterID);
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
				else
				{
					return "Narrator";
				}
			}
			return string.Empty;
		}


		public override bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmended = base.ConvertLocalVariableToGlobal (oldLocalID, newGlobalID);

			string newMessageText = AdvGame.ConvertLocalVariableTokenToGlobal (messageText, oldLocalID, newGlobalID);
			if (messageText != newMessageText && messageParameterID < 0)
			{
				wasAmended = true;
				messageText = newMessageText;
			}
			return wasAmended;
		}


		public override bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool isAffected = base.ConvertGlobalVariableToLocal (oldGlobalID, newLocalID, isCorrectScene);

			string newMessageText = AdvGame.ConvertGlobalVariableTokenToLocal (messageText, oldGlobalID, newLocalID);
			if (messageText != newMessageText && messageParameterID < 0)
			{
				isAffected = true;
				if (isCorrectScene)
				{
					messageText = newMessageText;
				}
			}
			return isAffected;
		}


		public override int GetNumVariableReferences (VariableLocation location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			string tokenText = AdvGame.GetVariableTokenText (location, varID, _variablesConstantID);
			if (!string.IsNullOrEmpty (tokenText) && !string.IsNullOrEmpty (messageText) && messageText.Contains (tokenText) && messageParameterID < 0)
			{
				thisCount ++;
			}
			thisCount += base.GetNumVariableReferences (location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			string oldTokenText = AdvGame.GetVariableTokenText (location, oldVarID, _variablesConstantID);
			if (!string.IsNullOrEmpty (oldTokenText) && !string.IsNullOrEmpty (messageText) && messageText.Contains (oldTokenText) && messageParameterID < 0)
			{
				string newTokenText = AdvGame.GetVariableTokenText (location, newVarID, _variablesConstantID);
				messageText = messageText.Replace (oldTokenText, newTokenText);
				thisCount++;
			}
			thisCount += base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
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
			if (playerID < 0 && parameterID < 0) return true;
			return (parameterID < 0 && playerID == _playerID);
		}

		#endif


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			if (KickStarter.speechManager.separateLines)
			{
				return GetSpeechArray () [index];
			}
			return messageText;
		}


		public int GetTranslationID (int index)
		{
			if (index == 0)
			{
				return lineID;
			}
			else
			{
				return multiLineIDs[index-1];
			}
		}

		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			if (KickStarter.speechManager.separateLines)
			{
				string[] textArray = GetSpeechArray ();
				if (index < textArray.Length)
				{
					textArray[index] = updatedText;
					
					messageText = string.Empty;
					for (int i=0; i<textArray.Length; i++)
					{
						messageText += textArray[i];
						if (i < textArray.Length-1)
						{
							messageText += "\n";
						}
					}
				}
			}
			else
			{
				messageText = updatedText;
			}
		}


		public int GetNumTranslatables ()
		{
			if (KickStarter.speechManager.separateLines)
			{
				string[] messages = GetSpeechArray ();

				if (messages.Length > 1)
				{
					List<int> lineIDs = new List<int>();
					for (int i=1; i<messages.Length; i++)
					{
						if (multiLineIDs != null && multiLineIDs.Length > (i-1))
						{
							lineIDs.Add (multiLineIDs[i-1]);
						}
						else
						{
							lineIDs.Add (-1);
						}
					}
					multiLineIDs = lineIDs.ToArray ();
				}
				else
				{
					multiLineIDs = null;
				}

				return messages.Length;
			}

			multiLineIDs = null;
			return 1;
		}


		public bool HasExistingTranslation (int index)
		{
			if (index == 0)
			{
				return (lineID > -1);
			}
			else
			{
				return (multiLineIDs[index-1] > -1);
			}
		}


		public void SetTranslationID (int index, int _lineID)
		{
			if (index == 0)
			{
				lineID = _lineID;
			}
			else
			{
				multiLineIDs[index-1] = _lineID;
			}
		}


		public virtual string GetOwner (int index)
		{
			bool _isPlayer = isPlayer;
			if (!_isPlayer && speaker != null && speaker.IsPlayer)
			{
				_isPlayer = true;
			}
			
			if (_isPlayer)
			{
				if (isPlayer && KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					if (parameterID < 0 && playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						if (playerPrefab != null && playerPrefab.playerOb != null)
						{
							return playerPrefab.playerOb.name;
						}
					}
				}
				else if (isPlayer && KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow && KickStarter.settingsManager.player)
				{
					return KickStarter.settingsManager.player.name;
				}
				else if (!isPlayer && speaker != null)
				{
					return speaker.name;
				}

				return "Player";
			}
			else
			{
				if (isAssetFile)
				{
					if (!isPlayer && parameterID == -1)
					{
						speaker = IDToField <Char> (speaker, constantID, false);
					}
				}

				if (speaker)
				{
					return speaker.name;
				}
				else
				{
					return "Narrator";
				}
			}
		}


		public virtual bool OwnerIsPlayer (int index)
		{
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && playerID >= 0 && parameterID < 0)
				{
					return false;
				}
				return true;
			}

			if (speaker != null && speaker.IsPlayer)
			{
				return true;
			}

			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Speech;
		}


		public bool CanTranslate (int index)
		{
			if (!string.IsNullOrEmpty (messageText) && messageParameterID < 0)
			{
				return true;
			}
			return false;
		}

		#endif

		#endregion


		protected string[] GetSpeechArray ()
		{
			string _text = messageText.Replace ("\\n", "\n");
			string[] textArray = _text.Split (stringSeparators, System.StringSplitOptions.None);
			return textArray;
		}


		protected bool StartAddressableSpeech ()
		{
			#if AddressableIsPresent

			int _lineID = lineID;
			if (KickStarter.speechManager.separateLines && splitIndex > 0)
			{
				string[] textArray = messageText.Replace ("\\n", "\n").Split (stringSeparators, System.StringSplitOptions.None);
				if (textArray.Length > 1)
				{
					if (multiLineIDs != null && multiLineIDs.Length > (splitIndex-1))
					{
						_lineID = multiLineIDs[splitIndex-1];
					}
					else
					{
						_lineID = -1;
					}
				}
			}

			if (!(isAwaitingAddressableAudio || isAwaitingAddressableLipsync))
			{
				SpeechLine speechLine = KickStarter.speechManager.GetLine (_lineID);
				if (speechLine == null)
				{
					LogWarning ("Could not find speech line with ID = " + _lineID);
					StartSpeech ();
					return false;
				}

				string overrideName = string.Empty;
				if (isPlayer && speechLine.SeparatePlayerAudio () && KickStarter.player)
				{
					overrideName = KickStarter.player.name;
				}

				string filename = speechLine.GetFilename (overrideName);
						
				Addressables.LoadAssetAsync<AudioClip>(filename).Completed += OnCompleteLoadAudio;
				isAwaitingAddressableAudio = true;

				if (KickStarter.speechManager.UseFileBasedLipSyncing ())
				{
					Addressables.LoadAssetAsync<TextAsset>(filename).Completed += OnCompleteLoadLipsync;
					isAwaitingAddressableLipsync = true;
				}

				isRunning = true;
				return true;
			}
			
			#endif

			return false;
		}


		protected void StartSpeech (AudioClip audioClip = null, TextAsset textAsset = null)
		{
			string _text = messageText;
			int _lineID = lineID;
			
			int languageNumber = Options.GetLanguage ();
			_text = KickStarter.runtimeLanguages.GetTranslation (_text, lineID, languageNumber, AC_TextType.Speech);
			
			_text = _text.Replace ("\\n", "\n");

			if (KickStarter.speechManager.separateLines)
			{
				string[] textArray = messageText.Replace ("\\n", "\n").Split (stringSeparators, System.StringSplitOptions.None);
				if (textArray.Length > 1)
				{
					_text = textArray [splitIndex];

					if (splitIndex > 0)
					{
						if (multiLineIDs != null && multiLineIDs.Length > (splitIndex-1))
						{
							_lineID = multiLineIDs[splitIndex-1];
						}
						else
						{
							_lineID = -1;
						}
					}

					_text = KickStarter.runtimeLanguages.GetTranslation (_text, _lineID, languageNumber, AC_TextType.Speech);
				}
			}
			
			if (!string.IsNullOrEmpty (_text))
			{
				_text = AdvGame.ConvertTokens (_text, languageNumber, localVariables, ownParameters);
			
				speech = KickStarter.dialog.StartDialog (runtimeSpeaker, _text, (isBackground || runActionListInBackground), _lineID, noAnimation, false, audioClip, textAsset);

				if (runtimeSpeaker != null && !noAnimation && speech != null)
				{
					if (runtimeSpeaker.GetAnimEngine () != null)
					{
						runtimeSpeaker.GetAnimEngine ().ActionSpeechRun (this);
					}
				}
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Play speech' Action with key variables already set.</summary>
		 * <param name = "charToSpeak">The character to speak</param>
		 * <param name = "subtitleText">What the character says</param>
		 * <param name = "translationID">The line's translation ID number, as generated by the Speech Manager</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the character has finished speaking</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpeech CreateNew (Char charToSpeak, string subtitleText, int translationID = -1, bool waitUntilFinish = true)
		{
			ActionSpeech newAction = CreateNew<ActionSpeech> ();
			newAction.speaker = charToSpeak;
			newAction.TryAssignConstantID (newAction.speaker, ref newAction.constantID);
			newAction.messageText = subtitleText;
			newAction.isBackground = !waitUntilFinish;
			newAction.lineID = translationID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Play speech' Action with key variables already set.</summary>
		 * <param name = "characterToSpeak">The character to speak</param>
		 * <param name = "subtitleText">What the character says</param>
		 * <param name = "translationIDs">The line's translation ID numbers, as generated by the Speech Manager</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the character has finished speaking</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpeech CreateNew (Char characterToSpeak, string subtitleText, int[] translationIDs, bool waitUntilFinish = true)
		{
			ActionSpeech newAction = CreateNew<ActionSpeech> ();
			newAction.speaker = characterToSpeak;
			newAction.TryAssignConstantID (newAction.speaker, ref newAction.constantID);
			newAction.messageText = subtitleText;
			newAction.isBackground = !waitUntilFinish;

			newAction.lineID = -1;
			if (translationIDs != null && translationIDs.Length > 0)
			{
				newAction.multiLineIDs = new int[translationIDs.Length-1];
				for (int i=0; i<translationIDs.Length; i++)
				{
					if (i == 0) newAction.lineID = translationIDs[i];
					else newAction.multiLineIDs[i-1] = translationIDs[i];
				}
			}
			return newAction;
		}

	}
	
}