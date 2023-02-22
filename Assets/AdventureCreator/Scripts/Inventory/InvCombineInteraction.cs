/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"InvCombineInteraction.cs"
 * 
 *	This script is a container class for inventory combine interactions.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	/** A data container for inventory combine interactions. */
	[System.Serializable]
	public class InvCombineInteraction : InvInteractionBase
	{

		#region Variables

		/** The ID of the item to combine with */
		public int combineID;
		/** True if the interaction is disabled by default */
		public bool disabledOnStart = false;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_combineID">The ID of the item to combine with</param>
		 * <param name = "_actionList">The ActionList to run when the interaction is triggered</param>
		 */
		public InvCombineInteraction (int _combineID, ActionListAsset _actionList, List<InvCombineInteraction> combineInteractions)
		{
			combineID = _combineID;
			actionList = _actionList;
			disabledOnStart = false;
			Upgrade (combineInteractions);
		}

		#endregion


		#region PublicFunctions

		public void Upgrade (List<InvCombineInteraction> allInteractions)
		{
			if (idPlusOne == 0)
			{
				idPlusOne = 1;

				if (allInteractions != null)
				{
					foreach (InvCombineInteraction interaction in allInteractions)
					{
						if (interaction != this && interaction.idPlusOne == idPlusOne)
						{
							idPlusOne++;
						}
					}
				}
			}
		}

		#endregion

	}

}