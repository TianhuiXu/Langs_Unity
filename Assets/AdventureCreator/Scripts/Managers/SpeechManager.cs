/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"SpeechManager.cs"
 * 
 *	This script handles the "Speech" tab of the main wizard.
 *	It is used to auto-number lines for audio files, and handle translations.
 * 
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using AC.SML;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Playables;

#if !ACIgnoreTimeline
using UnityEngine.Timeline;
#endif

namespace AC
{

	/**
	 * Handles the "Speech" tab of the Game Editor window.
	 * All translations for a game's text are stored here, as are the settings that control how speech is handled in-game.
	 */
	[System.Serializable]
	public class SpeechManager : ScriptableObject
	{

		/** If True, then speech text will scroll when displayed */
		public bool scrollSubtitles = true;
		/** If True, then narration text will scroll when displayed */
		public bool scrollNarration = false;
		/** The speed of scrolling text */
		public float textScrollSpeed = 50;
		/** The AudioClip to play when scrolling speech text */
		public AudioClip textScrollCLip = null;
		/** The AudioClip to play when scrolling narration text */
		public AudioClip narrationTextScrollCLip = null;
		/** If True, the textScrollClip audio will be played with every character addition to the subtitle text, as opposed to waiting for the previous audio to end */
		public bool playScrollAudioEveryCharacter = true;
		/** What to play speech-scrolling audio as */
		public SpeechScrollAudioSource speechScrollAudioSource = SpeechScrollAudioSource.SFX;
		/** The default delay to insert after triggering a line of dialogue with the 'Dialogue: Play speech' Action. A further delay can be added per-Action. */
		public float waitTimeOffset = 0f;

		/** If True, then speech text will remain on the screen until the player skips it */
		public bool displayForever = false;
		/** If true, then narration text will remain on the screen until the player skips it.  This only has an effect if displayForever = false */
		public bool displayNarrationForever = false;
		/** If True, and displayForever = True, then a speaking character will play their talking animation for the whole duration that their speech text is alive */
		public bool playAnimationForever = true;
		/** If True, and subtitles can be skipped, then skipping can be achieved with mouse-clicks, as well as by invoking the SkipSpeech input */
		public bool canSkipWithMouseClicks = true;
		/** The minimum time, in seconds, that a speech line will be displayed (unless an AudioClip is setting it's length) */
		public float minimumDisplayTime = 1f;
		/** The time that speech text will be displayed, divided by the number of characters in the text, if displayForever = False */
		public float screenTimeFactor = 0.1f;
		/** If True, then speech text during a cutscene can be skipped by the player left-clicking */
		public bool allowSpeechSkipping = false;
		/** If True, then speech text during gameplay can be skipped by the player left-clicking */
		public bool allowGameplaySpeechSkipping = false;
		/** The minimum time that speech text must be displayed before it can be skipped, if allowSpeechSkipping = True */
		public float skipThresholdTime = 0f;
		/** Deprecated */
		public bool endScrollBeforeSkip = false;
		/** What action to take if the user attempts to skip speech while it it still scrolling */
		public IfSkipWhileScrolling ifSkipWhileScrolling = IfSkipWhileScrolling.DisplayFullText;
		/** If True, and text is scrolling, then the display time upon completion will be influenced by the length of the speech text */
		public bool scrollingTextFactorsLength = false;
		/** If True, then subtitles with audio will cease to display once the audio has completed */
		public bool syncSubtitlesToAudio = false;

		/** Deprecated */
		public bool useAssetBundles = false;
		/** If True, then speech audio files will play when characters speak */
		public bool searchAudioFiles = true;
		/** Deprecated - use referenceSpeechFiles instead */
		public bool autoNameSpeechFiles = true;
		/** The method by which speech files (audio and lipsync) are referenced */
		public ReferenceSpeechFiles referenceSpeechFiles = ReferenceSpeechFiles.ByDirectReference;

		/** The subdirectory within Resources that speech files are pulled from, if referenceSpeechFiles = ReferenceSpeechFiles.ByNamingConvention */
		public string autoSpeechFolder = "Speech";
		/** The subdirectory within Resources that lipsync files are pulled from, if referenceSpeechFiles = ReferenceSpeechFiles.ByNamingConvention */
		public string autoLipsyncFolder = "Lipsync";
		/** If True, then speech text will always display if no relevant audio file is found - even if Subtitles are off in the Options menu */
		public bool forceSubtitles = true;
		/** If True, then each translation will have its own set of speech audio files */
		public bool translateAudio = true;
		/** If True, then the current voice audio language can be set independently of the the current text language. */
		public bool separateVoiceAndTextLanguages = false;
		/** If True, then translations that don't have speech audio files will use the audio files from the game's original language */
		public bool fallbackAudio = false;
		/** If True, then the text stored in the speech buffer (in MenuLabel) will not be cleared when no speech text is active */
		public bool keepTextInBuffer = false;
		/** If True, then background speech audio will end if foreground speech audio begins to play */
		public bool relegateBackgroundSpeechAudio = false;
		/** If True, then speech audio spoken by the player will expect the audio filenames to be named after the player's prefab, rather than just "Player" */
		public bool usePlayerRealName = false;
		/** If True, usePlayerRealName = True,referenceSpeechFiles = ReferenceSpeechFiles.ByNamingConvention, and playerSwitching = PlayerSwitching.Allow in SettingsManager, then speech lines marked as Player lines will have audio entries for each player prefab. */
		public bool separateSharedPlayerAudio = false;

		/** If True, then speech audio files will need to be placed in subfolders named after the character who speaks */
		public bool placeAudioInSubfolders = false;
		/** If True, then a speech line will be split by carriage returns into separate speech lines */
		public bool separateLines = false;
		/** The delay between carriage return-separated speech lines, if separateLines = True */
		public float separateLinePause = 1f;
		/** If True, then a character's expression will be reset with each new speech line */
		public bool resetExpressionsEachLine = true;

		#if LocalizationIsPresent
		/** If True, then the Localization system's Locale will be automatically synced with AC's language */
		public bool autoSyncLocaleWithLanguage = false;
		#endif

		/** All SpeechLines generated to store translations and audio filename references */
		public List<SpeechLine> lines = new List<SpeechLine> ();
		/** Deprecated - use Languages instead */
		[SerializeField] private List<string> languages = new List<string> ();
		/** Deprecated - use Languages instead */
		[SerializeField] private List<bool> languageIsRightToLeft = new List<bool> ();
		/** Deprecated - use Languages instead */
		[SerializeField] private List<string> languageAudioAssetBundles = new List<string>();
		/** Deprecated - use Languages instead */
		[SerializeField] private List<string> languageLipsyncAssetBundles = new List<string>();

		/** A list of all languages defined in the game. */
		public List<Language> Languages = new List<Language> ();

		[SerializeField] private bool ignoreOriginalText = false;
	
		/** The factor by which to reduce SFX audio when speech plays */
		public float sfxDucking = 0f;
		/** The factor by which to reduce music audio when speech plays */
		public float musicDucking = 0f;

		/** The game's lip-syncing method (Off, FromSpeechText, ReadPamelaFile, ReadSapiFile, ReadPapagayoFile, FaceFX, Salsa2D) */
		public LipSyncMode lipSyncMode = LipSyncMode.Off;
		/** What lip-syncing actually affects (Portrait, PortraitAndGameObject, GameObjectTexture) */
		public LipSyncOutput lipSyncOutput = LipSyncOutput.Portrait;
		/** The phoneme bins used to separate phonemes into animation frames */
		public List<string> phonemes = new List<string> ();
		/** The speed at which to process lip-sync data */
		public float lipSyncSpeed = 1f;

		/** An override delegate for the GetAutoAssetPathAndName function, used to retrieve the full filepath of an auto-assigned speech audio or lipsync file */
		public GetAutoAssetPathAndNameDelegate GetAutoAssetPathAndNameOverride;
		/** A delegate template for overriding the GetAutoAssetPathAndName function */
		public delegate string GetAutoAssetPathAndNameDelegate (SpeechLine speechLine, string language, bool forLipSync);
		/** What types of text are able to be translated (and will be included in the 'gather text' process) */
		public AC_TextTypeFlags translatableTextTypes = (AC_TextTypeFlags) ~0;

		#if UNITY_EDITOR

		/** A record of the highest-used ID number */
		public int maxID = -1;
		/** The rule to use when assigning new ID numbers (NeverRecycle, AlwaysRecycle, OnlyRecycleHighest */
		public SpeechIDRecycling speechIDRecycling = SpeechIDRecycling.NeverRecycle;

		private string[] sceneFiles;
		/** The current SpeechLine selected to reveal its properties */
		[NonSerialized] public int activeLineID = -1;
		/** Deprecated - use mergeMatchingIDs instead */
		public bool mergeMatchingSpeechIDs = false;

		/** If True, then 'Dialogue: Play speech' Actions can be assigned a SpeechTag, or label, to use when exporting script sheets */
		public bool useSpeechTags = false;
		/** A List of the available SpeechTags */
		public List<SpeechTag> speechTags = new List<SpeechTag>();
		
		private List<string> sceneNames = new List<string>();
		private List<SpeechLine> tempLines = new List<SpeechLine>();
		private List<ActionListAsset> allActionListAssets;
		private string textFilter;
		private FilterSpeechLine filterSpeechLine = FilterSpeechLine.Text;
		private GameTextSorting gameTextSorting = GameTextSorting.None;
		private GameTextSorting lastGameTextSorting = GameTextSorting.None;
		private List<ActionListAsset> checkedAssets = new List<ActionListAsset>();
		private TextTypeFilter typeFilter = TextTypeFilter.Speech;
		private int tagFilter;
		private int sceneFilter;
		private int sideLanguage;

		private AudioFilter audioFilter;
		private enum AudioFilter { None, OnlyWithAudio, OnlyWithoutAudio };

		private enum TransferComment { NotAsked, Yes, No };
		private TransferComment transferComment;

		private bool showSubtitles = true;
		private bool showAudio = true;
		private bool showLipSyncing = true;
		private bool showTranslations = true;
		private bool showGameText = true;
		private bool showScrollingAudio = true;
		private int minOrderValue;

		private List<SpeechLine> addedLines = new List<SpeechLine> ();

		/** The name of the Menu to display subtitles in when previewing Speech tracks in a Timeline */
		public string previewMenuName;

		public MergeMatchingIDs mergeMatchingIDs = MergeMatchingIDs.NoMerging;

		#if !ACIgnoreTimeline
		private List<TimelineAsset> allTimelineAssets = new List<TimelineAsset>();
		#endif


		/** Shows the GUI. */
		public void ShowGUI (Rect position)
		{
			#if UNITY_WEBPLAYER
			EditorGUILayout.HelpBox ("Exporting game text cannot be performed in WebPlayer mode - please switch platform to do so.", MessageType.Warning);
			GUILayout.Space (10);
			#endif

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showSubtitles = CustomGUILayout.ToggleHeader (showSubtitles, "Subtitles");
			if (showSubtitles)
			{
				previewMenuName = CustomGUILayout.DelayedTextField ("Subtitle preview menu:", previewMenuName, "AC.KickStarter.speechManager.previewMenuName", "The name of the Menu to display subtitles in when previewing Speech tracks in a Timeline.");

				waitTimeOffset = CustomGUILayout.Slider ("Initial post-line delay (s):", waitTimeOffset, 0f, 4f, "AC.KickStarter.speechManager.waitTimeOffset", "The default delay to insert after triggering a line of dialogue with the 'Dialogue: Play speech' Action. A further delay can be added per-Action.");

				separateLines = CustomGUILayout.ToggleLeft ("Treat carriage returns as separate speech lines?", separateLines, "AC.KickStarter.speechManager.separateLines", "If True, then a speech line will be split by carriage returns into separate speech lines");
				if (separateLines)
				{
					separateLinePause = CustomGUILayout.Slider ("Split line delay (s):", separateLinePause, 0f, 1f, "AC.KickStarter.speechManager.separateLinePause", "The delay between carriage return-separated speech lines");
				}
				scrollSubtitles = CustomGUILayout.ToggleLeft ("Scroll speech text?", scrollSubtitles, "AC.KickStarter.speechManager.scrollSubtitles", "If True, then speech text will scroll when displayed");
				scrollNarration = CustomGUILayout.ToggleLeft ("Scroll narration text?", scrollNarration, "AC.KickStarter.speechManager.scrollNarration", "If True, then narration text will scroll when displayed");
				if (scrollSubtitles || scrollNarration)
				{
					textScrollSpeed = CustomGUILayout.FloatField ("Text scroll speed:", textScrollSpeed, "AC.KickStarter.speechManager.textScrollSpeed", "The speed of scrolling text");
				}

				displayForever = CustomGUILayout.ToggleLeft ("Display subtitles forever until user skips it?", displayForever, "AC.KickStarter.speechManager.displayForever", "If True, then speech text will remain on the screen until the player skips it");
				if (!displayForever)
				{
					displayNarrationForever = CustomGUILayout.ToggleLeft ("Display narration forever until user skips it?", displayNarrationForever, "AC.KickStarter.speechManager.displayNarrationForever", "If true, then narration text will remain on the screen until the player skips it.");
				}

				
				if (!displayForever)
				{
					syncSubtitlesToAudio = CustomGUILayout.ToggleLeft ("Sync subtitles display with speech audio?", syncSubtitlesToAudio, "AC.KickStarter.speechManager.syncSubtitlesToAudio", "If True, then subtitles with audio will cease to display once the audio has completed.");
					if (syncSubtitlesToAudio)
					{
						EditorGUILayout.LabelField ("For lines with no audio:");
					}
					minimumDisplayTime = CustomGUILayout.FloatField ("Minimum display time (s):", minimumDisplayTime, "AC.KickStarter.speechManager.minimumDisplayTime", "The minimum time, in seconds, that a speech line will be displayed (unless an AudioClip is setting its length)");
					screenTimeFactor = CustomGUILayout.FloatField ("Display time factor:", screenTimeFactor, "AC.KickStarter.speechManager.screenTimeFactor", "The time that speech text will be displayed, divided by the number of characters in the text");
					if (syncSubtitlesToAudio)
					{
						EditorGUILayout.Space ();
					}
					allowSpeechSkipping = CustomGUILayout.ToggleLeft ("Subtitles can be skipped?", allowSpeechSkipping, "AC.KickStarter.speechManager.allowSpeechSkipping", "If True, then speech text during a cutscene can be skipped by the player left-clicking");

					if (screenTimeFactor > 0f)
					{
						if (scrollSubtitles || scrollNarration)
						{
							scrollingTextFactorsLength = CustomGUILayout.ToggleLeft ("Text length influences display time?", scrollingTextFactorsLength, "AC.KickStarter.speechManager.scrollingTextFactorsLength", "If True, and text is scrolling, then the display time upon completion will be influenced by the length of the speech text. This option will be ignored if speech has accompanying audio.");
							if (scrollingTextFactorsLength)
							{
								EditorGUILayout.HelpBox ("This option will be ignored if speech has accompanying audio.", MessageType.Info);
							}
						}
					}
				}

				if (displayForever || displayNarrationForever || allowSpeechSkipping)
				{
					string skipClickLabel = "Can skip with mouse clicks?";
					if (KickStarter.settingsManager != null)
					{
						 if ((KickStarter.settingsManager.inputMethod == InputMethod.MouseAndKeyboard && !KickStarter.settingsManager.defaultMouseClicks) ||
						 	KickStarter.settingsManager.inputMethod == InputMethod.KeyboardOrController)
						 {
							skipClickLabel = "Can skip with InteractionA / InteractionB inputs?";
						 }
						 else if (KickStarter.settingsManager.inputMethod == InputMethod.TouchScreen)
						 {
							skipClickLabel = "Can skip with screen taps?";
						 }
					}
					canSkipWithMouseClicks = CustomGUILayout.ToggleLeft (skipClickLabel, canSkipWithMouseClicks, "AC.KickStarter.speechManager.canSkipWithMouseClicks", "If True, and subtitles can be skipped, then skipping can be achieved with mouse-clicks, as well as by invoking the SkipSpeech input");
		
					skipThresholdTime = CustomGUILayout.FloatField ("Time before can skip (s):", skipThresholdTime, "AC.KickStarter.speechManager.skipThresholdTime", "The minimum time that speech text must be displayed before it can be skipped");
					if (scrollSubtitles || scrollNarration)
					{
						if (endScrollBeforeSkip)
						{
							endScrollBeforeSkip = false;
							ifSkipWhileScrolling = IfSkipWhileScrolling.DisplayFullText;
						}
						ifSkipWhileScrolling = (IfSkipWhileScrolling) CustomGUILayout.EnumPopup ("If skip while scrolling:", ifSkipWhileScrolling, "AC.KickStarter.speechManager.ifSkipWhileScrolling", "What action to take if the user attempts to skip speech while it it still scrolling");
					}
					allowGameplaySpeechSkipping = CustomGUILayout.ToggleLeft ("Subtitles during gameplay can also be skipped?", allowGameplaySpeechSkipping, "AC.KickStarter.speechManager.allowGameplaySpeechSkipping", "If True, then speech text during gameplay can be skipped by the player left-clicking");

					if (displayForever)
					{
						playAnimationForever = CustomGUILayout.ToggleLeft ("Play talking animations forever until user skips it?", playAnimationForever, "AC.KickStarter.speechManager.playAnimationForever", "If True, then a speaking character will play their talking animation for the whole duration that their speech text is alive");
					}
				}

				keepTextInBuffer = CustomGUILayout.ToggleLeft ("Retain subtitle text buffer once line has ended?", keepTextInBuffer, "AC.KickStarter.speechManager.keepTextInBuffer", "If True, then the text stored in the speech buffer (in Label menu elements) will not be cleared when no speech text is active");
				resetExpressionsEachLine = CustomGUILayout.ToggleLeft ("Reset character expression with each line?", resetExpressionsEachLine, "AC.KickStarter.speechManager.resetExpressionsEachLine", "If True, then a character's expression will be reset with each new speech line");

				if (GUILayout.Button ("Edit speech tags"))
				{
					SpeechTagsWindow.Init ();
				}

				if (scrollSubtitles || scrollNarration)
				{
					EditorGUILayout.Space ();
					showScrollingAudio = CustomGUILayout.ToggleHeader (showScrollingAudio, "Subtitle-scrolling audio");
					if (showScrollingAudio)
					{
						if (scrollSubtitles)
						{
							textScrollCLip = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Speech text-scroll audio:", textScrollCLip, false, "AC.KickStarter.speechManager.textScrollClip", "The AudioClip to play when scrolling speech text ");
						}
						if (scrollNarration)
						{
							narrationTextScrollCLip = (AudioClip) CustomGUILayout.ObjectField <AudioClip> ("Narration text-scroll audio:", narrationTextScrollCLip, false, "AC.KickStarter.speechManager.narrationTextScrollCLip", "The AudioClip to play when scrolling narration text");
						}
						if (scrollSubtitles || scrollNarration)
						{
							playScrollAudioEveryCharacter = CustomGUILayout.Toggle ("Play audio every letter?", playScrollAudioEveryCharacter, "AC.KickStarter.speechManager.playScrollAudioEveryCharacter", "If True, the AudioClip above will be played with every character addition to the subtitle text, as opposed to waiting for the previous audio to end");
						}
						if (scrollSubtitles)
						{
							speechScrollAudioSource = (SpeechScrollAudioSource) CustomGUILayout.EnumPopup ("Speech scroll audio is:", speechScrollAudioSource, "AC.KickStarter.speechManager.speechScrollAudioSource", "What sound type to play speech-scrolling audio as.  If Speech, it will come from the speaking character's own Speech AudioSource.");
						}
					}
				}
			}
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showAudio = CustomGUILayout.ToggleHeader (showAudio, "Speech audio");
			if (showAudio)
			{
				if (IsTextTypeTranslatable (AC_TextType.Speech))
				{
					Upgrade ();

					forceSubtitles = CustomGUILayout.ToggleLeft ("Force subtitles to display when no speech audio is found?", forceSubtitles, "AC.KickStarter.speechManager.forceSubtitles", "If True, then speech text will always display if no relevant audio file is found - even if Subtitles are off in the Options menu");
					searchAudioFiles = CustomGUILayout.ToggleLeft ("Auto-play speech audio files?", searchAudioFiles, "AC.KickStarter.speechManager.searchAudioFiles", "If True, then speech audio files will play when characters speak");

					referenceSpeechFiles = (ReferenceSpeechFiles)CustomGUILayout.EnumPopup ("Reference speech files:", referenceSpeechFiles, "AC.KickStarter.speechManager.referenceSpeechFiles", "How speech audio and lipsync files are referenced");

					if (referenceSpeechFiles == ReferenceSpeechFiles.ByNamingConvention)
					{
						autoSpeechFolder = CustomGUILayout.TextField ("Speech audio directory:", autoSpeechFolder, "AC.KickStarter.speechManager.autoSpeechFolder", "The subdirectory within Resources that speech files are pulled from");
					}

					if (referenceSpeechFiles == ReferenceSpeechFiles.ByAddressable)
					{
						#if !AddressableIsPresent
						EditorGUILayout.HelpBox ("The 'AddressableIsPresent' preprocessor define must be declared in the Player Settings.", MessageType.Warning);
						#endif
					}

					#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
					if (referenceSpeechFiles == ReferenceSpeechFiles.ByDirectReference)
					{
						EditorGUILayout.HelpBox ("Manually-assigning speech files takes memory - consider auto-naming on this platform.", MessageType.Warning);
					}
					#endif

					translateAudio = CustomGUILayout.ToggleLeft ("Speech audio can be translated?", translateAudio, "AC.KickStarter.speechManager.translateAudio", "If True, then each translation will have its own set of speech audio files");
					if (translateAudio)
					{
						separateVoiceAndTextLanguages = CustomGUILayout.ToggleLeft ("Speech audio and display text can be different languages?", separateVoiceAndTextLanguages, "AC.KickStarter.speechManager.separateVoiceAndTextLanguages", "If True, then the current voice audio language can be set independently of the the current text language.");
						if (referenceSpeechFiles == ReferenceSpeechFiles.ByNamingConvention || referenceSpeechFiles == ReferenceSpeechFiles.ByDirectReference)
						{
							fallbackAudio = CustomGUILayout.ToggleLeft ("Use original language audio if none found?", fallbackAudio, "AC.KickStarter.speechManager.fallbackAudio", "If True, then translations that don't have speech audio files will use the audio files from the game's original language");
						}
					}

					usePlayerRealName = CustomGUILayout.ToggleLeft ("Use Player prefab name in filenames?", usePlayerRealName, "AC.KickStarter.speechManager.usePlayerRealName", "If True, then speech audio spoken by the player will expect the audio filenames to be named after the player's prefab, rather than just 'Player'");
					if (referenceSpeechFiles == ReferenceSpeechFiles.ByNamingConvention)
					{
						if (usePlayerRealName && KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
						{
							separateSharedPlayerAudio = CustomGUILayout.ToggleLeft ("'Player' lines have separate audio for each Player?", separateSharedPlayerAudio, "AC.KickStarter.speechManager.separateSharedPlayerAudio", "If True, then speech lines marked as Player lines will have audio entries for each player prefab.");
						}
						placeAudioInSubfolders = CustomGUILayout.ToggleLeft ("Place audio files in speaker subfolders?", placeAudioInSubfolders, "AC.KickStarter.speechManager.placeAudioInSubfolders", "If True, then speech audio files will need to be placed in subfolders named after the character who speaks");
					}
					else if (referenceSpeechFiles == ReferenceSpeechFiles.ByAssetBundle || referenceSpeechFiles == ReferenceSpeechFiles.ByAddressable)
					{
						if (usePlayerRealName && KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
						{
							separateSharedPlayerAudio = CustomGUILayout.ToggleLeft ("'Player' lines have separate audio for each Player?", separateSharedPlayerAudio, "AC.KickStarter.speechManager.separateSharedPlayerAudio", "If True, then speech lines marked as Player lines will have audio entries for each player prefab.");
						}
					}

					sfxDucking = CustomGUILayout.Slider ("SFX reduction during:", sfxDucking, 0f, 1f, "AC.KickStarter.speechManager.sfxDucking", "The factor by which to reduce SFX audio when speech plays");
					musicDucking = CustomGUILayout.Slider ("Music reduction during:", musicDucking, 0f, 1f, "AC.KickStarter.speechManager.musicDucking", "The factor by which to reduce music audio when speech plays");
					relegateBackgroundSpeechAudio = CustomGUILayout.ToggleLeft ("End background speech audio if non-background plays?", relegateBackgroundSpeechAudio, "AC.KickStarter.speechManager.relegateBackgroundSpeechAudio", "If True, then background speech audio will end if foreground speech audio begins to play");
				}
				else
				{
					EditorGUILayout.HelpBox ("Speech audio is not possible because 'Speech' is not marked as a translatable text type.", MessageType.Info);
				}
			}
			CustomGUILayout.EndVertical ();
			EditorGUILayout.Space ();

			EditorGUILayout.Space ();
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showLipSyncing = CustomGUILayout.ToggleHeader (showLipSyncing, "Lip syncing");
			if (showLipSyncing)
			{
				lipSyncMode = (LipSyncMode) CustomGUILayout.EnumPopup ("Lip syncing:", lipSyncMode, "AC.KickStarter.speechManager.lipSyncMode", "The game's lip-syncing method");

				if (referenceSpeechFiles == ReferenceSpeechFiles.ByNamingConvention && UseFileBasedLipSyncing ())
				{
					autoLipsyncFolder = CustomGUILayout.TextField ("Lipsync data directory:", autoLipsyncFolder, "AC.KickStarter.speechManager.autoLipsyncFolder", "The subdirectory within Resources that lipsync files are pulled from");
				}

				if (lipSyncMode == LipSyncMode.FromSpeechText || lipSyncMode == LipSyncMode.ReadPamelaFile || lipSyncMode == LipSyncMode.ReadSapiFile || lipSyncMode == LipSyncMode.ReadPapagayoFile)
				{
					lipSyncOutput = (LipSyncOutput) CustomGUILayout.EnumPopup ("Perform lipsync on:", lipSyncOutput, "AC.KickStarter.speechManager.lipSyncOutput", "What lip-syncing actually affects");
					lipSyncSpeed = CustomGUILayout.FloatField ("Process speed:", lipSyncSpeed, "AC.KickStarter.speechManager.lipSyncSpeed", "The speed at which to process lip-sync data");
					
					if (GUILayout.Button ("Edit phonemes"))
					{
						PhonemesWindow.Init ();
					}

					if (lipSyncOutput == LipSyncOutput.GameObjectTexture)
					{
						EditorGUILayout.HelpBox ("Characters will require the 'LipSyncTexture' component in order to perform lip-syncing.", MessageType.Info);
					}
				}
				else if (lipSyncMode == LipSyncMode.FaceFX && !FaceFXIntegration.IsDefinePresent ())
				{
					EditorGUILayout.HelpBox ("The 'FaceFXIsPresent' preprocessor define must be declared in the Player Settings.", MessageType.Warning);
				}
				else if (lipSyncMode == LipSyncMode.Salsa2D)
				{
					lipSyncOutput = (LipSyncOutput) CustomGUILayout.EnumPopup ("Perform lipsync on:", lipSyncOutput, "AC.KickStarter.speechManager.lipSyncOutput");
					
					EditorGUILayout.HelpBox ("Speaking animations must have 4 frames: Rest, Small, Medium and Large.", MessageType.Info);
					
					#if !SalsaIsPresent
					EditorGUILayout.HelpBox ("The 'SalsaIsPresent' preprocessor define must be declared in the Player Settings.", MessageType.Warning);
					#endif
				}
				else if (lipSyncMode == LipSyncMode.RogoLipSync && !RogoLipSyncIntegration.IsDefinePresent ())
				{
					EditorGUILayout.HelpBox ("The 'RogoLipSyncIsPresent' preprocessor define must be declared in the Player Settings.", MessageType.Warning);
				}
			}
			CustomGUILayout.EndVertical ();

			EditorGUILayout.Space ();
			LanguagesGUI ();
			
			EditorGUILayout.Space ();

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showGameText = CustomGUILayout.ToggleHeader (showGameText, "Game text");
			if (showGameText)
			{
				translatableTextTypes = (AC_TextTypeFlags) CustomGUILayout.EnumFlagsField ("Translatable text types:", translatableTextTypes);

				speechIDRecycling = (SpeechIDRecycling) CustomGUILayout.EnumPopup ("ID number recycling:", speechIDRecycling, "AC.KickStarter.speechManager.speechIDRecycling", "The rule to use when assigning new ID numbers");
				mergeMatchingIDs = (MergeMatchingIDs) CustomGUILayout.EnumPopup ("Matching text ID merging:", mergeMatchingIDs, "AC.KickStarter.speechManager.mergeMatchingIDs", "Whether to merge separate lines with the same text, to avoid duplicates.");

				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				string numLines = (lines != null) ? lines.Count.ToString () : "0";
				EditorGUILayout.LabelField ("Gathered " + numLines + " lines of text.");
				CustomGUILayout.EndVertical ();

				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Gather text", EditorStyles.miniButtonLeft))
				{
					PopulateList ();
					return;
				}
				if (GUILayout.Button ("Reset text", EditorStyles.miniButtonRight))
				{
					ClearList ();
					return;
				}
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Import text...", EditorStyles.miniButtonLeft))
				{
					ImportGameText ();
				}
				if (GUILayout.Button ("Export text...", EditorStyles.miniButtonMid))
				{
					ExportGameText ();
				}

				if (lines.Count == 0)
				{
					GUI.enabled = false;
				}
				
				if (GUILayout.Button ("Create script sheet..", EditorStyles.miniButtonRight))
				{
					if (lines.Count > 0)
					{
						ScriptSheetWindow.Init (AdvGame.GetSceneFiles ());
					}
				}

				EditorGUILayout.EndHorizontal ();

				GUI.enabled = true;

				if (lines.Count > 0)
				{
					EditorGUILayout.Space ();

					if (Application.isPlaying && !EditorApplication.isPaused)
					{
						EditorGUILayout.HelpBox ("To aid performance, game text is hidden while the game is runninng - to show it, either stop or pause the game.", MessageType.Info);
					}
					else
					{
						ListLines (position);
					}
				}
			}
			CustomGUILayout.EndVertical ();

			if (GUI.changed)
			{
				EditorUtility.SetDirty (this);
			}
		}
		
		
		private string[] GetSceneNames ()
		{
			sceneNames.Clear ();
			sceneNames.Add ("(No scene)");
			sceneNames.Add ("(Any or no scene)");
			foreach (string sceneFile in sceneFiles)
			{
				if (!string.IsNullOrEmpty (sceneFile))
				{
					string sceneName = Path.GetFileNameWithoutExtension (sceneFile);
					sceneNames.Add (sceneName);
				}
			}
			return sceneNames.ToArray ();
		}


		private void ClearLines ()
		{
			if (lines == null)
			{
				lines = new List<SpeechLine>();
			}

			for (int i=0; i<lines.Count; i++)
			{
				if (!lines[i].alwaysRetainInSpeechManager)
				{
					lines.RemoveAt (i);
					i--;
				}
			}
		}


		private void CacheScenes ()
		{
			List<string> lineScenes = new List<string> ();
			foreach (SpeechLine line in lines)
			{
				if (!string.IsNullOrEmpty (line.scene) && !lineScenes.Contains (line.scene))
				{
					lineScenes.Add (line.scene);
				}
			}
			sceneFiles = lineScenes.ToArray ();
		}


		private Dictionary<int, SpeechLine> displayedLinesDictionary = new Dictionary<int, SpeechLine>();
		public void CacheDisplayLines ()
		{
			List<SpeechLine> sortedLines = new List<SpeechLine>();
			foreach (SpeechLine line in lines)
			{
				sortedLines.Add (new SpeechLine (line));
			}

			if (gameTextSorting == GameTextSorting.ByID)
			{
				sortedLines.Sort (delegate (SpeechLine a, SpeechLine b) {return a.lineID.CompareTo (b.lineID);});
			}
			else if (gameTextSorting == GameTextSorting.ByDescription)
			{
				sortedLines.Sort (delegate (SpeechLine a, SpeechLine b) {return a.description.CompareTo (b.description);});
			}

			CacheScenes ();
			GetSceneNames ();

			string selectedScene = string.Empty;
			if (sceneNames != null && sceneNames.Count > 0 && sceneFilter < sceneNames.Count)
			{
				selectedScene = sceneNames[sceneFilter] + ".unity";
			}

			displayedLinesDictionary.Clear ();
			foreach (SpeechLine line in sortedLines)
			{
				if ((typeFilter == TextTypeFilter.All || (int) line.textType == (int) typeFilter) && line.Matches (textFilter, filterSpeechLine))
				{
					string scenePlusExtension = Path.GetFileNameWithoutExtension (line.scene) + ".unity";
				
					if ((string.IsNullOrEmpty (line.scene) && sceneFilter == 0)
						|| sceneFilter == 1
						|| (!string.IsNullOrEmpty (line.scene) && !string.IsNullOrEmpty (selectedScene) && sceneFilter > 1 && scenePlusExtension == selectedScene))
					{
						if (tagFilter <= 0
						|| ((tagFilter-1) < speechTags.Count && line.tagID == speechTags[tagFilter-1].ID))
						{
							if (typeFilter == TextTypeFilter.Speech && referenceSpeechFiles == ReferenceSpeechFiles.ByDirectReference)
							{
								if (audioFilter == AudioFilter.OnlyWithAudio)
								{
									if (translateAudio && Languages != null && Languages.Count > 1)
									{
										for (int i=0; i<(Languages.Count-1); i++)
										{
											if (line.customTranslationAudioClips.Count > i)
											{
												if (line.customTranslationAudioClips[i] == null) continue;
											}
										}
									}
									if (line.customAudioClip == null) continue;
								}
								else if (audioFilter == AudioFilter.OnlyWithoutAudio)
								{
									bool hasAllAudio = true;

									if (translateAudio && Languages != null && Languages.Count > 1)
									{
										for (int i=0; i<(Languages.Count-1); i++)
										{
											if (line.customTranslationAudioClips.Count > i)
											{
												if (line.customTranslationAudioClips[i] == null) hasAllAudio = false;
											}
										}
									}
									if (line.customAudioClip == null) hasAllAudio = false;

									if (hasAllAudio) continue;
								}
							}

							displayedLinesDictionary.Add (line.lineID, line);
						}
					}
				}
			}
		}
		
		
		private void ListLines (Rect position)
		{
			if (sceneNames == null || sceneNames == new List<string>() || sceneFiles == null || sceneNames.Count != (sceneFiles.Length + 2))
			{
				CacheScenes ();
				GetSceneNames ();
			}
			
			typeFilter = (TextTypeFilter) CustomGUILayout.EnumPopup ("Type filter:", typeFilter);
			sceneFilter = CustomGUILayout.Popup ("Scene filter:", sceneFilter, sceneNames.ToArray ());
			
			CustomGUILayout.BeginHorizontal ();
			filterSpeechLine = (FilterSpeechLine) CustomGUILayout.EnumPopup ("Text filter", filterSpeechLine);
			textFilter = EditorGUILayout.TextField (textFilter);
			CustomGUILayout.EndHorizontal ();

			if (typeFilter == TextTypeFilter.Speech && useSpeechTags && speechTags != null && speechTags.Count > 1)
			{
				List<string> tagNames = new List<string>();
				tagNames.Add ("(Any or no tag)");
				foreach (SpeechTag speechTag in speechTags)
				{
					tagNames.Add (speechTag.label);
				}

				tagFilter = CustomGUILayout.Popup ("Tag filter:", tagFilter, tagNames.ToArray ());
			}
			else
			{
				tagFilter = 0;
			}

			if (typeFilter == TextTypeFilter.Speech && referenceSpeechFiles == ReferenceSpeechFiles.ByDirectReference)
			{
				audioFilter = (AudioFilter) CustomGUILayout.EnumPopup ("Audio filter:", audioFilter);
			}

			gameTextSorting = (GameTextSorting) CustomGUILayout.EnumPopup ("Sort by:", gameTextSorting);
			if (lastGameTextSorting != gameTextSorting)
			{
				activeLineID = -1;
			}
			lastGameTextSorting = gameTextSorting;

			EditorGUILayout.Space ();

			if (sceneNames.Count <= sceneFilter)
			{
				sceneFilter = 0;
				return;
			}

			bool doCache = GUI.changed;

			if (doCache || (displayedLinesDictionary.Count == 0 && lines.Count > 0))
			{
				CacheDisplayLines ();
			}

			foreach (KeyValuePair<int, SpeechLine> displayedLine in displayedLinesDictionary)
			{
				displayedLine.Value.ShowGUI (position.width - 30f);
			}

			if (lines.Count > 0)
			{
				if (displayedLinesDictionary.Count == 0)
				{
					EditorGUILayout.HelpBox ("No lines that match the above filters have been gathered.", MessageType.Info);
				}
				else
				{
					if (displayedLinesDictionary.Count != lines.Count)
					{
						EditorGUILayout.HelpBox ("Filtering " + displayedLinesDictionary.Count + " out of " + lines.Count + " lines.", MessageType.Info);
					}
				}
			}


			doCache = GUI.changed;

			if (doCache)
			{
				// Place back
				for (int j=0; j<lines.Count; j++)
				{
					SpeechLine displayedLine;
					if (displayedLinesDictionary.TryGetValue (lines[j].lineID, out displayedLine))
					{
						lines[j] = new SpeechLine (displayedLine);
					}
				}
			}
		}
		
		
		private void LanguagesGUI ()
		{
			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			showTranslations = CustomGUILayout.ToggleHeader (showTranslations, "Languages");
			if (showTranslations)
			{
				Upgrade ();

				if (Languages.Count == 0)
				{
					ClearLanguages ();
				}

				SyncLanguageData ();

				#if LocalizationIsPresent
				autoSyncLocaleWithLanguage = CustomGUILayout.ToggleLeft ("Auto-sync Locale with Language?", autoSyncLocaleWithLanguage, "AC.KickStarter.speechManager.autoSyncLocaleWithLanguage", "If True, then the Localization system's Locale will be automatically synced with AC's language");
				#endif

				EditorGUILayout.BeginVertical (CustomStyles.thinBox);
				EditorGUILayout.BeginHorizontal ();
				Languages[0].name = CustomGUILayout.TextField ("Original language:", Languages[0].name, "AC.KickStarter.speechManager.Languages[0].name", "The language's name");

				if (lines.Count > 0 && GUILayout.Button (string.Empty, CustomStyles.IconCog))
				{
					SideMenu (0);
				}
				EditorGUILayout.EndHorizontal ();

				Languages[0].ShowGUI (0);
				CustomGUILayout.EndVertical ();

				if (Languages.Count > 1)
				{
					for (int i=1; i< Languages.Count; i++)
					{
						EditorGUILayout.BeginVertical (CustomStyles.thinBox);
						EditorGUILayout.BeginHorizontal ();
						Languages[i].name = CustomGUILayout.TextField ("Language #" + i.ToString () + ":", Languages[i].name, "AC.KickStarter.speechManager.Languages[" + i + "].name", "The language's name");

						if (GUILayout.Button (string.Empty, CustomStyles.IconCog))
						{
							SideMenu (i);
						}
						EditorGUILayout.EndHorizontal ();

						Languages[i].ShowGUI (i);
						CustomGUILayout.EndVertical ();
					}
				}

				if (GUILayout.Button ("Create new translation"))
				{
					CreateLanguage ("New " + Languages.Count.ToString ());
				}

				if (lines.Count == 0 && Languages.Count > 1)
				{
					EditorGUILayout.HelpBox ("No text has been gathered for translations - add your scenes to the build, and click 'Gather text' below.", MessageType.Warning);
				}
			}

			if (Application.isPlaying)
			{
				EditorGUILayout.HelpBox ("Changes made will not be updated until the game is restarted.", MessageType.Info);
			}

			CustomGUILayout.EndVertical ();
		}


		private void SideMenu (int i)
		{
			GenericMenu menu = new GenericMenu ();

			sideLanguage = i;

			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Import"), false, MenuCallback, "Import translation");
				menu.AddItem (new GUIContent ("Export"), false, MenuCallback, "Export translation");
				menu.AddItem (new GUIContent ("Delete"), false, MenuCallback, "Delete translation");

				if (Languages.Count > 2)
				{
					menu.AddSeparator (string.Empty);

					if (i > 1)
					{
						menu.AddItem (new GUIContent ("Move up"), false, MenuCallback, "Move translation up");
					}

					if (i < (Languages.Count - 1))
					{
						menu.AddItem (new GUIContent ("Move down"), false, MenuCallback, "Move translation down");
					}
				}

			}

			if (lines.Count > 0)
			{
				if (i == 0)
				{
					menu.AddItem (new GUIContent ("Import"), false, MenuCallback, "Import translation");
					menu.AddItem (new GUIContent ("Export"), false, MenuCallback, "Export translation");
				}
				
				menu.AddSeparator (string.Empty);
				menu.AddItem (new GUIContent ("Create script sheet.."), false, MenuCallback, "Create script sheet");
			}

			menu.ShowAsContext ();
		}


		private void MenuCallback (object obj)
		{
			if (sideLanguage >= 0)
			{
				int i = sideLanguage;
				switch (obj.ToString ())
				{
				case "Import translation":
					ImportTranslation (i);
					break;

				case "Export translation":
					ExportWizardWindow.Init (this, GetSceneNames (), i);
					break;

				case "Delete translation":
					Undo.RecordObject (this, "Delete translation '" + Languages[i].name + "'");
					DeleteLanguage (i);
					break;

				case "Move translation down":
					Undo.RecordObject (this, "Move down translation '" + Languages[i].name + "'");
					MoveLanguageDown (i);
					break;

				case "Move translation up":
					Undo.RecordObject (this, "Move up translation '" + Languages[i].name + "'");
					MoveLanguageUp (i);
					break;

				case "Create script sheet":
					ScriptSheetWindow.Init (sceneFiles, i);
					break;
				}
			}
			
			sideLanguage = -1;
		}
		

		TransferComment correctTranslationCount;
		private void CreateLanguage (string name)
		{
			correctTranslationCount = TransferComment.NotAsked;
			int numFixes = 0;

			foreach (SpeechLine line in lines)
			{
				if (line.translationText.Count > (Languages.Count - 1))
				{
					if (correctTranslationCount == TransferComment.NotAsked)
					{
						bool canFix = EditorUtility.DisplayDialog ("Fix translations", "One or more lines have been found to have translations for languages that no longer exist.  Shall AC remove these for you?  You should back up the project beforehand.", "Yes", "No");
						correctTranslationCount = (canFix) ? TransferComment.Yes : TransferComment.No;
					}

					if (correctTranslationCount == TransferComment.Yes)
					{
						numFixes ++;
						while (line.translationText.Count > (Languages.Count - 1))
						{
							line.translationText.RemoveAt (line.translationText.Count-1);
						}
					}
				}
			}

			if (numFixes > 0)
			{
				ACDebug.Log ("Fixed " + numFixes + " translation mismatches.");
			}

			Languages.Add (new Language (name));
			
			foreach (SpeechLine line in lines)
			{
				line.translationText.Add (string.Empty);
			}
		}

		
		private void DeleteLanguage (int i)
		{
			SyncLanguageData ();
			Languages.RemoveAt (i);

			foreach (SpeechLine line in lines)
			{
				line.translationText.RemoveAt (i - 1);

				if (line.customTranslationAudioClips != null && line.customTranslationAudioClips.Count > (i - 1))
				{
					line.customTranslationAudioClips.RemoveAt (i - 1);
				}
				if (line.customTranslationLipsyncFiles != null && line.customTranslationLipsyncFiles.Count > (i - 1))
				{
					line.customTranslationLipsyncFiles.RemoveAt (i - 1);
				}
			}
		}


		private void MoveLanguageDown (int i)
		{
			SyncLanguageData ();

			Language thisLanguage = new Language (Languages[i]);
			Languages.Insert (i+2, thisLanguage);
			Languages.RemoveAt (i);

			foreach (SpeechLine line in lines)
			{
				string thisTranslationText = line.translationText[i - 1];
				line.translationText.Insert (i + 1, thisTranslationText);
				line.translationText.RemoveAt (i - 1);

				if (line.customTranslationAudioClips != null && line.customTranslationAudioClips.Count > (i - 1))
				{
					AudioClip thisAudioClip = line.customTranslationAudioClips[i - 1];
					if (line.customTranslationAudioClips.Count == i)
					{
						line.customTranslationAudioClips.Add (null);
					}
					line.customTranslationAudioClips.Insert (i + 1, thisAudioClip);
					line.customTranslationAudioClips.RemoveAt (i - 1);
				}
				if (line.customTranslationLipsyncFiles != null && line.customTranslationLipsyncFiles.Count > (i - 1))
				{
					UnityEngine.Object thisLipSyncFile = line.customTranslationLipsyncFiles[i - 1];
					if (line.customTranslationLipsyncFiles.Count == i)
					{
						line.customTranslationLipsyncFiles.Add (null);
					}
					line.customTranslationLipsyncFiles.Insert (i + 1, thisLipSyncFile);
					line.customTranslationLipsyncFiles.RemoveAt (i - 1);
				}
			}
		}


		private void MoveLanguageUp (int i)
		{
			SyncLanguageData ();

			Language thisLanguage = new Language (Languages[i]);
			Languages.Insert (i - 1, thisLanguage);
			Languages.RemoveAt (i + 1);

			foreach (SpeechLine line in lines)
			{
				string thisTranslationText = line.translationText[i - 1];
				line.translationText.Insert (i - 2, thisTranslationText);
				line.translationText.RemoveAt (i);

				if (line.customTranslationAudioClips != null && line.customTranslationAudioClips.Count > (i - 1))
				{
					AudioClip thisAudioClip = line.customTranslationAudioClips[i - 1];
					line.customTranslationAudioClips.Insert (i - 2, thisAudioClip);
					line.customTranslationAudioClips.RemoveAt (i);
				}
				if (line.customTranslationLipsyncFiles != null && line.customTranslationLipsyncFiles.Count > (i - 1))
				{
					UnityEngine.Object thisLipSyncFile = line.customTranslationLipsyncFiles[i - 1];
					line.customTranslationLipsyncFiles.Insert (i - 2, thisLipSyncFile);
					line.customTranslationLipsyncFiles.RemoveAt (i);
				}
			}
		}


		/** Removes all translations. */
		public void ClearLanguages ()
		{
			Languages.Clear ();
			
			foreach (SpeechLine line in lines)
			{
				line.translationText.Clear ();
				line.customTranslationAudioClips.Clear ();
				line.customTranslationLipsyncFiles.Clear ();
			}
			
			Languages.Add (new Language ("Original"));
		}


		/** Gets an array of all define language names */
		public string[] GetLanguageNameArray ()
		{
			Upgrade ();

			string[] labels = new string[Languages.Count];
			for (int i = 0; i < labels.Length; i++)
			{
				labels[i] = Languages[i].name;
			}
			return labels;
		}


		public void LocateLine (SpeechLine speechLine)
		{
			if (speechLine == null) return;
			if (speechLine.textType == AC_TextType.Speech)
			{
				LocateDialogueLine (speechLine);
			}
			else if (speechLine.textType == AC_TextType.DialogueOption)
			{
				LocateDialogueOption (speechLine);
			}
		}


		private void LocateDialogueOption (SpeechLine speechLine)
		{
			if (!string.IsNullOrEmpty (speechLine.scene))
			{
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					CacheScenes ();

					if (AdvGame.GetSceneFiles ().Length == 0)
					{
						ACDebug.LogWarning ("Could not find line " + speechLine.lineID + " - is the scene '" + speechLine.scene + "' added to the Build Settings?");
						return;
					}

					foreach (string sceneFile in AdvGame.GetSceneFiles ())
					{
						string sceneName = Path.GetFileNameWithoutExtension (sceneFile);
						if (sceneName != speechLine.scene && sceneFile != speechLine.scene) continue;

						UnityVersionHandler.OpenScene (sceneFile);

						ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
						foreach (ActionList list in actionLists)
						{
							if (list.source == ActionListSource.InScene)
							{
								foreach (Action action in list.actions)
								{
									if (action != null && action is ActionDialogOptionRename)
									{
										ActionDialogOptionRename actionDialogOptionRename = (ActionDialogOptionRename) action;
										if (actionDialogOptionRename.lineID == speechLine.lineID)
										{
											EditorGUIUtility.PingObject (list);
											return;
										}
									}
								}
							}
						}

						Conversation[] conversations = GameObject.FindObjectsOfType (typeof (Conversation)) as Conversation[];
						foreach (Conversation conversation in conversations)
						{
							if (conversation.interactionSource == InteractionSource.InScene)
							{
								if (conversation.options != null)
								{
									foreach (ButtonDialog option in conversation.options)
									{
										if (option != null && option.lineID == speechLine.lineID)
										{
											EditorGUIUtility.PingObject (conversation);
											return;
										}
									}
								}
							}
						}
					}

					ACDebug.LogWarning ("Could not find line " + speechLine.lineID + " - is the scene added to the Build Settings?");
				}
			}
			else
			{
				// Asset file

				CollectAllActionListAssets ();
				foreach (ActionListAsset actionListAsset in allActionListAssets)
				{
					foreach (Action action in actionListAsset.actions)
					{
						if (action != null && action is ActionDialogOptionRename)
						{
							ActionDialogOptionRename actionDialogOptionRename = (ActionDialogOptionRename) action;
							if (actionDialogOptionRename.lineID == speechLine.lineID)
							{
								EditorGUIUtility.PingObject (actionListAsset);
								return;
							}
						}
					}
				}

				ACDebug.Log ("Could not find line " + speechLine.lineID + " - is ActionList asset still present?");
			}
		}


		private void LocateDialogueLine (SpeechLine speechLine)
		{
			if (speechLine.scene != string.Empty)
			{
				// In a scene

				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					if (AdvGame.GetSceneFiles ().Length == 0)
					{
						ACDebug.LogWarning ("Could not find line " + speechLine.lineID + " - is the scene '" + speechLine.scene + "' added to the Build Settings?");
						return;
					}

					foreach (string sceneFile in AdvGame.GetSceneFiles ())
					{
						string sceneName = System.IO.Path.GetFileNameWithoutExtension (sceneFile);
						if (sceneName != speechLine.scene && sceneFile != speechLine.scene) continue;

						UnityVersionHandler.OpenScene (sceneFile);

						ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
						foreach (ActionList list in actionLists)
						{
							if (list.source == ActionListSource.InScene)
							{
								foreach (Action action in list.actions)
								{
									if (action != null)
									{
										if (action is ActionSpeech)
										{
											ActionSpeech actionSpeech = (ActionSpeech) action;
											if (actionSpeech.lineID == speechLine.lineID)
											{
												EditorGUIUtility.PingObject (list);
												return;
											}

											if (separateLines)
											{
												foreach (int lineID in actionSpeech.multiLineIDs)
												{
													if (lineID == speechLine.lineID)
													{
														EditorGUIUtility.PingObject (list);
														return;
													}
												}
											}
										}
									}
								}
							}
						}
					}

					ACDebug.LogWarning ("Could not find line " + speechLine.lineID + " - is the scene '" + speechLine.scene + "' added to the Build Settings?");
				}
			}
			else
			{
				// Asset file

				CollectAllActionListAssets ();
				foreach (ActionListAsset actionListAsset in allActionListAssets)
				{
					foreach (Action action in actionListAsset.actions)
					{
						if (action != null)
						{
							if (action is ActionSpeech)
							{
								ActionSpeech actionSpeech = (ActionSpeech) action;
								if (actionSpeech.lineID == speechLine.lineID)
								{
									EditorGUIUtility.PingObject (actionListAsset);
									return;
								}
							}

#if !ACIgnoreTimeline
							if (action is ActionTimeline)
							{
								ActionTimeline actionTimeline = (ActionTimeline) action;
								if (actionTimeline.director != null)
								{
									if (LocateLineInTimeline (actionTimeline.director, speechLine.lineID))
									{
										return;
									}
								}
							}
#endif
						}
					}
				}

#if !ACIgnoreTimeline
				// Still can't find, so need to search scenes - Timeline speech tracks have no scene
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					foreach (string sceneFile in AdvGame.GetSceneFiles ())
					{
						UnityVersionHandler.OpenScene (sceneFile);

						ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
						foreach (ActionList list in actionLists)
						{
							if (list.source == ActionListSource.InScene)
							{
								foreach (Action action in list.actions)
								{
									if (action != null)
									{
										if (action is ActionTimeline)
										{
											ActionTimeline actionTimeline = (ActionTimeline) action;
											if (actionTimeline.director != null)
											{
												if (LocateLineInTimeline (actionTimeline.director, speechLine.lineID))
												{
													return;
												}
											}
										}
									}
								}
							}
						}

						PlayableDirector[] directors = GameObject.FindObjectsOfType (typeof (PlayableDirector)) as PlayableDirector[];
						foreach (PlayableDirector director in directors)
						{
							if (LocateLineInTimeline (director, speechLine.lineID))
							{
								return;
							}
						}
					}
				}
#endif

				ACDebug.LogWarning ("Could not find line " + speechLine.lineID + " - is the asset it's a part of still present?");
			}
		}


		private int[] GetIDArray ()
		{
			List<int> idArray = new List<int>();
			
			foreach (SpeechLine line in lines)
			{
				idArray.Add (line.lineID);
			}

			if (tempLines != null)
			{
				foreach (SpeechLine tempLine in tempLines)
				{
					idArray.Add (tempLine.lineID);
				}
			}
			
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private int GetEmptyID ()
		{
			int[] idArray = GetIDArray ();

			if (idArray != null && idArray.Length > 0)
			{
				int lastEntry = idArray [idArray.Length-1];

				if (speechIDRecycling == SpeechIDRecycling.NeverRecycle)
				{
					maxID = Mathf.Max (maxID, lastEntry);
				}
				else if (speechIDRecycling == SpeechIDRecycling.RecycleHighestOnly)
				{
					maxID = lastEntry;
				}
				else if (speechIDRecycling == SpeechIDRecycling.AlwaysRecycle)
				{
					maxID = lastEntry;

					for (int i=1; i<idArray.Length; i++)
					{
						int lastID = idArray[i-1];
						int thisID = idArray[i];

						if (thisID > (lastID + 1))
						{
							maxID = lastID;
							break;
						}
					}
				}
			}

			return maxID + 1;
		}


		private void ShowGatherTextProgress (float progress)
		{
			EditorUtility.DisplayProgressBar ("Gathering text", "Please wait while your project is searched for game text.", progress);
		}


		private void ShowClearTestProgress (float progress)
		{
			EditorUtility.DisplayProgressBar ("Resetting text", "Please wait while your project's game text is reset.", progress);
		}


		public void PopulateList (bool currentSceneOnly = false)
		{
			transferComment = TransferComment.NotAsked;
			string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();

			string dialogMessage = (currentSceneOnly) ?
									 "AC will now go through your scene, and collect all game text so that it can be translated/voiced.\n\nPreviously-gathered lines that no longer exist will not be removed.\n\nIt is recommended to back up your project beforehand." :
									 "AC will now go through your game, and collect all game text so that it can be translated/voiced.\n\nIt is recommended to back up your project beforehand.";
			string dialogTitle = (currentSceneOnly) ?
									"Gather scene text" :
									"Gather game text";
			bool canProceed = EditorUtility.DisplayDialog (dialogTitle, dialogMessage, "OK", "Cancel");
			if (!canProceed) return;

			if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
			{
				Undo.RecordObject (this, "Update speech list");
				
				int originalLineCount = (lines != null) ? lines.Count : 0;
				addedLines.Clear ();

				// Store the lines temporarily, so that we can update the translations afterwards
				BackupTranslations ();

				if (currentSceneOnly)
				{
					GetLinesInScene (string.Empty, true);
				}
				else
				{
					ClearLines ();
					checkedAssets.Clear ();

					#if !ACIgnoreTimeline
					allTimelineAssets.Clear ();
					#endif

					sceneFiles = AdvGame.GetSceneFiles ();
					GetSceneNames ();
					
					// First look for lines that already have an assigned lineID
					for (int i=0; i<sceneFiles.Length; i++)
					{
						ShowGatherTextProgress (i / (float) sceneFiles.Length / 4f);
						GetLinesInScene (sceneFiles[i], false);
					}

					GetLinesFromSettings (false);
					GetLinesFromInventory (false);
					GetLinesFromVariables (false);
					GetLinesFromCursors (false);
					GetLinesFromMenus (false);

					CollectAllActionListAssets ();
					for (int i=0; i<allActionListAssets.Count; i++)
					{
						ShowGatherTextProgress (0.25f + (i / (float) allActionListAssets.Count / 4f));
						ProcessActionListAsset (allActionListAssets[i], false);
					}

					#if !ACIgnoreTimeline
					GetLinesFromTimelines (false);
					#endif

					checkedAssets.Clear ();

					// Now look for new lines, which don't have a unique lineID
					for (int i=0; i<sceneFiles.Length; i++)
					{
						ShowGatherTextProgress (0.5f + (i / (float) sceneFiles.Length / 4f));
						GetLinesInScene (sceneFiles[i], true);
					}

					GetLinesFromSettings (true);
					GetLinesFromInventory (true);
					GetLinesFromVariables (true);
					GetLinesFromCursors (true);
					GetLinesFromMenus (true);

					for (int i=0; i<allActionListAssets.Count; i++)
					{
						ShowGatherTextProgress (0.75f + (i / (float) allActionListAssets.Count / 4f));
						ProcessActionListAsset (allActionListAssets[i], true);
					}

					#if !ACIgnoreTimeline
					GetLinesFromTimelines (true);
					#endif

					checkedAssets.Clear ();

					GetEmptyID ();

					allActionListAssets.Clear ();
					#if !ACIgnoreTimeline
					allTimelineAssets.Clear ();
					#endif
					UnityVersionHandler.OpenScene (originalScene);
				}
	
				int newLineCount = (lines != null) ? lines.Count : 0;
				int numRemoved = Mathf.Max (originalLineCount + addedLines.Count - newLineCount, 0);

				tempLines = null;
				CacheDisplayLines ();

				foreach (SpeechLine addedLine in addedLines)
				{
					ACDebug.Log ("Added line ID: "+ addedLine.lineID + ", Type: " + addedLine.textType + ", Text: '"  + addedLine.text + "'");
				}

				EditorUtility.ClearProgressBar ();
				EditorUtility.DisplayDialog ("Gather game text", "Process complete.\n\n" + newLineCount + " total entries\n" + addedLines.Count + " entries added\n" + numRemoved + " entries removed", "OK");
			}

			if (sceneFiles != null && sceneFiles.Length > 0)
			{
				Array.Sort (sceneFiles);
			}
		}


		private bool UpdateTranslatable (ITranslatable translatable, SpeechLine speechLine, string updatedText)
		{
			if (speechLine == null) return false;
			if (string.IsNullOrEmpty (updatedText)) return false;

			int[] newIDs = new int[translatable.GetNumTranslatables ()];
			for (int i=0; i<newIDs.Length; i++)
			{
				if (translatable.GetTranslationID (i) == speechLine.lineID &&
					translatable.CanTranslate (i) &&
					IsTextTypeTranslatable (translatable.GetTranslationType (i)))
				{
					translatable.UpdateTranslatableString (i, updatedText);
					if (speechLine.text != translatable.GetTranslatableString (i))
					{
						speechLine.text = translatable.GetTranslatableString (i);
						return true;
					}
					return false;
				}
			}
			return false;
		}

		
		private void ExtractTranslatable (ITranslatable translatable, bool onlySeekNew, bool isInScene, bool isMonoBehaviour, string comment = "", int tagID = -1, string actionListName = "")
		{
			if (translatable == null) return;

			if (!string.IsNullOrEmpty (comment))
			{
				PromptCommentTransfer ();

				if (transferComment != TransferComment.Yes)
				{
					comment = string.Empty;
				}
			}

			string _scene = (isInScene) ? UnityVersionHandler.GetCurrentSceneName () : string.Empty;
			int numTranslations = Languages.Count - 1;

			int[] newIDs = new int[translatable.GetNumTranslatables ()];
			for (int i=0; i<newIDs.Length; i++)
			{
				if (translatable.CanTranslate (i) && IsTextTypeTranslatable (translatable.GetTranslationType (i)))
				{
					string textToTranslate = translatable.GetTranslatableString (i);
					if (onlySeekNew != translatable.HasExistingTranslation (i))
					{
						if (onlySeekNew)
						{
							// Assign a new ID on creation
							SpeechLine newLine = new SpeechLine (GetEmptyID (), _scene, translatable.GetOwner (i), textToTranslate, numTranslations, translatable.GetTranslationType (i), translatable.OwnerIsPlayer (i));
							
							addedLines.Add (newLine);
							
							if (tagID >= 0)
							{
								newLine.tagID = tagID;
							}
							newLine.TransferActionComment (comment, actionListName);

							int gatheredLineID = GetMatchingID (newLine);
							if (gatheredLineID > -1)
							{
								newLine.lineID = gatheredLineID;
							}
							else
							{
								int newLineID = GetSmartID (newLine);
								newLine.lineID = newLineID;
								lines.Add (newLine);
							}
							newIDs[i] = newLine.lineID;
						}
						else
						{
							// Already has an ID, so don't replace
							SpeechLine existingLine = new SpeechLine (translatable.GetTranslationID (i), _scene, translatable.GetOwner (i), textToTranslate, numTranslations, translatable.GetTranslationType (i), translatable.OwnerIsPlayer (i));
							if (tagID >= 0)
							{
								existingLine.tagID = tagID;
							}
							existingLine.TransferActionComment (comment, actionListName);

							int gatheredLineID = GetMatchingID (existingLine);
							if (gatheredLineID > -1)
							{
								existingLine.lineID = gatheredLineID;
							}
							else
							{
								int existingLineID = GetSmartID (existingLine);
								existingLine.lineID = existingLineID;
								lines.Add (existingLine);
							}

							// Restore backup
							foreach (SpeechLine tempLine in tempLines)
							{
								if (tempLine.lineID == existingLine.lineID)
								{
									existingLine.RestoreBackup (tempLine);
									break;
								}
							}

							newIDs[i] = existingLine.lineID;
						}
					}
					else
					{
						newIDs[i] = translatable.GetTranslationID (i);
					}
				}
				else
				{
					newIDs[i] = -1;
				}
			}

			UnityVersionHandler.AssignIDsToTranslatable (translatable, newIDs, isInScene, isMonoBehaviour);
		}


		private int GetMatchingID (SpeechLine speechLine)
		{
			// Return >= 0 only if we want to change the ID to a matching one
		
			switch (mergeMatchingIDs)
			{
				case MergeMatchingIDs.NoMerging:
					return -1;

				case MergeMatchingIDs.MergeSpeechOnly:
					if (speechLine.textType == AC_TextType.Speech)
					{
						foreach (SpeechLine gatheredLine in lines)
						{
							if (gatheredLine.IsMatch (speechLine, true))
							{
								return gatheredLine.lineID;
							}
						}
					}
					break;

				case MergeMatchingIDs.MergeIfTypesMatch:
					if (speechLine.textType == AC_TextType.Speech)
					{
						foreach (SpeechLine gatheredLine in lines)
						{
							if (gatheredLine.IsMatch (speechLine, true))
							{
								return gatheredLine.lineID;
							}
						}
					}
					else
					{
						foreach (SpeechLine gatheredLine in lines)
						{
							if (gatheredLine.textType == speechLine.textType && gatheredLine.text == speechLine.text)
							{
								return gatheredLine.lineID;
							}
						}
					}
					break;

				case MergeMatchingIDs.AlwaysMerge:
					if (speechLine.textType == AC_TextType.Speech)
					{
						foreach (SpeechLine gatheredLine in lines)
						{
							if (gatheredLine.IsMatch (speechLine, true))
							{
								return gatheredLine.lineID;
							}
						}
					}
					else
					{
						foreach (SpeechLine gatheredLine in lines)
						{
							if (gatheredLine.text == speechLine.text)
							{
								if (gatheredLine.textType != speechLine.textType)
								{
									Debug.Log ("The text '" + speechLine.text + "' of type " + speechLine.textType + " matches a line of type " + gatheredLine.textType + ". The two will be merged into line " + gatheredLine.lineID + " of type " + gatheredLine.textType);
								}
								return gatheredLine.lineID;
							}
						}
					}
					break;

				default:
					return -1;
			}

			return -1;
		}


		private int GetSmartID (SpeechLine speechLine)
		{
			if (DoLinesMatchID (speechLine.lineID))
			{
				// Same ID, different text, so re-assign ID
				int lineID = GetEmptyID ();
				ACDebug.LogWarning ("Conflicting ID number (" + speechLine.lineID + ") found with '"  + speechLine.text + "'. Changing to " + lineID + ".");
				return lineID;
			}
			return speechLine.lineID;
		}


		private void ClearTranslatable (ITranslatable translatable, bool isInScene, bool isMonoBehaviour)
		{
			int[] newIDs = new int[translatable.GetNumTranslatables ()];
			for (int i=0; i<newIDs.Length; i++)
			{
				newIDs[i] = -1;
			}

			UnityVersionHandler.AssignIDsToTranslatable (translatable, newIDs, isInScene, isMonoBehaviour);
		}


		private void PromptCommentTransfer ()
		{
			if (transferComment == TransferComment.NotAsked)
			{
				bool canTransfer = EditorUtility.DisplayDialog ("Transfer speech comments", "One or more translatable Actions have been found with comments embedded.\r\nWould you like to transfer them all to the Speech Manager as line descriptions?\r\nIf not, line descriptions will be set to the name of the ActionList they're placed in.", "Yes", "No");
				transferComment = (canTransfer) ? TransferComment.Yes : TransferComment.No;
			}
		}


		private bool DoLinesMatchID (int newLineID)
		{
			if (lines == null || lines.Count == 0)
			{
				return false;
			}
			
			foreach (SpeechLine line in lines)
			{
				if (line.lineID == newLineID)
				{
					return true;
				}
			}

			return false;
		}


		private void GetLinesFromSettings (bool onlySeekNew)
		{
			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			if (settingsManager)
			{
				ExtractTranslatable (settingsManager.saveLabels, onlySeekNew, false, false);

				if (settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					foreach (PlayerPrefab playerPrefab in settingsManager.players)
					{
						if (playerPrefab != null && playerPrefab.playerOb)
						{
							ExtractPrefab (playerPrefab.playerOb.gameObject, onlySeekNew);
						}
					}
				}
				else if (settingsManager.player)
				{
					ExtractPrefab (settingsManager.player.gameObject, onlySeekNew);
				}
			}
		}


		private void ExtractPrefab (GameObject prefab, bool onlySeekNew)
		{
			ITranslatable[] prefabTranslatables = prefab.GetComponentsInChildren<ITranslatable> ();
			foreach (ITranslatable prefabTranslatable in prefabTranslatables)
			{
				ExtractTranslatable (prefabTranslatable, onlySeekNew, false, true);
			}
		}
		
		
		private void GetLinesFromInventory (bool onlySeekNew)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			
			if (inventoryManager)
			{
				// Inventory properties
				for (int i=0; i<inventoryManager.invVars.Count; i++)
				{
					ExtractTranslatable (inventoryManager.invVars[i], onlySeekNew, false, false);
				}

				// Item-specific events
				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem item in inventoryManager.items)
					{
						// Label / properties
						ExtractTranslatable (item, onlySeekNew, false, false);

						// Prefixes
						if (item.overrideUseSyntax)
						{
							ExtractTranslatable (item.hotspotPrefix1, onlySeekNew, false, false);
							ExtractTranslatable (item.hotspotPrefix2, onlySeekNew, false, false);
						}
					}
				}

				// Documents
				if (inventoryManager.documents != null && inventoryManager.documents.Count > 0)
				{
					for (int i=0; i<inventoryManager.documents.Count; i++)
					{
						ExtractTranslatable (inventoryManager.documents[i], onlySeekNew, false, false);
					}
				}

				// Objectives
				if (inventoryManager.objectives != null && inventoryManager.objectives.Count > 0)
				{
					foreach (Objective objective in inventoryManager.objectives)
					{
						ExtractTranslatable (objective, onlySeekNew, false, false);

						foreach (ObjectiveState state in objective.states)
						{
							ExtractTranslatable (state, onlySeekNew, false, false);
						}
					}
				}

				EditorUtility.SetDirty (inventoryManager);
			}
		}


		private void GetLinesFromVariables (bool onlySeekNew)
		{
			VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
			if (variablesManager != null)
			{
				foreach (GVar globalVariable in variablesManager.vars)
				{
					ExtractTranslatable (globalVariable, onlySeekNew, false, false);
				}
				foreach (PopUpLabelData data in variablesManager.popUpLabelData)
				{
					ExtractTranslatable (data, onlySeekNew, false, false);
				}
				EditorUtility.SetDirty (variablesManager);
			}
		}


		private void GetLinesFromMenus (bool onlySeekNew)
		{
			MenuManager menuManager = AdvGame.GetReferences ().menuManager;
			
			if (menuManager)
			{
				// Gather elements
				if (menuManager.menus.Count > 0)
				{
					foreach (AC.Menu menu in menuManager.menus)
					{
						foreach (MenuElement element in menu.elements)
						{
							if (element is ITranslatable)
							{
								ExtractTranslatable (element as ITranslatable, onlySeekNew, false, false);
							}
						}
					}
				}
				
				EditorUtility.SetDirty (menuManager);
			}
		}
		
		
		private void GetLinesFromCursors (bool onlySeekNew)
		{
			CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
			
			if (cursorManager)
			{
				// Prefixes
				ExtractTranslatable (cursorManager.hotspotPrefix1, onlySeekNew, false, false);
				ExtractTranslatable (cursorManager.hotspotPrefix2, onlySeekNew, false, false);
				ExtractTranslatable (cursorManager.hotspotPrefix3, onlySeekNew, false, false);
				ExtractTranslatable (cursorManager.hotspotPrefix4, onlySeekNew, false, false);
				ExtractTranslatable (cursorManager.walkPrefix, onlySeekNew, false, false);

				// Gather icons
				if (cursorManager.cursorIcons.Count > 0)
				{
					foreach (CursorIcon icon in cursorManager.cursorIcons)
					{
						ExtractTranslatable (icon, onlySeekNew, false, false);
					}
				}
				
				EditorUtility.SetDirty (cursorManager);
			}
		}
		
		
		private void GetLinesInScene (string sceneFile, bool onlySeekNew)
		{
			if (!string.IsNullOrEmpty (sceneFile))
			{
				UnityVersionHandler.OpenScene (sceneFile);
			}

			// Actions
			ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
			foreach (ActionList list in actionLists)
			{
				if (list.source == ActionListSource.InScene)
				{
					ProcessActionList (list, onlySeekNew);
				}
			}

			// Translatables
			MonoBehaviour[] sceneObjects = FindObjectsOfType<MonoBehaviour>();
			for (int i=0; i<sceneObjects.Length; i++)
			{
				MonoBehaviour currentObj = sceneObjects[i];
				ITranslatable currentComponent = currentObj as ITranslatable;
				if (currentComponent != null)
				{
					ExtractTranslatable (currentComponent, onlySeekNew, true, true, string.Empty, -1, currentObj.gameObject.name);
				}
			}
			
			// Timelines
#if !ACIgnoreTimeline
			PlayableDirector[] sceneDirectors = FindObjectsOfType<PlayableDirector>();
			for (int i=0; i<sceneDirectors.Length; i++)
			{
				PlayableDirector currentDirector = sceneDirectors[i];
				if (currentDirector.playableAsset != null && currentDirector.playableAsset is TimelineAsset)
				{
					SmartAddAsset (currentDirector.playableAsset as TimelineAsset);
				}
			}
#endif

			// Save the scene
			UnityVersionHandler.SaveScene ();
			EditorUtility.SetDirty (this);
		}

		
		private void BackupTranslations ()
		{
			tempLines = new List<SpeechLine>();
			foreach (SpeechLine line in lines)
			{
				if (IsTextTypeTranslatable (line.textType))
				{
					tempLines.Add (line);
				}
			}
		}


		private void ImportTranslation (int i)
		{
			if (i < 0) return;
			string langType = (i == 0) ? "original text" : "translation";

			bool canProceed = EditorUtility.DisplayDialog ("Import " + langType, "AC will now prompt you for a CSV file to import. It is strongly recommended to back up your project beforehand.", "OK", "Cancel");
			if (!canProceed) return;

			string fileName = EditorUtility.OpenFilePanel ("Import all game text data", "Assets", "csv,xml");
			if (string.IsNullOrEmpty (fileName))
			{
				return;
			}
			
			if (File.Exists (fileName))
			{
				string csvText = Serializer.LoadFile (fileName);
				string [,] csvOutput = CSVReader.SplitCsvGrid (csvText);

				ImportWizardWindow.Init (this, csvOutput, i);
			}

			if (File.Exists (fileName))
			{
				string fileText = Serializer.LoadFile (fileName);
				if (!string.IsNullOrEmpty (fileText))
				{
					if (fileName.EndsWith (".xml"))
					{
						string[,] xmlOutput = SMLReader.SplitXMLGrid (fileText);
						ImportWizardWindow.Init (this, xmlOutput, -1, false);
					}
					else
					{
						string[,] csvOutput = CSVReader.SplitCsvGrid (fileText);
						ImportWizardWindow.Init (this, csvOutput, -1, true);
					}
				}
			}
		}


		public void GatherAllAssets ()
		{
			allActionListAssets = new List<ActionListAsset>();
			CollectAllActionListAssets ();

			#if !ACIgnoreTimeline
			allTimelineAssets = new List<TimelineAsset>();

			sceneFiles = AdvGame.GetSceneFiles ();
			GetSceneNames ();
			
			// First look for lines that already have an assigned lineID
			for (int i=0; i<sceneFiles.Length; i++)
			{
				if (UnityVersionHandler.OpenScene (sceneFiles[i]))
				{
					// Timelines
					PlayableDirector[] sceneDirectors = FindObjectsOfType<PlayableDirector>();
					foreach (PlayableDirector sceneDirector in sceneDirectors)
					{
						PlayableDirector currentDirector = sceneDirectors[i];
						if (currentDirector.playableAsset != null && currentDirector.playableAsset is TimelineAsset)
						{
							SmartAddAsset (currentDirector.playableAsset as TimelineAsset);
						}
					}
				}
			}
			#endif
		}


		public void ClearAllAssets ()
		{
			allActionListAssets.Clear ();
			#if !ACIgnoreTimeline
			allTimelineAssets.Clear ();
			#endif
		}



		public bool UpdateOriginalText (SpeechLine speechLine, string updatedText)
		{
			if (string.IsNullOrEmpty (updatedText)) return false;
			if (speechLine == null) return false;

			bool succesful = false;
			if (!string.IsNullOrEmpty (speechLine.scene))
			{
				// Scene line

				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					CacheScenes ();

					if (AdvGame.GetSceneFiles ().Length == 0)
					{
						ACDebug.LogWarning ("Could not find line " + speechLine.lineID + " - is the scene '" + speechLine.scene + "' added to the Build Settings?");
						return false;
					}

					foreach (string sceneFile in AdvGame.GetSceneFiles ())
					{
						string sceneName = System.IO.Path.GetFileNameWithoutExtension (sceneFile);
						if (sceneName != speechLine.scene && sceneFile != speechLine.scene) continue;

						UnityVersionHandler.OpenScene (sceneFile);

						// Actions
						ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
						foreach (ActionList list in actionLists)
						{
							if (list.source == ActionListSource.InScene)
							{
								foreach (Action action in list.actions)
								{
									if (action == null)
									{
										continue;
									}

									if (action is ITranslatable)
									{
										if (UpdateTranslatable (action as ITranslatable, speechLine, updatedText))
										{
											succesful = true;
											EditorUtility.SetDirty (list);
										}
									}
								}
							}
						}

						// Translatables
						MonoBehaviour[] sceneObjects = FindObjectsOfType<MonoBehaviour>();
						for (int i=0; i<sceneObjects.Length; i++)
						{
							MonoBehaviour currentObj = sceneObjects[i];
							ITranslatable currentComponent = currentObj as ITranslatable;
							if (currentComponent != null)
							{
								if (UpdateTranslatable (currentComponent, speechLine, updatedText))
								{
									succesful = true;
									EditorUtility.SetDirty (currentObj);
								}
							}
						}
						break;
					}
				}
				else
				{
					ACDebug.LogWarning ("Cannot update translation ID " + speechLine.lineID + " - error opening scene '" + speechLine.scene + "'");
				}

				if (succesful)
				{
					EditorUtility.SetDirty (this);
					UnityVersionHandler.SaveScene ();
				}
				return succesful;
			}

			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			if (settingsManager)
			{
				if (settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					foreach (PlayerPrefab playerPrefab in settingsManager.players)
					{
						if (playerPrefab != null && playerPrefab.playerOb != null)
						{
							if (UpdateTranslatable (playerPrefab.playerOb, speechLine, updatedText))
							{
								succesful = true;
								EditorUtility.SetDirty (settingsManager);
							}
						}
					}
				}
				else if (settingsManager.player)
				{
					if (UpdateTranslatable (settingsManager.player, speechLine, updatedText))
					{
						succesful = true;
						EditorUtility.SetDirty (settingsManager);
					}
				}
			}

			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager)
			{
				bool updated = false;

				// Inventory properties
				for (int i=0; i<inventoryManager.invVars.Count; i++)
				{
					if (UpdateTranslatable (inventoryManager.invVars[i], speechLine, updatedText))
					{
						updated = true;
					}
				}

				// Item-specific events
				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem item in inventoryManager.items)
					{
						// Label / properties
						if (UpdateTranslatable (item, speechLine, updatedText))
						{
							updated = true;
						}

						// Prefixes
						if (item.overrideUseSyntax)
						{
							if (UpdateTranslatable (item.hotspotPrefix1, speechLine, updatedText))
							{
								updated = true;
							}
							if (UpdateTranslatable (item.hotspotPrefix2, speechLine, updatedText))
							{
								updated = true;
							}
						}
					}
				}

				// Documents
				if (inventoryManager.documents != null && inventoryManager.documents.Count > 0)
				{
					for (int i=0; i<inventoryManager.documents.Count; i++)
					{
						if (UpdateTranslatable (inventoryManager.documents[i], speechLine, updatedText))
						{
							updated = true;
						}
					}
				}

				// Objectives
				if (inventoryManager.objectives != null && inventoryManager.objectives.Count > 0)
				{
					foreach (Objective objective in inventoryManager.objectives)
					{
						if (UpdateTranslatable (objective, speechLine, updatedText))
						{
							updated = true;
						}

						foreach (ObjectiveState state in objective.states)
						{
							if (UpdateTranslatable (state, speechLine, updatedText))
							{
								updated = true;
							}
						}
					}
				}

				if (updated)
				{
					succesful = true;
					EditorUtility.SetDirty (inventoryManager);
				}
			}

			VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
			if (variablesManager != null)
			{
				bool updated = false;
				foreach (GVar globalVariable in variablesManager.vars)
				{
					if (UpdateTranslatable (globalVariable, speechLine, updatedText))
					{
						updated = true;
					}
				}
				foreach (PopUpLabelData data in variablesManager.popUpLabelData)
				{
					if (UpdateTranslatable (data, speechLine, updatedText))
					{
						updated = true;
					}
				}

				if (updated)
				{
					succesful = true;
					EditorUtility.SetDirty (variablesManager);
				}
			}

			CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
			if (cursorManager)
			{
				bool updated = false;

				// Prefixes
				if (UpdateTranslatable (cursorManager.hotspotPrefix1, speechLine, updatedText))
				{
					updated = true;
				}
				if (UpdateTranslatable (cursorManager.hotspotPrefix2, speechLine, updatedText))
				{
					updated = true;
				}
				if (UpdateTranslatable (cursorManager.hotspotPrefix3, speechLine, updatedText))
				{
					updated = true;
				}
				if (UpdateTranslatable (cursorManager.hotspotPrefix4, speechLine, updatedText))
				{
					updated = true;
				}
				if (UpdateTranslatable (cursorManager.walkPrefix, speechLine, updatedText))
				{
					updated = true;
				}

				// Gather icons
				if (cursorManager.cursorIcons.Count > 0)
				{
					foreach (CursorIcon icon in cursorManager.cursorIcons)
					{
						if (UpdateTranslatable (icon, speechLine, updatedText))
						{
							updated = true;
						}
					}
				}

				if (updated)
				{
					succesful = true;
					EditorUtility.SetDirty (cursorManager);
				}
			}

			MenuManager menuManager = AdvGame.GetReferences ().menuManager;
			if (menuManager)
			{
				bool updated = false;

				if (menuManager.menus.Count > 0)
				{
					foreach (AC.Menu menu in menuManager.menus)
					{
						foreach (MenuElement element in menu.elements)
						{
							if (element is ITranslatable)
							{
								if (UpdateTranslatable (element as ITranslatable, speechLine, updatedText))
								{
									updated = true;
								}
							}
						}
					}
				}

				if (updated)
				{
					succesful = true;
					EditorUtility.SetDirty (menuManager);
				}
			}

			foreach (ActionListAsset actionListAsset in allActionListAssets)
			{
				foreach (Action action in actionListAsset.actions)
				{
					if (action == null)
					{
						continue;
					}

					if (action is ITranslatable)
					{
						if (UpdateTranslatable (action as ITranslatable, speechLine, updatedText))
						{
							succesful = true;
							EditorUtility.SetDirty (actionListAsset);
						}
					}
				}
			}

			#if !ACIgnoreTimeline
			foreach (TimelineAsset timelineAsset in allTimelineAssets)
			{
				IEnumerable<TrackAsset> trackAssets = timelineAsset.GetOutputTracks ();
				foreach (TrackAsset trackAsset in trackAssets)
				{
					if (trackAsset is ITranslatable)
					{
						if (UpdateTranslatable (trackAsset as ITranslatable, speechLine, updatedText))
						{
							succesful = true;
							EditorUtility.SetDirty (timelineAsset);
						}
					}
				}
			}
			#endif

			if (succesful)
			{
				EditorUtility.SetDirty (this);
			}
			return succesful;
		}


		private void UpdateTranslation (int i, int _lineID, string translationText)
		{
			foreach (SpeechLine line in lines)
			{
				if (line.lineID == _lineID)
				{
					line.translationText [i-1] = translationText;
					return;
				}
			}
		}


		private void ExportGameText ()
		{
			ExportWizardWindow.Init (this, GetSceneNames ());
		}


		private void ImportGameText ()
		{
			bool canProceed = EditorUtility.DisplayDialog ("Import game text", "AC will now prompt you for a CSV file to import. It is recommended to back up your project beforehand.", "OK", "Cancel");
			if (!canProceed) return;

			string fileName = EditorUtility.OpenFilePanel ("Import all game text data", "Assets", "csv,xml");
			if (fileName.Length == 0)
			{
				return;
			}
			
			if (File.Exists (fileName))
			{
				string fileText = Serializer.LoadFile (fileName);
				if (!string.IsNullOrEmpty (fileText))
				{
					if (fileName.EndsWith (".xml"))
					{
						string[,] xmlOutput = SMLReader.SplitXMLGrid (fileText);
						ImportWizardWindow.Init (this, xmlOutput, -1, false);
					}
					else
					{
						string[,] csvOutput = CSVReader.SplitCsvGrid (fileText);
						ImportWizardWindow.Init (this, csvOutput, -1, true);
					}
				}
			}
		}


		private void ClearList ()
		{
			if (EditorUtility.DisplayDialog ("Reset game text", "This will completely reset the IDs of every text line in your game, removing any supplied translations and invalidating speech audio filenames. Continue?", "OK", "Cancel"))
			{
				string originalScene = UnityVersionHandler.GetCurrentSceneFilepath ();
				
				if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				{
					ClearLines ();
					checkedAssets.Clear ();
					
					sceneFiles = AdvGame.GetSceneFiles ();
					GetSceneNames ();
					
					// First look for lines that already have an assigned lineID
					for (int i=0; i<sceneFiles.Length; i++)
					{
						ShowClearTestProgress (i / (float) sceneFiles.Length / 2f);
						ClearLinesInScene (sceneFiles[i]);
					}

					CollectAllActionListAssets ();
					for (int i=0; i<allActionListAssets.Count; i++)
					{
						ShowClearTestProgress (0.5f + (i / (float) sceneFiles.Length / 2f));
						ClearLinesFromActionListAsset (allActionListAssets[i]);
					}

					ClearLinesFromSettings ();
					ClearLinesFromInventory ();
					ClearLinesFromVariables ();
					ClearLinesFromCursors ();
					ClearLinesFromMenus ();
					
					checkedAssets.Clear ();

					if (string.IsNullOrEmpty (originalScene))
					{
						UnityVersionHandler.NewScene ();
					}
					else
					{
						UnityVersionHandler.OpenScene (originalScene);
					}

					allActionListAssets.Clear ();
					maxID = -1;

					CacheDisplayLines ();

					EditorUtility.ClearProgressBar ();
					EditorUtility.DisplayDialog ("Reset game text", "Process complete.", "OK");
				}
			}
		}
		
		
		private void ClearLinesInScene (string sceneFile)
		{
			UnityVersionHandler.OpenScene (sceneFile);
			
			// Speech lines and journal entries
			ActionList[] actionLists = GameObject.FindObjectsOfType (typeof (ActionList)) as ActionList[];
			foreach (ActionList list in actionLists)
			{
				if (list.source == ActionListSource.InScene)
				{
					ClearLinesFromActionList (list);
				}
			}

			MonoBehaviour[] sceneObjects = FindObjectsOfType<MonoBehaviour>();
			for (int i=0; i<sceneObjects.Length; i++)
			{
				MonoBehaviour currentObj = sceneObjects[i];
				ITranslatable currentComponent = currentObj as ITranslatable;
				if (currentComponent != null)
				{
					ClearTranslatable (currentComponent, true, true);
				}
			}

			#if !ACIgnoreTimeline
			PlayableDirector[] directors = FindObjectsOfType<PlayableDirector>();
			for (int i=0; i<directors.Length; i++)
			{
				ClearTimeline (directors[i]);
			}
			#endif

			// Save the scene
			UnityVersionHandler.SaveScene ();
			EditorUtility.SetDirty (this);
		}
		
		
		private void ClearLinesFromActionListAsset (ActionListAsset actionListAsset)
		{
			if (actionListAsset != null && !checkedAssets.Contains (actionListAsset))
			{
				checkedAssets.Add (actionListAsset);
				ClearLines (actionListAsset.actions, false);
				EditorUtility.SetDirty (actionListAsset);
			}
		}
		
		
		private void ClearLinesFromActionList (ActionList actionList)
		{
			if (actionList != null)
			{
				ClearLines (actionList.actions, true);
				EditorUtility.SetDirty (actionList);
			}
		}
		
		
		private void ClearLines (List<Action> actions, bool isInScene)
		{
			if (actions == null)
			{
				return;
			}
			
			foreach (Action action in actions)
			{
				if (action == null)
				{
					continue;
				}

				if (action is ITranslatable)
				{
					ClearTranslatable (action as ITranslatable, isInScene, false);
				}

				#if !ACIgnoreTimeline
				if (action is ActionTimeline)
				{
					ActionTimeline actionTimeline = (ActionTimeline) action;
					ClearTimeline (actionTimeline.director);
				}
				#endif
			}
		}


		private void ClearLinesFromSettings ()
		{
			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			if (settingsManager)
			{
				ClearTranslatable (settingsManager.saveLabels, false, false);

				if (settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					foreach (PlayerPrefab playerPrefab in settingsManager.players)
					{
						if (playerPrefab != null && playerPrefab.playerOb != null)
						{
							ClearTranslatable (playerPrefab.playerOb, false, true);
							EditorUtility.SetDirty (playerPrefab.playerOb);
						}
					}
				}
				else if (settingsManager.player)
				{
					ClearTranslatable (settingsManager.player, false, true);
				}
			}
		}


		private void ClearLinesFromVariables ()
		{
			VariablesManager variablesManager = AdvGame.GetReferences ().variablesManager;
			if (variablesManager != null)
			{
				foreach (GVar globalVariable in variablesManager.vars)
				{
					ClearTranslatable (globalVariable, false, false);
				}
				foreach (PopUpLabelData data in variablesManager.popUpLabelData)
				{
					ClearTranslatable (data, false, false);
				}
				EditorUtility.SetDirty (variablesManager);
			}
		}

		
		private void ClearLinesFromInventory ()
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			
			if (inventoryManager != null)
			{
				// Inventory properties
				if (inventoryManager.invVars != null && inventoryManager.invVars.Count > 0)
				{
					for (int i=0; i<inventoryManager.invVars.Count; i++)
					{
						ClearTranslatable (inventoryManager.invVars[i], false, false);
					}
				}

				if (inventoryManager.items != null && inventoryManager.items.Count > 0)
				{
					for (int i=0; i<inventoryManager.items.Count; i++)
					{
						// Label / properties
						ClearTranslatable (inventoryManager.items[i], false, false);

						// Prefixes
						if (inventoryManager.items[i].overrideUseSyntax)
						{
							ClearTranslatable (inventoryManager.items[i].hotspotPrefix1, false, false);
							ClearTranslatable (inventoryManager.items[i].hotspotPrefix2, false, false);
						}
					}
				}

				// Documents
				if (inventoryManager.documents != null && inventoryManager.documents.Count > 0)
				{
					for (int i=0; i<inventoryManager.documents.Count; i++)
					{
						ClearTranslatable (inventoryManager.documents[i], false, false);
					}
				}

				// Objectives
				if (inventoryManager.objectives != null && inventoryManager.objectives.Count > 0)
				{
					foreach (Objective objective in inventoryManager.objectives)
					{
						ClearTranslatable (objective, false, false);

						foreach (ObjectiveState state in objective.states)
						{
							ClearTranslatable (state, false, false);
						}
					}
				}
			}
				
			EditorUtility.SetDirty (inventoryManager);
		}
		
		
		private void ClearLinesFromCursors ()
		{
			CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
			
			if (cursorManager)
			{
				// Prefixes
				ClearTranslatable (cursorManager.hotspotPrefix1, false, false);
				ClearTranslatable (cursorManager.hotspotPrefix2, false, false);
				ClearTranslatable (cursorManager.hotspotPrefix3, false, false);
				ClearTranslatable (cursorManager.hotspotPrefix4, false, false);
				ClearTranslatable (cursorManager.walkPrefix, false, false);

				// Gather icons
				if (cursorManager.cursorIcons.Count > 0)
				{
					for (int i=0; i<cursorManager.cursorIcons.Count; i++)
					{
						ClearTranslatable (cursorManager.cursorIcons[i], false, false);
					}
				}
				
				EditorUtility.SetDirty (cursorManager);
			}
		}
		
		
		private void ClearLinesFromMenus ()
		{
			MenuManager menuManager = AdvGame.GetReferences ().menuManager;
			
			if (menuManager)
			{
				// Gather elements
				if (menuManager.menus.Count > 0)
				{
					foreach (AC.Menu menu in menuManager.menus)
					{
						foreach (MenuElement element in menu.elements)
						{
							if (element is ITranslatable)
							{
								ClearTranslatable (element as ITranslatable, false, false);
							}
						}
					}
				}
				
				EditorUtility.SetDirty (menuManager);
			}		
		}


		/**
		 * <summary>Gets all ActionList assets referenced by scenes, Managers and other asset files in the project</summary>
		 * <returns>All ActionList assets referenced by scenes, Managers and other asset files in the project</returns>
		 */
		public ActionListAsset[] GetAllActionListAssets ()
		{
			CollectAllActionListAssets ();
			return allActionListAssets.ToArray ();
		}


		private void CollectAllActionListAssets ()
		{
			allActionListAssets = new List<ActionListAsset>();

			// Search scenes
			sceneFiles = AdvGame.GetSceneFiles ();
			
			foreach (string sceneFile in sceneFiles)
			{
				UnityVersionHandler.OpenScene (sceneFile);

				CollectAllCurrentActionListAssets (null);

				// ActionLists
				ActionList[] actionLists = FindObjectsOfType (typeof (ActionList)) as ActionList[];
				foreach (ActionList actionList in actionLists)
				{
					if (actionList.source == ActionListSource.InScene)
					{
						foreach (Action action in actionList.actions)
						{
							if (action != null && action is ActionInstantiate)
							{
								ActionInstantiate actionInstantiate = action as	ActionInstantiate;
								if (actionInstantiate.invAction == InvAction.Add && actionInstantiate.gameObject != null && actionInstantiate.parameterID == -1)
								{
									CollectAllCurrentActionListAssets (actionInstantiate.gameObject);
								}
							}
						}
					}
				}
			}

			// Settings Manager
			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			if (settingsManager)
			{
				SmartAddAsset (settingsManager.actionListOnStart);
				if (settingsManager.activeInputs != null)
				{
					foreach (ActiveInput activeInput in settingsManager.activeInputs)
					{
						SmartAddAsset (activeInput.actionListAsset);
					}
				}

				if (settingsManager.playerSwitching == PlayerSwitching.DoNotAllow && settingsManager.player != null)
				{
					GatherAssetsFromPlayer (settingsManager.player);
				}
				if (settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					foreach (PlayerPrefab playerPrefab in KickStarter.settingsManager.players)
					{
						if (playerPrefab.playerOb != null)
						{
							GatherAssetsFromPlayer (playerPrefab.playerOb);
						}
					}
				}
			}

			// Inventory Manager
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager)
			{
				SmartAddAsset (inventoryManager.unhandledCombine);
				SmartAddAsset (inventoryManager.unhandledHotspot);
				SmartAddAsset (inventoryManager.unhandledGive);

				if (inventoryManager.items.Count > 0)
				{
					foreach (InvItem item in inventoryManager.items)
					{
						SmartAddAsset (item.useActionList);
						SmartAddAsset (item.lookActionList);
						SmartAddAsset (item.unhandledActionList);
						SmartAddAsset (item.unhandledGiveActionList);
						SmartAddAsset (item.unhandledCombineActionList);

						foreach (InvInteraction invInteraction in item.interactions)
						{
							SmartAddAsset (invInteraction.actionList);
						}

						foreach (InvCombineInteraction invCombineInteraction in item.combineInteractions)
						{
							SmartAddAsset (invCombineInteraction.actionList);
						}
					}
				}
				foreach (Recipe recipe in inventoryManager.recipes)
				{
					SmartAddAsset (recipe.invActionList);
					SmartAddAsset (recipe.actionListOnCreate);
				}
			}

			// Cursor Manager
			CursorManager cursorManager = AdvGame.GetReferences ().cursorManager;
			if (cursorManager)
			{
				foreach (ActionListAsset actionListAsset in cursorManager.unhandledCursorInteractions)
				{
					SmartAddAsset (actionListAsset);
				}
			}

			// Menu Manager
			MenuManager menuManager = AdvGame.GetReferences ().menuManager;
			if (menuManager)
			{
				if (menuManager.menus.Count > 0)
				{
					foreach (AC.Menu menu in menuManager.menus)
					{
						SmartAddAsset (menu.actionListOnTurnOn);
						SmartAddAsset (menu.actionListOnTurnOff);

						foreach (MenuElement element in menu.elements)
						{
							if (element is MenuButton)
							{
								MenuButton menuButton = (MenuButton) element;
								if (menuButton.buttonClickType == AC_ButtonClickType.RunActionList)
								{
									SmartAddAsset (menuButton.actionList);
								}
							}
							else if (element is MenuSavesList)
							{
								MenuSavesList menuSavesList = (MenuSavesList) element;
								SmartAddAsset (menuSavesList.actionListOnSave);
							}
							else if (element is MenuCycle)
							{
								MenuCycle menuCycle = (MenuCycle) element;
								SmartAddAsset (menuCycle.actionListOnClick);
							}
							else if (element is MenuJournal)
							{
								MenuJournal menuJournal = (MenuJournal) element;
								SmartAddAsset (menuJournal.actionListOnAddPage);
							}
							else if (element is MenuSlider)
							{
								MenuSlider menuSlider = (MenuSlider) element;
								SmartAddAsset (menuSlider.actionListOnChange);
							}
							else if (element is MenuToggle)
							{
								MenuToggle menuToggle = (MenuToggle) element;
								SmartAddAsset (menuToggle.actionListOnClick);
							}
							else if (element is MenuProfilesList)
							{
								MenuProfilesList menuProfilesList = (MenuProfilesList) element;
								SmartAddAsset (menuProfilesList.actionListOnClick);
							}
						}
					}
				}
			}
		}


		private void CollectAllCurrentActionListAssets (GameObject gameObject)
		{
			// Scene settings
			if (gameObject == null)
			{
				SceneSettings sceneSettings = FindObjectOfType <SceneSettings>();
				if (sceneSettings && sceneSettings.actionListSource == ActionListSource.AssetFile)
				{
					SmartAddAsset (sceneSettings.actionListAssetOnStart);
					SmartAddAsset (sceneSettings.actionListAssetOnLoad);
					SmartAddAsset (sceneSettings.actionListAssetOnVarChange);
				}
			}

			// ActionLists
			ActionList[] actionLists = (gameObject != null)
										? gameObject.GetComponentsInChildren <ActionList>()
										: FindObjectsOfType (typeof (ActionList)) as ActionList[];

			foreach (ActionList actionList in actionLists)
			{
				if (actionList.useParameters && actionList.parameters != null)
				{
					if (!actionList.syncParamValues && actionList.source == ActionListSource.AssetFile && actionList.assetFile && actionList.assetFile.useParameters)
					{
						foreach (ActionParameter parameter in actionList.parameters)
						{
							if (parameter.parameterType == ParameterType.UnityObject)
							{
								if (parameter.objectValue != null)
								{
									if (parameter.objectValue is ActionListAsset)
									{
										ActionListAsset _actionListAsset = (ActionListAsset) parameter.objectValue;
										SmartAddAsset (_actionListAsset);
									}
								}
							}
						}
					}
				}

				if (actionList.source == ActionListSource.AssetFile)
				{
					SmartAddAsset (actionList.assetFile);
				}
				else
				{
					GetActionListAssetsFromActions (actionList.actions);
				}
			}

			// Hotspots
			Hotspot[] hotspots = (gameObject != null)
								 ? gameObject.GetComponentsInChildren <Hotspot>()
								 : FindObjectsOfType (typeof (Hotspot)) as Hotspot[];

			foreach (Hotspot hotspot in hotspots)
			{
				if (hotspot.interactionSource == InteractionSource.AssetFile)
				{
					SmartAddAsset (hotspot.lookButton.assetFile);
					SmartAddAsset (hotspot.unhandledInvButton.assetFile);

					foreach (Button _button in hotspot.useButtons)
					{
						SmartAddAsset (_button.assetFile);
					}

					foreach (Button _button in hotspot.invButtons)
					{
						SmartAddAsset (_button.assetFile);
					}
				}
			}

			// Dialogue options
			Conversation[] conversations = (gameObject != null)
											? gameObject.GetComponentsInChildren <Conversation>()
											: FindObjectsOfType (typeof (Conversation)) as Conversation[];

			foreach (Conversation conversation in conversations)
			{
				foreach (ButtonDialog dialogOption in conversation.options)
				{
					SmartAddAsset (dialogOption.assetFile);
				}
				EditorUtility.SetDirty (conversation);
			}

			// SetParameterBase components
			SetParametersBase[] setParameterBases = (gameObject != null)
													? gameObject.GetComponentsInChildren <SetParametersBase>()
													: FindObjectsOfType (typeof (SetParametersBase)) as SetParametersBase[];

			foreach (SetParametersBase setParameterBase in setParameterBases)
			{
				List<ActionListAsset> foundAssets = setParameterBase.GetReferencedActionListAssets ();
				if (foundAssets != null)
				{
					foreach (ActionListAsset foundAsset in foundAssets)
					{
						SmartAddAsset (foundAsset);
					}
				}
			}

			// Draggable
			Moveable_Drag[] moveable_Drags = (gameObject != null)
											 ? gameObject.GetComponentsInChildren <Moveable_Drag>()
											 : FindObjectsOfType (typeof (Moveable_Drag)) as Moveable_Drag[];

			foreach (Moveable_Drag moveable_Drag in moveable_Drags)
			{
				if (moveable_Drag.actionListSource == ActionListSource.AssetFile)
				{
					SmartAddAsset (moveable_Drag.actionListAssetOnDrop);
					SmartAddAsset (moveable_Drag.actionListAssetOnMove);
				}
			}

			// PickUp
			Moveable_PickUp[] moveable_PickUps = (gameObject != null)
												 ? gameObject.GetComponentsInChildren <Moveable_PickUp>()
												 : FindObjectsOfType (typeof (Moveable_PickUp)) as Moveable_PickUp[];

			foreach (Moveable_PickUp moveable_PickUp in moveable_PickUps)
			{
				if (moveable_PickUp.actionListSource == ActionListSource.AssetFile)
				{
					SmartAddAsset (moveable_PickUp.actionListAssetOnGrab);
					SmartAddAsset (moveable_PickUp.actionListAssetOnDrop);
				}
			}
		}


		private void GatherAssetsFromPlayer (Player player)
		{
			if (player == null) return;

			Hotspot[] hotspots = player.GetComponentsInChildren <Hotspot>();
			foreach (Hotspot hotspot in hotspots)
			{
				if (hotspot.interactionSource == InteractionSource.AssetFile)
				{
					SmartAddAsset (hotspot.lookButton.assetFile);
					SmartAddAsset (hotspot.unhandledInvButton.assetFile);

					foreach (Button _button in hotspot.useButtons)
					{
						SmartAddAsset (_button.assetFile);
					}

					foreach (Button _button in hotspot.invButtons)
					{
						SmartAddAsset (_button.assetFile);
					}
				}
			}
		}


#if !ACIgnoreTimeline
		private void SetOrderIDs (TimelineAsset timelineAsset)
		{
			string prefix = timelineAsset.name + "_" + timelineAsset.GetHashCode () + "_";

			IEnumerable<TrackAsset> trackAssets = timelineAsset.GetOutputTracks ();

			foreach (TrackAsset trackAsset in trackAssets)
			{
				if (trackAsset is ITranslatable)
				{
					IEnumerable<TimelineClip> timelineClips = trackAsset.GetClips ();
					ITranslatable translatable = trackAsset as ITranslatable;

					int i = 0;
					foreach (TimelineClip timelineClip in timelineClips)
					{
						SpeechLine speechLine = GetLine (translatable.GetTranslationID (i));
						if (speechLine != null)
						{
							speechLine.orderID = (int) (timelineClip.start * 10);
							speechLine.orderPrefix = prefix;
						}

						i++;
					}
				}
			}
		}


		private void GetLinesFromTimelines (bool onlySeekNew)
		{
			foreach (TimelineAsset timelineAsset in allTimelineAssets)
			{
				IEnumerable<TrackAsset> trackAssets = timelineAsset.GetOutputTracks ();
				foreach (TrackAsset trackAsset in trackAssets)
				{
					if (trackAsset is ITranslatable)
					{
						ExtractTranslatable (trackAsset as ITranslatable, onlySeekNew, false, false, string.Empty, -1, timelineAsset.name);
					}
				}

				if (onlySeekNew)
				{
					SetOrderIDs (timelineAsset);
				}
			}
		}


		private bool LocateLineInTimeline (PlayableDirector director, int lineID)
		{
			if (director.playableAsset && director.playableAsset is TimelineAsset)
			{
				TimelineAsset timelineAsset = director.playableAsset as TimelineAsset;
				IEnumerable<TrackAsset> trackAssets = timelineAsset.GetOutputTracks ();

				foreach (TrackAsset trackAsset in trackAssets)
				{
					if (trackAsset is ITranslatable)
					{
						ITranslatable translatable = trackAsset as ITranslatable;

						int[] newIDs = new int[translatable.GetNumTranslatables ()];
						for (int i=0; i<newIDs.Length; i++)
						{
							if (translatable.CanTranslate (i) && translatable.GetTranslationID (i) == lineID)
							{
								EditorGUIUtility.PingObject (timelineAsset);
								return true;
							}
						}
					}
				}
			}

			return false;
		}


		private void SmartAddAsset (TimelineAsset asset)
		{
			if (asset != null)
			{
				if (allTimelineAssets.Contains (asset))
				{
					return;
				}

				allTimelineAssets.Add (asset);
			}
		}


		private void ClearTimeline (PlayableDirector director)
		{
			if (director != null)
			{
				if (director.playableAsset && director.playableAsset is TimelineAsset)
				{
					TimelineAsset timelineAsset = director.playableAsset as TimelineAsset;
					IEnumerable<TrackAsset> trackAssets = timelineAsset.GetOutputTracks ();

					foreach (TrackAsset trackAsset in trackAssets)
					{
						if (trackAsset is ITranslatable)
						{
							ClearTranslatable (trackAsset as ITranslatable, false, false);
						}
					}
				}
			}
		}

		#endif


		private void SmartAddAsset (ActionListAsset asset)
		{
			if (asset != null)
			{
				if (allActionListAssets.Contains (asset))
				{
					return;
				}
				allActionListAssets.Add (asset);
				GetActionListAssetsFromActions (asset.actions);
			}
		}


		private void GetActionListAssetsFromActions (List<Action> actions)
		{
			if (actions != null)
			{
				foreach (Action action in actions)
				{
					if (action == null) continue;

					action.Upgrade ();

					if (action is ActionRunActionList)
					{
						ActionRunActionList actionRunActionList = (ActionRunActionList) action;
						if (actionRunActionList.listSource == ActionRunActionList.ListSource.AssetFile)
						{
							SmartAddAsset (actionRunActionList.invActionList);
						}

						if ((actionRunActionList.listSource == ActionRunActionList.ListSource.InScene && actionRunActionList.actionList && actionRunActionList.actionList.useParameters) ||
							(actionRunActionList.listSource == ActionRunActionList.ListSource.AssetFile && actionRunActionList.invActionList && actionRunActionList.invActionList.useParameters))
						{
							if (actionRunActionList.localParameters != null)
							{
								foreach (ActionParameter localParameter in actionRunActionList.localParameters)
								{
									if (localParameter.parameterType == ParameterType.UnityObject)
									{
										if (localParameter.objectValue != null)
										{
											if (localParameter.objectValue is ActionListAsset)
											{
												ActionListAsset _actionListAsset = (ActionListAsset) localParameter.objectValue;
												SmartAddAsset (_actionListAsset);
											}
										}
									}
								}
							}
						}
					}

					if (action is ActionParamSet)
					{
						ActionParamSet actionParamSet = (ActionParamSet) action;
						if (actionParamSet.setParamMethod == SetParamMethod.EnteredHere)
						{
							if (actionParamSet.unityObjectValue != null)
							{
								if (actionParamSet.unityObjectValue is ActionListAsset)
								{
									ActionListAsset _actionListAsset = (ActionListAsset) actionParamSet.unityObjectValue;
									SmartAddAsset (_actionListAsset);
								}
							}
						}
					}

					foreach (ActionEnd ending in action.endings)
					{
						if (ending.resultAction == ResultAction.RunCutscene)
						{
							SmartAddAsset (ending.linkedAsset);
						}
					}
				}
			}
		}

		
		private void ProcessActionListAsset (ActionListAsset actionListAsset, bool onlySeekNew)
		{
			if (actionListAsset && !checkedAssets.Contains (actionListAsset))
			{
				checkedAssets.Add (actionListAsset);
				ProcessActions (actionListAsset.actions, onlySeekNew, false, actionListAsset.tagID, actionListAsset.name, actionListAsset.GetHashCode ());
				EditorUtility.SetDirty (actionListAsset);

				if (onlySeekNew)
				{
					int nullFound = 0;
					foreach (Action action in actionListAsset.actions)
					{
						if (action == null)
						{
							nullFound ++;
						}
					}
					if (nullFound > 0)
					{
						ACDebug.LogWarning (nullFound + " Null Action(s) found in ActionList asset '" + actionListAsset + "'.", actionListAsset);
					}
				}
			}
		}
		
		
		private void ProcessActionList (ActionList actionList, bool onlySeekNew)
		{
			if (actionList != null)
			{
				ProcessActions (actionList.actions, onlySeekNew, true, actionList.tagID, actionList.name, actionList.GetHashCode ());
				EditorUtility.SetDirty (actionList);

				if (onlySeekNew)
				{
					int nullFound = 0;
					foreach (Action action in actionList.actions)
					{
						if (action == null)
						{
							nullFound ++;
						}
					}
					if (nullFound > 0)
					{
						ACDebug.LogWarning (nullFound + " Null Action(s) found in ActionList '" + actionList + "' in scene '" + actionList.gameObject.scene.name + "'.", actionList);
					}
				}
			}
			
		}
		
		
		private void ProcessActions (List<Action> actions, bool onlySeekNew, bool isInScene, int tagID, string actionListName, int hashCode)
		{
			foreach (Action action in actions)
			{
				if (action == null)
				{
					continue;
				}

				if (action is ITranslatable)
				{
					ExtractTranslatable (action as ITranslatable, onlySeekNew, isInScene, false, action.comment, tagID, actionListName);
				}

				if (action is ActionInstantiate)
				{
					ActionInstantiate actionInstantiate = action as ActionInstantiate;
					if (actionInstantiate.invAction == InvAction.Add || actionInstantiate.invAction == InvAction.Replace)
					{
						#if AddressableIsPresent
						if (actionInstantiate.referenceByAddressable)
						{
							continue;
						}
						#endif
						if (actionInstantiate.parameterID < 0 && actionInstantiate.gameObject && UnityVersionHandler.IsPrefabFile (actionInstantiate.gameObject))
						{
							ITranslatable[] prefabTranslatables = actionInstantiate.gameObject.GetComponentsInChildren <ITranslatable>();
							foreach (ITranslatable prefabTranslatable in prefabTranslatables)
							{
								ExtractTranslatable (prefabTranslatable, onlySeekNew, false, true, string.Empty, -1, actionListName);
							}
						}
					}
				}

				#if !ACIgnoreTimeline
				if (!onlySeekNew)
				{
					if (action is ActionTimeline)
					{
						ActionTimeline actionTimeline = action as ActionTimeline;
						SmartAddAsset (actionTimeline.GetTimelineAsset ());
					}
				}
				#endif
			}

			if (onlySeekNew)
			{
				SetOrderIDs (actions, actionListName, hashCode);
			}
		}


		private void SetOrderIDs (List<Action> actions, string actionListName, int hashCode)
		{
			string prefix = actionListName + "_" + hashCode + "_";

			foreach (Action action in actions)
			{
				if (action == null) continue;
				action.isMarked = true;
			}

			minOrderValue = 0;

			ArrangeFromIndex (actions, prefix, 0);

			minOrderValue ++;

			foreach (Action _action in actions)
			{
				if (_action == null) continue;
				if (_action.isMarked)
				{
					// Wasn't arranged
					_action.isMarked = false;

					if (_action is ActionSpeech)
					{
						ActionSpeech actionSpeech = (ActionSpeech) _action;

						SpeechLine speechLine = GetLine (actionSpeech.lineID);
						if (speechLine != null)
						{
							speechLine.orderID = minOrderValue;
							speechLine.orderPrefix = prefix;
							minOrderValue ++;
						}

						if (actionSpeech.multiLineIDs != null && actionSpeech.multiLineIDs.Length > 0)
						{
							foreach (int multiLineID in actionSpeech.multiLineIDs)
							{
								SpeechLine multiSpeechLine = GetLine (multiLineID);
								if (multiSpeechLine != null)
								{
									multiSpeechLine.orderID = minOrderValue;
									multiSpeechLine.orderPrefix = prefix;
									minOrderValue ++;
								} 
							}
						}
					}
				}
			}

			foreach (Action action in actions)
			{
				if (action == null) continue;
				action.isMarked = false;
			}
		}


		private void ArrangeFromIndex (List<Action> actionList, string prefix, int i)
		{
			while (i > -1 && actionList.Count > i)
			{
				Action _action = actionList[i];

				if (_action == null)
				{
					return;
				}

				if (_action is ActionSpeech && _action.isMarked)
				{
					int yPos = minOrderValue;

					if (i > 0)
					{
						// Find top-most Y position
						bool doAgain = true;
						
						while (doAgain)
						{
							int numChanged = 0;
							foreach (Action otherAction in actionList)
							{
								if (otherAction is ActionSpeech && otherAction != _action)
								{
									ActionSpeech otherActionSpeech = (ActionSpeech) otherAction;
									SpeechLine otherSpeechLine = GetLine (otherActionSpeech.lineID);
									if (otherSpeechLine != null)
									{
										int otherOrderID = otherSpeechLine.orderID;
										if (otherOrderID >= yPos)
										{
											yPos ++;
											numChanged ++;
										}
									}

									if (otherActionSpeech.multiLineIDs != null && otherActionSpeech.multiLineIDs.Length > 0)
									{
										foreach (int otherMultiLineID in otherActionSpeech.multiLineIDs)
										{
											SpeechLine otherMultiSpeechLine = GetLine (otherMultiLineID);
											if (otherMultiSpeechLine != null)
											{
												int otherOrderID = otherMultiSpeechLine.orderID;
												if (otherOrderID >= yPos)
												{
													yPos ++;
													numChanged ++;
												}
											} 
										}
									}
								}
							}
							
							if (numChanged == 0)
							{
								doAgain = false;
							}
						}
					}

					if (yPos > minOrderValue)
					{
						minOrderValue = yPos;
					}

					ActionSpeech actionSpeech = (ActionSpeech) _action;
					SpeechLine speechLine = GetLine (actionSpeech.lineID);

					if (speechLine != null)
					{
						speechLine.orderID = minOrderValue;
						speechLine.orderPrefix = prefix;
						minOrderValue ++;
					}

					if (actionSpeech.multiLineIDs != null && actionSpeech.multiLineIDs.Length > 0)
					{
						foreach (int multiLineID in actionSpeech.multiLineIDs)
						{
							SpeechLine multiSpeechLine = GetLine (multiLineID);
							if (multiSpeechLine != null)
							{
								multiSpeechLine.orderID = minOrderValue;
								multiSpeechLine.orderPrefix = prefix;
								minOrderValue ++;
							} 
						}
					}

					if (yPos > minOrderValue)
					{
						minOrderValue = yPos;
					}
				}
				
				if (_action.isMarked == false)
				{
					return;
				}
				
				_action.isMarked = false;
				_action.Upgrade ();

				for (int j = _action.endings.Count - 1; j >= 0; j--)
				{
					ActionEnd ending = _action.endings[j];
					if (j >= 0)
					{
						if (ending.resultAction == ResultAction.Skip)
						{
							ArrangeFromIndex (actionList, prefix, ending.skipAction);
						}
						else if (ending.resultAction == ResultAction.Continue)
						{
							ArrangeFromIndex (actionList, prefix, i + 1);
						}
					}
				}
			}
		}



		/**
		 * <summary>Gets a defined SpeechTag.</summary>
		 * <param name = "ID">The ID number of the SpeechTag to get</param>
		 * <returns>The SpeechTag</summary>
		 */
		public SpeechTag GetSpeechTag (int ID)
		{
			foreach (SpeechTag speechTag in speechTags)
			{
				if (speechTag.ID == ID)
				{
					return speechTag;
				}
			}
			return null;
		}


		/**
		 * <summary>Converts the Speech Managers's references from a given local variable to a given global variable</summary>
		 * <param name = "variable">The old local variable</param>
		 * <param name = "newGlobalID">The ID number of the new global variable</param>
		 */
		public void ConvertLocalVariableToGlobal (GVar variable, int newGlobalID)
		{
			bool wasAmended = false;

			int lineID = -1;
			if (variable.type == VariableType.String)
			{
				lineID = variable.textValLineID;
			}
			else if (variable.type == VariableType.PopUp && variable.popUpID <= 0)
			{
				lineID = variable.popUpsLineID;
			}

			if (lineID >= 0)
			{
				SpeechLine speechLine = GetLine (lineID);
				if (speechLine != null && speechLine.textType == AC_TextType.Variable)
				{
					speechLine.scene = string.Empty;
					ACDebug.Log ("Updated Speech Manager line " + lineID);
					wasAmended = true;
				}
			}

			if (wasAmended)
			{
				EditorUtility.SetDirty (this);
			}
		}


		/**
		 * <summary>Converts the Speech Managers's references from a given global variable to a given local variable</summary>
		 * <param name = "variable">The old global variable</param>
		 * <param name = "sceneName">The name of the scene that the new variable lives in</param>
		 */
		public void ConvertGlobalVariableToLocal (GVar variable, string sceneName)
		{
			bool wasAmended = false;

			int lineID = -1;
			if (variable.type == VariableType.String)
			{
				lineID = variable.textValLineID;
			}
			else if (variable.type == VariableType.PopUp && variable.popUpID <= 0)
			{
				lineID = variable.popUpsLineID;
			}

			if (lineID >= 0)
			{
				SpeechLine speechLine = GetLine (lineID);
				if (speechLine != null && speechLine.textType == AC_TextType.Variable)
				{
					speechLine.scene = sceneName;
					ACDebug.Log ("Updated Speech Manager line " + lineID);
					wasAmended = true;
				}
			}

			if (wasAmended)
			{
				EditorUtility.SetDirty (this);
			}
		}

#endif


		/*
		 * The subdirectory within Resources that speech files are pulled from, if referenceSpeechFiles = ReferenceSpeechFiles.ByNamingConvention.  Always ends with a forward-slash '/'.
		 */
		public string AutoSpeechFolder
		{
			get
			{
				if (string.IsNullOrEmpty (autoSpeechFolder))
				{
					return string.Empty;
				}
				if (!autoSpeechFolder.EndsWith ("/"))
				{
					return autoSpeechFolder + "/";
				}
				return autoSpeechFolder;
			}
		}


		/*
		 * The subdirectory within Resources that lipsync files are pulled from, if referenceSpeechFiles = ReferenceSpeechFiles.ByNamingConvention.  Always ends with a forward-slash '/'.
		 */
		public string AutoLipsyncFolder
		{
			get
			{
				if (string.IsNullOrEmpty (autoLipsyncFolder))
				{
					return string.Empty;
				}
				if (!autoLipsyncFolder.EndsWith ("/"))
				{
					return autoLipsyncFolder + "/";
				}
				return autoLipsyncFolder;
			}
		}


		private void SyncLanguageData ()
		{
			if (Languages == null || Languages.Count <= 0)
			{
				if (languages.Count < languageIsRightToLeft.Count)
				{
					languageIsRightToLeft.RemoveRange (languages.Count, languageIsRightToLeft.Count - languages.Count);
				}
				else if (languages.Count > languageIsRightToLeft.Count)
				{
					if (languages.Count > languageIsRightToLeft.Capacity)
					{
						languageIsRightToLeft.Capacity = languages.Count;
					}
					for (int i = languageIsRightToLeft.Count; i < languages.Count; i++)
					{
						languageIsRightToLeft.Add (false);
					}
				}

				if (languages.Count < languageAudioAssetBundles.Count)
				{
					languageAudioAssetBundles.RemoveRange (languages.Count, languageAudioAssetBundles.Count - languages.Count);
				}
				else if (languages.Count > languageAudioAssetBundles.Count)
				{
					if (languages.Count > languageAudioAssetBundles.Capacity)
					{
						languageAudioAssetBundles.Capacity = languages.Count;
					}
					for (int i = languageAudioAssetBundles.Count; i < languages.Count; i++)
					{
						languageAudioAssetBundles.Add (string.Empty);
					}
				}

				if (languages.Count < languageLipsyncAssetBundles.Count)
				{
					languageLipsyncAssetBundles.RemoveRange (languages.Count, languageLipsyncAssetBundles.Count - languages.Count);
				}
				else if (languages.Count > languageLipsyncAssetBundles.Count)
				{
					if (languages.Count > languageLipsyncAssetBundles.Capacity)
					{
						languageLipsyncAssetBundles.Capacity = languages.Count;
					}
					for (int i = languageLipsyncAssetBundles.Count; i < languages.Count; i++)
					{
						languageLipsyncAssetBundles.Add (string.Empty);
					}
				}

				for (int i = 0; i < languages.Count; i++)
				{
					Language language = new	Language (languages[i], languageIsRightToLeft[i], languageAudioAssetBundles[i], languageLipsyncAssetBundles[i]);
					Languages.Add (language);
				}

				languages.Clear ();
				languageIsRightToLeft.Clear ();
				languageAudioAssetBundles.Clear ();
				languageLipsyncAssetBundles.Clear ();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (this);
				#endif
			}

			if (Languages.Count == 0)
			{
				for (int i = 0; i < languages.Count; i++)
				{
					Language language = new Language (languages[i], languageIsRightToLeft[i], languageAudioAssetBundles[i], languageLipsyncAssetBundles[i]);
					Languages.Add (language);
				}

				if (languages.Count == 0)
				{
					Languages.Add (new Language ("Original"));
				}

				languages.Clear ();
				languageIsRightToLeft.Clear ();
				languageAudioAssetBundles.Clear ();
				languageLipsyncAssetBundles.Clear ();

				#if UNITY_EDITOR
				EditorUtility.SetDirty (this);
				#endif
			}

			if (ignoreOriginalText && Languages.Count > 1)
			{
				ignoreOriginalText = false;
				Languages[0].isDisabled = true;
			}
		}


		/**
		 * <summary>Gets the audio filename of a SpeechLine.</summary>
		 * <param name = "_lineID">The translation ID number generated by SpeechManager's PopulateList() function</param>
		 * <param name = "speakerName">The name of the speaking character, which is only used if separating shared player audio</param>
		 * <returns>The audio filename of the speech line</summary>
		 */
		public string GetLineFilename (int _lineID, string speakerName = "")
		{
			foreach (SpeechLine line in lines)
			{
				if (line.lineID == _lineID)
				{
					return line.GetFilename (speakerName);
				}
			}
			return string.Empty;
		}


		/**
		 * <summary>Gets the full folder and filename for a speech line's audio or lipsync file, relative to the "Resources" Assets directory in which it is placed.</summary>
		 * <param name = "lineID">The ID number of the speech line</param>
		 * <param name = "speaker">The speaking character, if not a narration</param>
		 * <param name = "language">The language of the audio</param>
		 * <param name = "forLipSync">True if this is for a lipsync file</param>
		 * <returns>A string of the folder name that the audio or lipsync file should be placed in</returns>
		 */
		public string GetAutoAssetPathAndName (int lineID, Char speaker, string language, bool forLipsync = false)
		{
			SpeechLine speechLine = GetLine (lineID);
			if (speechLine != null)
			{
				if (GetAutoAssetPathAndNameOverride != null)
				{
					return GetAutoAssetPathAndNameOverride (speechLine, language, forLipsync);
				}

				string speakerOverride = (speaker != null) ? speaker.name : string.Empty;
				return speechLine.GetAutoAssetPathAndName (language, forLipsync, speakerOverride);
			}

			return string.Empty;
		}


		/**
		 * <summary>Gets a SpeechLine class, as generated by the Speech Manager.</summary>
		 * <param name = "_lineID">The translation ID number generated by SpeechManager's PopulateList() function</param>
		 * <returns>The generated SpeechLine class</summary>
		 */
		public SpeechLine GetLine (int _lineID)
		{
			foreach (SpeechLine line in lines)
			{
				if (line.lineID == _lineID)
				{
					return line;
				}
			}
			return null;
		}


		/**
		 * <summary>Checks if the current lipsyncing method relies on external text files for each line.</summary>
		 * <returns>True if the current lipsyncing method relies on external text files for each line.</returns>
		 */
		public bool UseFileBasedLipSyncing ()
		{
			if (lipSyncMode == LipSyncMode.ReadPamelaFile || lipSyncMode == LipSyncMode.ReadPapagayoFile || lipSyncMode == LipSyncMode.ReadSapiFile || lipSyncMode == LipSyncMode.RogoLipSync)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Checks if a text type is able to be translated (and will be included in the 'gather text' process)</summary>
		 * <param name = "textType">The text type to check</param>
		 * <returns>True if the text type is able to be translated<returns>  
		 */
		public bool IsTextTypeTranslatable (AC_TextType textType)
		{
			#if UNITY_EDITOR
			Upgrade ();
			#endif

			int s1 = (int) textType;
			int s1_modified = (int) Mathf.Pow (2f, (float) s1);
			int s2 = (int) translatableTextTypes;
			return (s1_modified & s2) != 0;
		}


		/** Checks if the method of animating character speech is based on the audio associated with the speech line. */
		public bool LipSyncingIsAudioBased ()
		{
			switch (lipSyncMode)
			{
				case LipSyncMode.Off:
				case LipSyncMode.FromSpeechText:
					return false;

				default:
					return true;
			}
		}


		/** Updates the Speech Manager from previous AC versions */
		public void Upgrade ()
		{
			#if UNITY_EDITOR
			if (autoNameSpeechFiles)
			{
				referenceSpeechFiles = (useAssetBundles) ? ReferenceSpeechFiles.ByAssetBundle : ReferenceSpeechFiles.ByNamingConvention;

				useAssetBundles = false;
				autoNameSpeechFiles = false;
			}
			if (mergeMatchingSpeechIDs)
			{
				mergeMatchingIDs = MergeMatchingIDs.MergeSpeechOnly;
				mergeMatchingSpeechIDs = false;
			}
			#endif
			SyncLanguageData ();
		}

	}

}