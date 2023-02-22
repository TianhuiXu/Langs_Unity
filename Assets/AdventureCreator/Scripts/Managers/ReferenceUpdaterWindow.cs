#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace AC
{

	public class ReferenceUpdaterWindow : EditorWindow
	{

		private int oldID;
		private int newID;
		public enum ReferenceType { InventoryItem, Document, Objective, GlobalVariable };
		private ReferenceType referenceType;
		private string label;


		public static void Init (ReferenceType referenceType, string label, int oldID)
		{
			ReferenceUpdaterWindow window = (ReferenceUpdaterWindow) GetWindow (typeof (ReferenceUpdaterWindow), true);
			window.titleContent = new GUIContent ("Reference updater - " + label);
			window.referenceType = referenceType;
			window.oldID = oldID;
			window.label = label;

			window.minSize = new Vector2 (300, 120);
			window.maxSize = new Vector2 (300, 120);

			window.Show ();
		}


		private void OnGUI ()
		{
			EditorGUILayout.LabelField ("Reference name:", label);
			EditorGUILayout.LabelField ("Reference type:", referenceType.ToString ());
			EditorGUILayout.LabelField ("Existing ID:", oldID.ToString ());
			newID = EditorGUILayout.IntField ("New ID:", newID);
			if (newID < 0) newID = 0;

			GUI.enabled = newID != oldID;

			if (GUILayout.Button ("Change ID"))
			{
				GUI.enabled = true;
				switch (referenceType)
				{
					case ReferenceType.InventoryItem:
						KickStarter.inventoryManager.ChangeItemID (oldID, newID);
						Close ();
						break;

					case ReferenceType.Document:
						KickStarter.inventoryManager.ChangeDocumentID (oldID, newID);
						break;

					case ReferenceType.Objective:
						KickStarter.inventoryManager.ChangeObjectiveID (oldID, newID);
						break;

					case ReferenceType.GlobalVariable:
						KickStarter.variablesManager.ChangeGlobalVariableID (oldID, newID);
						break;

					default:
						break;
				}
			}

			GUI.enabled = true;
		}

	}

}

#endif