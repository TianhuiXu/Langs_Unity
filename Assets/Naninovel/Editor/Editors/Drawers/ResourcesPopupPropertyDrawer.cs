// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomPropertyDrawer(typeof(ResourcePopupAttribute))]
    [CustomPropertyDrawer(typeof(ActorPopupAttribute))]
    public class ResourcesPopupPropertyDrawer : PropertyDrawer
    {
        private EditorResources editorResources;

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            if (!editorResources)
                editorResources = EditorResources.LoadOrDefault();

            if (attribute is ResourcePopupAttribute attr)
                editorResources.DrawPathPopup(position, property, attr.Category, attr.PathPrefix, attr.EmptyOption);
        }
    }
}
