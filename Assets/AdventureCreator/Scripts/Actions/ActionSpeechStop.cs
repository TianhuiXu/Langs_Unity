/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionSpeechStop.cs"
 * 
 *	This Action forces off all playing speech
 * 
 */

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionSpeechStop : Action
	{

		public bool forceMenus;
		public SpeechMenuLimit speechMenuLimit = SpeechMenuLimit.All;
		public SpeechMenuType speechMenuType = SpeechMenuType.All;
		public string limitToCharacters = "";


		public override ActionCategory Category { get { return ActionCategory.Dialogue; }}
		public override string Title { get { return "Stop speech"; }}
		public override string Description { get { return "Ends any currently-playing speech instantly."; }}
		
		
		public override float Run ()
		{
			KickStarter.dialog.KillDialog (true, forceMenus, speechMenuLimit, speechMenuType, limitToCharacters);

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI ()
		{
			speechMenuLimit = (SpeechMenuLimit) EditorGUILayout.EnumPopup ("Speech to stop:", speechMenuLimit);
			speechMenuType = (SpeechMenuType) EditorGUILayout.EnumPopup ("Characters to stop", speechMenuType);

			if (speechMenuType == SpeechMenuType.SpecificCharactersOnly)
			{
				limitToCharacters = EditorGUILayout.TextField ("Character(s) to stop:", limitToCharacters);
				EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
			}
			else if (speechMenuType == SpeechMenuType.AllExceptSpecificCharacters)
			{
				limitToCharacters = EditorGUILayout.TextField ("Character(s) to not stop:", limitToCharacters);
				EditorGUILayout.HelpBox ("Multiple character names should be separated by a colon ';'", MessageType.Info);
			}

			forceMenus = EditorGUILayout.Toggle ("Force off subtitles?", forceMenus);
		}
		
		
		public override string SetLabel ()
		{
			return speechMenuLimit.ToString ();
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Dialogue: Stop speech' Action</summary>
		 * <param name = "speechToStop">The type of speech to stop</param>
		 * <param name = "charactersToStop">The type of speaking characters to Stop</param>
		 * <param name = "specificCharacters">The specific characters, separated by colons</param>
		 * <param name = "forceOffSubtitles">If True, then any subtitles associated with the speech will be turned off</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionSpeechStop CreateNew (SpeechMenuLimit speechToStop, SpeechMenuType charactersToStop, string specificCharacters = "", bool forceOffSubtitles = false)
		{
			ActionSpeechStop newAction = CreateNew<ActionSpeechStop> ();
			newAction.speechMenuLimit = speechToStop;
			newAction.speechMenuType = charactersToStop;
			newAction.limitToCharacters = specificCharacters;
			newAction.forceMenus = forceOffSubtitles;
			return newAction;
		}
		
	}

}