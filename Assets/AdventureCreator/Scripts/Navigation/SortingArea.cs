/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SortingArea.cs"
 * 
 *	This script is a container class for individual regions of a SortingMap.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A data container for individual regions within a SortingMap.
	 */
	[System.Serializable]
	public class SortingArea
	{

		#region Variables

		/** The lower boundary position along the Z axis */
		public float z;
		/** The order in layer that SpriteRenderers that use FollowSortingMap will have when positioned within this region */
		public int order;
		/** The sorting layer that SpriteRenderers that use FollowSortingMap will have when positioned within this region */
		public string layer;
		/** The colour of the region, as used in the Scene window */
		public Color color;
		/** The factor by which characters that use FollowSortingMap will be scaled by when positioned at the bottom boundary of this region */
		public int scale = 100;

		protected string orderAsString;

		#endregion


		#region Constructors

		/**
		 * A Constructor that creates a new SortingArea based on the last one currently on the SortingMap.
		 */
		public SortingArea (SortingArea lastArea)
		{
			z = lastArea.z + 1f;
			order = lastArea.order + 1;
			layer = "";
			scale = lastArea.scale;
			color = GetRandomColor ();
		}


		/**
		 * A Constructor that creates a new SortingArea by interpolating between two others.
		 */
		public SortingArea (SortingArea area1, SortingArea area2)
		{
			z = (area1.z + area2.z) / 2f;

			float _avOrder = (float) area1.order + (float) area2.order;
			order = (int) (_avOrder / 2f);

			float _avScale = (float) area1.scale + (float) 	area2.scale;
			scale = (int) (_avScale / 2f);

			layer = "";
			color = GetRandomColor ();
		}


		/**
		 * The default Constructor.
		 */
		public SortingArea (float _z, int _order)
		{
			z = _z;
			order = _order;
			layer = "";
			scale = 100;
			color = GetRandomColor ();
		}

		#endregion


		#region ProtectedFunctions

		protected Color GetRandomColor ()
		{
			return new Color (Random.Range (0f, 1f),Random.Range (0f, 1f), Random.Range (0f, 1f));
		}

		#endregion

	}

}