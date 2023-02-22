#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (LipSyncTexture))]
	public class LipSyncTextureEditor : Editor
	{
		
		private LipSyncTexture _target;
		
		
		private void OnEnable ()
		{
			_target = (LipSyncTexture) target;
		}
		
		
		public override void OnInspectorGUI ()
		{
			if (_target.GetComponent <Char>() == null)
			{
				EditorGUILayout.HelpBox ("This component must be placed alongside either the NPC or Player component.", MessageType.Warning);
			}

			_target.skinnedMeshRenderer = (SkinnedMeshRenderer) CustomGUILayout.ObjectField <SkinnedMeshRenderer> ("Skinned Mesh Renderer:", _target.skinnedMeshRenderer, true, "", "The SkinnedMeshRenderer to affect");
			_target.materialIndex = CustomGUILayout.IntField ("Material to affect (index):", _target.materialIndex, "", "The index of the material to affect");
			_target.propertyName = CustomGUILayout.TextField ("Texture property name:", _target.propertyName, "", "The material's property name that will be replaced");

			_target.LimitTextureArray ();

			for (int i=0; i<_target.textures.Count; i++)
			{
				_target.textures[i] = (Texture2D) CustomGUILayout.ObjectField <Texture2D> ("Phoneme #" + i.ToString () + " texture:", _target.textures[i], false, "", "The Texture that corresponds to the phoneme defined in the Phonemes Editor");
			}

			_target.affectInLateUpdate = CustomGUILayout.Toggle ("Apply in LateUpdate?", _target.affectInLateUpdate, "", "If True, then changes to the material will by applied in LateUpdate, as opposed to Update");

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif