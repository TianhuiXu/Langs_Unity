/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ITranslatable.cs"
 * 
 *	An interface for any component that has translatable strings.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * An interface for any component that has translatable strings.
	 */
	public interface ITranslatable
	{

		/**
		 * <summary>Gets the text to be translated, given its index.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>The text to be translated</returns>
		 */
		string GetTranslatableString (int index);

		/**
		 * <summary>Gets the translation ID of a given text index.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>The translation ID of the text</returns>
		 */
		int GetTranslationID (int index);


		#if UNITY_EDITOR

		void UpdateTranslatableString (int index, string updatedText);

		/**
		 * <summary>Gets the maximum number of possible translatable texts.</summary>
		 * <returns>The maximum number of possible translatable texts.</returns>
		 */
		int GetNumTranslatables ();

		/**
		 * <summary>Checks if a given text index can and should be translated.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>True if the text can and should be translated</returns>
		 */
		bool CanTranslate (int index);

		/**
		 * <summary>Checks if a given text index has already been assigned a unique translation ID.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>True if the text has been assigned a unique translation ID</returns>
		 */
		bool HasExistingTranslation (int index);

		/**
		 * <summary>Sets the translation ID of a given text index</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <param name = "lineID">The new translation ID to assign the translatable text</param>
		 */
		void SetTranslationID (int index, int lineID);

		/**
		 * <summary>Gets the name of the translatable text's owner. In the case of speech text, it is the name of the character.  In the case of menu element text, it is the name of the menu element.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>The name of the translatable text's owner.</summary>
		 */
		string GetOwner (int index);

		/**
		 * <summary>Checks if the translatable text's owner is a Player. This is necessary for speech lines, since multiple player prefabs can feasibly share the same line.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>True if the translatable text's owner is a Player.</returns>
		 */
		bool OwnerIsPlayer (int index);

		/**
		 * <summary>Gets the translation type of a given text index.</summary>
		 * <param name = "index">The index of the translatable text</param>
		 * <returns>The translation type of a given text index.</returns>
		 */
		AC_TextType GetTranslationType (int index);

		#endif

	}

}