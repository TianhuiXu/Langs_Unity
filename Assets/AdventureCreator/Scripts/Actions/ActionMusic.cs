/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMusic.cs"
 * 
 *	This action can be used to play, fade or queue music clips.
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
	public class ActionMusic : Action
	{

		public int trackID;
		public int trackIDParameterID = -1;

		public float fadeTime;
		public int fadeTimeParameterID = -1;

		public bool loop;
		public bool isQueued;

		public bool resumeFromStart = true;
		public bool resumeIfPlayedBefore = false;

		public bool willWaitComplete;
		public MusicAction musicAction;

		protected Music music;
		public float loopingOverlapTime = 0f;

		
		public override ActionCategory Category { get { return ActionCategory.Sound; }}
		public override string Title { get { return "Play music"; }}
		public override string Description { get { return "Plays or queues music clips."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			trackID = AssignInteger (parameters, trackIDParameterID, trackID);
			fadeTime = AssignFloat (parameters, fadeTimeParameterID, fadeTime);

			music = KickStarter.stateHandler.GetMusicEngine ();
		}
		
		
		public override float Run ()
		{
			if (music == null) return 0f;

			if (!isRunning)
			{
				isRunning = true;
				float waitTime = Perform (fadeTime);

				if (CanWaitComplete () && willWaitComplete)
				{
					return defaultPauseTime;
				}

				if (willWait && waitTime > 0f && !isQueued)
				{
					return (waitTime);
				}
			}
			else
			{
				if (CanWaitComplete () && willWaitComplete && music.GetCurrentTrackID () == trackID && music.IsPlaying ())
				{
					return defaultPauseTime;
				}

				isRunning = false;
			}
			return 0f;
		}


		public override void Skip ()
		{
			if (music == null) return;	
			Perform (0f);
		}


		protected bool CanWaitComplete ()
		{
			return (!loop && !isQueued && (musicAction == MusicAction.Play || musicAction == MusicAction.Crossfade));
		}


		protected float Perform (float _time)
		{
			switch (musicAction)
			{
				case MusicAction.Play:
					return music.Play (trackID, loop, isQueued, _time, resumeIfPlayedBefore, 0, loopingOverlapTime);
				
				case MusicAction.Crossfade:
					return music.Crossfade (trackID, loop, isQueued, _time, resumeIfPlayedBefore, 0, loopingOverlapTime);
			
				case MusicAction.Stop:
					return music.StopAll (_time);
	
				case MusicAction.ResumeLastStopped:
					return music.ResumeLastQueue (_time, resumeFromStart);

				default:
					return 0f;
			}
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (AdvGame.GetReferences ().settingsManager != null)
			{
				SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;

				if (GUILayout.Button ("Music Storage window"))
				{
					MusicStorageWindow.Init ();
				}
				
				if (settingsManager.musicStorages.Count == 0)
				{
					EditorGUILayout.HelpBox ("Before a track can be selected, it must be added to the Music Storage window.", MessageType.Info);
					return;
				}

				musicAction = (MusicAction) EditorGUILayout.EnumPopup ("Method:", (MusicAction) musicAction);

				string fadeLabel = "Transition time (s):";
				if (musicAction == MusicAction.Play || musicAction == MusicAction.Crossfade)
				{
					GetTrackIndex (settingsManager.musicStorages.ToArray (), parameters);
				
					loop = EditorGUILayout.Toggle ("Loop?", loop);
					isQueued = EditorGUILayout.Toggle ("Queue?", isQueued);
					resumeIfPlayedBefore = EditorGUILayout.Toggle ("Resume if played before?", resumeIfPlayedBefore);
				}
				else if (musicAction == MusicAction.Stop)
				{
					fadeLabel = "Fade-out time (s):";
				}
				else if (musicAction == MusicAction.ResumeLastStopped)
				{
					resumeFromStart = EditorGUILayout.Toggle ("Restart track?", resumeFromStart);
				}

				if (CanWaitComplete ())
				{
					willWaitComplete = EditorGUILayout.Toggle ("Wait until track completes?", willWaitComplete);
				}

				fadeTimeParameterID = Action.ChooseParameterGUI (fadeLabel, parameters, fadeTimeParameterID, ParameterType.Float);
				if (fadeTimeParameterID < 0)
				{
					fadeTime = EditorGUILayout.Slider (fadeLabel, fadeTime, 0f, 10f);
				}

				if ((musicAction == MusicAction.Play || musicAction == MusicAction.Crossfade) && loop)
				{
					loopingOverlapTime = EditorGUILayout.Slider ("Loop overlap time (s):", loopingOverlapTime, 0f, 10f);
				}

				if (!CanWaitComplete () || !willWaitComplete)
				{
					if (fadeTime > 0f && !isQueued)
					{
						willWait = EditorGUILayout.Toggle ("Wait until transition ends?", willWait);
					}
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be defined for this Action to function correctly. Please go to your Game Window and assign one.", MessageType.Warning);
			}
		}


		protected int GetTrackIndex (MusicStorage[] musicStorages, List<ActionParameter> parameters, bool showGUI = true)
		{
			int trackIndex = -1;
			List<string> labelList = new List<string>();

			for (int i=0; i<musicStorages.Length; i++)
			{
				string label = musicStorages[i].Label;
				if (label == "Untitled") label = musicStorages[i].ID + " - " + label;
				labelList.Add (label);
				if (musicStorages[i].ID == trackID)
				{
					trackIndex = i;
				}
			}

			if (musicStorages.Length == 0)
			{
				labelList.Add ("(None set)");
			}

			if (showGUI)
			{
				trackIDParameterID = Action.ChooseParameterGUI ("Music track ID:", parameters, trackIDParameterID, ParameterType.Integer);
				if (trackIDParameterID < 0)
				{
					trackIndex = Mathf.Max (trackIndex, 0);
					trackIndex = EditorGUILayout.Popup ("Music track:", trackIndex, labelList.ToArray());
				}
			}

			if (trackIndex >= 0 && trackIndex < musicStorages.Length)
			{
				trackID = musicStorages[trackIndex].ID;
			}
			else
			{
				trackID = 0;
			}
			return trackIndex;
		}
		
		
		public override string SetLabel ()
		{
			string labelAdd = musicAction.ToString ();
			if (musicAction == MusicAction.Play &&
			    AdvGame.GetReferences ().settingsManager != null &&
			    AdvGame.GetReferences ().settingsManager.musicStorages != null)
			{
				int trackIndex = GetTrackIndex (AdvGame.GetReferences ().settingsManager.musicStorages.ToArray (), null, false);
				if (trackIndex >= 0 && trackIndex < AdvGame.GetReferences ().settingsManager.musicStorages.Count)
				{
					AudioClip clip = AdvGame.GetReferences ().settingsManager.musicStorages[trackIndex].audioClip;
					if (clip != null)
					{
						labelAdd += " " + clip.name.ToString ();
					}
				}
			}

			return labelAdd;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Sound: Play music' Action, set to play a new track</summary>
		 * <param name = "trackID">The music track ID</param>
		 * <param name = "loop">If True, then the new track will be looped</param>
		 * <param name = "addToQueue">If True, then the new track will be added to the current queue, as opposed to played immediately</param>
		 * <param name = "transitionTime">The time, in seconds, over which to start fully playing the new track</param>
		 * <param name = "doCrossfade">If True, and transitionTime > 0, then the new track will crossfade with any existing track - as opposed to fading out then fading in separately</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMusic CreateNew_Play (int trackID, bool loop = true, bool addToQueue = false, float transitionTime = 0f, bool doCrossfade = false, bool waitUntilFinish = false)
		{
			ActionMusic newAction = CreateNew<ActionMusic> ();
			newAction.musicAction = doCrossfade ? MusicAction.Crossfade : MusicAction.Play;
			newAction.trackID = trackID;
			newAction.loop = loop;
			newAction.isQueued = addToQueue;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Sound: Play ambience' Action, set to stop the current track</summary>
		 * <param name = "transitionTime">The time, in seconds, over which to fade out the track</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMusic CreateNew_Stop (float transitionTime = 0f, bool waitUntilFinish = false)
		{
			ActionMusic newAction = CreateNew<ActionMusic> ();
			newAction.musicAction = MusicAction.Stop;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Sound: Play ambience' Action, set to resume the last-played track</summary>
		 * <param name = "transitionTime">The time, in seconds, over which to start fully playing the track</param>
		 * <param name = "doRestart">If True, the track will play from the beginning</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMusic CreateNew_ResumeLastTrack (float transitionTime = 0f, bool doRestart = false, bool waitUntilFinish = false)
		{
			ActionMusic newAction = CreateNew<ActionMusic> ();
			newAction.musicAction = MusicAction.ResumeLastStopped;
			newAction.fadeTime = transitionTime;
			newAction.resumeFromStart = doRestart;
			newAction.willWaitComplete = waitUntilFinish;
			return newAction;
		}
		
	}
	
}