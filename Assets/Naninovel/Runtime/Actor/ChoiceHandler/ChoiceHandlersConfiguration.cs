// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    [EditInProjectSettings]
    public class ChoiceHandlersConfiguration : ActorManagerConfiguration<ChoiceHandlerMetadata>
    {
        public const string DefaultPathPrefix = "ChoiceHandlers";

        [Tooltip("ID of the choice handler to use by default.")]
        public string DefaultHandlerId = "ButtonList";
        [Tooltip("Configuration of the resource loader used for loading custom choice buttons.")]
        public ResourceLoaderConfiguration ChoiceButtonLoader = new ResourceLoaderConfiguration();
        [Tooltip("Metadata to use by default when creating choice handler actors and custom metadata for the created actor ID doesn't exist.")]
        public ChoiceHandlerMetadata DefaultMetadata = new ChoiceHandlerMetadata();
        [Tooltip("Metadata to use when creating choice handler actors with specific IDs.")]
        public ChoiceHandlerMetadata.Map Metadata = new ChoiceHandlerMetadata.Map {
            ["ButtonList"] = CreateBuiltinMeta(),
            ["ButtonArea"] = CreateBuiltinMeta(),
            ["ChatReply"] = CreateBuiltinMeta()
        };

        public override ChoiceHandlerMetadata DefaultActorMetadata => DefaultMetadata;
        public override ActorMetadataMap<ChoiceHandlerMetadata> ActorMetadataMap => Metadata;

        protected override ActorPose<TState> GetSharedPose<TState> (string poseName) => null;

        private static ChoiceHandlerMetadata CreateBuiltinMeta () => new ChoiceHandlerMetadata {
            Implementation = typeof(UIChoiceHandler).AssemblyQualifiedName
        };
    }
}
