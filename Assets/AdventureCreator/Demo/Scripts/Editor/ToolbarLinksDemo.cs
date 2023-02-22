#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AC
{

	public class ToolbarLinksDemo : EditorWindow
	{

		[MenuItem ("Adventure Creator/Getting started/Load 3D Demo", false, 6)]
		static void Demo3D ()
		{
			ManagerPackage package = AssetDatabase.LoadAssetAtPath (Resource.MainFolderPath + "/Demo/ManagerPackage.asset", typeof (ManagerPackage)) as ManagerPackage;
			if (package != null)
			{
				if (!ACInstaller.IsInstalled ())
				{
					ACInstaller.DoInstall ();
				}

				package.AssignManagers ();
				AdventureCreator.RefreshActions ();

				if (UnityVersionHandler.GetCurrentSceneName () != "Basement")
				{
					bool canProceed = EditorUtility.DisplayDialog ("Open demo scene", "Would you like to open the 3D Demo scene, Basement, now?", "Yes", "No");
					if (canProceed)
					{
						if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
						{
							UnityEditor.SceneManagement.EditorSceneManager.OpenScene (Resource.MainFolderPath + "/Demo/Scenes/Basement.unity");
						}
					}
				}

				AdventureCreator.Init ();
			}
		}

	}

}

#endif