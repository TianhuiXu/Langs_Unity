/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SpeechPlayableTrack.cs"
 * 
 *	A TrackAsset used by SpeechPlayableBehaviour
 * 
 */

#if !ACIgnoreTimeline
using UnityEngine.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public enum SpeechTrackPlaybackMode { Natural, ClipDuration };


	/**
	 * A TrackAsset used by SpeechPlayableBehaviour
	 */
	[TrackColor(0.9f, 0.4f, 0.9f)]
	[TrackClipType(typeof(SpeechPlayableClip))]
	public class SpeechTrack : TrackAsset, ITranslatable
	{

		#region Variables

		/** If True, the line is spoken by the Player */
		public bool isPlayerLine;
		/** The ID of the Player, if not the active one */
		public int playerID = -1;
		/** The prefab of the character who is speaking the lines on this track */
		public GameObject speakerObject;
		/** The ConstantID of the speaking character, used to locate the speaker in the scene at runtime */
		public int speakerConstantID;
		/** The playback mode for speech clips played on this track */
		public SpeechTrackPlaybackMode playbackMode = SpeechTrackPlaybackMode.Natural;

		#endregion


		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
		{
			foreach (TimelineClip timelineClip in GetClips ())
			{
				SpeechPlayableClip clip = (SpeechPlayableClip) timelineClip.asset;
				timelineClip.displayName = clip.GetDisplayName ();

				Char speaker = null;
				if (Application.isPlaying)
				{
					if (isPlayerLine)
					{
						speaker = AssignPlayer (playerID);
					}
					else if (speakerConstantID != 0)
					{
						speaker = ConstantID.GetComponent <Char> (speakerConstantID);
					}
				}
				else
				{
					if (isPlayerLine)
					{
						if (KickStarter.settingsManager)
						{
							if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && playerID >= 0)
							{
								PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
								if (playerPrefab != null) speaker = playerPrefab.playerOb;
							}
							else
							{
								speaker = KickStarter.settingsManager.GetDefaultPlayer (false);
							}
						}
					}
					else
					{
						speaker = SpeakerPrefab;
					}
				}

				clip.speechTrackPlaybackMode = playbackMode;
				clip.speaker = speaker;
				clip.isPlayerLine = isPlayerLine;
				clip.playerID = playerID;
				clip.trackInstanceID = GetInstanceID ();
			}

			ScriptPlayable<SpeechPlayableMixer> mixer = ScriptPlayable<SpeechPlayableMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			mixer.GetBehaviour ().trackInstanceID = GetInstanceID ();
			mixer.GetBehaviour ().playbackMode = playbackMode;
			return mixer;
	    }

		#endregion

		
		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			isPlayerLine = CustomGUILayout.Toggle ("Player line?", isPlayerLine, "", "If True, the line is spoken by the active Player");
			if (isPlayerLine)
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerID = ChoosePlayerGUI (playerID);
				}
			}
			else
			{
				// For some reason, dynamically generating an ID number for a Char component destroys the component (?!), so we need to store a GameObject instead and convert to Char in the GUI
				Char speakerPrefab = (speakerObject != null) ? speakerObject.GetComponent <Char>() : null;
				speakerPrefab = (Char) CustomGUILayout.ObjectField <Char> ("Speaker prefab:", speakerPrefab, false, "", "The prefab of the character who is speaking the lines on this track");
				speakerObject = (speakerPrefab != null) ? speakerPrefab.gameObject : null;

				if (speakerObject != null)
				{
					if (speakerObject.GetComponent <ConstantID>() == null || speakerObject.GetComponent <ConstantID>().constantID == 0)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (speakerObject, true);
					}

					if (speakerObject.GetComponent <ConstantID>())
					{
						speakerConstantID = speakerObject.GetComponent <ConstantID>().constantID;
					}

					if (speakerObject.GetComponent <ConstantID>() == null || speakerConstantID == 0)
					{
						EditorGUILayout.HelpBox ("A Constant ID number must be assigned to " + speakerObject + ".  Attach a ConstantID component and check 'Retain in prefab?'", MessageType.Warning);
					}
					else
					{
						CustomGUILayout.BeginVertical ();
						EditorGUILayout.LabelField ("Recorded ConstantID: " + speakerConstantID.ToString (), EditorStyles.miniLabel);
						CustomGUILayout.EndVertical ();
					}
				}
			}

			playbackMode = (SpeechTrackPlaybackMode) CustomGUILayout.EnumPopup ("Playback mode:", playbackMode);

			if (playbackMode == SpeechTrackPlaybackMode.Natural)
			{
				EditorGUILayout.HelpBox ("Speech lines will last as long as the settings in the Speech Manager dictate.", MessageType.Info);
			}
			else if (playbackMode == SpeechTrackPlaybackMode.ClipDuration)
			{
				EditorGUILayout.HelpBox ("Speech lines will last for the duration of their associated Timeline clip.", MessageType.Info);
			}
		}


		protected int ChoosePlayerGUI (int _playerID)
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager == null || settingsManager.playerSwitching == PlayerSwitching.DoNotAllow) return _playerID;

			List<string> labelList = new List<string> ();

			int i = 0;
			int playerNumber = 0;

			labelList.Add ("Active Player");
			
			foreach (PlayerPrefab playerPrefab in settingsManager.players)
			{
				if (playerPrefab.playerOb != null)
				{
					labelList.Add (playerPrefab.ID.ToString () + ": " + playerPrefab.playerOb.name);
				}
				else
				{
					labelList.Add (playerPrefab.ID.ToString () + ": " + "(Undefined prefab)");
				}

				if (playerPrefab.ID == _playerID)
				{
					// Found match
					playerNumber = i + 1;
				}

				i++;
			}

			if (_playerID >= 0)
			{
				if (playerNumber == 0)
				{
					// Wasn't found (item was possibly deleted), so revert to zero
					if (_playerID > 0) Debug.LogWarning ("Previously chosen Player no longer exists!");
					playerNumber = 0;
				}
			}

			playerNumber = CustomGUILayout.Popup ("Player:", playerNumber, labelList.ToArray ());

			if (playerNumber > 0)
			{
				_playerID = settingsManager.players[playerNumber - 1].ID;
			}
			else
			{
				_playerID = -1;
			}
			
			return _playerID;
		}

		#endif

		
		#region ProtectedFunctions

		protected Player AssignPlayer (int _playerID)
		{
			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && _playerID >= 0)
			{
				PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (_playerID);
				if (playerPrefab != null)
				{
					Player _player = playerPrefab.GetSceneInstance ();
					if (_player == null) Debug.LogWarning ("Cannot assign Player with ID = " + _playerID + " because they are not currently in the scene.");
					return _player;
				}
				else
				{
					Debug.LogWarning ("No Player prefab found with ID = " + _playerID);
				}
				return null;
			}
			return KickStarter.player;
		}


		protected SpeechPlayableClip[] GetClipsArray ()
		{
			List<SpeechPlayableClip> clipsList = new List<SpeechPlayableClip> ();
			IEnumerable<TimelineClip> timelineClips = GetClips ();
			foreach (TimelineClip timelineClip in timelineClips)
			{
				if (timelineClip != null && timelineClip.asset is SpeechPlayableClip)
				{
					clipsList.Add (timelineClip.asset as SpeechPlayableClip);
				}
			}

			return clipsList.ToArray ();
		}


		protected SpeechPlayableClip GetClip (int index)
		{
			return GetClipsArray ()[index];
		}

		#endregion


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			string text = GetClip (index).speechPlayableData.messageText;
			return text;
		}


		public int GetTranslationID (int index)
		{
			int lineID = GetClip (index).speechPlayableData.lineID;
			return lineID;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			SpeechPlayableClip clip = GetClip (index);
			if (clip != null)
			{
				clip.speechPlayableData.messageText = updatedText;
			}
		}


		public int GetNumTranslatables ()
		{
			return GetClipsArray ().Length;
		}


		public bool CanTranslate (int index)
		{
			string text = GetClip (index).speechPlayableData.messageText;
			return !string.IsNullOrEmpty (text);
		}


		public bool HasExistingTranslation (int index)
		{
			int lineID = GetClip (index).speechPlayableData.lineID;
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int lineID)
		{
			SpeechPlayableClip clip = GetClip (index);
			clip.speechPlayableData.lineID = lineID;
			UnityEditor.EditorUtility.SetDirty (clip);
		}


		public string GetOwner (int index)
		{
			bool _isPlayer = isPlayerLine;
			if (!_isPlayer && SpeakerPrefab && SpeakerPrefab.IsPlayer)
			{
				_isPlayer = true;
			}

			if (_isPlayer)
			{
				if (isPlayerLine && KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					if (playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						if (playerPrefab != null && playerPrefab.playerOb)
						{
							return playerPrefab.playerOb.name;
						}
					}
				}
				else if (isPlayerLine && KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow && KickStarter.settingsManager.player)
				{
					return KickStarter.settingsManager.player.name;
				}
				else if (!isPlayerLine && SpeakerPrefab)
				{
					return SpeakerPrefab.name;
				}

				return "Player";
			}
			else
			{
				if (SpeakerPrefab)
				{
					return SpeakerPrefab.name;
				}
				else
				{
					return "Narrator";
				}
			}
		}


		public bool OwnerIsPlayer (int index)
		{
			if (isPlayerLine)
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && playerID >= 0)
				{
					return false;
				}
				return true;
			}

			if (SpeakerPrefab && SpeakerPrefab.IsPlayer)
			{
				return true;
			}

			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Speech;
		}

		#endif

		#endregion


		#region GetSet

		protected Char SpeakerPrefab
		{
			get
			{
				if (speakerObject)
				{
					return speakerObject.GetComponent <Char>();
				}
				return null;
			}
		}

		#endregion

	}

}

#endif