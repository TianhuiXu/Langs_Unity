// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class ControlPanelSettingsButton : ScriptableButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnButtonClick ()
        {
            uiManager.GetUI<IPauseUI>()?.Hide();
            uiManager.GetUI<ISettingsUI>()?.Show();
        }
    } 
}
