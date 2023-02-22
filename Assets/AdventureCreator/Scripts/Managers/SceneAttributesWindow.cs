#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/** Provides an EditorWindow to manage available scene attributes. */
	public class SceneAttributesWindow : EditorWindow
	{

		private Vector2 scrollPos;
		private InvVar selectedSceneAttribute = null;
		private List<InvVar> sceneAttributes = new List<InvVar>();
		private int sideItem = 0;

		private bool showAll = true;
		private bool showSelected = true;


		/** Initialises the window. */
		public static void Init ()
		{
			SceneAttributesWindow window = (SceneAttributesWindow) GetWindow (typeof (SceneAttributesWindow));
			window.titleContent.text = "Scene attributes";
			window.position = new Rect (300, 200, 350, 360);
			window.minSize = new Vector2 (300, 160);
		}
		
		
		private void OnGUI ()
		{
			if (AdvGame.GetReferences ().settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			EditorGUILayout.LabelField ("Scene attributes", CustomStyles.managerHeader);

			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			sceneAttributes = settingsManager.sceneAttributes;

			EditorGUILayout.HelpBox ("Values for attributes defined here can be set in the Scene Manager, and checked using the 'Scene: Check attribute' Action.", MessageType.Info);

			CreateAttributesGUI ();

			if (selectedSceneAttribute != null && sceneAttributes.Contains (selectedSceneAttribute))
			{
				EditorGUILayout.Space ();

				string apiPrefix = "AC.KickStarter.variablesManager.GetProperty (" + selectedSceneAttribute.id + ")";
				
				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				showSelected = CustomGUILayout.ToggleHeader (showSelected, "Attribute '" + selectedSceneAttribute.label + "' properties");
				if (showSelected)
				{
					selectedSceneAttribute.label = CustomGUILayout.TextField ("Name:", selectedSceneAttribute.label, apiPrefix + ".label");
					selectedSceneAttribute.type = (VariableType) CustomGUILayout.EnumPopup ("Type:", selectedSceneAttribute.type, apiPrefix + ".type");
					if (selectedSceneAttribute.type == VariableType.PopUp)
					{
						selectedSceneAttribute.popUps = VariablesManager.PopupsGUI (selectedSceneAttribute.popUps);
					}
				}

				CustomGUILayout.EndVertical ();
			}

			settingsManager.sceneAttributes = sceneAttributes;
			if (GUI.changed)
			{
				EditorUtility.SetDirty (settingsManager);
			}
		}


		private void CreateAttributesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showAll = CustomGUILayout.ToggleHeader (showAll, "All attributes");
			if (showAll)
			{
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos);
				foreach (InvVar sceneAttribute in sceneAttributes)
				{
					EditorGUILayout.BeginHorizontal ();
				
					string buttonLabel = sceneAttribute.label;
					if (buttonLabel == "")
					{
						buttonLabel = "(Untitled)";	
					}
				
					if (GUILayout.Toggle (selectedSceneAttribute == sceneAttribute, sceneAttribute.id + ": " + buttonLabel, "Button"))
					{
						if (selectedSceneAttribute != sceneAttribute)
						{
							DeactivateAllAttributes ();
							ActivateAttribute (sceneAttribute);
						}
					}
				
					if (GUILayout.Button ("", CustomStyles.IconCog))
					{
						SideMenu (sceneAttribute);
					}
				
					EditorGUILayout.EndHorizontal ();
				}
				EditorGUILayout.EndScrollView ();
			
				if (GUILayout.Button ("Create new scene attribute"))
				{
					Undo.RecordObject (this, "Create scene attribute");
				
					InvVar newSceneAttribute = new InvVar (GetIDArray ());
					sceneAttributes.Add (newSceneAttribute);
					DeactivateAllAttributes ();
					ActivateAttribute (newSceneAttribute);
				}
			}
			CustomGUILayout.EndVertical ();
		}


		private void ActivateAttribute (InvVar sceneAttribute)
		{
			selectedSceneAttribute = sceneAttribute;
			EditorGUIUtility.editingTextField = false;
		}
		

		private void DeactivateAllAttributes ()
		{
			selectedSceneAttribute = null;
		}


		private int[] GetIDArray ()
		{
			List<int> idArray = new List<int>();
			for (int i=0; i<sceneAttributes.Count; i++)
			{
				idArray.Add (sceneAttributes[i].id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private void SideMenu (InvVar invVar)
		{
			GenericMenu menu = new GenericMenu ();
 			sideItem = sceneAttributes.IndexOf (invVar);
			
			menu.AddItem (new GUIContent ("Insert after"), false, PropertyCallback, "Insert after");
			if (sceneAttributes.Count > 0)
			{
				menu.AddItem (new GUIContent ("Delete"), false, PropertyCallback, "Delete");
			}
			if (sideItem > 0 || sideItem < sceneAttributes.Count-1)
			{
				menu.AddSeparator ("");
			}
			if (sideItem > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move to top"), false, PropertyCallback, "Move to top");
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, PropertyCallback, "Move up");
			}
			if (sideItem < sceneAttributes.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, PropertyCallback, "Move down");
				menu.AddItem (new GUIContent ("Re-arrange/Move to bottom"), false, PropertyCallback, "Move to bottom");
			}
			
			menu.ShowAsContext ();
		}
		

		private void PropertyCallback (object obj)
		{
			if (sideItem >= 0)
			{
				InvVar tempVar = sceneAttributes[sideItem];
				
				switch (obj.ToString ())
				{
				case "Insert after":
					Undo.RecordObject (this, "Insert item");
					sceneAttributes.Insert (sideItem+1, new InvVar (GetIDArray ()));
					break;
					
				case "Delete":
					Undo.RecordObject (this, "Delete item");
					DeactivateAllAttributes ();
					sceneAttributes.RemoveAt (sideItem);
					break;

				case "Move to top":
					sceneAttributes.RemoveAt (sideItem);
					sceneAttributes.Insert (0, tempVar);
					break;
					
				case "Move up":
					Undo.RecordObject (this, "Move item up");
					sceneAttributes.RemoveAt (sideItem);
					sceneAttributes.Insert (sideItem-1, tempVar);
					break;
					
				case "Move down":
					Undo.RecordObject (this, "Move item down");
					sceneAttributes.RemoveAt (sideItem);
					sceneAttributes.Insert (sideItem+1, tempVar);
					break;

				case "Move to bottom":
					sceneAttributes.RemoveAt (sideItem);
					sceneAttributes.Insert (sceneAttributes.Count, tempVar);
					break;
				}
			}
			
			EditorUtility.SetDirty (this);
			AssetDatabase.SaveAssets ();
			
			sideItem = -1;
		}

	}
	
}

#endif