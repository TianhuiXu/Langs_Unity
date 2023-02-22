/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ShapeableTrack.cs"
 * 
 *	A TrackAsset used by ShapeableMixer.
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;

namespace AC
{

	[System.Serializable]
	[TrackClipType (typeof (ShapeableShot))]
	[TrackColor (0.73f, 0.5f, 0.1f)]
	#if UNITY_2018_3_OR_NEWER
	[TrackBindingType (typeof (Shapeable), TrackBindingFlags.None)]
	#else
	[TrackBindingType (typeof (Shapeable))]
	#endif
	/** A TrackAsset used by ShapeableMixer. */
	public class ShapeableTrack : TrackAsset
	{

		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
		{
			ScriptPlayable<ShapeableMixer> mixer = ScriptPlayable<ShapeableMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			return mixer;
		}

		#endregion

	}

}

#endif