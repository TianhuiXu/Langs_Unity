/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionTransformRecord.cs"
 * 
 *	This action records an object's position, rotation, or scale - and stores it in a Vector3 variable.
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
	public class ActionTransformRecord : Action
	{

		public bool isPlayer;
		public int playerID = -1;
		public GameObject obToRead;
		public int obToReadParameterID = -1;
		public int obToReadConstantID = 0;
		protected GameObject runtimeObToRead;

		public TransformRecordType transformRecordType = TransformRecordType.Position;
		public enum TransformRecordType { Position, Rotation, Scale };
		public GlobalLocal transformLocation;

		public VariableLocation variableLocation;
		public int variableID;
		public int variableParameterID = -1;

		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;
		protected Variables runtimeVariables;
		protected LocalVariables localVariables;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Record transform"; }}
		public override string Description { get { return "Records the transform values of a GameObject."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				Player player = AssignPlayer (playerID, parameters, obToReadParameterID);
				runtimeObToRead = (player != null) ? player.gameObject : null;
			}
			else
			{
				runtimeObToRead = AssignFile (parameters, obToReadParameterID, obToReadConstantID, obToRead);
			}

			runtimeVariable = null;
			switch (variableLocation)
			{
				case VariableLocation.Global:
					variableID = AssignVariableID (parameters, variableParameterID, variableID);
					runtimeVariable = GlobalVariables.GetVariable (variableID, true);
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						variableID = AssignVariableID (parameters, variableParameterID, variableID);
						runtimeVariable = LocalVariables.GetVariable (variableID, localVariables);
					}
					break;

				case VariableLocation.Component:
					runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
					if (runtimeVariables != null)
					{
						runtimeVariable = runtimeVariables.GetVariable (variableID);
					}
					runtimeVariable = AssignVariable (parameters, variableParameterID, runtimeVariable);
					runtimeVariables = AssignVariablesComponent (parameters, variableParameterID, runtimeVariables);
					break;
			}
		}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}


		public override float Run ()	
		{
			if (runtimeObToRead != null)
			{
				if (runtimeVariable != null)
				{
					switch (transformRecordType)
					{
						case TransformRecordType.Position:
							if (transformLocation == GlobalLocal.Global)
							{
								runtimeVariable.Vector3Value = runtimeObToRead.transform.position;
							}
							else if (transformLocation == GlobalLocal.Local)
							{
								runtimeVariable.Vector3Value = runtimeObToRead.transform.localPosition;
							}
							break;

						case TransformRecordType.Rotation:
							if (transformLocation == GlobalLocal.Global)
							{
								runtimeVariable.Vector3Value = runtimeObToRead.transform.eulerAngles;
							}
							else if (transformLocation == GlobalLocal.Local)
							{
								runtimeVariable.Vector3Value = runtimeObToRead.transform.localEulerAngles;
							}
							break;

						case TransformRecordType.Scale:
							if (transformLocation == GlobalLocal.Global)
							{
								runtimeVariable.Vector3Value = runtimeObToRead.transform.lossyScale;
							}
							else if (transformLocation == GlobalLocal.Local)
							{
								runtimeVariable.Vector3Value = runtimeObToRead.transform.localScale;
							}
							break;
					}

					runtimeVariable.Upload (variableLocation, runtimeVariables);
				}
			}

			return 0f;
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Record Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					obToReadParameterID = ChooseParameterGUI ("Player ID:", parameters, obToReadParameterID, ParameterType.Integer);
					if (obToReadParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				obToReadParameterID = Action.ChooseParameterGUI ("Object to record:", parameters, obToReadParameterID, ParameterType.GameObject);
				if (obToReadParameterID >= 0)
				{
					obToReadConstantID = 0;
					obToRead = null;
				}
				else
				{
					obToRead = (GameObject) EditorGUILayout.ObjectField ("Object to record:", obToRead, typeof (GameObject), true);

					obToReadConstantID = FieldToID (obToRead, obToReadConstantID);
					obToRead = IDToField (obToRead, obToReadConstantID, false);
				}
			}
			
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Record:", GUILayout.MaxWidth (100f));
			transformLocation = (GlobalLocal) EditorGUILayout.EnumPopup (transformLocation);
			transformRecordType = (TransformRecordType) EditorGUILayout.EnumPopup (transformRecordType);
			EditorGUILayout.EndHorizontal ();

			variableLocation = (VariableLocation) EditorGUILayout.EnumPopup ("Variable location:", variableLocation);

			switch (variableLocation)
			{
				case VariableLocation.Global:
					variableParameterID = Action.ChooseParameterGUI ("Record to variable:", parameters, variableParameterID, ParameterType.GlobalVariable);
					if (variableParameterID < 0)
					{
						variableID = AdvGame.GlobalVariableGUI ("Record to variable:", variableID, VariableType.Vector3);
					}
					break;

				case VariableLocation.Local:
					if (!isAssetFile)
					{
						variableParameterID = Action.ChooseParameterGUI ("Record to variable:", parameters, variableParameterID, ParameterType.LocalVariable);
						if (variableParameterID < 0)
						{
							variableID = AdvGame.LocalVariableGUI ("Record to variable:", variableID, VariableType.Vector3);
						}
					}
					else
					{
						EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
					}
					break;

				case VariableLocation.Component:
					variableParameterID = Action.ChooseParameterGUI ("Record to variable:", parameters, variableParameterID, ParameterType.ComponentVariable);
					if (variableParameterID >= 0)
					{
						variables = null;
						variablesConstantID = 0;	
					}
					else
					{
						variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
						variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
						variables = IDToField <Variables> (variables, variablesConstantID, false);
						
						if (variables != null)
						{
							variableID = AdvGame.ComponentVariableGUI ("Record to variable:", variableID, VariableType.Vector3, variables);
						}
					}
					break;
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				AssignConstantID (obToRead, obToReadConstantID, obToReadParameterID);
			}

			if (variableLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, variableParameterID);
			}
		}


		public override string SetLabel ()
		{
			if (obToRead != null)
			{
				return obToRead.name + " " + transformRecordType.ToString ();
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (!isPlayer && obToReadParameterID < 0)
			{
				if (obToRead && obToRead == gameObject) return true;
				if (obToReadConstantID == id && id != 0) return true;
			}
			if (isPlayer && gameObject && gameObject.GetComponent <Player>()) return true;
			if (variableParameterID < 0 && variableLocation == VariableLocation.Component)
			{
				if (variables && variables.gameObject == gameObject) return true;
				if (variablesConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && obToReadParameterID < 0) return true;
			return (obToReadParameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Record transform' Action</summary>
		 * <param name = "objectToRecord">The GameObject whose transform to record</param>
		 * <param name = "recordType">The type of recording to take</param>
		 * <param name = "inWorldSpace">If True, the GameObjects's transform values will be read in world space</param>
		 * <param name = "variableLocation">The location of the Vector3 variable to store the values</param>
		 * <param name = "variableID">The ID number of the Vector3 variable to store the values</param>
		 * <param name = "variables">The variable's associated Variables component, if a Component Variable</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTransformRecord CreateNew (GameObject objectToRecord, TransformRecordType recordType, bool inWorldSpace, VariableLocation variableLocation, int variableID, Variables variables = null)
		{
			ActionTransformRecord newAction = CreateNew<ActionTransformRecord> ();
			newAction.obToRead = objectToRecord;
			newAction.TryAssignConstantID (newAction.obToRead, ref newAction.obToReadConstantID);
			newAction.transformRecordType = recordType;
			newAction.transformLocation = (inWorldSpace) ? GlobalLocal.Global : GlobalLocal.Local;
			newAction.variableLocation = variableLocation;
			newAction.variableID = variableID;
			newAction.variables = variables;
			return newAction;
		}

	}

}