#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberMoveable), true)]
	public class RememberMoveableEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI ()
		{
			RememberMoveable _target = (RememberMoveable) target;
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Moveable", EditorStyles.boldLabel);
			_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Moveable state on start:", _target.startState, "", "The interactive state of the object when the game begins");
			CustomGUILayout.EndVertical ();
			
			if (_target.GetComponent <Moveable>() == null)
			{
				EditorGUILayout.HelpBox ("This script expects a Moveable component!", MessageType.Warning);
			}
			
			SharedGUI ();
		}
		
	}

}

#endif