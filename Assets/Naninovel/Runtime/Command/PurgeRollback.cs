// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.Commands
{
    /// <summary>
    /// Prevents player from rolling back to the previous state snapshots.
    /// </summary>
    public class PurgeRollback : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            Engine.GetService<IStateManager>()?.PurgeRollbackData();
            return UniTask.CompletedTask;
        }
    }
}
