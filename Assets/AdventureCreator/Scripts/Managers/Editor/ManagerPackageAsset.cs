#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	public class ManagerPackageAsset
	{
		
		[MenuItem ("Assets/Create/Adventure Creator/Manager Package")]
		public static void CreateAsset ()
		{
			ScriptableObject t = CustomAssetUtility.CreateAsset <ManagerPackage> ("New ManagerPackage");
			EditorGUIUtility.PingObject (t);
		}

	}

}

#endif