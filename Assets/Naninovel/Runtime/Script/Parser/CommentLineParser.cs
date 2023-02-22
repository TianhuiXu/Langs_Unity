// Copyright 2022 ReWaffle LLC. All rights reserved.

using Naninovel.Parsing;

namespace Naninovel
{
    public class CommentLineParser : ScriptLineParser<CommentScriptLine, CommentLine>
    {
        protected override ParseLine<CommentLine> ParseFunc => parser.Parse;
        private readonly Parsing.CommentLineParser parser;

        public CommentLineParser (IErrorHandler errorHandler = null)
        {
            parser = new Parsing.CommentLineParser(errorHandler);
        }

        protected override CommentScriptLine Parse (CommentLine lineModel)
        {
            return new CommentScriptLine(lineModel.Comment.Text, LineIndex, LineHash);
        }
    }
}
