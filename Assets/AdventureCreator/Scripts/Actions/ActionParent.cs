/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionParent.cs"
 * 
 *	This action is used to set and clear the parent of GameObjects.
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
	public class ActionParent : Action
	{

		public int parentTransformID = 0;
		public int parentTransformParameterID = -1;
		public int obToAffectID = 0;
		public int obToAffectParameterID = -1;

		public enum ParentAction { SetParent, ClearParent };
		public ParentAction parentAction;

		public Transform parentTransform;
		protected Transform runtimeParentTransform;
		
		public GameObject obToAffect;
		protected GameObject runtimeObToAffect;
		public bool isPlayer;
		public int playerID = -1;
		public int playerParameterID = -1;

		public bool setPosition;
		public Vector3 newPosition;
		
		public bool setRotation;
		public Vector3 newRotation;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Set parent"; }}
		public override string Description { get { return "Parent one GameObject to another. Can also set the child's local position and rotation."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeParentTransform = AssignFile (parameters, parentTransformParameterID, parentTransformID, parentTransform);

			if (isPlayer)
			{
				Player player = AssignPlayer (playerID, parameters, playerParameterID);
				runtimeObToAffect = (player != null) ? player.gameObject : null;
			}
			else
			{
				runtimeObToAffect = AssignFile (parameters, obToAffectParameterID, obToAffectID, obToAffect);
			}
		}
		
		
		public override float Run ()
		{
			switch (parentAction)
			{ 
				case ParentAction.SetParent:
					if (runtimeParentTransform)
					{
						runtimeObToAffect.transform.parent = runtimeParentTransform;

						if (setPosition)
						{
							runtimeObToAffect.transform.localPosition = newPosition;
						}

						if (setRotation)
						{
							runtimeObToAffect.transform.localRotation = Quaternion.LookRotation (newRotation);
						}
					}
					break;

				case ParentAction.ClearParent:
					if (runtimeObToAffect.transform.parent)
					{
						if (runtimeObToAffect.transform.parent.gameObject.IsPersistent ())
						{
							runtimeObToAffect.transform.parent = null;
							UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene (runtimeObToAffect, KickStarter.kickStarter.gameObject.scene);
						}
						else
						{
							runtimeObToAffect.transform.parent = null;
						}
					}
					break;
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Affect Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					playerParameterID = ChooseParameterGUI ("Player ID:", parameters, playerParameterID, ParameterType.Integer);
					if (playerParameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				obToAffectParameterID = ChooseParameterGUI ("Object to affect:", parameters, obToAffectParameterID, ParameterType.GameObject);
				if (obToAffectParameterID >= 0)
				{
					obToAffectID = 0;
					obToAffect = null;
				}
				else
				{
					obToAffect = (GameObject) EditorGUILayout.ObjectField ("Object to affect:", obToAffect, typeof(GameObject), true);
					
					obToAffectID = FieldToID (obToAffect, obToAffectID);
					obToAffect = IDToField (obToAffect, obToAffectID, false);
				}
			}

			parentAction = (ParentAction) EditorGUILayout.EnumPopup ("Method:", parentAction);
			if (parentAction == ParentAction.SetParent)
			{
				parentTransformParameterID = Action.ChooseParameterGUI ("Parent to:", parameters, parentTransformParameterID, ParameterType.GameObject);
				if (parentTransformParameterID >= 0)
				{
					parentTransformID = 0;
					parentTransform = null;
				}
				else
				{
					parentTransform = (Transform) EditorGUILayout.ObjectField ("Parent to:", parentTransform, typeof(Transform), true);
					
					parentTransformID = FieldToID (parentTransform, parentTransformID);
					parentTransform = IDToField (parentTransform, parentTransformID, false);
				}
			
				setPosition = EditorGUILayout.Toggle ("Set local position?", setPosition);
				if (setPosition)
				{
					newPosition = EditorGUILayout.Vector3Field ("Position vector:", newPosition);
				}
				
				setRotation = EditorGUILayout.Toggle ("Set local rotation?", setRotation);
				if (setRotation)
				{
					newRotation = EditorGUILayout.Vector3Field ("Rotation vector:", newRotation);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberTransform> (obToAffect);
				if (parentTransform != null)
				{
					AddSaveScript <ConstantID> (parentTransform.gameObject);
				}

				if (!isPlayer)
				{
					if (obToAffect != null && obToAffect.GetComponent<RememberTransform> ())
					{
						obToAffect.GetComponent<RememberTransform> ().saveParent = true;

						if (obToAffect.transform.parent)
						{
							AddSaveScript<ConstantID> (obToAffect.transform.parent.gameObject);
						}
					}
				}
			}

			AssignConstantID (obToAffect, obToAffectID, obToAffectParameterID);
			AssignConstantID (parentTransform, parentTransformID, parentTransformParameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (obToAffect != null)
			{
				return obToAffect.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parentAction == ParentAction.SetParent && parentTransformParameterID < 0)
			{
				if (parentTransform && parentTransform.gameObject == gameObject) return true;
				if (parentTransformID == id) return true;
			}
			if (!isPlayer && obToAffectParameterID < 0)
			{
				if (obToAffect && obToAffect == gameObject) return true;
				if (obToAffectID == id && id != 0) return true;
			}
			if (isPlayer && gameObject && gameObject.GetComponent <Player>()) return true;
			return base.ReferencesObjectOrID (gameObject, id);
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
		 * <summary>Creates a new instance of the 'Object: Set parent' Action, set to parent one GameObject to another</summary>
		 * <param name = "objectToParent">The GameObject to affect</param>
		 * <param name = "newParent">The GameObject's new Transform parent</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParent CreateNew_SetParent (GameObject objectToParent, Transform newParent)
		{
			ActionParent newAction = CreateNew<ActionParent> ();
			newAction.parentAction = ParentAction.SetParent;
			newAction.obToAffect = objectToParent;
			newAction.TryAssignConstantID (newAction.obToAffect, ref newAction.obToAffectID);
			newAction.parentTransform = newParent;
			newAction.TryAssignConstantID (newAction.parentTransform, ref newAction.parentTransformID);

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Set parent' Action, set to clear a GameObject's parent</summary>
		 * <param name = "objectToParent">The GameObject to affect</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParent CreateNew_ClearParent (GameObject objectToClear)
		{
			ActionParent newAction = CreateNew<ActionParent> ();
			newAction.parentAction = ParentAction.ClearParent;
			newAction.obToAffect = objectToClear;
			newAction.TryAssignConstantID (newAction.obToAffect, ref newAction.obToAffectID);

			return newAction;
		}

	}

}