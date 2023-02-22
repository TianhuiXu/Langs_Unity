// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class ControlPanelQuickSaveButton : ScriptableButton
    {
        private IStateManager gameState;

        protected override void Awake ()
        {
            base.Awake();

            gameState = Engine.GetService<IStateManager>();
        }

        protected override void OnButtonClick () => QuickSaveAsync();

        private async void QuickSaveAsync ()
        {
            UIComponent.interactable = false;
            await gameState.QuickSaveAsync();
            UIComponent.interactable = true;
        }
    } 
}
