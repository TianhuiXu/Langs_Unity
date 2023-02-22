// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class ControlPanelTipsButton : ScriptableLabeledButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void Start ()
        {
            base.Start();

            var tipsUI = uiManager.GetUI<ITipsUI>();
            if (tipsUI is null || tipsUI.TipsCount == 0)
                gameObject.SetActive(false);
        }

        protected override void OnButtonClick ()
        {
            uiManager.GetUI<IPauseUI>()?.Hide();
            uiManager.GetUI<ITipsUI>()?.Show();
        }
    } 
}
