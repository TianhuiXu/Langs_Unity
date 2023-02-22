// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TipsPanel : CustomUI, ITipsUI
    {
        public const string DefaultManagedTextCategory = "Tips";

        public virtual int TipsCount { get; private set; }

        protected virtual string UnlockableIdPrefix => unlockableIdPrefix;
        protected virtual string ManagedTextCategory => managedTextCategory;
        protected virtual RectTransform ItemsContainer => itemsContainer;
        protected virtual TipsListItem ItemPrefab => itemPrefab;

        private const string separatorLiteral = "|";
        private const string selectedPrefix = "TIP_SELECTED_";

        [Header("Tips Setup")]
        [Tooltip("All the unlockable item IDs with the specified prefix will be considered Tips items.")]
        [SerializeField] private string unlockableIdPrefix = "Tips";
        [Tooltip("The name of the managed text document (category) where all the tips data is stored.")]
        [SerializeField] private string managedTextCategory = DefaultManagedTextCategory;

        [Header("UI Setup")]
        [SerializeField] private ScrollRect itemsScrollRect;
        [SerializeField] private RectTransform itemsContainer;
        [SerializeField] private TipsListItem itemPrefab;
        [SerializeField] private StringUnityEvent onTitleChanged;
        [SerializeField] private StringUnityEvent onNumberChanged;
        [SerializeField] private StringUnityEvent onCategoryChanged;
        [SerializeField] private StringUnityEvent onDescriptionChanged;

        private readonly List<TipsListItem> listItems = new List<TipsListItem>();
        private IUnlockableManager unlockableManager;
        private ITextManager textManager;

        public override UniTask InitializeAsync ()
        {
            FillListItems();
            Engine.GetService<ILocalizationManager>().OnLocaleChanged += HandleLocaleChanged;
            return UniTask.CompletedTask;
        }

        public virtual void SelectTipRecord (string tipId)
        {
            var unlockableId = $"{UnlockableIdPrefix}/{tipId}";
            var item = listItems.Find(i => i.UnlockableId == unlockableId);
            if (item is null) throw new Error($"Failed to select `{tipId}` tip record: item with the ID is not found.");
            itemsScrollRect.ScrollTo(item.GetComponent<RectTransform>());
            HandleItemClicked(item);
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(itemsScrollRect, ItemsContainer, ItemPrefab);

            unlockableManager = Engine.GetService<IUnlockableManager>();
            textManager = Engine.GetService<ITextManager>();

            ClearSelection();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            unlockableManager.OnItemUpdated += HandleUnlockableItemUpdated;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (unlockableManager != null)
                unlockableManager.OnItemUpdated -= HandleUnlockableItemUpdated;
        }

        protected virtual void FillListItems ()
        {
            var records = textManager.GetAllRecords(ManagedTextCategory);
            foreach (var record in records)
            {
                var unlockableId = $"{UnlockableIdPrefix}/{record.Key}";
                var title = record.Value.GetBefore(separatorLiteral) ?? record.Value;
                var selectedOnce = WasItemSelectedOnce(unlockableId);
                var item = TipsListItem.Instantiate(ItemPrefab, unlockableId, title, selectedOnce, HandleItemClicked);
                item.transform.SetParent(ItemsContainer, false);
                listItems.Add(item);
            }

            foreach (var item in listItems)
                item.SetUnlocked(unlockableManager.ItemUnlocked(item.UnlockableId));

            TipsCount = listItems.Count;
        }

        protected virtual void ClearListItems ()
        {
            foreach (var item in listItems.ToArray())
                ObjectUtils.DestroyOrImmediate(item.gameObject);
            listItems.Clear();
            ItemsContainer.DetachChildren();
            TipsCount = 0;
        }

        protected virtual void ClearSelection ()
        {
            SetTitle(string.Empty);
            SetNumber(string.Empty);
            SetCategory(string.Empty);
            SetDescription(string.Empty);
            foreach (var item in listItems)
                item.SetSelected(false);
        }

        protected virtual void HandleItemClicked (TipsListItem clickedItem)
        {
            if (!unlockableManager.ItemUnlocked(clickedItem.UnlockableId)) return;

            SetItemSelectedOnce(clickedItem.UnlockableId);
            foreach (var item in listItems)
                item.SetSelected(item.UnlockableId.EqualsFast(clickedItem.UnlockableId));
            var recordValue = textManager.GetRecordValue(clickedItem.UnlockableId.GetAfterFirst($"{UnlockableIdPrefix}/"), ManagedTextCategory);
            SetTitle(recordValue.GetBefore(separatorLiteral)?.Trim() ?? recordValue);
            SetNumber(clickedItem.Number.ToString());
            SetCategory(recordValue.GetBetween(separatorLiteral)?.Trim() ?? string.Empty);
            SetDescription(recordValue.GetAfter(separatorLiteral)?.Replace("\\n", "\n").Trim() ?? string.Empty);
        }

        protected virtual void HandleUnlockableItemUpdated (UnlockableItemUpdatedArgs args)
        {
            if (!args.Id.StartsWithFast(UnlockableIdPrefix)) return;

            var unlockedItem = listItems.Find(i => i.UnlockableId.EqualsFast(args.Id));
            if (unlockedItem) unlockedItem.SetUnlocked(args.Unlocked);
        }

        protected virtual void SetTitle (string value)
        {
            onTitleChanged?.Invoke(value);
        }

        protected virtual void SetNumber (string value)
        {
            onNumberChanged?.Invoke(value);
        }

        protected virtual void SetCategory (string value)
        {
            onCategoryChanged?.Invoke(value);
        }

        protected virtual void SetDescription (string value)
        {
            onDescriptionChanged?.Invoke(value);
        }

        protected virtual bool WasItemSelectedOnce (string unlockableId)
        {
            return unlockableManager.ItemUnlocked(selectedPrefix + unlockableId);
        }

        protected virtual void SetItemSelectedOnce (string unlockableId)
        {
            unlockableManager.SetItemUnlocked(selectedPrefix + unlockableId, true);
        }

        protected virtual void HandleLocaleChanged (string locale)
        {
            ClearSelection();
            ClearListItems();
            FillListItems();
        }
    }
}
