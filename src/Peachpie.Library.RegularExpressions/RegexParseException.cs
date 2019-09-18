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
        public int? Offset { get; }

        public RegexParseException(int offset, string message, Exception inner)
            : base(message, inner)
        {
            Offset = offset;
        }

        public RegexParseException(int offset, string message)
            : this(offset, message, null)
        {
        }

        public RegexParseException(string message)
            : this(message, null)
        {
        }

        public RegexParseException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
