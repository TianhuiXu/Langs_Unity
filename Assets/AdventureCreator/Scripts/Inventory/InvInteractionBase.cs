/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"InvInteractionBase.cs"
 * 
 *	A base class for inventory interactions.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/** A base class for inventory interactions.. */
	[System.Serializable]
	public abstract class InvInteractionBase
	{

		#region Variables

		/** The ActionList to run when the interaction is triggered */
		public ActionListAsset actionList;
		[SerializeField] protected int idPlusOne;

		#endregion


		#region GetSet

		/** A unique identifier */
		public int ID
		{
			get
			{
				return idPlusOne - 1;
			}
			set
			{
				idPlusOne = value + 1;
			}
		}

		#endregion

	}

}