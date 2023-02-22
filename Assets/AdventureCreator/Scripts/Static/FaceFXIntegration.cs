/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"FaceFXIntegration.cs"
 * 
 *	This script contains a number of static functions for use
 *	in integrating AC with the FaceFX asset.
 *
 *	To allow for FaceFX integration, the 'FaceFXIsPresent'
 *	preprocessor must be defined.  This can be done from
 *	Edit -> Project Settings -> Player, and entering
 *	'FaceFXIsPresent' into the Scripting Define Symbols text box
 *	for your game's build platform.
 *
 *	The FaceFX plugin for Unity can be downloaded here:
 *	http://unitydemos.facefx.com.s3.amazonaws.com/FaceFXBonesMorph.unitypackage
 * 
 */


using UnityEngine;


namespace AC
{

	/**
	 * A class the contains a number of static functions to assist with FaceFX integration.
	 * To use FaceFX with Adventure Creator, the 'FaceFXIsPresent' preprocessor must be defined.
	 */
	public class FaceFXIntegration
	{

		/**
		 * <summary>Checks if the 'FaceFXIsPresent' preprocessor has been defined.</summary>
		 * <returns>True if the 'FaceFXIsPresent' preprocessor has been defined</returns>
		 */
		public static bool IsDefinePresent ()
		{
			#if FaceFXIsPresent
			return true;
			#else
			return false;
			#endif
		}
		

		/**
		 * <summary>Plays a FaceFX animation on a character, based on an AudioClip.</summary>
		 * <param name = "speaker">The speaking character</param>
		 * <param name = "name">The unique identifier of the line in the format Joe13, where 'Joe' is the name of the character, and '13' is the ID number of ths speech line</param>
		 * <param name = "audioClip">The speech AudioClip</param>
		 */
		public static void Play (AC.Char speaker, string name, AudioClip audioClip)
		{
			#if FaceFXIsPresent
			FaceFXControllerScript_Base fcs = speaker.GetComponent <FaceFXControllerScript_Base>();
			if (fcs == null)
			{
				fcs = speaker.GetComponentInChildren <FaceFXControllerScript_Base>();
			}
			if (fcs != null)
			{
				speaker.isLipSyncing = true;
				fcs.PlayAnim ("Default_" + name, audioClip);
			}
			else
			{
				ACDebug.LogWarning ("No FaceFXControllerScript_Base script found on " + speaker.gameObject.name);
			}
			#else
			ACDebug.LogWarning ("The 'FaceFXIsPresent' preprocessor define must be declared in the Player Settings.");
			#endif
		}


		/**
		 * <summary>Stops the FaceFX animation on a character.</summary>
		 * <param name = "speaker">The speaking character</param>
		 */
		public static void Stop (AC.Char speaker)
		{
			#if FaceFXIsPresent
			FaceFXControllerScript_Base fcs = speaker.GetComponent <FaceFXControllerScript_Base>();
			if (fcs == null)
			{
				fcs = speaker.GetComponentInChildren <FaceFXControllerScript_Base>();
			}
			if (fcs != null)
			{
				fcs.StopAnim ();
			}
			else
			{
				ACDebug.LogWarning ("No FaceFXControllerScript_Base script found on " + speaker.gameObject.name);
			}
			#else
			ACDebug.LogWarning ("The 'FaceFXIsPresent' preprocessor define must be declared in the Player Settings.");
			#endif
		}
		
	}
	
}