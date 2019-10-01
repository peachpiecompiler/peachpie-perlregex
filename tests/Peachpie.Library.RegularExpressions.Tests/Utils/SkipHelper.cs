using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Peachpie.Library.RegularExpressions.Tests
{
    /// <summary>
    /// Helper class to enable selective test skipping and forcing it to run.
    /// </summary>
    internal static class SkipHelper
    {
        /// <summary>
        /// Constant to add in the front of a test pattern to cause its skipping using <see cref="SkipIfMarked(ref string)"/>.
        /// The reason should be mentioned in a comment.
        /// </summary>
        public const string SkippedPattern = "SKIP_THIS_";

        /// <summary>
        /// Constant to add in the front of a test pattern to cause its skipping using <see cref="SkipIfMarked(ref string)"/>.
        /// The reason is the different handling of named groups, such as (?<123>foo) or (?'abcd'bar).
        /// </summary>
        public const string SkippedPatternNamedGroup = SkippedPattern;

        /// <summary>
        /// The environment variable marking to ignore skipping and run all tests.
        /// </summary>
        public const string ForceSkippedEnvironmentVariable = "PEACHPIE_PERLREGEX_TESTS_FORCE_SKIPPED";
        public const string ForceSkippedEnvironmentVariableValue = "1";

        /// <summary>
        /// Skip the test if the pattern starts with <see cref="SkippedPattern"/> unless the environment variable
        /// <see cref="ForceSkippedEnvironmentVariable"/> is set to <see cref="ForceSkippedEnvironmentVariableValue"/>.
        /// </summary>
        public static void SkipIfMarked(ref string pattern)
        {
            if (pattern.StartsWith(SkippedPattern))
            {
                if (Environment.GetEnvironmentVariable(ForceSkippedEnvironmentVariable) == ForceSkippedEnvironmentVariableValue)
                    pattern = pattern.Substring(SkippedPattern.Length);
                else
                    Skip.If(true);
            }
        }
    }
}
