/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MainCameraMixer.cs"
 * 
 *	A PlayableBehaviour that allows for the MainCamera to cut to different _Camera instances on a Timeline.  This is adapted from CinemachineMixer.cs, published by Unity Technologies, and all credit goes to its respective authors.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine.Playables;

namespace AC
{

	/**
	 * A PlayableBehaviour that allows for the MainCamera to cut to different _Camera instances on a Timeline.  This is adapted from CinemachineMixer.cs, published by Unity Technologies, and all credit goes to its respective authors.
	 */
	internal sealed class MainCameraMixer : PlayableBehaviour
	{

		#region Variables

		private _Camera lastFrameCamera;
		private bool callCustomEvents;
		private bool setsCameraAfterRunning;

		#endregion


		#region PublicFunctions

		public override void OnGraphStop (Playable playable)
		{
			if (MainCamera)
			{
				lastFrameCamera = null;
				MainCamera.ReleaseTimelineOverride ();

				if (callCustomEvents && lastFrameCamera != KickStarter.mainCamera.attachedCamera)
				{
					KickStarter.eventManager.Call_OnSwitchCamera(lastFrameCamera, KickStarter.mainCamera.attachedCamera, 0f);
				}
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
			float shakeIntensity = 0f;

			for (int i=0; i<playable.GetInputCount (); ++i)
			{
				float weight = playable.GetInputWeight (i);
				ScriptPlayable <MainCameraPlayableBehaviour> clip = (ScriptPlayable <MainCameraPlayableBehaviour>) playable.GetInput (i);

				MainCameraPlayableBehaviour shot = clip.GetBehaviour ();

				if (shot != null)
				{
					callCustomEvents = shot.callCustomEvents;
					setsCameraAfterRunning = shot.setsCameraAfterRunning;
				}

				if (shot != null && 
					shot.IsValid &&
					playable.GetPlayState() == PlayState.Playing &&
					weight > 0.0001f)
				{
					clipA = clipB;

					clipB.camera = shot.gameCamera;
					clipB.weight = weight;
					clipB.localTime = clip.GetTime ();
					clipB.duration = clip.GetDuration ();
					clipB.shakeIntensity = shot.shakeIntensity;

					if (++activeInputs == 2)
					{
						break;
					}
				}
			}
			// Figure out which clip is incoming
			bool incomingIsB = clipB.weight >= 1 || clipB.localTime < clipB.duration / 2;
			if (activeInputs == 2)
			{
				if (clipB.localTime > clipA.localTime)
				{
					incomingIsB = true;
				}
				else if (clipB.localTime < clipA.localTime)
				{
					incomingIsB = false;
				}
				else 
				{
					incomingIsB = clipB.duration >= clipA.duration;
				}
			}

			shakeIntensity = incomingIsB ? clipB.shakeIntensity : clipA.shakeIntensity;

			_Camera cameraA = incomingIsB ? clipA.camera : clipB.camera;
			_Camera cameraB = incomingIsB ? clipB.camera : clipA.camera;
			float camWeightB = incomingIsB ? clipB.weight : 1 - clipB.weight;

			if (cameraB == null)
			{
				cameraB = cameraA;
				cameraA = null;
				camWeightB = 1f - camWeightB;
			}

			if (incomingIsB)
			{
				shakeIntensity = (clipA.shakeIntensity * (1f - camWeightB)) + (clipB.shakeIntensity * camWeightB);
			}
			else
			{
				shakeIntensity = (clipB.shakeIntensity * (1f - camWeightB)) + (clipA.shakeIntensity * camWeightB);
			}

			MainCamera.SetTimelineOverride (cameraA, cameraB, camWeightB, shakeIntensity);

			if (callCustomEvents)
			{
				_Camera thisFrameCamera = (incomingIsB) ? cameraB : cameraA;
				if (thisFrameCamera == null)
				{
					thisFrameCamera = KickStarter.mainCamera.attachedCamera;
				}
				if (thisFrameCamera != lastFrameCamera)
				{
					KickStarter.eventManager.Call_OnSwitchCamera (lastFrameCamera, thisFrameCamera, 0f);
					lastFrameCamera = thisFrameCamera;
				}
			}

			if (setsCameraAfterRunning && incomingIsB && camWeightB >= 1f && cameraB && KickStarter.mainCamera.attachedCamera != cameraB)
			{
				KickStarter.mainCamera.SetGameCamera (cameraB);
			}
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

			public _Camera camera;
			public float weight;
			public float shakeIntensity;
			public double localTime;
			public double duration;

		}

		#endregion

	}

}

#endif