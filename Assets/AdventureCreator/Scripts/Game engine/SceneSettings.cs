/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SceneSettings.cs"
 * 
 *	This script defines which cutscenes play when the scene is loaded,
 *	and where the player should begin from.
 * 
 */

#if UNITY_STANDALONE && !UNITY_2018_2_OR_NEWER
#define ALLOW_MOVIETEXTURES
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component is where settings specific to a scene are stored, such as the navigation method, and the Cutscene to play when the scene begins.
	 * The SceneManager provides a UI to assign these fields.
	 * This component should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_scene_settings.html")]
	public class SceneSettings : MonoBehaviour, iActionListAssetReferencer
	{

		#region Variables

		/** The source of Actions used for the scene's main cutscenes (InScene, AssetFile) */
		public ActionListSource actionListSource = ActionListSource.InScene;

		/** The Cutscene to run whenever the game beings from this scene, or when this scene is visited during gameplay, if actionListSource = ActionListSource.InScene */
		public Cutscene cutsceneOnStart;
		/** The Cutscene to run whenever this scene is loaded after restoring a saved game file, if actionListSource = ActionListSource.InScene */
		public Cutscene cutsceneOnLoad;
		/** The Cutscene to run whenever a variable's value is changed, if actionListSource = ActionListSource.InScene */
		public Cutscene cutsceneOnVarChange;

		/** The ActionListAsset to run whenever the game beings from this scene, or when this scene is visited during gameplay, if actionListSource = ActionListSource.AssetFile */
		public ActionListAsset actionListAssetOnStart;
		/** The ActionListAsset to run whenever this scene is loaded after restoring a saved game file, if actionListSource = ActionListSource.AssetFile */
		public ActionListAsset actionListAssetOnLoad;
		/** The ActionListAsset to run whenever a variable's value is changed, if actionListSource = ActionListSource.AssetFile */
		public ActionListAsset actionListAssetOnVarChange;

		/** The scene's default PlayerStart prefab */
		public PlayerStart defaultPlayerStart;
		/** The scene's navigation method (meshCollider, UnityNavigation, PolygonCollider) */
		public AC_NavigationMethod navigationMethod = AC_NavigationMethod.UnityNavigation;
		/** The class name of the NavigationEngine ScriptableObject that is used to handle pathfinding, if navigationMethod = AC_NavigationMethod.Custom */
		public string customNavigationClass;
		/** The scene's active NavigationMesh, if navigationMethod != AC_NavigationMethod.UnityNavigation */
		public NavigationMesh navMesh;
		/** The scene's default SortingMap prefab */
		public SortingMap sortingMap;
		/** The scene's default Sound prefab */
		public Sound defaultSound;
		/** The scene's default LightMap prefab */
		public TintMap tintMap;

		/** The scene's attributes */
		public List<InvVar> attributes = new List<InvVar>();

		/** If this is assigned, and the currently-loaded Manager assets do not match those defined within, then a Warning message will appear in the Console */
		public ManagerPackage requiredManagerPackage;

		/** If True, then the global verticalReductionFactor in SettingsManager will be overridden with a scene-specific value */
		public bool overrideVerticalReductionFactor = false;
		/** How much slower vertical movement is compared to horizontal movement, if the game is in 2D and overriderVerticalReductionFactor = True */
		public float verticalReductionFactor = 0.7f;

		/** The distance to offset a character by when it is in the same area of a SortingMap as another (to correct display order) */
		public float sharedLayerSeparationDistance = 0.001f;

		[SerializeField] protected bool overrideCameraPerspective = false;
		public CameraPerspective cameraPerspective;
		[SerializeField] protected MovingTurning movingTurning = MovingTurning.Unity2D;

		protected AudioSource defaultAudioSource;

		#if UNITY_EDITOR
		public bool visibilityHotspots = true;
		public bool visibilityTriggers = true;
		public bool visibilityCollision = true;
		public bool visibilityMarkers = true;
		public bool visibilityPlayerStarts = true;
		public bool visibilityNavMesh = true;
		#endif

		#endregion


		#region UnityStandards

		public void OnInitGameEngine ()
		{
			KickStarter.navigationManager.OnAwake (navMesh);
		}


		public void OnStart ()
		{
			AssignPlayerStart ();
			PlayStartCutscene ();
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Finds the appropriate PlayerStart to refer to, based on the last scene's conditions, and sets the Player there</summary>
		 * <param name = "onlySetCamera">If True, then the Player will not be moved - and only the camera will be switched to, if one was assigned</param>
		 **/
		public void AssignPlayerStart ()
		{
			PlayerStart playerStart = GetPlayerStart (KickStarter.saveSystem.CurrentPlayerID);
			if (playerStart)
			{
				playerStart.PlacePlayerAt ();
			}
			else if (KickStarter.player)
			{
				ACDebug.LogWarning ("No default PlayerStart was found.  The Player and camera may not be set up correctly until one is defined in the Scene Manager.");
			}
		}


		/** Assigns a new SortingMap */
		public void SetSortingMap (SortingMap _sortingMap)
		{
			sortingMap = _sortingMap;
			
			if (KickStarter.stateHandler)
			{
				foreach (FollowSortingMap followSortingMap in KickStarter.stateHandler.FollowSortingMaps)
				{
					followSortingMap.UpdateSortingMap ();
				}
			}
		}


		/** Assigns a new TintMap */
		public void SetTintMap (TintMap _tintMap)
		{
			tintMap = _tintMap;

			// Reset all FollowTintMap components
			FollowTintMap[] followTintMaps = FindObjectsOfType (typeof (FollowTintMap)) as FollowTintMap[];
			foreach (FollowTintMap followTintMap in followTintMaps)
			{
				followTintMap.ResetTintMap ();
			}
		}


		/**
		 * <summary>Gets the appropriate PlayerStart to use when the scene begins.</summary>
		 * <returns>The appropriate PlayerStart to use when the scene begins</returns>
		 */
		public PlayerStart GetPlayerStart (int playerID)
		{
			PlayerStart[] startersArray = FindObjectsOfType (typeof (PlayerStart)) as PlayerStart[];

			List<PlayerStart> starters = new List<PlayerStart>();
			foreach (PlayerStart starter in startersArray)
			{
				if (defaultPlayerStart == null || starter != defaultPlayerStart)
				{
					starters.Add (starter);
				}
			}

			if (defaultPlayerStart && !starters.Contains (defaultPlayerStart))
			{
				starters.Add (defaultPlayerStart);
			}

			foreach (PlayerStart starter in starters)
			{
				if (starter.MatchesPreviousScene (playerID))
				{
					return starter;
				}
			}
			
			if (defaultPlayerStart)
			{
				return defaultPlayerStart;
			}
			
			return null;
		}


		/**
		 * Runs the "cutsceneOnLoad" Cutscene.
		 */
		public void OnLoad ()
		{
			if (actionListSource == ActionListSource.InScene)
			{
				if (cutsceneOnLoad)
				{
					cutsceneOnLoad.Interact ();
				}
			}
			else if (actionListSource == ActionListSource.AssetFile)
			{
				if (actionListAssetOnLoad)
				{
					actionListAssetOnLoad.Interact ();
				}
			}
		}


		/**
		 * <summary>Plays an AudioClip on the default Sound prefab.</summary>
		 * <param name = "audioClip">The AudioClip to play</param>
		 * <param name = "doLoop">If True, the sound will loop</param>
		 * <param name="avoidRestarting">If True, then the sound will not play if the same clip is already playing</param>
		 */
		public void PlayDefaultSound (AudioClip audioClip, bool doLoop, bool avoidRestarting = false)
		{
			if (audioClip == null) return;
			if (defaultSound == null)
			{
				ACDebug.Log ("Cannot play audio '" + audioClip.name + "' since no Default Sound is defined in the scene - please assign one in the Scene Manager.", audioClip);
				return;
			}

			if (KickStarter.stateHandler.IsPaused () && !defaultSound.playWhilePaused)
			{
				ACDebug.LogWarning ("Cannot play audio '" + audioClip.name + "' on Sound '" + defaultSound.gameObject.name + "' while the game is paused - check 'Play while game paused?' on the Sound component's Inspector.", defaultSound);
			}

			if (defaultAudioSource == null)
			{
				defaultAudioSource = defaultSound.GetComponent<AudioSource> ();
			}

			if (doLoop)
			{
				defaultAudioSource.clip = audioClip;
				defaultSound.Play (doLoop);
			}
			else if (avoidRestarting)
			{
				if (defaultAudioSource.clip == audioClip && defaultSound.IsPlaying ())
				{
					return;
				}

				defaultAudioSource.clip = audioClip;
				defaultSound.Play(false);
			}
			else
			{
				defaultSound.SetMaxVolume ();
				defaultAudioSource.PlayOneShot (audioClip);
			}
		}


		/**
		 * <summary>Gets how much slower vertical movement is compared to horizontal movement, if the game is in 2D.</summary>
		 * <returns>Gets how much slower vertical movement is compared to horizontal movement</returns>
		 */
		public float GetVerticalReductionFactor ()
		{
			if (overrideVerticalReductionFactor)
			{
				return verticalReductionFactor;
			}
			return KickStarter.settingsManager.verticalReductionFactor;
		}


		public bool OverridesCameraPerspective ()
		{
			if (overrideCameraPerspective)
			{
				if (KickStarter.settingsManager.cameraPerspective != cameraPerspective)
				{
					return true;
				}

				if (cameraPerspective == CameraPerspective.TwoD)
				{
					if (KickStarter.settingsManager.movingTurning != movingTurning)
					{
						return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Gets a scene attribute.</summary>
		 * <param name = "ID">The ID number of the attribute to get</param>
		 * <returns>The attribute of the scene</returns>
		 */
		public InvVar GetAttribute (int ID)
		{
			if (ID >= 0)
			{
				foreach (InvVar attribute in attributes)
				{
					if (attribute.id == ID)
					{
						return attribute;
					}
				}
			}
			return null;
		}

		#endregion


		#region ProtectedFunctions

		protected void PlayStartCutscene ()
		{
			KickStarter.stateHandler.PlayGlobalOnStart ();

			KickStarter.eventManager.Call_OnStartScene ();

			switch (actionListSource)
			{
				case ActionListSource.InScene:
					if (cutsceneOnStart)
					{
						cutsceneOnStart.Interact ();
					}
					break;

				case ActionListSource.AssetFile:
					if (actionListAssetOnStart)
					{
						actionListAssetOnStart.Interact ();
					}
					break;

				default:
					break;
			}
		}

		#endregion


		#region StaticFunctions

		/**
		 * <summary>Checks if the scene is in 2D, and plays in screen-space (i.e. characters do not move towards or away from the camera).</summary>
		 * <returns>True if the game is in 2D, and plays in screen-space</returns>
		 */
		public static bool ActInScreenSpace ()
		{
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.overrideCameraPerspective)
			{
				if ((KickStarter.sceneSettings.movingTurning == MovingTurning.ScreenSpace || KickStarter.sceneSettings.movingTurning == MovingTurning.Unity2D) && KickStarter.sceneSettings.cameraPerspective == CameraPerspective.TwoD)
				{
					return true;
				}
			}
			else if (KickStarter.settingsManager)
			{
				if ((KickStarter.settingsManager.movingTurning == MovingTurning.ScreenSpace || KickStarter.settingsManager.movingTurning == MovingTurning.Unity2D) && KickStarter.settingsManager.cameraPerspective == CameraPerspective.TwoD)
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Checks if the scene uses Unity 2D for its camera perspective.<summary>
		 * <returns>True if the game uses Unity 2D for its camera perspective</returns>
		 */
		public static bool IsUnity2D ()
		{
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.overrideCameraPerspective)
			{
				if (KickStarter.sceneSettings.movingTurning == MovingTurning.Unity2D && KickStarter.sceneSettings.cameraPerspective == CameraPerspective.TwoD)
				{
					return true;
				}
			}
			else if (KickStarter.settingsManager)
			{
				if (KickStarter.settingsManager.movingTurning == MovingTurning.Unity2D && KickStarter.settingsManager.cameraPerspective == CameraPerspective.TwoD)
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if the scene uses Top Down for its camera perspective.<summary>
		 * <returns>True if the game uses Top Down for its camera perspective</returns>
		 */
		public static bool IsTopDown ()
		{
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.overrideCameraPerspective)
			{
				if (KickStarter.sceneSettings.movingTurning == MovingTurning.TopDown && KickStarter.sceneSettings.cameraPerspective == CameraPerspective.TwoD)
				{
					return true;
				}
			}
			else if (KickStarter.settingsManager)
			{
				if (KickStarter.settingsManager.movingTurning == MovingTurning.TopDown && KickStarter.settingsManager.cameraPerspective == CameraPerspective.TwoD)
				{
					return true;
				}
			}
			return false;
		}

		#endregion


		#region GetSet

		/**
		 * The camera perspective of the current scene.
		 */
		public static CameraPerspective CameraPerspective
		{
			get
			{
				if (KickStarter.sceneSettings && KickStarter.sceneSettings.overrideCameraPerspective)
				{
					return KickStarter.sceneSettings.cameraPerspective;
				}
				else if (KickStarter.settingsManager)
				{
					return KickStarter.settingsManager.cameraPerspective;
				}
				return CameraPerspective.ThreeD;
			}
		}

		#endregion


		#if UNITY_EDITOR

		protected string[] cameraPerspective_list = { "2D", "2.5D", "3D" };

		public void SetOverrideCameraPerspective (CameraPerspective _cameraPerspective, MovingTurning _movingTurning)
		{
			overrideCameraPerspective = true;
			cameraPerspective = _cameraPerspective;
			movingTurning = _movingTurning;
		}


		public void ShowCameraOverrideLabel ()
		{
			if (overrideCameraPerspective)
			{
				int cameraPerspective_int = (int) cameraPerspective;

				string persp = cameraPerspective_list[cameraPerspective_int];
				if (cameraPerspective == CameraPerspective.TwoD) persp += " (" + movingTurning + ")";
				UnityEditor.EditorGUILayout.HelpBox ("This scene's camera perspective is overriding the default and is " + persp + ".", UnityEditor.MessageType.Info);

			}
		}


		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (actionListSource == ActionListSource.AssetFile)
			{
				if (actionListAssetOnStart == actionListAsset) return true;
				if (actionListAssetOnLoad == actionListAsset) return true;
				if (actionListAssetOnVarChange == actionListAsset) return true;
			}
			return false;
		}

		#endif

	}
	
}