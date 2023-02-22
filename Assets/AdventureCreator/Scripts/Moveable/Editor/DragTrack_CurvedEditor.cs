#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(DragTrack_Curved))]
	public class DragTrack_CurvedEditor : DragTrackEditor
	{
		
		public override void OnInspectorGUI ()
		{
			DragTrack_Curved _target = (DragTrack_Curved) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Track shape:", EditorStyles.boldLabel);
			
			_target.radius = CustomGUILayout.FloatField ("Radius:", _target.radius, "", "The track's radius");
			if (_target.radius < 0f) _target.radius = 0f;

			_target.handleColour = CustomGUILayout.ColorField ("Handles colour:", _target.handleColour, "", "The colour of Scene window Handles");

			_target.doLoop = CustomGUILayout.Toggle ("Is looped?", _target.doLoop, "", "If True, then the track forms a complete loop");
			if (!_target.doLoop)
			{
				_target.maxAngle = CustomGUILayout.Slider ("Maximum angle:", _target.maxAngle, 0f, 360, "", "The angle of the tracks's curve");
			}
			_target.dragMovementCalculation = (DragMovementCalculation) CustomGUILayout.EnumPopup ("Movement input:", _target.dragMovementCalculation);
			if (_target.dragMovementCalculation == DragMovementCalculation.CursorPosition && !_target.Loops)
			{
				_target.preventEndToEndJumping = CustomGUILayout.ToggleLeft ("Prevent end-to-end jumping?", _target.preventEndToEndJumping, "", "If True, then the dragged object will be prevented from jumping from one end to the other without first moving somewhere in between");
			}
			_target.discSize = CustomGUILayout.Slider ("Gizmo size:", _target.discSize, 0f, 2f, "", "The size of the track's ends, as seen in the Scene window");
			
			CustomGUILayout.EndVertical ();
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("End-colliders", EditorStyles.boldLabel);
			
			if (!_target.Loops)
			{
				_target.generateColliders = CustomGUILayout.Toggle ("Generate end-colliders?", _target.generateColliders);
			}
			if (_target.generateColliders && !_target.Loops)
			{
				_target.colliderMaterial = (PhysicMaterial) CustomGUILayout.ObjectField <PhysicMaterial> ("Material:", _target.colliderMaterial, false, "", "Physics Material to give the track's end colliders");
			}
			
			CustomGUILayout.EndVertical ();

			SnapDataGUI (_target, true);
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		public void OnSceneGUI ()
		{
			DragTrack_Curved _target = (DragTrack_Curved) target;

			Handles.color = new Color (_target.handleColour.r / 2f, _target.handleColour.g / 2f, _target.handleColour.b / 2f, _target.handleColour.a);
			Handles.DrawSolidDisc (_target.GetGizmoPosition (0f), _target.transform.up, _target.discSize);
			
			if (!_target.Loops)
			{
				Handles.color = _target.handleColour;
				Quaternion rot = Quaternion.AngleAxis (_target.MaxAngle, _target.transform.forward);
				Vector3 endUp = RotatePointAroundPivot (_target.transform.up, Vector3.zero, rot);
				Handles.DrawSolidDisc (_target.GetGizmoPosition (1f), endUp, _target.discSize);
			}
			
			Handles.color = _target.handleColour;
			Handles.DrawWireArc (_target.transform.position, _target.transform.forward, _target.transform.right, _target.MaxAngle, _target.radius);

			foreach (TrackSnapData trackSnapData in _target.allTrackSnapData)
			{
				DrawTrackRegions (trackSnapData, _target);
			}
		}


		private void DrawTrackRegions (TrackSnapData trackSnapData, DragTrack_Curved curvedTrack)
		{
			float minPositionAlong = Mathf.Clamp01 (trackSnapData.PositionAlong - trackSnapData.Width);
			float maxPositionAlong = Mathf.Clamp01 (trackSnapData.PositionAlong + trackSnapData.Width);
			
			Quaternion rot = Quaternion.AngleAxis (curvedTrack.MaxAngle * trackSnapData.PositionAlong, curvedTrack.transform.forward);
			Vector3 endUp = RotatePointAroundPivot (curvedTrack.transform.up, Vector3.zero, rot);

			Handles.color = trackSnapData.GizmoColor;
			Vector3 positionAlongWorld = curvedTrack.GetGizmoPosition (trackSnapData.PositionAlong);
			Handles.DrawSolidDisc (positionAlongWorld, endUp, curvedTrack.discSize / 2f);

			rot = Quaternion.AngleAxis (curvedTrack.MaxAngle * minPositionAlong, curvedTrack.transform.forward);
			endUp = RotatePointAroundPivot (curvedTrack.transform.right, Vector3.zero, rot);

			Handles.DrawWireArc (curvedTrack.transform.position, curvedTrack.transform.forward, endUp, curvedTrack.MaxAngle * (maxPositionAlong - minPositionAlong), curvedTrack.radius * 1.01f);

			Quaternion minRot = Quaternion.AngleAxis (curvedTrack.MaxAngle * minPositionAlong, curvedTrack.transform.forward);
			endUp = RotatePointAroundPivot (curvedTrack.transform.up, Vector3.zero, minRot);
			Vector3 minPositionAlongWorld = curvedTrack.GetGizmoPosition (minPositionAlong);
			Handles.DrawSolidDisc (minPositionAlongWorld, endUp, curvedTrack.discSize / 4f);

			Quaternion maxRot = Quaternion.AngleAxis (curvedTrack.MaxAngle * maxPositionAlong, curvedTrack.transform.forward);
			endUp = RotatePointAroundPivot (curvedTrack.transform.up, Vector3.zero, maxRot);
			Vector3 maxPositionAlongWorld = curvedTrack.GetGizmoPosition (maxPositionAlong);
			Handles.DrawSolidDisc (maxPositionAlongWorld, endUp, curvedTrack.discSize / 4f);

			trackSnapData.DrawConnectionHandles(curvedTrack);
		}


		private Vector3 RotatePointAroundPivot (Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			return rotation * (point - pivot) + pivot;
		}
		
	}

}

#endif