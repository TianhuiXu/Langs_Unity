//#if !UNITY_SWITCH
#define ALLOW_VIDEO
//#endif

#if ALLOW_VIDEO && UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberVideoPlayer), true)]
	public class RememberVideoPlayerEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI ()
		{
			RememberVideoPlayer _target = (RememberVideoPlayer) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Video", EditorStyles.boldLabel);
			_target.saveClipAsset = CustomGUILayout.Toggle ("Save clip asset?", _target.saveClipAsset, "", "If True, the VideoClip assigned in the VideoPlayer component will be stored in save game files.");
			if (_target.saveClipAsset)
			{
				EditorGUILayout.HelpBox ("Both the original and new 'Video clip' assets will need placing in a Resources folder.", MessageType.Info);
			}
			CustomGUILayout.EndVertical ();

			SharedGUI ();
		}
		
	}

}

#endif