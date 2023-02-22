// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Registers a public static method with supported argument types as a console command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ConsoleCommandAttribute : Attribute
    {
        /// <summary>
        /// When provided, alias is used instead of method name to reference the command.
        /// </summary>
        public string Alias { get; }

        public ConsoleCommandAttribute (string alias = null)
        {
            Alias = alias;
        }
    }
}
