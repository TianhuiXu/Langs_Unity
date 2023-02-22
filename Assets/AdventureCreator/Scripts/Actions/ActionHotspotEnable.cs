/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionHotspotEnable.cs"
 * 
 *	This Action can enable and disable a Hotspot.
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
	public class ActionHotspotEnable : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		public Hotspot hotspot;
		protected Hotspot runtimeHotspot;
		public bool affectChildren = false;

		public ChangeType changeType = ChangeType.Enable;

		
		public override ActionCategory Category { get { return ActionCategory.Hotspot; }}
		public override string Title { get { return "Enable or disable"; }}
		public override string Description { get { return "Turns a Hotspot on or off. To record the state of a Hotspot in save games, be sure to add the RememberHotspot script to the Hotspot in question."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeHotspot = AssignFile <Hotspot> (parameters, parameterID, constantID, hotspot);
		}

		
		public override float Run ()
		{
			if (runtimeHotspot == null)
			{
				return 0f;
			}

			DoChange (runtimeHotspot);

			if (affectChildren)
			{
				Hotspot[] hotspots = runtimeHotspot.GetComponentsInChildren <Hotspot>();
				foreach (Hotspot _hotspot in hotspots)
				{
					if (_hotspot != runtimeHotspot)
					{
						DoChange (_hotspot);
					}
				}
			}

			return 0f;
		}


		protected void DoChange (Hotspot _hotspot)
		{
			if (changeType == ChangeType.Enable)
			{
				_hotspot.TurnOn ();
			}
			else
			{
				_hotspot.TurnOff ();
			}
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Hotspot to affect:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				hotspot = null;
			}
			else
			{
				hotspot = (Hotspot) EditorGUILayout.ObjectField ("Hotspot to affect:", hotspot, typeof (Hotspot), true);
				
				constantID = FieldToID <Hotspot> (hotspot, constantID);
				hotspot = IDToField <Hotspot> (hotspot, constantID, false);
			}

			changeType = (ChangeType) EditorGUILayout.EnumPopup ("Change to make:", changeType);
			affectChildren = EditorGUILayout.Toggle ("Also affect children?", affectChildren);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberHotspot> (hotspot);
			}
			AssignConstantID <Hotspot> (hotspot, constantID, parameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (hotspot != null)
			{
				return hotspot.name + " - " + changeType;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (hotspot && hotspot.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Hotspot: Enable or disable' Action</summary>
		 * <param name = "hotspotToAffect">The Hotspot to affect</param>
		 * <param name = "changeToMake">The type of change to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionHotspotEnable CreateNew (Hotspot hotspotToAffect, ChangeType changeToMake)
		{
			ActionHotspotEnable newAction = CreateNew<ActionHotspotEnable> ();
			newAction.hotspot = hotspotToAffect;
			newAction.TryAssignConstantID (newAction.hotspot, ref newAction.constantID);
			newAction.changeType = changeToMake;
			return newAction;
		}
		
	}

}