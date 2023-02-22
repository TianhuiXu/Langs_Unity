/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSpriteFade.cs"
 * 
 *	Fades a sprite in or out.
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
	public class ActionSpriteFade : Action
	{
		
		public int constantID = 0;
		public int parameterID = -1;
		public SpriteFader spriteFader;
		protected SpriteFader runtimeSpriteFader;
		
		public FadeType fadeType = FadeType.fadeIn;
		public float fadeSpeed;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Fade sprite"; }}
		public override string Description { get { return "Fades a sprite in or out."; }}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeSpriteFader = AssignFile <SpriteFader> (parameters, parameterID, constantID, spriteFader);
		}
		
		
		public override float Run ()
		{
			if (runtimeSpriteFader == null)
			{
				return 0f;
			}

			if (!isRunning)
			{
				isRunning = true;

				runtimeSpriteFader.Fade (fadeType, fadeSpeed);

				if (willWait)
				{
					return fadeSpeed;
				}
			}
			else
			{
				isRunning = false;
			}
			
			return 0f;
		}


		public override void Skip ()
		{
			if (runtimeSpriteFader != null)
			{
				runtimeSpriteFader.Fade (fadeType, 0f);
			}
		}
	
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Sprite to fade:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				spriteFader = null;
			}
			else
			{
				spriteFader = (SpriteFader) EditorGUILayout.ObjectField ("Sprite to fade:", spriteFader, typeof (SpriteFader), true);
				
				constantID = FieldToID <SpriteFader> (spriteFader, constantID);
				spriteFader = IDToField <SpriteFader> (spriteFader, constantID, false);
			}

			fadeType = (FadeType) EditorGUILayout.EnumPopup ("Type:", fadeType);
			
			fadeSpeed = EditorGUILayout.Slider ("Time to fade:", fadeSpeed, 0f, 10f);
			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberVisibility> (spriteFader);
			}
			AssignConstantID <SpriteFader> (spriteFader, constantID, parameterID);
		}

		
		public override string SetLabel ()
		{
			if (spriteFader != null)
			{
				return fadeType.ToString () + " " + spriteFader.gameObject.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (spriteFader && spriteFader.gameObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Fade sprite' Action</summary>
		 * <param name = "spriteFaderToAffect">The SpriteFader component to affect</param>
		 * <param name = "fadeType">The type of fade to perform</param>
		 * <param name = "transitionTime">The time, in seconds, to take when transitioning</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpriteFade CreateNew (SpriteFader spriteFaderToAffect, FadeType fadeType, float transitionTime = 1f, bool waitUntilFinish = false)
		{
			ActionSpriteFade newAction = CreateNew<ActionSpriteFade> ();
			newAction.spriteFader = spriteFaderToAffect;
			newAction.TryAssignConstantID (newAction.spriteFader, ref newAction.constantID);
			newAction.fadeType = fadeType;
			newAction.fadeSpeed = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}
	
}