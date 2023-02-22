#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberVisibility), true)]
	public class RememberVisibilityEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			RememberVisibility _target = (RememberVisibility) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Visibility", EditorStyles.boldLabel);
			_target.startState = (AC_OnOff) CustomGUILayout.EnumPopup ("Visibility on start:", _target.startState, "", "The Renderer's enabled state when the game begins");
			_target.affectChildren = CustomGUILayout.Toggle ("Affect children?", _target.affectChildren, "", "If True, child Renderers should be affected as well");

			if (_target.GetComponent <SpriteFader>() == null && _target.GetComponent <SpriteRenderer>())
			{
				_target.saveColour = CustomGUILayout.Toggle ("Save colour/alpha?", _target.saveColour, "", "If True, the sprite's colour/alpha will be saved");
			}

			CustomGUILayout.EndVertical ();

			SharedGUI ();
		}
		
	}

}

#endif