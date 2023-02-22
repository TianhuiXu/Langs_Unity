#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Callbacks;

namespace AC
{

	[CustomEditor(typeof(ActionListAsset))]
	[System.Serializable]
	public class ActionListAssetEditor : Editor
	{

		private AC.Action actionToAffect;
		private ActionsManager actionsManager;


		private void OnEnable ()
		{
			if (AdvGame.GetReferences ())
			{
				if (AdvGame.GetReferences ().actionsManager)
				{
					actionsManager = AdvGame.GetReferences ().actionsManager;
					AdventureCreator.RefreshActions ();
				}
			}
		}
		

		public override void OnInspectorGUI ()
		{
			ActionListAsset _target = (ActionListAsset) target;

			ShowPropertiesGUI (_target);
			EditorGUILayout.Space ();

			if (actionsManager == null)
			{
				EditorGUILayout.HelpBox ("An Actions Manager asset file must be assigned in the Game Editor Window", MessageType.Warning);
				OnEnable ();
				UnityVersionHandler.CustomSetDirty (_target);
				return;
			}

			if (actionsManager.displayActionsInInspector)
			{
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Expand all", EditorStyles.miniButtonLeft))
				{
					Undo.RecordObject (_target, "Expand actions");
					foreach (AC.Action action in _target.actions)
					{
						action.isDisplayed = true;
					}
				}
				if (GUILayout.Button ("Collapse all", EditorStyles.miniButtonMid))
				{
					Undo.RecordObject (_target, "Collapse actions");
					foreach (AC.Action action in _target.actions)
					{
						action.isDisplayed = false;
					}
				}
				if (GUILayout.Button ("Action List Editor", EditorStyles.miniButtonMid))
				{
					ActionListEditorWindow.Init (_target);
				}

				GUI.enabled = Application.isPlaying;

				bool isRunning = false;
				if (Application.isPlaying)
				{
					if (KickStarter.actionListAssetManager != null)
					{
						isRunning = KickStarter.actionListAssetManager.IsListRunning (_target) && !_target.canRunMultipleInstances;
					}
				}

				if (isRunning)
				{
					if (GUILayout.Button("Run now", EditorStyles.miniButtonRight))
					{
						_target.KillAllInstances ();
					}
				}
				else
				{
					if (GUILayout.Button ("Run now", EditorStyles.miniButtonRight))
					{
						if (KickStarter.actionListAssetManager != null)
						{
							if (!_target.canRunMultipleInstances)
							{
								int numRemoved = KickStarter.actionListAssetManager.EndAssetList (_target);
								if (numRemoved > 0)
								{
									ACDebug.Log ("Removed 1 instance of ActionList asset '" + _target.name + "' because it is set to only run one at a time.", _target);
								}
							}

							AdvGame.RunActionListAsset (_target);
						}
						else
						{
							ACDebug.LogWarning ("An AC PersistentEngine object must be present in the scene for ActionList assets to run.", _target);
						}
					}
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
			}
			else
			{
				if (Application.isPlaying)
				{
					EditorGUILayout.BeginHorizontal ();
					if (GUILayout.Button ("Edit Actions", GUILayout.Height (40f)))
					{
						ActionListEditorWindow.Init (_target);
					}
					if (GUILayout.Button ("Run now", GUILayout.Height (40f)))
					{
						AdvGame.RunActionListAsset (_target);
					}
					EditorGUILayout.EndHorizontal ();
				}
				else
				{
					if (GUILayout.Button ("Edit Actions", GUILayout.Height (40f)))
					{
						ActionListEditorWindow.Init (_target);
					}
				}
				UnityVersionHandler.CustomSetDirty (_target);
				return;
			}

			EditorGUILayout.Space ();

			for (int i=0; i<_target.actions.Count; i++)
			{
				int typeIndex = actionsManager.GetActionTypeIndex (_target.actions[i]);

				if (_target.actions[i] == null)
				{
					RebuildAction (_target.actions[i], typeIndex, _target, i);
				}

				if (_target.actions[i] == null)
				{
					continue;
				}
				
				_target.actions[i].isAssetFile = true;
				
				CustomGUILayout.BeginVertical ();

				string actionLabel = " (" + i + ") " + actionsManager.GetActionTypeLabel (_target.actions[i], true);
				actionLabel = actionLabel.Replace("\r\n", "");
				actionLabel = actionLabel.Replace("\n", "");
				actionLabel = actionLabel.Replace("\r", "");
				if (actionLabel.Length > 40)
				{
					actionLabel = actionLabel.Substring (0, 40) + "..)";
				}

				EditorGUILayout.BeginHorizontal ();
				_target.actions[i].isDisplayed = EditorGUILayout.Foldout (_target.actions[i].isDisplayed, actionLabel);
				if (!_target.actions[i].isEnabled)
				{
					EditorGUILayout.LabelField ("DISABLED", EditorStyles.boldLabel, GUILayout.Width (100f));
				}

				if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
				{
					ActionSideMenu (_target.actions[i]);
				}
				EditorGUILayout.EndHorizontal ();
				
				if (_target.actions[i].isDisplayed)
				{
					if (!actionsManager.DoesActionExist (_target.actions[i].GetType ().ToString ()))
					{
						EditorGUILayout.HelpBox ("This Action type is not listed in the Actions Manager", MessageType.Warning);
					}
					else
					{
						int newTypeIndex = ActionListEditor.ShowTypePopup (_target.actions[i], typeIndex);
						if (newTypeIndex >= 0)
						{
							// Rebuild constructor if Subclass and type string do not match
							ActionEnd _end = (_target.actions[i].endings.Count > 0) ? new ActionEnd (_target.actions[i].endings[0]) : null;
							
							Undo.RecordObject (_target, "Change Action type");

							RebuildAction (_target.actions[i], newTypeIndex, _target, i, _end);
						}

						EditorGUILayout.Space ();
						GUI.enabled = _target.actions[i].isEnabled;

						if (Application.isPlaying)
						{
							_target.actions[i].AssignValues (_target.GetParameters ());
						}

						_target.actions[i].ShowGUI (_target.GetParameters ());
					}
					GUI.enabled = true;
				}
				
				_target.actions[i].SkipActionGUI (_target.actions, _target.actions[i].isDisplayed);
				
				CustomGUILayout.EndVertical ();
				EditorGUILayout.Space ();
			}
			
			if (GUILayout.Button ("Add new Action"))
			{
				Undo.RecordObject (_target, "Create action");
				AddAction (ActionsManager.GetDefaultAction (), _target.actions.Count, _target);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		public static Action RebuildAction (AC.Action action, int typeIndex, ActionListAsset _target, int insertIndex = -1, ActionEnd _end = null)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			
			if (actionsManager)
			{
				if (typeIndex < 0 || typeIndex >= actionsManager.AllActions.Count) return action;

				bool _showComment = (action != null) ? action.showComment : false;
				bool _showOutputSockets = (action != null) ? action.showOutputSockets : true;
				string _comment = (action != null) ? action.comment : string.Empty;

				DeleteAction (action, _target);

				ActionType actionType = actionsManager.AllActions[typeIndex];
				string className = actionType.fileName;

				Action newAction = Action.CreateNew (className);
				
				if (_end != null)
				{
					newAction.endings.Add (new ActionEnd (_end));
				}

				newAction.showComment = _showComment;
				newAction.comment = _comment;
				newAction.showOutputSockets = _showOutputSockets;

				if (insertIndex >= 0)
				{
					_target.actions.Insert (insertIndex, newAction);
				}

				ActionListAsset.SyncAssetObjects (_target);

				return newAction;
			}
			
			return action;
		}


		public static void DeleteAction (AC.Action action, ActionListAsset _target)
		{
			if (action != null) 
			{
				_target.actions.Remove (action);
				ActionListAsset.SyncAssetObjects (_target);
			}
		}
		
		
		public static Action AddAction (string className, int i, ActionListAsset _target)
		{
			if (string.IsNullOrEmpty (className))
			{
				return null;
			}

			List<int> idArray = new List<int>();
			foreach (AC.Action _action in _target.actions)
			{
				if (_action == null) continue;
				idArray.Add (_action.id);
			}
			idArray.Sort ();
			
			Action newAction = Action.CreateNew (className);
			
			// Update id based on array
			foreach (int _id in idArray.ToArray())
			{
				if (newAction.id == _id)
					newAction.id ++;
			}
			
			return AddAction (newAction, i, _target);
		}


		public static Action AddAction (AC.Action newAction, int i, ActionListAsset _target)
		{
			if (i < 0)
			{
				_target.actions.Add (newAction);
			}
			else
			{
				_target.actions.Insert (i, newAction);
			}

			ActionListAsset.SyncAssetObjects (_target);

			return newAction;
		}
		
		
		private void ActionSideMenu (AC.Action action)
		{
			ActionListAsset _target = (ActionListAsset) target;
			
			int i = _target.actions.IndexOf (action);
			actionToAffect = action;
			GenericMenu menu = new GenericMenu ();
			
			if (action.isEnabled)
			{
				menu.AddItem (new GUIContent ("Disable"), false, Callback, "Disable");
			}
			else
			{
				menu.AddItem (new GUIContent ("Enable"), false, Callback, "Enable");
			}
			menu.AddSeparator ("");
			if (_target.actions.Count > 1)
			{
				menu.AddItem (new GUIContent ("Cut"), false, Callback, "Cut");
			}
			menu.AddItem (new GUIContent ("Copy"), false, Callback, "Copy");
			if (JsonAction.HasCopyBuffer ())
			{
				menu.AddItem (new GUIContent ("Paste after"), false, Callback, "Paste after");
			}
			menu.AddSeparator ("");
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			if (i > 0 || i < _target.actions.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (i < _target.actions.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}
			
			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			ActionListAsset t = (ActionListAsset) target;
			ModifyAction (t, actionToAffect, obj.ToString ());
			EditorUtility.SetDirty (t);
		}
		
		
		public static void ModifyAction (ActionListAsset _target, AC.Action _action, string callback)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			if (actionsManager == null)
			{
				return;
			}

			int i = -1;
			if (_action != null && _target.actions.IndexOf (_action) > -1)
			{
				i = _target.actions.IndexOf (_action);
			}

			bool doUndo = (callback != "Copy");

			if (doUndo)
			{
				Undo.SetCurrentGroupName (callback);
				Undo.RecordObjects (new Object [] { _target }, callback);
				#if !AC_ActionListPrefabs
				if (_target.actions != null) Undo.RecordObjects (_target.actions.ToArray (), callback);
				#endif
			}

			switch (callback)
			{
				case "Enable":
					_target.actions [i].isEnabled = true;
					break;
				
				case "Disable":
					_target.actions [i].isEnabled = false;
					break;
				
				case "Cut":
					List<Action> actionsToCut = new List<Action>();
					actionsToCut.Add (_action);
					JsonAction.ToCopyBuffer (actionsToCut, false);
					DeleteAction (_action, _target);
					break;
				
				case "Copy":
					List<Action> actionsToCopy = new List<Action> ();
					actionsToCopy.Add (_action);
					JsonAction.ToCopyBuffer (actionsToCopy);
					break;
				
				case "Paste after":
					int j = i + 1;
					List<Action> pasteList = JsonAction.CreatePasteBuffer (false);
					foreach (Action action in pasteList)
					{
						AddAction (action, j, _target);
						j++;
					}
					break;

				case "Insert after":
					Action newAction = AddAction (ActionsManager.GetDefaultAction (), i+1, _target);
					if (_action.endings.Count > 0)
					{
						newAction.endings.Add (new ActionEnd (_action.endings[0]));
					}
					break;
				
				case "Delete":
					DeleteAction (_action, _target);
					break;
				
				case "Move to top":
					_target.actions.Remove (_action);
					_target.actions.Insert (0, _action);
					break;
				
				case "Move up":
					_target.actions.Remove (_action);
					_target.actions.Insert (i-1, _action);
					break;
				
				case "Move to bottom":
					_target.actions.Remove (_action);
					_target.actions.Insert (_target.actions.Count, _action);
					break;
				
				case "Move down":
					_target.actions.Remove (_action);
					_target.actions.Insert (i+1, _action);
					break;

				default:
					break;
			}

			if (doUndo)
			{
				Undo.RecordObjects (new Object [] { _target }, callback);
#if !AC_ActionListPrefabs
				if (_target.actions != null) Undo.RecordObjects (_target.actions.ToArray (), callback);
#endif
				Undo.CollapseUndoOperations (Undo.GetCurrentGroup ());
				EditorUtility.SetDirty (_target);
			}
		}

			
		public static ActionListAsset ResizeList (ActionListAsset _target, int listSize)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			
			string defaultAction = string.Empty;
			
			if (actionsManager)
			{
				defaultAction = ActionsManager.GetDefaultAction ();
			}
			
			if (_target.actions.Count < listSize)
			{
				// Increase size of list
				while (_target.actions.Count < listSize)
				{
					List<int> idArray = new List<int>();
					
					foreach (AC.Action _action in _target.actions)
					{
						idArray.Add (_action.id);
					}
					
					idArray.Sort ();

					Action newAction = Action.CreateNew (defaultAction);
					AddAction (newAction, -1, _target);
					
					// Update id based on array
					foreach (int _id in idArray.ToArray())
					{
						if (_target.actions [_target.actions.Count -1].id == _id)
							_target.actions [_target.actions.Count -1].id ++;
					}
				}
			}
			else if (_target.actions.Count > listSize)
			{
				// Decrease size of list
				while (_target.actions.Count > listSize)
				{
					Action removeAction = _target.actions [_target.actions.Count - 1];
					DeleteAction (removeAction, _target);
				}
			}

			return (_target);
		}


		public static void ResetList (ActionListAsset _targetAsset)
		{
			if (_targetAsset.actions.Count == 0 || (_targetAsset.actions.Count == 1 && _targetAsset.actions[0] == null))
			{
				if (_targetAsset.actions.Count == 1)
				{
					DeleteAction (_targetAsset.actions[0], _targetAsset);
				}

				AddAction (ActionsManager.GetDefaultAction (), -1, _targetAsset);
			}
		}


		public static void ShowPropertiesGUI (ActionListAsset _target)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Asset properties", EditorStyles.boldLabel);
			_target.actionListType = (ActionListType) CustomGUILayout.EnumPopup ("When running:", _target.actionListType);
			if (_target.actionListType == ActionListType.PauseGameplay)
			{
				_target.isSkippable = CustomGUILayout.Toggle ("Is skippable?", _target.isSkippable);
				_target.unfreezePauseMenus = CustomGUILayout.Toggle ("Unfreeze 'pause' Menus?", _target.unfreezePauseMenus);
			}
			_target.canRunMultipleInstances = CustomGUILayout.Toggle ("Can run multiple instances?", _target.canRunMultipleInstances);
			if (!_target.IsSkippable ())
			{
				_target.canSurviveSceneChanges = CustomGUILayout.Toggle ("Can survive scene changes?", _target.canSurviveSceneChanges);
			}
			_target.useParameters = CustomGUILayout.Toggle ("Use parameters?", _target.useParameters);
			if (_target.useParameters)
			{
				_target.revertToDefaultParametersAfterRunning = CustomGUILayout.ToggleLeft ("Revert to default parameter values after running?", _target.revertToDefaultParametersAfterRunning);
			}

			CustomGUILayout.EndVertical ();
			
			if (_target.useParameters)
			{
				EditorGUILayout.Space ();
				CustomGUILayout.BeginVertical ();
				EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
				ActionListEditor.ShowParametersGUI (null, _target, _target.GetParameters ());
				CustomGUILayout.EndVertical ();
			}

			_target.tagID = ActionListEditor.ShowTagUI (_target.actions.ToArray (), _target.tagID);
		}


		[OnOpenAssetAttribute(10)]
		public static bool OnOpenAsset (int instanceID, int line)
		{
			if (Selection.activeObject is ActionListAsset && instanceID == Selection.activeInstanceID)
			{
				ActionListAsset actionListAsset = (ActionListAsset) Selection.activeObject as ActionListAsset;
				ActionListEditorWindow.Init (actionListAsset);
				return true;
			}
			return false;
		}

	}

}

#endif