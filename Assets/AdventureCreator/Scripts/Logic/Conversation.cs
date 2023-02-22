/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Conversation.cs"
 * 
 *	This script is handles character conversations.
 *	It generates instances of DialogOption for each line
 *	that the player can choose to say.
 * 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This component provides the player with a list of dialogue options that their character can say.
	 * Options are display in a MenuDialogList element, and will usually run a DialogueOption ActionList when clicked - unless overrided by the "Dialogue: Start conversation" Action that triggers it.
	 */
	[AddComponentMenu("Adventure Creator/Logic/Conversation")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_conversation.html")]
	public class Conversation : MonoBehaviour, ITranslatable, iActionListAssetReferencer, IItemReferencer, IVariableReferencer
	{

		#region Variables

		/** The source of the commands that are run when an option is chosen (InScene, AssetFile, CustomScript) */	
		public AC.InteractionSource interactionSource;
		/** All available dialogue options that make up the Conversation */
		public List<ButtonDialog> options = new List<ButtonDialog>();
		/** The option selected within the Conversation's Inspector  */
		public ButtonDialog selectedOption;

		/** The index number of the last-chosen Conversation dialogue option */
		public int lastOption = -1;

		/** If True, and only one option is available, then the option will be chosen automatically */
		public bool autoPlay = false;
		/** If True, then the Conversation is timed, and the options will only be shown for a fixed period */
		public bool isTimed = false;
		/** The duration, in seconds, that the Conversation is active, if isTime = True */
		public float timer = 5f;
		/** The index number of the option to select, if isTimed = True and the timer runs out before the player has made a choice. If -1, then the conversation will end */
		public int defaultOption = 0;

		protected float startTime;
		protected ActiveList overrideActiveList;
		protected ActiveList onFinishActiveList;
		protected MenuDialogList linkedDialogList;

		#endregion


		#region UnityStandards

		private void OnEnable ()
		{
			EventManager.OnEndActionList += OnEndActionList;
			EventManager.OnEndConversation += OnEndConversation;
			EventManager.OnFinishLoading += OnFinishLoading;
		}


		private void OnDisable ()
		{
			EventManager.OnEndActionList -= OnEndActionList;
			EventManager.OnEndConversation -= OnEndConversation;
			EventManager.OnFinishLoading -= OnFinishLoading;
		}

		private void Start ()
		{
			if (KickStarter.inventoryManager)
			{
				foreach (ButtonDialog option in options)
				{
					if (option.linkToInventory && option.cursorIcon.texture == null)
					{
						InvItem linkedItem = KickStarter.inventoryManager.GetItem (option.linkedInventoryID);
						if (linkedItem != null && linkedItem.tex != null)
						{
							option.cursorIcon.ReplaceTexture (linkedItem.tex);
						}
					}
				}
			}
		}

		#endregion


		#region PublicFunctions

		/** Show the Conversation's dialogue options. */
		public void Interact ()
		{
			Interact (null, null);
		}


		/**
		 * <summary>Shows the Conversation's dialogue options.</summary>
		 * <param name = "actionList">The ActionList that contains the Action that calls this function</param>
		 * <param name = "actionConversation">The "Dialogue: Start conversation" Action that calls this function.  This is necessary when that Action overrides the Converstion's options.</param>
		 */
		public void Interact (ActionList actionList, ActionConversation actionConversation)
		{
			overrideActiveList = null;
			
			if (actionList)
			{
				onFinishActiveList = null;

				int actionIndex = actionList.actions.IndexOf (actionConversation);
				if (actionList && actionIndex >= 0 && actionIndex < actionList.actions.Count)
				{
					if (actionConversation.overrideOptions)
					{
						overrideActiveList = new ActiveList (actionList, true, actionIndex);
						actionList.ResetList ();
					}
					else if (actionConversation.willWait && !KickStarter.settingsManager.allowGameplayDuringConversations && actionConversation.endings.Count > 0)
					{
						ActionEnd ending = actionConversation.endings[0];
						if (ending.resultAction != ResultAction.Stop)
						{
							switch (ending.resultAction)
							{
								case ResultAction.Continue:
									if (actionIndex < actionList.actions.Count - 1)
									{
										onFinishActiveList = new ActiveList (actionList, true, actionIndex + 1);
									}
									break;

								case ResultAction.Skip:
									onFinishActiveList = new ActiveList (actionList, true, ending.skipAction);
									break;

								case ResultAction.RunCutscene:
									if (actionList is RuntimeActionList)
									{
										if (ending.linkedAsset)
										{
											onFinishActiveList = new ActiveList (null, true, 0);
											onFinishActiveList.actionListAsset = ending.linkedAsset;
										}
									}
									else if (ending.linkedCutscene)
									{
										onFinishActiveList = new ActiveList (ending.linkedCutscene, true, 0);
									}
									break;

								default:
									break;
							}
						}
					}
				}
			}

			KickStarter.eventManager.Call_OnStartConversation (this);

			CancelInvoke ("RunDefault");
			int numPresent = 0;
			foreach (ButtonDialog _option in options)
			{
				if (_option.CanShow ())
				{
					numPresent ++;
				}
			}
			
			if (numPresent == 1 && autoPlay)
			{
				foreach (ButtonDialog _option in options)
				{
					if (_option.CanShow ())
					{
						RunOption (_option);
						return;
					}
				}
			}
			else if (numPresent > 0)
			{
				KickStarter.playerInput.activeConversation = this;
			}
			else
			{
				KickStarter.playerInput.EndConversation ();
				return;
			}
			
			if (isTimed)
			{
				startTime = Time.time;
				Invoke ("RunDefault", timer);
			}
		}


		/** Show the Conversation's dialogue options. */
		public void TurnOn ()
		{
			Interact ();
		}


		/**
		 * <summary>Checks if the Conversation is currently active.</summary>
		 * <param name = "includeResponses">If True, then the Conversation will be considered active if any of its dialogue option ActionLists are currently-running, as opposed to only when its options are displayed as choices on screen</param>
		 * </returns>True if the Conversation is active</returns>
		 */
		public bool IsActive (bool includeResponses)
		{
			if (KickStarter.playerInput.activeConversation == this ||
				KickStarter.playerInput.PendingOptionConversation == this)
			{
				return true;
			}

			if (includeResponses)
			{
				foreach (ButtonDialog buttonDialog in options)
				{
					if (interactionSource == InteractionSource.InScene)
					{
						if (KickStarter.actionListManager.IsListRunning (buttonDialog.dialogueOption))
						{
							return true;
						}
					}
					else if (interactionSource == InteractionSource.AssetFile)
					{
						if (KickStarter.actionListAssetManager.IsListRunning (buttonDialog.assetFile))
						{
							return true;
						}
					}
				}
			}
			return false;
		}


		/** Hides the Conversation's dialogue options, if it is the currently-active Conversation. */
		public void TurnOff ()
		{
			if (KickStarter.playerInput && KickStarter.playerInput.activeConversation == this)
			{
				CancelInvoke ("RunDefault");
				KickStarter.playerInput.EndConversation ();
			}
		}


		/**
		 * <summary>Runs a dialogue option.</summary>
		 * <param name = "slot">The index number of the dialogue option to run</param>
		 * <param name = "force">If True, then the option will be run regardless of whether it's enabled or valid</param>
		 */
		public void RunOption (int slot, bool force = false)
		{
			CancelInvoke ("RunDefault");
			int i = ConvertSlotToOption (slot, force);
			if (i == -1 || i >= options.Count)
			{
				return;
			}

			ButtonDialog buttonDialog = options[i];
			if (!gameObject.activeInHierarchy || interactionSource == AC.InteractionSource.CustomScript)
			{
				RunOption (buttonDialog);
			}
			else
			{
				StartCoroutine (RunOptionCo (buttonDialog));
			}

			KickStarter.playerInput.activeConversation = null;
		}


		/**
		 * <summary>Runs a dialogue option with a specific ID.</summary>
		 * <param name = "ID">The ID number of the dialogue option to run</param>
		 * <param name = "force">If True, then the option will be run regardless of whether it's enabled or valid</param>
		 */
		public void RunOptionWithID (int ID, bool force = false)
		{
			CancelInvoke ("RunDefault");
			
			ButtonDialog buttonDialog = GetOptionWithID (ID);
			if (buttonDialog == null) return;

			if (!buttonDialog.isOn && !force) return;

			if (!gameObject.activeInHierarchy || interactionSource == AC.InteractionSource.CustomScript)
			{
				RunOption (buttonDialog);
			}
			else
			{
				StartCoroutine (RunOptionCo (buttonDialog));
			}

			KickStarter.playerInput.activeConversation = null;
		}


		/**
		 * <summary>Gets the time remaining before a timed Conversation ends.</summary>
		 * <returns>The time remaining before a timed Conversation ends.</returns>
		 */
		public float GetTimeRemaining ()
		{
			return ((startTime + timer - Time.time) / timer);
		}


		/**
		 * <summary>Checks if a given slot exists</summary>
		 * <param name = "slot">The index number of the enabled dialogue option to find</param>
		 * <returns>True if a given slot exists</returns>
		 */
		public bool SlotIsAvailable (int slot)
		{
			int i = ConvertSlotToOption (slot);
			return (i >= 0 && i < options.Count);
		}


		/**
		 * <summary>Gets the ID of a dialogue option.</summary>
		 * <param name = "slot">The index number of the enabled dialogue option to find</param>
		 * <returns>The dialogue option's ID number, if found - or -1 otherwise.</returns>
		 */
		public int GetOptionID (int slot)
		{
			int i = ConvertSlotToOption (slot);
			if (i >= 0 && i < options.Count)
			{
				return options[i].ID;
			}
			return -1;
		}


		/**
		 * <summary>Gets the display label of a dialogue option.</summary>
		 * <param name = "slot">The index number of the enabled dialogue option to find</param>
		 * <returns>The display label of the dialogue option</returns>
		 */
		public string GetOptionName (int slot)
		{
			int i = ConvertSlotToOption (slot);
			if (i == -1)
			{
				i = 0;
			}

			string translatedLine = KickStarter.runtimeLanguages.GetTranslation (options[i].label, options[i].lineID, Options.GetLanguage (), GetTranslationType (0));
			return AdvGame.ConvertTokens (translatedLine).Replace ("\\n", "\n");
		}


		/**
		 * <summary>Gets the display label of a dialogue option with a specific ID.</summary>
		 * <param name = "ID">The ID of the dialogue option to find</param>
		 * <returns>The display label of the dialogue option</returns>
		 */
		public string GetOptionNameWithID (int ID)
		{
			ButtonDialog buttonDialog = GetOptionWithID (ID);
			if (buttonDialog == null) return null;

			string translatedLine = KickStarter.runtimeLanguages.GetTranslation (buttonDialog.label, buttonDialog.lineID, Options.GetLanguage (), GetTranslationType (0));
			return AdvGame.ConvertTokens (translatedLine).Replace ("\\n", "\n");
		}


		/**
		 * <summary>Gets the display icon of a dialogue option.</summary>
		 * <param name = "slot">The index number of the dialogue option to find</param>
		 * <returns>The display icon of the dialogue option</returns>
		 */
		public CursorIconBase GetOptionIcon (int slot)
		{
			int i = ConvertSlotToOption (slot);
			if (i == -1)
			{
				i = 0;
			}
			return options[i].cursorIcon;
		}


		/**
		 * <summary>Gets the display icon of a dialogue option with a specific ID.</summary>
		 * <param name = "ID">The ID of the dialogue option to find</param>
		 * <returns>The display icon of the dialogue option</returns>
		 */
		public CursorIconBase GetOptionIconWithID (int ID)
		{
			ButtonDialog buttonDialog = GetOptionWithID (ID);
			if (buttonDialog == null) return null;
			return buttonDialog.cursorIcon;
		}


		/**
		 * <summary>Gets the ButtonDialog data container, which stores data for a dialogue option.</summary>
		 * <param name = "slot">The index number of the dialogue option to find</param>
		 * <returns>The ButtonDialog data container</returns>
		 */
		public ButtonDialog GetOption (int slot)
		{
			int i = ConvertSlotToOption (slot);
			if (i == -1)
			{
				i = 0;
			}
			return options[i];
		}


		/**
		 * <summary>Gets the ButtonDialog data container with a given ID number, which stores data for a dialogue option.</summary>
		 * <param name = "id">The ID number associated with the dialogue option to find</param>
		 * <returns>The ButtonDialog data container</returns>
		 */
		public ButtonDialog GetOptionWithID (int id)
		{
			for (int i=0; i<options.Count; i++)
			{
				if (options[i].ID == id)
				{
					return options[i];
				}
			}
			return null;
		}


		/**
		 * <summary>Gets the number of dialogue options that are currently enabled.</summary>
		 * <returns>The number of currently-enabled dialogue options</returns>
		 */
		public int GetNumEnabledOptions ()
		{
			int num = 0;
			for (int i=0; i<options.Count; i++)
			{
				if (options[i].isOn)
				{
					num++;
				}
			}
			return num;
		}


		/**
		 * <summary>Checks if a dialogue option has been chosen at least once by the player.</summary>
		 * <param name = "slot">The index number of the dialogue option to find</param>
		 * <returns>True if the dialogue option has been chosen at least once by the player.</returns>
		 */
		public bool OptionHasBeenChosen (int slot)
		{
			int i = ConvertSlotToOption (slot);
			if (i == -1)
			{
				i = 0;
			}
			return options[i].hasBeenChosen;
		}


		/**
		 * <summary>Checks if a dialogue option with a specific ID has been chosen at least once by the player.</summary>
		 * <param name = "ID">The ID of the dialogue option to find</param>
		 * <returns>True if the dialogue option has been chosen at least once by the player.</returns>
		 */
		public bool OptionWithIDHasBeenChosen (int ID)
		{
			ButtonDialog buttonDialog = GetOptionWithID (ID);
			if (buttonDialog == null) return false;
			return buttonDialog.hasBeenChosen;
		}


		/** 
		 * <summary>Checks if all options have been chosen at least once by the player</summary>
		 * <param name = "onlyEnabled">If True, then only options that are currently enabled will be included in the check</param>
		 * <returns>True if all options have been chosen at least once by the player</returns>
		 */
		public bool AllOptionsBeenChosen (bool onlyEnabled)
		{
			foreach (ButtonDialog option in options)
			{
				if (!option.hasBeenChosen)
				{
					if (onlyEnabled && !option.isOn)
					{
						continue;
					}
					return false;
				}
			}
			return true;
		}


		/**
		 * <summary>Turns a dialogue option on, provided that it is unlocked.</summary>
		 * <param name = "id">The ID number of the dialogue option to enable</param>
		 */
		public void TurnOptionOn (int id)
		{
			foreach (ButtonDialog option in options)
			{
				if (option.ID == id)
				{
					if (!option.isLocked)
					{
						option.isOn = true;
					}
					else
					{
						ACDebug.Log (gameObject.name + "'s option '" + option.label + "' cannot be turned on as it is locked.", this);
					}
					return;
				}
			}
		}


		/**
		 * <summary>Turns a dialogue option off, provided that it is unlocked.</summary>
		 * <param name = "id">The ID number of the dialogue option to disable</param>
		 */
		public void TurnOptionOff (int id)
		{
			foreach (ButtonDialog option in options)
			{
				if (option.ID == id)
				{
					if (!option.isLocked)
					{
						option.isOn = false;
					}
					else
					{
						ACDebug.LogWarning (gameObject.name + "'s option '" + option.label + "' cannot be turned off as it is locked.", this);
					}
					return;
				}
			}
		}


		/**
		 * <summary>Sets the enabled and locked states of a dialogue option, provided that it is unlocked.</summary>
		 * <param name = "id">The ID number of the dialogue option to change</param>
		 * <param name = "flag">The "on/off" state to set the option</param>
		 * <param name = "isLocked">The "locked/unlocked" state to set the option</param>
		 */
		public void SetOptionState (int id, bool flag, bool isLocked)
		{
			foreach (ButtonDialog option in options)
			{
				if (option.ID == id)
				{
					if (!option.isLocked)
					{
						option.isLocked = isLocked;
						option.isOn = flag;
					}
					KickStarter.playerMenus.RefreshDialogueOptions ();
					return;
				}
			}
		}


		/**
		 * <summary>Turns all dialogue options on</summary>
		 * <param name = "includingLocked">If True, then locked options will be unlocked and turned on as well. Otherwise, they will remain locked</param>
		 */
		public void TurnAllOptionsOn (bool includingLocked)
		{
			foreach (ButtonDialog option in options)
			{
				if (includingLocked || !option.isLocked)
				{
					option.isLocked = false;
					option.isOn = true;
				}
			}
		}


		/**
		 * <summary>Renames a dialogue option.</summary>
		 * <param name = "id">The ID number of the dialogue option to rename</param>
		 * <param name = "newLabel">The new label text to give the dialogue option<param>
		 * <param name = "newLindID">The line ID number to give the dialogue option, as set by the Speech Manager</param>
		 */
		public void RenameOption (int id, string newLabel, int newLineID)
		{
			foreach (ButtonDialog option in options)
			{
				if (option.ID == id)
				{
					option.label = newLabel;
					option.lineID = newLineID;
					return;
				}
			}
		}
		

		/**
		 * <summary>Gets the number of enabled dialogue options.</summary>
		 * <returns>The number of enabled dialogue options</summary>
		 */
		public int GetCount ()
		{
			int numberOn = 0;
			foreach (ButtonDialog _option in options)
			{
				if (_option.CanShow ())
				{
					numberOn ++;
				}
			}
			return numberOn;
		}


		/**
		 * <summary>Checks if a dialogue option with a specific ID is active.</summary>
		 * <param name="ID">The ID of the dialogue option to check for</param>
		 * <returns>True if the specified option is active</summary>
		 */
		public bool OptionWithIDIsActive (int ID)
		{
			ButtonDialog buttonDialog = GetOptionWithID (ID);
			if (buttonDialog == null) return false;
			return buttonDialog.CanShow ();
		}


		/**
		 * <summmary>Gets an array of ID numbers of existing ButtonDialog classes, so that a unique number can be generated.</summary>
		 * <returns>Gets an array of ID numbers of existing ButtonDialog classes</returns>
		 */
		public int[] GetIDArray ()
		{
			List<int> idArray = new List<int>();
			foreach (ButtonDialog option in options)
			{
				idArray.Add (option.ID);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		/** Checks if the Converations options are currently being overridden by an ActionList */
		public bool HasActionListOverride ()
		{
			return (overrideActiveList != null);
		}

		#endregion


		#region ProtectedFunctions

		protected void RunOption (ButtonDialog _option)
		{
			if (_option.autoTurnOff)
			{
				_option.isOn = false;
			}

			_option.hasBeenChosen = true;
			if (options.Contains (_option))
			{
				lastOption = options.IndexOf (_option);

				if (overrideActiveList != null)
				{
					if (overrideActiveList.actionListAsset)
					{
						overrideActiveList.actionList = AdvGame.RunActionListAsset (overrideActiveList.actionListAsset, overrideActiveList.startIndex, true);
					}
					else if (overrideActiveList.actionList)
					{
						overrideActiveList.actionList.Interact (overrideActiveList.startIndex, true);
					}

					KickStarter.eventManager.Call_OnClickConversation (this, _option.ID);
					overrideActiveList = null;
					return;
				}
				lastOption = -1;
			}

			Conversation endConversation = null;
			if (interactionSource != AC.InteractionSource.CustomScript)
			{
				if (_option.conversationAction == ConversationAction.ReturnToConversation)
				{
					endConversation = this;
				}
				else if (_option.conversationAction == ConversationAction.RunOtherConversation && _option.newConversation)
				{
					endConversation = _option.newConversation;
				}
			}

			if (interactionSource == AC.InteractionSource.AssetFile && _option.assetFile)
			{
				AdvGame.RunActionListAsset (_option.assetFile, endConversation);
			}
			else if (interactionSource == AC.InteractionSource.CustomScript)
			{
				if (_option.customScriptObject && !string.IsNullOrEmpty (_option.customScriptFunction))
				{
					_option.customScriptObject.SendMessage (_option.customScriptFunction);
				}
			}
			else if (interactionSource == AC.InteractionSource.InScene && _option.dialogueOption)
			{
				_option.dialogueOption.conversation = endConversation;
				_option.dialogueOption.Interact ();
			}
			else
			{
				ACDebug.Log ("No DialogueOption object found on Conversation '" + gameObject.name + "'", this);
				KickStarter.eventManager.Call_OnEndConversation (this);

				if (endConversation)
				{
					endConversation.Interact ();
				}
			}

			KickStarter.eventManager.Call_OnClickConversation (this, _option.ID);
		}
		

		protected void RunDefault ()
		{
			if (KickStarter.playerInput && KickStarter.playerInput.IsInConversation ())
			{
				if (defaultOption < 0 || defaultOption >= options.Count)
				{
					TurnOff ();
				}
				else
				{
					RunOption (defaultOption, true);
				}
			}
		}
		
		
		protected IEnumerator RunOptionCo (ButtonDialog buttonDialog)
		{
			KickStarter.playerInput.PendingOptionConversation = this;

			float timeElapsed = 0f;
			while (timeElapsed < KickStarter.dialog.conversationDelay)
			{
				timeElapsed += Time.deltaTime;
				yield return new WaitForEndOfFrame ();
			}

			RunOption (buttonDialog);

			if (KickStarter.playerInput.PendingOptionConversation == this)
			{
				KickStarter.playerInput.PendingOptionConversation = null;
			}
		}
		

		protected int ConvertSlotToOption (int slot, bool force = false)
		{
			int foundSlots = 0;
			for (int j=0; j<options.Count; j++)
			{
				if (force || options[j].CanShow ())
				{
					foundSlots ++;
					if (foundSlots == (slot+1))
					{
						return j;
					}
				}
			}
			return -1;
		}


		protected void OnEndActionList (ActionList actionList, ActionListAsset actionListAsset, bool isSkipping)
		{
			if (overrideActiveList == null)
			{
				foreach (ButtonDialog buttonDialog in options)
				{
					if (interactionSource == InteractionSource.InScene)
					{
						if (buttonDialog.dialogueOption == actionList)
						{
							if (buttonDialog.conversationAction == ConversationAction.ReturnToConversation && GetNumEnabledOptions () > 0)
							{
								continue;
							}

							KickStarter.eventManager.Call_OnEndConversation (this);
							return;
						}
					}
					else if (interactionSource == InteractionSource.AssetFile)
					{
						if (actionListAsset && buttonDialog.assetFile == actionListAsset)
						{
							if (buttonDialog.conversationAction == ConversationAction.ReturnToConversation && GetNumEnabledOptions () > 0)
							{
								continue;
							}

							KickStarter.eventManager.Call_OnEndConversation (this);
							return;
						}
					}
				}
			}
		}


		protected void OnEndConversation (Conversation conversation)
		{
			if (conversation == this && onFinishActiveList != null)
			{
				if (onFinishActiveList.actionListAsset)
				{
					onFinishActiveList.actionList = AdvGame.RunActionListAsset (onFinishActiveList.actionListAsset, onFinishActiveList.startIndex, true);
				}
				else if (onFinishActiveList.actionList)
				{
					onFinishActiveList.actionList.Interact (onFinishActiveList.startIndex, true);
				}
			}

			onFinishActiveList = null;
		}


		protected void OnFinishLoading ()
		{
			onFinishActiveList = null;
			overrideActiveList = null;
		}

		#endregion


		#if UNITY_EDITOR

		/**
		 * <summary>Converts the Conversations's references from a given local variable to a given global variable</summary>
		 * <param name = "oldLocalID">The ID number of the old local variable</param>
		 * <param name = "newGlobalID">The ID number of the new global variable</param>
		 * <returns>True if the Action was amended</returns>
		 */
		public bool ConvertLocalVariableToGlobal (int oldLocalID, int newGlobalID)
		{
			bool wasAmened = false;

			if (options != null)
			{
				foreach (ButtonDialog option in options)
				{
					string newLabel = AdvGame.ConvertLocalVariableTokenToGlobal (option.label, oldLocalID, newGlobalID);
					if (newLabel != option.label)
					{
						option.label = newLabel;
						wasAmened = true;
					}
				}
			}

			return wasAmened;
		}


		/**
		 * <summary>Gets the number of references to a given variable</summary>
		 * <param name = "location">The location of the variable (Global, Local)</param>
		 * <param name = "varID">The ID number of the variable</param>
		 * <returns>The number of references to the variable</returns>
		 */
		public int GetNumVariableReferences (VariableLocation location, int varID, Variables variables = null, int _variablesConstantID = 0)
		{
			int numFound = 0;
			if (options != null)
			{
				string tokenText = AdvGame.GetVariableTokenText (location, varID, _variablesConstantID);

				foreach (ButtonDialog option in options)
				{
					if (option.label.ToLower ().Contains (tokenText))
					{
						numFound ++;
					}
				}
			}
			return numFound;
		}


		public int UpdateVariableReferences (VariableLocation location, int oldVariableID, int newVariableID, Variables variables = null, int variablesConstantID = 0)
		{
			int numFound = 0;
			if (options != null)
			{
				string oldTokenText = AdvGame.GetVariableTokenText (location, oldVariableID, variablesConstantID);
				foreach (ButtonDialog option in options)
				{
					if (option.label.ToLower ().Contains (oldTokenText))
					{
						string newTokenText = AdvGame.GetVariableTokenText (location, newVariableID, variablesConstantID);
						option.label = option.label.Replace (oldTokenText, newTokenText);
						numFound++;
					}
				}
			}
			return numFound;
		}


		public int GetNumItemReferences (int itemID)
		{
			int numFound = 0;
			foreach (ButtonDialog option in options)
			{
				if (option.linkToInventory && option.linkedInventoryID == itemID)
				{
					numFound ++;
				}
			}
			return numFound;
		}


		public int UpdateItemReferences (int oldItemID, int newItemID)
		{
			int numFound = 0;
			foreach (ButtonDialog option in options)
			{
				if (option.linkToInventory && option.linkedInventoryID == oldItemID)
				{
					option.linkedInventoryID = newItemID;
					numFound++;
				}
			}
			return numFound;
		}


		/**
		 * <summary>Converts the Conversations's references from a given global variable to a given local variable</summary>
		 * <param name = "oldLocalID">The ID number of the old global variable</param>
		 * <param name = "newLocalID">The ID number of the new local variable</param>
		 * <returns>True if the Action was amended</returns>
		 */
		public bool ConvertGlobalVariableToLocal (int oldGlobalID, int newLocalID, bool isCorrectScene)
		{
			bool wasAmened = false;

			if (options != null)
			{
				foreach (ButtonDialog option in options)
				{
					string newLabel = AdvGame.ConvertGlobalVariableTokenToLocal (option.label, oldGlobalID, newLocalID);
					if (newLabel != option.label)
					{
						wasAmened = true;
						if (isCorrectScene)
						{
							option.label = newLabel;
						}
					}
				}
			}

			return wasAmened;
		}

		#endif


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			return options[index].label;
		}


		public int GetTranslationID (int index)
		{
			return options[index].lineID;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.DialogueOption;
		}


		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			if (index < options.Count)
			{
				options[index].label = updatedText;
			}
		}


		public int GetNumTranslatables ()
		{
			if (options != null) return options.Count;
			return 0;
		}


		public bool HasExistingTranslation (int index)
		{
			return (options[index].lineID > 0);
		}


		public void SetTranslationID (int index, int _lineID)
		{
			options[index].lineID = _lineID;
		}


		public string GetOwner (int index)
		{
			return string.Empty;
		}


		public bool OwnerIsPlayer (int index)
		{
			return false;
		}


		public bool CanTranslate (int index)
		{
			return (!string.IsNullOrEmpty (options[index].label));
		}

		#endif

		#endregion


		#if UNITY_EDITOR

		public bool ReferencesAsset (ActionListAsset actionListAsset)
		{
			if (interactionSource == AC.InteractionSource.AssetFile)
			{
				foreach (ButtonDialog buttonDialog in options)
				{
					if (buttonDialog.assetFile == actionListAsset) return true;
				}
			}
			return false;
		}

		#endif


		public MenuDialogList LinkedDialogList
		{
			get
			{
				return linkedDialogList;
			}
			set
			{
				linkedDialogList = value;
			}
		}

	}

}