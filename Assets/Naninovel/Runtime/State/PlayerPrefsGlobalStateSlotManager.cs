// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel
{
    public class PlayerPrefsGlobalStateSlotManager : PlayerPrefsSaveSlotManager<GlobalStateMap>
    {
        protected override string KeyPrefix => base.KeyPrefix + savesFolderPath;
        protected override bool Binary => config.BinarySaveFiles;

        private readonly string savesFolderPath;
        private readonly StateConfiguration config;

        public PlayerPrefsGlobalStateSlotManager (StateConfiguration config, string savesFolderPath) 
        {
            this.savesFolderPath = savesFolderPath;
            this.config = config;
        }
    }
}
