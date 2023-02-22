﻿#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberConversation), true)]
	public class RememberConversationEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI()
		{
			RememberConversation _target = (RememberConversation) target;
			
			if (_target.GetComponent <Conversation>() == null)
			{
				EditorGUILayout.HelpBox ("This script expects a Conversation component!", MessageType.Warning);
			}
			
			SharedGUI ();
		}
		
	}

}

#endif