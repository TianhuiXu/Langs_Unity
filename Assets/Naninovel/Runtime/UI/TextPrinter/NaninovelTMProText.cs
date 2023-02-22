// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Text.RegularExpressions;
using Naninovel.ArabicSupport;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <summary>
    /// Wrapper over TMPro text with Naninovel-specific tags and arabic text support.
    /// </summary>
    public class NaninovelTMProText : TextMeshProUGUI, IPointerClickHandler, IInputTrigger
    {
        [Serializable]
        public class LinkClickedEvent : UnityEvent<TMP_LinkInfo> { }

        public override string text { get => base.text; set => base.text = ProcessText(value); }

        protected virtual string RubyVerticalOffset => rubyVerticalOffset;
        protected virtual float RubySizeScale => rubySizeScale;
        protected virtual bool AddRubyLineHeight => addRubyLineHeight;
        protected virtual bool UnlockTipsOnPrint => unlockTipsOnPrint;
        protected virtual bool FixArabicText => fixArabicText;
        protected virtual string TipTemplate => tipTemplate;
        protected virtual Canvas TopmostCanvas => topmostCanvasCache ? topmostCanvasCache : topmostCanvasCache = gameObject.FindTopmostComponent<Canvas>();
        protected virtual bool Edited => !Application.isPlaying || ObjectUtils.IsEditedInPrefabMode(gameObject);

        [Tooltip("Vertical line offset to use for the ruby (furigana) text; supported units: em, px, %.")]
        [SerializeField] private string rubyVerticalOffset = "1em";
        [Tooltip("Font size scale (relative to the main text font size) to apply for the ruby (furigana) text.")]
        [SerializeField] private float rubySizeScale = .5f;
        [Tooltip("Whether to compensate (add) line height for the lines that contain ruby tags.")]
        [SerializeField] private bool addRubyLineHeight = true;
        [Tooltip("Whether to automatically unlock associated tip records when text wrapped in <tip> tags is printed.")]
        [SerializeField] private bool unlockTipsOnPrint = true;
        [Tooltip("Template to use when processing text wrapped in <tip> tags. " + tipTemplateLiteral + " will be replaced with the actual tip content.")]
        [SerializeField] private string tipTemplate = $"<u>{tipTemplateLiteral}</u>";
        [Tooltip("Invoked when a text wrapped in <tip> tags is clicked; returned string argument is the ID of the clicked tip. Be aware, that the default behaviour (showing `ITipsUI` when a tip is clicked) won't be invoked when a custom handler is assigned.")]
        [SerializeField] private StringUnityEvent onTipClicked;
        [Tooltip("Whether to modify the text to support arabic languages (fix letters connectivity issues).")]
        [SerializeField] private bool fixArabicText;
        [Tooltip("When `Fix Arabic Text` is enabled, controls to whether also fix Farsi characters.")]
        [SerializeField] private bool fixArabicFarsi = true;
        [Tooltip("When `Fix Arabic Text` is enabled, controls to whether also fix rich text tags.")]
        [SerializeField] private bool fixArabicTextTags = true;
        [Tooltip("When `Fix Arabic Text` is enabled, controls to whether preserve numbers.")]
        [SerializeField] private bool fixArabicPreserveNumbers;
        [Tooltip("Template to use when processing text wrapped in <link> tags. " + linkTemplateLiteral + " will be replaced with the actual tip content. When nothing is specified, the link tags won't be modified.")]
        [SerializeField] private string linkTemplate = $"<u>{linkTemplateLiteral}</u>";
        [Tooltip("Invoked when a text wrapped in <link> tags is clicked.")]
        [SerializeField] private LinkClickedEvent onLinkClicked;

        private const string tipIdPrefix = "NANINOVEL.TIP.";
        private const string tipTemplateLiteral = "%TIP%";
        private const string linkTemplateLiteral = "%LINK%";
        private static readonly Regex captureRubyRegex = new Regex(@"<ruby=""([\s\S]*?)"">([\s\S]*?)<\/ruby>", RegexOptions.Compiled);
        private static readonly Regex captureTipRegex = new Regex(@"<tip=""([\w]*?)"">([\s\S]*?)<\/tip>", RegexOptions.Compiled);
        private static readonly Regex captureLinkRegex = new Regex(@"<link=""([\w]*?)"">([\s\S]*?)<\/link>", RegexOptions.Compiled);

        private readonly FastStringBuilder arabicBuilder = new FastStringBuilder(RTLSupport.DefaultBufferSize);
        private Canvas topmostCanvasCache;

        public virtual void OnPointerClick (PointerEventData eventData)
        {
            var renderCamera = TopmostCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TopmostCanvas.worldCamera;
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(this, eventData.position, renderCamera);
            if (linkIndex >= 0) OnLinkClicked(textInfo.linkInfo[linkIndex]);
        }

        public virtual bool CanTriggerInput ()
        {
            var evtSystem = EventSystem.current;
            if (!evtSystem) return true;
            var inputModule = evtSystem.currentInputModule;
            if (!inputModule) return true;
            var input = inputModule.input;
            if (!input) return true;

            var position = input.mousePosition;
            var renderCamera = TopmostCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : TopmostCanvas.worldCamera;
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(this, position, renderCamera);
            return linkIndex == -1;
        }

        protected override void Start ()
        {
            base.Start();
            if (!string.IsNullOrEmpty(text) && !Edited)
                text = text;
        }

        protected virtual void OnLinkClicked (TMP_LinkInfo linkInfo)
        {
            if (onLinkClicked?.GetPersistentEventCount() > 0)
                onLinkClicked.Invoke(linkInfo);

            var linkId = linkInfo.GetLinkID();
            if (linkId.StartsWithFast(tipIdPrefix))
                OnTipClicked(linkId.GetAfter(tipIdPrefix));
        }

        protected virtual void OnTipClicked (string tipId)
        {
            if (onTipClicked?.GetPersistentEventCount() > 0)
            {
                onTipClicked.Invoke(tipId);
                return;
            }

            var tipsUI = Engine.GetService<IUIManager>()?.GetUI<ITipsUI>();
            tipsUI?.Show();
            tipsUI?.SelectTipRecord(tipId);
        }

        /// <summary>
        /// Applies various pre-processing (ruby, tips, arabic, etc) before assigning TMPro <see cref="TextMeshProUGUI.text"/>.
        /// </summary>
        protected virtual string ProcessText (string content)
        {
            return FixArabic(ProcessRubyTags(ProcessTipTags(ProcessLinkTags(content))));
        }

        /// <summary>
        /// When 'Link Template' is assigned, will modify the link content in accordance with the template.
        /// </summary>
        protected virtual string ProcessLinkTags (string content)
        {
            if (string.IsNullOrEmpty(linkTemplate) || !linkTemplate.Contains(linkTemplateLiteral)) return content;

            var matches = captureLinkRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count != 3) continue;
                var fullMatch = match.Groups[0].ToString();
                var linkId = match.Groups[1].ToString();
                var linkContent = match.Groups[2].ToString();

                var replace = $"<link={linkId}>{linkTemplate.Replace(linkTemplateLiteral, linkContent)}</link>";
                content = content.Replace(fullMatch, replace);
            }

            return content;
        }

        /// <summary>
        /// Given the input text, extracts text wrapped in ruby tags and replace it with expression natively supported by TMPro.
        /// </summary>
        protected virtual string ProcessRubyTags (string content)
        {
            var matches = captureRubyRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count != 3) continue;
                var fullMatch = match.Groups[0].ToString();
                var rubyValue = match.Groups[1].ToString();
                var baseText = match.Groups[2].ToString();

                var baseTextWidth = GetPreferredValues(baseText).x;
                var rubyTextWidth = GetPreferredValues(rubyValue).x * RubySizeScale;
                var rubyTextOffset = baseTextWidth / 2f + rubyTextWidth / 2f;
                var compensationOffset = (baseTextWidth - rubyTextWidth) / 2f;
                var replace = $"<space={compensationOffset}><voffset={RubyVerticalOffset}><size={RubySizeScale * 100f}%>{rubyValue}</size></voffset><space=-{rubyTextOffset}>{baseText}";
                if (!AddRubyLineHeight) replace = "<line-height=100%>" + replace;
                content = content.Replace(fullMatch, replace);
            }

            return content;
        }

        /// <summary>
        /// Given the input text, extracts text wrapped in tip tags and replace it with expression natively supported by TMPro.
        /// </summary>
        protected virtual string ProcessTipTags (string content)
        {
            var matches = captureTipRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups.Count != 3) continue;
                var fullMatch = match.Groups[0].ToString();
                var tipID = match.Groups[1].ToString();
                var tipContent = match.Groups[2].ToString();

                if (UnlockTipsOnPrint)
                    Engine.GetService<IUnlockableManager>()?.UnlockItem($"Tips/{tipID}");

                var replace = $"<link={tipIdPrefix + tipID}>{TipTemplate.Replace(tipTemplateLiteral, tipContent)}</link>";
                content = content.Replace(fullMatch, replace);
            }

            return content;
        }

        protected virtual string FixArabic (string value)
        {
            if (!FixArabicText || string.IsNullOrWhiteSpace(value)) return value;
            arabicBuilder.Clear();
            RTLSupport.FixRTL(value, arabicBuilder, fixArabicFarsi, fixArabicTextTags, fixArabicPreserveNumbers);
            arabicBuilder.Reverse();
            return arabicBuilder.ToString();
        }
    }
}
