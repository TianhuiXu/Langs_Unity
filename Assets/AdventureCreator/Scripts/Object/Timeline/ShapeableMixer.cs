/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ShapeableMixer.cs"
 * 
 *	A PlayableBehaviour that allows for a Shapeable component to change key values
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	/** A PlayableBehaviour that allows for a Shapeable component to change key values */
	internal sealed class ShapeableMixer : PlayableBehaviour
	{

		#region Variables

		private Shapeable _shapeable;
		private int groupID;

		#endregion


		#region PublicFunctions

		public override void OnGraphStop (Playable playable)
		{
			if (_shapeable)
			{
				ShapeGroup shapeGroup = _shapeable.GetGroup (groupID);
				if (shapeGroup != null)
				{
					shapeGroup.ReleaseTimelineOverride ();
				}
			}
		}


		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			base.ProcessFrame (playable, info, playerData);

			if (!Application.isPlaying)
			{
				return;
			}

			if (_shapeable == null)
			{
				_shapeable = playerData as Shapeable;

				if (_shapeable == null)
				{
					GameObject shapeableObject = playerData as GameObject;
					if (shapeableObject)
					{
						_shapeable = shapeableObject.GetComponent<Shapeable> ();
					}
				}
			}

			if (_shapeable)
			{
				int activeInputs = 0;
				ClipInfo clipA = new ClipInfo ();
				ClipInfo clipB = new ClipInfo ();
				
				for (int i=0; i<playable.GetInputCount (); ++i)
				{
					float weight = playable.GetInputWeight (i);
					ScriptPlayable <ShapeablePlayableBehaviour> clip = (ScriptPlayable <ShapeablePlayableBehaviour>) playable.GetInput (i);

					ShapeablePlayableBehaviour shot = clip.GetBehaviour ();
					if (shot != null && 
						shot.IsValid &&
						playable.GetPlayState() == PlayState.Playing &&
						weight > 0.0001f)
					{
						clipA = clipB;

						clipB.weight = weight;
						clipB.localTime = clip.GetTime ();
						clipB.duration = clip.GetDuration ();
						clipB.groupID = shot.groupID;
						clipB.keyID = shot.keyID;
						clipB.intensity = shot.intensity;

						if (++activeInputs == 2)
						{
							break;
						}
					}
				}

				groupID = clipB.groupID;
				bool doBlending = activeInputs >= 2;

				if (groupID != clipA.groupID && clipA.intensity > 0 && clipA.weight > 0f)
				{
					doBlending = false;
					ACDebug.LogWarning ("Mismatching shape group IDs - cannot blend between them in Timeline");
				}
				
				if (doBlending)
				{
					_shapeable.SetTimelineOverride (groupID, clipA.keyID, (int) (clipA.intensity * clipA.weight), clipB.keyID, (int) (clipB.intensity * clipB.weight));
				}
				else
				{
					_shapeable.SetTimelineOverride (groupID, clipB.keyID, (int) (clipB.intensity * clipB.weight));
				}
			}
		}

		#endregion


		#region PrivateStructs

		private struct ClipInfo
		{

			public int groupID, keyID, intensity;
			public float weight;
			public double localTime;
			public double duration;

		}

		#endregion

	}

}

#endif