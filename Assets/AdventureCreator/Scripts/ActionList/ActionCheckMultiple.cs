
/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCheckMultiple.cs"
 * 
 *	Deprected - derive Actions with multiple sockets from Action directly.
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
	 * Deprected - derive Actions with multiple sockets from Action directly.
	 */
	[System.Serializable]
	public class ActionCheckMultiple : Action
	{

		public int numSockets = 2;
		public override int NumSockets { get { return numSockets; }}

	}
	
}