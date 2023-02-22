/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionOptionSet.cs"
 * 
 *	This Action allows you to set an Options variable to a specific value
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionOptionSet : Action
	{

		[SerializeField] protected int indexParameterID = -1;
		[SerializeField] protected int index;

		[SerializeField] protected int volumeParameterID = -1;
		[SerializeField] protected float volume;

		[SerializeField] protected OptionSetMethod method = OptionSetMethod.Language;
		protected enum OptionSetMethod { Language=0, Subtitles=1, SFXVolume=2, SpeechVolume=3, MusicVolume=4 };

		[SerializeField] protected SplitLanguageType splitLanguageType = SplitLanguageType.TextAndVoice;


		public override ActionCategory Category { get { return ActionCategory.Save; }}
		public override string Title { get { return "Set Option"; }}
		public override string Description { get { return "Set an Options variable to a specific value"; }}
		

		public override void AssignValues (List<ActionParameter> parameters)
		{
			switch (method)
			{
				case OptionSetMethod.Language:
					index = AssignInteger (parameters, indexParameterID, index);
					break;

				case OptionSetMethod.Subtitles:
					BoolValue boolValue = (BoolValue) index;
					boolValue = AssignBoolean (parameters, indexParameterID, boolValue);
					index = (int) boolValue;
					break;

				case OptionSetMethod.SFXVolume:
				case OptionSetMethod.SpeechVolume:
				case OptionSetMethod.MusicVolume:
					volume = AssignFloat (parameters, volumeParameterID, volume);
					volume = Mathf.Clamp01 (volume);
					break;
			}
		}
		
		
		public override float Run ()
		{
			switch (method)
			{
				case OptionSetMethod.Language:
					if (index >= 0 && KickStarter.speechManager != null && index < KickStarter.speechManager.Languages.Count)
					{
						if (KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages)
						{
							switch (splitLanguageType)
							{
								case SplitLanguageType.TextAndVoice:
									Options.SetLanguage (index);
									Options.SetVoiceLanguage (index);
									break;

								case SplitLanguageType.TextOnly:
									Options.SetLanguage (index);
									break;

								case SplitLanguageType.VoiceOnly:
									Options.SetVoiceLanguage (index);
									break;
							}
						}
						else
						{
							Options.SetLanguage (index);
						}
					}
					else
					{
						LogWarning ("Could not set language to index: " + index + " - does this language exist?");
					}
					break;

				case OptionSetMethod.Subtitles:
					Options.SetSubtitles ((index == 1));
					break;

				case OptionSetMethod.SpeechVolume:
					Options.SetSpeechVolume (volume);
					break;

				case OptionSetMethod.SFXVolume:
					Options.SetSFXVolume (volume);
					break;

				case OptionSetMethod.MusicVolume:
					Options.SetMusicVolume (volume);
					break;
			}

			return 0f;
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			method = (OptionSetMethod) EditorGUILayout.EnumPopup ("Option to set:", method);

			switch (method)
			{
				case OptionSetMethod.Language:
					if (KickStarter.speechManager == null)
					{
						EditorGUILayout.HelpBox ("No Speech Manager found! One must be assigned in order to change the language.", MessageType.Warning);
					}
					else if (KickStarter.speechManager.Languages != null && KickStarter.speechManager.Languages.Count > 1)
					{
						if (KickStarter.speechManager != null && KickStarter.speechManager.separateVoiceAndTextLanguages)
						{
							splitLanguageType = (SplitLanguageType) EditorGUILayout.EnumPopup ("Affect:", splitLanguageType);
						}

						indexParameterID = Action.ChooseParameterGUI ("Language:", parameters, indexParameterID, ParameterType.Integer);
						if (indexParameterID < 0)
						{
							index = EditorGUILayout.Popup ("Language:", index, KickStarter.speechManager.GetLanguageNameArray ());
						}
					}
					else
					{
						index = 0;
						EditorGUILayout.HelpBox ("Multiple languages not found!.", MessageType.Warning);
					}
					break;

				case OptionSetMethod.Subtitles:
					indexParameterID = Action.ChooseParameterGUI ("Show subtitles:", parameters, indexParameterID, ParameterType.Boolean);
					if (indexParameterID < 0)
					{
						bool showSubtitles = (index == 1);
						showSubtitles = EditorGUILayout.Toggle ("Show subtitles?", showSubtitles);
						index = (showSubtitles) ? 1 : 0;
					}
					break;

				case OptionSetMethod.SFXVolume:
				case OptionSetMethod.SpeechVolume:
				case OptionSetMethod.MusicVolume:
					volumeParameterID = Action.ChooseParameterGUI ("New volume:", parameters, volumeParameterID, ParameterType.Float);
					if (volumeParameterID < 0)
					{
						volume = EditorGUILayout.Slider ("New volume:", volume, 0f, 1f);
					}
					break;
			}
		}
		

		public override string SetLabel ()
		{
			return method.ToString ();
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Save: Set Option' Action, set to change the active language</summary>
		 * <param name = "languageIndex">The index number of the new language</param>
		 * <param name = "splitLanguageType">Whether to switch text language, voice language, or both</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionOptionSet CreateNew_Language (int languageIndex, SplitLanguageType splitLanguageType = SplitLanguageType.TextAndVoice)
		{
			ActionOptionSet newAction = CreateNew<ActionOptionSet> ();
			newAction.method = OptionSetMethod.Language;
			newAction.index = languageIndex;
			newAction.splitLanguageType = splitLanguageType;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Set Option' Action, set to change the state of subtitles</summary>
		 * <param name = "newState">If True, subtitles will be enabled</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionOptionSet CreateNew_Subtitles (bool newState)
		{
			ActionOptionSet newAction = CreateNew<ActionOptionSet> ();
			newAction.method = OptionSetMethod.Subtitles;
			newAction.index = (newState) ? 1 : 0;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Set Option' Action, set to change the SFX volume</summary>
		 * <param name = "newVolume">The new SFX volume, as a decimal</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionOptionSet CreateNew_SFXVolume (float newVolume)
		{
			ActionOptionSet newAction = CreateNew<ActionOptionSet> ();
			newAction.method = OptionSetMethod.SFXVolume;
			newAction.volume = newVolume;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Set Option' Action, set to change the music volume</summary>
		 * <param name = "newVolume">The new music volume, as a decimal</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionOptionSet CreateNew_MusicVolume (float newVolume)
		{
			ActionOptionSet newAction = CreateNew<ActionOptionSet> ();
			newAction.method = OptionSetMethod.MusicVolume;
			newAction.volume = newVolume;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Save: Set Option' Action, set to change the speech volume</summary>
		 * <param name = "newVolume">The new speech volume, as a decimal</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionOptionSet CreateNew_SpeechVolume (float newVolume)
		{
			ActionOptionSet newAction = CreateNew<ActionOptionSet> ();
			newAction.method = OptionSetMethod.SpeechVolume;
			newAction.volume = newVolume;
			return newAction;
		}

	}

}