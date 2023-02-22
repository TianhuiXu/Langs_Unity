#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (GameCameraThirdPerson))]
	public class GameCameraThirdPersonEditor : Editor
	{

		public override void OnInspectorGUI ()
		{
			GameCameraThirdPerson _target = (GameCameraThirdPerson) target;

			_target.ShowGUI ();

			UnityVersionHandler.CustomSetDirty (_target);
		}
	}

}

#endif