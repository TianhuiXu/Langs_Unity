#if !ACIgnoreTimeline && UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof(MainCameraTrack))]
	public class MainCameraTrackEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			MainCameraTrack _target = (MainCameraTrack) target;

			_target.ShowGUI ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (_target);
			}
		}
	}

}

#endif