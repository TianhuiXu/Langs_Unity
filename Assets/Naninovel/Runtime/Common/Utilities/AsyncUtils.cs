// Copyright 2022 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    public static class AsyncUtils
    {
        public static UniTask.Awaiter GetAwaiter (this UniTask? task)
        {
            return task?.GetAwaiter() ?? UniTask.CompletedTask.GetAwaiter();
        }

        public static UniTask<T>.Awaiter GetAwaiter<T> (this UniTask<T>? task)
        {
            return task?.GetAwaiter() ?? UniTask.FromResult<T>(default).GetAwaiter();
        }

        /// <summary>
        /// Waits till the end of the current update loop. Doesn't allocate on heap.
        /// </summary>
        public static YieldAwaitable WaitEndOfFrameAsync (AsyncToken asyncToken = default)
        {
            return UniTask.Yield(PlayerLoopTiming.PostLateUpdate, asyncToken);
        }

        public static async UniTask DelayFrameAsync (int frameCount, AsyncToken asyncToken = default)
        {
            await UniTask.DelayFrame(frameCount);
            asyncToken.ThrowIfCanceled();
        }
    }
}
