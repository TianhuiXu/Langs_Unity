/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RuntimeVariables.cs"
 * 
 *	This script creates a local copy of the VariableManager's Global vars.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Stores the game's global variables at runtime, as well as the speech log.
	 * This component should be attached to the PersistentEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_runtime_variables.html")]
	public class RuntimeVariables : MonoBehaviour
	{

		#region Variables

		/** The List of the game's global variables. */
		public List<GVar> globalVars = new List<GVar>();

		protected List<CustomToken> customTokens = new List<CustomToken>();
		protected List<SpeechLog> speechLines = new List<SpeechLog>();
		protected string[] textEventTokenKeys = new string[0];

		#endregion


		#region PublicFunctions

		/** Links all variables to their linked counterpart. */
		public void OnInitPersistentEngine ()
		{
			AssignOptionsLinkedVariables ();
			LinkAllValues ();
		}


		/** Downloads variables from the Global Manager to the scene. */
		public void TransferFromManager ()
		{
			if (AdvGame.GetReferences() && AdvGame.GetReferences().variablesManager)
			{
				VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;

				globalVars.Clear();
				foreach (GVar assetVar in variablesManager.vars)
				{
					GVar newVar = new GVar (assetVar);
					newVar.CreateRuntimeTranslations();
					globalVars.Add(newVar);
				}
			}
		}



		/**
		 * <summary>Gets the game's speech log.</summary>
		 * <returns>An array of SpeechLog variables</returns>
		 */
		public SpeechLog[] GetSpeechLog ()
		{
			return speechLines.ToArray ();
		}


		/**
		 * Clears the game's speech log.
		 */
		public void ClearSpeechLog ()
		{
			speechLines.Clear ();
		}


		/**
		 * <summary>Adds a speech line to the game's speech log.</summary>
		 * <param name = "_line">The SpeechLog variable to add</param>
		 */
		public void AddToSpeechLog (SpeechLog _line)
		{
			int ID = _line.lineID;
			if (ID >= 0)
			{
				foreach (SpeechLog speechLine in speechLines)
				{
					if (speechLine.lineID == ID)
					{
						speechLines.Remove (speechLine);
						break;
					}
				}
			}

			speechLines.Add (_line);
		}


		/**
		 * <summary>Creates a new custom token, or replaces it if one with the same ID number already exists.</summary>
		 * <param name = "_ID">The token's unique identifier</param>
		 * <param name = "_replacementText">The token's replacement text.</param>
		 */
		public void SetCustomToken (int _ID, string _replacementText)
		{
			CustomToken newToken = new CustomToken (_ID, _replacementText);

			for (int i=0; i<customTokens.Count; i++)
			{
				if (customTokens[i].ID == _ID)
				{
					customTokens.RemoveAt (i);
					break;
				}
			}

			customTokens.Add (newToken);
		}


		/**
		 * <summary>Removes all custom tokens from the game.</summary>
		 */
		public void ClearCustomTokens ()
		{
			customTokens.Clear ();
		}


		/**
		 * <summary>Replaces the supplied string with the replacementText of any defined CustomToken classes.</summary>
		 * <param name = "_text">The supplied string that may contain tokens of the form '[token:ID]'</param>
		 * <returns>The supplied string with any tokens replaced with the appropriate CustomToken's replacementText</returns>
		 */
		public string ConvertCustomTokens (string _text)
		{
			if (_text.Contains ("[token:"))
			{
				foreach (CustomToken customToken in customTokens)
				{
					string tokenText = "[token:" + customToken.ID + "]";
					if (_text.Contains (tokenText))
					{
						_text = _text.Replace (tokenText, customToken.replacementText);
					}
				}
			}
			return _text;
		}


		/**
		 * <summary>Re-assigns the CustomToken variables from a saved string.</summary>
		 * <param name = "savedString">The string that contains the CustomToken variables data</param>
		 */
		public void AssignCustomTokensFromString (string tokenData)
		{
			if (!string.IsNullOrEmpty (tokenData))
			{
				customTokens.Clear ();
				string[] countArray = tokenData.Split (SaveSystem.pipe[0]);
				
				foreach (string chunk in countArray)
				{
					string[] chunkData = chunk.Split (SaveSystem.colon[0]);
					
					int _id = 0;
					int.TryParse (chunkData[0], out _id);

					string _replacementText = chunkData[1];

					customTokens.Add (new CustomToken (_id, AdvGame.PrepareStringForLoading (_replacementText)));
				}
			}
		}


		/**
		 * <summary>Transfers the values of all option-linked global variables from the options data into the variables.
		 */
		public void AssignOptionsLinkedVariables ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
			{
				if (Options.optionsData != null && !string.IsNullOrEmpty (Options.optionsData.linkedVariables))
				{
					SaveSystem.AssignVariables (Options.optionsData.linkedVariables, true);
				}
			}
		}


		/**
		 * <summary>Updates a MainData class with its own variables that need saving.</summary>
		 * <param name = "mainData">The original MainData class</param>
		 * <returns>The updated MainData class</returns>
		 */
		public MainData SaveMainData (MainData mainData)
		{
			GlobalVariables.DownloadAll ();
			mainData.runtimeVariablesData = SaveSystem.CreateVariablesData (GlobalVariables.GetAllVars (), false, VariableLocation.Global);
			mainData.customTokenData = GetCustomTokensAsString ();
			return mainData;
		}


		/**
		 * <summary>Assigns all Global Variables to preset values.</summary>
		 * <param name = "varPreset">The VarPreset that contains the preset values</param>
		 * <param name = "ignoreOptionLinked">If True, then variables linked to Options Data will not be affected</param>
		 */
		public void AssignFromPreset (VarPreset varPreset, bool ignoreOptionLinked = false)
		{
			foreach (GVar globalVar in globalVars)
			{
				foreach (PresetValue presetValue in varPreset.presetValues)
				{
					if (globalVar.id == presetValue.id)
					{
						if (!ignoreOptionLinked || globalVar.link != VarLink.OptionsData)
						{
							globalVar.AssignPreset (presetValue);
							globalVar.Upload (VariableLocation.Global);
						}
					}
				}
			}
		}


		/**
		 * <summary>Assigns all Glocal Variables to preset values.</summary>
		 * <param name = "varPresetID">The ID number of the VarPreset that contains the preset values</param>
		 * <param name = "ignoreOptionLinked">If True, then variables linked to Options Data will not be affected</param>
		 */
		public void AssignFromPreset (int varPresetID, bool ignoreOptionLinked = false)
		{
			if (KickStarter.variablesManager.varPresets == null)
			{
				return;
			}

			foreach (VarPreset varPreset in KickStarter.variablesManager.varPresets)
			{
				if (varPreset.ID == varPresetID)
				{
					AssignFromPreset (varPreset, ignoreOptionLinked);
					return;
				}
			}
		}


		/**
		 * <summary>Gets a Global Variable preset with a specific ID number.</summary>
		 * <param name = "varPresetID">The ID number of the VarPreset</param>
		 * <returns>The Global Variable preset</returns>
		 */
		public VarPreset GetPreset (int varPresetID)
		{
			if (KickStarter.variablesManager.varPresets == null)
			{
				return null;
			}
			
			foreach (VarPreset varPreset in KickStarter.variablesManager.varPresets)
			{
				if (varPreset.ID == varPresetID)
				{
					return varPreset;
				}
			}
			
			return null;
		}

		#endregion


		#region ProtectedFunctions

		protected string GetCustomTokensAsString ()
		{
			if (customTokens != null)
			{
				System.Text.StringBuilder customTokenString = new System.Text.StringBuilder ();

				foreach (CustomToken customToken in customTokens)
				{
					customTokenString.Append (customToken.ID.ToString ());
					customTokenString.Append (SaveSystem.colon);
					customTokenString.Append (customToken.GetSafeReplacementText ());
					customTokenString.Append (SaveSystem.pipe);
				}
				
				if (customTokens.Count > 0)
				{
					customTokenString.Remove (customTokenString.Length-1, 1);
				}
				
				return customTokenString.ToString ();		
			}
			return string.Empty;
		}


		protected void LinkAllValues ()
		{
			foreach (GVar var in globalVars)
			{
				if (var.link == VarLink.PlaymakerVariable || var.link == VarLink.CustomScript)
				{
					if (var.updateLinkOnStart)
					{
						var.Download (VariableLocation.Global);
					}
					else
					{
						var.Upload (VariableLocation.Global);
					}
				}
			}
		}

		#endregion


		#region GetSet

		/**
		 * An array of string keys that can be inserted into text fields in the form [key:value].
		 * When processed, they will be removed from the text, but will trigger the OnRequestTextTokenReplacement event.
		 */
		public string[] TextEventTokenKeys
		{
			get
			{
				return textEventTokenKeys;
			}
			set
			{
				textEventTokenKeys = value;
			}
		}

		#endregion

	}

}