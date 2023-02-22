#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace AC
{

	public class PlayerStartDataEditor : EditorWindow
	{

		private int playerIndex;


		public void Init (int _playerIndex)
		{
			playerIndex = _playerIndex;
		}


		public static void CreateNew (int _playerIndex)
		{
			PlayerStartDataEditor window = EditorWindow.GetWindowWithRect<PlayerStartDataEditor> (new Rect (0, 0, 400, 150), true, "Player start data", true);
			window.titleContent.text = "Player start data";
			window.position = new Rect (300, 200, 400, 150);

			window.Init (_playerIndex);
		}


		private void OnGUI ()
		{
			if (KickStarter.settingsManager == null) return;

			if (playerIndex >= 0 && playerIndex < KickStarter.settingsManager.players.Count)
			{
				PlayerPrefab playerPrefab = KickStarter.settingsManager.players[playerIndex];
				
				playerPrefab.ShowStartDataGUI ("AC.KickStarter.players[" + playerIndex.ToString () + "]");
			}
		}

	}

}

#endif