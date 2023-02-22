/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CharacterAnimation2DBehaviour.cs"
 * 
 *	A PlayableBehaviour that allows for automatic animation playback of sprite-based characters, according to how they are moving.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	/**
	 * A PlayableBehaviour that allows for automatic animation playback of sprite-based characters, according to how they are moving.
	 */
	[System.Serializable]
	public class CharacterAnimation2DBehaviour : PlayableBehaviour
	{

		#region Variables

		protected CharDirection charDirection;
		protected bool turnInstantly;
		protected CharacterAnimation2DShot activeShot;
		protected bool forceDirection;
		protected PathSpeed moveSpeed;

		protected Vector3 lastFramePosition;
		protected Char character;

		#endregion


		#region PublicFunctions

		public override void OnBehaviourPause (Playable playable, FrameData info)
	    {
			if (!Application.isPlaying) return;

			SetOverrideState (false);
			lastFramePosition = Vector3.zero;
		}


		public void Init (PathSpeed _moveSpeed, bool _turnInstantly, bool _forceDirection, CharDirection _charDirection, CharacterAnimation2DShot _activeShot)
		{
			moveSpeed = _moveSpeed;
			forceDirection = _forceDirection;
			charDirection = _charDirection;
			turnInstantly = _turnInstantly;
			activeShot = _activeShot;
		}

	  
		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			if (!Application.isPlaying) return;
			character = playerData as AC.Char;

			if (character == null)
			{
				GameObject characterObject = playerData as GameObject;
				if (characterObject)
				{
					character = characterObject.GetComponent <AC.Char>();
				}
			}

			if (character)
			{
				if (character.GetAnimEngine () == null)
				{
					ACDebug.LogWarning ("The 2D character animation track requires that " + character + "'s has an animation engine.");
					return;
				}

				if (!character.GetAnimEngine ().isSpriteBased)
				{
					ACDebug.LogWarning ("The 2D character animation track requires that " + character + "'s animation is sprite-based.");
					return;
				}

				if (character.turn2DCharactersIn3DSpace)
				{
					ACDebug.LogWarning ("For the 2D character animation track to work, " + character + "'s 'Turn root object in 3D?' must be unchecked.");
					return;
				}

				if (forceDirection)
				{
					Vector3 lookVector = AdvGame.GetCharLookVector (charDirection);
					character.SetLookDirection (lookVector, turnInstantly);
				}

				if (lastFramePosition != Vector3.zero)
				{
					Vector3 deltaPosition = character.Transform.position - lastFramePosition;
					deltaPosition *= Time.deltaTime * 1000f;
					
					if (Mathf.Approximately (deltaPosition.sqrMagnitude, 0f))
					{
						if (character.isTalking && (character.talkingAnimation == TalkingAnimation.Standard || character.animationEngine == AnimationEngine.Custom))
						{
							character.GetAnimEngine ().PlayTalk ();
						}
						else
						{
							character.GetAnimEngine ().PlayIdle ();
						}
						SetOverrideState (false);
					}
					else
					{
						SetOverrideState (true);

						switch (moveSpeed)
						{
							case PathSpeed.Walk:
								character.GetAnimEngine ().PlayWalk ();
								break;

							case PathSpeed.Run:
								character.GetAnimEngine ().PlayRun ();
								break;

							default:
								break;
						}

						if (!forceDirection)
						{
							Vector3 lookVector = new Vector3 (deltaPosition.x, 0f, deltaPosition.y);
							character.SetLookDirection (lookVector, turnInstantly);
						}
					}
				}

				lastFramePosition = character.Transform.position;
			}
		}

		#endregion


		#region PrivateFunctions

		private void SetOverrideState (bool enable)
		{
			if (character)
			{
				if (enable)
				{
					character.ActiveCharacterAnimationShot = activeShot;
				}
				else
				{
					if (character.ActiveCharacterAnimationShot == activeShot)
					{
						character.ActiveCharacterAnimationShot = null;
					}
				}
			}
		}

		#endregion

	}

}

#endif