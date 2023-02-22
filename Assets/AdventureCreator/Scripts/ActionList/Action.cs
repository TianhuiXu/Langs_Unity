/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Action.cs"
 * 
 *	This is the base class from which all Actions derive.
 *	We need blank functions Run, ShowGUI and SetLabel,
 *	which will be over-ridden by the subclasses.
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
	 * The base class from which all Actions derive.
	 * An Action is a ScriptableObject that performs a specific command, like pausing the game or moving a character.
	 * They are chained together in ActionLists to form cutscenes, gameplay logic etc.
	 */
	[System.Serializable]
	#if AC_ActionListPrefabs
	abstract public class Action : iActionListAssetReferencer, IVariableReferencerAction
	#else
	abstract public class Action : ScriptableObject, iActionListAssetReferencer, IVariableReferencerAction
	#endif
	{

		/** A unique identifier */
		public int id;

		protected ActionCategory category = ActionCategory.Custom;
		protected string title = "Untitled";
		protected string description;

		/** If True, the Action is expanded in the Editor */
		public bool isDisplayed;
		/** If True, then the comment text will be shown in the Action's UI in the ActionList Editor window */
		public bool showComment;
		/** A user-defined comment about the Action's purpose */
		public string comment;

		/** If True, the ActionList will wait until the Action has finished running before continuing */
		public bool willWait;

		/** If True, then the Action is running */
		[System.NonSerialized] public bool isRunning;

		private int lastRunOutput = -10;

		/** Deprecated */
		[SerializeField] private ResultAction endAction = ResultAction.Continue;
		/** Deprecated */
		[SerializeField] private int skipAction = -1;
		/** Deprecated */
		[SerializeField] private AC.Action skipActionActual = null;
		/** Deprecated */
		[SerializeField] private Cutscene linkedCutscene = null;
		/** Deprecated */
		[SerializeField] private ActionListAsset linkedAsset = null;

		/** A List of the various outcomes that running the Action can have */
		public List<ActionEnd> endings = new List<ActionEnd> ();

		/** If True, the Action is enabled and can be run */
		public bool isEnabled = true;
		/** If True, the Action is stored within an ActionListAsset file */
		public bool isAssetFile = false;

		/** If True, the Action has been marked for modification in ActionListEditorWindow */
		[System.NonSerialized] public bool isMarked = false;
		/** If True, the Editor will pause when this Action is run in-game */
		public bool isBreakPoint = false;

		#if UNITY_EDITOR
		[SerializeField] private Rect nodeRect = new Rect (0,0,300,60);
		public Color overrideColor = Color.white;
		public bool showOutputSockets = true;
		/** The Action's parent ActionList, if in the scene.  This is not 100% reliable and should not be used in custom scripts */
		public ActionList parentActionListInEditor = null;

		#if UNITY_EDITOR_OSX
		public const int skipSocketSeparation = 44;
		public const int socketSeparation = 26;
		#else
		public const int skipSocketSeparation = 48;
		public const int socketSeparation = 28;
		#endif

		#endif


		/**
		 * The default Constructor.
		 */
		public Action ()
		{
			this.isDisplayed = true;
		}


		/** The category (ActionList, Camera, Character, Container, Dialogue, Engine, Hotspot, Input, Inventory, Menu, Moveable, Object, Player, Save, Sound, ThirdParty, Variable, Custom) */
		public virtual ActionCategory Category
		{
			get
			{
				return category;
			}
		}


		/** The Action's title */
		public virtual string Title
		{
			get
			{
				return title;
			}
		}


		/** A brief description about what the Action does */
		public virtual string Description
		{
			get
			{
				return description;
			}
		}


		public virtual int NumSockets
		{
			get
			{
				return 1;
			}
		}


		/** Used to upgrade Action data to the latest AC release */
		public virtual void Upgrade ()
		{
			if (skipAction != -99 && endings.Count == 0)
			{
				ActionEnd actionEnd = new ActionEnd ();
				actionEnd.resultAction = endAction;
				actionEnd.skipAction = skipAction;
				actionEnd.skipActionActual = skipActionActual;
				actionEnd.linkedCutscene = linkedCutscene;
				actionEnd.linkedAsset = linkedAsset;
				endings.Add (actionEnd);
				skipAction = -99;
			}
		}


		/**
		 * <summary>Runs the Action.</summary>
		 * <returns>The time, in seconds, to wait before ActionList calls this function again.  If 0, then the Action will not be re-run.  If >0, and isRunning = True, then the Action will be re-run isRunning = False</returns>
		 */
		public virtual float Run ()
		{
			return 0f;
		}


		/** Runs the Action instantaneously. */
		public virtual void Skip ()
		{
			Run ();
		}


		public virtual bool RunAllOutputs
		{
			get
			{
				return false;
			}
		}


		public float defaultPauseTime
		{
			get
			{
				return -1f;
			}
		}
		

		/**
		 * <summary>Gets the index of the output socket to use after the Action has run.</summary>
		 * <returns>The index of the output socket to run. If the index is negative or invalid it will result in the ActionList ending</returns>
		 */
		public virtual int GetNextOutputIndex ()
		{
			if (endings.Count > 0)
			{
				return 0;
			}
			return -1;
		}


		/**
		 * <summary>Prints the Action's comment, if applicable, to the Console.</summary>
		 * <param name = "actionList">The associated ActionList of which this Action is a part of</param>
		 * <param name = "actionListAsset">The associated ActionListAsset of which this Action is a part of</param>
		 */
		public void PrintComment (ActionList actionList, ActionListAsset actionListAsset = null)
		{
			if (actionList == null) return;

			if (!string.IsNullOrEmpty (comment))
			{
				string log = AdvGame.ConvertTokens (comment, 0, null, actionList.parameters);
				log += "\n" + "(From Action '(" + actionList.actions.IndexOf (this) + ") " + KickStarter.actionsManager.GetActionTypeLabel (this);

				if (actionListAsset)
				{
					log += "' in ActionList asset '" + actionListAsset.name + "')";
					ACDebug.Log (log, actionListAsset);
				}
				else
				{
					log += "' in ActionList '" + actionList.gameObject.name + "')";
					ACDebug.Log (log, actionList);
				}
			}
		}


		/**
		 * <summary>Update the Action's output sockets</summary>
		 * <param name = "actionEnds">A data container for the output sockets</param>
		 */
		public void SetOutputs (ActionEnd[] actionEnds)
		{
			endings = new List<ActionEnd>();
			foreach (ActionEnd actionEnd in actionEnds)
			{
				endings.Add (new ActionEnd (actionEnd));
			}

			if (endings.Count > NumSockets)
			{
				LogWarning ("Ending mismatch - setting " + actionEnds.Length + " outputs for Action, but only " + NumSockets + " are supported");
			}
		}


		#if UNITY_EDITOR

		/**
		 * <summary>Shows the Action's GUI when its parent ActionList / ActionListAsset uses parameters.</summary>
		 * <param name = "parameters">A List of parameters available in the parent ActionList / ActionListAsset</param>
		 */
		public virtual void ShowGUI (List<ActionParameter> parameters)
		{
			ShowGUI ();
		}


		/**
		 * Shows the Action's GUI.
		 */
		public virtual void ShowGUI ()
		{ }


		protected void AfterRunningOption ()
		{}
		

		public void SkipActionGUI (List<Action> actions, bool showGUI)
		{
			Upgrade ();
			
			int numSockets = Mathf.Max (NumSockets, 0);

			if (numSockets == 0)
			{
				endings.Clear ();
			}
			else if (numSockets < endings.Count)
			{
				endings.RemoveRange (Mathf.Max (1, numSockets), endings.Count - Mathf.Max (1, numSockets));
			}
			else if (numSockets > endings.Count)
			{
				if (numSockets > endings.Capacity)
				{
					endings.Capacity = numSockets;
				}
				for (int i = endings.Count; i < numSockets; i++)
				{
					ActionEnd newEnd = new ActionEnd ();
					newEnd.resultAction = ResultAction.Stop;
					endings.Add (newEnd);
				}
			}
			
			for (int i=0; i<numSockets; i++)
			{
				if (showGUI)
				{
					EditorGUILayout.Space ();
					endings[i].resultAction = (ResultAction) EditorGUILayout.EnumPopup (GetSocketLabel (i), (ResultAction) endings[i].resultAction);
				}

				if (endings[i].resultAction == ResultAction.RunCutscene && showGUI)
				{
					if (isAssetFile)
					{
						endings[i].linkedAsset = ActionListAssetMenu.AssetGUI ("ActionList to run:", endings[i].linkedAsset);
					}
					else
					{
						endings[i].linkedCutscene = ActionListAssetMenu.CutsceneGUI ("Cutscene to run:", endings[i].linkedCutscene);
					}
				}
				else if (endings[i].resultAction == ResultAction.Skip)
				{
					SkipActionGUI (endings[i], actions, showGUI);
				}
			}
		}


		protected void SkipActionGUI (ActionEnd ending, List<Action> actions, bool showGUI)
		{
			if (ending.skipAction == -1)
			{
				// Set default
				int i = actions.IndexOf (this);
				if (actions.Count > i + 1)
				{
					ending.skipAction = i + 1;
				}
				else
				{
					ending.skipAction = i;
				}
			}

			int tempSkipAction = ending.skipAction;
			List<string> labelList = new List<string> ();

			if (ending.skipActionActual != null)
			{
				bool found = false;

				for (int i = 0; i < actions.Count; i++)
				{
					labelList.Add ("(" + i.ToString () + ") " + ((KickStarter.actionsManager != null) ? KickStarter.actionsManager.GetActionTypeLabel (actions[i]) : string.Empty));

					if (ending.skipActionActual == actions[i])
					{
						ending.skipAction = i;
						found = true;
					}
				}

				if (!found)
				{
					ending.skipAction = tempSkipAction;
				}
			}

			if (ending.skipAction >= actions.Count)
			{
				ending.skipAction = actions.Count - 1;
			}

			if (showGUI)
			{
				if (actions.Count > 1)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("  Action to skip to:", GUILayout.Width (155f));
					tempSkipAction = EditorGUILayout.Popup (ending.skipAction, labelList.ToArray ());
					ending.skipAction = tempSkipAction;
					EditorGUILayout.EndHorizontal ();
				}
				else
				{
					EditorGUILayout.HelpBox ("Cannot skip action - no further Actions available", MessageType.Warning);
					return;
				}
			}

			ending.skipActionActual = actions[ending.skipAction];
		}


		/**
		 * <summary>Called when an ActionList has been converted from a scene-based object to an asset file.
		 * Within it, AssignConstantID should be called for each of the Action's Constant ID numbers, which will assign a new number if one does not already exist, based on the referenced scene object.</summary>
		 * <param name = "saveScriptsToo">If True, then the Action shall attempt to add the appropriate 'Remember' script to reference GameObjects as well.</param>
		 * <param name = "fromAssetFile">If True, then the Action is placed in an ActionListAsset file</param>
		 */
		public virtual void AssignConstantIDs (bool saveScriptsToo = false, bool fromAssetFile = false)
		{ }


		/**
		 * <summary>Gets a string that is shown after the Action's title in the Editor to help the user understand what it does.</summary>
		 * <returns>A string that is shown after the Action's title in the Editor to help the user understand what it does.</returns>
		 */
		public virtual string SetLabel ()
		{
			return (string.Empty);
		}


		public void DrawOutWires (List<Action> actions, int i, int offset, Vector2 scrollPosition)
		{
			if (endings.Count == 1)
			{
				if (endings[0].resultAction == ResultAction.Continue)
				{
					if (actions.Count > i + 1 && actions[i + 1] != null)
					{
						AdvGame.DrawNodeCurve (new Rect (NodeRect.position - scrollPosition, NodeRect.size),
											   new Rect (actions[i + 1].NodeRect.position - scrollPosition, actions[i + 1].NodeRect.size),
											   new Color (0.3f, 0.3f, 1f, 1f), 10, false, isDisplayed);
					}
				}
				else if (endings[0].resultAction == ResultAction.Skip && showOutputSockets)
				{
					if (endings[0].skipActionActual != null && actions.Contains (endings[0].skipActionActual))
					{
						AdvGame.DrawNodeCurve (new Rect (NodeRect.position - scrollPosition, NodeRect.size),
											   new Rect (endings[0].skipActionActual.NodeRect.position - scrollPosition, endings[0].skipActionActual.NodeRect.size),
											   new Color (0.3f, 0.3f, 1f, 1f), 10, false, isDisplayed);
					}
				}
				return;
			}

			int totalHeight = 7;
			for (int j = endings.Count - 1; j >= 0; j--)
			{
				ActionEnd ending = endings[j];

				float fac = (float) (endings.Count - endings.IndexOf (ending)) / endings.Count;
				Color wireColor = new Color (1f - fac, fac * 0.7f, 0.1f);

				if (ending.resultAction == ResultAction.Continue)
				{
					if (actions.Count > i + 1 && actions[i + 1] != null)
					{
						AdvGame.DrawNodeCurve (new Rect (NodeRect.position - scrollPosition, NodeRect.size),
											   new Rect (actions[i + 1].NodeRect.position - scrollPosition, actions[i + 1].NodeRect.size),
											   wireColor, totalHeight, true, isDisplayed);
					}
				}
				else if (ending.resultAction == ResultAction.Skip && showOutputSockets)
				{
					if (ending.skipActionActual != null && actions.Contains (ending.skipActionActual))
					{
						AdvGame.DrawNodeCurve (new Rect (NodeRect.position - scrollPosition, NodeRect.size),
											   new Rect (ending.skipActionActual.NodeRect.position - scrollPosition, ending.skipActionActual.NodeRect.size),
											   wireColor, totalHeight, true, isDisplayed);
					}
				}

				if (ending.resultAction == ResultAction.Skip)
				{
					totalHeight += skipSocketSeparation;
				}
				else
				{
					totalHeight += socketSeparation;
				}
			}
		}


		public static int ChooseParameterGUI (string label, List<ActionParameter> _parameters, int _parameterID, ParameterType _expectedType, int excludeParameterID = -1, string tooltip = "")
		{
			if (_parameters == null || _parameters.Count == 0)
			{
				return -1;
			}
			
			// Don't show list if no parameters of the correct type are present
			bool found = false;
			foreach (ActionParameter _parameter in _parameters)
			{
				if (excludeParameterID < 0 || excludeParameterID != _parameter.ID)
				{
					if (_parameter.parameterType == _expectedType ||
						(_expectedType == ParameterType.GameObject && _parameter.parameterType == ParameterType.ComponentVariable))
					{
						found = true;
					}
				}
			}
			if (!found)
			{
				return -1;
			}
			
			int chosenNumber = 0;
			List<PopupSelectData> popupSelectDataList = new List<PopupSelectData>();
			for (int i=0; i<_parameters.Count; i++)
			{
				if (excludeParameterID < 0 || excludeParameterID != _parameters[i].ID)
				{
					if (_parameters[i].parameterType == _expectedType ||
						(_expectedType == ParameterType.GameObject && _parameters[i].parameterType == ParameterType.ComponentVariable))
					{
						PopupSelectData popupSelectData = new PopupSelectData (_parameters[i].ID, _parameters[i].ID + ": " + _parameters[i].label, i);
						popupSelectDataList.Add (popupSelectData);

						if (popupSelectData.ID == _parameterID)
						{
							chosenNumber = popupSelectDataList.Count;
						}
					}
				}
			}

			List<string> labelList = new List<string>();
			labelList.Add ("(No parameter)");
			foreach (PopupSelectData popupSelectData in popupSelectDataList)
			{
				labelList.Add (popupSelectData.label);
			}

			if (!string.IsNullOrEmpty (label))
			{
				chosenNumber = CustomGUILayout.Popup ("-> " + label, chosenNumber, labelList.ToArray (), string.Empty, tooltip) - 1;
			}
			else
			{
				chosenNumber = EditorGUILayout.Popup (chosenNumber, labelList.ToArray ()) - 1;
			}

			if (chosenNumber < 0)
			{
				return -1;
			}
			int rootIndex = popupSelectDataList[chosenNumber].rootIndex;
			return _parameters [rootIndex].ID;
		}


		public static int ChooseParameterGUI (string label, List<ActionParameter> _parameters, int _parameterID, ParameterType[] _expectedTypes, int excludeParameterID = -1, string tooltip = "")
		{
			if (_parameters == null || _parameters.Count == 0)
			{
				return -1;
			}
			
			// Don't show list if no parameters of the correct type are present
			bool found = false;
			foreach (ActionParameter _parameter in _parameters)
			{
				if (excludeParameterID < 0 || excludeParameterID != _parameter.ID)
				{
					foreach (ParameterType _expectedType in _expectedTypes)
					{
						if (_parameter.parameterType == _expectedType ||
							(_expectedType == ParameterType.GameObject && _parameter.parameterType == ParameterType.ComponentVariable))
						{
							found = true;
						}
					}
				}
			}
			if (!found)
			{
				return -1;
			}
			
			int chosenNumber = 0;
			List<PopupSelectData> popupSelectDataList = new List<PopupSelectData>();
			for (int i=0; i<_parameters.Count; i++)
			{
				if (excludeParameterID < 0 || excludeParameterID != _parameters[i].ID)
				{
					foreach (ParameterType _expectedType in _expectedTypes)
					{
						if (_parameters[i].parameterType == _expectedType ||
							(_expectedType == ParameterType.GameObject && _parameters[i].parameterType == ParameterType.ComponentVariable))
						{
							PopupSelectData popupSelectData = new PopupSelectData (_parameters[i].ID, _parameters[i].ID + ": " + _parameters[i].label, i);
							popupSelectDataList.Add (popupSelectData);

							if (popupSelectData.ID == _parameterID)
							{
								chosenNumber = popupSelectDataList.Count;
							}
						}
					}
				}
			}

			List<string> labelList = new List<string>();
			labelList.Add ("(No parameter)");
			foreach (PopupSelectData popupSelectData in popupSelectDataList)
			{
				labelList.Add (popupSelectData.label);
			}

			if (!string.IsNullOrEmpty (label))
			{
				chosenNumber = CustomGUILayout.Popup ("-> " + label, chosenNumber, labelList.ToArray (), string.Empty, tooltip) - 1;
			}
			else
			{
				chosenNumber = EditorGUILayout.Popup (chosenNumber, labelList.ToArray ()) - 1;
			}

			if (chosenNumber < 0)
			{
				return -1;
			}
			int rootIndex = popupSelectDataList[chosenNumber].rootIndex;
			return _parameters [rootIndex].ID;
		}


		public static int ChooseParameterGUI (List<ActionParameter> _parameters, int _parameterID)
		{
			if (_parameters == null || _parameters.Count == 0)
			{
				return -1;
			}

			int chosenNumber = 0;
			List<string> labelList = new List<string>();
			foreach (ActionParameter _parameter in _parameters)
			{
				labelList.Add (_parameter.ID + ": " + _parameter.label);
				if (_parameter.ID == _parameterID)
				{
					chosenNumber = _parameters.IndexOf (_parameter);
				}
			}
			
			chosenNumber = EditorGUILayout.Popup ("Parameter:", chosenNumber, labelList.ToArray ());
			if (chosenNumber < 0)
			{
				return -1;
			}
			return _parameters [chosenNumber].ID;
		}


		public int FieldToID <T> (T field, int _constantID, bool alwaysAssign = false) where T : Behaviour
		{
			if (field != null)
			{
				if (alwaysAssign || isAssetFile || (!isAssetFile && !ObjectIsInScene (field.gameObject)))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!ObjectIsInScene (field.gameObject) && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		public int FieldToID (Collider field, int _constantID)
		{
			if (field != null)
			{
				if (isAssetFile || (!isAssetFile && !ObjectIsInScene (field.gameObject)))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!ObjectIsInScene (field.gameObject) && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		public void AddSaveScript <T> (Behaviour field) where T : ConstantID
		{
			if (field != null)
			{
				if (field.gameObject.GetComponent <T>() == null)
				{
					T newComponent = UnityVersionHandler.AddConstantIDToGameObject <T> (field.gameObject);

					if (!(newComponent is ConstantID))
					{
						ACDebug.Log ("Added '" + newComponent.GetType ().ToString () + "' component to " + field.gameObject.name, field.gameObject);
					}

					#if !AC_ActionListPrefabs
					EditorUtility.SetDirty (this);
					#endif
				}
			}
		}


		public void AddSaveScript <T> (GameObject _gameObject) where T : ConstantID
		{
			if (_gameObject != null)
			{
				if (_gameObject.GetComponent <T>() == null)
				{
					T newComponent = UnityVersionHandler.AddConstantIDToGameObject <T> (_gameObject);

					ACDebug.Log ("Added '" + newComponent.GetType ().ToString () + "' component to " + _gameObject.name, _gameObject);

					#if !AC_ActionListPrefabs
					EditorUtility.SetDirty (this);
					#endif
				}
			}
		}


		protected void AssignConstantID <T> (T field, int _constantID, int _parameterID) where T : Behaviour
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID <T> (field, _constantID);
			}
		}


		protected void AssignConstantID (Collider field, int _constantID, int _parameterID)
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID (field, _constantID);
			}
		}


		protected void AssignConstantID (Transform field, int _constantID, int _parameterID)
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID (field, _constantID);
			}
		}


		protected void AssignConstantID (GameObject field, int _constantID, int _parameterID)
		{
			if (_parameterID >= 0)
			{
				_constantID = 0;
			}
			else
			{
				_constantID = FieldToID (field, _constantID);
			}
		}
		
		
		public T IDToField <T> (T field, int _constantID, bool moreInfo) where T : Behaviour
		{
			if (isAssetFile || (!isAssetFile && (field == null || !ObjectIsInScene (field.gameObject))))
			{
				T newField = field;
				if (_constantID != 0)
				{
					newField = ConstantID.GetComponent <T> (_constantID);
					if (field && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newField && !Application.isPlaying)
					{
						field = newField;
					}

					CustomGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (!Application.isPlaying && GUILayout.Button ("Locate", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					CustomGUILayout.EndVertical ();
				}
			}
			return field;
		}


		public Collider IDToField (Collider field, int _constantID, bool moreInfo)
		{
			if (isAssetFile || (!isAssetFile && (field == null || !ObjectIsInScene (field.gameObject))))
			{
				Collider newField = field;
				if (_constantID != 0)
				{
					newField = ConstantID.GetComponent <Collider> (_constantID);
					if (field && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newField && !Application.isPlaying)
					{
						field = newField;
					}

					CustomGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (!Application.isPlaying && GUILayout.Button ("Locate", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					CustomGUILayout.EndVertical ();
				}
			}
			return field;
		}
		
		
		public int FieldToID (Transform field, int _constantID, bool alwaysAssign = false)
		{
			if (field != null)
			{
				if (alwaysAssign || isAssetFile || (!isAssetFile && !ObjectIsInScene (field.gameObject)))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!ObjectIsInScene (field.gameObject) && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}
		
		
		public Transform IDToField (Transform field, int _constantID, bool moreInfo)
		{
			if (isAssetFile || (!isAssetFile && (field == null || !ObjectIsInScene (field.gameObject))))
			{
				if (_constantID != 0)
				{
					ConstantID newID = ConstantID.GetComponent <ConstantID> (_constantID);
					if (field && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newID && !Application.isPlaying)
					{
						field = newID.transform;
					}

					CustomGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (!Application.isPlaying && GUILayout.Button ("Locate", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					CustomGUILayout.EndVertical ();
				}
			}
			return field;
		}


		public int FieldToID (GameObject field, int _constantID)
		{
			return FieldToID (field, _constantID, false);
		}

		
		public int FieldToID (GameObject field, int _constantID, bool alwaysAssign)
		{
			return FieldToID (field, _constantID, alwaysAssign, isAssetFile);
		}


		private static bool ObjectIsInScene (GameObject gameObject)
		{
			return gameObject != null && gameObject.activeInHierarchy && !UnityVersionHandler.IsPrefabEditing (gameObject);
		}


		public static int FieldToID (GameObject field, int _constantID, bool alwaysAssign, bool _isAssetFile)
		{
			if (field != null)
			{
				if (alwaysAssign || _isAssetFile || (!_isAssetFile && !ObjectIsInScene (field.gameObject)))
				{
					if (field.GetComponent <ConstantID>())
					{
						if (!ObjectIsInScene (field.gameObject) && field.GetComponent <ConstantID>().constantID == 0)
						{
							UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
						}
						_constantID = field.GetComponent <ConstantID>().constantID;
					}
					else if (field.GetComponent <Player>() == null)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (field.gameObject);
					}
					return _constantID;
				}
				if (!Application.isPlaying)
				{
					return 0;
				}
			}
			return _constantID;
		}


		public GameObject IDToField (GameObject field, int _constantID, bool moreInfo)
		{
			return IDToField (field, _constantID, moreInfo, false);
		}
		
		
		public GameObject IDToField (GameObject field, int _constantID, bool moreInfo, bool alwaysShow)
		{
			return IDToField (field, _constantID, moreInfo, alwaysShow, isAssetFile);
		}


		public static GameObject IDToField (GameObject field, int _constantID, bool moreInfo, bool alwaysShow, bool _isAssetFile)
		{
			if (alwaysShow || _isAssetFile || (!_isAssetFile && (field == null || !ObjectIsInScene (field))))
			{
				if (_constantID != 0)
				{
					ConstantID newID = ConstantID.GetComponent <ConstantID> (_constantID);
					if (field && field.GetComponent <ConstantID>() != null && field.GetComponent <ConstantID>().constantID == _constantID)
					{}
					else if (newID && !Application.isPlaying)
					{
						field = newID.gameObject;
					}

					CustomGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Recorded ConstantID: " + _constantID.ToString (), EditorStyles.miniLabel);
					if (field == null)
					{
						if (!Application.isPlaying && GUILayout.Button ("Locate", EditorStyles.miniButton))
						{
							AdvGame.FindObjectWithConstantID (_constantID);
						}
					}
					EditorGUILayout.EndHorizontal ();
					
					if (field == null && moreInfo)
					{
						EditorGUILayout.HelpBox ("Further controls cannot display because the referenced object cannot be found.", MessageType.Warning);
					}
					CustomGUILayout.EndVertical ();
				}
			}
			return field;
		}


		/**
		 * <summary>Converts the Action's references from a given local variable to a given global variable</summary>
		 * <param name = "oldLocalID">The ID number of the old local variable</param>
		 * <param name = "newGlobalID">The ID number of the new global variable</param>
		 * <returns>True if the Action was amended</returns>
		 */
		public virtual bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			string newComment = AdvGame.ConvertLocalVariableTokenToGlobal (comment, oldLocalID, newGlobalID);
			bool wasAmended = (comment != newComment);
			comment = newComment;
			return wasAmended;
		}


		/**
		 * <summary>Converts the Action's references from a given global variable to a given local variable</summary>
		 * <param name = "oldGlobalID">The ID number of the old global variable</param>
		 * <param name = "newLocalID">The ID number of the new local variable</param>
		 * <param name = "isCorrectScene">If True, the local variable is in the same scene as this ActionList.  Otherwise, no change will made, but the return value will be the same</param>
		 * <returns>True if the Action is affected</returns>
		 */
		public virtual bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			string newComment = AdvGame.ConvertGlobalVariableTokenToLocal (comment, oldGlobalID, newLocalID);
			if (comment != newComment)
			{
				if (isCorrectScene)
				{
					comment = newComment;
				}
				return true;
			}
			return false;
		}


		/**
		 * <summary>Gets the number of references the Action makes to a variable</summary>
		 * <param name = "location">The variable's location (Global, Local)</param>
		 * <param name = "varID">The variable's ID number</param>
		 * <param name = "parameters">The List of ActionParameters associated with the ActionList that contains the Action</param>
		 * <param name = "variables">The Variables component, if location = VariableLocation.Component</param>
		 * <returns>The number of references the Action makes to the variable</returns>
		 */
		public virtual int GetNumVariableReferences (VariableLocation location, int varID, List<ActionParameter> parameters, Variables variables = null, int variablesConstantID = 0)
		{
			string tokenText = AdvGame.GetVariableTokenText (location, varID, variablesConstantID);

			if (!string.IsNullOrEmpty (tokenText) && !string.IsNullOrEmpty (comment) && comment.Contains (tokenText))
			{
				return 1;
			}
			return 0;
		}


		/**
		 * <summary>Updated references the Action makes to a variable</summary>
		 * <param name = "location">The variable's location (Global, Local)</param>
		 * <param name = "oldVarID">The variable's original ID number</param>
		 * <param name = "newVarID">The variable's new ID number</param>
		 * <param name = "parameters">The List of ActionParameters associated with the ActionList that contains the Action</param>
		 * <param name = "variables">The Variables component, if location = VariableLocation.Component</param>
		 * <returns>The number of references the Action makes to the variable</returns>
		 */
		public virtual int UpdateVariableReferences (VariableLocation location, int oldVarID, int newVarID, List<ActionParameter> parameters, Variables variables = null, int variablesConstantID = 0)
		{
			string oldTokenText = AdvGame.GetVariableTokenText (location, oldVarID, variablesConstantID);
			if (!string.IsNullOrEmpty (oldTokenText) && !string.IsNullOrEmpty (comment) && comment.Contains (oldTokenText))
			{
				string newTokenText = AdvGame.GetVariableTokenText (location, newVarID, variablesConstantID);
				comment = comment.Replace (oldTokenText, newTokenText);
				return 1;
			}
			return 0;
		}


		/**
		 * <summary>Checks if the Action makes reference to a particular GameObject</summary>
		 * <param name = "gameObject">The GameObject to check for</param>
		 * <param name = "id">The GameObject's associated ConstantID value</param>
		 * <returns>True if the Action references the GameObject</param>
		 */
		public virtual bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			Upgrade ();

			if (!isAssetFile)
			{
				foreach (ActionEnd ending in endings)
				{
					if (ending.resultAction == ResultAction.RunCutscene)
					{
						if (ending.linkedCutscene && ending.linkedCutscene.gameObject == gameObject) return true;
					}
				}
			}
			return false;
		}


		public virtual bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			Upgrade ();

			if (isAssetFile)
			{
				foreach (ActionEnd ending in endings)
				{
					if (ending.resultAction == ResultAction.RunCutscene)
					{
						if (ending.linkedAsset == actionListAsset) return true;
					}
				}
			}
			return false;
		}


		/**
		 * <summary>Checks if the Action makes reference to a particular ActionList asset</summary>
		 * <param name = "playerID">The Player to check for, if player-switching is enabled</param>
		 * <returns>True if the Action references the Player</param>
		 */
		public virtual bool ReferencesPlayer (int playerID = -1)
		{
			return false;
		}


		protected int ChoosePlayerGUI (int _playerID, bool includeActiveOption = false)
		{
			SettingsManager settingsManager = KickStarter.settingsManager;
			if (settingsManager == null || settingsManager.playerSwitching == PlayerSwitching.DoNotAllow) return _playerID;

			List<string> labelList = new List<string> ();

			int i = 0;
			int playerNumber = -1;

			if (includeActiveOption)
			{
				labelList.Add ("Active Player");
				playerNumber = 0;
			}

			foreach (PlayerPrefab playerPrefab in settingsManager.players)
			{
				if (playerPrefab.playerOb != null)
				{
					labelList.Add (playerPrefab.ID.ToString () + ": " + playerPrefab.playerOb.name);
				}
				else
				{
					labelList.Add (playerPrefab.ID.ToString () + ": " + "(Undefined prefab)");
				}

				if (playerPrefab.ID == _playerID)
				{
					// Found match
					playerNumber = (includeActiveOption) ? (i+1) : i;
				}

				i++;
			}

			if (_playerID >= 0)
			{
				if ((includeActiveOption && playerNumber == 0) || (!includeActiveOption && playerNumber == -1))
				{
					// Wasn't found (item was possibly deleted), so revert to zero
					LogWarning ("Previously chosen Player no longer exists!");

					playerNumber = 0;
				}
			}

			playerNumber = EditorGUILayout.Popup ("Player:", playerNumber, labelList.ToArray ());

			if (playerNumber >= 0)
			{
				if (includeActiveOption)
				{
					if (playerNumber > 0)
					{
						_playerID = settingsManager.players[playerNumber - 1].ID;
					}
					else
					{
						_playerID = -1;
					}
				}
				else
				{
					_playerID = settingsManager.players[playerNumber].ID;
				}
			}

			return _playerID;
		}
		
		#endif

		
		protected void TryAssignConstantID (Component component, ref int _constantID)
		{
			if (component && !Application.isPlaying)
			{
				ConstantID constantIDComponent = component.GetComponent<ConstantID> ();
				if (constantIDComponent)
				{
					_constantID = constantIDComponent.constantID;
				}
			}
		}


		protected void TryAssignConstantID (GameObject gameObject, ref int _constantID)
		{
			if (gameObject && !Application.isPlaying)
			{
				ConstantID constantIDComponent = gameObject.GetComponent<ConstantID> ();
				if (constantIDComponent)
				{
					_constantID = constantIDComponent.constantID;
				}
			}
		}


		#if UNITY_EDITOR
		private ActionList parentList;
		#endif


		protected Player AssignPlayer (int _playerID, List<ActionParameter> parameters, int _playerParameterID)
		{
			_playerID = AssignInteger (parameters, _playerParameterID, _playerID);

			if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && _playerID >= 0)
			{
				PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (_playerID);
				if (playerPrefab != null)
				{
					return playerPrefab.GetSceneInstance ();
				}
				else
				{
					LogWarning ("No Player prefab found with ID = " + _playerID);
				}
				return null;
			}
			return KickStarter.player;
		}


		/**
		 * <summary>Passes the ActionList that the Action is a part of to the Action class. This is run before the Action is called or displayed in an Editor.</summary>
		 * <param name = "actionList">The ActionList that the Action is contained in</param>
		 */
		public virtual void AssignParentList (ActionList actionList)
		{
			#if UNITY_EDITOR
			parentList = actionList;
			#endif
		}


		protected void Log (string message, Object context = null)
		{
			#if UNITY_EDITOR
			ACDebug.Log (message, parentList, this, context);
			#else
			ACDebug.Log (message, context);
			#endif
		}


		protected void LogWarning (string message, Object context = null)
		{
			#if UNITY_EDITOR
			ACDebug.LogWarning (message, parentList, this, context);
			#else
			ACDebug.LogWarning (message, context);
			#endif
		}


		protected void LogError (string message, Object context = null)
		{
			#if UNITY_EDITOR
			ACDebug.LogError (message, parentList, this, context);
			#else
			ACDebug.LogError (message, context);
			#endif
		}


		/**
		 * <summary>Overwrites any appropriate variables with values set using parameters, or from ConstantID numbers.</summary>
		 * <param name = "parameters">A List of parameters that overwrite variable values</param>
		 */
		public virtual void AssignValues (List<ActionParameter> parameters)
		{
			AssignValues ();
		}


		/**
		 * Overwrites any appropriate variables from ConstantID numbers.
		 */
		public virtual void AssignValues ()
		{ }


		protected ActionParameter GetParameterWithID (List<ActionParameter> parameters, int _id)
		{
			if (parameters != null && _id >= 0)
			{
				foreach (ActionParameter _parameter in parameters)
				{
					if (_parameter.ID == _id)
					{
						return _parameter;
					}
				}
			}
			return null;
		}


		/**
		 * <summary>Resets any runtime values that are necessary to run the Action succesfully</summary>
		 * <param name = "actionList">The ActionList that the Action is a part of<param>
		 */
		public virtual void Reset (ActionList actionList)
		{
			isRunning = false;
		}


		protected string AssignString (List<ActionParameter> parameters, int _parameterID, string field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null)
			{
				switch (parameter.parameterType)
				{
					case ParameterType.String:
						return parameter.stringValue;

					case ParameterType.PopUp:
						return parameter.GetValueAsString ();

					default:
						break;
				}
			}
			return field;
		}


		/**
		 * <summary>Replaces a boolean based on an ActionParameter, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the float</param>
		 * <param name = "field">The bool to replace</param>
		 * <returns>The replaced BoolValue enum, or field if no replacements were found</returns>
		 */
		public BoolValue AssignBoolean (List<ActionParameter> parameters, int _parameterID, BoolValue field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Boolean)
			{
				if (parameter.intValue == 1)
				{
					return BoolValue.True;
				}
				return BoolValue.False;
			}
			return field;
		}


		/**
		 * <summary>Replaces an integer based on an ActionParameter, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the float</param>
		 * <param name = "field">The integer to replace</param>
		 * <returns>The replaced integer, or field if no replacements were found</returns>
		 */
		public int AssignInteger (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null)
			{
				switch (parameter.parameterType)
				{
					case ParameterType.Integer:
					case ParameterType.PopUp:
						return (parameter.intValue);

					default:
						break;
				}
			}
			return field;
		}


		/**
		 * <summary>Replaces a float based on an ActionParameter, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the float</param>
		 * <param name = "field">The float to replace</param>
		 * <returns>The replaced float, or field if no replacements were found</returns>
		 */
		public float AssignFloat (List<ActionParameter> parameters, int _parameterID, float field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Float)
			{
				return (parameter.floatValue);
			}
			return field;
		}


		protected Vector3 AssignVector3 (List<ActionParameter> parameters, int _parameterID, Vector3 field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Vector3)
			{
				return (parameter.vector3Value);
			}
			return field;
		}


		protected int AssignVariableID (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && (parameter.parameterType == ParameterType.GlobalVariable || parameter.parameterType == ParameterType.LocalVariable || parameter.parameterType == ParameterType.ComponentVariable))
			{
				return (parameter.intValue);
			}
			return field;
		}


		protected GVar AssignVariable (List<ActionParameter> parameters, int _parameterID, GVar field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null)
			{
				return (parameter.GetVariable ());
			}
			return field;
		}


		protected Variables AssignVariablesComponent (List<ActionParameter> parameters, int _parameterID, Variables field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				return (parameter.variables);
			}
			return field;
		}


		protected int AssignInvItemID (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.InventoryItem)
			{
				return (parameter.intValue);
			}
			return field;
		}


		protected int AssignDocumentID (List<ActionParameter> parameters, int _parameterID, int field)
		{
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.Document)
			{
				return (parameter.intValue);
			}
			return field;
		}


		/**
		 * <summary>Replaces a Transform based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Transform</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the Transform</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Transform to replace field with</param>
		 * <param name = "field">The Transform to replace</param>
		 * <returns>The replaced Transform, or field if no replacements were found</returns>
		 */
		public Transform AssignFile (List<ActionParameter> parameters, int _parameterID, int _constantID, Transform field)
		{
			Transform file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				if (parameter.intValue != 0)
				{
					ConstantID idObject = ConstantID.GetComponent (parameter.intValue);
					if (idObject != null)
					{
						file = idObject.gameObject.transform;
					}
				}

				if (file == null)
				{
					if (/*!isAssetFile && */parameter.gameObject != null)
					{
						file = parameter.gameObject.transform;
					}
					else if (parameter.intValue != 0)
					{
						ConstantID idObject = ConstantID.GetComponent (parameter.intValue);
						if (idObject != null)
						{
							file = idObject.gameObject.transform;
						}
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.transform;
				}
			}
			else if (_constantID != 0)
			{
				ConstantID idObject = ConstantID.GetComponent (_constantID);
				if (idObject != null)
				{
					file = idObject.gameObject.transform;
				}
			}
			
			return file;
		}


		/**
		 * <summary>Replaces a Collider based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Collider</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the Collider</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Collider to replace field with</param>
		 * <param name = "field">The Collider to replace</param>
		 * <returns>The replaced Collider, or field if no replacements were found</returns>
		 */
		public Collider AssignFile (List<ActionParameter> parameters, int _parameterID, int _constantID, Collider field)
		{
			Collider file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				file = null;
				if (parameter.intValue != 0)
				{
					file = ConstantID.GetComponent <Collider> (parameter.intValue);
				}
				if (file == null)
				{
					if (parameter.gameObject != null && parameter.gameObject.GetComponent <Collider>())
					{
						file = parameter.gameObject.GetComponent <Collider>();
					}
					else if (parameter.intValue != 0)
					{
						file = ConstantID.GetComponent <Collider> (parameter.intValue);
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.GetComponent <Collider>();;
				}
			}
			else if (_constantID != 0)
			{
				Collider newField = ConstantID.GetComponent <Collider> (_constantID);
				if (newField != null)
				{
					file = newField;
				}

			}
			return file;
		}


		/**
		 * <summary>Replaces a GameObject based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the GameObject</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the GameObject</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the GameObject to replace field with</param>
		 * <param name = "field">The GameObject to replace</param>
		 * <returns>The replaced GameObject, or field if no replacements were found</returns>
		 */
		protected GameObject AssignFile (List<ActionParameter> parameters, int _parameterID, int _constantID, GameObject field)
		{
			GameObject file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				file = null;
				if (parameter.intValue != 0)
				{
					ConstantID idObject = ConstantID.GetComponent (parameter.intValue);
					if (idObject != null)
					{
						file = idObject.gameObject;
					}
				}

				if (file == null)
				{
					if (parameter.gameObject != null)
					{
						file = parameter.gameObject;
					}
					else if (parameter.intValue != 0)
					{
						ConstantID idObject = ConstantID.GetComponent (parameter.intValue);
						if (idObject != null)
						{
							file = idObject.gameObject;
						}
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.gameObject;
				}
			}
			else if (_constantID != 0)
			{
				ConstantID idObject = ConstantID.GetComponent (_constantID);
				if (idObject != null)
				{
					file = idObject.gameObject;
				}
			}
			
			return file;
		}


		/**
		 * <summary>Replaces a GameObject based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the GameObject</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the GameObject</param>
		 * <param name = "field">The Object to replace</param>
		 * <returns>The replaced Object, or field if no replacements were found</returns>
		 */
		public Object AssignObject <T> (List<ActionParameter> parameters, int _parameterID, Object field) where T : Object
		{
			Object file = field;
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);

			if (parameter != null && parameter.parameterType == ParameterType.UnityObject)
			{
				file = null;
				if (parameter.objectValue != null)
				{
					if (parameter.objectValue is T)
					{
						file = parameter.objectValue;
					}
					else
					{
						ACDebug.LogWarning ("Cannot convert " + parameter.objectValue.name + " to type '" + typeof (T) + "'");
					}
				}
			}

			return file;
		}


		/**
		 * <summary>Replaces a generic Behaviour based on an ActionParameter or ConstantID instance, if appropriate.</summary>
		 * <param name = "parameters">A List of ActionParameters that may override the Behaviour</param>
		 * <param name = "_parameterID">The ID of the ActionParameter to search for within parameters that will replace the Behaviour</param>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Behaviour to replace field with</param>
		 * <param name = "field">The Behaviour to replace</param>
		 * <param name = "doLog">If True, and no file is found when one is expected, a warning message will be displayed in the Console</param>
		 * <returns>The replaced Behaviour, or field if no replacements were found</returns>
		 */
		public T AssignFile <T> (List<ActionParameter> parameters, int _parameterID, int _constantID, T field, bool doLog = true) where T : Behaviour
		{
			T file = field;
			
			ActionParameter parameter = GetParameterWithID (parameters, _parameterID);
			if (parameter != null && parameter.parameterType == ParameterType.GameObject)
			{
				file = null;
				if (parameter.intValue != 0)
				{
					file = ConstantID.GetComponent <T> (parameter.intValue);

					if (file == null && parameter.gameObject && parameter.intValue != -1 && doLog)
					{
						LogWarning ("No " + typeof(T) + " component attached to " + parameter.gameObject + "!", parameter.gameObject);
					}
				}
				if (file == null)
				{
					if (parameter.gameObject && parameter.gameObject.GetComponent<T> ())
					{
						file = parameter.gameObject.GetComponent<T> ();
					}
					else if (parameter.intValue != 0)
					{
						file = ConstantID.GetComponent<T> (parameter.intValue);
					}
					if (doLog && file == null && parameter.gameObject && parameter.gameObject.GetComponent<T> () == null)
					{
						LogWarning ("No " + typeof (T) + " component attached to " + parameter.gameObject + "!", parameter.gameObject);
					}
				}
			}
			else if (parameter != null && parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (parameter.variables != null)
				{
					file = parameter.variables.GetComponent <T>();
				}
			}
			else if (_constantID != 0)
			{
				T newField = ConstantID.GetComponent <T> (_constantID);
				if (newField != null)
				{
					file = newField;
				}
			}

			return file;
		}


		/**
		 * <summary>Replaces a generic Behaviour based on a ConstantID, if appropriate.</summary>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Behaviour to replace field with</param>
		 * <param name = "field">The Behaviour to replace</param>
		 * <returns>The replaced Behaviour, or field if no replacements were found</returns>
		 */
		public T AssignFile <T> (int _constantID, T field) where T : Behaviour
		{
			if (_constantID != 0)
			{
				T newField = ConstantID.GetComponent <T> (_constantID);
				if (newField != null)
				{
					return newField;
				}
			}
			return field;
		}


		/**
		 * <summary>Replaces a GameObject based on a ConstantID, if appropriate.</summary>
		 * <param name = "_constantID">If !=0, The ConstantID number of the GameObject to replace field with</param>
		 * <param name = "field">The GameObject to replace</param>
		 * <returns>The replaced GameObject, or field if no replacements were found</returns>
		 */
		protected GameObject AssignFile (int _constantID, GameObject field)
		{
			if (_constantID != 0)
			{
				ConstantID newField = ConstantID.GetComponent (_constantID);
				if (newField != null)
				{
					return newField.gameObject;
				}
			}
			return field;
		}


		/**
		 * <summary>Replaces a Transform based on a ConstantID, if appropriate.</summary>
		 * <param name = "_constantID">If !=0, The ConstantID number of the Transform to replace field with</param>
		 * <param name = "field">The Transform to replace</param>
		 * <returns>The replaced Transform, or field if no replacements were found</returns>
		 */
		public Transform AssignFile (int _constantID, Transform field)
		{
			if (_constantID != 0)
			{
				ConstantID newField = ConstantID.GetComponent (_constantID);
				if (newField != null)
				{
					return newField.transform;
				}
			}
			return field;
		}


		#if UNITY_EDITOR

		public void FixLinkAfterDeleting (Action actionToDelete, Action targetAction, List<Action> actionList)
		{
			foreach (ActionEnd end in endings)
			{
				if ((end.resultAction == ResultAction.Skip && end.skipActionActual == actionToDelete) || (end.resultAction == ResultAction.Continue && actionList.IndexOf (actionToDelete) == (actionList.IndexOf (this) + 1)))
				{
					if (targetAction == null)
					{
						end.resultAction = ResultAction.Stop;
					}
					else
					{
						end.resultAction = ResultAction.Skip;
						end.skipActionActual = targetAction;
					}
				}
			}
		}


		public virtual void ClearIDs ()
		{}


		public void BreakPoint (int i, ActionList list)
		{
			if (isBreakPoint)
			{
				ACDebug.Log ("Break-point with (" + i.ToString () + ")", list, this);
				EditorApplication.isPaused = true;
			}
		}


		protected virtual string GetSocketLabel (int i)
		{
			if (NumSockets == 1)
			{
				return "After running:";
			}
			return "Option " + i.ToString () + ":";
		}

		#endif


		protected ActionEnd GenerateActionEnd (ResultAction _resultAction, ActionListAsset _linkedAsset, Cutscene _linkedCutscene, int _skipAction, Action _skipActionActual, List<Action> _actions)
		{
			ActionEnd actionEnd = new ActionEnd ();

			actionEnd.resultAction = _resultAction;
			actionEnd.linkedAsset = _linkedAsset;
			actionEnd.linkedCutscene = _linkedCutscene;
			
			if (_resultAction == ResultAction.RunCutscene)
			{
				if (isAssetFile && _linkedAsset != null)
				{
					actionEnd.linkedAsset = _linkedAsset;
				}
				else if (!isAssetFile && _linkedCutscene != null)
				{
					actionEnd.linkedCutscene = _linkedCutscene;
				}
			}
			else if (_resultAction == ResultAction.Skip)
			{
				int skip = _skipAction;
				if (_skipActionActual != null && _actions.Contains (_skipActionActual))
				{
					skip = _actions.IndexOf (_skipActionActual);
				}
				else if (skip == -1)
				{
					skip = 0;
				}
				actionEnd.skipAction = skip;
			}
			
			return actionEnd;
		}

		
		public static ActionEnd GenerateStopActionEnd ()
		{
			ActionEnd stopActionEnd = new ActionEnd
			{
				resultAction = ResultAction.Stop
			};
			return stopActionEnd;
		}


		/**
		 * <summary>Updates which output was followed when the Action was last run</summary>
		 * <param name = "_lastRunOutput">The index of the ending last run</param>
		 */
		public virtual void SetLastResult (int _lastRunOutput)
		{
			lastRunOutput = _lastRunOutput;
		}


		public void ResetLastResult ()
		{
			lastRunOutput = -10;
		}


		/** Use this in Action subclasses to reset any value in an asset-based Action when the game resets */
		public virtual void ResetAssetValues ()
		{}


		public int LastRunOutput
		{
			get
			{
				return lastRunOutput;
			}
		}


		/**
		 * <summary>Update the Action's output socket</summary>
		 * <param name = "actionEnd">A data container for the output socket</param>
		 */
		public void SetOutput (ActionEnd actionEnd)
		{
			SetOutputs (new ActionEnd[1] { actionEnd });
		}


		public static T CreateNew <T> () where T : Action
		{
			#if AC_ActionListPrefabs
			T newAction = (T) System.Activator.CreateInstance<T> ();
			#else
			T newAction = (T) CreateInstance<T> ();
			#endif
			return newAction;
		}


		public static Action CreateNew (string className)
		{
			if (className == "ActionEvent")
			{
				// Dirty hack due to shared class name with Unity
				className = "AC." + className;
			}

			#if AC_ActionListPrefabs
			if (!className.StartsWith ("AC.")) className = "AC." + className;
			System.Runtime.Remoting.ObjectHandle handle = System.Activator.CreateInstance ("Assembly-CSharp", "AC." + className);
			Action newAction = (Action) handle.Unwrap ();
			#else
			Action newAction = (Action) CreateInstance (className);
			#endif
			return newAction;
		}


		#if UNITY_EDITOR

		public Rect NodeRect
		{
			get
			{
				return nodeRect;
			}
			set
			{
				nodeRect = value;
			}
		}


		public static void EditSource (Action _action)
		{
			if (_action == null) return;

			#if AC_ActionListPrefabs

			ActionType actionType = KickStarter.actionsManager.GetActionType (_action);
			string[] assets = AssetDatabase.FindAssets (actionType.fileName + " t: Script", null);
			if (assets.Length > 0)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath (assets[0]);
				var script = AssetDatabase.LoadMainAssetAtPath (assetPath);
				AssetDatabase.OpenAsset (script);
			}

			#else

			var script = MonoScript.FromScriptableObject (_action);
			if (script != null)
			{
				AssetDatabase.OpenAsset (script);
			}

			#endif
		}

		#endif

	}
	
}