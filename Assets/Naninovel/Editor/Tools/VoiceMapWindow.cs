// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class VoiceMapWindow : EditorWindow
    {
        private class MapItem
        {
            public bool Assigned => !string.IsNullOrEmpty(ClipGuid);
            public bool Loaded => ClipObject;
            public GUIContent Label;
            public string ClipGuid;
            public AudioClip ClipObject;
            public string Author;
            public Commands.PrintText Command;
        }

        private static readonly GUIContent scriptContent = new GUIContent("Script", "Naninovel script for which to edit auto voice associations.");
        private static readonly GUIContent authorFilterContent = new GUIContent("Author Filter", "When specified, will show only print commands with the specified author. Specify `*` to show commands with any author.");
        private static readonly Color invalidObjectColor = new Color(1, .8f, .8f);
        private static readonly Type clipType = typeof(AudioClip);
        private static readonly Type scriptType = typeof(Script);

        private readonly Dictionary<string, MapItem> map = new Dictionary<string, MapItem>();
        private AssetDropHandler dropHandler;
        private AudioConfiguration audioConfig;
        private EditorResources editorResources;
        private Script script;
        private string authorFilter;
        private Vector2 scrollPos;

        [MenuItem("Naninovel/Tools/Voice Map")]
        public static void OpenWindow ()
        {
            GetWindow<VoiceMapWindow>("Voice Map", true);
        }

        private void OnEnable ()
        {
            editorResources = EditorResources.LoadOrDefault();
            audioConfig = ProjectConfigurationProvider.LoadOrDefault<AudioConfiguration>();
            dropHandler = new AssetDropHandler(ProcessDroppedClips);
            dropHandler.TypeConstraint = typeof(AudioClip);
            dropHandler.DropMessage = "Drop voice clips or folders here to attempt automatic assignment.\nClip name should equal to the beginning of the printed text in order for this to work.";
            UpdateSelectedScript();
        }

        private void OnDisable ()
        {
            SaveResources();
        }

        private void OnGUI ()
        {
            DrawScriptSelection();

            if (!script)
            {
                EditorGUILayout.HelpBox("Select a script to start mapping the voice clips.", MessageType.None, false);
                return;
            }

            authorFilter = EditorGUILayout.TextField(authorFilterContent, authorFilter);

            EditorGUILayout.Space();

            if (map.Count == 0)
            {
                EditorGUILayout.HelpBox("Selected script doesn't contain any commands to associate voice clips with. " +
                                        "Voice clips can be associated with generic text lines (containing a text to print) and with @print commands.", MessageType.Info);
                return;
            }

            if (dropHandler.CanHandleDraggedObjects())
                dropHandler.DrawDropArea(EditorGUILayout.GetControlRect());
            DrawItems();
        }

        private void DrawScriptSelection ()
        {
            EditorGUI.BeginChangeCheck();
            script = EditorGUILayout.ObjectField(scriptContent, script, scriptType, false) as Script;
            if (EditorGUI.EndChangeCheck()) UpdateSelectedScript();
        }

        private void DrawItems ()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var kv in map)
            {
                if (!ShouldDrawItem(kv.Value)) continue;
                EditorGUILayout.BeginHorizontal();
                DrawItem(kv.Value);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private bool ShouldDrawItem (MapItem item)
        {
            if (string.IsNullOrEmpty(authorFilter)) return true;
            return !string.IsNullOrEmpty(item.Author) &&
                   (authorFilter == "*" || item.Author == authorFilter);
        }

        private void DrawItem (MapItem item)
        {
            EditorGUILayout.LabelField(item.Label);

            if (ShouldDrawWithoutLoading(item))
            {
                DrawWithoutLoading(item);
                return;
            }

            if (item.Assigned && !item.Loaded)
                LoadItemClip(item);

            EditorGUI.BeginChangeCheck();
            var initialGuidColor = GUI.color;
            if (!item.ClipObject) GUI.color = invalidObjectColor;
            item.ClipObject = EditorGUILayout.ObjectField(GUIContent.none, item.ClipObject, clipType, false) as AudioClip;
            GUI.color = initialGuidColor;
            if (EditorGUI.EndChangeCheck())
            {
                if (!item.ClipObject) item.ClipGuid = string.Empty;
                else AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item.ClipObject, out item.ClipGuid, out long _);
            }
        }

        private bool ShouldDrawWithoutLoading (MapItem item)
        {
            var rect = GUILayoutUtility.GetLastRect();
            rect.width = EditorGUIUtility.currentViewWidth;
            var hovered = rect.Contains(Event.current.mousePosition);
            return !hovered && !item.Loaded && item.Assigned;
        }

        private void DrawWithoutLoading (MapItem item)
        {
            var path = AssetDatabase.GUIDToAssetPath(item.ClipGuid);
            if (path.Contains("/")) path = path.GetAfter("/");
            if (path.Length > 30) path = path.Substring(path.Length - 30);
            EditorGUILayout.LabelField(path, EditorStyles.objectField);
        }

        private void LoadItemClip (MapItem item)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(item.ClipGuid);
            item.ClipObject = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (!item.ClipObject) item.ClipGuid = string.Empty;
        }

        private void UpdateSelectedScript ()
        {
            SaveResources();
            map.Clear();

            if (!script) return;

            var extractedCommands = script.ExtractCommands()
                .Where(c => c is Commands.PrintText)
                .Cast<Commands.PrintText>();

            foreach (var command in extractedCommands)
                ProcessCommand(command);
        }

        private void ProcessCommand (Commands.PrintText cmd)
        {
            if (!Command.Assigned(cmd.Text)) return;
            var hash = AudioConfiguration.GetAutoVoiceClipPath(cmd);
            var label = $"#{cmd.PlaybackSpot.LineNumber}.{cmd.PlaybackSpot.InlineIndex} ";
            if (!string.IsNullOrEmpty(cmd.AuthorId)) label += $"{cmd.AuthorId}: ";
            label += cmd.Text.DynamicValue ? cmd.Text.DynamicValueText : cmd.Text.Value;
            var clipPath = PathUtils.Combine(audioConfig.VoiceLoader.PathPrefix, hash);
            var clipGuid = editorResources.GetGuidByPath(clipPath);
            map[hash] = new MapItem {
                Label = new GUIContent(label, label),
                ClipGuid = clipGuid,
                Author = GetAuthor(cmd),
                Command = cmd
            };
        }

        private string GetAuthor (Commands.PrintText cmd)
        {
            if (!Command.Assigned(cmd.AuthorId) || cmd.AuthorId.DynamicValue) return "";
            return cmd.AuthorId.Value;
        }

        private void SaveResources ()
        {
            if (!audioConfig) return;

            var category = audioConfig.VoiceLoader.PathPrefix;

            foreach (var kv in map)
            {
                var hash = kv.Key;
                var item = kv.Value;

                editorResources.RemoveAllRecordsWithPath(category, hash, category);
                if (item.Assigned) editorResources.AddRecord(category, category, hash, item.ClipGuid);
            }

            EditorUtility.SetDirty(editorResources);
            AssetDatabase.SaveAssets();
        }

        private void ProcessDroppedClips (DroppedAsset[] assets)
        {
            foreach (var asset in assets)
                AttemptAutoAssign(asset.Asset as AudioClip, asset.Guid);
        }

        private void AttemptAutoAssign (AudioClip clip, string guid)
        {
            var itemToAssign = map.Values.FirstOrDefault(ShouldAssign);
            if (itemToAssign is null) return;
            itemToAssign.ClipObject = clip;
            itemToAssign.ClipGuid = guid;

            bool ShouldAssign (MapItem item)
            {
                if (item.Assigned) return false;
                var text = item.Command.Text.DynamicValue ? item.Command.Text.DynamicValueText : item.Command.Text.Value;
                return text?.StartsWith(clip.name, StringComparison.OrdinalIgnoreCase) ?? false;
            }
        }
    }
}
