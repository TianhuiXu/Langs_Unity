/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"VariableLinkingExample.cs"
 * 
 *	This script demonstrates how an AC global variable can be synchronised with a variable in a custom script.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script demonstrates how an AC global variable can be synchronised with a variable in a custom script.
	 * To use it, create a new global Integer variable in the Variables Manager, and set its 'Link to' field to 'Custom Script'.
	 * Then, place this script in the scene, and configure its Inspector so that the variable's ID matches the 'Variable ID To Sync With' property.
	 * Whenever the AC variable is read or modified, it will be synchronised with this script's 'My Custom Integer' property.
	 */
	[AddComponentMenu("Adventure Creator/3rd-party/Variable linking example")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_variable_linking_example.html")]
	public class VariableLinkingExample : MonoBehaviour
	{

		public int myCustomInteger = 2;
		public int variableIDToSyncWith = 0;


		private void OnEnable ()
		{
			EventManager.OnDownloadVariable += OnDownload;
			EventManager.OnUploadVariable += OnUpload;
		}


		private void OnDisable ()
		{
			EventManager.OnDownloadVariable -= OnDownload;
			EventManager.OnUploadVariable -= OnUpload;
		}


		private void OnDownload (GVar variable, Variables variables)
		{
			if (variable.id == variableIDToSyncWith)
			{
				variable.IntegerValue = myCustomInteger;
				Debug.Log ("DOWNLOADED : " + myCustomInteger);
			}
		}


		private void OnUpload (GVar variable, Variables variables)
		{
			if (variable.id == variableIDToSyncWith)
			{
				myCustomInteger = variable.IntegerValue;
				Debug.Log ("UPLOADED : " + myCustomInteger);
			}
		}

	}

}