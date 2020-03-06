using System;
using System.Collections.Generic;
using System.Text;

namespace Peachpie.Library.RegularExpressions.Utils
{
    internal static class StringExtensions
    {
        public static bool Equals(this ReadOnlySpan<char> span, string str, StringComparison comparisonType = StringComparison.Ordinal) =>
            MemoryExtensions.Equals(span, str.AsSpan(), comparisonType);
    }
}
