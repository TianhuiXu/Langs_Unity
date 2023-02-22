/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"DefaultSaveLabels.cs"
 * 
 *	A collection of save strings (Save, Import, Autosave) that can be translated
 * 
 */

using UnityEngine;

namespace AC
{

	/** A collection of save strings (Save, Import, Autosave) that can be translated */
	[System.Serializable]
	public class SaveLabels : ITranslatable
	{

		[SerializeField] private string defaultSaveLabel = "Save";
		[SerializeField] private int saveLabelID = -1;

		[SerializeField] private string defaultImportLabel = "Import";
		[SerializeField] private int importLabelID = -1;

		[SerializeField] private string defaultAutosaveLabel = "Autosave";
		[SerializeField] private int autosaveLabelID = -1;


		public string GetTranslatableString (int index)
		{
			switch (index)
			{
				case 0:
				default:
					return defaultSaveLabel;

				case 1:
					return defaultImportLabel;

				case 2:
					return defaultAutosaveLabel;
			}
		}

		
		public int GetTranslationID (int index)
		{
			switch (index)
			{
				case 0:
				default:
					return saveLabelID;

				case 1:
					return importLabelID;

				case 2:
					return autosaveLabelID;
			}
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			switch (index)
			{
				case 0:
				default:
					defaultSaveLabel = updatedText;
					break;

				case 1:
					defaultImportLabel = updatedText;
					break;

				case 2:
					defaultAutosaveLabel = updatedText;
					break;
			}
		}


		public int GetNumTranslatables ()
		{
			return 3;
		}


		public bool CanTranslate (int index)
		{
			switch (index)
			{
				case 0:
				default:
					return !string.IsNullOrEmpty (defaultSaveLabel);

				case 1:
					return !string.IsNullOrEmpty (defaultImportLabel);

				case 2:
					return !string.IsNullOrEmpty (defaultAutosaveLabel);
			}
		}


		public bool HasExistingTranslation (int index)
		{
			switch (index)
			{
				case 0:
				default:
					return (saveLabelID >= 0);

				case 1:
					return (importLabelID >= 0);

				case 2:
					return (autosaveLabelID >= 0);
			}
		}


		public void SetTranslationID (int index, int lineID)
		{
			switch (index)
			{
				case 0:
				default:
					saveLabelID = lineID;
					break;

				case 1:
					importLabelID = lineID;
					break;

				case 2:
					autosaveLabelID = lineID;
					break;
			}
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.MenuElement;
		}

		#endif

	}

}