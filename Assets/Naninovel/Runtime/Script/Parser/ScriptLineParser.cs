// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel
{
    public abstract class ScriptLineParser<TResult, TModel>
        where TResult : ScriptLine
        where TModel : IScriptLine
    {
        protected abstract ParseLine<TModel> ParseFunc { get; }
        protected virtual string ScriptName { get; private set; }
        protected virtual int LineIndex { get; private set; }
        protected virtual string LineHash { get; private set; }

        /// <summary>
        /// Produces a persistent hash code from the provided script line text (trimmed).
        /// </summary>
        public static string GetHash (string lineText)
        {
            return CryptoUtils.PersistentHexCode(lineText.TrimFull());
        }

        public virtual TResult Parse (string scriptName, int lineIndex,
            string lineText, IReadOnlyList<Token> tokens)
        {
            ScriptName = scriptName;
            LineIndex = lineIndex;
            LineHash = GetHash(lineText);
            var lineModel = ParseFunc(lineText, tokens);
            var result = Parse(lineModel);
            return result;
        }

        protected abstract TResult Parse (TModel lineModel);
    }
}
