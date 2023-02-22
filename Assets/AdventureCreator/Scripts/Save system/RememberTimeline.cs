/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberTimeline.cs"
 * 
 *	This script is attached to PlayableDirector objects in the scene
 *	we wish to save (Unity 2017+ only).
 * 
 */

using UnityEngine;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;
#endif
using UnityEngine.Playables;
#if AddressableIsPresent
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	/** Attach this script to PlayableDirector objects you wish to save. */
	[RequireComponent (typeof (PlayableDirector))]
	[AddComponentMenu("Adventure Creator/Save system/Remember Timeline")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_timeline.html")]
	public class RememberTimeline : Remember
	{

		/** If True, the GameObjects bound to the Timeline will be stored in save game files */
		public bool saveBindings;
		/** If True, the Timeline asset assigned in the PlayableDirector's Timeline field will be stored in save game files. */
		public bool saveTimelineAsset;
		/** If True, and the Timeline was not playing when it was saved, it will be evaluated at its playback point - causing the effects of it running at that single frame to be restored */
		public bool evaluateWhenStopped;

		private PlayableDirector playableDirector;


		public override string SaveData ()
		{
			TimelineData timelineData = new TimelineData ();
			timelineData.objectID = constantID;
			timelineData.savePrevented = savePrevented;

			timelineData.isPlaying = (PlayableDirector.state == PlayState.Playing);
			timelineData.currentTime = PlayableDirector.time;
			timelineData.trackObjectData = string.Empty;
			timelineData.timelineAssetID = string.Empty;

			if (PlayableDirector.playableAsset)
			{
				#if !ACIgnoreTimeline
				TimelineAsset timeline = (TimelineAsset) PlayableDirector.playableAsset;

				if (timeline)
				{
					if (saveTimelineAsset)
					{
						timelineData.timelineAssetID = AssetLoader.GetAssetInstanceID (timeline);
					}

					if (saveBindings)
					{
						int[] bindingIDs = new int[timeline.outputTrackCount];
						for (int i=0; i<bindingIDs.Length; i++)
						{
							TrackAsset trackAsset = timeline.GetOutputTrack (i);
							GameObject trackObject = PlayableDirector.GetGenericBinding (trackAsset) as GameObject;
							bindingIDs[i] = 0;
							if (trackObject)
							{
								ConstantID cIDComponent = trackObject.GetComponent <ConstantID>();
								if (cIDComponent)
								{
									bindingIDs[i] = cIDComponent.constantID;
								}
							}
						}

						for (int i=0; i<bindingIDs.Length; i++)
						{
							timelineData.trackObjectData += bindingIDs[i].ToString ();
							if (i < (bindingIDs.Length - 1))
							{
								timelineData.trackObjectData += ",";
							}
						}
					}
				}
				#endif
			}

			return Serializer.SaveScriptData <TimelineData> (timelineData);
		}
		

		public override void LoadData (string stringData)
		{
			TimelineData data = Serializer.LoadScriptData <TimelineData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			#if AddressableIsPresent

			if (saveTimelineAsset && KickStarter.settingsManager.saveAssetReferencesWithAddressables && !string.IsNullOrEmpty (data.timelineAssetID))
			{
				StopAllCoroutines ();
				#if !ACIgnoreTimeline
				StartCoroutine (LoadDataFromAddressable (data));
				#endif
				return;
			}

			#endif

			LoadDataFromResources (data);
		}


		#if AddressableIsPresent && !ACIgnoreTimeline

		private IEnumerator LoadDataFromAddressable (TimelineData data)
		{
			AsyncOperationHandle<TimelineAsset> handle = Addressables.LoadAssetAsync<TimelineAsset> (data.timelineAssetID);
			yield return handle;
			if (handle.Status == AsyncOperationStatus.Succeeded)
			{
				PlayableDirector.playableAsset = handle.Result;
			}
			Addressables.Release (handle);

			LoadRemainingData (data);
		}

		#endif


		private void LoadDataFromResources (TimelineData data)
		{
			#if !ACIgnoreTimeline
			if (PlayableDirector.playableAsset)
			{
				TimelineAsset timeline = (TimelineAsset) PlayableDirector.playableAsset;

				if (timeline)
				{
					if (saveTimelineAsset)
					{
						TimelineAsset _timeline = AssetLoader.RetrieveAsset (timeline, data.timelineAssetID);
						Debug.Log ("Get " + _timeline + " from " + data.timelineAssetID);
						if (_timeline)
						{
							PlayableDirector.playableAsset = _timeline;
							timeline = _timeline;
						}
					}
				}
			}
			#endif

			LoadRemainingData (data);
		}


		private void LoadRemainingData (TimelineData data)
		{
			#if !ACIgnoreTimeline
			if (PlayableDirector.playableAsset)
			{
				TimelineAsset timeline = (TimelineAsset) PlayableDirector.playableAsset;

				if (timeline)
				{
					if (saveBindings && !string.IsNullOrEmpty (data.trackObjectData))
					{
						string[] bindingIDs = data.trackObjectData.Split (","[0]);

						for (int i=0; i<bindingIDs.Length; i++)
						{
							int bindingID = 0;
							if (int.TryParse (bindingIDs[i], out bindingID))
							{
								if (bindingID != 0)
								{
									var track = timeline.GetOutputTrack (i);
									if (track)
									{
										ConstantID savedObject = ConstantID.GetComponent (bindingID, gameObject.scene, true);
										if (savedObject)
										{
											PlayableDirector.SetGenericBinding (track, savedObject.gameObject);
										}
									}
								}
							}
						}
					}
				}
			}
			#endif

			PlayableDirector.time = data.currentTime;
			if (data.isPlaying)
			{
				PlayableDirector.Play ();
			}
			else
			{
				PlayableDirector.Stop ();

				if (evaluateWhenStopped)
				{
					PlayableDirector.Evaluate ();
				}
			}
		}


		private PlayableDirector PlayableDirector
		{
			get
			{
				if (playableDirector == null)
				{
					playableDirector = GetComponent <PlayableDirector>();
				}
				return playableDirector;
			}
		}
		
	}
	

	/** A data container used by the RememberTimeline script. */
	[System.Serializable]
	public class TimelineData : RememberData
	{

		/** True if the Timline is playing */
		public bool isPlaying;
		/** The current time along the Timeline */
		public double currentTime;
		/** Which objects are loaded into the tracks */
		public string trackObjectData;
		/** The Instance ID of the current Timeline asset */
		public string timelineAssetID;

		
		/** The default Constructor. */
		public TimelineData () { }

	}
	
}