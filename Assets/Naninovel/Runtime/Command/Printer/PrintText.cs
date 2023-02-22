// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Linq;
using Naninovel.UI;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Prints (reveals over time) specified text message using a text printer actor.
    /// </summary>
    /// <remarks>
    /// This command is used under the hood when processing generic text lines, eg generic line `Kohaku: Hello World!` will be 
    /// automatically transformed into `@print "Hello World!" author:Kohaku` when parsing the naninovel scripts.<br/>
    /// Will reset (clear) the printer before printing the new message by default; set `reset` parameter to *false* or disable `Auto Reset` in the printer actor configuration to prevent that and append the text instead.<br/>
    /// Will make the printer default and hide other printers by default; set `default` parameter to *false* or disable `Auto Default` in the printer actor configuration to prevent that.<br/>
    /// Will wait for user input before finishing the task by default; set `waitInput` parameter to *false* or disable `Auto Wait` in the printer actor configuration to return as soon as the text is fully revealed.<br/>
    /// </remarks>
    [CommandAlias("print")]
    public class PrintText : PrinterCommand, Command.IPreloadable, Command.ILocalizable
    {
        /// <summary>
        /// Text of the message to print.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter, LocalizableParameter]
        public StringParameter Text;
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias("printer"), ActorContext(TextPrintersConfiguration.DefaultPathPrefix)]
        public StringParameter PrinterId;
        /// <summary>
        /// ID of the actor, which should be associated with the printed message.
        /// </summary>
        [ParameterAlias("author"), ActorContext(CharactersConfiguration.DefaultPathPrefix)]
        public StringParameter AuthorId;
        /// <summary>
        /// Text reveal speed multiplier; should be positive or zero. Setting to one will yield the default speed.
        /// </summary>
        [ParameterAlias("speed"), ParameterDefaultValue("1")]
        public DecimalParameter RevealSpeed = 1f;
        /// <summary>
        /// Whether to reset text of the printer before executing the printing task.
        /// Default value is controlled via `Auto Reset` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("reset")]
        public BooleanParameter ResetPrinter;
        /// <summary>
        /// Whether to make the printer default and hide other printers before executing the printing task.
        /// Default value is controlled via `Auto Default` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("default")]
        public BooleanParameter DefaultPrinter;
        /// <summary>
        /// Whether to wait for user input after finishing the printing task.
        /// Default value is controlled via `Auto Wait` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("waitInput")]
        public BooleanParameter WaitForInput;
        /// <summary>
        /// Number of line breaks to prepend before the printed text.
        /// Default value is controlled via `Auto Line Break` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("br")]
        public IntegerParameter LineBreaks;
        /// <summary>
        /// Controls duration (in seconds) of the printers show and hide animations associated with this command.
        /// Default value for each printer is set in the actor configuration.
        /// </summary>
        [ParameterAlias("fadeTime")]
        public DecimalParameter ChangeVisibilityDuration;
        /// <summary>
        /// Used by voice map utility to differentiate print commands with equal text within the same script.
        /// </summary>
        [ParameterAlias("voiceId")]
        public StringParameter AutoVoiceId;

        protected override string AssignedPrinterId => PrinterId;
        protected override string AssignedAuthorId => AuthorId;
        protected virtual float AssignedRevealSpeed => RevealSpeed;
        protected virtual string AutoVoicePath { get; set; }
        protected IAudioManager AudioManager => Engine.GetService<IAudioManager>();
        protected IScriptPlayer ScriptPlayer => Engine.GetService<IScriptPlayer>();
        protected CharacterMetadata AuthorMeta => Engine.GetService<ICharacterManager>().Configuration.GetMetadataOrDefault(AssignedAuthorId);

        public override async UniTask PreloadResourcesAsync ()
        {
            await base.PreloadResourcesAsync();

            if (AudioManager.Configuration.EnableAutoVoicing && !string.IsNullOrEmpty(PlaybackSpot.ScriptName))
            {
                AutoVoicePath = BuildAutoVoicePath();
                await AudioManager.VoiceLoader.LoadAndHoldAsync(AutoVoicePath, this);
            }

            if (Assigned(AuthorId) && !AuthorId.DynamicValue && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                await AudioManager.AudioLoader.LoadAndHoldAsync(AuthorMeta.MessageSound, this);
        }

        public override void ReleasePreloadedResources ()
        {
            base.ReleasePreloadedResources();

            AudioManager?.VoiceLoader?.Release(AutoVoicePath, this);

            if (Assigned(AuthorId) && !AuthorId.DynamicValue && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                AudioManager?.AudioLoader?.Release(AuthorMeta.MessageSound, this);
        }

        public override async UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            var printer = await GetOrAddPrinterAsync(asyncToken);
            var metadata = PrinterManager.Configuration.GetMetadataOrDefault(printer.Id);

            var resetText = ShouldResetText(metadata);
            if (resetText) ResetText(printer);

            if (!printer.Visible)
                await ShowPrinterAsync(printer, metadata, asyncToken);

            if (ShouldSetDefaultPrinter(metadata))
                SetDefaultPrinter(printer, asyncToken);

            if (metadata.StopVoice) AudioManager.StopVoice();

            if (ShouldPlayAutoVoice())
                await PlayAutoVoiceAsync(printer, asyncToken);

            // Evaluate whether to append before printing to account prior printer state.
            var appendBacklog = ShouldAppendBacklog(printer, metadata, resetText);

            if (ShouldAppendLineBreak(printer, metadata))
                await AppendLineBreakAsync(printer, metadata, asyncToken);

            // Copy to a temp var to prevent multiple evaluations of dynamic values.
            var printedText = Text.Value;
            if (string.IsNullOrEmpty(printedText)) return;

            if (ShouldApplyAuthoredTemplate(metadata))
                printedText = ApplyAuthoredTemplate(printedText, AssignedAuthorId, metadata.AuthoredTemplate);

            await PrintTextAsync(printedText, printer, asyncToken);

            if (metadata.PrintFrameDelay > 0) // Wait at least one frame to make the printed text visible while in skip mode.
                await AsyncUtils.DelayFrameAsync(metadata.PrintFrameDelay, asyncToken);

            if (ShouldWaitForInput(metadata, asyncToken))
                await WaitForInputAsync(printedText, asyncToken);
            else if (ShouldAllowRollbackWhenInputNotAwaited(asyncToken))
                Engine.GetService<IStateManager>()?.PeekRollbackStack()?.AllowPlayerRollback();

            if (metadata.AddToBacklog)
                AddBacklog(printedText, appendBacklog);
        }

        protected virtual bool ShouldResetText (TextPrinterMetadata metadata)
        {
            return Assigned(ResetPrinter) && ResetPrinter.Value || !Assigned(ResetPrinter) && metadata.AutoReset;
        }

        protected virtual void ResetText (ITextPrinterActor printer)
        {
            printer.Text = string.Empty;
            printer.RevealProgress = 0f;
        }

        protected virtual string BuildAutoVoicePath ()
        {
            return AudioManager.Configuration.AutoVoiceMode == AutoVoiceMode.ContentHash
                ? AudioConfiguration.GetAutoVoiceClipPath(this)
                : AudioConfiguration.GetAutoVoiceClipPath(PlaybackSpot);
        }

        protected virtual async UniTask ShowPrinterAsync (ITextPrinterActor printer, TextPrinterMetadata metadata, AsyncToken asyncToken)
        {
            var showDuration = Assigned(ChangeVisibilityDuration) ? ChangeVisibilityDuration.Value : metadata.ChangeVisibilityDuration;
            var showTask = printer.ChangeVisibilityAsync(true, showDuration, asyncToken: asyncToken);
            if (metadata.WaitVisibilityBeforePrint) await showTask;
            else showTask.Forget();
        }

        protected virtual bool ShouldSetDefaultPrinter (TextPrinterMetadata metadata)
        {
            return Assigned(DefaultPrinter) && DefaultPrinter.Value || !Assigned(DefaultPrinter) && metadata.AutoDefault;
        }

        protected virtual void SetDefaultPrinter (ITextPrinterActor defaultPrinter, AsyncToken asyncToken)
        {
            if (PrinterManager.DefaultPrinterId != defaultPrinter.Id)
                PrinterManager.DefaultPrinterId = defaultPrinter.Id;

            foreach (var printer in PrinterManager.GetAllActors())
                if (printer.Id != defaultPrinter.Id && printer.Visible)
                    HideOtherPrinter(printer);

            void HideOtherPrinter (ITextPrinterActor other)
            {
                var otherMeta = PrinterManager.Configuration.GetMetadataOrDefault(other.Id);
                var otherHideDuration = Assigned(ChangeVisibilityDuration) ? ChangeVisibilityDuration.Value : otherMeta.ChangeVisibilityDuration;
                other.ChangeVisibilityAsync(false, otherHideDuration, asyncToken: asyncToken).Forget();
            }
        }

        protected virtual bool ShouldPlayAutoVoice ()
        {
            return AudioManager.Configuration.EnableAutoVoicing &&
                   !string.IsNullOrEmpty(PlaybackSpot.ScriptName) &&
                   !ScriptPlayer.SkipActive;
        }

        protected virtual async UniTask PlayAutoVoiceAsync (ITextPrinterActor printer, AsyncToken asyncToken)
        {
            if (string.IsNullOrEmpty(AutoVoicePath)) AutoVoicePath = BuildAutoVoicePath();
            if (!await AudioManager.VoiceLoader.ExistsAsync(AutoVoicePath)) return;
            var playedVoicePath = AudioManager.GetPlayedVoicePath();
            if (AudioManager.Configuration.VoiceOverlapPolicy == VoiceOverlapPolicy.PreventCharacterOverlap &&
                printer.AuthorId == AssignedAuthorId && !string.IsNullOrEmpty(playedVoicePath))
                AudioManager.StopVoice();
            await AudioManager.PlayVoiceAsync(AutoVoicePath, authorId: AssignedAuthorId, asyncToken: asyncToken);
        }

        protected virtual bool ShouldAppendBacklog (ITextPrinterActor printer, TextPrinterMetadata metadata, bool resetText)
        {
            return !metadata.SplitBacklogMessages && !resetText && !string.IsNullOrEmpty(printer.Text) && AssignedAuthorId == printer.AuthorId;
        }

        protected virtual bool ShouldAppendLineBreak (ITextPrinterActor printer, TextPrinterMetadata metadata)
        {
            return Assigned(LineBreaks) && LineBreaks > 0 || !Assigned(LineBreaks) && metadata.AutoLineBreak > 0 && !string.IsNullOrWhiteSpace(printer.Text);
        }

        protected virtual async UniTask AppendLineBreakAsync (ITextPrinterActor printer, TextPrinterMetadata metadata, AsyncToken asyncToken)
        {
            var appendCommand = new AppendLineBreak {
                PrinterId = printer.Id,
                AuthorId = AssignedAuthorId,
                Count = Assigned(LineBreaks) ? LineBreaks.Value : metadata.AutoLineBreak
            };
            await appendCommand.ExecuteAsync(asyncToken);
        }

        protected virtual bool ShouldApplyAuthoredTemplate (TextPrinterMetadata metadata)
        {
            return !string.IsNullOrEmpty(AssignedAuthorId) && !string.IsNullOrEmpty(metadata.AuthoredTemplate);
        }

        protected virtual string ApplyAuthoredTemplate (string text, string authorId, string template)
        {
            var author = CharacterManager.GetDisplayName(authorId) ?? authorId;
            return template.Replace("%TEXT%", text).Replace("%AUTHOR%", author);
        }

        protected virtual async UniTask PrintTextAsync (string text, ITextPrinterActor printer, AsyncToken asyncToken)
        {
            await PrinterManager.PrintTextAsync(printer.Id, text, AssignedAuthorId, AssignedRevealSpeed, asyncToken);
            printer.RevealProgress = 1f; // Make sure all the text is always revealed.
        }

        protected virtual bool ShouldWaitForInput (TextPrinterMetadata metadata, AsyncToken asyncToken)
        {
            if (asyncToken.Completed && !metadata.WaitAfterRevealSkip) return false;
            if (Assigned(WaitForInput)) return WaitForInput.Value;
            return metadata.AutoWait;
        }

        protected virtual bool ShouldAllowRollbackWhenInputNotAwaited (AsyncToken asyncToken)
        {
            // Required for rollback to work when WaitAfterRevealSkip is disabled.
            return !(Assigned(WaitForInput) && !WaitForInput) && asyncToken.Completed;
        }

        protected virtual async UniTask WaitForInputAsync (string text, AsyncToken asyncToken)
        {
            if (ScriptPlayer.AutoPlayActive) // Add delay per printed chars when in auto mode.
            {
                var baseDelay = Configuration.ScaleAutoWait ? PrinterManager.BaseAutoDelay * AssignedRevealSpeed : PrinterManager.BaseAutoDelay;
                var autoPlayDelay = Mathf.Lerp(0, Configuration.MaxAutoWaitDelay, baseDelay) * text.Count(char.IsLetterOrDigit);
                var waitUntilTime = Time.time + autoPlayDelay;
                while ((Time.time < waitUntilTime || PlayingVoice()) && asyncToken.EnsureNotCanceledOrCompleted())
                    await AsyncUtils.DelayFrameAsync(1);
            }

            ScriptPlayer.SetWaitingForInputEnabled(true);

            bool PlayingVoice () => ShouldPlayAutoVoice() && AudioManager.GetPlayedVoicePath() == AutoVoicePath;
        }

        protected virtual void AddBacklog (string message, bool append)
        {
            var backlogUI = Engine.GetService<IUIManager>().GetUI<IBacklogUI>();
            if (backlogUI is null) return;
            var voiceClipName = AudioManager.VoiceLoader.IsLoaded(AutoVoicePath) ? AutoVoicePath : null;
            if (append) backlogUI.AppendMessage(message, voiceClipName, PlaybackSpot);
            else backlogUI.AddMessage(message, AssignedAuthorId, voiceClipName, PlaybackSpot);
        }
    }
}
