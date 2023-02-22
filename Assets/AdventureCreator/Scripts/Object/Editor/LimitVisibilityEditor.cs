#if UNITY_EDITOR

using UnityEditor;

namespace AC
{
	
	[CustomEditor (typeof (LimitVisibility))]
	public class LimitVisibilityEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			LimitVisibility _target = (LimitVisibility) target;

			_target.Upgrade ();

			int numOptions = _target.limitToCameras.Count;
			numOptions = EditorGUILayout.DelayedIntField ("Number of cameras:", _target.limitToCameras.Count);
			if (_target.limitToCameras.Count < 0)
			{
				numOptions = 0;
			}
			if (numOptions < 1)
			{
				numOptions = 1;
			}
			
			if (numOptions < _target.limitToCameras.Count)
			{
				_target.limitToCameras.RemoveRange (numOptions, _target.limitToCameras.Count - numOptions);
			}
			else if (numOptions > _target.limitToCameras.Count)
			{
				if(numOptions > _target.limitToCameras.Capacity)
				{
					_target.limitToCameras.Capacity = numOptions;
				}
				for (int i=_target.limitToCameras.Count; i<numOptions; i++)
				{
					_target.limitToCameras.Add (null);
				}
			}
			
			for (int i=0; i<_target.limitToCameras.Count; i++)
			{
				_target.limitToCameras [i] = (_Camera) CustomGUILayout.ObjectField <_Camera> ("Camera #" + i.ToString () + ":", _target.limitToCameras [i], true, "", "An AC _Camera to limit the GameObject's visibility to");
			}

			_target.negateEffect = CustomGUILayout.Toggle ("Negate effect?", _target.negateEffect, "", "If True, then the GameObject will instead be visible when the above cameras are not used");
			_target.affectChildren = CustomGUILayout.Toggle ("Affect children too?", _target.affectChildren, "", "If True, then child GameObjects will be affected in the same way");

			UnityVersionHandler.CustomSetDirty (_target);
		}
	
	}

}

#endif