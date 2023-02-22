/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCheckMultipleTemplate.cs"
 * 
 *	This is a blank action template, which has any number of outputs.
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
	public class ActionCheckMultipleTemplate : Action
	{
		
		// Declare properties here
		public override ActionCategory Category { get { return ActionCategory.Custom; }}
		public override string Title { get { return "Check multiple template"; }}
		public override string Description { get { return "This is a blank 'Check multiple' Action template."; }}
		public override int NumSockets { get { return numSockets; }}
		
		
		// Declare variables here
		int numSockets = 3;
		

		public override int GetNextOutputIndex ()
		{
			// Here, we decide which output socket to follow (starting from 0).  Here, we choose one at random:
			int outputSocketIndex = Random.Range (0, numSockets);

			// Then, we return this value
			return outputSocketIndex;
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI ()
		{
			// Action-specific Inspector GUI code here.

			// We can define the number of output sockets with the NumSockets property. Here, we use it to return the variable below, allowing us to dynamically set how many sockets are available
			numSockets = EditorGUILayout.DelayedIntField ("# of sockets:", numSockets);
		}
		

		public override string SetLabel ()
		{
			// (Optional) Return a string used to describe the specific action's job.

			return string.Empty;
		}

		#endif
		
	}

}