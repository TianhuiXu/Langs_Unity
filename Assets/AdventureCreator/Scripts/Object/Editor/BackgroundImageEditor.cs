#if UNITY_STANDALONE && UNITY_EDITOR

//#if !UNITY_SWITCH
#define ALLOW_VIDEO
//#endif

using UnityEngine;
using UnityEditor;
#if ALLOW_VIDEO
using UnityEngine.Video;
#endif

namespace AC
{

	[CustomEditor (typeof (BackgroundImage))]
	public class BackgroundImageEditor : Editor
	{
		
		private BackgroundImage _target;


		private void OnEnable ()
		{
			_target = (BackgroundImage) target;
		}
		
		
		public override void OnInspectorGUI ()
		{
			CustomGUILayout.BeginVertical ();

			_target = ShowUnityUIMethod (_target);

			#if ALLOW_VIDEO
			if (_target.backgroundImageSource == BackgroundImage.BackgroundImageSource.VideoClip)
			{
				CustomGUILayout.EndVertical ();
				UnityVersionHandler.CustomSetDirty (_target);
				return;
			}
			#endif

			#if UNITY_STANDALONE && !UNITY_2018_2_OR_NEWER

			EditorGUILayout.LabelField ("When playing a MovieTexture:");
			_target.loopMovie = CustomGUILayout.Toggle ("Loop clip?", _target.loopMovie, string.Empty, "If True, then any MovieTexture set as the background will be looped");
			_target.restartMovieWhenTurnOn = CustomGUILayout.Toggle ("Restart clip each time?", _target.restartMovieWhenTurnOn, string.Empty, "If True, then any MovieTexture set as the background will start from the beginning when the associated Camera is activated");

			#endif

			CustomGUILayout.EndVertical ();
			UnityVersionHandler.CustomSetDirty (_target);
		}


		private BackgroundImage ShowUnityUIMethod (BackgroundImage _target)
		{
			#if ALLOW_VIDEO

			_target.backgroundImageSource = (BackgroundImage.BackgroundImageSource) CustomGUILayout.EnumPopup ("Background type:", _target.backgroundImageSource, string.Empty, "What type of asset is used as a background");
			switch (_target.backgroundImageSource)
			{
				case BackgroundImage.BackgroundImageSource.Texture:
					_target.backgroundTexture = (Texture) CustomGUILayout.ObjectField <Texture> ("Texture:", _target.backgroundTexture, false, string.Empty, "The texture to display full-screen");
					break;

				case BackgroundImage.BackgroundImageSource.VideoClip:
					_target.backgroundTexture = (Texture) CustomGUILayout.ObjectField <Texture> ("Placeholder texture:", _target.backgroundTexture, false, string.Empty, "The texture to display full-screen while the VideoClip is being prepared");
					_target.backgroundVideo = (VideoClip) CustomGUILayout.ObjectField <VideoClip> ("Video clip:", _target.backgroundVideo, false, string.Empty, "The VideoClip to animate full-screen");
					break;
			}

			#else

			_target.backgroundTexture = (Texture) CustomGUILayout.ObjectField <Texture> ("Background texture:", _target.backgroundTexture, false, string.Empty, "The texture to display full-screen");

			#endif

			return _target;
		}

	}

}

#endif