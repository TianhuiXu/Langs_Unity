/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CSVReader.cs"
 * 
 *	This script imports CSV files for use by the Speech Manager.
 *	It is based on original code by Dock at http://wiki.unity3d.com/index.php?title=CSVReader
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AC
{

	/**
	 * A class that can read CSV files
	 */
	public class CSVReader
	{

		private const string legacy_csvDelimiter = "|";
		private const string legacy_csvComma = ",";
		private const string legacy_csvTemp = "{{{$$}}}";

		private const string textSeparator = "\"";
		private const string fieldSeparator = ",";
		

		/**
		 * <summary>Splits the contents of a CSV file into a 2D string array</summary>
		 * <param name = "csvText">The CSV file's contents</param>
		 * <returns>A 2D string array</returns>
		 */
		static public string[,] SplitCsvGrid (string csvText)
		{
			#if UNITY_EDITOR
			CSVFormat format = ACEditorPrefs.CSVFormat;
			#else
			CSVFormat format = CSVFormat.Standard;
			#endif

			switch (format)
			{
				case CSVFormat.Legacy:
					{
						csvText = csvText.Replace (legacy_csvComma, legacy_csvTemp);
						csvText = csvText.Replace (legacy_csvDelimiter, legacy_csvComma);

						csvText = csvText.Replace ("\r\n", "\n");
						csvText = csvText.Replace ("\r", "\n");

						string[] stringSeparators = new string[] { "\n" };
						string[] lines = csvText.Split (stringSeparators, System.StringSplitOptions.None);

						int width = 0;
						for (int i = 0; i < lines.Length; i++)
						{
							string[] row = lines[i].Split (legacy_csvComma[0]);
							width = Mathf.Max (width, row.Length);
						}

						string[,] outputGrid = new string[width + 1, lines.Length + 1];
						for (int y = 0; y < lines.Length; y++)
						{
							string[] row = lines[y].Split (legacy_csvComma[0]);
							for (int x = 0; x < row.Length; x++)
							{
								outputGrid[x, y] = row[x].Replace (legacy_csvTemp, legacy_csvComma);
							}
						}

						return outputGrid;
					}

				case CSVFormat.Standard:
					{
						csvText = csvText.Replace ("\r\n", "\n");
						csvText = csvText.Replace ("\r", "\n");

						string[] stringSeparators = new string[] { "\n" };
						string[] lines = csvText.Split (stringSeparators, System.StringSplitOptions.None);

						List<string[]> contents = new List<string[]> ();

						int numRows = lines.Length;
						for (int row = 0; row < numRows; row++)
						{
							if (string.IsNullOrEmpty (lines[row])) continue;

							Regex csvSplitter = new Regex (@",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
							string[] cells = csvSplitter.Split (lines[row]);

							for (int i = 0; i < cells.Length; i++)
							{
								string cellText = cells[i];
								if (cellText.StartsWith (textSeparator) && cellText.EndsWith (textSeparator))
								{
									if (cellText.Length == textSeparator.Length)
									{
										cellText = string.Empty;
									}
									else
									{
										cellText = cellText.Substring (1, cellText.Length - 2);
										if (cellText.Contains (textSeparator + textSeparator))
										{
											cellText = cellText.Replace (textSeparator + textSeparator, textSeparator);
										}
									}
								}
								cells[i] = cellText;
							}

							contents.Add (cells);
						}

						numRows = contents.Count+1;
						if (contents.Count > 0)
						{
							int width = contents[0].Length+1;
							string[,] outputGrid = new string[width, numRows];

							for (int y = 0; y < numRows-1; y++)
							{
								for (int x = 0; x < width-1; x++)
								{
									if (x >= contents[y].Length)
									{
										Debug.LogWarning ("Error importing file row: " + y + ", line ID: " + contents[y][0] + " - its column count (" + contents[y].Length + ") differs from the header + (" + contents[0].Length + "). Skipping.");
									}
									else
									{
										outputGrid[x, y] = contents[y][x];
									}
								}
							}
							return outputGrid;
						}
						break;
					}
			}

			return null; 
		}
		

		static public string CreateCSVGrid (List<string[]> contents)
		{
			#if UNITY_EDITOR
			CSVFormat format = ACEditorPrefs.CSVFormat;
			#else
			CSVFormat format = CSVFormat.Standard;
			#endif

			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			
			int numRows = contents.Count;
			for (int row=0; row<numRows; row++)
			{
				switch (format)
				{
					case CSVFormat.Legacy:
						{
							bool skipRow = false;

							// Check for delimiter presence
							int numCols = contents[row].Length;
							for (int col = 0; col < numCols; col++)
							{
								string cellText = contents[row][col];
								if (cellText.Contains (legacy_csvDelimiter))
								{
									ACDebug.LogWarning ("Skipping CSV export of text '" + cellText + "' on row #" + row + " because it contains the character '" + legacy_csvDelimiter + "'.");
									skipRow = true;
								}
							}

							if (!skipRow)
							{
								sb.AppendLine (string.Join (legacy_csvDelimiter, contents[row]));
							}
							break;
						}

					case CSVFormat.Standard:
						{
							// Check for delimiter presence
							int numCols = contents[row].Length;
							for (int col = 0; col < numCols; col++)
							{
								string cellText = contents[row][col];

								bool doTextDelimiting = cellText.Contains (fieldSeparator) || cellText.Contains (textSeparator);
								bool doubleQuotes = cellText.Contains (textSeparator);

								if (doubleQuotes)
								{
									cellText = cellText.Replace (textSeparator, textSeparator + textSeparator);
								}

								if (doTextDelimiting)
								{
									cellText = textSeparator + cellText + textSeparator;
								}

								contents[row][col] = cellText;
							}

							sb.AppendLine (string.Join (fieldSeparator, contents[row]));
							break;
						}
				}
			}
			return sb.ToString ();
		}
		
	}

}