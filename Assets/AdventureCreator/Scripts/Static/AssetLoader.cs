/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AssetLoader.cs"
 * 
 *	This handles the management and retrieval of "Resources"
 *	assets when loading saved games.
 * 
 */


//#if !UNITY_SWITCH
#define ALLOW_VIDEO
//#endif

using UnityEngine;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;
#endif
#if ALLOW_VIDEO
using UnityEngine.Video;
#endif

namespace AC
{

	/**
	 * A class that handles the retrieves of Resources assets stored within save game files.
	 */
	public static class AssetLoader
	{

		private static Object[] textureAssets;
		private static Object[] audioAssets;
		private static Object[] animationAssets;
		private static Object[] materialAssets;
		private static Object[] actionListAssets;
		private static Object[] runtimeAnimatorControllerAssets;
		#if !ACIgnoreTimeline
		private static Object[] timelineAssets;
		#endif
		#if ALLOW_VIDEO
		private static Object[] videoAssets;
		#endif


		/**
		 * <summary>Gets a unique name for an asset file that can be used to find it later.</summary>
		 * <param name = "originalFile">The asset file</param>
		 * <returns>A unique identifier for the asset file</returns>
		 */
		public static string GetAssetInstanceID <T> (T originalFile) where T : Object
		{
			if (originalFile != null)
			{
				#if AddressableIsPresent
				if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
				{
					string _name = originalFile.name;
					_name = _name.Replace (" (Instance)", string.Empty);
					return _name;
				}
				#endif

				string name = originalFile.GetType () + originalFile.name;
				name = name.Replace (" (Instance)", string.Empty);
				return name;
			}
			return string.Empty;
		}


		/**
		 * <summary>Retrieves an asset file.</summary>
		 * <param name = "originalFile">The current asset used in the scene already. If this is null, the operation will not work and null will be returned</param>
		 * <param name = "_name">A unique identifier for the asset file</param>
		 * <returns>The asset file, or the current asset if it wasn't found</returns>
		 */
		public static T RetrieveAsset <T> (T originalFile, string _name) where T : Object
		{
			if (string.IsNullOrEmpty (_name))
			{
				return originalFile;
			}

			if (originalFile == null)
			{
				return null;
			}

			Object newFile = null;

			if (originalFile is Texture)
			{
				newFile = RetrieveTextures (_name);
			}
			else if (originalFile is AudioClip)
			{
				newFile = RetrieveAudioClip (_name);
			}
			else if (originalFile is AnimationClip)
			{
				newFile = RetrieveAnimClips (_name);
			}
			else if (originalFile is Material)
			{
				newFile = RetrieveMaterials (_name);
			}
			else if (originalFile is ActionListAsset)
			{
				newFile = RetrieveActionListAssets (_name);
			}
			#if !ACIgnoreTimeline
			else if (originalFile is TimelineAsset)
			{
				newFile = RetrieveTimelines (_name);
			}
			#endif
			#if ALLOW_VIDEO
			else if (originalFile is VideoClip)
			{
				newFile = RetrieveVideoClips (_name);
			}
			#endif
			else if (originalFile is RuntimeAnimatorController)
			{
				newFile = RetrieveRuntimeAnimatorControllerAssets (_name);
			}
			else
			{
				Object[] genericAssets = null;
				RetrieveAssetFiles<T> (ref genericAssets, string.Empty);
				newFile = GetAssetFile<T> (genericAssets, _name);
			}

			return (newFile != null) ? (T) newFile : originalFile;
		}


		private static Texture RetrieveTextures (string _name)
		{
			RetrieveAssetFiles <Texture> (ref textureAssets, "Textures");
			return GetAssetFile <Texture> (textureAssets, _name);
		}


		/**
		 * <summary>Retrieves an AudioClip asset file</summary>
		 * <param name = "_name">A unique identifier for the AudioClipe</param>
		 * <returns>The AudioClip, or null if it wasn't found</returns>
		 */
		public static AudioClip RetrieveAudioClip (string _name)
		{
			RetrieveAssetFiles <AudioClip> (ref audioAssets, "Audio");
			return GetAssetFile <AudioClip> (audioAssets, _name);
		}


		private static AnimationClip RetrieveAnimClips (string _name)
		{
			RetrieveAssetFiles <AnimationClip> (ref animationAssets, "Animations");
			return GetAssetFile <AnimationClip> (animationAssets, _name);
		}


		private static Material RetrieveMaterials (string _name)
		{
			RetrieveAssetFiles <Material> (ref materialAssets, "Materials");
			return GetAssetFile <Material> (materialAssets, _name);
		}


		private static ActionListAsset RetrieveActionListAssets (string _name)
		{
			RetrieveAssetFiles <ActionListAsset> (ref actionListAssets, "ActionLists");
			return GetAssetFile <ActionListAsset> (actionListAssets, _name);
		}


		#if !ACIgnoreTimeline
		private static TimelineAsset RetrieveTimelines (string _name)
		{
			RetrieveAssetFiles <TimelineAsset> (ref timelineAssets, "Timelines");
			return GetAssetFile <TimelineAsset> (timelineAssets, _name);
		}
		#endif


		#if ALLOW_VIDEO
		private static VideoClip RetrieveVideoClips (string _name)
		{
			RetrieveAssetFiles <VideoClip> (ref videoAssets, "VideoClips");
			return GetAssetFile <VideoClip> (videoAssets, _name);
		}
		#endif


		private static RuntimeAnimatorController RetrieveRuntimeAnimatorControllerAssets (string _name)
		{
			RetrieveAssetFiles <RuntimeAnimatorController> (ref runtimeAnimatorControllerAssets, "Animators");
			return GetAssetFile <RuntimeAnimatorController> (runtimeAnimatorControllerAssets, _name);
		}


		private static T GetAssetFile <T> (Object[] assetFiles, string _name) where T : Object
		{
			if (assetFiles != null && _name != null)
			{
				_name = _name.Replace (" (Instance)", string.Empty);
				foreach (Object assetFile in assetFiles)
				{
					if (assetFile != null && (_name == (assetFile.GetType () + assetFile.name) || _name == assetFile.name))
					{
						return (T) assetFile;
					}
				}
			}

			return null;
		}


		private static void RetrieveAssetFiles <T> (ref Object[] assetFiles, string saveableFolderName) where T : Object
		{
			if (assetFiles == null && !string.IsNullOrEmpty (saveableFolderName))
			{
				assetFiles = Resources.LoadAll ("SaveableData/" + saveableFolderName, typeof (T));
			}
			if (assetFiles == null || assetFiles.Length == 0)
			{
				assetFiles = Resources.LoadAll (string.Empty, typeof (T));
			}
		}


		/** Clears the cache of stored assets from memory. */
		public static void UnloadAssets ()
		{
			textureAssets = null;
			audioAssets = null;
			animationAssets = null;
			materialAssets = null;
			actionListAssets = null;
			runtimeAnimatorControllerAssets = null;
			#if !ACIgnoreTimeline
			timelineAssets = null;
			#endif

			Resources.UnloadUnusedAssets ();
		}

	}

}