// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Represents a serializable <see cref="Command"/> parameter with <see cref="NamedString"/> value.
    /// </summary>
    [Serializable]
    public class NamedStringParameter : NamedParameter<NamedString, NullableString>
    {
        public static implicit operator NamedStringParameter (NamedString value) => new NamedStringParameter { Value = value };
        public static implicit operator NamedString (NamedStringParameter param) => param is null || !param.HasValue ? null : param.Value;

        protected override NamedString ParseValueText (string valueText, out string errors)
        {
            ParseNamedValueText(valueText, out var name, out var value, out errors);
            return new NamedString(name, value);
        }
    }
}
