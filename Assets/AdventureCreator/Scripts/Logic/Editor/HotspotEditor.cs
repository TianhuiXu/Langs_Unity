#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{
	
	[CustomEditor (typeof (Hotspot))]
	public class HotspotEditor : Editor
	{

		private Hotspot _target;
		private int sideIndex;

		private InventoryManager inventoryManager;
		private SettingsManager settingsManager;
		private CursorManager cursorManager;
		
		private static readonly GUIContent
			deleteContent = new GUIContent("-", "Delete this interaction"),
			addContent = new GUIContent("+", "Create this interaction");
		
		private static readonly GUILayoutOption
			autoWidth = GUILayout.MaxWidth (90f),
			buttonWidth = GUILayout.Width (20f);
		
		
		private void OnEnable ()
		{
			_target = (Hotspot) target;
		}
		
		
		public override void OnInspectorGUI ()
		{
			if (AdvGame.GetReferences () == null)
			{
				ACDebug.LogError ("A References file is required - please use the Adventure Creator window to create one.");
				EditorGUILayout.LabelField ("No References file found!");
			}
			else
			{
				if (AdvGame.GetReferences ().inventoryManager)
				{
					inventoryManager = AdvGame.GetReferences ().inventoryManager;
				}
				if (AdvGame.GetReferences ().cursorManager)
				{
					cursorManager = AdvGame.GetReferences ().cursorManager;
				}
				if (AdvGame.GetReferences ().settingsManager)
				{
					settingsManager = AdvGame.GetReferences ().settingsManager;
				}

				if (Application.isPlaying)
				{
					if ((settingsManager && _target.gameObject.layer != LayerMask.NameToLayer (settingsManager.hotspotLayer)) || !_target.enabled)
					{
						EditorGUILayout.HelpBox ("Current state: OFF", MessageType.Info);
					}
				}

				if (_target.lineID > -1)
				{
					EditorGUILayout.LabelField ("Speech Manager ID:", _target.lineID.ToString ());
				}
				
				_target.interactionSource = (AC.InteractionSource) CustomGUILayout.EnumPopup ("Interaction source:", _target.interactionSource, string.Empty, "The source of the commands that are run when an option is chosen");
				_target.hotspotName = CustomGUILayout.TextField ("Label (if not name):", _target.hotspotName, string.Empty, "The display name, if not the GameObject's name");

				bool isPronoun = !_target.canBeLowerCase;
				isPronoun = CustomGUILayout.Toggle ("Name is pronoun?", isPronoun, string.Empty, "If False, the name will be lower-cased when inside sentences.");
				_target.canBeLowerCase = !isPronoun;

				_target.highlight = (Highlight) CustomGUILayout.ObjectField <Highlight> ("Object to highlight:", _target.highlight, true, string.Empty, "The Highlight component that controls any highlighting effects associated with the Hotspot");

				if (AdvGame.GetReferences ().settingsManager != null && AdvGame.GetReferences ().settingsManager.hotspotDrawing == ScreenWorld.WorldSpace)
				{
					_target.iconSortingLayer = CustomGUILayout.TextField ("Icon sorting layer:", _target.iconSortingLayer, string.Empty, "The 'Sorting Layer' of the icon's SpriteRenderer");
					_target.iconSortingOrder = CustomGUILayout.IntField ("Icon sprite order:", _target.iconSortingOrder, string.Empty, "The 'Order in Layer' of the icon's SpriteRenderer");
				}

				EditorGUILayout.BeginHorizontal ();
				_target.centrePoint = (Transform) CustomGUILayout.ObjectField <Transform> ("Centre-point (override):", _target.centrePoint, true, string.Empty, "A Transform that represents the centre of the Hotspot, if it is not physically at the same point as the Hotspot's GameObject itself");

				if (_target.centrePoint == null)
				{
					if (GUILayout.Button ("Create", autoWidth))
					{
						string prefabName = "Hotspot centre: " + _target.gameObject.name;
						GameObject go = SceneManager.AddPrefab ("Navigation", "HotspotCentre", true, false, false);
						go.name = prefabName;
						go.transform.position = _target.transform.position;
						_target.centrePoint = go.transform;
						go.transform.parent = _target.transform;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.centrePoint)
				{
					_target.centrePointOverrides = (CentrePointOverrides) CustomGUILayout.EnumPopup ("Centre-point overrides:", _target.centrePointOverrides, string.Empty, "What the 'Centre-point (override)' Transform actually overrides");
				}

				EditorGUILayout.BeginHorizontal ();
				_target.walkToMarker = (Marker) CustomGUILayout.ObjectField <Marker> ("Walk-to Marker:", _target.walkToMarker, true, string.Empty, "The Marker that the player can optionally automatically walk to before an Interaction runs");
				if (_target.walkToMarker == null)
				{
					if (GUILayout.Button ("Create", autoWidth))
					{
						string prefabName = "Marker";
						if (SceneSettings.IsUnity2D ())
						{
							prefabName += "2D";
						}
						Marker newMarker = SceneManager.AddPrefab ("Navigation", prefabName, true, false, true).GetComponent <Marker>();
						newMarker.gameObject.name += (": " + _target.gameObject.name);
						newMarker.transform.position = _target.transform.position;
						_target.walkToMarker = newMarker;

						if (UnityVersionHandler.IsPrefabFile (_target.gameObject))
						{
							newMarker.transform.parent = _target.transform;
						}
					}
				}
				EditorGUILayout.EndHorizontal ();

				_target.limitToCamera = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Limit to camera:", _target.limitToCamera, true, string.Empty, "If assigned, then the Hotspot will only be interactive when the assigned _Camera is active");

				EditorGUILayout.BeginHorizontal ();
				_target.interactiveBoundary = (InteractiveBoundary) CustomGUILayout.ObjectField <InteractiveBoundary> ("Interactive boundary:", _target.interactiveBoundary, true, string.Empty, "If assigned, then the Hotspot will only be interactive when the player is within this Trigger Collider's boundary");
				if (_target.interactiveBoundary == null)
				{
					if (GUILayout.Button ("Create", autoWidth))
					{
						string prefabName = "InteractiveBoundary";
						if (SceneSettings.IsUnity2D ())
						{
							prefabName += "2D";
						}
						InteractiveBoundary newInteractiveBoundary = SceneManager.AddPrefab ("Logic", prefabName, true, false, true).GetComponent <InteractiveBoundary>();
						newInteractiveBoundary.gameObject.name += (": " + _target.gameObject.name);
						newInteractiveBoundary.transform.position = _target.transform.position;
						_target.interactiveBoundary = newInteractiveBoundary;

						if (UnityVersionHandler.IsPrefabFile(_target.gameObject))
						{
							newInteractiveBoundary.transform.parent = _target.transform;
						}
						else
						{
							UnityVersionHandler.PutInFolder (newInteractiveBoundary.gameObject, "_Hotspots");
						}
					}
				}
				EditorGUILayout.EndHorizontal ();

				_target.drawGizmos = CustomGUILayout.Toggle ("Draw yellow cube?", _target.drawGizmos, string.Empty, "If True, then a Gizmo may be drawn in the Scene window at the Hotspots's position");
				
				if (settingsManager != null && (settingsManager.interactionMethod == AC_InteractionMethod.ChooseHotspotThenInteraction || settingsManager.interactionMethod == AC_InteractionMethod.ChooseInteractionThenHotspot || settingsManager.interactionMethod == AC_InteractionMethod.CustomScript))
				{
					_target.oneClick = CustomGUILayout.Toggle ("Single 'Use' Interaction?", _target.oneClick, string.Empty, "If True, then clicking the Hotspot will run the Hotspot's first interaction in useButtons, regardless of the Settings Manager's Interaction method");

					if (_target.oneClick && settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
					{
						EditorGUILayout.HelpBox ("The above property can be accessed by reading the Hotspot script's IsSingleInteraction() method.", MessageType.Info);
					}
				}
				if (_target.oneClick || (settingsManager != null && settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive))
				{
					if (!(settingsManager != null && settingsManager.interactionMethod == AC_InteractionMethod.CustomScript))
					{
						_target.doubleClickingHotspot = (DoubleClickingHotspot) CustomGUILayout.EnumPopup ("Double-clicking:", _target.doubleClickingHotspot, string.Empty, "The effect that double-clicking on the Hotspot has");
					}
				}
				if (settingsManager != null && settingsManager.playerFacesHotspots)
				{
					_target.playerTurnsHead = CustomGUILayout.Toggle ("Players turn heads when active?", _target.playerTurnsHead, string.Empty, "If True, then the player will turn their head when the Hotspot is selected");
				}

				EditorGUILayout.Space ();
				
				UseInteractionGUI ();
				
				if (settingsManager == null || settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive || settingsManager.interactionMethod == AC_InteractionMethod.CustomScript)
				{
					EditorGUILayout.Space ();
					LookInteractionGUI ();
				}
				
				EditorGUILayout.Space ();
				InvInteractionGUI ();

				if (KickStarter.cursorManager != null && KickStarter.cursorManager.AllowUnhandledIcons ())
				{
					EditorGUILayout.Space ();
					UnhandledUseInteractionGUI ();
				}

				EditorGUILayout.Space ();
				UnhandledInvInteractionGUI ();
			}
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		private void LookInteractionGUI ()
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Examine interaction", EditorStyles.boldLabel);
			
			if (!_target.provideLookInteraction)
			{
				if (GUILayout.Button (addContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Create examine interaction");
					_target.provideLookInteraction = true;
				}
			}
			else
			{
				if (GUILayout.Button (deleteContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Delete examine interaction");
					_target.provideLookInteraction = false;
				}
			}
			
			EditorGUILayout.EndHorizontal ();
			if (_target.provideLookInteraction)
			{
				ButtonGUI (_target.lookButton, "Examine", _target.interactionSource);
			}
			CustomGUILayout.EndVertical ();
		}
		
		
		private void UseInteractionGUI ()
		{
			if (settingsManager && settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive)
			{
				if (_target.UpgradeSelf ())
				{
					UnityVersionHandler.CustomSetDirty (_target);
				}
			}
			
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Use interactions", EditorStyles.boldLabel);
			
			if (GUILayout.Button (addContent, EditorStyles.miniButtonRight, buttonWidth))
			{
				Undo.RecordObject (_target, "Create use interaction");
				_target.useButtons.Add (new Button ());
				_target.provideUseInteraction = true;
			}
			EditorGUILayout.EndHorizontal();
			
			if (_target.provideUseInteraction)
			{
				if (cursorManager)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					int iconNumber;
					
					if (cursorManager.cursorIcons.Count > 0)
					{
						foreach (CursorIcon _icon in cursorManager.cursorIcons)
						{
							labelList.Add (_icon.id.ToString () + ": " + _icon.label);
						}
						
						foreach (Button useButton in _target.useButtons)
						{
							iconNumber = -1;
							
							int j = 0;
							foreach (CursorIcon _icon in cursorManager.cursorIcons)
							{
								// If an item has been removed, make sure selected variable is still valid
								if (_icon.id == useButton.iconID)
								{
									iconNumber = j;
									break;
								}
								j++;
							}
							
							if (iconNumber == -1)
							{
								// Wasn't found (item was deleted?), so revert to zero
								iconNumber = 0;
								useButton.iconID = 0;
							}
							
							EditorGUILayout.Space ();
							EditorGUILayout.BeginHorizontal ();
							
							iconNumber = CustomGUILayout.Popup ("Cursor / icon:", iconNumber, labelList.ToArray (), string.Empty, "The cursor/icon associated with the interaction");
							
							// Re-assign variableID based on PopUp selection
							useButton.iconID = cursorManager.cursorIcons[iconNumber].id;
							string iconLabel = cursorManager.cursorIcons[iconNumber].label;
							
							if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
							{
								SideMenu ("Use", _target.useButtons.Count, _target.useButtons.IndexOf (useButton));
							}
							
							EditorGUILayout.EndHorizontal ();
							ButtonGUI (useButton, iconLabel, _target.interactionSource);

							CustomGUILayout.DrawUILine ();
						}
					}					
					else
					{
						EditorGUILayout.LabelField ("No cursor icons exist!");
						iconNumber = -1;
						
						for (int i=0; i<_target.useButtons.Count; i++)
						{
							_target.useButtons[i].iconID = -1;
						}
					}
				}
				else
				{
					ACDebug.LogWarning ("A CursorManager is required to run the game properly - please open the Adventure Creator wizard and set one.");
				}
			}
			
			CustomGUILayout.EndVertical ();
		}
		
		
		private void InvInteractionGUI ()
		{
			CustomGUILayout.BeginVertical ();
			
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Inventory interactions", EditorStyles.boldLabel);
			
			if (GUILayout.Button (addContent, EditorStyles.miniButtonRight, buttonWidth))
			{
				Undo.RecordObject (_target, "Create inventory interaction");
				_target.invButtons.Add (new Button ());
				_target.provideInvInteraction = true;
			}
			EditorGUILayout.EndHorizontal();
			
			if (_target.provideInvInteraction)
			{
				if (inventoryManager)
				{
					// Create a string List of the field's names (for the PopUp box)
					List<string> labelList = new List<string>();
					int invNumber;
					
					if (inventoryManager.items.Count > 0)
					{
						foreach (InvItem _item in inventoryManager.items)
						{
							labelList.Add (_item.label);
						}
						
						foreach (Button invButton in _target.invButtons)
						{
							invNumber = -1;
							
							int j = 0;
							string invName = string.Empty;
							foreach (InvItem _item in inventoryManager.items)
							{
								// If an item has been removed, make sure selected variable is still valid
								if (_item.id == invButton.invID)
								{
									invNumber = j;
									invName = _item.label;
									break;
								}
								
								j++;
							}
							
							if (invNumber == -1)
							{
								// Wasn't found (item was deleted?), so revert to zero
								if (invButton.invID > 0) ACDebug.Log ("Previously chosen item no longer exists!");
								invNumber = 0;
								invButton.invID = 0;
							}
							
							EditorGUILayout.Space ();
							EditorGUILayout.BeginHorizontal ();
							
							invNumber = CustomGUILayout.Popup ("Inventory item:", invNumber, labelList.ToArray (), string.Empty, "The inventory item associated with the interaction");
							
							// Re-assign variableID based on PopUp selection
							invButton.invID = inventoryManager.items[invNumber].id;

							if (settingsManager != null && settingsManager.CanGiveItems ())
							{
								if (_target.GetComponent <Char>() != null || _target.GetComponentInParent <Char>())
								{
									invButton.selectItemMode = (SelectItemMode) EditorGUILayout.EnumPopup (invButton.selectItemMode, GUILayout.Width (70f));
								}
							}

							if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
							{
								SideMenu ("Inv", _target.invButtons.Count, _target.invButtons.IndexOf (invButton));
							}

							
							EditorGUILayout.EndHorizontal ();
							if (!string.IsNullOrEmpty (invName))
							{
								string label = invName;
								if (_target.GetComponent <Char>() && settingsManager != null && settingsManager.CanGiveItems ())
								{
									label = invButton.selectItemMode.ToString () + " " + label;
								}
								ButtonGUI (invButton, label, _target.interactionSource, true);
							}
							else
							{
								ButtonGUI (invButton, "Inventory", _target.interactionSource, true);
							}

							CustomGUILayout.DrawUILine ();
						}
						
					}					
					else
					{
						EditorGUILayout.LabelField ("No inventory items exist!");
						
						for (int i=0; i<_target.invButtons.Count; i++)
						{
							_target.invButtons[i].invID = -1;
						}
					}
				}
				else
				{
					ACDebug.LogWarning ("An InventoryManager is required to run the game properly - please open the Adventure Creator wizard and set one.");
				}
			}
			
			CustomGUILayout.EndVertical ();
		}


		private void UnhandledUseInteractionGUI ()
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Unhandled Use interaction", EditorStyles.boldLabel);

			if (!_target.provideUnhandledUseInteraction)
			{
				if (GUILayout.Button (addContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Create unhandled use interaction");
					_target.provideUnhandledUseInteraction = true;
				}
			}
			else
			{
				if (GUILayout.Button (deleteContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Delete unhandled use interaction");
					_target.provideUnhandledUseInteraction = false;
				}
			}
			EditorGUILayout.EndHorizontal();
			
			if (_target.provideUnhandledUseInteraction)
			{
				EditorGUILayout.Space ();
				ButtonGUI (_target.unhandledUseButton, "Unhandled use", _target.interactionSource, false);
				EditorGUILayout.HelpBox ("If the Interaction field is empty, the Cursor Manager's 'Unhandled interaction' asset file will be run following the Player action.", MessageType.Info);
			}
			
			CustomGUILayout.EndVertical ();
		}


		private void UnhandledInvInteractionGUI ()
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Unhandled Inventory interaction", EditorStyles.boldLabel);

			if (!_target.provideUnhandledInvInteraction)
			{
				if (GUILayout.Button (addContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Create unhandled inventory interaction");
					_target.provideUnhandledInvInteraction = true;
				}
			}
			else
			{
				if (GUILayout.Button (deleteContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Delete unhandled inventory interaction");
					_target.provideUnhandledInvInteraction = false;
				}
			}
			EditorGUILayout.EndHorizontal();
			
			if (_target.provideUnhandledInvInteraction)
			{
				EditorGUILayout.Space ();
				ButtonGUI (_target.unhandledInvButton, "Unhandled inventory", _target.interactionSource, true);
				EditorGUILayout.HelpBox ("This interaction will override any 'unhandled' ones defined in the Inventory Manager.", MessageType.Info);
			}
			
			CustomGUILayout.EndVertical ();
		}
		
		
		private void ButtonGUI (Button button, string suffix, InteractionSource source, bool isForInventory = false)
		{
			bool isEnabled = !button.isDisabled;
			isEnabled = CustomGUILayout.Toggle ("Enabled:", isEnabled);
			button.isDisabled = !isEnabled;
			
			if (source == InteractionSource.AssetFile)
			{
				EditorGUILayout.BeginHorizontal ();
				button.assetFile = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("Interaction:", button.assetFile, false, string.Empty, "The ActionList asset to run");
				if (button.assetFile == null)
				{
					if (GUILayout.Button ("Create", autoWidth))
					{
						string defaultName = GenerateInteractionName (suffix, true);

						#if !(UNITY_WP8 || UNITY_WINRT)
						defaultName = System.Text.RegularExpressions.Regex.Replace (defaultName, "[^\\w\\._]", string.Empty);
						#else
						defaultName = string.Empty;
						#endif

						button.assetFile = ActionListAssetMenu.CreateAsset (defaultName);
					}
				}
				else if (GUILayout.Button (string.Empty, CustomStyles.IconNodes))
				{
					ActionListEditorWindow.Init (button.assetFile);
				}
				EditorGUILayout.EndHorizontal ();

				if (button.assetFile != null && button.assetFile.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					button.parameterID = Action.ChooseParameterGUI ("Hotspot parameter:", button.assetFile.DefaultParameters, button.parameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this Hotspot");
					EditorGUILayout.EndHorizontal ();

					if (isForInventory)
					{
						button.invParameterID = Action.ChooseParameterGUI ("Inventory item parameter:", button.assetFile.DefaultParameters, button.invParameterID, ParameterType.InventoryItem, -1, "The Inventory Item parameter to automatically assign as the used item");
					}
				}
			}
			else if (source == InteractionSource.CustomScript)
			{
				button.customScriptObject = (GameObject) CustomGUILayout.ObjectField <GameObject> ("Object with script:", button.customScriptObject, true, string.Empty, "The GameObject with the custom script to run");
				button.customScriptFunction = CustomGUILayout.TextField ("Message to send:", button.customScriptFunction, string.Empty, "The name of the function to run");

				if (isForInventory)
				{
					EditorGUILayout.HelpBox ("If the receiving function has an integer parameter, the Inventory item's ID will be passed to it.", MessageType.Info);
				}
			}
			else if (source == InteractionSource.InScene)
			{
				EditorGUILayout.BeginHorizontal ();
				button.interaction = (Interaction) CustomGUILayout.ObjectField <Interaction> ("Interaction:", button.interaction, true, string.Empty, "The Interaction ActionList to run");
				
				if (button.interaction == null)
				{
					if (GUILayout.Button ("Create", autoWidth))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent <Interaction>();
						
						newInteraction.gameObject.name = GenerateInteractionName (suffix, false);
						button.interaction = newInteraction;
					}
				}
				else
				{
					if (GUILayout.Button (string.Empty, CustomStyles.IconNodes))
					{
						ActionListEditorWindow.Init (button.interaction);
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (button.interaction != null && button.interaction.source == ActionListSource.InScene && button.interaction.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					button.parameterID = Action.ChooseParameterGUI ("Hotspot parameter:", button.interaction.parameters, button.parameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this Hotspot");
					EditorGUILayout.EndHorizontal ();

					if (isForInventory)
					{
						button.invParameterID = Action.ChooseParameterGUI ("Inventory item parameter:", button.interaction.parameters, button.invParameterID, ParameterType.InventoryItem, -1, "The Inventory Item parameter to automatically assign as the used item");
					}
				}
				else if (button.interaction != null && button.interaction.source == ActionListSource.AssetFile && button.interaction.assetFile != null && button.interaction.assetFile.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					button.parameterID = Action.ChooseParameterGUI ("Hotspot parameter:", button.interaction.assetFile.DefaultParameters, button.parameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this Hotspot");
					EditorGUILayout.EndHorizontal ();

					if (isForInventory)
					{
						button.invParameterID = Action.ChooseParameterGUI ("Inventory item parameter:", button.interaction.assetFile.DefaultParameters, button.invParameterID, ParameterType.InventoryItem, -1, "The Inventory Item parameter to automatically assign as the used item");
					}
				}
			}
			
			button.playerAction = (PlayerAction) CustomGUILayout.EnumPopup ("Player action:", button.playerAction, string.Empty, "What the Player prefab does after clicking the Hotspot, but before the Interaction itself is run");
			
			if (button.playerAction == PlayerAction.WalkTo || button.playerAction == PlayerAction.WalkToMarker)
			{
				if (button.playerAction == PlayerAction.WalkToMarker && _target.walkToMarker == null)
				{
					EditorGUILayout.HelpBox ("A 'Walk-to marker' must be assigned above for this option to work.", MessageType.Warning);
				}
				button.isBlocking = CustomGUILayout.Toggle ("Cutscene while moving?", button.isBlocking, string.Empty, "If True, then gameplay will be blocked while the Player moves");
				button.faceAfter = CustomGUILayout.Toggle ("Face after moving?", button.faceAfter, string.Empty, "If True, then the Player will face the Hotspot after reaching the Marker");
				
				if (button.playerAction == PlayerAction.WalkTo)
				{
					button.setProximity = CustomGUILayout.Toggle ("Set minimum distance?", button.setProximity, string.Empty, "If True, then the Interaction will be run once the Player is within a certain distance of the Hotspot");
					if (button.setProximity)
					{
						button.proximity = CustomGUILayout.FloatField ("Proximity:", button.proximity, string.Empty, "The proximity the Player must be within");
					}
				}
				if (button.playerAction == PlayerAction.WalkToMarker && _target.walkToMarker && !button.isBlocking && _target.doubleClickingHotspot == DoubleClickingHotspot.TriggersInteractionInstantly)
				{
					if (_target.oneClick || (settingsManager != null && settingsManager.interactionMethod == AC_InteractionMethod.ContextSensitive))
					{
						if (!(settingsManager != null && settingsManager.interactionMethod == AC_InteractionMethod.CustomScript))
						{
							bool doubleClickSnapsToMarker = !button.doubleClickDoesNotSnapPlayerToMarker;
							doubleClickSnapsToMarker = CustomGUILayout.Toggle ("Snap Player if double-click?", doubleClickSnapsToMarker, string.Empty, "If True, then double-clicking the Hotspot will snap the Player to the Walk-to Marker before the Interaction is run");
							button.doubleClickDoesNotSnapPlayerToMarker = !doubleClickSnapsToMarker;
						}
					}
				}
			}
		}


		private string GenerateInteractionName (string suffix, bool isAsset)
		{
			string hotspotName = _target.gameObject.name;
			if (_target != null && _target.hotspotName != null && _target.hotspotName.Length > 0)
			{
				hotspotName = _target.hotspotName;
			}
			return AdvGame.UniqueName (hotspotName + ((isAsset) ? "_" : ": ") + suffix);
		}


		private void SideMenu (string suffix, int listSize, int index)
		{
			GenericMenu menu = new GenericMenu ();
			sideIndex = index;

			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert " + suffix);
			if (listSize > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete " + suffix);
			}
			if (index > 0 || index < listSize-1)
			{
				menu.AddSeparator (string.Empty);
			}

			if (index > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, Callback, "Move to top " + suffix);
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up " + suffix);
			}
			if (index < (listSize - 1))
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down " + suffix);
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, Callback, "Move to bottom " + suffix);
			}

			menu.ShowAsContext ();
		}
		
		
		private void Callback (object obj)
		{
			if (sideIndex >= 0)
			{
				switch (obj.ToString ())
				{
				case "Insert Use":
					Undo.RecordObject (_target, "Insert Interaction");
					_target.useButtons.Insert (sideIndex+1, new Button ());
					break;
					
				case "Delete Use":
					Undo.RecordObject (_target, "Delete Interaction");
					_target.useButtons.RemoveAt (sideIndex);
					if (_target.useButtons.Count == 0)
					{
						_target.provideUseInteraction = false;
					}
					break;

				case "Move up Use":
					Undo.RecordObject (_target, "Move Interaction up");
					Button tempButton = _target.useButtons [sideIndex];
					_target.useButtons.RemoveAt (sideIndex);
					_target.useButtons.Insert (sideIndex-1, tempButton);
					break;
					
				case "Move down Use":
					Undo.RecordObject (_target, "Move Interaction down");
					Button tempButton2 = _target.useButtons [sideIndex];
					_target.useButtons.RemoveAt (sideIndex);
					_target.useButtons.Insert (sideIndex+1, tempButton2);
					break;

				case "Move to top Use":
					Undo.RecordObject (_target, "Move Interaction to top");
					Button tempButton3 = _target.useButtons [sideIndex];
					_target.useButtons.RemoveAt (sideIndex);
					_target.useButtons.Insert (0, tempButton3);
					break;
				
				case "Move to bottom Use":
					Undo.RecordObject (_target, "Move Interaction to bottom");
					Button tempButton4 = _target.useButtons [sideIndex];
					_target.useButtons.RemoveAt (sideIndex);
					_target.useButtons.Insert (_target.useButtons.Count, tempButton4);
					break;
				
				case "Insert Inv":
					Undo.RecordObject (_target, "Insert Interaction");
					_target.invButtons.Insert (sideIndex+1, new Button ());
					break;
				
				case "Delete Inv":
					Undo.RecordObject (_target, "Delete Interaction");
					_target.invButtons.RemoveAt (sideIndex);
					if (_target.invButtons.Count == 0)
					{
						_target.provideInvInteraction = false;
					}
					break;
				
				case "Move up Inv":
					Undo.RecordObject (_target, "Move Interaction up");
					Button tempButton5 = _target.invButtons [sideIndex];
					_target.invButtons.RemoveAt (sideIndex);
					_target.invButtons.Insert (sideIndex-1, tempButton5);
					break;
				
				case "Move down Inv":
					Undo.RecordObject (_target, "Move Interaction down");
					Button tempButton6 = _target.invButtons [sideIndex];
					_target.invButtons.RemoveAt (sideIndex);
					_target.invButtons.Insert (sideIndex+1, tempButton6);
					break;

				case "Move to top Inv":
					Undo.RecordObject (_target, "Move Interaction to top");
					Button tempButton7 = _target.invButtons [sideIndex];
					_target.invButtons.RemoveAt (sideIndex);
					_target.invButtons.Insert (0, tempButton7);
					break;
				
				case "Move to bottom Inv":
					Undo.RecordObject (_target, "Move Interaction to bottom");
					Button tempButton8 = _target.invButtons [sideIndex];
					_target.invButtons.RemoveAt (sideIndex);
					_target.invButtons.Insert (_target.invButtons.Count, tempButton8);
					break;
					
				}
				
			}
		}

	}
	
}

#endif