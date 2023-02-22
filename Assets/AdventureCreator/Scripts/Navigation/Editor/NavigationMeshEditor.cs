#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (NavigationMesh))]
	public class NavigationMeshEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			NavigationMesh _target = (NavigationMesh) target;

			if (KickStarter.navigationManager)
			{
				KickStarter.navigationManager.ResetEngine ();
				if (KickStarter.navigationManager.navigationEngine != null)
				{
					CustomGUILayout.BeginVertical ();
					_target = KickStarter.navigationManager.navigationEngine.NavigationMeshGUI (_target);
					CustomGUILayout.EndVertical ();
				}
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}

#endif