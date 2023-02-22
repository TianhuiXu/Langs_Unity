/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionBlendShape.cs"
 * 
 *	This action is used to animate blend shapes within
 *	groups, as set by the Shapeable script
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
	public class ActionBlendShape : Action
	{
		
		public int parameterID = -1;
		public int constantID = 0;
		
		public Shapeable shapeObject;
		public int shapeGroupID = 0;
		public int shapeKeyID = 0;
		public float shapeValue = 0f;
		
		public bool isPlayer = false;
		public int playerID = -1;
		public int playerParameterID = -1;

		public bool disableAllKeys = false;
		public float fadeTime = 0f;
		public MoveMethod moveMethod = MoveMethod.Smooth;
		public AnimationCurve timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));

		protected Shapeable runtimeShapeObject;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Blend shape"; }}
		public override string Description { get { return "Animates a Skinned Mesh Renderer's blend shape by a chosen amount. If the Shapeable script attached to the renderer has grouped multiple shapes into a group, all other shapes in that group will be deactivated."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeShapeObject = AssignFile <Shapeable> (parameters, parameterID, constantID, shapeObject);
			
			if (isPlayer)
			{
				Player player = AssignPlayer (playerID, parameters, playerParameterID);
				runtimeShapeObject = (player != null) ? player.GetShapeable () : null;
			}
		}
		
		
		public override float Run ()
		{
			if (isPlayer && runtimeShapeObject == null)
			{
				LogWarning ("Cannot BlendShape Player since cannot find Shapeable script on Player.");
			}

			if (!isRunning)
			{
				isRunning = true;
			   
				if (runtimeShapeObject != null)
				{
					DoShape (fadeTime);
					
					if (willWait)
					{
						return (fadeTime);
					}
				}
			}
			else
			{
				isRunning = false;
				return 0f;
			}
			return 0f;
		}


		public override void Skip ()
		{
			DoShape (0f);
		}


		protected void DoShape (float _time)
		{
			if (runtimeShapeObject != null)
			{
				if (disableAllKeys)
				{
					runtimeShapeObject.DisableAllKeys (shapeGroupID, _time, moveMethod, timeCurve);
				}
				else
				{
					runtimeShapeObject.SetActiveKey (shapeGroupID, shapeKeyID, shapeValue, _time, moveMethod, timeCurve);
				}
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerParameterID = ChooseParameterGUI ("Player ID:", parameters, playerParameterID, ParameterType.Integer);
					if (playerParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}

				Player _player = null;

				if (playerParameterID < 0 || KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
				{
					if (playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						if (playerPrefab != null)
						{
							_player = (Application.isPlaying) ? playerPrefab.GetSceneInstance () : playerPrefab.playerOb;
						}
					}
					else
					{
						_player = (Application.isPlaying) ? KickStarter.player : AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
					}
				}

				if (_player && _player.GetShapeable ())
				{
					shapeObject = _player.GetShapeable ();
				}
				else
				{
					shapeObject = null;
					EditorGUILayout.HelpBox ("Cannot find player with Shapeable script attached", MessageType.Warning);
				}
			}
			else
			{
				parameterID = ChooseParameterGUI ("Object:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					shapeObject = null;
				}
				else
				{
					shapeObject = (Shapeable) EditorGUILayout.ObjectField ("Object:", shapeObject, typeof (Shapeable), true);
					
					constantID = FieldToID <Shapeable> (shapeObject, constantID);
					shapeObject = IDToField <Shapeable> (shapeObject, constantID, false);
				}
			}
			
			if (shapeObject && shapeObject.shapeGroups != null)
			{
				shapeGroupID = ActionBlendShape.ShapeableGroupGUI ("Shape group:", shapeObject.shapeGroups, shapeGroupID);
				disableAllKeys = EditorGUILayout.Toggle ("Disable all keys?", disableAllKeys);
				if (!disableAllKeys)
				{
					ShapeGroup _shapeGroup = shapeObject.GetGroup (shapeGroupID);
					if (_shapeGroup != null)
					{
						if (_shapeGroup.shapeKeys != null && _shapeGroup.shapeKeys.Count > 0)
						{
							shapeKeyID = ShapeableKeyGUI (_shapeGroup.shapeKeys, shapeKeyID);
						}
						else
						{
							EditorGUILayout.HelpBox ("No shape keys found.", MessageType.Info);
						}
					}
					shapeValue = EditorGUILayout.Slider ("New value:", shapeValue, 0f, 100f);
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("An object must be assigned before more options can show.", MessageType.Info);
			}

			fadeTime = EditorGUILayout.FloatField ("Transition time:", fadeTime);
			if (fadeTime > 0f)
			{
				moveMethod = (MoveMethod) EditorGUILayout.EnumPopup ("Move method:", moveMethod);
				if (moveMethod == MoveMethod.CustomCurve)
				{
					timeCurve = EditorGUILayout.CurveField ("Time curve:", timeCurve);
				}
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (shapeObject)
			{
				return shapeObject.gameObject.name;
			}
			return string.Empty;
		}


		public static int ShapeableGroupGUI (string label, List<ShapeGroup> shapeGroups, int groupID)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int groupNumber = 0;
			
			if (shapeGroups.Count > 0)
			{
				foreach (ShapeGroup shapeGroup in shapeGroups)
				{
					if (!string.IsNullOrEmpty (shapeGroup.label))
					{
						labelList.Add (shapeGroup.ID + ": " + shapeGroup.label);
					}
					else
					{
						labelList.Add (shapeGroup.ID + ": (Untitled)");
					}
					if (shapeGroup.ID == groupID)
					{
						groupNumber = i;
					}
					i++;
				}
				
				if (groupNumber == -1)
				{
					if (groupID > 0) Debug.LogWarning ("Previously chosen shape group no longer exists!");
					groupID = 0;
				}
				
				groupNumber = EditorGUILayout.Popup (label, groupNumber, labelList.ToArray());
				groupID = shapeGroups[groupNumber].ID;
			}
			else
			{
				EditorGUILayout.HelpBox ("No shape groups exist!", MessageType.Info);
				groupID = -1;
			}
			
			return groupID;
		}
		
		
		private int ShapeableKeyGUI (List<ShapeKey> shapeKeys, int keyID)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int keyNumber = 0;
			
			if (shapeKeys.Count > 0)
			{
				foreach (ShapeKey shapeKey in shapeKeys)
				{
					if (shapeKey.label != "")
					{
						labelList.Add (shapeKey.label);
					}
					else
					{
						labelList.Add ("(Untitled)");
					}
					if (shapeKey.ID == keyID)
					{
						keyNumber = i;
					}
					i++;
				}
				
				if (keyNumber == -1)
				{
					if (keyID > 0) LogWarning ("Previously chosen shape key no longer exists!");
					keyID = 0;
				}
				
				keyNumber = EditorGUILayout.Popup ("Shape key:", keyNumber, labelList.ToArray());
				keyID = shapeKeys[keyNumber].ID;
			}
			else
			{
				EditorGUILayout.HelpBox ("No shape keys exist!", MessageType.Info);
				keyID = -1;
			}
			
			return keyID;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			Shapeable obToUpdate = shapeObject;

			if (isPlayer && (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow))
			{
				if (!fromAssetFile)
				{
					Player charToUpdate = Object.FindObjectOfType<Player> ();
					if (charToUpdate != null)
						obToUpdate = charToUpdate.GetShapeable ();
				}

				if (obToUpdate == null && AdvGame.GetReferences ().settingsManager != null)
				{
					Player player = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
					obToUpdate = player.GetShapeable ();
				}
			}

			if (saveScriptsToo)
			{
				AddSaveScript <RememberShapeable> (obToUpdate);
			}
			AssignConstantID <Shapeable> (obToUpdate, constantID, parameterID);
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (shapeObject && shapeObject.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow))
			{
				if (_gameObject && _gameObject.GetComponent<Char> () && _gameObject.GetComponent<Char> ().IsPlayer) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && playerParameterID < 0) return true;
			return (playerParameterID < 0 && playerID == _playerID);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Blend shape' Action, set to change which key in a Shapeable is active</summary>
		 * <param name = "shapeable">The Shapeable to manipulate</param>
		 * <param name = "groupID">The ID of the group to affect</param>
		 * <param name = "keyID">The ID of the key to affect</param>
		 * <param name = "newKeyValue">The key's new value</param>
		 * <param name = "transitionTime">The time, in seconds, to take</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionBlendShape CreateNew_SetActiveKey (Shapeable shapeable, int groupID, int keyID, float newKeyValue, float transitionTime = 0f, MoveMethod moveMethod = MoveMethod.Linear, AnimationCurve timeCurve = null)
		{
			ActionBlendShape newAction = CreateNew<ActionBlendShape> ();
			newAction.disableAllKeys = false;
			newAction.shapeObject = shapeable;
			newAction.TryAssignConstantID (newAction.shapeObject, ref newAction.constantID);
			newAction.shapeGroupID = groupID;
			newAction.shapeKeyID = keyID;
			newAction.shapeValue = newKeyValue;
			newAction.fadeTime = transitionTime;
			newAction.moveMethod = moveMethod;
			newAction.timeCurve = timeCurve;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Blend shape' Action, set to disable all keys on a Shapeable</summary>
		 * <param name = "shapeable">The Shapeable to manipulate</param>
		 * <param name = "groupID">The ID of the group to affect</param>
		 * <param name = "transitionTime">The time, in seconds, to take</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionBlendShape CreateNew_DisableAllKeys (Shapeable shapeable, int groupID, float transitionTime = 0f, MoveMethod moveMethod = MoveMethod.Linear, AnimationCurve timeCurve = null)
		{
			ActionBlendShape newAction = CreateNew<ActionBlendShape> ();
			newAction.disableAllKeys = true;
			newAction.shapeObject = shapeable;
			newAction.TryAssignConstantID (newAction.shapeObject, ref newAction.constantID);
			newAction.shapeGroupID = groupID;
			newAction.fadeTime = transitionTime;
			newAction.moveMethod = moveMethod;
			newAction.timeCurve = timeCurve;
			return newAction;
		}

	}
	
}