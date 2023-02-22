#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (GameCamera2DDrag))]
	public class GameCamera2DDragEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			GameCamera2DDrag _target = (GameCamera2DDrag) target;

			// X
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("X movement", "How movement along the X-axis is affected"), EditorStyles.boldLabel, GUILayout.Width (130f));
			_target.xLock = (RotationLock) EditorGUILayout.EnumPopup (_target.xLock);
			EditorGUILayout.EndHorizontal ();
			if (_target.xLock != RotationLock.Locked)
			{
				_target.xSpeed = CustomGUILayout.FloatField ("Speed:", _target.xSpeed, "", "The speed of X-axis movement");
				_target.xAcceleration = CustomGUILayout.FloatField ("Acceleration:", _target.xAcceleration, "", "The acceleration of X-axis movement");
				_target.xDeceleration = CustomGUILayout.FloatField ("Deceleration:", _target.xDeceleration, "", "The deceleration of X-axis movement");
				_target.invertX = CustomGUILayout.Toggle ("Invert?", _target.invertX, "", "If True, then X-axis movement will be inverted");
				_target.xOffset = CustomGUILayout.FloatField ("Offset:", _target.xOffset, "", "The X-axis offset");

				if (_target.xLock == RotationLock.Limited)
				{
					if (_target.GetComponent<Camera> ().orthographic)
					{
						_target.backgroundConstraint = (SpriteRenderer) CustomGUILayout.ObjectField<SpriteRenderer> ("Background constraint:", _target.backgroundConstraint, true, string.Empty, "If set, this sprite's boundary will be used to set the constraint limits");
						if (_target.backgroundConstraint)
						{
							_target.autoScaleToFitBackgroundConstraint = CustomGUILayout.Toggle ("Auto-set Orthographic size to fit?", _target.autoScaleToFitBackgroundConstraint, string.Empty, "If True, then the Camera's Orthographic Size value will be reduced if the background is not large enough to fill the screen.");
						}
					}

					if (!_target.GetComponent<Camera> ().orthographic || _target.backgroundConstraint == null)
					{
						_target.minX = CustomGUILayout.FloatField ("Minimum X:", _target.minX, "", "The minimum X-axis value");
						_target.maxX = CustomGUILayout.FloatField ("Maximum X:", _target.maxX, "", "The maximum X-axis value");
					}
				}
			}
			CustomGUILayout.EndVertical ();

			// Y
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Y movement", "How movement along the Y-axis is affected"), EditorStyles.boldLabel, GUILayout.Width (130f));
			_target.yLock = (RotationLock) EditorGUILayout.EnumPopup (_target.yLock);
			EditorGUILayout.EndHorizontal ();
			if (_target.yLock != RotationLock.Locked)
			{
				_target.ySpeed = CustomGUILayout.FloatField ("Speed:", _target.ySpeed, "", "The speed of Y-axis movement");
				_target.yAcceleration = CustomGUILayout.FloatField ("Acceleration:", _target.yAcceleration, "", "The acceleration of Y-axis movement");
				_target.yDeceleration = CustomGUILayout.FloatField ("Deceleration:", _target.yDeceleration, "", "The deceleration of Y-axis movement");
				_target.invertY = CustomGUILayout.Toggle ("Invert?", _target.invertY, "", "If True, then Y-axis movement will be inverted");
				_target.yOffset = CustomGUILayout.FloatField ("Offset:", _target.yOffset, "", "The Y-axis offset");
				
				if (_target.yLock == RotationLock.Limited)
				{
					if (_target.GetComponent<Camera> ().orthographic)
					{
						_target.backgroundConstraint = (SpriteRenderer) CustomGUILayout.ObjectField<SpriteRenderer> ("Background constraint:", _target.backgroundConstraint, true, string.Empty, "If set, this sprite's boundary will be used to set the constraint limits");
						if (_target.backgroundConstraint)
						{
							_target.autoScaleToFitBackgroundConstraint = CustomGUILayout.Toggle ("Auto-set Orthographic size to fit?", _target.autoScaleToFitBackgroundConstraint, string.Empty, "If True, then the Camera's Orthographic Size value will be reduced if the background is not large enough to fill the screen.");
						}
					}

					if (!_target.GetComponent<Camera> ().orthographic || _target.backgroundConstraint == null)
					{
						_target.minY = CustomGUILayout.FloatField ("Minimum Y:", _target.minY, "", "The minimum Y-axis value");
						_target.maxY = CustomGUILayout.FloatField ("Maximum Y:", _target.maxY, "", "The maximum Y-axis value");
					}
				}
			}
			CustomGUILayout.EndVertical ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif