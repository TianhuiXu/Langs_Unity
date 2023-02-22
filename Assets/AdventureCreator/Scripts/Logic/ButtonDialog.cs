/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ButtonDialog.cs"
 * 
 *	This script is a container class for dialogue options
 *	that are linked to Conversations.
 * 
 */

using UnityEngine;

namespace AC
{

	/** A data container for a dialogue option within a Conversation. */
	[System.Serializable]
	public class ButtonDialog
	{

		#region Variables

		/** The option's display label */
		public string label = "(Not set)";
		/** The translation ID number of the display label, as set by SpeechManager */
		public int lineID = -1;
		/** Deprecated */
		public Texture2D icon;
		/** The option's display icon */
		public CursorIconBase cursorIcon;
		/** If True, the option is enabled, and will be displayed in a MenuDialogList element */
		public bool isOn;
		/** If True, the option is locked, and cannot be enabled or disabled */
		public bool isLocked;
		/** What happens when the DialogueOption ActionList has finished (ReturnToConversation, Stop, RunOtherConversation) */
		public ConversationAction conversationAction;
		/** The new Conversation to run, if conversationAction = ConversationAction.RunOtherConversation */
		public Conversation newConversation;
		/** An ID number unique to this instance of ButtonDialog within a Conversation */
		public int ID = 0;
		/** If True, then the option has been chosen at least once by the player */
		public bool hasBeenChosen = false;
		/** If True, then the option will be disabled once chosen by the player */
		public bool autoTurnOff = false;

		/** If True, then the option will only be visible if a given inventory item is being carried */
		public bool linkToInventory = false;
		/** The ID number of the associated inventory item, if linkToInventory = True */
		public int linkedInventoryID = 0;

		/** The DialogueOption ActionList to run, if the Conversation's interactionSource = InteractionSource.InScene */
		public DialogueOption dialogueOption;
		/** The ActionListAsset to run, if the Conversation's interactionSource = InteractionSource.AssetFile */
		public ActionListAsset assetFile = null;

		/** The GameObject with the custom script to run, if the Conversation's interactionSource = InteractionSource.CustomScript */
		public GameObject customScriptObject = null;
		/** The name of the function to run, if the Conversation's interactionSource = InteractionSource.CustomScript */
		public string customScriptFunction = "";

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of existing ID numbers, so that a unique ID number can be assigned</param>
		 */
		public ButtonDialog (int[] idArray)
		{
			label = "";
			icon = null;
			cursorIcon = new CursorIconBase ();
			isOn = true;
			isLocked = false;
			conversationAction = ConversationAction.ReturnToConversation;
			assetFile = null;
			newConversation = null;
			dialogueOption = null;
			lineID = -1;
			ID = 1;

			// Update id based on array
			foreach (int _id in idArray)
			{
				if (ID == _id)
				{
					ID ++;
				}
			}
		}


		/**
		 * <summary>A constructor for a scene-based Conversation's dialogue option.</summary>
		 * <param name = "_ID">An ID number unique to this instance of ButtonDialog within a Conversation</param>
		 * <param name = "_label">The option's display text</param>
		 * <param name = "_startEnabled">If True, the option will be enabled by default</param>
		 * <param name = "_dialogueOption">The DialogueOption to run when the option is chosen</param>
		 * <param name = "_endsConversation">If True, the Conversation will end after the DialogueOption has finished running</param>
		 */
		public ButtonDialog (int _ID, string _label, bool _startEnabled, DialogueOption _dialogueOption, bool _endsConversation)
		{
			label = _label;
			icon = null;
			cursorIcon = new CursorIconBase ();
			isOn = _startEnabled;
			isLocked = false;

			conversationAction = (_endsConversation) ? ConversationAction.Stop : ConversationAction.ReturnToConversation;

			assetFile = null;
			newConversation = null;
			dialogueOption = _dialogueOption;
			lineID = -1;
			ID = _ID;
		}


		/**
		 * <summary>A constructor for an asset-based Conversation's dialogue option.</summary>
		 * <param name = "_ID">An ID number unique to this instance of ButtonDialog within a Conversation</param>
		 * <param name = "_label">The option's display text</param>
		 * <param name = "_startEnabled">If True, the option will be enabled by default</param>
		 * <param name = "_actionListAsset">The ActionListAsset to run when the option is chosen</param>
		 * <param name = "_endsConversation">If True, the Conversation will end after the ActionListAsset has finished running</param>
		 */
		public ButtonDialog (int _ID, string _label, bool _startEnabled, ActionListAsset _actionListAsset, bool _endsConversation)
		{
			label = _label;
			icon = null;
			cursorIcon = new CursorIconBase ();
			isOn = _startEnabled;
			isLocked = false;

			conversationAction = (_endsConversation) ? ConversationAction.Stop : ConversationAction.ReturnToConversation;

			assetFile = _actionListAsset;
			newConversation = null;
			dialogueOption = null;
			lineID = -1;
			ID = _ID;
		}


		/**
		 * <summary>A constructor for a runtime-generated Conversation's dialogue option.</summary>
		 * <param name = "_ID">An ID number unique to this instance of ButtonDialog within a Conversation</param>
		 * <param name = "_label">The option's display text</param>
		 * <param name = "_isOn">If True, the option will be enabled</param>
		 */
		public ButtonDialog (int _ID, string _label, bool _isOn)
		{
			label = _label;
			icon = null;
			cursorIcon = new CursorIconBase ();
			isOn = _isOn;
			isLocked = false;

			conversationAction = ConversationAction.Stop;

			assetFile = null;
			newConversation = null;
			dialogueOption = null;
			lineID = -1;
			ID = _ID;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if the dialogue option can be currently shown.</summary>
		 * <returns>True if the dialogue option can be currently shown</returns>
		 */
		public bool CanShow ()
		{
			if (isOn)
			{
				if (!linkToInventory)
				{
					return true;
				}

				if (linkToInventory && KickStarter.runtimeInventory != null && KickStarter.runtimeInventory.IsCarryingItem (linkedInventoryID))
				{
					return true;
				}
			}
			return false;
		}


		/**
		 * <summary>Upgrades the class to make use of cursorIcon instead of icon as the texture to display</summary>
		 * <returns>True if the class was upgraded</returns>
		 */
		public bool Upgrade ()
		{
			if (icon != null)
			{
				if (cursorIcon.texture == null)
				{
					cursorIcon.texture = icon;
					return true;
				}
				icon = null;
			}
			return false;
		}

		#endregion

	}

}