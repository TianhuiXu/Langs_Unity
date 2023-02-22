/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"LipSyncTexture.cs"
 * 
 *	Animates a SkinnedMeshRenderer's textures based on lipsync animation
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/** Animates a SkinnedMeshRenderer's textures based on lipsync animation */
	[AddComponentMenu("Adventure Creator/Characters/Lipsync texture")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_lip_sync_texture.html")]
	public class LipSyncTexture : MonoBehaviour
	{

		#region Variables

		/** The SkinnedMeshRenderer to affect */
		public SkinnedMeshRenderer skinnedMeshRenderer;
		/** The index of the material to affect */
		public int materialIndex;
		/** The material's property name that will be replaced */
		public string propertyName = "_MainTex";
		/** A List of Texture2Ds that correspond to the phoneme defined in the Phonemes Editor */
		public List<Texture2D> textures = new List<Texture2D>();
		/** If True, then changes to the material will be applied in LateUpdate, as opposed to Update calls */
		public bool affectInLateUpdate;

		protected int thisFrameIndex = -1;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			LimitTextureArray ();
		}


		protected void LateUpdate ()
		{
			if (affectInLateUpdate)
			{
				SetFrame (thisFrameIndex, true);
			}
		}

		#endregion


		#region PublicFunctions

		/** Resizes the textures List to match the number of phonemes defined in the Phonemes Editor */
		public void LimitTextureArray ()
		{
			if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().speechManager == null)
			{
				return;
			}

			int arraySize = AdvGame.GetReferences ().speechManager.phonemes.Count;

			if (textures.Count != arraySize)
			{
				int numTextures = textures.Count;

				if (arraySize < numTextures)
				{
					textures.RemoveRange (arraySize, numTextures - arraySize);
				}
				else if (arraySize > numTextures)
				{
					for (int i=textures.Count; i<arraySize; i++)
					{
						textures.Add (null);
					}
				}
			}
		}


		/**
		 * <summary>Sets the material's texture based on the currently-active phoneme.</summary>
		 * <param name = "textureIndex">The index number of the phoneme</param>
		 * <param name = "fromLateUpdate">If True, then this function is called from LateUpdate</param>
		 */
		public void SetFrame (int textureIndex, bool fromLateUpdate = false)
		{
			thisFrameIndex = textureIndex;

			if (textureIndex < 0 || textures == null || textureIndex >= textures.Count)
			{
				return;
			}

			if (affectInLateUpdate != fromLateUpdate)
			{
				return;
			}

			if (skinnedMeshRenderer)
			{
				if (materialIndex >= 0 && skinnedMeshRenderer.materials.Length > materialIndex)
				{
					skinnedMeshRenderer.materials [materialIndex].SetTexture (propertyName, textures [textureIndex]);
				}
				else
				{
					ACDebug.LogWarning ("Cannot find material index " + materialIndex + " on SkinnedMeshRenderer " + skinnedMeshRenderer.gameObject.name, skinnedMeshRenderer);
				}
			}
		}

		#endregion

	}

}