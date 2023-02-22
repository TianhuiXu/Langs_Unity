/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCharRender.cs"
 * 
 *	This Action overrides Character
 *	render settings.
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
	public class ActionCharRender : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		public bool isPlayer;
		public int playerID = -1;
		public Char _char;
		protected Char runtimeChar;

		public RenderLock renderLock_sorting;
		public SortingMapType mapType;

		public int sortingOrder;
		public int sortingOrderParameterID = -1;
		public string sortingLayer;
		public int sortingLayerParameterID = -1;

		public RenderLock renderLock_scale;
		public int scale;
		public int scaleParameterID = -1;

		public RenderLock renderLock_direction;
		public int directionParameterID = -1;
		public CharDirection direction;

		public RenderLock renderLock_sortingMap;

		public SortingMap sortingMap;
		public int sortingMapConstantID = 0;
		public int sortingMapParameterID = -1;

		protected SortingMap runtimeSortingMap;

		public bool setNewDirections = false;
		public SpriteDirectionData spriteDirectionData = new SpriteDirectionData (true, true);


		public override ActionCategory Category { get { return ActionCategory.Character; }}
		public override string Title { get { return "Change rendering"; }}
		public override string Description { get { return "Overrides a Character's scale, sorting order, sprite direction or Sorting Map. This is intended mainly for 2D games."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (isPlayer)
			{
				runtimeChar = AssignPlayer (playerID, parameters, parameterID);
			}
			else
			{
				runtimeChar = AssignFile <Char> (parameters, parameterID, constantID, _char);
			}

			sortingOrder = AssignInteger (parameters, sortingOrderParameterID, sortingOrder);
			sortingLayer = AssignString (parameters, sortingLayerParameterID, sortingLayer);
			runtimeSortingMap = AssignFile <SortingMap> (parameters, sortingMapParameterID, sortingMapConstantID, sortingMap);
			scale = AssignInteger (parameters, scaleParameterID, scale);

			if (directionParameterID >= 0)
			{
				int _directionInt = AssignInteger (parameters, directionParameterID, 0);
				direction = (CharDirection) _directionInt;
			}
		}
		
		
		public override float Run ()
		{
			if (runtimeChar != null)
			{
				if (renderLock_sorting == RenderLock.Set)
				{
					if (mapType == SortingMapType.OrderInLayer)
					{
						runtimeChar.SetSorting (sortingOrder);
					}
					else if (mapType == SortingMapType.SortingLayer)
					{
						runtimeChar.SetSorting (sortingLayer);
					}
				}
				else if (renderLock_sorting == RenderLock.Release)
				{
					runtimeChar.ReleaseSorting ();
				}

				if (runtimeChar.GetAnimEngine () != null)
				{
					runtimeChar.GetAnimEngine ().ActionCharRenderRun (this);
				}
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					parameterID = ChooseParameterGUI ("Player ID:", parameters, parameterID, ParameterType.Integer);
					if (parameterID < 0)
						playerID = ChoosePlayerGUI (playerID, true);
				}

				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					if (parameterID < 0 && playerID >= 0)
					{
						PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (playerID);
						_char = (playerPrefab != null) ? playerPrefab.playerOb : null;
					}
					else
					{
						_char = KickStarter.settingsManager.GetDefaultPlayer ();
					}
				}
				else if (Application.isPlaying)
				{
					_char = KickStarter.player;
				}
				else
				{
					_char = KickStarter.settingsManager.GetDefaultPlayer ();
				}
			}
			else
			{
				parameterID = Action.ChooseParameterGUI ("Character:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					_char = null;
				}
				else
				{
					_char = (Char) EditorGUILayout.ObjectField ("Character:", _char, typeof (Char), true);
					
					constantID = FieldToID <Char> (_char, constantID);
					_char = IDToField <Char> (_char, constantID, false);
				}
			}

			if (_char)
			{
				EditorGUILayout.Space ();
				renderLock_sorting = (RenderLock) EditorGUILayout.EnumPopup ("Sorting:", renderLock_sorting);
				if (renderLock_sorting == RenderLock.Set)
				{
					mapType = (SortingMapType) EditorGUILayout.EnumPopup ("Sorting type:", mapType);
					if (mapType == SortingMapType.OrderInLayer)
					{
						sortingOrderParameterID = Action.ChooseParameterGUI ("New order:", parameters, sortingOrderParameterID, ParameterType.Integer);
						if (sortingOrderParameterID < 0)
						{
							sortingOrder = EditorGUILayout.IntField ("New order:", sortingOrder);
						}

					}
					else if (mapType == SortingMapType.SortingLayer)
					{
						sortingLayerParameterID = Action.ChooseParameterGUI ("New layer:", parameters, sortingLayerParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
						if (sortingLayerParameterID < 0)
						{
							sortingLayer = EditorGUILayout.TextField ("New layer:", sortingLayer);
						}
					}
				}

				if (_char.GetAnimEngine ())
				{
					_char.GetAnimEngine ().ActionCharRenderGUI (this, parameters);

					#if !AC_ActionListPrefabs
					if (GUI.changed && this) EditorUtility.SetDirty (this);
					#endif
				}
			}
			else
			{
				EditorGUILayout.HelpBox ("This Action requires a Character before more options will show.", MessageType.Info);
			}

			EditorGUILayout.Space ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (!isPlayer)
			{
				if (saveScriptsToo)
				{
					if (!isPlayer && _char != null && !_char.IsPlayer)
					{
						AddSaveScript <RememberNPC> (_char);
					}
				}

				AssignConstantID <Char> (_char, constantID, parameterID);
			}
		}
		
		
		public override string SetLabel ()
		{
			if (isPlayer)
			{
				return "Player";
			}
			else if (_char != null)
			{
				return "_char.name";
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!isPlayer && parameterID < 0)
			{
				if (_char && _char.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (isPlayer && _gameObject && _gameObject.GetComponent <Player>() != null) return true;
			if (sortingMapParameterID < 0)
			{
				if (sortingMap && sortingMap.gameObject == _gameObject) return true;
				if (sortingMapConstantID == id) return true;
			}
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


		public SortingMap RuntimeSortingMap
		{
			get
			{
				return runtimeSortingMap;
			}
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Render' Action, set to update a Mecanim-based character</summary>
		 * <param name = "characterToAffect">The character to affect</param>
		 * <param name = "sortingLock">Whether or not to lock the character's sorting</param>
		 * <param name = "newSortingOrder">The new sorting order, if locking the character's sorting</param>
		 * <param name = "scaleLock">Whether of not to lock the character's scale</param>
		 * <param name = "newScale">The new scale, as a percentage, if locking the character's scale</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharRender CreateNew_Mecanim (AC.Char characterToAffect, RenderLock sortingLock, int newSortingOrder, RenderLock scaleLock, int newScale)
		{
			ActionCharRender newAction = CreateNew<ActionCharRender> ();
			newAction._char = characterToAffect;
			newAction.TryAssignConstantID (newAction._char, ref newAction.constantID);

			newAction.renderLock_sorting = sortingLock;
			newAction.mapType = SortingMapType.OrderInLayer;
			newAction.sortingOrder = newSortingOrder;

			newAction.renderLock_scale = scaleLock;
			newAction.scale = newScale;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Render' Action, set to update a Mecanim-based character</summary>
		 * <param name = "characterToAffect">The character to affect</param>
		 * <param name = "sortingLock">Whether or not to lock the character's sorting</param>
		 * <param name = "newSortingLayer">The new sorting layer, if locking the character's sorting</param>
		 * <param name = "scaleLock">Whether of not to lock the character's scale</param>
		 * <param name = "newScale">The new scale, as a percentage, if locking the character's scale</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharRender CreateNew_Mecanim (AC.Char characterToAffect, RenderLock sortingLock, string newSortingLayer, RenderLock scaleLock, int newScale)
		{
			ActionCharRender newAction = CreateNew<ActionCharRender> ();
			newAction._char = characterToAffect;
			newAction.TryAssignConstantID (newAction._char, ref newAction.constantID);

			newAction.renderLock_sorting = sortingLock;
			newAction.mapType = SortingMapType.SortingLayer;
			newAction.sortingLayer = newSortingLayer;

			newAction.renderLock_scale = scaleLock;
			newAction.scale = newScale;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Render' Action, set to update a sprite-based character</summary>
		 * <param name = "characterToAffect">The character to affect</param>
		 * <param name = "sortingLock">Whether or not to lock the character's sorting</param>
		 * <param name = "newSortingOrder">The new sorting order, if locking the character's sorting</param>
		 * <param name = "scaleLock">Whether of not to lock the character's scale</param>
		 * <param name = "newScale">The new scale, as a percentage, if locking the character's scale</param>
		 * <param name = "directionLock">Whether or not to lock the character's facing direction</parm>
		 * <param name = "newDirection">The new direction, if locking the character's facing direction</param>
		 * <param name = "sortingMapLock">Whether or not to lock the character's current SortingMap</parm>
		 * <param name = "newSortingMap">The new SortingMap, if locking the character's facing direction</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharRender CreateNew_Sprites (AC.Char characterToAffect, RenderLock sortingLock, int newSortingOrder, RenderLock scaleLock, int newScale, RenderLock directionLock, CharDirection newDirection, RenderLock sortingMapLock, SortingMap newSortingMap)
		{
			ActionCharRender newAction = CreateNew<ActionCharRender> ();
			newAction._char = characterToAffect;
			newAction.TryAssignConstantID (newAction._char, ref newAction.constantID);

			newAction.renderLock_sorting = sortingLock;
			newAction.mapType = SortingMapType.OrderInLayer;
			newAction.sortingOrder = newSortingOrder;

			newAction.renderLock_scale = scaleLock;
			newAction.scale = newScale;

			newAction.renderLock_direction = directionLock;
			newAction.direction = newDirection;
			newAction.renderLock_sortingMap = sortingMapLock;
			newAction.sortingMap = newSortingMap;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Character: Render' Action, set to update a sprite-based character</summary>
		 * <param name = "characterToAffect">The character to affect</param>
		 * <param name = "sortingLock">Whether or not to lock the character's sorting</param>
		 * <param name = "newSortingLayer">The new sorting layer, if locking the character's sorting</param>
		 * <param name = "scaleLock">Whether of not to lock the character's scale</param>
		 * <param name = "newScale">The new scale, as a percentage, if locking the character's scale</param>
		 * <param name = "directionLock">Whether or not to lock the character's facing direction</parm>
		 * <param name = "newDirection">The new direction, if locking the character's facing direction</param>
		 * <param name = "sortingMapLock">Whether or not to lock the character's current SortingMap</parm>
		 * <param name = "newSortingMap">The new SortingMap, if locking the character's facing direction</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCharRender CreateNew_Sprites (AC.Char characterToAffect, RenderLock sortingLock, string newSortingLayer, RenderLock scaleLock, int newScale, RenderLock directionLock, CharDirection newDirection, RenderLock sortingMapLock, SortingMap newSortingMap)
		{
			ActionCharRender newAction = CreateNew<ActionCharRender> ();
			newAction._char = characterToAffect;
			newAction.TryAssignConstantID (newAction._char, ref newAction.constantID);
			newAction.renderLock_sorting = sortingLock;
			newAction.mapType = SortingMapType.SortingLayer;
			newAction.sortingLayer = newSortingLayer;

			newAction.renderLock_scale = scaleLock;
			newAction.scale = newScale;

			newAction.renderLock_direction = directionLock;
			newAction.direction = newDirection;
			newAction.renderLock_sortingMap = sortingMapLock;
			newAction.sortingMap = newSortingMap;

			return newAction;
		}

	}

}