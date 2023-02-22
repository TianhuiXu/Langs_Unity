// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class CGGalleryReturnButton : ScriptableButton
    {
        private ICGGalleryUI cgGalleryUI;

        protected override void Awake ()
        {
            base.Awake();

            cgGalleryUI = GetComponentInParent<ICGGalleryUI>();
        }

        protected override void OnButtonClick () => cgGalleryUI.Hide();
    }
}
