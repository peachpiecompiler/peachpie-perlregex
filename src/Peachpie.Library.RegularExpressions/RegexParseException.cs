// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Peachpie.Library.RegularExpressions
{
    public sealed class RegexParseException : ArgumentException
    {
        /// <summary>
        /// The offset in the supplied pattern.
        /// </summary>
        public int Offset { get; } = -1;

        public RegexParseException(int offset, string message)
            : base(message)
        {
            Offset = offset;
        }

        public RegexParseException() : base()
        {
        }

        public RegexParseException(string message) : base(message)
        {
        }

        public RegexParseException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
