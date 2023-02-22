#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberNPC), true)]
	public class RememberNPCEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			RememberNPC _target = (RememberNPC) target;

			if (_target.GetComponent <Hotspot>() && _target.GetComponent <RememberHotspot>() == null)
			{
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("NPC", EditorStyles.boldLabel);
				_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Hotspot state on start:", _target.startState, "", "The state of the NPC's Hotspot component when the game begins");
				CustomGUILayout.EndVertical ();
			}

			if (_target.GetComponent <NPC>() == null)
			{
				EditorGUILayout.HelpBox ("This script expects an NPC component!", MessageType.Warning);
			}


			SharedGUI ();
		}
		
	}

}

#endif