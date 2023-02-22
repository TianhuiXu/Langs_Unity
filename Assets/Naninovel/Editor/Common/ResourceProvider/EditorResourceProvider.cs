// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides resources stored anywhere inside the project's `Assets` folder using provided path to GUID map; works only in the editor.
    /// </summary>
    public class EditorResourceProvider : ResourceProvider
    {
        private readonly Dictionary<string, string> pathToGuidMap = new Dictionary<string, string>();

        public void AddResourceGuid (string path, string guid)
        {
            pathToGuidMap[path] = guid;
            LocationsCache.Add(new CachedResourceLocation(path, typeof(UnityEngine.Object)));
        }

        public void RemoveResourceGuid (string path)
        {
            pathToGuidMap.Remove(path);
            LocationsCache.RemoveAll(r => r.Path.EqualsFast(path));
        }

        public override bool SupportsType<T> () => true;

        protected override LoadResourceRunner<T> CreateLoadResourceRunner<T> (string path)
        {
            return new EditorResourceLoader<T>(this, path, pathToGuidMap, LogMessage);
        }

        protected override LocateResourcesRunner<T> CreateLocateResourcesRunner<T> (string path)
        {
            return new EditorResourceLocator<T>(this, path, pathToGuidMap.Keys);
        }

        protected override LocateFoldersRunner CreateLocateFoldersRunner (string path)
        {
            return new EditorFolderLocator(this, path, pathToGuidMap.Keys);
        }

        protected override void DisposeResource (Resource resource)
        {
            if (!resource.Valid) return;
            #if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.Contains(resource.Object))
            {
                // Can't unload prefabs: https://forum.unity.com/threads/393385.
                if (resource.Object is GameObject || resource.Object is Component) return;
                Resources.UnloadAsset(resource.Object);
            }
            else ObjectUtils.DestroyOrImmediate(resource.Object);
            #endif
        }

        protected override bool AreTypesCompatible (Type sourceType, Type targetType) => true;
    }
}
