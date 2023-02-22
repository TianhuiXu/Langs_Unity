/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ConstantID.cs"
 * 
 *	This script is used by the Serialization classes to store a permanent ID
 *	of the gameObject (like InstanceID, only retained after reloading the project).
 *	To save a reference to an arbitrary object in a scene, this script must be attached to it.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * This script is used by the Serialization classes to store a permanent ID
 	 * of the gameObject (like InstanceID, only retained after reloading the project).
 	 * To save a reference to an arbitrary object in a scene, this script must be attached to it.
	*/
	[ExecuteInEditMode]
	[AddComponentMenu("Adventure Creator/Save system/Constant ID")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_constant_i_d.html")]
	public class ConstantID : MonoBehaviour
	{

		#region Variables

		/** The recorded Constant ID number */
		public int constantID;
		/** If True, prefabs will share the same Constant ID as their scene-based counterparts */ 
		public bool retainInPrefab = false;
		/** Is the Constant ID set automatically or manually? */
		public AutoManual autoManual = AutoManual.Automatic;

		#endregion


		#region UnityStandards
		
		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
			EventManager.OnInitialiseScene -= OnInitialiseScene;
			EventManager.OnInitialiseScene += OnInitialiseScene;
		}


		protected virtual void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
			EventManager.OnInitialiseScene -= OnInitialiseScene;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Serialises appropriate GameObject values into a string.  Overriden by subclasses.</summary>
		 * <returns>The data, serialised as a string (empty in the base class)</returns>
		 */
		public virtual string SaveData ()
		{
			return string.Empty;
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.  Overridden by subclasses.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public virtual void LoadData (string stringData)
		{}

			
		protected bool GameIsPlaying ()
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				return false;
			}
			#endif
			return true;
		}

		#endregion


		#region CustomEvents

		protected virtual void OnInitialiseScene () { }

		#endregion


		#region ProtectedFunctions

		protected bool[] StringToBoolArray (string _string)
		{
			if (_string == null || _string == "" || _string.Length == 0)
			{
				return null;
			}
			
			string[] boolArray = _string.Split (SaveSystem.pipe[0]);
			List<bool> boolList = new List<bool>();
			
			foreach (string chunk in boolArray)
			{
				if (chunk == "False")
				{
					boolList.Add (false);
				}
				else
				{
					boolList.Add (true);
				}
			}
			
			return boolList.ToArray ();
		}


		protected int[] StringToIntArray (string _string)
		{
			if (_string == null || _string == "" || _string.Length == 0)
			{
				return null;
			}
			
			string[] intArray = _string.Split (SaveSystem.pipe[0]);
			List<int> intList = new List<int>();
			
			foreach (string chunk in intArray)
			{
				intList.Add (int.Parse (chunk));
			}
			
			return intList.ToArray ();
		}


		protected float[] StringToFloatArray (string _string)
		{
			if (_string == null || _string == "" || _string.Length == 0)
			{
				return null;
			}
			
			string[] floatArray = _string.Split (SaveSystem.pipe[0]);
			List<float> floatList = new List<float>();
			
			foreach (string chunk in floatArray)
			{
				floatList.Add (float.Parse (chunk));
			}
			
			return floatList.ToArray ();
		}


		protected string[] StringToStringArray (string _string)
		{
			if (_string == null || _string == "" || _string.Length == 0)
			{
				return null;
			}
			
			string[] stringArray = _string.Split (SaveSystem.pipe[0]);
			for (int i=0; i<stringArray.Length; i++)
			{
				stringArray[i] = AdvGame.PrepareStringForLoading (stringArray[i]);
			}

			return stringArray;
		}
		
		
		protected string ArrayToString <T> (T[] _list)
		{
			System.Text.StringBuilder _string = new System.Text.StringBuilder ();
			
			foreach (T state in _list)
			{
				string stateString = AdvGame.PrepareStringForSaving (state.ToString ());
				_string.Append (stateString + SaveSystem.pipe);
			}
			if (_string.Length > 0)
			{
				_string.Remove (_string.Length-1, 1);
			}
			return _string.ToString ();
		}

		#endregion


		#region StaticFunctions

		/**
		 * <summary>Gets all components in the Hierarchy that also have a ConstantID component on the same GameObject.</summary>
		 * <param name = "constantID">The Constant ID number generated by the ConstantID component</param>
		 * <returns>The components with a matching Constant ID number</returns>
		 */
		public static HashSet<T> GetComponents <T> (int constantIDValue) where T : Component
		{
			if (KickStarter.stateHandler)
			{
				return KickStarter.stateHandler.ConstantIDManager.GetComponents <T> (constantIDValue);
			}
			return null;
		}


		/**
		 * <summary>Gets a component in the Hierarchy that also has a ConstantID component on the same GameObject.</summary>
		 * <param name = "constantID">The Constant ID number generated by the ConstantID component</param>
		 * <returns>The component with a matching Constant ID number</returns>
		 */
		public static T GetComponent <T> (int constantIDValue) where T : Component
		{
			if (constantIDValue == 0) return null;

			if (!Application.isPlaying || KickStarter.stateHandler == null)
			{
				T[] objects = FindObjectsOfType <T>();
				foreach (T _object in objects)
				{
					ConstantID[] idScripts = _object.GetComponents <ConstantID>();
					if (idScripts != null)
					{
						foreach (ConstantID idScript in idScripts)
						{
							if (idScript.constantID == constantIDValue)
							{
								// Found it
								return _object;
							}
						}
					}
				}
				return null;
			}

			return KickStarter.stateHandler.ConstantIDManager.GetComponent <T> (constantIDValue);
		}


		/**
		 * <summary>Gets a ConstantID component in the Hierarchy with a given ID number.</summary>
		 * <param name = "constantID">The ID number to search for</param>
		 * <returns>The ConstantID component with the matching ID number</returns>
		 */
		public static ConstantID GetComponent (int constantIDValue)
		{
			if (constantIDValue == 0) return null;

			if (!Application.isPlaying || KickStarter.stateHandler == null)
			{
				ConstantID[] constantIDs = FindObjectsOfType<ConstantID> ();
				foreach (ConstantID constantIDComponent in constantIDs)
				{
					if (constantIDComponent.constantID == constantIDValue)
					{
						// Found it
						return constantIDComponent;
					}
				}
				return null;
			}
			
			return KickStarter.stateHandler.ConstantIDManager.GetConstantID (constantIDValue);
		}


		/**
		 * <summary>Gets all components of a particular type within the scene, with a given ConstantID number.</summary>
		 * <param name = "constantIDValue">The ID number to search for</param>
		 * <param name = "scene">The scene to search</param>
		 * <returns>All components of the give type in the scene, provided they have an associated ConstantID</returns>
		 */
		public static HashSet<T> GetComponents <T> (int constantIDValue, Scene scene) where T : Component
		{
			if (KickStarter.stateHandler)
			{
				return KickStarter.stateHandler.ConstantIDManager.GetComponents <T> (constantIDValue, scene);
			}
			return null;
		}


		/**
		 * <summary>Gets all components of a particular type within the scene.  Only components with an associated ConstantID number will be returned.</summary>
		 * <param name = "scene">The scene to search</param>
		 * <returns>All components of the give type in the scene, provided they have an associated ConstantID</returns>
		 */
		public static HashSet<T> GetComponents <T> (Scene scene) where T : Component
		{
			if (KickStarter.stateHandler)
			{
				return KickStarter.stateHandler.ConstantIDManager.GetComponents <T> (scene);
			}
			return null;
		}


		/**
		 * <summary>Gets a component associated with a given ConstantID number, in a particular scene</summary>
		 * <param name = "constantIDValue">The ID number to search for</param>
		 * <param name = "scene">The scene to search</param>
		 * <param name = "sceneOnlyPrioritises">If True, then the supplied scene is searched first, but all other scenes are then searched if no result is yet found</param>
		 * <returns>The component associated with the Constant ID number</returns>
		 */
		public static T GetComponent <T> (int constantIDValue, Scene scene, bool sceneOnlyPrioritises = false) where T : Component
		{
			if (KickStarter.stateHandler)
			{
				return KickStarter.stateHandler.ConstantIDManager.GetComponent <T> (constantIDValue, scene, sceneOnlyPrioritises);
			}
			return null;
		}


		/**
		 * <summary>Gets a ConstantID component ID number, in a particular scene</summary>
		 * <param name = "constantIDValue">The ID number to search for</param>
		 * <param name = "scene">The scene to search</param>
		 * <param name = "sceneOnlyPrioritises">If True, then the supplied scene is searched first, but all other scenes are then searched if no result is yet found</param>
		 * <returns>The ConstantID component associated with the ID number</returns>
		 */
		public static ConstantID GetComponent (int constantIDValue, Scene scene, bool sceneOnlyPrioritises = false)
		{
			if (KickStarter.stateHandler)
			{
				return KickStarter.stateHandler.ConstantIDManager.GetConstantID (constantIDValue, scene, sceneOnlyPrioritises);
			}
			return null;
		}

		#endregion


		#if UNITY_EDITOR

		private bool isNewInstance = true;


		/**
		 * <summary>Sets a new Constant ID number.</summary>
		 * <param name = "forcePrefab">If True, sets "retainInPrefab" to True. Otherwise, it will be determined by whether or not the component is part of an asset file.</param>
		 * <returns>The new Constant ID number</returns>
		 */
		public int AssignInitialValue (bool forcePrefab = false)
		{
			if (forcePrefab || UnityVersionHandler.ShouldAssignPrefabConstantID (gameObject))
			{
				retainInPrefab = true;
				SetNewID_Prefab ();
			}
			else
			{
				retainInPrefab = false;
				SetNewID ();
			}
			return constantID;
		}


		protected void Update ()
		{
			if (gameObject.activeInHierarchy && !Application.isPlaying)
			{
				if (!UnityVersionHandler.IsPrefabFile (gameObject) ||
					UnityVersionHandler.IsPrefabEditing (gameObject))
				{
					if (constantID == 0)
					{
						SetNewID ();
					}
					
					if (isNewInstance)
					{
						isNewInstance = false;
						CheckForDuplicateIDs ();
					}
				}
			}
		}


		/** Sets a new Constant ID number for a prefab. */
		public void SetNewID_Prefab ()
		{
			SetNewID ();
			isNewInstance = false;
		}
		

		private void SetNewID (bool ignoreOthers = false)
		{
			// Share ID if another ID script already exists on object
			ConstantID[] idScripts = GetComponents <ConstantID>();

			foreach (ConstantID idScript in idScripts)
			{
				if (idScript != this && idScript.constantID != 0)
				{
					if (ignoreOthers && idScript.constantID == constantID)
					{
						continue;
					}

					constantID = idScript.constantID;
					UnityVersionHandler.CustomSetDirty (this, true);
					return;
				}
			}

			if (UnityVersionHandler.IsPrefabFile (gameObject) &&
				UnityVersionHandler.IsPrefabEditing (gameObject) &&
				!retainInPrefab)
			{
				// Avoid setting ID to a prefab that shouldn't have one
				return;
			}

			constantID = GetInstanceID ();
			if (constantID < 0)
			{
				constantID *= -1;
			}

			UnityVersionHandler.CustomSetDirty (this, true);
			ACDebug.Log ("Set new ID for " + this.GetType ().ToString () + " to " + gameObject.name + ": " + constantID, gameObject);
		}
		

		public void SetManualID (int _id)
		{
			autoManual = AutoManual.Manual;
			constantID = _id;
			UnityVersionHandler.CustomSetDirty (this, true);
		}

		
		private void CheckForDuplicateIDs ()
		{
			ConstantID[] idScripts = GetAllIDScriptsInHierarchy ();
				
			foreach (ConstantID idScript in idScripts)
			{
				if (idScript.constantID == constantID && idScript.gameObject != this.gameObject && constantID != 0)
				{
					ACDebug.Log ("Duplicate ID found: " + idScript.gameObject.name + " and " + this.name + " : " + constantID, gameObject);
					SetNewID (true);
					break;
				}
			}
		}


		private ConstantID[] GetAllIDScriptsInHierarchy ()
		{
			ConstantID[] idScripts = null;

			if (UnityVersionHandler.IsPrefabEditing (gameObject))
			{
				GameObject rootObject = (transform.root) ? transform.root.gameObject : gameObject;
				idScripts = rootObject.GetComponentsInChildren <ConstantID>();
			}
			else
			{
				idScripts = FindObjectsOfType (typeof (ConstantID)) as ConstantID[];
			}
			return idScripts;
		}


		[MenuItem("CONTEXT/ConstantID/Find local references")]
		public static void FindLocalReferences (MenuCommand command)
		{
			ConstantID _constantID = (ConstantID) command.context;
			if (_constantID)
			{
				if (_constantID.constantID == 0)
				{
					ACDebug.LogWarning ("Cannot find references for " + _constantID.name + " because it's ConstantID value is zero!", _constantID);
					return;
				}

				SearchSceneForReferences (_constantID.gameObject, _constantID.constantID);
			}
		}


		[MenuItem("CONTEXT/ConstantID/Find global references")]
		public static void FindGlobalReferences (MenuCommand command)
		{
			ConstantID _constantID = (ConstantID) command.context;
			if (_constantID)
			{
				if (_constantID.constantID == 0)
				{
					ACDebug.LogWarning ("Cannot find references for " + _constantID.name + " because it's ConstantID value is zero!", _constantID);
					return;
				}

				if (EditorUtility.DisplayDialog ("Search '" + _constantID.gameObject.name + "' references?", "The Editor will search assets, and active scenes listed in the Build Settings, for references to this GameObject.  The current scene will need to be saved and listed to be included in the search process. Continue?", "OK", "Cancel"))
				{
					if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
					{
						int CID = _constantID.constantID;
						GameObject CIDObject = _constantID.gameObject;

						// Menus
						if (KickStarter.menuManager)
						{
							foreach (Menu menu in KickStarter.menuManager.menus)
							{
								if (menu.IsUnityUI ())
								{
									if (menu.ReferencesObjectOrID (CIDObject, CID))
									{
										if (CIDObject) Debug.Log ("'" + CIDObject.name + "' is referenced by Menu '" + menu.title + "'");
										else Debug.Log ("Constant ID " + CID + "' is referenced by Menu '" + menu.title + "'");
									}

									foreach (MenuElement element in menu.elements)
									{
										if (element && element.ReferencesObjectOrID (CIDObject, CID))
										{
											if (CIDObject) Debug.Log ("'" + CIDObject.name + "' is referenced by Menu Element '" + element.title + "' in Menu '" + menu.title + "'");
											else Debug.Log ("Constant ID " + CID + "' is referenced by Menu Element '" + element.title + "' in Menu '" + menu.title + "'");
										}
									}
								}
							}
						}

						// ActionList assets
						if (AdvGame.GetReferences ().speechManager)
						{
							ActionListAsset[] allActionListAssets = AdvGame.GetReferences ().speechManager.GetAllActionListAssets ();
							foreach (ActionListAsset actionListAsset in allActionListAssets)
							{
								SearchActionListAssetForReferences (CIDObject, CID, actionListAsset);
							}
						}

						// Scenes
						string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
						string[] sceneFiles = AdvGame.GetSceneFiles ();

						foreach (string sceneFile in sceneFiles)
						{
							UnityVersionHandler.OpenScene (sceneFile);

							string suffix = " in scene '" + sceneFile + "'";
							SearchSceneForReferences (null, CID, suffix);
						}

						UnityVersionHandler.OpenScene (originalScene);
					}
				}
			}
		}


		private static void SearchSceneForReferences (GameObject _constantIDObject, int _constantID, string suffix = "")
		{
			SetParametersBase[] setParametersBases = FindObjectsOfType <SetParametersBase>();
			foreach (SetParametersBase setParametersBase in setParametersBases)
			{
				if (setParametersBase.ReferencesObjectOrID (_constantIDObject, _constantID))
				{
					if (_constantIDObject) Debug.Log ("'" + _constantIDObject.name + "' is referenced by '" + setParametersBase.name + "'" + suffix, setParametersBase);
					else Debug.Log ("Constant ID " + _constantID + " is referenced by '" + setParametersBase.name + "'" + suffix, setParametersBase);
				}
			}

			ActionList[] localActionLists = FindObjectsOfType <ActionList>();
			foreach (ActionList actionList in localActionLists)
			{
				if (actionList.source == ActionListSource.InScene)
				{
					foreach (Action action in actionList.actions)
					{
						if (action != null && action.ReferencesObjectOrID (_constantIDObject, _constantID))
						{
							string actionLabel = (KickStarter.actionsManager) ? (" (" + KickStarter.actionsManager.GetActionTypeLabel (action) + ")") : "";
							if (_constantIDObject) Debug.Log ("'" + _constantIDObject.name + "' is referenced by Action #" + actionList.actions.IndexOf (action) + actionLabel + " in ActionList '" + actionList.gameObject.name + "'" + suffix, actionList);
							else Debug.Log ("Constant ID " + _constantID + " is referenced by Action #" + actionList.actions.IndexOf (action) + actionLabel + " in ActionList '" + actionList.gameObject.name + "'" + suffix, actionList);
						}
					}
				}
				else if (actionList.source == ActionListSource.AssetFile)
				{
					SearchActionListAssetForReferences (_constantIDObject, _constantID, actionList.assetFile);
				}
			}
		}


		private static void SearchActionListAssetForReferences (GameObject _constantIDObject, int _constantID, ActionListAsset actionListAsset)
		{
			if (actionListAsset == null) return;

			foreach (Action action in actionListAsset.actions)
			{
				if (action != null && action.ReferencesObjectOrID (_constantIDObject, _constantID))
				{
					string actionLabel = (KickStarter.actionsManager) ? (" (" + KickStarter.actionsManager.GetActionTypeLabel (action) + ")") : "";
					if (_constantIDObject) Debug.Log ("'" + _constantIDObject.name + "' is referenced by Action #" + actionListAsset.actions.IndexOf (action) + actionLabel + " in ActionList asset '" + actionListAsset.name + "'", actionListAsset);
					else Debug.Log ("Constant ID " + _constantID + " is referenced by Action #" + actionListAsset.actions.IndexOf (action) + actionLabel + " in ActionList asset '" + actionListAsset.name + "'", actionListAsset);
				}
			}
		}

		#endif

	}


	/** A subclass of ConstantID, that is used to distinguish further subclasses from ConstantID components. */
	[System.Serializable]
	public abstract class Remember : ConstantID
	{

		#region Variables

		protected bool savePrevented = false;
		protected bool loadedData = false;

		#endregion


		#region CustomEvents

		protected override void OnInitialiseScene ()
		{
			loadedData = false;
		}

		#endregion


		#region GetSet

		/** If True, saving is prevented */
		public bool SavePrevented
		{
			get
			{
				return savePrevented;
			}
			set
			{
				savePrevented = value;
			}
		}


		/** Checks if data has been loaded for this component. */
		public bool LoadedData
		{
			get
			{
				return loadedData;
			}
		}

		#endregion

	}


	/** The base class of saved data.  Each Remember subclass uses its own RememberData subclass to store its data.	 */
	[System.Serializable]
	public class RememberData
	{

		/** The ConstantID number of the object being saved */
		public int objectID;
		/** If True, saving is prevented */
		public bool savePrevented;

		/**
		 * The base constructor.
		 */
		public RememberData () { }
	}
	
}