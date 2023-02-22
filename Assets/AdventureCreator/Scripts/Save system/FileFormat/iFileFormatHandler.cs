/*
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"iFileFormatHandler.cs"
 * 
 *	An interface for classes that handle the conversion of data to saveable strings and vice-versa.  These classes do not handle the disk-handling, only the conversion of data.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	/**
	 * An interface for classes that handle the conversion of data to saveable strings and vice-versa.
	 * These classes do not handle the disk-handling, only the conversion of data.
	 * 
	 * To override the format of save files, create a new class that implements iFileFormatHandler, and assign it with:
	 * 
	 * \code
	 * SaveSystem.FileFormatHandler = new MyClassName ();
	 * \endcode
	 *
	 * Where MyClassName is the name of your class.
	 *
	 * To have this code run when the game begins, place it in the Awake function of a script in your game's first scene.
	 */
	public interface iFileFormatHandler
	{

		/**
		 * <summary>Gets the name of the file format.</summary>
		 * <returns>The name of the file format.</returns>
		 */
		string GetSaveMethod ();

		/**
		 * <summary>Gets the extension of files that are saved in this format.</summary>
		 * <returns>The extension of files that are saved in this format.</returns>
		 */
		string GetSaveExtension ();


		/**
		 * <summary>Converts a serializeable object to a data string that can be saved to disk.</summary>
		 * <param name = "dataObject">The object to convert</param>
		 * <returns>The object, serialized as a string</returns>
		 */
		string SerializeObject <T> (object dataObject);

		/**
		 * <summary>Converts a data string to an object that it represents</summary>
		 * <param name = "dataString">The object represented as a serialized string</param>
		 * <returns>The object, deserialized from the string</returns>
		 */
		T DeserializeObject <T> (string dataString);


		/**
		 * <summary>Converts all scene data, as a List of SingleLevelData isntances, to a single string</summary>
		 * <param name = "dataObjects">The scene data to serialize</param>
		 * <returns>The scene data, serialized as a string</returns>
		 */
		string SerializeAllRoomData (List<SingleLevelData> dataObjects);

		/**
		 * <summary>Converts a data string to a List of SingleLevelData instances</summary>
		 * <param name = "dataString">The List of SingleLevelData, represented as a serialized string</param>
		 * <returns>The List of SingleLevelData instances, deserialized from the string</returns>
		 */
		List<SingleLevelData> DeserializeAllRoomData (string dataString);

		/**
		 * <summary>Converts a data string to a subclass of RememberData</summary>
		 * <param name = "dataString">The RememberData subclass, represented as a serialized string</param>
		 * <returns>The RememberData subclass, deserialized from the string</returns>
		 */
		T LoadScriptData <T> (string dataString) where T : RememberData;

	}

}