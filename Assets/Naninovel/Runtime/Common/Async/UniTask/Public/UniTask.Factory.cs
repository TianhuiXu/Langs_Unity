// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Threading;
using Naninovel.Async;

namespace Naninovel
{
    public readonly partial struct UniTask
    {
        private static readonly UniTask CanceledUniTask = new Func<UniTask>(() => {
            var promise = new UniTaskCompletionSource<AsyncUnit>();
            promise.TrySetCanceled();
            return new UniTask(promise);
        })();

        public static UniTask CompletedTask => new UniTask();

        public static UniTask FromException (Exception ex)
        {
            var promise = new UniTaskCompletionSource<AsyncUnit>();
            promise.TrySetException(ex);
            return new UniTask(promise);
        }

        public static UniTask<T> FromException<T> (Exception ex)
        {
            var promise = new UniTaskCompletionSource<T>();
            promise.TrySetException(ex);
            return new UniTask<T>(promise);
        }

        public static UniTask<T> FromResult<T> (T value)
        {
            return new UniTask<T>(value);
        }

        public static UniTask FromCanceled ()
        {
            return CanceledUniTask;
        }

        public static UniTask<T> FromCanceled<T> ()
        {
            return CanceledUniTaskCache<T>.Task;
        }

        public static UniTask FromCanceled (CancellationToken token)
        {
            var promise = new UniTaskCompletionSource<AsyncUnit>();
            promise.TrySetException(new OperationCanceledException(token));
            return new UniTask(promise);
        }

        public static UniTask<T> FromCanceled<T> (CancellationToken token)
        {
            var promise = new UniTaskCompletionSource<T>();
            promise.TrySetException(new OperationCanceledException(token));
            return new UniTask<T>(promise);
        }

        /// <summary>shorthand of new UniTask[T](Func[UniTask[T]] factory)</summary>
        public static UniTask<T> Lazy<T> (Func<UniTask<T>> factory)
        {
            return new UniTask<T>(factory);
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooEvent += () => UniTask.Void(async () => { /* */ })
        /// </summary>
        public static void Void (Func<UniTask> asyncAction)
        {
            asyncAction().Forget();
        }

        /// <summary>
        /// helper of create add UniTaskVoid to delegate.
        /// For example: FooEvent += (sender, e) => UniTask.Void(async arg => { /* */ }, (sender, e))
        /// </summary>
        public static void Void<T> (Func<T, UniTask> asyncAction, T state)
        {
            asyncAction(state).Forget();
        }

        private static class CanceledUniTaskCache<T>
        {
            public static readonly UniTask<T> Task;

            static CanceledUniTaskCache ()
            {
                var promise = new UniTaskCompletionSource<T>();
                promise.TrySetCanceled();
                Task = new UniTask<T>(promise);
            }
        }
    }

    internal static class CompletedTasks
    {
        public static readonly UniTask<bool> True = UniTask.FromResult(true);
        public static readonly UniTask<bool> False = UniTask.FromResult(false);
        public static readonly UniTask<int> Zero = UniTask.FromResult(0);
        public static readonly UniTask<int> MinusOne = UniTask.FromResult(-1);
        public static readonly UniTask<int> One = UniTask.FromResult(1);
    }
}
