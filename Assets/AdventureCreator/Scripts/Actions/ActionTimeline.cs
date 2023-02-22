/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionDirector.cs"
 * 
 *	This action plays and stops controls Playable Directors
 * 
 */

using UnityEngine;
using System.Collections.Generic;
#if !ACIgnoreTimeline
using UnityEngine.Timeline;
#endif
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionTimeline : Action
	{

		public bool disableCamera;

		public PlayableDirector director;
		protected PlayableDirector runtimeDirector;
		#if !ACIgnoreTimeline
		public TimelineAsset newTimeline;
		#endif
		public int directorConstantID = 0;
		public int directorParameterID = -1;

		public enum ActionDirectorMethod { Play, Stop };
		public ActionDirectorMethod method = ActionDirectorMethod.Play;
		public bool restart = true;
		public bool pause = false;
		public bool updateBindings = false;
		[SerializeField] protected BindingData[] newBindings = new BindingData[0];

		private double lastFrameDuration;


		public override ActionCategory Category { get { return ActionCategory.Engine; }}
		public override string Title { get { return "Control Timeline"; }}
		public override string Description { get { return "Controls a Timeline.  This is only compatible with Unity 2017 or newer."; }}

		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeDirector = AssignFile <PlayableDirector> (parameters, directorParameterID, directorConstantID, director);

			if (newBindings != null)
			{
				for (int i=0; i<newBindings.Length; i++)
				{
					if (newBindings[i].isPlayer)
					{
						Player player = AssignPlayer (newBindings[i].playerID, parameters, newBindings[i].parameterID);
						if (player != null)
						{
							newBindings[i].gameObject = player.gameObject;
						}
						else
						{
							ACDebug.LogWarning ("Cannot bind timeline track to Player, because no Player was found!", runtimeDirector);
						}
					}
					else
					{
						newBindings[i].gameObject = AssignFile (parameters, newBindings[i].parameterID, newBindings[i].constantID, newBindings[i].gameObject);
					}
				}
			}
		}
		
		
		public override float Run ()
		{
			#if ACIgnoreTimeline
			return 0f;
			#endif

			if (!isRunning)
			{
				if (runtimeDirector)
				{
					switch (method)
					{
						case ActionDirectorMethod.Play:
							{
								isRunning = true;

								if (restart)
								{
									PrepareDirector ();

									runtimeDirector.time = 0f;
									runtimeDirector.Play ();
								}
								else
								{
									runtimeDirector.Resume ();
								}

								if (willWait)
								{
									if (disableCamera)
									{
										KickStarter.mainCamera.Disable ();
									}
									isRunning = true;

									lastFrameDuration = runtimeDirector.time;
									return defaultPauseTime;
								}
							}
							break;

						case ActionDirectorMethod.Stop:
							{
								if (disableCamera)
								{
									KickStarter.mainCamera.Enable ();
								}

								if (pause)
								{
									runtimeDirector.Pause ();
								}
								else
								{
									PrepareDirectorEnd ();

									runtimeDirector.time = runtimeDirector.duration;
									runtimeDirector.Stop ();
								}
							}
							break;

						default:
							break;
					}
				}
			}
			else
			{
				if (KickStarter.stateHandler.ApplicationIsPaused ())
				{
					return defaultPauseTime;
				}

				if (runtimeDirector.state != PlayState.Paused || (!KickStarter.stateHandler.ApplicationIsInFocus () && !Application.runInBackground))
				{
					if (lastFrameDuration <= runtimeDirector.time && lastFrameDuration < runtimeDirector.duration)
					{
						lastFrameDuration = runtimeDirector.time;
						return defaultPauseTime;
					}
				}

				if (disableCamera)
				{
					KickStarter.mainCamera.Enable ();
				}

				PrepareDirectorEnd ();
				isRunning = false;
			}

			return 0f;
		}


		public override void Skip ()
		{
			#if ACIgnoreTimeline
			return;
			#endif

			if (runtimeDirector != null)
			{
				if (disableCamera)
				{
					KickStarter.mainCamera.Enable ();
				}

				if (!isRunning)
				{
					PrepareDirector ();
				}

				switch (method)
				{
					case ActionDirectorMethod.Play:
						if (runtimeDirector.extrapolationMode == DirectorWrapMode.Loop)
						{
							if (restart)
							{
								runtimeDirector.Play ();
							}
							else
							{
								runtimeDirector.Resume ();
							}
							return;
						}

						if (!isRunning)
						{
							runtimeDirector.time = runtimeDirector.duration;
							runtimeDirector.Evaluate ();
							runtimeDirector.Stop ();
						}

						PrepareDirectorEnd ();

						runtimeDirector.time = runtimeDirector.duration;
						runtimeDirector.Evaluate ();
						runtimeDirector.Stop ();
						break;

					case ActionDirectorMethod.Stop:
						if (pause)
						{
							runtimeDirector.Pause ();
						}
						else
						{
							runtimeDirector.Stop ();
						}
						break;

					default:
						break;
				}
			}
		}


		public override void Reset (ActionList actionList)
		{
			if (isRunning)
			{
				Skip ();
				isRunning = false;
			}
		}

		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			#if ACIgnoreTimeline
			EditorGUILayout.HelpBox ("This Action requires Timeline to be installed.", MessageType.Warning);
			#else			
			
			directorParameterID = Action.ChooseParameterGUI ("Director:", parameters, directorParameterID, ParameterType.GameObject);
			if (directorParameterID >= 0)
			{
				directorConstantID = 0;
				director = null;
			}
			else
			{
				director = (PlayableDirector) EditorGUILayout.ObjectField ("Director:", director, typeof (PlayableDirector), true);
				
				directorConstantID = FieldToID <PlayableDirector> (director, directorConstantID);
				director = IDToField <PlayableDirector> (director, directorConstantID, false);
			}

			method = (ActionDirectorMethod) EditorGUILayout.EnumPopup ("Method:", method);

			if (director != null || directorParameterID >= 0)
			{
				if (method == ActionDirectorMethod.Play)
				{
					restart = EditorGUILayout.Toggle ("Play from beginning?", restart);
					if (restart)
					{
						newTimeline = (TimelineAsset) EditorGUILayout.ObjectField ("Timeline (optional):", newTimeline, typeof (TimelineAsset), false);
						updateBindings = EditorGUILayout.Toggle ("Remap bindings?", updateBindings);
						if (updateBindings)
						{
							if (newTimeline)
							{
								ShowBindingsUI (newTimeline, parameters);
							}
							else if (director != null && director.playableAsset != null)
							{
								ShowBindingsUI (director.playableAsset as TimelineAsset, parameters);
							}
							else
							{
								EditorGUILayout.HelpBox ("A Director or Timeline must be assigned in order to update bindings.", MessageType.Warning);
							}
						}
						else if (newTimeline != null)
						{
							EditorGUILayout.HelpBox ("The existing bindings will be transferred onto the new Timeline.", MessageType.Info);
						}
					}
					willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);

					if (willWait)
					{
						/*if (director && director.extrapolationMode == DirectorWrapMode.Loop)
						{
							EditorGUILayout.HelpBox ("Cannot wait if the Director's 'Wrap Mode' is set to 'Loop'", MessageType.Warning);
						}*/
						disableCamera = EditorGUILayout.Toggle ("Disable AC camera?", disableCamera);
					}
				}
				else if (method == ActionDirectorMethod.Stop)
				{
					pause = EditorGUILayout.Toggle ("Pause timeline?", pause);
					disableCamera = EditorGUILayout.Toggle ("Enable AC camera?", disableCamera);
				}
			}

			#endif
		}


		#if !ACIgnoreTimeline
		protected int rebindTrackIndex;
		protected void ShowBindingsUI (TimelineAsset timelineAsset, List<ActionParameter> parameters)
		{
			if (timelineAsset == null) return;

			if (newBindings == null || timelineAsset.outputTrackCount != newBindings.Length)
			{
				BindingData[] tempBindings = new BindingData[newBindings.Length];
				for (int i=0; i<newBindings.Length; i++)
				{
					tempBindings[i] = new BindingData (newBindings[i]);
				}

				newBindings = new BindingData[timelineAsset.outputTrackCount];
				for (int i=0; i<newBindings.Length; i++)
				{
					if (i < tempBindings.Length)
					{
						newBindings[i] = new BindingData (tempBindings[i]);
					}
					else
					{
						newBindings[i] = new BindingData ();
					}
				}
			}

			string[] popUpLabels = new string[newBindings.Length];
			for (int i=0; i<newBindings.Length; i++)
			{
				string trackName = (timelineAsset.GetOutputTrack (i) != null) ? timelineAsset.GetOutputTrack (i).name : " Track";
				if (string.IsNullOrEmpty (trackName))
				{
					trackName = " Unnamed";
				}
				popUpLabels[i] = "#" + i.ToString () + ": " + trackName;
			}

			EditorGUILayout.BeginVertical (CustomStyles.thinBox);
			rebindTrackIndex = EditorGUILayout.Popup ("Select a track:", rebindTrackIndex, popUpLabels);
			ShowBindingUI (rebindTrackIndex, parameters);

			if (newBindings.Length > 1)
			{
				EditorGUILayout.HelpBox ("All bindings will be affected - not just the one selected above.", MessageType.Info);
			}
			CustomGUILayout.EndVertical ();
		}


		protected void ShowBindingUI (int i, List<ActionParameter> parameters)
		{
			if (newBindings == null || newBindings.Length <= i) return;
			
			newBindings[i].isPlayer = EditorGUILayout.Toggle ("Bind to Player?", newBindings[i].isPlayer);
			if (newBindings[i].isPlayer)
			{
				if (KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
				{
					newBindings[i].parameterID = ChooseParameterGUI ("Player ID:", parameters, newBindings[i].parameterID, ParameterType.Integer);
					if (newBindings[i].parameterID < 0)
						newBindings[i].playerID = ChoosePlayerGUI (newBindings[i].playerID, true);
				}
			}
			else
			{
				newBindings[i].parameterID = Action.ChooseParameterGUI ("Bind to:", parameters, newBindings[i].parameterID, ParameterType.GameObject);
				if (newBindings[i].parameterID >= 0)
				{
					newBindings[i].constantID = 0;
					newBindings[i].gameObject = null;
				}
				else
				{
					newBindings[i].gameObject = (GameObject) EditorGUILayout.ObjectField ("Bind to:", newBindings[i].gameObject, typeof (GameObject), true);

					newBindings[i].constantID = FieldToID (newBindings[i].gameObject, newBindings[i].constantID);
					newBindings[i].gameObject = IDToField (newBindings[i].gameObject, newBindings[i].constantID, false);
				}
			}
		}


		public TimelineAsset GetTimelineAsset ()
		{
			if (method == ActionDirectorMethod.Play && restart)
			{
				return newTimeline;
			}
			return null;
		}
		#endif

		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberTimeline> (director);
			}
			AssignConstantID <PlayableDirector> (director, directorConstantID, directorParameterID);

			if (updateBindings && newBindings != null && newBindings.Length > 0)
			{
				for (int i=0; i<newBindings.Length; i++)
				{
					if (newBindings[i].gameObject != null)
					{
						if (saveScriptsToo)
						{
							AddSaveScript <ConstantID> (newBindings[i].gameObject);
						}
						AssignConstantID (newBindings[i].gameObject, newBindings[i].constantID, newBindings[i].parameterID);
					}
				}
			}
		}

		
		public override string SetLabel ()
		{
			if (director != null)
			{
				return method.ToString () + " " + director.gameObject.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (directorParameterID < 0)
			{
				if (director && director.gameObject == gameObject) return true;
				if (directorConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}


		public override bool ReferencesPlayer (int _playerID = -1)
		{
			if (updateBindings)
			{
				foreach (BindingData newBinding in newBindings)
				{
					if (newBinding.isPlayer)
					{
						if (_playerID < 0) return true;
						if (newBinding.playerID < 0) return true;
						if (newBinding.playerID == _playerID) return true;
					}
				}
			}
			return false;
		}

		#endif


		protected void PrepareDirector ()
		{
			#if !ACIgnoreTimeline

			if (newTimeline)
			{
				if (runtimeDirector.playableAsset != null && runtimeDirector.playableAsset is TimelineAsset)
				{
					TimelineAsset oldTimeline = (TimelineAsset) runtimeDirector.playableAsset;
					GameObject[] transferBindings = new GameObject[oldTimeline.outputTrackCount];
					for (int i=0; i<transferBindings.Length; i++)
					{
						TrackAsset trackAsset = oldTimeline.GetOutputTrack (i);
						transferBindings[i] = runtimeDirector.GetGenericBinding (trackAsset) as GameObject;
					}

					runtimeDirector.playableAsset = newTimeline;

					for (int i=0; i<transferBindings.Length; i++)
					{
						if (transferBindings[i] != null)
						{
							var track = newTimeline.GetOutputTrack (i);
							if (track != null)
							{
								runtimeDirector.SetGenericBinding (track, transferBindings[i].gameObject);
							}
						}
					}
				}
				else
				{
					runtimeDirector.playableAsset = newTimeline;
				}
			}

			TimelineAsset timelineAsset = runtimeDirector.playableAsset as TimelineAsset;
			if (timelineAsset)
			{
				for (int i=0; i<timelineAsset.outputTrackCount; i++)
				{
					TrackAsset trackAsset = timelineAsset.GetOutputTrack (i);

					if (updateBindings && newBindings != null && i < newBindings.Length && newBindings[i] != null)
					{
						if (trackAsset != null && newBindings[i].gameObject != null)
						{
							SpeechTrack speechTrackAsset = trackAsset as SpeechTrack;
							if (speechTrackAsset != null)
							{
								speechTrackAsset.isPlayerLine = newBindings[i].isPlayer;
								speechTrackAsset.playerID = newBindings[i].playerID;
								speechTrackAsset.speakerObject = newBindings[i].gameObject;
							}
							else
							{
								runtimeDirector.SetGenericBinding (trackAsset, newBindings[i].gameObject);
							}
						}
					}

					GameObject bindingObject = runtimeDirector.GetGenericBinding (trackAsset) as GameObject;
					if (bindingObject == null)
					{
						Animator bindingAnimator = runtimeDirector.GetGenericBinding (trackAsset) as Animator;
						if (bindingAnimator != null)
						{
							bindingObject = bindingAnimator.gameObject;
						}
					}

					if (bindingObject)
					{
						Char bindingObjectChar = bindingObject.GetComponent <Char>();
						if (bindingObjectChar)
						{
							bindingObjectChar.OnEnterTimeline (runtimeDirector, i);
						}
					}
				}

				#if UNITY_EDITOR
				HashSet<GameObject> referencedChars = new HashSet<GameObject> ();
				for (int i=0; i<timelineAsset.outputTrackCount; i++)
				{
					TrackAsset trackAsset = timelineAsset.GetOutputTrack (i);
					if (trackAsset != null)
					{
						SpeechTrack speechTrackAsset = trackAsset as SpeechTrack;
						if (speechTrackAsset)
						{
							GameObject speakerOb = speechTrackAsset.speakerObject;
							if (speechTrackAsset.isPlayerLine)
							{
								if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && speechTrackAsset.playerID >= 0)
								{
									PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (speechTrackAsset.playerID);
									if (playerPrefab != null)
									{
										Player _player = playerPrefab.GetSceneInstance ();
										if (_player)
										{
											speakerOb = _player.gameObject;
										}
									}
								}
								else if (KickStarter.player)
								{
									speakerOb = KickStarter.player.gameObject;
								}
							}
							else if (speakerOb && speechTrackAsset.speakerConstantID != 0)
							{
								Char _char = ConstantID.GetComponent<Char> (speechTrackAsset.speakerConstantID);
								if (_char) speakerOb = _char.gameObject;
							}

							if (speakerOb)
							{
								if (referencedChars.Contains (speakerOb))
								{
									LogWarning ("Multiple Speech tracks in the Timeline " + timelineAsset + " reference the same character, " + speakerOb + ".  This is not allowed - a character must only have one Speech Track per Timeline");
								}
								else
								{
									referencedChars.Add (speakerOb);
								}
							}
						}
					}
				}
				#endif
			}

			#endif
		}


		protected void PrepareDirectorEnd ()
		{
			#if !ACIgnoreTimeline
			TimelineAsset timelineAsset = runtimeDirector.playableAsset as TimelineAsset;
			if (timelineAsset != null)
			{
				for (int i=0; i<timelineAsset.outputTrackCount; i++)
				{
					TrackAsset trackAsset = timelineAsset.GetOutputTrack (i);

					GameObject bindingObject = runtimeDirector.GetGenericBinding (trackAsset) as GameObject;
					if (bindingObject == null)
					{
						Animator bindingAnimator = runtimeDirector.GetGenericBinding (trackAsset) as Animator;
						if (bindingAnimator != null)
						{
							bindingObject = bindingAnimator.gameObject;
						}
					}

					if (bindingObject != null)
					{
						Char bindingObjectChar = bindingObject.GetComponent <Char>();
						if (bindingObjectChar != null)
						{
							bindingObjectChar.OnExitTimeline (runtimeDirector, i);
						}
					}
	            }
			}
			#endif
		}


		[System.Serializable]
		protected class BindingData
		{

			public GameObject gameObject;
			public bool isPlayer;
			public int playerID = -1;
			public int constantID;
			public int parameterID = -1;


			public BindingData ()
			{
				gameObject = null;
				isPlayer = false;
				constantID = 0;
				parameterID = -1;
				playerID = -1;
			}


			public BindingData (BindingData bindingData)
			{
				gameObject = bindingData.gameObject;
				isPlayer = bindingData.isPlayer;
				constantID = bindingData.constantID;
				parameterID = bindingData.parameterID;
				playerID = bindingData.playerID;
			}

		}


		#if !ACIgnoreTimeline

		/**
		 * <summary>Creates a new instance of the 'Engine: Control Timeline' Action, set to play a new Timeline</summary>
		 * <param name = "director">The PlayableDirector to play from</param>
		 * <param name = "timelineAsset">If set, the TimelineAsset to assign to the PlayableDirector</param>
		 * <param name = "playFromBeginning">If True, the Timeline will be played from the start</param>
		 * <param name = "disableACCamera">If True, the AC MainCamera will be disabled</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the Timeline has finished playing</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTimeline CreateNew_Play (PlayableDirector director, TimelineAsset timelineAsset = null, bool playFromBeginning = true, bool disableACCamera = false, bool waitUntilFinish = false)
		{
			ActionTimeline newAction = CreateNew<ActionTimeline> ();
			newAction.method = ActionDirectorMethod.Play;
			newAction.director = director;
			newAction.TryAssignConstantID (newAction.director, ref newAction.directorConstantID);
			newAction.newTimeline = timelineAsset;
			newAction.restart = (timelineAsset != null) ? playFromBeginning : false;
			newAction.disableCamera = disableACCamera;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Engine: Control Timeline' Action, set to stop a currently-playing Timeline</summary>
		 * <param name = "director">The PlayableDirector to stop</param>
		 * <param name = "pauseTimeline">If True, the Timeline will be paused as opposed to stopped and reset</param>
		 * <param name = "disableACCamera">If True, the AC MainCamera will be enabled</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTimeline CreateNew_Stop (PlayableDirector director, bool pauseTimeline = true, bool enableACCamera = true)
		{
			ActionTimeline newAction = CreateNew<ActionTimeline> ();
			newAction.method = ActionDirectorMethod.Stop;
			newAction.director = director;
			newAction.TryAssignConstantID (newAction.director, ref newAction.directorConstantID);
			newAction.pause = pauseTimeline;
			newAction.disableCamera = enableACCamera;
			return newAction;
		}

	}
	
}