#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace AC
{

	[CustomEditor(typeof(AutoCorrectUIDimensions))]
	public class AutoCorrectUIDimensionsEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			AutoCorrectUIDimensions _target = (AutoCorrectUIDimensions) target;

			_target.updateScale = EditorGUILayout.Toggle ("Update scale?", _target.updateScale);

			if (_target.updateScale)
			{
				if (_target.GetComponent <CanvasScaler>() == null)
				{
					EditorGUILayout.HelpBox ("A CanvasScaler component must be attached to update the scale.", MessageType.Info);
				}
			}

			_target.updatePosition = EditorGUILayout.Toggle ("Update position?", _target.updatePosition);

			if (_target.updatePosition)
			{
				_target.minAnchorPoint = EditorGUILayout.Vector2Field ("Min anchor point:", _target.minAnchorPoint);
				_target.maxAnchorPoint = EditorGUILayout.Vector2Field ("Max anchor point:", _target.maxAnchorPoint);

				_target.transformToControl = (RectTransform) EditorGUILayout.ObjectField ("Transform to control:", _target.transformToControl, typeof (RectTransform), true);

				if (_target.transformToControl == null)
				{
					EditorGUILayout.HelpBox ("If no Transform is assigned above, the associated Menu's 'RectTransform boundary' will be used instead.", MessageType.Info);
				}
			}

			if (_target.GetComponent <Canvas>() == null)
			{
				EditorGUILayout.HelpBox ("A Canvas component must be attached", MessageType.Warning);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif