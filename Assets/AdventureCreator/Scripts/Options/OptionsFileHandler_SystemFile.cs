using UnityEngine;
using System.IO;

namespace AC
{

	/** A file handler that stores Options data as files on the machine */
	public class OptionsFileHandler_SystemFile : iOptionsFileHandler
	{

		#region PublicFunctions

		public void SaveOptions (int profileID, string dataString, bool showLog)
		{
			string fullFilename = GetProfileFullFilename (profileID);
			
			try
			{
				StreamWriter writer;
				FileInfo t = new FileInfo (fullFilename);

				if (!t.Exists)
				{
					writer = t.CreateText ();
				}

				else
				{
					t.Delete ();
					writer = t.CreateText ();
				}

				writer.Write (dataString);
				writer.Close ();

				if (showLog)
				{
					ACDebug.Log ("File written: " + fullFilename);
				}
			}
			catch (System.Exception e)
			{
				ACDebug.LogWarning ("Could not save data to file '" + fullFilename + "'. Exception: " + e);
			}
		}


		public string LoadOptions (int profileID, bool showLog)
		{
			string fullFilename = GetProfileFullFilename (profileID);

			if (File.Exists (fullFilename))
			{
				StreamReader r = File.OpenText (fullFilename);

				string _info = r.ReadToEnd ();
				r.Close ();
				string dataString = _info;

				if (showLog && !string.IsNullOrEmpty (dataString))
				{
					ACDebug.Log ("File read: " + fullFilename);
				}

				return dataString;
			}

			return string.Empty;
		}


		public void DeleteOptions (int profileID)
		{
			string fullFilename = GetProfileFullFilename (profileID);

			FileInfo t = new FileInfo (fullFilename);
			if (t.Exists)
			{
				t.Delete ();
				ACDebug.Log ("File deleted: " + fullFilename);
			}
		}


		public int GetActiveProfile ()
		{
			string fullFilename = GetActiveProfileDataFullFilename ();

			if (File.Exists (fullFilename))
			{
				StreamReader r = File.OpenText (fullFilename);

				string _info = r.ReadToEnd ();
				r.Close ();
				string dataString = _info;

				int profileID;
				if (int.TryParse (dataString, out profileID))
				{
					return profileID;
				}
			}

			return 0;
		}


		public void SetActiveProfile (int profileID)
		{
			string fullFilename = GetActiveProfileDataFullFilename ();

			try
			{
				StreamWriter writer;
				FileInfo t = new FileInfo (fullFilename);

				if (!t.Exists)
				{
					writer = t.CreateText ();
				}

				else
				{
					t.Delete ();
					writer = t.CreateText ();
				}

				writer.Write (profileID.ToString ());
				writer.Close ();
			}
			catch (System.Exception e)
			{
				ACDebug.LogWarning ("Could not save data to file '" + fullFilename + "'. Exception: " + e);
			}
		}

		
		public bool DoesProfileExist (int profileID)
		{
			string fullFilename = GetProfileFullFilename (profileID);

			FileInfo t = new FileInfo (fullFilename);
			if (t.Exists)
			{
				return true;
			}

			return false;
		}

		#endregion


		#region ProtectedFunctions

		protected string GetProfileFullFilename (int profileID)
		{
			return KickStarter.saveSystem.PersistentDataPath + Path.DirectorySeparatorChar.ToString () + KickStarter.settingsManager.SavePrefix + "_ProfileData_" + profileID.ToString () + SaveSystem.OptionsFileFormatHandler.GetSaveExtension ();
		}


		protected string GetActiveProfileDataFullFilename ()
		{
			return KickStarter.saveSystem.PersistentDataPath + Path.DirectorySeparatorChar.ToString () + KickStarter.settingsManager.SavePrefix + "_ActiveProfileData" + SaveSystem.OptionsFileFormatHandler.GetSaveExtension ();
		}

		#endregion

	}

}