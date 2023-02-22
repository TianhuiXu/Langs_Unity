/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SpeechPlayableData.cs"
 * 
 *	A data container for SpeechPlayableClip
 * 
 */

namespace AC
{

	/** A data container for SpeechPlayableClip */
	[System.Serializable]
	public class SpeechPlayableData
	{

		#region Variables

		/** The Speech Manager ID of the speech line, used for translations */
		public int lineID = -1;
		/** The display text of the speech line */
		public string messageText;
		/** If True, the line will be considered "background" speech */
		public bool isBackground = false;

		#endregion

	}

}