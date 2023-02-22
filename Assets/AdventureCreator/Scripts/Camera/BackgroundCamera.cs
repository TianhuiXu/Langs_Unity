/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"BackgroundCamera.cs"
 * 
 *	The BackgroundCamera is used to display background images underneath the scene geometry.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This is used to display background images underneath scene geometry in 2.5D games.
	 * It should not normally render anything other than a BackgroundImage.
	 */
	[RequireComponent (typeof (Camera))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_background_camera.html")]
	public class BackgroundCamera : MonoBehaviour
	{

		#region Variables
		
		protected Camera _camera;

		#endregion


		#region UnityStandards		
		
		protected void Awake ()
		{
			_camera = GetComponent <Camera>();
			
			UpdateRect ();
			SetCorrectLayer ();
		}


		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}

		#endregion


		#region PublicFunctions

		/**
		 * Updates the Camera's Rect.
		 * 
		 */
		public void UpdateRect ()
		{
			if (_camera == null)
			{
				_camera = GetComponent <Camera>();
			}
			_camera.rect = KickStarter.CameraMain.rect;
		}

		#endregion


		#region ProtectedFunctions

		protected void SetCorrectLayer ()
		{
			if (KickStarter.settingsManager)
			{
				if (LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer) == -1)
				{
					ACDebug.LogWarning ("No '" + KickStarter.settingsManager.backgroundImageLayer + "' layer exists - please define one in the Tags Manager.");
				}
				else
				{
					GetComponent <Camera>().cullingMask = (1 << LayerMask.NameToLayer (KickStarter.settingsManager.backgroundImageLayer));
				}
			}
			else
			{
				ACDebug.LogWarning ("A Settings Manager is required for this camera type");
			}
		}

		#endregion


		#region Instance

		protected static BackgroundCamera instance;
		public static BackgroundCamera Instance
		{
			get
			{
				if (instance == null)
				{ 
					instance = (BackgroundCamera) Object.FindObjectOfType <BackgroundCamera>();
				}
				#if UNITY_EDITOR
				if (instance == null)
				{
					GameObject newOb = SceneManager.AddPrefab ("Automatic", "BackgroundCamera", false, false, false);
					instance = newOb.GetComponent <BackgroundCamera>();
				}
				#endif
				instance.SetCorrectLayer ();
				return instance;
			}
		}

		#endregion
		
	}
	
}