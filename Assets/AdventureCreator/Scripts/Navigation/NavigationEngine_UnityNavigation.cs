/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavigationEngine_UnityNavigation.cs"
 * 
 *	This script uses Unity's built-in Navigation
 *	system to allow pathfinding in a scene.
 * 
 */

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class NavigationEngine_UnityNavigation : NavigationEngine
	{

		#region PublicFunctions

		public override void SceneSettingsGUI ()
		{
			#if UNITY_EDITOR
			if (SceneSettings.IsUnity2D ())
			{
				EditorGUILayout.HelpBox ("This method is not compatible with 'Unity 2D' mode.", MessageType.Warning);
			}
			#endif
		}


		public override void TurnOn (NavigationMesh navMesh)
		{
			ACDebug.LogWarning ("Cannot enable NavMesh " + navMesh.gameObject.name + " as this scene's Navigation Method is Unity Navigation.");
		}


		public override Vector3[] GetPointsArray (Vector3 startPosition, Vector3 targetPosition, AC.Char _char = null)
		{
			NavMeshPath _path = new NavMeshPath();

			if (!NavMesh.CalculatePath (startPosition, targetPosition, -1, _path))
			{
				// Could not find path with current vectors
				float maxDistance = 0.001f;
				float originalDist = Vector3.Distance (startPosition, targetPosition);

				NavMeshHit hit = new NavMeshHit ();
				for (maxDistance = 0.001f; maxDistance < originalDist; maxDistance += 0.05f)
				{
					if (NavMesh.SamplePosition (startPosition, out hit, maxDistance, -1))
					{
						startPosition = hit.position;
						break;
					}
				}

				bool foundNewEnd = false;
				for (maxDistance = 0.001f; maxDistance < originalDist; maxDistance += 0.05f)
				{
					if (NavMesh.SamplePosition (targetPosition, out hit, maxDistance, -1))
					{
						targetPosition = hit.position;
						foundNewEnd = true;
						break;
					}
				}

				if (!foundNewEnd)
				{
					ACDebug.LogWarning ("No path could be calculated between " + startPosition + " and " + targetPosition);
					return new Vector3[0];
				}

				NavMesh.CalculatePath (startPosition, targetPosition, -1, _path);
			}
			
			List<Vector3> pointArray = new List<Vector3>();
			for (int i=0; i<_path.corners.Length; i++)
			{
				pointArray.Add (_path.corners[i]);
			}
			if (pointArray.Count > 1 && Mathf.Approximately (pointArray[0].x, startPosition.x) && Mathf.Approximately (pointArray[0].z, startPosition.x))
			{
				pointArray.RemoveAt (0);
			}
			else if (pointArray.Count == 0)
			{
				pointArray.Clear ();
				pointArray.Add (targetPosition);
			}

			return (pointArray.ToArray ());
		}


		public override Vector3 GetPointNear (Vector3 point, float minDistance, float maxDistance)
		{
			Vector2 circle = Random.insideUnitCircle.normalized;

			Vector3 randomOffset = new Vector3 (circle.x, 0f, circle.y) * Random.Range (minDistance, maxDistance);
			Vector3 randomPoint = point + randomOffset;

			NavMeshHit hit = new NavMeshHit ();
			bool blocked = NavMesh.Raycast (point, randomPoint, out hit, NavMesh.AllAreas);
			if (!blocked)
			{
				return randomPoint;
			}

			if (hit.position != Vector3.zero)
			{
				return hit.position;
			}
			return base.GetPointNear (point, minDistance, maxDistance);
		}


		public override string GetPrefabName ()
		{
			return ("NavMeshSegment");
		}

		#endregion

	}

}