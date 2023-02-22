/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CameraFadeShot.cs"
 * 
 *	A PlayableAsset that keeps track of which texture to overlay in the CameraFadeMixer.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AC
{

	/**
	 * A PlayableAsset that keeps track of which texture to overlay in the CameraFadeMixer.
	 */
	public sealed class CameraFadeShot : PlayableAsset
	{

		#region Variables

		public Texture2D overlayTexture;

		#endregion


		#region PublicFunctions

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<CameraFadePlayableBehaviour>.Create (graph);
			playable.GetBehaviour ().overlayTexture = overlayTexture;
			return playable;
		}

		#endregion

	}

}

#endif