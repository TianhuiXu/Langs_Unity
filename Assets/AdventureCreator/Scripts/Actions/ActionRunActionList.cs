/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionRunActionList.cs"
 * 
 *	This Action runs other ActionLists
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionRunActionList : Action, IItemReferencerAction, IDocumentReferencerAction
	{
		
		public enum ListSource { InScene, AssetFile };
		public ListSource listSource = ListSource.InScene;

		public ActionList actionList;
		public int constantID = 0;
		public int parameterID = -1;

		public ActionListAsset invActionList;
		public int assetParameterID = -1;

		public bool runFromStart = true;
		public int jumpToAction;
		public int jumpToActionParameterID = -1;
		public AC.Action jumpToActionActual;
		public bool runInParallel = false; // No longer visible, but needed for legacy upgrades

		public bool isSkippable = false; // Important: Set by ActionList, to determine whether or not the ActionList it runs should be added to the skip queue

		public List<ActionParameter> localParameters = new List<ActionParameter>();
		public List<int> parameterIDs = new List<int>();

		public bool setParameters = false; // Deprecated

		protected bool isAwaitingDelay;

		protected RuntimeActionList runtimeActionList;

		[SerializeField] protected RunMode runMode = RunMode.RunOnly;
		protected RunMode runtimeRunMode;
		protected enum RunMode { RunOnly, SetParametersAndRun, SetParametersOnly };


		public override ActionCategory Category { get { return ActionCategory.ActionList; }}
		public override string Title { get { return "Run"; }}
		public override string Description { get { return "Runs any ActionList (either scene-based like Cutscenes, Triggers and Interactions, or ActionList assets). If the new ActionList takes parameters, this Action can be used to set them."; }}
		

		public override void Upgrade ()
		{
			base.Upgrade ();

			if (!runInParallel)
			{
				runInParallel = true;
				endings[0].resultAction = ResultAction.Stop;
			}

			if (setParameters)
			{
				setParameters = false;
				runMode = RunMode.SetParametersAndRun;
			}
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeRunMode = runMode;
			isAwaitingDelay = false;

			if (listSource == ListSource.InScene)
			{
				actionList = AssignFile <ActionList> (parameters, parameterID, constantID, actionList);
				jumpToAction = AssignInteger (parameters, jumpToActionParameterID, jumpToAction);

				if (parameterID > 0)
				{
					runtimeRunMode = RunMode.RunOnly;
				}
			}
			else if (listSource == ListSource.AssetFile)
			{
				invActionList = (ActionListAsset) AssignObject <ActionListAsset> (parameters, assetParameterID, invActionList);

				if (assetParameterID > 0)
				{
					runtimeRunMode = RunMode.RunOnly;
				}
			}

			if (localParameters != null && localParameters.Count > 0)
			{
				for (int i=0; i<localParameters.Count; i++)
				{
					if (parameterIDs != null && parameterIDs.Count > i && parameterIDs[i] >= 0)
					{
						int ID = parameterIDs[i];
						foreach (ActionParameter parameter in parameters)
						{
							if (parameter.ID == ID)
							{
								localParameters[i].CopyValues (parameter);
								break;
							}
						}
					}
				}
			}
		}


		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				runtimeActionList = null;

				switch (listSource)
				{
					case ListSource.InScene:
						if (actionList != null && !actionList.actions.Contains (this))
						{
							KickStarter.actionListManager.EndList (actionList);

							if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
							{
								if (actionList.syncParamValues)
								{
									SendParameters (actionList.assetFile.GetParameters (), true);
								}
								else
								{
									SendParameters (actionList.parameters, false);
								}
								if (runtimeRunMode == RunMode.SetParametersOnly)
								{
									isRunning = false;
									return 0f;
								}
							}
							else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
							{
								SendParameters (actionList.parameters, false);
								if (runtimeRunMode == RunMode.SetParametersOnly)
								{
									isRunning = false;
									return 0f;
								}
							}

							if (runFromStart)
							{
								actionList.Interact (0, !isSkippable);
							}
							else
							{
								actionList.Interact (GetSkipIndex (actionList.actions), !isSkippable);
							}
						}
						else
						{
							LogWarning ("Could not find ActionList to run.");
							isRunning = false;
							return 0f;
						}
						break;

					case ListSource.AssetFile:
						if (invActionList != null && !invActionList.actions.Contains (this))
						{
							if (invActionList.useParameters)
							{
								SendParameters (invActionList.GetParameters (), true);
								if (runtimeRunMode == RunMode.SetParametersOnly)
								{
									isRunning = false;
									return 0f;
								}
							}

							if (!invActionList.canRunMultipleInstances)
							{
								KickStarter.actionListAssetManager.EndAssetList (invActionList);
							}

							if (runFromStart)
							{
								runtimeActionList = AdvGame.RunActionListAsset (invActionList, 0, !isSkippable);
							}
							else
							{
								runtimeActionList = AdvGame.RunActionListAsset (invActionList, GetSkipIndex (invActionList.actions), !isSkippable);
							}
						}
						else
						{
							LogWarning ("Could not find ActionList asset to run");
							isRunning = false;
							return 0f;
						}
						break;

					default:
						break;
				}

				if (!runInParallel || (runInParallel && willWait))
				{
					if (listSource == ListSource.InScene && actionList && actionList.triggerTime > 0f)
					{
						isAwaitingDelay = true;
						EventManager.OnEndActionList += OnEndActionList;
					}
					return defaultPauseTime;
				}
			}
			else
			{
				switch (listSource)
				{
					case ListSource.InScene:
						if (actionList)
						{
							if (isAwaitingDelay)
							{
								return defaultPauseTime;
							}
							else if (KickStarter.actionListManager.IsListRunning (actionList))
							{
								isAwaitingDelay = false;
								EventManager.OnEndActionList -= OnEndActionList;
								return defaultPauseTime;
							}
						}
						break;

					case ListSource.AssetFile:
						if (invActionList)
						{
							if (invActionList.canRunMultipleInstances)
							{
								if (runtimeActionList != null && KickStarter.actionListManager.IsListRunning (runtimeActionList))
								{
									return defaultPauseTime;
								}
							}
							else
							{
								if (KickStarter.actionListAssetManager.IsListRunning (invActionList))
								{
									return defaultPauseTime;
								}
							}
						}
						break;

					default:
						break;
				}
			}

			EventManager.OnEndActionList -= OnEndActionList;
			isAwaitingDelay = false;
			isRunning = false;
			return 0f;
		}


		private void OnEndActionList (ActionList _actionList, ActionListAsset _actionListAsset, bool isSkipping)
		{
			if (listSource == ListSource.InScene && actionList == _actionList && isAwaitingDelay)
			{
				isAwaitingDelay = false;
				EventManager.OnEndActionList -= OnEndActionList;
			}
		}


		public override void Skip ()
		{
			switch (listSource)
			{
				case ListSource.InScene:
					if (actionList)
					{
						if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
						{
							if (actionList.syncParamValues)
							{
								SendParameters (actionList.assetFile.GetParameters (), true);
							}
							else
							{
								SendParameters (actionList.parameters, false);
							}
							if (runtimeRunMode == RunMode.SetParametersOnly)
							{
								return;
							}
						}
						else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							SendParameters (actionList.parameters, false);
							if (runtimeRunMode == RunMode.SetParametersOnly)
							{
								return;
							}
						}

						if (runFromStart)
						{
							actionList.Skip ();
						}
						else
						{
							actionList.Skip (GetSkipIndex (actionList.actions));
						}
					}
					break;

				case ListSource.AssetFile:
					if (invActionList)
					{
						if (invActionList.useParameters)
						{
							SendParameters (invActionList.GetParameters (), true);
							if (runtimeRunMode == RunMode.SetParametersOnly)
							{
								return;
							}
						}

						if (runtimeActionList != null && !invActionList.IsSkippable () && invActionList.canRunMultipleInstances)
						{
							KickStarter.actionListAssetManager.EndAssetList (runtimeActionList);
						}

						if (runFromStart)
						{
							AdvGame.SkipActionListAsset (invActionList);
						}
						else
						{
							AdvGame.SkipActionListAsset (invActionList, GetSkipIndex (invActionList.actions));
						}
					}
					break;

				default:
					break;
			}
		}


		protected int GetSkipIndex (List<Action> _actions)
		{
			int skip = jumpToAction;
			if (jumpToActionActual != null && _actions.IndexOf (jumpToActionActual) > 0)
			{
				skip = _actions.IndexOf (jumpToActionActual);
			}
			return skip;
		}


		protected void SendParameters (List<ActionParameter> externalParameters, bool sendingToAsset)
		{
			if (runtimeRunMode == RunMode.RunOnly)
			{
				return;
			}
			SyncLists (externalParameters, localParameters);
			SetParametersBase.BulkAssignParameterValues (externalParameters, localParameters, sendingToAsset, isAssetFile);
		}


		#if UNITY_EDITOR
				
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			listSource = (ListSource) EditorGUILayout.EnumPopup ("Source:", listSource);
			if (listSource == ListSource.InScene)
			{
				parameterID = Action.ChooseParameterGUI ("ActionList:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					localParameters.Clear ();
					constantID = 0;
					actionList = null;

					if (setParameters)
					{
						EditorGUILayout.HelpBox ("If the ActionList has parameters, they will be set here - unset the parameter to edit them.", MessageType.Info);
					}
				}
				else
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					constantID = FieldToID <ActionList> (actionList, constantID);
					actionList = IDToField <ActionList> (actionList, constantID, true);

					if (actionList != null)
					{
						if (actionList.actions.Contains (this))
						{
							EditorGUILayout.HelpBox ("This Action cannot be used to run the ActionList it is in - use the Skip option below instead.", MessageType.Warning);
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.NumParameters > 0)
						{
							SetParametersGUI (actionList.assetFile.DefaultParameters, parameters);
							if (runMode == RunMode.SetParametersOnly)
							{
								return;
							}
						}
						else if (actionList.source == ActionListSource.InScene && actionList.NumParameters > 0)
						{
							SetParametersGUI (actionList.parameters, parameters);
							if (runMode == RunMode.SetParametersOnly)
							{
								return;
							}
						}
					}
				}


				runFromStart = EditorGUILayout.Toggle ("Run from start?", runFromStart);

				if (!runFromStart)
				{
					jumpToActionParameterID = Action.ChooseParameterGUI ("Action # to skip to:", parameters, jumpToActionParameterID, ParameterType.Integer);
					if (jumpToActionParameterID == -1 && actionList != null && actionList.actions.Count > 1)
					{
						JumpToActionGUI (actionList.actions);
					}
				}
			}
			else if (listSource == ListSource.AssetFile)
			{
				assetParameterID = Action.ChooseParameterGUI ("ActionList asset:", parameters, assetParameterID, ParameterType.UnityObject);
				if (assetParameterID < 0)
				{
					invActionList = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", invActionList, typeof (ActionListAsset), true);
				}

				if (assetParameterID >= 0)
				{
					EditorGUILayout.LabelField ("Placeholder asset: " + ((invActionList != null) ? invActionList.name : "(None set)"), EditorStyles.whiteLabel);
				}

				if (invActionList != null)
				{
					if (assetParameterID < 0 && invActionList.actions.Contains (this))
					{
						EditorGUILayout.HelpBox ("This Action cannot be used to run the ActionList it is in - use the Skip option below instead.", MessageType.Warning);
					}
					else if (invActionList.NumParameters > 0)
					{
						SetParametersGUI (invActionList.DefaultParameters, parameters);
						if (runMode == RunMode.SetParametersOnly)
						{
							return;
						}
					}
				}
				else if (assetParameterID >= 0 && setParameters)
				{
					EditorGUILayout.HelpBox ("If the ActionList asset has parameters, they will be set here - unset the parameter to edit them.", MessageType.Info);
				}

				runFromStart = EditorGUILayout.Toggle ("Run from start?", runFromStart);
				
				if (!runFromStart && invActionList != null && invActionList.actions.Count > 1)
				{
					JumpToActionGUI (invActionList.actions);
				}
			}

			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
		}


		private void JumpToActionGUI (List<Action> actions)
		{
			int tempSkipAction = jumpToAction;
			List<string> labelList = new List<string>();
			
			if (jumpToActionActual != null)
			{
				bool found = false;
				
				for (int i = 0; i < actions.Count; i++)
				{
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));

					if (jumpToActionActual == actions [i])
					{
						jumpToAction = i;
						found = true;
					}
				}

				if (!found)
				{
					jumpToAction = tempSkipAction;
				}
			}
			
			if (jumpToAction < 0)
			{
				jumpToAction = 0;
			}
			
			if (jumpToAction >= actions.Count)
			{
				jumpToAction = actions.Count - 1;
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
			tempSkipAction = EditorGUILayout.Popup (jumpToAction, labelList.ToArray());
			jumpToAction = tempSkipAction;
			EditorGUILayout.EndHorizontal();
			jumpToActionActual = actions [jumpToAction];
		}


		public static int ShowVarSelectorGUI (string label, List<GVar> vars, int ID, string tooltip = "")
		{
			int variableNumber = -1;
			
			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (GVar _var in vars)
			{
				labelList.Add (_var.label);
			}
			
			variableNumber = GetVarNumber (vars, ID) + 1;
			variableNumber = CustomGUILayout.Popup (label, variableNumber, labelList.ToArray(), string.Empty, tooltip) - 1;

			if (variableNumber >= 0)
			{
				return vars[variableNumber].id;
			}

			return -1;
		}


		public static int ShowInvItemSelectorGUI (string label, List<InvItem> items, int ID, string tooltip = "")
		{
			int invNumber = -1;
			
			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (InvItem _item in items)
			{
				labelList.Add (_item.label);
			}
			
			invNumber = GetInvNumber (items, ID) + 1;
			invNumber = CustomGUILayout.Popup (label, invNumber, labelList.ToArray(), string.Empty, tooltip) - 1;

			if (invNumber >= 0)
			{
				return items[invNumber].id;
			}
			return -1;
		}


		public static int ShowDocumentSelectorGUI (string label, List<Document> documents, int ID, string tooltip = "")
		{
			int docNumber = -1;
			
			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (Document document in documents)
			{
				labelList.Add (document.Title);
			}
			
			docNumber = GetDocNumber (documents, ID) + 1;
			docNumber = CustomGUILayout.Popup (label, docNumber, labelList.ToArray(), string.Empty, tooltip) - 1;

			if (docNumber >= 0)
			{
				return documents[docNumber].ID;
			}
			return -1;
		}


		private static int GetVarNumber (List<GVar> vars, int ID)
		{
			int i = 0;
			foreach (GVar _var in vars)
			{
				if (_var.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		private static int GetInvNumber (List<InvItem> items, int ID)
		{
			int i = 0;
			foreach (InvItem _item in items)
			{
				if (_item.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		private static int GetDocNumber (List<Document> documents, int ID)
		{
			int i = 0;
			foreach (Document document in documents)
			{
				if (document.ID == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		private void SetParametersGUI (List<ActionParameter> externalParameters, List<ActionParameter> ownParameters = null)
		{
			runMode = (RunMode) EditorGUILayout.EnumPopup ("Run mode:", runMode);
			if (runMode == RunMode.RunOnly)
			{
				return;
			}

			SetParametersBase.GUIData guiData = SetParametersBase.SetParametersGUI (externalParameters, isAssetFile, new SetParametersBase.GUIData (localParameters, parameterIDs), ownParameters);
			localParameters = guiData.fromParameters;
			parameterIDs = guiData.parameterIDs;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID <ActionList> (actionList, constantID, parameterID);
		}


		public override string SetLabel ()
		{
			if (listSource == ListSource.InScene && actionList != null)
			{
				return actionList.name;
			}
			else if (listSource == ListSource.AssetFile && invActionList != null)
			{
				return invActionList.name;
			}
			return string.Empty;
		}


		public override int GetNumVariableReferences (VariableLocation location, int varID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (listSource == ListSource.InScene && actionList != null)
			{
				if (actionList.source == ActionListSource.InScene && actionList.useParameters)
				{
					SyncLists (actionList.parameters, localParameters);
				}
				else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
				{
					SyncLists (actionList.assetFile.DefaultParameters, localParameters);
				}
			}
			else if (listSource == ListSource.AssetFile && invActionList != null && invActionList.useParameters)
			{
				SyncLists (invActionList.DefaultParameters, localParameters);
			}

			foreach (ActionParameter localParameter in localParameters)
			{
				if (localParameter != null && localParameter.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && varID == localParameter.intValue)
				{
					thisCount ++;
				}
				else if (localParameter != null && localParameter.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && varID == localParameter.intValue)
				{
					thisCount ++;
				}
				else if (localParameter != null && localParameter.parameterType == ParameterType.ComponentVariable && location == VariableLocation.Component && varID == localParameter.intValue)
				{
					if (_variables == localParameter.variables)
					{
						thisCount ++;
					}
					else if (_variablesConstantID != 0 && _variablesConstantID != localParameter.constantID)
					{
						thisCount ++;
					}
				}
			}

			thisCount += base.GetNumVariableReferences (location, varID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public override int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables _variables = null, int _variablesConstantID = 0)
		{
			int thisCount = 0;

			if (listSource == ListSource.InScene && actionList != null)
			{
				if (actionList.source == ActionListSource.InScene && actionList.useParameters)
				{
					SyncLists (actionList.parameters, localParameters);
				}
				else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
				{
					SyncLists (actionList.assetFile.DefaultParameters, localParameters);
				}
			}
			else if (listSource == ListSource.AssetFile && invActionList != null && invActionList.useParameters)
			{
				SyncLists (invActionList.DefaultParameters, localParameters);
			}

			foreach (ActionParameter localParameter in localParameters)
			{
				if (localParameter != null && localParameter.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && oldVarID == localParameter.intValue)
				{
					localParameter.intValue = newVarID;
					thisCount++;
				}
				else if (localParameter != null && localParameter.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && oldVarID == localParameter.intValue)
				{
					localParameter.intValue = newVarID;
					thisCount++;
				}
				else if (localParameter != null && localParameter.parameterType == ParameterType.ComponentVariable && location == VariableLocation.Component && oldVarID == localParameter.intValue)
				{
					if (_variables == localParameter.variables)
					{
						localParameter.intValue = newVarID;
						thisCount++;
					}
					else if (_variablesConstantID != 0 && _variablesConstantID != localParameter.constantID)
					{
						localParameter.intValue = newVarID;
						thisCount++;
					}
				}
			}

			thisCount += base.UpdateVariableReferences (location, oldVarID, newVarID, parameters, _variables, _variablesConstantID);
			return thisCount;
		}


		public int GetNumItemReferences (int _itemID, List<ActionParameter> parameters)
		{
			return GetParameterReferences (parameters, _itemID, ParameterType.InventoryItem);
		}


		public int UpdateItemReferences (int oldItemID, int newItemID, List<ActionParameter> parameters)
		{
			return GetParameterReferences (parameters, oldItemID, ParameterType.InventoryItem, true, newItemID);
		}


		public int GetNumDocumentReferences (int _docID, List<ActionParameter> parameters)
		{
			return GetParameterReferences (parameters, _docID, ParameterType.Document);
		}


		public int UpdateDocumentReferences (int oldDocumentID, int newDocumentID, List<ActionParameter> parameters)
		{
			return GetParameterReferences (parameters, oldDocumentID, ParameterType.Document, true, newDocumentID);
		}


		private int GetParameterReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType, bool updateID = false, int _newID = 0)
		{
			int thisCount = 0;

			if (listSource == ListSource.InScene && actionList != null)
			{
				if (actionList.source == ActionListSource.InScene && actionList.useParameters)
				{
					SyncLists (actionList.parameters, localParameters);
				}
				else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
				{
					SyncLists (actionList.assetFile.DefaultParameters, localParameters);
				}
			}
			else if (listSource == ListSource.AssetFile && invActionList != null && invActionList.useParameters)
			{
				SyncLists (invActionList.DefaultParameters, localParameters);
			}

			foreach (ActionParameter localParameter in localParameters)
			{
				if (localParameter != null && localParameter.parameterType == _paramType && _ID == localParameter.intValue)
				{
					if (updateID)
					{
						localParameter.intValue = _newID;
					}
					thisCount ++;
				}
			}

			return thisCount;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (listSource == ListSource.InScene && parameterID < 0)
			{
				if (actionList && actionList.gameObject == gameObject) return true;
				if (constantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}


		public override bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (listSource == ListSource.AssetFile && invActionList == actionListAsset)
				return true;
			return base.ReferencesAsset (actionListAsset);
		}

		#endif


		[SerializeField] private bool hasUpgradedAgain = false;
		protected void SyncLists (List<ActionParameter> externalParameters, List<ActionParameter> oldLocalParameters)
		{
			if (!hasUpgradedAgain)
			{
				// If parameters were deleted before upgrading, there may be a mismatch - so first ensure that order internal IDs match external

				if (oldLocalParameters != null && externalParameters != null && oldLocalParameters.Count != externalParameters.Count && oldLocalParameters.Count > 0)
				{
					LogWarning ("Parameter mismatch detected - please check the 'ActionList: Run' Action for its parameter values.");
				}

				for (int i=0; i<externalParameters.Count; i++)
				{
					if (i < oldLocalParameters.Count)
					{
						oldLocalParameters[i].ID = externalParameters[i].ID;
					}
				}

				hasUpgradedAgain = true;
			}

			// Now that all parameter IDs match to begin with, we can rebuild the internal record based on the external parameters
			SetParametersBase.GUIData newGUIData = SetParametersBase.SyncLists (externalParameters, new SetParametersBase.GUIData (oldLocalParameters, parameterIDs));
			localParameters = newGUIData.fromParameters;
			parameterIDs = newGUIData.parameterIDs;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Run' Action</summary>
		 * <param name = "actionList">The ActionList to run</param>
		 * <param name = "startingActionIndex">The index number of the Action to start from</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionRunActionList CreateNew (ActionList actionList, int startingActionIndex = 0)
		{
			ActionRunActionList newAction = CreateNew<ActionRunActionList> ();
			newAction.listSource = ListSource.InScene;
			newAction.actionList = actionList;
			newAction.TryAssignConstantID (newAction.actionList, ref newAction.constantID);
			newAction.runFromStart = (startingActionIndex <= 0);
			newAction.jumpToAction = startingActionIndex;
			newAction.runInParallel = true;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Run' Action</summary>
		 * <param name = "actionListAsset">The ActionList asset to run</param>
		 * <param name = "startingActionIndex">The index number of the Action to start from</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionRunActionList CreateNew (ActionListAsset actionListAsset, int startingActionIndex = 0)
		{
			ActionRunActionList newAction = CreateNew<ActionRunActionList> ();
			newAction.listSource = ListSource.AssetFile;
			newAction.invActionList = actionListAsset;
			newAction.runFromStart = (startingActionIndex <= 0);
			newAction.jumpToAction = startingActionIndex;
			newAction.runInParallel = true;
			return newAction;
		}

	}

}