/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"WorldSpaceCursorExample.cs"
 * 
 *	This script serves as an example of how you can override AC's input system to allow an object's position in world space to dictate the cursor position.
 *	To use it, add it to a mesh object you wish to act as the cursor, and place it on the 'Ignore Raycast' layer.
 *
 *	The mesh object will be controlled by the mouse, but will move in 3D space.
 *
 *	If you wish to instead move the mesh object by an other means (for example, by use of a VR-wand), you can duplicated this script and amend it to suit your needs. 
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script serves as an example of how you can override AC's input system to allow an object's position in world space to dictate the cursor position.
	 * To use it, add it to a mesh object you wish to act as the cursor, and place it on the 'Ignore Raycast' layer.
	 *
	 * The mesh object will be controlled by the mouse, but will move in 3D space.
	 *
	 * If you wish to instead move the mesh object by an other means (for example, by use of a VR-wand), you can duplicated this script and amend it to suit your needs.
	 */
	[AddComponentMenu("Adventure Creator/3rd-party/World-space cursor example")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_world_space_cursor_example.html")]
	public class WorldSpaceCursorExample : MonoBehaviour
	{

		/** The layers that the cursor will collide with. */
		public LayerMask collisionLayer;
		/** The minimum distance that the cursor can be to the camera */
		public float minDistance = 1f;
		/** The maximum distance that the cursor can be from the camera */
		public float maxDistance = 30f;
		/** If True, the AC cursor position will be used instead of the raw mouse position */
		public bool useACCursor = false;

		private RaycastHit hit;
		private Ray ray;
		private Collider ownCollider;


		private void Start ()
		{
			/*
			 * First, we'll assign the 'ownCollider' variable, and set the PlayerInput script's InputMousePositionDelegate to our own function.
			 * This will cause AC to rely on CustomMousePosition for the cursor's position in Screen-space.
			 */

			ownCollider = GetComponent <Collider>();
			KickStarter.playerInput.InputMousePositionDelegate = CustomMousePosition;
		}
		
		
		private Vector2 CustomMousePosition (bool cursorIsLocked)
		{
			/**
			 * Now, we create a ray from the camera - using either the mouse position or the centre of the screen,
			 * depending on whether the cursor is in it's 'Locked' state or not.
			 */

			if (useACCursor)
			{
				ray = KickStarter.CameraMain.ScreenPointToRay (KickStarter.playerInput.GetMousePosition ());
			}
			else
			{
				if (cursorIsLocked)
				{
					ray = KickStarter.CameraMain.ViewportPointToRay (new Vector2 (0.5f, 0.5f));
				}
				else
				{
					ray = KickStarter.CameraMain.ScreenPointToRay (Input.mousePosition);
				}
			}

			/**
			 * We fire this ray and see if it hits anything. If it does, we place the GameObject as close to the hit point as possible.
			 * Otherwise, we continue to move it in 3D space, but keep it's distance from the camera constant.
			 */

			if (Physics.Raycast (ray, out hit, maxDistance, collisionLayer))
			{
				if (ownCollider == null || hit.collider != ownCollider)
				{
					SetPosition (hit.point);
				}
				else
				{
					SetPosition (transform.position);
				}
			}
			else
			{
				SetPosition (transform.position);
			}

			return KickStarter.CameraMain.WorldToScreenPoint (transform.position);
		}


		private void SetPosition (Vector3 targetPosition)
		{
			/**
			 * This function positions the GameObject as close to a target as it can within the boundaries we've defined.
			 */

			float distanceFromCamera = (targetPosition - KickStarter.CameraMainTransform.position).magnitude;
			distanceFromCamera = Mathf.Clamp (distanceFromCamera, minDistance, maxDistance);
			
			transform.position = KickStarter.CameraMainTransform.position + (ray.direction.normalized * distanceFromCamera);
		}

	}

}