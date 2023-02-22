#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (TintMap))]
	public class TintMapEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			TintMap _target = (TintMap) target;

			_target.tintMapTexture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> ("Texture to use (optional):", _target.tintMapTexture, false, "", "An optional texture to make use of.  If this field is empty, then the texture found on the attached MeshRenderer's material will be used instead");
			if (_target.tintMapTexture && !Application.isPlaying)
			{
				EditorGUILayout.HelpBox ("The supplied texture will be applied to the Mesh Renderer's material when the game begins.", MessageType.Info);
			}
			_target.colorModifier = EditorGUILayout.ColorField ("Color modifier:", _target.colorModifier);
			_target.disableRenderer = CustomGUILayout.Toggle ("Disable mesh renderer?", _target.disableRenderer, "", "If True, then the MeshRenderer component will be disabled automatically when the game begins");

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif