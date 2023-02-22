/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionListAsset.cs"
 * 
 *	This script stores a list of Actions in an asset file.
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
	 * An ActionListAsset is a ScriptableObject that allows a List of Action objects to be stored within an asset file.
	 * When the file is run, the Actions are transferred to a local instance of RuntimeActionList and run from there.
	 */
	[System.Serializable]
	#if UNITY_EDITOR
	[InitializeOnLoad]
	#endif
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_action_list_asset.html")]
	public class ActionListAsset : ScriptableObject, IItemReferencer, IDocumentReferencer, IObjectiveReferencer, IMenuReferencer, IVariableReferencer
	{

		/** The Actions within this asset file */
		#if AC_ActionListPrefabs
		[SerializeReference] public List<AC.Action> actions = new List<AC.Action>();
		#else
		public List<AC.Action> actions = new List<AC.Action>();
		#endif
		/** If True, the Actions will be skipped when the user presses the 'EndCutscene' Input button */
		public bool isSkippable = true;
		/** The effect that running the Actions has on the rest of the game (PauseGameplay, RunInBackground) */
		public ActionListType actionListType = ActionListType.PauseGameplay;
		/** If True, the game will un-freeze itself while the Actions run if the game was previously paused */
		public bool unfreezePauseMenus = true;
		/** If True, ActionParameters can be used to override values within the Action objects */
		public bool useParameters = false;
		/** If True, then multiple instances of this asset can be run simultaneously in the scene */
		public bool canRunMultipleInstances = false;
		/** If True, then the ActionList will not end when the scene changes through natural gameplay (but will still end when loading a save game file) */
		public bool canSurviveSceneChanges = false;
		/** If True, and useParameters = True, then the parameters will revert to their default values after each time the Actions are run */
		public bool revertToDefaultParametersAfterRunning = false;

		[SerializeField] private List<ActionParameter> parameters = new List<ActionParameter>();
		private List<ActionParameter> runtimeParameters = new List<ActionParameter> ();

		/** The ID of the associated SpeechTag */
		[HideInInspector] public int tagID;

		#if UNITY_EDITOR && UNITY_2019_2_OR_NEWER
		[SerializeField] private JsonAction[] backupData;

		public void BackupData ()
		{
			var newBackupData = JsonAction.BackupActions (actions);
			if (newBackupData != null)
			{
				backupData = JsonAction.BackupActions (actions);
			}
		}


		public void RestoreData ()
		{
			if (backupData != null && backupData.Length > 0)
			{
				actions = JsonAction.RestoreActions (backupData);
				ActionListAsset.SyncAssetObjects (this);
			}
		}

		#endif
		
		#if UNITY_EDITOR

		private void OnEnable ()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				ResetParameters ();
			}

			EditorApplication.playModeStateChanged += OnPlayStateChange;
		}


		private void OnDisable ()
		{
			EditorApplication.playModeStateChanged -= OnPlayStateChange;

			foreach (Action action in actions)
			{
				if (action == null) continue;
				action.ResetAssetValues ();
			}
		}


		private void OnPlayStateChange (PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.ExitingPlayMode:
					ResetParameters ();
					break;
			}
			
		}


		[MenuItem("CONTEXT/ActionListAsset/Convert to Cutscene")]
		public static void ConvertToCutscene (MenuCommand command)
		{
			ActionListAsset actionListAsset = (ActionListAsset) command.context;
			GameObject newOb = new GameObject (actionListAsset.name);
			if (GameObject.Find ("_Cutscenes") && GameObject.Find ("_Cutscenes").transform.position == Vector3.zero)
			{
				newOb.transform.parent = GameObject.Find ("_Cutscenes").transform;
			}
			Cutscene cutscene = newOb.AddComponent <Cutscene>();
			cutscene.CopyFromAsset (actionListAsset);
			ACDebug.Log ("ActionList asset " + actionListAsset.name + " has been converted into a Cutscene.", cutscene);
			EditorGUIUtility.PingObject (newOb);
		}


		[MenuItem("CONTEXT/ActionList/Convert to ActionList asset")]
		public static void ConvertToActionListAsset (MenuCommand command)
		{
			ActionList actionList = (ActionList) command.context;

			bool referenceNewAsset = EditorUtility.DisplayDialog ("Reference asset?", "Do you want the existing ActionList '" + actionList.name + "' to use the new ActionList asset as it's Actions source?", "Yes", "No");

			ScriptableObject t = CustomAssetUtility.CreateAsset <ActionListAsset> (actionList.gameObject.name);

			ActionListAsset actionListAsset = (ActionListAsset) t;
			actionListAsset.CopyFromActionList (actionList);
			AssetDatabase.SaveAssets ();
			ACDebug.Log ("ActionList " + actionList.name + " has been converted into an ActionList asset.", t);
			EditorGUIUtility.PingObject (t);

			EditorUtility.SetDirty (actionListAsset);

			if (referenceNewAsset)
			{
				actionList.source = ActionListSource.AssetFile;
				actionList.assetFile = actionListAsset;
			}
		}


		[MenuItem ("CONTEXT/ActionListAsset/Find references")]
		public static void FindGlobalReferences (MenuCommand command)
		{
			ActionListAsset actionListAsset = (ActionListAsset)command.context;

			if (EditorUtility.DisplayDialog ("Search '" + actionListAsset.name + "' references?", "The Editor will search assets, and active scenes listed in the Build Settings, for references to this ActionList asset.  The current scene will need to be saved and listed to be included in the search process. Continue?", "OK", "Cancel"))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					bool foundReference = false;

					// Menus
					if (KickStarter.menuManager != null)
					{
						foreach (Menu menu in KickStarter.menuManager.menus)
						{
							if (menu.ReferencesAsset (actionListAsset))
							{
								Debug.Log ("'" + actionListAsset.name + "' is referenced by Menu '" + menu.title + "'");
								foundReference = true;
							}

							foreach (MenuElement element in menu.elements)
							{
								if (element != null && element.ReferencesAsset (actionListAsset))
								{
									Debug.Log ("'" + actionListAsset.name + "' is referenced by Menu Element '" + element.title + "' in Menu '" + menu.title + "'");
									foundReference = true;
								}
							}
						}
					}

					// Settings
					if (KickStarter.settingsManager != null)
					{
						if (KickStarter.settingsManager.actionListOnStart == actionListAsset)
						{
							Debug.Log ("'" + actionListAsset.name + "' is referenced by the Settings Manager");
							foundReference = true;
						}
					}

					// Inventory
					if (KickStarter.inventoryManager != null)
					{
						if (KickStarter.inventoryManager.unhandledCombine == actionListAsset ||
							KickStarter.inventoryManager.unhandledGive == actionListAsset ||
							KickStarter.inventoryManager.unhandledHotspot == actionListAsset)
						{
							Debug.Log ("'" + actionListAsset.name + "' is referenced by the Inventory Manager");
							foundReference = true;
						}

						foreach (Recipe recipe in KickStarter.inventoryManager.recipes)
						{
							if (recipe.actionListOnCreate == actionListAsset ||
							   (recipe.onCreateRecipe == OnCreateRecipe.RunActionList && recipe.invActionList == actionListAsset))
							{
								Debug.Log ("'" + actionListAsset.name + "' is referenced by Recipe " + recipe.EditorLabel);
								foundReference = true;
							}
						}

						foreach (InvItem invItem in KickStarter.inventoryManager.items)
						{
							if (invItem.ReferencesAsset (actionListAsset))
							{
								Debug.Log ("'" + actionListAsset.name + "' is referenced by Inventory Item " + invItem.EditorLabel);
								foundReference = true;
							}
						}
					}

					// Cursor
					if (KickStarter.cursorManager != null)
					{
						if (KickStarter.cursorManager.AllowUnhandledIcons ())
						{
							foreach (ActionListAsset unhandledCursorInteraction in KickStarter.cursorManager.unhandledCursorInteractions)
							{
								if (unhandledCursorInteraction == actionListAsset)
								{
									Debug.Log ("'" + actionListAsset.name + "' is referenced by the Cursor Manager");
									foundReference = true;
								}
							}
						}
					}

					// ActionListAssets
					if (KickStarter.speechManager != null)
					{
						ActionListAsset[] allAssets = KickStarter.speechManager.GetAllActionListAssets ();
						foreach (ActionListAsset asset in allAssets)
						{
							if (asset == actionListAsset) continue;

							foreach (Action action in asset.actions)
							{
								if (action != null)
								{
									if (action.ReferencesAsset (actionListAsset))
									{
										string actionLabel = (KickStarter.actionsManager != null) ? (" (" + KickStarter.actionsManager.GetActionTypeLabel (action) + ")") : "";
										Debug.Log ("'" + actionListAsset.name + "' is referenced by Action #" + asset.actions.IndexOf (action) + actionLabel + " in ActionList asset '" + asset.name + "'", asset);
										foundReference = true;
									}
								}
							}
						}
					}

					// Scenes
					string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
					string[] sceneFiles = AdvGame.GetSceneFiles ();

					foreach (string sceneFile in sceneFiles)
					{
						UnityVersionHandler.OpenScene (sceneFile);

						string suffix = " in scene '" + sceneFile + "'";
						
						// iActionListAssetReferencers
						MonoBehaviour[] sceneObjects = FindObjectsOfType<MonoBehaviour> ();
						for (int i = 0; i < sceneObjects.Length; i++)
						{
							MonoBehaviour currentObj = sceneObjects[i];
							iActionListAssetReferencer currentComponent = currentObj as iActionListAssetReferencer;
							if (currentComponent != null && currentComponent.ReferencesAsset (actionListAsset))
							{
								Debug.Log ("'" + actionListAsset.name + "' is referenced by '" + currentComponent + "'" + suffix);
								foundReference = true;
							}
						}
					}

					UnityVersionHandler.OpenScene (originalScene);

					if (!foundReference)
					{
						Debug.Log ("No instances of '" + actionListAsset.name + "' were found.", actionListAsset);
					}
				}
			}
		}


		public static void SyncAssetObjects (ActionListAsset actionListAsset)
		{
			#if !AC_ActionListPrefabs

			bool modified = false;

			// Search for assets to delete
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath (AssetDatabase.GetAssetPath (actionListAsset));
			foreach (Object asset in assets)
			{
				Action actionAsset = asset as Action;
				if (actionAsset != null)
				{
					bool foundMatch = false;

					foreach (Action action in actionListAsset.actions)
					{
						if (actionAsset == action)
						{
							foundMatch = true;
							break;
						}
					}

					if (!foundMatch)
					{
						//Debug.LogWarning ("Found no match of asset " + actionAsset + " - deleting now");
						Undo.DestroyObjectImmediate (actionAsset);
						modified = true;
					}
				}
			}

			// Search for assets to add
			foreach (Action action in actionListAsset.actions)
			{
				if (action != null)
				{
					bool foundMatch = false;

					foreach (Object asset in assets)
					{
						Action actionAsset = asset as Action;
						if (actionAsset == action)
						{
							foundMatch = true;
							break;
						}
					}

					if (!foundMatch)
					{
						action.hideFlags = HideFlags.HideInHierarchy;
						AssetDatabase.AddObjectToAsset (action, actionListAsset);
						AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (action));
						//Debug.LogWarning ("Found no match of " + action + " '" + action.name + "' in database - adding now to " + AssetDatabase.GetAssetPath (action));
						modified = true;
					}
				}
			}

			if (modified)
			{
				AssetDatabase.SaveAssets ();
				AssetDatabase.Refresh ();
			}

			#endif
		}


		public static ActionListAsset CreateFromActions (string fileName, string filePath, List<Action> _actions)
		{
			ActionListAsset newAsset = CustomAssetUtility.CreateAsset<ActionListAsset> (fileName, filePath);
			newAsset = AddActionsToAsset (newAsset, _actions, false);
			
			AssetDatabase.SaveAssets ();
			
			return newAsset;
		}


		private static ActionListAsset AddActionsToAsset (ActionListAsset asset, List<Action> _actions, bool doCopy)
		{
			#if AC_ActionListPrefabs

			if (doCopy)
			{
				JsonAction.ToCopyBuffer (_actions);
				asset.actions = JsonAction.CreatePasteBuffer ();
				JsonAction.ClearCopyBuffer ();

				foreach (Action action in asset.actions)
				{
					action.isAssetFile = true;
					action.AssignConstantIDs ();
					action.isMarked = false;
					action.ClearIDs ();
					action.parentActionListInEditor = null;
				}
			}
			else
			{
				asset.actions = _actions;

				foreach (Action action in asset.actions)
				{
					action.isAssetFile = true;
				}
			}

			#else

			if (doCopy)
			{
				asset.actions = new List<Action>();
				asset.actions.Clear ();
			
				Vector2 firstPosition = new Vector2 (14f, 14f);
				foreach (Action originalAction in _actions)
				{
					if (originalAction == null)
					{
						continue;
					}
				
					Action duplicatedAction = Instantiate (originalAction) as AC.Action;
				
					if (_actions.IndexOf (originalAction) == 0)
					{
						Rect newRect = new Rect (firstPosition, duplicatedAction.NodeRect.size);
						duplicatedAction.NodeRect = newRect;
					}
					else
					{
						Rect newRect = new Rect (originalAction.NodeRect.position, duplicatedAction.NodeRect.size);
						duplicatedAction.NodeRect = newRect;
					}

					duplicatedAction.isAssetFile = true;
					duplicatedAction.AssignConstantIDs ();
					duplicatedAction.isMarked = false;
					duplicatedAction.ClearIDs ();
					duplicatedAction.parentActionListInEditor = null;

					duplicatedAction.hideFlags = HideFlags.HideInHierarchy;
				
					AssetDatabase.AddObjectToAsset (duplicatedAction, asset);
					AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (duplicatedAction));
					AssetDatabase.SaveAssets ();
					AssetDatabase.Refresh ();

					asset.actions.Add (duplicatedAction);
				}
			}
			else
			{
				asset.actions = _actions;
				foreach (Action action in asset.actions)
				{
					if (action == null) continue;

					action.isAssetFile = true;
					action.hideFlags = HideFlags.HideInHierarchy;

					AssetDatabase.AddObjectToAsset (action, asset);
					AssetDatabase.ImportAsset (AssetDatabase.GetAssetPath (action));
					AssetDatabase.SaveAssets ();
					AssetDatabase.Refresh ();
				}
			}

			#endif

			return asset;
		}


		public void CopyFromActionList (ActionList actionList)
		{
			isSkippable = actionList.isSkippable;
			actionListType = actionList.actionListType;
			useParameters = actionList.useParameters;
			
			// Copy parameters
			parameters = new List<ActionParameter>();
			parameters.Clear ();
			foreach (ActionParameter parameter in actionList.parameters)
			{
				parameters.Add (new ActionParameter (parameter, true));
			}
			
			// Actions
			AddActionsToAsset (this, actionList.actions, true);
		}

		#endif

		/**
		 * <summary>Checks if the ActionListAsset is skippable. This is safer than just reading 'isSkippable', because it also accounts for actionListType - since ActionLists that run in the background cannot be skipped</summary>
		 * <returns>True if the ActionListAsset is skippable</returns>
		 */
		public bool IsSkippable ()
		{
			if (isSkippable && actionListType == ActionListType.PauseGameplay)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Runs the ActionList asset file</summary>
		 */
		public void Interact ()
		{
			AdvGame.RunActionListAsset (this);
		}


		/**
		 * <summary>Runs the ActionList asset file, but updates parameter values before doing so</summary>
		 * <param name="newParameters">The new parameter values</param>
		 */
		public void Interact (List<ActionParameter> newParameters)
		{
			AssignParameterValues (newParameters);
			Interact ();
		}


		/**
		 * <summary>Runs the ActionList asset file from a set point.</summary>
		 * <param name = "index">The index number of actions to start from</param>
		 */
		public void RunFromIndex (int index)
		{
			AdvGame.RunActionListAsset (this, index, true);
		}


		/**
		 * <summary>Runs the ActionList asset file, after setting the value of an integer parameter if it has one.</summary>
		 * <param name = "parameterID">The ID of the Integer parameter to set</param>
		 * <param name = "parameterValue">The value to set the Integer parameter to</param>
		 */
		public RuntimeActionList Interact (int parameterID, int parameterValue)
		{
			return AdvGame.RunActionListAsset (this, parameterID, parameterValue);
		}


		/**
		 * <summary>Gets an array of all ActionLists associated with this asset that are currently running.</summary>
		 * <returns>All ActionLists associated with this asset that are currently running.</returns>
		 */
		public RuntimeActionList[] GetRuntimeInstances ()
		{
			ActiveList[] activeLists = KickStarter.actionListAssetManager.ActiveLists.ToArray ();
			List<RuntimeActionList> runtimeInstances = new List<RuntimeActionList>();
			foreach (ActiveList activeList in activeLists)
			{
				if (activeList.IsFor (this) && activeList.actionList)
				{
					RuntimeActionList runtimeActionList = activeList.actionList as RuntimeActionList;
					if (runtimeActionList)
					{
						runtimeInstances.Add (runtimeActionList);
					}
				}
			}

			return runtimeInstances.ToArray ();
		}


		/**
		 * <summary>Kills all currently-running instances of the asset.</summary>
		 */
		public void KillAllInstances ()
		{
			if (KickStarter.actionListAssetManager != null)
			{
				int numRemoved = KickStarter.actionListAssetManager.EndAssetList (this, null, true);
				ACDebug.Log ("Ended " + numRemoved + " instances of the ActionList Asset '" + this.name + "'", this);
			}
		}


		/**
		 * <summary>Gets a parameter of a given ID number. This is not a default parameter, but one used at runtime to actually modify Actions.</summary>
		 * <param name = "_ID">The ID of the parameter to get</param>
		 * <returns>The parameter with the given ID number</returns>
		 */
		public ActionParameter GetParameter (int _ID)
		{
			if (useParameters && parameters != null)
			{
				#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					foreach (ActionParameter parameter in parameters)
					{
						if (parameter.ID == _ID)
						{
							return parameter;
						}
					}
				}
				#endif

				if (runtimeParameters == null) runtimeParameters = new List<ActionParameter> ();

				foreach (ActionParameter parameter in runtimeParameters)
				{
					if (parameter.ID == _ID)
					{
						return parameter;
					}
				}

				foreach (ActionParameter parameter in parameters)
				{
					if (parameter.ID == _ID)
					{
						ActionParameter newRuntimeParameter = new ActionParameter (parameter, true);
						runtimeParameters.Add (newRuntimeParameter);
						return newRuntimeParameter;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Gets all parameters associated with the asset. If called in Edit mode, these will be the default parameters.  If called at runtime, this will instead return the parameters used at runtime to actually modify Actions.</summary>
		 * <returns>All parameters associated with the asset</returns>
		 */
		public List<ActionParameter> GetParameters ()
		{
			if (useParameters && parameters != null)
			{
				#if UNITY_EDITOR
				if (!Application.isPlaying) return parameters;
				#endif

				if (runtimeParameters == null) runtimeParameters = new List<ActionParameter> ();

				foreach (ActionParameter parameter in parameters)
				{
					GetParameter (parameter.ID);
				}
				return runtimeParameters;
			}
			return null;
		}


		/**
		 * <summary>Updates a List of parameter values to be used at runtime.</summary>
		 * <param name="newParameters">The new parameter values.  Parameters will be updated by matchind ID value, not by index.  Parameters that are not included in the list will not be updated.</param>
		 */
		public void AssignParameterValues (List<ActionParameter> newParameters)
		{
			if (useParameters && parameters != null)
			{
				if (runtimeParameters == null) runtimeParameters = new List<ActionParameter> ();

				foreach (ActionParameter newParameter in newParameters)
				{
					ActionParameter matchingParameter = GetParameter (newParameter.ID);
					if (matchingParameter != null)
					{
						matchingParameter.CopyValues (newParameter);
					}
				}
			}
		}


		/** Called after the Actions are downloaded to a RuntimeActionList instance */
		public void AfterDownloading ()
		{
			if (revertToDefaultParametersAfterRunning)
			{
				ResetParameters ();
			}
		}


		/** The default List of ActionParameter objects that can be used to override values within the Actions, if useParameters = True */
		public List<ActionParameter> DefaultParameters
		{
			get
			{
				return parameters;
			}
			set
			{
				parameters = value;
			}
		}


		/** The number of parameters associated with the asset */
		public int NumParameters
		{
			get
			{
				if (useParameters && parameters != null) return parameters.Count;
				return 0;
			}
		}


		private void ResetParameters ()
		{
			runtimeParameters.Clear ();
		}


		#if UNITY_EDITOR

		public int GetNumItemReferences (int itemID)
		{
			int totalNumReferences = 0;

			if (NumParameters > 0)
			{
				int thisNumReferences = GetParameterReferences (parameters, itemID, ParameterType.InventoryItem);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IItemReferencerAction)
				{
					IItemReferencerAction itemReferencerAction = action as IItemReferencerAction;
					int thisNumReferences = itemReferencerAction.GetNumItemReferences (itemID, DefaultParameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}
			return totalNumReferences;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID)
		{
			int totalNumReferences = 0;

			if (NumParameters > 0)
			{
				int thisNumReferences = GetParameterReferences (parameters, oldItemID, ParameterType.InventoryItem, null, 0, true, oldItemID);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IItemReferencerAction)
				{
					IItemReferencerAction itemReferencerAction = action as IItemReferencerAction;
					int thisNumReferences = itemReferencerAction.UpdateItemReferences (oldItemID, newItemID, DefaultParameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}
			return totalNumReferences;
		}


		public int GetNumMenuReferences (string menuName, string elementName = "")
		{
			int totalNumReferences = 0;

			foreach (Action action in actions)
			{
				if (action != null && action is IMenuReferencer)
				{
					IMenuReferencer menuReferencer = action as IMenuReferencer;
					int thisNumReferences = menuReferencer.GetNumMenuReferences (menuName, elementName);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumVariableReferences (VariableLocation variableLocation, int variableID, Variables _variables = null, int _variablesConstantID = 0)
		{
			int totalNumReferences = 0;

			if (NumParameters > 0)
			{
				switch (variableLocation)
				{
					case VariableLocation.Global:
						totalNumReferences += GetParameterReferences (parameters, variableID, ParameterType.GlobalVariable);
						break;

					case VariableLocation.Component:
						totalNumReferences += GetParameterReferences (parameters, variableID, ParameterType.ComponentVariable, _variables, _variablesConstantID);
						break;

					default:
						break;
				}

				foreach (Action action in actions)
				{
					if (action != null && action is IVariableReferencerAction)
					{
						IVariableReferencerAction variableReferencerAction = action as IVariableReferencerAction;
						int thisNumReferences = variableReferencerAction.GetNumVariableReferences (variableLocation, variableID, DefaultParameters, _variables, _variablesConstantID);
						if (thisNumReferences > 0)
						{
							totalNumReferences += thisNumReferences;
							ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
						}
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateVariableReferences (VariableLocation variableLocation, int oldVariableID, int newVariableID, Variables _variables = null, int _variablesConstantID = 0)
		{
			int totalNumReferences = 0;

			if (NumParameters > 0)
			{
				switch (variableLocation)
				{
					case VariableLocation.Global:
						totalNumReferences += GetParameterReferences (parameters, oldVariableID, ParameterType.GlobalVariable, null, 0, true, newVariableID);
						break;

					case VariableLocation.Component:
						totalNumReferences += GetParameterReferences (parameters, oldVariableID, ParameterType.ComponentVariable, _variables, _variablesConstantID, true, newVariableID);
						break;

					default:
						break;
				}

				foreach (Action action in actions)
				{
					if (action != null && action is IVariableReferencerAction)
					{
						IVariableReferencerAction variableReferencerAction = action as IVariableReferencerAction;
						int thisNumReferences = variableReferencerAction.UpdateVariableReferences (variableLocation, oldVariableID, newVariableID, DefaultParameters, _variables, _variablesConstantID);
						if (thisNumReferences > 0)
						{
							totalNumReferences += thisNumReferences;
							ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
						}
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumDocumentReferences (int documentID)
		{
			int totalNumReferences = 0;

			if (NumParameters > 0)
			{
				int thisNumReferences = GetParameterReferences (parameters, documentID, ParameterType.Document);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IDocumentReferencerAction)
				{
					IDocumentReferencerAction documentReferencerAction = action as IDocumentReferencerAction;
					int thisNumReferences = documentReferencerAction.GetNumDocumentReferences (documentID, DefaultParameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateDocumentReferences (int oldDocumentID, int newDocumentID)
		{
			int totalNumReferences = 0;

			if (NumParameters > 0)
			{
				int thisNumReferences = GetParameterReferences (parameters, oldDocumentID, ParameterType.Document, null, 0, true, newDocumentID);
				if (thisNumReferences > 0)
				{
					totalNumReferences += thisNumReferences;
				}
			}

			foreach (Action action in actions)
			{
				if (action != null && action is IDocumentReferencerAction)
				{
					IDocumentReferencerAction documentReferencerAction = action as IDocumentReferencerAction;
					int thisNumReferences = documentReferencerAction.UpdateDocumentReferences (oldDocumentID, newDocumentID, DefaultParameters);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int GetNumObjectiveReferences (int objectiveID)
		{
			int totalNumReferences = 0;

			foreach (Action action in actions)
			{
				if (action != null && action is IObjectiveReferencerAction)
				{
					IObjectiveReferencerAction documentReferencerAction = action as IObjectiveReferencerAction;
					int thisNumReferences = documentReferencerAction.GetNumObjectiveReferences (objectiveID);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		public int UpdateObjectiveReferences (int oldObjectiveID, int newObjectiveID)
		{
			int totalNumReferences = 0;

			foreach (Action action in actions)
			{
				if (action != null && action is IObjectiveReferencerAction)
				{
					IObjectiveReferencerAction documentReferencerAction = action as IObjectiveReferencerAction;
					int thisNumReferences = documentReferencerAction.UpdateObjectiveReferences (oldObjectiveID, newObjectiveID);
					if (thisNumReferences > 0)
					{
						totalNumReferences += thisNumReferences;
						ActionList.logSuffix += "\n (" + actions.IndexOf (action) + ") " + action.Category + ": " + action.Title;
					}
				}
			}

			return totalNumReferences;
		}


		private int GetParameterReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType, Variables _variables = null, int _variablesConstantID = 0, bool updateID = false, int _newID = 0)
		{
			int thisCount = 0;

			foreach (ActionParameter parameter in parameters)
			{
				if (parameter != null && parameter.parameterType == _paramType && _ID == parameter.intValue)
				{
					if (_paramType == ParameterType.ComponentVariable)
					{
						if (_variables && parameter.variables != _variables)
						{
							continue;
						}
						if (parameter.constantID == 0 || _variablesConstantID == 0 || parameter.constantID != _variablesConstantID)
						{
							continue;
						}
					}

					if (updateID)
					{
						parameter.intValue = _newID;
					}
					thisCount++;
				}
			}

			return thisCount;
		}

		#endif

	}


	public class ActionListAssetMenu
	{

		#if UNITY_EDITOR
	
		[MenuItem ("Assets/Create/Adventure Creator/ActionList")]
		public static ActionListAsset CreateAsset ()
		{
			string assetName = "New ActionList";

			ScriptableObject t = CustomAssetUtility.CreateAsset <ActionListAsset> (assetName);
			EditorGUIUtility.PingObject (t);
			ACDebug.Log ("Created ActionList: " + assetName, t);
			return (ActionListAsset) t;
		}


		public static ActionListAsset CreateAsset (string assetName)
		{
			if (string.IsNullOrEmpty (assetName))
			{
				return CreateAsset ();
			}

			ScriptableObject t = CustomAssetUtility.CreateAsset <ActionListAsset> (assetName);
			EditorGUIUtility.PingObject (t);
			ACDebug.Log ("Created ActionList: " + assetName, t);
			return (ActionListAsset) t;
		}


		public static ActionListAsset AssetGUI (string label, ActionListAsset actionListAsset, string defaultName = "", string api = "", string tooltip = "")
		{
			EditorGUILayout.BeginHorizontal ();
			actionListAsset = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> (label, actionListAsset, false, api, tooltip);

			if (actionListAsset == null)
			{
				if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
				{
					#if !(UNITY_WP8 || UNITY_WINRT)
					defaultName = System.Text.RegularExpressions.Regex.Replace (defaultName, "[^\\w\\._]", "");
					#else
					defaultName = "";
					#endif

					actionListAsset = ActionListAssetMenu.CreateAsset (defaultName);
				}
			}

			EditorGUILayout.EndHorizontal ();
			return actionListAsset;
		}


		public static Cutscene CutsceneGUI (string label, Cutscene cutscene, string defaultName = "", string api = "", string tooltip = "")
		{
			EditorGUILayout.BeginHorizontal ();
			cutscene = (Cutscene) CustomGUILayout.ObjectField <Cutscene> (label, cutscene, true, api, tooltip);

			if (cutscene == null)
			{
				if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
				{
					cutscene = SceneManager.AddPrefab ("Logic", "Cutscene", true, false, true).GetComponent <Cutscene>();
					cutscene.Initialise ();

					if (!string.IsNullOrEmpty (defaultName))
					{
						cutscene.gameObject.name = AdvGame.UniqueName (defaultName);
					}
				}
			}

			EditorGUILayout.EndHorizontal ();
			return cutscene;
		}

		#endif


	}

}