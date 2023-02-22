#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage the export of inventory items */
	public class InvItemExportWizardWindow : EditorWindow
	{

		private InventoryManager inventoryManager;
		private List<ExportColumn> exportColumns = new List<ExportColumn>();
		private int sideMenuIndex = -1;

		private Vector2 scroll;


		public void _Init (InventoryManager _inventoryManager)
		{
			inventoryManager = _inventoryManager;

			exportColumns.Clear ();
			exportColumns.Add (new ExportColumn (ExportColumn.ColumnType.InternalName));
		}


		/** Initialises the window. */
		public static void Init (InventoryManager _inventoryManager)
		{
			if (_inventoryManager == null) return;

			InvItemExportWizardWindow window = (InvItemExportWizardWindow) GetWindow (typeof (InvItemExportWizardWindow));

			window.titleContent.text = "Inventory item exporter";
			window.position = new Rect (300, 200, 350, 500);
			window._Init (_inventoryManager);
			window.minSize = new Vector2 (300, 180);
		}
		
		
		private void OnGUI ()
		{
			if (inventoryManager == null)
			{
				EditorGUILayout.HelpBox ("An Inventory Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			if (inventoryManager.items == null || inventoryManager.items.Count == 0)
			{
				EditorGUILayout.HelpBox ("No inventory items are available to export.", MessageType.Warning);
				return;
			}
			
			if (exportColumns == null)
			{
				exportColumns = new List<ExportColumn>();
				exportColumns.Add (new ExportColumn ());
			}

			EditorGUILayout.LabelField ("Inventory item exporter", CustomStyles.managerHeader);
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
			
			if (inventoryManager == null || exportColumns == null || exportColumns.Count == 0 || inventoryManager.items == null || inventoryManager.items.Count == 0) return;

			string suggestedFilename = "";
			if (AdvGame.GetReferences ().settingsManager)
			{
				suggestedFilename = AdvGame.GetReferences ().settingsManager.saveFileName + " - ";
			}
			suggestedFilename += "Inventory.csv";
			
			string fileName = EditorUtility.SaveFilePanel ("Export inventory items", "Assets", suggestedFilename, "csv");
			if (fileName.Length == 0)
			{
				return;
			}

			List<InvItem> exportItems = new List<InvItem>();
			foreach (InvItem item in inventoryManager.items)
			{
				exportItems.Add (new InvItem (item));
			}

			List<string[]> output = new List<string[]>();

			List<string> headerList = new List<string>();
			headerList.Add ("ID");
			foreach (ExportColumn exportColumn in exportColumns)
			{
				headerList.Add (exportColumn.GetHeader ());
			}
			output.Add (headerList.ToArray ());
			
			foreach (InvItem exportItem in exportItems)
			{
				List<string> rowList = new List<string>();
				rowList.Add (exportItem.id.ToString ());
				foreach (ExportColumn exportColumn in exportColumns)
				{
					string cellText = exportColumn.GetCellText (exportItem, inventoryManager);
					rowList.Add (cellText);
				}
				output.Add (rowList.ToArray ());
			}

			string fileContents = CSVReader.CreateCSVGrid (output);
			if (!string.IsNullOrEmpty (fileContents) && Serializer.SaveFile (fileName, fileContents))
			{
				ACDebug.Log ((exportItems.Count-1).ToString () + " items exported.");
			}

			//this.Close ();
			#endif
		}


		private class ExportColumn
		{

			public enum ColumnType { InternalName, Label, MainGraphic, Category, CategoryID, CarryOnStart, CanCarryMultiple };
			private ColumnType columnType;


			public ExportColumn ()
			{
				columnType = ColumnType.InternalName;
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


			public string GetCellText (InvItem invItem, InventoryManager inventoryManager)
			{
				string cellText = " ";

				switch (columnType)
				{
					case ColumnType.InternalName:
						cellText = invItem.label;
						break;

					case ColumnType.Label:
						cellText = invItem.altLabel;
						break;

					case ColumnType.MainGraphic:
						cellText = (invItem.tex) ? invItem.tex.name : "";
						break;

					case ColumnType.Category:
						if (invItem.binID >= 0)
						{
							InvBin invBin = inventoryManager.GetCategory (invItem.binID);
							cellText = (invBin != null) ? invBin.label : "";
						}
						break;

					case ColumnType.CategoryID:
						cellText = (invItem.binID >= 0) ? invItem.binID.ToString () : "";
						break;

					case ColumnType.CarryOnStart:
						cellText = (invItem.carryOnStart) ? "True" : "False";
						break;

					case ColumnType.CanCarryMultiple:
						cellText = (invItem.canCarryMultiple) ? "True" : "False";
						break;
				}

				if (cellText == "") cellText = " ";
				return RemoveLineBreaks (cellText);
			}


			private string RemoveLineBreaks (string text)
			{
				if (text.Length == 0) return " ";
	           // text = text.Replace("\r\n", "[break]").Replace("\n", "[break]");
				text = text.Replace("\r\n", "[break]");
				text = text.Replace("\n", "[break]");
				text = text.Replace("\r", "[break]");
	            return text;
	        }

		}

	}
	
}

#endif