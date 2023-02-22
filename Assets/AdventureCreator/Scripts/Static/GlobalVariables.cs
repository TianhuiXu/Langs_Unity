/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"GlobalVariables.cs"
 * 
 *	This script contains static functions to access Global Variables at runtime.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * A class that can manipulate and retrieve the game's Global Variables at runtime.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_global_variables.html")]
	public class GlobalVariables : MonoBehaviour
	{

		/**
		 * <summary>Returns a list of all global variables.</summary>
		 * <returns>A List of GVar variables</returns>
		 */
		public static List<GVar> GetAllVars ()
		{
			if (KickStarter.runtimeVariables)
			{
				return KickStarter.runtimeVariables.globalVars;
			}
			return null;
		}


		/**
		 * Backs up the values of all global variables.
		 * Necessary when skipping ActionLists that involve checking variable values.
		 */
		public static void BackupAll ()
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar _var in KickStarter.runtimeVariables.globalVars)
				{
					_var.BackupValue ();
				}
			}
		}
		
		
		/**
		 * Uploads the values all linked variables to their linked counterparts.
		 */
		public static void UploadAll ()
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar var in KickStarter.runtimeVariables.globalVars)
				{
					var.Upload (VariableLocation.Global);
				}
			}
		}
		
		
		/**
		 * Downloads the values of all linked variables from their linked counterparts.
		 */
		public static void DownloadAll ()
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar var in KickStarter.runtimeVariables.globalVars)
				{
					var.Download (VariableLocation.Global);
				}
			}
		}
		

		/**
		 * <summary>Returns a global variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <returns>The global variable, or null if it was not found</returns>
		 */
		public static GVar GetVariable (int _id, bool synchronise = false)
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar _var in KickStarter.runtimeVariables.globalVars)
				{
					if (_var.id == _id)
					{
						if (synchronise)
						{
							_var.Download (VariableLocation.Global);
						}
						return _var;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Returns a global variable.</summary>
		 * <param name = "_name">The name of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <returns>The global variable, or null if it was not found</returns>
		 */
		public static GVar GetVariable (string _name, bool synchronise = false)
		{
			if (KickStarter.runtimeVariables)
			{
				foreach (GVar _var in KickStarter.runtimeVariables.globalVars)
				{
					if (_var.label == _name)
					{
						if (synchronise)
						{
							_var.Download (VariableLocation.Global);
						}
						return _var;
					}
				}
			}
			return null;
		}
		
		
		/**
		 * <summary>Returns the value of a global Integer variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <returns>The integer value of the variable</returns>
		 */
		public static int GetIntegerValue (int _id, bool synchronise = true)
		{
			GVar var = GetVariable (_id, synchronise);
			if (var != null)
			{
				return var.IntegerValue;
			}

			ACDebug.LogWarning ("Variable with ID=" + _id + " not found!");
			return 0;
		}
		
		
		/**
		 * <summary>Returns the value of a global Boolean variable.<summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <returns>The bool value of the variable</returns>
		 */
		public static bool GetBooleanValue (int _id, bool synchronise = true)
		{
			GVar var = GetVariable (_id, synchronise);
			if (var != null)
			{
				return var.BooleanValue;
			}

			ACDebug.LogWarning ("Variable with ID=" + _id + " not found!");
			return false;
		}
		
		
		/**
		 * <summary>Returns the value of a global String variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <param name = "languageNumber">The index number of the game's current language</param>
		 * <returns>The string value of the variable</returns>
		 */
		public static string GetStringValue (int _id, bool synchronise = true, int languageNumber = 0)
		{
			GVar var = GetVariable (_id, synchronise);
			if (var != null)
			{
				return var.GetValue (languageNumber);
			}
			
			ACDebug.LogWarning ("Variable with ID=" + _id + " not found!");
			return string.Empty;
		}
		

		/**
		 * <summary>Returns the value of a global Float variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <returns>The float value of the variable</returns>
		 */
		public static float GetFloatValue (int _id, bool synchronise = true)
		{
			GVar var = GetVariable (_id, synchronise);
			if (var != null)
			{
				return var.FloatValue;
			}
			
			ACDebug.LogWarning ("Variable with ID=" + _id + " not found!");
			return 0f;
		}


		/**
		 * <summary>Returns the value of a global Vector3 variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <returns>The Vector3 value of the variable</returns>
		 */
		public static Vector3 GetVector3Value (int _id, bool synchronise = true)
		{
			GVar var = GetVariable (_id, synchronise);
			if (var != null)
			{
				return var.Vector3Value;
			}
			
			ACDebug.LogWarning ("Variable with ID=" + _id + " not found!");
			return Vector3.zero;
		}
		

		/**
		 * <summary>Returns the value of a global Popup variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 * <param name = "languageNumber">The index number of the game's current language</param>
		 * <returns>The string value of the variable</returns>
		 */
		public static string GetPopupValue (int _id, bool synchronise = true, int languageNumber = 0)
		{
			GVar var = GetVariable (_id, synchronise);
			if (var != null)
			{
				return var.GetPopUpForIndex (var.IntegerValue, languageNumber);
			}
			
			ACDebug.LogWarning ("Variable with ID=" + _id + " not found!");
			return string.Empty;
		}


		/**
		 * <summary>Sets the value of a global Integer variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new integer value of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 */
		public static void SetIntegerValue (int _id, int _value, bool synchronise = true)
		{
			GVar var = GetVariable (_id);
			if (var != null)
			{
				var.IntegerValue = _value;

				if (synchronise)
				{
					var.Upload (VariableLocation.Global);
				}
			}
		}

		
		/**
		 * <summary>Sets the value of a global Boolean variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new bool value of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 */
		public static void SetBooleanValue (int _id, bool _value, bool synchronise = true)
		{
			GVar var = GetVariable (_id);
			if (var != null)
			{
				var.BooleanValue = _value;

				if (synchronise)
				{
					var.Upload (VariableLocation.Global);
				}
			}
		}
		
		
		/**
		 * <summary>Sets the value of a global String variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new string value of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 */
		public static void SetStringValue (int _id, string _value, bool synchronise = true)
		{
			GVar var = GetVariable (_id);
			if (var != null)
			{
				var.SetStringValue (_value);
				
				if (synchronise)
				{
					var.Upload (VariableLocation.Global);
				}
			}
		}
		
		
		/**
		 * <summary>Sets the value of a global Float variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new float value of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 */
		public static void SetFloatValue (int _id, float _value, bool synchronise = true)
		{
			GVar var = GetVariable (_id);
			if (var != null)
			{
				var.FloatValue = _value;

				if (synchronise)
				{
					var.Upload (VariableLocation.Global);
				}
			}
		}


		/**
		 * <summary>Sets the value of a global Vector3 variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new float value of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 */
		public static void SetVector3Value (int _id, Vector3 _value, bool synchronise = true)
		{
			GVar var = GetVariable (_id);
			if (var != null)
			{
				var.Vector3Value = _value;

				if (synchronise)
				{
					var.Upload (VariableLocation.Global);
				}
			}
		}


		/**
		 * <summary>Sets the value of a global PopUp variable.</summary>
		 * <param name = "_id">The ID number of the variable</param>
		 * <param name = "_value">The new index value of the variable</param>
		 * <param name = "synchronise">If True, then the variable's value will be synchronised with any external link it may have.</param>
		 */
		public static void SetPopupValue (int _id, int _value, bool synchronise = true)
		{
			SetIntegerValue (_id, _value, synchronise);
		}

	}

}