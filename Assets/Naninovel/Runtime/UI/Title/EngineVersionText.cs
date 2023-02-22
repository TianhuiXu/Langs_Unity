// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(Text))]
    public class EngineVersionText : MonoBehaviour
    {
        private void Start ()
        {
            var version = EngineVersion.LoadFromResources();
            GetComponent<Text>().text = $"Naninovel v{version.Version}{Environment.NewLine}Build {version.Build}";
        }

    }
}
