/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	SceneInfo.cs"
 * 
 *	A data container for an actual scene in the build.
 * 
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
#if AddressableIsPresent
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace AC
{

	/** A data container for an actual scene in the build. */
	public class SceneInfo
	{

		#region Variables

		private readonly int buildIndex;
		private readonly string filename;

		#if AddressableIsPresent
		private readonly bool addedToBuildSettings;
		private static Dictionary<string, AsyncOperationHandle<SceneInstance>> openSceneHandles = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
		#endif

		#endregion


		#region Constructors

		/** The default constructor */
		public SceneInfo (int _buildIndex, string _filename, bool _addedToBuildSettings = true)
		{
			buildIndex = _buildIndex;
			filename = _filename;
			#if AddressableIsPresent
			addedToBuildSettings = _addedToBuildSettings;
			#endif
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if this represents the currently-active main scene</summary>
		 * <returns>True if this represents the currently-active main scene</returns>
		 */
		public bool IsCurrentActive ()
		{
			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					return filename == SceneChanger.CurrentSceneName;

				case ChooseSceneBy.Number:
				default:
					return buildIndex == SceneChanger.CurrentSceneIndex;
			}
		}


		/**
		 * <summary>Loads the scene normally.</summary>
		 * <param name = "forceReload">If True, the scene will be re-loaded if it is already open.</param>
		 */
		public bool Open (bool forceReload = false)
		{
			return Open (forceReload, LoadSceneMode.Single);
		}


		/** Adds the scene additively.*/
		public void Add ()
		{
			Open (false, LoadSceneMode.Additive);
		}


		/** Closes the scene additively. */
		public void Close (bool evenIfCurrent = false)
		{
			if (evenIfCurrent || !IsCurrentActive ())
			{
				switch (KickStarter.settingsManager.referenceScenesInSave)
				{
					case ChooseSceneBy.Name:
						#if AddressableIsPresent
						if (!addedToBuildSettings && KickStarter.settingsManager.loadScenesFromAddressable)
						{
							if (openSceneHandles.ContainsKey (filename))
							{
								UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync (openSceneHandles[filename], true);
								openSceneHandles.Remove (filename);
							}
							else
							{
								ACDebug.LogWarning ("Cannot close scene '" + filename + " because no recorded AsyncOperation was found.");
							}
							return;
						}
						#endif
						UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync (filename);
						break;

					case ChooseSceneBy.Number:
					default:
						UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync (buildIndex);
						break;
				}
			}
		}


		/**
		 * <summary>Loads the scene asynchronously.</summary>
		 * <returns>The generated AsyncOperation class</returns>
		 */
		public AsyncOperation OpenAsync ()
		{
			switch (KickStarter.settingsManager.referenceScenesInSave)
			{
				case ChooseSceneBy.Name:
					#if AddressableIsPresent
					if (!addedToBuildSettings && KickStarter.settingsManager.loadScenesFromAddressable)
					{
						AsyncOperationHandle<SceneInstance> handle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync (filename, LoadSceneMode.Additive);
						if (handle.IsValid ())
						{
							openSceneHandles.Add (filename, handle);
						}
					}
					#endif
					return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync (filename);
					
				case ChooseSceneBy.Number:
				default:
					return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync (buildIndex);
			}
		}


		#if AddressableIsPresent
		public AsyncOperationHandle<SceneInstance> OpenAddressableAsync (bool manualActivation)
		{
			AsyncOperationHandle<SceneInstance> asyncHandle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync (filename, LoadSceneMode.Single, !manualActivation);
			return asyncHandle;
		}
		#endif

		#endregion


		#region PrivateFunctions

		private bool Open (bool forceReload, LoadSceneMode loadSceneMode)
		{
			if (KickStarter.settingsManager.reloadSceneWhenLoading)
			{
				forceReload = true;
			}

			try
			{
				if (forceReload || !IsCurrentActive ())
				{
					switch (KickStarter.settingsManager.referenceScenesInSave)
					{
						case ChooseSceneBy.Name:
							#if AddressableIsPresent
							if (!addedToBuildSettings && KickStarter.settingsManager.loadScenesFromAddressable)
							{
								var handle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync (filename, loadSceneMode);
								if (!handle.IsValid ())
								{
									return false;
								}
								handle.WaitForCompletion ();
								if (loadSceneMode == LoadSceneMode.Additive)
								{
									openSceneHandles.Add (filename, handle);
								}
							}
							else
							#endif
							{
								UnityEngine.SceneManagement.SceneManager.LoadScene (filename, loadSceneMode);
							}
							break;

						case ChooseSceneBy.Number:
						default:
							UnityEngine.SceneManagement.SceneManager.LoadScene (buildIndex, loadSceneMode);
							break;
					}
					return true;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning ("Error when opening scene " + buildIndex + ": " + e);
			}
			return false;
		}

		#endregion


		#region GetSet

		/** The scene's filename, without extension or filepath */
		public string Filename
		{
			get
			{
				return filename;
			}
		}


		/** The scene's build index number */
		public int BuildIndex
		{
			get
			{
				return buildIndex;
			}
		}

		#endregion

	}

}