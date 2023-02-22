// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ManagedTextConfiguration : Configuration
    {
        public const string DefaultPathPrefix = "Text";

        [Tooltip("Configuration of the resource loader used with the managed text documents.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultPathPrefix };
    }
}
