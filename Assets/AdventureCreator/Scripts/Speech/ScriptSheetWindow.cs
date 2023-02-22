#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage the export of script-sheets */
	public class ScriptSheetWindow : EditorWindow
	{

		private bool includeDescriptions = false;
		private bool removeTokens = false;

		private int languageIndex = 0;
		
		private bool limitToCharacter = false;
		private string characterName = "";
		
		private bool limitToTag = false;
		private int tagID = 0;

		private bool limitToMissingAudio = false;
		private Vector2 scroll;

		private string[] sceneFiles;


		/**
		 * <summary>Initialises the window.</summary>
		 * <param name = "_languageIndex">The index number of the language to select by default.</param>
		 */
		public static void Init (string[] _sceneFiles, int _languageIndex = 0)
		{
			ScriptSheetWindow window = EditorWindow.GetWindowWithRect <ScriptSheetWindow> (new Rect (0, 0, 400, 305), true, "Script sheet exporter", true);

			window.titleContent.text = "Script sheet exporter";
			window.position = new Rect (300, 200, 400, 305);
			window.languageIndex = _languageIndex;
			window.sceneFiles = _sceneFiles;
		}
		
		
		private void OnGUI ()
		{
			if (AdvGame.GetReferences ().speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Speech Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}
			
			scroll = GUILayout.BeginScrollView (scroll);
			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			
			EditorGUILayout.HelpBox ("Check the settings below and click 'Create' to save a new script sheet.", MessageType.Info);
			EditorGUILayout.Space ();
			
			if (speechManager.Languages.Count > 1)
			{
				languageIndex = EditorGUILayout.Popup ("Language:", languageIndex, speechManager.GetLanguageNameArray ());
			}
			else
			{
				languageIndex = 0;
			}
			
			limitToCharacter = EditorGUILayout.Toggle ("Limit to character?", limitToCharacter);
			if (limitToCharacter)
			{
				characterName = EditorGUILayout.TextField ("Character name:", characterName);
			}
			
			limitToTag = EditorGUILayout.Toggle ("Limit by tag?", limitToTag);
			if (limitToTag)
			{
				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				int i = 0;
				int tagNumber = -1;
				
				if (speechManager.speechTags.Count > 0)
				{
					foreach (SpeechTag speechTag in speechManager.speechTags)
					{
						labelList.Add (speechTag.label);
						if (speechTag.ID == tagID)
						{
							tagNumber = i;
						}
						i++;
					}
					
					if (tagNumber == -1)
					{
						if (tagID > 0) ACDebug.LogWarning ("Previously chosen speech tag no longer exists!");
						tagNumber = 0;
						tagID = 0;
					}
					
					tagNumber = EditorGUILayout.Popup ("Speech tag:", tagNumber, labelList.ToArray());
					tagID = speechManager.speechTags [tagNumber].ID;
				}
				else
				{
					EditorGUILayout.HelpBox ("No speech tags!", MessageType.Info);
				}
			}

			if (speechManager.referenceSpeechFiles != ReferenceSpeechFiles.ByAddressable)
			{
				limitToMissingAudio = EditorGUILayout.Toggle ("Limit to lines with no audio?", limitToMissingAudio);
			}

			includeDescriptions = EditorGUILayout.Toggle ("Include descriptions?", includeDescriptions);
			removeTokens = EditorGUILayout.Toggle ("Remove text tokens?", removeTokens);
			
			if (GUILayout.Button ("Create"))
			{
				CreateScript ();
			}

			EditorGUILayout.Space ();
			EditorGUILayout.EndScrollView ();
		}
		
		
		private void CreateScript ()
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (AdvGame.GetReferences () == null || AdvGame.GetReferences ().speechManager == null)
			{
				ACDebug.LogError ("Cannot create script sheet - no Speech Manager is assigned!");
				return;
			}
			
			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			languageIndex = Mathf.Max (languageIndex, 0);
			
			string suggestedFilename = "Adventure Creator";
			if (AdvGame.GetReferences ().settingsManager)
			{
				suggestedFilename = AdvGame.GetReferences ().settingsManager.saveFileName;
			}
			if (limitToCharacter && characterName != "")
			{
				suggestedFilename += " (" + characterName + ")";
			}
			if (limitToTag && tagID >= 0)
			{
				SpeechTag speechTag = speechManager.GetSpeechTag (tagID);
				if (speechTag != null && speechTag.label.Length > 0)
				{
					suggestedFilename += " (" + speechTag.label + ")";
				}
			}
			suggestedFilename += " - ";
			if (languageIndex > 0)
			{
				suggestedFilename += speechManager.Languages[languageIndex].name + " ";
			}
			suggestedFilename += "script.html";
			
			string fileName = EditorUtility.SaveFilePanel ("Save script file", "Assets", suggestedFilename, "html");
			if (fileName.Length == 0)
			{
				return;
			}
			
			string gameName = "Adventure Creator";
			if (AdvGame.GetReferences ().settingsManager && AdvGame.GetReferences ().settingsManager.saveFileName.Length > 0)
			{
				gameName = AdvGame.GetReferences ().settingsManager.saveFileName;
				if (languageIndex > 0)
				{
					gameName += " (" + speechManager.Languages[languageIndex].name + ")";
				}
			}
			
			System.Text.StringBuilder script = new System.Text.StringBuilder ();
			script.Append ("<html>\n<head>\n");
			script.Append ("<meta http-equiv='Content-Type' content='text/html;charset=ISO-8859-1' charset='UTF-8'>\n");
			script.Append ("<title>" + gameName + "</title>\n");
			script.Append ("<style> body, table, div, p, dl { font: 400 14px/22px Roboto,sans-serif; } footer { text-align: center; padding-top: 20px; font-size: 12px;} footer a { color: blue; text-decoration: none} </style>\n</head>\n");
			script.Append ("<body>\n");
			
			script.Append ("<h1>" + gameName + " - script sheet");
			if (limitToCharacter && characterName != "")
			{
				script.Append (" (" + characterName + ")");
			}
			script.Append ("</h1>\n");
			script.Append ("<h2>Created: " + DateTime.UtcNow.ToString("HH:mm dd MMMM, yyyy") + "</h2>\n");
			
			// By scene
			foreach (string sceneFile in sceneFiles)
			{
				List<SpeechLine> sceneSpeechLines = new List<SpeechLine>();

				int slashPoint = sceneFile.LastIndexOf ("/") + 1;
				string sceneName = sceneFile.Substring (slashPoint);
				
				foreach (SpeechLine line in speechManager.lines)
				{
					if (line.textType == AC_TextType.Speech &&
					    (line.scene == sceneFile || sceneName == (line.scene + ".unity")) &&
					    (!limitToCharacter || characterName == "" || line.owner == characterName || (line.isPlayer && characterName == "Player")) &&
					    (!limitToTag || line.tagID == tagID))
					{
						if (limitToMissingAudio && line.HasAudio (languageIndex))
						{
							continue;
						}

						sceneSpeechLines.Add (line);
					}
				}

				if (sceneSpeechLines != null && sceneSpeechLines.Count > 0)
				{
					sceneSpeechLines.Sort (delegate (SpeechLine a, SpeechLine b) {return a.OrderIdentifier.CompareTo (b.OrderIdentifier);});

					script.Append ("<hr/>\n<h3><b>Scene:</b> " + sceneName + "</h3>\n");
					foreach (SpeechLine sceneSpeechLine in sceneSpeechLines)
					{
						script.Append (sceneSpeechLine.Print (languageIndex, includeDescriptions, removeTokens));
					}
				}
			}
			
			// No scene
			List<SpeechLine> assetSpeechLines = new List<SpeechLine>();
			
			foreach (SpeechLine line in speechManager.lines)
			{
				if (line.scene == "" &&
				    line.textType == AC_TextType.Speech &&
				    (!limitToCharacter || characterName == "" || line.owner == characterName || (line.isPlayer && characterName == "Player")) &&
				    (!limitToTag || line.tagID == tagID))
				{
					if (limitToMissingAudio && line.HasAudio (languageIndex))
					{
						continue;
					}

					assetSpeechLines.Add (line);
				}
			}

			if (assetSpeechLines != null && assetSpeechLines.Count > 0)
			{
				assetSpeechLines.Sort (delegate (SpeechLine a, SpeechLine b) {return a.OrderIdentifier.CompareTo (b.OrderIdentifier);});

				script.Append ("<hr/>\n<h3>Scene-independent lines:</h3>\n");
				foreach (SpeechLine assetSpeechLine in assetSpeechLines)
				{
					script.Append (assetSpeechLine.Print (languageIndex, includeDescriptions, removeTokens));
				}
			}
			
			script.Append ("<footer>Generated by <a href='http://adventurecreator.org' target=blank>Adventure Creator</a>, by Chris Burton</footer>\n");
			script.Append ("</body>\n</html>");
			
			Serializer.SaveFile (fileName, script.ToString ());
			
			#endif
			
			this.Close ();
		}

	}
	
}

#endif