/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CharacterAnimation2DTrack.cs"
 * 
 *	A TrackAsset used by CharacterAnimation2DBehaviour.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine.Timeline;

namespace AC
{

	[TrackClipType (typeof (CharacterAnimation2DShot))]
	#if UNITY_2018_3_OR_NEWER
	[TrackBindingType (typeof (AC.Char), TrackBindingFlags.None)]
	#else
	[TrackBindingType (typeof (AC.Char))]
	#endif
	[TrackColor (0.2f, 0.6f, 0.9f)]
	public class CharacterAnimation2DTrack : TrackAsset
	{}

}

#endif