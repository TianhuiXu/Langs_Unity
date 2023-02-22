#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	/** Provides an EditorWindow to create Variables in bulk. */
	public class BulkCreateVarsWindow : EditorWindow
	{

		private VariableLocation location = VariableLocation.Global;
		private int numVars = 1;
		private Variables variables;
		private GVar newVar = null;
		private Vector2 scrollPos;


		/**
		 * <summary>Initialises the window.</summary>
		 * <param name = "_location">Where to place the new variables, once created</param>
		 * <param name = "_variables">The Variables component, if the location is VariableLocation.Component</param>
		 */
		public static void Init (VariableLocation _location, Variables _variables = null)
		{
			string title = "Bulk-create " + _location + " vars";
			BulkCreateVarsWindow window = (BulkCreateVarsWindow) GetWindow (typeof (BulkCreateVarsWindow));
			window.titleContent.text = title;
			window.position = new Rect (300, 200, 450, 270);

			window.location = _location;
			window.variables = _variables;
			if (_location != VariableLocation.Component) window.variables = null;

			window.minSize = new Vector2 (300, 180);
		}


		private void OnGUI ()
		{
			scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

			EditorGUILayout.LabelField ("Bulk-create " + location + " variables", CustomStyles.managerHeader);

			if (newVar == null) newVar = new GVar ();

			numVars = EditorGUILayout.IntSlider ("# of new variables:", numVars, 1, 20);

			newVar.ShowGUI (location, true, null, string.Empty, variables);

			EditorGUILayout.Space ();

			GUI.enabled = !string.IsNullOrEmpty (newVar.label);
			if (GUILayout.Button ("Bulk-create"))
			{
				Create ();
			}
			GUI.enabled = true;

			EditorGUILayout.EndScrollView ();
		}


		private void Create ()
		{
			switch (location)
			{
				case VariableLocation.Global:
					if (KickStarter.variablesManager == null) return;
					Undo.RecordObject (KickStarter.variablesManager, "Add " + location + " variables");
					break;

				case VariableLocation.Local:
					if (KickStarter.localVariables == null) return;
					Undo.RecordObject (KickStarter.localVariables, "Add " + location + " variables");
					break;

				case VariableLocation.Component:
					if (variables == null) return;
					Undo.RecordObject (variables, "Add " + location + " variables");
					break;

				default:
					break;
			}

			if (Vars == null) return;

			for (int i=0; i<numVars; i++)
			{
				GVar variable = new GVar (newVar);
				variable.AssignUniqueID (GetIDArray ());
				variable.label = newVar.label + "_" + i.ToString ();
				Vars.Add (variable);
			}

			switch (location)
			{
				case VariableLocation.Global:
					EditorUtility.SetDirty (KickStarter.variablesManager);
					AssetDatabase.SaveAssets ();
					break;

				case VariableLocation.Local:
					UnityVersionHandler.CustomSetDirty (KickStarter.localVariables);
					break;

				case VariableLocation.Component:
					EditorUtility.SetDirty (variables);
					break;

				default:
					break;
			}

			ACDebug.Log (numVars + " new " + location + " variables created");
		}


		private List<GVar> Vars
		{
			get
			{
				switch (location)
				{
					case VariableLocation.Global:
						return KickStarter.variablesManager.vars;

					case VariableLocation.Local:
						return KickStarter.localVariables.localVars;

					case VariableLocation.Component:
						return variables.vars;

					default:
						return null;
				}
			}
		}


		private int[] GetIDArray ()
		{
			// Returns a list of id's in the list
			
			List<int> idArray = new List<int>();
			
			foreach (GVar variable in Vars)
			{
				idArray.Add (variable.id);
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}

}

#endif