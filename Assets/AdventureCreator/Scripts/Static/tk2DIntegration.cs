/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"tk2DIntegration.cs"
 * 
 *	This script contains a number of static functions for use
 *	in playing back 2DToolkit sprite animations.  Requires 2DToolkit to work.
 *
 *	To allow for 2DToolkit integration, the 'tk2DIsPresent'
 *	preprocessor must be defined.  This can be done from
 *	Edit -> Project Settings -> Player, and entering
 *	'tk2DIsPresent' into the Scripting Define Symbols text box
 *	for your game's build platform.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A class the contains a number of static functions to assist with 2D Toolkit integration.
	 * To use 2D Toolkit with Adventure Creator, the 'tk2DIsPresent' preprocessor must be defined.
	 */
	public class tk2DIntegration
	{

		/**
		 * <summary>Checks if the 'tk2DIsPresent' preprocessor has been defined.</summary>
		 * <returns>True if the 'tk2DIsPresent' preprocessor has been defined</returns>
		 */
		public static bool IsDefinePresent ()
		{
			#if tk2DIsPresent
				return true;
			#else
				return false;
			#endif
		}
		

		/**
		 * <summary>Plays an animation.</summary>
		 * <param name = "sprite">The Transform with the 2Dtk Sprite</param>
		 * <param name = "clipName">The name of the animation clip to play</param>
		 * <param name = "frame">The frame number to play. If >= 0, then the animation will freeze at the specified frame</param>
		 */
		public static bool PlayAnimation (Transform sprite, string clipName, int frame = -1)
		{
			#if tk2DIsPresent
				return (tk2DIntegration.PlayAnimation (sprite, clipName, false, WrapMode.Once, frame));
			#else
				ACDebug.Log ("The 'tk2DIsPresent' preprocessor is not defined - check your Build Settings.");
				return true;
			#endif
		}


		/**
		 * <summary>Checks if a given gameobject has a tk2dSpriteAnimator component, so long as the 'tk2DIsPresent' preprocessor has been defined.</summary>
		 * <param name = "spriteObject">The gameobject to check</param>
		 * <returns>True if a given gameobject has a tk2dSpriteAnimator component, so long as the 'tk2DIsPresent' preprocessor has been defined.</returns>
		 */
		public static bool Is2DtkSprite (GameObject spriteObject)
		{
			#if tk2DIsPresent
			if (spriteObject != null && spriteObject.GetComponent <tk2dSpriteAnimator>())
			{
				return true;
			}
			#endif
			return false;
		}
		

		/**
		 * <summary>Plays an animation.</summary>
		 * <param name = "sprite">The Transform with the 2Dtk Sprite</param>
		 * <param name = "clipName">The name of the animation clip to play</param>
		 * <param name = "changeWrapMode">If True, then the clip's wrap mode will be changed</param>
		 * <param name = "wrapMode">The new WrapMode to use, if changeWrapMode = True</param>
		 * <param name = "frame">The frame number to play. If >= 0, then the animation will freeze at the specified frame</param>
		 */
		public static bool PlayAnimation (Transform sprite, string clipName, bool changeWrapMode, WrapMode wrapMode, int frame = -1)
		{
			#if tk2DIsPresent
			
			tk2dSpriteAnimationClip.WrapMode wrapMode2D = tk2dSpriteAnimationClip.WrapMode.Once;
			if (wrapMode == WrapMode.Loop)
			{
				wrapMode2D = tk2dSpriteAnimationClip.WrapMode.Loop;
			}
			else if (wrapMode == WrapMode.PingPong)
			{
				wrapMode2D = tk2dSpriteAnimationClip.WrapMode.PingPong;
			}
			
			if (sprite && sprite.GetComponent <tk2dSpriteAnimator>())
			{
				tk2dSpriteAnimator anim = sprite.GetComponent <tk2dSpriteAnimator>();
				tk2dSpriteAnimationClip clip = anim.GetClipByName (clipName);

				if (clip != null)
				{
					if (!anim.IsPlaying (clip))
					{
						if (changeWrapMode)
						{
							clip.wrapMode = wrapMode2D;
						}
						
				    	anim.Play (clip);

						if (frame >= 0)
						{
							anim.SetFrame (frame);
							anim.Stop ();
						}
					}
					
					return true;
				}

				return false;	
			}
			
			#else
			ACDebug.Log ("The 'tk2DIsPresent' preprocessor is not defined - check your Build Settings.");
			#endif
			
			return true;
		}


		/**
		 * <summary>Stops all animation on a Sprite.</summary>
		 * <param name = "sprite">The Transform with the 2Dtk Sprite</param>
		 */
		public static void StopAnimation (Transform sprite)
		{
			#if tk2DIsPresent
			if (sprite && sprite.GetComponent <tk2dSpriteAnimator>())
			{
				tk2dSpriteAnimator anim = sprite.GetComponent <tk2dSpriteAnimator>();

		    	anim.Stop ();
			}
			#else
			ACDebug.Log ("The 'tk2DIsPresent' preprocessor is not defined - check your Build Settings.");
			#endif
		}
		

		/**
		 * <summary>Checks if a 2Dtk Sprite is playing a specific animation.</summary>
		 * <param name = "sprite">The Transform with the 2Dtk Sprite</param>
		 * <param name = "clipName">The name of the animatino clip to check for</param>
		 * <returns>True if the 2Dtk Sprite is playing the animation</returns>
		 */
		public static bool IsAnimationPlaying (Transform sprite, string clipName)
		{
			#if tk2DIsPresent
			tk2dSpriteAnimator anim = sprite.GetComponent <tk2dSpriteAnimator>();
			tk2dSpriteAnimationClip clip = anim.GetClipByName (clipName);
			
			if (clip != null)
			{
				if (anim.IsPlaying (clip))
				{
					return true;
				}
			}
			#endif
			return false;
		}

	}

}