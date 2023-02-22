/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberMaterial.cs"
 * 
 *	This script is attached to renderers with materials we wish to record changes in.
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if AddressableIsPresent
using System.Collections;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
#endif

namespace AC
{

	/** Attach this to Renderer components with Materials you wish to record changes in. */
	[AddComponentMenu("Adventure Creator/Save system/Remember Material")]
	[RequireComponent (typeof (Renderer))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_material.html")]
	public class RememberMaterial : Remember
	{

		private Renderer _renderer;


		public override string SaveData ()
		{
			MaterialData materialData = new MaterialData ();
			materialData.objectID = constantID;
			materialData.savePrevented = savePrevented;

			List<string> materialIDs = new List<string>();
			Material[] mats = Renderer.materials;

			foreach (Material material in mats)
			{
				materialIDs.Add (AssetLoader.GetAssetInstanceID (material));
			}
			materialData._materialIDs = ArrayToString <string> (materialIDs.ToArray ());

			return Serializer.SaveScriptData <MaterialData> (materialData);
		}
		

		public override void LoadData (string stringData)
		{
			MaterialData data = Serializer.LoadScriptData <MaterialData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			#if AddressableIsPresent

			if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
			{
				StopAllCoroutines ();
				StartCoroutine (LoadDataFromAddressable (data));
				return;
			}

			#endif

			LoadDataFromResources (data);
		}


		#if AddressableIsPresent

		private IEnumerator LoadDataFromAddressable (MaterialData data)
		{
			Material[] mats = Renderer.materials;
			string[] materialIDs = StringToStringArray (data._materialIDs);

			int count = Mathf.Min (materialIDs.Length, mats.Length);
			for (int i = 0; i < count; i++)
			{
				if (string.IsNullOrEmpty (materialIDs[i])) continue;
				AsyncOperationHandle<Material> handle = Addressables.LoadAssetAsync<Material> (materialIDs[i]);
				yield return handle;
				if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
				{
					mats[i] = handle.Result;
				}
				Addressables.Release (handle);
			}

			Renderer.materials = mats;
		}

		#endif


		private void LoadDataFromResources (MaterialData data)
		{
			Material[] mats = Renderer.materials;
			string[] materialIDs = StringToStringArray (data._materialIDs);

			for (int i = 0; i < materialIDs.Length; i++)
			{
				if (i < mats.Length)
				{
					Material _material = AssetLoader.RetrieveAsset (mats[i], materialIDs[i]);
					if (_material)
					{
						mats[i] = _material;
					}
				}
			}

			Renderer.materials = mats;
		}


		private Renderer Renderer
		{
			get
			{
				if (_renderer == null)
				{
					_renderer = GetComponent <Renderer>();
				}
				return _renderer;
			}
		}
		
	}
	

	/** A data container used by the RememberMaterial script. */
	[System.Serializable]
	public class MaterialData : RememberData
	{

		/** The unique identifier of each Material in the Renderer */
		public string _materialIDs;

		/** The default Constructor. */
		public MaterialData () { }

	}
	
}