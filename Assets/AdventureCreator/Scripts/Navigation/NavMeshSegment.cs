/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavMeshSegment.cs"
 * 
 *	This script is used for the NavMeshSegment prefab, which defines
 *	the area to be baked by the Unity Navigation window.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Controls a navigation area used by Unity Navigation-based pathfinding method.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_nav_mesh_segment.html")]
	[AddComponentMenu("Adventure Creator/Navigation/NavMesh Segment")]
	public class NavMeshSegment : NavMeshBase
	{

		#region UnityStandards

		protected void Awake ()
		{
			BaseAwake ();

			if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.UnityNavigation)
			{
				if (LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer) == -1)
				{
					ACDebug.LogWarning ("No 'NavMesh' layer exists - please define one in the Tags Manager.");
				}
				else
				{
					gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.navMeshLayer);
				}
			}
		}

		#endregion

	}

}