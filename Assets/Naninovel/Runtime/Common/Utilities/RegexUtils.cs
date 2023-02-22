// Copyright 2022 ReWaffle LLC. All rights reserved.

using System.Text.RegularExpressions;

namespace Naninovel
{
    public static class RegexUtils
    {
        /// <summary>
        /// Get index of the last character in the match.
        /// </summary>
        public static int GetEndIndex (this Match match)
        {
            return match.Index + match.Length - 1;
        }
    }
}
