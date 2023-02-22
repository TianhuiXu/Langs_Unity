// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEditor;
using UnityEditor.UI;

namespace Naninovel
{
    [CustomEditor(typeof(LabeledButton), true), CanEditMultipleObjects]
    public class LabeledButtonEditor : ButtonEditor
    {
        private SerializedProperty labelTextProperty;
        private SerializedProperty labelColorsProperty;

        protected override void OnEnable ()
        {
            base.OnEnable();

            labelTextProperty = serializedObject.FindProperty("labelText");
            labelColorsProperty = serializedObject.FindProperty("labelColors");
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(labelTextProperty);

            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(labelColorsProperty);
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
