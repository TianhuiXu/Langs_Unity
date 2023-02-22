// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a view for choosing between a set of choices.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ChoiceHandlerPanel : CustomUI, IManagedUI
    {
        [Serializable]
        public new class GameState
        {
            public bool RemoveAllButtonsPending;
            // Saving buttons separately from handler actor choices, as they're destroyed dependently.
            public List<ChoiceState> Buttons = new List<ChoiceState>();
        }

        /// <summary>
        /// Invoked when one of active choices are chosen.
        /// </summary>
        public event Action<ChoiceState> OnChoice;

        protected virtual List<ChoiceHandlerButton> ChoiceButtons { get; } = new List<ChoiceHandlerButton>();
        protected virtual RectTransform ButtonsContainer => buttonsContainer;
        protected virtual ChoiceHandlerButton DefaultButtonPrefab => defaultButtonPrefab;
        protected virtual bool FocusChoiceButtons => focusChoiceButtons;

        [Tooltip("Container that will hold spawned choice buttons.")]
        [SerializeField] private RectTransform buttonsContainer;
        [Tooltip("Button prototype to use by default.")]
        [SerializeField] private ChoiceHandlerButton defaultButtonPrefab;
        [Tooltip("Whether to focus added choice buttons for keyboard and gamepad control.")]
        [SerializeField] private bool focusChoiceButtons = true;

        private IResourceLoader<GameObject> customButtonLoader;
        private IBacklogUI backlogUI;
        private bool removeAllButtonsPending;

        UniTask IManagedUI.ChangeVisibilityAsync (bool visible, float? duration, AsyncToken asyncToken)
        {
            Debug.LogError("@showUI and @hideUI commands can't be used with choice handlers; use @show/hide commands instead");
            return UniTask.CompletedTask;
        }

        public virtual void AddChoiceButton (ChoiceState choice)
        {
            if (removeAllButtonsPending)
            {
                removeAllButtonsPending = false;
                RemoveAllChoiceButtons();
            }

            if (ChoiceButtons.Any(b => b.ChoiceState.Id == choice.Id)) return; // Could happen on rollback.

            var choicePrefab = string.IsNullOrWhiteSpace(choice.ButtonPath)
                ? defaultButtonPrefab
                : LoadCustomButtonPrefab(choice.ButtonPath);
            var choiceButton = Instantiate(choicePrefab, buttonsContainer, false);
            choiceButton.Initialize(choice);
            choiceButton.Show();
            choiceButton.OnButtonClicked += () => OnChoice?.Invoke(choice);

            if (backlogUI != null)
            {
                choiceButton.OnButtonClicked += () => backlogUI.AddChoice(ChoiceButtons
                    .Select(b => new Tuple<string, bool>(b.ChoiceState.Summary, b.ChoiceState.Id == choice.Id)).ToList());
            }

            if (choice.OverwriteButtonPosition)
                choiceButton.transform.localPosition = choice.ButtonPosition;

            ChoiceButtons.Add(choiceButton);

            if (ShouldFocusAddedButton(choiceButton))
                FocusAddedButton(choiceButton);
        }

        public virtual void RemoveChoiceButton (string id)
        {
            var buttons = ChoiceButtons.FindAll(c => c.ChoiceState.Id == id);
            if (buttons.Count == 0) return;

            foreach (var button in buttons)
            {
                if (button) Destroy(button.gameObject);
                ChoiceButtons.Remove(button);
            }
        }

        /// <summary>
        /// Will remove the buttons before the next <see cref="AddChoiceButton(ChoiceState)"/> call.
        /// </summary>
        public virtual void RemoveAllChoiceButtonsDelayed ()
        {
            ChoiceButtons?.ForEach(HideIfValid);
            removeAllButtonsPending = true;

            void HideIfValid (ChoiceHandlerButton button)
            {
                if (button) button.Hide();
            }
        }

        public virtual void RemoveAllChoiceButtons ()
        {
            for (int i = 0; i < ChoiceButtons.Count; i++)
                Destroy(ChoiceButtons[i].gameObject);
            ChoiceButtons.Clear();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(defaultButtonPrefab, buttonsContainer);

            backlogUI = Engine.GetService<IUIManager>().GetUI<IBacklogUI>();
            customButtonLoader = Engine.GetService<IChoiceHandlerManager>().ChoiceButtonLoader;
        }

        protected virtual ChoiceHandlerButton LoadCustomButtonPrefab (string path)
        {
            var resource = customButtonLoader.GetLoadedOrNull(path) ?? Resource<GameObject>.Invalid;
            if (resource.Valid && resource.Object.TryGetComponent<ChoiceHandlerButton>(out var b2)) return b2;
            if (Resources.Load<ChoiceHandlerButton>(path) is ChoiceHandlerButton b1 && b1) return b1;
            throw new Error($"Failed to add custom `{path}` choice button. Make sure the button prefab is stored in a `Resources` folder " +
                            "or custom loader in choices configuration is set up correctly. " +
                            "Be aware, that when using custom loader, dynamic path values (with expressions) are not supported.");
        }

        protected virtual bool ShouldFocusAddedButton (ChoiceHandlerButton button)
        {
            return FocusChoiceButtons &&
                   ChoiceButtons.Count == 1; // Focus only the first added button.
        }

        protected virtual void FocusAddedButton (ChoiceHandlerButton button)
        {
            switch (FocusModeType)
            {
                case FocusMode.Visibility:
                    if (EventSystem.current)
                        EventSystem.current.SetSelectedGameObject(button.gameObject);
                    break;
                case FocusMode.Navigation:
                    FocusOnNavigation = button.gameObject;
                    break;
            }
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState {
                RemoveAllButtonsPending = removeAllButtonsPending,
                Buttons = ChoiceButtons.Select(b => b.ChoiceState).ToList()
            };
            stateMap.SetState(state, name);
        }

        protected override async UniTask DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            var state = stateMap.GetState<GameState>(name);
            if (state is null) return;

            var existingButtonIds = ChoiceButtons.Select(b => b.ChoiceState.Id).ToList();
            foreach (var buttonId in existingButtonIds)
                if (state.Buttons.All(s => s.Id != buttonId))
                    RemoveChoiceButton(buttonId);

            foreach (var buttonState in state.Buttons)
                if (ChoiceButtons.All(b => b.ChoiceState != buttonState))
                    AddChoiceButton(buttonState);

            removeAllButtonsPending = state.RemoveAllButtonsPending;
        }
    }
}
