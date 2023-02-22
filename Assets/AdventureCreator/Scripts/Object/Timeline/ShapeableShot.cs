/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ShapeableShot.cs"
 * 
 *	A PlayableAsset that keeps track of what key settings to use in ShapeableMixer
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	/** A PlayableAsset that keeps track of which transform to face in the HeadTurnMixer */
	public sealed class ShapeableShot : PlayableAsset
	{

		#region Variables

		public int groupID;
		public int keyID;
		[Range (0, 100)] public int intensity = 100;

		#endregion


		#region PublicFunctions

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable<ShapeablePlayableBehaviour>.Create (graph);
			playable.GetBehaviour().groupID = groupID;
			playable.GetBehaviour ().keyID = keyID;
			playable.GetBehaviour ().intensity = intensity;
			return playable;
		}

		#endregion

	}

}

#endif