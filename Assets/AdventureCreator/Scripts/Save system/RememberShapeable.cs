/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberShapeable.cs"
 * 
 *	This script is attached to shapeable scripts in the scene
 *	with shapekey values we wish to save.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attach this to Shapeable objects with shapekey values you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Shapeable")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_shapeable.html")]
	public class RememberShapeable : Remember
	{

		public override string SaveData ()
		{
			ShapeableData shapeableData = new ShapeableData();
			shapeableData.objectID = constantID;
			shapeableData.savePrevented = savePrevented;

			Shapeable shapeable = GetComponent <Shapeable>();
			if (shapeable)
			{
				List<int> activeKeyIDs = new List<int>();
				List<float> values = new List<float>();
				
				foreach (ShapeGroup shapeGroup in shapeable.shapeGroups)
				{
					activeKeyIDs.Add (shapeGroup.GetActiveKeyID ());
					values.Add (shapeGroup.GetActiveKeyValue ());
				}

				shapeableData._activeKeyIDs = ArrayToString <int> (activeKeyIDs.ToArray ());
				shapeableData._values = ArrayToString <float> (values.ToArray ());
			}

			return Serializer.SaveScriptData <ShapeableData> (shapeableData);
		}
		

		public override void LoadData (string stringData)
		{
			ShapeableData data = Serializer.LoadScriptData <ShapeableData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			Shapeable shapeable = GetComponent <Shapeable>();
			if (shapeable)
			{
				int[] activeKeyIDs = StringToIntArray (data._activeKeyIDs);
				float[] values = StringToFloatArray (data._values);

				for (int i=0; i<activeKeyIDs.Length; i++)
				{
					if (values.Length > i)
					{
						shapeable.shapeGroups[i].SetActive (activeKeyIDs[i], values[i], 0f, MoveMethod.Linear, null);
					}
				}
			}
		}
	
	}


	/**
	 * A data container used by the RememberShapeable script.
	 */
	[System.Serializable]
	public class ShapeableData : RememberData
	{

		/** The active ID number of each shape group */
		public string _activeKeyIDs;
		/** The value of each active shape key in each shape group */
		public string _values;

		/**
		 * The default Constructor.
		 */
		public ShapeableData () { }

	}

}
