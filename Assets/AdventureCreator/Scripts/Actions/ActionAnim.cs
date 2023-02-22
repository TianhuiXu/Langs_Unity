/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionAnim.cs"
 * 
 *	This action is used for standard animation playback for GameObjects.
 *	It is fairly simplistic, and not meant for characters.
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
	public class ActionAnim : Action
	{

		public int parameterID = -1;
		public int constantID = 0;

		// 3D variables
		
		public Animation _anim;
		public Animation runtimeAnim;
		public AnimationClip clip;
		public float fadeTime = 0f;
		public int clipParameterID = -1;
		
		// 2D variables
		
		public Transform _anim2D;
		public Transform runtimeAnim2D;
		public Animator animator;
		public Animator runtimeAnimator;
		public string clip2D;
		public int clip2DParameterID = -1;
		public enum WrapMode2D { Once, Loop, PingPong };
		public WrapMode2D wrapMode2D;
		public int layerInt;

		// BlendShape variables

		public Shapeable shapeObject;
		public Shapeable runtimeShapeObject;
		public int shapeKey = 0;
		public float shapeValue = 0f;
		public bool isPlayer = false;

		// Mecanim variables

		public AnimMethodMecanim methodMecanim;
		public MecanimParameterType mecanimParameterType;
		public string parameterName;
		public int parameterNameID = -1;
		public float parameterValue;
		public int parameterValueParameterID = -1;

		// Regular variables
		
		public AnimMethod method;
		
		public AnimationBlendMode blendMode = AnimationBlendMode.Blend;
		public AnimPlayMode playMode;
		
		public AnimationEngine animationEngine = AnimationEngine.Legacy;
		public string customClassName;
		public AnimEngine animEngine;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Animate"; }}
		public override string Description { get { return "Causes a GameObject to play or stop an animation, or modify a Blend Shape. The available options will differ depending on the chosen animation engine."; }}
		

		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (animEngine == null)
			{
				ResetAnimationEngine ();
			}
			
			if (animEngine != null)
			{
				animEngine.ActionAnimAssignValues (this, parameters);
			}

			parameterName = AssignString (parameters, parameterNameID, parameterName);
			clip2D = AssignString (parameters, clip2DParameterID, clip2D);

			if (method == AnimMethod.BlendShape && isPlayer && KickStarter.player)
			{
				runtimeShapeObject = KickStarter.player.GetComponent <Shapeable>();
			}
		}


		public override float Run ()
		{
			if (method == AnimMethod.BlendShape && isPlayer && runtimeShapeObject == null)
			{
				LogWarning ("Cannot BlendShape Player since cannot find Shapeable script on Player.");
			}

			if (animEngine != null)
			{
				return animEngine.ActionAnimRun (this);
			}
			else
			{
				LogWarning ("Could not create animation engine!");
				return 0f;
			}
		}


		public override void Skip ()
		{
			if (animEngine != null)
			{
				animEngine.ActionAnimSkip (this);
			}
		}


		public void ReportWarning (string message, Object context = null)
		{
			LogWarning (message, context);
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			ResetAnimationEngine ();
			
			animationEngine = (AnimationEngine) EditorGUILayout.EnumPopup ("Animation engine:", animationEngine);
			if (animationEngine == AnimationEngine.Custom)
			{
				customClassName = EditorGUILayout.DelayedTextField ("Script name:", customClassName);
			}

			if (animEngine)
			{
				animEngine.ActionAnimGUI (this, parameters);

				#if !AC_ActionListPrefabs
				if (GUI.changed && this) EditorUtility.SetDirty (this);
				#endif
			}
		}
		
		
		public override string SetLabel ()
		{
			if (animEngine)
			{
				return animEngine.ActionAnimLabel (this);
			}
			return string.Empty;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				if (method == AnimMethod.BlendShape)
				{
					if (isPlayer)
					{
						if (!fromAssetFile && GameObject.FindObjectOfType <Player>() != null)
						{
							Player player = GameObject.FindObjectOfType <Player>();
							shapeObject = player.GetComponent <Shapeable>();
						}

						if (shapeObject == null && AdvGame.GetReferences ().settingsManager)
						{
							Player player = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
							if (player != null)
							{
								shapeObject = player.GetComponent <Shapeable>();
							}
						}
					}

					if (shapeObject != null)
					{
						AddSaveScript <RememberShapeable> (shapeObject);
					}
				}

				ResetAnimationEngine ();
				if (animEngine != null && animator != null && animEngine.RequiresRememberAnimator (this))
				{
					animEngine.AddSaveScript (this, animator.gameObject);
				}
			}
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (_anim != null && _anim.gameObject && _anim.gameObject == _gameObject) return true;
				if (animator && animator.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (method == AnimMethod.BlendShape)
			{
				if (!isPlayer)
				{
					if (shapeObject && shapeObject.gameObject == _gameObject) return true;
				}
				if (isPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		protected void ResetAnimationEngine ()
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
				
			if (!string.IsNullOrEmpty (className) && (animEngine == null || animEngine.ToString () != className))
			{
				animEngine = (AnimEngine) ScriptableObject.CreateInstance (className);
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to play a custom animation using the 'Sprites Unity' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "clipName">The animation clip name to play</param>
		 * <param name = "layerIndex">The index number on the Animator Controller that the animation clip is located in</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_SpritesUnity_PlayCustom (Animator animator, string clipName, int layerIndex, float transitionTime = 0f, bool waitUntilFinish = false)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.SpritesUnity;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.clip2D = clipName;
			newAction.layerInt = layerIndex;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to play a custom animation using the 'Sprites Unity Complex' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "clipName">The animation clip name to play</param>
		 * <param name = "layerIndex">The index number on the Animator Controller that the animation clip is located in</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_SpritesUnityComplex_PlayCustom (Animator animator, string clipName, int layerIndex, float transitionTime = 0f, bool waitUntilFinish = false)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.SpritesUnityComplex;
			newAction.methodMecanim = AnimMethodMecanim.PlayCustom;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.clip2D = clipName;
			newAction.layerInt = layerIndex;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's integer parameter value using the 'Sprites Unity Complex' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (Animator animator, string parameterName, int newValue)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.SpritesUnityComplex;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Int;
			newAction.parameterValue = (float) newValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's float parameter value using the 'Sprites Unity Complex' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (Animator animator, string parameterName, float newValue)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.SpritesUnityComplex;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Float;
			newAction.parameterValue = newValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's bool parameter value using the 'Sprites Unity Complex' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (Animator animator, string parameterName, bool newValue)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.SpritesUnityComplex;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Bool;
			newAction.parameterValue = (newValue) ? 1f : 0f;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's trigger parameter value using the 'Sprites Unity Complex' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_SpritesUnityComplex_ChangeParameterValue (Animator animator, string parameterName)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.SpritesUnityComplex;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Trigger;
			newAction.parameterValue = 0f;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to play a custom animation using the 'Mecanim' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "clipName">The animation clip name to play</param>
		 * <param name = "layerIndex">The index number on the Animator Controller that the animation clip is located in</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning from the old to the new animation</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_Mecanim_PlayCustom (Animator animator, string clipName, int layerIndex, float transitionTime = 0f, bool waitUntilFinish = false)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.Mecanim;
			newAction.methodMecanim = AnimMethodMecanim.PlayCustom;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.clip2D = clipName;
			newAction.layerInt = layerIndex;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's integer parameter value using the 'Mecanim' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_Mecanim_ChangeParameterValue (Animator animator, string parameterName, int newValue)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.Mecanim;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Int;
			newAction.parameterValue = (float) newValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's float parameter value using the 'Mecanim' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_Mecanim_ChangeParameterValue (Animator animator, string parameterName, float newValue)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.Mecanim;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Float;
			newAction.parameterValue = newValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's bool parameter value using the 'Mecanim' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_Mecanim_ChangeParameterValue (Animator animator, string parameterName, bool newValue)
		{
			ActionAnim newAction = CreateNew <ActionAnim> ();
			newAction.animationEngine = AnimationEngine.Mecanim;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Bool;
			newAction.parameterValue = (newValue) ? 1f : 0f;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to change an Animator's trigger parameter value using the 'Mecanim' engine</summary>
		 * <param name = "animator">The Animator to manipulate</param>
		 * <param name = "parameterName">The name of the parameter</param>
		 * <param name = "newValue">The parameter's new value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_Mecanim_ChangeParameterValue (Animator animator, string parameterName)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.Mecanim;
			newAction.methodMecanim = AnimMethodMecanim.ChangeParameterValue;
			newAction.animator = animator;
			newAction.TryAssignConstantID (newAction.animator, ref newAction.constantID);
			newAction.parameterName = parameterName;
			newAction.mecanimParameterType = MecanimParameterType.Trigger;
			newAction.parameterValue = 0f;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Animate' Action, set to control Blendshapes</summary>
		 * <param name = "shapeable">The Shapeable to manipulate</param>
		 * <param name = "shapeKey">The ID of the key to affect</param>
		 * <param name = "shape">The shape key's new value</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionAnim CreateNew_BlendShape (Shapeable shapeable, int shapeKey, float shapeValue, float transitionTime = 1f, bool waitUntilFinish = false)
		{
			ActionAnim newAction = CreateNew<ActionAnim> ();
			newAction.animationEngine = AnimationEngine.Mecanim;
			newAction.methodMecanim = AnimMethodMecanim.BlendShape;
			newAction.shapeObject = shapeable;
			newAction.TryAssignConstantID (newAction.shapeObject, ref newAction.constantID);
			newAction.shapeKey = shapeKey;
			newAction.shapeValue = shapeValue;
			newAction.fadeTime = transitionTime;
			newAction.willWait = waitUntilFinish;

			return newAction;
		}

	}

}