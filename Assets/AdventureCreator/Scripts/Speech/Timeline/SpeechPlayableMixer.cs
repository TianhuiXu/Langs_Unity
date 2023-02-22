#if !ACIgnoreTimeline

using UnityEngine;
using UnityEngine.Playables;

namespace AC
{

	internal sealed class SpeechPlayableMixer : PlayableBehaviour
	{

		#region Variables

		public int trackInstanceID;
		public SpeechTrackPlaybackMode playbackMode;
		private Char speaker;
		private bool speakerSet;

		#endregion


		#region PublicFunctions

		public override void OnGraphStop (Playable playable)
		{
			if (speakerSet && playable.GetInputCount () > 0)
			{
				StopSpeaking ();
			}
		}


		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			base.ProcessFrame (playable, info, playerData);

			if (!speakerSet)
			{
				ScriptPlayable <SpeechPlayableBehaviour> clip = (ScriptPlayable <SpeechPlayableBehaviour>) playable.GetInput (0);
				SpeechPlayableBehaviour shot = clip.GetBehaviour ();
				if (shot != null)
				{
					speakerSet = true;
					speaker = shot.Speaker;
				}
			}

			for (int i=0; i<playable.GetInputCount (); ++i)
			{
				float weight = playable.GetInputWeight (i);
				if (weight > 0f)
				{
					return;
				}
			}

			if (playbackMode == SpeechTrackPlaybackMode.ClipDuration || !Application.isPlaying)
			{
				StopSpeaking ();
			}
		}

		#endregion


		#region PrivateFunctions

		private void StopSpeaking ()
		{
			if (!Application.isPlaying)
			{
				#if UNITY_EDITOR
				if (KickStarter.menuPreview)
				{
					KickStarter.menuPreview.ClearPreviewSpeech (trackInstanceID);
				}
				#endif
			}
			else
			{
				if (KickStarter.dialog)
				{
					KickStarter.dialog.EndSpeechByCharacter (speaker);
				}
			}
		}

		#endregion

	}

}

#endif