using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AC.SML
{

	public class SMLReader
	{

		private const string xmlHeader = "<?xml version=\"1.0\" encoding=\"utf-8\"?><?mso-application progid=\"Excel.Sheet\"?>";
		private const string workbookHeader = "<Workbook xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:x2=\"urn:schemas-microsoft-com:office:excel2\" xmlns:html=\"http://www.w3.org/TR/REC-html40\" xmlns:dt=\"uuid:C2F41010-65B3-11d1-A29F-00AA00C14882\">";


		static public string[,] SplitXMLGrid (string xmlText)
		{
			try
			{
				xmlText = xmlText.Replace ("<ss:", "<");
				xmlText = xmlText.Replace ("</ss:", "</");

				var ms = new MemoryStream (Encoding.UTF8.GetBytes (xmlText));
				var reader = new XmlTextReader (ms) { Namespaces = false };
				var serializer = new XmlSerializer (typeof (WorkbookXml));

				WorkbookXml result = (WorkbookXml) serializer.Deserialize (reader);

				int numWorksheets = result.Worksheets.Length;
				if (numWorksheets == 0)
				{
					return new string[0,0];
				}

				int w = 0;
				
				int numRows = result.Worksheets[w].Table.Rows.Length;
				int numCols = result.Worksheets[w].Table.Rows[w].Cells.Length;
				string[,] outputGrid = new string[numCols, numRows];

				for (int r = 0; r < numRows; r++)
				{
					if (w != 0 && r == 0) continue;

					RowXml row = result.Worksheets[0].Table.Rows[r];
					for (int c = 0; c < numCols; c++)
					{
						outputGrid[c,r] = row.Cells[c].Data;
					}
				}

				return outputGrid;
			}
			catch (Exception e)
			{
				ACDebug.LogWarning ("Error importing XML file, exception: " + e);
				return null;
			}
		}


		public static string CreateXMLGrid (List<string[]> contents)
		{
			List<string[]>[] contentsArray = new List<string[]>[1] { contents };
			return CreateXMLGrid (contentsArray);
		}


		public static string CreateXMLGrid (List<string[]>[] contentsArray)
		{
			StringBuilder sb = new StringBuilder ();

			sb.Append (xmlHeader);
			sb.AppendLine ();
			sb.Append (workbookHeader);

			int numSheets = contentsArray.Length;
			for (int w = 0; w < numSheets; w++)
			{
				List<string[]> contents = contentsArray[w];
				int numRows = contents.Count;
				int numCols = contents[0].Length;

				string sheetName = "Sheet" + (w+1).ToString ();

				sb.Append ("<Worksheet ss:Name=\"").Append (sheetName).Append ("\">");
				sb.AppendLine ();
				sb.Append ("<ss:Names />");
				sb.AppendLine ();
				sb.Append ("<ss:Table ss:DefaultRowHeight=\"12.75\" ss:DefaultColumnWidth=\"66\" ss:ExpandedRowCount=\"").Append (numRows.ToString ()).Append ("\" ss:ExpandedColumnCount=\"").Append (numCols.ToString ()).Append ("\">");
				sb.AppendLine ();

				for (int row = 0; row < numRows; row++)
				{
					string rowIndex = (row+1).ToString ();

					sb.Append ("<Row ss:Index=\"").Append (rowIndex).Append ("\">");
					sb.AppendLine ();

					for (int col = 0; col < numCols; col++)
					{
						string cellText = contents[row][col];
						sb.Append ("<Cell><Data ss:Type=\"String\">").Append (cellText).Append ("</Data></Cell>");
						sb.AppendLine ();
					}

					sb.Append ("</Row>");
					sb.AppendLine ();
				}

				sb.Append ("</ss:Table>");
				sb.AppendLine ();
				sb.Append ("</Worksheet>");
				sb.AppendLine ();
			}

			sb.Append ("</Workbook>");
			sb.AppendLine ();

			return sb.ToString ();
		}

	}


	[XmlRoot ("Workbook")]
	public class WorkbookXml
	{
		[XmlAnyAttribute] public XmlAttribute[] XAttributes { get; set; }
		[XmlElement (ElementName = "Worksheet")] public WorksheetXml[] Worksheets { get; set; }
	}


	public class WorksheetXml
	{
		[XmlAttribute ("Names")] public string Names { get; set; }
		[XmlElement (ElementName = "Table")] public TableXml Table { get; set; }
	}


	public class TableXml
	{
		[XmlElement (ElementName = "Row")] public RowXml[] Rows { get; set; }
	}


	public class RowXml
	{
		[XmlElement (ElementName = "Cell")] public CellXml[] Cells { get; set; }
	}


	public class CellXml
	{
		[XmlElement ("Data")] public string Data { get; set; }
	}

}