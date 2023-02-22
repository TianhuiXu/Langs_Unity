/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"NavigationManager.cs"
 * 
 *	This script instantiates the chosen
 *	NavigationEngine subclass at runtime.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This component instantiates the scene's chosen NavigationEngine ScriptableObject when the game begins.
	 * It should be placed on the GameEngine prefab.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_navigation_manager.html")]
	public class NavigationManager : MonoBehaviour
	{

		#region Variables

		/** The NavigationEngine ScriptableObject that performs the scene's pathfinding algorithms. */
		[HideInInspector] public NavigationEngine navigationEngine = null;

		#endregion


		#region UnityStandards

		public void OnAwake (NavigationMesh defaultNavMesh)
		{
			navigationEngine = null;
			ResetEngine ();

			// Turn off all NavMesh objects
			NavigationMesh[] navMeshes = FindObjectsOfType (typeof (NavigationMesh)) as NavigationMesh[];
			foreach (NavigationMesh _navMesh in navMeshes)
			{
				if (defaultNavMesh != _navMesh)
				{
					_navMesh.TurnOff ();
				}
			}

			if (navigationEngine == null || navigationEngine.RequiresNavMeshGameObject)
			{
				if (defaultNavMesh)
				{
					defaultNavMesh.TurnOn ();
				}
				else if (navigationEngine != null)
				{
					AC.Char[] allChars = FindObjectsOfType (typeof (AC.Char)) as AC.Char[];
					if (allChars.Length > 0)
					{
						ACDebug.LogWarning ("No NavMesh set. Characters will not be able to PathFind until one is defined - please choose one using the Scene Manager.");
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * Sets up the scene's chosen NavigationEngine ScriptableObject if it is not already present.
		 */
		public void ResetEngine ()
		{
			string className = string.Empty;
			if (KickStarter.sceneSettings.navigationMethod == AC_NavigationMethod.Custom)
			{
				className = KickStarter.sceneSettings.customNavigationClass;
			}
			else
			{
				className = "NavigationEngine_" + KickStarter.sceneSettings.navigationMethod.ToString ();
			}

			if (string.IsNullOrEmpty (className) && Application.isPlaying)
			{
				ACDebug.LogWarning ("Could not initialise navigation - a custom script must be assigned if the Pathfinding method is set to Custom.");
			}
			else if (navigationEngine == null || !navigationEngine.ToString ().Contains (className))
			{
				navigationEngine = (NavigationEngine) ScriptableObject.CreateInstance (className);
				if (navigationEngine != null)
				{
					navigationEngine.OnReset (KickStarter.sceneSettings.navMesh);
				}
			}
		}


		/**
		 * <summary>Checks if the Navigation Engine is written to work with Unity 2D or not.</summary>
		 * <returns>True if the Navigation Engine is written to work with Unity 2D.</returns>
		 */
		public bool Is2D ()
		{
			if (navigationEngine != null)
			{
				return navigationEngine.is2D;
			}
			return false;
		}

		#endregion

	}

}