#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	public abstract class SoundtrackStorageWindow : EditorWindow
	{

		protected Vector2 scrollPos;
		protected bool showOptions = true;
		protected bool showTracks = true;
		private MusicStorage selectedTrack;
		private int sideTrack = -1;
		private bool showSelectedTrack = true;
		protected string filter = "";


		protected static void Init <T> (string title) where T : SoundtrackStorageWindow
		{
			T window = (T) GetWindow (typeof (T));
			window.position = new Rect (300, 200, 350, 540);
			window.titleContent.text = title;
			window.minSize = new Vector2 (300, 260);
		}


		protected virtual List<MusicStorage> Storages
		{
			get
			{
				return null;
			}
			set
			{}
		}


		protected virtual string APIPrefix
		{
			get
			{
				return string.Empty;
			}
		}


		protected void SharedGUI (string headerLabel)
		{
			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;

			bool showMixerOptions = settingsManager.volumeControl == VolumeControl.AudioMixerGroups;

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showTracks = CustomGUILayout.ToggleHeader (showTracks, headerLabel);
			if (showTracks)
			{
				List<MusicStorage> storages = Storages;

				scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
				foreach (MusicStorage storage in storages)
				{
					if (!string.IsNullOrEmpty (filter) && !storage.Label.ToLower ().Contains (filter.ToLower ()))
					{ 
						continue;
					}

					EditorGUILayout.BeginHorizontal ();
					string label = storage.ID + ": " + storage.Label;
					
					if (GUILayout.Toggle (selectedTrack == storage, label, "Button"))
					{
						if (selectedTrack != storage)
						{
							DeactivateAllTracks ();
							ActivateTrack (storage);
						}
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (storage);
					}

					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Add new clip"))
				{
					Undo.RecordObject (settingsManager, "Add Track");
					storages.Add (new MusicStorage (GetIDArray (storages.ToArray ())));
				}

				if (Storages.Count > 1)
				{
					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						GlobalSideMenu ();
					}
				}
				EditorGUILayout.EndHorizontal ();
				CustomGUILayout.EndVertical ();

				Storages = storages;
			}

			EditorGUILayout.Space ();

			if (selectedTrack != null && Storages.Contains (selectedTrack))
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showSelectedTrack = CustomGUILayout.ToggleHeader (showSelectedTrack, "Track #" + selectedTrack.ID + ": " + selectedTrack.Label);
				if (showSelectedTrack)
				{
					int i = Storages.IndexOf (selectedTrack);
					selectedTrack.ShowGUI (APIPrefix + "[" + i + "]", showMixerOptions);
				}
				CustomGUILayout.EndVertical ();
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty (settingsManager);
			}
		}


		protected int[] GetIDArray (MusicStorage[] musicStorages)
		{
			List<int> idArray = new List<int>();
			foreach (MusicStorage musicStorage in musicStorages)
			{
				idArray.Add (musicStorage.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private void SideMenu (MusicStorage storage)
		{
			GenericMenu menu = new GenericMenu ();
			sideTrack = Storages.IndexOf (storage);

			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (Storages.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (sideTrack > 0 || sideTrack < Storages.Count - 1)
			{
				menu.AddSeparator ("");
			}
			if (sideTrack > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (sideTrack < Storages.Count - 1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			if (sideTrack >= 0)
			{
				MusicStorage tempStorage = Storages[sideTrack];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (KickStarter.settingsManager, "Insert Track");
						Storages.Insert (sideTrack + 1, new MusicStorage (GetIDArray (Storages.ToArray ())));
						break;

					case "Delete":
						Undo.RecordObject (KickStarter.settingsManager, "Delete Track");
						if (tempStorage == selectedTrack)
						{
							DeactivateAllTracks ();
						}
						Storages.RemoveAt (sideTrack);
						break;

					case "Move up":
						Undo.RecordObject (KickStarter.settingsManager, "Move Track up");
						Storages.RemoveAt (sideTrack);
						Storages.Insert (sideTrack - 1, tempStorage);
						break;

					case "Move down":
						Undo.RecordObject (KickStarter.settingsManager, "Move Track down");
						Storages.RemoveAt (sideTrack);
						Storages.Insert (sideTrack + 1, tempStorage);
						break;

					case "Move to top":
						Undo.RecordObject (KickStarter.settingsManager, "Move Track to top");
						Storages.RemoveAt (sideTrack);
						Storages.Insert (0, tempStorage);
						break;

					case "Move to bottom":
						Undo.RecordObject (KickStarter.settingsManager, "Move Track to bottom");
						Storages.Add (tempStorage);
						Storages.RemoveAt (sideTrack);
						break;
				}
			}

			EditorUtility.SetDirty (KickStarter.settingsManager);
			AssetDatabase.SaveAssets ();

			sideTrack = -1;
		}


		private void GlobalSideMenu ()
		{
			GenericMenu menu = new GenericMenu ();
			menu.AddItem (new GUIContent ("Delete all"), false, GlobalCallback, "Delete all");
			menu.ShowAsContext ();
		}


		private void GlobalCallback (object obj)
		{
			switch (obj.ToString ())
			{
				case "Delete all":
					Undo.RecordObject (KickStarter.settingsManager, "Delete all Tracks");
					selectedTrack = null;
					Storages.Clear ();
					break;

				default:
					break;
			}

			EditorUtility.SetDirty (KickStarter.settingsManager);
			AssetDatabase.SaveAssets ();
		}


		private void DeactivateAllTracks ()
		{
			selectedTrack = null;
		}


		private void ActivateTrack (MusicStorage storage)
		{
			selectedTrack = storage;
			EditorGUIUtility.editingTextField = false;
		}

	}
	
}

#endif