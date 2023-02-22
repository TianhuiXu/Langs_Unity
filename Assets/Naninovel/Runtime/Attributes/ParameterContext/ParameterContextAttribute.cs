// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using Naninovel.Metadata;

namespace Naninovel
{
    public abstract class ParameterContextAttribute : Attribute
    {
        public readonly ValueContextType Type;
        public readonly string SubType;
        public readonly string ParameterId;
        public readonly int NamedIndex;

        /// <param name="namedIndex">When applied to named parameter, specify index of the associated value (0 is for name and 1 for value).</param>
        /// <param name="paramId">When attribute is applied to a class, specify parameter field name.</param>
        protected ParameterContextAttribute (ValueContextType type, string subType, int namedIndex = -1, string paramId = null)
        {
            Type = type;
            SubType = subType;
            ParameterId = paramId;
            NamedIndex = namedIndex;
        }
    }
}
