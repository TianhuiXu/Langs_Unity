#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (Parallax2D))]
	public class Parallax2DEditor : Editor
	{

		private Parallax2D _target;


		private void OnEnable ()
		{
			_target = (Parallax2D) target;
		}


		public override void OnInspectorGUI ()
		{
			CustomGUILayout.BeginVertical ();
			_target.reactsTo = (ParallaxReactsTo) CustomGUILayout.EnumPopup ("Reacts to:", _target.reactsTo, "", "What entity affects the parallax behaviour");
			if (_target.reactsTo == ParallaxReactsTo.Transform)
			{
				_target.transformToReactTo = (Transform) EditorGUILayout.ObjectField ("Tranform to react to:", _target.transformToReactTo, typeof (Transform), true);
			}
			_target.depth = CustomGUILayout.FloatField ("Depth:", _target.depth, "", "The intensity of the depth effect. Positive values will make the GameObject appear further away (i.e. in the background), negative values will make it appear closer to the camera (i.e. in the foreground).");
			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			_target.xScroll = CustomGUILayout.Toggle ("Scroll in X direction?", _target.xScroll, "", "If True, then the GameObject will scroll in the X-axis");
			if (_target.xScroll)
			{
				_target.xOffset = CustomGUILayout.FloatField ("Offset:", _target.xOffset, "", "An offset for the GameObject's initial position along the X-axis");
				_target.limitX = CustomGUILayout.Toggle ("Constrain?", _target.limitX, "If True, scrolling in the X-axis will be constrained");
				if (_target.limitX)
				{
					if (_target.reactsTo == ParallaxReactsTo.Camera)
					{
						_target.backgroundConstraint = (SpriteRenderer) CustomGUILayout.ObjectField<SpriteRenderer> ("Background constraint:", _target.backgroundConstraint, true, string.Empty, "If set, this sprite's boundary will be used to set the constraint limits");
					}

					if (_target.reactsTo == ParallaxReactsTo.Camera && _target.backgroundConstraint)
					{
						_target.horizontalConstraint = (Parallax2D.HorizontalParallaxConstraint) CustomGUILayout.EnumPopup ("Align to edge:", _target.horizontalConstraint, string.Empty, "Which edge of the assigned background to align itselef with");
					}
					else
					{
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField ("Minimum:", GUILayout.Width (70f));
						_target.minX = EditorGUILayout.FloatField (_target.minX);
						EditorGUILayout.LabelField ("Maximum:", GUILayout.Width (70f));
						_target.maxX = EditorGUILayout.FloatField (_target.maxX);
						EditorGUILayout.EndHorizontal ();
					}
				}
			}
			CustomGUILayout.EndVertical ();

			CustomGUILayout.BeginVertical ();
			_target.yScroll = CustomGUILayout.Toggle ("Scroll in Y direction?", _target.yScroll, "", "If True, then the GameObject will scroll in the Y-axis");
			if (_target.yScroll)
			{
				_target.yOffset = CustomGUILayout.FloatField ("Offset:", _target.yOffset, "", "An offset for the GameObject's initial position along the Y-axis");
				_target.limitY = CustomGUILayout.Toggle ("Constrain?", _target.limitY, "", "If True, scrolling in the Y-axis will be constrained");
				if (_target.limitY)
				{
					if (_target.reactsTo == ParallaxReactsTo.Camera)
					{
						_target.backgroundConstraint = (SpriteRenderer) CustomGUILayout.ObjectField<SpriteRenderer> ("Background constraint:", _target.backgroundConstraint, true, string.Empty, "If set, this sprite's boundary will be used to set the constraint limits");
					}

					if (_target.reactsTo == ParallaxReactsTo.Camera && _target.backgroundConstraint)
					{
						_target.verticalConstraint = (Parallax2D.VerticalParallaxConstraint) CustomGUILayout.EnumPopup ("Align to edge:", _target.verticalConstraint, string.Empty, "Which edge of the assigned background to align itselef with");
					}
					else
					{
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.LabelField ("Minimum:", GUILayout.Width (70f));
						_target.minY = EditorGUILayout.FloatField (_target.minY);
						EditorGUILayout.LabelField ("Maximum:", GUILayout.Width (70f));
						_target.maxY = EditorGUILayout.FloatField (_target.maxY);
						EditorGUILayout.EndHorizontal ();
					}
				}
			}
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}

#endif