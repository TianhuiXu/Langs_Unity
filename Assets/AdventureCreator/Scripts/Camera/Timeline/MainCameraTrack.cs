/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MainCameraTrack.cs"
 * 
 *	A TrackAsset used by MainCameraMixer.  This is adapted from CinemachineTrack.cs, published by Unity Technologies, and all credit goes to its respective authors.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	[TrackClipType (typeof (MainCameraShot))]
	[TrackColor (0.73f, 0.1f, 0.1f)]
	/**
	 * A TrackAsset used by MainCameraMixer.  This is adapted from CinemachineTrack.cs, published by Unity Technologies, and all credit goes to its respective authors.
	 */
	public class MainCameraTrack : TrackAsset
	{

		#region Variables

		[SerializeField] private bool callCustomEvents = false;
		[SerializeField] private bool setsCameraAfterRunning = false;

		#endregion


		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
		{
			foreach (TimelineClip clip in GetClips ()) 
			{
				MainCameraShot shot = (MainCameraShot) clip.asset;
				shot.callCustomEvents = callCustomEvents;
				shot.setsCameraAfterRunning = setsCameraAfterRunning;
			}

			ScriptPlayable<MainCameraMixer> mixer = ScriptPlayable<MainCameraMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			return mixer;
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			setsCameraAfterRunning = CustomGUILayout.Toggle ("Sets camera after running?", setsCameraAfterRunning, string.Empty, "If True, the MainCamera's active camera will be updated with each camera shot, causing it to remain active once the Timeline ends");
			callCustomEvents = CustomGUILayout.Toggle ("Calls custom events?", callCustomEvents, string.Empty, "If True, OnCameraSwitch events will be fired whenever there is a new camera shot.");
			if (callCustomEvents)
			{
				EditorGUILayout.HelpBox ("The OnCameraSwitch event's transition time will always be zero.", MessageType.Info);
			}
		}

		#endif

	}

}

#endif