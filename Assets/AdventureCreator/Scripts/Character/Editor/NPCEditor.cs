#if UNITY_EDITOR

using UnityEditor;

namespace AC
{

	[CustomEditor (typeof (NPC))]
	public class NPCEditor : CharEditor
	{

		public override void OnInspectorGUI ()
		{
			NPC _target = (NPC) target;
			
			SharedGUIOne (_target);

			NPC_GUI (_target);

			SharedGUITwo (_target);

			UnityVersionHandler.CustomSetDirty (_target);
		}


		protected void NPC_GUI (NPC _target)
		{
			CustomGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("NPC settings:", EditorStyles.boldLabel);
			_target.moveOutOfPlayersWay = CustomGUILayout.Toggle ("Keep out of Player's way?", _target.moveOutOfPlayersWay, "", "If True, the NPC will attempt to keep out of the Player's way");
			if (_target.moveOutOfPlayersWay)
			{
				_target.minPlayerDistance = CustomGUILayout.FloatField ("Min. distance to keep:", _target.minPlayerDistance, "", "The minimum distance to keep from the Player");
			}
			CustomGUILayout.EndVertical ();
		}

	}

}

#endif