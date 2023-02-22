/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SubScene.cs"
 * 
 *	This component keeps track of data regarding any scene added to the active scene during gameplay.
 *	It is generated automatically by the sub-scene's MultiSceneChecker, and should not be added manually.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace AC
{

	/**
	 *	This component keeps track of data regarding any scene added to the active scene during gameplay.
	 *	It is generated automatically by the sub-scene's MultiSceneChecker, and should not be added manually.
	 */
	public class SubScene : MonoBehaviour
	{

		#region Variables

		protected int sceneIndex;
		protected string sceneName;

		protected LocalVariables localVariables;
		protected SceneSettings sceneSettings;

		protected KickStarter kickStarter;
		protected MainCamera mainCamera;

		[SerializeField] bool selfInitialise = false;

		#endregion


		#region UnityStandards

		private void Awake ()
		{
			if (selfInitialise && KickStarter.sceneChanger)
			{
				sceneIndex = gameObject.scene.buildIndex;
				sceneName = gameObject.scene.name;
				KickStarter.sceneChanger.RegisterSubScene (this);
			}
		}

		private void OnDestroy ()
		{
			if (KickStarter.sceneChanger)
			{
				KickStarter.sceneChanger.UnregisterSubScene (this);
			}
		}

		#endregion


		#region PublicFunctions

		/**
		* <summary>Syncs the component with the correct scene.</summary>
		* <param name = "_multiSceneChecker">The MultiSceneChecker component in the scene for which this component is to sync with.</param>
		*/
		public void Initialise (MultiSceneChecker _multiSceneChecker)
		{
			Scene scene = _multiSceneChecker.gameObject.scene;

			kickStarter = _multiSceneChecker.GetComponent <KickStarter>();

			sceneIndex = scene.buildIndex;
			sceneName = scene.name;
			gameObject.name = "SubScene " + sceneIndex;

			localVariables = _multiSceneChecker.GetComponent <LocalVariables>();
			sceneSettings = _multiSceneChecker.GetComponent <SceneSettings>();

			UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (gameObject, scene);

			kickStarter = _multiSceneChecker.GetComponent<KickStarter> ();
			_multiSceneChecker.gameObject.SetActive (false);

			mainCamera = UnityVersionHandler.GetOwnSceneInstance <MainCamera> (gameObject);
			if (mainCamera)
			{
				mainCamera.gameObject.SetActive (false);
			}

			Player ownPlayer = UnityVersionHandler.GetOwnSceneInstance <Player> (gameObject);
			if (ownPlayer)
			{
				ownPlayer.gameObject.SetActive (false);
			}
			if (sceneSettings.OverridesCameraPerspective ())
			{
				ACDebug.LogError ("The added scene (" + scene.name + ", " + scene.buildIndex + ") overrides the default camera perspective - this feature should not be used in conjunction with multiple-open scenes.", gameObject);
			}

			if (KickStarter.sceneChanger == null)
			{
				ACDebug.LogWarning ("Cannot register " + scene.name + " as a sub-scene - no SceneChanger component was found. Is the main scene an AC scene?", this);
				return;
			}

			KickStarter.sceneChanger.RegisterSubScene (this);
		}


		/** Prepares the sub-scene to become the new active scene, due to the active scene being removed.  The gameobject will be destroyed afterwards. */
		public void MakeMain ()
		{
			if (mainCamera)
			{
				mainCamera.gameObject.SetActive (true);
				if (KickStarter.settingsManager.blackOutWhenInitialising)
				{
					mainCamera.ForceOverlayForFrames (4);
				}
			}
			if (kickStarter)
			{
				kickStarter.gameObject.SetActive (true);
				KickStarter.SetGameEngine (kickStarter.gameObject);
				if (mainCamera)
				{
					KickStarter.mainCamera = mainCamera;
				}
			}

			KickStarter.sceneChanger.SubScenes.Remove (this);
			Destroy (gameObject);
		}

		#endregion


		#region GetSet

		/** Gets the build index of the scene that this component represents. */
		public int SceneIndex
		{
			get
			{
				return sceneIndex;
			}
		}


		/** Gets the name the scene that this component represents. */
		public string SceneName
		{
			get
			{
				return sceneName;
			}
		}


		/** Gets the LocalVariables for the scene that this component represents. */
		public LocalVariables LocalVariables
		{
			get
			{
				return localVariables;
			}
		}


		/** Gets the SceneSettings for the scene that this component represents. */
		public SceneSettings SceneSettings
		{
			get
			{
				return sceneSettings;
			}
		}

		#endregion

	}

}