// Copyright 2022 ReWaffle LLC. All rights reserved.

using Naninovel.Parsing;

namespace Naninovel
{
    public class CommandLineParser : ScriptLineParser<CommandScriptLine, CommandLine>
    {
        protected override ParseLine<CommandLine> ParseFunc => parser.Parse;
        protected virtual CommandParser CommandParser { get; }

        private readonly Parsing.CommandLineParser parser;

        public CommandLineParser (IErrorHandler errorHandler = null)
        {
            CommandParser = new CommandParser(errorHandler);
            parser = new Parsing.CommandLineParser(errorHandler);
        }

        protected override CommandScriptLine Parse (CommandLine lineModel)
        {
            var command = CommandParser.Parse(lineModel.Command, ScriptName, LineIndex, 0);
            return new CommandScriptLine(command, LineIndex, LineHash);
        }
    }
}
