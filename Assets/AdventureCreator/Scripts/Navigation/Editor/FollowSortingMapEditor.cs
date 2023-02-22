#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (FollowSortingMap))]
	public class FollowSortingMapEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			FollowSortingMap _target = (FollowSortingMap) target;

			_target.followSortingMap = CustomGUILayout.Toggle ("Follow default Sorting Map?", _target.followSortingMap, "", "If True, then the component will follow the default Sorting Map defined in the Scene Manager");
			if (!_target.followSortingMap)
			{
				_target.customSortingMap = (SortingMap) CustomGUILayout.ObjectField <SortingMap> ("Sorting Map to follow:", _target.customSortingMap, true, "", "The Sorting Map to follow");
			}


			if (_target.followSortingMap || _target.customSortingMap != null)
			{
				if (_target.GetComponentInChildren <SortingGroup>() != null)
				{
					EditorGUILayout.HelpBox ("'Sorting Group' detected - this will be controlled instead of Sprite Renderers.", MessageType.Info);
				}
				else
				{
					_target.offsetOriginal = CustomGUILayout.Toggle ("Offset original Order?", _target.offsetOriginal, "", "If True, then the SpriteRenderer's sorting values will be increased by their original values when the game began");
					_target.affectChildren = CustomGUILayout.Toggle ("Also affect children?", _target.affectChildren, "", "If True, then the sorting values of child SpriteRenderers will be affected as well");
				}

				bool oldPreviewValue = _target.livePreview;
				_target.livePreview = CustomGUILayout.Toggle ("Edit-mode preview?", _target.livePreview, "", "If True, then the script will update the SpriteRender's sorting values when the game is not running");
				if (oldPreviewValue && !_target.livePreview)
				{
					// Just unchecked, so reset scale
					if (!Application.isPlaying && _target.GetComponentInParent <Char>() != null && _target.GetComponentInParent <Char>().spriteChild != null)
					{
						_target.GetComponentInParent <Char>().transform.localScale = Vector3.one;
					}
				}
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}

#endif