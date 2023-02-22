/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"BackgroundImageUI.cs"
 * 
 *	The BackgroundImageUI prefab is used to control a Unity UI canvas for use in background images for 2.5D games.
 * 
 */

using UnityEngine;
using UnityEngine.UI;

namespace AC
{

	public class BackgroundImageUI : MonoBehaviour
	{

		#region Variables

		public Canvas canvas;
		public RawImage rawImage;
		public Texture emptyTexture;

		protected RectTransform rawImageRectTransform;

		#endregion


		#region UnityStandards

		protected void Start ()
		{
			if (rawImage)
			{
				rawImageRectTransform = rawImage.GetComponent <RectTransform>();
			}
			CorrectLayer ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Sets the RawImage component's texture to a given texture</summary>
		 * <param name = "texture">The texture to assign</param>
		 */
		public void SetTexture (Texture texture)
		{
			if (texture == null) return;

			if (canvas.worldCamera == null)
			{
				BackgroundCamera backgroundCamera = Object.FindObjectOfType <BackgroundCamera>();
				if (backgroundCamera)
				{
					canvas.worldCamera = backgroundCamera.GetComponent <Camera>();
				}
				else
				{
					ACDebug.LogWarning ("No 'BackgroundCamera' found - is it present in the scene? If not, drag it in from /AdventureCreator/Prefabs/Automatic.");
				}
			}

			canvas.planeDistance = 0.015f;
			rawImage.texture = texture;
		}


		/**
		 * <summary>Clears the RawImage component's texture</summary>
		 * <param name = "texture">If not null, the texture will only be cleared if this texture is currently assigned.</param>
		 */
		public void ClearTexture (Texture texture)
		{
			if (rawImage.texture == texture || texture == null)
			{
				rawImage.texture = emptyTexture;
			}
		}


		/**
		 * <summary>Creates a shake effect by scaling and repositioning the background image</summary>
		 * <param name = "intensity">The intensity of the effect</param>
		 */
		public void SetShakeIntensity (float intensity)
		{
			float scale = 1f + (intensity / 50f);
			rawImageRectTransform.localScale = Vector3.one * scale;

			float xShift = Random.Range (-intensity, intensity) * 2f;
			float yShift = Random.Range (-intensity, intensity) * 2f;

			Vector2 offset = new Vector2 (xShift, yShift);
			rawImageRectTransform.localPosition = offset;
		}

		#endregion


		#region ProtectedFunctions

		protected void AssignCamera ()
		{
			if (canvas.worldCamera == null)
			{
				BackgroundCamera backgroundCamera = Object.FindObjectOfType <BackgroundCamera>();
				if (backgroundCamera)
				{
					canvas.worldCamera = backgroundCamera.GetComponent <Camera>();
				}
				else
				{
					ACDebug.LogWarning ("No 'BackgroundCamera' found - is it present in the scene? If not, drag it in from /AdventureCreator/Prefabs/Automatic.");
				}
			}
		}


		protected void CorrectLayer ()
		{
			if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) == -1)
			{
				ACDebug.LogWarning ("No '" + KickStarter.settingsManager.backgroundImageLayer + "' layer exists - please define one in the Tags Manager.");
			}
			else
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer);
			}
		}


		#endregion


		#region Static

		private static BackgroundImageUI instance;
		public static BackgroundImageUI Instance
		{
			get
			{
				if (instance == null)
				{ 
					instance = FindObjectOfType <BackgroundImageUI>();
					if (instance == null)
					{
						GameObject newInstanceOb = Instantiate (Resource.BackgroundImageUI);
						instance = newInstanceOb.GetComponent <BackgroundImageUI>();
						newInstanceOb.name = Resource.BackgroundImageUI.name;
					}
				}
				instance.CorrectLayer ();
				instance.AssignCamera ();
				return instance;
			}
		}

		#endregion

	}

}