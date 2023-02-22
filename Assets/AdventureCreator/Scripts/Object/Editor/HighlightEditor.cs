#if UNITY_EDITOR

using UnityEditor;

namespace AC
{
	
	[CustomEditor (typeof (Highlight))]
	public class HighlightEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			Highlight _target = (Highlight) target;

			_target.highlightWhenSelected = CustomGUILayout.ToggleLeft ("Enable when associated Hotspot is selected?", _target.highlightWhenSelected, "", "If True, then the Highlight effect will be enabled automatically when the Hotspot is selected");
			_target.brightenMaterials = CustomGUILayout.ToggleLeft ("Auto-brighten materials when enabled?", _target.brightenMaterials, "", "If True, then Materials associated with the GameObject's Renderer will be affected. Otherwise, their intended values will be calculated, but not applied, allowing for custom effects to be achieved");
			if (_target.brightenMaterials)
			{
				_target.affectChildren = CustomGUILayout.ToggleLeft ("Also affect child Renderer components?", _target.affectChildren, "", "If True, then child Renderer GameObjects will be brightened as well");
			}
			//_target.maxHighlight = CustomGUILayout.Slider ("Maximum highlight intensity:", _target.maxHighlight, 1f, 5f, "", "The maximum highlight intensity (1 = no effect)");
			_target.highlightCurve = CustomGUILayout.CurveField ("Intensity curve:", _target.highlightCurve, "", "An animation curve that describes the effect over time");
			_target.fadeTime = CustomGUILayout.Slider ("Transition time (s):", _target.fadeTime, 0f, 5f, "", "The fade time for the highlight transition effect");
			_target.flashHoldTime = CustomGUILayout.Slider ("Flash hold time (s)", _target.flashHoldTime, 0f, 5f, "", "The length of time that a flash will hold for");

			_target.callEvents = CustomGUILayout.ToggleLeft ("Call custom events?", _target.callEvents, "", "If True, then custom events can be called when highlighting the object");
			if (_target.callEvents)
			{
				this.serializedObject.Update ();
				EditorGUILayout.PropertyField (this.serializedObject.FindProperty ("onHighlightOn"), true);
				EditorGUILayout.PropertyField (this.serializedObject.FindProperty ("onHighlightOff"), true);
	            this.serializedObject.ApplyModifiedProperties ();
			}

			UnityVersionHandler.CustomSetDirty (_target);
		}
	
	}
}

#endif