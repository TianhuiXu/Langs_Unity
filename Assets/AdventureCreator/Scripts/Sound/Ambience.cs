/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Ambience.cs"
 * 
 *	This script handles the playback of Ambience when played using the 'Sound: Play ambience' Action.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script handles the playback of Ambience when played using the 'Sound: Play ambience' Action.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_ambience.html")]
	public class Ambience : Soundtrack
	{

		#region Variables

		private int timeSamplesBackup = -1;

		#endregion


		#region UnityStandards

		protected new void Awake ()
		{
			soundType = SoundType.SFX;
			playWhilePaused = KickStarter.settingsManager.playAmbienceWhilePaused;
			base.Awake ();
		}

		#endregion


		#region PublicFunctions

		public override MainData SaveMainData (MainData mainData)
		{
			mainData.lastAmbienceQueueData = CreateLastSoundtrackString ();
			mainData.ambienceQueueData = CreateTimesampleString ();

			mainData.ambienceTimeSamples = 0;
			mainData.lastAmbienceTimeSamples = LastTimeSamples;

			mainData.ambienceTimeSamples = (timeSamplesBackup >= 0) ? timeSamplesBackup : GetTimeSamplesToSave ();
			timeSamplesBackup = -1;

			mainData.oldAmbienceTimeSamples = CreateOldTimesampleString ();

			return mainData;
		}


		public override void LoadMainData (MainData mainData)
		{
			LoadMainData (mainData.ambienceTimeSamples, mainData.oldAmbienceTimeSamples, mainData.lastAmbienceTimeSamples, mainData.lastAmbienceQueueData, mainData.ambienceQueueData);
		}


		/** Prepares save data that cannot be generated while threading */
		public void PrepareSaveBeforeThreading ()
		{
			timeSamplesBackup = GetTimeSamplesToSave ();
		}

		#endregion


		#region ProtectedFunctions

		protected int GetTimeSamplesToSave ()
		{
			if (GetCurrentTrackID () >= 0)
			{
				MusicStorage musicStorage = GetSoundtrack (GetCurrentTrackID ());
				if (musicStorage != null && musicStorage.audioClip != null && audioSource.clip == musicStorage.audioClip && IsPlaying ())
				{
					return audioSource.timeSamples;
				}
			}
			return 0;
		}

		#endregion


		#region GetSet

		protected override List<MusicStorage> Storages
		{
			get
			{
				return KickStarter.settingsManager.ambienceStorages;
			}
		}


		protected override float LoadFadeTime
		{
			get
			{
				return KickStarter.settingsManager.loadAmbienceFadeTime;
			}
		}


		protected override bool CrossfadeWhenLoading
		{
			get
			{
				return KickStarter.settingsManager.crossfadeAmbienceWhenLoading;
			}
		}


		protected override bool RestartTrackWhenLoading
		{
			get
			{
				return KickStarter.settingsManager.restartAmbienceTrackWhenLoading;
			}
		}

		#endregion

	}

}