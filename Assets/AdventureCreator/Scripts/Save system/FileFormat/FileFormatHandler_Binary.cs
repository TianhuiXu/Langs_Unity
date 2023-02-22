#if !(UNITY_WP8 || UNITY_WINRT || UNITY_WII || UNITY_PS4)
#define CAN_USE_BINARY
#endif

using System.Collections.Generic;
using System;

#if CAN_USE_BINARY
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace AC
{

	/** A format handler that serializes data into binary format */
	public class FileFormatHandler_Binary : iFileFormatHandler
	{

		public string GetSaveMethod ()
		{
			return "Binary";
		}


		public string GetSaveExtension ()
		{
			return ".save";
		}


		public virtual string SerializeObject <T> (object dataObject)
		{
			#if CAN_USE_BINARY
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			MemoryStream memoryStream = new MemoryStream ();
			binaryFormatter.Serialize (memoryStream, dataObject);
			return (Convert.ToBase64String (memoryStream.GetBuffer ()));
			#else
			return string.Empty;
			#endif
		}


		public virtual T DeserializeObject <T> (string dataString)
		{
			#if CAN_USE_BINARY
			BinaryFormatter binaryFormatter = new BinaryFormatter ();
			MemoryStream memoryStream = new MemoryStream (Convert.FromBase64String (dataString));
			return (T) binaryFormatter.Deserialize (memoryStream);
			#else
			return default (T);
			#endif
		}


		public virtual string SerializeAllRoomData (List<SingleLevelData> dataObjects)
		{
			return SerializeObject <List<SingleLevelData>> (dataObjects);
		}


		public virtual List<SingleLevelData> DeserializeAllRoomData (string dataString)
		{
			return (List<SingleLevelData>) DeserializeObject <List<SingleLevelData>> (dataString);
		}


		public virtual T LoadScriptData <T> (string dataString) where T : RememberData
		{
			#if CAN_USE_BINARY
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			MemoryStream memoryStream = new MemoryStream (Convert.FromBase64String (dataString));
			T myObject;
			myObject = binaryFormatter.Deserialize (memoryStream) as T;
			return myObject;
			#else
			return null;
			#endif
		}

	}

}