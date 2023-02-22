// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.IO;
using UnityEngine;

namespace Naninovel
{
    public static class PathUtils
    {
        /// <summary>
        /// Given an absolute path inside current Unity project (eg, `C:\UnityProject\Assets\FooAsset.asset`),
        /// transforms it to a relative project asset path (eg, `Assets/FooAsset.asset`).
        /// </summary>
        public static string AbsoluteToAssetPath (string absolutePath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            if (!absolutePath.StartsWithFast(Application.dataPath)) return null;
            return "Assets" + absolutePath.Replace(Application.dataPath, string.Empty);
        }

        /// <summary>
        /// Invokes <see cref="Path.Combine(string[])"/> and replaces back slashes with forward slashes on the result.
        /// </summary>
        public static string Combine (params string[] paths)
        {
            return Path.Combine(paths).Replace("\\", "/");
        }
    }
}
