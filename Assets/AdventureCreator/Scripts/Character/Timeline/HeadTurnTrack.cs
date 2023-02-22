/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"HeadTurnTrack.cs"
 * 
 *	A TrackAsset used by HeadTurnMixer.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


namespace AC
{

	[System.Serializable]
	[TrackClipType (typeof (HeadTurnShot))]
	[TrackColor (0.1f, 0.1f, 0.73f)]
	#if UNITY_2018_3_OR_NEWER
	[TrackBindingType (typeof (AC.Char), TrackBindingFlags.None)]
	#else
	[TrackBindingType(typeof(AC.Char))]
	#endif
	/**
	 * A TrackAsset used by HeadTurnMixer.
	 */
	public class HeadTurnTrack : TrackAsset
	{

		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
		{
			ScriptPlayable<HeadTurnMixer> mixer = ScriptPlayable<HeadTurnMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			return mixer;
		}

		#endregion

	}

}

#endif