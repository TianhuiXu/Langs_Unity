#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (SetTriggerParameters))]
	public class SetTriggerParametersEditor : Editor
	{

		private SetTriggerParameters _target;


		public override void OnInspectorGUI ()
		{
			_target = (SetTriggerParameters) target;

			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
		
	}

}

#endif