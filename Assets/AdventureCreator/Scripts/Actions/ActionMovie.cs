/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionMovie.cs"
 * 
 *	Plays movie clips either on a Texture, or full-screen on mobile devices.
 * 
 */

//#if !UNITY_SWITCH
#define ALLOW_VIDEO
//#endif

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ALLOW_VIDEO
using UnityEngine.Video;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionMovie : Action
	{

		public MovieClipType movieClipType = MovieClipType.VideoPlayer;
		public MovieMaterialMethod movieMaterialMethod = MovieMaterialMethod.PlayMovie;
		
		public string skipKey;
		public bool canSkip;

		#if ALLOW_VIDEO
		public VideoPlayer videoPlayer;
		protected VideoPlayer runtimeVideoPlayer;
		public int videoPlayerParameterID = -1;
		public int videoPlayerConstantID;
		public bool prepareOnly = false;
		public bool pauseWithGame = false;
		private bool waitedAtLeastOneFrame;

			#if UNITY_WEBGL
			public string movieURL = "http://";
			public int movieURLParameterID = -1;
			#else
			public VideoClip newClip;
			public int newClipParameterID = -1;
			#endif
			protected bool isPaused;

		#endif

		#if (UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_TVOS)
		public string filePath;
		#endif


		public override ActionCategory Category { get { return ActionCategory.Engine; }}
		public override string Title { get { return "Play movie clip"; }}
		public override string Description { get { return "Plays movie clips either on a Texture, or full-screen on mobile devices."; }}



		public override void AssignValues (List<ActionParameter> parameters)
		{
			#if !(UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_TVOS)
			movieClipType = MovieClipType.VideoPlayer;
			#endif

			#if ALLOW_VIDEO
			runtimeVideoPlayer = AssignFile <VideoPlayer> (parameters, videoPlayerParameterID, videoPlayerConstantID, videoPlayer);
			isPaused = false;

				#if UNITY_WEBGL
				movieURL = AssignString (parameters, movieURLParameterID, movieURL);
				#else
				newClip = (VideoClip) AssignObject<VideoClip> (parameters, newClipParameterID, newClip);
				#endif

			waitedAtLeastOneFrame = false;
			#endif
		}
		

		public override float Run ()
		{
			if (movieClipType == MovieClipType.VideoPlayer)
			{
				#if ALLOW_VIDEO
				if (runtimeVideoPlayer != null)
				{
					if (!isRunning)
					{
						isRunning = true;

						if (runtimeVideoPlayer.isPlaying)
						{
							Log ("The VideoPlayer '" + runtimeVideoPlayer.name + "' is already playing - playback will be replaced");
						}

						if (movieMaterialMethod == MovieMaterialMethod.PlayMovie)
						{
							#if UNITY_WEBGL
							if (!string.IsNullOrEmpty (movieURL))
							{
								runtimeVideoPlayer.url = movieURL;
							}
							#else
							if (newClip != null)
							{
								runtimeVideoPlayer.clip = newClip;
							}
							#endif

							if (prepareOnly)
							{
								runtimeVideoPlayer.Prepare ();

								if (willWait)
								{
									return defaultPauseTime;
								}
							}
							else
							{
								KickStarter.playerInput.skipMovieKey = string.Empty;
								runtimeVideoPlayer.Play ();

								if (willWait)
								{
									if (runtimeVideoPlayer.isLooping)
									{
										LogWarning ("Cannot wait for " + runtimeVideoPlayer.name + " to finish because it is looping!");
										return 0f;
									}

									if (canSkip && !string.IsNullOrEmpty (skipKey))
									{
										KickStarter.playerInput.skipMovieKey = skipKey;
									}

									return defaultPauseTime;
								}
							}
						}
						else if (movieMaterialMethod == MovieMaterialMethod.PauseMovie)
						{
							runtimeVideoPlayer.Pause ();
						}
						else if (movieMaterialMethod == MovieMaterialMethod.StopMovie)
						{
							runtimeVideoPlayer.Stop ();
						}

						return 0f;
					}
					else
					{
						if (prepareOnly)
						{
							if (!runtimeVideoPlayer.isPrepared)
							{
								return defaultPauseTime;
							}
						}
						else
						{
							if (pauseWithGame)
							{
								if (KickStarter.stateHandler.gameState == GameState.Paused)
								{
									if (runtimeVideoPlayer.isPlaying && !isPaused)
									{
										runtimeVideoPlayer.Pause ();
										isPaused = true;
									}
									return defaultPauseTime;
								}
								else
								{
									if (!runtimeVideoPlayer.isPlaying && isPaused)
									{
										isPaused = false;
										runtimeVideoPlayer.Play ();
									}
								}
							}

							if (canSkip && !string.IsNullOrEmpty (skipKey) && string.IsNullOrEmpty (KickStarter.playerInput.skipMovieKey))
							{
								runtimeVideoPlayer.Stop ();
								isRunning = false;
								return 0f;
							}

							if (!runtimeVideoPlayer.isPrepared || runtimeVideoPlayer.isPlaying)
							{
								return defaultPauseTime;
							}

							if (!waitedAtLeastOneFrame)
							{
								waitedAtLeastOneFrame = true;
								return defaultPauseTime;
							}
						}

						runtimeVideoPlayer.Stop ();
						isRunning = false;
						return 0f;
					}
				}
				else
				{
					LogWarning ("Cannot play video - no Video Player found!");
				}
				#else
				LogWarning ("Use of the VideoPlayer for movie playback is not available on this platform.");
				#endif
				return 0f;
			}

			#if (UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_TVOS)

			if (!isRunning && filePath != "")
			{
				isRunning = true;

				if (canSkip)
				{
					Handheld.PlayFullScreenMovie (filePath, Color.black, FullScreenMovieControlMode.CancelOnInput);
				}
				else
				{
					Handheld.PlayFullScreenMovie (filePath, Color.black, FullScreenMovieControlMode.Hidden);
				}
				return defaultPauseTime;
			}
			else
			{
				isRunning = false;
				return 0f;
			}

			#else

			LogWarning ("On non-mobile platforms, this Action requires use of the Video Player.");
			return 0f;

			#endif
		}


		public override void Skip ()
		{
			OnComplete ();
		}


		protected void OnComplete ()
		{
			if (movieClipType == MovieClipType.VideoPlayer)
			{
				#if ALLOW_VIDEO
				if (runtimeVideoPlayer != null)
				{
					if (prepareOnly)
					{
						runtimeVideoPlayer.Prepare ();
					}
					else if (!runtimeVideoPlayer.isLooping)
					{
						runtimeVideoPlayer.Stop ();
					}
				}
				#endif
			}
			else if (movieClipType == MovieClipType.FullScreen || (movieClipType == MovieClipType.OnMaterial && movieMaterialMethod == MovieMaterialMethod.PlayMovie))
			{}
			else if (movieClipType == MovieClipType.OnMaterial && movieMaterialMethod != MovieMaterialMethod.PlayMovie)
			{
				Run ();
			}
		}

		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			#if (UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_TVOS)
			movieClipType = (MovieClipType) EditorGUILayout.EnumPopup ("Play clip:", movieClipType);
			#else
			movieClipType = MovieClipType.VideoPlayer;
			#endif

			if (movieClipType == MovieClipType.VideoPlayer)
			{
				#if ALLOW_VIDEO

				videoPlayerParameterID = Action.ChooseParameterGUI ("Video player:", parameters, videoPlayerParameterID, ParameterType.GameObject);
				if (videoPlayerParameterID >= 0)
				{
					videoPlayerConstantID = 0;
					videoPlayer = null;
				}
				else
				{
					videoPlayer = (VideoPlayer) EditorGUILayout.ObjectField ("Video player:", videoPlayer, typeof (VideoPlayer), true);

					videoPlayerConstantID = FieldToID <VideoPlayer> (videoPlayer, videoPlayerConstantID);
					videoPlayer = IDToField <VideoPlayer> (videoPlayer, videoPlayerConstantID, false);
				}

				movieMaterialMethod = (MovieMaterialMethod) EditorGUILayout.EnumPopup ("Method:", movieMaterialMethod);

				if (movieMaterialMethod == MovieMaterialMethod.PlayMovie)
				{
					#if UNITY_WEBGL
					movieURLParameterID = Action.ChooseParameterGUI ("Movie URL:", parameters, movieURLParameterID, new ParameterType[2] { ParameterType.String, ParameterType.PopUp });
					if (movieURLParameterID < 0)
					{
						movieURL = EditorGUILayout.TextField ("Movie URL:", movieURL);
					}
					#else
					newClipParameterID = Action.ChooseParameterGUI ("New clip (optional):", parameters, newClipParameterID, ParameterType.UnityObject);
					if (newClipParameterID < 0)
					{
						newClip = (VideoClip) EditorGUILayout.ObjectField ("New Clip (optional):", newClip, typeof (VideoClip), true);
					}
					#endif
            
					prepareOnly = EditorGUILayout.Toggle ("Prepare only?", prepareOnly);
					willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);

					if (willWait && !prepareOnly)
					{
						pauseWithGame = EditorGUILayout.Toggle ("Pause when game does?", pauseWithGame);
						canSkip = EditorGUILayout.Toggle ("Player can skip?", canSkip);
						if (canSkip)
						{
							skipKey = EditorGUILayout.TextField ("Skip with Input Button:", skipKey);
						}
					}
				}

				#elif UNITY_SWITCH

				EditorGUILayout.HelpBox ("This option not available on Switch.", MessageType.Info);

				#else

				EditorGUILayout.HelpBox ("This option is only available when using Unity 5.6 or later.", MessageType.Info);

				#endif

				return;
			}

			#if (UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_TVOS)

			if (movieClipType == MovieClipType.OnMaterial)
			{
				EditorGUILayout.HelpBox ("This option is not available on the current platform.", MessageType.Info);
			}
			else
			{
				filePath = EditorGUILayout.TextField ("Path to clip file:", filePath);
				canSkip = EditorGUILayout.Toggle ("Player can skip?", canSkip);

				EditorGUILayout.HelpBox ("The clip must be placed in a folder named 'StreamingAssets'.", MessageType.Info);
			}

			#endif
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			#if ALLOW_VIDEO
			if (movieClipType == MovieClipType.VideoPlayer && videoPlayer != null)
			{
				if (saveScriptsToo)
				{
					AddSaveScript <RememberVideoPlayer> (videoPlayer);
				}

				AssignConstantID (videoPlayer, videoPlayerConstantID, videoPlayerParameterID);
			}
			#endif
		}
		
		
		public override string SetLabel ()
		{
			if (movieClipType == MovieClipType.VideoPlayer)
			{
				#if ALLOW_VIDEO
				string labelAdd = movieMaterialMethod.ToString ();
				if (videoPlayer != null) labelAdd += " " + videoPlayer.name.ToString ();
				return labelAdd;
				#else
				return string.Empty;
				#endif
			}

			#if (UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_TVOS)

			if (!string.IsNullOrEmpty (filePath))
			{
				return filePath;
			}

			#endif
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			#if ALLOW_VIDEO
			if (movieClipType == MovieClipType.VideoPlayer && videoPlayerParameterID < 0)
			{
				if (videoPlayer && videoPlayer.gameObject == _gameObject) return true;
				if (videoPlayerConstantID == id) return true;
			}
			#endif
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		#if ALLOW_VIDEO

		/**
		 * <summary>Creates a new instance of the 'Engine: Play movie clip' Action, set to play a new video</summary>
		 * <param name = "videoPlayer">The video player to play on</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the video is complete</param>
		 * <param name = "pausewhenGameDoes">If True, the video player will pause when the game itself is paused</param>
		 * <param name = "inputButtonToSkip">If not empty, the name input button that will skip playback when pressed by the player</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMovie CreateNew_Play (VideoPlayer videoPlayer, bool waitUntilFinish = true, bool pauseWhenGameDoes = true, string inputButtonToSkip = "")
		{
			ActionMovie newAction = CreateNew<ActionMovie> ();
			newAction.movieClipType = MovieClipType.VideoPlayer;
			newAction.movieMaterialMethod = MovieMaterialMethod.PlayMovie;
			newAction.videoPlayer = videoPlayer;
			newAction.TryAssignConstantID (newAction.videoPlayer, ref newAction.videoPlayerConstantID);
			newAction.willWait = waitUntilFinish;
			newAction.prepareOnly = false;
			newAction.pauseWithGame = pauseWhenGameDoes;
			newAction.canSkip = !string.IsNullOrEmpty (inputButtonToSkip);
			newAction.skipKey = inputButtonToSkip;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: Play movie clip' Action, set to prepare a new video</summary>
		 * <param name = "videoPlayer">The video player to prepare</param>
		 */
		public static ActionMovie CreateNew_Prepare (VideoPlayer videoPlayer)
		{
			ActionMovie newAction = CreateNew<ActionMovie> ();
			newAction.movieClipType = MovieClipType.VideoPlayer;
			newAction.movieMaterialMethod = MovieMaterialMethod.PlayMovie;
			newAction.videoPlayer = videoPlayer;
			newAction.TryAssignConstantID (newAction.videoPlayer, ref newAction.videoPlayerConstantID);
			newAction.prepareOnly = true;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Engine: Play movie clip' Action, set to stop video playback</summary>
		 * <param name = "videoPlayer">The video player to stop</param>
		 * <param name = "pauseOnly">If True, then the video will be paused so that it can be later resumed</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionMovie CreateNew_Stop (VideoPlayer videoPlayer, bool pauseOnly = false)
		{
			ActionMovie newAction = CreateNew<ActionMovie> ();
			newAction.movieClipType = MovieClipType.VideoPlayer;
			newAction.movieMaterialMethod = (pauseOnly) ? MovieMaterialMethod.PauseMovie : MovieMaterialMethod.StopMovie;
			return newAction;
		}

		#endif

	}
	
}