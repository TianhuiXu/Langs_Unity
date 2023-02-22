/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"JsonAction.cs"
 * 
 *	A class used to convert Action data to and from Json serialization.  It is primarily used to copying/pasting Actions.
 * 
 */

#if UNITY_2019_2_OR_NEWER
#define NewCopying
#endif

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace AC
{

	[System.Serializable]
	public class JsonAction
	{

		#region Variables

		[SerializeField] private string className;
		[SerializeField] private string jsonData;
		public bool[] endingReferencesBuffer = new bool[0];
		public int[] endingOverrideIndex = new int[0];

		#if UNITY_EDITOR

		#if NewCopying
		private static JsonAction[] jsonCopiedActions = new JsonAction[0];
		private const string instanceChecker = "{\"instanceID\":";
		private static HashSet<ActionObjectReference> cachedGlobalObjectIds;
		#endif
		private static AC.Action[] copiedActions = new AC.Action[0];

		#endif

		#endregion


		#region Constructors

		private JsonAction (string _className, string _jsonData, int _endings)
		{
			className = _className;
			jsonData = _jsonData;
			endingReferencesBuffer = new bool[_endings];
			endingOverrideIndex = new int[_endings];
		}

		#endregion


		#region PrivateFunctions

		private Action CreateAction ()
		{
			if (string.IsNullOrEmpty (jsonData))
			{
				return null;
			}

			try
			{
				Action newAction = Action.CreateNew (className);
				JsonUtility.FromJsonOverwrite (jsonData, newAction);
				return newAction;
			}
			catch { return null; }
		}


		#if UNITY_EDITOR

		private void ClearIDs ()
		{
			Action action = CreateAction ();
			if (action == null) return;

			action.ClearIDs ();
			jsonData = JsonUtility.ToJson (action);
		}

		#endif


		#if UNITY_EDITOR && NewCopying

		private void InstanceToTarget (HashSet<ActionObjectReference> objectReferences)
		{
			foreach (ActionObjectReference objectReference in objectReferences)
			{
				string _old = instanceChecker + objectReference.InstanceID + "}";
				string _new = instanceChecker + objectReference.PersistentID + "}";

				if (jsonData.Contains (_old))
				{
					jsonData = jsonData.Replace (_old, _new);
				}
			}
		}


		private void TargetToInstance (HashSet<ActionObjectReference> objectReferences)
		{
			foreach (ActionObjectReference objectReference in objectReferences)
			{
				string _old = instanceChecker + objectReference.PersistentID + "}";
				string _new = instanceChecker + objectReference.InstanceID + "}";

				if (!string.IsNullOrEmpty (jsonData) && jsonData.Contains (_old))
				{
					jsonData = jsonData.Replace (_old, _new);
				}
			}
		}

		#endif

		#endregion


		#region StaticFunctions

		public static Action CreateCopy (Action action)
		{
			string jsonAction = JsonUtility.ToJson (action);
			string className = action.GetType ().ToString ();

			JsonAction copiedJsonAction = new JsonAction (className, jsonAction, action.endings.Count);
			Action copiedAction = copiedJsonAction.CreateAction ();
			return copiedAction;
		}


		#if UNITY_EDITOR

		/** Clears the copy buffer */
		public static void ClearCopyBuffer ()
		{
			copiedActions = new AC.Action[0];

			#if NewCopying
			jsonCopiedActions = new JsonAction[0];
			UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnEditorSceneChange;
			#endif
		}


		/**
		 * <summary>Stores an list of Actions in a temporary buffer</summary>
		 * <param name = "clearIDs">If True, then Speech Manager line IDs in the Actions will be reset</param>
		 * <param name="actions">The list of Actions to store.</param>
		 */
		public static void ToCopyBuffer (List<Action> actions, bool clearIDs = true)
		{
			#if NewCopying || AC_ActionListPrefabs
			jsonCopiedActions = BackupActions (actions, clearIDs);
			UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnEditorSceneChange;
			UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnEditorSceneChange;
			#endif

			#if !AC_ActionListPrefabs
			copiedActions = actions.ToArray ();

			copiedActions = new Action[actions.Count];
			for (int i=0; i<actions.Count; i++)
			{
				Action copyAction = Object.Instantiate (actions[i]) as Action;
				copyAction.name = copyAction.name.Replace ("(Clone)", "");
				copyAction.isMarked = false;

				if (clearIDs)
				{
					copyAction.ClearIDs ();
				}

				for (int e = 0; e < actions[i].endings.Count; e++)
				{
					if (actions[i].endings[e].resultAction == ResultAction.Skip && actions[i].endings[e].skipActionActual != null && actions.Contains (actions[i].endings[e].skipActionActual))
					{
						// References an Action inside the copy buffer, so record the index in the buffer
						copyAction.endings[e].skipAction = -10 - actions.IndexOf (actions[i].endings[e].skipActionActual);
						copyAction.endings[e].skipActionActual = null;
					}
				}

				copiedActions[i] = copyAction;
			}
			#endif
		}


		/**
		 * <summary>Generates Actions based on the buffer created with ToCopyBuffer</summary>
		 * <param name = "clearIDs">If True, then Speech Manager line IDs in the Actions will be reset</param>
		 * <returns>The Actions stored in the buffer, recreated.</returns>
		 */
		public static List<Action> CreatePasteBuffer (bool clearIDs = true)
		{
			#if AC_ActionListPrefabs
			return RestoreActions (jsonCopiedActions, true);
			#else

			List<AC.Action> tempList = new List<AC.Action>();
			foreach (AC.Action action in copiedActions)
			{
				if (action != null)
				{
					Action copyAction = Object.Instantiate (action) as Action;
					if (clearIDs)
					{
						copyAction.ClearIDs ();
					}
					foreach (ActionEnd ending in copyAction.endings)
					{
						ending.skipActionActual = null;
					}
					tempList.Add (copyAction);
				}
			}

			foreach (AC.Action action in tempList)
			{
				foreach (ActionEnd ending in action.endings)
				{
					if (ending.resultAction == ResultAction.Skip)
					{
						// Correct skip endings for those that reference others in the same list
						bool endingIsOffset = ending.skipAction <= -10;
						if (endingIsOffset)
						{
							int newIndex = -(ending.skipAction + 10);
							if (newIndex >= 0 && newIndex < tempList.Count)
							{
								ending.skipActionActual = tempList[newIndex];
								ending.skipAction = -1;
							}
						}
						else
						{
							ending.resultAction = ResultAction.Stop;
						}
					}
				}
			}
			
			//copiedActions = new AC.Action[0];
			return tempList;

			#endif
		}


		/** Return True if Action data is stored in the copy buffer */
		public static bool HasCopyBuffer ()
		{
			#if AC_ActionListPrefabs
			return (jsonCopiedActions != null && jsonCopiedActions.Length > 0);
			#else
			return (copiedActions != null && copiedActions.Length > 0);
			#endif
		}

		#if NewCopying

		private static void OnEditorSceneChange (UnityEngine.SceneManagement.Scene sceneOne, UnityEngine.SceneManagement.Scene sceneTwo)
		{
			if (jsonCopiedActions != null && jsonCopiedActions.Length > 0)
			{
				copiedActions = RestoreActions (jsonCopiedActions, true).ToArray ();
			}
		}


		public static JsonAction[] BackupActions (List<Action> actions, bool clearIDs = false)
		{
			int length = actions.Count;
			JsonAction[] backupActions = new JsonAction[length];

			if (length == 0 || (length == 1 && actions[0] == null))
			{
				return null;
			}

			// Create initial Json data
			for (int i = 0; i < length; i++)
			{
				if (actions[i] == null)
				{
					backupActions[i] = null;
					return null;
				}

				string jsonAction = JsonUtility.ToJson (actions[i]);

				string className = actions[i].GetType ().ToString ();
				backupActions[i] = new JsonAction (className, jsonAction, actions[i].endings.Count);

				if (clearIDs)
				{
					backupActions[i].ClearIDs ();
				}

				for (int e = 0; e < actions[i].endings.Count; e++)
				{
					if (actions[i].endings[e].resultAction == ResultAction.Skip && actions[i].endings[e].skipActionActual != null && actions.Contains (actions[i].endings[e].skipActionActual))
					{
						// References an Action inside the copy buffer, so record the index in the buffer
						backupActions[i].endingReferencesBuffer[e] = true;
						backupActions[i].endingOverrideIndex[e] = actions.IndexOf (actions[i].endings[e].skipActionActual);
					}
				}
			}

			// Get reference data for all scene objects
			if (cachedGlobalObjectIds != null)
			{
				// Amend Json data by replacing InstanceID references with TargetID references
				foreach (JsonAction backupAction in backupActions)
				{
					if (backupAction != null)
					{
						backupAction.InstanceToTarget (cachedGlobalObjectIds);
					}
				}
			}
			else
			{
				HashSet<ActionObjectReference> globalObjectIds = GetSceneObjectReferences ();

				// Amend Json data by replacing InstanceID references with TargetID references
				foreach (JsonAction backupAction in backupActions)
				{
					if (backupAction != null)
					{
						backupAction.InstanceToTarget (globalObjectIds);
					}
				}
			}

			return backupActions;
		}


		public static void CacheSceneObjectReferences ()
		{
			cachedGlobalObjectIds = GetSceneObjectReferences ();
		}


		public static void ClearSceneObjectReferencesCache ()
		{
			cachedGlobalObjectIds = null;
		}


		public static List<Action> RestoreActions (JsonAction[] jsonActions, bool createNew = false)
		{
			HashSet<ActionObjectReference> globalObjectIds = GetSceneObjectReferences ();

			// Amend Json data by replacing TargetID references with InstanceID references
			foreach (JsonAction jsonAction in jsonActions)
			{
				jsonAction.TargetToInstance (globalObjectIds);
			}

			// Create Actions fromJson
			List<Action> newActions = new List<Action> ();
			for (int i = 0; i < jsonActions.Length; i++)
			{
				if (jsonActions[i] == null)
				{
					continue;
				}

				Action newAction = jsonActions[i].CreateAction ();

				if (newAction == null)
				{
					ACDebug.LogWarning ("Error when pasting Action - cannot find original " + jsonActions[i].className);
				}
				else if (createNew)
				{
					newAction.ClearIDs ();
					//newAction.NodeRect = new Rect (0, 0, 300, 60);
				}

				newActions.Add (newAction);
			}

			// Correct skip endings for those that reference others in the same list
			for (int i = 0; i < newActions.Count; i++)
			{
				if (newActions[i] == null)
				{
					continue;
				}

				for (int e = 0; e < jsonActions[i].endingReferencesBuffer.Length; e++)
				{
					bool endingIsOffset = jsonActions[i].endingReferencesBuffer[e];
					if (endingIsOffset)
					{
						newActions[i].endings[e].skipAction = -1;
						newActions[i].endings[e].skipActionActual = newActions[jsonActions[i].endingOverrideIndex[e]];
					}
				}
			}

			// Remove nulls
			for (int i = 0; i < newActions.Count; i++)
			{
				if (newActions[i] == null)
				{
					newActions.RemoveAt (i);
					i = -1;
				}
			}

			return newActions;
		}


		private static HashSet<ActionObjectReference> GetSceneObjectReferences ()
		{
			HashSet<Object> sceneObjects = new HashSet<Object> ();
			GameObject[] sceneGameObjects = Object.FindObjectsOfType<GameObject> ();
			foreach (GameObject sceneGameObject in sceneGameObjects)
			{
				sceneObjects.Add (sceneGameObject);
				Component[] components = sceneGameObject.GetComponents<Component> ();
				foreach (Component component in components)
				{
					sceneObjects.Add (component);
				}
			}

			HashSet<ActionObjectReference> objectReferences = new HashSet<ActionObjectReference> ();
			foreach (Object sceneObject in sceneObjects)
			{
				if (sceneObject == null) continue;
				objectReferences.Add (new ActionObjectReference (sceneObject));
			}
			return objectReferences;
		}

		#endif

		#endif

		#endregion


		#region PrivateClasses

		#if UNITY_EDITOR && NewCopying

		private class ActionObjectReference
		{

			private string targetObjectID;
			private string targetPrefabID;
			public string InstanceID { get; private set; }


			public ActionObjectReference (Object _object)
			{
				GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow (_object);
				targetObjectID = globalObjectId.targetObjectId.ToString ();
				targetPrefabID = globalObjectId.targetPrefabId.ToString ();
				InstanceID = _object.GetInstanceID ().ToString ();
			}


			public string PersistentID { get { return targetObjectID + "_" + targetPrefabID; } }

		}

		#endif

		#endregion

	}

}