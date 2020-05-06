using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Peachpie.Library.RegularExpressions
{
    /// <summary>
    /// Helper class to transform typical UTF-8 ranges expressed as sequences of byte ranges to UTF-16 ranges,
    /// e.g. <c>[\xC2-\xDF][\x80-\xBF]</c> to <c>[\u0080-\u07FF]</c>.
    /// </summary>
    internal static class RegexUtf8RangeTransformer
    {
        /// <summary>
        /// State of a helper automaton to identify the sequence of ranges and supply the resulting UTF-16 range.
        /// All its states are immutable and static, the user traverses it by navigating through their references.
        /// </summary>
        private class MatchState
        {
            public string Utf16Range { get; }

            public bool IsFinal => Utf16Range != null;

            private delegate MatchState NextStateMatcher(char lower, char upper);

            private NextStateMatcher NextMatcher { get; }

            private MatchState(string utf16Range, NextStateMatcher next)
            {
                this.Utf16Range = utf16Range;
                this.NextMatcher = next;
            }

            private static MatchState CreateIntermediate(NextStateMatcher nextMatcher) => new MatchState(null, nextMatcher);

            private static MatchState CreateFinal(char utf16RangeFirst, char utf16RangeLast)
            {
                var charClass = new RegexCharClass();
                charClass.AddRange(utf16RangeFirst, utf16RangeLast);

                return new MatchState(charClass.ToStringClass(), (f, l) => null);
            }

            public MatchState MatchNextState(char singleChar) => NextMatcher(singleChar, singleChar) ?? Start;

            public MatchState MatchNextState(string range)
            {
                // We are interested only in single ranges, e.g. [\x80-\xBF]
                if (range.Length == 5 && range.StartsWith("\x00\x02\x00"))
                {
                    return NextMatcher(range[3], (char)(range[4] - 1)) ?? Start;
                }
                else
                {
                    return Start;
                }
            }

            /// <summary>
            /// Initial state of the automaton waiting for the first range input.
            /// </summary>
            public static MatchState Start = CreateIntermediate((f, l) =>
                (f, l) switch
                {
                    ('\xC2', '\xDF') => TwoByte1,                   //  [\xC2-\xDF][\x80-\xBF]     => [\u0080-\u07FF]
                    ('\xE0', '\xE0') => ThreeByteNoOverlongs1,      //  \xE0[\xA0-\xBF][\x80-\xBF] => [\u0800-\u0FFF]
                    ('\xE1', '\xEC') => ThreeByteStraight1,         //  [\xE1-\xEC][\x80-\xBF]{2}  => [\u1000-\uCFFF]
                    ('\xED', '\xED') => ThreeBytePresurrogates1,    //  \xED[\x80-\x9F][\x80-\xBF] => [\uD000-\uD7FF]
                    ('\xEE', '\xEF') => ThreeBytePostsurrogates1,   //  [\xEE-\xEF][\x80-\xBF]{2}  => [\uE000-\uFFFF]
                    _ => null
                });

            private static MatchState TwoByte1 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? TwoByte2 : null);

            private static MatchState TwoByte2 = CreateFinal('\u0080', '\u07FF');


            private static MatchState ThreeByteNoOverlongs1 = CreateIntermediate((f, l) => (f, l) == ('\xA0', '\xBF') ? ThreeByteNoOverlongs2 : null);

            private static MatchState ThreeByteNoOverlongs2 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? ThreeByteNoOverlongs3 : null);

            private static MatchState ThreeByteNoOverlongs3 = CreateFinal('\u0800', '\u0FFF');


            private static MatchState ThreeByteStraight1 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? ThreeByteStraight2 : null);

            private static MatchState ThreeByteStraight2 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? ThreeByteStraight3 : null);

            private static MatchState ThreeByteStraight3 = CreateFinal('\u1000', '\uCFFF');


            private static MatchState ThreeBytePresurrogates1 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\x9F') ? ThreeBytePresurrogates2 : null);

            private static MatchState ThreeBytePresurrogates2 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? ThreeBytePresurrogates3 : null);

            private static MatchState ThreeBytePresurrogates3 = CreateFinal('\uD000', '\uD7FF');


            private static MatchState ThreeBytePostsurrogates1 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? ThreeBytePostsurrogates2 : null);

            private static MatchState ThreeBytePostsurrogates2 = CreateIntermediate((f, l) => (f, l) == ('\x80', '\xBF') ? ThreeBytePostsurrogates3 : null);

            private static MatchState ThreeBytePostsurrogates3 = CreateFinal('\uE000', '\uFFFF');
        }

        /// <summary>
        /// Attempts to identify common patterns for matching UTF-8 ranges and convert them to UTF-16 ranges
        /// by modifying the children of the given concatenation.
        /// </summary>
        /// <param name="concatenation"></param>
        public static void TryTransformRanges(RegexNode concatenation)
        {
            if (concatenation.Children == null)
            {
                return;
            }

            // Mark the state of the current search and its start in the node list
            var matchState = MatchState.Start;
            int iMatchStart = -1;

            for (int i = 0; i < concatenation.Children.Count; i++)
            {
                var child = concatenation.Children[i];
                switch (child.Type())
                {
                    case RegexNode.One:
                        // Single character is equivalent to an interval [c-c]
                        matchState = matchState.MatchNextState(child.Ch);
                        break;

                    case RegexNode.Set:
                        matchState = matchState.MatchNextState(child.Str);
                        break;

                    case RegexNode.Setloop:
                        if (child.M != child.N || child.M > 3)
                        {
                            goto default;
                        }
                        else
                        {
                            // Either the whole loop is accepted or nothing (there's no splitting the loop)
                            for (int j = 0; j < child.N; j++)
                            {
                                matchState = matchState.MatchNextState(child.Str);
                            }
                        }
                        break;

                    default:
                        // Any other node type resets the matching logic
                        iMatchStart = -1;
                        matchState = MatchState.Start;
                        break;
                }

                if (matchState != MatchState.Start)
                {
                    if (iMatchStart == -1)
                    {
                        iMatchStart = i;
                    }

                    if (matchState.IsFinal)
                    {
                        // Replace the matched sequence by a single range
                        concatenation.Children[iMatchStart] = new RegexNode(RegexNode.Set, concatenation.Options, matchState.Utf16Range);
                        concatenation.Children.RemoveRange(iMatchStart + 1, i - iMatchStart);

                        // Fix iteration variable after the range removal
                        i = iMatchStart;

                        // Reset the found match
                        iMatchStart = -1;
                        matchState = MatchState.Start;
                    }
                }
                else
                {
                    // Reset the current match start in case of a partial but unsuccessful match
                    iMatchStart = -1;
                }
            }
        }
    }
}
