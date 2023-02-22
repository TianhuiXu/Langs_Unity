/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberNPC.cs"
 * 
 *	This script is attached to NPCs in the scene
 *	with path and transform data we wish to save.
 * 
 */

using UnityEngine;

namespace AC
{

	/** Attach this script to NPCs in the scene whose state you wish to save. */
	[AddComponentMenu("Adventure Creator/Save system/Remember NPC")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_n_p_c.html")]
	[RequireComponent (typeof (NPC))]
	public class RememberNPC : Remember
	{

		#region Variables

		/** Determines whether the object is on or off when the game starts */
		public AC_OnOff startState = AC_OnOff.On;
		private Hotspot ownHotspot;

		#endregion


		#region UnityStandards

		protected override void Start ()
		{
			base.Start ();

			if (loadedData) return;

			if (OwnHotspot != null &&
				GetComponent <RememberHotspot>() == null &&
				KickStarter.settingsManager &&
				GameIsPlaying ())
			{
				if (startState == AC_OnOff.On)
				{
					OwnHotspot.TurnOn ();
				}
				else
				{
					OwnHotspot.TurnOff ();
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			NPCData npcData = new NPCData();

			npcData.objectID = constantID;
			npcData.savePrevented = savePrevented;

			if (OwnHotspot)
			{
				npcData.isOn = OwnHotspot.IsOn ();
			}
			
			npcData.LocX = transform.position.x;
			npcData.LocY = transform.position.y;
			npcData.LocZ = transform.position.z;
			
			npcData.RotX = transform.eulerAngles.x;
			npcData.RotY = transform.eulerAngles.y;
			npcData.RotZ = transform.eulerAngles.z;
			
			npcData.ScaleX = transform.localScale.x;
			npcData.ScaleY = transform.localScale.y;
			npcData.ScaleZ = transform.localScale.z;
			
			NPC npc = GetComponent <NPC>();
			npcData = npc.SaveData (npcData);
			
			return Serializer.SaveScriptData <NPCData> (npcData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			NPCData data = Serializer.LoadScriptData <NPCData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (GetComponent <RememberHotspot>() == null)
			{
				if (OwnHotspot)
				{
					if (data.isOn)
					{
						OwnHotspot.TurnOn ();
					}
					else
					{
						OwnHotspot.TurnOff ();
					}
				}
			}

			transform.localScale = new Vector3 (data.ScaleX, data.ScaleY, data.ScaleZ);
			
			NPC npc = GetComponent <NPC>();
			npc.Teleport (new Vector3 (data.LocX, data.LocY, data.LocZ));
			npc.SetRotation (Quaternion.Euler (new Vector3 (data.RotX, data.RotY, data.RotZ)));
			npc.LoadData (data);
			
			loadedData = true;
		}

		#endregion


		#region GetSet

		private Hotspot OwnHotspot
		{
			get
			{
				if (ownHotspot == null)
				{
					ownHotspot = GetComponent <Hotspot>();
				}
				return ownHotspot;
			}
		}

		#endregion

	}


	/** A data container used by the RememberNPC script. */
	[System.Serializable]
	public class NPCData : RememberData
	{

		/** True if the NPC is enabled */
		public bool isOn;

		/** The X position */
		public float LocX;
		/** The Y position */
		public float LocY;
		/** The Z position */
		public float LocZ;

		/** The X rotation */
		public float RotX;
		/** The Y rotation */
		public float RotY;
		/** The Z rotation */
		public float RotZ;

		/** The X scale */
		public float ScaleX;
		/** The Y scale */
		public float ScaleY;
		/** The Z scale */
		public float ScaleZ;

		/** The NPC's idle animation */
		public string idleAnim;
		/** The NPC's walk animation */
		public string walkAnim;
		/** The NPC's talk animation */
		public string talkAnim;
		/** The NPC's run animation */
		public string runAnim;

		/** A unique identifier for the NPC's walk sound AudioClip */
		public string walkSound;
		/** A unique identifier for the NPC's run sound AudioClip */
		public string runSound;
		/** A unique identifier for the NPC's portrait graphic */
		public string portraitGraphic;

		/** The NPC's walk speed */
		public float walkSpeed;
		/** The NPC's run speed */
		public float runSpeed;

		/** True if a sprite-based NPC is locked to face a particular direction */
		public bool lockDirection;
		/** The direction that a sprite-based NPC is facing */
		public string spriteDirection;
		/** True if a sprite-based NPC has its scale locked */
		public bool lockScale;
		/** The scale of a sprite-based NPC */
		public float spriteScale;
		/** True if a sprite-based NPC has its sorting locked */
		public bool lockSorting;
		/** The sorting order of a sprite-based NPC */
		public int sortingOrder;
		/** The sorting layer of a sprite-based NPC */
		public string sortingLayer;

		/** The Constant ID number of the NPC's current Path */
		public int pathID;
		/** The target node number of the NPC's current Path */
		public int targetNode;
		/** The previous node number of the NPC's current Path */
		public int prevNode;
		/** The positions of each node in a pathfinding-generated Path */
		public string pathData;
		/** True if the NPC is running */
		public bool isRunning;
		/** True if the NPC's current Path affects the Y position */
		public bool pathAffectY;

		/** The Constant ID number of the NPC's last-used Path */
		public int lastPathID;
		/** The target node number of the NPC's last-used Path */
		public int lastTargetNode;
		/** The previous node number of the NPC's last-used Path */
		public int lastPrevNode;

		/** The Constant ID number of the NPC's follow target */
		public int followTargetID = 0;
		/** True if the NPC is following the player */
		public bool followTargetIsPlayer = false;
		/** The frequency with which the NPC follows its target */
		public float followFrequency = 0f;
		/** The distance that the NPC keeps with when following its target */
		public float followDistance = 0f;
		/** The maximum distance that the NPC keeps when following its target */
		public float followDistanceMax = 0f;
		/** If True, the NPC will face their follow target when idle */
		public bool followFaceWhenIdle = false;
		/** If True, the NPC will stand a random direction from their target */
		public bool followRandomDirection = false;
		/** If True, the NPC is playing a custom animation */
		public bool inCustomCharState = false;

		/** True if the NPC's head is pointed towards a target */
		public bool isHeadTurning = false;
		/** The ConstantID number of the head target Transform */
		public int headTargetID = 0;
		/** The NPC's head target's X position (offset) */
		public float headTargetX = 0f;
		/** The NPC's head target's Y position (offset) */
		public float headTargetY = 0f;
		/** The NPC's head target's Z position (offset) */
		public float headTargetZ = 0f;

		/** True if the NPC has a FollowSortingMap component that follows the scene's default SortingMap */
		public bool followSortingMap;
		/** The ConstantID number of the SortingMap that the NPC's FollowSortingMap follows, if not the scene's default */
		public int customSortingMapID = 0;

		/** The NPC's display name */
		public string speechLabel;
		/** The ID number that references the NPC's name, as generated by the Speech Manager */
		public int displayLineID;
		/** Data related to the character's left hand Ik state */
		public string leftHandIKState;
		/** Data related to the character's right hand Ik state */
		public string rightHandIKState;
		/** Data related to the character's available sprite directions */
		public string spriteDirectionData;

		/**
		 * The default Constructor.
		 */
		public NPCData () { }

	}

}