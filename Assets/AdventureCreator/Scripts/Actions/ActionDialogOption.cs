/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionDialogOption.cs"
 * 
 *	This action changes the visibility of dialogue options.
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
	public class ActionDialogOption : Action
	{
		
		public enum SwitchType { On, Off, OnForever, OffForever };
		public SwitchType switchType;
		public int optionNumber; // This is now the ID number minus one

		public int constantID;
		public Conversation linkedConversation;
		protected Conversation runtimeLinkedConversation;
		
		
		public override ActionCategory Category { get { return ActionCategory.Dialogue; }}
		public override string Title { get { return "Toggle option"; }}
		public override string Description { get { return "Sets the display of a dialogue option. Can hide, show, and lock options."; }}


		public override void AssignValues ()
		{
			runtimeLinkedConversation = AssignFile <Conversation> (constantID, linkedConversation);
		}

		
		public override float Run ()
		{
			if (runtimeLinkedConversation)
			{
				bool setOption = false;
				if (switchType == SwitchType.On || switchType == SwitchType.OnForever)
				{
					setOption = true;
				}
				
				bool clampOption = false;
				if (switchType == SwitchType.OffForever || switchType == SwitchType.OnForever)
				{
					clampOption = true;
				}

				runtimeLinkedConversation.SetOptionState (optionNumber+1, setOption, clampOption);
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			linkedConversation = (Conversation) EditorGUILayout.ObjectField ("Conversation:", linkedConversation, typeof (Conversation), true);

			constantID = FieldToID <Conversation> (linkedConversation, constantID);
			linkedConversation = IDToField <Conversation> (linkedConversation, constantID, true);

			if (linkedConversation)
			{
				optionNumber = ShowOptionGUI (linkedConversation.options, optionNumber);
			}
			if (linkedConversation != null || constantID != 0)
			{
				switchType = (SwitchType) EditorGUILayout.EnumPopup ("Set to:", switchType);
			}
		}


		private int ShowOptionGUI (List<ButtonDialog> options, int optionID)
		{
			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			
			int i = 0;
			int tempNumber = -1;

			if (options.Count > 0)
			{
				foreach (ButtonDialog option in options)
				{
					string label = option.ID.ToString () + ": " + option.label;
					if (option.label == "")
					{
						label += "(Untitled option)";
					}
					labelList.Add (label);
					
					if (option.ID == (optionID+1))
					{
						tempNumber = i;
					}
					
					i ++;
				}
				
				if (tempNumber == -1)
				{
					// Wasn't found (variable was deleted?), so revert to zero
					if (optionID > 0) LogWarning ("Previously chosen option no longer exists!");
					tempNumber = 0;
					optionID = 0;
				}

				tempNumber = EditorGUILayout.Popup (tempNumber, labelList.ToArray());
				optionID = options [tempNumber].ID-1;
			}
			else
			{
				EditorGUILayout.HelpBox ("No options exist!", MessageType.Info);
				optionID = -1;
				tempNumber = -1;
			}
			
			return optionID;
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberConversation> (linkedConversation);
			}
			AssignConstantID <Conversation> (linkedConversation, constantID, 0);
		}


		public override string SetLabel ()
		{
			if (linkedConversation != null)
			{
				return linkedConversation.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (linkedConversation && linkedConversation.gameObject == _gameObject) return true;
			if (constantID == id) return true;
			return base.ReferencesObjectOrID (_gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Conversation: Toggle option' Action</summary>
		 * <param name = "conversationToModify">The Conversation to modify</param>
		 * <param name = "dialogueOptionID">The ID number of the dialogue option to toggle</param>
		 * <param name = "optionSwitchType">How to affect the option</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionDialogOption CreateNew (Conversation conversationToModify, int dialogueOptionID, SwitchType optionSwitchType)
		{
			ActionDialogOption newAction = CreateNew<ActionDialogOption> ();
			newAction.linkedConversation = conversationToModify;
			newAction.TryAssignConstantID (newAction.linkedConversation, ref newAction.constantID);
			newAction.optionNumber = dialogueOptionID-1;
			newAction.switchType = optionSwitchType;
			return newAction;
		}
		
	}

}