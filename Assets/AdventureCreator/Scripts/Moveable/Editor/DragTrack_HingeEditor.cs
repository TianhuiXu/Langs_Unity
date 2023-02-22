#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(DragTrack_Hinge))]
	public class DragTrack_HingeEditor : DragTrackEditor
	{
		
		public override void OnInspectorGUI ()
		{
			DragTrack_Hinge _target = (DragTrack_Hinge) target;
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Track shape:", EditorStyles.boldLabel);

			_target.radius = CustomGUILayout.FloatField ("Radius:", _target.radius, "", "The track's radius (for visualising in the Scene window)");
			if (_target.radius < 0f) _target.radius = 0f;
			_target.handleColour = CustomGUILayout.ColorField ("Handles colour:", _target.handleColour, "", "The colour of Scene window Handles");
			
			_target.doLoop = CustomGUILayout.Toggle ("Is looped?", _target.doLoop, "", "If True, then objects can be rotated a full revolution");
			if (!_target.doLoop)
			{
				_target.maxAngle = CustomGUILayout.Slider ("Maximum angle:", _target.maxAngle, 0f, 360, "", "How much an object can be rotated by");
			}
			else
			{
				_target.limitRevolutions = CustomGUILayout.Toggle ("Limit revolutions?", _target.limitRevolutions, "", "If True, then the number of revolutions an object can rotate is limited");
				if (_target.limitRevolutions)
				{
					_target.maxRevolutions = CustomGUILayout.IntField ("Max revolutions:", _target.maxRevolutions, "", "The maximum number of revolutions an object can be rotated by");
					if (_target.maxRevolutions < 1) _target.maxRevolutions = 1;
				}
			}

			_target.dragMovementCalculation = (DragMovementCalculation) CustomGUILayout.EnumPopup ("Movement input:", _target.dragMovementCalculation);
			if (_target.dragMovementCalculation == DragMovementCalculation.DragVector)
			{
				_target.alignDragToFront = CustomGUILayout.ToggleLeft ("Align drag vector to front?", _target.alignDragToFront, "", "If True, then the calculated drag vector will be based on the track's orientation, rather than the object being rotated, so that the input drag vector will always need to be the same direction");
			}
			else if (_target.dragMovementCalculation == DragMovementCalculation.CursorPosition && !_target.Loops)
			{
				_target.preventEndToEndJumping = CustomGUILayout.ToggleLeft ("Prevent end-to-end jumping?", _target.preventEndToEndJumping, "", "If True, then the dragged object will be prevented from jumping from one end to the other without first moving somewhere in between");
			}
			
			_target.discSize = CustomGUILayout.Slider ("Gizmo size:", _target.discSize, 0f, 2f, "", "The size of the track's ends, as seen in the Scene window");
			
			CustomGUILayout.EndVertical ();

			SnapDataGUI (_target, true);
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		public void OnSceneGUI ()
		{
			DragTrack_Hinge _target = (DragTrack_Hinge) target;
			
			Handles.color = new Color (_target.handleColour.r / 2f, _target.handleColour.g / 2f, _target.handleColour.b / 2f, _target.handleColour.a);
			Handles.DrawSolidDisc (_target.GetGizmoPosition (0f), _target.transform.up, _target.discSize);
			Handles.color = _target.handleColour;
			
			if (!_target.Loops)
			{
				Quaternion rot = Quaternion.AngleAxis (_target.MaxAngle, _target.transform.forward);
				Vector3 endUp = RotatePointAroundPivot (_target.transform.up, Vector3.zero, rot);
				Handles.DrawSolidDisc (_target.GetGizmoPosition (1f), endUp, _target.discSize);
			}

			Handles.DrawWireArc (_target.transform.position, _target.transform.forward, _target.transform.right, _target.MaxAngle, _target.radius);

			foreach (TrackSnapData trackSnapData in _target.allTrackSnapData)
			{
				DrawTrackRegions(trackSnapData, _target);
			}
		}


		private void DrawTrackRegions(TrackSnapData trackSnapData, DragTrack_Hinge hingeTrack)
		{
			float minPositionAlong = Mathf.Clamp01 (trackSnapData.PositionAlong - trackSnapData.Width);
			float maxPositionAlong = Mathf.Clamp01 (trackSnapData.PositionAlong + trackSnapData.Width);
			
			Quaternion rot = Quaternion.AngleAxis (hingeTrack.MaxAngle * trackSnapData.PositionAlong, hingeTrack.transform.forward);
			Vector3 endUp = RotatePointAroundPivot (hingeTrack.transform.up, Vector3.zero, rot);

			Handles.color = trackSnapData.GizmoColor;
			Vector3 positionAlongWorld = hingeTrack.GetGizmoPosition (trackSnapData.PositionAlong);
			Handles.DrawSolidDisc (positionAlongWorld, endUp, hingeTrack.discSize / 2f);

			rot = Quaternion.AngleAxis (hingeTrack.MaxAngle * minPositionAlong, hingeTrack.transform.forward);
			endUp = RotatePointAroundPivot (hingeTrack.transform.right, Vector3.zero, rot);

			Handles.DrawWireArc (hingeTrack.transform.position, hingeTrack.transform.forward, endUp, hingeTrack.MaxAngle * (maxPositionAlong - minPositionAlong), hingeTrack.radius);

			Quaternion minRot = Quaternion.AngleAxis (hingeTrack.MaxAngle * minPositionAlong, hingeTrack.transform.forward);
			endUp = RotatePointAroundPivot (hingeTrack.transform.up, Vector3.zero, minRot);
			Vector3 minPositionAlongWorld = hingeTrack.GetGizmoPosition (minPositionAlong);
			Handles.DrawSolidDisc (minPositionAlongWorld, endUp, hingeTrack.discSize / 4f);

			Quaternion maxRot = Quaternion.AngleAxis (hingeTrack.MaxAngle * maxPositionAlong, hingeTrack.transform.forward);
			endUp = RotatePointAroundPivot (hingeTrack.transform.up, Vector3.zero, maxRot);
			Vector3 maxPositionAlongWorld = hingeTrack.GetGizmoPosition (maxPositionAlong);
			Handles.DrawSolidDisc (maxPositionAlongWorld, endUp, hingeTrack.discSize / 4f);
		}


		private Vector3 RotatePointAroundPivot (Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			return rotation * (point - pivot) + pivot;
		}
		
	}

}

#endif