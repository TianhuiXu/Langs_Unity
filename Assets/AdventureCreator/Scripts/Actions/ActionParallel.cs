/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionParallel.cs"
 * 
 *	This action can play multiple subsequent Actions at once.
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
	public class ActionParallel : Action
	{
		
		public int numSockets;

		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Run in parallel"; }}
		public override string Description { get { return "Runs any subsequent Actions (whether in the same list or in a new one) simultaneously. This is useful when making complex cutscenes that require timing to be exact."; }}
		public override int NumSockets { get { return numSockets; }}
		public override bool RunAllOutputs { get { return true; }}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			numSockets = EditorGUILayout.IntSlider ("# of outputs:", numSockets, 1, 10);

			bool shownError = false;
			List<int> outputIndices = new List<int> ();
			foreach (ActionEnd ending in endings)
			{
				if (ending.resultAction == ResultAction.Skip)
				{
					if (outputIndices.Contains (ending.skipAction))
					{
						if (!shownError)
						{
							EditorGUILayout.HelpBox ("Two or more output sockets connect to the same subsequent Action - this may cause unexpected behaviour and should be changed.", MessageType.Warning);
						}
						shownError = true;
					}
					else
					{
						outputIndices.Add (ending.skipAction);
					}
				}
			}
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Run in parallel' Action</summary>
		 * <param name = "actionEnds">An array of data about what output sockets the Action has</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParallel CreateNew (ActionEnd[] actionEnds)
		{
			ActionParallel newAction = CreateNew<ActionParallel> ();
			newAction.endings = new List<ActionEnd>();
			foreach (ActionEnd actionEnd in actionEnds)
			{
				newAction.endings.Add (new ActionEnd (actionEnd));
			}
			return newAction;
		}
		
	}
	
}