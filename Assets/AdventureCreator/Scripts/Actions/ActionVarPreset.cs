/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVarPreset.cs"
 * 
 *	This action is used to set the value of Global and Local Variables
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionVarPreset : Action
	{
		
		public VariableLocation location;
		public int presetID;
		public int parameterID = -1;
		public bool ignoreOptionLinked = false;

		protected LocalVariables localVariables;

		
		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Assign preset"; }}
		public override string Description { get { return "Bulk-assigns the values of all Global or Local values to a predefined preset within the Variables Manager."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			presetID = AssignVariableID (parameters, parameterID, presetID);
		}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}

		
		public override float Run ()
		{
			if (location == VariableLocation.Local && !isAssetFile)
			{
				if (localVariables != null)
				{
					localVariables.AssignFromPreset (presetID);
				}
			}
			else
			{
				KickStarter.runtimeVariables.AssignFromPreset (presetID, ignoreOptionLinked);
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (isAssetFile)
			{
				location = VariableLocation.Global;
			}
			else
			{
				location = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", location);
			}
			
			if (location == VariableLocation.Global)
			{
				if (AdvGame.GetReferences ().variablesManager)
				{
					ShowPresetGUI (AdvGame.GetReferences ().variablesManager.varPresets);
					ignoreOptionLinked = EditorGUILayout.ToggleLeft ("Ignore option-linked variables?", ignoreOptionLinked);
				}
			}
			
			else if (location == VariableLocation.Local)
			{
				if (localVariables)
				{
					ShowPresetGUI (localVariables.varPresets);
				}
				else
				{
					EditorGUILayout.HelpBox ("No 'Local Variables' component found in the scene. Please add an AC GameEngine object from the Scene Manager.", MessageType.Info);
				}
			}

			else if (location == VariableLocation.Component)
			{
				EditorGUILayout.HelpBox ("This Variable source type does not suppport presets.", MessageType.Info);
			}
		}
		
		
		private void ShowPresetGUI (List<VarPreset> _varPresets)
		{
			List<string> labelList = new List<string>();
			
			int i = 0;
			int presetNumber = -1;
			
			if (_varPresets.Count > 0)
			{
				foreach (VarPreset _varPreset in _varPresets)
				{
					if (_varPreset.label != "")
					{
						labelList.Add (i.ToString () + ": " + _varPreset.label);
					}
					else
					{
						labelList.Add (i.ToString () + ": (Untitled)");
					}
					
					if (_varPreset.ID == presetID)
					{
						presetNumber = i;
					}
					i++;
				}
				
				if (presetNumber == -1)
				{
					presetID = 0;
				}
				else if (presetNumber >= _varPresets.Count)
				{
					presetNumber = Mathf.Max (0, _varPresets.Count - 1);
				}
				else
				{
					presetNumber = EditorGUILayout.Popup ("Created presets:", presetNumber, labelList.ToArray());
					presetID = _varPresets[presetNumber].ID;
				}
			}
			else
			{
				presetID = presetNumber = -1;
				EditorGUILayout.HelpBox ("No presets defined - presets are created in the Variables Manager", MessageType.Warning);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (location == VariableLocation.Local && !isAssetFile)
			{
				if (localVariables)
				{
					return GetLabelString (localVariables.varPresets);
				}
			}
			else
			{
				if (AdvGame.GetReferences ().variablesManager)
				{
					return GetLabelString (AdvGame.GetReferences ().variablesManager.varPresets);
				}
			}
			return string.Empty;
		}
		
		
		private string GetLabelString (List<VarPreset> varPresets)
		{
			foreach (VarPreset varPreset in varPresets)
			{
				if (varPreset.ID == presetID)
				{
					return varPreset.label;
				}
			}
			return string.Empty;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Variable: Assign preset' Action, set to assign a Global preset</summary>
		 * <param name = "presetID">The ID number of the Global preset to assign</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarPreset CreateNew_Global (int presetID)
		{
			ActionVarPreset newAction = CreateNew<ActionVarPreset> ();
			newAction.location = VariableLocation.Global;
			newAction.presetID = presetID;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Variable: Assign preset' Action, set to assign a Local preset</summary>
		 * <param name = "presetID">The ID number of the Local preset to assign</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVarPreset CreateNew_Local (int presetID)
		{
			ActionVarPreset newAction = CreateNew<ActionVarPreset> ();
			newAction.location = VariableLocation.Local;
			newAction.presetID = presetID;
			return newAction;
		}
		
	}
	
}
