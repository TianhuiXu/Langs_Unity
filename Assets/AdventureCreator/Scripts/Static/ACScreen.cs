#if UNITY_IOS || UNITY_ANDROID || UNITY_TVOS
#define MOBILE_PLATFORM
#endif

using UnityEngine;

namespace AC
{

	public static class ACScreen
	{

		#if UNITY_EDITOR
		private static int cachedWidth = 0;
		private static int cachedHeight = 0;
		#endif

		public static int width
		{
			get
			{
				#if UNITY_EDITOR
				if (!Application.isPlaying) return Screen.width;
				if (cachedWidth == 0) UpdateCache ();
				return cachedWidth;
				#else
				return Screen.width;
				#endif
			}
		}


		public static int height
		{
			get
			{
				#if UNITY_EDITOR
				if (!Application.isPlaying) return Screen.height;
				if (cachedHeight == 0) UpdateCache ();
				return cachedHeight;
				#else
				return Screen.height;
				#endif
			}
		}


		public static Rect safeArea
		{
			get
			{
				#if UNITY_EDITOR
				return new Rect (0f, 0f, width, height);
				#else

					#if MOBILE_PLATFORM
					if (!KickStarter.settingsManager.relyOnSafeArea)
					{
						return new Rect (0f, 0f, width, height);
					}
					#endif

				return Screen.safeArea;
				#endif
			}
		}


		public static int LongestDimension
		{
			get
			{
				#if UNITY_EDITOR
				if (!Application.isPlaying) return Screen.height > Screen.width ? Screen.height : Screen.width;
				if (cachedHeight == 0) UpdateCache ();
				return cachedHeight > cachedWidth ? cachedHeight : cachedWidth;
				#else
				return Screen.height > Screen.width ? Screen.height : Screen.width;
				#endif
			}
		}


		#if UNITY_EDITOR

		public static void UpdateCache ()
		{
			cachedWidth = Screen.width;
			cachedHeight = Screen.height;
		}

		#endif

	}

}