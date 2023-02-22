#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{

	/** Provides an EditorWindow to manage phoneme settings */
	public class PhonemesWindow : EditorWindow
	{

		private SpeechManager speechManager;
		private Vector2 scrollPos;


		/** Initialises the window. */
		public static void Init ()
		{
			PhonemesWindow window = (PhonemesWindow) GetWindow (typeof (PhonemesWindow));
			window.titleContent.text = "Phonemes editor";
			window.position = new Rect (300, 200, 450, 270);
			window.minSize = new Vector2 (300, 160);
		}


		private void OnEnable ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().speechManager)
			{
				speechManager = AdvGame.GetReferences ().speechManager;
			}
		}


		private void OnGUI ()
		{
			if (speechManager == null)
			{
				return;
			}

			EditorGUILayout.LabelField ("Phonemes editor", CustomStyles.managerHeader);

			speechManager.phonemes = ShowPhonemesGUI (speechManager.phonemes, speechManager.lipSyncMode);

			if (GUI.changed)
			{
				EditorUtility.SetDirty (speechManager);
			}
		}


		private List<string> ShowPhonemesGUI (List<string> phonemes, LipSyncMode mode)
		{
			EditorGUILayout.HelpBox ("Sort letters or phoneme sounds into groups below, with each group representing a different animation frame.  Separate sounds with a forward slash (/).\nThe first frame will be considered the default.", MessageType.Info);
			EditorGUILayout.Space ();

			scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

			for (int i=0; i<phonemes.Count; i++)
			{
				EditorGUILayout.BeginHorizontal ();
				phonemes [i] = EditorGUILayout.TextField ("Frame #" + i.ToString () + ":", phonemes [i]);

				if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
				{
					PhonemeSideMenu (i);
				}
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.Space ();
			EditorGUILayout.EndScrollView ();

			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Create new frame"))
			{
				phonemes.Add (string.Empty);
			}
			if (GUILayout.Button ("Revert to defaults"))
			{
				Undo.RecordObject (speechManager, "Revert phonemes");
				phonemes = SetDefaults (mode);
			}
			EditorGUILayout.EndHorizontal ();

			return phonemes;
		}
	

		private List<string> SetDefaults (LipSyncMode mode)
		{
			List<string> phonemes = new List<string>();

			if (mode == LipSyncMode.ReadPamelaFile)
			{
				phonemes.Add ("B/M/P/ ");
				phonemes.Add ("EH0/EH1/EH2/ER0/ER1/ER2/EY0/EY1/EY2/IY0/IY1/IY2");
				phonemes.Add ("CH/G/HH/IH0/IH1/IH2/JH/K/R/S/SH/Y/Z/ZH");
				phonemes.Add ("F/V");
				phonemes.Add ("D/DH/L/N/NG");
				phonemes.Add ("AA0/AA1/AA2/AE0/AE1/AE2/AH0/AH1/AH2/AY0/AY1/AY2");
				phonemes.Add ("AO0/AO1/AO2/AW0/AW1/AW2/OW0/OW1/OW2");
				phonemes.Add ("T/TH");
				phonemes.Add ("OY0/OY1/OY2/UH0/UH1/UH2/UW0/UW1/UW2/W");
			}
			else if (mode == LipSyncMode.FromSpeechText || mode == LipSyncMode.ReadSapiFile || mode == LipSyncMode.ReadPapagayoFile)
			{
				phonemes.Add ("B/M/P/MBP/ ");
				phonemes.Add ("AY/AH/IH/EY/ER");
				phonemes.Add ("G/O/OO/OH/W");
				phonemes.Add ("SH/R/Z/SF/D/L/F/TN/K/N/NG/H/X/FV");
				phonemes.Add ("UH/EH/DH/AE/IY");
			}

			return phonemes;
		}


		private void PhonemeSideMenu (int i)
		{
			GUI.SetNextControlName (string.Empty);
			GUI.FocusControl (string.Empty);

			selectedFrameIndex = i;
			GenericMenu menu = new GenericMenu ();
			
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");

			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (i < speechManager.phonemes.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}
			
			menu.ShowAsContext ();
		}


		private int selectedFrameIndex;
		private void Callback (object obj)
		{
			int i = selectedFrameIndex;
			string oldFrame = speechManager.phonemes[i];
				
			switch (obj.ToString ())
			{
			case "Insert after":
				Undo.RecordObject (speechManager, "Add phoneme frame");
				speechManager.phonemes.Insert (i+1, "");
				break;
				
			case "Delete":
				Undo.RecordObject (speechManager, "Delete phoneme frame");
				speechManager.phonemes.RemoveAt (i);
				break;
				
			case "Move up":
				Undo.RecordObject (speechManager, "Move phoneme frame up");
				speechManager.phonemes.RemoveAt (i);
				speechManager.phonemes.Insert (i-1, oldFrame);
				break;
				
			case "Move down":
				Undo.RecordObject (speechManager, "Move phoneme frame down");
				speechManager.phonemes.RemoveAt (i);
				speechManager.phonemes.Insert (i+1, oldFrame);
				break;

			case "Move to top":
				Undo.RecordObject (speechManager, "Move phoneme frame to top");
				speechManager.phonemes.RemoveAt (i);
				speechManager.phonemes.Insert (0, oldFrame);
				break;
			
			case "Move to bottom":
				Undo.RecordObject (speechManager, "Move phoneme frame to bottom");
				speechManager.phonemes.RemoveAt (i);
				speechManager.phonemes.Insert (speechManager.phonemes.Count, oldFrame);
				break;
			}

			EditorUtility.SetDirty (speechManager);
		}

	}

}

#endif