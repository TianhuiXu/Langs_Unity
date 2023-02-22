// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Threading;
using UnityEngine;

namespace Naninovel
{
    public static class SyncContextUtils
    {
        public static SynchronizationContext UnitySynchronizationContext { get; private set; }

        /// <summary>
        /// Provided action will be invoked on the main thread no matter from which thread this is used.
        /// </summary>
        public static void InvokeOnUnityThread (Action action)
        {
            if (SynchronizationContext.Current == UnitySynchronizationContext) action?.Invoke();
            else UnitySynchronizationContext.Post(_ => action(), null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize ()
        {
            UnitySynchronizationContext = SynchronizationContext.Current;
        }
    }
}
