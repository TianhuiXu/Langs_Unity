#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	public class ToolbarLinks : EditorWindow
	{

		[MenuItem ("Adventure Creator/Online resources/Website", false, 0)]
		static void Website ()
		{
			Application.OpenURL (Resource.websiteLink);
		}


		[MenuItem ("Adventure Creator/Online resources/Tutorials", false, 1)]
		static void Tutorials ()
		{
			Application.OpenURL (Resource.tutorialsLink);
		}


		[MenuItem ("Adventure Creator/Online resources/Downloads", false, 2)]
		static void Downloads ()
		{
			Application.OpenURL (Resource.downloadsLink);
		}


		[MenuItem ("Adventure Creator/Online resources/Forum", false, 3)]
		static void Forum ()
		{
			Application.OpenURL (Resource.forumLink);
		}


		[MenuItem ("Adventure Creator/Online resources/Scripting guide", false, 4)]
		static void ScriptingGuide ()
		{
			Application.OpenURL (Resource.scriptingGuideLink);
		}


		[MenuItem ("Adventure Creator/Online resources/Community wiki", false, 5)]
		static void Wiki ()
		{
			Application.OpenURL (Resource.wikiLink);
		}

	}

}

#endif