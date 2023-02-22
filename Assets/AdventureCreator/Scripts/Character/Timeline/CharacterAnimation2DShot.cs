/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CharacterAnimation2DShot.cs"
 * 
 *	A PlayableAsset that keeps track of what direction a 2D character should face when controlled by Timeline
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class CharacterAnimation2DShot : CharacterAnimationShot
	{

		#region Variables

		[SerializeField] protected bool forceDirection = false;
		[SerializeField] protected CharDirection charDirection = CharDirection.Down;
		[SerializeField] protected bool turnInstantly = false;
		[SerializeField] protected PathSpeed moveSpeed = PathSpeed.Walk;

		#endregion


		#region PublicFunctions

		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			moveSpeed = (PathSpeed) EditorGUILayout.EnumPopup ("Animation when moving:", moveSpeed);
			turnInstantly = EditorGUILayout.Toggle ("Turning is instant?", turnInstantly);
			forceDirection = EditorGUILayout.Toggle ("Face fixed direction?", forceDirection);
			if (forceDirection)
			{
				charDirection = (CharDirection) EditorGUILayout.EnumPopup ("Direction:", charDirection);
			}

			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox ("This track type does not support live previewing.", MessageType.Info);
		}

		#endif
	 
		public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
		{
			var playable = ScriptPlayable <CharacterAnimation2DBehaviour>.Create (graph);
			var characterAnimation2DBehaviour = playable.GetBehaviour ();

			characterAnimation2DBehaviour.Init (moveSpeed, turnInstantly, forceDirection, charDirection, this);

			return playable;	
		}

		#endregion

	}

}

#endif