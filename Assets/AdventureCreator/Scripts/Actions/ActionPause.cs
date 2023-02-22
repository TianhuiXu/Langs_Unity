/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionPause.cs"
 * 
 *	This action pauses the game by a given amount.
 * 
 */

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionPause : Action
	{

		public int parameterID = -1;
		public float timeToPause;

		
		public override ActionCategory Category { get { return ActionCategory.Engine; }}
		public override string Title { get { return "Wait"; }}
		public override string Description { get { return "Waits a set time before continuing."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			timeToPause = AssignFloat (parameters, parameterID, timeToPause);
		}

		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;

				if (timeToPause < 0f)
				{
					return defaultPauseTime;
				}
				return timeToPause;
			}
			else
			{
				isRunning = false;
				return 0f;
			}
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Wait time (s):", parameters, parameterID, ParameterType.Float);
			if (parameterID < 0)
			{
				timeToPause = EditorGUILayout.FloatField ("Wait time (s):", timeToPause);
				if (timeToPause < 0f)
				{
					EditorGUILayout.HelpBox ("A negative value will pause the ActionList by one frame.", MessageType.Info);
				}
			}
		}
		

		public override string SetLabel ()
		{
			return timeToPause.ToString () + "s";
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Engine: Wait' Action with key variables already set.</summary>
		 * <param name = "waitTime">The time to wait</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionPause CreateNew (float waitTime)
		{
			ActionPause newAction = CreateNew<ActionPause> ();
			newAction.timeToPause = waitTime;
			return newAction;
		}
		
	}

}