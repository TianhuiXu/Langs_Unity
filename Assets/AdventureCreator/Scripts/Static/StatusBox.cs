/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"StatusBox.cs"
 * 
 *	This script handles the display of the 'AC Status' box, which is a debug window available from the Settings Manager.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	public static class StatusBox
	{

		private static Rect debugWindowRect = new Rect (0, 0, 260, 500);
		private static GUISkin sceneManagerSkin = null;


		/** Draws the debug window in the top-left corner of the Game window */
		public static void DrawDebugWindow ()
		{
			if (KickStarter.settingsManager.showActiveActionLists != DebugWindowDisplays.Never)
			{
				#if !UNITY_EDITOR
				if (KickStarter.settingsManager.showActiveActionLists == DebugWindowDisplays.EditorOnly)
				{
					return;
				}
				#endif
				GUI.depth = KickStarter.menuManager.globalDepth + 1;
				debugWindowRect.height = 21f;
				debugWindowRect = GUILayout.Window (10, debugWindowRect, StatusWindow, "AC status", GUILayout.Width (260));
			}
		}


		private static void StatusWindow (int windowID)
		{
			if (sceneManagerSkin == null)
			{
				sceneManagerSkin = (GUISkin) Resources.Load ("SceneManagerSkin");
			}
			GUI.skin = sceneManagerSkin;

			GUILayout.Label ("Current game state: " + KickStarter.stateHandler.gameState.ToString ());

			Options.DrawStatus ();
			KickStarter.sceneChanger.DrawStatus ();

			if (KickStarter.player != null)
			{
				if (GUILayout.Button ("Current player: " + KickStarter.player.gameObject.name))
				{
					#if UNITY_EDITOR
					UnityEditor.EditorGUIUtility.PingObject (KickStarter.player.gameObject);
					#endif
				}
			}

			if (KickStarter.mainCamera != null)
			{
				KickStarter.mainCamera.DrawStatus ();
			}

			KickStarter.playerInput.DrawStatus ();
			
			GUILayout.Space (4f);

			KickStarter.actionListManager.DrawStatus ();
			KickStarter.actionListAssetManager.DrawStatus ();

			if (KickStarter.actionListManager.IsGameplayBlocked () || KickStarter.stateHandler.MovementIsOff || !KickStarter.stateHandler.CanInteract ())
			{
				GUILayout.Space (4f);
				if (KickStarter.actionListManager.IsGameplayBlocked ())
				{
					GUILayout.Label ("Gameplay is blocked");
				}
				if (KickStarter.stateHandler.MovementIsOff)
				{
					GUILayout.Label ("Movement system disabled");
				}
				if (!KickStarter.stateHandler.CanInteract ())
				{
					GUILayout.Label ("Interaction system disabled");
				}
			}
			GUI.DragWindow ();
		}

	}

}