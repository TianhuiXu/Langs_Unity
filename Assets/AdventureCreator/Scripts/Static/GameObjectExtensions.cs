using UnityEngine;

namespace AC
{

	public static class GameObjectExtensions
	{

		public static bool IsPersistent (this GameObject gameObject)
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying) return false;
			#endif

			if (gameObject.scene.name == "DontDestroyOnLoad" ||
				(gameObject.scene.name == null && gameObject.scene.buildIndex == -1)) // Because on Android, DontDestroyOnLoad scene has no name
			{
				return true;
			}
			return false;
		}

	}

}