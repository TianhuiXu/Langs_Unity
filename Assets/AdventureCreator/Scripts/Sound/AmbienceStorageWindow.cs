#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage which ambience tracks can be played in-game. */
	public class AmbienceStorageWindow : SoundtrackStorageWindow
	{

		[MenuItem ("Adventure Creator/Editors/Soundtrack/Ambience storage", false, 6)]
		public static void Init ()
		{
			Init <AmbienceStorageWindow> ("Ambience storage");
		}


		protected override List<MusicStorage> Storages
		{
			get
			{
				return KickStarter.settingsManager.ambienceStorages;
			}
			set
			{
				KickStarter.settingsManager.ambienceStorages = value;
			}
		}


		protected override string APIPrefix
		{ 
			get
			{
				return "AC.KickStarter.settingsManager.ambienceStorages";
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
				showOptions = CustomGUILayout.ToggleHeader (showOptions, "Ambience settings");
				if (showOptions)
				{
					KickStarter.settingsManager.playAmbienceWhilePaused = CustomGUILayout.ToggleLeft ("Can play when game is paused?", KickStarter.settingsManager.playAmbienceWhilePaused, "AC.KickStarter.settingsManager.playAmbienceWhilePaused", "If True, then ambience can play when the game is paused");
					KickStarter.settingsManager.loadAmbienceFadeTime = CustomGUILayout.Slider ("Fade time after loading:", KickStarter.settingsManager.loadAmbienceFadeTime, 0f, 5f, "AC.KickStarter.settingsManager.loadAmbienceFadeTime", "The fade-in duration when resuming ambience audio after loading a save game");
					if (KickStarter.settingsManager.loadAmbienceFadeTime > 0f)
					{
						KickStarter.settingsManager.crossfadeAmbienceWhenLoading = CustomGUILayout.ToggleLeft ("Crossfade after loading?", KickStarter.settingsManager.crossfadeAmbienceWhenLoading, "AC.KickStarter.settingsManager.crossfadeAmbienceWhenLoading", "If True, previously-playing ambience audio will be crossfaded out upon loading");
					}
					KickStarter.settingsManager.restartAmbienceTrackWhenLoading = CustomGUILayout.ToggleLeft ("Restart track after loading?", KickStarter.settingsManager.restartAmbienceTrackWhenLoading, "AC.KickStarter.settingsManager.restartAmbienceTrackWhenLoading", "If True, then the ambience track at the time of saving will be resumed from the start upon loading");
					KickStarter.settingsManager.ambiencePrefabOverride = (Ambience) CustomGUILayout.ObjectField<Ambience> ("Ambience prefab (override):", KickStarter.settingsManager.ambiencePrefabOverride, false, "AC.KickStarter.settingsManager.ambiencePrefabOverride", "If set, this prefab will replace the default Ambience object");
					filter = EditorGUILayout.TextField ("Name filter:", filter);

					if (GUI.changed)
					{
						EditorUtility.SetDirty (KickStarter.settingsManager);
					}
				}

				EditorGUILayout.Space ();
				CustomGUILayout.EndVertical ();
			}

			SharedGUI ("Ambience tracks");
		}

	}
	
}

#endif