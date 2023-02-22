#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage which music tracks can be played in-game.
	 */
	public class MusicStorageWindow : SoundtrackStorageWindow
	{

		[MenuItem ("Adventure Creator/Editors/Soundtrack/Music storage", false, 6)]
		public static void Init ()
		{
			Init <MusicStorageWindow> ("Music storage");
		}


		protected override List<MusicStorage> Storages
		{
			get
			{
				return KickStarter.settingsManager.musicStorages;
			}
			set
			{
				KickStarter.settingsManager.musicStorages = value;
			}
		}


		protected override string APIPrefix
		{
			get
			{
				return "AC.KickStarter.settingsManager.musicStorages";
			}
		}


		protected void OnGUI ()
		{
			if (AdvGame.GetReferences().settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField (titleContent.text, CustomStyles.managerHeader);

			if (KickStarter.settingsManager)
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showOptions = CustomGUILayout.ToggleHeader (showOptions, "Music settings");
				if (showOptions)
				{
					KickStarter.settingsManager.playMusicWhilePaused = CustomGUILayout.ToggleLeft ("Can play when game is paused?", KickStarter.settingsManager.playMusicWhilePaused, "AC.KickStarter.settingsManager.playMusicWhilePaused", "If True, then music can play when the game is paused");
					KickStarter.settingsManager.loadMusicFadeTime = CustomGUILayout.Slider ("Fade time after loading:", KickStarter.settingsManager.loadMusicFadeTime, 0f, 5f, "AC.KickStarter.settingsManager.loadMusicFadeTime", "The fade-in duration when resuming music audio after loading a save game");
					if (KickStarter.settingsManager.loadMusicFadeTime > 0f)
					{
						KickStarter.settingsManager.crossfadeMusicWhenLoading = CustomGUILayout.ToggleLeft ("Crossfade after loading?", KickStarter.settingsManager.crossfadeMusicWhenLoading, "AC.KickStarter.settingsManager.crossfadeMusicWhenLoading", "If True, previously-playing music audio will be crossfaded out upon loading");
					}
					KickStarter.settingsManager.restartMusicTrackWhenLoading = CustomGUILayout.ToggleLeft ("Restart track after loading?", KickStarter.settingsManager.restartMusicTrackWhenLoading, "AC.KickStarter.settingsManager.restartMusicTrackWhenLoading", "If True, then the music track at the time of saving will be resumed from the start upon loading");
					KickStarter.settingsManager.autoEndOtherMusicWhenPlayed = CustomGUILayout.ToggleLeft ("Auto-end all Music when play?", KickStarter.settingsManager.autoEndOtherMusicWhenPlayed, "AC.KickStarter.settingsManager.autoEndOtherMusicWhenPlayed", "If True, then playing Music will force all other Sounds in the scene to stop if they are also playing Music");
					KickStarter.settingsManager.musicPrefabOverride = (Music) CustomGUILayout.ObjectField<Music> ("Music prefab (override):", KickStarter.settingsManager.musicPrefabOverride, false, "AC.KickStarter.settingsManager.musicPrefabOverride", "If set, this prefab will replace the default Music object");
					filter = EditorGUILayout.TextField ("Name filter:", filter);

					if (GUI.changed)
					{
						EditorUtility.SetDirty (KickStarter.settingsManager);
					}
				}

				EditorGUILayout.Space ();
				CustomGUILayout.EndVertical ();
			}

			SharedGUI ("Music tracks");
		}

	}
	
}

#endif