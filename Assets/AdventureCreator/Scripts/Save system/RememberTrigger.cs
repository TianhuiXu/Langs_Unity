/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberTrigger.cs"
 * 
 *	This script is attached to Trigger objects in the scene
 *	whose on/off state we wish to save. 
 * 
 */

using UnityEngine;

namespace AC
{

	/** Attach this script to Trigger objects in the scene whose on/off state you wish to save. */
	[AddComponentMenu("Adventure Creator/Save system/Remember Trigger")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_trigger.html")]
	public class RememberTrigger : Remember
	{

		#region Variables

		/** Whether the Trigger should be enabled or not when the game begins */
		public AC_OnOff startState = AC_OnOff.On;

		#endregion


		#region UnityStandards

		protected override void Start ()
		{
			base.Start ();

			if (loadedData) return;

			if (GameIsPlaying () && isActiveAndEnabled)
			{
				AC_Trigger trigger = GetComponent<AC_Trigger>();
				if (trigger)
				{ 
					if (startState == AC_OnOff.On)
					{
						trigger.TurnOn ();
					}
					else
					{
						trigger.TurnOff ();
					}
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
			TriggerData triggerData = new TriggerData ();
			triggerData.objectID = constantID;
			triggerData.savePrevented = savePrevented;

			Collider _collider = GetComponent <Collider>();
			if (_collider)
			{
				triggerData.isOn = _collider.enabled;
			}
			else
			{
				Collider2D _collider2D = GetComponent <Collider2D>();
				if (_collider2D)
				{
					triggerData.isOn = _collider2D.enabled;
				}
				else
				{
					triggerData.isOn = false;
				}
			}

			return Serializer.SaveScriptData <TriggerData> (triggerData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			TriggerData data = Serializer.LoadScriptData <TriggerData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			Collider _collider = GetComponent <Collider>();
			if (_collider)
			{
				_collider.enabled = data.isOn;
			}
			else 
			{
				Collider2D _collider2D = GetComponent <Collider2D>();
				if (_collider2D)
				{
					_collider2D.enabled = data.isOn;
				}
			}

			loadedData = true;
		}

		#endregion

	}


	/**
	 * A data container used by the RememberTrigger script.
	 */
	[System.Serializable]
	public class TriggerData : RememberData
	{

		/** True if the Trigger is enabled */
		public bool isOn;


		/**
		 * The default Constructor.
		 */
		public TriggerData () { }

	}

}