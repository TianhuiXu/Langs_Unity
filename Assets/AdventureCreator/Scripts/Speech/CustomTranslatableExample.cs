/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CustomTranslatableExample.cs"
 * 
 *	An example script demonstrating how custom translatables can be implemented using the ITranslatable interface.  Placing this on a GameObject in a scene will cause it to be picked up by the Speech Manager's "Gather text" operation and listed as translatable text.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/** An example script demonstrating how custom translatables can be implemented using the ITranslatable interface.  Placing this on a GameObject in a scene will cause it to be picked up by the Speech Manager's "Gather text" operation and listed as translatable text. */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_custom_translatable_example.html")]
	public class CustomTranslatableExample : MonoBehaviour, ITranslatable
	{

		/** This is the text we'll make available for translation */
		public string myCustomText;
		/** This is the ID number our translatable text will be assigned */
		public int myCustomLineID = -1;


		private void OnEnable ()
		{
			EventManager.OnChangeLanguage += OnChangeLanguage;
		}


		private void OnDisable ()
		{
			EventManager.OnChangeLanguage -= OnChangeLanguage;
		}


		private void OnChangeLanguage (int language)
		{
			// Update myCustomText whenever the game's language is set
			myCustomText = KickStarter.runtimeLanguages.GetTranslation (myCustomLineID);
		}


		public string GetTranslatableString (int index)
		{
			// Return the text to be translated
			return myCustomText;
		}

		
		public int GetTranslationID (int index)
		{
			// Return the integer variable used to store the translation ID
			return myCustomLineID;
		}


		#if UNITY_EDITOR

		/** Note: These functions are placed in UNITY_EDITOR as they only need accessing from within the Speech Manager, outside of runtime */

		public void UpdateTranslatableString (int index, string updatedText)
		{
			// Update the original text
			myCustomText = updatedText;
		}


		public int GetNumTranslatables ()
		{
			// Return 1 unless you want to store multiple translatable texts in a single script.
			return 1;
		}


		public bool CanTranslate (int index)
		{
			// Check if the text is OK to be translated (usually just IsNullOrEmpty on the string will do, but sometimes it'll depend on other options)
			return !string.IsNullOrEmpty (myCustomText);
		}


		public bool HasExistingTranslation (int index)
		{
			// Basically check if the ID number > -1, since this is what happens when a translation is recorded
			return (myCustomLineID >= 0);
		}


		public void SetTranslationID (int index, int lineID)
		{
			// Set the translation ID variable
			myCustomLineID = lineID;
		}


		public string GetOwner (int index)
		{
			// This is normally string.Empty, as it's mainly used for speech lines and menu elements
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			// This is normally false, as it's mainly used for speech lines
			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			// Return the type of translation, for sorting within the Speech Manager
			return AC_TextType.Custom;
		}

		#endif
	}

}