/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Paths.cs"
 * 
 *	This script stores a series of "nodes", which act
 *	as waypoints for character movement.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Stores a series of "nodes", which act as waypoints for character movements.
	 * Nodes can either be generated in-game by pathfinding algorithms (see NavigationEngine), or by defining them in the Unity Editor.
	 * Characters can be made to move along a path in one direction only, back-and-forth, or choose nodes at random.
	 * ActionLists can also be set to run when a character reaches each node.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_paths.html")]
	public class Paths : MonoBehaviour, iActionListAssetReferencer
	{

		#region Variables

		/** A List of nodes (Vector3) that define the path */
		public List<Vector3> nodes = new List<Vector3>();
		/** A List of NodeCommand instances that dictate what ActionList is run when each node is reached */
		public List<NodeCommand> nodeCommands = new List<NodeCommand>();
		/** The source of ActionList objects that are run when nodes are reached (InScene, AssetFile) */
		public ActionListSource commandSource;
		/** The way in which characters move between each node (ForwardOnly, Loop, PingPong, IsRandom) */
		public AC_PathType pathType = AC_PathType.ForwardOnly;
		/** The speed at which characters will traverse a path (Walk, Run) */
		public PathSpeed pathSpeed;
		/** If True, then the character will teleport to the first node before traversing the path */
		public bool teleportToStart;
		/** If True, then characters will attempt to move vertically to reach nodes */
		public bool affectY;
		/** The time, in seconds, that a character will wait at each node before continuing along the path */
		public float nodePause;

		private Transform _transform;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			if (nodePause < 0f)
			{
				nodePause = 0f;
			}

			if (nodes == null || nodes.Count == 0)
			{
				nodes.Add (Transform.position);
			}
			else
			{
				nodes[0] = Transform.position;
			}
		}


		private void OnDrawGizmos ()
		{
			// Draws a blue line from this transform to the target
			#if UNITY_EDITOR
			Gizmos.color = ACEditorPrefs.PathGizmoColor;
			#else
			Gizmos.color = Color.blue;
			#endif
			int i;
			int numNodes = nodes.Count;

			if (nodes.Count > 0)
			{
				nodes[0] = Transform.position;
			}

			if (pathType == AC_PathType.IsRandom && numNodes > 1)
			{
				for (i = 1; i < numNodes; i++)
				{
					for (int j = 0; j < numNodes; j++)
					{
						if (i != j)
						{
							ConnectNodes (i,j);
						}
					}
				}
			}
			else
			{
				if (numNodes > 1)
				{
					for (i = 1; i<numNodes; i++)
					{
						Gizmos.DrawIcon (nodes[i], string.Empty, true);
						
						ConnectNodes (i, i - 1);
					}
				}
				
				if (pathType == AC_PathType.Loop && !teleportToStart)
				{
					if (numNodes > 2)
					{
						ConnectNodes (numNodes - 1, 0);
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if the next node is the final one.</summary>
		 * <param name = "currentNode">The index number of the current node (i.e. the node before the one being checked)</param>
		 * <returns>True if the next node is the final one</returns>
		 */
		public bool WillStopAtNextNode (int currentNode)
		{
			if (GetNextNode (currentNode, currentNode-1, false) == -1)
			{
				return true;
			}
			
			return false;
		}


		/*
		 * <summary>Recalculates all nodes to build a path from a given starting position</summary>
		 * <param name = "startPosition">The position to start from</param>
		 * <param name = "maxNodeDistance">If >0, the maximum allowed distance between two nodes</param>
		 */
		public void RecalculateToCenter (Vector3 startPosition, float maxNodeDistance = -1f)
		{
			Vector3[] pointArray;
			Vector3 targetPosition = startPosition;

			if (SceneSettings.ActInScreenSpace ())
			{
				targetPosition = AdvGame.GetScreenNavMesh (targetPosition);
			}

			if (KickStarter.navigationManager)
			{
				pointArray = KickStarter.navigationManager.navigationEngine.GetPointsArray (Transform.position, targetPosition);
			}
			else
			{
				List<Vector3> pointList = new List<Vector3>();
				pointList.Add (targetPosition);
				pointArray = pointList.ToArray ();
			}

			pointArray = SetMaxDistances (pointArray, maxNodeDistance);

			BuildNavPath (pointArray);
			pathType = AC_PathType.ReverseOnly;
		}
		

		/**
		 * <summary>Rebuilds the nodes List from an array of points. The first point on the new path will be the GameObject's current position.</summary>
		 * <param name = "pointData">An array of position vectors that dictate the new path</param>
		 */
		public void BuildNavPath (Vector3[] pointData)
		{
			if (pointData != null && pointData.Length > 0)
			{
				pathType = AC_PathType.ForwardOnly;
				affectY = false;
				nodePause = 0;

				List<Vector3> newNodes = new List<Vector3>();
				
				newNodes.Clear ();
				newNodes.Add (Transform.position);

				nodeCommands.Clear ();

				for (int i=0; i<pointData.Length; i++)
				{
					if (i==0)
					{
						// If first point, ignore if same as position
						if (SceneSettings.IsUnity2D ())
						{
							Vector2 testPoint = new Vector2 (Transform.position.x, Transform.position.y);
							Vector2 testPoint2 = new Vector2 (pointData[0].x, pointData[0].y);
							if ((testPoint - testPoint2).magnitude < 0.001f)
							{
								continue;
							}
						}
						else
						{
							Vector3 testPoint = new Vector3 (Transform.position.x, pointData[0].y, Transform.position.z);
							if ((testPoint - pointData[0]).magnitude < 0.001f)
							{
								continue;
							}
						}

					}
					newNodes.Add (pointData[i]);
				}

				nodes = newNodes;
			}
		}
		

		/**
		 * <summary>Gets the next node along a path, given the current one.</summary>
		 * <param name = "currentNode">The index number of the current node</param>
		 * <param name = "prevNode">The index number of the previous node (used for determining the direction along which the path is being traversed)</param>
		 * <param name = "playerControlled">True if the Player is moving along the path during gameplay</param>
		 * <param name = "lockedPathType">The type of Path, if the Player is moving along it during gameplay</param>
		 * <returns>The index number of the next node</returns>
		 */
		public int GetNextNode (int currentNode, int prevNode, bool playerControlled = false, AC_PathType lockedPathType = AC_PathType.ForwardOnly)
		{
			int numNodes = nodes.Count;

			if (numNodes == 1)
			{
				return -1;
			}
			
			if (playerControlled)
			{
				switch (pathType)
				{
					case AC_PathType.ReverseOnly:
						if (currentNode <= 0 && lockedPathType == AC_PathType.Loop)
						{
							return numNodes - 1;
						}
						return currentNode - 1;

					default:
						if (currentNode >= numNodes - 1)
						{
							if (lockedPathType == AC_PathType.Loop)
							{
								return 0;
							}
							return -1;
						}
						return currentNode + 1;
				}
			}
			else
			{
				switch (pathType)
				{
					case AC_PathType.ForwardOnly:
						if (currentNode == numNodes - 1)
						{
							return -1;
						}
						return (currentNode + 1);

					case AC_PathType.Loop:
						if (currentNode == numNodes - 1)
						{
							return 0;
						}
						return (currentNode + 1);

					case AC_PathType.ReverseOnly:
						if (currentNode == 0)
						{
							return -1;
						}
						return (currentNode - 1);

					case AC_PathType.PingPong:
						if (prevNode > currentNode)
						{
							// Going backwards
							if (currentNode == 0)
							{
								return 1;
							}
							return (currentNode - 1);
						}
						else
						{
							// Going forwards
							if (currentNode == numNodes - 1)
							{
								return (currentNode - 1);
							}
							return (currentNode + 1);
						}

					case AC_PathType.IsRandom:
						if (numNodes > 0)
						{
							int randomNode = Random.Range (0, numNodes);

							while (randomNode == currentNode)
							{
								randomNode = Random.Range (0, numNodes);
							}

							return (randomNode);
						}
						return 0;

					default:
						return -1;
				}
			}
		}


		/**
		 * <summary>Gets the distance along the path from the origin to a particular node</summary>
		 * <param name = "n">The index number of the node to check the distance to</param>
		 * <returns>The distance along the path from the origin to node n</returns>
		 */
		public float GetLengthToNode (int n)
		{
			if (n > 0 && nodes.Count > n)
			{
				float length = 0f;
				
				for (int i=1; i<=n; i++)
				{
					length += Vector3.Distance (nodes[i-1], nodes[i]);
				}
				
				return length;
			}
			
			return 0f;
		}


		/**
		 * <summary>Gets the distance along the path from one node to another</summary>
		 * <param name = "a">The node to start from</param>
		 * <param name = "b">The node to reach</param>
		 * <returns>The distance along the path between nodes a and b.</returns>
		 */
		public float GetLengthBetweenNodes (int a, int b)
		{
			if (a == b)
			{
				return 0f;
			}

			if (b < a)
			{
				int c = a;
				a = b;
				b = c;
			}

			float length = 0f;
			
			for (int i=a+1; i<=b; i++)
			{
				length += Vector3.Distance (nodes[i-1], nodes[i]);
			}
			
			return length;
		}


		/**
		 * <summary>Gets the total length of the path</summary>
		 * <returns>The total length of the path</returns>
		 */
		public float GetTotalLength ()
		{
			if (nodes.Count > 1)
			{
				return GetLengthToNode (nodes.Count-1);
			}

			return 0f;
		}


		/**
		 * <summary>Gets the index of the node that's nearest to a given position</summary>
		 * <param name = "position">The position to query</param>
		 * <returns>The index of the node that's nearest to the position</returns>
		 */
		public int GetNearestNode (Vector3 position)
		{
			int winningIndex = 0;
			float winningSqrDist = Mathf.Infinity;

			for (int i = 0; i < nodes.Count; i++)
			{
				float sqrDist = (position - nodes[i]).sqrMagnitude;
				if (sqrDist < winningSqrDist)
				{
					winningIndex = i;
					winningSqrDist = sqrDist;
				}
			}

			return winningIndex;
		}

		#endregion
		

		#region ProtectedFunctions
		
		protected void ConnectNodes (int a, int b)
		{
            Vector3 PosA = nodes[a] + (Vector3.up * 0.001f);
			Vector3 PosB = nodes[b] + (Vector3.up * 0.001f);
			Gizmos.DrawLine (PosA, PosB);
		}


		protected Vector3[] SetMaxDistances (Vector3[] pointArray, float maxNodeDistance)
		{
			if (maxNodeDistance <= 0f || pointArray.Length <= 1)
			{
				return pointArray;
			}

			List<Vector3> pointList = new List<Vector3>();
			for (int i=0; i<pointArray.Length; i++)
			{
				Vector3 point = pointArray[i];

				if (i == 0)
				{
					pointList.Add (point);
				}
				else
				{
					Vector3 lastPoint = pointArray[i-1];
					float nodeDistance = Vector3.Distance (point, lastPoint);

					float factor = nodeDistance / maxNodeDistance;
					int numIntervals = Mathf.FloorToInt (factor);

					if (numIntervals > 0)
					{
						float intervalDistance = nodeDistance / (float) (numIntervals + 1);
						Vector3 direction = (point - lastPoint).normalized;

						for (int n=1; n<=numIntervals; n++)
						{
							Vector3 intervalPoint = lastPoint + ((float) n * direction * intervalDistance);
							pointList.Add (intervalPoint);
						}
					}

					pointList.Add (point);
				}
			}
			return pointList.ToArray ();
		}

		#endregion


		#region GetSet

		/** Gets the position of the last node in the Path */
		public Vector3 Destination
		{
			get
			{
				return nodes[nodes.Count-1];
			}
		}


		/** A cache of the Path's transform component */
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
		protected bool relativeMode;
		protected Vector3 lastFramePosition;

		public bool RelativeMode
		{
			get
			{
				return relativeMode;
			}
			set
			{
				relativeMode = value;
			}
		}

		public Vector3 LastFramePosition
		{
			get
			{
				return lastFramePosition;
			}
			set
			{

				lastFramePosition = value;
			}
		}


		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (commandSource == ActionListSource.AssetFile)
			{
				foreach (NodeCommand command in nodeCommands)
				{
					if (command.actionListAsset == actionListAsset) return true;
				}
			}
			return false;
		}

		#endif

	}


	/**
	 * A data container used by Paths to store the ActionListAsset or Cutscene that is run when a path node is reached.
	 */
	[System.Serializable]
	public class NodeCommand
	{

		/** The Cutscene to run, if the Paths commandSource = ActionListSource.InScene */
		public Cutscene cutscene;
		/** The ActionListAsset to run, if the Paths commandSource = ActionListSource.AssetFile */
		public ActionListAsset actionListAsset;
		/** The ID of the ActionParameter in the ActionListAsset / Cutscene that represents the character moving along the path, if appropriate. */
		public int parameterID;
		/** If True, then the character moving along the path will stop moving while the cutscene is run */
		public bool pausesCharacter = true;


		/**
		 * The default Constructor.
		 */
		public NodeCommand ()
		{
			cutscene = null;
			actionListAsset = null;
			parameterID = -1;
			pausesCharacter = true;
		}


		public void SetParameter (ActionListSource source, GameObject gameObject)
		{
			if (source == ActionListSource.InScene && cutscene)
			{
				if (parameterID >= 0 && cutscene.NumParameters > parameterID)
				{
					ActionParameter parameter = cutscene.GetParameter (parameterID);
					if (parameter != null)
					{
						parameter.SetValue (gameObject);
					}
				}
				
				if (!pausesCharacter)
				{
					cutscene.Interact ();
				}
			}
			else if (source == ActionListSource.AssetFile && actionListAsset)
			{
				if (parameterID >= 0 && actionListAsset.NumParameters > parameterID)
				{
					int idToSend = 0;
					if (gameObject.GetComponent <ConstantID>())
					{
						idToSend = gameObject.GetComponent <ConstantID>().constantID;
					}
					else
					{
						ACDebug.LogWarning (gameObject.name + " requires a ConstantID script component!", gameObject);
					}

					ActionParameter parameter = actionListAsset.GetParameter (parameterID);
					if (parameter != null)
					{
						parameter.SetValue (idToSend);
					}
				}

				if (!pausesCharacter)
				{
					actionListAsset.Interact ();
				}
			}
		}

	}

}