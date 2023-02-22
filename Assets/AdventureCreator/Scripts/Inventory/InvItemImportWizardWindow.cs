#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage the import of inventory items */
	public class InvItemImportWizardWindow : EditorWindow
	{

		private InventoryManager inventoryManager;
		private string[,] csvData;
		private int numRows;
		private int numCols;
		private bool failedImport = false;
		private bool canCreateNew;

		private Vector2 scroll;
		private List<ImportColumn> importColumns = new List<ImportColumn>();
	

		private void _Init (InventoryManager _inventoryManager, string[,] _csvData, bool removeLastColumn = true)
		{
			inventoryManager = _inventoryManager;
			csvData = _csvData;
			failedImport = false;

			if (inventoryManager != null && csvData != null)
			{
				numCols = csvData.GetLength (0);
				if (removeLastColumn) numCols --;

				numRows = csvData.GetLength (1);

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
			}
			else
			{
				numRows = numCols = 0;
			}
		}


		/** Initialises the window. */
		public static void Init (InventoryManager _inventoryManager, string[,] _csvData)
		{
			if (_inventoryManager == null) return;

			InvItemImportWizardWindow window = (InvItemImportWizardWindow) GetWindow (typeof (InvItemImportWizardWindow));

			window.titleContent.text = "Inventory item importer";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_inventoryManager, _csvData);
			window.minSize = new Vector2 (300, 180);
		}
		
		
		private void OnGUI ()
		{
			EditorGUILayout.LabelField ("Inventory item importer", CustomStyles.managerHeader);

			if (inventoryManager == null)
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (failedImport || numRows == 0 || numCols == 0 || importColumns == null)
			{
				EditorGUILayout.HelpBox ("There was an error processing the imported file - please check that the format is correct. The correct format can be shown by exporting a CSV file with the Inventory Manager.", MessageType.Warning);
				return;
			}

			scroll = GUILayout.BeginScrollView (scroll);

			EditorGUILayout.LabelField ("Detected columns", CustomStyles.subHeader);

			EditorGUILayout.HelpBox ("Number of rows: " + (numRows-1).ToString () + "\r\n" + "Number of columns: " + numCols.ToString () + "\r\n" +
									 "Choose the columns to import below, then click 'Import CSV'.", MessageType.Info);
			EditorGUILayout.Space ();

			for (int i=0; i<importColumns.Count; i++)
			{
				importColumns[i].ShowGUI (i);
			}

			EditorGUILayout.Space ();

			canCreateNew = EditorGUILayout.Toggle ("Can create new items?", canCreateNew);
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
			
			if (inventoryManager == null || importColumns == null || importColumns.Count == 0) return;
			int itemID = -1;

			int numUpdated = 0;
			for (int row = 1; row < numRows; row ++)
			{
				if (csvData [0, row] != null && csvData [0, row].Length > 0)
				{
					itemID = -1;
					if (int.TryParse (csvData [0, row], out itemID))
					{
						InvItem invItem = inventoryManager.GetItem (itemID);

						if (invItem == null)
						{
							if (!canCreateNew)
							{
								continue;
							}

							invItem = inventoryManager.CreateNewItem (itemID);
							ACDebug.Log ("Created item, ID = " + invItem.id.ToString ());
						}

						if (invItem != null)
						{
							for (int col = 0; col < numCols; col ++)
							{
								if (importColumns.Count > col)
								{
									string cellData = csvData [col, row];
									importColumns[col].Process (cellData, inventoryManager, invItem);
								}
							}
							numUpdated ++;
						}
					}
					else
					{
						ACDebug.LogWarning ("Error importing item (ID:" + csvData[0, row] + ") in row #" + row.ToString () + ".");
					}
				}
			}
	
			EditorUtility.SetDirty (inventoryManager);

			ACDebug.Log ((numRows-2).ToString () + " item(s) imported, " + numUpdated.ToString () + " item(s) updated.");
			this.Close ();
			#endif
		}


		private class ImportColumn
		{

			private string header;
			private enum ImportColumnType { DoNotImport, ImportAsLabel, ImportAsMainGraphic, ImportAsCategoryID, ImportAsCarryOnStart, ImportAtCanCarryMultiple };
			private ImportColumnType importColumnType;


			public ImportColumn (string _header)
			{
				header = _header;
				importColumnType = ImportColumnType.DoNotImport;
			}


			public void ShowGUI (int i)
			{
				CustomGUILayout.BeginVertical ();
				GUILayout.Label ("Column #" + (i+1).ToString () + ": " + header);

				if (i > 0)
				{
					importColumnType = (ImportColumnType) EditorGUILayout.EnumPopup ("Import rule:", importColumnType);
				}
				CustomGUILayout.EndVertical ();
			}


			public void Process (string cellText, InventoryManager inventoryManager, InvItem invItem)
			{
				if (cellText == null || inventoryManager == null) return;

				cellText = AddLineBreaks (cellText);

				switch (importColumnType)
				{
					case ImportColumnType.DoNotImport:
						return;

					case ImportColumnType.ImportAsLabel:
						invItem.altLabel = cellText;
						break;

					case ImportColumnType.ImportAsMainGraphic:
						string[] guids = AssetDatabase.FindAssets (cellText + " t:texture2D");
						if (guids != null && guids.Length > 0)
						{
							string path = AssetDatabase.GUIDToAssetPath (guids[0]);
							if (!string.IsNullOrEmpty (path))
							{
								Texture tex = (Texture) AssetDatabase.LoadAssetAtPath (path, typeof (Texture));
								if (tex)
								{
									invItem.tex = tex;
								}
							}
						}
					   break;

					case ImportColumnType.ImportAsCategoryID:
						int binID = -1;
						int.TryParse (cellText, out binID);
						if (binID >= 0)
						{
							invItem.binID = binID;
						}
						break;

					case ImportColumnType.ImportAsCarryOnStart:
						invItem.carryOnStart = (cellText.ToLower () == "true");
						break;

					case ImportColumnType.ImportAtCanCarryMultiple:
						invItem.canCarryMultiple = (cellText.ToLower () == "true");
						break;
				}
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