#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (RememberTransform), true)]
	public class RememberTransformEditor : ConstantIDEditor
	{
		
		public override void OnInspectorGUI ()
		{
			RememberTransform _target = (RememberTransform) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Transform", EditorStyles.boldLabel);
			_target.transformSpace = (GlobalLocal) CustomGUILayout.EnumPopup ("Co-ordinate space:", _target.transformSpace, "", "The co-ordinate space to store position and rotation values in");
			_target.saveParent = CustomGUILayout.Toggle ("Save change in Parent?", _target.saveParent, "", "If True, the GameObject's change in parent should be recorded");
			_target.saveScenePresence = CustomGUILayout.Toggle ("Save scene presence?", _target.saveScenePresence, "", "If True, the GameObject's change in scene presence should be recorded");

			if (_target.saveScenePresence)
			{
				if (Application.isPlaying && _target.gameObject.IsPersistent ())
				{
					EditorGUILayout.HelpBox ("Objects that survive scene-changes cannot have their scene presence saved.", MessageType.Warning);
				}
				else
				{
					#if AddressableIsPresent
					if (KickStarter.settingsManager.saveAssetReferencesWithAddressables)
					{
						_target.addressableName = CustomGUILayout.TextField ("Addressable name:", _target.addressableName, string.Empty, "The addressable name of the prefab to spawn in the scene, if this is not present but needs to be added.");
					}
					else
					#endif
					{
						_target.linkedPrefabID = CustomGUILayout.IntField ("Linked prefab ConstantID:", _target.linkedPrefabID, "", "If non-zero, the Constant ID number of the prefab to re-spawn if not present in the scene, but saveScenePresence = true.  If zero, the prefab will be assumed to have the same ID as this.");
						EditorGUILayout.HelpBox ("If the above is non-zero, the Resources prefab with that ID number will be spawned if this is not present in the scene.  This allows for multiple instances of the object to be spawned.", MessageType.Info);

						_target.retainInPrefab = true;
						EditorGUILayout.HelpBox ("This prefab must be placed in a 'Resources' asset folder", MessageType.Info);
					}
				}
			}
			CustomGUILayout.EndVertical ();

			SharedGUI ();
		}
		
	}

}

#endif