/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberActionListParameters.cs"
 * 
 *	This script is attached to ActionLists in the scene whose parameter values we wish to save.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script is attached to ActionLists in the scene whose parameter values we wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember ActionList parameters")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_action_list_parameters.html")]
	public class RememberActionListParameters : Remember
	{

		public override string SaveData ()
		{
			ActionList actionList = GetComponent <ActionList>();
			if (actionList == null) return string.Empty;

			ActionListParamData data = new ActionListParamData();
			data.objectID = constantID;
			data.savePrevented = savePrevented;

			data.paramData = actionList.GetParameterData ();

			return Serializer.SaveScriptData <ActionListParamData> (data);
		}


		public override void LoadData (string stringData)
		{
			ActionList actionList = GetComponent <ActionList>();
			if (actionList == null) return;

			ActionListParamData data = Serializer.LoadScriptData <ActionListParamData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented;if (savePrevented) return;

			actionList.SetParameterData (data.paramData);
		}

	}


	/**
	 * A data container used by the RememberActionListParameters script.
	 */
	[System.Serializable]
	public class ActionListParamData : RememberData
	{

		/** The paramater values */
		public string paramData;

		/** The default Constructor. */
		public ActionListParamData () { }

	}

}