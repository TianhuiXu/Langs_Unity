#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (UnityUICursor))]
	public class UnityUICursorEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			UnityUICursor _target = (UnityUICursor) target;

			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif