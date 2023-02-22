/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"HeadTurnMixer.cs"
 * 
 *	A PlayableBehaviour that allows for a character to turn their head using IK in Timeline.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	/**
	 * A PlayableBehaviour that allows for a character to turn their head using IK in Timeline.
	 */
	internal sealed class HeadTurnMixer : PlayableBehaviour
	{

		#region Variables

		private Char _character;

		#endregion


		#region PublicFunctions

		public override void OnGraphStop (Playable playable)
		{
			if (_character)
			{
				_character.ReleaseTimelineHeadTurnOverride ();
			}
		}


		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			base.ProcessFrame (playable, info, playerData);

			if (_character == null)
			{
				_character = playerData as Char;

				if (_character == null)
				{
					GameObject characterObject = playerData as GameObject;
					if (characterObject)
					{
						_character = characterObject.GetComponent<AC.Char>();
					}
				}
			}

			if (_character)
			{
				int activeInputs = 0;
				ClipInfo clipA = new ClipInfo ();
				ClipInfo clipB = new ClipInfo ();
				Transform headTurnTarget = null;
				Vector3 headTurnOffset = Vector3.zero;

				for (int i=0; i<playable.GetInputCount (); ++i)
				{
					float weight = playable.GetInputWeight (i);
					ScriptPlayable <HeadTurnPlayableBehaviour> clip = (ScriptPlayable <HeadTurnPlayableBehaviour>) playable.GetInput (i);

					HeadTurnPlayableBehaviour shot = clip.GetBehaviour ();
					if (shot != null && 
						shot.IsValid &&
						playable.GetPlayState() == PlayState.Playing &&
						weight > 0.0001f)
					{
						clipA = clipB;

						clipB.weight = weight;
						clipB.localTime = clip.GetTime ();
						clipB.duration = clip.GetDuration ();
						clipB.headTurnTarget = shot.headTurnTarget;
						clipB.headTurnOffset = shot.headTurnOffset;

						if (++activeInputs == 2)
						{
							break;
						}
					}
				}

				headTurnTarget = (clipB.headTurnTarget) ? clipB.headTurnTarget : clipA.headTurnTarget;
				float _weight = clipB.weight;

				if (clipA.headTurnTarget)
				{
					headTurnOffset = (clipB.headTurnOffset * clipB.weight) + (clipA.headTurnOffset * clipA.weight);
				}
				else
				{
					headTurnOffset = clipB.headTurnOffset;
				}
			
				_character.SetTimelineHeadTurnOverride (headTurnTarget, headTurnOffset, _weight);
			}
		}

		#endregion


		#region PrivateStructs

		private struct ClipInfo
		{

			public Transform headTurnTarget;
			public Vector3 headTurnOffset;
			public float weight;
			public double localTime;
			public double duration;

		}

		#endregion

	}

}

#endif