/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SpeechLine.cs"
 * 
 *	This script is a data container for speech lines found by Speech Manager.
 *	Such data is used to provide translation support, as well as auto-numbering
 *	of speech lines for sound files.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if LocalizationIsPresent
using UnityEngine.Localization;
#endif

namespace AC
{

	/**
	 * A container class for text gathered by the Speech Manager.
	 * It is not limited to just speech, as all text displayed in a game will be gathered.
	 */
	[System.Serializable]
	public class SpeechLine
	{

		#region Variables
		
		/** True if this is a speech line spoken by the Player */
		public bool isPlayer;
		/** A unique ID number to identify the instance by */
		public int lineID;
		/** The name of the scene that the text was found in */
		public string scene;
		/** If not the player, who the text is owned by */
		public string owner;
		/** The display text itself */
		public string text;
		/** A user-generated description of the text */
		public string description;
		/** The type of text this is (Speech, Hotspot, DialogueOption, InventoryItem, CursorIcon, MenuElement, HotspotPrefix, JournalEntry) */
		public AC_TextType textType;
		/** An array of translations for the display text */
		public List<string> translationText = new List<string>();
		/** The AudioClip used for speech, if set manually */
		public AudioClip customAudioClip;
		/** The TextAsset used for lipsyncing, if set manually */
		public Object customLipsyncFile;
		/** An array of AudioClips used for translated speech, if set manually */
		public List<AudioClip> customTranslationAudioClips;
		/** An array of TextAssets used for translated lipsyncing, if set manually */
		public List<Object> customTranslationLipsyncFiles;
		/** The ID of the associated SpeechTag, if a speech line */
		public int tagID;
		/** If True, and the textType = AC_TextType.Speech, then the speech line can only be played once. Additional calls to play it will be ignored */
		public bool onlyPlaySpeechOnce = false;
		/** If True, then this class instance will not be removed by the Speech Manager's "Gather text" and "Reset text" processes. This flag should be used under caution, as this will not affect the state of any linked iTranslatable interface */
		public bool alwaysRetainInSpeechManager = false;
		/** If True, then this line will not be included in CSV exports, even if it matches the criteria set in the Export Wizard window */
		public bool ignoreDuringExport = false;

		#if LocalizationIsPresent
		#if UNITY_EDITOR
		SerializedProperty localizedStringProperty;
		SerializedObject serializedObject;
		#endif
		/** If True, then the line's text will be pulled from Unity's Localization tables */
		public bool useLocalizedString;
		/** The Localization string to use, if useLocalizedString is True */
		public LocalizedString localizedString;
		#endif

		public string customFilename = "";

		#if UNITY_EDITOR
		public int orderID = -1;
		public string orderPrefix = "";
		#endif

		protected bool gotCommentFromDescription;
		protected static string[] badChars = new string[] {"/", "`", "'", "!", "@", "£", "$", "%", "^", "&", "*", "(", ")", "{", "}", ":", ";", ".", "|", "<", ",", ">", "?", "#", "-", "=", "+", "-"};

		private static string speechMarkAsString;
		private const float labelWidth = 85f;

		#endregion


		#region Constructors

		/**
		 * A Constructor that copies all values from another SpeechLine.
		 * This way ensures that no connection remains to the original class.
		 */
		public SpeechLine (SpeechLine _speechLine)
		{
			isPlayer = _speechLine.isPlayer;
			lineID = _speechLine.lineID;
			scene = _speechLine.scene;
			owner = _speechLine.owner;
			text = _speechLine.text;
			description = _speechLine.description;
			textType = _speechLine.textType;
			translationText = _speechLine.translationText;
			customAudioClip = _speechLine.customAudioClip;
			customLipsyncFile = _speechLine.customLipsyncFile;
			customTranslationAudioClips = _speechLine.customTranslationAudioClips;
			customTranslationLipsyncFiles = _speechLine.customTranslationLipsyncFiles;
			tagID = _speechLine.tagID;
			onlyPlaySpeechOnce = _speechLine.onlyPlaySpeechOnce;
			customFilename = _speechLine.customFilename;
			alwaysRetainInSpeechManager = _speechLine.alwaysRetainInSpeechManager;
			ignoreDuringExport = _speechLine.ignoreDuringExport;

			#if UNITY_EDITOR
			orderID = _speechLine.orderID;
			orderPrefix = _speechLine.orderPrefix;
			#endif

			#if LocalizationIsPresent
			useLocalizedString = _speechLine.useLocalizedString;
			localizedString = _speechLine.localizedString;
			#endif
		}


		public SpeechLine (SpeechLine _speechLine, int voiceLanguage)
		{
			isPlayer = _speechLine.isPlayer;
			lineID = _speechLine.lineID;
			scene = _speechLine.scene;
			owner = _speechLine.owner;
			text = _speechLine.text;
			description = _speechLine.description;
			textType = _speechLine.textType;
			translationText = _speechLine.translationText;
			customAudioClip = (voiceLanguage == 0 || KickStarter.speechManager.fallbackAudio) ? _speechLine.customAudioClip : null;
			customLipsyncFile = (voiceLanguage == 0 || KickStarter.speechManager.fallbackAudio) ? _speechLine.customLipsyncFile : null;
			customTranslationAudioClips = GetAudioListForTranslation (_speechLine.customTranslationAudioClips, voiceLanguage);
			customTranslationLipsyncFiles = GetLipsyncListForTranslation (_speechLine.customTranslationLipsyncFiles, voiceLanguage);
			tagID = _speechLine.tagID;
			onlyPlaySpeechOnce = _speechLine.onlyPlaySpeechOnce;
			customFilename = _speechLine.customFilename;
			alwaysRetainInSpeechManager = _speechLine.alwaysRetainInSpeechManager;
			ignoreDuringExport = _speechLine.ignoreDuringExport;

			#if UNITY_EDITOR
			orderID = _speechLine.orderID;
			orderPrefix = _speechLine.orderPrefix;
			#endif

			#if LocalizationIsPresent
			useLocalizedString = _speechLine.useLocalizedString;
			localizedString = _speechLine.localizedString;
			#endif
		}

		
		/**
		 * A constructor for speech text in which the ID number is explicitly defined.
		 */
		public SpeechLine (int _id, string _scene, string _owner, string _text, int _languagues, AC_TextType _textType, bool _isPlayer)
		{
			lineID = _id;
			scene = _scene;
			owner = _owner;
			text = _text;
			textType = _textType;
			description = string.Empty;
			isPlayer = _isPlayer;
			customAudioClip = null;
			customLipsyncFile = null;
			customTranslationAudioClips = new List<AudioClip>();
			customTranslationLipsyncFiles = new List<Object>();
			tagID = -1;
			onlyPlaySpeechOnce = false;
			customFilename = string.Empty;
			alwaysRetainInSpeechManager = false;
			ignoreDuringExport = false;
			
			translationText = new List<string>();
			for (int i=0; i<_languagues; i++)
			{
				translationText.Add (string.Empty);
			}

			#if UNITY_EDITOR
			orderID = -1;
			orderPrefix = string.Empty;
			#endif

			#if LocalizationIsPresent
			useLocalizedString = false;
			localizedString = null;
			#endif
		}

		#endregion


		#region PublicFunctions

		public void CopyValues (SpeechLine _speechLine)
		{
			isPlayer = _speechLine.isPlayer;
			lineID = _speechLine.lineID;
			scene = _speechLine.scene;
			owner = _speechLine.owner;
			text = _speechLine.text;
			description = _speechLine.description;
			textType = _speechLine.textType;
			translationText = _speechLine.translationText;
			customAudioClip = _speechLine.customAudioClip;
			customLipsyncFile = _speechLine.customLipsyncFile;
			customTranslationAudioClips = _speechLine.customTranslationAudioClips;
			customTranslationLipsyncFiles = _speechLine.customTranslationLipsyncFiles;
			tagID = _speechLine.tagID;
			onlyPlaySpeechOnce = _speechLine.onlyPlaySpeechOnce;
			customFilename = _speechLine.customFilename;
			alwaysRetainInSpeechManager = _speechLine.alwaysRetainInSpeechManager;
			ignoreDuringExport = _speechLine.ignoreDuringExport;

			#if UNITY_EDITOR
			orderID = _speechLine.orderID;
			orderPrefix = _speechLine.orderPrefix;
			#endif

			#if LocalizationIsPresent
			useLocalizedString = _speechLine.useLocalizedString;
			localizedString = _speechLine.localizedString;
			#endif
		}


		/**
		 * <summary>Checks if the class matches another, in terms of line ID, text, type and owner.
		 * Used to determine if a speech line is a duplicate of another.</summary>
		 * <param name = "newLine">The SpeechLine class to check against</param>
		 * <param name = "ignoreID">If True, then a difference in lineID number will not matter</param>
		 * <returns>True if the two classes have the same line ID, text, type and owner</returns>
		 */
		public bool IsMatch (SpeechLine newLine, bool ignoreID = false)
		{
			if (text == newLine.text && textType == newLine.textType && owner == newLine.owner)
			{
				if (lineID == newLine.lineID || ignoreID)
				{
					return true;
				}
			}
			return false;
		}


		public void TransferActionComment (string comment, string gameObjectName)
		{
			if (!string.IsNullOrEmpty (comment))
			{
				description = comment;
				gotCommentFromDescription = true;
			}
			else
			{
				if (!string.IsNullOrEmpty (gameObjectName) && string.IsNullOrEmpty (description))
				{
					description = "From: " + gameObjectName;
				}
				gotCommentFromDescription = false;
			}
		}


		public void RestoreBackup (SpeechLine backupLine)
		{
			translationText = backupLine.translationText;
			customAudioClip = backupLine.customAudioClip;
			customLipsyncFile = backupLine.customLipsyncFile;
			customTranslationAudioClips = backupLine.customTranslationAudioClips;
			customTranslationLipsyncFiles = backupLine.customTranslationLipsyncFiles;
			onlyPlaySpeechOnce = backupLine.onlyPlaySpeechOnce;
			customFilename = backupLine.customFilename;
			alwaysRetainInSpeechManager = backupLine.alwaysRetainInSpeechManager;
			ignoreDuringExport = backupLine.ignoreDuringExport;

			if (!gotCommentFromDescription && !string.IsNullOrEmpty (backupLine.description))
			{
				description = backupLine.description;
			}

			#if LocalizationIsPresent
			useLocalizedString = backupLine.useLocalizedString;
			localizedString = backupLine.localizedString;
			#endif
		}


		/**
		 * <summary>Gets the full folder and filename for a speech line's audio or lipsync file, relative to the "Resources" Assets directory in which it is placed.</summary>
		 * <param name = "language">The language of the audio</param>
		 * <param name = "forLipSync">True if this is for a lipsync file</param>
		 * <param name = "overrideName">If set, then this string (with special characters removed) will be used instead</param>
		 * <returns>A string of the folder name that the audio or lipsync file should be placed in</returns>
		 */
		public string GetAutoAssetPathAndName (string language, bool forLipsync = false, string overrideName = "")
		{
			return GetRelativePath (language, forLipsync, overrideName) + GetFilename (overrideName);
		}


		/**
		 * <summary>Checks if the line is shared by multiple player characters, each with their own audio and lipsync files.</summary>
		 * <returns>True if the line is shared by multiple player characters, each with their own audio and lipsync files.</returns>
		 */
		public bool SeparatePlayerAudio ()
		{
			if (isPlayer &&
				textType == AC_TextType.Speech &&
				owner == "Player" &&
				KickStarter.speechManager.usePlayerRealName &&
				KickStarter.speechManager.separateSharedPlayerAudio &&
				KickStarter.settingsManager != null &&
				KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
			{
				return true;
			}
			return false;
		}

		
		/**
		 * <summary>Gets the clean-formatted filename for a speech line's audio file.</summary>
		 * <param name = "overrideName">If set, then this string (with special characters removed) will be used instead</param>
		 * <returns>The filename</returns>
		 */
		public string GetFilename (string overrideName = "")
		{
			switch (KickStarter.speechManager.referenceSpeechFiles)
			{
				case ReferenceSpeechFiles.ByNamingConvention:
				case ReferenceSpeechFiles.ByAssetBundle:
					if (!string.IsNullOrEmpty (customFilename) && string.IsNullOrEmpty (overrideName))
					{
						return customFilename;
					}
					break;

				case ReferenceSpeechFiles.ByAddressable:
					if (Application.isPlaying && KickStarter.speechManager.translateAudio && Options.GetLanguage () > 0)
					{
						return GetSubfolderPath (overrideName) + lineID.ToString () + "_" + Options.GetLanguageName ();
					}
					break;

				default:
					break;
			}

			

			return GetSubfolderPath (overrideName) + lineID.ToString ();
		}


		/**
		 * <summary>Gets the translation for a given language index.</summary>
		 * <param name = "originalText">The original text, returned if the language index is invalid</param>
		 * <param name = "language">The language index</param>
		 * <returns>The translation text</returns>
		 */
		public string GetTranslation (string originalText, int language)
		{
			if (language <= 0)
			{
				return originalText;
			}
			if (translationText.Count > (language - 1))
			{
				return translationText[language - 1];
			}
			ACDebug.LogWarning ("A translation is being requested that does not exist!");
			return originalText;
		}

		#endregion


		#region UnityEditor

		#if UNITY_EDITOR

		/** Displays the GUI of the class's entry within the Speech Manager. */
		public void ShowGUI (float maxWidth)
		{
			CustomGUILayout.DrawUILine ();

			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			string apiPrefix = "KickStarter.speechManager.GetLine (" + lineID + ")";

			if (lineID == speechManager.activeLineID)
			{
				CustomGUILayout.BeginVertical ();

				EditorGUILayout.BeginHorizontal ();

				EditorGUILayout.LabelField ("ID #:", GUILayout.Width (labelWidth));
				EditorGUILayout.LabelField (lineID.ToString (), GUILayout.MaxWidth (maxWidth - labelWidth));

				if ((textType == AC_TextType.Speech || textType == AC_TextType.DialogueOption) && GUILayout.Button ("Locate source", GUILayout.Width (100)))
				{
					KickStarter.speechManager.LocateLine (this);
				}
				EditorGUILayout.EndHorizontal ();
		
				ShowField ("Type:", textType.ToString (), false, maxWidth, apiPrefix + ".textType", "The type of text this is.");
				ShowField ("Original text:", text, true, maxWidth, apiPrefix + ".text", "The original text as recorded during the 'Gather text' process");

				string sceneName = scene.Replace ("Assets/", string.Empty);
				sceneName = sceneName.Replace (".unity", string.Empty);
				ShowField ("Scene:", sceneName, true, maxWidth, apiPrefix + ".scene", "The name of the scene that the text was found in");
				
				if (textType == AC_TextType.Speech)
				{
					ShowField ("Speaker:", GetSpeakerName (), false, maxWidth, apiPrefix + ".owner", "The character who says the line");
				}

				#if LocalizationIsPresent
				useLocalizedString = CustomGUILayout.Toggle ("Rely on Localization?", useLocalizedString, apiPrefix + ".useLocalizedString", "If True, then the line's text will be pulled from Unity's Localization tables");
				if (useLocalizedString)
				{
					if (serializedObject == null)
					{
						serializedObject = new SerializedObject (KickStarter.speechManager);
					}
					if (localizedStringProperty == null)
					{
						int index = -1;
						for (int i = 0; i < KickStarter.speechManager.lines.Count; i++)
						{
							if (KickStarter.speechManager.lines[i].lineID == lineID)
							{
								index = i;
							}
						}

						string propertyName = "lines.Array.data[" + index + "].localizedString";
						localizedStringProperty = serializedObject.FindProperty (propertyName);

						if (localizedStringProperty == null)
						{
							EditorGUILayout.HelpBox ("Cannot find property ' " + propertyName + "' to serialize!", MessageType.Warning);
						}
					}

					if (localizedStringProperty != null)
					{
						serializedObject.Update ();
						EditorGUILayout.PropertyField (localizedStringProperty, new GUIContent ("Localized string:"));
						serializedObject.ApplyModifiedProperties ();
					}
				}
				#endif

				if (speechManager.Languages != null && speechManager.Languages.Count > 1)
				{
					for (int i=0; i<speechManager.Languages.Count; i++)
					{
						if (i == 0)
						{
							if (speechManager.Languages.Count > 1 && speechManager.translateAudio && textType == AC_TextType.Speech)
							{
								EditorGUILayout.Space ();
								EditorGUILayout.LabelField ("Original language:");
							}
						}
						else if (translationText.Count > (i-1))
						{
							#if LocalizationIsPresent
							if (!useLocalizedString)
							#endif
							{
								translationText [i-1] = EditField (speechManager.Languages[i].name + ":", translationText [i-1], true, maxWidth, "", "The display text in '" + speechManager.Languages[i].name + "'");
							}
						}
						else
						{
							ShowField (speechManager.Languages[i].name + ":", "(Not defined)", false, maxWidth);
						}
						
						if (speechManager.translateAudio && textType == AC_TextType.Speech)
						{
							string languageName = string.Empty;
							if (speechManager.Languages[i].isDisabled)
							{
								continue;
							}
							else if (i > 0)
							{
								languageName = speechManager.Languages[i].name;
							}
							
							switch (speechManager.referenceSpeechFiles)
							{
								case ReferenceSpeechFiles.ByNamingConvention:
									{
										if (SeparatePlayerAudio () && speechManager.placeAudioInSubfolders)
										{
											for (int j = 0; j < KickStarter.settingsManager.players.Count; j++)
											{
												if (KickStarter.settingsManager.players[j].playerOb)
												{
													if (speechManager.UseFileBasedLipSyncing ())
													{
														ShowField (" Lipsync path " + j.ToString () + ":", GetFolderName (languageName, true, KickStarter.settingsManager.players[j].playerOb.name), false, maxWidth);
													}
													ShowField (" Audio Path " + j.ToString () + ":", GetFolderName (languageName, false, KickStarter.settingsManager.players[j].playerOb.name), false, maxWidth);
												}
											}
										}
										else
										{
											if (speechManager.UseFileBasedLipSyncing ())
											{
												EditorGUILayout.BeginHorizontal ();
												ShowField (" Lipsync path:", GetFolderName (languageName, true), false, maxWidth);
												ShowLocateButton (i, true);
												EditorGUILayout.EndHorizontal ();
											}

											EditorGUILayout.BeginHorizontal ();
											ShowField (" Audio path:", GetFolderName (languageName), false, maxWidth);
											ShowLocateButton (i);
											EditorGUILayout.EndHorizontal ();
										}
									}
									break;

								case ReferenceSpeechFiles.ByAssetBundle:
									{
										if (speechManager.UseFileBasedLipSyncing ())
										{
											if (!string.IsNullOrEmpty (speechManager.Languages[i].lipsyncAssetBundle))
											{
												ShowField (" Lipsync AB:", "StreamingAssets/" + speechManager.Languages[i].lipsyncAssetBundle, false, maxWidth);
											}
										}

										if (!string.IsNullOrEmpty (speechManager.Languages[i].audioAssetBundle))
										{
											ShowField (" Audio AB:", "StreamingAssets/" + speechManager.Languages[i].audioAssetBundle, false, maxWidth);
										}
									}
									break;

								case ReferenceSpeechFiles.ByDirectReference:
									{
										if (i > 0)
										{
											SetCustomArraySizes (speechManager.Languages.Count - 1);
											if (speechManager.UseFileBasedLipSyncing ())
											{
												customTranslationLipsyncFiles[i - 1] = EditField<Object> (" Lipsync file:", customTranslationLipsyncFiles[i - 1], maxWidth, apiPrefix + ".customTranslationLipsyncFiles[i-1]", "The text file to use for lipsyncing");
											}
											customTranslationAudioClips[i - 1] = EditField<AudioClip> (" Audio clip:", customTranslationAudioClips[i - 1], maxWidth, apiPrefix + ".customTranslationAudioClips[i-1]", "The voice AudioClip to play");
										}
										else
										{
											if (speechManager.UseFileBasedLipSyncing ())
											{
												customLipsyncFile = EditField<Object> (" Lipsync file:", customLipsyncFile, maxWidth, apiPrefix + ".customLipsyncFile", "The text file to use for lipsyncing in the original language");
											}
											customAudioClip = EditField<AudioClip> (" Audio clip:", customAudioClip, maxWidth, apiPrefix + ".customAudioClip", "The voice AudioClip to play in the original language");
										}
									}
									break;

								case ReferenceSpeechFiles.ByAddressable:
									{
										if (SeparatePlayerAudio ())
										{
											for (int p = 0; p < KickStarter.settingsManager.players.Count; p++)
											{
												if (KickStarter.settingsManager.players[p].playerOb)
												{
													if (i > 0)
													{
														ShowField ("Addressable " + p.ToString () + ":", GetFilename (KickStarter.settingsManager.players[p].playerOb.name) + "_" + languageName, false, maxWidth, string.Empty, string.Empty, true);
													}
													else
													{
														ShowField ("Addressable " + p.ToString () + ":", GetFilename (KickStarter.settingsManager.players[p].playerOb.name), false, maxWidth, string.Empty, string.Empty, true);
													}
												}
											}
										}
										else
										{
											if (i > 0)
											{
												ShowField (" Addressable:", GetFilename () + "_" + languageName, false, maxWidth, string.Empty, string.Empty, true);
											}
											else
											{
												ShowField (" Addressable:", GetFilename (), false, maxWidth, string.Empty, string.Empty, true);
											}
										}
									}
									break;

								default:
									break;
							}

							EditorGUILayout.Space ();
						}
					}
				}

				// Original language
				if (textType == AC_TextType.Speech)
				{
					if (speechManager.Languages == null || speechManager.Languages.Count <= 1 || !speechManager.translateAudio)
					{
						Language language = speechManager.Languages[0];

						switch (speechManager.referenceSpeechFiles)
						{
							case ReferenceSpeechFiles.ByNamingConvention:
								{
									if (SeparatePlayerAudio () && speechManager.placeAudioInSubfolders)
									{
										for (int i = 0; i < KickStarter.settingsManager.players.Count; i++)
										{
											if (KickStarter.settingsManager.players[i].playerOb)
											{
												if (speechManager.UseFileBasedLipSyncing ())
												{
													ShowField ("Lipsync path " + i.ToString () + ":", GetFolderName (string.Empty, true, KickStarter.settingsManager.players[i].playerOb.name), false, maxWidth, "", "The filepath to the text asset file to rely on for Lipsyncing");
												}
												ShowField ("Audio Path " + i.ToString () + ":", GetFolderName (string.Empty, false, KickStarter.settingsManager.players[i].playerOb.name), false, maxWidth, "", "The filepath to the voice AudioClip asset file to play");
											}
										}
									}
									else
									{
										if (speechManager.UseFileBasedLipSyncing ())
										{
											EditorGUILayout.BeginHorizontal ();
											ShowField ("Lipsync path:", GetFolderName (string.Empty, true), false, maxWidth);
											ShowLocateButton (0, true);
											EditorGUILayout.EndHorizontal ();
										}

										EditorGUILayout.BeginHorizontal ();
										ShowField ("Audio Path:", GetFolderName (string.Empty), false, maxWidth);
										ShowLocateButton (0);
										EditorGUILayout.EndHorizontal ();
									}
								}
								break;

							case ReferenceSpeechFiles.ByAssetBundle:
								{
									if (speechManager.UseFileBasedLipSyncing ())
									{
										if (!string.IsNullOrEmpty (language.lipsyncAssetBundle))
										{
											ShowField ("Lipsync AB:", "StreamingAssets/" + language.lipsyncAssetBundle, false, maxWidth);
										}
									}

									if (!string.IsNullOrEmpty (language.audioAssetBundle))
									{
										ShowField ("Audio AB:", "StreamingAssets/" + language.audioAssetBundle, false, maxWidth);
									}
								}
								break;

							case ReferenceSpeechFiles.ByDirectReference:
								{
									if (speechManager.UseFileBasedLipSyncing ())
									{
										customLipsyncFile = EditField<Object> ("Lipsync file:", customLipsyncFile, maxWidth, apiPrefix + ".customLipsyncFile", "The text file to use for lipsyncing in the original language");
									}
									customAudioClip = EditField<AudioClip> ("Audio clip:", customAudioClip, maxWidth, apiPrefix + ".customAudioClip", "The voice AudioClip to play in the original language");
								}
								break;

							case ReferenceSpeechFiles.ByAddressable:
								{
									if (SeparatePlayerAudio ())
									{
										for (int i = 0; i < KickStarter.settingsManager.players.Count; i++)
										{
											if (KickStarter.settingsManager.players[i].playerOb)
											{
												ShowField ("Addressable " + i.ToString () + ":", GetFilename (KickStarter.settingsManager.players[i].playerOb.name), false, maxWidth, string.Empty, string.Empty, true);
											}
										}
									}
									else
									{
										ShowField ("Addressable:", GetFilename (), false, maxWidth, string.Empty, string.Empty, true);
									}
								}
								break;

							default:
								break;
						}
					}

					switch (speechManager.referenceSpeechFiles)
					{
						case ReferenceSpeechFiles.ByNamingConvention:
						case ReferenceSpeechFiles.ByAssetBundle:
							{
								if (SeparatePlayerAudio ())
								{
									for (int i = 0; i < KickStarter.settingsManager.players.Count; i++)
									{
										if (KickStarter.settingsManager.players[i].playerOb)
										{
											ShowField ("Filename " + i.ToString () + ":", GetFilename (KickStarter.settingsManager.players[i].playerOb.name), false, maxWidth, string.Empty, string.Empty, true);
										}
									}
								}
								else
								{
									if (string.IsNullOrEmpty (customFilename))
									{
										EditorGUILayout.BeginHorizontal ();
										ShowField ("Filename:", GetFilename (), false, maxWidth, string.Empty, string.Empty, true);
										if (GUILayout.Button ("Set custom", GUILayout.Width (100f)))
										{
											customFilename = GetFilename ();
										}
										EditorGUILayout.EndHorizontal ();
									}
									else
									{
										customFilename = EditField ("Filename:", customFilename, false, maxWidth, apiPrefix + ".customFilename");
									}
								}
							}
							break;

						default:
							break;
					}

					if (tagID >= 0 && speechManager.useSpeechTags)
					{
						SpeechTag speechTag = speechManager.GetSpeechTag (tagID);
						if (speechTag != null && speechTag.label.Length > 0)
						{
							ShowField ("Tag: ", speechTag.label, false, maxWidth, apiPrefix + ".tagID");
						}
					}

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent ("Only say once?", "If True, then this line can only be spoken once. Additional calls to play it will be ignored."), GUILayout.Width (labelWidth));
					onlyPlaySpeechOnce = CustomGUILayout.Toggle (onlyPlaySpeechOnce, GUILayout.MaxWidth (maxWidth - labelWidth), apiPrefix + ".onlyPlaySpeechOnce");
					EditorGUILayout.EndHorizontal ();
				}

				if (SupportsDescriptions ())
				{
					description = EditField ("Description:", description, true, maxWidth, apiPrefix + ".description", "An Editor-only description");
				}
				
				CustomGUILayout.EndVertical ();
			}
			else
			{
				if (GUILayout.Button (lineID.ToString () + ": '" + text + "'", EditorStyles.label, GUILayout.MaxWidth (maxWidth)))
				{
					speechManager.activeLineID = lineID;
					EditorGUIUtility.editingTextField = false;
				}
				GUILayout.Box (string.Empty, GUILayout.ExpandWidth (true), GUILayout.Height(1));
			}
		}


		private bool SupportsDescriptions ()
		{
			return true;
			//return (textType == AC_TextType.Speech || textType == AC_TextType.DialogueOption);
		}


		/**
		 * <summary>Displays a GUI of a field within the class.</summary>
		 * <param name = "label">The label in front of the field</param>
		 * <param name = "field">The field to display</param>
		 * <param name = "multiLine">True if the field should be word-wrapped</param>
		 */
		public static void ShowField (string label, string field, bool multiLine, float maxWidth = 570f, string api = "", string tooltip = "", bool selectable = false)
		{
			if (string.IsNullOrEmpty (field)) return;
			
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent (label, tooltip), GUILayout.Width (labelWidth));
			
			if (multiLine)
			{
				GUIStyle style = new GUIStyle ();
				if (EditorGUIUtility.isProSkin)
				{
					style.normal.textColor = new Color (0.8f, 0.8f, 0.8f);
				}
				style.wordWrap = true;
				style.alignment = TextAnchor.MiddleLeft;

				EditorGUILayout.LabelField (field, style, GUILayout.MaxWidth (maxWidth - labelWidth));
			}
			else
			{
				if (selectable)
				{
					EditorGUILayout.SelectableLabel (field, new GUILayoutOption[] { GUILayout.MaxWidth (maxWidth), GUILayout.Height (18f) });
				}
				else
				{
					EditorGUILayout.LabelField (field, GUILayout.MaxWidth (maxWidth));
				}
			}
			EditorGUILayout.EndHorizontal ();
		}
		
		
		private string EditField (string label, string field, bool multiLine, float maxWidth = 570f, string api = "", string tooltip = "")
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent (label, tooltip), GUILayout.Width (labelWidth));
			if (multiLine)
			{
				field = CustomGUILayout.TextArea (field, GUILayout.MaxWidth (maxWidth - labelWidth), api);
			}
			else
			{
				field = CustomGUILayout.DelayedTextField (field, GUILayout.MaxWidth (maxWidth - labelWidth), api);
			}
			EditorGUILayout.EndHorizontal ();
			return field;
		}
		
		
		private T EditField <T> (string label, T field, float maxWidth = 570f, string api = "", string tooltip = "") where T : Object
		{
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent (label, tooltip), GUILayout.Width (labelWidth));
			field = (T) CustomGUILayout.ObjectField <T> (field, false, GUILayout.MaxWidth (maxWidth - labelWidth), api);
			EditorGUILayout.EndHorizontal ();
			return field;
		}
		
		
		private string GetFolderName (string language, bool forLipsync = false, string overrideName = "")
		{
			return "Resources/" + GetRelativePath (language, forLipsync, overrideName);
		}


		/**
		 * <summary>Checks to see if the class matches a filter set in the Speech Manager.</summary>
		 * <param name = "filter The filter text</param>
		 * <param name = "filterSpeechLine The type of filtering selected (Type, Text, Scene, Speaker, Description, All)</param>
		 * <returns>True if the class matches the criteria of the filter, and should be listed</returns>
		 */
		public bool Matches (string filter, FilterSpeechLine filterSpeechLine)
		{
			if (filter == null || string.IsNullOrEmpty (filter))
			{
				return true;
			}

			filter = filter.ToLower ();
			if (filterSpeechLine == FilterSpeechLine.All)
			{
				if (description.ToLower ().Contains (filter)
				    || scene.ToLower ().Contains (filter)
				    || owner.ToLower ().Contains (filter)
				    || text.ToLower ().Contains (filter)
				    || lineID.ToString ().Contains (filter)
				    || textType.ToString ().ToLower ().Contains (filter))
				{
					return true;
				}
			}
			else if (filterSpeechLine == FilterSpeechLine.Description)
			{
				return description.ToLower ().Contains (filter);
			}
			else if (filterSpeechLine == FilterSpeechLine.Scene)
			{
				return scene.ToLower ().Contains (filter);
			}
			else if (filterSpeechLine == FilterSpeechLine.Speaker)
			{
				return owner.ToLower ().Contains (filter);
			}
			else if (filterSpeechLine == FilterSpeechLine.Text)
			{
				return text.ToLower ().Contains (filter);
			}
			else if (filterSpeechLine == FilterSpeechLine.Type)
			{
				return textType.ToString ().ToLower ().Contains (filter);
			}
			else if (filterSpeechLine == FilterSpeechLine.ID)
			{
				return lineID.ToString ().Contains (filter);
			}
			return false;
		}
		
		
		/**
		 * <summary>Combines the type and owner into a single string, for display in exported game text.</summary>
		 * <returns>A string of the type, and the owner if there is one</returns>
		 */
		public string GetInfo ()
		{
			string info = textType.ToString ();
			if (!string.IsNullOrEmpty (owner))
			{
				info += " (" + owner + ")";
			}
			return info;
		}
		
		
		/**
		 * <summary>Combines the class's various fields into a formatted HTML string, for display in exported game text.</summary>
		 * <param name = "languageIndex">The index number of the language to display fields for, where 0 = the game's original language</param>
		 * <param name = "includeDescriptions">If True, its description will also be included</param>
		 * <param name = "removeTokens">If True, text tokens such as [wait] within the text will be removed</param>
		 * <returns>A string of the owner, filename, text and description</returns>
		 */
		public string Print (int languageIndex = 0, bool includeDescriptions = false, bool removeTokens = false)
		{
			int i = languageIndex;
			
			string result = "<table>\n";
			result += "<tr><td><b>Line ID:</b></td><td>" + lineID + "</td></tr>\n";
			result += "<tr><td width=150><b>Character:</b></td><td>" + GetSpeakerName () + "</td></tr>\n";

			string lineText = text;
			if (i > 0 && translationText.Count > (i-1))
			{
				lineText = translationText [i-1];
			}

			if (removeTokens)
			{
				Speech tempSpeech = new Speech (lineText);
				lineText = tempSpeech.displayText;
			}

			result += "<tr><td><b>Line text:</b></td><td>" + lineText + "</td></tr>\n";
			
			if (!string.IsNullOrEmpty (description) && includeDescriptions)
			{
				result += "<tr><td><b>Description:</b></td><td>" + description + "</td></tr>\n";
			}
			
			string languageName = string.Empty;
			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			if (i > 0 && speechManager.translateAudio)
			{
				languageName = AdvGame.GetReferences ().speechManager.Languages[i].name;
			}
			
			switch (speechManager.referenceSpeechFiles)
			{
				case ReferenceSpeechFiles.ByNamingConvention:
					{
						if (SeparatePlayerAudio ())
						{
							if (speechManager.UseFileBasedLipSyncing ())
							{
								for (int j = 0; j < KickStarter.settingsManager.players.Count; j++)
								{
									if (KickStarter.settingsManager.players[j].playerOb)
									{
										string overrideName = KickStarter.settingsManager.players[j].playerOb.name;
										result += "<td><b>Lipsync file:</b></td><td>" + GetFolderName (languageName, true, overrideName) + GetFilename (overrideName) + "</td></tr>\n";
									}
								}
							}

							for (int j = 0; j < KickStarter.settingsManager.players.Count; j++)
							{
								if (KickStarter.settingsManager.players[j].playerOb)
								{
									string overrideName = KickStarter.settingsManager.players[j].playerOb.name;
									result += "<tr><td><b>Audio file:</b></td><td>" + GetFolderName (languageName, false, overrideName) + GetFilename (overrideName) + "</td></tr>\n";
								}
							}
						}
						else
						{
							if (speechManager.UseFileBasedLipSyncing ())
							{
								result += "<td><b>Lipsync file:</b></td><td>" + GetFolderName (languageName, true) + GetFilename () + "</td></tr>\n";
							}
							result += "<tr><td><b>Audio file:</b></td><td>" + GetFolderName (languageName, false) + GetFilename () + "</td></tr>\n";
						}
					}
					break;

				case ReferenceSpeechFiles.ByDirectReference:
					{
						if (speechManager.UseFileBasedLipSyncing () && customLipsyncFile)
						{
							result += "<td><b>Lipsync file:</b></td><td>" + customLipsyncFile.name + "</td></tr>\n";
						}
						if (customAudioClip)
						{
							result += "<tr><td><b>Audio file:</b></td><td>" + customAudioClip.name + "</td></tr>\n";
						}
					}
					break;

				default:
					break;
			}

			result += "</table>\n\n";
			result += "<br/>\n";
			return result;
		}


		/**
		 * <summary>Checks if the line has all associated audio for all languages, if a speech line</summary>
		 * <returns>True if the line has all associated audio for all language</returns>
		 */
		public bool HasAllAudio ()
		{
			if (textType != AC_TextType.Speech ||
				KickStarter.speechManager == null)
			{
				return false;
			}

			if (KickStarter.speechManager.translateAudio)
			{
				int numLanguages = (KickStarter.speechManager.Languages != null) ? KickStarter.speechManager.Languages.Count : 1;
				for (int i=0; i<numLanguages; i++)
				{
					if (!HasAudio (i))
					{
						return false;
					}
				}
				return true;
			}
			else
			{
				return HasAudio (0);
			}
		}


		private void ShowLocateButton (int languageIndex, bool forLipSync = false)
		{
			if (GUILayout.Button ("Ping file", GUILayout.Width (100f)))
			{
				string languageName = (languageIndex == 0) ? string.Empty : AdvGame.GetReferences ().speechManager.Languages[languageIndex].name;
				string fullName = string.Empty;

				Object foundClp = null;

				if (KickStarter.settingsManager && SeparatePlayerAudio ())
				{
					foreach (PlayerPrefab player in KickStarter.settingsManager.players)
					{
						if (player != null && player.playerOb)
						{
							fullName = GetAutoAssetPathAndName (languageName, forLipSync, player.playerOb.name);

							if (forLipSync)
							{
								if (KickStarter.speechManager.lipSyncMode == LipSyncMode.RogoLipSync)
								{
									Object lipSyncFile = RogoLipSyncIntegration.GetObjectToPing (fullName);
									if (lipSyncFile)
									{
										foundClp = lipSyncFile;
									}
								}
								else
								{
									TextAsset textFile = Resources.Load (fullName) as TextAsset;
									if (textFile )
									{
										foundClp = textFile;
									}
								}
							}
							else
							{
								AudioClip clipObj = Resources.Load (fullName) as AudioClip;
								if (clipObj)
								{
									foundClp = clipObj;
									break;
								}
							}
						}
					}
				}
				else
				{
					fullName = GetAutoAssetPathAndName (languageName, forLipSync);

					if (forLipSync)
					{
						if (KickStarter.speechManager.lipSyncMode == LipSyncMode.RogoLipSync)
						{
							foundClp = RogoLipSyncIntegration.GetObjectToPing (fullName);
						}
						else
						{
							TextAsset textFile = Resources.Load (fullName) as TextAsset;
							foundClp = textFile;
						}
					}
					else
					{
						AudioClip clipObj = Resources.Load (fullName) as AudioClip;
						foundClp = clipObj;
					}
				}

				if (foundClp)
				{
					EditorGUIUtility.PingObject (foundClp);
				}
				else
				{
					ACDebug.Log (((forLipSync) ? "Text" : "Audio") + " file '/Resources/" + fullName + "' was not found.");
				}
			}
		}


		/**
		 * <summary>Checks if the line has all associated audio for a given language, if a speech line</summary>
		 * <param name = "languageIndex">The index number of the language in question.  The game's original language is 0</param>
		 * <returns>True if the line has all associated audio for the given language</returns>
		 */
		public bool HasAudio (int languageIndex)
		{
			if (textType != AC_TextType.Speech) return false;

			SpeechManager speechManager = KickStarter.speechManager;
			if (speechManager == null) return false;

			switch (speechManager.referenceSpeechFiles)
			{
				case ReferenceSpeechFiles.ByNamingConvention:
					{
						string languageName = (languageIndex == 0) ? string.Empty : speechManager.Languages[languageIndex].name;

						if (SeparatePlayerAudio ())
						{
							bool doDisplay = false;
							foreach (PlayerPrefab player in KickStarter.settingsManager.players)
							{
								if (player != null && player.playerOb)
								{
									string fullName = GetAutoAssetPathAndName (languageName, false, player.playerOb.name);
									AudioClip clipObj = Resources.Load (fullName) as AudioClip;

									if (clipObj == null)
									{
										doDisplay = true;
									}
								}
							}

							if (!doDisplay)
							{
								return false;
							}
							return true;
						}
						else
						{
							string fullName = GetAutoAssetPathAndName (languageName);
							AudioClip clipObj = Resources.Load (fullName) as AudioClip;

							if (clipObj)
							{
								return true;
							}
						}
					}
					break;

				case ReferenceSpeechFiles.ByDirectReference:
					{
						if (languageIndex == 0 && customAudioClip)
						{
							return true;
						}
						if (speechManager.translateAudio && languageIndex > 0 && customTranslationAudioClips.Count > (languageIndex - 1) && customTranslationAudioClips[languageIndex - 1])
						{
							return true;
						}
						if (!speechManager.translateAudio && customAudioClip)
						{
							return true;
						}
					}
					break;

				default:
					break;
			}

			return false;
		}

		#endif

		#endregion


		#region ProtectedFunctions

		protected string GetSubfolderPath (string overrideName = "")
		{
			string filename = string.Empty;
			if (SeparatePlayerAudio () && !string.IsNullOrEmpty (overrideName))
			{
				filename = overrideName;
			}
			else if (!string.IsNullOrEmpty (owner))
			{
				filename = owner;

				if (isPlayer &&
					textType == AC_TextType.Speech &&
					(KickStarter.speechManager == null || !KickStarter.speechManager.usePlayerRealName))
				{
					filename = "Player";
				}
			}
			else
			{
				filename = "Narrator";
			}

			for (int i = 0; i < badChars.Length; i++)
			{
				filename = filename.Replace (badChars[i], "_");
			}
			filename = filename.Replace (SpeechMarkAsString, "_");
			return filename;
		}


		protected string GetRelativePath (string language, bool forLipsync = false, string overrideName = "")
		{
			string folderName = (forLipsync) ? KickStarter.speechManager.AutoLipsyncFolder : KickStarter.speechManager.AutoSpeechFolder;
			
			if (!string.IsNullOrEmpty (language) && KickStarter.speechManager.translateAudio)
			{
				// Not in original language
				folderName += language + "/";
			}
			if (KickStarter.speechManager.placeAudioInSubfolders)
			{
				folderName += GetSubfolderPath (overrideName) + "/";
			}

			return folderName;
		}


		protected string GetSpeakerName ()
		{
			if (isPlayer && (AdvGame.GetReferences ().speechManager == null || !AdvGame.GetReferences ().speechManager.usePlayerRealName))
			{
				return "Player";
			}
			return owner;
		}


		protected void SetCustomArraySizes (int newCount)
		{
			if (customTranslationAudioClips == null)
			{
				customTranslationAudioClips = new List<AudioClip>();
			}
			if (customTranslationLipsyncFiles == null)
			{
				customTranslationLipsyncFiles = new List<Object>();
			}
			
			if (newCount < 0)
			{
				newCount = 0;
			}
			
			if (newCount < customTranslationAudioClips.Count)
			{
				customTranslationAudioClips.RemoveRange (newCount, customTranslationAudioClips.Count - newCount);
			}
			else if (newCount > customTranslationAudioClips.Count)
			{
				if (newCount > customTranslationAudioClips.Capacity)
				{
					customTranslationAudioClips.Capacity = newCount;
				}
				for (int i=customTranslationAudioClips.Count; i<newCount; i++)
				{
					customTranslationAudioClips.Add (null);
				}
			}
			
			if (newCount < customTranslationLipsyncFiles.Count)
			{
				customTranslationLipsyncFiles.RemoveRange (newCount, customTranslationLipsyncFiles.Count - newCount);
			}
			else if (newCount > customTranslationLipsyncFiles.Count)
			{
				if (newCount > customTranslationLipsyncFiles.Capacity)
				{
					customTranslationLipsyncFiles.Capacity = newCount;
				}
				for (int i=customTranslationLipsyncFiles.Count; i<newCount; i++)
				{
					customTranslationLipsyncFiles.Add (null);
				}
			}
		}


		protected List<AudioClip> GetAudioListForTranslation (List<AudioClip> audioClips, int language)
		{
			List<AudioClip> audioList = new List<AudioClip>();

			if (language > 0)
			{
				int indexToKeep = language-1;

				for (int i=0; i<audioClips.Count; i++)
				{
					audioList.Add ((indexToKeep == i) ? audioClips[i] : null);
				}
			}

			return audioList;
		}


		protected List<Object> GetLipsyncListForTranslation (List<Object> lipsyncFiles, int language)
		{
			List<Object> lipsyncList = new List<Object>();

			if (language > 0)
			{
				int indexToKeep = language-1;

				for (int i=0; i<lipsyncFiles.Count; i++)
				{
					lipsyncList.Add ((indexToKeep == i) ? lipsyncFiles[i] : null);
				}
			}

			return lipsyncList;
		}

		#endregion


		#region GetSet

		#if UNITY_EDITOR

		public string OrderIdentifier
		{
			get
			{
				return orderPrefix + orderID.ToString ("D4");
			}
		}

		#endif


		/** The line's default filenames - used unless overridden by custom filenaming */
		public string DefaultFilename
		{
			get
			{
				return GetSubfolderPath () + lineID.ToString ();
			}
		}


		protected static string SpeechMarkAsString
		{
			get
			{
				if (string.IsNullOrEmpty (speechMarkAsString))
				{
					speechMarkAsString = '"'.ToString ();
				}
				return speechMarkAsString;
			}
		}

		#endregion
		
	}
	
}