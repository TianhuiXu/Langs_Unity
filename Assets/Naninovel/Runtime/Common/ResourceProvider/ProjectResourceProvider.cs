// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides resources stored in the 'Resources' folders of the project.
    /// </summary>
    public class ProjectResourceProvider : ResourceProvider
    {
        public class TypeRedirector
        {
            public virtual Type SourceType { get; }
            public virtual Type RedirectType { get; }
            public virtual IConverter RedirectToSourceConverter { get; }

            public TypeRedirector (Type sourceType, Type redirectType, IConverter redirectToSourceConverter)
            {
                SourceType = sourceType;
                RedirectType = redirectType;
                RedirectToSourceConverter = redirectToSourceConverter;
            }

            public virtual async UniTask<TSource> ToSourceAsync<TSource> (object obj, string name)
            {
                return (TSource)await RedirectToSourceConverter.ConvertAsync(obj, name);
            }

            public virtual TSource ToSource<TSource> (object obj, string name)
            {
                return (TSource)RedirectToSourceConverter.Convert(obj, name);
            }
        }

        public string RootPath { get; }

        private readonly IReadOnlyDictionary<string, Type> projectResources;
        private readonly Dictionary<Type, TypeRedirector> redirectors;

        public ProjectResourceProvider (string rootPath = null)
        {
            RootPath = rootPath;
            projectResources = GetProjectResources();
            redirectors = new Dictionary<Type, TypeRedirector>();
            foreach (var kv in projectResources)
                LocationsCache.Add(new CachedResourceLocation(kv.Key, kv.Value));

            IReadOnlyDictionary<string, Type> GetProjectResources ()
            {
                var filter = string.IsNullOrEmpty(RootPath) ? null : $"{RootPath}/";
                return ProjectResources.Get().GetAllResources(filter);
            }
        }

        public override bool SupportsType<T> () => true;

        public virtual void AddRedirector<TSource, TRedirect> (IConverter<TRedirect, TSource> redirectToSourceConverter)
        {
            var sourceType = typeof(TSource);
            if (!redirectors.ContainsKey(sourceType))
            {
                var redirector = new TypeRedirector(sourceType, typeof(TRedirect), redirectToSourceConverter);
                redirectors.Add(redirector.SourceType, redirector);
            }
        }

        protected override LoadResourceRunner<T> CreateLoadResourceRunner<T> (string path)
        {
            return new ProjectResourceLoader<T>(this, RootPath, path, redirectors.ContainsKey(typeof(T)) ? redirectors[typeof(T)] : null, LogMessage);
        }

        protected override LocateResourcesRunner<T> CreateLocateResourcesRunner<T> (string path)
        {
            return new ProjectResourceLocator<T>(this, path, projectResources);
        }

        protected override LocateFoldersRunner CreateLocateFoldersRunner (string path)
        {
            return new ProjectFolderLocator(this, path, projectResources);
        }

        protected override void DisposeResource (Resource resource)
        {
            if (!resource.Valid) return;

            // Non-asset resources are created when using type redirectors.
            if (redirectors.Count > 0 && redirectors.ContainsKey(resource.Object.GetType()))
            {
                ObjectUtils.DestroyOrImmediate(resource.Object);
                return;
            }

            // Can't unload prefabs: https://forum.unity.com/threads/393385.
            if (resource.Object is GameObject || resource.Object is Component) return;

            Resources.UnloadAsset(resource.Object);
        }

        protected override bool AreTypesCompatible (Type sourceType, Type targetType)
        {
            return base.AreTypesCompatible(sourceType, targetType) ||
                   redirectors.Values.Any(r => r.SourceType == sourceType && r.RedirectType == targetType);
        }
    }
}
