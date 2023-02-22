/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberMoveable.cs"
 * 
 *	This script, when attached to Moveable objects in the scene,
 *	will record appropriate positional data
 * 
 */

using UnityEngine;

namespace AC
{

	/** This script is attached to Moveable, Draggable or PickUp objects you wish to save. */
	[AddComponentMenu("Adventure Creator/Save system/Remember Moveable")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_moveable.html")]
	public class RememberMoveable : Remember
	{

		#region Variables

		/** Determines whether the object is on or off when the game starts */
		public AC_OnOff startState = AC_OnOff.On;

		#endregion


		#region UnityStandards

		protected override void Start ()
		{
			base.Start ();

			if (loadedData) return;

			if (KickStarter.settingsManager && GameIsPlaying () && isActiveAndEnabled)
			{
				DragBase dragBase = GetComponent <DragBase>();
				if (dragBase)
				{
					if (startState == AC_OnOff.On)
					{
						dragBase.TurnOn ();
					}
					else
					{
						dragBase.TurnOff ();
					}
				}

				if (startState == AC_OnOff.On)
				{
					gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
				}
				else
				{
					gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
				}
			}
		}


		public override string SaveData ()
		{
			MoveableData moveableData = new MoveableData ();
			
			moveableData.objectID = constantID;
			moveableData.savePrevented = savePrevented;

			Moveable_Drag moveable_Drag = GetComponent <Moveable_Drag>();
			if (moveable_Drag)
			{
				moveableData.isOn = moveable_Drag.IsOn ();
				moveableData.trackID = 0;
				if (moveable_Drag.dragMode == DragMode.LockToTrack && moveable_Drag.track && moveable_Drag.track.GetComponent<ConstantID>())
				{
					moveableData.trackID = moveable_Drag.track.GetComponent<ConstantID>().constantID;
				}
				moveableData.trackValue = moveable_Drag.trackValue;
				moveableData.revolutions = moveable_Drag.revolutions;
			}
			else
			{
				moveableData.isOn = (gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer));
			}
			
			moveableData.LocX = transform.position.x;
			moveableData.LocY = transform.position.y;
			moveableData.LocZ = transform.position.z;

			moveableData.RotX = transform.eulerAngles.x;
			moveableData.RotY = transform.eulerAngles.y;
			moveableData.RotZ = transform.eulerAngles.z;
			
			moveableData.ScaleX = transform.localScale.x;
			moveableData.ScaleY = transform.localScale.y;
			moveableData.ScaleZ = transform.localScale.z;

			Moveable moveable = GetComponent <Moveable>();
			if (moveable)
			{
				moveableData = moveable.SaveData (moveableData);
			}

			return Serializer.SaveScriptData <MoveableData> (moveableData);
		}
		

		public override void LoadData (string stringData)
		{
			MoveableData data = Serializer.LoadScriptData <MoveableData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			DragBase dragBase = GetComponent <DragBase>();
			if (dragBase)
			{
				if (data.isOn)
				{
					dragBase.TurnOn ();
				}
				else
				{
					dragBase.TurnOff ();
				}
			}

			if (data.isOn)
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
			}
			else
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
			}

			transform.position = new Vector3 (data.LocX, data.LocY, data.LocZ);
			transform.eulerAngles = new Vector3 (data.RotX, data.RotY, data.RotZ);
			transform.localScale = new Vector3 (data.ScaleX, data.ScaleY, data.ScaleZ);

			Moveable_Drag moveable_Drag = GetComponent <Moveable_Drag>();
			if (moveable_Drag)
			{
				if (moveable_Drag.IsHeld)
				{
					moveable_Drag.LetGo ();
				}
				if (moveable_Drag.dragMode == DragMode.LockToTrack)
				{
					DragTrack dragTrack = ConstantID.GetComponent<DragTrack> (data.trackID);
					if (dragTrack)
					{
						moveable_Drag.SnapToTrack (dragTrack, data.trackValue);
					}

					if (moveable_Drag.track)
					{
						moveable_Drag.trackValue = data.trackValue;
						moveable_Drag.revolutions = data.revolutions;
						moveable_Drag.StopAutoMove ();
						moveable_Drag.track.SetPositionAlong (data.trackValue, moveable_Drag);
					}
				}
			}

			Moveable moveable = GetComponent <Moveable>();
			if (moveable)
			{
				moveable.LoadData (data);
			}

			loadedData = true;
		}

		#endregion

	}


	/** A data container used by the RememberMoveable script. */
	[System.Serializable]
	public class MoveableData : RememberData
	{

		/** True if the object is on */
		public bool isOn;

		/** The ConstantID value of the track it's attached to (if locked to a track) */
		public int trackID;

		/** How far along a DragTrack a Draggable object is (if locked to a track) */
		public float trackValue;
		/** If a Draggable object is locked to a DragTrack_Curved, how many revolutions it has made */
		public int revolutions;

		/** Its X position */
		public float LocX;
		/** Its Y position */
		public float LocY;
		/** Its Z position */
		public float LocZ;

		/** If True, the attached Moveable component is rotating with euler angles, not quaternions */
		public bool doEulerRotation;
		/** Its W rotation */
		public float RotW;
		/** Its X rotation */
		public float RotX;
		/** Its Y position */
		public float RotY;
		/** Its Z position */
		public float RotZ;

		/** Its X scale */
		public float ScaleX;
		/** Its Y scale */
		public float ScaleY;
		/** Its Z scale */
		public float ScaleZ;

		/** If True, the movement is occuring in world-space */
		public bool inWorldSpace;


		/** The default Constructor. */
		public MoveableData () { }
		
	}
	
}