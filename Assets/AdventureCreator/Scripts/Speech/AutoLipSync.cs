/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"AutoLipsync.cs"
 * 
 *	This script provides simple lipsyncing for talking characters, "Half Life 1"-style.
 *	The Transform defined in jawBone will rotate according to the sound that the gameObject is emitting.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A component that provides simple lipsyncing, Half Life 1-style, in which a jaw bone rotates with the volume of the sound that the GameObject is emitting.
	 */
	[RequireComponent (typeof (AudioSource))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_auto_lip_sync.html")]
	public class AutoLipSync : MonoBehaviour
	{

		#region Variables

		/** The jaw bone to rotate */
		public Transform jawBone;
		/** Which axis to rotate the bone around */
		public Coord coordinateToAffect;
		/** The rotation factor */
		public float rotationFactor = 10f;
		/** If True, rotations will be added, rather than replace, existing animation */
		public bool isAdditive = true;
		
		protected float volume;
		protected float bin = 0.04f;
		protected int width = 64;
		protected float output;

		protected float[] array;
		protected Quaternion jawRotation;
		protected AudioSource _audio;
		protected Quaternion originalRotation;
		protected Char _character;

		#endregion


		#region UnityStandards		
		
		protected void Awake ()
		{
			_audio = GetComponent <AudioSource>();
			_character = GetComponent <Char>();
			array = new float[width];	

			originalRotation = jawBone.localRotation;
		}
		
		
		protected void LateUpdate ()
		{
			if (_audio.isPlaying)
			{
				_audio.GetOutputData (array, 0);
				float num3 = 0f;
				for (int i = 0; i < width; i++)
				{
				    float num4 = Mathf.Abs(array[i]);
				    num3 += num4;
				}
				num3 /= (float) width;
				if (Options.GetSpeechVolume () > 0f)
				{
					num3 /= Options.GetSpeechVolume ();
				}

				// Only record changes big enough
				if (Mathf.Abs (num3 - volume) > bin)
					volume = num3;

				volume = Mathf.Clamp01 (volume * 2);
				volume *= 0.3f;
				
				output = Mathf.Lerp (output, volume, Time.deltaTime * Mathf.Abs (rotationFactor));
			}
			else
			{
				output = 0f;
			}

			if (_character && !_character.isTalking && Mathf.Approximately (output, 0f))
			{
				return;
			}

			jawRotation = (isAdditive) ? jawBone.localRotation : originalRotation;

			if (coordinateToAffect == Coord.W)
			{
				if (rotationFactor < 0)
				{
					jawRotation.w += output;
				}
				else
				{
					jawRotation.w -= output;
				}
			}
			else if (coordinateToAffect == Coord.X)
			{
				if (rotationFactor < 0)
				{
					jawRotation.x += output;
				}
				else
				{
					jawRotation.x -= output;
				}
			}
			else if (coordinateToAffect == Coord.Y)
			{
				if (rotationFactor < 0)
				{
					jawRotation.y += output;
				}
				else
				{
					jawRotation.y -= output;
				}
			}
			else if (coordinateToAffect == Coord.Z)
			{
				if (rotationFactor < 0)
				{
					jawRotation.z += output;
				}
				else
				{
					jawRotation.z -= output;
				}
			}
			
			jawBone.localRotation = jawRotation;
		}

		#endregion

	}

}