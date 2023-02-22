using UnityEngine;

namespace AC
{

	public static class ACDebug
	{

		private static string hr = "\n\n -> AC debug logger";


		public static void Log (object message, UnityEngine.Object context = null)
		{
			if (KickStarter.eventManager)
			{
				message = KickStarter.eventManager.Call_OnDebugLog (message, DebugLogType.Info, context, CanDisplay (DebugLogType.Info));
			}
			if (CanDisplay (DebugLogType.Info))
			{
				Debug.Log (message + hr, context);
			}
		}


		public static void LogWarning (object message, UnityEngine.Object context = null)
		{
			if (KickStarter.eventManager)
			{
				message = KickStarter.eventManager.Call_OnDebugLog (message, DebugLogType.Warning, context, CanDisplay (DebugLogType.Warning));
			}
			if (CanDisplay (DebugLogType.Warning))
			{
				Debug.LogWarning (message + hr, context);
			}
		}


		public static void LogError (object message, UnityEngine.Object context = null)
		{
			if (KickStarter.eventManager)
			{
				message = KickStarter.eventManager.Call_OnDebugLog (message, DebugLogType.Error, context, CanDisplay (DebugLogType.Error));
			}
			if (CanDisplay (DebugLogType.Error))
			{
				Debug.LogError (message + hr, context);
			}
		}


		public static void Log (object message, ActionList actionList, AC.Action action, UnityEngine.Object context = null)
		{
			if (KickStarter.eventManager)
			{
				message = KickStarter.eventManager.Call_OnDebugLog (message, DebugLogType.Info, context, CanDisplay (DebugLogType.Info));
			}
			if (CanDisplay (DebugLogType.Info))
			{
				if (context == null) context = actionList;
				Debug.Log (message + GetActionListSuffix (actionList, action) + hr, context);
			}
		}


		public static void LogWarning (object message, ActionList actionList, AC.Action action, UnityEngine.Object context = null)
		{
			if (KickStarter.eventManager)
			{
				message = KickStarter.eventManager.Call_OnDebugLog (message, DebugLogType.Warning, context, CanDisplay (DebugLogType.Warning));
			}
			if (CanDisplay (DebugLogType.Warning))
			{
				if (context == null) context = actionList;
				Debug.LogWarning (message + GetActionListSuffix (actionList, action) + hr, context);
			}
		}


		public static void LogError (object message, ActionList actionList, AC.Action action, UnityEngine.Object context = null)
		{
			if (KickStarter.eventManager)
			{
				message = KickStarter.eventManager.Call_OnDebugLog (message, DebugLogType.Error, context, CanDisplay (DebugLogType.Error));
			}
			if (CanDisplay (DebugLogType.Error))
			{
				if (context == null) context = actionList;
				Debug.LogError (message + GetActionListSuffix (actionList, action) + hr, context);
			}
		}


		private static string GetActionListSuffix (ActionList actionList, AC.Action action)
		{
			if (actionList && actionList.actions.Contains (action))
			{
				return ("\n(From Action #" + actionList.actions.IndexOf (action) + " in ActionList '" + actionList.name + "')");
			}
			else if (action != null)
			{
				return ("\n(From Action '" + action.Category + ": " + action.Title + "')");
			}
			return string.Empty;
		}


		private static bool CanDisplay (DebugLogType debugLogType)
		{
			#if UNITY_EDITOR
			if (KickStarter.stateHandler == null)
			{
				return true;
			}
			#endif
			if (KickStarter.settingsManager)
			{
				switch (KickStarter.settingsManager.showDebugLogs)
				{
					case ShowDebugLogs.Always:
						return true;

					case ShowDebugLogs.Never:
						return false;

					case ShowDebugLogs.OnlyWarningsOrErrors:
						return debugLogType != DebugLogType.Info;

					default:
						return true;
				}
			}
			return true;
		}

	}

}