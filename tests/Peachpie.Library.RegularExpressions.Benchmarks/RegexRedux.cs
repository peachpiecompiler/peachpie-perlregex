using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoreFxRegex = System.Text.RegularExpressions;
using PeachpieRegex = Peachpie.Library.RegularExpressions;

namespace Peachpie.Library.RegularExpressions.Benchmarks
{
    /// <summary>
    /// Implementation of the regex-redux benchmark from The Computer Language Benchmarks Game
    /// </summary>
    /// <remarks>
    /// See https://benchmarksgame-team.pages.debian.net/benchmarksgame/description/regexredux.html#regexredux.
    /// 
    /// It can be run both using Peachpie PCRE or standard CoreFX regular expressions. Note that the PCRE expressions
    /// need to be enclosed in delimiters (e.g. "/expr/") to work.
    /// </remarks>
    public static class RegexRedux
    {
        public struct Result
        {
            public int[] Counts;
            public int InputLength;
            public int CleanedLength;
            public int ReplacedLength;
        }

        private abstract class ReduxBase
        {
            /// <summary>
            /// Helper structure to hold information about which pattern to replace with which text.
            /// </summary>
            protected struct ReplacementRecord
            {
                public string Pattern;
                public string Replacement;

                public ReplacementRecord(string pattern, string replacement)
                {
                    Pattern = pattern;
                    Replacement = replacement;
                }
            }

            #region Default patterns

            protected const string CleanupPattern = ">.*\n|\n";

            protected static readonly ReplacementRecord[] ReplacementPatterns =
                new[]
                {
                    new ReplacementRecord("tHa[Nt]", "<4>"),
                    new ReplacementRecord("aND|caN|Ha[DS]|WaS", "<3>"),
                    new ReplacementRecord("a[NSt]|BY", "<2>"),
                    new ReplacementRecord("<[^>]*>", "|"),
                    new ReplacementRecord( "\\|[^|][^|]*\\|", "-"),
                };

            protected static readonly string[] CountPatterns =
                new[]
                {
                    "agggtaaa|tttaccct",
                    "[cgt]gggtaaa|tttaccc[acg]",
                    "a[act]ggtaaa|tttacc[agt]t",
                    "ag[act]gtaaa|tttac[agt]ct",
                    "agg[act]taaa|ttta[agt]cct",
                    "aggg[acg]aaa|ttt[cgt]ccct",
                    "agggt[cgt]aa|tt[acg]accct",
                    "agggta[cgt]a|t[acg]taccct",
                    "agggtaa[cgt]|[acg]ttaccct"
                };

            #endregion

            protected abstract string Clean(string sequences);

            protected abstract string Replace(int replacePatternIndex, string sequences);

            protected abstract int Count(int countPatternIndex, string sequences);

            public Result Run(string sequences)
            {
                // Clean up description lines
                string cleanedSequences = Clean(sequences);

                // Replacements
                var replaceTask = Task.Run(() =>
                {
                    string newSequences = cleanedSequences;
                    for (int i = 0; i < ReplacementPatterns.Length; i++)
                    {
                        newSequences = Replace(i, newSequences);
                    }
                    return newSequences.Length;
                });

                // Counts
                var countTasks = new Task<int>[CountPatterns.Length];
                for (int i = 0; i < countTasks.Length; i++)
                {
                    int countPatternIndex = i;
                    countTasks[i] = Task.Run(() => Count(countPatternIndex, cleanedSequences));
                }

                // Gather and retrieve results
                replaceTask.Wait();
                Task.WaitAll(countTasks);
                return new Result
                {
                    InputLength = sequences.Length,
                    CleanedLength = cleanedSequences.Length,
                    ReplacedLength = replaceTask.Result,
                    Counts = countTasks.Select(t => t.Result).ToArray()
                };
            }
        }

        private sealed class CoreFxRedux : ReduxBase
        {
            private CoreFxRedux() { }

            public static CoreFxRedux Instance { get; } = new CoreFxRedux();

            protected override string Clean(string sequences)
            {
                return CoreFxRegex.Regex.Replace(sequences, CleanupPattern, "");
            }

            protected override int Count(int countPatternIndex, string sequences)
            {
                var r = new CoreFxRegex.Regex(CountPatterns[countPatternIndex]);
                int count = 0;
                for (var m = r.Match(sequences); m.Success; m = m.NextMatch())
                {
                    count++;
                }

                return count;
            }

            protected override string Replace(int replacePatternIndex, string sequences)
            {
                var record = ReplacementPatterns[replacePatternIndex];
                return CoreFxRegex.Regex.Replace(sequences, record.Pattern, record.Replacement);
            }
        }

        private sealed class PeachpieRedux : ReduxBase
        {
            #region PCRE patterns

            private static string AddDelimiters(string pattern) => $"/{pattern}/";

            private static readonly string PcreCleanupPattern = AddDelimiters(CleanupPattern);

            private static readonly ReplacementRecord[] PcreReplacementPatterns =
                ReplacementPatterns
                    .Select((record) => new ReplacementRecord(AddDelimiters(record.Pattern), record.Replacement))
                    .ToArray();

            private static readonly string[] PcreCountPatterns =
                CountPatterns.Select(AddDelimiters).ToArray();

            #endregion

            private PeachpieRedux() { }

            public static PeachpieRedux Instance { get; } = new PeachpieRedux();

            protected override string Clean(string sequences)
            {
                return PeachpieRegex.Regex.Replace(sequences, PcreCleanupPattern, "");
            }

            protected override int Count(int countPatternIndex, string sequences)
            {
                var r = new PeachpieRegex.Regex(PcreCountPatterns[countPatternIndex]);
                int count = 0;
                for (var m = r.Match(sequences); m.Success; m = m.NextMatch())
                {
                    count++;
                }

                return count;
            }

            protected override string Replace(int replacePatternIndex, string sequences)
            {
                var record = PcreReplacementPatterns[replacePatternIndex];
                return PeachpieRegex.Regex.Replace(sequences, record.Pattern, record.Replacement);
            }
        }

        public static Result RunCoreFx(string sequences) => CoreFxRedux.Instance.Run(sequences);

        public static Result RunPeachpie(string sequences) => PeachpieRedux.Instance.Run(sequences);
    }
}
