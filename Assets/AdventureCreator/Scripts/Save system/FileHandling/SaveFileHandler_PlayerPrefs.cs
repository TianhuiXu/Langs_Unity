#if !(UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII || UNITY_PS4 || UNITY_SWITCH || UNITY_WSA)
#define CAN_HANDLE_SCREENSHOTS
#endif

using UnityEngine;
using System;
using System.Collections.Generic;

namespace AC
{

	/** A save-file handler that stores save-games as separate keys in the PlayerPrefs */
	public class SaveFileHandler_PlayerPrefs : iSaveFileHandler
	{

		protected const string screenshotKey = "_screenshot";


		public virtual string GetDefaultSaveLabel (int saveID)
		{
			string label = (saveID == 0)
							? SaveSystem.AutosaveLabel
							: (SaveSystem.SaveLabel + " " + saveID.ToString ());

			return label;
		}


		public virtual void DeleteAll (int profileID)
		{
			List<SaveFile> allSaveFiles = GatherSaveFiles (profileID);
			foreach (SaveFile saveFile in allSaveFiles)
			{
				Delete (saveFile);
			}
		}


		public virtual void Delete (SaveFile saveFile, System.Action<bool> callback = null)
		{
			string filename = saveFile.fileName;

			if (PlayerPrefs.HasKey (filename))
			{
				PlayerPrefs.DeleteKey (filename);
				ACDebug.Log ("PlayerPrefs key deleted: " + filename);

				if (KickStarter.settingsManager.saveScreenshots == SaveScreenshots.Always || (KickStarter.settingsManager.saveScreenshots == SaveScreenshots.ExceptWhenAutosaving && !saveFile.IsAutoSave))
				{
					if (PlayerPrefs.HasKey (filename + screenshotKey))
					{
						PlayerPrefs.DeleteKey (filename + screenshotKey);

						if (callback != null) callback.Invoke (true);
						return;
					}
				}
			}

			if (callback != null) callback.Invoke (false);
		}


		public virtual void Save (SaveFile saveFile, string dataToSave, System.Action<bool> callback)
		{
			string fullFilename = GetSaveFilename (saveFile.saveID, saveFile.profileID);
			bool isSuccessful = false;

			try
			{
				PlayerPrefs.SetString (fullFilename, dataToSave);
				#if UNITY_PS4 || UNITY_SWITCH
				PlayerPrefs.Save ();
				#endif
				ACDebug.Log ("PlayerPrefs key written: " + fullFilename);
				isSuccessful = true;
			}
			catch (Exception e)
 			{
				ACDebug.LogWarning ("Could not save PlayerPrefs data under key " + fullFilename + ". Exception: " + e);
 			}

 			if (isSuccessful)
 			{
 				string dateKey = fullFilename + "_timestamp";

	 			try
	 			{
					DateTime startDate = new DateTime (2000, 1, 1, 0, 0, 0).ToUniversalTime ();

					int secs = (int) (DateTime.UtcNow - startDate).TotalSeconds;
					string timestampData = secs.ToString ();

					PlayerPrefs.SetString (dateKey, timestampData);
	 				#if UNITY_PS4 || UNITY_SWITCH
					PlayerPrefs.Save ();
					#endif
	 			}
				catch (Exception e)
	 			{
					ACDebug.LogWarning ("Could not save PlayerPrefs data under key " + dateKey + ". Exception: " + e);
	 			}
	 		}

			callback.Invoke (isSuccessful);
		}


		public virtual void Load (SaveFile saveFile, bool doLog, System.Action<SaveFile, string> callback)
		{
			string filename = saveFile.fileName;
			string _data = PlayerPrefs.GetString (filename, string.Empty);
			
			if (doLog && !string.IsNullOrEmpty (_data))
			{
				ACDebug.Log ("PlayerPrefs key read: " + filename);
			}

			callback.Invoke (saveFile, _data);
		}


		public virtual bool SupportsSaveThreading ()
		{
			return false;
		}


		public virtual List<SaveFile> GatherSaveFiles (int profileID)
		{
			return GatherSaveFiles (profileID, false, -1, string.Empty);
		}


		public virtual SaveFile GetSaveFile (int saveID, int profileID)
		{
			return GetSaveFile (saveID, profileID, false, -1, string.Empty);
		}


		protected virtual SaveFile GetSaveFile (int saveID, int profileID, bool isImport, int boolID, string separateFilePrefix)
		{
			bool isAutoSave = (saveID == 0);
			string filename = (isImport) ? GetImportFilename (saveID, separateFilePrefix, profileID) : GetSaveFilename (saveID, profileID);

			if (PlayerPrefs.HasKey (filename))
			{
				string label = isAutoSave
								? SaveSystem.AutosaveLabel
								: SaveSystem.SaveLabel + " " + saveID.ToString ();

				Texture2D screenShot = null;
				if (KickStarter.settingsManager.saveScreenshots == SaveScreenshots.Always || (KickStarter.settingsManager.saveScreenshots == SaveScreenshots.ExceptWhenAutosaving && !isAutoSave))
				{
					if (PlayerPrefs.HasKey (filename + screenshotKey) && KickStarter.saveSystem)
					{
						try
						{
							string screenshotData = PlayerPrefs.GetString (filename + screenshotKey);
							if (!string.IsNullOrEmpty (screenshotData))
							{
								byte[] result = Convert.FromBase64String (screenshotData);
								if (result != null)
								{
									screenShot = new Texture2D (KickStarter.saveSystem.ScreenshotWidth, KickStarter.saveSystem.ScreenshotHeight, TextureFormat.RGB24, false, KickStarter.settingsManager.linearColorTextures);
									screenShot.LoadImage (result);
									screenShot.Apply ();
								}
							}
						}
						catch (Exception e)
						{
							ACDebug.LogWarning ("Could not load PlayerPrefs data from key " + filename + screenshotKey + ". Exception: " + e);
						}
					}
				}

				int updateTime = 0;
				if (KickStarter.settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
				{
					string dateKey = filename + "_timestamp";

					if (PlayerPrefs.HasKey (dateKey))
					{
						string timestampData = PlayerPrefs.GetString (dateKey);
						if (!string.IsNullOrEmpty (timestampData))
						{
							if (int.TryParse (timestampData, out updateTime) && !isAutoSave)
							{
								DateTime startDate = new DateTime (2000, 1, 1, 0, 0, 0).ToUniversalTime ();
								DateTime saveDate = startDate.AddSeconds (updateTime);

								label += GetTimeString (saveDate);
							}
						}
					}
				}

				return new SaveFile (saveID, profileID, label, filename, screenShot, string.Empty, updateTime);
			}

			return null;
		}


		public virtual List<SaveFile> GatherImportFiles (int profileID, int boolID, string separateProductName, string separateFilePrefix)
		{
			if (!string.IsNullOrEmpty (separateProductName) && !string.IsNullOrEmpty (separateFilePrefix))
			{
				return GatherSaveFiles (profileID, true, boolID, separateFilePrefix);
			}
			return null;
		}


		protected virtual List<SaveFile> GatherSaveFiles (int profileID, bool isImport, int boolID, string separateFilePrefix)
		{
			List<SaveFile> gatheredSaveFiles = new List<SaveFile>();

			for (int i = 0; i < SaveSystem.MAX_SAVES; i++)
			{
				SaveFile saveFile = GetSaveFile (i, profileID, isImport, boolID, separateFilePrefix);
				if (saveFile != null)
				{
					gatheredSaveFiles.Add (saveFile);
				}
			}

			return gatheredSaveFiles;
		}


		public virtual void SaveScreenshot (SaveFile saveFile)
		{
			string fullFilename = GetSaveFilename (saveFile.saveID, saveFile.profileID) + screenshotKey;

			try
			{
				byte[] bytes = saveFile.screenShot.EncodeToJPG ();
				string dataToSave = Convert.ToBase64String (bytes);

				PlayerPrefs.SetString (fullFilename, dataToSave);
				#if UNITY_PS4 || UNITY_SWITCH
				PlayerPrefs.Save ();
				#endif
				ACDebug.Log ("PlayerPrefs key written: " + fullFilename);
			}
			catch (Exception e)
 			{
				ACDebug.LogWarning ("Could not save PlayerPrefs data under key " + fullFilename + ". Exception: " + e);
 			}
		}


		protected virtual string GetSaveFilename (int saveID, int profileID = -1)
		{
			if (profileID == -1)
			{
				profileID = Options.GetActiveProfileID ();
			}

			return KickStarter.settingsManager.SavePrefix + SaveSystem.GenerateSaveSuffix (saveID, profileID);
		}


		protected virtual string GetImportFilename (int saveID, string filePrefix, int profileID = -1)
		{
			if (profileID == -1)
			{
				profileID = Options.GetActiveProfileID ();
			}

			return filePrefix + SaveSystem.GenerateSaveSuffix (saveID, profileID);
		}


		protected virtual string GetTimeString (DateTime dateTime)
		{
			if (KickStarter.settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
			{
				if (KickStarter.settingsManager.saveTimeDisplay == SaveTimeDisplay.CustomFormat)
				{
					string creationTime = dateTime.ToString (KickStarter.settingsManager.customSaveFormat);
					return " (" + creationTime + ")";
				}
				else
				{
					string creationTime = dateTime.ToShortDateString ();
					if (KickStarter.settingsManager.saveTimeDisplay == SaveTimeDisplay.TimeAndDate)
					{
						creationTime += " " + dateTime.ToShortTimeString ();
					}
					return " (" + creationTime + ")";
				}
			}

			return string.Empty;
		}

	}

}