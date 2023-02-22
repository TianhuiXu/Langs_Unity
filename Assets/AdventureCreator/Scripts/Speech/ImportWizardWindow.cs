#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an Editor Window to manage the import of game text */
	public class ImportWizardWindow : EditorWindow
	{

		private SpeechManager speechManager;
		private string[,] csvData;
		private int numRows;
		private int numCols;
		private bool failedImport = false;

		private Vector2 scroll;
		private List<ImportColumn> importColumns = new List<ImportColumn>();
	

		public void _Init (SpeechManager _speechManager, string[,] _csvData, int forLanguage, bool removeLastColumn = true)
		{
			speechManager = _speechManager;
			csvData = _csvData;
			failedImport = false;

			if (speechManager != null && csvData != null)
			{
				numCols = csvData.GetLength (0);
				numRows = csvData.GetLength (1);

				if (removeLastColumn)
				{
					numCols --;
				}

				if (numRows < 2 || numCols < 1)
				{
					failedImport = true;
					return;
				}

				importColumns = new List<ImportColumn>();
				for (int col=0; col<numCols; col++)
				{
					importColumns.Add (new ImportColumn (csvData [col, 0]));
				}

				if (forLanguage > 0 && speechManager.Languages != null && speechManager.Languages.Count > forLanguage)
				{
					if (importColumns.Count > 1)
					{
						importColumns [importColumns.Count-1].SetToTranslation (forLanguage-1);
					}
				}
				else if (forLanguage == 0)
				{
					if (importColumns.Count > 1)
					{
						importColumns [importColumns.Count-1].SetToOriginal ();
					}
				}
			}
			else
			{
				numRows = numCols = 0;
			}
		}


		/** Initialises the window. */
		public static void Init (SpeechManager _speechManager, string[,] _csvData, int _forLanguage = -1, bool removeLastColumn = true)
		{
			if (_speechManager == null) return;

			ImportWizardWindow window = (ImportWizardWindow) GetWindow (typeof (ImportWizardWindow));
			
			window.titleContent.text = "Text import wizard";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_speechManager, _csvData, _forLanguage, removeLastColumn);
			window.minSize = new Vector2 (300, 180);
		}
		
		
		private void OnGUI ()
		{
			EditorGUILayout.LabelField ("Text import wizard", CustomStyles.managerHeader);

			if (speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Speech Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (speechManager.lines == null || speechManager.lines.Count == 0)
			{
				EditorGUILayout.HelpBox ("No text is available to import - click 'Gather text' in your Speech Manager to find your game's text.", MessageType.Warning);
				return;
			}

			if (failedImport || numRows == 0 || numCols == 0 || importColumns == null)
			{
				EditorGUILayout.HelpBox ("There was an error processing the imported file - please check that the format is correct. The correct format can be shown by exporting a CSV file with the Speech Manager.", MessageType.Warning);
				return;
			}

			scroll = GUILayout.BeginScrollView (scroll);

			EditorGUILayout.LabelField ("Detected columns", CustomStyles.subHeader);

			List<string> translations = new List<string>();
			if (speechManager.Languages != null && speechManager.Languages.Count > 1)
			{
				for (int i=1; i<speechManager.Languages.Count; i++)
				{
					translations.Add (speechManager.Languages[i].name);
				}
			}
			string[] translationsArray = translations.ToArray ();

			EditorGUILayout.HelpBox ("Number of rows: " + (numRows-1).ToString () + "\r\n" + "Number of columns: " + numCols.ToString () + "\r\n" +
									 "Choose the columns to import below, then click 'Import CSV'.", MessageType.Info);
			EditorGUILayout.Space ();

			for (int i=0; i<importColumns.Count; i++)
			{
				importColumns[i].ShowGUI (i, translationsArray);
			}

			EditorGUILayout.Space ();
			if (GUILayout.Button ("Import CSV"))
			{
				Import ();
			}

			EditorGUILayout.EndScrollView ();
		}


		private void Import ()
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (speechManager == null || importColumns == null || importColumns.Count == 0 || speechManager.lines == null || speechManager.lines.Count == 0) return;
			int lineID = -1;

			bool importingOverwrite = false;
			foreach (ImportColumn importColumn in importColumns)
			{
				if (importColumn.UpdatesOriginal ())
				{
					importingOverwrite = true;
				}
			}

			if (importingOverwrite)
			{
				speechManager.GetAllActionListAssets ();
			}

			HashSet<SpeechLine> updatedLines = new HashSet<SpeechLine> ();
			for (int row = 1; row < numRows; row ++)
			{
				if (csvData [0, row] != null && csvData [0, row].Length > 0)
				{
					lineID = -1;
					if (int.TryParse (csvData [0, row], out lineID))
					{
						SpeechLine speechLine = speechManager.GetLine (lineID);
						
						if (speechLine != null)
						{
							for (int col = 0; col < numCols; col ++)
							{
								if (importColumns.Count > col)
								{
									string cellData = csvData [col, row];
									if (importColumns[col].Process (speechManager, cellData, speechLine))
									{
										if (!updatedLines.Contains (speechLine))
										{
											updatedLines.Add (speechLine);
										}
									}
								}
							}
						}
					}
					else
					{
						ACDebug.LogWarning ("Error importing translation (ID:" + csvData [0, row] + ") on row #" + row.ToString () + ".");
					}
				}
			}

			if (importingOverwrite)
			{
				speechManager.ClearAllAssets ();
			}

			speechManager.CacheDisplayLines ();
			EditorUtility.SetDirty (speechManager);

			int numLinesImported = (numRows - 2);
			int numLinesUpdated = updatedLines.Count;

			foreach (SpeechLine updatedLine in updatedLines)
			{
				ACDebug.Log ("Updated line ID: " + updatedLine.lineID + ", Type: " + updatedLine.textType + ", Text: '" + updatedLine.text + "'");
			}

			EditorUtility.DisplayDialog ("Import game text", "Process complete.\n\n" + numLinesImported + " line(s) imported, " + numLinesUpdated + " line(s) updated.", "OK");

			this.Close ();
			#endif
		}


		private class ImportColumn
		{

			private string header;
			private enum ImportColumnType { DoNotImport, ImportAsTranslation, ImportAsOriginalText, ImportAsDescription, ImportAsCustomFilename };
			private ImportColumnType importColumnType;
			private int translationIndex;


			public ImportColumn (string _header)
			{
				header = _header;
				importColumnType = ImportColumnType.DoNotImport;
				translationIndex = 0;
			}


			public void SetToTranslation (int _translationIndex)
			{
				importColumnType = ImportColumnType.ImportAsTranslation;
				translationIndex = _translationIndex;
			}


			public void SetToOriginal ()
			{
				importColumnType = ImportColumnType.ImportAsOriginalText;
			}


			public bool UpdatesOriginal ()
			{
				return (importColumnType == ImportColumnType.ImportAsOriginalText);
			}


			public void ShowGUI (int i, string[] translations)
			{
				CustomGUILayout.BeginVertical ();
				GUILayout.Label ("Column #" + (i + 1).ToString () +": " + header);

				if (i > 0)
				{
					importColumnType = (ImportColumnType) EditorGUILayout.EnumPopup ("Import rule:", importColumnType);

					switch (importColumnType)
					{
						case ImportColumnType.ImportAsTranslation:
							if (translations == null || translations.Length == 0)
							{
								EditorGUILayout.HelpBox ("No translations found!", MessageType.Warning);
							}
							else
							{
								translationIndex = EditorGUILayout.Popup ("Translation:", translationIndex, translations);
							}
							break;

						case ImportColumnType.ImportAsCustomFilename:
							EditorGUILayout.HelpBox ("Empty fields will clear custom filenames, reverting them to defaults.", MessageType.Info);
							break;

						case ImportColumnType.ImportAsOriginalText:
							EditorGUILayout.HelpBox ("Intensive operation - it is strongly recommended to back up your project first.", MessageType.Warning);
							break;

						default:
							break;
					}
				}
				CustomGUILayout.EndVertical ();
			}


			public bool Process (SpeechManager speechManager, string cellText, SpeechLine speechLine)
			{
				if (cellText == null) return false;

				cellText = AddLineBreaks (cellText);
				
				switch (importColumnType)
				{
					case ImportColumnType.ImportAsDescription:
						if (speechLine.description != cellText)
						{
							speechLine.description = cellText;
							return true;
						}
						break;

					case ImportColumnType.ImportAsTranslation:
						if (speechLine.translationText != null && speechLine.translationText.Count > translationIndex)
						{
							if (speechLine.translationText[translationIndex] != cellText)
							{
								speechLine.translationText[translationIndex] = cellText;
								return true;
							}
						}
						break;

					case ImportColumnType.ImportAsOriginalText:
						return speechManager.UpdateOriginalText (speechLine, cellText);

					case ImportColumnType.ImportAsCustomFilename:
						if (speechLine.textType == AC_TextType.Speech && speechLine.customFilename != cellText)
						{
							if (cellText == speechLine.DefaultFilename)
							{
								cellText = string.Empty;
							}
							speechLine.customFilename = cellText;
							return true;
						}
						break;

					default:
						break;
				}

				return false;
			}


			private string AddLineBreaks (string text)
			{
				text = text.Replace ("[break]", "\n");
				return text;
			}
	
		}
		
		
	}
	
}

#endif