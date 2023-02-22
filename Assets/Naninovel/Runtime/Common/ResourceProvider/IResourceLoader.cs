// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to load and unload <see cref="Resource"/> objects, agnostic to the provision source.
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>
        /// Given provided resource is loaded by this loader,
        /// returns local (to the loader) path of the resource, null otherwise.
        /// </summary>
        string GetLocalPath (Resource resource);
        /// <summary>
        /// Checks whether a resource with the provided path is loaded.
        /// </summary>
        bool IsLoaded (string path);
        /// <summary>
        /// Returns a resource with the provided path in case it's loaded, null otherwise.
        /// </summary>
        Resource GetLoadedOrNull (string path);
        /// <summary>
        /// Returns all the currently loaded resources.
        /// </summary>
        IReadOnlyCollection<Resource> GetAllLoaded ();
        /// <summary>
        /// Attempts to load a resource with the provided path.
        /// </summary>
        UniTask<Resource> LoadAsync (string path);
        /// <summary>
        /// Attempts to load all the available resources (optionally) filtered by a base path.
        /// </summary>
        UniTask<IReadOnlyCollection<Resource>> LoadAllAsync (string path = null);
        /// <summary>
        /// Locates paths of all the available resources (optionally) filtered by a base path.
        /// </summary>
        UniTask<IReadOnlyCollection<string>> LocateAsync (string path);
        /// <summary>
        /// Checks whether a resource with the provided path is available (can be loaded).
        /// </summary>
        UniTask<bool> ExistsAsync (string path);
        /// <summary>
        /// Unloads a resource with the provided path.
        /// </summary>
        void Unload (string path);
        /// <summary>
        /// Unloads all the currently loaded resources.
        /// </summary>
        void UnloadAll ();
        /// <summary>
        /// Registers the provided object as a holder of a loaded resource with the specified path.
        /// The resource won't be unloaded by <see cref="Release"/> while it's held by at least one object.
        /// </summary>
        void Hold (string path, object holder);
        /// <summary>
        /// Removes the provided object from the holders list of a loaded resource with the specified path.
        /// Will (optionally) unload the resource after the release in case no other objects are holding it.
        /// </summary>
        void Release (string path, object holder, bool unload = true);
        /// <summary>
        /// Removes the provided holder object from all the loaded resources.
        /// Will (optionally) unload the affected resources after the release in case no other objects are holding them.
        /// </summary>
        void ReleaseAll (object holder, bool unload = true);
        /// <summary>
        /// Checks whether a loaded resource with the provided path is being held by the object.
        /// </summary>
        bool IsHeldBy (string path, object holder);
        /// <summary>
        /// Returns number of objects currently holding a loaded resource with the specified path.
        /// </summary>
        int CountHolders (string path);
    }

    /// <summary>
    /// Implementation is able to load and unload <see cref="Resource{TResource}"/> objects, agnostic to the provision source.
    /// </summary>
    public interface IResourceLoader<TResource> : IResourceLoader
        where TResource : Object
    {
        /// <inheritdoc cref="IResourceLoader.GetLoadedOrNull"/>
        new Resource<TResource> GetLoadedOrNull (string path);
        /// <inheritdoc cref="IResourceLoader.GetAllLoaded"/>
        new IReadOnlyCollection<Resource<TResource>> GetAllLoaded ();
        /// <inheritdoc cref="IResourceLoader.LoadAsync"/>
        new UniTask<Resource<TResource>> LoadAsync (string path);
        /// <inheritdoc cref="IResourceLoader.LoadAllAsync"/>
        new UniTask<IReadOnlyCollection<Resource<TResource>>> LoadAllAsync (string path = null);
    }
}
