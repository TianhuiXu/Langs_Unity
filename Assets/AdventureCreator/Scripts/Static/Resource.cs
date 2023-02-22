/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"Resource.cs"
 * 
 *	This script contains variables for Resource prefabs.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public class Resource
	{

		// Main Reference resource
		private const string referencesName = "References";

		// Created by Kickstarter on Awake
		public const string persistentEngine = "PersistentEngine";

		// Created by BackgroundImageUI singleton
		private const string backgroundImageUIName = "BackgroundImageUI";

		// Used by DragTracks
		private const string dragColliderName = "DragCollider";

		// Used by UISlot
		private const string emptySlotName = "EmptySlot";

		// External links
		public const string manualLink = "https://www.adventurecreator.org/files/Manual.pdf";
		public const string assetLink = "https://assetstore.unity.com/packages/templates/systems/adventure-creator-11896";
		public const string websiteLink = "https://adventurecreator.org/";
		public const string tutorialsLink = "https://www.adventurecreator.org/tutorials/";
		public const string downloadsLink = "https://www.adventurecreator.org/downloads/";
		public const string forumLink = "https://www.adventurecreator.org/forum/";
		public const string scriptingGuideLink = "https://www.adventurecreator.org/scripting-guide/";
		public const string wikiLink = "http://adventure-creator.wikia.com/wiki/Adventure_Creator_Wikia";
		public const string introTutorialLink = "https://www.adventurecreator.org/tutorials/game-editor-window";


		private static Collider dragColliderPrefab;
		public static Collider DragCollider
		{
			get
			{
				if (dragColliderPrefab == null)
				{
					dragColliderPrefab = (Collider) Resources.Load (dragColliderName, typeof (Collider));
				}
				return dragColliderPrefab;
			}
		}


		private static Sprite emptySlotAsset;
		public static Sprite EmptySlot
		{
			get
			{
				if (emptySlotAsset == null)
				{
					emptySlotAsset = (Sprite) Resources.Load (emptySlotName, typeof (Sprite));
				}
				return emptySlotAsset;
			}
		}


		private static GameObject backgroundImageUIPrefab;
		public static GameObject BackgroundImageUI
		{
			get
			{
				if (backgroundImageUIPrefab == null)
				{
					backgroundImageUIPrefab = (GameObject) Resources.Load (backgroundImageUIName, typeof (GameObject));
				}
				return backgroundImageUIPrefab;
			}
		}


		private static References referencesAsset;
		public static References References
		{
			get
			{
				if (referencesAsset == null)
				{
					referencesAsset = (References) Resources.Load (referencesName, typeof (References));

					if (referencesAsset == null)
					{
						References[] allReferences = Resources.FindObjectsOfTypeAll (typeof (References)) as References[];
						if (allReferences.Length > 0) referencesAsset = allReferences[0];
					}
				}
				return referencesAsset;
			}
			set
			{
				referencesAsset = value;
			}
		}


		#if UNITY_EDITOR

		private static Texture2D acLogo;
		private static Texture2D greyTexture;
		private static GUISkin nodeSkin;


		public static Texture2D ACLogo
		{
			get
			{
				if (acLogo == null)
				{
					acLogo = (Texture2D) AssetDatabase.LoadAssetAtPath (MainFolderPath + "/Graphics/Textures/logo.png", typeof (Texture2D));
					if (acLogo == null)
					{
						ACDebug.LogWarning ("Cannot find Texture asset file '" + MainFolderPath + "/Graphics/Textures/logo.png'");
					}
				}
				return acLogo;
			}
		}


		public static Texture2D GreyTexture
		{
			get
			{
				if (greyTexture == null)
				{
					greyTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (MainFolderPath + "/Graphics/Textures/grey.png", typeof (Texture2D));
					if (greyTexture == null)
					{
						ACDebug.LogWarning ("Cannot find Texture asset file '" + MainFolderPath + "/Graphics/Textures/grey.png'");
					}
				}
				return greyTexture;
			}
		}


		public static GUISkin NodeSkin
		{
			get
			{
				if (nodeSkin == null)
				{
					nodeSkin = (GUISkin) AssetDatabase.LoadAssetAtPath (MainFolderPath + "/Graphics/Skins/ACNodeSkin.guiskin", typeof (GUISkin));
					if (nodeSkin == null)
					{
						ACDebug.LogWarning ("Cannot find GUISkin asset file '" + MainFolderPath + "/Graphics/Skins/ACNodeSkin.guiskin'");
					}
				}
				return nodeSkin;
			}
		}


		// Path to root AC folder
		public static string MainFolderPath
		{
			get
			{
				return ACEditorPrefs.InstallPath;
			}
		}


		public static string DefaultReferencesPath
		{
			get
			{
				return MainFolderPath + "/Resources";
			}
		}


		public static string DefaultActionsPath
		{
			get
			{
				return MainFolderPath + "/Scripts/Actions";
			}
		}

		#endif

	}

}