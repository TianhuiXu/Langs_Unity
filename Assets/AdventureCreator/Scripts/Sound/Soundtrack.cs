using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace AC
{

	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_soundtrack.html")]
	public abstract class Soundtrack : Sound
	{

		#region Variables

		protected List<QueuedSoundtrack> queuedSoundtrack = new List<QueuedSoundtrack>();
		protected List<MusicCrossfade> crossfades = new List<MusicCrossfade>();
		protected List<QueuedSoundtrack> lastQueuedSoundtrack = new List<QueuedSoundtrack>();
		protected List<SoundtrackSample> oldSoundtrackSamples = new List<SoundtrackSample>();
		protected int lastTimeSamples;

		// Delay
		protected float delayTime;
		protected int delayAudioID = -1;
		protected float delayFadeTime;
		protected bool delayLoop;
		protected bool delayResumeIfPlayedBefore;
		protected int delayNewTrackTimeSamples;

		protected bool wasPlayingLastFrame;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			surviveSceneChange = true;

			OnInitialiseScene ();

			queuedSoundtrack.Clear ();
			lastQueuedSoundtrack.Clear ();
		}


		protected override void OnEnable ()
		{
			EventManager.OnRestartGame += OnRestartGame;
			base.OnEnable ();
		}


		protected override void OnDisable ()
		{
			EventManager.OnRestartGame -= OnRestartGame;
			base.OnDisable ();
		}


		public override void _Update ()
		{
			float deltaTime = Time.deltaTime;
			if (gameIsPaused)
			{
				if (soundType == SoundType.Music && KickStarter.settingsManager.playMusicWhilePaused)
				{
					deltaTime = Time.fixedDeltaTime;
				}
				else if (soundType == SoundType.SFX && KickStarter.settingsManager.playAmbienceWhilePaused)
				{
					deltaTime = Time.fixedDeltaTime;
				}
				else
				{
					return;
				}
			}

			foreach (MusicCrossfade crossfade in crossfades)
			{
				if (crossfade)
				{
					crossfade._Update ();
				}
			}

			if (delayAudioID >= 0 && delayTime > 0f)
			{
				delayTime -= deltaTime;

				if (delayTime <= 0f)
				{
					AfterDelay ();
				}
				base._Update ();
			}

			if (queuedSoundtrack.Count > 0 && delayAudioID < 0)
			{
				if (!IsPlaying ())
				{
					ClearSoundtrackSample (queuedSoundtrack[0].trackID);
					queuedSoundtrack.RemoveAt (0);

					if (queuedSoundtrack.Count > 0)
					{
						MusicStorage musicStorage = GetSoundtrack (queuedSoundtrack[0].trackID);
						if (musicStorage != null && musicStorage.audioClip)
						{
							int nextTimeSamples = (queuedSoundtrack[0].doResume) ? GetSoundtrackSample (queuedSoundtrack[0].trackID) : queuedSoundtrack[0].newTimeSamples;

							SetupAudioSourceForSoundtrack (musicStorage);
							Play (musicStorage.audioClip, queuedSoundtrack[0].trackLoop, nextTimeSamples);
						}
					}
				}
				else if (queuedSoundtrack.Count > 1 && delayAudioID < 0)
				{
					QueuedSoundtrack nextSoundtrack = queuedSoundtrack[1];
					if (nextSoundtrack.fadeTime > 0f)
					{
						int nextTimeSamples = (nextSoundtrack.doResume) ? GetSoundtrackSample (nextSoundtrack.trackID) : nextSoundtrack.newTimeSamples;

						// Need to pre-empt next track
						float thresholdProportion = (audioSource.clip.length - nextSoundtrack.fadeTime) / audioSource.clip.length;
						int thresholdSamples = (int) (thresholdProportion * (float) audioSource.clip.samples);

						if (audioSource.timeSamples > thresholdSamples)
						{
							MusicStorage musicStorage = GetSoundtrack (nextSoundtrack.trackID);
							ClearSoundtrackSample (queuedSoundtrack[0].trackID);
							queuedSoundtrack.RemoveAt (0);

							if (nextSoundtrack.isCrossfade)
							{
								CreateNewCrossfade (nextSoundtrack.fadeTime);
								audioSource.clip = musicStorage.audioClip;
								SetupAudioSourceForSoundtrack (musicStorage);
								HandleFadeIn (nextSoundtrack.fadeTime, nextSoundtrack.trackLoop, nextTimeSamples);
							}
							else
							{
								FadeOutThenIn (musicStorage, nextSoundtrack.fadeTime, nextSoundtrack.trackLoop, nextSoundtrack.doResume, nextSoundtrack.newTimeSamples);
							}
						}
					}
				}
				else if (queuedSoundtrack.Count == 1 && delayAudioID < 0 && queuedSoundtrack[0].trackLoop && queuedSoundtrack[0].loopingOverlapTime > 0f)
				{
					// Looping overlap

					// Need to pre-empt next track
					float thresholdProportion = (audioSource.clip.length - queuedSoundtrack[0].loopingOverlapTime) / audioSource.clip.length;
					int thresholdSamples = (int) (thresholdProportion * (float) audioSource.clip.samples);

					if (audioSource.timeSamples > thresholdSamples)
					{
						CreateNewCrossfade (queuedSoundtrack[0].loopingOverlapTime);
						HandleFadeIn (queuedSoundtrack[0].loopingOverlapTime, true, 0);
					}
				}
			}

			base._Update ();

			if (!IsPlaying () && wasPlayingLastFrame)
			{
				KickStarter.eventManager.Call_OnStopSoundtrack (IsMusic, 0f);
			}
			wasPlayingLastFrame = IsPlaying ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Plays a new soundtrack</summary>
		 * <param name = "trackID">The ID number of the track, as generated by the MusicStorage class that stores the AudioClip</para>
		 * <param name = "loop">If True, the new track will be looped</param>
		 * <param name = "isQueued">If True, the track will be queued until the current track has finished playing</param>
		 * <param name = "fadeTime">The fade-in duration, in seconds</param>
		 * <param name = "resumeIfPlayedBefore">If True, and the track has been both played before and stopped before it finished, the track will be resumed</param>
		 * <param name = "newTrackTimeSamples">The timeSamples to play the new track from, if not overridden with resumeIfPlayedBefore</param>
		 * <param name "loopingOverlapTime">The time that the track will overlap itself when looping</param>
		 * <returns>The duration, in seconds, for the new track to begin playing and the previous track transition to end</reuturns>
		 */
		public float Play (int trackID, bool loop, bool isQueued, float fadeTime, bool resumeIfPlayedBefore = false, int newTrackTimeSamples = 0, float loopingOverlapTime = 0f)
		{
			return HandlePlay (trackID, loop, isQueued, fadeTime, false, resumeIfPlayedBefore, newTrackTimeSamples, loopingOverlapTime);
		}


		/**
		 * <summary>Crossfade a new soundtrack</summary>
		 * <param name = "trackID">The ID number of the track, as generated by the MusicStorage class that stores the AudioClip</para>
		 * <param name = "loop">If True, the new track will be looped</param>
		 * <param name = "isQueued">If True, the track will be queued until the current track has finished playing</param>
		 * <param name = "fadeTime">The crossfade duration, in seconds</param>
		 * <param name = "resumeIfPlayedBefore">If True, and the track has been both played before and stopped before it finished, the track will be resumed</param>
		 * <param name = "newTrackTimeSamples">The timeSamples to play the new track from, if not overridden with resumeIfPlayedBefore</param>
		 * <param name "loopingOverlapTime">The time that the track will overlap itself when looping</param>
		 * <returns>The duration, in seconds, for the new track to begin playing and the previous track transition to end</reuturns>
		 */
		public float Crossfade (int trackID, bool loop, bool isQueued, float fadeTime, bool resumeIfPlayedBefore = false, int newTrackTimeSamples = 0, float loopingOverlapTime = 0f)
		{
			return HandlePlay (trackID, loop, isQueued, fadeTime, true, resumeIfPlayedBefore, newTrackTimeSamples, loopingOverlapTime);
		}


		/**
		 * <summary>Resumes the last-played soundtrack queue</summary>
		 * <param name = "fadeTime">The fade-in time in seconds, if greater than zero</param>
		 * <param name = "playFromStart">If True, the track will play from the beginning</param>
		 * <returns>The duration, in seconds, for the new track to begin playing and the previous track transition to end</reuturns>
		 */
		public float ResumeLastQueue (float fadeTime, bool playFromStart)
		{
			if (lastQueuedSoundtrack.Count == 0)
			{
				ACDebug.LogWarning ("Can't resume track - nothing in the queue!", this);
				return 0f;
			}

			bool isAlreadyPlaying = (queuedSoundtrack.Count > 0) ? true : false;
			if (isAlreadyPlaying)
			{
				ACDebug.LogWarning ("Can't resume last stopped track, as a track is already playing.", this);
				return 0f;
			}

			queuedSoundtrack.Clear ();
			foreach (QueuedSoundtrack lastQueueSoundtrack in lastQueuedSoundtrack)
			{
				queuedSoundtrack.Add (new QueuedSoundtrack (lastQueueSoundtrack));
			}

			Resume ((playFromStart) ? 0 : lastTimeSamples, fadeTime);
			return fadeTime;
		}


		/**
		 * <summary>Stops the currently-playing track, and cancels all those in the queue.</summary>
		 * <param name = "fadeTime">The time over which to stop the track</param>
		 * <param name = "storeCurrentIndex">If the current time index of the track will be stored so that it can be resumed later</param>
		 * <returns>The fade time necessary to stop the track</returns>
		 */
		public float StopAll (float fadeTime, bool storeCurrentIndex = true)
		{
			if (queuedSoundtrack.Count == 0 && audioSource != null && !IsPlaying () && !IsCrossfading ())
			{
				return 0f;
			}

			return ForceStopAll (fadeTime, storeCurrentIndex);
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public virtual MainData SaveMainData (MainData mainData)
		{
			return mainData;
		}


		/**
		 * <summary>Updates its own variables from a MainData class.</summary>
		 * <param name = "mainData">The MainData class to load from</param>
		 */
		public virtual void LoadMainData (MainData mainData)
		{}


		/**
		 * The ID number of the currently-playing track.
		 */
		public int GetCurrentTrackID ()
		{
			if (queuedSoundtrack.Count > 0)
			{
				return queuedSoundtrack[0].trackID;
			}
			return -1;
		}


		/**
		 * <summary>Sets the recorded 'time sample' of a track to match the currently-playing audio.  This is useful if you want to blend into a new track at the same position as the current one, via the 'resumeIfPlayedBefore' option in the Play function.</summary>
		 * <param name = "trackID">The ID of the track to sync with the currently-playing audio</param>
		 * <param name = "sampleOffset">An offset value to apply to the track, on top of the current audio's timeSamples</param>
		 */
		public void SyncTrackWithCurrent (int trackID, int sampleOffset = 0)
		{
			int timeSamples = audioSource.timeSamples;
			SetSoundtrackSample (trackID, Mathf.Max (0, timeSamples + sampleOffset));
		}

		#endregion


		#region ProtectedFunctions

		protected bool IsCrossfading ()
		{
			foreach (MusicCrossfade crossfade in crossfades)
			{
				if (crossfade) return true;
			}
			return false;
		}


		protected void CreateNewCrossfade (float fadeTime)
		{
			MusicCrossfade newCrossfade = MusicCrossfade.FadeOut (this, audioSource, fadeTime);
			if (newCrossfade)
			{
				crossfades.Add (newCrossfade);
			}
		}


		protected void EndOthers ()
		{
			if (EndsOthers ())
			{
				Sound[] sounds = FindObjectsOfType (typeof (Sound)) as Sound[];
				foreach (Sound sound in sounds)
				{
					sound.EndOld (soundType, this);
				}
			}
		}


		protected virtual bool EndsOthers ()
		{
			return false;
		}


		protected float HandlePlay (int trackID, bool loop, bool isQueued, float fadeTime, bool isCrossfade, bool resumeIfPlayedBefore, int newTrackTimeSamples = 0, float loopingOverlapTime = 0f)
		{
			MusicStorage musicStorage = GetSoundtrack (trackID);
			if (musicStorage == null || musicStorage.audioClip == null)
			{
				ACDebug.LogWarning ("Cannot play " + name + " - no AudioClip assigned to track " + trackID + "!");
				return 0f;
			}

			if (isQueued && queuedSoundtrack.Count > 0)
			{
				queuedSoundtrack.Add (new QueuedSoundtrack (trackID, loop, fadeTime, isCrossfade, resumeIfPlayedBefore, newTrackTimeSamples, loopingOverlapTime));
				return 0f;
			}
			else
			{
				if (queuedSoundtrack.Count > 0 && queuedSoundtrack[0].trackID == trackID)
				{
					// Already playing
					if (resumeIfPlayedBefore)
					{
						QueuedSoundtrack track = queuedSoundtrack[0];
						track.trackLoop = loop;
						audioSource.loop = loop;
						queuedSoundtrack[0] = track;
					}
					return 0f;
				}
				
				// End other objects
				EndOthers ();

				bool alreadyPlaying = (queuedSoundtrack.Count > 0) ? true : false;
				if (alreadyPlaying)
				{
					StoreSoundtrackSampleByIndex (0);
				}

				if (resumeIfPlayedBefore)
				{
					newTrackTimeSamples = GetSoundtrackSample (trackID);
				}
				
				queuedSoundtrack.Clear ();
				
				if (loop && loopingOverlapTime > 0f)
				{
					queuedSoundtrack.Add (new QueuedSoundtrack (trackID, loop, fadeTime, false, false, 0, loopingOverlapTime));
				}
				else
				{
					queuedSoundtrack.Add (new QueuedSoundtrack (trackID, loop));
				}

				KickStarter.eventManager.Call_OnPlaySoundtrack (trackID, IsMusic, loop, fadeTime, newTrackTimeSamples);

				if (alreadyPlaying)
				{
					if (fadeTime > 0f)
					{
						if (isCrossfade)
						{
							CreateNewCrossfade (fadeTime);

							SetupAudioSourceForSoundtrack (musicStorage);
							audioSource.clip = musicStorage.audioClip;
							HandleFadeIn (fadeTime, loop, newTrackTimeSamples);

							return fadeTime;
						}
						else
						{
							FadeOutThenIn (musicStorage, fadeTime, loop, resumeIfPlayedBefore, newTrackTimeSamples);
							return (fadeTime * 2f);
						}
					}
					else
					{
						Stop ();
						SetupAudioSourceForSoundtrack (musicStorage);
						Play (musicStorage.audioClip, loop, newTrackTimeSamples);
						return 0f;
					}
				}
				else
				{
					SetupAudioSourceForSoundtrack (musicStorage);

					if (fadeTime <= 0f && !gameIsPaused)
					{
						// Prevents volume not correct in first frame of play
						fadeTime = 0.001f;
					}

					if (fadeTime > 0f)
					{
						audioSource.clip = musicStorage.audioClip;
						HandleFadeIn (fadeTime, loop, newTrackTimeSamples);
						return fadeTime;
					}
					else
					{
						Play (musicStorage.audioClip, loop, newTrackTimeSamples);
						return 0f;
					}
				}
			}
		}


		protected float ForceStopAll (float fadeTime, bool storeCurrentIndex = true)
		{
			if (fadeTime <= 0f)
			{
				foreach (MusicCrossfade crossfade in crossfades)
				{
					if (crossfade)
					{ 
						crossfade.Stop ();
					}
				}
			}

			if (storeCurrentIndex)
			{
				StoreSoundtrackSampleByIndex (0);
			}

			delayAudioID = -1;

			ClearSoundtrackQueue ();

			wasPlayingLastFrame = false;
			KickStarter.eventManager.Call_OnStopSoundtrack (IsMusic, fadeTime);

			if (fadeTime > 0f && IsPlaying ())
			{
				FadeOut (fadeTime);
				return fadeTime;
			}
			else
			{
				Stop ();
			}
			return 0f;
		}


		protected void ClearSoundtrackQueue ()
		{
			lastTimeSamples = 0;
			if (queuedSoundtrack != null && queuedSoundtrack.Count > 0)
			{
				MusicStorage musicStorage = GetSoundtrack (queuedSoundtrack[0].trackID);
				if (musicStorage != null && musicStorage.audioClip != null && audioSource.clip == musicStorage.audioClip && IsPlaying ())
				{
					lastTimeSamples = audioSource.timeSamples;
				}
			}

			lastQueuedSoundtrack.Clear ();
			foreach (QueuedSoundtrack soundtrack in queuedSoundtrack)
			{
				lastQueuedSoundtrack.Add (new QueuedSoundtrack (soundtrack));
			}
			queuedSoundtrack.Clear ();
		}


		protected void FadeOutThenIn (MusicStorage musicStorage, float fadeTime, bool loop, bool resumeIfPlayedBefore, int newTrackTimeSamples)
		{
			FadeOut (fadeTime);

			delayTime = fadeTime;
			delayAudioID = musicStorage.ID;
			delayFadeTime = fadeTime;
			delayLoop = loop;
			delayResumeIfPlayedBefore = resumeIfPlayedBefore;
			delayNewTrackTimeSamples = newTrackTimeSamples;
		}


		protected void AfterDelay ()
		{
			if (delayAudioID >= 0)
			{
				delayTime = 0f;

				MusicStorage musicStorage = GetSoundtrack (delayAudioID);
				if (musicStorage != null)
				{
					int timeSamples = (delayResumeIfPlayedBefore) ? GetSoundtrackSample (delayAudioID) : delayNewTrackTimeSamples;

					audioSource.clip = musicStorage.audioClip;
					SetupAudioSourceForSoundtrack (musicStorage);
					FadeIn (delayFadeTime, delayLoop, timeSamples);
				}
			}

			delayAudioID = -1;
		}


		protected void Resume (int _timeSamples, float fadeTime = 0f)
		{
			if (queuedSoundtrack.Count > 0)
			{
				MusicStorage musicStorage = GetSoundtrack (queuedSoundtrack[0].trackID);
				if (musicStorage != null && musicStorage.audioClip)
				{
					audioSource.clip = musicStorage.audioClip;
					SetupAudioSourceForSoundtrack (musicStorage);
					PlayAtPoint (queuedSoundtrack[0].trackLoop, _timeSamples);

					if (fadeTime > 0f)
					{
						HandleFadeIn (fadeTime, queuedSoundtrack[0].trackLoop, _timeSamples);
					}
				}
			}
		}


		protected void HandleFadeIn (float _fadeTime, bool loop, int _timeSamples)
		{
			FadeIn (_fadeTime, loop, _timeSamples);
		}


		protected string CreateTimesampleString ()
		{
			StringBuilder sampleString = new StringBuilder ();
			for (int i=0; i<queuedSoundtrack.Count; i++)
			{
				sampleString.Append (queuedSoundtrack[i].trackID.ToString ());
				sampleString.Append (SaveSystem.colon);
				sampleString.Append ((queuedSoundtrack[i].trackLoop) ? "1" : "0");
				sampleString.Append (SaveSystem.colon);
				sampleString.Append (queuedSoundtrack[i].fadeTime);
				sampleString.Append (SaveSystem.colon);
				sampleString.Append ((queuedSoundtrack[i].isCrossfade) ? "1" : "0");
				sampleString.Append (SaveSystem.colon);
				sampleString.Append (queuedSoundtrack[i].loopingOverlapTime);
			
				if (i < (queuedSoundtrack.Count-1))
				{
					sampleString.Append (SaveSystem.pipe);
				}
			}
			return sampleString.ToString ();
		}


		protected string CreateLastSoundtrackString ()
		{
			StringBuilder lastSoundtrackString = new StringBuilder ();
			for (int i=0; i<lastQueuedSoundtrack.Count; i++)
			{
				lastSoundtrackString.Append (lastQueuedSoundtrack[i].trackID.ToString ());
				lastSoundtrackString.Append (SaveSystem.colon);
				lastSoundtrackString.Append ((lastQueuedSoundtrack[i].trackLoop) ? "1" : "0");
				lastSoundtrackString.Append (SaveSystem.colon);
				lastSoundtrackString.Append (lastQueuedSoundtrack[i].fadeTime);
				lastSoundtrackString.Append (SaveSystem.colon);
				lastSoundtrackString.Append ((lastQueuedSoundtrack[i].isCrossfade) ? "1" : "0");
				lastSoundtrackString.Append (SaveSystem.colon);
				lastSoundtrackString.Append (lastQueuedSoundtrack[i].loopingOverlapTime);
			
				if (i < (lastQueuedSoundtrack.Count-1))
				{
					lastSoundtrackString.Append (SaveSystem.pipe);
				}
			}
			return lastSoundtrackString.ToString ();
		}


		protected string CreateOldTimesampleString ()
		{
			StringBuilder oldTimeSamplesString = new StringBuilder ();
			for (int i=0; i<oldSoundtrackSamples.Count; i++)
			{
				oldTimeSamplesString.Append (oldSoundtrackSamples[i].trackID.ToString ());
				oldTimeSamplesString.Append (SaveSystem.colon);
				oldTimeSamplesString.Append (oldSoundtrackSamples[i].timeSample.ToString ());

				if (i < (oldSoundtrackSamples.Count-1))
				{
					oldTimeSamplesString.Append (SaveSystem.pipe);
				}
			}
			return oldTimeSamplesString.ToString ();
		}


		protected void LoadMainData (int _timeSamples, string _oldTimeSamples, int _lastTimeSamples, string _lastQueueData, string _queueData)
		{
			if (!CrossfadeWhenLoading || LoadFadeTime <= 0f)
			{
				ForceStopAll (0f, false);
			}
			else if (!string.IsNullOrEmpty (_queueData))
			{
				queuedSoundtrack.Clear();
				lastQueuedSoundtrack.Clear();
			}

			if (!string.IsNullOrEmpty (_oldTimeSamples))
			{
				oldSoundtrackSamples.Clear ();
				string[] oldArray = _oldTimeSamples.Split (SaveSystem.pipe[0]);
				foreach (string chunk in oldArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);

					// ID
					int _id = 0;
					int.TryParse (chunkData[0], out _id);

					// TimeSample
					int _timeSample = 0;
					int.TryParse (chunkData[1], out _timeSample);

					oldSoundtrackSamples.Add (new SoundtrackSample (_id, _timeSample));
				}
			}

			lastTimeSamples = _lastTimeSamples;

			if (!string.IsNullOrEmpty (_lastQueueData))
			{
				lastQueuedSoundtrack.Clear ();
				string[] queueArray = _lastQueueData.Split (SaveSystem.pipe[0]);
				foreach (string chunk in queueArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);

					// ID
					int _id = 0;
					int.TryParse (chunkData[0], out _id);

					// Loop
					int _loop = 0;
					int.TryParse (chunkData[1], out _loop);
					bool loopBool = (_loop == 1) ? true : false;

					// FadeTime
					float _fadeTime = 0f;
					float.TryParse (chunkData[2], out _fadeTime);

					// Crossfade
					int _crossfade = 0;
					int.TryParse (chunkData[3], out _crossfade);
					bool crossfadeBool = (_crossfade == 1) ? true : false;

					// Looping overlap time
					float loopingOverlapTime = 0f;
					if (chunkData.Length >= 5)
					{
						float.TryParse (chunkData[4], out loopingOverlapTime);
					}

					lastQueuedSoundtrack.Add (new QueuedSoundtrack (_id, loopBool, _fadeTime, crossfadeBool, false, 0, loopingOverlapTime));
				}
			}

			if (!string.IsNullOrEmpty (_queueData))
			{
				string[] queueArray = _queueData.Split (SaveSystem.pipe[0]);
				foreach (string chunk in queueArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);

					// ID
					int _id = 0;
					int.TryParse (chunkData[0], out _id);

					// Loop
					int _loop = 0;
					int.TryParse (chunkData[1], out _loop);
					bool loopBool = (_loop == 1) ? true : false;

					// FadeTime
					float _fadeTime = 0f;
					float.TryParse (chunkData[2], out _fadeTime);

					// Crossfade
					int _crossfade = 0;
					int.TryParse (chunkData[3], out _crossfade);
					bool crossfadeBool = (_crossfade == 1) ? true : false;

					// Looping overlap time
					float loopingOverlapTime = 0f;
					if (chunkData.Length >= 5)
					{
						float.TryParse (chunkData[4], out loopingOverlapTime);
					}

					queuedSoundtrack.Add (new QueuedSoundtrack (_id, loopBool, _fadeTime, crossfadeBool, false, 0, loopingOverlapTime));
				}

				if (RestartTrackWhenLoading)
				{
					_timeSamples = 0;
				}

				if (CrossfadeWhenLoading && LoadFadeTime > 0f && queuedSoundtrack.Count > 0)
				{
					MusicStorage musicStorage = GetSoundtrack(queuedSoundtrack[0].trackID);
					if (musicStorage != null)
					{
						CreateNewCrossfade (LoadFadeTime);
						audioSource.clip = musicStorage.audioClip;
						SetupAudioSourceForSoundtrack (musicStorage);
						HandleFadeIn (LoadFadeTime, queuedSoundtrack[0].trackLoop, _timeSamples);
					}
				}
				else
				{
					Resume (_timeSamples, LoadFadeTime);
				}
			}
		}


		private void SetupAudioSourceForSoundtrack (MusicStorage musicStorage)
		{
			if (musicStorage.overrideMixerGroup)
			{
				audioSource.outputAudioMixerGroup = musicStorage.overrideMixerGroup;
			}
			else if (soundType == SoundType.Music)
			{
				audioSource.outputAudioMixerGroup = KickStarter.settingsManager.musicMixerGroup;
			}
			else if (soundType == SoundType.SFX)
			{
				audioSource.outputAudioMixerGroup = KickStarter.settingsManager.sfxMixerGroup;
			}
			SetRelativeVolume (musicStorage.relativeVolume);
		}


		protected MusicStorage GetSoundtrack (int ID)
		{
			foreach (MusicStorage storage in Storages)
			{
				if (storage.ID == ID)
				{
					return storage;
				}
			}
			return null;
		}


		protected void SetRelativeVolume (float _relativeVolume)
		{
			relativeVolume = _relativeVolume;
			SetMaxVolume ();
		}


		protected void StoreSoundtrackSampleByIndex (int index)
		{
			if (queuedSoundtrack != null && queuedSoundtrack.Count > index)
			{
				int trackID = queuedSoundtrack[index].trackID;
				MusicStorage musicStorage = GetSoundtrack (trackID);
				if (musicStorage != null && musicStorage.audioClip != null && audioSource.clip == musicStorage.audioClip && IsPlaying ())
				{
					SetSoundtrackSample (trackID, audioSource.timeSamples);
				}
			}
		}
		

		protected int GetSoundtrackSample (int trackID)
		{
			foreach (SoundtrackSample oldSoundtrackSample in oldSoundtrackSamples)
			{
				if (oldSoundtrackSample.trackID == trackID)
				{
					return oldSoundtrackSample.timeSample;
				}
			}
			return 0;
		}


		protected void ClearSoundtrackSample (int trackID)
		{
			foreach (SoundtrackSample soundtrackSample in oldSoundtrackSamples)
			{
				if (soundtrackSample.trackID == trackID)
				{
					oldSoundtrackSamples.Remove (soundtrackSample);
					return;
				}
			}
		}


		protected void SetSoundtrackSample (int trackID, int timeSample)
		{
			ClearSoundtrackSample (trackID);

			SoundtrackSample newSoundtrackSample = new SoundtrackSample (trackID, timeSample);
			oldSoundtrackSamples.Add (newSoundtrackSample);
		}

		#endregion


		#region PrivateFunctions

		private void OnRestartGame ()
		{
			queuedSoundtrack.Clear ();
			lastQueuedSoundtrack.Clear ();
			oldSoundtrackSamples.Clear ();
			crossfades.Clear ();
		}

		#endregion


		#region GetSet

		protected virtual List<MusicStorage> Storages
		{
			get
			{
				return null;
			}
		}


		protected int LastTimeSamples
		{
			get
			{
				return lastTimeSamples;
			}
		}


		protected virtual bool IsMusic
		{
			get
			{
				return false;
			}
		}


		protected virtual float LoadFadeTime
		{
			get
			{
				return 0f;
			}
		}


		protected virtual bool CrossfadeWhenLoading
		{
			get
			{
				return false;
			}
		}
		

		protected virtual bool RestartTrackWhenLoading
		{
			get
			{
				return false;
			}
		}

		#endregion


		#region ProtectedStructs

		protected struct QueuedSoundtrack
		{

			public int trackID;
			public bool trackLoop;
			public float fadeTime;
			public bool isCrossfade;
			public bool doResume;
			public int newTimeSamples;
			public float loopingOverlapTime;


			public QueuedSoundtrack (int _trackID, bool _trackLoop, float _fadeTime = 0f, bool _isCrossfade = false, bool _doResume = false, int _newTimeSamples = 0, float _loopingOverlapTime = 0f)
			{
				trackID = _trackID;
				trackLoop = _trackLoop;
				fadeTime = _fadeTime;
				doResume = _doResume;
				newTimeSamples = _newTimeSamples;
				loopingOverlapTime = _loopingOverlapTime;
				isCrossfade = (fadeTime > 0f) ? _isCrossfade : false;
			}


			public QueuedSoundtrack (QueuedSoundtrack _queuedSoundtrack)
			{
				trackID = _queuedSoundtrack.trackID;
				trackLoop = _queuedSoundtrack.trackLoop;
				fadeTime = _queuedSoundtrack.fadeTime;
				isCrossfade = _queuedSoundtrack.isCrossfade;
				doResume = false;
				newTimeSamples = 0;
				loopingOverlapTime = _queuedSoundtrack.loopingOverlapTime;
			}

		}


		protected struct SoundtrackSample
		{

			public int trackID;
			public int timeSample;


			public SoundtrackSample (int _trackID, int _timeSample)
			{
				trackID = _trackID;
				timeSample = _timeSample;
			}

		}

		#endregion

	}

}