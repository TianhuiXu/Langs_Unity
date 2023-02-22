// Copyright 2022 ReWaffle LLC. All rights reserved.

using Naninovel.Parsing;

namespace Naninovel
{
    public class LabelLineParser : ScriptLineParser<LabelScriptLine, LabelLine>
    {
        protected override ParseLine<LabelLine> ParseFunc => parser.Parse;
        private readonly Parsing.LabelLineParser parser;

        public LabelLineParser (IErrorHandler errorHandler = null)
        {
            parser = new Parsing.LabelLineParser(errorHandler);
        }

        protected override LabelScriptLine Parse (LabelLine lineModel)
        {
            return new LabelScriptLine(lineModel.Label.Text, LineIndex, LineHash);
        }
    }
}
