#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberHotspot), true)]
	public class RememberHotspotEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			RememberHotspot _target = (RememberHotspot) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Hotspot", EditorStyles.boldLabel);
			_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Hotspot state on start:", _target.startState, "The interactive state of the Hotspot when the game begins");
			CustomGUILayout.EndVertical ();

			if (_target.GetComponent <Hotspot>() == null)
			{
				EditorGUILayout.HelpBox ("This script requires a Hotspot component!", MessageType.Warning);
			}

			SharedGUI ();
		}

	}

}

#endif