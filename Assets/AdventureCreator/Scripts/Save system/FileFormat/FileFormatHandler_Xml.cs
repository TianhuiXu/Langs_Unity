using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml; 
using System.Xml.Serialization;

namespace AC
{

	/** A format handler that serializes data into XML format */
	public class FileFormatHandler_Xml : iFileFormatHandler
	{

		public string GetSaveMethod ()
		{
			return "XML";
		}


		public string GetSaveExtension ()
		{
			return ".savx";
		}


		public virtual string SerializeObject <T> (object dataObject)
		{
			MemoryStream memoryStream = new MemoryStream(); 
			XmlSerializer xs = new XmlSerializer (typeof (T)); 
			XmlTextWriter xmlTextWriter = new XmlTextWriter (memoryStream, Encoding.UTF8); 
			
			xs.Serialize (xmlTextWriter, dataObject); 
			memoryStream = (MemoryStream) xmlTextWriter.BaseStream;

			return UTF8ByteArrayToString (memoryStream.ToArray());
		}


		public virtual T DeserializeObject <T> (string dataString)
		{
			if (!dataString.Contains ("<?xml") && !dataString.Contains ("xml version"))
			{
				return default (T);
			}

			XmlSerializer xs = new XmlSerializer (typeof (T)); 
			MemoryStream memoryStream = new MemoryStream (StringToUTF8ByteArray (dataString)); 

			try
			{
				string dataType = typeof(T).ToString();
				if (dataType.StartsWith ("AC."))
				{
					dataType = dataType.Substring (3);
				}
				else if (dataType.Contains ("[AC."))
				{
					// If it's a list, it's a bit more complicated

					int startIndex = dataType.IndexOf ("[AC.") + 4;
					int length = dataType.Substring (startIndex).IndexOf ("]");
					if (length > 1)
					{
						dataType = dataType.Substring (startIndex, length);
					}
				}

				if (dataString.Contains ("</" + dataType + ">"))
				{
					object deserializedObject = xs.Deserialize (memoryStream);
					if (deserializedObject is T)
					{
						return (T) deserializedObject;
					}
				} 
			}
			catch (System.Exception e)
			{
				ACDebug.LogWarning ("Could not XML deserialize datastring '" + dataString + "; Exception: " + e);
			}
			return default (T);
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
			return DeserializeObject <T> (dataString);
		}


		protected string UTF8ByteArrayToString (byte[] characters) 
		{		
			UTF8Encoding encoding = new UTF8Encoding (); 
			string constructedString = encoding.GetString (characters, 0, characters.Length);
			return (constructedString); 
		}


		protected byte[] StringToUTF8ByteArray (string pXmlString) 
		{ 
			UTF8Encoding encoding = new UTF8Encoding(); 
			byte[] byteArray = encoding.GetBytes (pXmlString); 
			return byteArray; 
		}

	}

}