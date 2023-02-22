/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AlignToCamera.cs"
 * 
 *	Attach this script to an object you wish to align to a camera's view.
 *	This works best with sprites being used as foreground objects in 2.5D games.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Aligns an object to a camera's viewport. This is intended for sprites being used as foreground objects in 2.5D games.
	 */
	[ExecuteInEditMode]
	[AddComponentMenu("Adventure Creator/Camera/Align to camera")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_align_to_camera.html")]
	public class AlignToCamera : MonoBehaviour
	{

		#region Variables

		/** The _Camera to align the GameObject to */
		public _Camera cameraToAlignTo;
		/** If True, the distance from the camera will be fixed (though adjustable in the Inspector) */
		public bool lockDistance = true;
		/** How far to place the GameObject away from the cameraToAlignTo, once set */
		public float distanceToCamera;
		/** If True, the percieved scale of the GameObject, as seen through the cameraToAlignTo, will be fixed even if the distance between the two changes */
		public bool lockScale;
		/** If lockScale is True, this GameObject's scale will be multiplied by this value */
		public Vector2 scaleFactor = Vector2.zero;
		/** How the object is aligned (YAxisOnly, CopyFullRotation) */
		public AlignType alignType = AlignType.YAxisOnly;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			Align ();
		}


		#if UNITY_EDITOR

		protected void Update ()
		{
			if (!Application.isPlaying)
			{
				Align ();
			}
		}

		#endif

		#endregion


		#region PublicFunctions

		#if UNITY_EDITOR

		/**
		 * Attempts to place the GameObject in the centre of cameraToAlignTo's view.
		 */
		public void CentreToCamera ()
		{
			float distanceFromCamera = Vector3.Dot (cameraToAlignTo.Transform.forward, transform.position - cameraToAlignTo.Transform.position);
			if (Mathf.Approximately (distanceFromCamera, 0f))
			{
				return;
			}

			if (lockDistance)
			{
				Vector3 newPosition = cameraToAlignTo.Transform.position + (cameraToAlignTo.Transform.forward * distanceFromCamera);
				transform.position = newPosition;
			}
		}

		#endif

		#endregion


		#region ProtectedFunctions

		protected void Align ()
		{
			if (cameraToAlignTo)
			{
				if (alignType == AlignType.YAxisOnly)
				{
					transform.rotation = Quaternion.Euler (transform.rotation.eulerAngles.x, cameraToAlignTo.Transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
				}
				else
				{
					transform.rotation = cameraToAlignTo.Transform.rotation;
				}

				if (lockDistance)
				{
					if (distanceToCamera > 0f)
					{
						Vector3 relativePosition = transform.position - cameraToAlignTo.Transform.position;
						float currentDistance = relativePosition.magnitude;
						if (!Mathf.Approximately (currentDistance, distanceToCamera))
						{
							if (currentDistance > 0f)
							{
								transform.position = cameraToAlignTo.Transform.position + (relativePosition * distanceToCamera / currentDistance);
							}
							else
							{
								transform.position = cameraToAlignTo.Transform.position + cameraToAlignTo.Transform.forward * distanceToCamera;
							}
						}

						if (lockScale)
						{
							CalculateScale ();

							if (scaleFactor != Vector2.zero)
							{
								transform.localScale = scaleFactor * distanceToCamera;
							}
						}
					}
					else if (distanceToCamera < 0f)
					{
						distanceToCamera = 0f;
					}
					else if (Mathf.Approximately (distanceToCamera, 0f))
					{
						Vector3 relativePosition = transform.position - cameraToAlignTo.Transform.position;
						float magnitude = relativePosition.magnitude;
						if (magnitude > 0f)
						{
							distanceToCamera = magnitude;
						}
					}
				}
				
				if (!lockScale || cameraToAlignTo == null)
				{
					scaleFactor = Vector2.zero;
				}
			}
		}


		protected void CalculateScale ()
		{
			if (scaleFactor == Vector2.zero)
			{
				scaleFactor = new Vector2 (transform.localScale.x / distanceToCamera, transform.localScale.y / distanceToCamera);
			}
		}

		#endregion

	}

}