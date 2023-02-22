#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(Paths))]
	public class PathsEditor : Editor
	{
		
		private static readonly GUIContent insertContent = new GUIContent("+", "Insert node");
		private static readonly GUIContent deleteContent = new GUIContent("-", "Delete node");

		private static readonly GUILayoutOption buttonWidth = GUILayout.MaxWidth(20f);


		private void OnEnable ()
		{
			EditorApplication.update += Update;
		}


		private void OnDisable ()
		{
			EditorApplication.update -= Update;
		}

		
		public override void OnInspectorGUI ()
		{
			Paths _target = (Paths) target;

			if (_target.GetComponent <AC.Char>())
			{
				return;
			}

			int numNodes = _target.nodes.Count;
			if (numNodes < 1)
			{
				numNodes = 1;
				_target.nodes = ResizeList (_target.nodes, numNodes);
			}

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Path properties", EditorStyles.boldLabel);
			_target.nodePause = CustomGUILayout.FloatField ("Node wait time (s):", _target.nodePause, "", "The time, in seconds, that a character will wait at each node before continuing along the path");
			_target.pathSpeed = (PathSpeed) CustomGUILayout.EnumPopup ("Walk or run:", _target.pathSpeed, "", "The speed at which characters will traverse a path");
			_target.pathType = (AC_PathType) CustomGUILayout.EnumPopup ("Path type:", _target.pathType, "", "The way in which characters move between each node");
			if (_target.pathType == AC_PathType.Loop)
			{
				_target.teleportToStart = CustomGUILayout.Toggle ("Teleports when looping?", _target.teleportToStart, "", "If True, then the character will teleport to the first node before traversing the path");
			}
			_target.affectY = CustomGUILayout.Toggle ("Override gravity?", _target.affectY, "", "If True, then characters will attempt to move vertically to reach nodes");
			_target.commandSource = (ActionListSource) CustomGUILayout.EnumPopup ("Node commands source:", _target.commandSource, "", "The source of ActionList objects that are run when nodes are reached");
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			// List nodes
			ResetCommandList (_target);

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			EditorGUILayout.LabelField ("Origin node:");
			ShowNodeCommandGUI (_target, 0);
			CustomGUILayout.EndVertical ();

			for (int i=1; i<_target.nodes.Count; i++)
			{
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				EditorGUILayout.BeginHorizontal ();

				if (_target.RelativeMode)
				{
					EditorGUILayout.LabelField ("Node " + i + ": " + _target.nodes[i].ToString ());
				}
				else
				{
					_target.nodes[i] = CustomGUILayout.Vector3Field ("Node " + i + ": ", _target.nodes[i], "", "");
				}
				
				if (GUILayout.Button (insertContent, EditorStyles.miniButtonLeft, buttonWidth))
				{
					Undo.RecordObject (_target, "Add path node");
					Vector3 newNodePosition;
					newNodePosition = _target.nodes[i] + new Vector3 (1.0f, 0f, 0f);

					if (i < (_target.nodes.Count - 1) && _target.nodes[i] != _target.nodes[i+1])
					{
						newNodePosition = (_target.nodes[i] + _target.nodes[i+1]) / 2f;
					}

					_target.nodes.Insert (i+1, newNodePosition);
					_target.nodeCommands.Insert (i+1, new NodeCommand ());
					numNodes += 1;
					ResetCommandList (_target);
				}
				if (GUILayout.Button (deleteContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Delete path node");
					_target.nodes.RemoveAt (i);
					_target.nodeCommands.RemoveAt (i);
					numNodes -= 1;
					ResetCommandList (_target);
				}

				EditorGUILayout.EndHorizontal ();
				ShowNodeCommandGUI (_target, i);
				CustomGUILayout.EndVertical ();
			}

			EditorGUILayout.Space ();
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Add node"))
			{
				Undo.RecordObject (_target, "Add path node");
				numNodes += 1;
			}

			if (numNodes > 1)
			{
				bool newRelativeMode = GUILayout.Toggle (_target.RelativeMode, "Lock relative positions", "Button");
				if (newRelativeMode && _target.RelativeMode != newRelativeMode)
				{
					_target.LastFramePosition = _target.transform.position;
				}
				_target.RelativeMode = newRelativeMode;
			}
			else
			{
				_target.RelativeMode = false;
			}
			EditorGUILayout.EndHorizontal ();

			if (_target.RelativeMode)
			{
				EditorGUILayout.HelpBox ("Before saving/exiting the scene, unlock the above button once nodes have been repositioned.", MessageType.Warning);
			}

			_target.nodes[0] = _target.transform.position;
			_target.nodes = ResizeList (_target.nodes, numNodes);

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void ShowNodeCommandGUI (Paths _target, int i)
		{
			if (_target.nodeCommands.Count > i)
			{
				if (_target.commandSource == ActionListSource.InScene)
				{
					_target.nodeCommands[i].cutscene = ActionListAssetMenu.CutsceneGUI ("Cutscene on reach:", _target.nodeCommands[i].cutscene, _target.name + "_OnReachNode_" + i.ToString (), "", "The Cutscene to run when the node is reached");
					
					if (_target.nodeCommands[i].cutscene != null && _target.nodeCommands[i].cutscene.useParameters)
					{
						_target.nodeCommands[i].parameterID = SetParametersGUI (_target.nodeCommands[i].cutscene.parameters, _target.nodeCommands[i].parameterID);
					}
				}
				else
				{
					_target.nodeCommands[i].actionListAsset = ActionListAssetMenu.AssetGUI ("ActionList on reach:", _target.nodeCommands[i].actionListAsset, _target.name + "_OnReachNode_" + i.ToString (), "", "The ActionList asset to run when the node is reached");
				
					if (_target.nodeCommands[i].actionListAsset != null && _target.nodeCommands[i].actionListAsset.NumParameters > 0)
					{
						_target.nodeCommands[i].parameterID = SetParametersGUI (_target.nodeCommands[i].actionListAsset.DefaultParameters, _target.nodeCommands[i].parameterID);
					}
				}

				if ((_target.commandSource == ActionListSource.InScene && _target.nodeCommands[i].cutscene != null) ||
					(_target.commandSource == ActionListSource.AssetFile && _target.nodeCommands[i].actionListAsset != null))
				{
					_target.nodeCommands[i].pausesCharacter = CustomGUILayout.Toggle ("Character waits during?", _target.nodeCommands[i].pausesCharacter, string.Empty, "If True, then the character moving along the path will stop moving while the cutscene is run");
				}
			}
		}
		

		private void OnSceneGUI ()
		{
			Paths _target = (Paths) target;

			GUIStyle style = new GUIStyle ();
			style.normal.textColor = Color.white;
			style.normal.background = Resource.GreyTexture;

			for (int i=0; i<_target.nodes.Count; i++)
			{
				if (i>0 && !Application.isPlaying)
				{
					if (!_target.RelativeMode)
					{
						_target.nodes[i] = Handles.PositionHandle (_target.nodes[i], Quaternion.identity);
					}
				}
				Handles.Label (_target.nodes[i], i.ToString(), style);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void Update ()
		{
			Paths _target = (Paths) target;

			if (_target.RelativeMode)
			{
				Vector3 deltaPosition = _target.transform.position - _target.LastFramePosition;

				for (int i=0; i<_target.nodes.Count; i++)
				{
					if (i>0 && !Application.isPlaying)
					{
						Vector3 newNodePosition = _target.nodes[i] + deltaPosition;
						_target.nodes[i] = newNodePosition;
					}
				}

				_target.LastFramePosition = _target.transform.position;

				UnityVersionHandler.CustomSetDirty (_target);
			}
		}

		
		private List<Vector3> ResizeList (List<Vector3> list, int listSize)
		{
			if (list.Count < listSize)
			{
				// Increase size of list
				while (list.Count < listSize)
				{
					Vector3 newNodePosition;
					if (list.Count > 0)
					{
						newNodePosition = list[list.Count-1] + new Vector3 (1.0f, 0f, 0f);
					}
					else
					{
						newNodePosition = Vector3.zero;
					}
					list.Add (newNodePosition);
				}
			}
			else if (list.Count > listSize)
			{
				// Decrease size of list
				while (list.Count > listSize)
				{
					list.RemoveAt (list.Count - 1);
				}
			}
			return (list);
		}


		private int SetParametersGUI (List<ActionParameter> externalParameters, int parameterID)
		{
			if (externalParameters == null || externalParameters.Count == 0)
			{
				return -1;
			}

			List<string> labelList = new List<string>();
			labelList.Add (" (None)");
			foreach (ActionParameter paramater in externalParameters)
			{
				labelList.Add (paramater.label);
			}

			parameterID ++;
			parameterID = EditorGUILayout.Popup ("Character parameter:", parameterID, labelList.ToArray ());
			parameterID --;

			return parameterID;
		}


		private void ResetCommandList (Paths _target)
		{
			int numNodes = _target.nodes.Count;
			int numCommands = _target.nodeCommands.Count;

			if (numNodes < numCommands)
			{
				_target.nodeCommands.RemoveRange (numNodes, numCommands - numNodes);
			}
			else if (numNodes > numCommands)
			{
				if (numNodes > _target.nodeCommands.Capacity)
				{
					_target.nodeCommands.Capacity = numNodes;
				}
				for (int i=numCommands; i<numNodes; i++)
				{
					_target.nodeCommands.Add (new NodeCommand ());
				}
			}
		}

	}

}

#endif