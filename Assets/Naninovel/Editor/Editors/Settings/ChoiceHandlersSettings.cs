// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEditor;

namespace Naninovel
{
    public class ChoiceHandlersSettings : ActorManagerSettings<ChoiceHandlersConfiguration, IChoiceHandlerActor, ChoiceHandlerMetadata>
    {
        protected override string HelpUri => "guide/choices.html";
        protected override string ResourcesSelectionTooltip => GetTooltip();
        protected override MetadataEditor<IChoiceHandlerActor, ChoiceHandlerMetadata> MetadataEditor { get; } = new ChoiceHandlerMetadataEditor();

        private string GetTooltip ()
        {
            if (EditedActorId == Configuration.DefaultHandlerId)
                return "Use `@choice \"Choice summary text.\"` in naninovel scripts to add a choice with this handler.";
            return $"Use `@choice \"Choice summary text.\" handler:{EditedActorId}` in naninovel scripts to add a choice with this handler.";
        }

        [MenuItem("Naninovel/Resources/Choice Handlers")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
