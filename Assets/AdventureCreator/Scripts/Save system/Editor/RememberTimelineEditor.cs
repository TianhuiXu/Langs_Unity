#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberTimeline), true)]
	public class RememberTimelineEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI ()
		{
			RememberTimeline _target = (RememberTimeline) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Timeline", EditorStyles.boldLabel);
			_target.saveBindings = CustomGUILayout.Toggle ("Save bindings?", _target.saveBindings, "", "If True, the GameObjects bound to the Timeline will be stored in save game files.");
			_target.saveTimelineAsset = CustomGUILayout.Toggle ("Save Timeline asset?", _target.saveTimelineAsset, "", "If True, the Timeline asset assigned in the PlayableDirector's Timeline field will be stored in save game files.");
			if (_target.saveTimelineAsset)
			{
				EditorGUILayout.HelpBox ("Both the original and new 'Timeline' assets will need placing in a Resources folder.", MessageType.Info);
			}
			_target.evaluateWhenStopped = CustomGUILayout.Toggle ("Evaluate when stopped?", _target.evaluateWhenStopped, "", "If True, and the Timeline was not playing when it was saved, it will be evaluated at its playback point - causing the effects of it running at that single frame to be restored");
			CustomGUILayout.EndVertical ();

			SharedGUI ();
		}
		
	}

}

#endif