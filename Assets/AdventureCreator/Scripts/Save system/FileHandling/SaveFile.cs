/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SaveFile.cs"
 * 
 *	A data container for save game files found in the file system.  Instances of this struct are listed in the foundSaveFiles List in SaveSystem.
 * 
 */

using UnityEngine;

namespace AC
{

	/** A data container for save game files found in the file system.  Instances of this struct are listed in the foundSaveFiles List in SaveSystem. */
	public class SaveFile
	{

		#region Variables

		/** A unique identifier for the save file */
		public int saveID;
		/** The associated profile for the save */
		public int profileID;
		/** The save's label, as displayed in a MenuSavesList element */
		public string label;
		/** The save's screenshot, if save game screenshots are enabled */
		public Texture2D screenShot;
		/** The complete filename of the file, including the filepath */
		public string fileName;
		/** The complete filename of the associated screenshot, including the filepath (if available) */
		public string screenshotFilename;
		/** The timestamp of the file's last-updated time */
		public int updatedTime;

		#endregion


		#region Constructors

		/** The default Constructor. */
		public SaveFile (int _saveID, int _profileID, string _label, string _fileName, Texture2D _screenShot, string _screenshotFilename, int _updatedTime = 0)
		{
			saveID = _saveID;
			profileID = _profileID;
			label = _label;
			fileName = _fileName;
			screenShot = _screenShot;
			screenshotFilename = _screenshotFilename;
			
			if (_updatedTime > 0)
			{
				updatedTime = 200000000 - _updatedTime;
			}
			else
			{
				updatedTime = 0;
			}
		}


		/** A Constructor that copies the values of another SaveFile. */
		public SaveFile (SaveFile _saveFile)
		{
			saveID = _saveFile.saveID;
			profileID = _saveFile.profileID;
			label = _saveFile.label;
			screenShot = _saveFile.screenShot;
			screenshotFilename = _saveFile.screenshotFilename;
			fileName = _saveFile.fileName;
			updatedTime = _saveFile.updatedTime;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Sets the save file's label in a safe format. Pipe's and colons are converted so that they can be stored.</summary>
		 * <param name = "_label">The new label for the file.</param>
		 */
		public void SetLabel (string _label)
		{
			label = AdvGame.PrepareStringForLoading (_label);
		}


		/**
		 * <summary>Gets the save file's label.  Pipes and colons are converted back so that they can be read as expected.</summary>
		 * <returns>The file's label</returns>
		 */
		public string GetSafeLabel ()
		{
			return AdvGame.PrepareStringForSaving (label);
		}

		#endregion


		#region GetSet
		
		/** If True, then the file is considered to be an AutoSave */
		public bool IsAutoSave
		{
			get
			{
				return (saveID == 0);
			}
		}

		#endregion

	}

}