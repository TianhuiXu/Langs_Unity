/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AdvGame.cs"
 * 
 *	This script provides a number of static functions used by various game scripts.
 * 
 * 	The "DrawTextOutline" function is based on Bérenger's code: http://wiki.unity3d.com/index.php/ShadowAndOutline
 * 
 */

using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A class that provides a number of useful functons for both editor and runtime.
	 */
	public class AdvGame : ScriptableObject
	{
		
		#if UNITY_EDITOR
		private static Texture2D _aaLineTex = null;
		#endif


		/**
		 * <summary>Sets the volume of an Audio Mixer Group (Unity 5 and onward only).</summary>
		 * <param name = "audioMixerGroup">The Audio Mixer Group to affect</param>
		 * <param name = "parameter">The name of the attenuation parameter</param>
		 * <param name = "volume">The new volume (ranges from 0 to 1)</param>
		 */
		public static void SetMixerVolume (AudioMixerGroup audioMixerGroup, string parameter, float volume)
		{
			if (string.IsNullOrEmpty (parameter) || audioMixerGroup == null)
			{
				return;
			}

			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				float attenuation = (volume > 0f) ? (Mathf.Log10 (volume) * 20f) : -80f;
				audioMixerGroup.audioMixer.SetFloat (parameter, attenuation);
			}
		}


		/**
		 * <summary>Sets the 'Output Audio Mixer Group' of an Audio Source, based on its sound type (Unity 5 only).</summary>
		 * <param name = "audioSource">The Audio Source component to affect</param>
		 * <param name = "soundType">The sound type that controls the volume</param>
		 */
		public static void AssignMixerGroup (AudioSource audioSource, SoundType soundType)
		{
			if (audioSource && KickStarter.settingsManager != null && KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				if (audioSource.outputAudioMixerGroup)
				{
					return;
				}

				if (soundType == SoundType.Music)
				{
					if (KickStarter.settingsManager.musicMixerGroup)
					{
						audioSource.outputAudioMixerGroup = KickStarter.settingsManager.musicMixerGroup;
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign " + audioSource.gameObject.name + " a music AudioMixerGroup!");
					}
				}
				else if (soundType == SoundType.SFX)
				{
					if (KickStarter.settingsManager.sfxMixerGroup)
					{
						audioSource.outputAudioMixerGroup = KickStarter.settingsManager.sfxMixerGroup;
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign " + audioSource.gameObject.name + " a sfx AudioMixerGroup!");
					}
				}
				else if (soundType == AC.SoundType.Speech)
				{
					if (KickStarter.settingsManager.speechMixerGroup)
					{
						audioSource.outputAudioMixerGroup = KickStarter.settingsManager.speechMixerGroup;
					}
					else
					{
						ACDebug.LogWarning ("Cannot assign " + audioSource.gameObject.name + " a speech AudioMixerGroup!");
					}
				}
			}
		}

		
		/**
		 * <summary>Returns the integer value of the AnimLayer enum.
		 * Necessary because two Neck layers are used, though only one is present in the enum.</summary>
		 * <param name = "animLayer">The AnimLayer enum</param>
		 * <returns>The integer value</returns>
		 */
		public static int GetAnimLayerInt (AnimLayer animLayer)
		{
			int layerInt = (int) animLayer;
			
			// Hack, because we actually use two neck layers
			if (layerInt > 4)
			{
				layerInt ++;
			}
			
			return layerInt;
		}


		/**
		 * Returns the References asset, which should be located in a Resources directory.
		 */
		public static References GetReferences ()
		{
			return Resource.References;
		}
		

		/**
		 * <summary>Runs an ActionList asset file.
		 * If the ActionList contains an Integer parameter, the parameter's value can be set here.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "parameterID">The ID of the parameter to set</param>
		 * <param name = "parameterValue">The value to set the parameter to, provided that IsIntegerBased returns True</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, int parameterID = -1, int parameterValue = 0)
		{
			if (parameterID >= 0 && actionListAsset && actionListAsset.NumParameters > 0)
			{
				ActionParameter parameter = actionListAsset.GetParameter (parameterID);
				if (parameter != null)
				{
					if (parameter.IsIntegerBased ())
					{
						parameter.intValue = parameterValue;
					}
					else
					{
						ACDebug.LogWarning ("Cannot update " + actionListAsset.name + "'s parameter '" + parameter.label + "' because it's value is not integer-based.", actionListAsset);
					}
				}
			}

			return RunActionListAsset (actionListAsset, null, 0, false, true);
		}


		/**
		 * <summary>Runs an ActionList asset file, and sets the value of the first parameter, provided that it is a GameObject.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "parameterValue">The value to set the GameObject parameter to</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, GameObject parameterValue)
		{
			if (actionListAsset && actionListAsset.NumParameters > 0)
			{
				ActionParameter parameter = actionListAsset.GetParameters ()[0];
				if (parameter.parameterType == ParameterType.GameObject)
				{
					parameter.gameObject = parameterValue;
				}
				else
				{
					ACDebug.LogWarning ("Cannot update " + actionListAsset.name + "'s parameter '" + parameter.label + "' because it is not a GameObject!", actionListAsset);
				}
			}
			
			return RunActionListAsset (actionListAsset, null, 0, false, true);
		}


		/**
		 * <summary>Runs an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "i">The index of the Action to start from</param>
		 * <param name = "addToSkipQueue">True if the ActionList should be added to the skip queue</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, int i, bool addToSkipQueue)
		{
			return RunActionListAsset (actionListAsset, null, i, false, addToSkipQueue);
		}
		

		/**
		 * <summary>Runs an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "endConversation">The Conversation to enable when the ActionList is complete</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, Conversation endConversation)
		{
			return RunActionListAsset (actionListAsset, endConversation, 0, false, true);
		}
		

		/**
		 * <summary>Runs or skips an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "endConversation">The Conversation to enable when the ActionList is complete</param>
		 * <param name = "i">The index of the Action to start from</param>
		 * <param name = "doSkip">If True, all Actions within the ActionList will be run and completed instantly.</param>
		 * <param name = "addToSkipQueue">True if the ActionList should be added to the skip queue</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList RunActionListAsset (ActionListAsset actionListAsset, Conversation endConversation, int i, bool doSkip, bool addToSkipQueue)
		{
			if (KickStarter.actionListAssetManager == null)
			{
				ACDebug.LogWarning ("Cannot run an ActionList asset file without the presence of the Action List Asset Manager component - is this an AC scene?");
				return null;
			}
			if (actionListAsset && actionListAsset.actions.Count > 0)
			{
				int numInstances = KickStarter.actionListAssetManager.GetNumInstances (actionListAsset);

				GameObject runtimeActionListObject = new GameObject ();
				runtimeActionListObject.name = actionListAsset.name;
				if (numInstances > 0) runtimeActionListObject.name += " " + numInstances.ToString ();

				RuntimeActionList runtimeActionList = runtimeActionListObject.AddComponent <RuntimeActionList>();
				runtimeActionList.DownloadActions (actionListAsset, endConversation, i, doSkip, addToSkipQueue);

				return runtimeActionList;
			}
			
			return null;
		}


		/**
		 * <summary>Skips an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to skip</param>
		 * <returns>the temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList SkipActionListAsset (ActionListAsset actionListAsset)
		{
			return RunActionListAsset (actionListAsset, null, 0, true, false);
		}


		/**
		 * <summary>Skips an ActionList asset file.</summary>
		 * <param name = "actionListAsset">The ActionList asset to skip</param>
		 * <param name = "i">The index of the Action to skip from</param>
		 * <param name = "endConversation">The Conversation to enable when the ActionList is complete</param>
		 * <returns>The temporary RuntimeActionList object in the scene that performs the Actions within the asset</returns>
		 */
		public static RuntimeActionList SkipActionListAsset (ActionListAsset actionListAsset, int i, Conversation endConversation = null)
		{
			return RunActionListAsset (actionListAsset, endConversation, i, true, false);
		}


		/**
		 * <summary>Calculates a formula (Not available for Windows Phone devices).</summary>
		 * <param name = "formula">The formula string to calculate</param>
		 * <returns>The result</returns>
		 */
		public static double CalculateFormula (string formula)
		{
			#if UNITY_WP8 || UNITY_WINRT
			return 0;
			#else
			try
			{
				return ((double) new System.Xml.XPath.XPathDocument
						(new System.IO.StringReader("<r/>")).CreateNavigator().Evaluate
						(string.Format("number({0})", new System.Text.RegularExpressions.Regex (@"([\+\-\*\^])").Replace (formula, " ${1} ").Replace ("/", " div ").Replace ("%", " mod "))));
			}
			catch
			{
				ACDebug.LogWarning ("Cannot compute formula: " + formula);
				return 0;
			}
			#endif
		}


		/**
		 * <summary>Combines two on-screen strings into one.</summary>
		 * <param name = "string1">The first string</param>
		 * <param name = "string1">The second string</param>
		 * <param name = "languageIndex">The index number of the current language. If the language reads right-to-left, then the strings will be combined in reverse</param>
		 * <param name = "separateWithSpace">If True, the two strings will be separated by a space</param>
		 * <returns>The combined string</returns>
		 */
		public static string CombineLanguageString (string string1, string string2, int langugeIndex, bool separateWithSpace = true)
		{
			if (string.IsNullOrEmpty (string1))
			{
				return string2;
			}

			if (string.IsNullOrEmpty (string2))
			{
				return string1;
			}

			if (KickStarter.runtimeLanguages.LanguageReadsRightToLeft (langugeIndex))
			{
				if (separateWithSpace)
				{
					return (string2 + " " + string1);
				}
				return (string2 + string1);
			}
			
			if (separateWithSpace)
			{
				return (string1 + " " + string2);
			}
			return (string1 + string2);
		}


		/**
		 * <summary>Converts a string's tokens into their true values.
		 * The '[var:ID]' token will be replaced by the value of global variable 'ID'.
		 * The '[localvar:ID]' token will be replaced by the value of local variable 'ID'.</summary>
		 * <param name = "_text">The original string with tokens</param>
		 * <returns>The converted string without tokens</returns>
		 */
		public static string ConvertTokens (string _text)
		{
			return ConvertTokens (_text, Options.GetLanguage ());
		}

		private static string tokenStart;
		private static int tokenIndex, tokenValueStartIndex, tokenValueEndIndex;

		/**
		 * <summary>Converts a string's tokens into their true values.
		 * The '[var:ID]' token will be replaced by the value of global variable 'ID'.
		 * The '[localvar:ID]' token will be replaced by the value of local variable 'ID'.</summary>
		 * <param name = "_text">The original string with tokens</param>
		 * <param name = "languageNumber">The index number of the game's current language</param>
		 * <param name = "localVariables">The LocalVariables script to read local variables from, if not the scene default</param>
		 * <returns>The converted string without tokens</returns>
		 */
		public static string ConvertTokens (string _text, int languageNumber, LocalVariables localVariables = null, List<ActionParameter> parameters = null)
		{
			if (!Application.isPlaying)
			{
				return _text;
			}

			if (localVariables == null) localVariables = KickStarter.localVariables;
			
			if (!string.IsNullOrEmpty (_text))
			{
				if (KickStarter.runtimeVariables && KickStarter.eventManager && KickStarter.runtimeVariables.TextEventTokenKeys != null)
				{
					string[] eventKeys = KickStarter.runtimeVariables.TextEventTokenKeys;
					foreach (string eventKey in eventKeys)
					{
						if (string.IsNullOrEmpty (eventKey)) continue;

						tokenStart = "[" + eventKey + ":";
						if (_text.Contains (tokenStart))
						{
							int valueStartIndex = _text.IndexOf (tokenStart) + tokenStart.Length;
							int valueEndIndex = _text.Substring (valueStartIndex).IndexOf ("]");

							if (valueEndIndex > 0)
							{
								string eventValue = _text.Substring (valueStartIndex, valueEndIndex);
								string fullToken = tokenStart + eventValue + "]";

								string replacementText = KickStarter.eventManager.Call_OnRequestTextTokenReplacement (eventKey, eventValue);
								_text = _text.Replace (fullToken, replacementText);
							}
						}
					}
				}

				int numIterations = 1;
				while (numIterations > 0)
				{
					// Translation ID
					tokenStart = "[line:";
					tokenIndex = _text.IndexOf (tokenStart);
					if (tokenIndex >= 0)
					{
						tokenValueStartIndex = tokenIndex + tokenStart.Length;
						tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

						if (tokenValueEndIndex > 0)
						{
							string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
							int _lineID = -1;
							if (int.TryParse (stringValue, out _lineID))
							{
								string replacementText = KickStarter.runtimeLanguages.GetCurrentLanguageText (_lineID);
								if (!string.IsNullOrEmpty (replacementText))
								{
									string fullToken = tokenStart + stringValue + "]";
									_text = _text.Replace (fullToken, replacementText);
									numIterations = 2;
								}
							}
						}
					}

					// Parameters
					if (parameters != null)
					{
						tokenStart = "[param:";
						tokenIndex = _text.IndexOf (tokenStart);
						if (tokenIndex >= 0)
						{
							tokenValueStartIndex = tokenIndex + tokenStart.Length;
							tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

							if (tokenValueEndIndex > 0)
							{
								string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
								int _paramID = -1;
								if (int.TryParse (stringValue, out _paramID))
								{
									foreach (ActionParameter parameter in parameters)
									{
										if (parameter.ID == _paramID)
										{
											string fullToken = tokenStart + stringValue + "]";
											_text = _text.Replace (fullToken, parameter.GetValueAsString ());
											numIterations = 2;
										}
									}
								}
							}
						}

						// Parameter values
						tokenStart = "[paramval:";
						tokenIndex = _text.IndexOf (tokenStart);
						if (tokenIndex >= 0)
						{
							tokenValueStartIndex = tokenIndex + tokenStart.Length;
							tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

							if (tokenValueEndIndex > 0)
							{
								string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
								int _paramID = -1;
								if (int.TryParse (stringValue, out _paramID))
								{
									foreach (ActionParameter parameter in parameters)
									{
										if (parameter.ID == _paramID)
										{
											string fullToken = tokenStart + stringValue + "]";
											string paramValue = string.Empty;
											GVar paramVariable = parameter.GetVariable ();
											if (paramVariable != null)
											{
												paramValue = paramVariable.GetValue (languageNumber);
											}
											else
											{
												paramValue = parameter.GetValueAsString ();
											}
											_text = _text.Replace (fullToken, paramValue);
											numIterations = 2;
										}
									}
								}
							}
						}

						// Parameter labels
						tokenStart = "[paramlabel:";
						tokenIndex = _text.IndexOf (tokenStart);
						if (tokenIndex >= 0)
						{
							tokenValueStartIndex = tokenIndex + tokenStart.Length;
							tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

							if (tokenValueEndIndex > 0)
							{
								string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
								int _paramID = -1;
								if (int.TryParse (stringValue, out _paramID))
								{
									foreach (ActionParameter parameter in parameters)
									{
										if (parameter.ID == _paramID)
										{
											string fullToken = tokenStart + stringValue + "]";
											_text = _text.Replace (fullToken, parameter.GetLabel ());
											numIterations = 2;
										}
									}
								}
							}
						}
					}

					// Global variables
					tokenStart = "[var:";
					tokenIndex = _text.IndexOf (tokenStart);
					if (tokenIndex >= 0)
					{
						tokenValueStartIndex = tokenIndex + tokenStart.Length;
						tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

						if (tokenValueEndIndex > 0)
						{
							string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
							int _varID = -1;
							if (int.TryParse (stringValue, out _varID))
							{
								GVar _var = GlobalVariables.GetVariable (_varID, true);
								if (_var != null)
								{
									string fullToken = tokenStart + stringValue + "]";
									_text = _text.Replace (fullToken, _var.GetValue (languageNumber));
									numIterations = 2;
								}
							}
						}
					}

					// Global variables (alternate)
					tokenStart = "[Var:";
					tokenIndex = _text.IndexOf (tokenStart);
					if (tokenIndex >= 0)
					{
						tokenValueStartIndex = tokenIndex + tokenStart.Length;
						tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

						if (tokenValueEndIndex > 0)
						{
							string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
							int _varID = -1;
							if (int.TryParse (stringValue, out _varID))
							{
								GVar _var = GlobalVariables.GetVariable (_varID, true);
								if (_var != null)
								{
									string fullToken = tokenStart + stringValue + "]";
									_text = _text.Replace (fullToken, _var.GetValue (languageNumber));
									numIterations = 2;
								}
							}
						}
					}

					// Local variables
					tokenStart = "[localvar:";
					tokenIndex = _text.IndexOf (tokenStart);
					if (tokenIndex >= 0)
					{
						tokenValueStartIndex = tokenIndex + tokenStart.Length;
						tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

						if (tokenValueEndIndex > 0)
						{
							string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
							int _varID = -1;
							if (int.TryParse (stringValue, out _varID))
							{
								GVar _var = LocalVariables.GetVariable (_varID, localVariables);
								if (_var != null)
								{
									string fullToken = tokenStart + stringValue + "]";
									_text = _text.Replace (fullToken, _var.GetValue (languageNumber));
									numIterations = 2;
								}
							}
						}
					}

					// Component variables
					tokenStart = "[compvar:";
					tokenIndex = _text.IndexOf (tokenStart);
					if (tokenIndex >= 0)
					{
						int idStartIndex = tokenIndex + 9;
						int idEndIndex = _text.Substring (idStartIndex).IndexOf (":");

						if (idEndIndex > 0)
						{
							string idString = _text.Substring (idStartIndex, idEndIndex);
							int id = 0;
							if (int.TryParse (idString, out id))
							{
								if (id != 0)
								{
									Variables variables = ConstantID.GetComponent <Variables> (id);
									if (variables)
									{
										tokenValueStartIndex = idStartIndex + idEndIndex + 1;
										tokenValueEndIndex = _text.Substring (tokenValueStartIndex).IndexOf ("]");

										if (tokenValueEndIndex > 0)
										{
											string stringValue = _text.Substring (tokenValueStartIndex, tokenValueEndIndex);
											int _varID = -1;
											if (int.TryParse (stringValue, out _varID))
											{
												GVar _var = variables.GetVariable (_varID);
												if (_var != null)
												{
													string fullToken = tokenStart + idString + ":" + stringValue + "]";
													_text = _text.Replace (fullToken, _var.GetValue (languageNumber));
													numIterations = 2;
												}
											}
										}
									}
								}
							}
						}
					}

					numIterations --;
				}

				if (KickStarter.runtimeVariables)
				{
					_text = KickStarter.runtimeVariables.ConvertCustomTokens (_text);
				}
			}
			
			return _text;
		}
		
		
		#if UNITY_EDITOR

		public static string GetVariableTokenText (VariableLocation location, int varID, int variablesConstantID = 0)
		{
			switch (location)
			{
				case VariableLocation.Global:
					return "[var:" + varID.ToString () + "]";

				case VariableLocation.Local:
					return "[localvar:" + varID.ToString () + "]";

				case VariableLocation.Component:
					if (variablesConstantID != 0)
					{
						return "[compvar:" + variablesConstantID.ToString () + ":" + varID.ToString () + "'";
					}
					break;

				default:
					break;
			}

			return string.Empty;
		}


		/**
		 * <summary>Converts a token that refers to a given local variable, to one that refers to a given global variable</summary>
		 * <param name = "_text">The text to convert</param>
		 * <param name = "oldLocalID">The ID number of the old local variable</param>
		 * <param name = "newGlobalID">The ID number of the new global variable</param>
		 * <returns>The converted text</returns>
		 */
		public static string ConvertLocalVariableTokenToGlobal (string _text, int oldLocalID, int newGlobalID)
		{
			if (string.IsNullOrEmpty (_text)) return _text;

			string oldVarToken = "[localvar:" + oldLocalID.ToString () + "]";
			if (_text.Contains (oldVarToken))
			{
				string newVarToken = "[var:" + newGlobalID.ToString () + "]";
				_text = _text.Replace (oldVarToken, newVarToken);
			}
			return _text;
		}


		/**
		 * <summary>Converts a token that refers to a given local variable, to one that refers to a given global variable</summary>
		 * <param name = "_text">The text to convert</param>
		 * <param name = "oldGlobalID">The ID number of the old global variable</param>
		 * <param name = "newLocalID">The ID number of the new local variable</param>
		 * <returns>The converted text</returns>
		 */
		public static string ConvertGlobalVariableTokenToLocal (string _text, int oldGlobalID, int newLocalID)
		{
			if (string.IsNullOrEmpty (_text)) return _text;

			string oldVarToken = "[var:" + oldGlobalID.ToString () + "]";
			if (_text.Contains (oldVarToken))
			{
				string newVarToken = "[localvar:" + newLocalID.ToString () + "]";
				_text = _text.Replace (oldVarToken, newVarToken);
			}

			oldVarToken = "[Var:" + oldGlobalID.ToString () + "]";
			if (_text.Contains (oldVarToken))
			{
				string newVarToken = "[localvar:" + newLocalID.ToString () + "]";
				_text = _text.Replace (oldVarToken, newVarToken);
			}
			return _text;
		}


		/**
		 * <summary>Draws a cube gizmo in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "color">The colour of the cube</param>
		 */
		public static void DrawCubeCollider (Transform transform, Color color)
		{
			if (transform.GetComponent <BoxCollider2D>())
			{
				BoxCollider2D _boxCollider2D = transform.GetComponent <BoxCollider2D>();
				Vector2 pos = _boxCollider2D.offset;

				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.color = color;
				Gizmos.DrawCube (pos, _boxCollider2D.size);
				Gizmos.matrix = Matrix4x4.identity;
			}
			else if (transform.GetComponent <BoxCollider>())
			{
				BoxCollider _boxCollider = transform.GetComponent <BoxCollider>();

				Gizmos.matrix = transform.localToWorldMatrix;
				Gizmos.color = color;
				Gizmos.DrawCube (_boxCollider.center, _boxCollider.size);
				Gizmos.matrix = Matrix4x4.identity;
			}
		}


		/**
		 * <summary>Draws a box gizmo in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "color">The colour of the box</param>
		 */
		public static void DrawBoxCollider (Transform transform, Color color)
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = color;
			Gizmos.DrawLine (new Vector3 (-0.5f, -0.5f), new Vector3 (-0.5f, 0.5f));
			Gizmos.DrawLine (new Vector3 (-0.5f, 0.5f), new Vector3 (0.5f, 0.5f));
			Gizmos.DrawLine (new Vector3 (0.5f, 0.5f), new Vector3 (0.5f, -0.5f));
			Gizmos.DrawLine (new Vector3 (0.5f, -0.5f), new Vector3 (-0.5f, -0.5f));
		}


		/**
		 * <summary>Draws an outline of a Polygon Collider 2D in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "poly">The Polygon Collider 2D</param>
		 * <param name = "color">The colour of the outline</param>
		 */
		public static void DrawPolygonCollider (Transform transform, PolygonCollider2D poly, Color color)
		{
			Gizmos.color = color;
			Gizmos.DrawLine (transform.TransformPoint (poly.points [0]), transform.TransformPoint (poly.points [poly.points.Length-1]));
			for (int i=0; i<poly.points.Length-1; i++)
			{
				Gizmos.DrawLine (transform.TransformPoint (poly.points [i]), transform.TransformPoint (poly.points [i+1]));
			}
		}


		/**
		 * <summary>Draws an outline of a 3D Mesh in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "mesh">The Mesh to draw</param>
		 * <param name = "color">The colour of the mesh</param>
		 */
		public static void DrawMeshCollider (Transform transform, Mesh mesh, Color color)
		{
			if (mesh)
			{
				Gizmos.color = color;
				Gizmos.DrawMesh (mesh, 0, transform.position, transform.rotation, transform.lossyScale);
			}
		}


		/**
		 * <summary>Draws a sphere in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "sphereCollider">The SphereCollider to use as a reference</param>
		 * <param name = "color">The colour to draw with</param>
		 */
		public static void DrawSphereCollider (Transform transform, SphereCollider sphereCollider, Color color)
		{
			if (sphereCollider)
			{
				Gizmos.color = color;
				Vector3 centre = transform.TransformPoint (sphereCollider.center);
				float minTransformSize = Mathf.Max (transform.lossyScale.x, transform.lossyScale.y);
				minTransformSize = Mathf.Max (minTransformSize, transform.lossyScale.z);
				Gizmos.DrawSphere (centre, minTransformSize * sphereCollider.radius);
			}
		}


		/**
		 * <summary>Draws a capsule in the Scene window.</summary>
		 * <param name = "transform">The transform of the object to draw around</param>
		 * <param name = "centre">The capsule's centre</param>
		 * <param name="radius">The capsule's radius</param>
		 * <param name="height">The capsule's height</param>
		 * <param name = "color">The colour to draw with</param>
		 */
		public static void DrawCapsule (Transform transform, Vector3 centre, float radius, float height, Color color)
		{
			Vector3 _pos = transform.TransformPoint (centre);
			Quaternion _rot = transform.rotation;

			Handles.color = color;
			Matrix4x4 angleMatrix = Matrix4x4.TRS (_pos, _rot, Handles.matrix.lossyScale);
			using (new Handles.DrawingScope (angleMatrix))
			{
				float _radius = radius * transform.lossyScale.x;
				float _height = height * transform.lossyScale.x;

				var pointOffset = (_height - (_radius * 2)) / 2;

				Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, _radius);
				Handles.DrawLine(new Vector3(0, pointOffset, -_radius), new Vector3(0, -pointOffset, -_radius));
				Handles.DrawLine(new Vector3(0, pointOffset, _radius), new Vector3(0, -pointOffset, _radius));
				Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, _radius);
				
				Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, _radius);
				Handles.DrawLine(new Vector3(-_radius, pointOffset, 0), new Vector3(-_radius, -pointOffset, 0));
				Handles.DrawLine(new Vector3(_radius, pointOffset, 0), new Vector3(_radius, -pointOffset, 0));
				Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, _radius);
				
				Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, _radius);
				Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, _radius);

			}
		}


		/**
		 * <summary>Locates an object with a supplied ConstantID number (Unity Editor only).
		 * If the object is not found in the current scene, all scenes in the Build Settings will be searched.
		 * Once an object is found, it will be pinged in the Hierarchy window.</summary>
		 * <param name = "_constantID">The ConstantID number of the object to find</param>
		 */
		public static void FindObjectWithConstantID (int _constantID)
		{
			string originalScene = UnityVersionHandler.GetCurrentSceneName ();
			
			if (Application.isPlaying)
			{
				Debug.LogWarning ("Cannot locate file while in Play Mode.");
				return;
			}

			if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
			{
				// Search scene files for ID
				string[] sceneFiles = GetSceneFiles ();
				foreach (string sceneFile in sceneFiles)
				{
					UnityVersionHandler.OpenScene (sceneFile);

					ConstantID[] idObjects = FindObjectsOfType (typeof (ConstantID)) as ConstantID[];
					if (idObjects != null && idObjects.Length > 0)
					{
						foreach (ConstantID idObject in idObjects)
						{
							if (idObject.constantID == _constantID)
							{
								ACDebug.Log ("Found Constant ID: " + _constantID + " on '" + idObject.gameObject.name + "' in scene: " + sceneFile, idObject.gameObject);
								EditorGUIUtility.PingObject (idObject.gameObject);
								EditorGUIUtility.ExitGUI ();
								return;
							}
						}
					}
				}
				
				ACDebug.LogWarning ("Cannot find object with Constant ID: " + _constantID);
				UnityVersionHandler.OpenScene (originalScene);
			}
		}
		

		/**
		 * <summary>Returns all scene filenames listed in the Build Settings (Unity Editor only).</summary>
		 * <returns>An array of scene filenames as strings</returns>
		 */
		public static string[] GetSceneFiles ()
		{
			List<string> temp = new List<string>();
			foreach (UnityEditor.EditorBuildSettingsScene S in UnityEditor.EditorBuildSettings.scenes)
			{
				#if AC_SearchAllScenes
				temp.Add(S.path);
				#else
				if (S.enabled)
				{
					temp.Add(S.path);
				}
				#endif
			}
			
			return temp.ToArray();
		}


		/**
		 * <summary>Generates a Global Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected global variable's ID number</param>
		 * <returns>The newly-selected global variable's ID number</returns>
		 */
		public static int GlobalVariableGUI (string label, int variableID, string tooltip = "")
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
			{
				VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;

				// Create a string List of the field's names (for the PopUp box)
				List<string> labelList = new List<string>();
				
				int i = 0;
				int variableNumber = -1;

				if (variablesManager.vars.Count > 0)
				{
					foreach (GVar _var in variablesManager.vars)
					{
						labelList.Add (_var.label);
						
						// If a GlobalVar variable has been removed, make sure selected variable is still valid
						if (_var.id == variableID)
						{
							variableNumber = i;
						}
						
						i++;
					}
					
					if (variableNumber == -1)
					{
						// Wasn't found (variable was deleted?), so revert to zero
						if (variableID > 0) ACDebug.LogWarning ("Previously chosen variable no longer exists!");
						variableNumber = 0;
						variableID = 0;
					}

					variableNumber = CustomGUILayout.Popup (label, variableNumber, labelList.ToArray (), "", tooltip);
					variableID = variablesManager.vars [variableNumber].id;
				}
				else
				{
					EditorGUILayout.HelpBox ("No global variables exist!", MessageType.Info);
					variableID = -1;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No Variables Manager exists!", MessageType.Info);
				variableID = -1;
			}

			return variableID;
		}


		/**
		 * <summary>Generates a Global Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected global variable's ID number</param>
		 * <param name = "variableType">The variable type to restrict choices to</param>
		 * <returns>The newly-selected global variable's ID number</returns>
		 */
		public static int GlobalVariableGUI (string label, int variableID, VariableType variableType, string tooltip = "")
		{
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager)
			{
				return VariableGUI (label, variableID, variableType, VariableLocation.Global, AdvGame.GetReferences ().variablesManager.vars, tooltip);
			}
			return variableID;
		}


		/**
		 * <summary>Generates a Global Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected global variable's ID number</param>
		 * <param name = "variableTypes">An array variable types to restrict choices to</param>
		 * <returns>The newly-selected global variable's ID number</returns>
		 */
		public static int GlobalVariableGUI (string label, int variableID, VariableType[] variableTypes, string tooltip = "")
		{
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().variablesManager)
			{
				return VariableGUI (label, variableID, variableTypes, VariableLocation.Global, AdvGame.GetReferences ().variablesManager.vars, tooltip);
			}
			return variableID;
		}


		/**
		 * <summary>Generates a Local Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected local variable's ID number</param>
		 * <param name = "variableType">The variable type to restrict choices to</param>
		 * <returns>The newly-selected local variable's ID number</returns>
		 */
		public static int LocalVariableGUI (string label, int variableID, VariableType variableType)
		{
			if (KickStarter.localVariables)
			{
				return VariableGUI (label, variableID, variableType, VariableLocation.Local, KickStarter.localVariables.localVars);
			}
			return variableID;
		}


		/**
		 * <summary>Generates a Local Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected local variable's ID number</param>
		 * <param name = "variableTypes">An array variable types to restrict choices to</param>
		 * <returns>The newly-selected local variable's ID number</returns>
		 */
		public static int LocalVariableGUI (string label, int variableID, VariableType[] variableTypes)
		{
			if (KickStarter.localVariables)
			{
				return VariableGUI (label, variableID, variableTypes, VariableLocation.Local, KickStarter.localVariables.localVars);
			}
			return variableID;
		}


		/**
		 * <summary>Generates a Component Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected local variable's ID number</param>
 		 * <param name = "variableType">The variable type to restrict choices to</param>
		 * <param name = "variables">The Variables component that contains the variable</param>
		 * <returns>The newly-selected local variable's ID number</returns>
		 */
		public static int ComponentVariableGUI (string label, int variableID, VariableType variableType, Variables variables)
		{
			if (variables)
			{
				return VariableGUI (label, variableID, variableType, VariableLocation.Component, variables.vars);
			}
			return variableID;
		}


		/**
		 * <summary>Generates a Component Variable selector GUI (Unity Editor only).</summary>
		 * <param name = "label">The label of the popup GUI</param>
		 * <param name = "variableID">The currently-selected local variable's ID number</param>
		 * <param name = "variableTypes">An array variable types to restrict choices to</param>
		 * <param name = "variables">The Variables component that contains the variable</param>
		 * <returns>The newly-selected local variable's ID number</returns>
		 */
		public static int ComponentVariableGUI (string label, int variableID, VariableType[] variableTypes, Variables variables)
		{
			if (variables)
			{
				return VariableGUI (label, variableID, variableTypes, VariableLocation.Component, variables.vars);
			}
			return variableID;
		}


		private static int VariableGUI (string label, int variableID, VariableType variableType, VariableLocation variableLocation, List<GVar> vars, string tooltip = "")
		{
			VariableType[] variableTypes = new VariableType[1];
			variableTypes[0] = variableType;

			return VariableGUI (label, variableID, variableTypes, variableLocation, vars, tooltip);
		}


		private static int VariableGUI (string label, int variableID, VariableType[] variableTypes, VariableLocation variableLocation, List<GVar> vars, string tooltip = "")
		{
			if (vars != null && vars.Count > 0)
			{
				int variableNumber = 0;

				List<PopupSelectData> popupSelectDataList = new List<PopupSelectData>();
				for (int i=0; i<vars.Count; i++)
				{
					bool foundVarType = false;

					foreach (VariableType variableType in variableTypes)
					{
						if (!foundVarType && vars[i].type == variableType)
						{
							foundVarType = true;

							PopupSelectData popupSelectData = new PopupSelectData (vars[i].id, vars[i].label, i);
							popupSelectDataList.Add (popupSelectData);

							if (popupSelectData.ID == variableID)
							{
								variableNumber = popupSelectDataList.Count-1;
							}
						}
					}
				}

				List<string> labelList = new List<string>();
				foreach (PopupSelectData popupSelectData in popupSelectDataList)
				{
					labelList.Add (popupSelectData.EditorLabel);
				}

				if (labelList.Count > 0)
				{
					variableNumber = CustomGUILayout.Popup (label, variableNumber, labelList.ToArray (), string.Empty, tooltip);
					int rootIndex = popupSelectDataList[variableNumber].rootIndex;
					variableID = vars [rootIndex].id;
				}
				else
				{
					if (variableTypes.Length > 0)
					{
						string typesLabel = string.Empty;
						for (int i=0; i<variableTypes.Length; i++)
						{
							typesLabel += variableTypes[i].ToString ();
							if (i < (variableTypes.Length - 1))
							{
								typesLabel += "', '";
							}
						}

						EditorGUILayout.HelpBox ("No variables of the type '" + typesLabel + "' exist!", MessageType.Info);
					}
					variableID = -1;
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No " + variableLocation.ToString () + " variables found!", MessageType.Info);
				variableID = -1;
			}

			return variableID;
		}
		

		/**
		 * <summary>Draws a curve between two Actions in the ActionList Editor window (Unity Editor only).</summary>
		 * <param name = "start">The Rect of the Action to draw from</param>
		 * <param name = "end">The Rect of the Action to draw to</param>
		 * <param name = "color">The colour of the curve</param>
		 * <param name = "offset">How far the line should be offset along the rect</param>
		 * <param name = "onSide">True if the curve should begin on the side of the Action</param>
		 * <param name = "isDisplayed">True if the Action to draw from is expanded</param>
		 */
		public static void DrawNodeCurve (Rect start, Rect end, Color color, int offset, bool onSide, bool isDisplayed)
		{
			bool arrangeVertically = true;
			if (AdvGame.GetReferences ().actionsManager && AdvGame.GetReferences ().actionsManager.displayActionsInEditor == DisplayActionsInEditor.ArrangedHorizontally)
			{
				arrangeVertically = false;
			}

			float endOffset = 0f;
			if (onSide)
			{
				endOffset = ((float) offset)/6f;
				if (endOffset > 143f) endOffset = 143f;

				if (!arrangeVertically)
				{
					if (endOffset > end.height - 12f) endOffset = end.height - 12f;
				}
			}


			Color originalColor = GUI.color;
			GUI.color = color;

			if (arrangeVertically)
			{
				Vector2 endPos = new Vector2 (end.x + end.width / 2f + endOffset, end.y - 8);
				DrawNodeCurve (start, endPos, color, offset, onSide, false, isDisplayed);
				Texture2D arrow = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/Textures/node-arrow.png", typeof (Texture2D));
				GUI.Label (new Rect (endPos.x-5, endPos.y-4, 12, 16), arrow, "Label");
			}
			else
			{
				Vector2 endPos = new Vector2 (end.x - 8f, end.y + 10 + endOffset);
				DrawNodeCurve (start, endPos, color, offset, onSide, true, isDisplayed);
				Texture2D arrow = (Texture2D) AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Graphics/Textures/node-arrow-side.png", typeof (Texture2D));
				GUI.Label (new Rect (endPos.x-4, endPos.y-7, 16, 12), arrow, "Label");
			}

			GUI.color = originalColor;
		}
		
		
		/**
		 * <summary>Draws a curve between two Actions in the ActionList Editor window (Unity Editor only).</summary>
		 * <param name = "start">The Rect of the Action to draw from</param>
		 * <param name = "end">The point to draw to</param>
		 * <param name = "color">The colour of the curve</param>
		 * <param name = "offset">How far the line should be offset along the rect</param>
		 * <param name = "fromSide">True if the curve should begin on the side of the Action</param>
		 * <param name = "toSide">True if the curve should end on the side of the Action</param>
		 * <param name = "isDisplayed">True if the Action to draw from is expanded</param>
		 */
		public static void DrawNodeCurve (Rect start, Vector2 end, Color color, int offset, bool fromSide, bool toSide, bool isDisplayed)
		{
			Vector3 endPos = new Vector3(end.x, end.y - 1, 0);

			if (fromSide)
			{
				if (!isDisplayed)
				{
					offset = 0;
				}
				Vector3 startPos = new Vector3(start.x + start.width + 10, start.y + start.height - offset - 4, 0);
				if (!isDisplayed)
				{
					startPos.x -= 10;
				}
				float dist = Mathf.Abs (startPos.y - endPos.y);

				Vector3 startTan = startPos + Vector3.right * Mathf.Min (Mathf.Abs (startPos.x - endPos.x), 200f) / 2f;

				if (toSide)
				{
					Vector3 endTan = endPos + Vector3.left * Mathf.Min (dist, 200) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
				else
				{
					Vector3 endTan = endPos + Vector3.down * Mathf.Min (dist, 200) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
			}
			else
			{
				Vector3 startPos = new Vector3(start.x + start.width / 2f, start.y + start.height + offset + 2, 0);
				float dist = Mathf.Abs (startPos.y - endPos.y);
				Vector3 startTan = startPos + Vector3.up * Mathf.Min (dist, 200f) / 2f;
				if (endPos.y < startPos.y && endPos.x <= startPos.x && !toSide)
				{
					startTan.x -= Mathf.Min (dist, 200f) / 2f;
				}

				if (toSide)
				{
					Vector3 endTan = endPos + Vector3.left * Mathf.Min (dist, 200f) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
				else
				{
					Vector3 endTan = endPos + Vector3.down * Mathf.Min (dist, 200f) / 2f;
					Handles.DrawBezier (startPos, endPos, startTan, endTan, color, adLineTex, 3);
				}
			}
		}


		private static Texture2D adLineTex
		{
			get
			{
				if (!_aaLineTex)
				{
					_aaLineTex = new Texture2D(1, 3, TextureFormat.ARGB32, true);
					_aaLineTex.SetPixel(0, 0, new Color(1, 1, 1, 0));
					_aaLineTex.SetPixel(0, 1, Color.white);
					_aaLineTex.SetPixel(0, 2, new Color(1, 1, 1, 0));
					_aaLineTex.Apply();
				}
				return _aaLineTex;
			}
		}


		public static LayerMask LayerMaskField (string label, LayerMask layerMask, string tooltip = "")
		{
			List<int> layerNumbers = new List<int>();
			string[] layers = UnityEditorInternal.InternalEditorUtility.layers;
			
			for (int i = 0; i < layers.Length; i++)
				layerNumbers.Add(LayerMask.NameToLayer(layers[i]));
			
			int maskWithoutEmpty = 0;
			for (int i = 0; i < layerNumbers.Count; i++)
			{
				if (((1 << layerNumbers[i]) & layerMask.value) > 0)
					maskWithoutEmpty |= (1 << i);
			}
			
			maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField (new GUIContent (label, tooltip), maskWithoutEmpty, layers);
			
			int mask = 0;
			for (int i = 0; i < layerNumbers.Count; i++)
			{
				if ((maskWithoutEmpty & (1 << i)) > 0)
					mask |= (1 << layerNumbers[i]);
			}
			layerMask.value = mask;
			
			return layerMask;
		}

		#endif


		/**
		 * <summary>Returns the vector between two world-space points when converted to screen-space.</summary?
		 * <param name = "originWorldPosition">The first point in world-space</param>
		 * <param name = "targetWorldPosition">The second point in world-space<param>
		 * <returns>The vector between the two points in screen-space</returns>
		 */
		public static Vector3 GetScreenDirection (Vector3 originWorldPosition, Vector3 targetWorldPosition)
		{
			Vector3 originScreenPosition = KickStarter.CameraMain.WorldToScreenPoint (originWorldPosition);
			Vector3 targetScreenPosition = KickStarter.CameraMain.WorldToScreenPoint (targetWorldPosition);
			
			Vector3 lookVector = targetScreenPosition - originScreenPosition;
			lookVector.z = lookVector.y;
			lookVector.y = 0;
			
			return (lookVector);
		}


		/**
		 * <summary>Returns the percieved point on a NavMesh of a world-space position, when viewed through screen-space.</summary>
		 * <param name = "targetWorldPosition">The position in world-space<param>
		 * <returns>The point on the NavMesh that the position lies when viewed through screen-space</returns>
		 */
		public static Vector3 GetScreenNavMesh (Vector3 targetWorldPosition)
		{
			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;

			Vector3 targetScreenPosition = KickStarter.CameraMain.WorldToScreenPoint (targetWorldPosition);
			Ray ray = KickStarter.CameraMain.ScreenPointToRay (targetScreenPosition);
			RaycastHit hit = new RaycastHit ();

			if (settingsManager && Physics.Raycast (ray, out hit, settingsManager.navMeshRaycastLength, 1 << LayerMask.NameToLayer (settingsManager.navMeshLayer)))
			{
				return hit.point;
			}

			return targetWorldPosition;
		}


		/**
		 * <summary>Sets the vanishing point of a perspective-locked camera.</summary>
		 * <param name = "_camera">The Camera to affect</param>
		 * <param name = "perspectiveOffset">The offset from the perspective's centre</param>
		 * <param name = "accountForFOV">If True, then the Camera's FOV will be accounted for</param>
		 * <returns>A Matrix4x4 of the corrected perspective</returns>
		 */
		public static Matrix4x4 SetVanishingPoint (Camera _camera, Vector2 perspectiveOffset, bool accountForFOV = false)
		{
			Matrix4x4 m = _camera.projectionMatrix;
			float w = 2f * _camera.nearClipPlane / m.m00;
			float h = 2f * _camera.nearClipPlane / m.m11;
			
			float left = -(w / 2) + perspectiveOffset.x;
			float right = left + w;
			float bottom = -(h / 2) + perspectiveOffset.y;
			float top = bottom + h;
			
			Matrix4x4 projectionMatrix = PerspectiveOffCenter (left, right, bottom, top, _camera.nearClipPlane, _camera.farClipPlane);

			if (accountForFOV)
			{
				float matrixY = 1f / Mathf.Tan (_camera.fieldOfView / ( 2f * Mathf.Rad2Deg ) );
				float matrixX = matrixY / _camera.aspect;
			 
			    projectionMatrix[ 0, 0 ] = matrixX;
				projectionMatrix[ 1, 1 ] = matrixY;
			}

			return projectionMatrix;
		}
		
		
		private static Matrix4x4 PerspectiveOffCenter (float left, float right, float bottom, float top, float near, float far)
		{
			float x =  (2f * near) / (right - left);
			float y =  (2f * near) / (top - bottom);
			float a =  (right + left) / (right - left);
			float b =  (top + bottom) / (top - bottom);
			float c = -(far + near) / (far - near);
			float d = -(2f * far * near) / (far - near);
			float e = -1f;
			
			Matrix4x4 m = new Matrix4x4();
			m[0,0] = x;		m[0,1] = 0f;	m[0,2] = a;		m[0,3] = 0f;
			m[1,0] = 0f;	m[1,1] = y;		m[1,2] = b;		m[1,3] = 0f;
			m[2,0] = 0f;	m[2,1] = 0f;	m[2,2] = c;		m[2,3] =   d;
			m[3,0] = 0f;	m[3,1] = 0f;	m[3,2] = e;		m[3,3] = 0f;
			return m;
		}
		

		/**
		 * <summary>Generates a unique name for a GameObject by adding numbers to the end of it.</summary>
		 * <param name = "name">The original name of the GameObject</param>
		 * <returns>A unique name for the GameObject</retuns>
		 */
		public static string UniqueName (string name)
		{
			if (GameObject.Find (name))
			{
				string newName = name;
				
				for (int i=2; i<20; i++)
				{
					newName = name + i.ToString ();
					
					if (!GameObject.Find (newName))
					{
						break;
					}
				}
				
				return newName;
			}
			else
			{
				return name;
			}
		}


		/**
		 * <summary>Generates a Rect from a square.</summary>
		 * <param name = "centre_x">The centre of the square in the x-direction</param>
		 * <param name = "centre_y">The centre of the square in the y-direction</param>
		 * <param name = "size">The size of the square</param>
		 * <returns>The generated Rect</returns>
		 */
		public static Rect GUIBox (float centre_x, float centre_y, float size)
		{
			Rect newRect;
			newRect = GUIRect (centre_x, centre_y, size, size);
			return (newRect);
		}
		
		
		/**
		 * <summary>Generates a Rect from a square.</summary>
		 * <param name = "posVector">The top-left corner of the square</param>
		 * <param name = "size">The size of the square</param>
		 * <returns>The generated Rect</returns>
		 */
		public static Rect GUIBox (Vector2 posVector, float size)
		{
			return GUIRect (posVector.x / ACScreen.width, ( ACScreen.height - posVector.y) / ACScreen.height, size, size);
		}


		/**
		 * <summary>Generates a Rect from a rectangle.</summary>
		 * <param name = "centre_x">The centre of the rectangle in the x-direction</param>
		 * <param name = "centre_y">The centre of the rectangle in the y-direction</param>
		 * <param name = "width">The width of the rectangle</param>
		 * <param name = "height">The height of the rectangle</param>
		 * <returns>The generated Rect</returns>
		 */
		public static Rect GUIRect (float centre_x, float centre_y, float width, float height)
		{
			Rect newRect;
			newRect = new Rect ( ACScreen.width * centre_x - ( ACScreen.width * width)/2, ACScreen.height * centre_y - ( ACScreen.width * height)/2, ACScreen.width * width, ACScreen.width * height);
			return (newRect);
		}


		private static void AddAnimClip (Animation _animation, int layer, AnimationClip clip, AnimationBlendMode blendMode, WrapMode wrapMode, Transform mixingBone)
		{
			if (clip != null && _animation != null)
			{
				// Initialises a clip
				string clipName = clip.name;
				_animation.AddClip (clip, clipName);
				
				if (mixingBone != null)
				{
					_animation [clipName].AddMixingTransform (mixingBone);
				}
				
				// Set up the state
				if (_animation [clip.name])
				{
					_animation [clipName].layer = layer;
					_animation [clipName].normalizedTime = 0f;
					_animation [clipName].blendMode = blendMode;
					_animation [clipName].wrapMode = wrapMode;
					_animation [clipName].enabled = true;
				}
			}
		}
		

		/**
		 * <summary>Initialises and plays a legacy AnimationClip on an Animation component, starting from a set point.</summary>
		 * <param name = "_animation">The Animation component</param>
		 * <param name = "layer">The layer to play the animation on</param>
		 * <param name = "clip">The AnimatonClip to play</param>
		 * <param name = "blendMode">The animation's AnimationBlendMode</param>
		 * <param name = "wrapMode">The animation's WrapMode</param>
		 * <param name = "fadeTime">The transition time to the new animation</param>
		 * <param name = "mixingBone">The transform to set as the animation's mixing transform</param>
		 * <param name = "normalisedFrame">How far along the timeline the animation should start from (0 to 1)</param>
		 */
		public static void PlayAnimClipFrame (Animation _animation, int layer, AnimationClip clip, AnimationBlendMode blendMode, WrapMode wrapMode, float fadeTime, Transform mixingBone, float normalisedFrame)
		{
			if (clip != null)
			{
				AddAnimClip (_animation, layer, clip, blendMode, wrapMode, mixingBone);
				_animation [clip.name].normalizedTime = normalisedFrame;
				_animation [clip.name].speed *= 1f;
				_animation.Play (clip.name);
				CleanUnusedClips (_animation);
			}
		}
		
		
		/**
		 * <summary>Initialises and plays a legacy AnimationClip on an Animation component.</summary>
		 * <param name = "_animation">The Animation component</param>
		 * <param name = "layer">The layer to play the animation on</param>
		 * <param name = "clip">The AnimatonClip to play</param>
		 * <param name = "blendMode">The animation's AnimationBlendMode</param>
		 * <param name = "wrapMode">The animation's WrapMode</param>
		 * <param name = "fadeTime">The transition time to the new animation</param>
		 * <param name = "mixingBone">The transform to set as the animation's mixing transform</param>
		 * <param name = "reverse">True if the animation should be reversed</param>
		 */
		public static void PlayAnimClip (Animation _animation, int layer, AnimationClip clip, AnimationBlendMode blendMode = AnimationBlendMode.Blend, WrapMode wrapMode = WrapMode.ClampForever, float fadeTime = 0f, Transform mixingBone = null, bool reverse = false)
		{
			if (clip != null)
			{
				AddAnimClip (_animation, layer, clip, blendMode, wrapMode, mixingBone);
				if (reverse)
				{
					_animation[clip.name].speed *= -1f;
				}
				_animation.CrossFade (clip.name, fadeTime);
				CleanUnusedClips (_animation);
			}
		}
		

		/**
		 * <summary>Cleans the supplied Animation component of any clips not being played.</summary>
		 * <param name = "_animation">The Animation component to clean</param>
		 */
		public static void CleanUnusedClips (Animation _animation)
		{
			// Remove any non-playing animations
			List <string> removeClips = new List <string>();

			foreach (AnimationState state in _animation)
			{
				if (!_animation [state.name].enabled)
				{
					// Queued animations get " - Queued Clone" appended to it, so remove
					if (state.name.Contains (queuedCloneAnimSuffix))
					{
						removeClips.Add (state.name.Replace (queuedCloneAnimSuffix, string.Empty));
					}
					else
					{
						removeClips.Add (state.name);
					}
				}
			}
			
			foreach (string _clip in removeClips)
			{
				_animation.RemoveClip (_clip);
			}
		}
		private static string queuedCloneAnimSuffix = " - Queued Clone";
		

		/**
		 * <summary>Lerps from one float to another over time.</summary>
		 * <param name = "from">The initial value</param>
		 * <param name = "to">The final value</param>
		 * <param name = "t">The time value.  If greater than 1, the result will overshoot the final value. If less than 1, the result will undershoot the initial value</param>
		 * <returns>The lerped float</returns>
		 */
		public static float Lerp (float from, float to, float t)
		{
			if (t < 0 || t > 1)
			{
				return from + (to-from)*t;
			}
			return Mathf.Lerp (from, to, t);
		}
		
		
		/**
		 * <summary>Lerps from one Vector3 to another over time.</summary>
		 * <param name = "from">The initial value</param>
		 * <param name = "to">The final value</param>
		 * <param name = "t">The time value.  If greater than 1, the result will overshoot the final value. If less than 1, the result will undershoot the initial value</param>
		 * <returns>The lerped Vector3</returns>
		 */
		public static Vector3 Lerp (Vector3 from, Vector3 to, float t)
		{
			if (t < 0 || t > 1)
			{
				return from + (to-from)*t;
			}
			return Vector3.Lerp (from, to, t);
		}
		

		/**
		 * <summary>Lerps from one Quaternion to another over time.</summary>
		 * <param name = "from">The initial value</param>
		 * <param name = "to">The final value</param>
		 * <param name = "t">The time value.  If greater than 1, the result will overshoot the final value. If less than 1, the result will undershoot the initial value</param>
		 * <returns>The lerped Quaternion</returns>
		 */
		public static Quaternion Lerp (Quaternion from, Quaternion to, float t)
		{
			if (t < 0 || t > 1)
			{
				Vector3 fromVec = from.eulerAngles;
				Vector3 toVec = to.eulerAngles;
				
				if (fromVec.x - toVec.x > 180f)
				{
					toVec.x -= 360f;
				}
				else if (fromVec.x - toVec.x > 180f)
				{
					toVec.x += 360;
				}
				if (fromVec.y - toVec.y < -180f)
				{
					toVec.y -= 360f;
				}
				else if (fromVec.y - toVec.y > 180f)
				{
					toVec.y += 360;
				}
				if (fromVec.z - toVec.z > 180f)
				{
					toVec.z -= 360f;
				}
				else if (fromVec.z - toVec.z > 180f)
				{
					toVec.z += 360;
				}
				
				return Quaternion.Euler (Lerp (fromVec, toVec, t));
			}

			return Quaternion.Lerp (from, to, t);
		}
		

		/**
		 * <summary>Interpolates a float over time, according to various interpolation methods.</summary>
		 * <param name = "startT">The starting time</param>
		 * <param name = "deltaT">The time difference</param>
		 * <param name = "moveMethod">The method of interpolation (Linear, Smooth, Curved, EaseIn, EaseOut, Curved)</param>
		 * <param name = "timeCurve">The AnimationCurve to interpolate against, if the moveMethod = MoveMethod.Curved</param>
		 * <returns>The interpolated float</returns>
		 */
		public static float Interpolate (float startT, float deltaT, MoveMethod moveMethod, AnimationCurve timeCurve = null)
		{
			switch (moveMethod)
			{
				case MoveMethod.Curved:
				case MoveMethod.Smooth:
					return -0.5f * (Mathf.Cos (Mathf.PI * (Time.time - startT) / deltaT) - 1f);

				case MoveMethod.EaseIn:
					return 1f - Mathf.Cos ((Time.time - startT) / deltaT * (Mathf.PI / 2));

				case MoveMethod.EaseOut:
					return Mathf.Sin ((Time.time - startT) / deltaT * (Mathf.PI / 2));

				case MoveMethod.CustomCurve:
					if (timeCurve == null || timeCurve.length == 0)
					{
						return 1f;
					}
					float startTime = timeCurve [0].time;
					float endTime = timeCurve [timeCurve.length - 1].time;
					return timeCurve.Evaluate ((endTime - startTime) * (Time.time - startT) / deltaT + startTime);

				case MoveMethod.Linear:
				default:
					return ((Time.time - startT) / deltaT);
			}
		}


		public static float Interpolate (float weight, MoveMethod moveMethod, AnimationCurve timeCurve = null)
		{
			switch (moveMethod)
			{
				case MoveMethod.Curved:
				case MoveMethod.Smooth:
					return -0.5f * (Mathf.Cos (Mathf.PI * weight) - 1f);

				case MoveMethod.EaseIn:
					return 1f - Mathf.Cos (weight * (Mathf.PI / 2));

				case MoveMethod.EaseOut:
					return Mathf.Sin (weight * (Mathf.PI / 2));

				case MoveMethod.CustomCurve:
					if (timeCurve == null || timeCurve.length == 0)
					{
						return 1f;
					}
					float startTime = timeCurve [0].time;
					float endTime = timeCurve [timeCurve.length - 1].time;
					return timeCurve.Evaluate ((endTime - startTime) * weight + startTime);

				case MoveMethod.Linear:
				default:
					return weight;
			}
		}
		

		/**
		 * <summary>Draws GUI text with an outline and/or shadow.</summary>
		 * <param name = "rect">The Rect of the GUI text</param>
		 * <param name = "text">The text itself</param>
		 * <param name = "style">The GUIStyle that the GUI text uses</param>
		 * <param name = "outColour">The colour of the text's outline/shadow</param>
		 * <param name = "inColour">The colour of the text itself</param>
		 * <param name = "size">The size of the text</param>
		 * <param name = "textEffects">The type of text effect (Outline, Shadow, OutlineAndShadow, None)</param>
		 */
		public static void DrawTextEffect (Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size, TextEffects textEffects)
		{
			if (AdvGame.GetReferences ().menuManager && AdvGame.GetReferences ().menuManager.scaleTextEffects)
			{
				size = ACScreen.safeArea.width / 200f / size;
			}

			int i=0;
			string effectText = text;

			if (effectText != null)
			{
				while (i < text.Length && text.IndexOf ("<color=", i) >= 0)
				{
					int startPos = effectText.IndexOf ("<color=", i);
					int endPos = 0;
					if (effectText.IndexOf (">", startPos) > 0)
					{
						endPos = effectText.IndexOf (">", startPos);
					}

					if (endPos > 0)
					{
						effectText = effectText.Substring (0, startPos) + "<color=black>" + effectText.Substring (endPos + 1);
					}

					i = startPos + 1;
				}

				switch (textEffects)
				{
					case TextEffects.Outline:
						DrawTextOutline (rect, text, style, outColor, inColor, size, effectText);
						break;

					case TextEffects.OutlineAndShadow:
						DrawTextOutline (rect, text, style, outColor, inColor, size, effectText);
						DrawTextShadow (rect, text, style, outColor, inColor, size, effectText);
						break;

					case TextEffects.Shadow:
						DrawTextShadow (rect, text, style, outColor, inColor, size, effectText);
						break;

					default:
						break;
				}
			}
		}
		
		
		private static void DrawTextShadow (Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size, string effectText = "")
		{
			GUIStyle backupStyle = new GUIStyle (style);
			Color backupColor = GUI.color;

			if (effectText.Length == 0)
			{
				effectText = text;
			}

			if (style.normal.background != null)
			{
				GUI.Label (rect, string.Empty, style);
			}
			style.normal.background = null;

			outColor.a *= GUI.color.a;
			style.normal.textColor = outColor;
			GUI.color = outColor;
			
			rect.x += size;
			GUI.Label (rect, effectText, style);
			
			rect.y += size;
			GUI.Label (rect, effectText, style);
			
			rect.x -= size;
			rect.y -= size;
			style.normal.textColor = inColor;
			GUI.color = backupColor;
			GUI.Label (rect, text, style);
			
			style = backupStyle;
		}
		
		
		private static void DrawTextOutline (Rect rect, string text, GUIStyle style, Color outColor, Color inColor, float size, string effectText = "")
		{
			float halfSize = size * 0.5f;
			GUIStyle backupStyle = new GUIStyle (style);
			Color backupColor = GUI.color;

			if (string.IsNullOrEmpty (effectText))
			{
				effectText = text;
			}

			if (style.normal.background != null)
			{
				GUI.Label (rect, string.Empty, style);
			}
			style.normal.background = null;
			outColor.a *= GUI.color.a;
			style.normal.textColor = outColor;
			GUI.color = outColor;
			
			rect.x -= halfSize;
			GUI.Label (rect, effectText, style);

			rect.y -= halfSize;
			GUI.Label (rect, effectText, style);

			rect.x += halfSize;
			GUI.Label (rect, effectText, style);

			rect.x += halfSize;
			GUI.Label (rect, effectText, style);

			rect.y += halfSize;
			GUI.Label (rect, effectText, style);

			rect.y += halfSize;
			GUI.Label (rect, effectText, style);

			rect.x -= halfSize;
			GUI.Label (rect, effectText, style);

			rect.x -= halfSize;
			GUI.Label (rect, effectText, style);

			rect.x += halfSize;
			rect.y -= halfSize;
			style.normal.textColor = inColor;
			GUI.color = backupColor;
			GUI.Label (rect, text, style);
			
			style = backupStyle;
		}


		/*
		 * <summary>Converts any special characters in a string that might conflict with save game file data into temporary replacements.</summary>
		 * <param name = "_string">The original string</param>
		 * <returns>The modified string, ready to be placed in save game file data</returns>
		 */
		public static string PrepareStringForSaving (string _string)
		{
			_string = _string.Replace (SaveSystem.pipe, "*PIPE*");
			_string = _string.Replace (SaveSystem.colon, "*COLON*");
			
			return _string;
		}
		
		
		/*
		 * <summary>Converts temporarily replacements in a string back into special characters that might conflict with save game file data.</summary>
		 * <param name = "_string">The string to convert</param>
		 * <returns>The original string</returns>
		 */
		public static string PrepareStringForLoading (string _string)
		{
			_string = _string.Replace ("*PIPE*", SaveSystem.pipe);
			_string = _string.Replace ("*COLON*", SaveSystem.colon);
			
			return _string;
		}


		/**
		 * <summary>Gets the signed angle between two 2D vectors</summary>
		 * <param name = "from">The first vector</param>
		 * <param name = "to">The second vector</param>
		 * <returns>The signed angle</returns>
		 */
		public static float SignedAngle (Vector2 from, Vector2 to)
        {
            float unsigned_angle = Vector2.Angle (from, to);
            float sign = Mathf.Sign(from.x * to.y - from.y * to.x);
            return unsigned_angle * sign;
        }


		public static Vector3 GetCharLookVector (CharDirection direction, Char _character = null)
		{
			Vector3 camForward = KickStarter.CameraMainTransform.forward;
			camForward = new Vector3 (camForward.x, 0f, camForward.z).normalized;

			if (SceneSettings.IsTopDown ())
			{
				camForward = -KickStarter.CameraMainTransform.forward;
			}
			else if (SceneSettings.CameraPerspective == CameraPerspective.TwoD)
			{
				camForward = KickStarter.CameraMainTransform.up;
			}

			Vector3 camRight = new Vector3 (KickStarter.CameraMainTransform.right.x, 0f, KickStarter.CameraMainTransform.right.z);

			// Angle slightly so that left->right rotations face camera
			if (KickStarter.settingsManager.IsInFirstPerson ())
			{
				// No angle tweaking in first-person
			}
			else if (SceneSettings.CameraPerspective == CameraPerspective.TwoD)
			{
				camRight -= new Vector3 (0f, 0f, 0.01f);
			}
			else
			{
				camRight -= camForward * 0.01f;
			}

			if (_character != null)
			{
				camForward = _character.TransformForward;
				camRight = _character.TransformRight;
			}

			Vector3 lookVector = Vector3.zero;
			switch (direction)
			{
				case CharDirection.Down:
					lookVector = -camForward;
					break;

				case CharDirection.Left:
					lookVector = -camRight;
					break;

				case CharDirection.Right:
					lookVector = camRight;
					break;

				case CharDirection.Up:
					lookVector = camForward;
					break;

				case CharDirection.DownLeft:
					lookVector = (-camForward - camRight).normalized;
					break;

				case CharDirection.DownRight:
					lookVector = (-camForward + camRight).normalized;
					break;

				case CharDirection.UpLeft:
					lookVector = (camForward - camRight).normalized;
					break;

				case CharDirection.UpRight:
					lookVector = (camForward + camRight).normalized;
					break;
			}

			if (SceneSettings.IsTopDown ())
			{
				return lookVector;
			}
			if (SceneSettings.CameraPerspective == CameraPerspective.TwoD && _character == null)
			{
				return new Vector3 (lookVector.x, 0f, lookVector.y).normalized;
			}
			return lookVector;
		}

	}
	
}