/*	Adventure Creator
*	by Chris Burton, 2013-2022
*	
*	"VarPreset.cs"
* 
*	This class is a data container for pre-set variable values.
* 
*/
		
using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/** A data container of preset variable values. */
	[System.Serializable]
	public class VarPreset
	{

		#region Variables

		/** Its display name */
		public string label;
		/** A unique identifier */
		public int ID;
		/** A List of PresetValues that match either all global or local variables (see GVar) by ID number */
		public List<PresetValue> presetValues = new List<PresetValue>();

		#endregion


		#region PublicFunctions

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_vars">A List of variables to create presets for</param>
		 * <param name = "idArray">An array of previously-used ID numbers, so that a unique one can be assigned</param>
		 */
		public VarPreset (List<GVar> _vars, int[] idArray)
		{
			presetValues = new List<PresetValue>();
			presetValues.Clear ();

			foreach (GVar _var in _vars)
			{
				presetValues.Add (new PresetValue (_var));
			}

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (ID == _id)
				{
					ID ++;
				}
			}

			label = "New preset";
		}


		/**
		 * <summary>Ensure the List of PresetValues contains a preset for each supplied variable.</summary>
		 * <param name = "_vars">The List of variables to create presets for</param>
		 */
		public void UpdateCollection (List<GVar> _vars)
		{
			foreach (GVar _var in _vars)
			{
				bool foundMatch = false;

				foreach (PresetValue presetValue in presetValues)
				{
					if (presetValue.id == _var.id)
					{
						foundMatch = true;
						break;
					}
				}

				if (!foundMatch)
				{
					presetValues.Add (new PresetValue (_var));
				}
			}

			for (int i=0; i<presetValues.Count; i++)
			{
				bool foundMatch = false;
				
				foreach (GVar _var in _vars)
				{
					if (presetValues[i].id == _var.id)
					{
						foundMatch = true;
						break;
					}
				}

				if (!foundMatch)
				{
					presetValues.RemoveAt (i);
				}
			}
		}


		/**
		 * <summary>Ensure the List of PresetValues contains a preset for a supplied variable.</summary>
		 * <param name = "_vars">The variable to create a preset for</param>
		 */
		public void UpdateCollection (GVar _var)
		{
			bool foundMatch = false;
			
			foreach (PresetValue presetValue in presetValues)
			{
				if (presetValue.id == _var.id)
				{
					foundMatch = true;
					break;
				}
			}
			
			if (!foundMatch)
			{
				presetValues.Add (new PresetValue (_var));
			}
		}


		/**
		 * <summary>Gets the PresetValue for a specific variable.</summary>
		 * <param name = "_var">The variable to get the PresetValue of</param>
		 */
		public PresetValue GetPresetValue (GVar _var)
		{
			foreach (PresetValue presetValue in presetValues)
			{
				if (presetValue.id == _var.id)
				{
					return presetValue;
				}
			}

			PresetValue newPresetValue = new PresetValue (_var);
			presetValues.Add (newPresetValue);
			return newPresetValue;
		}


		/**
		 * <summary>Gets the PresetValue for a specific variable.</summary>
		 * <param name = "variableID">The ID number of the variable to get the PresetValue of</param>
		 */
		public PresetValue GetPresetValue (int variableID)
		{
			foreach (PresetValue presetValue in presetValues)
			{
				if (presetValue.id == variableID)
				{
					return presetValue;
				}
			}

			return null;
		}

		#endregion

	}


	/**
	 * A data container for a single variable's preset value.
	 */
	[System.Serializable]
	public class PresetValue
	{

		/** The associated variable's ID number */
		public int id;
		/** Its value, if an integer, popup or boolean. If a boolean, 0 = False, and 1 = True. */
		public int val;
		/** Its value, if a float. */
		public float floatVal;
		/** Its value, if a string */
		public string textVal;
		/** Its value, if a Vector3 */
		public Vector3 vector3Val;
		/** Its value, if a GameObject */
		public GameObject gameObjectVal;
		/** Its value if a Unity Object */
		public Object objectVal;


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_gVar">The variable that this is a preset for</param>
		 */
		public PresetValue (GVar _gVar)
		{
			id = _gVar.id;
			val = _gVar.IntegerValue;
			floatVal = _gVar.FloatValue;
			textVal = _gVar.TextValue;
			vector3Val = _gVar.Vector3Value;
			gameObjectVal = _gVar.GameObjectValue;
			objectVal = _gVar.UnityObjectValue;
		}

	}

}