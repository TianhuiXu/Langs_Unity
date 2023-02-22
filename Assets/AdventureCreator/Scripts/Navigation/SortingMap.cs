/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SortingMap.cs"
 * 
 *	This script is used to change the sorting order of
 *	2D Character sprites based on their Z-position.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script is used to change the sorting order and scale of 2D characters, based on their position in the scene.
	 * The instance of this class stored in SceneSettings' sortingMap variable will be read by FollowSortingMap components to determine what their SpriteRenderer's order and scale should be.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_sorting_map.html")]
	public class SortingMap : MonoBehaviour
	{

		#region Variables

		/** True if characters that follow this map should have their sorting affected */
		public bool affectSorting = true;
		/** How SpriteRenderer components that follow this map are effected (OrderInLayer, SortingLayer) */
		public SortingMapType mapType = SortingMapType.OrderInLayer;
		/** A List of SortingArea data that makes up the map */
		public List <SortingArea> sortingAreas = new List<SortingArea>();			
		/** True if characters that follow this map should have their scale affected */
		public bool affectScale = false;
		/** True if characters that follow this map should have their movement speed affected by the scale factor */
		public bool affectSpeed = true;
		/** The scale (as a percentage) that characters will have at the very top of the map (if affectScale = True) */
		public int originScale = 100;
		/** How scaling values are defined (Linear, AnimationCurve) */
		public SortingMapScaleType sortingMapScaleType = SortingMapScaleType.Linear;
		/** The AnimationCurve used to define character scaling, where 0s is the smallest scale, and 1s is the largest (if sortingMapScaleType = AnimationCurve) */
		public AnimationCurve scalingAnimationCurve;

		protected Transform _transform;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}


		protected void OnDrawGizmos ()
		{
			Vector3 right = Transform.right * 0.1f;
			bool doScaling = affectScale || affectSpeed;

			float scaleGizmo = (doScaling && sortingMapScaleType == SortingMapScaleType.Linear) ? (originScale / 100f) : 1f;
			Gizmos.DrawLine (Transform.position - right * scaleGizmo, Transform.position + right * scaleGizmo);

			for (int i=0; i<sortingAreas.Count; i++)
			{
				scaleGizmo = (doScaling && sortingMapScaleType == SortingMapScaleType.Linear) ? (sortingAreas[i].scale / 100f) : 1f;

				Gizmos.color = sortingAreas [i].color;
				Gizmos.DrawIcon (GetAreaPosition (i), string.Empty, true);
				Gizmos.DrawLine (GetAreaPosition (i) - right * scaleGizmo, GetAreaPosition (i) + right * scaleGizmo);

				Vector3 startPosition = (i == 0) ? Transform.position : GetAreaPosition (i-1);

				float startScaleGizmo = (doScaling && sortingMapScaleType == SortingMapScaleType.Linear) ? ((i == 0) ? (originScale / 100f) : (sortingAreas[i-1].scale / 100f)) : 1f;
			
				Gizmos.DrawLine (startPosition + right * startScaleGizmo, GetAreaPosition (i) + right * scaleGizmo);
				Gizmos.DrawLine (startPosition - right * startScaleGizmo, GetAreaPosition (i) - right * scaleGizmo);
			}
		}

		#endregion


		#region PublicFunctions

		/** Adjusts all relevant FollowSortingMaps that are within the same region, so that they are all displayed correctly. */
		public void UpdateSimilarFollowers ()
		{
			if (KickStarter.sceneSettings.sharedLayerSeparationDistance <= 0f)
			{
				return;
			}

			foreach (SortingArea sortingArea in sortingAreas)
			{
				List<FollowSortingMap> testFollowers = new List<FollowSortingMap>();

				foreach (FollowSortingMap followSortingMap in KickStarter.stateHandler.FollowSortingMaps)
				{
					if (followSortingMap.GetSortingMap () == this)
					{
						if ((mapType == SortingMapType.OrderInLayer && followSortingMap.SortingOrder == sortingArea.order) ||
							(mapType == SortingMapType.SortingLayer && followSortingMap.SortingLayer == sortingArea.layer))
						{
							// Found a follower with the same order/layer
							testFollowers.Add (followSortingMap);
						}
					}
				}

				switch (testFollowers.Count)
				{
					case 0:
						break;

					case 1:
						testFollowers[0].SetDepth (0);
						break;

					default:
						testFollowers.Sort (SortByScreenPosition);
						for (int i=0; i<testFollowers.Count; i++)
						{
							testFollowers [i].SetDepth (i);
						}
						break;
				}
			}
		}


		/**
		 * <summary>Gets the boundary position of a particular SortingArea.</summary>
		 * <param name = "i">The index of the SortingArea to get the boundary position of</param>
		 * <returns>The boundary positon of the SortingArea</returns>
		 */
		public Vector3 GetAreaPosition (int i)
		{
			return (Transform.position + (Transform.forward * sortingAreas [i].z));
		}


		/**
		 * <summary>Gets an interpolated scale factor, based on a position in the scene.</summary>
		 * <param name = "followPosition">The position in the scene to get the scale factor for</param>
		 * <returns>The interpolated scale factor for any FollowSortingMap components at the given position</returns>
		 */
		public float GetScale (Vector3 followPosition)
		{
			if (sortingAreas.Count == 0)
			{
				return (float) originScale;
			}
			
			// Behind first?
			if (Vector3.Angle (Transform.forward, Transform.position - followPosition) < 90f)
			{
				if (sortingMapScaleType == SortingMapScaleType.AnimationCurve)
				{
					float scaleValue = scalingAnimationCurve.Evaluate (0) * 100f;
					return Mathf.Max (scaleValue, 1f);
				}
				return (float) originScale;
			}
			
			// In front of last?
			if (Vector3.Angle (Transform.forward, GetAreaPosition (sortingAreas.Count-1) - followPosition) > 90f)
			{
				if (sortingMapScaleType == SortingMapScaleType.AnimationCurve)
				{
					float scaleValue = scalingAnimationCurve.Evaluate (1f) * 100f;
					return Mathf.Max (scaleValue, 1f);
				}
				return (float) sortingAreas [sortingAreas.Count-1].scale;
			}
			
			// In between two?
			if (sortingMapScaleType == SortingMapScaleType.AnimationCurve)
			{
				int i = sortingAreas.Count-1;
				float angle = Vector3.Angle (Transform.forward, GetAreaPosition (i) - followPosition);
				float proportionAlong = 1 - Vector3.Distance (GetAreaPosition (i), followPosition) / sortingAreas [i].z * Mathf.Cos (Mathf.Deg2Rad * angle);

				float scaleValue = scalingAnimationCurve.Evaluate (proportionAlong) * 100f;
				return Mathf.Max (scaleValue, 1f);
			}

			for (int i=0; i<sortingAreas.Count; i++)
			{
				float angle = Vector3.Angle (Transform.forward, GetAreaPosition (i) - followPosition);
				if (angle < 90f)
				{
					float prevZ = 0;
					if (i > 0)
					{
						prevZ = sortingAreas [i-1].z;
					}
					
					float proportionAlong = 1 - Vector3.Distance (GetAreaPosition (i), followPosition) / (sortingAreas [i].z - prevZ) * Mathf.Cos (Mathf.Deg2Rad * angle);
					float previousScale = (float) originScale;
					if (i > 0)
					{
						previousScale = sortingAreas [i-1].scale;
					}
					
					return (previousScale + proportionAlong * ((float) sortingAreas [i].scale - previousScale));
				}
			}
			
			return 1f;
		}
		

		/**
		 * Assigns the scale factors for all SortingArea data that lie in between the top and bottom boundaries.
		 */
		public void SetInBetweenScales ()
		{
			if (sortingAreas.Count < 2)
			{
				return;
			}
			
			float finalScale = sortingAreas [sortingAreas.Count-1].scale;
			float finalZ = sortingAreas [sortingAreas.Count-1].z;
			
			for (int i=0; i<sortingAreas.Count-1; i++)
			{
				float newScale = ((sortingAreas [i].z / finalZ) * ((float) finalScale - (float) originScale)) + (float) originScale;
				sortingAreas [i].scale = (int) newScale;
			}
		}

		#endregion


		#region StaticFunctions		
		
		protected static int SortByScreenPosition (FollowSortingMap o1, FollowSortingMap o2)
		{
			return KickStarter.CameraMain.WorldToScreenPoint (o1.transform.position).y.CompareTo (KickStarter.CameraMain.WorldToScreenPoint (o2.transform.position).y);
		}

		#endregion


		#region GetSet

		/** A cache of the SortingMap's transform component */
		public Transform Transform
		{
			get
			{
				if (_transform == null) _transform = transform;
				return _transform;
			}
		}

		#endregion

	}

}