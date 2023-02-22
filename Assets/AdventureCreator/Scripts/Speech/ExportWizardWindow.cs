#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using AC.SML;

namespace AC
{
	
	/** Provides an EditorWindow to manage the export of game text */
	public class ExportWizardWindow : EditorWindow
	{

		private enum Format { CSV, XML };

		private SpeechManager speechManager;
		private List<ExportColumn> exportColumns = new List<ExportColumn>();
		private int sideMenuIndex = -1;

		private bool filterByType = false;
		private bool filterByScene = false;
		private bool filterByText = false;
		private bool filterByTag = false;
		private string textFilter;
		private FilterSpeechLine filterSpeechLine = FilterSpeechLine.Text;
		private AC_TextTypeFlags textTypeFilters = (AC_TextTypeFlags) ~0;
		private int tagFilter;
		private int sceneFilter;

		private bool doRowSorting = false;
		private enum RowSorting { ByID, ByType, ByScene, ByAssociatedObject, ByDescription };
		private RowSorting rowSorting = RowSorting.ByID;
		private string[] sceneNames;


		private Vector2 scroll;


		private void _Init (SpeechManager _speechManager, string[] _sceneNames, int forLanguage)
		{
			speechManager = _speechManager;
			sceneNames = _sceneNames;

			exportColumns.Clear ();
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.Type));
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.DisplayText));
			if (speechManager != null && forLanguage > 0 && speechManager.Languages != null && speechManager.Languages.Count > forLanguage)
			{
				exportColumns.Add (new ExportColumn (forLanguage));
			}
		}


		/** Initialises the window. */
		public static void Init (SpeechManager _speechManager, string[] _sceneNames, int forLanguage = 0)
		{
			if (_speechManager == null) return;

			ExportWizardWindow window = (ExportWizardWindow) GetWindow (typeof (ExportWizardWindow));
			window.titleContent.text = "Text export wizard";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_speechManager, _sceneNames, forLanguage);
			window.minSize = new Vector2 (300, 180);
		}
		
		
		private void OnGUI ()
		{
			if (speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Speech Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (speechManager.lines == null || speechManager.lines.Count == 0)
			{
				EditorGUILayout.HelpBox ("No text is available to export - click 'Gather text' in your Speech Manager to find your game's text.", MessageType.Warning);
				return;
			}
			
			if (exportColumns == null)
			{
				exportColumns = new List<ExportColumn>();
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.LabelField ("Text export wizard", CustomStyles.managerHeader);
			scroll = GUILayout.BeginScrollView (scroll);

			EditorGUILayout.HelpBox ("Choose the fields to export as columns below, then click 'Export CSV'.", MessageType.Info);
			EditorGUILayout.Space ();

			ShowColumnsGUI ();
			ShowRowsGUI ();
			ShowSortingGUI ();

			EditorGUILayout.Space ();
			if (exportColumns.Count == 0)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button ("Export CSV"))
			{
				Export (Format.CSV);
			}
			if (GUILayout.Button ("Export SpreadsheetML"))
			{
				Export (Format.XML);
			}
			GUI.enabled = true;

			GUILayout.EndScrollView ();
		}


		private void ShowColumnsGUI ()
		{
			EditorGUILayout.LabelField ("Define columns",  CustomStyles.subHeader);
			for (int i=0; i<exportColumns.Count; i++)
			{
				CustomGUILayout.BeginVertical ();

				EditorGUILayout.BeginHorizontal ();
				exportColumns[i].ShowFieldSelector (i);

				if (GUILayout.Button ("", CustomStyles.IconCog))
				{
					SideMenu (i);
				}
				EditorGUILayout.EndHorizontal ();

				exportColumns[i].ShowLanguageSelector (speechManager.GetLanguageNameArray ());

				CustomGUILayout.EndVertical ();
			}

			if (GUILayout.Button ("Add new column"))
			{
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.Space ();
		}


		private void ShowRowsGUI ()
		{
			EditorGUILayout.LabelField ("Row filtering", CustomStyles.subHeader);

			filterByType = EditorGUILayout.Toggle ("Filter by type?", filterByType);
			if (filterByType)
			{
				textTypeFilters = (AC_TextTypeFlags) EditorGUILayout.EnumFlagsField ("Limit to type(s):", textTypeFilters);
			}

			filterByScene = EditorGUILayout.Toggle ("Filter by scene?", filterByScene);
			if (filterByScene)
			{
				if (sceneNames != null && sceneNames.Length > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("-> Limit to scene:", GUILayout.Width (100f));
					sceneFilter = EditorGUILayout.Popup (sceneFilter, sceneNames);
					EditorGUILayout.EndHorizontal ();
				}
			}

			filterByText = EditorGUILayout.Toggle ("Filter by text:", filterByText);
			if (filterByText)
			{
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("-> Limit to text:", GUILayout.Width (100f));
				filterSpeechLine = (FilterSpeechLine) EditorGUILayout.EnumPopup (filterSpeechLine, GUILayout.MaxWidth (100f));
				textFilter = EditorGUILayout.TextField (textFilter);
				EditorGUILayout.EndHorizontal ();
			}

			if (IsTextTypeFiltered (AC_TextType.Speech) && speechManager.useSpeechTags)
			{
				filterByTag = EditorGUILayout.Toggle ("Filter by speech tag:", filterByTag);
				if (filterByTag)
				{
					if (speechManager.speechTags != null && speechManager.speechTags.Count > 1)
					{
						if (tagFilter == -1)
						{
							tagFilter = 0;
						}

						List<string> tagNames = new List<string>();
						foreach (SpeechTag speechTag in speechManager.speechTags)
						{
							tagNames.Add (speechTag.label);
						}

						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField ("-> Limit by tag:", GUILayout.Width (65f));
						tagFilter = EditorGUILayout.Popup (tagFilter, tagNames.ToArray ());
						EditorGUILayout.EndHorizontal ();
					}
					else
					{
						tagFilter = -1;
						EditorGUILayout.HelpBox ("No tags defined - they can be created by clicking 'Edit speech tags' in the Speech Manager.", MessageType.Info);
					}
				}
			}

			EditorGUILayout.Space ();
		}


		private bool IsTextTypeFiltered (AC_TextType textType)
		{
			int s1 = (int) textType;
			int s1_modified = (int) Mathf.Pow (2f, (float) s1);
			int s2 = (int) textTypeFilters;
			return (s1_modified & s2) != 0;
		}


		private void ShowSortingGUI ()
		{
			EditorGUILayout.LabelField ("Row sorting", CustomStyles.subHeader);

			doRowSorting = EditorGUILayout.Toggle ("Apply row sorting?", doRowSorting);
			if (doRowSorting)
			{
				rowSorting = (RowSorting) EditorGUILayout.EnumPopup ("Sort rows:", rowSorting);
			}
		}


		private void SideMenu (int i)
		{
			GenericMenu menu = new GenericMenu ();

			sideMenuIndex = i;

			if (exportColumns.Count > 1)
			{
				if (i > 0)
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, MenuCallback, "Move to top");
					menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, MenuCallback, "Move up");
				}
				if (i < (exportColumns.Count - 1))
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, MenuCallback, "Move down");
					menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, MenuCallback, "Move to bottom");
				}
				menu.AddSeparator ("");
			}
			menu.AddItem (new GUIContent ("Delete"), false, MenuCallback, "Delete");
			menu.ShowAsContext ();
		}


		private void MenuCallback (object obj)
		{
			if (sideMenuIndex >= 0)
			{
				int i = sideMenuIndex;
				ExportColumn _column = exportColumns[i];

				switch (obj.ToString ())
				{
				case "Move to top":
					exportColumns.Remove (_column);
					exportColumns.Insert (0, _column);
					break;
					
				case "Move up":
					exportColumns.Remove (_column);
					exportColumns.Insert (i-1, _column);
					break;
					
				case "Move to bottom":
					exportColumns.Remove (_column);
					exportColumns.Insert (exportColumns.Count, _column);
					break;
					
				case "Move down":
					exportColumns.Remove (_column);
					exportColumns.Insert (i+1, _column);
					break;

				case "Delete":
					exportColumns.Remove (_column);
					break;
				}
			}
			
			sideMenuIndex = -1;
		}

		
		private void Export (Format format)
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (speechManager == null || exportColumns == null || exportColumns.Count == 0 || speechManager.lines == null || speechManager.lines.Count == 0) return;

			string suggestedFilename = string.Empty;
			if (AdvGame.GetReferences ().settingsManager)
			{
				suggestedFilename = AdvGame.GetReferences ().settingsManager.saveFileName + " - ";
			}

			string extension;
			switch (format)
			{
				case Format.CSV:
				default:
					extension = "csv";
					break;

				case Format.XML:
					extension = "xml";
					break;
			}
			
			suggestedFilename += "GameText." + extension;

			string fileName = EditorUtility.SaveFilePanel ("Export game text", "Assets", suggestedFilename, extension);
			if (fileName.Length == 0)
			{
				return;
			}

			List<SpeechLine> exportLines = new List<SpeechLine>();
			foreach (SpeechLine line in speechManager.lines)
			{
				if (line.ignoreDuringExport)
				{
					continue;
				}

				if (filterByType)
				{
					if (!IsTextTypeFiltered (line.textType))
					{
						continue;
					}
				}
				if (filterByScene)
				{
					if (sceneNames != null && sceneNames.Length > sceneFilter)
					{
						string selectedScene = sceneNames[sceneFilter] + ".unity";
						string scenePlusExtension = (string.IsNullOrEmpty (line.scene)) ? string.Empty : (line.scene + ".unity");
						
						if ((string.IsNullOrEmpty (line.scene) && sceneFilter == 0)
						    || sceneFilter == 1
						    || (!string.IsNullOrEmpty (line.scene) && sceneFilter > 1 && line.scene.EndsWith (selectedScene))
						    || (!string.IsNullOrEmpty (line.scene) && sceneFilter > 1 && scenePlusExtension.EndsWith (selectedScene)))
						{}
						else
						{
							continue;
						}
					}
				}
				if (filterByText)
				{
					if (!line.Matches (textFilter, filterSpeechLine))
					{
						continue;
					}
				}
				if (filterByTag)
				{
					if (tagFilter == -1
						|| (tagFilter < speechManager.speechTags.Count && line.tagID == speechManager.speechTags[tagFilter].ID))
					{}
					else
					{
						continue;
					}
				}

				exportLines.Add (new SpeechLine (line));
			}

			if (doRowSorting)
			{
				switch (rowSorting)
				{
					case RowSorting.ByID:
						exportLines.Sort ((a, b) => a.lineID.CompareTo (b.lineID));
						break;

					case RowSorting.ByDescription:
						exportLines.Sort ((a, b) => string.Compare (a.description, b.description, System.StringComparison.Ordinal));
						break;

					case RowSorting.ByType:
						exportLines.Sort ((a, b) => string.Compare (a.textType.ToString (), b.textType.ToString (), System.StringComparison.Ordinal));
						break;

					case RowSorting.ByAssociatedObject:
						exportLines.Sort ((a, b) => string.Compare (a.owner, b.owner, System.StringComparison.Ordinal));
						break;

					case RowSorting.ByScene:
						exportLines.Sort ((a, b) => string.Compare (a.scene, b.scene, System.StringComparison.Ordinal));
						break;

					default:
						break;
				}
			}

			List<string[]> output = new List<string[]>();

			List<string> headerList = new List<string>();
			headerList.Add ("ID");
			foreach (ExportColumn exportColumn in exportColumns)
			{
				headerList.Add (exportColumn.GetHeader (speechManager.GetLanguageNameArray ()));
			}
			output.Add (headerList.ToArray ());
		
			foreach (SpeechLine line in exportLines)
			{
				List<string> rowList = new List<string>();
				rowList.Add (line.lineID.ToString ());
				foreach (ExportColumn exportColumn in exportColumns)
				{
					string cellText = exportColumn.GetCellText (line);
					rowList.Add (cellText);
				}
				output.Add (rowList.ToArray ());
			}

			string fileContents;
			switch (format)
			{
				case Format.CSV:
				default:
					fileContents = CSVReader.CreateCSVGrid (output);
					break;

				case Format.XML:
					fileContents = SMLReader.CreateXMLGrid (output);
					break;
			}

			if (!string.IsNullOrEmpty (fileContents) && Serializer.SaveFile (fileName, fileContents))
			{
				int numLines = exportLines.Count;
				ACDebug.Log (numLines.ToString () + " line" + ((numLines != 1) ? "s" : string.Empty) + " exported.");
			}

			#endif
		}


		private class ExportColumn
		{

			public enum ColumnType { DisplayText, Type, AssociatedObject, Scene, Description, TagID, TagName, SpeechOrder, AudioFilename, AudioFilePresence };
			private ColumnType columnType;
			private int language;


			public ExportColumn ()
			{
				columnType = ColumnType.DisplayText;
				language = 0;
			}


			public ExportColumn (ColumnType _columnType)
			{
				columnType = _columnType;
				language = 0;
			}


			public ExportColumn (int _language)
			{
				columnType = ColumnType.DisplayText;
				language = _language;
			}


			public void ShowFieldSelector (int i)
			{
				columnType = (ColumnType) EditorGUILayout.EnumPopup ("Column #" + (i+1).ToString () + ":", columnType);

				if (columnType == ColumnType.AudioFilePresence && KickStarter.speechManager.referenceSpeechFiles == ReferenceSpeechFiles.ByAssetBundle)
				{
					EditorGUILayout.HelpBox ("Presence from asset bundles cannot be determined in Edit mode - files will be searched for in Resources folders.", MessageType.Warning);
				}
				else if (columnType == ColumnType.AudioFilePresence && KickStarter.speechManager.referenceSpeechFiles == ReferenceSpeechFiles.ByAddressable)
				{
					EditorGUILayout.HelpBox ("Presence of Addressable assets cannot be determined in Edit mode.", MessageType.Warning);
				}
			}


			public void ShowLanguageSelector (string[] languages)
			{
				if (columnType == ColumnType.DisplayText)
				{
					language = EditorGUILayout.Popup ("Language:", language, languages);
				}
			}


			public string GetHeader (string[] languages)
			{
				if (columnType == ColumnType.DisplayText)
				{
					if (language > 0)
					{
						if (languages != null && languages.Length > language)
						{
							return languages[language];
						}
						return ("Invalid language");
					}
					return ("Original text");
				}
				return columnType.ToString ();
			}


			public string GetCellText (SpeechLine speechLine)
			{
				string cellText = " ";

				switch (columnType)
				{
					case ColumnType.DisplayText:
						if (language > 0)
						{
							int translation = language-1;
							if (speechLine.translationText != null && speechLine.translationText.Count > translation)
							{
								cellText = speechLine.translationText[translation];
							}
						}
						else
						{
							cellText = speechLine.text;
						}
						break;

					case ColumnType.Type:
						cellText = speechLine.textType.ToString ();
						break;

					case ColumnType.AssociatedObject:
						if (speechLine.isPlayer && string.IsNullOrEmpty (speechLine.owner) && speechLine.textType == AC_TextType.Speech)
						{
							cellText = "Player";
						}
						else
						{
							cellText = speechLine.owner;
						}
						break;

					case ColumnType.Scene:
						cellText = speechLine.scene;
						break;

					case ColumnType.Description:
						cellText = speechLine.description;
						break;

					case ColumnType.TagID:
						cellText = speechLine.tagID.ToString ();
						break;

					case ColumnType.TagName:
						SpeechTag speechTag = KickStarter.speechManager.GetSpeechTag (speechLine.tagID);
						cellText = (speechTag != null) ? speechTag.label : "";
						break;

					case ColumnType.SpeechOrder:
						cellText = speechLine.OrderIdentifier;
						if (cellText == "-0001")
						{
							cellText = string.Empty;
						}
						break;

					case ColumnType.AudioFilename:
						if (speechLine.textType == AC_TextType.Speech)
						{
							if (speechLine.SeparatePlayerAudio ())
							{
								string result = string.Empty;
								for (int j = 0; j < KickStarter.settingsManager.players.Count; j++)
								{
									if (KickStarter.settingsManager.players[j].playerOb != null)
									{
										string overrideName = KickStarter.settingsManager.players[j].playerOb.name;
										result += speechLine.GetFilename (overrideName) + ";";
									}
								}
								cellText = result;
							}
							else
							{
								cellText = speechLine.GetFilename ();
							}
						}
						break;

					case ColumnType.AudioFilePresence:
						if (speechLine.textType == AC_TextType.Speech)
						{
							bool hasAllAudio = speechLine.HasAllAudio ();
							if (hasAllAudio)
							{
								cellText = "Has all audio";
							}
							else
							{
								if (speechLine.HasAudio (0))
								{
									string missingLabel = "Missing ";
									for (int i=1; i<KickStarter.speechManager.Languages.Count; i++)
									{
										if (!speechLine.HasAudio (i))
										{
											missingLabel += KickStarter.speechManager.Languages[i].name + ", ";
										}
									}
									if (missingLabel.EndsWith (", "))
									{
										missingLabel = missingLabel.Substring (0, missingLabel.Length-2);
									}
									cellText = missingLabel;
								}
								else
								{
									cellText = "Missing main audio";
								}
							}
						}
						break;
				}


				if (string.IsNullOrEmpty (cellText))
				{
					cellText = " ";
				}
				return RemoveLineBreaks (cellText);
			}


			private string RemoveLineBreaks (string text)
			{
				if (text.Length == 0) return " ";
	            //text = text.Replace("\r\n", "[break]").Replace("\n", "[break]");
				text = text.Replace("\r\n", "[break]");
				text = text.Replace("\n", "[break]");
				text = text.Replace("\r", "[break]");
	            return text;
	        }

		}
		
		
	}
	
}

#endif