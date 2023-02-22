// Copyright 2022 ReWaffle LLC. All rights reserved.

namespace Naninovel.UI
{
    public class GameSettingsReturnButton : ScriptableButton
    {
        private GameSettingsMenu settingsMenu;
        private IStateManager settingsManager;

        protected override void Awake ()
        {
            base.Awake();

            settingsMenu = GetComponentInParent<GameSettingsMenu>();
            settingsManager = Engine.GetService<IStateManager>();
        }

        protected override void OnButtonClick () => ApplySettingsAsync().Forget();

        private async UniTaskVoid ApplySettingsAsync ()
        {
            using (var _ = new InteractionBlocker())
                await settingsManager.SaveSettingsAsync();
            settingsMenu.Hide();
        }
    }
}
