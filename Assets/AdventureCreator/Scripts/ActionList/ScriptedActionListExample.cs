/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ScriptedActionListExample.cs"
 * 
 *	This script offers two examples on how ActionList can be generated through script.
 *	To use it, place the "Scripted Action List Example" component in your scene and assign the fields in the Inspector.
 *	Once in Play mode, each example can then be run by clicking the cog icon to the top-right of the Inspector and choosing either 'Run Example 1' or 'Run Example 2'.
 * 
 *	Note: This script implements the ITranslatable interface but this is only necessary to allow the Player's speech to be included in the Speech Manager.
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script offers two examples on how ActionList can be generated through script.
 	 * To use it, place the "Scripted Action List Example" component in your scene and assign the fields in the Inspector.
 	 * Once in Play mode, each example can then be run by clicking the cog icon to the top-right of the Inspector and choosing either 'Run Example 1' or 'Run Example 2'.
 	 *
 	 * Note: This script implements the ITranslatable interface but this is only necessary to allow the Player's speech to be included in the Speech Manager.
	 */
	public class ScriptedActionListExample : MonoBehaviour, ITranslatable
	{

		[Header ("Example 1 fields")]
		[SerializeField] private Marker markerToMoveTo = null;
		[SerializeField] private int inventoryItemIDToAdd = 0;
		[SerializeField] private string playerSpeechText = "This is a scripted ActionList!";
		[HideInInspector] [SerializeField] private int playerSpeechTranslationID = -1;
		#if UNITY_EDITOR
		[SerializeField] private bool translateSpeech = false;
		#endif

		[Header ("Example 2 fields")]
		[SerializeField] private int globalBoolVariableID = 0;


		/**
		 * In this example, we create a linear sequence of Actions directly within the ActionList they are a part of.
		 * This sequence involves playing a simple Console message, moving the player, having him speak, and add an item to their inventory.
		 * The player's speech can also be included in the Speech Manager, but this is optional.  See the ITranslatable implementation at the bottom of the script, as well as the Manual's "Custom translatables" chapter for more.
		 */
		[ContextMenu ("Run Example 1")]
		public void RunExampleOne ()
		{
			// Get the ActionList component
			ActionList actionList = CreateActionList ();

			// Make sure we're in gameplay and that no Actions are already running
			if (actionList.AreActionsRunning () || !Application.isPlaying)
			{
				Debug.LogWarning ("Cannot run Actions at this time", this);
				return;
			}

			// Declare the Actions within it
			actionList.actions = new List<Action>
			{
				// Show a Console message
				ActionComment.CreateNew ("Running Example 1 - move the player, say something, and add an inventory item"),

				// Move the Player to the Marker (note: uses pathfinding, so a NavMesh will need to be set up)
				ActionCharPathFind.CreateNew (KickStarter.player, markerToMoveTo),

				// Have the Player say something (Note: The 'translation ID' parameter is optional)
				ActionSpeech.CreateNew (KickStarter.player, playerSpeechText, playerSpeechTranslationID),

				// Add an item to the Player's inventory
				ActionInventorySet.CreateNew_Add (inventoryItemIDToAdd),

				// Show another Console message
				ActionComment.CreateNew ("Example complete!"),
			};

			// Run it
			actionList.Interact ();
		}


		/**
		 * In this example, we check the value of a Global Bool variable, and show one of two Console messages accordingly.
		 * To do this, we must modify the Action outputs so that we can control what happens after each is run.
		 * By defining our Actions before placing them in the ActionList, we have references to them that we can use to tweak them further.
		 */
		[ContextMenu ("Run Example 2")]
		private void RunExampleTwo ()
		{
			// Get the ActionList component
			ActionList actionList = CreateActionList ();

			// Make sure we're in gameplay and that no Actions are already running
			if (actionList.AreActionsRunning () || !Application.isPlaying)
			{
				Debug.LogWarning ("Cannot run Actions at this time", this);
				return;
			}

			// Create the Actions, but this time with references to them so that we can later modify their 'After running' options

			// Check if a global bool variable is True or False
			ActionVarCheck variableCheck = ActionVarCheck.CreateNew_Global (globalBoolVariableID);

			// Response if the variable is True
			ActionComment commentIfTrue = ActionComment.CreateNew ("The bool variable is currently True!");

			// Response if the variable is False
			ActionComment commentIfFalse = ActionComment.CreateNew ("The bool variable is currently False!");
	
			// Assign the Actions to the ActionList
			actionList.actions = new List<Action>
			{
				variableCheck,
				commentIfTrue,
				commentIfFalse,
			};

			// Modify the 'Variable: Check' Action's 'After running' fields so that commentIfTrue is run if the condition is met, and commentIfFalse is run otherwise
			variableCheck.SetOutputs (new ActionEnd (commentIfTrue), new ActionEnd (commentIfFalse));

			// Modify the two comment Actions so that their 'After running' fields are both set to Stop, so that the ActionList stops after either one is run
			commentIfTrue.SetOutput (new ActionEnd (true));
			commentIfFalse.SetOutput (new ActionEnd (true));

			// Run the ActionList
			actionList.Interact ();
		}


		private ActionList CreateActionList ()
		{
			// This function returns the ActionList attached to the GameObject.  If none is present, it is added automatically.

			ActionList currentActionList = gameObject.GetComponent <ActionList>();
			if (currentActionList != null)
			{
				return currentActionList;
			}
			return gameObject.AddComponent <ActionList>();
		}






		#region ITranslatable

		/**
		 * The following code is only necessary because the script implements ITranslatable.  This is to demonstrate how the Player's speech line can be included in the Speech Manager for translation, audio etc.
		 * If a scripted ActionList does not require this, all of the below can be left out (provided that the implemenation is also removed in the class declaration)
		 */

		public string GetTranslatableString (int index)
		{
			return playerSpeechText;
		}


		public int GetTranslationID (int index)
		{
			return playerSpeechTranslationID;
		}

		#if UNITY_EDITOR

		public void UpdateTranslatableString (int index, string updatedText)
		{
			playerSpeechText = updatedText;
		}


		public int GetNumTranslatables ()
		{
			return (translateSpeech) ? 1 : 0;
		}


		public bool HasExistingTranslation (int index)
		{
			return playerSpeechTranslationID > -1;
		}


		public void SetTranslationID (int index, int _lineID)
		{
			playerSpeechTranslationID = _lineID;
		}


		public string GetOwner (int index)
		{
			return "Player";
		}


		public bool OwnerIsPlayer (int index)
		{
			return true;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Speech;
		}


		public bool CanTranslate (int index)
		{
			return (translateSpeech && !string.IsNullOrEmpty (playerSpeechText));
		}

		#endif

		#endregion
		
	}

}