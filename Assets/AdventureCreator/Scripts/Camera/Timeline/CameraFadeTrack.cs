/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CameraFadeTrack.cs"
 * 
 *	A TrackAsset used by CameraFadeMixer.
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;


namespace AC
{

	[System.Serializable]
	[TrackClipType (typeof (CameraFadeShot))]
	[TrackColor (0.1f, 0.1f, 0.73f)]
	/**
	 * A TrackAsset used by CameraFadeMixer.
	 */
	public class CameraFadeTrack : TrackAsset
	{

		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
		{
			ScriptPlayable<CameraFadeMixer> mixer = ScriptPlayable<CameraFadeMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			return mixer;
		}

		#endregion

	}

}

#endif