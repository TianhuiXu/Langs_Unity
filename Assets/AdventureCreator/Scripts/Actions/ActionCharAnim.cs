/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCharAnim.cs"
 * 
 *	This action is used to control character animation.
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
	public class ActionCharAnim : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		protected AnimEngine editingAnimEngine;

		public bool isPlayer;
		public int playerID = -1;

		public Char animChar;
		protected Char runtimeAnimChar;
		public AnimationClip clip;
		public int clipParameterID = -1;
		public string clip2D;
		public int clip2DParameterID = -1;

		public enum AnimMethodChar { PlayCustom, StopCustom, ResetToIdle, SetStandard };
		public AnimMethodChar method;
		
		public AnimationBlendMode blendMode;
		public AnimLayer layer = AnimLayer.Base;
		public AnimStandard standard;
		public bool includeDirection = false;

		public bool changeSound = false;
		public AudioClip newSound;
		public int newSoundParameterID = -1;
		public int newSpeedParameterID = -1;

		public int layerInt;
		public bool idleAfter = true;
		public bool idleAfterCustom = false;

		public AnimPlayMode playMode;
		public AnimPlayModeBase playModeBase = AnimPlayModeBase.PlayOnceAndClamp;

		public float fadeTime = 0f;

		public bool changeSpeed = false;
		public float newSpeed = 0f;

		public AnimMethodCharMecanim methodMecanim;
		public MecanimCharParameter mecanimCharParameter;
		public MecanimParameterType mecanimParameterType;
		public string parameterName;
		public int parameterNameID = -1;
		public float parameterValue;
		public int parameterValueParameterID = -1;

		public bool hideHead = false;
		public bool doLoop; // Ignored by official animation engines


		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "Animate"; }}
		public override string Description { get { return "Affects a Character's animation. Can play or stop a custom animation, change a standard animation (idle, walk or run), change a footstep sound, or revert the Character to idle."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			newSound = (AudioClip) AssignObject <AudioClip> (parameters, newSoundParameterID, newSound);
			newSpeed = AssignFloat (parameters, newSpeedParameterID, newSpeed);
			parameterName = AssignString (parameters, parameterNameID, parameterName);
			clip2D = AssignString (parameters, clip2DParameterID, clip2D);

			if (isPlayer)
			{
				runtimeAnimChar = AssignPlayer (playerID, parameters, parameterID);
			}
			else
			{
				runtimeAnimChar = AssignFile <Char> (parameters, parameterID, constantID, animChar);
			}

			if (runtimeAnimChar && runtimeAnimChar.GetAnimEngine () != null)
			{
				runtimeAnimChar.GetAnimEngine ().ActionCharAnimAssignValues (this, parameters);
			}
		}

		
		public override float Run ()
		{
			if (runtimeAnimChar != null)
			{
				if (runtimeAnimChar.GetAnimEngine () != null)
				{
					return runtimeAnimChar.GetAnimEngine ().ActionCharAnimRun (this);
				}
				else
				{
					LogWarning ("Could not create animation engine for " + runtimeAnimChar.name, runtimeAnimChar);
				}
			}
			else
			{
				LogWarning ("Could not create animation engine!");
			}

			return 0f;
		}


		public override void Skip ()
		{
			if (runtimeAnimChar != null)
			{
				if (runtimeAnimChar.GetAnimEngine () != null)
				{
					runtimeAnimChar.GetAnimEngine ().ActionCharAnimSkip (this);
				}
			}
		}


		public void ReportWarning (string message, Object context = null)
		{
			LogWarning (message, context);
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					parameterID = ChooseParameterGUI ("Player ID:", parameters, parameterID, ParameterType.Integer);
					if (parameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}

				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					if (parameterID < 0 && playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						animChar = (playerPrefab != null) ? playerPrefab.playerOb : null;
					}
					else
					{
						animChar = KickStarter.settingsManager.GetDefaultPlayer ();
					}
				}
				else if (Application.isPlaying)
				{
					animChar = KickStarter.player;
				}
				else if (AdvGame.GetReferences ().settingsManager)
				{
					animChar = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("Character:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					animChar = null;
				}
				else
				{
					animChar = (Char) EditorGUILayout.ObjectField ("Character:", animChar, typeof (Char), true);
					
					constantID = FieldToID <Char> (animChar, constantID);
					animChar = IDToField <Char> (animChar, constantID, true);
				}
			}

			if (animChar)
			{
				ResetAnimationEngine (animChar.animationEngine, animChar.customAnimationClass);
			}

			if (editingAnimEngine != null)
			{
				editingAnimEngine.ActionCharAnimGUI (this, parameters);

				#if !AC_ActionListPrefabs
				if (GUI.changed && this) EditorUtility.SetDirty (this);
				#endif
			}
			else
			{
				EditorGUILayout.HelpBox ("This Action requires a Character before more options will show.", MessageType.Info);
			}
		}

		
		public override string SetLabel ()
		{
			if (isPlayer)
			{
				return "Player";
			}
			else if (animChar != null)
			{
				return animChar.name;
			}
			return string.Empty;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (isPlayer)
			{
				if (!fromAssetFile && GameObject.FindObjectOfType <Player>() != null)
				{
					animChar = GameObject.FindObjectOfType <Player>();
				}

				if (animChar == null && AdvGame.GetReferences ().settingsManager != null)
				{
					animChar = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
				}
			}

			if (animChar != null)
			{
				ResetAnimationEngine (animChar.animationEngine, animChar.customAnimationClass);

				if (saveScriptsToo && editingAnimEngine && editingAnimEngine.RequiresRememberAnimator (this))
				{
					editingAnimEngine.AddSaveScript (this, animChar.gameObject);
				}

				AssignConstantID <Char> (animChar, constantID, parameterID);
			}
		}


		protected void ResetAnimationEngine (AnimationEngine animationEngine, string customClassName)
		{
			string className = "";
			if (animationEngine == AnimationEngine.Custom)
			{
				className = customClassName;
			}
			else
			{
				className = "AnimEngine_" + animationEngine.ToString ();
			}
				
			if (className != "" && (editingAnimEngine == null || editingAnimEngine.ToString () != className))
			{
				editingAnimEngine = (AnimEngine) ScriptableObject.CreateInstance (className);
			}
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (animChar && animChar.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && parameterID < 0) return true;
			return (parameterID < 0 && playerID == _playerID);
		}

		#endif


		public Char RuntimeAnimChar
		{
			get
			{
				return runtimeAnimChar;
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to play a custom animation using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "clipName">The animation clip name to play</param>
		 * <param name = "addDirectionalSuffix">If True, a directional suffix (e.g. '_R' for right-facing) will be added automatically to the clip name, based on the direction the character is facing</param>
		 * <param name = "layerIndex">The index number on the Animator Controller that the animation clip is located in</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <param name = "returnToIdleAfter">If True, and waitUntilFinish = True, then the character will return to an idle state once the animation is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnity_PlayCustom (AC.Char characterToAnimate, string clipName, bool addDirectionalSuffix = false, int layerIndex = 0, float transitionTime = 0f, bool waitUntilFinish = true, bool returnToIdleAfter = true)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.method = AnimMethodChar.PlayCustom;
			newAction.clip2D = clipName;
			newAction.includeDirection = addDirectionalSuffix;
			newAction.layerInt = layerIndex;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			newAction.idleAfter = returnToIdleAfter;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to change a character's standard animation using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to affect</param>
		 * <param name = "standardToChange">The standard animation type (e.g. Idle, Walk) to change</param>
		 * <param name = "newStandardName">The new name of the standard animation</param>
		 * <param name = "newSound">The new sound to optionally associate with the animation, if walk or run</param>
		 * <param name = "newSpeed">The new speed, if walk or run and greater than zero</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnity_SetStandard (AC.Char characterToAnimate, AnimStandard standardToChange, string newStandardName, AudioClip newSound = null, float newSpeed = 0f)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.method = AnimMethodChar.SetStandard;

			newAction.standard = standardToChange;
			newAction.clip2D = newStandardName;
			if (newSound != null)
			{
				newAction.changeSound = true;
				newAction.newSound = newSound;
			}
			if (newSpeed > 0f)
			{
				newAction.changeSpeed = true;
				newAction.newSpeed = newSpeed;
			}

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to reset a character to idle using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to manipulate</param>
		 * <param name = "waitForCustomAnimationToFinish">If True, then the Action will wait for any currently-playing custom animation to finish before returning the character to idle</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnity_ResetToIdle (AC.Char characterToAnimate, bool waitForCustomAnimationToFinish = false)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.method = AnimMethodChar.ResetToIdle;

			newAction.idleAfterCustom = waitForCustomAnimationToFinish;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to play a custom animation using the 'Sprites Unity Complex' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "clipName">The animation clip name to play</param>
		 * <param name = "addDirectionalSuffix">If True, a directional suffix (e.g. '_R' for right-facing) will be added automatically to the clip name, based on the direction the character is facing</param>
		 * <param name = "layerIndex">The index number on the Animator Controller that the animation clip is located in</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnityComplex_PlayCustom (AC.Char characterToAnimate, string clipName, bool addDirectionalSuffix = false, int layerIndex = 0, float transitionTime = 0f, bool waitUntilFinish = true)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.PlayCustom;

			newAction.clip2D = clipName;
			newAction.includeDirection = addDirectionalSuffix;
			newAction.layerInt = layerIndex;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to invoke an Animator Trigger parameter value using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Trigger parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (AC.Char characterToAnimate, string parameterName)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Trigger;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to change an Animator Integer parameter value using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Integer parameter</param>
		 * <param name = "parameterValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (AC.Char characterToAnimate, string parameterName, int parameterValue)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Int;
			newAction.parameterValue = (int) parameterValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to change an Animator Float parameter value using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Float parameter</param>
		 * <param name = "parameterValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (AC.Char characterToAnimate, string parameterName, float parameterValue)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Float;
			newAction.parameterValue = parameterValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to change an Animator Bool parameter value using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Bool parameter</param>
		 * <param name = "parameterValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (AC.Char characterToAnimate, string parameterName, bool parameterValue)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Bool;
			newAction.parameterValue = (parameterValue) ? 1f : 0f;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to change a character's standard animation using the 'Sprites Unity' engine</summary>
		 * <param name = "characterToAnimate">The character to affect</param>
		 * <param name = "parameterToChange">The Mecanim parameter type (e.g. MoveSpeedFloat) to change</param>
		 * <param name = "newParameterName">The new name of the parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_SpritesUnityComplex_SetStandard (AC.Char characterToAnimate, MecanimCharParameter parameterToChange, string newParameterName)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.SetStandard;

			newAction.mecanimCharParameter = parameterToChange;
			newAction.parameterName = newParameterName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to invoke an Animator Trigger parameter value using the 'Mecanim' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Trigger parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_Mecanim_ChangeParameterValue (AC.Char characterToAnimate, string parameterName)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Trigger;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to update an Animator Integer parameter value using the 'Mecanim' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Integer parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_Mecanim_ChangeParameterValue (AC.Char characterToAnimate, string parameterName, int parameterValue)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Int;
			newAction.parameterValue = (int) parameterValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to update an Animator Float parameter value using the 'Mecanim' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Float parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_Mecanim_ChangeParameterValue (AC.Char characterToAnimate, string parameterName, float parameterValue)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Float;
			newAction.parameterValue = parameterValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to update an Animator Bool parameter value using the 'Mecanim' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "parameterName">The name of the Bool parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_Mecanim_ChangeParameterValue (AC.Char characterToAnimate, string parameterName, bool parameterValue)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.ChangeParameterValue;
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Bool;
			newAction.parameterValue = (parameterValue) ? 1f : 0f;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to change a character's standard animation using the 'Mecanim' engine</summary>
		 * <param name = "characterToAnimate">The character to affect</param>
		 * <param name = "parameterToChange">The Mecanim parameter type (e.g. MoveSpeedFloat) to change</param>
		 * <param name = "newParameterName">The new name of the parameter</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_Mecanim_SetStandard (AC.Char characterToAnimate, MecanimCharParameter parameterToChange, string newParameterName)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.SetStandard;

			newAction.mecanimCharParameter = parameterToChange;
			newAction.parameterName = newParameterName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Animate' Action, set to play a custom animation using the 'Mecanim' engine</summary>
		 * <param name = "characterToAnimate">The character to animate</param>
		 * <param name = "clipName">The animation clip name to play</param>
		 * <param name = "layerIndex">The index number on the Animator Controller that the animation clip is located in</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharAnim CreateNew_Mecanim_PlayCustom (AC.Char characterToAnimate, string clipName, int layerIndex = 0, float transitionTime = 0f, bool waitUntilFinish = true)
		{
			ActionCharAnim newAction = CreateNew<ActionCharAnim> ();
			newAction.animChar = characterToAnimate;
			newAction.TryAssignConstantID (newAction.animChar, ref newAction.constantID);
			newAction.methodMecanim = AnimMethodCharMecanim.PlayCustom;

			newAction.clip2D = clipName;
			newAction.includeDirection = false;
			newAction.layerInt = layerIndex;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

	}

}