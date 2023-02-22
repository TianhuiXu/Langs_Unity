#if UNITY_EDITOR

/*
*
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ACInstaller.cs"
 * 
 *	This script contains functions used to set up AC within Unity automatically.
 *	It includes adapted code originally by Leslie Young: http://plyoung.appspot.com/blog/manipulating-input-manager-in-script.html and http://forum.unity3d.com/threads/adding-layer-by-script.41970/
 * 
 */

using UnityEditor;

namespace AC
{

	[InitializeOnLoad]
	public class ACInstaller
	{

		private const string defaultNavMeshLayer = "NavMesh";
		private const string defaultBackgroundImageLayer = "BackgroundImage";
		private const string defaultDistantHotspotLayer = "DistantHotspot";
		private const string defaultMenuAxis = "Menu";

		static ACInstaller ()
		{
			CheckInstall ();
		}


		public static bool IsInstalled ()
		{
			if (ACEditorPrefs.DisableInstaller)
			{
				return true;
			}

			if (IsAxisDefined (defaultMenuAxis) && IsLayerDefined (defaultNavMeshLayer) && IsLayerDefined (defaultBackgroundImageLayer) && IsLayerDefined (defaultDistantHotspotLayer))
			{
				return true;
			}
			return false;
		}


		private static void CheckInstall ()
		{
			if (!IsInstalled ())
			{
				DoInstall ();
			}
		}


		public static void DoInstall ()
		{
			bool gotMenu = IsAxisDefined (defaultMenuAxis);
			bool gotNavMesh = IsLayerDefined (defaultNavMeshLayer);
			bool gotBackgroundImage = IsLayerDefined (defaultBackgroundImageLayer);
			bool gotDistantHotspot = IsLayerDefined (defaultDistantHotspotLayer);

			if (!gotMenu || !gotNavMesh || !gotBackgroundImage || !gotDistantHotspot)
			{
				string changesToMake = string.Empty;
				
				if (!gotMenu)
				{
					changesToMake += "'Menu' - an input used to open the Pause menu\r\n";
				}
				if (!gotNavMesh)
				{
					changesToMake += "'" + defaultNavMeshLayer + "' - a Layer used for pathfinding\r\n";
				}
				if (!gotBackgroundImage)
				{
					changesToMake += "'" + defaultBackgroundImageLayer + "' - a Layer used by 2.5D cameras\r\n";
				}
				if (!gotDistantHotspot)
				{
					changesToMake += "'" + defaultDistantHotspotLayer + "' - a Layer used by Hotspots too far away\r\n";
				}

				bool canProceed = EditorUtility.DisplayDialog ("Adventure Creator installation", "Adventure Creator requires that the following be created:\r\n\r\n" + changesToMake + "\r\nAC will now make the necessary changes automatically.", "OK");
				if (canProceed)
				{
					DefineInputs ();
					DefineLayers ();

					//Want to have this open, but Unity 2017.1 has a bug
					//AboutWindow.Init ();
				}
			}
		}


		private static void DefineInputs ()
		{
			AddAxis (new InputAxis ()
			{
				name = defaultMenuAxis,
				positiveButton = "escape",
				gravity = 1000f,
				dead = 0.001f,
				sensitivity = 1000f,
				type = AxisType.KeyOrMouseButton,
				axis = 1
			});
		}


		private static void DefineLayers ()
		{
			IsLayerDefined (defaultNavMeshLayer, true);
			IsLayerDefined (defaultBackgroundImageLayer, true);
			IsLayerDefined (defaultDistantHotspotLayer, true);
		}


		// Inputs

		private enum AxisType
		{
			KeyOrMouseButton = 0,
			MouseMovement = 1,
			JoystickAxis = 2
		};


		private class InputAxis
		{
			public string name = "";
			public string descriptiveName = "";
			public string descriptiveNegativeName = "";
			public string negativeButton = "";
			public string positiveButton = "";
			public string altNegativeButton = "";
			public string altPositiveButton = "";

			public float gravity = 0f;
			public float dead = 0f;
			public float sensitivity = 0f;

			public bool snap = false;
			public bool invert = false;

			public AxisType type = AxisType.KeyOrMouseButton;

			public int axis = 0;
			public int joyNum = 0;
		}


		private static void AddAxis (InputAxis axis)
		{
			if (IsAxisDefined (axis.name))
			{
				return;
			}

			SerializedObject serializedObject = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
			SerializedProperty axesProperty = serializedObject.FindProperty ("m_Axes");

			axesProperty.arraySize ++;
			serializedObject.ApplyModifiedProperties ();

			SerializedProperty axisProperty = axesProperty.GetArrayElementAtIndex (axesProperty.arraySize - 1);

			GetChildProperty (axisProperty, "m_Name").stringValue = axis.name;
			GetChildProperty (axisProperty, "descriptiveName").stringValue = axis.descriptiveName;
			GetChildProperty (axisProperty, "descriptiveNegativeName").stringValue = axis.descriptiveNegativeName;
			GetChildProperty (axisProperty, "negativeButton").stringValue = axis.negativeButton;
			GetChildProperty (axisProperty, "positiveButton").stringValue = axis.positiveButton;
			GetChildProperty (axisProperty, "altNegativeButton").stringValue = axis.altNegativeButton;
			GetChildProperty (axisProperty, "altPositiveButton").stringValue = axis.altPositiveButton;
			GetChildProperty (axisProperty, "gravity").floatValue = axis.gravity;
			GetChildProperty (axisProperty, "dead").floatValue = axis.dead;
			GetChildProperty (axisProperty, "sensitivity").floatValue = axis.sensitivity;
			GetChildProperty (axisProperty, "snap").boolValue = axis.snap;
			GetChildProperty (axisProperty, "invert").boolValue = axis.invert;
			GetChildProperty (axisProperty, "type").intValue = (int)axis.type;
			GetChildProperty (axisProperty, "axis").intValue = axis.axis - 1;
			GetChildProperty (axisProperty, "joyNum").intValue = axis.joyNum;

			serializedObject.ApplyModifiedProperties ();

			ACDebug.Log ("Created input: '" + axis.name + "'");
		}


		private static SerializedProperty GetChildProperty (SerializedProperty parent, string nameToFind)
		{
			SerializedProperty child = parent.Copy ();
			if (child.Next (true))
			{
				do
				{
					if (child.name == nameToFind)
					{
						return child;
					}
				}
				while (child.Next (false));
			}

			return null;
		}


		private static bool IsAxisDefined (string axisName)
		{
			UnityEngine.Object[] inputAssets = AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/InputManager.asset");
			if (inputAssets != null && inputAssets.Length > 0)
			{
				SerializedObject inputManager = new SerializedObject (inputAssets[0]);
				SerializedProperty allAxes = inputManager.FindProperty ("m_Axes");

				if (allAxes == null || !allAxes.isArray)
				{
					return false;
				}

				allAxes.Next (true);
				allAxes.Next (true);

				while (allAxes.Next (false))
				{
					SerializedProperty axis = allAxes.Copy ();
					if (axis.Next (true) && axis.stringValue == axisName)
					{
						return true;
					}
				}

				return false;
			}

			// Can't access somehow, so ignore to avoid spamming
			return true;
		}


		// Layers

		private static bool IsLayerDefined (string layerName, bool addIfUndefined = false)
		{
			SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/TagManager.asset")[0]);
			SerializedProperty allLayers = tagManager.FindProperty ("layers");
			if (allLayers == null || !allLayers.isArray)
			{
				return false;
			}

			// Check if layer is present
			bool foundLayer = false;
			for (int i = 0; i <= 31; i++)
			{
				SerializedProperty sp = allLayers.GetArrayElementAtIndex (i);

				if (sp != null && layerName.Equals (sp.stringValue))
				{
					foundLayer = true;
					break;
				}
			}

			if (!addIfUndefined)
			{
				return foundLayer;
			}

			if (foundLayer)
			{
				return true;
			}

		   	// Create layer
			SerializedProperty slot = null;
			for (int i = 8; i <= 31; i++)
			{
				SerializedProperty sp = allLayers.GetArrayElementAtIndex (i);

				if (sp != null && string.IsNullOrEmpty (sp.stringValue))
				{
					slot = sp;
					break;
				}
			}

			if (slot != null)
			{
				slot.stringValue = layerName;
			}
			else
			{
				ACDebug.LogError ("Could not find an open Layer Slot for: " + layerName);
				return false;
			}

			tagManager.ApplyModifiedProperties ();

			ACDebug.Log ("Created layer: '" + layerName + "'");
			return true;
		} 

	}

}

#endif