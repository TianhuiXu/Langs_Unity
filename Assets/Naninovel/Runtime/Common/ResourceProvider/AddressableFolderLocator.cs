// Copyright 2022 ReWaffle LLC. All rights reserved.

#if ADDRESSABLES_AVAILABLE

using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Naninovel
{
    public class AddressableFolderLocator : LocateFoldersRunner
    {
        private readonly List<IResourceLocation> locations;

        public AddressableFolderLocator (AddressableResourceProvider provider, string resourcePath, List<IResourceLocation> locations)
            : base(provider, resourcePath)
        {
            this.locations = locations;
        }

        public override UniTask RunAsync ()
        {
            var locatedResourcePaths = locations
                .Select(l => l.PrimaryKey.GetAfterFirst("/")) // Remove the addressables prefix.
                .LocateFolderPathsAtFolder(Path)
                .Select(p => new Folder(p)).ToArray();
            SetResult(locatedResourcePaths);

            return UniTask.CompletedTask;
        }
    }
}

#endif
