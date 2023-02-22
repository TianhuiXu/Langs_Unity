/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"DetectHotspots.cs"
 * 
 *	This script is used to determine which
 *	active Hotspot is nearest the player.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Used to only allow Hotspots within a given volume to be selectable.
	 * Attach this as a child object to your Player prefab, and assign it as your Hotspot detector child - be sure to set "hotspot detection" to Player Vicinity in SettingsManager.
	 */
	[AddComponentMenu("Adventure Creator/Hotspots/Hotspot detector")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_detect_hotspots.html")]
	public class DetectHotspots : MonoBehaviour
	{

		#region Variables

		protected Hotspot nearestHotspot;
		protected int selected = 0;
		protected List<Hotspot> hotspots = new List<Hotspot>();
		protected int hotspotLayerInt;
		protected int distantHotspotLayerInt;

		#endregion


		#region UnityStandards

		protected void Start ()
		{
			if (KickStarter.settingsManager)
			{
				string layerName = LayerMask.LayerToName (gameObject.layer);
				if (layerName == KickStarter.settingsManager.hotspotLayer)
				{
					ACDebug.LogWarning ("The HotspotDetector's layer, " + layerName + ", is the same used by Hotspots, and will prevent Hotspots from being properly detected. It should be moved to the Ignore Raycast layer.", gameObject);
				}

				hotspotLayerInt = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
				distantHotspotLayerInt = LayerMask.NameToLayer (KickStarter.settingsManager.distantHotspotLayer);
			}
		}


		protected void OnEnable ()
		{
			EventManager.OnInitialiseScene += OnInitialiseScene;
			EventManager.OnUnregisterHotspot += ForceRemoveHotspot;
		}


		protected void OnDisable ()
		{
			EventManager.OnInitialiseScene -= OnInitialiseScene;
			EventManager.OnUnregisterHotspot -= ForceRemoveHotspot;
		}


		protected void OnTriggerStay (Collider other)
		{
			Hotspot otherHotspot = other.GetComponent <Hotspot>();
			if (otherHotspot && otherHotspot.PlayerIsWithinBoundary () && IsLayerCorrect (other.gameObject.layer, true))
			{
				if (nearestHotspot == null ||
					(transform.position - other.transform.position).sqrMagnitude <= (transform.position - nearestHotspot.Transform.position).sqrMagnitude)
				{
					nearestHotspot = otherHotspot;
				}

				foreach (Hotspot hotspot in hotspots)
				{
					if (hotspot == otherHotspot)
					{
						return;
					}
				}

				hotspots.Add (otherHotspot);
				hotspots = KickStarter.eventManager.Call_OnModifyHotspotDetectorCollection (this, hotspots);
			}
        }


		protected void OnTriggerStay2D (Collider2D other)
		{
			Hotspot otherHotspot = other.GetComponent <Hotspot>();
			if (otherHotspot && otherHotspot.PlayerIsWithinBoundary () && IsLayerCorrect (other.gameObject.layer, true))
			{
				if (nearestHotspot == null ||
					(transform.position - other.transform.position).sqrMagnitude <= (transform.position - nearestHotspot.Transform.position).sqrMagnitude)
				{
					nearestHotspot = otherHotspot;
				}
				
				foreach (Hotspot hotspot in hotspots)
				{
					if (hotspot == otherHotspot)
					{
						return;
					}
				}
				hotspots.Add (otherHotspot);
				hotspots = KickStarter.eventManager.Call_OnModifyHotspotDetectorCollection (this, hotspots);
			}
		}


		protected void OnTriggerExit (Collider other)
		{
			if (IsActivePlayer ())
			{
				ForceRemoveHotspot (other.GetComponent <Hotspot>());
			}
		}

		#endregion


		#region PublicFunctions

		/** Detects Hotspots in its vicinity. This is public so that it can be called by StateHandler every frame. */
		public void _Update ()
		{
			if (nearestHotspot && nearestHotspot.gameObject.layer == LayerMask.NameToLayer (AdvGame.GetReferences ().settingsManager.deactivatedLayer))
			{
				nearestHotspot = null;
			}

			if (KickStarter.stateHandler && KickStarter.stateHandler.IsInGameplay ())
			{
				if (!IsActivePlayer ())
				{
					return;
				}

				if (KickStarter.playerInput.InputGetButtonDown ("CycleHotspotsLeft"))
				{
					CycleHotspots (false);
				}
				else if (KickStarter.playerInput.InputGetButtonDown ("CycleHotspotsRight"))
				{
					CycleHotspots (true);
				}
				else if (KickStarter.playerInput.InputGetAxis ("CycleHotspots") > 0.1f)
				{
					CycleHotspots (true);
				}
				else if (KickStarter.playerInput.InputGetAxis ("CycleHotspots") < -0.1f)
				{
					CycleHotspots (false);
				}
			}
		}


		/**
		 * <summary>Gets all Hotspots found within the object's Trigger collider.</summary>
		 * <returns>All Hotspots found within the object's Trigger collider.</returns>
		 */
		public Hotspot[] GetAllDetectedHotspots ()
		{
			return hotspots.ToArray ();
		}


		/**
		 * <summary>Gets the currently-active Hotspot.</summary>
		 * <returns>The currently active Hotspot.</returns>
		 */
		public Hotspot GetSelected ()
		{
			if (hotspots.Count > 0)
			{
				if (AdvGame.GetReferences ().settingsManager.hotspotsInVicinity == HotspotsInVicinity.NearestOnly)
				{
					if (selected >= 0 && selected < hotspots.Count)
					{
						if (hotspots[selected] && IsLayerCorrect (hotspots[selected].gameObject.layer))
						{
							return nearestHotspot;
						}
						else
						{
							nearestHotspot = null;
							hotspots.Remove (nearestHotspot);
							hotspots = KickStarter.eventManager.Call_OnModifyHotspotDetectorCollection (this, hotspots);
						}
					}
				}
				else if (AdvGame.GetReferences ().settingsManager.hotspotsInVicinity == HotspotsInVicinity.CycleMultiple)
				{
					if (selected >= hotspots.Count)
					{
						selected = hotspots.Count - 1;
					}
					else if (selected < 0)
					{
						selected = 0;
					}

					if (hotspots[selected] && IsLayerCorrect (hotspots[selected].gameObject.layer))
					{
						return hotspots [selected];
					}
					else
					{
						if (nearestHotspot == hotspots [selected])
						{
							nearestHotspot = null;
						}

						hotspots.RemoveAt (selected);
						hotspots = KickStarter.eventManager.Call_OnModifyHotspotDetectorCollection (this, hotspots);
					}
				}
			}

			return null;
		}


		/**
		 * <summary>Removes a Hotspot from the script's internal record of detected Hotspots.</summary>
		 * <param name = "_hotspot">The Hotspot to forget</param>
		 */
		public void ForceRemoveHotspot (Hotspot _hotspot)
		{
			if (_hotspot == null)
			{
				return;
			}

			if (nearestHotspot == _hotspot)
			{
				nearestHotspot = null;
			}
			
			if (IsHotspotInTrigger (_hotspot))
			{
				hotspots.Remove (_hotspot);
				hotspots = KickStarter.eventManager.Call_OnModifyHotspotDetectorCollection (this, hotspots);
			}
			
			if (_hotspot.highlight)
			{
				_hotspot.highlight.HighlightOff ();
			}
		}


		/**
		 * <summary>Checks if a specific Hotspot is within its volume.</summary>
		 * <param name = "hotspot">The Hotspot to check for</param>
		 * <returns>True if the Hotspot is within the Collider volume</returns>
		 */
		public bool IsHotspotInTrigger (Hotspot hotspot)
		{
			if (hotspots.Contains (hotspot))
			{
				return true;
			}
			return false;
		}


		/**
		 * Highlights all Hotspots within its volume.
		 */
		public void HighlightAll ()
		{
			foreach (Hotspot _hotspot in hotspots)
			{
				if (_hotspot.highlight)
				{
					_hotspot.highlight.HighlightOn ();
				}
			}
		}

		#endregion


		#region ProtectedFunctions

		protected void OnInitialiseScene ()
		{
			hotspots.Clear ();
			hotspots = KickStarter.eventManager.Call_OnModifyHotspotDetectorCollection (this, hotspots);
			selected = 0;
		}


		protected bool IsLayerCorrect (int layerInt, bool distantToo = false)
		{
			if (distantToo)
			{
				if (layerInt == hotspotLayerInt || layerInt == distantHotspotLayerInt)
				{
					return true;
				}
			}
			else
			{
				if (layerInt == hotspotLayerInt)
				{
					return true;
				}
			}
			return false;
		}


		protected void OnTriggerExit2D (Collider2D other)
		{
			if (IsActivePlayer ())
			{
				ForceRemoveHotspot (other.GetComponent <Hotspot>());
			}
		}


		protected void CycleHotspots (bool goRight)
		{
			if (goRight)
			{
				selected ++;
			}
			else
			{
				selected --;
			}

			if (selected >= hotspots.Count)
			{
				selected = 0;
			}
			else if (selected < 0)
			{
				selected = hotspots.Count - 1;
			}
		}


		protected bool IsActivePlayer ()
		{
			if (KickStarter.settingsManager == null || KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow)
			{
				return true;
			}

			if (KickStarter.player && KickStarter.player.hotspotDetector == this)
			{
				return true;
			}

			return false;
		}

		#endregion


		#region GetSet

		/** The Hotspot nearest to the centre of the detector */
		public Hotspot NearestHotspot
		{
			get
			{
				return nearestHotspot;
			}
		}

		#endregion

	}

}