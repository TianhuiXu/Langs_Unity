
/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"InvVar.cs"
 * 
 *	This script is a data class for inventory properties.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * A data class for inventory properties.
	 * Properties are created in the Inventory Manager asset file, and stored in each instance of InvItem class.
	 * Inventory items held by the player during gameplay are stored in the localItems List within RuntimeInventory.
	 */
	[System.Serializable]
	public class InvVar : GVar
	{

		#region Variables

		/** If True, then the property will be limited to inventory items within certain categories */
		public bool limitToCategories = false;
		/** A List of category IDs that the property belongs to, if limitToCategories = True.  Categories are stored within InventoryManager's bins variable */
		public List<int> categoryIDs = new List<int>();

		#endregion


		#region Constructors

		/** The main Constructor. An array of ID numbers is required, to ensure its own ID is unique. */
		public InvVar (int[] idArray)
		{
			val = 0;
			floatVal = 0f;
			textVal = string.Empty;
			type = VariableType.Boolean;
			id = 0;
			popUps = null;
			textValLineID = -1;
			popUpsLineID = -1;
			vector3Val = Vector3.zero;
			popUpID = 0;
			gameObjectVal = null;
			objectVal = null;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (id == _id)
				{
					id ++;
				}
			}
			
			label = "Property " + (id + 1).ToString ();
		}


		/** A blank Constructor. */
		public InvVar (int _id, VariableType _type)
		{
			val = 0;
			floatVal = 0f;
			textVal = string.Empty;
			type = _type;
			id = _id;
			popUps = null;
			textValLineID = -1;
			popUpsLineID = -1;
			label = string.Empty;
			vector3Val = Vector3.zero;
			popUpID = 0;
			gameObjectVal = null;
			objectVal = null;
		}


		/** A Constructor that copies all values from another inventory property. This way ensures that no connection remains to the asset file. */
		public InvVar (InvVar assetVar)
		{
			val = assetVar.val;
			floatVal = assetVar.floatVal;
			textVal = assetVar.textVal;
			type = assetVar.type;
			id = assetVar.id;
			label = assetVar.label;
			link = assetVar.link;
			pmVar = assetVar.pmVar;
			popUps = assetVar.popUps;
			updateLinkOnStart = assetVar.updateLinkOnStart;
			categoryIDs = assetVar.categoryIDs;
			limitToCategories = assetVar.limitToCategories;
			textValLineID = assetVar.textValLineID;
			popUpsLineID = assetVar.popUpsLineID;
			vector3Val = assetVar.vector3Val;
			popUpID = assetVar.popUpID;
			gameObjectVal = assetVar.gameObjectVal;
			objectVal = assetVar.objectVal;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Updates the 'value' variables specific to an inventory item based on another InvVar instance.</summary>
		 * <param name = "invVar">The other InvVar to copy 'value' variables from</param>
		 * <param name = "addValues">If True, integer and float values will be added to the property, rather then replaced/param>
		 */
		public void TransferValues (InvVar invVar, bool addValues = false)
		{
			if (addValues)
			{
				val += invVar.val;
				floatVal += invVar.floatVal;
			}
			else
			{
				val = invVar.val;
				floatVal = invVar.floatVal;
			}

			textVal = invVar.textVal;
			textValLineID = invVar.textValLineID;
			vector3Val = invVar.vector3Val;
			popUpsLineID = invVar.popUpsLineID;
			popUpID = invVar.popUpID;
			gameObjectVal = invVar.gameObjectVal;
			objectVal = invVar.objectVal;
		}


		/**
		 * <summary>Gets the property's value as a string.</summary>
		 * <param name = "languageNumber">The index number of the game's current language (0 = original)</param>
		 * <param name = "itemCount">If the variable's type is Float or Integer, the value will by multipled by this</param>
		 * <returns>The property's value as a string</returns>
		 */
		public string GetDisplayValue (int languageNumber = 0, int itemCount = 1)
		{
			switch (type)
			{
				case VariableType.Integer:
					return (val * itemCount).ToString ();

				case VariableType.Float:
					return (floatVal * (float) itemCount).ToString ();

				case VariableType.Boolean:
					return (val == 1) ? "True" : "False";

				case VariableType.PopUp:
					if (runtimeTranslations == null || runtimeTranslations.Length == 0) CreateRuntimeTranslations ();
					return GetPopUpForIndex (val, languageNumber);

				case VariableType.String:
					return KickStarter.runtimeLanguages.GetTranslation (textVal, textValLineID, languageNumber, GetTranslationType (0));
					
				case VariableType.GameObject:
					if (gameObjectVal)
					{
						return gameObjectVal.name;
					}
					return string.Empty;

				case VariableType.UnityObject:
					if (objectVal)
					{
						return objectVal.name;
					}
					return string.Empty;

				case VariableType.Vector3:
					return "(" + vector3Val.x.ToString () + ", " + vector3Val.y.ToString () + ", " + vector3Val.z.ToString () + ")";
			}
			return string.Empty;
		}


		public void LoadData (string[] data)
		{
			switch (type)
			{
				case VariableType.Float:
					float _floatValue = -1f;
					if (float.TryParse (data[1], out _floatValue))
					{
						floatVal = _floatValue;
					}
					break;

				case VariableType.String:
					if (data.Length > 2)
					{
						int _textValueID;
						if (int.TryParse (data[2], out _textValueID))
						{
							string _textValue = data[1];
							_textValue = AdvGame.PrepareStringForLoading (_textValue);
							SetStringValue (_textValue, _textValueID);
						}
					}
					break;

				case VariableType.Vector3:
					string[] vectorArray = data[1].Split (","[0]);
					if (vectorArray.Length == 3)
					{
						float _xValue = -1f;
						if (float.TryParse (vectorArray[0], out _xValue))
						{
							float _yValue = -1f;
							if (float.TryParse (vectorArray[1], out _yValue))
							{
								float _zValue = -1f;
								if (float.TryParse (vectorArray[2], out _zValue))
								{
									vector3Val = new Vector3 (_xValue, _yValue, _zValue);
								}
							}
						}
					}
					break;

				default:
					int _intValue = -1;
					if (int.TryParse (data[1], out _intValue))
					{
						val = _intValue;
					}
					break;
			}
		}

		#endregion


		#region ITranslatable

		public override string GetTranslatableString (int index)
		{
			return GetPopUpsString ();
		}


		public override int GetTranslationID (int index)
		{
			return popUpsLineID;
		}


		public override AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.InventoryItemProperty;
		}


		#if UNITY_EDITOR

		public override bool HasExistingTranslation (int index)
		{
			return popUpsLineID > -1;
		}


		public override void SetTranslationID (int index, int _lineID)
		{
			popUpsLineID = _lineID;
		}


		public override bool CanTranslate (int index)
		{
			if (type == VariableType.PopUp && popUpID <= 0)
			{
				return !string.IsNullOrEmpty (GetPopUpsString ());
			}
			return false;
		}

		#endif

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI (string apiPrefix, bool allowSceneGameObjects = false)
		{
			string _label = label + ":";
			if (string.IsNullOrEmpty (label))
			{
				_label = "Property " + id.ToString () + ":";
			}

			switch (type)
			{
				case VariableType.Boolean:
					if (val != 1) val = 0;
					val = CustomGUILayout.Popup (_label, val, boolType, apiPrefix + ".BooleanValue", "The property's value for this item");
					break;

				case VariableType.Integer:
					val = CustomGUILayout.IntField (_label, val, apiPrefix + ".IntegerValue", "The property's value for this item");
					break;

				case VariableType.PopUp:
					val = CustomGUILayout.Popup (_label, val, GenerateEditorPopUpLabels (), apiPrefix + ".IntegerValue", "The property's value for this item");
					break;

				case VariableType.String:
					textVal = CustomGUILayout.TextArea (_label, textVal, apiPrefix + ".TextValue", "The property's value for this item");
					break;

				case VariableType.Float:
					floatVal = CustomGUILayout.FloatField (_label, floatVal, apiPrefix + ".FloatValue", "The property's value for this item");
					break;

				case VariableType.Vector3:
					vector3Val = CustomGUILayout.Vector3Field (_label, vector3Val, apiPrefix + ".Vector3Value", "The property's value for this item");
					break;

				case VariableType.GameObject:
					gameObjectVal = (GameObject) CustomGUILayout.ObjectField <GameObject> (_label, gameObjectVal, allowSceneGameObjects, apiPrefix + ".GameObjectValue", "The property's value for this item");
					break;

				case VariableType.UnityObject:
					objectVal = CustomGUILayout.ObjectField <Object> (_label, objectVal, false);
					break;
			}
		}

		#endif

	}

}