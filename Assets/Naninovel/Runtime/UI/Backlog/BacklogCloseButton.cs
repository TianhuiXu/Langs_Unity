// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class BacklogCloseButton : ScriptableLabeledButton
    {
        private BacklogPanel backlogPanel;

        protected override void Awake ()
        {
            base.Awake();

            backlogPanel = GetComponentInParent<BacklogPanel>();
        }

        protected override void OnButtonClick () => backlogPanel.Hide();
    }
}
