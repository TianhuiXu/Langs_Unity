// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Naninovel.Commands;
using Naninovel.Parsing;

namespace Naninovel
{
    public class GenericTextLineParser : ScriptLineParser<GenericTextScriptLine, GenericLine>
    {
        protected override ParseLine<GenericLine> ParseFunc => parser.Parse;
        protected virtual CommandParser CommandParser { get; }
        protected virtual GenericLine Model { get; private set; }
        protected virtual IList<Command> InlinedCommands { get; } = new List<Command>();
        protected virtual string AuthorId => Model.Prefix?.Author ?? "";
        protected virtual string AuthorAppearance => Model.Prefix?.Appearance ?? "";

        private readonly GenericLineParser parser;
        private readonly ScriptSerializer serializer = new ScriptSerializer();

        public GenericTextLineParser (IErrorHandler errorHandler = null)
        {
            parser = new GenericLineParser(errorHandler);
            CommandParser = new CommandParser(errorHandler);
        }

        protected override GenericTextScriptLine Parse (GenericLine lineModel)
        {
            ResetState(lineModel);
            AddAppearanceChange();
            AddContent();
            AddLastWaitInput();
            return new GenericTextScriptLine(InlinedCommands, LineIndex, LineHash);
        }

        protected virtual void ResetState (GenericLine model)
        {
            Model = model;
            InlinedCommands.Clear();
        }

        protected virtual void AddAppearanceChange ()
        {
            if (string.IsNullOrEmpty(AuthorId)) return;
            if (string.IsNullOrEmpty(AuthorAppearance)) return;
            AddCommand($"char {AuthorId}.{AuthorAppearance} wait:false");
        }

        protected virtual void AddContent ()
        {
            foreach (var content in Model.Content)
                if (content is InlinedCommand inlined) AddCommand(inlined.Command);
                else AddGenericText(content as MixedValue);
        }

        protected virtual void AddCommand (Parsing.Command commandModel)
        {
            var command = CommandParser.Parse(commandModel, ScriptName,
                LineIndex, InlinedCommands.Count);
            AddCommand(command);
        }

        protected virtual void AddCommand (string bodyText)
        {
            var command = CommandParser.Parse(bodyText, ScriptName,
                LineIndex, InlinedCommands.Count);
            AddCommand(command);
        }

        protected virtual void AddCommand (Command command)
        {
            if (command is WaitForInput && InlinedCommands.LastOrDefault() is PrintText print)
                print.WaitForInput = true;
            else InlinedCommands.Add(command);
        }

        protected virtual void AddGenericText (MixedValue genericText)
        {
            var text = serializer.Serialize(genericText, true);
            var author = string.IsNullOrEmpty(AuthorId) ? "" : $"author:{AuthorId}";
            var printedBefore = InlinedCommands.Any(c => c is PrintText);
            var reset = printedBefore ? "reset:false" : "";
            var br = printedBefore ? "br:0" : "";
            AddCommand($"print {text} {author} {reset} {br} wait:true waitInput:false");
        }

        protected virtual void AddLastWaitInput ()
        {
            if (InlinedCommands.Any(c => c is SkipInput)) return;
            if (InlinedCommands.LastOrDefault() is WaitForInput) return;
            if (InlinedCommands.LastOrDefault() is PrintText print)
                print.WaitForInput = true;
            else AddCommand("i");
        }
    }
}
