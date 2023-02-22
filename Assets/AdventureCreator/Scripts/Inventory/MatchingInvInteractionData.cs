/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"MatchingInvInteractionData.cs"
 * 
 *	This script stores data related to which inventory items can be used with a given Hotspot or Inventory item.
 * 
 */

using System.Collections.Generic;

namespace AC
{

	/** This script stores data related to which inventory items can be used with a given Hotspot or Inventory item. */
	public class MatchingInvInteractionData
	{

		#region Variables

		private List<InvInstance> invInstances;
		private List<int> invInteractionIndices;
		private List<SelectItemMode> selectItemModes;

		#endregion


		#region Constructors

		/**
		 * <summary>A constructor for a given Hotspot</summary>
		 * <param name="hotspot">The Hotspot to create inventory data for</param>
		 */
		public MatchingInvInteractionData (Hotspot hotspot)
		{
			invInstances = new List<InvInstance> ();
			invInteractionIndices = new List<int>();
			selectItemModes = new List<SelectItemMode>();

			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && !KickStarter.settingsManager.cycleInventoryCursors)
			{
				return;
			}

			for (int i=0; i<hotspot.invButtons.Count; i++)
			{
				Button button = hotspot.invButtons[i];
				if (button.isDisabled) continue;

				foreach (InvInstance invInstance in KickStarter.runtimeInventory.PlayerInvCollection.InvInstances)
				{
					if (InvInstance.IsValid (invInstance) && invInstance.ItemID == button.invID && !button.isDisabled)
					{
						invInteractionIndices.Add (i);
						selectItemModes.Add (button.selectItemMode);
						invInstances.Add (invInstance);
						break;
					}
				}
			}
		}


		/**
		 * <summary>A constructor for a given inventory item</summary>
		 * <param name="invInstance">The item instance to create inventory data for</param>
		 */
		public MatchingInvInteractionData (InvInstance invInstance)
		{
			invInstances = new List<InvInstance> ();
			invInteractionIndices = new List<int> ();
			selectItemModes = new List<SelectItemMode> ();

			if (KickStarter.settingsManager.SelectInteractionMethod () == SelectInteractions.CyclingCursorAndClickingHotspot && !KickStarter.settingsManager.cycleInventoryCursors)
			{
				return;
			}

			for (int i=0; i<invInstance.CombineInteractions.Length; i++)
			{
				foreach (InvInstance localInvInstance in KickStarter.runtimeInventory.PlayerInvCollection.InvInstances)
				{
					if (InvInstance.IsValid (localInvInstance) && localInvInstance.ItemID == invInstance.CombineInteractions[i].combineID)
					{
						invInteractionIndices.Add (i);
						selectItemModes.Add (SelectItemMode.Use);
						invInstances.Add (localInvInstance);
						break;
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		public void SetSelectItemMode (int index)
		{
			if (index >= 0 && index < selectItemModes.Count)
			{
				invInstances[index].SelectItemMode = selectItemModes[index];
			}
		}


		public int GetInvInteractionIndex (int index)
		{
			if (index >= 0 && index < invInteractionIndices.Count)
			{
				return invInteractionIndices[index];
			}
			return -1;
		}

		#endregion


		#region GetSet

		/** The inventory items that can interact with the Hotspot or item this data set was created for */
		public List<InvInstance> InvInstances
		{
			get
			{
				return invInstances;
			}
		}


		/** The number of inventory items that can interact with the Hotspot or item this data set was created for */
		public int NumMatchingInteractions
		{
			get
			{
				return invInstances.Count;
			}
		}

		#endregion

	}

}