// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class SelectableTooltip
    {
        public static void Draw (string message, string tooltip = default)
        {
            EditorGUILayout.Space();
            var rect = GUILayoutUtility.GetRect(new GUIContent(message), GUIStyles.ResourceTooltipStyle);
            EditorGUI.SelectableLabel(rect, message, GUIStyles.ResourceTooltipStyle);
            if (tooltip != null) EditorGUI.LabelField(rect, new GUIContent("", tooltip));
            EditorGUILayout.Space();
        }
    }
}
