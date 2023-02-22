/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberVariables.cs"
 * 
 *	This script is attached to Variables components in the scene we wish to save the state of.
 * 
 */

using UnityEngine;

namespace AC
{

	/** This script is attached to Variables components in the scene we wish to save the state of. */
	[RequireComponent (typeof (Variables))]
	[AddComponentMenu("Adventure Creator/Save system/Remember Variables")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_variables.html")]
	public class RememberVariables : Remember
	{

		#region Variables

		private Variables variables;

		#endregion


		#region PublicFunctions

		public override string SaveData ()
		{
			VariablesData data = new VariablesData ();

			foreach (GVar var in Variables.vars)
			{
				var.Download (VariableLocation.Component);
			}

			data.variablesData = SaveSystem.CreateVariablesData (Variables.vars, false, VariableLocation.Component);

			return Serializer.SaveScriptData <VariablesData> (data);
		}
		

		public override void LoadData (string stringData)
		{
			VariablesData data = Serializer.LoadScriptData <VariablesData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			Variables.vars = SaveSystem.UnloadVariablesData (data.variablesData, true, Variables.vars);

			foreach (GVar var in Variables.vars)
			{
				var.Upload (VariableLocation.Component, Variables);
				var.BackupValue ();
			}

			loadedData = true;
		}

		#endregion


		#region GetSet

		private Variables Variables
		{
			get
			{
				if (variables == null)
				{
					variables = GetComponent <Variables>();
				}
				return variables;
			}
		}

		#endregion

	}


	/** A data container used by the RememberVariables script. */
	[System.Serializable]
	public class VariablesData : RememberData
	{

		/** The values of the variables */
		public string variablesData;


		/** The default constructor */
		public VariablesData ()
		{
			variablesData = string.Empty;
		}

	}

}