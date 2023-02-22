// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;

namespace Naninovel
{
    public class EditorResourceLocator<TResource> : LocateResourcesRunner<TResource>
        where TResource : UnityEngine.Object
    {
        private readonly IReadOnlyCollection<string> editorResourcePaths;

        public EditorResourceLocator (IResourceProvider provider, string resourcesPath,
            IReadOnlyCollection<string> editorResourcePaths) : base(provider, resourcesPath ?? string.Empty)
        {
            this.editorResourcePaths = editorResourcePaths;
        }

        public override UniTask RunAsync ()
        {
            var locatedResourcePaths = LocateProjectResources(Path, editorResourcePaths);
            SetResult(locatedResourcePaths);
            return UniTask.CompletedTask;
        }

        public static IReadOnlyCollection<string> LocateProjectResources (string path, IReadOnlyCollection<string> editorResourcePaths)
        {
            return editorResourcePaths.LocateResourcePathsAtFolder(path).ToArray();
        }
    }
}
