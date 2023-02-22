/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"InvInteraction.cs"
 * 
 *	This script is a container class for inventory interactions.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	/** A data container for standard inventory interactions. */
	[System.Serializable]
	public class InvInteraction : InvInteractionBase
	{

		#region Variables

		/** The icon, defined in CursorManager, associated with the interaction */
		public CursorIcon icon;
		/** True if the interaction is disabled by default */
		public bool disabledOnStart = false;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_icon">The icon, defined in CursorManager, associated with the interaction</param>
		 */
		public InvInteraction (CursorIcon _icon, List<InvInteraction> invInteractions)
		{
			icon = _icon;
			actionList = null;
			disabledOnStart = false;
			Upgrade (invInteractions);
		}

		#endregion


		#region PublicFunctions

		public void Upgrade (List<InvInteraction> allInteractions)
		{
			if (idPlusOne == 0)
			{
				idPlusOne = 1;

				if (allInteractions != null)
				{
					foreach (InvInteraction interaction in allInteractions)
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