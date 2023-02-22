// Copyright 2022 ReWaffle LLC. All rights reserved.

#if UNITY_GOOGLE_DRIVE_AVAILABLE

using System.Collections.Generic;
using System.Linq;
using UnityGoogleDrive;

namespace Naninovel
{
    public class GoogleDriveResourceLocator<TResource> : LocateResourcesRunner<TResource> 
        where TResource : UnityEngine.Object
    {
        public virtual string RootPath { get; }

        private readonly IRawConverter<TResource> converter;

        public GoogleDriveResourceLocator (IResourceProvider provider, string rootPath, string resourcesPath, 
            IRawConverter<TResource> converter) : base (provider, resourcesPath)
        {
            RootPath = rootPath;
            this.converter = converter;
        }

        public override async UniTask RunAsync ()
        {
            var result = new List<string>();

            // 1. Find all the files by path.
            var fullPath = PathUtils.Combine(RootPath, Path) + "/";
            var files = await Helpers.FindFilesByPathAsync(fullPath, fields: new List<string> { "files(name, mimeType)" });

            // 2. Filter the results by representations (MIME types).
            var reprToFileMap = new Dictionary<RawDataRepresentation, List<UnityGoogleDrive.Data.File>>();
            foreach (var representation in converter.Representations)
                reprToFileMap.Add(representation, files.Where(f => f.MimeType == representation.MimeType).ToList());

            // 3. Create resources using located files.
            foreach (var reprToFile in reprToFileMap)
            {
                foreach (var file in reprToFile.Value)
                {
                    var fileName = string.IsNullOrEmpty(reprToFile.Key.Extension) ? file.Name : file.Name.GetBeforeLast(".");
                    var filePath = string.IsNullOrEmpty(Path) ? fileName : string.Concat(Path, '/', fileName);
                    result.Add(filePath);
                }
            }

            SetResult(result);
        }
    }
}

#endif
