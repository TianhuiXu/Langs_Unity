// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel
{
    public class PlayerPrefsSettingsSlotManager : PlayerPrefsSaveSlotManager<SettingsStateMap>
    {
        protected override string KeyPrefix => base.KeyPrefix + savesFolderPath;
        protected override bool Binary => false;

        private readonly string savesFolderPath;

        public PlayerPrefsSettingsSlotManager (StateConfiguration config, string savesFolderPath) 
        {
            this.savesFolderPath = savesFolderPath;
        }
    }
}
