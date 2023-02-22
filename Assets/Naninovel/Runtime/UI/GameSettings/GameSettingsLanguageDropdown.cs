// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Naninovel.UI
{
    public class GameSettingsLanguageDropdown : ScriptableDropdown
    {
        private IReadOnlyList<string> availableLocales;
        private ILocalizationManager localizationManager;
        private ITextManager textManager;

        protected override void Awake ()
        {
            base.Awake();

            textManager = Engine.GetService<ITextManager>();
            localizationManager = Engine.GetService<ILocalizationManager>();
            localizationManager.OnLocaleChanged += UpdateSelectedLocale;
            availableLocales = localizationManager.GetAvailableLocales().ToArray();
            InitializeOptions();
        }

        protected virtual void InitializeOptions ()
        {
            UIComponent.ClearOptions();
            UIComponent.AddOptions(availableLocales.Select(GetLabelForLocale).ToList());
            UpdateSelectedLocale(localizationManager.SelectedLocale);
        }

        protected virtual void UpdateSelectedLocale (string locale)
        {
            UIComponent.value = availableLocales.IndexOf(locale);
            UIComponent.RefreshShownValue();
        }

        protected virtual string GetLabelForLocale (string locale)
        {
            return textManager.GetRecordValue(locale, LanguageTags.ManagedTextCategory)
                   ?? LanguageTags.GetLanguageByTag(locale);
        }

        protected override void OnValueChanged (int value)
        {
            var selectedLocale = availableLocales[value];
            HandleLocaleSelectedAsync(selectedLocale).Forget();
        }

        protected virtual async UniTask HandleLocaleSelectedAsync (string locale)
        {
            using (var _ = new InteractionBlocker())
            {
                await localizationManager.SelectLocaleAsync(locale);
                if (Engine.GetService<IScriptPlayer>().PlayedScript != null)
                    await ReloadPlayedScript();
                else await Engine.GetService<IScriptManager>().ReloadAllScriptsAsync();
            }
        }

        protected virtual async UniTask ReloadPlayedScript ()
        {
            var tempSlotId = $"TEMP_LOCALE_CHANGE{Guid.NewGuid():N}";
            var stateManager = Engine.GetService<IStateManager>();
            await stateManager.SaveGameAsync(tempSlotId);
            await stateManager.ResetStateAsync();
            await Engine.GetService<IScriptManager>().ReloadAllScriptsAsync();
            await stateManager.LoadGameAsync(tempSlotId);
            stateManager.GameSlotManager.DeleteSaveSlot(tempSlotId);

            // Attempt rollback to the start of the played line to localize the printed content.
            await stateManager.RollbackAsync(s => s.PlaybackSpot.InlineIndex == 0);
        }
    }
}
