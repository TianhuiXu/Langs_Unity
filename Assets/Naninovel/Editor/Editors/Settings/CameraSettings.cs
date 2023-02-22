// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class CameraSettings : ConfigurationSettings<CameraConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers ()
        {
            var drawers = base.OverrideConfigurationDrawers();
            drawers[nameof(CameraConfiguration.CustomUICameraPrefab)] = p => DrawWhen(Configuration.UseUICamera, p);
            return drawers;
        }
    }
}
