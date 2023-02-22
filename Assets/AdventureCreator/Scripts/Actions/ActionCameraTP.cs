/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCameraTP.cs"
 * 
 *	This action can rotate a GameCameraThirdPerson to a set rotation.
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
	public class ActionCameraTP : Action
	{

		public float newPitchAngle = 0f;
		public float newSpinAngle = 0f;
		public int newRotationParameterID = -1;
		private Vector3 newRotation;

		public GameCameraThirdPerson thirdPersonCamera = null;
		public int thirdPersonCameraConstantID = 0;
		public int thirdPersonCameraParameterID = -1;

		public enum NewTPCamMethod { SetLookAtOverride, ClearLookAtOverride, MoveToRotation, SnapToMainCamera };
		public NewTPCamMethod method = NewTPCamMethod.MoveToRotation;
		public Transform lookAtOverride = null;

		public int lookAtOverrideConstantID = 0;
		public int lookAtOverrideParameterID = -1;

		public float transitionTime = 0f;
		public int transitionTimeParameterID = -1;
		public bool isRelativeToTarget = false;


		public override ActionCategory Category { get { return ActionCategory.Camera; } }
		public override string Title { get { return "Rotate third-person"; } }
		public override string Description { get { return "Manipulates the new third-person camera"; } }


		override public void AssignValues (List<ActionParameter> parameters)
		{
			thirdPersonCamera = AssignFile (parameters, thirdPersonCameraParameterID, thirdPersonCameraConstantID, thirdPersonCamera);
			lookAtOverride = AssignFile (parameters, lookAtOverrideParameterID, lookAtOverrideConstantID, lookAtOverride);
			transitionTime = AssignFloat (parameters, transitionTimeParameterID, transitionTime);

			newRotation = new Vector3 (newSpinAngle, newPitchAngle, 0f);
			newRotation = AssignVector3 (parameters, newRotationParameterID, newRotation);
		}


		override public float Run ()
		{
			if (thirdPersonCamera)
			{
				if (!isRunning)
				{
					switch (method)
					{
						case NewTPCamMethod.SetLookAtOverride:
							thirdPersonCamera.SetLookAtOverride (lookAtOverride, transitionTime);
							break;

						case NewTPCamMethod.ClearLookAtOverride:
							thirdPersonCamera.ClearLookAtOverride (transitionTime);
							break;

						case NewTPCamMethod.MoveToRotation:
							thirdPersonCamera.BeginAutoMove (transitionTime, newRotation, isRelativeToTarget);
							if (transitionTime > 0f && willWait)
							{
								isRunning = true;
								return defaultPauseTime;
							}
							break;

						case NewTPCamMethod.SnapToMainCamera:
							thirdPersonCamera.SnapToDirection (Camera.main.transform.forward, Camera.main.transform.right);
							break;
					}
				}
				else
				{
					if (thirdPersonCamera.IsAutoMoving ())
					{
						return defaultPauseTime;
					}
					isRunning = false;
				}
			}
			return 0f;
		}


		#if UNITY_EDITOR

		override public void ShowGUI (List<ActionParameter> parameters)
		{
			thirdPersonCameraParameterID = Action.ChooseParameterGUI ("Third-person camera:", parameters, thirdPersonCameraParameterID, ParameterType.GameObject);
			if (thirdPersonCameraParameterID >= 0)
			{
				thirdPersonCameraConstantID = 0;
				thirdPersonCamera = null;
			}
			else
			{
				thirdPersonCamera = (GameCameraThirdPerson) EditorGUILayout.ObjectField ("Third-person camera:", thirdPersonCamera, typeof (GameCameraThirdPerson), true);

				thirdPersonCameraConstantID = FieldToID (thirdPersonCamera, thirdPersonCameraConstantID);
				thirdPersonCamera = IDToField (thirdPersonCamera, thirdPersonCameraConstantID, false);
			}

			method = (NewTPCamMethod) EditorGUILayout.EnumPopup ("Method:", method);

			if (method == NewTPCamMethod.MoveToRotation)
			{
				newRotationParameterID = Action.ChooseParameterGUI ("New rotation:", parameters, newRotationParameterID, ParameterType.Vector3);
				if (newRotationParameterID < 0)
				{
					newSpinAngle = EditorGUILayout.Slider ("New spin:", newSpinAngle, -180f, 180f);
					newPitchAngle = EditorGUILayout.Slider ("New pitch:", newPitchAngle, -80f, 80f);
				}

				isRelativeToTarget = EditorGUILayout.Toggle ("Spin relative to target?", isRelativeToTarget);

				transitionTimeParameterID = Action.ChooseParameterGUI ("Speed:", parameters, transitionTimeParameterID, ParameterType.Float);
				if (transitionTimeParameterID < 0)
				{
					transitionTime = EditorGUILayout.Slider ("Speed:", transitionTime, 0f, 10f);
				}
				if (transitionTimeParameterID < 0 || transitionTime > 0f)
				{
					willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
				}
			}
			else if (method == NewTPCamMethod.SetLookAtOverride || method == NewTPCamMethod.ClearLookAtOverride)
			{
				if (method == NewTPCamMethod.SetLookAtOverride)
				{
					lookAtOverrideParameterID = Action.ChooseParameterGUI ("Look-at Transform:", parameters, lookAtOverrideParameterID, ParameterType.GameObject);
					if (lookAtOverrideParameterID >= 0)
					{
						lookAtOverrideConstantID = 0;
						lookAtOverride = null;
					}
					else
					{
						lookAtOverride = (Transform) EditorGUILayout.ObjectField ("Look-at Transform:", lookAtOverride, typeof (Transform), true);

						lookAtOverrideConstantID = FieldToID (lookAtOverride, lookAtOverrideConstantID);
						lookAtOverride = IDToField (lookAtOverride, lookAtOverrideConstantID, false);
					}
				}

				transitionTimeParameterID = Action.ChooseParameterGUI ("Speed:", parameters, transitionTimeParameterID, ParameterType.Float);
				if (transitionTimeParameterID < 0)
				{
					transitionTime = EditorGUILayout.Slider ("Transition time (s):", transitionTime, 0f, 10f);
				}
			}
		}


		override public void AssignConstantIDs (bool saveScriptsToo = false, bool fromAssetFile = false)
		{
			AssignConstantID (lookAtOverride, lookAtOverrideConstantID, lookAtOverrideParameterID);
			AssignConstantID<GameCameraThirdPerson> (thirdPersonCamera, thirdPersonCameraConstantID, thirdPersonCameraParameterID);
		}


		override public string SetLabel ()
		{
			return method.ToString ();
		}

		#endif

	}

}