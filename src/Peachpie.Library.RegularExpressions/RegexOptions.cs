// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Peachpie.Library.RegularExpressions
{
    [Flags]
    public enum RegexOptions
    {
        None                    = 0x0000,
        IgnoreCase              = 0x0001, // "i"
        Multiline               = 0x0002, // "m"
        ExplicitCapture         = 0x0004, // "n"
        Compiled                = 0x0008, // "c"
        Singleline              = 0x0010, // "s"
        IgnorePatternWhitespace = 0x0020, // "x"
        RightToLeft             = 0x0040, // "r"

#if DEBUG
        Debug                   = 0x0080, // "d"
#endif

        ECMAScript              = 0x0100, // "e"
        CultureInvariant        = 0x0200,

        //
        // Perl regular expression specific options.
        //
        
        PCRE_CASELESS = IgnoreCase,         // i
        PCRE_MULTILINE = Multiline,         // m
        PCRE_DOTALL = Singleline,           // s
        PCRE_EXTENDED = IgnorePatternWhitespace,      // x
        PCRE_ANCHORED = 0x0400,             // A
        PCRE_DOLLAR_ENDONLY = 0x0800,       // D
        PCRE_UNGREEDY = 0x1000,             // U
        PCRE_UTF8 = 0x2000,                 // u
        PCRE_EXTRA = 0x4000,                // X

        /// <summary>
        /// Spend more time studying the pattern - ignoring.
        /// </summary>
        PCRE_S = 0x8000,                    // S

        ///// <summary>
        ///// Evaluate as PHP code.
        ///// Deprecated and removed.
        ///// </summary>
        //PREG_REPLACE_EVAL = 0x10000,        // e

        //
        // PCRE options for newline handling
        //

        PCRE_NEWLINE_CR         = 0x00100000,   // (*CR)
        PCRE_NEWLINE_LF         = 0x00200000,   // (*LF)
        PCRE_NEWLINE_CRLF       = 0x00300000,   // (*CRLF)
        PCRE_NEWLINE_ANY        = 0x00400000,   // (*ANY)
        PCRE_NEWLINE_ANYCRLF    = 0x00500000,   // (*ANYCRLF)
        PCRE_BSR_ANYCRLF        = 0x00800000,   // (*BSR_ANYCRLF)
        PCRE_BSR_UNICODE        = 0x01000000    // (*BSR_UNICODE)
    }

    public static class RegexOptionExtensions
    {
        private const RegexOptions NewlineConventionMask =
            RegexOptions.PCRE_NEWLINE_CR | RegexOptions.PCRE_NEWLINE_LF | RegexOptions.PCRE_NEWLINE_CRLF | RegexOptions.PCRE_NEWLINE_ANY | RegexOptions.PCRE_NEWLINE_ANYCRLF;

        private const RegexOptions BsrNewlineConventionMask =
            RegexOptions.PCRE_BSR_ANYCRLF | RegexOptions.PCRE_BSR_UNICODE;

        /// <summary>
        /// Clear previous newline convention and set it to the given one.
        /// </summary>
        public static RegexOptions WithNewlineConvention(this RegexOptions options, RegexOptions newlineConvention)
        {
            Debug.Assert((newlineConvention & ~(NewlineConventionMask)) == 0);

            return (options & ~NewlineConventionMask) | newlineConvention;
        }

        /// <summary>
        /// Clear previous \R newline convention and set it to the given one.
        /// </summary>
        public static RegexOptions WithBsrNewlineConvention(this RegexOptions options, RegexOptions bsrNewlineConvention)
        {
            Debug.Assert((bsrNewlineConvention & ~(BsrNewlineConventionMask)) == 0);

            return (options & ~BsrNewlineConventionMask) | bsrNewlineConvention;
        }
    }
}
