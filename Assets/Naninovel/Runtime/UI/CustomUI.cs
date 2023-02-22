// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// An implementation of <see cref="IManagedUI"/>, that
    /// can be used to create custom user managed UI objects.
    /// </summary>
    public class CustomUI : ScriptableUIBehaviour, IManagedUI
    {
        [Serializable]
        public class GameState
        {
            public bool Visible;
        }

        [Serializable]
        public class FontChangeConfiguration
        {
            [Tooltip("The game object with a text component, which should be affected by font changes.")]
            public GameObject Object;
            [Tooltip("Whether to allow changing font of the text component.")]
            public bool AllowFontChange = true;
            [Tooltip("Whether to allow changing font size of the text component.")]
            public bool AllowFontSizeChange = true;
            [Tooltip("Sizes list should contain actual font sizes to apply for text component. Each element in the list corresponds to font size dropdown list index: Small -> 0, Default -> 1, Large -> 2, Extra Large -> 3 (can be changed via SettingsUI). Default value will be ignored and font size initially set in the prefab will be used instead.")]
            public List<int> FontSizes;
            [NonSerialized]
            public int DefaultSize;
            [NonSerialized]
            public Font DefaultFont;
            [NonSerialized]
            public TMP_FontAsset DefaultTMPFont;
        }

        public virtual bool HideOnLoad => hideOnLoad;
        public virtual bool HideInThumbnail => hideInThumbnail;
        public virtual bool SaveVisibilityState => saveVisibilityState;
        public virtual bool BlockInputWhenVisible => blockInputWhenVisible;
        public virtual bool ModalUI => modalUI;

        protected virtual List<FontChangeConfiguration> FontChangeConfigurations => fontChangeConfiguration;
        protected virtual string[] AllowedSamplers => allowedSamplers;

        [Tooltip("Whether to automatically hide the UI when loading game or resetting state.")]
        [SerializeField] private bool hideOnLoad = true;
        [Tooltip("Whether to hide the UI when capturing thumbnail for save-load slots.")]
        [SerializeField] private bool hideInThumbnail;
        [Tooltip("Whether to preserve visibility of the UI when saving/loading game.")]
        [SerializeField] private bool saveVisibilityState = true;
        [Tooltip("Whether to halt user input processing while the UI is visible. Will also exit auto read and skip script player modes when the UI becomes visible.")]
        [SerializeField] private bool blockInputWhenVisible;
        [Tooltip("Which input samplers should still be allowed in case the input is blocked while the UI is visible.")]
        [SerializeField] private string[] allowedSamplers;
        [Tooltip("Whether to make all the other managed UIs not interactable while the UI is visible.")]
        [SerializeField] private bool modalUI;
        [Tooltip("Setup which game objects should be affected by font and text size changes (set in game settings).")]
        [SerializeField] private List<FontChangeConfiguration> fontChangeConfiguration;

        private IStateManager stateManager;
        private IInputManager inputManager;
        private IUIManager uiManager;
        private IScriptPlayer scriptPlayer;

        public virtual UniTask InitializeAsync () => UniTask.CompletedTask;

        public virtual void SetFont (Font font, TMP_FontAsset tmpFont)
            => FontChanger.ChangeFont(font, tmpFont, FontChangeConfigurations);

        public virtual void SetFontSize (int dropdownIndex)
            => FontChanger.ChangeFontSize(dropdownIndex, FontChangeConfigurations);

        protected override void Awake ()
        {
            stateManager = Engine.GetService<IStateManager>();
            inputManager = Engine.GetService<IInputManager>();
            uiManager = Engine.GetService<IUIManager>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();

            base.Awake();

            InitializeFontChangeConfiguration();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (HideOnLoad)
            {
                stateManager.OnGameLoadStarted += HandleGameLoadStarted;
                stateManager.OnResetStarted += Hide;
            }

            stateManager.AddOnGameSerializeTask(SerializeState);
            stateManager.AddOnGameDeserializeTask(DeserializeState);

            if (BlockInputWhenVisible)
                inputManager.AddBlockingUI(this, AllowedSamplers);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (HideOnLoad && stateManager != null)
            {
                stateManager.OnGameLoadStarted -= HandleGameLoadStarted;
                stateManager.OnResetStarted -= Hide;
            }

            if (stateManager != null)
            {
                stateManager.RemoveOnGameSerializeTask(SerializeState);
                stateManager.RemoveOnGameDeserializeTask(DeserializeState);
            }

            if (BlockInputWhenVisible)
                inputManager?.RemoveBlockingUI(this);
        }

        protected virtual void SerializeState (GameStateMap stateMap)
        {
            if (SaveVisibilityState)
            {
                var state = new GameState {
                    Visible = Visible
                };
                stateMap.SetState(state, name);
            }
        }

        protected virtual UniTask DeserializeState (GameStateMap stateMap)
        {
            if (SaveVisibilityState)
            {
                var state = stateMap.GetState<GameState>(name);
                if (state is null) return UniTask.CompletedTask;
                Visible = state.Visible;
            }
            return UniTask.CompletedTask;
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (ModalUI)
            {
                if (visible) uiManager?.AddModalUI(this);
                else uiManager?.RemoveModalUI(this);
            }

            if (BlockInputWhenVisible)
            {
                if (scriptPlayer.SkipActive && !(AllowedSamplers?.Contains(InputConfiguration.SkipName) ?? false))
                    scriptPlayer.SetSkipEnabled(false);
                if (scriptPlayer.AutoPlayActive && !(AllowedSamplers?.Contains(InputConfiguration.AutoPlayName) ?? false))
                    scriptPlayer.SetAutoPlayEnabled(false);
            }
        }

        protected virtual void InitializeFontChangeConfiguration () =>
            FontChanger.InitializeConfiguration(FontChangeConfigurations);

        private void HandleGameLoadStarted (GameSaveLoadArgs args) => Hide();
    }
}
