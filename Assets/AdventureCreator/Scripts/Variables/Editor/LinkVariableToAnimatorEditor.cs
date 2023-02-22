#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (LinkVariableToAnimator))]
	public class LinkVariableToAnimatorEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			LinkVariableToAnimator _target = (LinkVariableToAnimator) target;
			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}

	}

}

#endif