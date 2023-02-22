#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace AC
{

	[CustomEditor (typeof (ManagerPackage))]

	[System.Serializable]
	public class ManagerPackageEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			ManagerPackage _target = (ManagerPackage) target;

			CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("Manager asset files", EditorStyles.boldLabel);
				_target.sceneManager = (SceneManager) EditorGUILayout.ObjectField ("Scene manager:", _target.sceneManager, typeof (SceneManager), false);
				_target.settingsManager = (SettingsManager) EditorGUILayout.ObjectField ("Settings manager:", _target.settingsManager, typeof (SettingsManager), false);
				_target.actionsManager = (ActionsManager) EditorGUILayout.ObjectField ("Actions manager:", _target.actionsManager, typeof (ActionsManager), false);
				_target.variablesManager = (VariablesManager) EditorGUILayout.ObjectField ("Variables manager:", _target.variablesManager, typeof (VariablesManager), false);
				_target.inventoryManager = (InventoryManager) EditorGUILayout.ObjectField ("Inventory manager:", _target.inventoryManager, typeof (InventoryManager), false);
				_target.speechManager = (SpeechManager) EditorGUILayout.ObjectField ("Speech manager:", _target.speechManager, typeof (SpeechManager), false);
				_target.cursorManager = (CursorManager) EditorGUILayout.ObjectField ("Cursor manager:", _target.cursorManager, typeof (CursorManager), false);
				_target.menuManager = (MenuManager) EditorGUILayout.ObjectField ("Menu manager:", _target.menuManager, typeof (MenuManager), false);
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			if (GUILayout.Button ("Assign managers"))
			{
				Undo.RecordObject (AdvGame.GetReferences (), "Assign managers");
				_target.AssignManagers ();
				AdventureCreator.RefreshActions ();
				AdventureCreator.Init ();
			}

			EditorUtility.SetDirty (_target);
		}


		[OnOpenAssetAttribute(2)]
		public static bool OnOpenAsset (int instanceID, int line)
		{
			if (Selection.activeObject is ManagerPackage)
			{
				ManagerPackage managerPackage = (ManagerPackage) Selection.activeObject as ManagerPackage;
				Undo.RecordObject (AdvGame.GetReferences (), "Assign managers");
				managerPackage.AssignManagers ();
				AdventureCreator.RefreshActions ();
				AdventureCreator.Init ();
				return true;
			}
			return false;
		}

	}

}

#endif