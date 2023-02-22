/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AC_Trigger.cs"
 * 
 *	This ActionList runs when the Player enters it.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * An ActionList that is run when the Player, or another object, comes into contact with it.
	 */
	[AddComponentMenu("Adventure Creator/Logic/Trigger")]
	[System.Serializable]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_a_c___trigger.html")]
	public class AC_Trigger : ActionList
	{

		#region Variables

		/** If detectionMethod = TriggerDetectionMethod.RigidbodyCollision, what the Trigger will react to (Player, SetObject, AnyObject, AnyObjectWithComponent) */
		public TriggerDetects detects = TriggerDetects.Player;
		/** The GameObject that the Trigger reacts to, if detectionMethod = TriggerDetectionMethod.RigidbodyCollision and detects = TriggerDetects.SetObject */
		public GameObject obToDetect;
		/** The component that must be attached to an object for the Trigger to react to, if detectionMethod = TriggerDetectionMethod.RigidbodyCollision and detects = TriggerDetects.AnyObjectWithComponent */
		public string detectComponent = "";

		/** What kind of contact the Trigger reacts to (0 = "On enter", 1 = "Continuous", 2 = "On exit") */
		public int triggerType;
		/** If True, and the Player sets off the Trigger while walking towards a Hotspot Interaction, then the Player will stop, and the Interaction will be cancelled */
		public bool cancelInteractions = false;
		/** The state of the game under which the trigger reacts (OnlyDuringGameplay, OnlyDuringCutscenes, DuringCutscenesAndGameplay) */
		public TriggerReacts triggerReacts = TriggerReacts.OnlyDuringGameplay;
		/** The way in which objects are detected (RigidbodyCollision, TransformPosition) */
		public TriggerDetectionMethod detectionMethod = TriggerDetectionMethod.RigidbodyCollision;

		/** If True, and detectionMethod = TriggerDetectionMethod.TransformPosition, then the Trigger will react to the active Player */
		public bool detectsPlayer = true;
		/** If True, and detectsPlayer = True, and player-switching is enabled, then inactive Players will be detected too */
		public bool detectsAllPlayers = false;
		/** The GameObjects that the Trigger reacts to, if detectionMethod = TriggerDetectionMethod.TransformPosition */
		public List<GameObject> obsToDetect = new List<GameObject>();

		/** If True, then the Trigger will restart if it is triggered while already running. Otherwise, it will not restart. */
		public bool canInterruptSelf = true;

		public int gameObjectParameterID = -1;

		protected Collider2D _collider2D;
		protected Collider _collider;
		protected List<PositionDetectObject> positionDetectObjects = new List<PositionDetectObject>();

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			InitTrigger ();

			EventManager.OnPlayerSpawn += OnPlayerSpawn;
			EventManager.OnPlayerRemove += OnPlayerRemove;
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);

			EventManager.OnPlayerSpawn -= OnPlayerSpawn;
			EventManager.OnPlayerRemove -= OnPlayerRemove;
		}


		public void _Update ()
		{
			if (detectionMethod == TriggerDetectionMethod.TransformPosition)
			{
				for (int i=0; i<positionDetectObjects.Count; i++)
				{
					positionDetectObjects[i].Process (this);
				}
			}
		}


		protected void OnTriggerEnter (Collider other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 0 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerEnter2D (Collider2D other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 0 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerStay (Collider other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 1 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerStay2D (Collider2D other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 1 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerExit (Collider other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 2 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerExit2D (Collider2D other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 2 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if the Trigger is enabled.</summary>
		 * <returns>True if the Trigger is enabled.</summary>
		 */
		public bool IsOn ()
		{
			if (_collider)
			{
				return _collider.enabled;
			}
			else if (_collider2D)
			{
				return _collider2D.enabled;
			}
			return false;
		}
		

		/** Enables the Trigger. */
		public void TurnOn ()
		{
			InitTrigger ();

			if (_collider)
			{
				_collider.enabled = true;
			}
			else if (_collider2D)
			{
				_collider2D.enabled = true;
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn " + this.name + " on because it has no Collider component.", this);
			}
		}
		

		/** Disables the Trigger. */
		public void TurnOff ()
		{
			InitTrigger ();

			if (_collider)
			{
				_collider.enabled = false;
			}
			else if (_collider2D)
			{
				_collider2D.enabled = false;
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn " + this.name + " off because it has no Collider component.", this);
			}

			for (int i=0; i<positionDetectObjects.Count; i++)
			{
				positionDetectObjects[i].OnTurnOff ();
			}
		}


		public override void Interact ()
		{
			Interact (null);
		}


		/**
		 * <summary>Registers an object as one that can be detected by the Trigger, provided that detectionMethod = TriggerDetectionMethod.TransformPosition</summary>
		 * <param name = "_gameObject">The object to detect</param>
		 */
		public void AddObjectToDetect (GameObject _gameObject)
		{
			if (_gameObject == null || obsToDetect.Contains (_gameObject))
			{
				return;
			}

			obsToDetect.Add (_gameObject);
			positionDetectObjects.Add (new PositionDetectObject (_gameObject));
		}


		/**
		 * <summary>Registers an object as one that cancanot be detected by the Trigger, provided that detectionMethod = TriggerDetectionMethod.TransformPosition</summary>
		 * <param name = "_gameObject">The object to no longer detect</param>
		 */
		public void RemoveObjectToDetect (GameObject _gameObject)
		{
			if (_gameObject == null || !obsToDetect.Contains (_gameObject))
			{
				return;
			}

			obsToDetect.Remove (_gameObject);

			for (int i = 0; i < positionDetectObjects.Count; i++)
			{
				if (positionDetectObjects[i].IsForObject (_gameObject))
				{
					positionDetectObjects.RemoveAt (i);
					i = 0;
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void Interact (GameObject collisionOb)
		{
			if (!enabled) return;

			if (AreActionsRunning ())
			{
				if (canInterruptSelf)
				{
					Kill ();
				}
				else
				{
					return;
				}
			}

			if (cancelInteractions)
			{
				KickStarter.playerInteraction.StopMovingToHotspot ();
			}
			
			if (actionListType == ActionListType.PauseGameplay)
			{
				KickStarter.playerInteraction.DeselectHotspot (false);
			}

			KickStarter.eventManager.Call_OnRunTrigger (this, collisionOb);

			// Set correct parameter
			if (collisionOb)
			{
				if (source == ActionListSource.InScene)
				{
					if (useParameters && parameters != null && parameters.Count >= 1)
					{
						if (parameters[0].parameterType == ParameterType.GameObject)
						{
							parameters[0].gameObject = collisionOb;
						}
						else
						{
							ACDebug.Log ("Cannot set the value of parameter 0 ('" + parameters[0].label + "') as it is not of the type 'Game Object'.", this);
						}
					}
				}
				else if (source == ActionListSource.AssetFile
						&& assetFile != null
						&& assetFile.NumParameters > 0
						&& gameObjectParameterID >= 0)
				{
					ActionParameter param = (syncParamValues)
											? assetFile.GetParameter (gameObjectParameterID)
											: GetParameter (gameObjectParameterID);
					
					if (param != null) param.SetValue (collisionOb);
				}
			}

			base.Interact ();
		}


		protected bool IsObjectCorrect (GameObject obToCheck)
		{
			if (KickStarter.stateHandler == null || KickStarter.stateHandler.gameState == GameState.Paused || obToCheck == null)
			{
				return false;
			}

			if (KickStarter.saveSystem.loadingGame != LoadingGame.No)
			{
				return false;
			}

			if (triggerReacts == TriggerReacts.OnlyDuringGameplay && KickStarter.stateHandler.gameState != GameState.Normal)
			{
				return false;
			}
			else if (triggerReacts == TriggerReacts.OnlyDuringCutscenes && KickStarter.stateHandler.IsInGameplay ())
			{
				return false;
			}

			if (KickStarter.stateHandler && KickStarter.stateHandler.AreTriggersDisabled ())
			{
				return false;
			}

			if (detectionMethod == TriggerDetectionMethod.TransformPosition)
			{
				return true;
			}

			switch (detects)
			{
				case TriggerDetects.Player:
					if (KickStarter.player && obToCheck == KickStarter.player.gameObject)
					{
						return true;
					}
					break;

				case TriggerDetects.SetObject:
					if (obToDetect && obToCheck == obToDetect)
					{
						return true;
					}
					break;

				case TriggerDetects.AnyObjectWithComponent:
					if (!string.IsNullOrEmpty (detectComponent))
					{
						string[] allComponents = detectComponent.Split (";"[0]);
						foreach (string component in allComponents)
						{
							if (!string.IsNullOrEmpty (component) && obToCheck.GetComponent (component))
							{
								return true;
							}
						}
					}
					break;

				case TriggerDetects.AnyObjectWithTag:
					if (!string.IsNullOrEmpty (detectComponent))
					{
						string[] allComponents = detectComponent.Split (";"[0]);
						foreach (string component in allComponents)
						{
							if (!string.IsNullOrEmpty (component) && obToCheck.tag == component)
							{
								return true;
							}
						}
					}
					break;

				case TriggerDetects.AnyObject:
					return true;

				default:
					break;
			}
			
			return false;
		}


		protected bool CheckForPoint (Vector3 position)
		{
			if (_collider2D)
			{
				if (_collider2D.enabled)
				{
					return _collider2D.OverlapPoint (position);
				}
				return false;
			}

			if (_collider && _collider.enabled)
			{
				return _collider.bounds.Contains (position);
			}

			return false;
		}


		protected void OnPlayerSpawn (Player player)
		{
			if (detectionMethod == TriggerDetectionMethod.TransformPosition && detectsPlayer && detectsAllPlayers)
			{
				foreach (PositionDetectObject positionDetectObject in positionDetectObjects)
				{
					if (positionDetectObject.IsForObject (player.gameObject))
					{
						return;
					}
				}

				positionDetectObjects.Add (new PositionDetectObject (player));
			}
		}


		protected void OnPlayerRemove (Player player)
		{
			if (detectionMethod == TriggerDetectionMethod.TransformPosition && detectsPlayer && detectsAllPlayers)
			{
				for (int i=0; i<positionDetectObjects.Count; i++)
				{
					if (positionDetectObjects[i].IsForObject (player.gameObject))
					{
						positionDetectObjects.RemoveAt (i);
					}
				}
			}
		}


		protected void InitTrigger ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);

			_collider2D = GetComponent <Collider2D>();
			_collider = GetComponent <Collider>();

			if (detectionMethod == TriggerDetectionMethod.TransformPosition)
			{
				positionDetectObjects.Clear ();
				foreach (GameObject obToDetect in obsToDetect)
				{
					if (obToDetect != null)
					{
						positionDetectObjects.Add (new PositionDetectObject (obToDetect));
					}
				}

				if (detectsPlayer)
				{
					if (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow || !detectsAllPlayers)
					{
						positionDetectObjects.Add (new PositionDetectObject (-1));
					}
					else if (detectsAllPlayers && KickStarter.settingsManager && KickStarter.settingsManager.playerSwitching == PlayerSwitching.Allow)
					{
						Player[] players = FindObjectsOfType<Player>();
						foreach (Player player in players)
						{
							positionDetectObjects.Add (new PositionDetectObject (player));
						}
					}
				}
			}

			if (_collider == null && _collider2D == null)
			{
				ACDebug.LogWarning ("Trigger '" + gameObject.name + " cannot detect collisions because it has no Collider!", this);
			}
		}

		#endregion


		#if UNITY_EDITOR
		
		protected void OnDrawGizmos ()
		{
			if (KickStarter.sceneSettings && KickStarter.sceneSettings.visibilityTriggers && UnityEditor.Selection.activeGameObject != gameObject)
			{
				DrawGizmos ();
			}
		}
		
		
		protected void OnDrawGizmosSelected ()
		{
			DrawGizmos ();
		}
		
		
		protected void DrawGizmos ()
		{
			Color gizmoColor = ACEditorPrefs.TriggerGizmoColor;

			PolygonCollider2D polygonCollider2D = GetComponent <PolygonCollider2D>();
			if (polygonCollider2D)
			{
				AdvGame.DrawPolygonCollider (transform, polygonCollider2D, gizmoColor);
			}
			else
			{
				MeshCollider meshCollider = GetComponent <MeshCollider>();
				if (meshCollider)
				{
					AdvGame.DrawMeshCollider(transform, meshCollider.sharedMesh, gizmoColor);
				}
				else
				{
					SphereCollider sphereCollider = GetComponent <SphereCollider>();
					if (sphereCollider)
					{
						AdvGame.DrawSphereCollider (transform, sphereCollider, gizmoColor);
					}
					else
					{
						CapsuleCollider capsuleCollider = GetComponent <CapsuleCollider>();
						if (capsuleCollider)
						{
							AdvGame.DrawCapsule (transform, capsuleCollider.center, capsuleCollider.radius, capsuleCollider.height, gizmoColor);
						}
						else
						{
							CharacterController characterController = GetComponent <CharacterController>();
							if (characterController)
							{
								AdvGame.DrawCapsule (transform, characterController.center, characterController.radius, characterController.height, gizmoColor);
							}
							else
							{
								if (GetComponent<BoxCollider>() || GetComponent<BoxCollider2D>())
								{
									AdvGame.DrawCubeCollider(transform, gizmoColor);
								}
							}
						}
					}
				}
			}
		}

		#endif


		#region PrivateStructs

		protected class PositionDetectObject
		{

			private GameObject obToDetect;
			private bool lastFrameWithin;
			private int playerID;
			

			public PositionDetectObject (GameObject _obToDetect)
			{
				obToDetect = _obToDetect;
				lastFrameWithin = false;
				playerID = -2;
			}


			public PositionDetectObject (Player _player)
			{
				obToDetect = _player.gameObject;
				lastFrameWithin = false;
				playerID = _player.ID;
			}


			public PositionDetectObject (int _playerID)
			{
				obToDetect = null;
				lastFrameWithin = false;
				playerID = _playerID;
			}


			public void OnTurnOff ()
			{
				lastFrameWithin = false;
			}


			public bool IsForObject (GameObject gameObject)
			{ 
				return (obToDetect == gameObject);
			}


			public void Process (AC_Trigger trigger)
			{
				if (playerID == -1 && KickStarter.player)
				{
					obToDetect = KickStarter.player.gameObject;
				}
				if (obToDetect)
				{
					bool isInside = trigger.CheckForPoint (obToDetect.transform.position);
					if (DetermineValidity (isInside, trigger.triggerType))
					{
						if (trigger.IsObjectCorrect (obToDetect))
						{
							trigger.Interact (obToDetect);
						}
					}
					lastFrameWithin = isInside;
				}
			}


			private bool DetermineValidity (bool thisFrameWithin, int triggerType)
			{
				switch (triggerType)
				{
					case 0:
						// OnEnter
						if (thisFrameWithin && !lastFrameWithin)
						{
							return true;
						}
						return false;

					case 1:
						// Continuous
						return thisFrameWithin;

					case 2:
						// OnExit
						if (!thisFrameWithin && lastFrameWithin)
						{
							return true;
						}
						return false;

					default:
						return false;
				}
			}
		}

		#endregion

	}
	
}