#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(Conversation))]
	public class ConversationEditor : Editor
	{

		private int sideItem = -1;
		private Conversation _target;
		private Vector2 scrollPos;

		
		public void OnEnable ()
		{
			_target = (Conversation) target;
		}
		
		
		public override void OnInspectorGUI ()
		{
			if (_target == null) return;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Conversation settings", EditorStyles.boldLabel);
			_target.interactionSource = (AC.InteractionSource) CustomGUILayout.EnumPopup ("Interaction source:", _target.interactionSource, "", "The source of the commands that are run when an option is chosen");
			_target.autoPlay = CustomGUILayout.Toggle ("Auto-play lone option?", _target.autoPlay, "", "If True, and only one option is available, then the option will be chosen automatically");
			_target.isTimed = CustomGUILayout.Toggle ("Is timed?", _target.isTimed, "", "If True, then the Conversation is timed, and the options will only be shown for a fixed period");
			if (_target.isTimed)
			{
				_target.timer = CustomGUILayout.FloatField ("Timer length (s):", _target.timer, "", "The duration, in seconds, that the Conversation is active");

				if (_target.options != null && _target.options.Count > 0)
				{
					bool noDefault = (_target.defaultOption < 0);
					noDefault = CustomGUILayout.Toggle ("End if timer runs out?", noDefault, "", "If True, the Conversation will end when the timer runs out - and no option will be chosen");
					if (noDefault)
					{
						_target.defaultOption = -1;
					}
					else if (_target.defaultOption < 0)
					{
						_target.defaultOption = 0;
					}
				}
				else
				{
					_target.defaultOption = -1;
				}
			}
			CustomGUILayout.EndVertical ();
			
			if (Application.isPlaying && KickStarter.playerInput && KickStarter.playerInput.activeConversation != _target)
			{
				if (GUILayout.Button ("Run now"))
				{
					_target.Interact ();
				}
			}

			EditorGUILayout.Space ();
			CreateOptionsGUI ();
			EditorGUILayout.Space ();

			if (_target.selectedOption != null && _target.options.Contains (_target.selectedOption))
			{
				EditorGUILayout.LabelField ("Dialogue option '" + _target.selectedOption.label + "' properties", EditorStyles.boldLabel);
				EditOptionGUI (_target.selectedOption, _target.interactionSource);
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}

		
		private void CreateOptionsGUI ()
		{
			EditorGUILayout.LabelField ("Dialogue options", EditorStyles.boldLabel);

			scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (Mathf.Min (_target.options.Count * 22, ACEditorPrefs.MenuItemsBeforeScroll * 22) + 9));
			foreach (ButtonDialog option in _target.options)
			{
				EditorGUILayout.BeginHorizontal ();
				
				string buttonLabel = option.ID + ": " + option.label;
				if (option.label == "")
				{
					buttonLabel += "(Untitled)";	
				}
				if (_target.isTimed && _target.options.IndexOf (option) == _target.defaultOption)
				{
					buttonLabel += " (Default)";
				}
				
				if (GUILayout.Toggle (_target.selectedOption == option, buttonLabel, "Button"))
				{
					if (_target.selectedOption != option)
					{
						DeactivateAllOptions ();
						ActivateOption (option);
					}
				}

				if (GUILayout.Button ("", CustomStyles.IconCog))
				{
					SideMenu (option);
				}

				EditorGUILayout.EndHorizontal ();
			}
			EditorGUILayout.EndScrollView();
			
			if (GUILayout.Button ("Add new dialogue option"))
			{
				Undo.RecordObject (_target, "Create dialogue option");
				ButtonDialog newOption = new ButtonDialog (_target.GetIDArray ());
				_target.options.Add (newOption);
				DeactivateAllOptions ();
				ActivateOption (newOption);
			}
		}


		private void ActivateOption (ButtonDialog option)
		{
			_target.selectedOption = option;
			EditorGUIUtility.editingTextField = false;
		}
		
		
		private void DeactivateAllOptions ()
		{
			_target.selectedOption = null;
			EditorGUIUtility.editingTextField = false;
		}
		
		
		private void EditOptionGUI (ButtonDialog option, InteractionSource source)
		{
			CustomGUILayout.BeginVertical ();
			
			if (option.lineID > -1)
			{
				EditorGUILayout.LabelField ("Speech Manager ID:", option.lineID.ToString ());
			}
			
			option.label = CustomGUILayout.TextField ("Label:", option.label, "", "The option's display label");

			if (source == InteractionSource.AssetFile)
			{
				option.assetFile = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("Interaction:", option.assetFile, false, "", "The ActionListAsset to run");
			}
			else if (source == InteractionSource.CustomScript)
			{
				option.customScriptObject = (GameObject) CustomGUILayout.ObjectField <GameObject> ("Object with script:", option.customScriptObject, true, "", "The GameObject with the custom script to run");
				option.customScriptFunction = CustomGUILayout.TextField ("Message to send:", option.customScriptFunction, "", "The name of the function to run");
			}
			else if (source == InteractionSource.InScene)
			{
				EditorGUILayout.BeginHorizontal ();
				option.dialogueOption = (DialogueOption) CustomGUILayout.ObjectField <DialogueOption> ("DialogOption:", option.dialogueOption, true, "", "The DialogOption to run");
				if (option.dialogueOption == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Auto-create dialogue option");
						DialogueOption newDialogueOption = SceneManager.AddPrefab ("Logic", "DialogueOption", true, false, true).GetComponent <DialogueOption>();
						
						newDialogueOption.gameObject.name = AdvGame.UniqueName (_target.gameObject.name + "_Option");
						newDialogueOption.Initialise ();
						EditorUtility.SetDirty (newDialogueOption);
						option.dialogueOption = newDialogueOption;
					}
				}
				EditorGUILayout.EndHorizontal ();
			}
			
			option.cursorIcon.ShowGUI (false, true, "Icon texture:", CursorRendering.Software, "", "The icon to display in DialogList menu elements");
			
			option.isOn = CustomGUILayout.Toggle ("Is enabled?", option.isOn, "", "If True, the option is enabled, and will be displayed in a MenuDialogList element");
			if (source == InteractionSource.CustomScript)
			{
				EditorGUILayout.HelpBox ("Using a custom script will cause the conversation to end when finished, unless it is re-run explicitly.", MessageType.Info);
			}
			else
			{
				option.conversationAction = (ConversationAction) CustomGUILayout.EnumPopup ("When finished:", option.conversationAction, "", "What happens when the DialogueOption ActionList has finished");
				if (option.conversationAction == AC.ConversationAction.RunOtherConversation)
				{
					option.newConversation = (Conversation) CustomGUILayout.ObjectField <Conversation> ("Conversation to run:", option.newConversation, true, "", "The new Conversation to run");
				}
			}

			option.autoTurnOff = CustomGUILayout.Toggle ("Auto-disable when chosen?", option.autoTurnOff, "", "If True, then the option will be disabled automatically once chosen by the player");
			option.linkToInventory = CustomGUILayout.Toggle ("Link visibility to inventory item?", option.linkToInventory, "", " If True, then the option will only be visible if a given inventory item is being carried");
			if (option.linkToInventory)
			{
				option.linkedInventoryID = CreateInventoryGUI (option.linkedInventoryID);
				if (option.cursorIcon.texture == null && !Application.isPlaying)
				{
					EditorGUILayout.HelpBox ("The option's icon texture will automatically be set to the item's texture at runtime.", MessageType.Info);
				}
			}

			CustomGUILayout.EndVertical ();
		}


		private int CreateInventoryGUI (int invID)
		{
			if (AdvGame.GetReferences ().inventoryManager == null || AdvGame.GetReferences ().inventoryManager.items == null || AdvGame.GetReferences ().inventoryManager.items.Count == 0)
			{
				EditorGUILayout.HelpBox ("Cannot find any inventory items!", MessageType.Warning);
				return invID;
			}

			// Create a string List of the field's names (for the PopUp box)
			List<string> labelList = new List<string>();
			int invNumber = -1;
			int i = 0;

			foreach (InvItem _item in AdvGame.GetReferences ().inventoryManager.items)
			{
				labelList.Add (_item.label);
					
				// If a item has been removed, make sure selected variable is still valid
				if (_item.id == invID)
				{
					invNumber = i;
				}
				i++;
			}
				
			if (invNumber == -1)
			{
				if (invID > 0) ACDebug.Log ("Previously chosen item no longer exists!");
				return invID;
			}
			
			invNumber = CustomGUILayout.Popup ("Linked inventory item:", invNumber, labelList.ToArray (), string.Empty, "The inventory item that the player must be carrying for the option to be active");
			invID = AdvGame.GetReferences ().inventoryManager.items[invNumber].id;

			return invID;
		}


		private void SideMenu (ButtonDialog option)
		{
			GenericMenu menu = new GenericMenu ();
			sideItem = _target.options.IndexOf (option);
			
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (_target.options.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}

			if (sideItem > 0 || sideItem < _target.options.Count-1)
			{
				menu.AddSeparator ("");
			}

			if (sideItem > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (sideItem < (_target.options.Count - 1))
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom");
			}

			if (_target.isTimed && _target.options.IndexOf (option) != _target.defaultOption)
			{
				menu.AddSeparator ("");
				menu.AddItem (new GUIContent ("Make default"), false, Callback, "Make default");
			}
			
			menu.ShowAsContext ();
		}
		
		
		private void Callback (object obj)
		{
			if (sideItem >= 0)
			{
				ButtonDialog tempItem = _target.options[sideItem];

				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (_target, "Insert option");
					_target.options.Insert (sideItem+1, new ButtonDialog (_target.GetIDArray ()));
					break;
					
				case "Delete":
					Undo.RecordObject (_target, "Delete option");
					DeactivateAllOptions ();
					_target.options.RemoveAt (sideItem);
					break;

				case "Move to top":
					Undo.RecordObject (this, "Move option to top");
					if (_target.defaultOption == sideItem)
					{
						_target.defaultOption = 0;
					}
					_target.options.RemoveAt (sideItem);
					_target.options.Insert (0, tempItem);
					break;
					
				case "Move up":
					Undo.RecordObject (_target, "Move option up");
					if (_target.defaultOption == sideItem)
					{
						_target.defaultOption --;
					}
					_target.options.RemoveAt (sideItem);
					_target.options.Insert (sideItem-1, tempItem);
					break;
					
				case "Move down":
					Undo.RecordObject (_target, "Move option down");
					if (_target.defaultOption == sideItem)
					{
						_target.defaultOption ++;
					}
					_target.options.RemoveAt (sideItem);
					_target.options.Insert (sideItem+1, tempItem);
					break;

				case "Move to bottom":
					Undo.RecordObject (_target, "Move option to bottom");
					if (_target.defaultOption == sideItem)
					{
						_target.defaultOption = _target.options.Count - 1;
					}
					_target.options.RemoveAt (sideItem);
					_target.options.Insert (_target.options.Count, tempItem);
					break;

				case "Make default":
					Undo.RecordObject (_target, "Change default Conversation option");
					_target.defaultOption = sideItem;
					EditorUtility.SetDirty (_target);
					break;
				}
			}

			EditorUtility.SetDirty (_target);

			sideItem = -1;
		}
		
	}

}

#endif