/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionsManager.cs"
 * 
 *	This script handles the "Actions" tab of the Game Editor window.
 *	Custom actions can be added and removed by selecting them with this.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * Handles the "Actions" tab of the Game Editor window.
	 * All available Actions are listed here, and custom Actions can be added.
	 */
	[System.Serializable]
	public class ActionsManager : ScriptableObject
	{
		
		#if UNITY_EDITOR

		public List<string> customFolderPaths = new List<string>();

		public List<FavouriteActionData> allFavouriteActionData = new List<FavouriteActionData>();

		#endif

		/** If True, then Actions can be displayed in an ActionList's Inspector window */
		public bool displayActionsInInspector = true;
		/** How Actions are arranged in the ActionList Editor window (ArrangedVertically, ArrangedHorizontally) */
		public DisplayActionsInEditor displayActionsInEditor = DisplayActionsInEditor.ArrangedVertically;
		/** If True, then multiple ActionList Editor windows can be opened at once */
		public bool allowMultipleActionListWindows = false;
		/** The effect the mouse scrollwheel has inside the ActionList Editor window (PansWindow, ZoomsWindow) */
		public ActionListEditorScrollWheel actionListEditorScrollWheel = ActionListEditorScrollWheel.PansWindow;
		/** If True, then panning is inverted in the ActionList Editor window (useful for Macbooks) */
		public bool invertPanning = false;
		/** If True, then the ActionList Editor window will focus on newly-pasted Actions */
		public bool focusOnPastedActions = true;
		/** The speed factor for panning/zooming */
		public float panSpeed = 1f;
		/** If True, the ActionList Editor will pan automatically when dragging the cursor near the window's edge */
		public bool autoPanNearWindowEdge = true;
		/** The index number of the default Action (deprecated) */
		public int defaultClass;
		/** The class name of the default Action */
		public string defaultClassName;
		/** A List of all Action classes found */
		public List<ActionType> AllActions = new List<ActionType>();

		#if UNITY_EDITOR

		private ActionType selectedClass = null;

		[SerializeField] List<DefaultActionCategoryData> defaultActionCategoryDatas = new List<DefaultActionCategoryData>();

		private bool showEditing = true;
		private bool showCustom = true;
		private bool showCategories = true;
		private bool showActionTypes = true;
		private bool showActionType = true;
		
		private int selectedCategoryInt = -1;
		private ActionCategory selectedCategory;

		#endif


		/**
		 * <summary>Gets the filename of the default Action.</summary>
		 * <returns>The filename of the default Action.</returns>
		 */
		public static string GetDefaultAction ()
		{
			if (AdvGame.GetReferences () != null && AdvGame.GetReferences ().actionsManager != null)
			{
				return AdvGame.GetReferences ().actionsManager._GetDefaultAction ();
			}
			ACDebug.LogError ("Cannot create Action - no Actions Manager found.");
			return string.Empty;
		}


		private string _GetDefaultAction ()
		{
			Upgrade ();

			if (!string.IsNullOrEmpty (defaultClassName))
			{
				return defaultClassName;
			}
			ACDebug.LogError ("Cannot create default Action - no default set.");
			return string.Empty;
		}


		private void Upgrade ()
		{
			if (defaultClass >= 0 && AllActions.Count > 0 && AllActions.Count > defaultClass)
			{
				defaultClassName = AllActions[defaultClass].fileName;
				defaultClass = -1;
			}

			if (string.IsNullOrEmpty (defaultClassName) && AllActions.Count > 0)
			{
				defaultClassName = AllActions[0].fileName;
				defaultClass = -1;
			}
		}
		
		
		#if UNITY_EDITOR

		public void ShowGUI (Rect position)
		{
			ShowEditingGUI ();

			EditorGUILayout.Space ();

			ShowCustomGUI ();

			EditorGUILayout.Space ();

			if (AllActions.Count > 0)
			{
				Upgrade ();
				ShowCategoriesGUI ();

				if (selectedCategoryInt >= 0)
				{
					EditorGUILayout.Space ();
					ShowActionTypesGUI ();
					if (selectedClass != null)
					{
						EditorGUILayout.Space ();
						ShowActionTypeGUI (position.width);
					}
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("No Action subclass files found.", MessageType.Warning);
			}

			if (GUI.changed)
			{
				Upgrade ();
				EditorUtility.SetDirty (this);
			}
		}


		private int sideAction;
		private void SideMenu (int index)
		{
			sideAction = index;
			GenericMenu menu = new GenericMenu ();

			ActionType subclass = AllActions[index];
			if (!string.IsNullOrEmpty (defaultClassName) && subclass.fileName != defaultClassName)
											{
				menu.AddItem (new GUIContent ("Make default"), false, Callback, "Make default");
			}

			menu.AddItem (new GUIContent ("Make default in category"), false, Callback, "Make default in category");
			menu.AddItem (new GUIContent ("Edit script"), false, Callback, "EditSource");
			menu.AddSeparator (string.Empty);

			menu.AddItem (new GUIContent ("Search local instances"), false, Callback, "Search local instances");
			menu.AddItem (new GUIContent ("Search all instances"), false, Callback, "Search all instances");

			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			if (sideAction >= 0)
			{
				ActionType subclass = AllActions[sideAction];

				switch (obj.ToString ())
				{
					case "Make default":
						if (AllActions.Contains (subclass))
						{
							defaultClassName = subclass.fileName;
							subclass.isEnabled = true;
						}
						break;

					case "Make default in category":
						bool updatedExisting = false;
						foreach (DefaultActionCategoryData defaultActionCategoryData in defaultActionCategoryDatas)
						{
							if (defaultActionCategoryData.Category == subclass.category)
							{
								defaultActionCategoryData.DefaultClassName = subclass.fileName;
								updatedExisting = true;
								break;
							}
						}
						if (!updatedExisting)
						{
							defaultActionCategoryDatas.Add (new DefaultActionCategoryData (subclass.category, subclass.fileName));
						}
						break;

					case "EditSource":
						Action tempAction = Action.CreateNew (subclass.fileName);
						if (tempAction != null && tempAction is Action)
						{
							Action.EditSource (tempAction);
						}
						break;

					case "Search local instances":
						SearchForInstances (true, subclass);
						break;

					case "Search all instances":
						if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
						{
							SearchForInstances (false, subclass);
						}
						break;
				}
			}
		}


		private void ShowEditingGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showEditing = CustomGUILayout.ToggleHeader (showEditing, "ActionList editing settings");
			if (showEditing)
			{
				displayActionsInInspector = CustomGUILayout.Toggle ("List Actions in Inspector?", displayActionsInInspector, "AC.KickStarter.actionsManager.displayActionsInInspector", "If True, then Actions can be displayed in an ActionList's Inspector window");
				displayActionsInEditor = (DisplayActionsInEditor) CustomGUILayout.EnumPopup ("Actions in Editor are:", displayActionsInEditor, "AC.KickStarter.actionsManager.displayActionsInEditor", "How Actions are arranged in the ActionList Editor window");
				actionListEditorScrollWheel = (ActionListEditorScrollWheel) CustomGUILayout.EnumPopup ("Using scroll-wheel:", actionListEditorScrollWheel, "AC.KickStarter.actionsManager.actionListEditorScrollWheel", "The effect the mouse scrollwheel has inside the ActionList Editor window");

				#if UNITY_EDITOR_OSX
				string altKey = "Option";
				#else
				string altKey = "Alt";
				#endif
				if (actionListEditorScrollWheel == ActionListEditorScrollWheel.ZoomsWindow)
				{
					EditorGUILayout.HelpBox ("Panning is possible by holding down the middle-mouse button, or by scrolling with the " + altKey + " key pressed.", MessageType.Info);
				}
				else
				{
					EditorGUILayout.HelpBox ("Zooming is possible by scrolling with the " + altKey + " key pressed.", MessageType.Info);
				}

				autoPanNearWindowEdge = CustomGUILayout.Toggle ("Auto-panning in Editor?", autoPanNearWindowEdge, "AC.KickStarter.actionListManager.autoPanNearWindowEdge", "If True, the ActionList Editor will pan automatically when dragging the cursor near the window's edge");
				panSpeed = CustomGUILayout.FloatField ((actionListEditorScrollWheel == ActionListEditorScrollWheel.PansWindow) ? "Panning speed:" : "Zoom speed:", panSpeed, "AC.KickStarter.actionsManager.panSpeed", "The speed factor for panning/zooming");
				invertPanning = CustomGUILayout.Toggle ("Invert panning in Editor?", invertPanning, "AC.KickStarter.actionsManager.invertPanning", "If True, then panning is inverted in the ActionList Editor window (useful for Macbooks)");
				focusOnPastedActions = CustomGUILayout.Toggle ("Focus on pasted Actions?", focusOnPastedActions, "AC.KickStarter.actionListManager.focusOnPastedActions", "If True, then the ActionList Editor window will focus on newly - pasted Actions");
				allowMultipleActionListWindows = CustomGUILayout.Toggle ("Allow multiple Editors?", allowMultipleActionListWindows, "AC.KickStarter.actionsManager.allowMultipleActionListWindows", "If True, then multiple ActionList Editor windows can be opened at once");

				if (allFavouriteActionData != null && allFavouriteActionData.Count > 0)
				{
					if (GUILayout.Button ("Clear favourite Actions"))
					{
						Undo.RecordObject (this, "Clear favourite Actions");
						allFavouriteActionData.Clear ();
					}
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowCustomGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCustom = CustomGUILayout.ToggleHeader (showCustom, "Custom Action scripts");
			if (showCustom)
			{
				if (customFolderPaths.Count == 0)
				{
					customFolderPaths.Add (string.Empty);
				}

				for (int i=0; i<customFolderPaths.Count; i++)
				{
					customFolderPaths[i] = ShowCustomFolderGUI (i);
				}

				int lastIndex = customFolderPaths.Count - 1;
				if (lastIndex >= 0)
				{
					int lastButOneIndex = lastIndex - 1;

					if (!string.IsNullOrEmpty (customFolderPaths[lastIndex]))
					{
						customFolderPaths.Add (string.Empty);
					}
					else if (lastButOneIndex >= 0 && string.IsNullOrEmpty (customFolderPaths[lastButOneIndex]))
					{
						customFolderPaths.RemoveAt (lastIndex);
					}
				}
			}
			GUILayout.Space (3f);
			CustomGUILayout.EndVertical ();
		}


		private string ShowCustomFolderGUI (int i)
		{
			string _path = customFolderPaths[i];
			string displayPath = _path;
			if (!string.IsNullOrEmpty (displayPath) && displayPath.Length > 40)
			{
				displayPath = displayPath.Substring (0, 40) + "...";
			}

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Folder #" + i.ToString () + ":", GUILayout.Width (110f));
			GUILayout.Label (displayPath, EditorStyles.textField);

			if (GUILayout.Button (string.Empty, CustomStyles.FolderIcon))
			{
				string path = EditorUtility.OpenFolderPanel ("Set custom Actions directory", "Assets", "");
				string dataPath = Application.dataPath;
				if (path.Contains (dataPath))
				{
					if (path == dataPath)
					{
						_path = string.Empty;
					}
					else
					{
						_path = path.Replace (dataPath + "/", "");
					}
				}
				else if (!string.IsNullOrEmpty (path))
				{
					ACDebug.LogWarning ("Cannot set new directory - be sure to select within the Assets directory.");
				}
			}

			if (GUILayout.Button ("-", GUILayout.Width (22f)))
			{
				_path = string.Empty;
			}

			EditorGUILayout.EndHorizontal ();

			if (_path == FolderPath) _path = string.Empty;

			return _path;
		}


		private void ShowCategoriesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showCategories = CustomGUILayout.ToggleHeader (showCategories, "Action categories");
			if (showCategories)
			{
				ActionCategory[] categories = (ActionCategory[]) System.Enum.GetValues (typeof(ActionCategory));

				for (int i=0; i<categories.Length; i++)
				{
					if ((i % 4) == 0)
					{
						if (i > 0)
						{
							EditorGUILayout.EndHorizontal ();
						}
						EditorGUILayout.BeginHorizontal ();
					}

					if (GUILayout.Toggle (selectedCategoryInt == i, categories[i].ToString (), "Button", GUILayout.MinWidth (70f)))
					{
						if (selectedCategoryInt != i || selectedCategory != categories[i])
						{
							selectedCategoryInt = i;
							selectedCategory = categories[i];
							selectedClass = null;
						}
					}
				}

				EditorGUILayout.EndHorizontal ();
			}
			CustomGUILayout.EndVertical ();

			if (defaultClass > AllActions.Count - 1)
			{
				defaultClass = AllActions.Count - 1;
			}
		}


		private void ShowActionTypesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showActionTypes = CustomGUILayout.ToggleHeader (showActionTypes, "Category: " + selectedCategory);
			if (showActionTypes)
			{
				ActionType[] actionTypes = GetActionTypesInCategory (selectedCategory);

				if (actionTypes.Length == 0)
				{
					EditorGUILayout.HelpBox ("No Actions in this category found.", MessageType.Info);
					selectedClass = null;
				}

				for (int i=0; i<actionTypes.Length; i++)
				{
					if (actionTypes[i] == null) continue;

					EditorGUILayout.BeginHorizontal ();

					string label = actionTypes[i].title;
					if (!string.IsNullOrEmpty (defaultClassName) && actionTypes[i].fileName == defaultClassName)
					{
						label += " (DEFAULT)";
					}
					else if (!actionTypes[i].isEnabled)
					{
						label += " (DISABLED)";
					}
					else
					{
						foreach (DefaultActionCategoryData defaultActionCategoryData in defaultActionCategoryDatas)
						{
							if (defaultActionCategoryData.Category == actionTypes[i].category && defaultActionCategoryData.DefaultClassName == actionTypes[i].fileName)
							{
								label += " (CATEGORY DEFAULT)";
							}
						}
					}

					if (GUILayout.Toggle (actionTypes[i].IsMatch (selectedClass), label, "Button"))
					{
						selectedClass = actionTypes[i];
					}

					if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
					{
						SideMenu (AllActions.IndexOf (actionTypes[i]));
					}
					EditorGUILayout.EndHorizontal ();
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void ShowActionTypeGUI (float maxWidth)
		{
			if (selectedClass == null || string.IsNullOrEmpty (selectedClass.fileName)) return;
			
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showActionType = CustomGUILayout.ToggleHeader (showActionType, selectedClass.GetFullTitle ());
			if (showActionType)
			{
				SpeechLine.ShowField ("Name:", selectedClass.GetFullTitle (), false, maxWidth);
				SpeechLine.ShowField ("Filename:", selectedClass.fileName + ".cs", false, maxWidth);
				SpeechLine.ShowField ("Description:", selectedClass.description, true, maxWidth);

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Node colour:", GUILayout.Width (85f));
				selectedClass.color = EditorGUILayout.ColorField (selectedClass.color);
				EditorGUILayout.EndHorizontal ();

				if (!string.IsNullOrEmpty (defaultClassName) && selectedClass.fileName == defaultClassName)
				{
					EditorGUILayout.HelpBox ("This is marked as the default Action", MessageType.Info);
				}
				else
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Is enabled?", GUILayout.Width (85f));
					selectedClass.isEnabled = EditorGUILayout.Toggle (selectedClass.isEnabled);
					EditorGUILayout.EndHorizontal ();

					if (selectedClass.isEnabled)
					{
						foreach (DefaultActionCategoryData defaultActionCategoryData in defaultActionCategoryDatas)
						{
							if (defaultActionCategoryData.Category == selectedClass.category && defaultActionCategoryData.DefaultClassName == selectedClass.fileName)
							{
								EditorGUILayout.HelpBox ("This is marked as the default Action", MessageType.Info);
							}
						}
					}
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void SearchForInstances (bool justLocal, ActionType actionType)
		{
			bool foundInstance = false;
			if (justLocal)
			{
				foundInstance = SearchSceneForType (string.Empty, actionType);
			}
			else
			{
				// First look for lines that already have an assigned lineID
				string[] sceneFiles = AdvGame.GetSceneFiles ();
				if (sceneFiles == null || sceneFiles.Length == 0)
				{
					Debug.LogWarning ("Cannot search scenes - no enabled scenes could be found in the Build Settings.");
				}
				else
				{
					foreach (string sceneFile in sceneFiles)
					{
						bool foundSceneInstance = SearchSceneForType (sceneFile, actionType);
						if (foundSceneInstance)
						{
							foundInstance = true;
						}
					}
				}

				ActionListAsset[] allActionListAssets = AdvGame.GetReferences ().speechManager.GetAllActionListAssets ();
				foreach (ActionListAsset actionListAsset in allActionListAssets)
				{
					int[] foundIDs = SearchActionsForType (actionListAsset.actions, actionType);
					if (foundIDs != null && foundIDs.Length > 0)
					{
						ACDebug.Log ("(Asset: " + actionListAsset.name + ") Found " + foundIDs.Length + " instances of '" + actionType.GetFullTitle () + "' " + CreateIDReport (foundIDs), actionListAsset);
						foundInstance = true;
					}
				}
			}

			if (!foundInstance)
			{
				ACDebug.Log ("No instances of '" + actionType.GetFullTitle () + "' were found.");
			}
		}
		
		
		private bool SearchSceneForType (string sceneFile, ActionType actionType)
		{
			string sceneLabel = string.Empty;
			bool foundInstance = false;

			if (sceneFile != string.Empty)
			{
				sceneLabel = "(Scene: " + sceneFile + ") ";
				UnityVersionHandler.OpenScene (sceneFile);
			}

			// Speech lines and journal entries
			ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
			foreach (ActionList list in actionLists)
			{
				int[] foundIDs = SearchActionsForType (list.GetActions (), actionType);
				if (foundIDs != null && foundIDs.Length > 0)
				{
					ACDebug.Log (sceneLabel + " Found " + foundIDs.Length + " instances in '" + list.gameObject.name + "' " + CreateIDReport (foundIDs), list.gameObject);
					foundInstance = true;
				}
			}

			return foundInstance;
		}


		private string CreateIDReport (int[] foundIDs)
		{
			string idLabel = "(IDs ";
			for (int i=0; i<foundIDs.Length; i++)
			{
				idLabel += foundIDs[i];
				if (i < (foundIDs.Length - 1))
				{
					idLabel += ", ";
				}
			}
			idLabel += ")";
			return idLabel;
		}
		
		
		private int[] SearchActionsForType (List<Action> actionList, ActionType actionType)
		{
			List<int> foundIDs = new List<int>();
			if (actionList != null)
			{
				foreach (Action action in actionList)
				{
					if (action == null) continue;

					if ((action.Category == actionType.category && action.Title == actionType.title) ||
					    (action.GetType ().ToString () == actionType.fileName) ||
					    (action.GetType ().ToString () == "AC." + actionType.fileName))
					{
						int id = actionList.IndexOf (action);
						foundIDs.Add (id);
					}
				}
			}
			return foundIDs.ToArray ();
		}


		/** The folder path to the default Actions */
		public string FolderPath
		{
			get
			{
				return Resource.DefaultActionsPath;
			}
		}


		public string GetActionTypeLabel (Action _action, bool includeLabel)
		{
			if (!includeLabel)
			{
				return GetActionTypeLabel (_action);
			}

			int index = GetActionTypeIndex (_action);
			string suffix = (includeLabel) ? _action.SetLabel () : string.Empty;

			if (!string.IsNullOrEmpty (suffix))
			{
				suffix = " (" + suffix + ")";
			}

			if (index >= 0 && AllActions != null && index < AllActions.Count)
			{
				return AllActions[index].GetFullTitle () + suffix;
			}
			return _action.Category + ": " + _action.Title + suffix;
		}
		
		#endif


		public string GetActionTypeLabel (Action _action)
		{
			if (_action == null) return string.Empty;

			int index = GetActionTypeIndex (_action);

			if (index >= 0 && AllActions != null && index < AllActions.Count)
			{
				return AllActions[index].GetFullTitle ();
			}
			return _action.Category + ": " + _action.Title;
		}


		/**
		 * <summary>Gets the filename of an enabled Action.</summary>
		 * <param name = "i">The index number of the Action, in EnabledActions, to get the filename of</param>
		 * <returns>Gets the filename of the Action</returns>
		 */
		public string GetActionName (int i)
		{
			return (AllActions [i].fileName);
		}


		/**
		 * <summary>Checks if any enabled Actions have a specific filename.</summary>
		 * <param name = "_name">The filename to check for</param>
		 * <returns>True if any enabled Actions have the supplied filename</returns>
		 */
		public bool DoesActionExist (string _name)
		{
			foreach (ActionType actionType in AllActions)
			{
				if (_name == actionType.fileName || _name == ("AC." + actionType.fileName))
				{
					return true;
				}
			}
			return false;
		}
		

		/**
		 * <summary>Gets the number of enabled Actions.</summary>
		 * <returns>The number of enabled Actions</returns>
		 */
		public int GetActionsSize ()
		{
			return (AllActions.Count);
		}


		/**
		 * <summary>Gets all Action titles within EnabledActions.</summary>
		 * <returns>A string array of all Action titles within EnabledActions</returns>
		 */
		public string[] GetActionTitles ()
		{
			List<string> titles = new List<string>();
			
			foreach (ActionType type in AllActions)
			{
				titles.Add (type.title);
			}
			
			return (titles.ToArray ());
		}


		/**
		 * <summary>Gets the index number of an Action within EnabledActions.</summary>
		 * <param name = "_action">The Action to search for</param>
		 * <returns>The index number of the Action within EnabledActions</returns>
		 */
		public int GetActionTypeIndex (Action _action)
		{
			if (_action != null)
			{
				string className = _action.GetType ().ToString ();
				className = className.Replace ("AC.", "");
				foreach (ActionType actionType in AllActions)
				{
					if (actionType.fileName == className)
					{
						return AllActions.IndexOf (actionType);
					}
				}	
			}
			else
			{
				ACDebug.LogWarning ("Null Action found.  Was an Action class deleted?");
			}
			return defaultClass;
		}


		public ActionType GetActionType (Action _action)
		{
			if (_action != null)
			{
				string className = _action.GetType ().ToString ();
				className = className.Replace ("AC.", "");
				foreach (ActionType actionType in AllActions)
				{
					if (actionType.fileName == className)
					{
						return actionType;
					}
				}
			}
			else
			{
				ACDebug.LogWarning ("Null Action found.  Was an Action class deleted?");
			}
			return null;
		}


		/**
		 * <summary>Gets the index number of an Action within EnabledActions.</summary>
		 * <param name = "_category">The category of the Action to search for</param>
		 * <param name = "subCategoryIndex">The index number of the Action in a list of all Actions that share its category</param>
		 * <returns>The index number of the Action within EnabledActions</returns>
		 */
		public int GetEnabledActionTypeIndex (ActionCategory _category, int subCategoryIndex)
		{
			List<ActionType> types = new List<ActionType>();
			foreach (ActionType type in AllActions)
			{
				if (type.category == _category && type.isEnabled)
				{
					types.Add (type);
				}
			}
			if (subCategoryIndex < types.Count)
			{
				return AllActions.IndexOf (types[subCategoryIndex]);
			}
			return 0;
		}


		/**
		 * <summary>Gets all found Action titles within a given ActionCategory.</summary>
		 * <param name = "_category">The category of the Actions to get the titles of.</param>
		 * <returns>A string array of all Action titles within the ActionCategory</returns>
		 */
		public string[] GetActionSubCategories (ActionCategory _category)
		{
			List<string> titles = new List<string>();

			foreach (ActionType type in AllActions)
			{
				if (type.category == _category &&type.isEnabled)
				{
					titles.Add (type.title);
				}
			}
			
			return (titles.ToArray ());
		}
		

		/**
		 * <summary>Gets the ActionCategory of an Action within EnabledActions.</summary>
		 * <param name = "number">The index number of the Action's place in EnabledActions</param>
		 * <returns>The ActionCategory of the Action</returns>
		 */
		public ActionCategory GetActionCategory (int number)
		{
			if (AllActions == null || AllActions.Count == 0 || AllActions.Count < number)
			{
				return 0;
			}
			return AllActions[number].category;
		}
		

		/**
		 * <summary>Gets the index of an Action within a list of all Actions that share its category.</summary>
		 * <param name = "_action">The Action to get the index of</param>
		 * <returns>The index of the Action within a list of all Actions that share its category</returns>
		 */
		public int GetActionSubCategory (Action _action)
		{
			string fileName = _action.GetType ().ToString ().Replace ("AC.", "");
			ActionCategory _category = _action.Category;
			
			// Learn category
			foreach (ActionType type in AllActions)
			{
				if (type.fileName == fileName)
				{
					_category = type.category;
				}
			}
			
			// Learn subcategory
			int i=0;
			foreach (ActionType type in AllActions)
			{
				if (type.category == _category)
				{
					if (type.fileName == fileName)
					{
						return i;
					}
					i++;
				}
			}
			
			ACDebug.LogWarning ("Error building Action " + _action);
			return 0;
		}


		/**
		 * <summary>Gets all found ActionType classes that belong in a given category</summary>
		 * <param name = "category">The category of ActionType classes to collect</param>
		 * <retuns>An array of all ActionType classes that belong in the given category</returns>
		 */
		public ActionType[] GetActionTypesInCategory (ActionCategory category)
		{
			List<ActionType> types = new List<ActionType>();
			foreach (ActionType type in AllActions)
			{
				if (type.category == category)
				{
					types.Add (type);
				}
			}
			return types.ToArray ();
		}


		public bool IsActionTypeEnabled (int index)
		{
			if (AllActions != null && index < AllActions.Count)
			{
				return AllActions[index].isEnabled;
			}
			return false;
		}


		public Color GetActionTypeColor (Action _action)
		{
			int index = GetActionTypeIndex (_action);

			if (index >= 0 && AllActions != null && index < AllActions.Count)
			{
				return GUI.color = AllActions[index].color;
			}
			return Color.white;
		}


		#if UNITY_EDITOR

		public void SetFavourite (Action action, int ID)
		{
			FavouriteActionData existingFavouriteActionData = GetFavouriteActionData (ID);
			if (existingFavouriteActionData != null)
			{
				existingFavouriteActionData.Update (action);
				return;
			}

			FavouriteActionData newFavouriteActionData = new FavouriteActionData (action, ID);
			allFavouriteActionData.Add (newFavouriteActionData);
		}


		public string GetFavouriteActionLabel (int ID)
		{
			FavouriteActionData existingFavouriteActionData = GetFavouriteActionData (ID);
			if (existingFavouriteActionData != null)
			{
				return existingFavouriteActionData.Label;
			}
			return string.Empty;
		}


		public Action GenerateFavouriteAction (int ID)
		{
			FavouriteActionData existingFavouriteActionData = GetFavouriteActionData (ID);
			if (existingFavouriteActionData != null)
			{
				return existingFavouriteActionData.Generate ();
			}
			return null;
		}


		public int GetNumFavouriteActions ()
		{
			return allFavouriteActionData.Count;
		}


		private FavouriteActionData GetFavouriteActionData (int ID)
		{
			foreach (FavouriteActionData favouriteActionData in allFavouriteActionData)
			{
				if (favouriteActionData.ID == ID)
				{
					return favouriteActionData;
				}
			}
			return null;
		}

		
		public int GetDefaultActionInCategory (ActionCategory category)
		{
			foreach (DefaultActionCategoryData defaultActionCategoryData in defaultActionCategoryDatas)
			{
				if (defaultActionCategoryData.Category == category)
				{
					List<ActionType> types = new List<ActionType> ();
					foreach (ActionType type in AllActions)
					{
						if (type.category == category && type.isEnabled)
						{
							types.Add (type);
						}
					}

					foreach (ActionType type in types)
					{
						if (type.fileName == defaultActionCategoryData.DefaultClassName)
						{
							if (type.isEnabled)
							{
								return types.IndexOf (type);
							}
							return 0;
						}
					}
					return 0;
				}
			}
			return 0;
		}


		[System.Serializable]
		private class DefaultActionCategoryData
		{

			[SerializeField] private ActionCategory category;
			[SerializeField] private string defaultClassName;

			public DefaultActionCategoryData (ActionCategory _category, string _defaultClassName)
			{
				category = _category;
				defaultClassName = _defaultClassName;
			}

			public ActionCategory Category { get { return category; } }
			public string DefaultClassName { get { return defaultClassName; } set { defaultClassName = value; } }

		}

		#endif

	}
	
}