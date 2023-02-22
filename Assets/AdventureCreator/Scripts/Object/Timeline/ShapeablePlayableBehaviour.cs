/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ShapeablePlayableBehaviour.cs"
 * 
 *	A PlayableBehaviour used by ShapeableMixer.
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
#if !ACIgnoreTimeline

namespace AC
{

	/** A PlayableBehaviour used by ShapeableMixer. */
	internal sealed class ShapeablePlayableBehaviour : PlayableBehaviour
	{

		#region Variables

		public int groupID;
		public int keyID;
		[Range (0, 100)] public int intensity = 100;

		#endregion


		#region GetSet

		public bool IsValid
		{
			get
			{
				return true;
			}
		}

		#endregion

	}

}

#endif