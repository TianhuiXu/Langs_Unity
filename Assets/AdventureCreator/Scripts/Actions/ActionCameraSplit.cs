/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionCameraSplit.cs"
 * 
 *	This Action splits the screen horizontally or vertically.
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
	public class ActionCameraSplit : Action
	{

		public int parameterID1 = -1;
		public int parameterID2 = -1;

		public int constantID1 = 0;
		public int constantID2 = 0;

		public float splitAmount1 = 0.49f;
		public float splitAmount2 = 0.49f;

		public _Camera cam1;
		public _Camera cam2;

		protected _Camera runtimeCam1;
		protected _Camera runtimeCam2;

		public Rect overlayRect = new Rect (0.5f, 0.5f, 0.5f, 0.5f);

		public bool turnOff;
		public CameraSplitOrientation orientation;
		public bool mainIsTopLeft;

		
		public override ActionCategory Category { get { return ActionCategory.Camera; }}
		public override string Title { get { return "Split-screen"; }}
		public override string Description { get { return "Displays two cameras on the screen at once, arranged either horizontally or vertically. Which camera is the 'main' (i.e. which one responds to mouse clicks) can also be set."; }}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeCam1 = AssignFile <_Camera> (parameters, parameterID1, constantID1, cam1);
			runtimeCam2 = AssignFile <_Camera> (parameters, parameterID2, constantID2, cam2);
		}
		
		
		public override float Run ()
		{
			MainCamera mainCamera = KickStarter.mainCamera;
			mainCamera.RemoveSplitScreen ();

			if (turnOff || runtimeCam1 == null || runtimeCam2 == null)
			{
				return 0f;
			}

			if (orientation == CameraSplitOrientation.Overlay)
			{
				mainCamera.SetBoxOverlay (runtimeCam1, runtimeCam2, overlayRect);
			}
			else
			{
				if (splitAmount1 + splitAmount2 > 1f)
				{
					splitAmount2 = 1f - splitAmount1;
				}

				if (mainIsTopLeft)
				{
					mainCamera.SetSplitScreen (runtimeCam1, runtimeCam2, orientation, mainIsTopLeft, splitAmount1, splitAmount2);
				}
				else
				{
					mainCamera.SetSplitScreen (runtimeCam2, runtimeCam1, orientation, mainIsTopLeft, splitAmount1, splitAmount2);
				}
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			turnOff = EditorGUILayout.Toggle ("Just disable previous?", turnOff);
			if (!turnOff)
			{
				string label1 = string.Empty;
				string label2 = string.Empty;

				orientation = (CameraSplitOrientation) EditorGUILayout.EnumPopup ("Orientation:", orientation);

				switch (orientation)
				{
					case CameraSplitOrientation.Horizontal:
						label1 = "Top";
						label2 = "Bottom";
						break;

					case CameraSplitOrientation.Vertical:
						label1 = "Left";
						label2 = "Right";
						break;

					case CameraSplitOrientation.Overlay:
						label1 = "Underlay";
						label2 = "Overlay";
						break;
				}

				parameterID1 = Action.ChooseParameterGUI (label1 + " camera:", parameters, parameterID1, ParameterType.GameObject);
				if (parameterID1 >= 0)
				{
					constantID1 = 0;
					cam1 = null;
				}
				else
				{
					cam1 = (_Camera) EditorGUILayout.ObjectField (label1 + " camera:", cam1, typeof (_Camera), true);
					
					constantID1 = FieldToID <_Camera> (cam1, constantID1);
					cam1 = IDToField <_Camera> (cam1, constantID1, false);
				}

				if (orientation != CameraSplitOrientation.Overlay)
				{
					splitAmount1 = EditorGUILayout.Slider (label1 + " camera space:", splitAmount1, 0f, 1f);
				}

				parameterID2 = Action.ChooseParameterGUI (label2 + " camera:", parameters, parameterID2, ParameterType.GameObject);
				if (parameterID2 >= 0)
				{
					constantID2 = 0;
					cam2 = null;
				}
				else
				{
					cam2 = (_Camera) EditorGUILayout.ObjectField (label2 + " camera:", cam2, typeof (_Camera), true);
					
					constantID2 = FieldToID <_Camera> (cam2, constantID2);
					cam2 = IDToField <_Camera> (cam2, constantID2, false);
				}

				if (orientation == CameraSplitOrientation.Overlay)
				{
					overlayRect.x = EditorGUILayout.Slider ("Centre X:", overlayRect.x, 0f, 1f);
					overlayRect.y = EditorGUILayout.Slider ("Centre Y:", overlayRect.y, 0f, 1f);

					overlayRect.width = EditorGUILayout.Slider ("Size X:", overlayRect.width, 0f, 1f);
					overlayRect.height = EditorGUILayout.Slider ("Size Y:", overlayRect.height, 0f, 1f);
				}
				else
				{
					splitAmount2 = Mathf.Min (splitAmount2, 1f-splitAmount1);
					splitAmount2 = EditorGUILayout.Slider (label2 + " camera space:", splitAmount2, 0f, 1f);
					splitAmount1 = Mathf.Min (splitAmount1, 1f-splitAmount2);

					mainIsTopLeft = EditorGUILayout.Toggle ("Main Camera is " + label1.ToLower () + "?", mainIsTopLeft);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <ConstantID> (cam1);
				AddSaveScript <ConstantID> (cam2);
			}

			AssignConstantID <_Camera> (cam1, constantID1, parameterID1);
			AssignConstantID <_Camera> (cam2, constantID2, parameterID2);
		}
		
		
		public override string SetLabel ()
		{
			return orientation.ToString ();
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID1 < 0)
			{
				if (cam1 && cam1.gameObject == _gameObject) return true;
				if (constantID1 == id) return true;
			}
			if (parameterID2 < 0)
			{
				if (cam2 && cam2.gameObject == _gameObject) return true;
				if (constantID2 == id) return true;
			}
			return base.ReferencesObjectOrID (_gameObject, id);
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Camera: Split-screen' Action, set to overlay one camera over another</summary>
		 * <param name = "underlayCamera">The camera to display full-screen underneath</param>
		 * <param name = "overlayCamera">The camera to display on top</param>
		 * <param name = "overlayRect">The portion of the screen that the overlay camera is drawn in</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCameraSplit CreateNew_Overlay (_Camera underlayCamera, _Camera overlayCamera, Rect overlayRect)
		{
			ActionCameraSplit newAction = CreateNew<ActionCameraSplit> ();
			newAction.orientation = CameraSplitOrientation.Overlay;
			newAction.cam1 = underlayCamera;
			newAction.TryAssignConstantID (newAction.cam1, ref newAction.constantID1);
			newAction.cam2 = overlayCamera;
			newAction.TryAssignConstantID (newAction.cam2, ref newAction.constantID2);
			newAction.overlayRect = overlayRect;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Camera: Split-screen' Action, set to arrange two cameras above and below each other</summary>
		 * <param name = "topCamera">The camera to at the top</param>
		 * <param name = "bottomCamera">The camera to display at the bottom</param>
		 * <param name = "topIsActive">If True, the top camera will be interactive. Otherwise, the bottom camera will be</param>
		 * <param name = "topCameraSpace">The proportion of the screen's height that the top camera takes up</param>
		 * <param name = "bottomCameraSpace">The proportion of the screen's height that the bottom camera takes up</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCameraSplit CreateNew_AboveAndBelow (_Camera topCamera, _Camera bottomCamera, bool topIsActive = true, float topCameraSpace = 0.49f, float bottomCameraSpace = 0.49f)
		{
			ActionCameraSplit newAction = CreateNew<ActionCameraSplit> ();
			newAction.orientation = CameraSplitOrientation.Horizontal;
			newAction.cam1 = topCamera;
			newAction.TryAssignConstantID (newAction.cam1, ref newAction.constantID1);
			newAction.cam2 = bottomCamera;
			newAction.TryAssignConstantID (newAction.cam2, ref newAction.constantID2);
			newAction.mainIsTopLeft = topIsActive;
			newAction.splitAmount1 = topCameraSpace;
			newAction.splitAmount2 = bottomCameraSpace;
			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Camera: Split-screen' Action, set to arrange two cameras side by side</summary>
		 * <param name = "leftCamera">The camera to on the left</param>
		 * <param name = "rightCamera">The camera to display on the right</param>
		 * <param name = "leftIsActive">If True, the left camera will be interactive. Otherwise, the right camera will be</param>
		 * <param name = "leftCameraSpace">The proportion of the screen's width that the left camera takes up</param>
		 * <param name = "rightCameraSpace">The proportion of the screen's width that the right camera takes up</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionCameraSplit CreateNew_SideBySide (_Camera leftCamera, _Camera rightCamera, bool leftIsActive = true, float leftCameraSpace = 0.49f, float rightCameraSpace = 0.49f)
		{
			ActionCameraSplit newAction = CreateNew<ActionCameraSplit> ();
			newAction.orientation = CameraSplitOrientation.Vertical;
			newAction.cam1 = leftCamera;
			newAction.cam2 = rightCamera;
			newAction.mainIsTopLeft = leftIsActive;
			newAction.splitAmount1 = leftCameraSpace;
			newAction.splitAmount2 = rightCameraSpace;
			return newAction;
		}

	}

}