/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"TrackSnapData.cs"
 * 
 *	Stores information related to snapping draggable objects along tracks.
 * 
 */
 
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	/**
	 * Stores information related to snapping draggable objects along tracks.
	 */
	[System.Serializable]
	public class TrackSnapData
	{

		#region Variables

		[SerializeField] protected bool isDisabled = false;
		[SerializeField] protected float positionAlong;
		[SerializeField] protected float width;
		[SerializeField] protected int id;
		[SerializeField] protected List<TrackSnapConnection> connections = new List<TrackSnapConnection>();
		[SerializeField] protected Cutscene cutsceneOnSnap = null;
		[SerializeField] protected AudioClip soundOnEnter = null;
		[SerializeField] protected ActionListAsset actionListAssetOnSnap = null;

		#if UNITY_EDITOR
		[SerializeField] protected string label;
		[SerializeField] protected Color gizmoColor;
		#endif

		#endregion


		#region Constructors

		/**
		 * The default constructor
		 */
		public TrackSnapData (float _positionAlong, int[] idArray)
		{
			positionAlong = _positionAlong;
			width = 0.1f;
			isDisabled = false;
			#if UNITY_EDITOR
			gizmoColor = Color.blue;
			label = string.Empty;
			#endif

			id = 0;
			// Update id based on array
			if (idArray != null && idArray.Length > 0)
			{
				foreach (int _id in idArray)
				{
					if (id == _id)
						id ++;
				}
			}
		}

		#endregion


		#region PublicFunctions

		#if UNITY_EDITOR

		public TrackSnapData ShowGUI (DragTrack dragTrack, bool useAngles)
		{
			label = CustomGUILayout.TextField ("Editor label:", label, string.Empty, "The region's label when displayed in Actions.");

			bool isEnabled = !isDisabled;
			isEnabled = CustomGUILayout.Toggle ("Is enabled?", isEnabled, string.Empty, "If True, the region is enabled");
			isDisabled = !isEnabled;

			positionAlong = CustomGUILayout.Slider ("Centre " + ((useAngles) ? "angle" : "position:"), positionAlong, 0f, 1f, string.Empty, "How far along the track (as a decimal) the region lies.");

			width = CustomGUILayout.Slider ("Catchment size:", width, 0f, 1f, string.Empty, "How far apart from the snapping point (as a decimal of the track's length) the object can be for this to be enforced.");
			gizmoColor = CustomGUILayout.ColorField ("Editor colour:", gizmoColor, string.Empty, "What colour to draw handles in the Scene with.");

			if (dragTrack.doSnapping)
			{
				if (dragTrack.actionListSource == ActionListSource.InScene)
				{
					cutsceneOnSnap = (Cutscene) CustomGUILayout.ObjectField <Cutscene> ("Cutscene on snap:", cutsceneOnSnap, true, "", "An optional Cutscene to run when a Draggable object snaps to this region");
				}
				else if (dragTrack.actionListSource == ActionListSource.AssetFile)
				{
					actionListAssetOnSnap = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList on snap:", actionListAssetOnSnap, false, "", "An optional ActionList asset to run when a Draggable object snaps to this region");
				}
			}
			soundOnEnter = (AudioClip) EditorGUILayout.ObjectField ("Sound on enter:", soundOnEnter, typeof (AudioClip), false);

			if (dragTrack.TypeSupportsSnapConnections ())
			{
				if (connections.Count == 0)
				{
					TrackSnapConnection trackSnapConnection = new TrackSnapConnection();
					connections.Add (trackSnapConnection);
				}

				for (int i = 0; i < connections.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					connections[i] = connections[i].ShowGUI (dragTrack, i);
					if (GUILayout.Button("+", GUILayout.MaxWidth(20f)))
					{
						Undo.RecordObject(dragTrack, "Add connection");
						TrackSnapConnection trackSnapConnection = new TrackSnapConnection();
						connections.Insert (i+1, trackSnapConnection);
						i = -1;
						break;
					}
					if (connections.Count > 1 && GUILayout.Button("-", GUILayout.MaxWidth (20f)))
					{
						Undo.RecordObject(dragTrack, "Delete connection");
						connections.RemoveAt(i);
						i = -1;
						break;
					}
					EditorGUILayout.EndHorizontal();
				}

			}
			return this;
		}


		public void DrawConnectionHandles (DragTrack track)
		{
			foreach (TrackSnapConnection connection in connections)
			{
				connection.DrawHandles (GetWorldPosition (track));
			}
		}

		#endif


		/**
		 * <summary>Gets the position in world space of the centre of the snap region</summary>
		 * <param name="track">The track that the snap region is a part of.</param>
		 * <returns>The snap region's position in world space</returns>
		 **/
		public Vector3 GetWorldPosition (DragTrack track)
		{
			return track.GetGizmoPosition (PositionAlong);
		}


		/**
		 * <summary>Gets the distance, in Unity units, between a region along the track and the centre of a snapping point</summary>
		 * <param name = "trackValue">The distance along the track, as a decimal of its total length</param>
		 * <returns>The distance between the draggable object and the centre of the snapping point</returns>
		 */
		public float GetDistanceFrom (float trackValue)
		{
			float distance = positionAlong - trackValue;

			if (Mathf.Abs (distance) > width)
			{
				return Mathf.Infinity;
			}			
			return distance;
		}


		public void EvaluateConnectionPoints (DragTrack track, Moveable_Drag draggable, Vector3 dragForce)
		{
			if (connections == null || !IsEnabled) return;

			float ownScore = 0f;
			switch (draggable.track.dragMovementCalculation)
			{
				case DragMovementCalculation.DragVector:
					ownScore = draggable.track.GetForceDotProduct (dragForce, draggable);
					break;

				case DragMovementCalculation.CursorPosition:
					Vector2 draggableScreenPosition = KickStarter.CameraMain.WorldToScreenPoint (draggable.Transform.position);
					if (Vector2.Distance (draggableScreenPosition, KickStarter.playerInput.GetMousePosition ()) < 0.05f)
					{
						return;
					}
					ownScore = draggable.track.GetMinDistanceToScreenPoint (KickStarter.playerInput.GetMousePosition());
					break;

				default:
					break;
			}
			
			TrackSnapConnection winningConnection = null;
			float winningScore = ownScore;
			float winningScoreAbsolute = Mathf.Abs (winningScore);
			
			foreach (TrackSnapConnection connection in connections)
			{
				float connectionScore = connection.EvaluateInputScore (draggable.track.dragMovementCalculation, draggable, dragForce);
				float connectionScoreAbsolute = Mathf.Abs (connectionScore);

				switch (draggable.track.dragMovementCalculation)
				{
					case DragMovementCalculation.DragVector:
						if ((connectionScoreAbsolute > winningScoreAbsolute) || (connectionScoreAbsolute == winningScoreAbsolute && connectionScore > winningScore))
						{
							winningScoreAbsolute = connectionScoreAbsolute;
							winningScore = connectionScore;
							winningConnection = connection;
						}
						break;

					case DragMovementCalculation.CursorPosition:
						if (connectionScore < winningScore)
						{
							winningScore = connectionScore;
							winningConnection = connection;
						}
						break;

					default:
						break;
				}

				
			}

			if (winningScoreAbsolute > 0f && winningConnection != null && draggable.track == track)
			{
				winningConnection.MakeConnection (draggable);
			}
		}


		/**
		 * <summary>Moves a draggable object towards the snap point</summary>
		 * <param name = "draggable">The object to move</param>
		 * <param name = "speed">How fast to move the object by</param>
		 */
		public void MoveTo (Moveable_Drag draggable, float speed)
		{
			draggable.AutoMoveAlongTrack (positionAlong, speed, true, 1 << 0, ID);
		}


		/**
		 * <summary>Checks if a region along the track is within the snap's region</summary>
		 * <param name = "trackValue">The distance along the track, as a decimal of its total length</param>
		 * <returns>True if a region along the track is within the snap's region</region>
		 */
		public bool IsWithinRegion (float trackValue)
		{
			if (IsEnabled && GetDistanceFrom (trackValue) <= width)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Runs the 'on snap' ActionList associated with the region</summary>
		 * <param name = "actionListSource">The source of the ActionList (InScene, AssetFile)</param>
		 */
		public void RunSnapCutscene (ActionListSource actionListSource)
		{
			switch (actionListSource)
			{
				case ActionListSource.InScene:
					if (cutsceneOnSnap)
					{
						cutsceneOnSnap.Interact ();
					}
					break;

				case ActionListSource.AssetFile:
					if (actionListAssetOnSnap)
					{
						actionListAssetOnSnap.Interact ();
					}
					break;
			}
		}

		#endregion


		#region GetSet

		/** How far along the track the snap point is */
		public float PositionAlong
		{
			get
			{
				return positionAlong;
			}
		}


		/** How wide, as a proportion of the track length, the snap point is valid for */
		public float Width
		{
			get
			{
				return width;
			}
		}


		/** A unique identifier */
		public int ID
		{
			get
			{
				return id;
			}
		}


		/** If True, the region is enabled */
		public bool IsEnabled
		{
			get
			{
				return !isDisabled;
			}
			set
			{
				isDisabled = !value;
			}
		}


		/** The sound to play when a draggable enters this region */
		public AudioClip SoundOnEnter
		{
			get
			{
				return soundOnEnter;
			}
		}


		#if UNITY_EDITOR

		public string EditorLabel
		{
			get
			{
				if (!string.IsNullOrEmpty (label))
				{
					return (id.ToString () + ": " + label);
				}
				return (id.ToString () + ": " + positionAlong.ToString ());
			}
		}


		public Color GizmoColor
		{
			get
			{
				return gizmoColor;
			}
		}

		#endif

		#endregion

	}

}