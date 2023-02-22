// Copyright 2022 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    public interface IAddressableBuilder
    {
        void RemoveEntries ();
        bool TryAddEntry (string assetGuid, string resourcePath);
        void BuildContent ();
    }
}
