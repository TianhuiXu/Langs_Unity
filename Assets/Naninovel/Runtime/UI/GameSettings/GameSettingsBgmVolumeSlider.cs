// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    public class GameSettingsBgmVolumeSlider : ScriptableSlider
    {
        private IAudioManager audioManager;

        protected override void Awake ()
        {
            base.Awake();

            audioManager = Engine.GetService<IAudioManager>();
        }

        protected override void Start ()
        {
            base.Start();

            UIComponent.value = audioManager.BgmVolume;
        }

        protected override void OnValueChanged (float value)
        {
            audioManager.BgmVolume = value;
        }
    }
}
