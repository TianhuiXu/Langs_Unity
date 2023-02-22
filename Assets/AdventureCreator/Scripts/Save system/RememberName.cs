/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberName.cs"
 * 
 *	This script is attached to gameObjects in the scene
 *	with a name we wish to save.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script is attached to GameObject in the scene whose change in name we wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Name")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_name.html")]
	public class RememberName : Remember
	{

		public override string SaveData ()
		{
			NameData nameData = new NameData();
			nameData.objectID = constantID;
			nameData.savePrevented = savePrevented;

			nameData.newName = gameObject.name;

			return Serializer.SaveScriptData <NameData> (nameData);
		}


		public override void LoadData (string stringData)
		{
			NameData data = Serializer.LoadScriptData <NameData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			gameObject.name = data.newName;
		}

	}


	/**
	 * A data container used by the RememberName script.
	 */
	[System.Serializable]
	public class NameData : RememberData
	{

		/** The GameObject's new name */
		public string newName;

		/**
		 * The default Constructor.
		 */
		public NameData () { }

	}

}