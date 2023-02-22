#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{

	public class ActiveInputsEditor : EditorWindow
	{

		private SettingsManager settingsManager;
		private Vector2 scrollPos;
		private ActiveInput selectedActiveInput;
		private int sideInput = -1;

		private bool showActiveInputsList = true;
		private bool showSelectedActiveInput = true;


		[MenuItem ("Adventure Creator/Editors/Active Inputs Editor", false, 0)]
		public static void Init ()
		{
			ActiveInputsEditor window = (ActiveInputsEditor) GetWindow (typeof (ActiveInputsEditor));
			window.titleContent.text = "Active Inputs";
			window.position = new Rect (300, 200, 450, 490);
			window.minSize = new Vector2 (300, 180);
		}


		private void OnEnable ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().settingsManager)
			{
				settingsManager = AdvGame.GetReferences ().settingsManager;
			}
		}


		private void OnGUI ()
		{
			if (settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField ("Active inputs", CustomStyles.managerHeader);

			ActiveInput.Upgrade ();
			ShowActiveInputsGUI ();

			UnityVersionHandler.CustomSetDirty (settingsManager);
		}


		private void ShowActiveInputsGUI ()
		{
			EditorGUILayout.HelpBox ("Active Inputs are used to trigger ActionList assets when an input key is pressed under certain gameplay conditions.", MessageType.Info);

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showActiveInputsList = CustomGUILayout.ToggleHeader (showActiveInputsList, "Active inputs");
			if (showActiveInputsList)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
				foreach (ActiveInput activeInput in settingsManager.activeInputs)
				{
					EditorGUILayout.BeginHorizontal ();

					if (GUILayout.Toggle (selectedActiveInput == activeInput, activeInput.ID + ": " + activeInput.Label, "Button"))
					{
						if (selectedActiveInput != activeInput)
						{
							DeactivateAllInputs ();
							ActivateInput (activeInput);
						}
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (activeInput);
					}

					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Create new Active Input"))
				{
					Undo.RecordObject (this, "Create new Active Input");

					if (settingsManager.activeInputs.Count > 0)
					{
						List<int> idArray = new List<int> ();
						foreach (ActiveInput activeInput in settingsManager.activeInputs)
						{
							idArray.Add (activeInput.ID);
						}
						idArray.Sort ();

						ActiveInput newActiveInput = new ActiveInput (idArray.ToArray ());
						settingsManager.activeInputs.Add (newActiveInput);

						DeactivateAllInputs ();
						ActivateInput (newActiveInput);
					}
					else
					{
						ActiveInput newActiveInput = new ActiveInput (1);
						settingsManager.activeInputs.Add (newActiveInput);
					}
				}

				if (settingsManager.activeInputs.Count > 1)
				{
					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						GlobalSideMenu ();
					}
				}
				EditorGUILayout.EndHorizontal ();
				CustomGUILayout.EndVertical ();
			}

			EditorGUILayout.Space ();

			if (selectedActiveInput != null && settingsManager.activeInputs.Contains (selectedActiveInput))
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showSelectedActiveInput = CustomGUILayout.ToggleHeader (showSelectedActiveInput, "Input #" + selectedActiveInput.ID + ": " + selectedActiveInput.label);
				if (showSelectedActiveInput)
				{
					selectedActiveInput.ShowGUI ();
				}
				CustomGUILayout.EndVertical ();
			}
		}


		private void SideMenu (ActiveInput activeInput)
		{
			GenericMenu menu = new GenericMenu ();
			sideInput = settingsManager.activeInputs.IndexOf (activeInput);

			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (settingsManager.activeInputs.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (sideInput > 0 || sideInput < settingsManager.activeInputs.Count - 1)
			{
				menu.AddSeparator ("");
			}
			if (sideInput > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (sideInput < settingsManager.activeInputs.Count - 1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			if (sideInput >= 0)
			{
				ActiveInput tempInput = settingsManager.activeInputs[sideInput];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (settingsManager, "Insert Active Input");
						settingsManager.activeInputs.Insert (sideInput + 1, new ActiveInput (GetIDList ().ToArray ()));
						break;

					case "Delete":
						Undo.RecordObject (settingsManager, "Delete Active Input");
						if (tempInput == selectedActiveInput)
						{
							DeactivateAllInputs ();
						}
						settingsManager.activeInputs.RemoveAt (sideInput);
						break;

					case "Move up":
						Undo.RecordObject (settingsManager, "Move Active Input up");
						settingsManager.activeInputs.RemoveAt (sideInput);
						settingsManager.activeInputs.Insert (sideInput - 1, tempInput);
						break;

					case "Move down":
						Undo.RecordObject (settingsManager, "Move Active Input down");
						settingsManager.activeInputs.RemoveAt (sideInput);
						settingsManager.activeInputs.Insert (sideInput + 1, tempInput);
						break;

					case "Move to top":
						Undo.RecordObject (settingsManager, "Move Active Input to top");
						settingsManager.activeInputs.RemoveAt (sideInput);
						settingsManager.activeInputs.Insert (0, tempInput);
						break;

					case "Move to bottom":
						Undo.RecordObject (settingsManager, "Move Active Input to bottom");
						settingsManager.activeInputs.Add (tempInput);
						settingsManager.activeInputs.RemoveAt (sideInput);
						break;
				}
			}

			EditorUtility.SetDirty (settingsManager);
			AssetDatabase.SaveAssets ();

			sideInput = -1;
		}


		private void GlobalSideMenu ()
		{
			GenericMenu menu = new GenericMenu ();
			menu.AddItem (new GUIContent ("Delete all"), false, GlobalCallback, "Delete all");
			menu.ShowAsContext ();
		}


		private void GlobalCallback (object obj)
		{
			switch (obj.ToString ())
			{
				case "Delete all":
					Undo.RecordObject (settingsManager, "Delete all Active Inputs");
					selectedActiveInput = null;
					settingsManager.activeInputs.Clear ();
					break;

				default:
					break;
			}

			EditorUtility.SetDirty (settingsManager);
			AssetDatabase.SaveAssets ();
		}


		private void DeactivateAllInputs ()
		{
			selectedActiveInput = null;
		}


		private List<int> GetIDList ()
		{
			List<int> idList = new List<int> ();
			foreach (ActiveInput activeInput in settingsManager.activeInputs)
			{
				idList.Add (activeInput.ID);
			}

			idList.Sort ();

			return idList;
		}


		private void ActivateInput (ActiveInput activeInput)
		{
			selectedActiveInput = activeInput;
			EditorGUIUtility.editingTextField = false;
		}

	}

}

#endif