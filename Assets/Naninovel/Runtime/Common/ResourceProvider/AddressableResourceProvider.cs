// Copyright 2022 ReWaffle LLC. All rights reserved.

#if ADDRESSABLES_AVAILABLE

using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Naninovel
{
    public class AddressableResourceProvider : ResourceProvider
    {
        /// <summary>
        /// All the assets managed by this provider should have this label assigned, 
        /// also their addresses are expected to start with the label followed by a slash.
        /// </summary>
        public virtual string MainLabel { get; }
        /// <summary>
        /// When specified, the provider will only work with assets that have the set of labels.
        /// </summary>
        public virtual IReadOnlyCollection<string> ExtraLabels { get; }

        private List<IResourceLocation> locations;

        public AddressableResourceProvider (string mainLabel = "UNITY_COMMON", string[] extraLabels = null)
        {
            MainLabel = mainLabel;
            if (extraLabels != null && extraLabels.Length > 0)
            {
                var labels = new List<string>(extraLabels);
                labels.Add(mainLabel);
                ExtraLabels = labels.ToArray();
            }
        }

        public override bool SupportsType<T> () => true;

        public override async UniTask<Resource<T>> LoadResourceAsync<T> (string path)
        {
            if (locations is null) locations = await LoadAllLocations();
            return await base.LoadResourceAsync<T>(path);
        }

        public override async UniTask<IReadOnlyCollection<string>> LocateResourcesAsync<T> (string path)
        {
            if (locations is null) locations = await LoadAllLocations();
            return await base.LocateResourcesAsync<T>(path);
        }

        public override async UniTask<IReadOnlyCollection<Folder>> LocateFoldersAsync (string path)
        {
            if (locations is null) locations = await LoadAllLocations();
            return await base.LocateFoldersAsync(path);
        }

        protected override LoadResourceRunner<T> CreateLoadResourceRunner<T> (string path)
        {
            return new AddressableResourceLoader<T>(this, path, locations, LogMessage);
        }

        protected override LocateResourcesRunner<T> CreateLocateResourcesRunner<T> (string path)
        {
            return new AddressableResourceLocator<T>(this, path, locations);
        }

        protected override LocateFoldersRunner CreateLocateFoldersRunner (string path)
        {
            return new AddressableFolderLocator(this, path, locations);
        }

        protected override void DisposeResource (Resource resource)
        {
            if (!resource.Valid) return;

            Addressables.Release(resource.Object);
        }

        protected virtual async UniTask<List<IResourceLocation>> LoadAllLocations ()
        {
            // ReSharper disable once CoVariantArrayConversion
            var task = ExtraLabels != null
                ? Addressables.LoadResourceLocationsAsync(ExtraLabels, Addressables.MergeMode.Intersection)
                : Addressables.LoadResourceLocationsAsync(MainLabel);
            while (!task.IsDone) // When awaiting the method directly it fails on WebGL (they're using multithreaded Task fot GetAwaiter)
                await AsyncUtils.WaitEndOfFrameAsync();
            var locations = task.Result?.ToList() ?? new List<IResourceLocation>();
            CacheLocations(locations);
            return locations;
        }

        protected virtual void CacheLocations (IEnumerable<IResourceLocation> locations)
        {
            foreach (var location in locations)
            {
                var path = location.PrimaryKey.GetAfterFirst("/"); // Remove the addressables prefix.
                LocationsCache.Add(new CachedResourceLocation(path, location.ResourceType));
            }
        }
    }
}

#endif
