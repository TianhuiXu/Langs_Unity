#if UNITY_2018_3_OR_NEWER
#define NEW_PREFABS
#endif

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (ConstantID), true)]
	public class ConstantIDEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			SharedGUI ();
		}
		
		
		protected void SharedGUI()
		{
			ConstantID _target = (ConstantID) target;

			CustomGUILayout.BeginVertical ();

			EditorGUILayout.LabelField ("Constant ID number", EditorStyles.boldLabel);

			_target.autoManual = (AutoManual) CustomGUILayout.EnumPopup ("Set:", _target.autoManual, "", "Is the Constant ID set automatically or manually?");

			bool _retainInPrefab = _target.retainInPrefab;
			_retainInPrefab = CustomGUILayout.Toggle ("Retain in prefab?", _retainInPrefab, "", "If True, prefabs will share the same Constant ID as their scene-based counterparts");

			if (UnityVersionHandler.IsPrefabFile (_target.gameObject))
			{
				// Prefab
				if (_target.retainInPrefab && !_retainInPrefab && _target.constantID != 0)
				{
					#if NEW_PREFABS
					ManuallyUpdateSceneInstances (_target, _target.constantID);
					#else
					_target.retainInPrefab = false;
					_target.constantID = 0;
					#endif
				}
				else if (_retainInPrefab && _target.constantID == 0)
				{
					_target.SetNewID_Prefab ();
					_target.retainInPrefab = _retainInPrefab;
				}
				else
				{
					_target.retainInPrefab = _retainInPrefab;
				}
			}
			else
			{
				_target.retainInPrefab = _retainInPrefab;
			}

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("ID:", "The recorded Constant ID number"), GUILayout.Width (50f));
			if (_target.autoManual == AutoManual.Automatic)
			{
				EditorGUILayout.LabelField (_target.constantID.ToString ());
			}
			else
			{
				_target.constantID = EditorGUILayout.DelayedIntField (_target.constantID);
			}
			if (GUILayout.Button ("Copy number"))
			{
				EditorGUIUtility.systemCopyBuffer = _target.constantID.ToString ();
			}
			EditorGUILayout.EndHorizontal ();
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}


		#if NEW_PREFABS
		private void ManuallyUpdateSceneInstances (ConstantID _target, int fixedID)
		{
			int option = EditorUtility.DisplayDialogComplex ("Correct scene instances?", "Unchecking 'Retain in prefab?' will reset IDs for instances of the prefab already present in the scene.  AC can go through your scenes to ensure that they remain as they were, with an ID value of " + fixedID, "Update instances", "Do not update instances", "Cancel");

			switch (option)
			{
				// Update isntances
				case 0:
					{
						string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();

						if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
						{
							string[] sceneFiles = AdvGame.GetSceneFiles ();
							int numUpdated = 0;

							// First look for lines that already have an assigned lineID
							foreach (string sceneFile in sceneFiles)
							{
								UnityVersionHandler.OpenScene (sceneFile);

								ConstantID[] constantIDs = FindObjectsOfType (typeof (ConstantID)) as ConstantID[];
								foreach (ConstantID constantID in constantIDs)
								{
									GameObject originalPrefab = PrefabUtility.GetCorrespondingObjectFromSource (constantID.gameObject);
									if (originalPrefab == _target.gameObject && constantID.constantID == fixedID && constantID.retainInPrefab && constantID.autoManual == AutoManual.Automatic)
									{
										constantID.SetManualID (-1); // Necessary to override
										UnityVersionHandler.SaveScene ();
										numUpdated++;

										constantID.SetManualID (fixedID);
										UnityVersionHandler.SaveScene ();
										ACDebug.Log ("Updated " + constantID.gameObject.name + " in scene " + sceneFile);
									}
								}

							}

							if (string.IsNullOrEmpty (originalScene))
							{
								UnityVersionHandler.NewScene ();
							}
							else
							{
								UnityVersionHandler.OpenScene (originalScene);
							}

							_target.constantID = 0;
							_target.retainInPrefab = false;
							AssetDatabase.SaveAssets ();
							ACDebug.Log ("Process complete. " + numUpdated + " scene instance" + ((numUpdated == 1) ? string.Empty : "s") + " updated.");
						}
					}
					break;

				// Do not update instances
				case 1:
					_target.constantID = 0;
					_target.retainInPrefab = false;
					break;

				// Cancel
				default:
					break;
			}
		}
		#endif

	}

}

#endif