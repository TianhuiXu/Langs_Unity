/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SelectiveLoad.cs"
 * 
 *	A container class for selective-loading.
 *	This can be optionally passed to SaveSystem's LoadGame function to prevent the loading of certain components.
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/**
	 * A container class for selective-loading. This can be optionally passed to SaveSystem's LoadGame function to prevent the loading of certain components.
	 */
	[System.Serializable]
	public class SelectiveLoad
	{
		
		/** If True, then variables will be loaded */
		public bool loadVariables;
		/** If True, then inventory will be loaded */
		public bool loadInventory;
		/** If True, then player data will be loaded */
		public bool loadPlayer;
		/** If True, then the active scene at the time of saving will be loaded */
		public bool loadScene;
		/** If True, then any sub-scenes open at the time of saving will be loaded */
		public bool loadSubScenes;
		/** If True, then changes made to scene objects will be loaded */
		public bool loadSceneObjects;
		
		
		/**
		 * The default Constructor.
		 */
		public SelectiveLoad ()
		{
			loadVariables = true;
			loadPlayer = true;
			loadSceneObjects = true;
			loadScene = true;
			loadInventory = true;
			loadSubScenes = true;
		}
		
		
		#if UNITY_EDITOR
		public void ShowGUI ()
		{
			loadVariables = EditorGUILayout.Toggle ("Load variables?", loadVariables);
			loadInventory = EditorGUILayout.Toggle ("Load inventory?", loadInventory);
			loadPlayer = EditorGUILayout.Toggle ("Load player data?", loadPlayer);
			loadScene = EditorGUILayout.Toggle ("Load scene?", loadScene);
			loadSubScenes = EditorGUILayout.Toggle ("Load sub-scenes?", loadSubScenes);
			loadSceneObjects = EditorGUILayout.Toggle ("Load scene changes?", loadSceneObjects);
		}
		#endif
		
	}

}