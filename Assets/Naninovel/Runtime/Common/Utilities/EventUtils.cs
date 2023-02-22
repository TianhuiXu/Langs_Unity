// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Naninovel
{
    public static class EventUtils
    {
        private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        /// <summary>
        /// Get top-most hovered game object.
        /// </summary>
        public static GameObject GetHoveredGameObject (EventSystem eventSystem)
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            if (!eventSystem) throw new UnityException("Provided event system is not valid.");

            var pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;

            raycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, raycastResults);
            if (raycastResults.Count > 0)
                return raycastResults[0].gameObject;
            return null;
            #else
            Debug.LogWarning("`UnityCommon.GetHoveredGameObject` requires legacy input system, which is disabled; the method will always return null.");
            return null;
            #endif
        }

        public static void SafeInvoke (this Action action)
        {
            action?.Invoke();
        }

        public static void SafeInvoke<T0> (this Action<T0> action, T0 arg0)
        {
            action?.Invoke(arg0);
        }

        public static void SafeInvoke<T0, T1> (this Action<T0, T1> action, T0 arg0, T1 arg1)
        {
            action?.Invoke(arg0, arg1);
        }

        public static void SafeInvoke<T0, T1, T2> (this Action<T0, T1, T2> action, T0 arg0, T1 arg1, T2 arg2)
        {
            action?.Invoke(arg0, arg1, arg2);
        }
    }

    [Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    [Serializable]
    public class FloatUnityEvent : UnityEvent<float> { }

    [Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    [Serializable]
    public class BoolUnityEvent : UnityEvent<bool> { }

    [Serializable]
    public class Vector3UnityEvent : UnityEvent<Vector3> { }

    [Serializable]
    public class Vector2UnityEvent : UnityEvent<Vector2> { }

    [Serializable]
    public class QuaternionUnityEvent : UnityEvent<Quaternion> { }

    [Serializable]
    public class ColorUnityEvent : UnityEvent<Color> { }
}
