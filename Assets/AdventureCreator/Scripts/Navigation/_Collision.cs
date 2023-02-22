/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"_Collision.cs"
 * 
 *	This script allows colliders that block the Player's movement
 *	to be turned on and off easily via actions.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Provides functions to easily turn Collider components on and off, either through script or with the "Object: Send message" Action.
	 * This script is attached to AC's Collider prefabs.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1___collision.html")]
	public class _Collision : MonoBehaviour
	{

		#region Variables

		/** If True, then this component will control the GameObject's layer when turned on and off */
		public bool controlsObjectLayer = true;

		#endregion


		#region UnityStandards

		#if UNITY_EDITOR
		
		private void OnDrawGizmos ()
		{
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.visibilityCollision && UnityEditor.Selection.activeGameObject != gameObject)
			{
				DrawGizmos ();
			}
		}
		
		
		private void OnDrawGizmosSelected ()
		{
			DrawGizmos ();
		}

		#endif

		#endregion


		#region PublicFunctions
		
		/**
		 * Enables 3D and 2D colliders attached to the GameObject, and places it on the Hotspot (Default) layer - causing it to block Hotspot raycasts.
		 */
		public void TurnOn ()
		{
			Collider _collider = GetComponent <Collider>();
			if (_collider)
			{
				_collider.enabled = true;
			}
			else
			{
				Collider2D _collider2D = GetComponent <Collider2D>();
				if (_collider2D)
				{
					_collider2D.enabled = true;
				}
			}

			if (controlsObjectLayer)
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
			}
		}
		

		/**
		 * Disables 3D and 2D colliders attached to the GameObject, and places it on the Deactivated (Ignore Raycast) layer - allowing Hotspot raycasts to pass through it.
		 */
		public void TurnOff ()
		{
			Collider _collider = GetComponent <Collider>();
			if (_collider)
			{
				_collider.enabled = false;
			}
			else
			{
				Collider2D _collider2D = GetComponent <Collider2D>();
				if (_collider2D)
				{
					_collider2D.enabled = false;
				}
			}

			if (controlsObjectLayer)
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
			}
		}

		#endregion


		#if UNITY_EDITOR

		protected void DrawGizmos ()
		{
			AdvGame.DrawCubeCollider (transform, ACEditorPrefs.CollisionGizmoColor);
		}

		#endif

	}

}