#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(InvActionList))]

	[System.Serializable]
	public class InvActionListEditor : ActionListAssetEditor
	{ }

}

#endif