// Copyright 2022 ReWaffle LLC. All rights reserved.


namespace Naninovel.UI
{
    /// <summary>
    /// Set of UI elements used to browse unlockable CG illustrations.
    /// </summary>
    public interface ICGGalleryUI : IManagedUI
    {
        /// <summary>
        /// Number of existing CG slots (unlockable items), independent of the unlock state.
        /// </summary>
        int CGCount { get; }
    }
}
