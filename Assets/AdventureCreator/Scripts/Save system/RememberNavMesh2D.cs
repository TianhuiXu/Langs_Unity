/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberNavMesh2D.cs"
 * 
 *	This script is attached to NavMesh2D prefabs
 *	who have their "holes" changed during gameplay.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script is attached to NavMesh2D objects who have their "holes" changed during gameplay.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember NavMesh2D")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_nav_mesh2_d.html")]
	public class RememberNavMesh2D : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			NavMesh2DData navMesh2DData = new NavMesh2DData ();
			
			navMesh2DData.objectID = constantID;
			navMesh2DData.savePrevented = savePrevented;

			if (GetComponent <NavigationMesh>())
			{
				NavigationMesh navMesh = GetComponent <NavigationMesh>();
				List<int> linkedIDs = new List<int>();

				for (int i=0; i<navMesh.polygonColliderHoles.Count; i++)
				{
					if (navMesh.polygonColliderHoles[i].GetComponent <ConstantID>())
					{
						linkedIDs.Add (navMesh.polygonColliderHoles[i].GetComponent <ConstantID>().constantID);
					}
					else
					{
						ACDebug.LogWarning ("Cannot save " + this.gameObject.name + "'s holes because " + navMesh.polygonColliderHoles[i].gameObject.name + " has no Constant ID!", gameObject);
					}
				}

				navMesh2DData._linkedIDs = ArrayToString <int> (linkedIDs.ToArray ());
			}
			
			return Serializer.SaveScriptData <NavMesh2DData> (navMesh2DData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			NavMesh2DData data = Serializer.LoadScriptData <NavMesh2DData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			NavigationMesh navMesh = GetComponent <NavigationMesh>();
			if (navMesh)
			{
				navMesh.polygonColliderHoles.Clear ();

				if (KickStarter.sceneSettings.navMesh == navMesh)
				{
					KickStarter.navigationManager.navigationEngine.ResetHoles (navMesh);
				}

				if (!string.IsNullOrEmpty (data._linkedIDs))
				{
					int[] linkedIDs = StringToIntArray (data._linkedIDs);
					for (int i=0; i<linkedIDs.Length; i++)
					{
						PolygonCollider2D polyHole = ConstantID.GetComponent <PolygonCollider2D> (linkedIDs[i]);
						if (polyHole)
						{
							navMesh.AddHole (polyHole);
						}
					}
				}
			}
		}
		
	}
	

	/**
	 * A data container used by the RememberNavMesh2D script.
	 */
	[System.Serializable]
	public class NavMesh2DData : RememberData
	{

		/** The Constant ID numbers of each "hole" currently assigned */
		public string _linkedIDs;

		/**
		 * The default Constructor.
		 */
		public NavMesh2DData () { }

	}
	
}