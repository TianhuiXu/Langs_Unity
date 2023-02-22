#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (MenuLink))]
	public class MenuLinkEditor : Editor
	{

		private MenuLink _target;
		
		
		private void OnEnable ()
		{
			_target = (MenuLink) target;
		}

		public override void OnInspectorGUI ()
		{
			if (_target == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				EditorGUILayout.LabelField ("Menu name:", _target.menuName);
				EditorGUILayout.LabelField ("Element name:",_target.elementName);
				EditorGUILayout.LabelField ("Slot number:", _target.slot.ToString ());
				EditorGUILayout.LabelField ("Is visible?", _target.IsVisible ().ToString ());

				if (GUILayout.Button ("Interact"))
				{
					_target.Interact ();
				}
			}
			else
			{
				_target.menuName = CustomGUILayout.TextField ("Menu name:", _target.menuName, "", "The name of the associated Menu");
				_target.elementName = CustomGUILayout.TextField ("Element name:", _target.elementName, "", "The name of the associated MenuElement in the Menu above");
				_target.slot = CustomGUILayout.IntField ("Slot number (optional):", _target.slot, "", "The slot index of the associated MenuElement");
			}

			_target.setTextLabels = CustomGUILayout.Toggle ("Set guiText / TextMesh labels?", _target.setTextLabels, "", "If True, then any GUIText or TextMesh components will have their text values overridden by that of the associated MenuElement");

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif