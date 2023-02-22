// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class BackgroundsConfiguration : OrthoActorManagerConfiguration<BackgroundMetadata>
    {
        /// <summary>
        /// ID of the background actor used by default.
        /// </summary>
        public const string MainActorId = "MainBackground";
        public const string DefaultPathPrefix = "Backgrounds";

        public override BackgroundMetadata DefaultActorMetadata => DefaultMetadata;
        public override ActorMetadataMap<BackgroundMetadata> ActorMetadataMap => Metadata;

        [Tooltip("Metadata to use by default when creating background actors and custom metadata for the created actor ID doesn't exist.")]
        public BackgroundMetadata DefaultMetadata = new BackgroundMetadata();
        [Tooltip("Metadata to use when creating background actors with specific IDs.")]
        public BackgroundMetadata.Map Metadata = new BackgroundMetadata.Map {
            [MainActorId] = new BackgroundMetadata()
        };
        [Tooltip("Named states (poses) shared between the backgrounds; pose name can be used as appearance in `@back` commands to set enabled properties of the associated state.")]
        public List<BackgroundMetadata.Pose> SharedPoses = new List<BackgroundMetadata.Pose>();

        protected override ActorPose<TState> GetSharedPose<TState> (string poseName) => SharedPoses.FirstOrDefault(p => p.Name == poseName) as ActorPose<TState>;
    }
}
