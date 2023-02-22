#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(ArrowPrompt))]
	public class ArrowPromptEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			ArrowPrompt _target = (ArrowPrompt) target;
			
			CustomGUILayout.BeginVertical ();
			GUILayout.Label ("Settings", EditorStyles.boldLabel);
			_target.arrowPromptType = (ArrowPromptType) CustomGUILayout.EnumPopup ("Input type:", _target.arrowPromptType, "", "What kind of input the arrows respond to");
			_target.disableHotspots = CustomGUILayout.ToggleLeft ("Disable Hotspots when active?", _target.disableHotspots, "", "If True, then Hotspots will be disabled when the arrows are on screen");
			_target.positionFactor = CustomGUILayout.Slider ("Position factor:", _target.positionFactor, 0.5f, 4f, "", "A factor for the arrow position");
			_target.scaleFactor = CustomGUILayout.Slider ("Scale factor:", _target.scaleFactor, 0.5f, 4f, "", "A factor for the arrow size");
			_target.source = (ActionListSource) CustomGUILayout.EnumPopup ("Actions source:", _target.source, "", "Where the Actions are stored when not being run");
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			GUILayout.Label ("Up arrow", EditorStyles.boldLabel);
			ArrowGUI (_target.upArrow, _target.source, "Up");
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();
			
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			GUILayout.Label ("Left arrow", EditorStyles.boldLabel);
			ArrowGUI (_target.leftArrow, _target.source, "Left");
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			GUILayout.Label ("Right arrow", EditorStyles.boldLabel);
			ArrowGUI (_target.rightArrow, _target.source, "Right");
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			GUILayout.Label ("Down arrow", EditorStyles.boldLabel);
			ArrowGUI (_target.downArrow, _target.source, "Down");
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		private void ArrowGUI (Arrow arrow, ActionListSource source, string label)
		{
			if (arrow != null)
			{
				ArrowPrompt _target = (ArrowPrompt) target;

				arrow.isPresent = CustomGUILayout.Toggle ("Provide?", arrow.isPresent, "", "If True, the Arrow is defined and used in the ArrowPrompt");
			
				if (arrow.isPresent)
				{
					arrow.texture = (Texture2D) CustomGUILayout.ObjectField <Texture2D> ("Icon texture:", arrow.texture, true, "", "The texture to draw on-screen");

					EditorGUILayout.BeginHorizontal ();
					if (source == ActionListSource.InScene)
					{
						arrow.linkedCutscene = ActionListAssetMenu.CutsceneGUI ("Linked Cutscene", arrow.linkedCutscene, _target.gameObject.name + ": " + label, "The Cutscene to run when the Arrow is triggered");
					}
					else if (source == ActionListSource.AssetFile)
					{
						arrow.linkedActionList = ActionListAssetMenu.AssetGUI ("Linked ActionList:", arrow.linkedActionList, _target.gameObject.name + "_" + label, "", "The ActionList asset to run when the Arrow is triggered");
					}
					EditorGUILayout.EndHorizontal ();
				}
			}	
		}

	}

}

#endif