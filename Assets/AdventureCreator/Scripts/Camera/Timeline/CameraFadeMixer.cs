/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CameraFadeMixer.cs"
 * 
 *	A PlayableBehaviour that allows for a texture to overlay on top of the MainCamera using Timeline.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	/**
	 * A PlayableBehaviour that allows for a texture to overlay on top of the MainCamera using Timeline.
	 */
	internal sealed class CameraFadeMixer : PlayableBehaviour
	{

		#region PublicFunctions

		public override void OnGraphStop (Playable playable)
		{
			if (MainCamera)
			{
				MainCamera.ReleaseTimelineFadeOverride ();
			}
		}


		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			base.ProcessFrame (playable, info, playerData);

			if (MainCamera == null)
			{
				return;
			}

			int activeInputs = 0;
			ClipInfo clipA = new ClipInfo ();
			ClipInfo clipB = new ClipInfo ();
			
			for (int i=0; i<playable.GetInputCount (); ++i)
			{
				float weight = playable.GetInputWeight (i);
				ScriptPlayable <CameraFadePlayableBehaviour> clip = (ScriptPlayable <CameraFadePlayableBehaviour>) playable.GetInput (i);

				CameraFadePlayableBehaviour shot = clip.GetBehaviour ();
				if (shot != null && 
					shot.IsValid &&
					playable.GetPlayState() == PlayState.Playing &&
					weight > 0.0001f)
				{
					clipA = clipB;

					clipB.weight = weight;
					clipB.localTime = clip.GetTime ();
					clipB.duration = clip.GetDuration ();
					clipB.overlayTexture = shot.overlayTexture;

					if (++activeInputs == 2)
					{
						break;
					}
				}
			}

			Texture2D overlayTexture = (clipB.overlayTexture) ? clipB.overlayTexture : clipA.overlayTexture;
			float _weight = clipB.weight;

			MainCamera.SetTimelineFadeOverride (overlayTexture, _weight);
		}

		#endregion


		#region GetSet

		private MainCamera MainCamera
		{
			get
			{
				return KickStarter.mainCamera;
			}
		}

		#endregion


		#region PrivateStructs

		private struct ClipInfo
		{

			public Texture2D overlayTexture;
			public float weight;
			public double localTime;
			public double duration;

		}

		#endregion

	}

}

#endif