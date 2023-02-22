// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a directory in the project assets.
    /// </summary>
    [System.Serializable]
    public class Folder
    {
        public string Path => path;
        public string Name => Path.Contains("/") ? Path.GetAfter("/") : Path;

        [SerializeField] private string path;

        public Folder (string path)
        {
            this.path = path;
        }
    }
}
