/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberTrack.cs"
 * 
 *	This script is attached to Drag Track objects you wish to save.
 * 
 */

using UnityEngine;
using System.Text;

namespace AC
{

	/**
	 * This script is attached to Drag Track objects you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Track")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_moveable.html")]
	public class RememberTrack : Remember
	{

		public override string SaveData()
		{
			TrackData data = new TrackData ();

			data.objectID = constantID;
			data.savePrevented = savePrevented;

			DragTrack track = GetComponent <DragTrack>();
			if (track && track.allTrackSnapData != null)
			{
				StringBuilder stateString = new StringBuilder ();

				foreach (TrackSnapData trackSnapData in track.allTrackSnapData)
				{
					stateString.Append (trackSnapData.ID.ToString ());
					stateString.Append (SaveSystem.colon);
					stateString.Append (trackSnapData.IsEnabled ? "1" : "0");
					stateString.Append (SaveSystem.pipe);
				}

				data.enabledStates = stateString.ToString();
			}

			return Serializer.SaveScriptData<MoveableData> (data);
		}


		public override void LoadData(string stringData)
		{
			TrackData data = Serializer.LoadScriptData <TrackData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			DragTrack track = GetComponent <DragTrack>();
			if (track && track.allTrackSnapData != null)
			{
				string[] valuesArray = data.enabledStates.Split (SaveSystem.pipe[0]);
				for (int i = 0; i < track.allTrackSnapData.Count; i++)
				{
					if (i < valuesArray.Length)
					{
						string[] chunkData = valuesArray[i].Split (SaveSystem.colon[0]);
						if (chunkData != null && chunkData.Length == 2)
						{
							int _regionID = 0;
							if (int.TryParse (chunkData[0], out _regionID))
							{
								TrackSnapData snapData = track.GetSnapData(_regionID);
								if (snapData != null)
								{
									int _isEnabled = 1;
									if (int.TryParse (chunkData[1], out _isEnabled))
									{
										snapData.IsEnabled = (_isEnabled == 1);
									}
								}
							}
						}
					}
				}
			}
		}

	}


	/**
	 * A data container used by the RememberTrack script.
	 */
	[System.Serializable]
	public class TrackData : RememberData
	{

		/** True if the object is on */
		public bool isOn;

		/** Data related to the enabled states of the regions along the Track */
		public string enabledStates;


		/**
		 * The default Constructor.
		 */
		public TrackData() { }

	}

}