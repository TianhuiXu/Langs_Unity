/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavigationEngine.cs"
 * 
 *	This script is a base class for the Navigation method scripts.
 *  Create a subclass of name "NavigationEngine_NewMethodName" and
 * 	add "NewMethodName" to the AC_NavigationMethod enum to integrate
 * 	a new method into the engine.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A base class for all navigation methods.  Subclasses of this script are used to return a navigation path, as an array of Vector3s, based on two positions.
	 * A number of functions to allow easier integration within SceneManager are also included.
	 * To create a new navigation method, create a new subclass of this script with the name syntax "NavigationEngine_NewMethodName", and add "NewMethodName" to the AC_NavigationMethod enum in Enums.cs.
	 * The method will then be an option in the "Navigation engine" popup in the Scene Manager.
	 */
	public class NavigationEngine : ScriptableObject
	{

		/** If True, then navigation raycasts performed by PlayerMovement will be done in 2D, rather than 3D */
		public bool is2D = false;

		protected Vector2[] vertexData;


		/**
		 * <summary>Called when the scene begins or is reset.</summary>
		 * <param name = "navMesh">The NavigationMesh that is active in the scene.</param>
		 */
		public virtual void OnReset (NavigationMesh navMesh)
		{}


		/**
		 * <summary>Calculates a path between two points.</summary>
		 * <param name = "startPosition">The start position</param>
		 * <param name = "targetPosition">The intended end position</param>
		 * <param name = "_char">The character (see Char) who this path is for (only used in PolygonCollider pathfinding)</param>
		 * <returns>The path to take, as an array of Vector3s.</returns>
		 */
		public virtual Vector3[] GetPointsArray (Vector3 startPosition, Vector3 targetPosition, AC.Char _char = null)
		{
			List<Vector3> pointsList = new List<Vector3>();
			pointsList.Add (targetPosition);
			return pointsList.ToArray ();
		}


		/**
		 * <summary>Calculates a path between multiple points.</summary>
		 * <param name = "startPosition">The start position</param>
		 * <param name = "targetPositions">An array of positions to travel through along the path, with the last entry being the intended destination</param>
		 * <param name = "_char">The character (see Char) who this path is for (only used in PolygonCollider pathfinding)</param>
		 * <returns>The path to take, as an array of Vector3s.</returns>
		 */
		public Vector3[] GetPointsArray (Vector3 startPosition, Vector3[] targetPositions, AC.Char _char = null)
		{
			if (targetPositions == null || targetPositions.Length == 0)
			{
				return GetPointsArray (startPosition, startPosition, _char);
			}

			List<Vector3> pointsList = new List<Vector3>();
			for (int i=0; i<targetPositions.Length; i++)
			{
				Vector3 partialStartPosition = (i > 0) ? targetPositions[i-1] : startPosition;
				Vector3[] partialPointsArray = GetPointsArray (partialStartPosition, targetPositions[i], _char);

				foreach (Vector3 partialPoint in partialPointsArray)
				{
					if (pointsList.Count == 0 || pointsList[pointsList.Count-1] != partialPoint)
					{
						pointsList.Add (partialPoint);
					}
				}
			}

			return pointsList.ToArray ();
		}


		/**
		 * <summary>Finds a random position surrounding a given point on a NavMesh.</summary>
		 * <param name = "point">The given point on the NavMesh</param>
		 * <param name = "minDistance">The minimum distance between the given point and the random point</param>
		 * <param name = "maxDistance">The maximum distance between the given point and the random point</param>
		 * <returns>A random position surrounding the given point. If a suitable point is not found, the original point will be returned.</returns>
		 */
		public virtual Vector3 GetPointNear (Vector3 point, float minDistance, float maxDistance)
		{
			return point;
		}


		/**
		 * <summary>Gets the name of a "helper" prefab to list in the Scene Manager.</summary>
		 * <returns>The name of the prefab to list in SceneManager. The prefab must be placed in the Assets/AdventureCreator/Prefabs/Navigation folder. If nothing is returned, no prefab will be listed.</returns>
		 */
		public virtual string GetPrefabName ()
		{
			return "";
		}


		/**
		 * <summary>Enables the NavMesh so that it can be used in pathfinding.</summary>
		 * <param name = "navMeshOb">The NavigationMesh gameobject to enable</param>
		 */
		public virtual void TurnOn (NavigationMesh navMesh)
		{}


		/**
		 * Integrates all PolygonCollider2D objects in the polygonColliderHoles List into the base PolygonCollider2D shape.
		 * This is called automatically by AddHole() and RemoveHole() once the List has been amended
		 */
		public virtual void ResetHoles (NavigationMesh navMesh)
		{}


		/**
		 * Provides a space for any custom Editor GUI code that should be displayed in SceneManager.
		 */
		public virtual void SceneSettingsGUI ()
		{}


		/** Returns True if the engine relies on a specific GameObject for pathfinding */
		public virtual bool RequiresNavMeshGameObject
		{
			get
			{
				return false;
			}
		}


		#if UNITY_EDITOR

		/**
		 * Provides a space for any custom Editor GUI code that should be displayed in the NavigationMesh inspector.
		 */
		public virtual NavigationMesh NavigationMeshGUI (NavigationMesh _target)
		{
			_target.disableRenderer = CustomGUILayout.ToggleLeft ("Disable mesh renderer?", _target.disableRenderer, "", "If True, the MeshRenderer will be disabled when the game begins");
			_target.ignoreCollisions = CustomGUILayout.ToggleLeft ("Ignore collisions?", _target.ignoreCollisions, "", "If True, then Physics collisions with this GameObject's Collider will be disabled");
			return _target;
		}


		/**
		 * <summary>Draws gizmos in the Scene/Game window.</summary>
		 * <param name = "navMeshOb">The NavigationMesh gameobject to draw gizmos for</param>
		 */
		public virtual void DrawGizmos (GameObject navMeshOb)
		{}

		#endif

	}

}