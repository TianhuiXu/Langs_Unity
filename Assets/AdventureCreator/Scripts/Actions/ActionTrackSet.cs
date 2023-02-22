/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"ActionTrackSet.cs"
 * 
 *	This action is used to automatically move
 *	a draggable object along a track, provided
 *	it's already on one.
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
	public class ActionTrackSet : Action
	{
		
		public DragTrack newTrack;
		public int newTrackConstantID = 0;
		protected DragTrack runtimeNewTrack;

		public Moveable_Drag dragObject;
		public int dragParameterID = -1;
		public int dragConstantID = 0;
		protected Moveable_Drag runtimeDragObject;

		public float positionAlong;
		public int positionParameterID = -1;

		public float speed = 200f;
		public bool removePlayerControl = false;
		public bool isInstant;

		public bool stopOnCollide = false;
		public LayerMask layerMask;


		public override ActionCategory Category { get { return ActionCategory.Moveable; }}
		public override string Title { get { return "Set track position"; }}
		public override string Description { get { return "Moves a Draggable object along its track automatically to a specific point. The effect will be disabled once the object reaches the intended point, or the Action is run again with the speed value set as a negative number."; }}
		

		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeNewTrack = AssignFile <DragTrack> (newTrackConstantID, newTrack);
			runtimeDragObject = AssignFile <Moveable_Drag> (parameters, dragParameterID, dragConstantID, dragObject);

			positionAlong = AssignFloat (parameters, positionParameterID, positionAlong);
			positionAlong = Mathf.Max (0f, positionAlong);
			positionAlong = Mathf.Min (1f, positionAlong);
		}

		
		public override float Run ()
		{
			if (runtimeDragObject == null)
			{
				isRunning = false;
				return 0f;
			}

			if (runtimeNewTrack != null)
			{
				runtimeDragObject.SnapToTrack (runtimeNewTrack, positionAlong);
				isRunning = false;
				return 0f;
			}

			if (!isRunning)
			{
				isRunning = true;

				if (isInstant)
				{
					runtimeDragObject.AutoMoveAlongTrack (positionAlong, 0f, removePlayerControl);
				}
				else
				{
					if (stopOnCollide)
					{
						runtimeDragObject.AutoMoveAlongTrack (positionAlong, speed, removePlayerControl, layerMask);
					}
					else
					{
						runtimeDragObject.AutoMoveAlongTrack (positionAlong, speed, removePlayerControl);
					}
				}

				if (willWait && !isInstant && speed > 0f)
				{
					return defaultPauseTime;
				}
				isRunning = false;
				return 0f;
			}
			else
			{
				if (runtimeDragObject.IsAutoMoving (false))
				{
					return defaultPauseTime;
				}
				isRunning = false;
				return 0f;
			}
		}


		public override void Skip ()
		{
			if (runtimeDragObject == null)
			{
				return;
			}
			
			runtimeDragObject.AutoMoveAlongTrack (positionAlong, 0f, removePlayerControl);
		}


		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			dragParameterID = Action.ChooseParameterGUI ("Draggable object:", parameters, dragParameterID, ParameterType.GameObject);
			if (dragParameterID >= 0)
			{
				dragConstantID = 0;
				dragObject = null;
			}
			else
			{
				dragObject = (Moveable_Drag) EditorGUILayout.ObjectField ("Draggable object:", dragObject, typeof (Moveable_Drag), true);
				
				dragConstantID = FieldToID <Moveable_Drag> (dragObject, dragConstantID);
				dragObject = IDToField <Moveable_Drag> (dragObject, dragConstantID, false);

				if (dragObject != null && dragObject.dragMode != DragMode.LockToTrack)
				{
					EditorGUILayout.HelpBox ("The chosen Drag object must be in 'Lock To Track' mode", MessageType.Warning);
				}
			}

			newTrack = (DragTrack)EditorGUILayout.ObjectField("Track (optional):", newTrack, typeof(DragTrack), true);

			newTrackConstantID = FieldToID<DragTrack>(newTrack, newTrackConstantID);
			newTrack = IDToField<DragTrack>(newTrack, newTrackConstantID, false);

			positionParameterID = Action.ChooseParameterGUI ("New track position:", parameters, positionParameterID, ParameterType.Float);
			if (positionParameterID < 0)
			{
				positionAlong = EditorGUILayout.Slider ("New track position:", positionAlong, 0f, 1f);
			}

			if (newTrack == null)
			{
				isInstant = EditorGUILayout.Toggle ("Is instant?", isInstant);
				if (!isInstant)
				{
					speed = EditorGUILayout.FloatField ("Movement speed:", speed);
					removePlayerControl = EditorGUILayout.Toggle ("Remove player control?", removePlayerControl);
					stopOnCollide = EditorGUILayout.Toggle ("Stop if has collision?", stopOnCollide);
					if (stopOnCollide)
					{
						layerMask = AdvGame.LayerMaskField ("'Stop' collision layer(s):", layerMask);
					}

					willWait = EditorGUILayout.Toggle ("Wait until finish?", willWait);
				}
			}
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberMoveable> (dragObject);
			}
			AssignConstantID <Moveable_Drag> (dragObject, dragConstantID, dragParameterID);
		}
		

		public override string SetLabel ()
		{
			if (dragObject != null)
			{
				return dragObject.name + " to " + positionAlong;
			}
			return string.Empty;
		}

		
		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (dragParameterID < 0)
			{
				if (dragObject && dragObject.gameObject == gameObject) return true;
				if (dragConstantID == id && id != 0) return true;
			}
			return base.ReferencesObjectOrID (gameObject, id);
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Moveable: Set track position' Action</summary>
		 * <param name = "dragObject">The moveable object to move</param>
		 * <param name = "newTrackPosition">The moveable object's new target track position</param>
		 * <param name = "movementSpeed">How quickly to move the object</param>
		 * <param name = "removePlayerControl">If True, control will be taken away from the player if currently holding the object</param>
		 * <param name = "stopUponCollision">If True, the movement will cease if the object collides with another object</param>
		 * <param name = "collisionLayer">The layer to detect collisions with, if stopUponCollision = True</param>
		 * <param name = "waitUntilFinish">If True, then the Action will wait until the object has finished moving</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionTrackSet CreateNew (Moveable_Drag draggableObject, float newTrackPosition, float movementSpeed = 0f, bool removePlayerControl = false, bool stopUponCollision = false, LayerMask collisionLayer = new LayerMask (), bool waitUntilFinish = false)
		{
			ActionTrackSet newAction = CreateNew<ActionTrackSet> ();
			newAction.dragObject = draggableObject;
			newAction.TryAssignConstantID (newAction.dragObject, ref newAction.dragConstantID);
			newAction.positionAlong = newTrackPosition;
			newAction.isInstant = (movementSpeed <= 0f);
			newAction.speed = movementSpeed;
			newAction.removePlayerControl = removePlayerControl;
			newAction.stopOnCollide = stopUponCollision;
			newAction.layerMask = collisionLayer;
			newAction.willWait = waitUntilFinish;
			return newAction;
		}

	}

}