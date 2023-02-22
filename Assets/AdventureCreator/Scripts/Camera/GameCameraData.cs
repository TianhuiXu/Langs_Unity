/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"GameCameraData.cs"
 * 
 *	This is a container class for the MainCamera's active camera data
 * 
 */

#if UNITY_2018_2_OR_NEWER
#define ALLOW_PHYSICAL_CAMERA
#endif

using UnityEngine;

namespace AC
{

	/** This is a container class for the MainCamera's active camera data. This is used to determine the MainCamera's transform and camera data, based on both the active camera, and the transition from the previous camera. */
	public class GameCameraData
	{

		#region Variables

		/** The camera position */
		public Vector3 position { get; private set; }
		/** The camera rotation */
		public Quaternion rotation { get; private set; }
		/** Whether the camera is orthographic or not */
		public bool isOrthographic { get; private set; }
		/** The camera's field of view */
		public float fieldOfView { get; private set; }
		/** The camera's orthographic size */
		public float orthographicSize { get; private set; }
		/** The camera's focal distance */
		public float focalDistance { get; private set; }

		/** If True, the camera is 2D */
		public bool is2D { get; private set; }
		/** The camera's perspective offset, if is2D = True */
		public Vector2 perspectiveOffset { get; private set; }

		#if ALLOW_PHYSICAL_CAMERA
		/** If True, the camera is a Physical Camera */
		public bool usePhysicalProperties { get; private set; }
		/** The camera's sensor size, if a Physical Camera */
		public Vector2 sensorSize { get; private set; }
		/** The camera's lens shift, if a Physical Camera */
		public Vector2 lensShift { get; private set; }
		#endif

		#endregion


		#region Constructors

		/** The default Constructor */
		public GameCameraData () {}


		/** A Constructor that generates data based on the MainCamera's current state */
		public GameCameraData (MainCamera mainCamera)
		{
			position = mainCamera.Transform.position;
			rotation = mainCamera.Transform.rotation;
			fieldOfView = mainCamera.Camera.fieldOfView;
			isOrthographic = mainCamera.Camera.orthographic;
			orthographicSize = mainCamera.Camera.orthographicSize;
			focalDistance = mainCamera.GetFocalDistance ();

			is2D = false;
			perspectiveOffset = Vector2.zero;

			#if ALLOW_PHYSICAL_CAMERA
			usePhysicalProperties = mainCamera.Camera.usePhysicalProperties;
			sensorSize = mainCamera.Camera.sensorSize;
			lensShift = mainCamera.Camera.lensShift;
			#endif
		}


		/** A Constructor that generates data based on a _Camera's current state */
		public GameCameraData (_Camera _camera)
		{
			position = _camera.CameraTransform.position;
			rotation = _camera.CameraTransform.rotation;

			is2D = _camera.Is2D ();
			Vector2 cursorOffset = _camera.CreateRotationOffset ();

			if (is2D)
			{
				if (_camera.Camera.orthographic)
				{
					position += (Vector3) cursorOffset;
				}
			}
			else
			{
				if (_camera.Camera.orthographic || _camera.CursorOffsetForcesTranslation)
				{
					position += (_camera.Transform.right * cursorOffset.x) + (_camera.Transform.up * cursorOffset.y);
				}
				else
				{
					rotation *= Quaternion.Euler (5f * new Vector3 (-cursorOffset.y, cursorOffset.x, 0f));
				}
			}

			fieldOfView = _camera.Camera.fieldOfView;
			isOrthographic = _camera.Camera.orthographic;
			orthographicSize = _camera.Camera.orthographicSize;
			focalDistance = _camera.focalDistance;

			perspectiveOffset = (is2D)
								? _camera.GetPerspectiveOffset ()
								: Vector2.zero;

			if (is2D && !_camera.Camera.orthographic)
			{
				perspectiveOffset += cursorOffset;
			}

			#if ALLOW_PHYSICAL_CAMERA
			usePhysicalProperties = _camera.Camera.usePhysicalProperties;
			sensorSize = _camera.Camera.sensorSize;
			lensShift = _camera.Camera.lensShift;
			#endif
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Creates a new class that mixes its values with another class</summary>
		 * <param name="otherData">The other data to mix with</param>
		 * <param name="otherDataWeight">The blending weight of the other data</param>
		 * <param name="slerpRotation">If True, rotation is Slerped, not Lerped</param>
		 * <returns>The mixed class</returns>
		 */
		public GameCameraData CreateMix (GameCameraData otherData, float otherDataWeight, bool slerpRotation = false)
		{
			if (otherDataWeight <= 0f)
			{
				return this;
			}

			if (otherDataWeight >= 1f)
			{
				return otherData;
			}

			GameCameraData mixData = new GameCameraData ();

			mixData.is2D = otherData.is2D;

			if (mixData.is2D)
			{
				float offsetX = AdvGame.Lerp (perspectiveOffset.x, otherData.perspectiveOffset.x, otherDataWeight);
				float offsetY = AdvGame.Lerp (perspectiveOffset.y, otherData.perspectiveOffset.y, otherDataWeight);

				mixData.perspectiveOffset = new Vector2 (offsetX, offsetY);
			}

			mixData.position = Vector3.Lerp (position, otherData.position, otherDataWeight);
			mixData.rotation = (slerpRotation)
								? Quaternion.Lerp (rotation, otherData.rotation, otherDataWeight)
								: Quaternion.Slerp (rotation, otherData.rotation, otherDataWeight);

			mixData.isOrthographic = otherData.isOrthographic;
			mixData.fieldOfView = Mathf.Lerp (fieldOfView, otherData.fieldOfView, otherDataWeight);
			mixData.orthographicSize = Mathf.Lerp (orthographicSize, otherData.orthographicSize, otherDataWeight);
			mixData.focalDistance = Mathf.Lerp (focalDistance, otherData.focalDistance, otherDataWeight);

			#if ALLOW_PHYSICAL_CAMERA
			mixData.usePhysicalProperties = otherData.usePhysicalProperties;
			mixData.sensorSize = Vector2.Lerp (sensorSize, otherData.sensorSize, otherDataWeight);
			mixData.lensShift = Vector2.Lerp (lensShift, otherData.lensShift, otherDataWeight);
			#endif

			return mixData;
		}

		#endregion

	}

}
 