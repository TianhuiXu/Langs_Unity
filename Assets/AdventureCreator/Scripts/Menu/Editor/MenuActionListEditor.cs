#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(MenuActionList))]
	[System.Serializable]
	public class MenuActionListEditor : ActionListAssetEditor
	{ }

}

#endif