/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionInputCheck.cs"
 * 
 *	This action checks if a specific key
 *	is being pressed
 * 
 */

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionInputCheck : ActionCheck
	{
		
		public string inputName;
		public int parameterID = -1;
		
		public InputCheckType checkType = InputCheckType.Button;
		
		public IntCondition axisCondition;
		public float axisValue;
		
		
		public override ActionCategory Category { get { return ActionCategory.Input; }}
		public override string Title { get { return "Check"; }}
		public override string Description { get { return "Queries whether or not the player is invoking a button or axis declared in Unity's Input manager."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			inputName = AssignString (parameters, parameterID, inputName);
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				return defaultPauseTime / 6f;
			}
			else
			{
				isRunning = false;
				return 0f;
			}
		}
		
		
		public override bool CheckCondition ()
		{
			switch (checkType)
			{
				case InputCheckType.SingleTapOrClick:
					return KickStarter.playerInput.ClickedRecently ();

				case InputCheckType.DoubleTapOrClick:
					return KickStarter.playerInput.ClickedRecently (true);

				case InputCheckType.Button:
					if (inputName != "" && KickStarter.playerInput.InputGetButton (inputName))
					{
						return true;
					}
					break;
					
				case InputCheckType.Axis:
					if (inputName != "")
					{
						return CheckAxisValue (KickStarter.playerInput.InputGetAxis (inputName));
					}
					break;
			}
			return false;
		}
		
		
		protected bool CheckAxisValue (float fieldValue)
		{
			if (axisCondition == IntCondition.EqualTo)
			{
				if (Mathf.Approximately (fieldValue, axisValue))
				{
					return true;
				}
			}
			else if (axisCondition == IntCondition.NotEqualTo)
			{
				if (!Mathf.Approximately (fieldValue, axisValue))
				{
					return true;
				}
			}
			else if (axisCondition == IntCondition.LessThan)
			{
				if (fieldValue < axisValue)
				{
					return true;
				}
			}
			else if (axisCondition == IntCondition.MoreThan)
			{
				if (fieldValue > axisValue)
				{
					return true;
				}
			}
			
			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			checkType = (InputCheckType) EditorGUILayout.EnumPopup ("Check type:" , checkType);
			
			if (checkType == InputCheckType.Axis || checkType == InputCheckType.Button)
			{
				parameterID = Action.ChooseParameterGUI (checkType.ToString () + " name:", parameters, parameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
				if (parameterID < 0)
				{
					inputName = EditorGUILayout.TextField (checkType.ToString () + " name:", inputName);
				}
				
				if (checkType == InputCheckType.Axis)
				{
					EditorGUILayout.BeginHorizontal ();
					axisCondition = (IntCondition) EditorGUILayout.EnumPopup (axisCondition);
					axisValue = EditorGUILayout.FloatField (axisValue);
					EditorGUILayout.EndHorizontal ();
				}
			}
		}
		
		
		public override string SetLabel ()
		{
			return inputName;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Input: Check' Action, set to check if a button is pressed</summary>
		 * <param name = "buttonName">The button to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInputCheck CreateNew_Button (string buttonName)
		{
			ActionInputCheck newAction = CreateNew<ActionInputCheck> ();
			newAction.checkType = InputCheckType.Button;
			newAction.inputName = buttonName;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Input: Check' Action, set to check if a axis is held down</summary>
		 * <param name = "axisName">The axis to check for</param>
		 * <param name = "axisValue">The axis value to check for</param>
		 * <param name = "condition">The condition to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInputCheck CreateNew_Axis (string axisName, float axisValue = 0.2f, IntCondition condition = IntCondition.MoreThan)
		{
			ActionInputCheck newAction = CreateNew<ActionInputCheck> ();
			newAction.checkType = InputCheckType.Axis;
			newAction.inputName = axisName;
			newAction.axisValue = axisValue;
			newAction.axisCondition = condition;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Input: Check' Action, set to check if a tap or click is being made</summary>
		 * <param name = "requireDoubleClick">If True, it must be a double-tap / double-click</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInputCheck CreateNew_TapOrClick (bool requireDoubleClick = false)
		{
			ActionInputCheck newAction = CreateNew<ActionInputCheck> ();
			newAction.checkType = (requireDoubleClick) ? InputCheckType.DoubleTapOrClick : InputCheckType.SingleTapOrClick;
			return newAction;
		}
		
	}
	
}