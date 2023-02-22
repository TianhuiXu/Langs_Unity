#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	[CustomEditor (typeof (RuntimeActionList))]
	[System.Serializable]
	public class RuntimeActionListEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			RuntimeActionList _target = (RuntimeActionList) target;

			if (Application.isPlaying)
			{
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.ObjectField ("Asset source:", _target.assetSource, typeof (ActionListAsset), false);

				if (_target.useParameters)
				{
					CustomGUILayout.EndVertical ();
					CustomGUILayout.BeginVertical ();
					EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
					ActionListEditor.ShowParametersGUI (_target, null, _target.parameters);
				}
				CustomGUILayout.EndVertical ();
			}
			else
			{
				EditorGUILayout.HelpBox ("This component should not be added manually - it is added automatically by AC at runtime.", MessageType.Warning);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif