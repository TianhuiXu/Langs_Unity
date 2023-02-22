// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using Naninovel.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.Runtime.UI
{
    public class FontChanger : MonoBehaviour
    {
        [Tooltip("Setup which game objects should be affected by font and text size changes (set in game settings).")]
        [SerializeField] private CustomUI.FontChangeConfiguration[] configuration;

        private IUIManager uiManager;
        private string lastAppliedFontName;
        private int lastAppliedFontSize = -1;

        public static void InitializeConfiguration (IReadOnlyList<CustomUI.FontChangeConfiguration> configuration)
        {
            if (configuration is null) return;

            for (int i = 0; i < configuration.Count; i++) // Store default fonts and sizes.
            {
                var item = configuration[i];
                if (!item.Object) throw new Error("Failed to initialize font size list: game object is invalid.");
                if (item.Object.TryGetComponent<Text>(out var text))
                {
                    item.DefaultSize = text.fontSize;
                    item.DefaultFont = text.font;
                }
                if (item.Object.TryGetComponent<TextMeshProUGUI>(out var tmpText))
                {
                    item.DefaultSize = (int)tmpText.fontSize;
                    item.DefaultTMPFont = tmpText.font;
                }
            }
        }

        public static void ChangeFont (Font font, TMP_FontAsset tmpFont,
            IReadOnlyCollection<CustomUI.FontChangeConfiguration> configuration)
        {
            if (configuration is null || configuration.Count == 0) return;

            foreach (var config in configuration)
            {
                if (!config.AllowFontChange) continue;

                if (config.Object.TryGetComponent<Text>(out var text))
                    text.font = ObjectUtils.IsValid(font) ? font : config.DefaultFont;
                else if (config.Object.TryGetComponent<TextMeshProUGUI>(out var tmpText))
                {
                    var shader = tmpText.fontMaterial.shader;
                    tmpText.font = ObjectUtils.IsValid(tmpFont) ? tmpFont : config.DefaultTMPFont;
                    foreach (var material in tmpText.fontMaterials)
                        material.shader = shader;
                }
            }
        }

        public static void ChangeFontSize (int dropdownIndex,
            IReadOnlyCollection<CustomUI.FontChangeConfiguration> configuration)
        {
            if (configuration is null || configuration.Count == 0) return;

            foreach (var config in configuration)
            {
                if (!config.AllowFontSizeChange) continue;

                if (dropdownIndex != -1 && !config.FontSizes.IsIndexValid(dropdownIndex))
                    throw new Error($"Failed to apply selected font size dropdown index (`{dropdownIndex}`) " +
                                    $"to `{config.Object.name}` UI: index is not available in `Font Sizes` list.");

                var size = dropdownIndex == -1 ? config.DefaultSize : config.FontSizes[dropdownIndex];

                if (config.Object.TryGetComponent<Text>(out var text))
                    text.fontSize = size;
                else if (config.Object.TryGetComponent<TextMeshProUGUI>(out var tmpText))
                    tmpText.fontSize = size;
            }
        }

        protected virtual void Awake ()
        {
            uiManager = Engine.GetService<IUIManager>();
            InitializeConfiguration(configuration);
        }

        protected virtual void OnEnable ()
        {
            uiManager.OnFontNameChanged += HandleFontNameChanged;
            uiManager.OnFontSizeChanged += HandleFontSizeChanged;

            if (IsFontNameChangePending())
                HandleFontNameChanged(uiManager.FontName);
            if (IsFontSizeChangePending())
                HandleFontSizeChanged(uiManager.FontSize);
        }

        protected virtual void OnDisable ()
        {
            if (uiManager is null) return;
            uiManager.OnFontNameChanged -= HandleFontNameChanged;
            uiManager.OnFontSizeChanged -= HandleFontSizeChanged;
        }

        protected virtual void HandleFontNameChanged (string fontName)
        {
            var fontOption = uiManager.Configuration.GetFontOption(fontName);
            ChangeFont(fontOption?.Font, fontOption?.TMPFont, configuration);
            lastAppliedFontName = fontName;
        }

        protected virtual void HandleFontSizeChanged (int sizeIndex)
        {
            ChangeFontSize(sizeIndex, configuration);
            lastAppliedFontSize = sizeIndex;
        }

        protected virtual bool IsFontNameChangePending ()
        {
            return lastAppliedFontName != uiManager.FontName;
        }

        protected virtual bool IsFontSizeChangePending ()
        {
            return lastAppliedFontSize != uiManager.FontSize;
        }
    }
}
