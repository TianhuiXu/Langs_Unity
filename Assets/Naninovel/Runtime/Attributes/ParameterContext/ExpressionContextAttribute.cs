// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    /// <summary>
    /// Can be applied to a command parameter for expression functions.
    /// Used by the bridging service to provide the context for external tools (IDE extension, web editor, etc).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ExpressionContextAttribute : ParameterContextAttribute
    {
        public ExpressionContextAttribute (int namedIndex = -1, string paramId = null)
            : base(ValueContextType.Expression, "", namedIndex, paramId) { }
    }
}
