// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class InputSettings : ConfigurationSettings<InputConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(InputConfiguration.CustomEventSystem)] = p => DrawWhen(Configuration.SpawnEventSystem, p);
            drawers[nameof(InputConfiguration.CustomInputModule)] = p => DrawWhen(Configuration.SpawnInputModule, p);
            return drawers;
        }
    }
}
