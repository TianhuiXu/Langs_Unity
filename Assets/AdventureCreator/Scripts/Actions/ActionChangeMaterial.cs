/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionChangeMaterial.cs"
 * 
 *	This Action allows you to change an object's material.
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
	public class ActionChangeMaterial : Action
	{

		public int constantID = 0;
		public int parameterID = -1;

		public bool isPlayer;
		public int playerID = -1;

		public GameObject obToAffect;
		protected GameObject runtimeObToAffect;

		public int materialIndex;
		public Material newMaterial;
		public int newMaterialParameterID = -1;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Change material"; }}
		public override string Description { get { return "Changes the material on any scene-based mesh object."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				Player player = AssignPlayer (playerID, parameters, parameterID);
				runtimeObToAffect = GetPlayerRenderer (player);
			}
			else
			{
				runtimeObToAffect = AssignFile (parameters, parameterID, constantID, obToAffect);
			}

			newMaterial = (Material) AssignObject <Material> (parameters, newMaterialParameterID, newMaterial);
		}

		
		public override float Run ()
		{
			if (runtimeObToAffect && newMaterial)
			{
				Renderer _renderer = runtimeObToAffect.GetComponent <Renderer>();
				if (_renderer != null)
				{
					Material[] mats = _renderer.materials;
					mats[materialIndex] = newMaterial;
					runtimeObToAffect.GetComponent <Renderer>().materials = mats;
				}
			}
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Affect player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					parameterID = ChooseParameterGUI ("Player ID:", parameters, parameterID, ParameterType.Integer);
					if (parameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("Object to affect:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					obToAffect = null;
				}
				else
				{
					obToAffect = (GameObject) EditorGUILayout.ObjectField ("Renderer:", obToAffect, typeof (GameObject), true);
					
					constantID = FieldToID (obToAffect, constantID);
					obToAffect = IDToField (obToAffect, constantID, true, false);
				}
			}

			materialIndex = EditorGUILayout.IntSlider ("Material index:", materialIndex, 0, 10);

			newMaterialParameterID = Action.ChooseParameterGUI ("New material:", parameters, newMaterialParameterID, ParameterType.UnityObject);
			if (newMaterialParameterID < 0)
			{
				newMaterial = (Material) EditorGUILayout.ObjectField ("New material:", newMaterial, typeof (Material), false);
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			GameObject obToUpdate = obToAffect;

			if (isPlayer && (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow))
			{
				if (!fromAssetFile && GameObject.FindObjectOfType <Player>() != null)
				{
					obToUpdate = GetPlayerRenderer (GameObject.FindObjectOfType <Player>());
				}

				if (obToUpdate == null && AdvGame.GetReferences ().settingsManager != null)
				{
					Player player = AdvGame.GetReferences ().settingsManager.GetDefaultPlayer ();
					obToUpdate = GetPlayerRenderer (player);
				}
			}

			if (saveScriptsToo)
			{
				AddSaveScript <RememberMaterial> (obToUpdate);
			}
			AssignConstantID (obToUpdate, constantID, parameterID);
		}


		public override string SetLabel ()
		{
			if (obToAffect != null)
			{
				string labelAdd = obToAffect.gameObject.name;
				if (newMaterial != null)
				{
					labelAdd += " - " + newMaterial;
				}
				return labelAdd;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (obToAffect && obToAffect == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (!isPlayer) return false;
			if (_playerID < 0) return true;
			if (playerID < 0 && parameterID < 0) return true;
			return (parameterID < 0 && playerID == _playerID);
		}

		#endif


		protected GameObject GetPlayerRenderer (Player player)
		{
			if (player == null)
			{
				return null;
			}

			if (player.spriteChild && player.spriteChild.GetComponent <Renderer>())
			{
			    return player.spriteChild.gameObject;
			}

			if (player.GetComponentInChildren <Renderer>())
			{
				return player.gameObject.GetComponentInChildren <Renderer>().gameObject;
			}
			else
			{
				return player.gameObject;
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Change material' Action</summary>
		 * <param name = "renderer">The renderer with the material</param>
		 * <param name = "newMaterial">The new material to assign</param>
		 * <param name = "materialIndex">The index number of the renderer's materials to replace</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionChangeMaterial CreateNew (Renderer renderer, Material newMaterial, int materialIndex = 0)
		{
			ActionChangeMaterial newAction = CreateNew<ActionChangeMaterial> ();
			newAction.obToAffect = (renderer != null) ? renderer.gameObject : null;
			newAction.TryAssignConstantID (newAction.obToAffect, ref newAction.constantID);
			newAction.newMaterial = newMaterial;
			newAction.materialIndex = materialIndex;
			return newAction;
		}

	}
	
}