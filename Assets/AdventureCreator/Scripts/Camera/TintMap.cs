/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"TintMap.cs"
 * 
 *	This script is used to change the colour of 
 *	2D Character sprites based on their X/Y-position.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script is used to change the colour tinting of 2D character sprites, based on their position in the scene.
	 * The instance of this class stored in SceneSettings' tintMap variable can be read by FollowTintMap components to determine what their SpriteRenderer's colour should be.
	 */
	[RequireComponent (typeof (MeshRenderer))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_tint_map.html")]
	public class TintMap : MonoBehaviour
	{

		#region Variables

		/** An optional texture to make use of. If this field is null, then the texture found on the attached MeshRenderer's material will be used instead */
		public Texture2D tintMapTexture;
		/** A Color to multiply the textured color by */
		public Color colorModifier = Color.white;
		/** If True, then the MeshRenderer component will be disabled automatically when the game begins */
		public bool disableRenderer = true;

		protected Texture2D actualTexture;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			AssignTexture (tintMapTexture);

			if (disableRenderer)
			{
				GetComponent <MeshRenderer>().enabled = false;
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Gets the colour tint at a specific position in the scene.</summary>
		 * <param name = "position">The 2D position in the scene to get the colour tint at</param>
		 * <param name = "intensity">The intensity of the effect, where 0 = no effect, 1 = fully tinted</param>
		 * <param name = "alpha">The alpha value to give the final Color</param>
		 * <returns>The colour tint. If no appropriate texture is found, Color.white will be returned</returns>
		 */
		public Color GetColorData (Vector2 position, float intensity = 1f, float alpha = 1f)
		{
			if (actualTexture != null && intensity > 0f)
			{
				RaycastHit hit;
				var ray = new Ray (new Vector3 (position.x, position.y, transform.position.z - 0.0005f), Vector3.forward);
				if (!Physics.Raycast (ray, out hit, 0.001f))
				{
					return new Color (1f, 1f, 1f, alpha) * colorModifier;
				}
				Vector2 pixelUV = hit.textureCoord;

				if (intensity >= 1f)
				{
					Color fullyTintedColor = actualTexture.GetPixelBilinear (pixelUV.x, pixelUV.y);
					fullyTintedColor.a = alpha;
					return fullyTintedColor * colorModifier;
				}
				Color newColour = Color.Lerp (Color.white, actualTexture.GetPixelBilinear (pixelUV.x, pixelUV.y) * colorModifier, intensity);
				return new Color (newColour.r, newColour.g, newColour.b, alpha);
			}
			//return new Color (intensity, intensity, intensity, alpha) * colorModifier;
			//return new Color (1f, 1f, 1f, alpha) * colorModifier;
			else
			{
				Color newColour = Color.Lerp (Color.white, new Color (1f, 1f, 1f, alpha) * colorModifier, intensity);
				return new Color (newColour.r, newColour.g, newColour.b, alpha);
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void AssignTexture (Texture2D newTexture = null)
		{
			if (GetComponent <MeshRenderer>().material)
			{
				if (newTexture)
				{
					GetComponent <MeshRenderer>().material.mainTexture = newTexture;
				}
				actualTexture = (Texture2D) GetComponent <MeshRenderer>().material.mainTexture;
			}
		}

		#endregion

	}

}