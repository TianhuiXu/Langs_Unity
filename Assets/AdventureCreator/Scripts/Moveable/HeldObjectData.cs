/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"HeldObjectData.cs"
 * 
 *	A data container class for information about how a draggable object is currently held
 * 
 */

using UnityEngine;

namespace AC
{

	/** A data container class for information about how a draggable object is currently held */
	public class HeldObjectData
	{

		#region Variables

		private DragBase dragBase;
		private Vector3 dragForce;
		private bool ignoreDragState;
		private int touchIndex = -1;
		private bool ignoreBuiltInDragInput;

		#endregion


		#region Constructors

		/**
		 * <summaryThe default constructor</summary>
		 * <param name="_dragBase">The object to drag</param>
		 */
		public HeldObjectData (DragBase _dragBase)
		{
			dragBase = _dragBase;
			touchIndex = -1;
			ignoreDragState = false;
			ignoreBuiltInDragInput = false;
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Attempts to release the object</summary>
		 * <param name = "force">If True, the object will be released for certain. Otherwise, it will only be released if natural conditions mean it should be.</param>
		 */
		public void AttemptRelease (bool force)
		{
			if (!force &&
				dragBase.IsHeld &&
				dragBase.IsOnScreen () &&
				dragBase.IsCloseToCamera (KickStarter.settingsManager.moveableRaycastLength * 1.25f))
			{
				if (ignoreDragState)
				{
					return;
				}
				if (touchIndex < 0 && KickStarter.playerInput.GetDragState () == DragState.Moveable)
				{
					return;
				}
				else if (touchIndex >= 0 && touchIndex < Input.touchCount)
				{
					return;
				}
			}
			dragBase.LetGo ();
		}


		/**
		 * <summary>Drags the held object</summary>
		 * <param name="deltaCamera">The change in camera position since the last frame</param>
		 * <param name="deltaInput">The change in input position since the last frame, in screen-space</param>
		 * <param name="inputPosition">The input position this frame, in screen-space</param>
		 */
		public void Drag (Vector3 deltaCamera, Vector2 deltaInput, Vector2 inputPosition)
		{
			if (touchIndex >= 0)
			{
				if (Input.touchCount > touchIndex)
				{
					Touch touch = Input.GetTouch (touchIndex);
					deltaInput = touch.deltaPosition;
					inputPosition = touch.position;
				}
				else
				{
					return;
				}
			}
			else if (dragBase.invertInput)
			{
				deltaInput = new Vector2 (-deltaInput.x, -deltaInput.y);
			}

			dragForce = (KickStarter.CameraMainTransform.right * deltaInput.x) + (KickStarter.CameraMainTransform.up * deltaInput.y);

			// Scale force with distance to camera, to lessen effects when close
			float distanceToCamera = (KickStarter.CameraMainTransform.position - dragBase.Transform.position).magnitude;
			
			// Incoporate camera movement
			if (dragBase.playerMovementInfluence > 0f)
			{
				dragForce += 100000f * dragBase.playerMovementInfluence * deltaCamera;
			}

			dragForce /= Time.fixedDeltaTime * 50f;
			dragBase.ApplyDragForce (dragForce, inputPosition, distanceToCamera);
		}

		#endregion


		#region GetSet

		/** The object being dragged */
		public DragBase DragObject
		{
			get
			{
				return dragBase;
			}
		}


		/** The current force being applied to the object */
		public Vector3 DragForce
		{
			get
			{
				return dragForce;
			}
		}


		/** If True, the object will not be released when the input or touch is released */
		public bool IgnoreDragState
		{
			get
			{
				return ignoreDragState;
			}
			set
			{
				ignoreDragState = value;
			}
		}


		/** If True, the object will not be moved by AC's built-in calls to the Drag function, allowing values to be set manually */
		public bool IgnoreBuiltInDragInput
		{
			get
			{
				return ignoreBuiltInDragInput;
			}
			set
			{
				ignoreBuiltInDragInput = value;
			}
		}


		/** The touch index to start dragging with, if on a touch-screen */
		public int TouchIndex
		{
			get
			{
				return touchIndex;
			}
			set
			{
				touchIndex = value;
			}
		}

		#endregion

	}

}
 