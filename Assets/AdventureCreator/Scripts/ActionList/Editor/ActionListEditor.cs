#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor (typeof (ActionList))]
	[System.Serializable]
	public class ActionListEditor : Editor
	{

		private AC.Action actionToAffect = null;
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
			ActionList _target = (ActionList) target;

			ShowPropertiesGUI (_target);
			DrawSharedElements (_target);

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void ShowPropertiesGUI (ActionList _target)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("ActionList properties", EditorStyles.boldLabel);
			_target.source = (ActionListSource) CustomGUILayout.EnumPopup ("Actions source:", _target.source, "", "Where the Actions are stored");
			if (_target.source == ActionListSource.AssetFile)
			{
				_target.assetFile = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("ActionList asset:", _target.assetFile, false, "", "The ActionList asset that stores the Actions");
				if (_target.assetFile && _target.assetFile.NumParameters > 0)
				{
					_target.syncParamValues = CustomGUILayout.Toggle ("Sync parameter values?", _target.syncParamValues, "", "If True, the ActionList asset's parameter values will be shared amongst all linked ActionLists");
				}
			}
			_target.actionListType = (ActionListType) CustomGUILayout.EnumPopup ("When running:", _target.actionListType, "", "The effect that running the Actions has on the rest of the game");
			if (_target.actionListType == ActionListType.PauseGameplay)
			{
				_target.isSkippable = CustomGUILayout.Toggle ("Is skippable?", _target.isSkippable, "", "If True, the Actions will be skipped when the user presses the 'EndCutscene' Input button");
			}
			_target.tagID = ShowTagUI (_target.actions.ToArray (), _target.tagID);
			if (_target.source == ActionListSource.InScene)
			{
				_target.useParameters = CustomGUILayout.Toggle ("Use parameters?", _target.useParameters, "", "If True, ActionParameters can be used to override values within the Action objects");
			}
			else if (_target.source == ActionListSource.AssetFile && _target.assetFile && !_target.syncParamValues && _target.assetFile.useParameters && !Application.isPlaying)
			{
				_target.useParameters = CustomGUILayout.Toggle ("Set local parameter values?", _target.useParameters, "", "If True, parameter values set here will be assigned locally, and not on the ActionList asset");
			}
			CustomGUILayout.EndVertical ();

			if (_target.source == ActionListSource.InScene)
			{
				EditorGUILayout.Space ();
				CustomGUILayout.BeginVertical ();

				EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
				ShowParametersGUI (_target, null, _target.parameters);

				CustomGUILayout.EndVertical ();
			}
			else if (_target.source == ActionListSource.AssetFile && _target.assetFile && _target.assetFile.useParameters)
			{
				if (_target.syncParamValues)
				{
					EditorGUILayout.Space ();
					CustomGUILayout.BeginVertical ();
					EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
					ShowParametersGUI (null, _target.assetFile, _target.assetFile.GetParameters (), !Application.isPlaying);
					CustomGUILayout.EndVertical ();
				}
				else
				{
					if (_target.useParameters)
					{
						bool isAsset = UnityVersionHandler.IsPrefabFile (_target.gameObject);

						EditorGUILayout.Space ();
						CustomGUILayout.BeginVertical ();

						EditorGUILayout.LabelField ("Local parameters", EditorStyles.boldLabel);
						ShowLocalParametersGUI (_target.parameters, _target.assetFile.GetParameters (), isAsset);

						CustomGUILayout.EndVertical ();
					}
					else
					{
						// Use default from asset initially

						EditorGUILayout.Space ();
						CustomGUILayout.BeginVertical ();
						EditorGUILayout.LabelField ("Parameters", EditorStyles.boldLabel);
						if (Application.isPlaying)
						{
							ShowParametersGUI (_target, null, _target.parameters);
						}
						else
						{
							ShowParametersGUI (null, _target.assetFile, _target.assetFile.DefaultParameters, true);
						}
						CustomGUILayout.EndVertical ();
					}
				}
			}
	    }


		public static bool IsActionListPrefab (ActionList _target)
		{
			if (UnityVersionHandler.IsPrefabFile (_target.gameObject) && _target.source == ActionListSource.InScene)
			{
				return true;
			}
			return false;
		}
		
		
		protected void DrawSharedElements (ActionList _target)
		{
#if !AC_ActionListPrefabs
			if (IsActionListPrefab (_target))
			{
				EditorGUILayout.HelpBox ("Scene-based Actions can not live in prefabs - use ActionList assets instead.", MessageType.Info);
				return;
			}
#endif

			int numActions = 0;
			if (_target.source != ActionListSource.AssetFile)
			{
				numActions = _target.actions.Count;
				if (numActions < 1)
				{
					numActions = 1;
					AddAction (ActionsManager.GetDefaultAction (), -1, _target);
				}
			}

			EditorGUILayout.Space ();

			if (_target.source == ActionListSource.InScene)
			{
				ResetList (_target);
			}

			actionsManager = AdvGame.GetReferences ().actionsManager;
			if (actionsManager == null)
			{
				EditorGUILayout.HelpBox ("An Actions Manager asset file must be assigned in the Game Editor Window", MessageType.Warning);
				OnEnable ();
				return;
			}

			if (!actionsManager.displayActionsInInspector || _target.source == ActionListSource.AssetFile)
			{
				if (Application.isPlaying)
				{
					EditorGUILayout.BeginHorizontal ();
					if (GUILayout.Button ("Edit Actions", GUILayout.Height (40f)))
					{
						ActionListEditorWindow.OpenForActionList (_target);
					}

					bool isRunning = false;
					if (Application.isPlaying)
					{
						if (KickStarter.actionListManager != null)
						{
							isRunning = KickStarter.actionListManager.IsListRunning(_target);
						}
					}

					if (isRunning)
					{
						if (GUILayout.Button ("Stop", GUILayout.Height (40f)))
						{
							_target.Kill ();
						}
					}
					else
					{
						if (GUILayout.Button ("Run now", GUILayout.Height (40f)))
						{
							_target.Interact ();
						}
					}
					EditorGUILayout.EndHorizontal ();
				}
				else
				{
					if (GUILayout.Button ("Edit Actions", GUILayout.Height (40f)))
					{
						ActionListEditorWindow.OpenForActionList (_target);
					}
				}
				return;
			}
			else
			{
				EditorGUILayout.BeginHorizontal ();

				GUI.enabled = (_target.source == ActionListSource.InScene);

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

				GUI.enabled = true;

				if (GUILayout.Button ("Action List Editor", EditorStyles.miniButtonMid))
				{
					ActionListEditorWindow.OpenForActionList (_target);
				}
				GUI.enabled = Application.isPlaying;
				if (GUILayout.Button ("Run now", EditorStyles.miniButtonRight))
				{
					_target.Interact ();
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Space ();
			}

			for (int i=0; i<_target.actions.Count; i++)
			{
				if (_target.actions[i] == null)
				{
					ACDebug.LogWarning ("An empty Action was found, and was deleted", _target);
					_target.actions.RemoveAt (i);
					numActions --;
					continue;
				}

				_target.actions[i].Upgrade ();
				_target.actions[i].AssignParentList (_target);

				CustomGUILayout.BeginVertical ();
				EditorGUILayout.BeginHorizontal ();
				int typeIndex = actionsManager.GetActionTypeIndex (_target.actions[i]);

				string actionLabel = " (" + i.ToString () + ") " + actionsManager.GetActionTypeLabel (_target.actions[i], true);
				actionLabel = actionLabel.Replace("\r\n", "");
				actionLabel = actionLabel.Replace("\n", "");
				actionLabel = actionLabel.Replace("\r", "");
				if (actionLabel.Length > 40)
				{
					actionLabel = actionLabel.Substring (0, 40) + "..)";
				}

				GUILayout.Label (" ", GUILayout.MaxWidth (10f));
				
				_target.actions[i].isDisplayed = EditorGUILayout.Foldout (_target.actions[i].isDisplayed, actionLabel);
				if (!_target.actions[i].isEnabled)
				{
					EditorGUILayout.LabelField ("DISABLED", EditorStyles.boldLabel, GUILayout.MaxWidth (100f));
				}

				if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
				{
					ActionSideMenu (i);
				}

				_target.actions[i].isAssetFile = false;
				
				EditorGUILayout.EndHorizontal ();

				if (_target.actions[i].isBreakPoint)
				{
					EditorGUILayout.HelpBox ("Break point", MessageType.None);
				}

				if (_target.actions[i].isDisplayed)
				{
					GUI.enabled = _target.actions[i].isEnabled;

					if (!actionsManager.DoesActionExist (_target.actions[i].GetType ().ToString ()))
					{
						EditorGUILayout.HelpBox ("This Action type is not listed in the Actions Manager", MessageType.Warning);
					}
					else
					{
						int newTypeIndex = ShowTypePopup (_target.actions[i], typeIndex);
						if (newTypeIndex >= 0)
						{
							// Rebuild constructor if Subclass and type string do not match
							ActionEnd _end = (_target.actions[i].endings.Count > 0) ? new ActionEnd (_target.actions[i].endings[0]) : new ActionEnd ();

							Undo.RecordObject (_target, "Change Action type");
							_target.actions[i] = RebuildAction (_target.actions[i], newTypeIndex, _target, -1, _end);
						}

						if (_target.NumParameters > 0)
						{
							_target.actions[i].ShowGUI (_target.parameters);
						}
						else
						{
							_target.actions[i].ShowGUI (null);
						}
					}
				}

				_target.actions[i].SkipActionGUI (_target.actions, _target.actions[i].isDisplayed);
				
				GUI.enabled = true;
				
				CustomGUILayout.EndVertical ();
				EditorGUILayout.Space ();
			}

			if (GUILayout.Button("Add new action"))
			{
				Undo.RecordObject (_target, "Create action");
				numActions += 1;
			}
			
			_target = ResizeList (_target, numActions);

		}


		public static int ShowTagUI (Action[] actions, int tagID)
		{
			bool hasSpeechAction = false;
			foreach (Action action in actions)
			{
				if (action != null && action is ActionSpeech)
				{
					hasSpeechAction = true;
				}
			}

			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			if (speechManager == null || !speechManager.useSpeechTags || !hasSpeechAction) return tagID;

			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			int i = 0;
			int tagNumber = -1;
			
			if (speechManager.speechTags.Count > 0)
			{
				foreach (SpeechTag speechTag in speechManager.speechTags)
				{
					labelList.Add (speechTag.label);
					if (speechTag.ID == tagID)
					{
						tagNumber = i;
					}
					i++;
				}
				
				if (tagNumber == -1)
				{
					if (tagID > 0) ACDebug.LogWarning ("Previously chosen speech tag no longer exists!");
					tagNumber = 0;
				}
				
				tagNumber = EditorGUILayout.Popup ("Speech tag:", tagNumber, labelList.ToArray());
				tagID = speechManager.speechTags [tagNumber].ID;
			}
			else
			{
				EditorGUILayout.HelpBox ("No speech tags!", MessageType.Info);
			}

			return tagID;
		}


		public static int ShowTypePopup (AC.Action action, int typeIndex)
		{
			if (!KickStarter.actionsManager.IsActionTypeEnabled (typeIndex))
			{
				EditorGUILayout.LabelField ("<b>This Action type has been disabled.</b>", CustomStyles.disabledActionType);
				//return typeIndex;
				return -1;
			}

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Action type:", GUILayout.Width (80));
			
			ActionCategory oldCategory = KickStarter.actionsManager.GetActionCategory (typeIndex);
			ActionCategory category = oldCategory;
			category = (ActionCategory) EditorGUILayout.EnumPopup (category);
			
			int subcategory = KickStarter.actionsManager.GetActionSubCategory (action);
			// This is for all, needs to be converted to enabled for that category

			int enabledSubcategory = -1;
			ActionType[] categoryTypes = KickStarter.actionsManager.GetActionTypesInCategory (category);
			for (int i=0; i<=subcategory; i++)
			{
				if (i < categoryTypes.Length && categoryTypes[i].isEnabled)
				{
					enabledSubcategory++;
				}
			}

			string[] subCategories = KickStarter.actionsManager.GetActionSubCategories (category);

			if (category != oldCategory)
			{
				enabledSubcategory = KickStarter.actionsManager.GetDefaultActionInCategory (category);
				if (enabledSubcategory >= subCategories.Length) enabledSubcategory = 0;
			}

			enabledSubcategory = EditorGUILayout.Popup (enabledSubcategory, subCategories);
			int newTypeIndex = KickStarter.actionsManager.GetEnabledActionTypeIndex (category, enabledSubcategory);

			EditorGUILayout.EndHorizontal ();
			GUILayout.Space (4f);

			if (newTypeIndex != typeIndex)
			{
				return newTypeIndex;
			}
			return -1;
		}


		public static AC.Action RebuildAction (AC.Action existingAction, int typeIndex, ActionList _target, int insertIndex = -1, ActionEnd _end = null)
		{
			ActionsManager actionsManager = AdvGame.GetReferences ().actionsManager;
			int existingIndex = _target.actions.IndexOf (existingAction);
			if (actionsManager)
			{
				string className = actionsManager.AllActions [typeIndex].fileName;
				
				if (existingAction.GetType ().ToString () != className && existingAction.GetType ().ToString () != ("AC." + className))
				{
					bool _showComment = existingAction.showComment;
					bool _showOutputSockets = existingAction.showOutputSockets;
					string _comment = existingAction.comment;
					ActionList _parentActionListInEditor = existingAction.parentActionListInEditor;

					Action newAction = Action.CreateNew (className);;

					if (newAction == null && !className.StartsWith ("AC."))
					{
						newAction = Action.CreateNew ("AC." + className);
					}
					if (newAction == null)
					{
						newAction = Action.CreateNew (ActionsManager.GetDefaultAction ());
					}
					
					if (_end != null)
					{
						newAction.endings.Add (new ActionEnd (_end));
					}

					newAction.showComment = _showComment;
					newAction.showOutputSockets = _showOutputSockets;
					newAction.comment = _comment;
					newAction.parentActionListInEditor = _parentActionListInEditor;

					if (insertIndex >= 0)
					{
						_target.actions.Insert (insertIndex, newAction);
					}
					else if (existingIndex >= 0)
					{
						_target.actions[existingIndex] = newAction;
					}

					//SyncAssetObjects (_target);

					return newAction;
				}
			}

			return existingAction;
		}


		/*public static void SyncAssetObjects (ActionList actionList)
		{
			if (!UnityVersionHandler.IsPrefabFile (actionList.gameObject))
			{
				return;
			}

			Object prefabObject = PrefabUtility.GetPrefabObject (actionList);
			if (prefabObject == null)
			{
				return;
			}

			ActionList[] allActionLists = GetAllActionListsInPrefab (actionList);
			bool modified = false;

			// Search for assets to delete
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath (AssetDatabase.GetAssetPath (prefabObject));
			foreach (Object asset in assets)
			{
				Action actionAsset = asset as Action;
				if (actionAsset != null)
				{
					Debug.Log ("TEST DELETION: Asset: " + actionAsset);
					bool foundMatch = false;

					foreach (ActionList searchList in allActionLists)
					{
						Debug.Log ("Search ActionList: " + searchList);
						foreach (Action action in searchList.actions)
						{
							if (actionAsset == action)
							{
								Debug.Log ("Found Action: " + action);
								foundMatch = true;
								break;
							}
						}
					}

					if (!foundMatch)
					{
						Debug.LogWarning ("Found no match of asset " + actionAsset + " - deleting now");
						Undo.DestroyObjectImmediate (actionAsset);
						modified = true;
					}
				}
			}

			// Search for assets to add
			foreach (ActionList searchList in allActionLists)
			{
				foreach (Action action in searchList.actions)
				{
					if (action != null)
					{
						Debug.Log ("TEST ADDITION: " + action);
						bool foundMatch = false;

						foreach (Object asset in assets)
						{
							Action actionAsset = asset as Action;
							if (actionAsset == action)
							{
								Debug.Log ("Found asset: " + actionAsset);
								foundMatch = true;
								break;
							}
						}

						if (!foundMatch)
						{
							//action.hideFlags = HideFlags.HideInHierarchy;
							AssetDatabase.AddObjectToAsset (action, prefabObject);
							AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (action));
							Debug.LogWarning ("Found no match of " + action + " '" + action.name + "' in database - adding now to " + AssetDatabase.GetAssetPath (action));
							modified = true;
						}
					}
				}
			}

			if (modified)
			{
				AssetDatabase.SaveAssets ();
				AssetDatabase.Refresh ();
			}
		}


		private static ActionList[] GetAllActionListsInPrefab (ActionList actionList)
		{
			return actionList.transform.root.GetComponentsInChildren <ActionList>();
		}*/

		
		private void ActionSideMenu (int i)
		{
			ActionList _target = (ActionList) target;
			actionToAffect = _target.actions[i];
			GenericMenu menu = new GenericMenu ();
			
			if (_target.actions[i].isEnabled)
			{
				menu.AddItem (new GUIContent ("Disable"), false, Callback, "Disable");
			}
			else
			{
				menu.AddItem (new GUIContent ("Enable"), false, Callback, "Enable");
			}
			menu.AddSeparator (string.Empty);
			if (!Application.isPlaying)
			{
				if (_target.actions.Count > 1)
				{
					menu.AddItem (new GUIContent ("Cut"), false, Callback, "Cut");
				}
				menu.AddItem (new GUIContent ("Copy"), false, Callback, "Copy");
			}
			if (JsonAction.HasCopyBuffer ())
			{
				menu.AddItem (new GUIContent ("Paste after"), false, Callback, "Paste after");
			}
			menu.AddSeparator (string.Empty);
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (_target.actions.Count > 1)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (i > 0 || i < _target.actions.Count-1)
			{
				menu.AddSeparator (string.Empty);
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

			menu.AddSeparator (string.Empty);
			menu.AddItem (new GUIContent ("Toggle breakpoint"), false, Callback, "Toggle breakpoint");

			menu.AddSeparator (string.Empty);
			menu.AddItem (new GUIContent ("Edit Script"), false, Callback, "EditSource");
			
			menu.ShowAsContext ();
		}
		
		
		private void Callback (object obj)
		{
			ActionList t = (ActionList) target;
			ModifyAction (t, actionToAffect, obj.ToString ());
			EditorUtility.SetDirty (t);
		}
		
		
		public static void ModifyAction (ActionList _target, AC.Action _action, string callback)
		{
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
					_action.isEnabled = true;
					break;
				
				case "Disable":
					_action.isEnabled = false;
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
					List<Action> pasteList = JsonAction.CreatePasteBuffer (false);
					_target.actions.InsertRange (i + 1, pasteList);
					break;

				case "Insert end":
					Action insertEndAction = AddAction (ActionsManager.GetDefaultAction (), -1, _target);
					if (insertEndAction.NumSockets == 1)
					{
						if (insertEndAction.endings.Count == 0)
						{
							insertEndAction.endings.Add (Action.GenerateStopActionEnd ());
						}
						else
						{
							insertEndAction.endings[0] = Action.GenerateStopActionEnd ();
						}
					}
					break;
				
				case "Insert after":
					Action insertAfterAction = AddAction (ActionsManager.GetDefaultAction (), i+1, _target);
					if (_action.endings.Count > 0)
					{
						insertAfterAction.endings.Add (new ActionEnd (_action.endings[0]));
					}
					break;
				
				case "Delete":
					Undo.RecordObject (_target, "Delete action");
					DeleteAction (_action, _target);
					break;
				
				case "Move to top":
					Vector2 newPosition = _target.actions[0].NodeRect.position + new Vector2 (30, 30);
					_target.actions[0].NodeRect = new Rect (newPosition, _target.actions[0].NodeRect.size);
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

				case "Toggle breakpoint":
					_action.isBreakPoint = !_action.isBreakPoint;
					break;

				case "EditSource":
					Action.EditSource (_action);
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


		public static void DeleteAction (AC.Action action, ActionList _target)
		{
			if (action != null) 
			{
				_target.actions.Remove (action);

#if !AC_ActionListPrefabs
				Undo.DestroyObjectImmediate (action);
#endif
				//SyncAssetObjects (_target);
			}
		}


		public static Action AddAction (string className, int i, ActionList _target)
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


		public static Action AddAction (AC.Action newAction, int i, ActionList _target)
		{
			if (i < 0)
			{
				_target.actions.Add (newAction);
			}
			else
			{
				_target.actions.Insert (i, newAction);
			}

			//SyncAssetObjects (_target);

			return newAction;
		}


		public static void PushNodes (List<AC.Action> list, float xPoint, int count)
		{
			foreach (AC.Action action in list)
			{
				if (action.NodeRect.x > xPoint)
				{
					Vector2 newPosition = new Vector2 (action.NodeRect.x + (350 * count), action.NodeRect.y);
					action.NodeRect = new Rect (newPosition, action.NodeRect.size);
				}
			}
		}
		
		
		public static ActionList ResizeList (ActionList _target, int listSize)
		{
			string defaultAction = ActionsManager.GetDefaultAction ();
			if (string.IsNullOrEmpty (defaultAction))
			{
				return _target;
			}

			if (_target.actions.Count < listSize)
			{
				// Increase size of list
				while (_target.actions.Count < listSize)
				{
					List<int> idArray = new List<int>();
					
					foreach (AC.Action _action in _target.actions)
					{
						if (_action == null) continue;
						idArray.Add (_action.id);
					}
					
					idArray.Sort ();

					AddAction (defaultAction, -1, _target);
					
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
					DeleteAction (_target.actions[_target.actions.Count-1], _target);
				}
			}
			
			return _target;
		}


		public static int[] GetParameterIDArray (List<ActionParameter> parameters)
		{
			List<int> idArray = new List<int>();
			foreach (ActionParameter _parameter in parameters)
			{
				idArray.Add (_parameter.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}


		public static void ShowParametersGUI (ActionList actionList, ActionListAsset actionListAsset, List<ActionParameter> parameters, bool readOnly = false)
		{
			if (parameters.Count > 0)
			{
				for (int i=0; i<parameters.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();

					string buttonLabel = parameters[i].label;
					if (string.IsNullOrEmpty (buttonLabel))
					{
						buttonLabel = "(Untitled)";
					}

					bool isDisplayed = parameters[i].isDisplayed;
					isDisplayed = GUILayout.Toggle (isDisplayed, parameters[i].ID.ToString () + ": " + buttonLabel, "Button");
					if (parameters[i].isDisplayed != isDisplayed)
					{
						parameters[i].isDisplayed = isDisplayed;
						EditorGUIUtility.editingTextField = false;
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						ParameterSideMenu (actionList, actionListAsset, parameters.Count, i);
					}

					EditorGUILayout.EndHorizontal ();

					if (parameters[i].isDisplayed)
					{
						if (Application.isPlaying)
						{
							parameters[i].ShowGUI (actionListAsset != null);
						}
						else
						{
							parameters[i].ShowGUI (actionListAsset != null, false, readOnly);
						}
						EditorGUILayout.Space ();
					}
				}
			}

			if (!Application.isPlaying && !readOnly)
			{
				EditorGUILayout.Space ();

				if (GUILayout.Button ("Create new parameter", EditorStyles.miniButton))
				{
					ActionParameter newParameter = new ActionParameter (GetParameterIDArray (parameters));
					newParameter.parameterType = ParameterType.Integer;
					parameters.Add (newParameter);
				}
			}

		}


		private static int parameterToAffect;
		private static ActionList parameterSideActionList;
		private static ActionListAsset parameterSideActionListAsset;
		private static void ParameterSideMenu (ActionList actionList, ActionListAsset actionListAsset, int numParameters, int i)
		{
			parameterToAffect = i;
			parameterSideActionList = actionList;
			if (actionList == null)
			{
				parameterSideActionListAsset = actionListAsset;
			}
			else
			{
				parameterSideActionListAsset = null;
			}

			GenericMenu menu = new GenericMenu ();

			menu.AddItem (new GUIContent ("Insert"), false, ParameterCallback, "Insert");
			menu.AddItem (new GUIContent ("Delete"), false, ParameterCallback, "Delete");

			if (i > 0 || i < numParameters-1)
			{
				menu.AddSeparator (string.Empty);

				if (i > 0)
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, ParameterCallback, "Move to top");
					menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, ParameterCallback, "Move up");
				}
				if (i < numParameters-1)
				{
					menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, ParameterCallback, "Move down");
					menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, ParameterCallback, "Move to bottom");
				}

				menu.AddSeparator (string.Empty);
				menu.AddItem (new GUIContent ("All/Collapse"), false, ParameterCallback, "Collapse all");
				menu.AddItem (new GUIContent ("All/Expand"), false, ParameterCallback, "Expand all");
			}

			menu.ShowAsContext ();
		}
		
		
		private static void ParameterCallback (object obj)
		{
			if (parameterSideActionList != null)
			{
				ModifyParameter (parameterSideActionList, parameterToAffect, obj.ToString ());
				EditorUtility.SetDirty (parameterSideActionList);
			}
			else if (parameterSideActionListAsset != null)
			{
				ModifyParameter (parameterSideActionListAsset, parameterToAffect, obj.ToString ());
				EditorUtility.SetDirty (parameterSideActionListAsset);
			}

		}
		
		
		private static void ModifyParameter (ActionList _target, int i, string callback)
		{
			if (_target == null || _target.parameters == null) return;

			ActionParameter moveParameter = _target.parameters[i];
				
			switch (callback)
			{
				case "Insert":
					Undo.RecordObject (_target, "Create parameter");
					ActionParameter newParameter = new ActionParameter (ActionListEditor.GetParameterIDArray (_target.parameters));
					_target.parameters.Insert (i+1, newParameter);
					break;
				
				case "Delete":
					Undo.RecordObject (_target, "Delete parameter");
					_target.parameters.RemoveAt (i);
					break;

				case "Move to top":
					Undo.RecordObject (_target, "Move parameter to top");
					_target.parameters.Remove (moveParameter);
					_target.parameters.Insert (0, moveParameter);
					break;
				
				case "Move up":
					Undo.RecordObject (_target, "Move parameter up");
					_target.parameters.Remove (moveParameter);
					_target.parameters.Insert (i-1, moveParameter);
					break;
				
				case "Move to bottom":
					Undo.RecordObject (_target, "Move parameter to bottom");
					_target.parameters.Remove (moveParameter);
					_target.parameters.Insert (_target.NumParameters, moveParameter);
					break;
				
				case "Move down":
					Undo.RecordObject (_target, "Move parameter down");
					_target.parameters.Remove (moveParameter);
					_target.parameters.Insert (i+1, moveParameter);
					break;

				case "Collapse all":
					foreach (ActionParameter actionParameter in _target.parameters)
					{
						actionParameter.isDisplayed = false;
					}
					break;

				case "Expand all":
					foreach (ActionParameter actionParameter in _target.parameters)
					{
						actionParameter.isDisplayed = true;
					}
					break;

				default:
					break;
			}
		}


		private static void ModifyParameter (ActionListAsset _target, int i, string callback)
		{
			if (_target == null || _target.NumParameters == 0) return;

			ActionParameter moveParameter = _target.DefaultParameters[i];
				
			switch (callback)
			{
			case "Insert":
				Undo.RecordObject (_target, "Create parameter");
				ActionParameter newParameter = new ActionParameter (ActionListEditor.GetParameterIDArray (_target.DefaultParameters));
				_target.DefaultParameters.Insert (i+1, newParameter);
				break;
				
			case "Delete":
				Undo.RecordObject (_target, "Delete parameter");
				_target.DefaultParameters.RemoveAt (i);
				break;

			case "Move to top":
				Undo.RecordObject (_target, "Move parameter to top");
				_target.DefaultParameters.Remove (moveParameter);
				_target.DefaultParameters.Insert (0, moveParameter);
				break;
				
			case "Move up":
				Undo.RecordObject (_target, "Move parameter up");
				_target.DefaultParameters.Remove (moveParameter);
				_target.DefaultParameters.Insert (i-1, moveParameter);
				break;
				
			case "Move to bottom":
				Undo.RecordObject (_target, "Move parameter to bottom");
				_target.DefaultParameters.Remove (moveParameter);
				_target.DefaultParameters.Insert (_target.DefaultParameters.Count, moveParameter);
				break;
				
			case "Move down":
				Undo.RecordObject (_target, "Move parameter down");
				_target.DefaultParameters.Remove (moveParameter);
				_target.DefaultParameters.Insert (i+1, moveParameter);
				break;
			}
		}


		public static void ShowLocalParametersGUI (List<ActionParameter> localParameters, List<ActionParameter> assetParameters, bool isAssetFile)
		{
			int numParameters = assetParameters.Count;

			if (numParameters < localParameters.Count)
			{
				localParameters.RemoveRange (numParameters, localParameters.Count - numParameters);
			}
			else if (numParameters > localParameters.Count)
			{
				if (numParameters > localParameters.Capacity)
				{
					localParameters.Capacity = numParameters;
				}
				for (int i=localParameters.Count; i<numParameters; i++)
				{
					ActionParameter newParameter = new ActionParameter (ActionListEditor.GetParameterIDArray (localParameters));
					localParameters.Add (newParameter);
				}
			}

			for (int i=0; i<numParameters; i++)
			{
				localParameters[i].label = assetParameters[i].label;
				localParameters[i].parameterType = assetParameters[i].parameterType;

				EditorGUILayout.LabelField ("Label " + assetParameters[i].ID + ":", assetParameters[i].label);
				localParameters[i].ShowGUI (isAssetFile, true);

				if (i < (numParameters-1))
				{
					EditorGUILayout.Space ();
				}
			}
		}


		public static void ResetList (ActionList _target)
		{
			if (_target.actions.Count == 0 || (_target.actions.Count == 1 && _target.actions[0] == null))
			{
				_target.actions.Clear ();
				AddAction (ActionsManager.GetDefaultAction (), -1, _target);
			}
		}

	}

}

#endif