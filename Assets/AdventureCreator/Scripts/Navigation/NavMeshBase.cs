/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavMeshBase.cs"
 * 
 *	A base class for NavigationMesh and NavMeshSegment
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A base class for NavigationMesh and NavMeshSegment, which control scene objects used by pathfinding algorithms.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_nav_mesh_base.html")]
	public class NavMeshBase : MonoBehaviour
	{

		#region Variables

		/** Disables the Renderer when the game begins */
		public bool disableRenderer = true;

		private Collider _collider;
		private MeshRenderer _meshRenderer;
		private MeshCollider _meshCollider;
		private MeshFilter _meshFilter;

		/** If True, then Physics collisions with this GameObject's Collider will be disabled */
		public bool ignoreCollisions = true;

		#endregion

		#region UnityStandards

		private void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		private void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		private void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}

		#endregion


		#region PublicFunctions

		/**
		 * Disables the Renderer component.
		 */
		public void Hide ()
		{
			#if UNITY_EDITOR
			if (_meshRenderer == null)
			{
				_meshRenderer = GetComponent <MeshRenderer>();
			}
			#endif

			if (_meshRenderer)
			{
				_meshRenderer.enabled = false;
			}
		}


		/**
		 * Enables the Renderer component.
		 * If the GameObject has both a MeshFilter and a MeshCollider, then the MeshColliders's mesh will be used by the MeshFilter.
		 */
		public void Show ()
		{
			#if UNITY_EDITOR
			if (_meshRenderer == null)
			{
				_meshRenderer = GetComponent <MeshRenderer>();
			}
			#endif

			if (_meshRenderer)
			{
				_meshRenderer.enabled = true;

				if (_meshFilter && _meshCollider != null && _meshCollider.sharedMesh)
				{
					_meshFilter.mesh = _meshCollider.sharedMesh;
				}
			}
		}


		/**
		 * Calls Physics.IgnoreCollision on all appropriate Collider combinations (Unity 5 only).
		 */
		public void IgnoreNavMeshCollisions (Collider[] allColliders = null)
		{
			if (ignoreCollisions)
			{
				if (allColliders == null)
				{
					allColliders = FindObjectsOfType (typeof(Collider)) as Collider[];
				}

				if (_collider && _collider.enabled && _collider.gameObject.activeInHierarchy)
				{
					foreach (Collider otherCollider in allColliders)
					{
						if (_collider != otherCollider && !_collider.isTrigger && !otherCollider.isTrigger && otherCollider.enabled && otherCollider.gameObject.activeInHierarchy && !(_collider is TerrainCollider))
						{
							Physics.IgnoreCollision (_collider, otherCollider);
						}
					}
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void BaseAwake ()
		{
			_collider = GetComponent <Collider>();
			_meshRenderer = GetComponent <MeshRenderer>();
			_meshCollider = GetComponent <MeshCollider>();
			_meshFilter = GetComponent <MeshFilter>();

			if (disableRenderer)
			{
				Hide ();
			}
		}

		#endregion


		#region GetSet

		/** The attached Collider component */
		public Collider Collider
		{
			get
			{
				return _collider;
			}
		}

		#endregion

	}

}
