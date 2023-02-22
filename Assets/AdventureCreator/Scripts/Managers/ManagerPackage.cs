/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ManagerPackage.cs"
 * 
 *	This script is used to store references to Manager assets,
 *	so that they can be quickly loaded into the game engine in bulk.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	/** An asset file that stores references to Manager assets, so that they can be quickly assigned in bulk. */
	[System.Serializable]
	public class ManagerPackage : ScriptableObject
	{

		public ActionsManager actionsManager;
		public SceneManager sceneManager;
		public SettingsManager settingsManager;
		public InventoryManager inventoryManager;
		public VariablesManager variablesManager;
		public SpeechManager speechManager;
		public CursorManager cursorManager;
		public MenuManager menuManager;


		/** Checks if all 8 Manager types are assigned. */
		public bool IsFullyAssigned ()
		{
			if (actionsManager == null)
			{
				return false;
			}
			if (sceneManager == null)
			{
				return false;
			}
			if (settingsManager == null)
			{
				return false;
			}
			if (inventoryManager == null)
			{
				return false;
			}
			if (variablesManager == null)
			{
				return false;
			}
			if (speechManager == null)
			{
				return false;
			}
			if (cursorManager == null)
			{
				return false;
			}
			if (menuManager == null)
			{
				return false;
			}
			return true;
		}


		/** Assigns its various Manager asset files. */
		public void AssignManagers ()
		{
			if (AdvGame.GetReferences () != null)
			{
				int numAssigned = 0;

				if (sceneManager)
				{
					AdvGame.GetReferences ().sceneManager = sceneManager;
					numAssigned ++;
				}
				
				if (settingsManager)
				{
					AdvGame.GetReferences ().settingsManager = settingsManager;
					numAssigned ++;
				}
				
				if (actionsManager)
				{
					AdvGame.GetReferences ().actionsManager = actionsManager;
					numAssigned ++;
				}
				
				if (variablesManager)
				{
					AdvGame.GetReferences ().variablesManager = variablesManager;
					numAssigned ++;
				}
				
				if (inventoryManager)
				{
					AdvGame.GetReferences ().inventoryManager = inventoryManager;
					numAssigned ++;
				}
				
				if (speechManager)
				{
					AdvGame.GetReferences ().speechManager = speechManager;
					numAssigned ++;
				}
				
				if (cursorManager)
				{
					AdvGame.GetReferences ().cursorManager = cursorManager;
					numAssigned ++;
				}
				
				if (menuManager)
				{
					AdvGame.GetReferences ().menuManager = menuManager;
					numAssigned ++;
				}

				#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					KickStarter.ClearManagerCache ();
				}

				if (KickStarter.sceneManager)
				{
					KickStarter.sceneManager.GetPrefabsInScene ();
				}

				UnityVersionHandler.CustomSetDirty (AdvGame.GetReferences (), true);
				AssetDatabase.SaveAssets ();
				#endif

				if (this)
				{
					if (numAssigned == 0)
					{
						ACDebug.Log (this.name + " No Mangers assigned.");
					}
					else if (numAssigned == 1)
					{
						ACDebug.Log (this.name + " - (" + numAssigned.ToString () + ") Manager assigned.", this);
					}
					else
					{
						ACDebug.Log (this.name + " - (" + numAssigned.ToString () + ") Managers assigned.", this);
					}
				}
			}
			else
			{
				#if UNITY_EDITOR
				string intendedDirectory = Resource.DefaultReferencesPath + "/Resources";

				bool canProceed = EditorUtility.DisplayDialog ("Error - missing References", "A 'References' file must be present in the directory '" + Resource.DefaultReferencesPath + "'. Create one?", "OK", "Cancel");
				if (!canProceed) return;

				CustomAssetUtility.CreateAsset<References> ("References", intendedDirectory);
				CustomAssetUtility.CreateAsset<References> ("References", Resource.DefaultReferencesPath);

				if (AdvGame.GetReferences () != null)
				{
					AssignManagers ();
				}

				#else

				ACDebug.LogError ("Can't assign managers - no References file found in Resources folder.");

				#endif
			}
		}

	}

}