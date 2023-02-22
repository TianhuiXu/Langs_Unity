/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Marker.cs"
 * 
 *	This script allows a simple way of teleporting
 *	characters and objects around the scene.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A component used to create reference transforms, as needed by the PlayerStart and various Actions.
	 * When the game begins, the renderer will be disabled and the GameObject will be rotated if the game is 2D.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_marker.html")]
	[AddComponentMenu("Adventure Creator/Navigation/Marker")]
	public class Marker : MonoBehaviour
	{

		#region Variables

		private Transform _transform;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			Renderer _renderer = GetComponent<Renderer> ();
			if (_renderer)
			{
				_renderer.enabled = false;
			}
		}

		#endregion


		#region GetSet

		/** The Marker's position */
		public Vector3 Position
		{
			get
			{
				return Transform.position;
			}
		}


		/** The Marker's forward-facing angle, corrected for 2D if necessary */
		public float ForwardAngle
		{
			get
			{
				if (SceneSettings.IsUnity2D ())
				{
					return -Transform.eulerAngles.z;
				}
				return Transform.eulerAngles.y;
			}
		}


		/** The Marker's forward direction, corrected for 2D if necessary */
		public Vector3 ForwardDirection
		{
			get
			{
				if (SceneSettings.IsUnity2D ())
				{
					return Rotation * Vector3.forward;
				}
				return Transform.forward;
			}
		}


		/** The Marker's rotation, corrected for 2D if necessary */
		public Quaternion Rotation
		{
			get
			{
				if (SceneSettings.IsUnity2D ())
				{
					return Quaternion.AngleAxis (ForwardAngle, Vector3.up);
				}
				return Transform.rotation;
			}
		}


		/** A cache of the Markers's transform component */
		public Transform Transform
		{
			get
			{
				if (_transform == null) _transform = transform;
				return _transform;
			}
		}

		#endregion
		

		#if UNITY_EDITOR

		protected void OnDrawGizmos ()
		{
			if (KickStarter.sceneSettings && UnityEditor.Selection.activeGameObject != gameObject)
			{
				DrawGizmos ();
			}
		}
		
		
		protected void OnDrawGizmosSelected ()
		{
			DrawGizmos ();
		}


		public virtual void DrawGizmos ()
		{
			if (Application.isPlaying) return;
			Renderer _renderer = GetComponent<Renderer> ();
			if (_renderer)
			{
				_renderer.enabled = KickStarter.sceneSettings.visibilityMarkers;
			}
		}

		#endif

	}

}