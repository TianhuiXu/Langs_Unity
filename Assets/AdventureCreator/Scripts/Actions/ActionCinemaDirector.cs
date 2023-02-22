/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCinemaDirector.cs"
 * 
 *	This action triggers Cinema Director Cutscenes
 * 
 */

 using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
	
	[System.Serializable]
	public class ActionCinemaDirector : Action
	{

		public bool disableCamera;
		public int constantID = 0;
		public int parameterID = -1;
		#if CinemaDirectorIsPresent
		public CinemaDirector.Cutscene cdCutscene;
		#endif


		public override ActionCategory Category { get { return ActionCategory.ThirdParty; }}
		public override string Title { get { return "Cinema Director"; }}
		public override string Description { get { return "Runs a Cutscene built with Cinema Director. Note that Cinema Director is a separate Unity Asset, and the 'CinemaDirectorIsPresent' preprocessor must be defined for this to work."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			#if CinemaDirectorIsPresent
			cdCutscene = AssignFile <CinemaDirector.Cutscene> (parameters, parameterID, constantID, cdCutscene);
			#endif
		}
		
		
		public override float Run ()
		{
			#if CinemaDirectorIsPresent
			if (!isRunning)
			{
				if (cdCutscene != null)
				{
					isRunning = true;
					cdCutscene.Play ();

					if (disableCamera)
					{
						KickStarter.mainCamera.Disable ();
					}

					if (willWait)
					{
						return cdCutscene.Duration;
					}
				}
			}
			else
			{
				if (disableCamera)
				{
					KickStarter.mainCamera.Enable ();
				}

				isRunning = false;
			}
			#endif
			
			return 0f;
		}


		public override void Skip ()
		{
			#if CinemaDirectorIsPresent
			if (cdCutscene != null)
			{
				if (disableCamera)
				{
					KickStarter.mainCamera.Enable ();
				}

				cdCutscene.Skip ();
				if (!cdCutscene.IsSkippable)
				{
					ACDebug.LogWarning ("Cannot skip Cinema Director cutscene " + cdCutscene.name, cdCutscene);
				}
			}
			#endif
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			#if CinemaDirectorIsPresent
			parameterID = Action.ChooseParameterGUI ("Director cutscene:", parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				cdCutscene = null;
			}
			else
			{
				cdCutscene = (CinemaDirector.Cutscene) EditorGUILayout.ObjectField ("Director cutscene:", cdCutscene, typeof (CinemaDirector.Cutscene), true);
				
				constantID = FieldToID <CinemaDirector.Cutscene> (cdCutscene, constantID);
				cdCutscene = IDToField <CinemaDirector.Cutscene> (cdCutscene, constantID, false);
			}

			disableCamera = EditorGUILayout.Toggle ("Override AC camera?", disableCamera);
			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
			#endif
			#if !CinemaDirectorIsPresent
			EditorGUILayout.HelpBox ("The 'CinemaDirectorIsPresent' Scripting Define Symbol must be listed in the\nPlayer Settings. Please set it from Edit -> Project Settings -> Player", MessageType.Warning);
			#endif
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			#if CinemaDirectorIsPresent
			AssignConstantID <CinemaDirector.Cutscene> (cdCutscene, constantID, parameterID);
			#endif
		}

		
		public override string SetLabel ()
		{
			#if CinemaDirectorIsPresent
			if (cdCutscene != null)
			{
				return cdCutscene.gameObject.name;
			}
			#endif
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				#if CinemaDirectorIsPresent
				if (cdCutscene && cdCutscene.gameObject == _gameObject) return true;
				#endif
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}

		#endif

		#if CinemaDirectorIsPresent

		/**
		* <summary>Creates a new instance of the 'Third Party: Cinema Director' Action</summary>
		* <param name = "cutsceneToPlay">The cutscene to play</param>
		* <param name = "waitUntilFinish">If True, the Action will wait until the cutscene has completed</param>
		* <returns>The generated Action</returns>
		*/
		public static ActionCinemaDirector CreateNew_ResumeLastTrack (CinemaDirector.Cutscene cutsceneToPlay, bool waitUntilFinish = true)
		{
			ActionCinemaDirector newAction = CreateNew<ActionCinemaDirector> ();
			newAction.cdCutscene = cutsceneToPlay;
			newAction.TryAssignConstantID (newAction.cdCutscene, ref newAction.constantID);
			newAction.willWait = waitUntilFinish;
		}

		#endif

	}
	
}