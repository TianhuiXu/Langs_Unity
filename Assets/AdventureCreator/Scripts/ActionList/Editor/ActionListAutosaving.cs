/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionListAutosaving.cs"
 * 
 *	A class used to autosave ActionList data in the scene, in case of data loss.
 *	This script's code is adapted from liortal53's code available at: https://github.com/liortal53/AutoSaveScene/blob/master/Assets/Editor/AutoSaveScene.cs
 * 
 */

using System;
using UnityEditor;

namespace AC
{

	public class ActionListAutosaving
	{

		#if UNITY_2019_2_OR_NEWER && UNITY_EDITOR

		#region Variables

		private static DateTime lastSaveTime = DateTime.Now;
		private static TimeSpan updateInterval;

		#endregion


		#region PublicFunctions

		[InitializeOnLoadMethod]
		public static void Register ()
		{
			updateInterval = new TimeSpan (0, ACEditorPrefs.AutosaveActionListsInterval, 0);

			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}

		#endregion


		#region PrivateFunctions

		private static void OnUpdate ()
		{
			if (ACEditorPrefs.AutosaveActionListsInterval <= 0)
			{
				return;
			}

			if ((DateTime.Now - lastSaveTime) >= updateInterval)
			{
				updateInterval = new TimeSpan (0, ACEditorPrefs.AutosaveActionListsInterval, 0);

				AutosaveActions ();
				lastSaveTime = DateTime.Now;
			}
		}


		private static void AutosaveActions ()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			ActionList[] actionLists = UnityEngine.Object.FindObjectsOfType<ActionList> ();
			if (actionLists.Length == 0)
			{
				return;
			}

			ACDebug.Log ("Auto-saving ActionLists.\nThis feature can be amended or disabled in Unity's Project Settings.");
			JsonAction.CacheSceneObjectReferences ();
			foreach (ActionList actionList in actionLists)
			{
				actionList.BackupData ();
			}
			JsonAction.ClearSceneObjectReferencesCache ();
		}

		#endregion

		#endif

	}

}