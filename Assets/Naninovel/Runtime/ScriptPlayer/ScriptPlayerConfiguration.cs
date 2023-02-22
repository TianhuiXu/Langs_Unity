// Copyright 2022 ReWaffle LLC. All rights reserved.

using Naninovel.Commands;
using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ScriptPlayerConfiguration : Configuration
    {
        [Tooltip("Default skip mode to set when the game is first started.")]
        public PlayerSkipMode DefaultSkipMode = PlayerSkipMode.ReadOnly;
        [Tooltip("Time scale to use when in skip (fast-forward) mode."), Range(1f, 100f)]
        public float SkipTimeScale = 10f;
        [Tooltip("Minimum seconds to wait before executing next command while in auto play mode.")]
        public float MinAutoPlayDelay = 3f;
        [Tooltip("Whether to instantly complete blocking (`wait:true`) commands performed over time (eg, animations, hide/reveal, tint changes, etc) when `Continue` input is activated.")]
        public bool CompleteOnContinue = true;
        [Tooltip("Whether to show player debug window on engine initialization.")]
        public bool ShowDebugOnInit;
        [Tooltip("Whether to wait the played commands when the `wait` parameter is not explicitly specified.")]
        public bool WaitByDefault = true;

        public virtual bool ShouldWait (Command command)
        {
            if (command.ForceWait) return true;
            if (Command.Assigned(command.Wait)) return command.Wait;
            return WaitByDefault || command is Wait;
        }
    }
}
