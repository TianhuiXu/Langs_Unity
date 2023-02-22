/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCamera.cs"
 * 
 *	This action controls the MainCamera's "activeCamera",
 *	i.e., which GameCamera it is attached to.
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
	public class ActionCamera : Action
	{
		
		public int constantID = 0;
		public int parameterID = -1;
		public _Camera linkedCamera;
		protected _Camera runtimeLinkedCamera;
		protected GameCameraAnimated runtimeLinkedCameraAnimated;

		public float transitionTime;
		public int transitionTimeParameterID = -1;

		public AnimationCurve timeCurve = new AnimationCurve (new Keyframe(0, 0), new Keyframe(1, 1));
		public MoveMethod moveMethod;
		public bool returnToLast;
        public bool retainPreviousSpeed = false;
		public bool waitForAnimation = false;

		public override ActionCategory Category { get { return ActionCategory.Camera; }}
		public override string Title { get { return "Switch"; }}
		public override string Description { get { return "Moves the MainCamera to the position, rotation and field of view of a specified GameCamera. Can be instantaneous or transition over time."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeLinkedCamera = AssignFile <_Camera> (parameters, parameterID, constantID, linkedCamera);
			transitionTime = AssignFloat (parameters, transitionTimeParameterID, transitionTime);
		}
		
		
		public override float Run ()
		{
			if (!isRunning)
			{
				MainCamera mainCam = KickStarter.mainCamera;
				
				if (mainCam)
				{
					_Camera cam = runtimeLinkedCamera;

					if (returnToLast)
					{
						cam = mainCam.GetLastGameplayCamera ();
					}

					if (cam)
					{
						if (mainCam.attachedCamera != cam)
						{
							if (cam is GameCameraAnimated)
							{
								GameCameraAnimated animCam = (GameCameraAnimated) cam;
								animCam.PlayClip ();
							}

							if (transitionTime > 0f && runtimeLinkedCamera is GameCamera25D)
							{
								mainCam.SetGameCamera (cam, 0f);
								LogWarning ("Switching to a 2.5D camera (" + runtimeLinkedCamera.name + ") must be instantaneous.");
							}
							else
							{
								mainCam.SetGameCamera (cam, transitionTime, moveMethod, timeCurve, retainPreviousSpeed);
								
								if (willWait)
								{
									runtimeLinkedCameraAnimated = runtimeLinkedCamera as GameCameraAnimated;
									if (transitionTime > 0f)
									{
										isRunning = true;
										return defaultPauseTime;
									}
									else
									{
										if (runtimeLinkedCameraAnimated && waitForAnimation)
										{
											isRunning = true;
											return defaultPauseTime;
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				if (runtimeLinkedCameraAnimated && waitForAnimation)
				{
					if (runtimeLinkedCameraAnimated.IsPlaying ())
					{
						return defaultPauseTime;
					}
					else
					{
						isRunning = false;
						return 0f;
					}
				}
				else
				{
					if (KickStarter.mainCamera.IsInTransition () && KickStarter.mainCamera.attachedCamera == runtimeLinkedCamera)
					{
						return defaultPauseTime;
					}
					else
					{
						isRunning = false;
						return 0f;
					}
				}
			}
			
			return 0f;
		}
		
		
		public override void Skip ()
		{
			MainCamera mainCam = KickStarter.mainCamera;
			if (mainCam)
			{
				_Camera cam = runtimeLinkedCamera;
				
				if (returnToLast)
				{
					cam = mainCam.GetLastGameplayCamera ();
				}
				
				if (cam)
				{
					cam.MoveCameraInstant ();
					mainCam.SetGameCamera (cam);
				}
			}
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			bool showWaitOption = false;
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
					linkedCamera = (_Camera) EditorGUILayout.ObjectField ("New camera:", linkedCamera, typeof (_Camera), true);
					
					constantID = FieldToID <_Camera> (linkedCamera, constantID);
					linkedCamera = IDToField <_Camera> (linkedCamera, constantID, true);
				}
			}
			
			if (linkedCamera is GameCamera25D && !returnToLast)
			{
				transitionTime = 0f;
			}
			else
			{
				transitionTimeParameterID = Action.ChooseParameterGUI ("Transition time (s):", parameters, transitionTimeParameterID, ParameterType.Float);
				if (transitionTimeParameterID < 0)
				{
					transitionTime = EditorGUILayout.FloatField ("Transition time (s):", transitionTime);
				}
				
				if (transitionTime > 0f || transitionTimeParameterID >= 0)
				{
					moveMethod = (MoveMethod) EditorGUILayout.EnumPopup ("Move method:", moveMethod);
					showWaitOption = true;
					
					if (moveMethod == MoveMethod.CustomCurve)
					{
						timeCurve = EditorGUILayout.CurveField ("Time curve:", timeCurve);
					}
					retainPreviousSpeed = EditorGUILayout.Toggle ("Smooth transition out?", retainPreviousSpeed);
				}
			}
			
			if (showWaitOption)
			{
				willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);

				if (willWait && !returnToLast && linkedCamera && linkedCamera is GameCameraAnimated)
				{
					GameCameraAnimated animatedCamera = (GameCameraAnimated) linkedCamera;
					if (animatedCamera.animatedCameraType == AnimatedCameraType.PlayWhenActive)
					{
						waitForAnimation = EditorGUILayout.Toggle ("Wait for animation?", waitForAnimation);
					}
				}
			}
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
			if (linkedCamera && !returnToLast)
			{
				return linkedCamera.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (linkedCamera && linkedCamera.gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Camera: Switch' Action with key variables already set.</summary>
		 * <param name = "newCamera">The camera to switch to</param>
		 * <param name = "duration">The time, in seconds, to take while transitioning to the new camera</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the transition is complete<param>
		 * <param name = "moveMethod">The method of moving from the current camera to the new camera, if transitionTime > 0</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCamera CreateNew (_Camera newCamera, float duration = 0f, bool waitUntilFinish = true, MoveMethod moveMethod = MoveMethod.Smooth)
		{
			ActionCamera newAction = CreateNew<ActionCamera> ();
			newAction.linkedCamera = newCamera;
			newAction.TryAssignConstantID (newAction.linkedCamera, ref newAction.constantID);
			newAction.transitionTime = duration;
			newAction.moveMethod = moveMethod;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

    }
	
}