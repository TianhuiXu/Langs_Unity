#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(DragTrack))]
	public class DragTrackEditor : Editor
	{

		public void SnapDataGUI (DragTrack _target, bool useAngles)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Track regions", EditorStyles.boldLabel);

			for (int i=0; i<_target.allTrackSnapData.Count; i++)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Region " + _target.allTrackSnapData[i].ID.ToString ());
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("-"))
				{
					Undo.RecordObject (this, "Delete region");
					_target.allTrackSnapData.RemoveAt (i);
					i=-1;
					break;
				}
				EditorGUILayout.EndHorizontal ();

				_target.allTrackSnapData[i] = _target.allTrackSnapData[i].ShowGUI (_target, useAngles);
				EditorGUILayout.Space ();

				if (i < _target.allTrackSnapData.Count - 1)
				{ 
					GUILayout.Box(string.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(1));
				}
			}
			if (GUILayout.Button ("Create new track region"))
			{
				Undo.RecordObject (this, "Create track region");
				TrackSnapData trackSnapData = new TrackSnapData (0f, GetSnapIDArray (_target.allTrackSnapData));
				_target.allTrackSnapData.Add (trackSnapData);
			}

			CustomGUILayout.EndVertical ();

			if (_target.allTrackSnapData.Count > 0)
			{
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("Snapping", EditorStyles.boldLabel);

				_target.doSnapping = CustomGUILayout.Toggle ("Enable region snapping?", _target.doSnapping, string.Empty, "If True, then snapping is enabled and any object attached to the track can snap to pre-set regions along it when let go by the player");
				if (_target.doSnapping)
				{
					_target.snapSpeed = CustomGUILayout.FloatField ("Snap speed:", _target.snapSpeed, string.Empty, "The speed to move by when attached objects snap");
					_target.onlySnapOnPlayerRelease = CustomGUILayout.Toggle ("Only snap on release?", _target.onlySnapOnPlayerRelease, string.Empty, "If True, then snapping will only occur when the player releases the object - and not when moving on its own accord");
					_target.actionListSource = (ActionListSource) CustomGUILayout.EnumPopup ("ActionList source:", _target.actionListSource, string.Empty, "The source of ActionLists that can be run when a draggable option snaps to a region.");
				}

				CustomGUILayout.EndVertical ();
			}


			UnityVersionHandler.CustomSetDirty (_target);
		}


		private int[] GetSnapIDArray (List<TrackSnapData> allTrackSnapData)
		{
			List<int> idArray = new List<int>();
			if (allTrackSnapData != null)
			{
				foreach (TrackSnapData trackSnapData in allTrackSnapData)
				{
					idArray.Add (trackSnapData.ID);
				}
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}

#endif