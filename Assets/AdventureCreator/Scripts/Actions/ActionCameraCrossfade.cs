/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCameraCrossfade.cs"
 * 
 *	This action crossfades the MainCamera from one
 *	GameCamera to another.
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
	public class ActionCameraCrossfade : Action
	{

		public int parameterID = -1;
		public int constantID = 0;
		public _Camera linkedCamera;
		protected _Camera runtimeLinkedCamera;

		public float transitionTime;
		public int transitionTimeParameterID = -1;
		public AnimationCurve fadeCurve = new AnimationCurve (new Keyframe(0, 0, 1, 1), new Keyframe(1, 1, 1, 1));

		public bool returnToLast;


		public override ActionCategory Category { get { return ActionCategory.Camera; }}
		public override string Title { get { return "Crossfade"; }}
		public override string Description { get { return "Crossfades the camera from its current GameCamera to a new one, over a specified time."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeLinkedCamera = AssignFile <_Camera> (parameters, parameterID, constantID, linkedCamera);
			transitionTime = AssignFloat (parameters, transitionTimeParameterID, transitionTime);

			if (returnToLast)
			{
				runtimeLinkedCamera = KickStarter.mainCamera.GetLastGameplayCamera ();
			}
		}

		
		public override float Run ()
		{
			if (!isRunning)
			{
				isRunning = true;
				MainCamera mainCam = KickStarter.mainCamera;

				if (runtimeLinkedCamera && mainCam.attachedCamera != runtimeLinkedCamera)
				{
					if (runtimeLinkedCamera is GameCameraAnimated)
					{
						GameCameraAnimated animCam = (GameCameraAnimated) runtimeLinkedCamera;
						animCam.PlayClip ();
					}
					
					runtimeLinkedCamera.MoveCameraInstant ();
					mainCam.Crossfade (transitionTime, runtimeLinkedCamera, fadeCurve);
						
					if (transitionTime > 0f && willWait)
					{
						return (transitionTime);
					}
				}
			}
			else
			{
				isRunning = false;
			}
			
			return 0f;
		}
		
		
		public override void Skip ()
		{
			MainCamera mainCam = KickStarter.mainCamera;

			if (runtimeLinkedCamera && mainCam.attachedCamera != runtimeLinkedCamera)
			{
				if (runtimeLinkedCamera is GameCameraAnimated)
				{
					GameCameraAnimated animCam = (GameCameraAnimated) runtimeLinkedCamera;
					animCam.PlayClip ();
				}
				
				runtimeLinkedCamera.MoveCameraInstant ();
				mainCam.SetGameCamera (runtimeLinkedCamera);
			}
		}


		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			returnToLast = EditorGUILayout.Toggle ("Return to last gameplay?", returnToLast);
			if (!returnToLast)
			{
				parameterID = Action.ChooseParameterGUI ("New camera:", parameters, parameterID, ParameterType.GameObject);
				if (parameterID >= 0)
				{
					constantID = 0;
					linkedCamera = null;
				}
				else
				{
					linkedCamera = (_Camera) EditorGUILayout.ObjectField ("New camera:", linkedCamera, typeof(_Camera), true);
					
					constantID = FieldToID <_Camera> (linkedCamera, constantID);
					linkedCamera = IDToField <_Camera> (linkedCamera, constantID, true);
				}
			}

			transitionTimeParameterID = Action.ChooseParameterGUI ("Transition time (s):", parameters, transitionTimeParameterID, ParameterType.Float);
			if (transitionTimeParameterID < 0)
			{
				transitionTime = EditorGUILayout.FloatField ("Transition time (s):", transitionTime);
			}

			fadeCurve = (AnimationCurve)EditorGUILayout.CurveField ("Transition curve:", fadeCurve);
			willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <ConstantID> (linkedCamera);
			}
			AssignConstantID <_Camera> (linkedCamera, constantID, parameterID);
		}

		
		public override string SetLabel ()
		{
			if (linkedCamera != null)
			{
				return linkedCamera.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!returnToLast && parameterID < 0)
			{
				if (linkedCamera && linkedCamera.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Camera: Crossfade' Action with key variables already set.</summary>
		 * <param name = "newCamera">The camera to crossfade to</param>
		 * <param name = "transitionTime">The time, in seconds, to take while transitioning to the new camera</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete<param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCameraCrossfade CreateNew (_Camera newCamera, float transitionTime = 1f, bool waitUntilFinish = false)
		{
			ActionCameraCrossfade newAction = CreateNew<ActionCameraCrossfade> ();
			newAction.linkedCamera = newCamera;
			newAction.TryAssignConstantID (newAction.linkedCamera, ref newAction.constantID);
			newAction.transitionTime = transitionTime;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}
		
	}

}