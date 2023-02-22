// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class ExternalScriptsBrowserPanel : ScriptNavigatorPanel, IExternalScriptsUI
    {
        public override async UniTask LocateScriptsAsync (AsyncToken asyncToken = default)
        {
            var scripts = await ScriptManager.LocateExternalScriptsAsync();
            asyncToken.ThrowIfCanceled();
            GenerateScriptButtons(scripts);
        }
    }
}
