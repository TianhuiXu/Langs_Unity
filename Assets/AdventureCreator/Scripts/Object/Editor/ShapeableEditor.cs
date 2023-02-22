#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(Shapeable))]
	public class ShapeableEditor : Editor
	{
		
		private ShapeGroup selectedGroup;
		private ShapeKey selectedKey;
		private Shapeable _target;


		public override void OnInspectorGUI ()
		{
			_target = (Shapeable) target;
			
			_target.shapeGroups = AllGroupsGUI (_target.shapeGroups);
			
			if (selectedGroup != null)
			{
				List<string> blendShapeNames = new List<string>();
				if (_target.GetComponent <SkinnedMeshRenderer>() && _target.GetComponent <SkinnedMeshRenderer>().sharedMesh)
				{
					for (int i=0; i<_target.GetComponent <SkinnedMeshRenderer>().sharedMesh.blendShapeCount; i++)
					{
						blendShapeNames.Add (_target.GetComponent <SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName (i));
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("This component should be attached to a Skinned Mesh Renderer.", MessageType.Warning);
				}

				selectedGroup = GroupGUI (selectedGroup, blendShapeNames.ToArray ());
			}
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		private ShapeGroup GroupGUI (ShapeGroup shapeGroup, string[] blendShapeNames)
		{
			EditorGUILayout.Space ();
			
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			CustomGUILayout.ToggleHeader (true, "Shape group " + shapeGroup.label);

			shapeGroup.label = CustomGUILayout.TextField ("Group label:", shapeGroup.label, "", "The editor-friendly name of the group");
			shapeGroup.shapeKeys = AllKeysGUI (shapeGroup.shapeKeys);
			
			EditorGUILayout.EndVertical ();
			
			if (selectedKey != null && shapeGroup.shapeKeys.Contains (selectedKey))
			{
				selectedKey = KeyGUI (selectedKey, blendShapeNames);
			}
			
			return shapeGroup;
		}
		
		
		private ShapeKey KeyGUI (ShapeKey shapeKey, string[] blendShapeNames)
		{
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			CustomGUILayout.ToggleHeader (true, "Shape key " + shapeKey.label);

			shapeKey.Upgrade ();
			shapeKey.label = CustomGUILayout.TextField ("Key label:", shapeKey.label, "", "An editor-friendly name of the blendshape");
			
			foreach (ShapeKeyBlendshape blendshape in shapeKey.blendshapes)
			{
				CustomGUILayout.BeginHorizontal ();
				if (blendShapeNames != null && blendShapeNames.Length > 0)
				{
					blendshape.index = CustomGUILayout.Popup ("Blendshape:", blendshape.index, blendShapeNames, "", "The Blendshape that this relates to");
				}
				else
				{
					blendshape.index = CustomGUILayout.IntField ("BlendShape index:", blendshape.index, "", "The Blendshape that this relates to");
				}

				if (shapeKey.blendshapes.Count > 1 && GUILayout.Button ("-", GUILayout.Width (20f), GUILayout.Height (15f)))
				{
					Undo.RecordObject (_target, "Delete shapekey Blendshape");
					shapeKey.blendshapes.Remove (blendshape);
					selectedGroup = null;
					selectedKey = null;
					break;
				}

				CustomGUILayout.EndHorizontal ();
				blendshape.relativeIntensity = CustomGUILayout.Slider ("   Relative intensity:", blendshape.relativeIntensity, 0f, 100f, "", "The relative intensity (from 0 -> 100) of the Blendshape when the Key is fully active");

				if (shapeKey.blendshapes.IndexOf (blendshape) < (shapeKey.blendshapes.Count - 1))
				{
					CustomGUILayout.DrawUILine ();
				}
			}

			if (GUILayout.Button ("Add new Blendshape"))
			{
				Undo.RecordObject (_target, "Add new Blendshape");
				shapeKey.blendshapes.Add (new ShapeKeyBlendshape (0));
			}

			EditorGUILayout.EndVertical ();

			return shapeKey;
		}
		
		
		private List<ShapeGroup> AllGroupsGUI (List<ShapeGroup> shapeGroups)
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			CustomGUILayout.ToggleHeader (true, "Shape groups");

			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				EditorGUILayout.BeginHorizontal ();
				
				string buttonLabel = shapeGroup.ID + ": ";
				if (shapeGroup.label == "")
				{
					buttonLabel += "(Untitled)";	
				}
				else
				{
					buttonLabel += shapeGroup.label;
				}
				
				bool buttonOn = (selectedGroup == shapeGroup);
				if (GUILayout.Toggle (buttonOn, buttonLabel, "Button"))
				{
					if (selectedGroup != shapeGroup)
					{
						selectedGroup = shapeGroup;
						selectedKey = null;
					}
				}
				
				if (GUILayout.Button ("-", GUILayout.Width (20f), GUILayout.Height (15f)))
				{
					Undo.RecordObject (_target, "Delete shape group");
					shapeGroups.Remove (shapeGroup);
					selectedGroup = null;
					selectedKey = null;
					break;
				}
				
				EditorGUILayout.EndHorizontal ();
			}
			
			if (GUILayout.Button ("Create new shape group"))
			{
				Undo.RecordObject (_target, "Create new shape group");
				ShapeGroup newShapeGroup = new ShapeGroup (GetIDArray (shapeGroups));
				shapeGroups.Add (newShapeGroup);
				selectedGroup = newShapeGroup;
				selectedKey = null;
			}

			EditorGUILayout.EndVertical ();
			
			return shapeGroups;
		}
		
		
		private List<ShapeKey> AllKeysGUI (List<ShapeKey> shapeKeys)
		{
			EditorGUILayout.LabelField ("Shape keys", EditorStyles.boldLabel);
			
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				EditorGUILayout.BeginHorizontal ();
				
				string buttonLabel = shapeKey.ID + ": ";
				if (shapeKey.label == "")
				{
					buttonLabel += "(Untitled)";	
				}
				else
				{
					buttonLabel += shapeKey.label;
				}
				
				bool buttonOn = (selectedKey == shapeKey);
				if (GUILayout.Toggle (buttonOn, buttonLabel, "Button"))
				{
					selectedKey = shapeKey;
				}
				
				if (GUILayout.Button ("-", GUILayout.Width (20f), GUILayout.Height (15f)))
				{
					Undo.RecordObject (_target, "Delete shape key");
					shapeKeys.Remove (shapeKey);
					selectedKey = null;
					break;
				}
				
				EditorGUILayout.EndHorizontal ();
			}
			
			if (GUILayout.Button ("Create new shape key"))
			{
				Undo.RecordObject (_target, "Create new shape key");
				ShapeKey newShapeKey = new ShapeKey (GetIDArray (shapeKeys));
				shapeKeys.Add (newShapeKey);
				selectedKey = newShapeKey;
			}
			
			return shapeKeys;
		}
		
		
		private int[] GetIDArray (List<ShapeKey> shapeKeys)
		{
			List<int> idArray = new List<int>();
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				idArray.Add (shapeKey.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
		
		private int[] GetIDArray (List<ShapeGroup> shapeGroups)
		{
			List<int> idArray = new List<int>();
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				idArray.Add (shapeGroup.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
	}

}

#endif