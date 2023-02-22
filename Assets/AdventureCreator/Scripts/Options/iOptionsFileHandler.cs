/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"iOptionsFileHandler.cs"
 * 
 *	An interface for classes that handle the saving and loading of OptionsData.
 * 
 */

namespace AC
{

	/**
	 * An interface for classes that handle the saving and loading of OptionsData.
	 * This class only handles the reading and writing of the data to and from disk - the data is already serialized as a string, and does not need processing. 
	 * 
	 * To override where options data is saved, create a new class that implements iOptionsFileHandler, and assign it with:
	 * 
	 * \code
	 * Options.OptionsFileHandler = new MyClassName ();
	 * \endcode
	 *
	 * Where MyClassName is the name of your class.
	 *
	 * To have this code run when the game begins, place it in the Awake function of a script in your game's first scene.
	 */
	public interface iOptionsFileHandler
	{

		/**
		 * <summary>Saves the OptionsData, serialized as a string, to disk.</summary>
		 * <param name = "profileID">The ID number of the profile to be saved, or 0 if profiles are not enabled</param>
		 * <param name = "dataString">The serialized data to save</param>
		 * <param name = "showLog">If True, a Console message upon succesful saving is requested</param>
		 */
		void SaveOptions (int profileID, string dataString, bool showLog);

		/**
		 * <summary>Loads the OptionsData, serialized as a string, from disk.</summary>
		 * <param name = "profileID">The ID number of the profile to be loaded, or 0 if profiles are not enabled</param>
		 * <returns>The OptionsData, serialized as a string</returns>
		 */
		string LoadOptions (int profileID, bool showLog);

		/**
		 * <summary>Deletes the saved OptionsData string for a given profile.</summary>
		 * <param name = "profileID">The ID number of the profile to delete</parm>
		 */
		void DeleteOptions (int profileID);


		/**
		 * <summary>Returns the active profile ID number, if profiles are enabled</summary>
		 * <returns>The ID number of the active profile, or 0 if profiles are not enabled.</returns>
		 */
		int GetActiveProfile ();

		/**
		 * <summary>Records the active profile ID</summary>
		 * <param name = "profileID">The ID number of the profile to set as active</parm>
		 */
		void SetActiveProfile (int profileID);

		/**
		 * <summary>Checks if OptionsData for a given profile is stored</summary>
		 * <param name = "profileID">The ID number of the profile to check for</parm>
		 * <returns>True if OptionsData for the given profile exists</returns>
		 */
		bool DoesProfileExist (int profileID);

	}

}