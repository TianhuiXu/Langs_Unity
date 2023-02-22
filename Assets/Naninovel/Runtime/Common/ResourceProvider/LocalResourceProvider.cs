// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    public class LocalResourceProvider : ResourceProvider
    {
        public string RootPath { get; }

        private readonly Dictionary<Type, List<IConverter>> converters = new Dictionary<Type, List<IConverter>>();

        /// <param name="rootPath">
        /// An absolute path to the folder where the resources are located,
        /// or a relative path with one of the available origins:
        /// - %DATA% - <see cref="Application.dataPath"/>
        /// - %PDATA% - <see cref="Application.persistentDataPath"/>
        /// - %STREAM% - <see cref="Application.streamingAssetsPath"/>
        /// - %SPECIAL{F}%, where F is a value from <see cref="Environment.SpecialFolder"/> - <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/>
        /// </param>
        public LocalResourceProvider (string rootPath)
        {
            if (rootPath.StartsWith("%DATA%")) RootPath = string.Concat(Application.dataPath, rootPath.GetAfterFirst("%DATA%"));
            else if (rootPath.StartsWith("%PDATA%")) RootPath = string.Concat(Application.persistentDataPath, rootPath.GetAfterFirst("%PDATA%"));
            else if (rootPath.StartsWith("%STREAM%")) RootPath = string.Concat(Application.streamingAssetsPath, rootPath.GetAfterFirst("%STREAM%"));
            else if (rootPath.StartsWith("%SPECIAL{"))
            {
                var specialFolderStr = rootPath.GetBetween("%SPECIAL{", "}%");
                if (!Enum.TryParse<Environment.SpecialFolder>(specialFolderStr, true, out var specialFolder))
                    throw new Error($"Failed to parse `{rootPath}` special folder path for local resource provider root.");
                RootPath = string.Concat(Environment.GetFolderPath(specialFolder), rootPath.GetAfterFirst("}%"));
            }
            else RootPath = rootPath; // Absolute path.

            RootPath = RootPath.Replace("\\", "/");
        }

        public override bool SupportsType<T> () => converters.ContainsKey(typeof(T));

        /// <summary>
        /// Adds a resource type converter.
        /// </summary>
        public virtual void AddConverter<T> (IRawConverter<T> converter)
        {
            var key = typeof(T);
            if (!converters.ContainsKey(key))
                converters[key] = new List<IConverter>();
            converters[key].Add(converter);
        }

        protected override LoadResourceRunner<T> CreateLoadResourceRunner<T> (string path)
        {
            return new LocalResourceLoader<T>(this, RootPath, path, ResolveConverters<T>(), LogMessage);
        }

        protected override LocateResourcesRunner<T> CreateLocateResourcesRunner<T> (string path)
        {
            return new LocalResourceLocator<T>(this, RootPath, path, ResolveConverters<T>());
        }

        protected override LocateFoldersRunner CreateLocateFoldersRunner (string path)
        {
            return new LocalFolderLocator(this, RootPath, path);
        }

        protected override void DisposeResource (Resource resource)
        {
            if (!resource.Valid) return;
            ObjectUtils.DestroyOrImmediate(resource.Object);
        }

        protected virtual IEnumerable<IRawConverter<T>> ResolveConverters<T> ()
        {
            var resourceType = typeof(T);
            if (!converters.ContainsKey(resourceType))
                throw new Error($"Converter for resource of type '{resourceType.Name}' is not available.");
            return converters[resourceType].Cast<IRawConverter<T>>();
        }
    }
}
