#if UNITY_EDITOR && !ACIgnoreTimeline

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (CharacterAnimation2DShot))]
	public class CharacterAnimation2DShotEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			CharacterAnimation2DShot _target = (CharacterAnimation2DShot) target;

			_target.ShowGUI ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (_target);
			}
		}
	}

}

#endif