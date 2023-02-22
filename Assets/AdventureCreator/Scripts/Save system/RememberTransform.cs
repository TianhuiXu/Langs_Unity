/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberTransform.cs"
 * 
 *	This script is attached to gameObjects in the scene
 *	with transform data we wish to save.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Attach this to GameObject whose position, parentage, or scene presence you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Transform")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_transform.html")]
	public class RememberTransform : ConstantID
	{

		/** True if the GameObject's change in parent should be recorded */
		public bool saveParent;
		/** True if the GameObject's change in scene presence should be recorded */
		public bool saveScenePresence;
		/** If non-zero, the Constant ID number of the prefab to re-spawn if not present in the scene, but saveScenePresence = true.  If zero, the prefab will be assumed to have the same ID as this. */
		public int linkedPrefabID;
		/** How to reference transform co-ordinates (Global, Local) */
		public GlobalLocal transformSpace = GlobalLocal.Global;
		
		#if AddressableIsPresent
		/** The name of the prefab to spawn if it needs to be added to the scene, and addressables are used when saving */
		public string addressableName;
		#endif

		private bool savePrevented = false;


		/** If True, saving is prevented */
		public bool SavePrevented
		{
			get
			{
				return savePrevented;
			}
			set
			{
				savePrevented = value;
			}
		}


		/** Initialises the component.  This needs to be called if the object it is attached to is generated/spawned at runtime manually through code. */
		public void OnSpawn ()
		{
			if (linkedPrefabID != 0)
			{
				int newID = GetInstanceID ();
				ConstantID[] idScripts = GetComponents <ConstantID>();
				foreach (ConstantID idScript in idScripts)
				{
					idScript.constantID = newID;
				}

				ACDebug.Log ("Spawned new instance of " + gameObject.name + ", given new ID: " + newID, this);
			}
		}


		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public TransformData SaveTransformData ()
		{
			TransformData transformData = new TransformData();
			
			transformData.objectID = constantID;
			transformData.savePrevented = savePrevented;

			switch (transformSpace)
			{
				case GlobalLocal.Global:
					{
						transformData.LocX = transform.position.x;
						transformData.LocY = transform.position.y;
						transformData.LocZ = transform.position.z;

						Char attachedChar = transform.GetComponent<Char> ();
						if (attachedChar)
						{
							transformData.RotX = attachedChar.TransformRotation.eulerAngles.x;
							transformData.RotY = attachedChar.TransformRotation.eulerAngles.y;
							transformData.RotZ = attachedChar.TransformRotation.eulerAngles.z;
						}
						else
						{
							transformData.RotX = transform.eulerAngles.x;
							transformData.RotY = transform.eulerAngles.y;
							transformData.RotZ = transform.eulerAngles.z;
						}
					}
					break;

				case GlobalLocal.Local:
					{
						transformData.LocX = transform.localPosition.x;
						transformData.LocY = transform.localPosition.y;
						transformData.LocZ = transform.localPosition.z;

						transformData.RotX = transform.localEulerAngles.x;
						transformData.RotY = transform.localEulerAngles.y;
						transformData.RotZ = transform.localEulerAngles.z;
					}
					break;
			}
			
			transformData.ScaleX = transform.localScale.x;
			transformData.ScaleY = transform.localScale.y;
			transformData.ScaleZ = transform.localScale.z;

			transformData.bringBack = saveScenePresence;
			#if AddressableIsPresent
			transformData.addressableName = (saveScenePresence) ? addressableName : string.Empty;
			#endif
			transformData.linkedPrefabID = (saveScenePresence) ? linkedPrefabID : 0;

			if (saveParent)
			{
				// Attempt to find the "hand" bone of a character
				Transform t = transform.parent;

				if (t == null)
				{
					transformData.parentID = 0;
					return transformData;
				}

				while (t.parent)
				{
					t = t.parent;

					AC.Char parentCharacter = t.GetComponent <AC.Char>();
					if (parentCharacter)
					{						
						if (parentCharacter.IsPlayer || (parentCharacter.GetComponent <ConstantID>() && parentCharacter.GetComponent <ConstantID>().constantID != 0))
						{
							if (transform.parent == parentCharacter.leftHandBone || transform.parent == parentCharacter.rightHandBone)
							{
								if (parentCharacter.IsPlayer)
								{
									transformData.parentIsPlayer = true;
									transformData.parentIsNPC = false;
									transformData.parentID = 0;
									transformData.parentPlayerID = -1;

									if (KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow && parentCharacter != KickStarter.player)
									{
										Player player = parentCharacter as Player;
										transformData.parentPlayerID = player.ID;
									}
								}
								else
								{
									transformData.parentIsPlayer = false;
									transformData.parentIsNPC = true;
									transformData.parentID = parentCharacter.GetComponent <ConstantID>().constantID;
									transformData.parentPlayerID = -1;
								}
								
								if (transform.parent == parentCharacter.leftHandBone)
								{
									transformData.heldHand = Hand.Left;
								}
								else
								{
									transformData.heldHand = Hand.Right;
								}
								
								return transformData;
							}
						}
						
						break;
					}
				}

				if (transform.parent.GetComponent <ConstantID>() && transform.parent.GetComponent <ConstantID>().constantID != 0)
				{
					transformData.parentID = transform.parent.GetComponent <ConstantID>().constantID;
				}
				else
				{
					transformData.parentID = 0;
					ACDebug.LogWarning ("Could not save " + this.name + "'s parent since it has no Constant ID", this);
				}
			}

			return transformData;
		}


		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public void LoadTransformData (TransformData data)
		{
			if (data == null) return;
			savePrevented = data.savePrevented; if (savePrevented) return;
			
			if (data.parentIsPlayer)
			{
				Player player = KickStarter.player;

				if (data.parentPlayerID >= 0)
				{
					player = null;
					PlayerPrefab playerPrefab = KickStarter.settingsManager.GetPlayerPrefab (data.parentPlayerID);
					if (playerPrefab != null)
					{
						player = playerPrefab.GetSceneInstance ();
					}
				}

				if (player)
				{
					if (data.heldHand == Hand.Left)
					{
						transform.parent = player.leftHandBone;
					}
					else
					{
						transform.parent = player.rightHandBone;
					}
				}
			}
			else if (data.parentID != 0)
			{
				ConstantID parentObject = ConstantID.GetComponent <ConstantID> (data.parentID);

				if (parentObject)
				{
					if (data.parentIsNPC)
					{
						Char _char = parentObject.GetComponent<NPC> ();
						if (_char && !_char.IsPlayer)
						{
							if (data.heldHand == Hand.Left)
							{
								transform.parent = _char.leftHandBone;
							}
							else
							{
								transform.parent = _char.rightHandBone;
							}
						}
					}
					else
					{
						transform.parent = parentObject.gameObject.transform;
					}
				}
			}
			else if (data.parentID == 0 && saveParent)
			{
				transform.parent = null;
			}

			switch (transformSpace)
			{
				case GlobalLocal.Global:
					transform.position = new Vector3 (data.LocX, data.LocY, data.LocZ);
					transform.eulerAngles = new Vector3 (data.RotX, data.RotY, data.RotZ);
					break;

				case GlobalLocal.Local:
					transform.localPosition = new Vector3 (data.LocX, data.LocY, data.LocZ);
					transform.localEulerAngles = new Vector3 (data.RotX, data.RotY, data.RotZ);
					break;
			}

			transform.localScale = new Vector3 (data.ScaleX, data.ScaleY, data.ScaleZ);
		}

	}


	/**
	 * A data container used by the RememberTransform script.
	 */
	[System.Serializable]
	public class TransformData
	{

		/** The ConstantID number of the object being saved */
		public int objectID;
		/** If True, saving is prevented */
		public bool savePrevented;

		#if AddressableIsPresent
		/** The addressable of the prefab to spawn, if necessary */
		public string addressableName;
		#endif

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

		/** True if the GameObject should be re-instantiated if removed from the scene */
		public bool bringBack;
		/** The Constant ID number of the Resources prefab this is linked to */
		public int linkedPrefabID;

		/** The Constant ID number of the GameObject's parent */
		public int parentID;
		/** True if the GameObject's parent is an NPC */
		public bool parentIsNPC = false;
		/** True if the GameObject's parent is the Player */
		public bool parentIsPlayer = false;
		/** If the GameObject's parent is a Character, which hand is it held in? (Left, Right) */
		public Hand heldHand;
		/** If player-switching is allowed, and the GameObject's parent is an inactive Player, the ID number of that Player */
		public int parentPlayerID = -1;

		/**
		 * The default Constructor.
		 */
		public TransformData () { }
		
	}

}