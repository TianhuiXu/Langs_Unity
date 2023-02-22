// Copyright 2022 ReWaffle LLC. All rights reserved.

using Naninovel.UI;
using UnityEditor;

namespace Naninovel
{
    [CustomEditor(typeof(RevealableTMProText), true)]
    [CanEditMultipleObjects]
    public class RevealableTMProTextEditor : NaninovelTMProTextEditor
    {
        private SerializedProperty revealFadeWidth;
        private SerializedProperty slideClipRect;
        private SerializedProperty defaultSlantAngle;
        private SerializedProperty italicSlantAngle;
        private SerializedProperty clipRectScale;
        private SerializedProperty drawClipRects;

        protected override void OnEnable ()
        {
            base.OnEnable();

            revealFadeWidth = serializedObject.FindProperty("revealFadeWidth");
            slideClipRect = serializedObject.FindProperty("slideClipRect");
            defaultSlantAngle = serializedObject.FindProperty("defaultSlantAngle");
            italicSlantAngle = serializedObject.FindProperty("italicSlantAngle");
            clipRectScale = serializedObject.FindProperty("clipRectScale");
            drawClipRects = serializedObject.FindProperty("drawClipRects");
        }

        protected override void DrawAdditionalInspectorGUI ()
        {
            EditorGUILayout.LabelField("Revealing", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(revealFadeWidth);
                EditorGUILayout.PropertyField(slideClipRect);
                EditorGUILayout.PropertyField(defaultSlantAngle);
                EditorGUILayout.PropertyField(italicSlantAngle);
                EditorGUILayout.PropertyField(clipRectScale);
                EditorGUILayout.PropertyField(drawClipRects);
            }
            --EditorGUI.indentLevel;
        }
    }
}
