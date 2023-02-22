/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionVisibleCheck.cs"
 * 
 *	This action checks the visibilty of a GameObject.
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
	public class ActionVisibleCheck : ActionCheck
	{
		
		public int parameterID = -1;
		public int constantID = 0;
		public GameObject obToAffect;
		protected GameObject runtimeObToAffect;

		public CheckVisState checkVisState = CheckVisState.InScene;


		public override ActionCategory Category { get { return ActionCategory.Object; }}
		public override string Title { get { return "Check visibility"; }}
		public override string Description { get { return "Checks the visibility of a GameObject."; }}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeObToAffect = AssignFile (parameters, parameterID, constantID, obToAffect);
		}


		public override bool CheckCondition ()
		{
			if (runtimeObToAffect)
			{
				SpriteFader _spriteFader = runtimeObToAffect.GetComponent<SpriteFader> ();
				if (_spriteFader && _spriteFader.GetAlpha () <= 0f)
				{
					return false;
				}

				Renderer _renderer = runtimeObToAffect.GetComponent<Renderer> ();
				if (_renderer)
				{
					switch (checkVisState)
					{
						case CheckVisState.InCamera:
							return _renderer.isVisible;

						case CheckVisState.InScene:
							return _renderer.enabled;

						default:
							break;
					}
				}

				Canvas _canvas = runtimeObToAffect.GetComponent<Canvas> ();
				if (_canvas)
				{
					return _canvas.enabled;
				}

				#if UNITY_2019_4_OR_NEWER
				CanvasGroup canvasGroup = runtimeObToAffect.GetComponent<CanvasGroup> ();
				if (canvasGroup)
				{
					return !(canvasGroup.enabled && canvasGroup.alpha <= 0f);
				}
				#endif

				ACDebug.LogWarning ("Cannot check visibility of " + runtimeObToAffect.name + " as it has no renderer component", runtimeObToAffect);
			}
			return false;
		}
		
			
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			parameterID = Action.ChooseParameterGUI ("Object to check:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				obToAffect = null;
			}
			else
			{
				obToAffect = (GameObject) EditorGUILayout.ObjectField ("Object to check:", obToAffect, typeof (GameObject), true);
				
				constantID = FieldToID (obToAffect, constantID);
				obToAffect = IDToField (obToAffect, constantID, false);
			}

			checkVisState = (CheckVisState) EditorGUILayout.EnumPopup ("Visibility to check:", checkVisState);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (obToAffect, constantID, parameterID);
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
			if (parameterID < 0)
			{
				if (obToAffect && obToAffect == gameObject) return true;
				return (constantID == id && id != 0);
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Check visibility' Action</summary>
		 * <param name = "objectToAffect">The object to check</param>
		 * <param name = "visibilityCheck">The visibility state to query</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionVisibleCheck CreateNew (GameObject objectToCheck, CheckVisState visibilityToCheck)
		{
			ActionVisibleCheck newAction = CreateNew<ActionVisibleCheck> ();
			newAction.obToAffect = objectToCheck;
			newAction.TryAssignConstantID (newAction.obToAffect, ref newAction.constantID);
			newAction.checkVisState = visibilityToCheck;
			return newAction;
		}

	}
	
}