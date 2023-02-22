#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage the export of variables */
	public class VarExportWizardWindow : EditorWindow
	{

		private VariablesManager variablesManager;
		private Variables variables;
		private VariableLocation variableLocation;
		private bool allScenes;

		private List<ExportColumn> exportColumns = new List<ExportColumn>();
		private int sideMenuIndex = -1;
		private bool replaceForwardSlashes;

		private Vector2 scroll;


		public void _Init (VariableLocation _variableLocation, bool _allScenes, Variables _variables)
		{
			variableLocation = _variableLocation;
			variablesManager = KickStarter.variablesManager;
			variables = _variables;
			allScenes = _allScenes;

			exportColumns.Clear ();
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.Location));

			if (variableLocation == VariableLocation.Local)
			{
				exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.SceneName));
			}
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.Label));
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.Type));
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.InitialValue));
		}


		/** Initialises the window. */
		public static void Init (VariableLocation _variableLocation, bool _allScenes, Variables _variables)
		{
			VarExportWizardWindow window = (VarExportWizardWindow) GetWindow (typeof (VarExportWizardWindow));

			window.titleContent.text = _variableLocation.ToString () + " Variables exporter";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_variableLocation, _allScenes, _variables);
			window.minSize = new Vector2 (300, 180);
		}
		
		
		private void OnGUI ()
		{
			if (variablesManager == null)
			{
				EditorGUILayout.HelpBox ("A Variables Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (exportColumns == null)
			{
				exportColumns = new List<ExportColumn>();
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.LabelField (variableLocation.ToString () + " Variables exporter", CustomStyles.managerHeader);
			scroll = GUILayout.BeginScrollView (scroll);

			EditorGUILayout.HelpBox ("Choose the fields to export as columns below, then click 'Export CSV'.", MessageType.Info);
			EditorGUILayout.Space ();

			ShowColumnsGUI ();

			EditorGUILayout.Space ();
			if (exportColumns.Count == 0)
			{
				GUI.enabled = false;
			}
			if (GUILayout.Button ("Export CSV"))
			{
				Export ();
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

				if (exportColumns[i].GetHeader () == "Label")
				{
					replaceForwardSlashes = EditorGUILayout.Toggle ("Replace '/' with '.'?", replaceForwardSlashes);
				}

				CustomGUILayout.EndVertical ();
			}

			if (GUILayout.Button ("Add new column"))
			{
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.Space ();
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

		
		private void Export ()
		{
			#if UNITY_WEBPLAYER
			ACDebug.LogWarning ("Game text cannot be exported in WebPlayer mode - please switch platform and try again.");
			#else
			
			if (variablesManager == null || exportColumns == null || exportColumns.Count == 0) return;

			if (variableLocation == VariableLocation.Local && allScenes)
			{
				bool canProceed = EditorUtility.DisplayDialog ("Export variables", "AC will now go through your game, and collect all variables to be exported.\n\nIt is recommended to back up your project beforehand.", "OK", "Cancel");
				if (!canProceed) return;

				if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					return;
				}
			}

			string suggestedFilename = "";
			if (AdvGame.GetReferences ().settingsManager)
			{
				suggestedFilename = AdvGame.GetReferences ().settingsManager.saveFileName + " - ";
			}
			if (variableLocation == VariableLocation.Local && allScenes)
			{
				suggestedFilename += " All ";
			}
			suggestedFilename += variableLocation.ToString () + " Variables.csv";
			
			string fileName = EditorUtility.SaveFilePanel ("Export variables", "Assets", suggestedFilename, "csv");
			if (fileName.Length == 0)
			{
				return;
			}

			List<string[]> output = new List<string[]>();

			List<string> headerList = new List<string>();
			headerList.Add ("ID");
			foreach (ExportColumn exportColumn in exportColumns)
			{
				headerList.Add (exportColumn.GetHeader ());
			}
			output.Add (headerList.ToArray ());

			// Global
			if (variableLocation == VariableLocation.Global)
			{
				List<GVar> exportVars = new List<GVar>();
				foreach (GVar globalVariable in variablesManager.vars)
				{
					exportVars.Add (new GVar (globalVariable));
				}

				foreach (GVar exportVar in exportVars)
				{
					List<string> rowList = new List<string>();
					rowList.Add (exportVar.id.ToString ());
					foreach (ExportColumn exportColumn in exportColumns)
					{
						string cellText = exportColumn.GetCellText (exportVar, VariableLocation.Global, replaceForwardSlashes);
						rowList.Add (cellText);
					}
					output.Add (rowList.ToArray ());
				}
			}

			// Local
			else if (variableLocation == VariableLocation.Local)
			{
				if (allScenes)
				{
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
					string[] sceneFiles = AdvGame.GetSceneFiles ();
					foreach (string sceneFile in sceneFiles)
					{
						UnityVersionHandler.OpenScene (sceneFile);

						if (FindObjectOfType <LocalVariables>())
						{
							LocalVariables localVariables = FindObjectOfType <LocalVariables>();
							if (localVariables != null)
							{
								string sceneName = UnityVersionHandler.GetCurrentSceneName ();
								output = GatherOutput (output, localVariables.localVars, sceneName);
							}
						}
					}

					if (string.IsNullOrEmpty (originalScene))
					{
						UnityVersionHandler.NewScene ();
					}
					else
					{
						UnityVersionHandler.OpenScene (originalScene);
					}
				}
				else
				{
					string sceneName = UnityVersionHandler.GetCurrentSceneName ();
					output = GatherOutput (output, KickStarter.localVariables.localVars, sceneName);
				}
			}

			// Component
			else if (variableLocation == VariableLocation.Component)
			{
				string sceneName = UnityVersionHandler.GetCurrentSceneName ();
				if (variables != null)
				{
					output = GatherOutput (output, variables.vars, sceneName);
				}
			}

			string fileContents = CSVReader.CreateCSVGrid (output);
			if (!string.IsNullOrEmpty (fileContents) && Serializer.SaveFile (fileName, fileContents))
			{
				int numExported = output.Count - 1;
				if (numExported == 1)
				{
					ACDebug.Log ("1 " + variableLocation + " variable exported.");
				}
				else
				{
					ACDebug.Log (numExported.ToString () + " " + variableLocation + " variables exported.");
				}
			}

			#endif
		}


		private List<string[]> GatherOutput (List<string[]> output, List<GVar> vars, string sceneName)
		{
			List<GVar> exportVars = new List<GVar>();
					
			foreach (GVar variable in vars)
			{
				exportVars.Add (new GVar (variable));
			}

			foreach (GVar exportVar in exportVars)
			{
				List<string> rowList = new List<string>();
				rowList.Add (exportVar.id.ToString ());
				foreach (ExportColumn exportColumn in exportColumns)
				{
					string cellText = exportColumn.GetCellText (exportVar, variableLocation, replaceForwardSlashes, sceneName);
					rowList.Add (cellText);
				}

				output.Add (rowList.ToArray ());
			}

			return output;
		}


		private class ExportColumn
		{

			public enum ColumnType { Location, SceneName, Label, Type, Description, InitialValue };
			private ColumnType columnType;


			public ExportColumn ()
			{
				columnType = ColumnType.Label;
			}


			public ExportColumn (ColumnType _columnType)
			{
				columnType = _columnType;
			}


			public void ShowFieldSelector (int i)
			{
				columnType = (ColumnType) EditorGUILayout.EnumPopup ("Column #" + (i+1).ToString () + ":", columnType);
			}


			public string GetHeader ()
			{
				return columnType.ToString ();
			}


			public string GetCellText (GVar variable, VariableLocation location, bool replaceForwardSlashes, string sceneName = "")
			{
				string cellText = " ";

				switch (columnType)
				{
					case ColumnType.Location:
						cellText = location.ToString ();
						break;

					case ColumnType.SceneName:
						if (location == VariableLocation.Local || location == VariableLocation.Component)
						{
							cellText = sceneName;
						}
						break;

					case ColumnType.Label:
						cellText = variable.label;
						if (replaceForwardSlashes)
						{
							cellText = cellText.Replace ("/", ".");
						}
						break;

					case ColumnType.Type:
						cellText = variable.type.ToString ();
						break;

					case ColumnType.Description:
						cellText = variable.description;
						break;

					case ColumnType.InitialValue:
						cellText = variable.GetValue ();
						break;
				}

				if (string.IsNullOrEmpty (cellText)) cellText = " ";
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