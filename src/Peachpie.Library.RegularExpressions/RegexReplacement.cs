// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The RegexReplacement class represents a substitution string for
// use when using regexes to search/replace, etc. It's logically
// a sequence intermixed (1) constant strings and (2) group numbers.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Peachpie.Library.RegularExpressions
{
    internal sealed class RegexReplacement
    {
        private const int ReplaceBufferSize = 256;

        // Constants for special insertion patterns
        private const int Specials = 4;
        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;

        private readonly List<string> _strings; // table of string constants
        private readonly List<int> _rules;      // negative -> group #, positive -> string #

        /// <summary>
        /// Since RegexReplacement shares the same parser as Regex,
        /// the constructor takes a RegexNode which is a concatenation
        /// of constant strings and backreferences.
        /// </summary>
        public RegexReplacement(string rep, RegexNode concat, Dictionary<int, int> _caps)
        {
            if (concat.Type() != RegexNode.Concatenate)
                throw new ArgumentException(SR.ReplacementError);

            Span<char> buffer = stackalloc char[ReplaceBufferSize];
            ValueStringBuilder vsb = new ValueStringBuilder(buffer);
            List<string> strings = new List<string>();
            List<int> rules = new List<int>();

            for (int i = 0; i < concat.ChildCount(); i++)
            {
                RegexNode child = concat.Child(i);

                switch (child.Type())
                {
                    case RegexNode.Multi:
                        vsb.Append(child._str);
                        break;

                    case RegexNode.One:
                        vsb.Append(child._ch);
                        break;

                    case RegexNode.Ref:
                        if (vsb.Length > 0)
                        {
                            rules.Add(strings.Count);
                            strings.Add(vsb.ToString());
                            vsb.Length = 0;
                        }
                        int slot = child._m;

                        if (_caps != null && slot >= 0)
                            slot = (int)_caps[slot];

                        rules.Add(-Specials - 1 - slot);
                        break;

                    default:
                        throw new ArgumentException(SR.ReplacementError);
                }
            }

            if (vsb.Length > 0)
            {
                rules.Add(strings.Count);
                strings.Add(vsb.ToString());
            }

            Pattern = rep;
            _strings = strings;
            _rules = rules;
        }

        /// <summary>
        /// Either returns a weakly cached RegexReplacement helper or creates one and caches it.
        /// </summary>
        /// <returns></returns>
        public static RegexReplacement GetOrCreate(WeakReference<RegexReplacement> replRef, string replacement, Dictionary<int, int> caps,
            int capsize, Dictionary<string, int> capnames, RegexOptions roptions)
        {
            RegexReplacement repl;

            if (!replRef.TryGetTarget(out repl) || !repl.Pattern.Equals(replacement))
            {
                repl = RegexParser.ParseReplacement(replacement, caps, capsize, capnames, roptions);
                replRef.SetTarget(repl);
            }

            return repl;
        }

        /// <summary>
        /// The original pattern string
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Given a Match, emits into the StringBuilder the evaluated
        /// substitution pattern.
        /// </summary>
        public void ReplacementImpl(ref ValueStringBuilder vsb, Match match)
        {
            for (int i = 0; i < _rules.Count; i++)
            {
                int r = _rules[i];
                if (r >= 0)   // string lookup
                    vsb.Append(_strings[r]);
                else if (r < -Specials) // group lookup
                    vsb.Append(match.GroupToStringImpl(-Specials - 1 - r));
                else
                {
                    switch (-Specials - 1 - r)
                    { // special insertion patterns
                        case LeftPortion:
                            vsb.Append(match.GetLeftSubstring());
                            break;
                        case RightPortion:
                            vsb.Append(match.GetRightSubstring());
                            break;
                        case LastGroup:
                            vsb.Append(match.LastGroupToStringImpl());
                            break;
                        case WholeString:
                            vsb.Append(match.Text);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Given a Match, emits into the ValueStringBuilder the evaluated
        /// Right-to-Left substitution pattern.
        /// </summary>
        public void ReplacementImplRTL(ref ValueStringBuilder vsb, Match match)
        {
            for (int i = _rules.Count - 1; i >= 0; i--)
            {
                int r = _rules[i];
                if (r >= 0)  // string lookup
                    vsb.AppendReversed(_strings[r].AsSpan());
                else if (r < -Specials) // group lookup
                    vsb.AppendReversed(match.GroupToStringImpl(-Specials - 1 - r));
                else
                {
                    switch (-Specials - 1 - r)
                    { // special insertion patterns
                        case LeftPortion:
                            vsb.AppendReversed(match.GetLeftSubstring());
                            break;
                        case RightPortion:
                            vsb.AppendReversed(match.GetRightSubstring());
                            break;
                        case LastGroup:
                            vsb.AppendReversed(match.LastGroupToStringImpl());
                            break;
                        case WholeString:
                            vsb.AppendReversed(match.Text.AsSpan());
                            break;
                    }
                }
            }
        }

        // Three very similar algorithms appear below: replace (pattern),
        // replace (evaluator), and split.

        /// <summary>
        /// Replaces all occurrences of the regex in the string with the
        /// replacement pattern.
        ///
        /// Note that the special case of no matches is handled on its own:
        /// with no matches, the input string is returned unchanged.
        /// The right-to-left case is split out because StringBuilder
        /// doesn't handle right-to-left string building directly very well.
        /// </summary>
        public string Replace(Regex regex, string input, int count, int startat, ref long replacements)
        {
            if (count < -1)
                throw new ArgumentOutOfRangeException(nameof(count), SR.CountTooSmall);
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            if (count == 0)
                return input;

            Match match = regex.Match(input, startat);
            if (!match.Success)
            {
                return input;
            }
            else
            {
                Span<char> charInitSpan = stackalloc char[ReplaceBufferSize];
                var vsb = new ValueStringBuilder(charInitSpan);

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    do
                    {
                        if (match.Index != prevat)
                            vsb.Append(input.AsSpan(prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;
                        ReplacementImpl(ref vsb, match);
                        ++replacements;
                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
                        vsb.Append(input.AsSpan(prevat, input.Length - prevat));
                }
                else
                {
                    // In right to left mode append all the inputs in reversed order to avoid an extra dynamic data structure
                    // and to be able to work with Spans. A final reverse of the transformed reversed input string generates
                    // the desired output. Similar to Tower of Hanoi.

                    int prevat = input.Length;

                    do
                    {
                        if (match.Index + match.Length != prevat)
                            vsb.AppendReversed(input.AsSpan(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;
                        ReplacementImplRTL(ref vsb, match);
                        ++replacements;
                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat > 0)
                        vsb.AppendReversed(input.AsSpan(0, prevat));

                    vsb.Reverse();
                }

                return vsb.ToString();
            }
        }

        /// <summary>
        /// Replaces all occurrences of the regex in the string with the
        /// replacement evaluator.
        ///
        /// Note that the special case of no matches is handled on its own:
        /// with no matches, the input string is returned unchanged.
        /// The right-to-left case is split out because StringBuilder
        /// doesn't handle right-to-left string building directly very well.
        /// </summary>
        internal static string Replace(MatchEvaluator evaluator, Regex regex,
                                       string input, int count, int startat,
                                       ref long replacements)
        {
            if (evaluator == null)
                throw new ArgumentNullException(nameof(evaluator));
            if (count < -1)
                throw new ArgumentOutOfRangeException(nameof(count), SR.CountTooSmall);
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            if (count == 0)
                return input;

            Match match = regex.Match(input, startat);

            if (!match.Success)
            {
                return input;
            }
            else
            {
                Span<char> charInitSpan = stackalloc char[ReplaceBufferSize];
                var vsb = new ValueStringBuilder(charInitSpan);

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    do
                    {
                        if (match.Index != prevat)
                            vsb.Append(input.AsSpan(prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;
                        string result = evaluator(match);
                        if (!string.IsNullOrEmpty(result))
                            vsb.Append(evaluator(match));

                        ++replacements;

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat < input.Length)
                        vsb.Append(input.AsSpan(prevat, input.Length - prevat));
                }
                else
                {
                    // In right to left mode append all the inputs in reversed order to avoid an extra dynamic data structure
                    // and to be able to work with Spans. A final reverse of the transformed reversed input string generates
                    // the desired output. Similar to Tower of Hanoi.

                    int prevat = input.Length;

                    do
                    {
                        if (match.Index + match.Length != prevat)
                            vsb.AppendReversed(input.AsSpan(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;
                        vsb.AppendReversed(evaluator(match).AsSpan());

                        ++replacements;

                        if (--count == 0)
                            break;

                        match = match.NextMatch();
                    } while (match.Success);

                    if (prevat > 0)
                        vsb.AppendReversed(input.AsSpan(0, prevat));

                    vsb.Reverse();
                }

                return vsb.ToString();
            }
        }

        /// <summary>
        /// Does a split. In the right-to-left case we reorder the
        /// array to be forwards.
        /// </summary>
        internal static string[] Split(Regex regex, string input, int count, int startat)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.CountTooSmall);
            if (startat < 0 || startat > input.Length)
                throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

            string[] result;

            if (count == 1)
            {
                result = new string[1];
                result[0] = input;
                return result;
            }

            count -= 1;

            Match match = regex.Match(input, startat);

            if (!match.Success)
            {
                result = new string[1];
                result[0] = input;
                return result;
            }
            else
            {
                List<string> al = new List<string>();

                if (!regex.RightToLeft)
                {
                    int prevat = 0;

                    for (; ;)
                    {
                        al.Add(input.Substring(prevat, match.Index - prevat));

                        prevat = match.Index + match.Length;

                        // add all matched capture groups to the list.
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.IsMatched(i))
                                al.Add(match.Groups[i].ToString());
                        }

                        if (--count == 0)
                            break;

                        match = match.NextMatch();

                        if (!match.Success)
                            break;
                    }

                    al.Add(input.Substring(prevat, input.Length - prevat));
                }
                else
                {
                    int prevat = input.Length;

                    for (; ;)
                    {
                        al.Add(input.Substring(match.Index + match.Length, prevat - match.Index - match.Length));

                        prevat = match.Index;

                        // add all matched capture groups to the list.
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.IsMatched(i))
                                al.Add(match.Groups[i].ToString());
                        }

                        if (--count == 0)
                            break;

                        match = match.NextMatch();

                        if (!match.Success)
                            break;
                    }

                    al.Add(input.Substring(0, prevat));

                    al.Reverse(0, al.Count);
                }

                return al.ToArray();
            }
        }
    }
}
