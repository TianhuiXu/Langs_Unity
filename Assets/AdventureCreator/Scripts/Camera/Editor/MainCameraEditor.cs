#if UNITY_EDITOR

#if !UNITY_2020_2_OR_NEWER && (UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS)
#define ALLOW_VR
#endif

using UnityEditor;

#if ALLOW_VR
using UnityEngine.VR;
#endif

namespace AC
{

	[CustomEditor(typeof(MainCamera))]
	public class MainCameraEditor : Editor
	{
		
		public override void OnInspectorGUI()
		{
			MainCamera _target = (MainCamera) target;

			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif