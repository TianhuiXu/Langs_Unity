#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage speech tags wwith */
	public class SpeechTagsWindow : EditorWindow
	{

		private Vector2 scrollPos;
		private int selectedIndex;
		

		/** Initialises the window.  */
		public static void Init ()
		{
			SpeechTagsWindow window = (SpeechTagsWindow) GetWindow (typeof (SpeechTagsWindow));
			window.titleContent.text = "Speech Tags editor";
			window.position = new Rect (300, 200, 450, 303);
			window.minSize = new Vector2 (300, 160);
		}
		
		
		private void OnGUI ()
		{
			if (AdvGame.GetReferences ().speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField ("Speech Tags editor", CustomStyles.managerHeader);

			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			EditorGUILayout.HelpBox ("Assign any labels you want to be able to tag 'Dialogue: Play speech' Actions as here.", MessageType.Info);
			EditorGUILayout.Space ();

			speechManager.useSpeechTags = EditorGUILayout.Toggle ("Use speech tags?", speechManager.useSpeechTags);
			if (speechManager.useSpeechTags)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

				if (speechManager.speechTags.Count == 0)
				{
					speechManager.speechTags.Add (new SpeechTag ("(Untagged)"));
				}
				
				for (int i=0; i<speechManager.speechTags.Count; i++)
				{
					CustomGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();

					if (i == 0)
					{
						EditorGUILayout.TextField ("Tag " + speechManager.speechTags[i].ID.ToString () + ": " + speechManager.speechTags[0].label, EditorStyles.boldLabel);
					}
					else
					{
						SpeechTag speechTag = speechManager.speechTags[i];
						speechTag.label = EditorGUILayout.DelayedTextField ("Tag " + speechManager.speechTags[i].ID.ToString () + ":", speechTag.label);
						speechManager.speechTags[i] = speechTag;

						if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
						{
							SpeechTagsSideWindow (i);
						}
					}
					EditorGUILayout.EndHorizontal ();
					CustomGUILayout.EndVertical ();
				}
			
				EditorGUILayout.EndScrollView ();
				
				if (GUILayout.Button ("Add new tag"))
				{
					Undo.RecordObject (speechManager, "Delete tag");
					speechManager.speechTags.Add (new SpeechTag (GetIDArray (speechManager.speechTags.ToArray ())));
				}
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty (speechManager);
			}
		}


		private void SpeechTagsSideWindow (int i)
		{
			GUI.SetNextControlName (string.Empty);
			GUI.FocusControl (string.Empty);

			selectedIndex = i;
			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			Undo.RecordObject (AdvGame.GetReferences ().speechManager, "Delete tag");
			AdvGame.GetReferences ().speechManager.speechTags.RemoveAt (selectedIndex);

			EditorUtility.SetDirty (AdvGame.GetReferences ().speechManager);
		}


		private int[] GetIDArray (SpeechTag[] speechTags)
		{
			List<int> idArray = new List<int>();
			foreach (SpeechTag speechTag in speechTags)
			{
				idArray.Add (speechTag.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
	}
	
}

#endif