/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AnimEngine.cs"
 * 
 *	This script is a base class for the Animation engine scripts.
 *  Create a subclass of name "AnimEngine_NewMethodName" and
 * 	add "NewMethodName" to the AnimationEngine enum to integrate
 * 	a new method into the engine.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A base class for all Animation Engines. Subclasses of this script contain functions to perform standard character animations (e.g "Idle", "Walk", etc) and are called as appropriately by Player / NPC.
	 * It also contains code that alter each of the various animation Actions (e.g. ActionAnim).
	 * To create a new animation engine to control an NPC or Player, create a new subclass of this script and set the character's animationEngine to AnimationEngine.Custom. Then set the character's customAnimationClass string to match the name of the new subclass.
	 */
	[System.Serializable]
	public class AnimEngine : ScriptableObject
	{

		/** How character turning is handled (Linear, RootMotion, Script) */
		public TurningStyle turningStyle = TurningStyle.Script;
		/** If True, then the engine is sprite-based, and character's will rely on their spriteChild for animation */
		public bool isSpriteBased = false;
		/** If True, then the TurnHead method will be called every frame regardless of whether or not the head is looking at something */
		public bool updateHeadAlways { get; protected set; } 

		protected AC.Char character;


		/**
		 * <summary>Initialises the engine.</summary>
		 * <param name = "_character">The Player/NPC that this instance is controlling.</param>
		 */
		public virtual void Declare (AC.Char _character)
		{
			character = _character;
			turningStyle = TurningStyle.Script;
			isSpriteBased = false;
		}

		public virtual void CharSettingsGUI ()
		{}

		public virtual bool IKEnabled
		{
			get
			{
				return false;
			}
		}

		public virtual void CharExpressionsGUI ()
		{}

		public virtual PlayerData SavePlayerData (PlayerData playerData, Player player)
		{
			return playerData;
		}

		public virtual void LoadPlayerData (PlayerData playerData, Player player)
		{}

		public virtual NPCData SaveNPCData (NPCData npcData, NPC npc)
		{
			return npcData;
		}

		public virtual void LoadNPCData (NPCData npcData, NPC npc)
		{}

		public virtual void ActionCharAnimGUI (ActionCharAnim action, List<ActionParameter> parameters = null)
		{
			#if UNITY_EDITOR
			action.method = (ActionCharAnim.AnimMethodChar) EditorGUILayout.EnumPopup ("Method:", action.method);
			#endif
		}

		public virtual void ActionCharAnimAssignValues (ActionCharAnim action, List<ActionParameter> parameters)
		{}

		public virtual float ActionCharAnimRun (ActionCharAnim action)
		{
			return 0f;
		}

		public virtual void ActionCharAnimSkip (ActionCharAnim action)
		{
			ActionCharAnimRun (action);
		}
		
		public virtual bool ActionCharHoldPossible ()
		{
			return false;
		}

		public virtual void ActionSpeechGUI (ActionSpeech action, Char speaker)
		{
			#if UNITY_EDITOR
			#endif
		}
		
		public virtual void ActionSpeechRun (ActionSpeech action)
		{ }

		public virtual void ActionSpeechSkip (ActionSpeech action)
		{
			ActionSpeechRun (action);
		}

		public virtual void ActionAnimGUI (ActionAnim action, List<ActionParameter> parameters)
		{
			#if UNITY_EDITOR
			#endif
		}

		public virtual string ActionAnimLabel (ActionAnim action)
		{
			return string.Empty;
		}

		public virtual void ActionAnimAssignValues (ActionAnim action, List<ActionParameter> parameters)
		{ }
		
		public virtual float ActionAnimRun (ActionAnim action)
		{
			return 0f;
		}

		public virtual void ActionAnimSkip (ActionAnim action)
		{
			ActionAnimRun (action);
		}


		public virtual void ActionCharRenderGUI (ActionCharRender action, List<ActionParameter> parameters)
		{
			ActionCharRenderGUI (action);
		}


		public virtual void ActionCharRenderGUI (ActionCharRender action)
		{ }

		public virtual float ActionCharRenderRun (ActionCharRender action)
		{
			return 0f;
		}

		/**
		 * Plays the character's 'Idle' animation.
		 */
		public virtual void PlayIdle ()
		{ }

		/**
		 * Plays the character's 'Walk' animation.
		 */
		public virtual void PlayWalk ()
		{ }

		/**
		 * Plays the character's 'Run' animation.
		 */
		public virtual void PlayRun ()
		{ }

		/**
		 * Plays the character's 'Talk' animation.
		 */
		public virtual void PlayTalk ()
		{ }

		/**
		 * Called every frame to animate the character based on height change.
		 * The character's height change can be found with GetHeightChange ().
		 */
		public virtual void PlayVertical ()
		{ }

		/**
		 * Plays the character's 'Jump' animation.
		 */
		public virtual void PlayJump ()
		{ 
			PlayIdle ();
		}

		/**
		 * Plays the character's 'Spot-turn left' animation.
		 */
		public virtual void PlayTurnLeft ()
		{
			PlayIdle ();
		}

		/**
		 * Plays the character's 'Spot-turn right' animation.
		 */
		public virtual void PlayTurnRight ()
		{
			PlayIdle ();
		}

		/**
		 * <summary>Rotates a character's head.</summary>
		 * <param name = "angles">The new angles to rotate the head to</param>
		 */
		public virtual void TurnHead (Vector2 angles)
		{}

		/** <summary>Called whenever the character's Expression is changed. It can be read with CurrentExpression.</summary> */
		public virtual void OnSetExpression ()
		{}

		#if UNITY_EDITOR

		public virtual bool RequiresRememberAnimator (ActionCharAnim action)
		{
			return false;
		}


		public virtual bool RequiresRememberAnimator (ActionAnim action)
		{
			return false;
		}

		/**
		 * <summary>Adds any relevent Remember scripts onto a GameObject referenced by an animation-based Action.</summary>
		 * <param name = "_action">The Action referencing the GameObject</param>
		 * <param name = "_gameObject">The GameObject being referenced</param>
		 */
		public virtual void AddSaveScript (Action _action, GameObject _gameObject)
		{ }

		#endif

	}

}