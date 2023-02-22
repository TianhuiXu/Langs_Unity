// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class ScriptableScrollRect : ScriptableUIControl<ScrollRect>
    {
        public event Action<Vector2> OnPositionChanged;

        protected override void BindUIEvents ()
        {
            UIComponent.onValueChanged.AddListener(OnScrollPositionChanged);
            UIComponent.onValueChanged.AddListener(InvokeOnPositionChanged);
        }

        protected override void UnbindUIEvents ()
        {
            UIComponent.onValueChanged.RemoveListener(OnScrollPositionChanged);
            UIComponent.onValueChanged.RemoveListener(InvokeOnPositionChanged);
        }

        protected virtual void OnScrollPositionChanged (Vector2 scrollPosition) { }

        private void InvokeOnPositionChanged (Vector2 value)
        {
            OnPositionChanged?.Invoke(value);
        }
    }
}
