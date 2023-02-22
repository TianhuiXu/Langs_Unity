/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CharacterAnimationd3DShot.cs"
 * 
 *	A PlayableAsset that keeps track of a 3D character's speed Animator values when controlled by Timeline
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	public class CharacterAnimation3DShot : CharacterAnimationShot
	{

		#region Variables

		[SerializeField] private float moveScaleFactor = 1f;
		[SerializeField] private float turnScaleFactor = 1f;

		#endregion


		#region PublicFunctions

		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable <CharacterAnimation3DBehaviour>.Create (graph);
			var characterAnimation3DBehaviour = playable.GetBehaviour ();

			characterAnimation3DBehaviour.Init (this, moveScaleFactor, turnScaleFactor);

			return playable;	
		}

		#endregion

	}

}

#endif