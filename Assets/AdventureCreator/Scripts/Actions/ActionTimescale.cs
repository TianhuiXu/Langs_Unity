/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionTimescale.cs"
 * 
 *	This action modifies the speed at which the game runs at.
 *	It can be used for slow-motion effects during both cutscenes and gameplay.
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
	public class ActionTimescale : Action
	{
		
		public float timeScale;
		public int parameterID = -1;
		public bool useTimeCurve = false;
		public AnimationCurve timeCurve;


		public override ActionCategory Category { get { return ActionCategory.Engine; }}
		public override string Title { get { return "Change timescale"; }}
		public override string Description { get { return "Changes the timescale to a value between 0 and 1. This allows for slow-motion effects."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			timeScale = AssignFloat (parameters, parameterID, timeScale);
			if (timeScale < 0f) timeScale = 0f;
		}

		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;

				if (useTimeCurve)
				{
					if (timeCurve != null)
					{
						KickStarter.playerInput.SetTimeCurve (timeCurve);
						if (willWait)
						{
							return defaultPauseTime;
						}
					}
				}
				else if (timeScale > 0f)
				{
					KickStarter.playerInput.SetTimeScale (timeScale);
				}
				else
				{
					LogWarning ("Cannot set timescale to zero!");
				}
			}
			else
			{
				if (KickStarter.playerInput.HasTimeCurve ())
				{
					return defaultPauseTime;
				}
				else
				{
					isRunning = false;
				}
			}
			return 0f;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			useTimeCurve = EditorGUILayout.Toggle ("Use time curve?", useTimeCurve);
			if (useTimeCurve)
			{
				if (timeCurve == null)
				{
					timeCurve = AnimationCurve.Linear (0f, 0.1f, 1f, 1f);
				}

				timeCurve = EditorGUILayout.CurveField ("Time curve:", timeCurve);
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("Timescale:", parameters, parameterID, ParameterType.Float);
				if (parameterID < 0)
				{
					timeScale = EditorGUILayout.Slider ("Timescale:", timeScale, 0f, 1f);
				}
			}
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Engine: Change timescale' Action</summary>
		 * <param name = "newTimeScale">The game's new Time.timeScale value</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTimescale CreateNew (float newTimeScale)
		{
			ActionTimescale newAction = CreateNew<ActionTimescale> ();
			newAction.useTimeCurve = false;
			newAction.timeScale = newTimeScale;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: Change timescale' Action</summary>
		 * <param name = "newTimeCurve">An animation curve that describes the change in Time.timeScale value over time</param>
		 * <param name = "waitUntilFinish">If True, the Action will wait until the animation curve has been completely followed</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTimescale CreateNew (AnimationCurve newTimeCurve, bool waitUntilFinish = false)
		{
			ActionTimescale newAction = CreateNew<ActionTimescale> ();
			newAction.useTimeCurve = true;
			newAction.timeCurve = newTimeCurve;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

	}

}