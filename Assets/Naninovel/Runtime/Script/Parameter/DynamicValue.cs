// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Naninovel
{
    /// <summary>
    /// Represents data required to evaluate dynamic value of a <see cref="ICommandParameter"/>.
    /// </summary>
    [Serializable]
    public class DynamicValue
    {
        public PlaybackSpot PlaybackSpot;
        public string ValueText;
        public string[] Expressions;

        public DynamicValue () { }

        public DynamicValue (PlaybackSpot playbackSpot, string valueText, IEnumerable<string> expressions)
        {
            PlaybackSpot = playbackSpot;
            ValueText = valueText;
            Expressions = expressions.ToArray();
        }
    }
}