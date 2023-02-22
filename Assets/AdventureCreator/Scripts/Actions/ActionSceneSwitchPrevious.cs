/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionScene.cs"
 * 
 *	This action loads a new scene.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionSceneSwitchPrevious : Action
	{
		
		public bool useActivePlayer = false;

		public bool assignScreenOverlay;
		public bool onlyPreload = false;

		public bool relativePosition = false;
		public Marker relativeMarker;
		protected Marker runtimeRelativeMarker;
		public int relativeMarkerID;
		public int relativeMarkerParameterID = -1;


		public override ActionCategory Category { get { return ActionCategory.Scene; }}
		public override string Title { get { return "Switch previous"; }}
		public override string Description { get { return "Moves the Player to the previously-loaded scene. By default, the screen will cut to black during the transition, but the last frame of the current scene can instead be overlayed. This allows for cinematic effects: if the next scene fades in, it will cause a crossfade effect; if the next scene doesn't fade, it will cause a straight cut."; }}
		public override int NumSockets { get { return (onlyPreload) ? 1 : 0; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeRelativeMarker = AssignFile <Marker> (parameters, relativeMarkerParameterID, relativeMarkerID, relativeMarker);
		}
		
		
		public override float Run ()
		{
			bool runtimeActivePlayer = useActivePlayer;

			if (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				runtimeActivePlayer = false;
			}

			if (!onlyPreload && relativePosition && runtimeRelativeMarker != null)
			{
				KickStarter.sceneChanger.SetRelativePosition (runtimeRelativeMarker);
			}

			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					string previousSceneName = GetSceneName (runtimeActivePlayer);
					if (string.IsNullOrEmpty (previousSceneName))
					{
						LogWarning ("Cannot load previous scene as there is no data stored - is this the first scene in the game?");
						return 0f;
					}

					if (onlyPreload && !relativePosition)
					{
						if (AdvGame.GetReferences ().settingsManager.useAsyncLoading)
						{
							KickStarter.sceneChanger.PreloadScene (previousSceneName);
						}
						else
						{
							LogWarning ("To pre-load scenes, 'Load scenes asynchronously?' must be enabled in the Settings Manager.");
						}
					}
					else
					{
						KickStarter.sceneChanger.ChangeScene (previousSceneName, true, false, assignScreenOverlay);
					}
					break;

				case ChooseSceneBy.Number:
				default:
					int previousSceneIndex = GetSceneIndex (runtimeActivePlayer);
					if (previousSceneIndex < 0)
					{
						LogWarning ("Cannot load previous scene as there is no data stored - is this the first scene in the game?");
						return 0f;
					}

					if (onlyPreload && !relativePosition)
					{
						if (AdvGame.GetReferences ().settingsManager.useAsyncLoading)
						{
							KickStarter.sceneChanger.PreloadScene (previousSceneIndex);
						}
						else
						{
							LogWarning ("To pre-load scenes, 'Load scenes asynchronously?' must be enabled in the Settings Manager.");
						}
					}
					else
					{
						KickStarter.sceneChanger.ChangeScene (previousSceneIndex, true, false, assignScreenOverlay);
					}
					break;
			}

			return 0f;
		}


		private int GetSceneIndex (bool runtimeActivePlayer)
		{
			if (runtimeActivePlayer)
			{
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (KickStarter.saveSystem.CurrentPlayerID);
				if (playerData != null)
				{
					return playerData.previousScene;
				}
			}
			return KickStarter.sceneChanger.PreviousSceneIndex;
		}


		private string GetSceneName (bool runtimeActivePlayer)
		{
			if (runtimeActivePlayer)
			{
				PlayerData playerData = KickStarter.saveSystem.GetPlayerData (KickStarter.saveSystem.CurrentPlayerID);
				if (playerData != null)
				{
					return playerData.previousSceneName;
				}
			}
			return KickStarter.sceneChanger.PreviousSceneName;
		}


#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				useActivePlayer = EditorGUILayout.ToggleLeft ("Player's previous scene, not game's?", useActivePlayer);
			}

			onlyPreload = EditorGUILayout.ToggleLeft ("Don't change scene, just preload data?", onlyPreload);

			if (!onlyPreload)
			{
				relativePosition = EditorGUILayout.ToggleLeft ("Position Player relative to Marker?", relativePosition);
				if (relativePosition)
				{
					relativeMarkerParameterID = Action.ChooseParameterGUI ("Relative Marker:", parameters, relativeMarkerParameterID, ParameterType.GameObject);
					if (relativeMarkerParameterID >= 0)
					{
						relativeMarkerID = 0;
						relativeMarker = null;
					}
					else
					{
						relativeMarker = (Marker) EditorGUILayout.ObjectField ("Relative Marker:", relativeMarker, typeof(Marker), true);
						
						relativeMarkerID = FieldToID (relativeMarker, relativeMarkerID);
						relativeMarker = IDToField (relativeMarker, relativeMarkerID, false);
					}
				}
			}

			if (onlyPreload)
			{
				if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.useAsyncLoading)
				{}
				else
				{
					EditorGUILayout.HelpBox ("To pre-load scenes, 'Load scenes asynchronously?' must be enabled in the Settings Manager.", MessageType.Warning);
				}
			}
			else
			{
				assignScreenOverlay = EditorGUILayout.ToggleLeft ("Overlay current screen during switch?", assignScreenOverlay);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (relativeMarker, relativeMarkerID, relativeMarkerParameterID);
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (relativePosition && relativeMarkerParameterID < 0)
			{
				if (relativeMarker && relativeMarker.gameObject == gameObject) return true;
				if (relativeMarkerID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Scene: Switch previous' Action</summary>
		 * <param name = "overlayCurrentScreen">If True, the original scene will be displayed during the switch to mask the transition</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSceneSwitchPrevious CreateNew (bool overlayCurrentScreen)
		{
			ActionSceneSwitchPrevious newAction = CreateNew<ActionSceneSwitchPrevious> ();
			newAction.assignScreenOverlay = overlayCurrentScreen;
			return newAction;
		}
		
	}

}