//#if !UNITY_SWITCH
#define ALLOW_VIDEO
//#endif

using UnityEngine;
using System;
using System.Collections.Generic;

namespace AC
{

	/** A format handler that serializes data into Json format */
	public class FileFormatHandler_Json : iFileFormatHandler
	{

		protected const string roomDelimiter = "|ROOMDELIMITER|";


		public string GetSaveMethod ()
		{
			return "Json";
		}


		public string GetSaveExtension ()
		{
			return ".savj";
		}


		public virtual string SerializeObject <T> (object dataObject)
		{
			return JsonUtility.ToJson (dataObject);
		}


		public virtual T DeserializeObject <T> (string dataString)
		{
			return (T) DeserializeObjectJson <T> (dataString);
		}


		protected virtual object DeserializeObjectJson <T> (string jsonString)
		{
			object jsonData = JsonUtility.FromJson (jsonString, typeof (T));

			#if ALLOW_VIDEO
			if (jsonData is VideoPlayerData && !jsonString.Contains ("isPlaying"))
			{
				return null;
			}
			#endif

			// Dirty hack, but with Unity's Json utility we can't check if the data type is correct

			if (jsonData is ActionListParamData && !jsonString.Contains ("paramData"))
			{
				return null;
			}
			if (jsonData is AnimatorData && !jsonString.Contains ("layerWeightData"))
			{
				return null;
			}
			if (jsonData is ColliderData && !jsonString.Contains ("isOn"))
			{
				return null;
			}
			if (jsonData is ContainerData && !jsonString.Contains ("_linkedIDs"))
			{
				return null;
			}
			if (jsonData is ConversationData && !jsonString.Contains ("_optionStates"))
			{
				return null;
			}
			if (jsonData is FootstepSoundData && !jsonString.Contains ("walkSounds"))
			{
				return null;
			}
			if (jsonData is HotspotData && !jsonString.Contains ("buttonStates"))
			{
				return null;
			}
			if (jsonData is MaterialData && !jsonString.Contains ("_materialIDs"))
			{
				return null;
			}
			if (jsonData is MoveableData && !jsonString.Contains ("trackValue"))
			{
				return null;
			}
			if (jsonData is NameData && !jsonString.Contains ("newName"))
			{
				return null;
			}
			if (jsonData is NavMesh2DData && !jsonString.Contains ("_linkedIDs"))
			{
				return null;
			}
			if (jsonData is NPCData && !jsonString.Contains ("isHeadTurning"))
			{
				return null;
			}
			if (jsonData is ParticleSystemData && !jsonString.Contains ("isPaused"))
			{
				return null;
			}
			if (jsonData is ShapeableData && !jsonString.Contains ("_activeKeyIDs"))
			{
				return null;
			}
			if (jsonData is SoundData && !jsonString.Contains ("isPlaying"))
			{
				return null;
			}
			if (jsonData is TimelineData && !jsonString.Contains ("timelineAssetID"))
			{
				return null;
			}
			if (jsonData is TrackData && !jsonString.Contains ("enabledStates"))
			{
				return null;
			}
			if (jsonData is TransformData && !jsonString.Contains ("bringBack"))
			{
				return null;
			}
			if (jsonData is TriggerData && !jsonString.Contains ("isOn"))
			{
				return null;
			}
			if (jsonData is VariablesData && !jsonString.Contains ("variablesData"))
			{
				return null;
			}
			#if ALLOW_VIDEO
			if (jsonData is VideoPlayerData && !jsonString.Contains ("clipAssetID"))
			{
				return null;
			}
			#endif
			if (jsonData is VisibilityData && !jsonString.Contains ("useDefaultTintMap"))
			{
				return null;
			}
			return jsonData;
		}


		public virtual string SerializeAllRoomData (List<SingleLevelData> dataObjects)
		{
			// Can't serialize a list, so split by delimeter
			string serializedString = string.Empty;
			if (dataObjects != null && dataObjects.Count > 0)
			{
				for (int i=0; i<dataObjects.Count; i++)
				{
					serializedString += SerializeObject <SingleLevelData> (dataObjects[i]);
					if (i < (dataObjects.Count -1))
					{
						serializedString += roomDelimiter;
					}
				}
			}
			return serializedString;
		}


		public virtual List<SingleLevelData> DeserializeAllRoomData (string dataString)
		{
			// Can't serialize a list, so split by delimeter
			List<SingleLevelData> allLevelData = new List<SingleLevelData>();
			string[] stringSeparators = new string[] {roomDelimiter};
			string[] levelDataStrings = dataString.Split (stringSeparators, StringSplitOptions.None);
			foreach (string levelDataString in levelDataStrings)
			{
				SingleLevelData levelData = DeserializeObject <SingleLevelData> (levelDataString);
				allLevelData.Add (levelData);
			}
			return allLevelData;
		}


		public virtual T LoadScriptData <T> (string dataString) where T : RememberData
		{
			return DeserializeObject <T> (dataString);
		}

	}

}