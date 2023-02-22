/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2022
 *	
 *	"CharacterAnimation2DBehaviour.cs"
 * 
 *	A PlayableBehaviour that allows for automatic animation playback of 3D characters, according to how they are moving.
 * 
 */

#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	/** A PlayableBehaviour that allows for automatic animation playback of 3D characters, according to how they are moving. */
	[System.Serializable]
	public class CharacterAnimation3DBehaviour : PlayableBehaviour
	{

		#region Variables

		protected CharacterAnimation3DShot activeShot;

		private float moveScaleFactor = 0.5f;
		private float turnScaleFactor = 0.02f;
		private const float lerpFactor = 5f;
		private const float maxFrameDiff = 0.4f;
		private const float minThreshold = 0.0003f;

		protected Vector3 lastFramePosition;
		protected Vector3 lastFrameForward;
		protected Char character;

		#endregion


		#region PublicFunctions

		public override void OnBehaviourPause (Playable playable, FrameData info)
		{
			if (!Application.isPlaying) return;

			SetOverrideState (false);
			lastFramePosition = Vector3.zero;
		}


		public void Init (CharacterAnimation3DShot _activeShot, float _moveScaleFactor, float _turnScaleFactor)
		{
			moveScaleFactor = 0.5f * _moveScaleFactor;
			turnScaleFactor = 0.02f * _turnScaleFactor;
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
				if (character.GetAnimator () == null)
				{
					return;
				}

				if (character.GetAnimator ().applyRootMotion)
				{
					ACDebug.LogWarning ("The Character Animation 3D Track is not intended for characters that use Root Motion - its effects wil be disabled for " + character, character);
					return;
				}

				SetOverrideState (true);
				UpdateAnimation ();

				lastFramePosition = character.Transform.position;
				lastFrameForward = character.TransformForward;
			}
		}

		#endregion


		#region PrivateFunctions

		private void UpdateAnimation ()
		{
			if (lastFramePosition.sqrMagnitude > 0f)
			{
				UpdateForwardParameter ();
				UpdateTurnParameter ();
			}
		}


		private void UpdateForwardParameter ()
		{
			if (string.IsNullOrEmpty (character.moveSpeedParameter))
			{
				return;
			}

			Vector3 frameDeltaPosition = (character.transform.position - lastFramePosition) / Time.deltaTime;
			float forwardDot = Vector3.Dot (frameDeltaPosition, character.TransformForward) * moveScaleFactor;

			float oldDeltaForward = character.GetAnimator ().GetFloat (character.moveSpeedParameter);
			float newDeltaForward = Mathf.Lerp (oldDeltaForward, forwardDot, Time.deltaTime * lerpFactor);

			newDeltaForward = Mathf.Clamp (newDeltaForward, oldDeltaForward - maxFrameDiff, oldDeltaForward + maxFrameDiff);
			if (Mathf.Abs (newDeltaForward) < minThreshold)
			{
				newDeltaForward = 0f;
			}

			character.GetAnimator ().SetFloat (character.moveSpeedParameter, newDeltaForward);
		}


		private void UpdateTurnParameter ()
		{
			if (string.IsNullOrEmpty (character.turnParameter))
			{
				return;
			}

			float frameDeltaAngle = Vector3.SignedAngle (character.TransformForward, lastFrameForward, -Vector3.up) / Time.deltaTime * turnScaleFactor;

			float oldDeltaAngle = character.GetAnimator ().GetFloat (character.turnParameter);
			float newDeltaAngle = Mathf.Lerp (oldDeltaAngle, frameDeltaAngle, Time.deltaTime * lerpFactor);

			newDeltaAngle = Mathf.Clamp (newDeltaAngle, oldDeltaAngle - maxFrameDiff, oldDeltaAngle + maxFrameDiff);
			if (Mathf.Abs (newDeltaAngle) < minThreshold)
			{
				newDeltaAngle = 0f;
			}
			character.GetAnimator ().SetFloat (character.turnParameter, newDeltaAngle);
		}


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