#if UNITY_2018_2_OR_NEWER
#define ALLOW_PHYSICAL_CAMERA
#endif

/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"GameCamera25D.cs"
 * 
 *	This GameCamera is fixed, but allows for a background image to be displayed.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A fixed camera that allows for a BackgroundImage to be displayed underneath all scene objects.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_game_camera25_d.html")]
	public class GameCamera25D : _Camera
	{

		#region Variables

		/** The BackgroundImage to display underneath all scene objects. */
		public BackgroundImage backgroundImage;
		/** If True, then the MainCamera will copy its position when the Inspector is viewed */
		public bool isActiveEditor = false;
		/** The offset in perspective from the camera's centre */
		public Vector2 perspectiveOffset = Vector2.zero;

		#endregion


		#region PublicFunctions

		/**
		 * Enables the assigned backgroundImage, disables all other BackgroundImage objects, and ensures MainCamera can view it.
		 */
		public void SetActiveBackground ()
		{
			if (backgroundImage)
			{
				if (BackgroundCamera.Instance && BackgroundImageUI.Instance)
				{
					// Update mask/layers
				}

				// Move background images onto correct layer
				BackgroundImage[] backgroundImages = FindObjectsOfType (typeof (BackgroundImage)) as BackgroundImage[];
				foreach (BackgroundImage image in backgroundImages)
				{
					if (image == backgroundImage)
					{
						image.TurnOn ();
					}
					else
					{
						image.TurnOff ();
					}
				}
				
				// Set MainCamera's Clear Flags
				KickStarter.mainCamera.PrepareForBackground ();
			}

			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				// Preview mode only
				SnapCameraInEditor ();
			}
			#endif
		}


		new public void ResetTarget ()
		{}


		public override Vector2 GetPerspectiveOffset ()
		{
			return perspectiveOffset;
		}


		public override void MoveCameraInstant ()
		{
			SetProjection ();
		}

		#endregion


		#region ProtectedFunctions

		protected void SetProjection ()
		{
			if (MainCamera.AllowProjectionShifting (Camera))
			{
				Camera.projectionMatrix = AdvGame.SetVanishingPoint (Camera, perspectiveOffset);
			}
		}

		#endregion


		#if UNITY_EDITOR

		private void SnapCameraInEditor ()
		{
			GameCamera25D[] camera25Ds = FindObjectsOfType (typeof (GameCamera25D)) as GameCamera25D[];
			foreach (GameCamera25D camera25D in camera25Ds)
			{
				if (camera25D == this)
				{
					isActiveEditor = true;
				}
				else
				{
					camera25D.isActiveEditor = false;
				}
			}
			UpdateCameraSnap ();
		}


		public void UpdateCameraSnap ()
		{
			if (KickStarter.mainCamera)
			{
				Camera targetCamera = GetComponent <Camera>();

				KickStarter.mainCamera.Transform.position = Transform.position;
				KickStarter.mainCamera.Transform.rotation = Transform.rotation;
				KickStarter.mainCamera.Camera.orthographic = targetCamera.orthographic;

				KickStarter.mainCamera.Camera.fieldOfView = targetCamera.fieldOfView;
				KickStarter.mainCamera.Camera.farClipPlane = targetCamera.farClipPlane;
				KickStarter.mainCamera.Camera.nearClipPlane = targetCamera.nearClipPlane;
				KickStarter.mainCamera.Camera.orthographicSize = targetCamera.orthographicSize;

				#if ALLOW_PHYSICAL_CAMERA
				KickStarter.mainCamera.Camera.ResetProjectionMatrix ();
				KickStarter.mainCamera.Camera.usePhysicalProperties = targetCamera.usePhysicalProperties;
				if (targetCamera.usePhysicalProperties)
				{
					KickStarter.mainCamera.Camera.sensorSize = targetCamera.sensorSize;
					KickStarter.mainCamera.Camera.lensShift = targetCamera.lensShift;
				}
				#endif

				if (MainCamera.AllowProjectionShifting (targetCamera))
				{
					KickStarter.CameraMain.projectionMatrix = AdvGame.SetVanishingPoint (KickStarter.CameraMain, perspectiveOffset, true);
				}
			}
		}


		[ContextMenu ("Make active")]
		protected void LocalMakeActive ()
		{
			MakeActive ();
		}

		#endif
	}
		
}