/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSendMessage.cs"
 * 
 *	This action calls "SendMessage" on a GameObject.
 *	Both standard messages, and custom ones with paremeters, can be sent.
 * 
 */

using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionEvent : Action
	{
		
		public UnityEvent unityEvent;
		public UnityEvent skipEvent;
		public bool ignoreWhenSkipping = false;
		
		
		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Call event"; }}
		public override string Description { get { return "Calls a given function on a GameObject."; }}


		public override float Run ()
		{
			if (unityEvent != null)
			{
				unityEvent.Invoke ();
			}
			
			return 0f;
		}

		
		public override void Skip ()
		{
			if (!ignoreWhenSkipping)
			{
				Run ();
			}
			else if (skipEvent != null)
			{
				skipEvent.Invoke ();
			}
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			if (this == null) return;

			#if AC_ActionListPrefabs
			#else
			var serializedObject = new SerializedObject (this);

			serializedObject.Update ();
			SerializedProperty eventProperty = serializedObject.FindProperty ("unityEvent");
			EditorGUILayout.PropertyField (eventProperty, true);

			ignoreWhenSkipping = EditorGUILayout.Toggle ("Ignore when skipping?", ignoreWhenSkipping);
			if (ignoreWhenSkipping)
			{
				SerializedProperty skipEventProperty = serializedObject.FindProperty ("skipEvent");
				EditorGUILayout.PropertyField (skipEventProperty, true);
			}

			serializedObject.ApplyModifiedProperties ();
			#endif

			#if !UNITY_2019_1_OR_NEWER
			EditorGUILayout.HelpBox ("Parameters passed from here cannot be set, unfortunately, due to a Unity limitation.", MessageType.Warning);
			#endif
		}

		#endif
		
	}
	
}