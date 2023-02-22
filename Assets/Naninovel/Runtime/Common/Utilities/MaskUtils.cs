// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    public static class MaskUtils
    {
        public static int SetLayer (int mask, int layer, bool enabled)
        {
            if (enabled) return mask |= 1 << layer;
            return mask &= ~(1 << layer);
        }

        public static bool GetLayer (int mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static int SetLayer (int mask, string layerName, bool enabled)
        {
            var layer = LayerMask.NameToLayer(layerName);
            return SetLayer(mask, layer, enabled);
        }

        public static bool GetLayer (int mask, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            return GetLayer(mask, layer);
        }
    }
}
