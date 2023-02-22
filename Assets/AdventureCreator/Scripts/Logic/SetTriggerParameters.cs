/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SetTriggerParameters.cs"
 * 
 *	A component used to set all of an Interaction's parameters when run as the result of interacting with a Hotspot.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** A component used to set all of an Trigger's parameter values */
	[RequireComponent (typeof (AC_Trigger))]
	public class SetTriggerParameters : SetParametersBase
	{

		#region Variables

		private AC_Trigger ownTrigger;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			ownTrigger = GetComponent <AC_Trigger>();
			EventManager.OnRunTrigger += OnRunTrigger;
		}


		protected void OnDisable ()
		{
			EventManager.OnRunTrigger -= OnRunTrigger;
		}

		#endregion


		#region CustomEvents

		protected void OnRunTrigger (AC_Trigger trigger, GameObject collidingObject)
		{
			if (trigger.source != ActionListSource.AssetFile || trigger.assetFile == null)
			{
				return;
			}

			if (trigger.assetFile.NumParameters == 0)
			{
				return;
			}

			if (trigger.gameObject != gameObject)
			{
				return;
			}

			if (trigger != ownTrigger && ownTrigger.assetFile && trigger.assetFile.NumParameters != ownTrigger.assetFile.NumParameters)
			{
				return;
			}

			AssignParameterValues (trigger);
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			AC_Trigger[] triggers = GetComponents <AC_Trigger>();

			if (triggers.Length == 0)
			{
				EditorGUILayout.HelpBox ("This component must be attached to an AC Trigger.", MessageType.Info);
				return;
			}

			AC_Trigger trigger = triggers[0];

			if (trigger.source == ActionListSource.InScene)
			{
				EditorGUILayout.HelpBox ("This component requires that the Trigger's Source field is set to Asset File", MessageType.Warning);
				return;
			}
			else if (trigger.source == ActionListSource.AssetFile && trigger.assetFile && trigger.assetFile.NumParameters > 0)
			{
				ShowParametersGUI (trigger.assetFile.DefaultParameters, trigger.syncParamValues);
			}
			else
			{
				EditorGUILayout.HelpBox ("No parameters defined for Trigger '" + trigger.gameObject.name + "'.", MessageType.Warning);
				return;
			}

			if (triggers.Length > 1)
			{
				EditorGUILayout.HelpBox ("Multiple Trigger components detected - parameters will be set for all Triggers that share the same number of parameters as the first.", MessageType.Info);
			}
		}

		#endif

	}

}
