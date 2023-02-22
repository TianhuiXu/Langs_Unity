// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <summary>
    /// Used by <see cref="DragDrop"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DragDropHandle : MonoBehaviour, IDragHandler
    {
        public event Action<Vector2> OnDragged;

        public virtual void OnDrag (PointerEventData eventData)
        {
            OnDragged?.Invoke(eventData.position);
        }
    }
}
