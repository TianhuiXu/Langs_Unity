/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SaveOperation.cs"
 * 
 *	This class gathers save data and sends it to the SaveFileHandler for file-writing - using threading if supported.
 * 
 */

using System.Threading;
using UnityEngine;

namespace AC
{

	/** This class gathers save data and sends it to the SaveFileHandler for file-writing - using threading if supported. */
	public class SaveOperation : MonoBehaviour
	{

		#region Variables

		private bool saveThreadComplete;
		private SaveData saveData;
		private SaveFile requestedSave;
		private string allData;

		#endregion


		#region UnityStandards

		private void Update ()
		{
			if (saveThreadComplete)
			{
				saveThreadComplete = false;

				if (SaveSystem.SaveFileHandler.SupportsSaveThreading ())
				{
					KickStarter.saveSystem.OnCompleteSaveOperation (requestedSave, true, this);
				}
				else
				{
					SaveSystem.SaveFileHandler.Save (requestedSave, allData, OnFinishSaveRequest);
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Begins a new save operation</summary>
		 * <param name="saveData">The SaveData class, already filled with data that cannot be saved through threading</param>
		 * <param name="saveFile">The SaveFile to write to</param>
		 */
		public void BeginOperation (ref SaveData saveData, SaveFile saveFile)
		{
			this.saveData = saveData;
			requestedSave = new SaveFile (saveFile);

			if (KickStarter.settingsManager.saveWithThreading)
			{
				Thread saveThread = new Thread (SendSaveToFile);
				if (KickStarter.stateHandler.GetMusicEngine ())
				{
					KickStarter.stateHandler.GetMusicEngine ().PrepareSaveBeforeThreading ();
				}
				if (KickStarter.stateHandler.GetAmbienceEngine ())
				{
					KickStarter.stateHandler.GetAmbienceEngine ().PrepareSaveBeforeThreading ();
				}
				saveThread.Start ();
			}
			else
			{
				SendSaveToFile ();
			}
		}

		
		/**
		 * <summary>Checks if a given SaveFile class matches with the one this operation is handling</summary>
		 * <param name="saveFile">The SaveFile class to compare</param>
		 * <returns>True if the SaveFile class matches the one being handled</returns>
		 */
		public bool Matches (SaveFile saveFile)
		{
			if (requestedSave != null && saveFile != null && requestedSave.saveID == saveFile.saveID && requestedSave.profileID == saveFile.profileID)
			{
				return true;
			}
			return false;
		}

		#endregion


		#region PrivateFunctions

		private void OnFinishSaveRequest (bool wasSuccesful)
		{
			// Received data matches requested
			if (!wasSuccesful)
			{
				KickStarter.eventManager.Call_OnSave (FileAccessState.Fail, requestedSave.saveID);
				KickStarter.saveSystem.OnCompleteSaveOperation (requestedSave, false, this);
				return;
			}

			if (KickStarter.settingsManager.saveWithThreading && SaveSystem.SaveFileHandler.SupportsSaveThreading ())
			{
				saveThreadComplete = true;
			}
			else
			{
				KickStarter.saveSystem.OnCompleteSaveOperation (requestedSave, true, this);
			}
		}


		private void SendSaveToFile ()
		{
			saveData.mainData = KickStarter.stateHandler.SaveMainData (saveData.mainData);
			saveData.mainData.movementMethod = (int) KickStarter.settingsManager.movementMethod;
			saveData.mainData.activeInputsData = ActiveInput.CreateSaveData (KickStarter.settingsManager.activeInputs);
			saveData.mainData.timersData = Timer.CreateSaveData (KickStarter.variablesManager.timers);

			saveData.mainData.currentPlayerID = (KickStarter.player)
												? KickStarter.player.ID
												: KickStarter.settingsManager.GetEmptyPlayerID ();
			
			saveData.mainData = KickStarter.runtimeInventory.SaveMainData (saveData.mainData);
			saveData.mainData = KickStarter.runtimeVariables.SaveMainData (saveData.mainData);
			saveData.mainData = KickStarter.playerMenus.SaveMainData (saveData.mainData);
			saveData.mainData = KickStarter.runtimeLanguages.SaveMainData (saveData.mainData);
			saveData.mainData = KickStarter.sceneChanger.SaveMainData (saveData.mainData);

			string mainData = Serializer.SerializeObject<SaveData> (saveData, true);
			string levelData = SaveSystem.FileFormatHandler.SerializeAllRoomData (KickStarter.levelStorage.allLevelData);
			allData = MergeData (mainData, levelData);

			if (KickStarter.settingsManager.saveWithThreading)
			{
				if (SaveSystem.SaveFileHandler.SupportsSaveThreading ())
				{
					SaveSystem.SaveFileHandler.Save (requestedSave, allData, OnFinishSaveRequest);
				}
				else
				{
					saveThreadComplete = true;
				}
			}
			else
			{
				SaveSystem.SaveFileHandler.Save (requestedSave, allData, OnFinishSaveRequest);
			}
		}


		private string MergeData (string _mainData, string _levelData)
		{
			return _mainData.Replace (SaveSystem.mainDataDivider, SaveSystem.mainDataDivider_Replacement) + SaveSystem.mainDataDivider + _levelData.Replace (SaveSystem.mainDataDivider, SaveSystem.mainDataDivider_Replacement);
		}

		#endregion

	}

}