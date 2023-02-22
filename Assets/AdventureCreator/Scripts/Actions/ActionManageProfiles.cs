/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionManageProfiles.cs"
 * 
 *	This Action creates, renames and and deletes save game profiles
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
	public class ActionManageProfiles : Action
	{

		public ManageProfileType manageProfileType = ManageProfileType.CreateProfile;
		public DeleteProfileType deleteProfileType = DeleteProfileType.ActiveProfile;

		public int profileIndex = 0;
		public int profileIndexParameterID = -1;

		public int varID;
		public int slotVarID;
		public string customLabel;

		public bool useCustomLabel = false;
		public bool preProcessTokens = true;

		public string menuName = "";
		public string elementName = "";

		
		public override ActionCategory Category { get { return ActionCategory.Save; }}
		public override string Title { get { return "Manage profiles"; }}
		public override string Description { get { return "Creates, renames and deletes save game profiles."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			UpgradeSelf ();
			profileIndex = AssignInteger (parameters, profileIndexParameterID, profileIndex);
		}
		
		
		public override float Run ()
		{
			if (!KickStarter.settingsManager.useProfiles)
			{
				LogWarning ("Save game profiles are not enabled - please set in Settings Manager to use this Action.");
				return 0f;
			}

			string newProfileLabel = string.Empty;
			if ((manageProfileType == ManageProfileType.CreateProfile && useCustomLabel) || manageProfileType == ManageProfileType.RenameProfile)
			{
				newProfileLabel = customLabel;
				if (preProcessTokens)
				{
					newProfileLabel = AdvGame.ConvertTokens (newProfileLabel);
				}
			}

			if (manageProfileType == ManageProfileType.CreateProfile)
			{
				KickStarter.options.CreateProfile (newProfileLabel);
			}
			else if (manageProfileType == ManageProfileType.DeleteProfile ||
					 manageProfileType == ManageProfileType.RenameProfile ||
					 manageProfileType == ManageProfileType.SwitchActiveProfile)
			{
				if (deleteProfileType == DeleteProfileType.ActiveProfile)
				{
					if (manageProfileType == ManageProfileType.DeleteProfile)
					{
						KickStarter.saveSystem.DeleteProfile ();
					}
					else if (manageProfileType == ManageProfileType.RenameProfile)
					{
						KickStarter.options.RenameProfile (newProfileLabel);
					}
					return 0f;
				}
				else if (deleteProfileType == DeleteProfileType.SetProfileID)
				{
					int profileID = Mathf.Max (0, profileIndex);

					if (manageProfileType == ManageProfileType.DeleteProfile)
					{
						KickStarter.saveSystem.DeleteProfileID (profileID);
					}
					else if (manageProfileType == ManageProfileType.RenameProfile)
					{
						KickStarter.options.RenameProfileID (newProfileLabel, profileID);
					}
					else if (manageProfileType == ManageProfileType.SwitchActiveProfile)
					{
						Options.SwitchProfileID (profileID);
					}
				}
				else if (deleteProfileType == DeleteProfileType.SetSlotIndex ||
						 deleteProfileType == DeleteProfileType.SlotIndexFromVariable)
			 	{
					int i = Mathf.Max (0, profileIndex);

					if (deleteProfileType == DeleteProfileType.SlotIndexFromVariable)
					{
						GVar gVar = GlobalVariables.GetVariable (slotVarID);
						if (gVar != null)
						{
							i = gVar.IntegerValue;
						}
						else
						{
							LogWarning ("Could not " + manageProfileType.ToString () + " - no variable found.");
							return 0f;
						}
					}

					bool includeActive = true;
					if (menuName != "" && elementName != "")
					{
						MenuElement menuElement = PlayerMenus.GetElementWithName (menuName, elementName);
						if (menuElement != null && menuElement is MenuProfilesList)
						{
							MenuProfilesList menuProfilesList = (MenuProfilesList) menuElement;

							if (menuProfilesList.fixedOption)
							{
								LogWarning ("Cannot refer to ProfilesLst " + elementName + " in Menu " + menuName + ", as it lists a fixed profile ID only!");
								return 0f;
							}

							i += menuProfilesList.GetOffset ();
							includeActive = menuProfilesList.showActive;
						}
						else
						{
							LogWarning ("Cannot find ProfilesList element '" + elementName + "' in Menu '" + menuName + "'.");
						}
					}
					else
					{
						LogWarning ("No ProfilesList element referenced when trying to delete profile slot " + i.ToString ());
					}

					if (manageProfileType == ManageProfileType.DeleteProfile)
					{
						KickStarter.saveSystem.DeleteProfile (i, includeActive);
					}
					else if (manageProfileType == ManageProfileType.RenameProfile)
					{
						KickStarter.options.RenameProfile (newProfileLabel, i, includeActive);
					}
					else if (manageProfileType == ManageProfileType.SwitchActiveProfile)
					{
						KickStarter.options.SwitchProfile (i, includeActive);
					}
				}
			}
			
			return 0f;
		}
		

		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			UpgradeSelf ();

			if (AdvGame.GetReferences ().settingsManager != null && !AdvGame.GetReferences ().settingsManager.useProfiles)
			{
				EditorGUILayout.HelpBox ("Save game profiles are not enabled - please set in Settings Manager to use this Action.", MessageType.Warning);
				return;
			}

			manageProfileType = (ManageProfileType) EditorGUILayout.EnumPopup ("Method:", manageProfileType);

			if (manageProfileType == ManageProfileType.CreateProfile)
			{
				useCustomLabel = EditorGUILayout.Toggle ("Use custom label?", useCustomLabel);
			}

			if ((manageProfileType == ManageProfileType.CreateProfile && useCustomLabel) || manageProfileType == AC.ManageProfileType.RenameProfile)
			{
				customLabel = EditorGUILayout.TextField ("Custom label:", customLabel);
				preProcessTokens = EditorGUILayout.Toggle ("Pre-process tokens?", preProcessTokens);
			}

			if (manageProfileType == ManageProfileType.DeleteProfile ||
			    manageProfileType == ManageProfileType.RenameProfile ||
			    manageProfileType == ManageProfileType.SwitchActiveProfile)
			{
				string _action = "delete";
				if (manageProfileType == ManageProfileType.RenameProfile)
				{
					_action = "rename";
				}
				else if (manageProfileType == ManageProfileType.SwitchActiveProfile)
				{
					_action = "switch to";
				}

				deleteProfileType = (DeleteProfileType) EditorGUILayout.EnumPopup ("Profile to " + _action + ":", deleteProfileType);
				if (deleteProfileType == DeleteProfileType.SetSlotIndex)
				{
					profileIndexParameterID = Action.ChooseParameterGUI ("Slot index to " + _action + ":", parameters, profileIndexParameterID, ParameterType.Integer);
					if (profileIndexParameterID == -1)
					{
						profileIndex = EditorGUILayout.IntField ("Slot index to " + _action + ":", profileIndex);
					}
				}
				else if (deleteProfileType == DeleteProfileType.SlotIndexFromVariable)
				{
					slotVarID = AdvGame.GlobalVariableGUI ("Integer variable:", slotVarID, VariableType.Integer);
				}
				else if (deleteProfileType == DeleteProfileType.SetProfileID)
				{
					profileIndexParameterID = Action.ChooseParameterGUI ("Profile ID to " + _action + ":", parameters, profileIndexParameterID, ParameterType.Integer);
					if (profileIndexParameterID == -1)
					{
						profileIndex = EditorGUILayout.IntField ("Profile ID to " + _action + ":", profileIndex);
					}
				}
				else if (deleteProfileType == DeleteProfileType.ActiveProfile)
				{
					if (manageProfileType == ManageProfileType.SwitchActiveProfile)
					{
						EditorGUILayout.HelpBox ("This combination of fields will no effect - please choose another.", MessageType.Info);
					}
				}

				if (deleteProfileType == DeleteProfileType.SetSlotIndex || 
						 deleteProfileType == DeleteProfileType.SlotIndexFromVariable)
				{
					EditorGUILayout.Space ();
					menuName = EditorGUILayout.TextField ("Menu with ProfilesList:", menuName);
					elementName = EditorGUILayout.TextField ("ProfilesList element:", elementName);
				}
			}
		}


		public override string SetLabel ()
		{
			return manageProfileType.ToString ();
		}


		public override int GetNumVariableReferences (VariableLocation location, int _varID, List<ActionParameter> parameters, Variables variables = null, int variablesConstantID = 0)
		{
			UpgradeSelf ();
			if ((manageProfileType == ManageProfileType.CreateProfile && useCustomLabel) || manageProfileType == AC.ManageProfileType.RenameProfile)
			{
				string tokenText = AdvGame.GetVariableTokenText (location, _varID, variablesConstantID);
				if (customLabel.ToLower ().Contains (tokenText))
				{
					return 1 + base.GetNumVariableReferences (location, _varID, parameters, variables, variablesConstantID);
				}
			}
			return base.GetNumVariableReferences (location, _varID, parameters, variables, variablesConstantID);
		}


		public override int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables variables = null, int variablesConstantID = 0)
		{
			UpgradeSelf ();
			if ((manageProfileType == ManageProfileType.CreateProfile && useCustomLabel) || manageProfileType == ManageProfileType.RenameProfile)
			{
				string oldTokenText = AdvGame.GetVariableTokenText (location, oldVarID, variablesConstantID);
				if (customLabel.ToLower ().Contains (oldTokenText))
				{
					string newTokenText = AdvGame.GetVariableTokenText (location, newVarID, variablesConstantID);
					customLabel = customLabel.Replace (oldTokenText, newTokenText);
					return 1 + base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, variables, variablesConstantID);
				}
			}
			return base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, variables, variablesConstantID);
		}

		#endif


		private void UpgradeSelf ()
		{
			if (string.IsNullOrEmpty (customLabel) && varID >= 0)
			{
				customLabel = "[var:" + varID.ToString () + "]";
				varID = -1;
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Manage profile' Action, set to create a new profile</summary>
		 * <param name = "labelGlobalStringVariableID">If non-negative, the ID number of a Global String variable whose value will be used as the new profile's label</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionManageProfiles CreateNew_CreateProfile (int labelGlobalStringVariableID = -1)
		{
			ActionManageProfiles newAction = CreateNew<ActionManageProfiles> ();
			newAction.manageProfileType = ManageProfileType.CreateProfile;
			newAction.useCustomLabel = (labelGlobalStringVariableID >= 0);
			newAction.customLabel = "[var:" + labelGlobalStringVariableID + "]";
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Manage profile' Action, set to delete a profile</summary>
		 * <param name = "deleteProfileType">The type of profile to delete</param>
		 * <param name = "menuName">The name of the menu containing the ProfilesList element</param>
		 * <param name = "elementName">The name of the ProfilesList element</param>
		 * <param name = "indexOrID">The index or variable ID number referenced by the deleteProfileType</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionManageProfiles CreateNew_DeleteProfile (DeleteProfileType deleteProfileType, string menuName, string elementName, int indexOrID)
		{
			ActionManageProfiles newAction = CreateNew<ActionManageProfiles> ();
			newAction.manageProfileType = ManageProfileType.DeleteProfile;
			newAction.deleteProfileType = deleteProfileType;
			newAction.profileIndex = indexOrID;
			newAction.slotVarID = indexOrID;
			newAction.menuName = menuName;
			newAction.elementName = elementName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Manage profile' Action, set to rename a profile</summary>
		 * <param name = "deleteProfileType">The type of profile to rename</param>
		 * <param name = "menuName">The name of the menu containing the ProfilesList element</param>
		 * <param name = "elementName">The name of the ProfilesList element</param>
		 * <param name = "indexOrID">The index or variable ID number referenced by the renameProfileType</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionManageProfiles CreateNew_RenameProfile (int labelGlobalStringVariableID, DeleteProfileType renameProfileType, string menuName, string elementName, int indexOrID)
		{
			ActionManageProfiles newAction = CreateNew<ActionManageProfiles> ();
			newAction.manageProfileType = ManageProfileType.RenameProfile;
			newAction.deleteProfileType = renameProfileType;
			newAction.varID = labelGlobalStringVariableID;
			newAction.profileIndex = indexOrID;
			newAction.slotVarID = indexOrID;
			newAction.menuName = menuName;
			newAction.elementName = elementName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Manage profile' Action, set to select a profile</summary>
		 * <param name = "deleteProfileType">The type of profile to select</param>
		 * <param name = "menuName">The name of the menu containing the ProfilesList element</param>
		 * <param name = "elementName">The name of the ProfilesList element</param>
		 * <param name = "indexOrID">The index or variable ID number referenced by the selectProfileType</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionManageProfiles CreateNew_SwitchActiveProfile (DeleteProfileType selectProfileType, string menuName, string elementName, int indexOrID)
		{
			ActionManageProfiles newAction = CreateNew<ActionManageProfiles> ();
			newAction.manageProfileType = ManageProfileType.SwitchActiveProfile;
			newAction.deleteProfileType = selectProfileType;
			newAction.profileIndex = indexOrID;
			newAction.slotVarID = indexOrID;
			newAction.menuName = menuName;
			newAction.elementName = elementName;
			return newAction;
		}
		
	}
	
}