/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavigationEngine_meshCollider.cs"
 * 
 *	This script uses a custom mesh collider to
 *	allow pathfinding in a scene.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class NavigationEngine_meshCollider : NavigationEngine
	{

		#region Variables
		
		protected bool pathFailed = false;
		private Vector3 upDirection = new Vector3 (0f, 1f, 0f);

		#endregion


		#region PublicFunctions

		public override void OnReset (NavigationMesh navMesh)
		{
			if (!Application.isPlaying) return;

			if (navMesh == null && KickStarter.settingsManager && KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick)
			{
				ACDebug.LogWarning ("Could not initialise NavMesh - was one set as the Default in the Settings Manager?");
			}
		}


		public override void TurnOn (NavigationMesh navMesh)
		{
			if (navMesh == null || KickStarter.settingsManager == null) return;

			upDirection = navMesh.UpDirection;
			
			if (LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer) == -1)
			{
				ACDebug.LogError ("Can't find layer " + KickStarter.settingsManager.navMeshLayer + " - please define it in Unity's Tags Manager (Edit -> Project settings -> Tags and Layers).");
			}
			else if (!string.IsNullOrEmpty (KickStarter.settingsManager.navMeshLayer))
			{
				navMesh.gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer);
			}
			
			if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.meshCollider && navMesh.GetComponent <Collider>() == null)
			{
				ACDebug.LogWarning ("A Collider component must be attached to " + navMesh.gameObject.name + " for pathfinding to work - please attach one.", navMesh.gameObject);
			}
		}

			
		public override Vector3[] GetPointsArray (Vector3 originPos, Vector3 targetPos, AC.Char _char = null)
		{
			List <Vector3> pointsList = new List<Vector3>();
			
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.navMesh && KickStarter.sceneSettings.navMesh.GetComponent <Collider>())
			{
				Vector3 originalOriginPos = originPos;
				Vector3 originalTargetPos = targetPos;
				originPos = GetNearestToMesh (originPos);
				targetPos = GetNearestToMesh (targetPos);
				
				pointsList.Add (originPos);
				
				if (!IsLineClear (targetPos, originPos, false))
				{
					pointsList = FindComplexPath (originPos, targetPos, false);
					
					if (pathFailed)
					{
						Vector3 newTargetPos = GetLineBreak (pointsList [pointsList.Count - 1], targetPos);
						
						if (newTargetPos != Vector3.zero)
						{
							targetPos = newTargetPos;
							
							if (!IsLineClear (targetPos, originPos, true))
							{
								pointsList = FindComplexPath (originPos, targetPos, true);
								
								if (pathFailed)
								{
									// Couldn't find an alternative, so just clear the path
									pointsList.Clear ();
									pointsList.Add (originPos);
								}
							}
							else
							{
								// Line between origin and new target is clear
								pointsList.Clear ();
								pointsList.Add (originPos);
							}
						}
					}
				}
				
				// Finally, remove any extraneous points
				if (pointsList.Count > 2)
				{
					for (int i=0; i<pointsList.Count; i++)
					{
						for (int j=i; j<pointsList.Count; j++)
						{
							if (IsLineClear (pointsList[i], pointsList[j], false) && j > i+1)
							{
								// Point i+1 is irrelevant, remove and reset
								pointsList.RemoveRange (i+1, j-i-1);
								j=0;
								i=0;
							}
						}
					}
				}
				pointsList.Add (targetPos);
				
				if (pointsList[0] == originalOriginPos)
					pointsList.RemoveAt (0);	// Remove origin point from start
				
				// Special case: where player is stuck on a Collider above the mesh
				if (pointsList.Count == 1 && pointsList[0] == originPos)
				{
					pointsList[0] = originalTargetPos;
				}
			}
			else
			{
				// Special case: no Collider, no path
				pointsList.Add (targetPos);
			}
			
			return pointsList.ToArray ();
		}


		public override void ResetHoles (NavigationMesh navMesh)
		{
			if (navMesh == null || navMesh.GetComponent <MeshCollider>() == null || navMesh.GetComponent <MeshCollider>().sharedMesh == null) return;

			if (navMesh.GetComponent <MeshCollider>().sharedMesh == null)
			{
				if (navMesh.GetComponent <MeshFilter>() && navMesh.GetComponent <MeshFilter>().sharedMesh)
				{
					navMesh.GetComponent <MeshCollider>().sharedMesh = navMesh.GetComponent <MeshFilter>().sharedMesh;
					ACDebug.LogWarning (navMesh.gameObject.name + " has no MeshCollider mesh - temporarily using MeshFilter mesh instead.", navMesh.gameObject);
				}
				else
				{
					ACDebug.LogWarning (navMesh.gameObject.name + " has no MeshCollider mesh.", navMesh.gameObject);
				}
			}
		}
		
		
		public override string GetPrefabName ()
		{
			return ("NavMesh");
		}
		
		
		public override Vector3 GetPointNear (Vector3 point, float minDistance, float maxDistance)
		{
			Vector2 circle = Random.insideUnitCircle.normalized;

			Vector3 randomOffset = Vector3.Cross (new Vector3 (circle.x, 0f, circle.y), upDirection) * Random.Range (minDistance, maxDistance);
			Vector3 randomPoint = point + randomOffset;

			if (IsLineClear (point, randomPoint, false))
			{
				return randomPoint;
			}

			Vector3 intersectPoint = GetLineBreak (randomPoint, point);
			if (intersectPoint != Vector3.zero)
			{
				return intersectPoint;
			}

			return base.GetPointNear (point, minDistance, maxDistance);
		}
		
		
		public override void SceneSettingsGUI ()
		{
			#if UNITY_EDITOR
			KickStarter.sceneSettings.navMesh = (NavigationMesh) EditorGUILayout.ObjectField ("Default NavMesh:", KickStarter.sceneSettings.navMesh, typeof (NavigationMesh), true);
			if (SceneSettings.IsUnity2D ())
			{
				EditorGUILayout.HelpBox ("This method is not compatible with 'Unity 2D' mode.", MessageType.Warning);
			}
			#endif
		}
		
		#endregion


		#region ProtectedFunctions

		protected bool IsVertexImperfect (Vector3 vertex, Vector3[] blackList)
		{
			for (int i=0; i<blackList.Length; i++)
			{
				if (vertex == blackList[i])
				{
					return true;
				}
			}
			
			return false;
		}
		
		
		protected float GetPathLength (List <Vector3> _pointsList, Vector3 candidatePoint, Vector3 endPos)
		{
			float length = 0f;
			
			List <Vector3> newPath = new List<Vector3>();
			foreach (Vector3 point in _pointsList)
			{
				newPath.Add (point);
			}
			newPath.Add (candidatePoint);
			newPath.Add (endPos);
			
			for (int i=1; i<newPath.Count; i++)
			{
				length += Vector3.Distance (newPath[i], newPath[i-1]);
			}
			
			return (length);
		}
		
		
		protected bool IsLineClear (Vector3 startPos, Vector3 endPos, bool ignoreOthers)
		{
			// Raise positions to above mesh, so they can "look down"
			
			float startOffset = Vector3.Dot (startPos, upDirection);
			float endOffset = Vector3.Dot (endPos, upDirection);

			float diffOffset = endOffset - startOffset;
			if (diffOffset > 0f)
			{
				startPos -= diffOffset * upDirection;
			}
			else
			{
				endPos -= diffOffset * upDirection;
			}

			Vector3 actualPos = startPos;
			RaycastHit hit = new RaycastHit();
			Ray ray = new Ray ();
			
			for (float i=0f; i<1f; i+= 0.01f)
			{
				actualPos = startPos + ((endPos - startPos) * i);
				ray = new Ray (actualPos + (upDirection * 2f), -upDirection);
				
				if (KickStarter.settingsManager && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, 1 << KickStarter.sceneSettings.navMesh.gameObject.layer))
				{
					if (hit.collider.gameObject != KickStarter.sceneSettings.navMesh.gameObject && !ignoreOthers)
					{
						return false;
					}
				}
				else
				{
					return false;
				}
				
			}
			
			return true;
		}
		
		
		protected Vector3 GetLineBreak (Vector3 startPos, Vector3 endPos)
		{
			// Raise positions to above mesh, so they can "look down"
			
			float startOffset = Vector3.Dot (startPos, upDirection);
			float endOffset = Vector3.Dot (endPos, upDirection);

			float diffOffset = endOffset - startOffset;
			if (diffOffset > 0f)
			{
				startPos -= diffOffset * upDirection;
			}
			else
			{
				endPos -= diffOffset * upDirection;
			}
			
			Vector3 actualPos = startPos;
			RaycastHit hit = new RaycastHit();
			Ray ray = new Ray ();
			
			for (float i=0f; i<1f; i+= 0.01f)
			{
				actualPos = startPos + ((endPos - startPos) * i);
				ray = new Ray (actualPos + (upDirection * 2f), -upDirection);
				
				if (KickStarter.settingsManager && Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, 1 << KickStarter.sceneSettings.navMesh.gameObject.layer))
				{
					if (hit.collider.gameObject != KickStarter.sceneSettings.navMesh.gameObject)
					{
						return actualPos;
					}
				}
			}
			
			return Vector3.zero;
		}
		
		
		protected Vector3[] CreateVertexArray (Vector3 targetPos)
		{
			Mesh mesh = KickStarter.sceneSettings.navMesh.transform.GetComponent <MeshCollider>().sharedMesh;
			if (mesh == null)
			{
				ACDebug.LogWarning ("Active NavMesh has no mesh!", KickStarter.sceneSettings.navMesh.gameObject);
				return null;
			}
			Vector3[] _vertices = mesh.vertices;
			
			List<NavMeshData> navMeshData = new List<NavMeshData>();
			
			foreach (Vector3 vertex in _vertices)
			{
				navMeshData.Add (new NavMeshData (vertex, targetPos, KickStarter.sceneSettings.navMesh.transform));
			}
			
			navMeshData.Sort (delegate (NavMeshData a, NavMeshData b) {return a.distance.CompareTo (b.distance);});
			
			List <Vector3> vertexData = new List<Vector3>();
			foreach (NavMeshData data in navMeshData)
			{
				vertexData.Add (data.vertex);
			}
			
			return (vertexData.ToArray ());
		}
		
		
		protected List<Vector3> FindComplexPath (Vector3 originPos, Vector3 targetPos, bool ignoreOthers)
		{
			targetPos = GetNearestToMesh (targetPos);
			
			pathFailed = false;
			List <Vector3> pointsList = new List<Vector3>();
			pointsList.Add (originPos);
			
			// Find nearest vertex to targetPos that originPos can also "see"
			bool pathFound = false;
			
			// An array of the navMesh's vertices, in order of distance from the target position
			Vector3[] vertices = CreateVertexArray (targetPos);
			
			int j=0;
			float pathLength = 0f;
			bool foundCandidate = false;
			bool foundCandidateForBoth = false;
			Vector3 candidatePoint = Vector3.zero;
			List<Vector3> imperfectCandidates = new List<Vector3>();
			
			while (!pathFound)
			{
				pathLength = 0f;
				foundCandidate = false;
				foundCandidateForBoth = false;
				
				foreach (Vector3 vertex in vertices)
				{
					if (!IsVertexImperfect (vertex, imperfectCandidates.ToArray ()))
					{
						if (IsLineClear (vertex, pointsList [pointsList.Count - 1], ignoreOthers))
						{
							// Do we now have a clear path?
							if (IsLineClear (targetPos, vertex, ignoreOthers))
							{
								if (!foundCandidateForBoth)
								{
									// Test a new candidate
									float testPathLength = GetPathLength (pointsList, vertex, targetPos);
									
									if (testPathLength < pathLength || !foundCandidate)
									{
										foundCandidate = true;
										foundCandidateForBoth = true;
										candidatePoint = vertex;
										pathLength = testPathLength;
									}
								}
								else
								{
									// Test a new candidate
									float testPathLength = GetPathLength (pointsList, vertex, targetPos);
									
									if (testPathLength < pathLength)
									{
										candidatePoint = vertex;
										pathLength = testPathLength;
									}
								}
							}
							else if (!foundCandidateForBoth)
							{
								if (!foundCandidate)
								{
									candidatePoint = vertex;
									foundCandidate = true;
									pathLength = GetPathLength (pointsList, vertex, targetPos);
								}
								else
								{
									// Test a new candidate
									float testPathLength = GetPathLength (pointsList, vertex, targetPos);
									
									if (testPathLength < pathLength)
									{
										candidatePoint = vertex;
										pathLength = testPathLength;
									}
								}
							}
						}
					}
				}
				
				if (foundCandidate)
				{
					pointsList.Add (candidatePoint);
					
					if (foundCandidateForBoth)
					{
						pathFound = true;
					}
					else
					{
						imperfectCandidates.Add (candidatePoint);
					}
				}
				
				j++;
				if (j > vertices.Length)
				{
					pathFailed = true;
					return pointsList;
				}
			}
			
			return pointsList;
		}
		
		
		protected Vector3 GetNearestToMesh (Vector3 point)
		{
			RaycastHit hit = new RaycastHit();
			Ray ray = new Ray ();
			
			// Test to make sure starting on the collision mesh
			ray = new Ray (point + (upDirection * 2f), -upDirection);
			if (KickStarter.settingsManager && !Physics.Raycast (ray, out hit, KickStarter.settingsManager.navMeshRaycastLength, 1 << KickStarter.sceneSettings.navMesh.gameObject.layer))
			{
				Vector3[] vertices = CreateVertexArray (point);
				return vertices[0];
			}
			
			return (point);	
		}

		#endregion


		#region ProtectedClasses

		protected class NavMeshData
		{

			/** The vertex's position */
			public Vector3 vertex;
			/** The distance between the vertex and the current target position */
			public float distance;
			

			/**
			 * The default Constructor.
			 */
			public NavMeshData (Vector3 _vertex, Vector3 _target, Transform navObject)
			{
				vertex = navObject.TransformPoint (_vertex);
				distance = Vector3.Distance (vertex, _target);
			}
			
		}

		#endregion


		#region GetSet

		public override bool RequiresNavMeshGameObject
		{
			get
			{
				return true;
			}
		}

		#endregion

	}

}