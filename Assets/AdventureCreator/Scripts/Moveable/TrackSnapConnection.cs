/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"TrackSnapConnection.cs"
 * 
 *	Stores information related to connection points along tracks.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** Stores information related to connection points along tracks. */
	[System.Serializable]
	public class TrackSnapConnection
	{

		#region Variables

		[SerializeField] private DragTrack connectedTrack = null;
		
		#endregion


		#region PublicFunctions

		public float EvaluateInputScore (DragMovementCalculation dragMovementCalculation, Moveable_Drag draggable, Vector3 dragForce)
		{
			if (!IsValid ())
			{
				if (dragMovementCalculation == DragMovementCalculation.CursorPosition)
				{
					return Mathf.Infinity;
				}
				return 0f;
			}

			switch (dragMovementCalculation)
			{
				case DragMovementCalculation.DragVector:
					return connectedTrack.GetForceDotProduct(dragForce, draggable);
					
				case DragMovementCalculation.CursorPosition:
					return connectedTrack.GetMinDistanceToScreenPoint (KickStarter.playerInput.GetMousePosition());

				default:
					return 0f;
			}
		}


		/**
		 * <summary>Connects a draggable object to the track defined in this data block</summary>
		 * <param name="draggable">The draggable object to update</param>
		 */
		public void MakeConnection (Moveable_Drag draggable)
		{
			// First need to decide which snap point on the connectedTrack is closest to the draggable
			TrackSnapData winningSnap = null;
			float winningDistance = Mathf.Infinity;

			foreach (TrackSnapData trackSnapData in connectedTrack.allTrackSnapData)
			{
				if (!trackSnapData.IsEnabled) continue;

				Vector3 snapWorldPosition = trackSnapData.GetWorldPosition (connectedTrack);
				float sqrDist = (snapWorldPosition - draggable.Transform.position).sqrMagnitude;
				if (sqrDist < winningDistance)
				{
					winningDistance = sqrDist;
					winningSnap = trackSnapData;
				}
			}

			if (winningSnap != null)
			{ 
				draggable.SnapToTrack (connectedTrack, winningSnap.ID);
			}
		}


		/** Checks if the data is valid */
		public bool IsValid ()
		{
			if (connectedTrack && connectedTrack.TypeSupportsSnapConnections ())
			{
				if (connectedTrack.allTrackSnapData != null && connectedTrack.allTrackSnapData.Count > 0)
				{
					return true;
				}
			}
			return false;
		}

		#endregion


		#if UNITY_EDITOR

		public TrackSnapConnection ShowGUI (DragTrack ownTrack, int i)
		{
			connectedTrack = (DragTrack) CustomGUILayout.ObjectField <DragTrack> ("Connected track " + i + ":", connectedTrack, true, "", "A connected track that a draggable object can transfer to when positioned at this point.");
			if (connectedTrack)
			{
				if (connectedTrack == ownTrack)
				{
					ACDebug.LogWarning (ownTrack + " cannot connect to itself", connectedTrack);
					connectedTrack = null;
				}
				else if (connectedTrack.allTrackSnapData == null || connectedTrack.allTrackSnapData.Count == 0)
				{
					//EditorGUILayout.HelpBox ("Cannot connect to this track - no snap regions defined!", MessageType.Warning);
					ACDebug.LogWarning (ownTrack + " cannot connect to track " + connectedTrack + ", - no snap regions defined", connectedTrack);
					connectedTrack = null;
				}
				else if (!connectedTrack.TypeSupportsSnapConnections ())
				{
					//EditorGUILayout.HelpBox ("This track type does not support connections.", MessageType.Warning);
					ACDebug.LogWarning (ownTrack + " cannot connect to track " + connectedTrack + ", - this track type does not support connections", connectedTrack);
					connectedTrack = null;
				}
			}
			return this;
		}


		public void DrawHandles (Vector3 ownPosition)
		{
			if (connectedTrack)
			{
				TrackSnapData winningSnap = null;
				float winningDistance = Mathf.Infinity;

				foreach (TrackSnapData trackSnapData in connectedTrack.allTrackSnapData)
				{
					Vector3 snapWorldPosition = trackSnapData.GetWorldPosition(connectedTrack);
					float sqrDist = (snapWorldPosition - ownPosition).sqrMagnitude;
					if (sqrDist < winningDistance)
					{
						winningDistance = sqrDist;
						winningSnap = trackSnapData;
					}
				}

				if (winningSnap != null)
				{
					float connectedPositionAlong = connectedTrack.GetRegionPositionAlong (winningSnap.ID);
					Vector3 worldPosition = connectedTrack.GetGizmoPosition (connectedPositionAlong);
					Handles.DrawDottedLine (ownPosition, worldPosition, 4f);
				}
			}
		}

		#endif

	}

}
 