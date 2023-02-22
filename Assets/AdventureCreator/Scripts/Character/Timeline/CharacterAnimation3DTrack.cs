/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CharacterAnimation3DTrack.cs"
 * 
 *	A TrackAsset used by CharacterAnimation3DBehaviour.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine.Timeline;

namespace AC
{

	[TrackClipType (typeof (CharacterAnimation3DShot))]
	#if UNITY_2018_3_OR_NEWER
	[TrackBindingType (typeof (AC.Char), TrackBindingFlags.None)]
	#else
	[TrackBindingType (typeof (AC.Char))]
	#endif
	[TrackColor (0.2f, 0.6f, 0.9f)]
	public class CharacterAnimation3DTrack : TrackAsset
	{}

}

#endif