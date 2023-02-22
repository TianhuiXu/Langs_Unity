/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"InvBin.cs"
 * 
 *	This script is a container class for inventory item categories.
 * 
 */


namespace AC
{

	/**
	 * A data container for an inventory item category.
	 */
	[System.Serializable]
	public class InvBin
	{

		#region Variables

		/** The category's editor name */
		public string label;
		/** A unique identifier */
		public int id;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public InvBin (int[] idArray)
		{
			id = 0;

			foreach (int _id in idArray)
			{
				if (id == _id)
				{
					id ++;
				}
			}

			label = "Category " + (id + 1).ToString ();
		}

		#endregion


		#if UNITY_EDITOR

		public string EditorLabel
		{
			get
			{
				return id.ToString () + ": " + (string.IsNullOrEmpty (label) ? "(Unnamed)" : label);
			}
		}

		#endif

	}

}