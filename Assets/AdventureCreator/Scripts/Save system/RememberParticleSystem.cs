/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"RememberParticleSystem.cs"
 * 
 *	This script is attached to ParticleSystem components in the scene
 *	whose current playback state we wish to save.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Attach this to ParticleSystem components whose current playback state you wish to save. This will save whether the ParticleSystem is playing or paused, as well as the current length of time it has been playing.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember ParticleSystem")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_particle_system.html")]
	public class RememberParticleSystem : Remember
	{

		public override string SaveData ()
		{
			ParticleSystemData particleSystemData = new ParticleSystemData();
			particleSystemData.objectID = constantID;
			particleSystemData.savePrevented = savePrevented;

			ParticleSystem particleSystem = GetComponent <ParticleSystem>();
			if (particleSystem)
			{
				particleSystemData.isPlaying = particleSystem.isPlaying;
				particleSystemData.isPaused = particleSystem.isPaused;
				particleSystemData.currentTime = particleSystem.time;
			}

			return Serializer.SaveScriptData <ParticleSystemData> (particleSystemData);
		}


		public override void LoadData (string stringData)
		{
			ParticleSystemData data = Serializer.LoadScriptData <ParticleSystemData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			ParticleSystem particleSystem = GetComponent <ParticleSystem>();
			if (particleSystem)
			{
				particleSystem.time = data.currentTime;
				if (data.isPlaying)
				{
					particleSystem.Simulate (data.currentTime);
					particleSystem.Play ();
				}
				else
				{
					if (data.isPaused)
					{
						particleSystem.Pause ();
					}
					else
					{
						particleSystem.Stop ();
					}
				}
				particleSystem.time = data.currentTime;
			}
		}
	
	}


	/**
	 * A data container used by the RememberParticleSystem script.
	 */
	[System.Serializable]
	public class ParticleSystemData : RememberData
	{

		/** True if the ParticleSystem is currently paused */
		public bool isPaused;
		/** True if the ParticleSystem is currently playing */
		public bool isPlaying;
		/** The current length of time the ParticleSystem has been playing */
		public float currentTime;

		/**
		 * The default Constructor.
		 */
		public ParticleSystemData () { }

	}

}