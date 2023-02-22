#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{

	public class TimersEditor : EditorWindow
	{

		private VariablesManager variablesManager;
		private Vector2 scrollPos;
		private Timer selectedTimer;
		private int sideTimer = -1;

		private bool showTimersList = true;
		private bool showSelectedTimer = true;


		[MenuItem ("Adventure Creator/Editors/Timers Editor", false, 7)]
		public static void Init ()
		{
			TimersEditor window = (TimersEditor) GetWindow (typeof (TimersEditor));
			window.titleContent.text = "Timers";
			window.position = new Rect (300, 200, 450, 490);
			window.minSize = new Vector2 (300, 180);
		}


		private void OnEnable ()
		{
			if (AdvGame.GetReferences () && AdvGame.GetReferences ().variablesManager)
			{
				variablesManager = AdvGame.GetReferences ().variablesManager;
			}
		}


		private void OnGUI ()
		{
			if (variablesManager == null)
			{
				EditorGUILayout.HelpBox ("A Variables Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField ("Timers", CustomStyles.managerHeader);

			ShowTimersGUI ();

			UnityVersionHandler.CustomSetDirty (variablesManager);
		}


		private void ShowTimersGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showTimersList = CustomGUILayout.ToggleHeader (showTimersList, "Timers");
			if (showTimersList)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
				foreach (Timer timer in variablesManager.timers)
				{
					EditorGUILayout.BeginHorizontal ();

					string label = timer.ID + ": " + timer.Label;
					if (Application.isPlaying && timer.IsRunning)
					{ 
						label += " (RUNNING)";
					}

					if (GUILayout.Toggle (selectedTimer == timer, label, "Button"))
					{
						if (selectedTimer != timer)
						{
							DeactivateAllTimers ();
							ActivateTimer (timer);
						}
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (timer);
					}

					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();

				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Create new Timer"))
				{
					Undo.RecordObject (this, "Create new Timer");

					if (variablesManager.timers.Count > 0)
					{
						List<int> idArray = new List<int> ();
						foreach (Timer timer in variablesManager.timers)
						{
							idArray.Add (timer.ID);
						}
						idArray.Sort ();

						Timer newTimer = new Timer (idArray.ToArray ());
						variablesManager.timers.Add (newTimer);

						DeactivateAllTimers ();
						ActivateTimer (newTimer);
					}
					else
					{
						Timer newTimer = new Timer ();
						variablesManager.timers.Add (newTimer);
						ActivateTimer (newTimer);
					}
				}

				if (variablesManager.timers.Count > 1)
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

			if (selectedTimer != null && variablesManager.timers.Contains (selectedTimer))
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);

				showSelectedTimer = CustomGUILayout.ToggleHeader (showSelectedTimer, "Timer #" + selectedTimer.ID + ": " + selectedTimer.Label);
				if (showSelectedTimer)
				{
					selectedTimer.ShowGUI ();
				}
				CustomGUILayout.EndVertical ();
			}
		}


		private void SideMenu (Timer timer)
		{
			GenericMenu menu = new GenericMenu ();
			sideTimer = variablesManager.timers.IndexOf (timer);

			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (variablesManager.timers.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (sideTimer > 0 || sideTimer < variablesManager.timers.Count - 1)
			{
				menu.AddSeparator ("");
			}
			if (sideTimer > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (sideTimer < variablesManager.timers.Count - 1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}

			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			if (sideTimer >= 0)
			{
				Timer tempTimer = variablesManager.timers[sideTimer];

				switch (obj.ToString ())
				{
					case "Insert after":
						Undo.RecordObject (variablesManager, "Insert Timer");
						variablesManager.timers.Insert (sideTimer + 1, new Timer (GetIDList ().ToArray ()));
						break;

					case "Delete":
						Undo.RecordObject (variablesManager, "Delete Timer");
						if (tempTimer == selectedTimer)
						{
							DeactivateAllTimers ();
						}
						variablesManager.timers.RemoveAt (sideTimer);
						break;

					case "Move up":
						Undo.RecordObject (variablesManager, "Move Timer up");
						variablesManager.timers.RemoveAt (sideTimer);
						variablesManager.timers.Insert (sideTimer - 1, tempTimer);
						break;

					case "Move down":
						Undo.RecordObject (variablesManager, "Move Timer down");
						variablesManager.timers.RemoveAt (sideTimer);
						variablesManager.timers.Insert (sideTimer + 1, tempTimer);
						break;

					case "Move to top":
						Undo.RecordObject (variablesManager, "Move Timer to top");
						variablesManager.timers.RemoveAt (sideTimer);
						variablesManager.timers.Insert (0, tempTimer);
						break;

					case "Move to bottom":
						Undo.RecordObject (variablesManager, "Move Timer to bottom");
						variablesManager.timers.Add (tempTimer);
						variablesManager.timers.RemoveAt (sideTimer);
						break;
				}
			}

			EditorUtility.SetDirty (variablesManager);
			AssetDatabase.SaveAssets ();

			sideTimer = -1;
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
					Undo.RecordObject (variablesManager, "Delete all Timers");
					selectedTimer = null;
					variablesManager.timers.Clear ();
					break;

				default:
					break;
			}

			EditorUtility.SetDirty (variablesManager);
			AssetDatabase.SaveAssets ();
		}


		private void DeactivateAllTimers ()
		{
			selectedTimer = null;
		}


		private List<int> GetIDList ()
		{
			List<int> idList = new List<int> ();
			foreach (Timer timer in variablesManager.timers)
			{
				idList.Add (timer.ID);
			}

			idList.Sort ();

			return idList;
		}


		private void ActivateTimer (Timer timer)
		{
			selectedTimer = timer;
			EditorGUIUtility.editingTextField = false;
		}

	}

}

#endif