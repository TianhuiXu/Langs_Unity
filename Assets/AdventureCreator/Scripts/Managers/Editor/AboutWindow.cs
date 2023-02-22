#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	public class AboutWindow : EditorWindow
	{

		private static AboutWindow window;


		[MenuItem ("Adventure Creator/About", false, 20)]
		public static void Init ()
		{
			if (window != null)
			{
				return;
			}

			window = EditorWindow.GetWindowWithRect <AboutWindow> (new Rect (0, 0, 420, 340), true, "Adventure Creator", true);
			window.titleContent.text = "Adventure Creator";
		}


		private void OnGUI ()
		{
			GUILayout.BeginVertical (CustomStyles.thinBox, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));

			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			
			GUILayout.BeginVertical ();
			GUILayout.Space (20f);

			if (Resource.ACLogo)
			{
				GUI.DrawTexture (new Rect (80, 25, 256, 128), Resource.ACLogo);
				GUILayout.Space (132f);
			}
			else
			{
				GUILayout.Label ("Adventure Creator",  CustomStyles.managerHeader);
			}

			GUILayout.Label ("By Chris Burton, ICEBOX Studios",  CustomStyles.managerHeader);

			if (GUILayout.Button ("www.adventurecreator.org", CustomStyles.linkCentre))
			{
				Application.OpenURL (Resource.websiteLink);
			}
			GUILayout.Label ("<b>v" + AdventureCreator.version + "</b>",  CustomStyles.smallCentre);
			GUILayout.Space (12f);

			GUI.enabled = !UpdateChecker.IsChecking ();
			if (GUILayout.Button ("Check for updates"))
			{
				UpdateChecker.CheckForUpdate ();
			}
			GUI.enabled = true;

			if (GUILayout.Button ("Manual"))
			{
				Application.OpenURL (System.Environment.CurrentDirectory + "/" + Resource.MainFolderPath + "/Manual.pdf");
			}

			if (GUILayout.Button ("Tutorials"))
			{
				Application.OpenURL (Resource.tutorialsLink);
			}

			/*if (GUILayout.Button ("Asset Store page"))
			{
				Application.OpenURL (Resource.assetLink);
			}*/

			if (!ACInstaller.IsInstalled ())
			{
				if (GUILayout.Button ("Auto-configure Unity project settings"))
				{
					ACInstaller.DoInstall ();
				}
			}
			else
			{
				if (GUILayout.Button ("New Game Wizard"))
				{
					Close ();
					NewGameWizardWindow.Init ();
				}
			}

			GUILayout.EndVertical ();
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();
		}

	}

}

#endif