#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	public class ToolbarLinks2DDemo : EditorWindow
	{

		[MenuItem ("Adventure Creator/Getting started/Load 2D Demo", false, 5)]
		static void Demo2D ()
		{
			ManagerPackage package = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/2D Demo/ManagerPackage.asset", typeof (ManagerPackage)) as ManagerPackage;
			if (package != null)
			{
				package.AssignManagers ();
				AdventureCreator.RefreshActions ();

				if (!ACInstaller.IsInstalled ())
				{
					ACInstaller.DoInstall ();
				}

				if (UnityVersionHandler.GetCurrentSceneName () != "Park")
				{
					bool canProceed = EditorUtility.DisplayDialog ("Open demo scene", "Would you like to open the 2D Demo scene, Park, now?", "Yes", "No");
					if (canProceed)
					{
						if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
						{
							UnityEditor.SceneManagement.EditorSceneManager.OpenScene (Resource.MainFolderPath + "/2D Demo/Scenes/Park.unity");
						}
					}
				}

				AdventureCreator.Init ();
			}
		}

	}

}

#endif