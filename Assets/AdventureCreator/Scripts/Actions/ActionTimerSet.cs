/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionTimerSet.cs"
 * 
 *	Starts or stops a Timer
 * 
 */

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionTimerSet : Action
	{

		public int timerID;
		public enum TimerMethod { Start, Resume, Stop };
		public TimerMethod timerMethod = TimerMethod.Start;
		public bool resetTicker = true;

		
		public override ActionCategory Category { get { return ActionCategory.Variable; }}
		public override string Title { get { return "Set timer"; }}
		public override string Description { get { return "Starts or stops a Timer"; }}


		public override float Run ()
		{
			if (KickStarter.variablesManager != null && KickStarter.variablesManager.timers != null)
			{
				foreach (Timer timer in KickStarter.variablesManager.timers)
				{
					if (timer.ID == timerID)
					{
						switch (timerMethod)
						{
							case TimerMethod.Start:
							default:
								timer.Start ();
								break;

							case TimerMethod.Resume:
								timer.Resume (resetTicker);
								break;

							case TimerMethod.Stop:
								timer.Stop ();
								break;
						}

						return 0f;
					}
				}

				LogWarning ("Couldn't find the Timer with ID=" + timerID);
				return 0f;
			}

			LogWarning ("No Timers found! Is the Variables Manager assigned properly?");
			return 0f;
		}
		

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			if (GUILayout.Button ("Timers window"))
			{
				TimersEditor.Init ();
			}

			int tempNumber = -1;

			if (KickStarter.variablesManager != null && KickStarter.variablesManager.timers != null && KickStarter.variablesManager.timers.Count > 0)
			{
				string[] labelList = new string[KickStarter.variablesManager.timers.Count];
				for (int i=0; i<KickStarter.variablesManager.timers.Count; i++)
				{
					labelList[i] = i.ToString () + ": " + KickStarter.variablesManager.timers[i].Label;

					if (KickStarter.variablesManager.timers[i].ID == timerID)
					{
						tempNumber = i;
					}
				}

				if (tempNumber == -1)
				{
					// Wasn't found (was deleted?), so revert to zero
					if (timerID != 0)
						LogWarning ("Previously chosen Timer no longer exists!");
					tempNumber = 0;
					timerID = 0;
				}

				tempNumber = EditorGUILayout.Popup ("Timer:", tempNumber, labelList);
				timerID = KickStarter.variablesManager.timers [tempNumber].ID;
				timerMethod = (TimerMethod) EditorGUILayout.EnumPopup ("Method:", timerMethod);

				if (timerMethod == TimerMethod.Resume)
				{
					resetTicker = EditorGUILayout.Toggle ("Reset ticker?", resetTicker);
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No Timers exist!", MessageType.Info);
				timerID = 0;
			}
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Variable: Set timer' Action</summary>
		 * <param name = "timer">The ID number of the Timer to affect</param>
		 * <param name = "changeType">The type of change to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTimerSet CreateNew (int timerID, TimerMethod timerMethod)
		{
			ActionTimerSet newAction = CreateNew<ActionTimerSet> ();
			newAction.timerID = timerID;
			newAction.timerMethod = timerMethod;
			return newAction;
		}
		
	}
	
}