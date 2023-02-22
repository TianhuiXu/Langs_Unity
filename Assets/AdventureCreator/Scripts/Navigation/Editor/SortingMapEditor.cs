#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (SortingMap))]
	[System.Serializable]
	public class SortingMapEditor : Editor
	{
		
		private static readonly GUIContent
			insertContent = new GUIContent("+", "Insert node"),
			deleteContent = new GUIContent("-", "Delete node");
		
		private static readonly GUILayoutOption
			labelWidth = GUILayout.MaxWidth (40f),
			buttonWidth = GUILayout.MaxWidth (20f);


		public override void OnInspectorGUI()
		{
			SortingMap _target = (SortingMap) target;

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Properties", EditorStyles.boldLabel);

			_target.affectSorting = CustomGUILayout.Toggle ("Affect Character sorting?", _target.affectSorting, "", "If True, characters that follow this map should have their sorting affected");
			if (_target.affectSorting)
			{
				_target.mapType = (SortingMapType) CustomGUILayout.EnumPopup ("Affect sprite's:", _target.mapType);
			}
			_target.affectScale = CustomGUILayout.Toggle ("Affect Character size?", _target.affectScale, "", "If True, characters that follow this map should have their scale affected");
			_target.affectSpeed = CustomGUILayout.Toggle ("Affect Character speed?", _target.affectSpeed, "", "If True, characters that follow this map should have their movement speed affected by the scale factor");

			if (_target.affectScale || _target.affectSpeed)
			{
				_target.sortingMapScaleType = (SortingMapScaleType) CustomGUILayout.EnumPopup ("Scaling mode:", _target.sortingMapScaleType, "", "How scaling values are defined");
				if (_target.sortingMapScaleType == SortingMapScaleType.Linear || _target.sortingAreas.Count == 0)
				{
					_target.originScale = CustomGUILayout.IntField ("Start scale (%):", _target.originScale, "", "The scale (as a percentage) that characters will have at the very top of the map");

					if (_target.sortingMapScaleType == SortingMapScaleType.AnimationCurve)
					{
						EditorGUILayout.HelpBox ("The Sorting Map must have at least one area defined to make use of an animation curve.", MessageType.Warning);
					}
				}
				else
				{
					if (_target.scalingAnimationCurve == null)
					{
						_target.scalingAnimationCurve = AnimationCurve.Linear (0f, 0.1f, 1f, 1f);
					}
					_target.scalingAnimationCurve = CustomGUILayout.CurveField ("Scaling curve:", _target.scalingAnimationCurve, "", "The AnimationCurve used to define character scaling, where 0s is the smallest scale, and 1s is the largest");
					EditorGUILayout.HelpBox ("The curve's values will be read from 0s to 1s only.", MessageType.Info);
				}

				if (_target.sortingMapScaleType == SortingMapScaleType.Linear && _target.sortingAreas.Count > 1)
				{
					if (GUILayout.Button ("Interpolate in-between scales"))
					{
						Undo.RecordObject (_target, "Interpolate scales");
						_target.SetInBetweenScales ();
						EditorUtility.SetDirty (_target);
					}
				}
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Sorting areas", EditorStyles.boldLabel);
			foreach (SortingArea area in _target.sortingAreas)
			{
				int i = _target.sortingAreas.IndexOf (area);

				CustomGUILayout.BeginVertical ();
				EditorGUILayout.BeginHorizontal ();

				area.color = EditorGUILayout.ColorField (area.color);

				EditorGUILayout.LabelField ("Position:", GUILayout.Width (55f));
				area.z = EditorGUILayout.FloatField (area.z, GUILayout.Width (80f));

				if (_target.affectSorting)
				{
					if (_target.mapType == SortingMapType.OrderInLayer)
					{
						EditorGUILayout.LabelField ("Order:", labelWidth);
						area.order = EditorGUILayout.IntField (area.order);
					}
					else if (_target.mapType == SortingMapType.SortingLayer)
					{
						EditorGUILayout.LabelField ("Layer:", labelWidth);
						area.layer = EditorGUILayout.TextField (area.layer);
					}
				}

				if (GUILayout.Button (insertContent, EditorStyles.miniButtonLeft, buttonWidth))
				{
					Undo.RecordObject (_target, "Add area");
					if (i < _target.sortingAreas.Count - 1)
					{
						_target.sortingAreas.Insert (i+1, new SortingArea (area, _target.sortingAreas[i+1]));
					}
					else
					{
						_target.sortingAreas.Insert (i+1, new SortingArea (area));
					}
					break;
				}
				if (GUILayout.Button (deleteContent, EditorStyles.miniButtonRight, buttonWidth))
				{
					Undo.RecordObject (_target, "Delete area");
					_target.sortingAreas.Remove (area);
					break;
				}

				EditorGUILayout.EndHorizontal ();

				if ((_target.affectScale || _target.affectSpeed) && _target.sortingMapScaleType == SortingMapScaleType.Linear)
				{
					area.scale = CustomGUILayout.IntField ("End scale (%):", area.scale, "", "The factor by which characters that use FollowSortingMap will be scaled by when positioned at the bottom boundary of this region");
				}

				CustomGUILayout.EndVertical ();
				CustomGUILayout.DrawUILine ();
			}

			if (GUILayout.Button ("Add area"))
			{
				Undo.RecordObject (_target, "Add area");

				if (_target.sortingAreas.Count > 0)
				{
					SortingArea lastArea = _target.sortingAreas [_target.sortingAreas.Count - 1];
					_target.sortingAreas.Add (new SortingArea (lastArea));
				}
				else
				{
					_target.sortingAreas.Add (new SortingArea (_target.transform.position.z + 1f, 1));
				}
			}

			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();

			if (SceneSettings.IsTopDown ())
			{}
			else if (SceneSettings.IsUnity2D ())
			{}
			else
			{
				if (GUILayout.Button ("Face active camera"))
				{
					Undo.RecordObject (_target, "Face active camera");
					Vector3 forwardVector = KickStarter.CameraMainTransform.forward;
					_target.transform.forward = -forwardVector;
					EditorUtility.SetDirty (_target);
				}
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void OnSceneGUI ()
		{
			SortingMap _target = (SortingMap) target;

			GUIStyle style = new GUIStyle ();
			style.normal.textColor = Color.white;
			style.normal.background = Resource.GreyTexture;

			for (int i=0; i<_target.sortingAreas.Count; i++)
			{
				Vector3 newPosition = _target.GetAreaPosition (i);
				newPosition = Handles.PositionHandle (newPosition, Quaternion.identity);
				_target.sortingAreas [i].z = (newPosition - _target.transform.position).magnitude / _target.transform.forward.magnitude;

				Vector3 midPoint = _target.transform.position;
				if (i == 0)
				{
					midPoint += _target.transform.forward * _target.sortingAreas [i].z / 2f;
				}
				else
				{
					midPoint += _target.transform.forward * (_target.sortingAreas [i].z + _target.sortingAreas [i-1].z) / 2f;
				}

				string label = (_target.mapType == SortingMapType.OrderInLayer) ? _target.sortingAreas [i].order.ToString () : _target.sortingAreas [i].layer;

				Handles.Label (midPoint, label, style);
			}
			
			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif