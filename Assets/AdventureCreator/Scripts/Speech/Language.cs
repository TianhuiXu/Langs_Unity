/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Language.cs"
 * 
 *	Stores data about a language defined in the Speech Manager.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	/** Stores data about a language defined in the Speech Manager.*/
	[System.Serializable]
	public class Language
	{

		#region Variables

		/** The language's name */
		public string name;
		/** Whether or not the language reads right-to-left (Arabic / Hebrew-style) */
		public bool isRightToLeft;
		/** An AssetBundle filename (in a StreamingAssets folder) that stores the language's speech audio files */
		public string audioAssetBundle;
		/** An AssetBundle filename (in a StreamingAssets folder) that stores the language's speech lipsync files */
		public string lipsyncAssetBundle;
		/** The index of the language to display if this language's text is empty */
		public int fallbackLanguageIndex = 0;
		/** If True, the language is not available at runtime */
		public bool isDisabled = false;

		#endregion


		#region Constructors

		public Language (string _name, bool _isRightToLeft = false, string _audioAssetBundle = "", string _lipsyncAssetBundle = "")
		{
			name = _name;
			isRightToLeft = _isRightToLeft;
			audioAssetBundle = _audioAssetBundle;
			lipsyncAssetBundle = _lipsyncAssetBundle;
			fallbackLanguageIndex = 0;
		}


		public Language (Language language)
		{
			name = language.name;
			isRightToLeft = language.isRightToLeft;
			audioAssetBundle = language.audioAssetBundle;
			lipsyncAssetBundle = language.lipsyncAssetBundle;
			fallbackLanguageIndex = language.fallbackLanguageIndex;
			isDisabled = language.isDisabled;
		}

		#endregion


		#region PublicFunctions

		#if UNITY_EDITOR

		public void ShowGUI (int i)
		{
			string apiPrefix = "AC.KickStarter.speechManager.Languages[" + i + "]";

			isRightToLeft = CustomGUILayout.Toggle ("Reads right-to-left?", isRightToLeft, apiPrefix + ".isRightToLeft", "Whether or not the language reads right-to-left (Arabic / Hebrew-style)");
			
			if (i > 0)
			{
				List<string> popupLabels = new List<string> ();
				for (int j = 0; j < KickStarter.speechManager.Languages.Count; j++)
				{
					if (j != i)
					{
						popupLabels.Add (KickStarter.speechManager.Languages[j].name);
					}
				}

				int temp = fallbackLanguageIndex;
				if (fallbackLanguageIndex >= i)
				{
					temp -= 1;
				}

				temp = CustomGUILayout.Popup ("Fallback language:", temp, popupLabels.ToArray (), apiPrefix + ".fallbackLanguageIndex", "The index of the language to display if this language's text is empty");
				
				if (temp >= i)
				{
					fallbackLanguageIndex = temp + 1;
				}
				else
				{
					fallbackLanguageIndex = temp;
				}
			}

			if (KickStarter.speechManager.referenceSpeechFiles == ReferenceSpeechFiles.ByAssetBundle && (i == 0 || KickStarter.speechManager.translateAudio))
			{
				audioAssetBundle = CustomGUILayout.TextField ("Audio AssetBundle name:", audioAssetBundle, apiPrefix + ".audioAssetBundle", "An AssetBundle filename (in a StreamingAssets folder) that stores the language's speech audio files");
				if (KickStarter.speechManager.UseFileBasedLipSyncing ())
				{
					lipsyncAssetBundle = CustomGUILayout.TextField ("Lipsync AssetBundle name:", lipsyncAssetBundle, apiPrefix + ".lipsyncAssetBundle", "An AssetBundle filename (in a StreamingAssets folder) that stores the language's speech lipsync files");
				}
			}

			if (KickStarter.speechManager.Languages.Count > 1)
			{
				isDisabled = CustomGUILayout.Toggle ("Is disabled?", isDisabled, apiPrefix + ".isDisabled", "If True, then the language will not be used in-game");
			}
			else
			{
				isDisabled = false;
			}
		}

		#endif

		#endregion

	}

}