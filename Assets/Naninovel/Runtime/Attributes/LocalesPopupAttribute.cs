// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Draws a selectable dropdown list (popup) of available locales (language tags) based on <see cref="LanguageTags"/>.
    /// </summary>
    public class LocalesPopupAttribute : PropertyAttribute
    {
        public readonly bool IncludeEmpty;

        /// <param name="includeEmpty">Whether to include an empty ('None') option to the list.</param>
        public LocalesPopupAttribute (bool includeEmpty)
        {
            IncludeEmpty = includeEmpty;
        }
    }
}
