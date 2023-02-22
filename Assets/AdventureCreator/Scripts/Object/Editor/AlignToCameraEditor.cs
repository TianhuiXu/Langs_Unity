#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (AlignToCamera))]
	public class AlignToCameraEditor : Editor
	{
		
		private AlignToCamera _target;
		
		
		private void OnEnable ()
		{
			_target = (AlignToCamera) target;
		}
		
		
		public override void OnInspectorGUI ()
		{
			_target.cameraToAlignTo = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Camera to align to:", _target.cameraToAlignTo, true, "", "The AC _Camera that this GameObject should align itself to");

			if (_target.cameraToAlignTo)
			{
				_target.alignType = (AlignType) CustomGUILayout.EnumPopup ("Align type:", _target.alignType, "", "The way in which this GameObject is aligned to " + _target.cameraToAlignTo.name);
				_target.lockDistance = CustomGUILayout.Toggle ("Lock distance?", _target.lockDistance, "", "If True, the distance from the camera will be fixed (though adjustable in the Inspector)");
				if (_target.lockDistance)
				{
					_target.distanceToCamera = CustomGUILayout.FloatField ("Distance from camera:", _target.distanceToCamera, "", "How far to place the GameObject away from " + _target.cameraToAlignTo.name + ", once set");
					_target.lockScale = CustomGUILayout.Toggle ("Lock perceived scale?", _target.lockScale, "", "If True, the percieved scale of the GameObject, as seen through " + _target.cameraToAlignTo.name + ", will be fixed even if the distance between the two changes");
				}

				if (GUILayout.Button ("Centre to camera"))
				{
					Undo.RecordObject (_target, "Centre to camera");
					_target.CentreToCamera ();
				}
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif