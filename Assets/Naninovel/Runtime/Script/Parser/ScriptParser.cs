// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptParser"/>
    public class ScriptParser : IScriptParser
    {
        protected virtual CommentLineParser CommentLineParser { get; }
        protected virtual LabelLineParser LabelLineParser { get; }
        protected virtual CommandLineParser CommandLineParser { get; }
        protected virtual GenericTextLineParser GenericTextLineParser { get; }

        private readonly Lexer lexer = new Lexer();
        private readonly List<Token> tokens = new List<Token>();
        private readonly List<ScriptLine> lines = new List<ScriptLine>();
        private readonly ParseErrorHandler errorHandler = new ParseErrorHandler();

        public ScriptParser ()
        {
            CommentLineParser = new CommentLineParser(errorHandler);
            LabelLineParser = new LabelLineParser(errorHandler);
            CommandLineParser = new CommandLineParser(errorHandler);
            GenericTextLineParser = new GenericTextLineParser(errorHandler);
        }

        public virtual Script ParseText (string scriptName, string scriptText, ICollection<ScriptParseError> errors = null)
        {
            errorHandler.Errors = errors;
            lines.Clear();
            var textLines = Parsing.ScriptParser.SplitText(scriptText);
            for (int i = 0; i < textLines.Length; i++)
                lines.Add(ParseLine(i, textLines[i]));
            var script = Script.FromLines(scriptName, lines);
            return script;

            ScriptLine ParseLine (int lineIndex, string lineText)
            {
                if (string.IsNullOrWhiteSpace(lineText))
                    return new EmptyScriptLine(lineIndex);
                errorHandler.LineIndex = lineIndex;
                tokens.Clear();
                var lineType = lexer.TokenizeLine(lineText, tokens);
                switch (lineType)
                {
                    case LineType.Comment: return ParseCommentLine(scriptName, lineIndex, lineText, tokens);
                    case LineType.Label: return ParseLabelLine(scriptName, lineIndex, lineText, tokens);
                    case LineType.Command: return ParseCommandLine(scriptName, lineIndex, lineText, tokens);
                    case LineType.Generic: return ParseGenericTextLine(scriptName, lineIndex, lineText, tokens);
                    default: throw new Error($"Unknown line type: {lineType}");
                }
            }
        }

        protected virtual CommentScriptLine ParseCommentLine (string scriptName, int lineIndex,
            string lineText, IReadOnlyList<Token> tokens)
        {
            return CommentLineParser.Parse(scriptName, lineIndex, lineText, tokens);
        }

        protected virtual LabelScriptLine ParseLabelLine (string scriptName, int lineIndex,
            string lineText, IReadOnlyList<Token> tokens)
        {
            return LabelLineParser.Parse(scriptName, lineIndex, lineText, tokens);
        }

        protected virtual CommandScriptLine ParseCommandLine (string scriptName, int lineIndex,
            string lineText, IReadOnlyList<Token> tokens)
        {
            return CommandLineParser.Parse(scriptName, lineIndex, lineText, tokens);
        }

        protected virtual GenericTextScriptLine ParseGenericTextLine (string scriptName, int lineIndex,
            string lineText, IReadOnlyList<Token> tokens)
        {
            return GenericTextLineParser.Parse(scriptName, lineIndex, lineText, tokens);
        }
    }
}
