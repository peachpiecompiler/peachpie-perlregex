using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Peachpie.Library.RegularExpressions
{
    ref partial struct RegexParser
    {
        [Flags]
        private enum NewlineTypes
        {
            Cr = 0x1,
            Lf = 0x2,
            CrLf = 0x4,
            Unicode = 0x8,
            DocumentStart = 0x10,
            DocumentEnd = 0x20,
            AnyCrLf = Cr | Lf | CrLf,
            AnySequence = AnyCrLf | Unicode
        }

        private RegexNode CreatePseudoDotNode()
        {
            // Create a negative character match for everything other than new line characters, e.g.:
            // [^\r\n]

            var charClass = GetNewlineCharClass(GetNewlineTypes(), _options);
            charClass.Negate = true;

            return new RegexNode(RegexNode.Set, _options, charClass.ToStringClass());
        }

        private RegexNode CreatePseudoCircumflexNode()
        {
            // Create a lookbehind, e.g.: (?<=(?>\A|\r\n|[\r\n]))

            var options = _options | RegexOptions.RightToLeft;

            var newlineTypes = GetNewlineTypes() | NewlineTypes.DocumentStart;
            var lineCheckNode = CreateNewLineParseNode(newlineTypes, options);

            var lookaheadNode = new RegexNode(RegexNode.Require, options);
            lookaheadNode.AddChild(lineCheckNode);
            return lookaheadNode;
        }

        private RegexNode CreatePseudoDollarNode()
        {
            if (!UseOptionM())
            {
                Debug.Assert(!UseOptionDollarEndOnly());    // Could have been handled by a simple \z
                return CreatePseudoEndZNode();
            }

            // Create a lookahead, e.g.: (?=(?>\z|\r\n|[\r\n]))

            var options = _options & ~RegexOptions.RightToLeft;

            var newlineTypes = GetNewlineTypes() | NewlineTypes.DocumentEnd;
            var lineCheckNode = CreateNewLineParseNode(newlineTypes, options);

            var lookaheadNode = new RegexNode(RegexNode.Require, options);
            lookaheadNode.AddChild(lineCheckNode);
            return lookaheadNode;
        }

        private RegexNode CreatePseudoEndZNode()
        {
            // Create a lookahead, e.g.: (?=(?>\r\n|[\r\n])?\z)
            
            var options = _options & ~RegexOptions.RightToLeft;

            var lineCheckNode = CreateNewLineParseNode(GetNewlineTypes(), options);

            var maybeNode = new RegexNode(RegexNode.Loop, options, 0, 1);
            maybeNode.AddChild(lineCheckNode);

            var concatNode = new RegexNode(RegexNode.Concatenate, options);
            concatNode.AddChild(maybeNode);
            concatNode.AddChild(new RegexNode(RegexNode.End, options));

            var lookaheadNode = new RegexNode(RegexNode.Require, options);
            lookaheadNode.AddChild(concatNode);
            return lookaheadNode;
        }

        private NewlineTypes GetNewlineTypes()
        {
            return _options.GetNewlineConvention() switch
            {
                RegexOptions.PCRE_NEWLINE_CR => NewlineTypes.Cr,
                RegexOptions.PCRE_NEWLINE_LF => NewlineTypes.Lf,
                RegexOptions.PCRE_NEWLINE_CRLF => NewlineTypes.CrLf,
                RegexOptions.PCRE_NEWLINE_ANY => NewlineTypes.AnySequence,
                RegexOptions.PCRE_NEWLINE_ANYCRLF => NewlineTypes.AnyCrLf,
                _ => NewlineTypes.Lf,
            };
        }

        /// <summary>
        /// Creates a <see cref="RegexNode"/> which processes new lines according to their specified types
        /// and current settings, see:
        /// https://www.pcre.org/original/doc/html/pcrepattern.html#SEC27
        /// </summary>
        private RegexNode CreateNewLineParseNode(NewlineTypes newlines, RegexOptions options)
        {
            Debug.Assert(newlines != 0);

            var altNodes = new List<RegexNode>();

            if ((newlines & NewlineTypes.DocumentStart) != 0)
            {
                // \A
                altNodes.Add(new RegexNode(RegexNode.Start, options));
            }

            if ((newlines & NewlineTypes.DocumentEnd) != 0)
            {
                // \z
                altNodes.Add(new RegexNode(RegexNode.End, options));
            }

            if ((newlines & NewlineTypes.CrLf) != 0)
            {
                // \r\n
                altNodes.Add(new RegexNode(RegexNode.Multi, options, "\r\n"));
            }

            if ((newlines & (NewlineTypes.Cr | NewlineTypes.Lf | NewlineTypes.Unicode)) != 0)
            {
                // [\r\n\x000B...]
                RegexCharClass charClass = GetNewlineCharClass(newlines, options);
                altNodes.Add(new RegexNode(RegexNode.Set, options, charClass.ToStringClass()));
            }

            if (altNodes.Count == 1)
            {
                return altNodes[0];
            }
            else
            {
                // Use greedy (non-capturing) group to capture either \r\n, \A, \z or any of the single characters, e.g.:
                // (?>\r\n|\z|[\n\r...])
                var altNode = new RegexNode(RegexNode.Alternate, options);
                foreach (var node in altNodes)
                {
                    altNode.AddChild(node);
                }

                return altNode.MakePossessive();
            }
        }

        private static RegexCharClass GetNewlineCharClass(NewlineTypes newlines, RegexOptions options)
        {
            var charClass = new RegexCharClass();

            if ((newlines & NewlineTypes.Cr) != 0)
            {
                charClass.AddChar('\r');
            }

            if ((newlines & NewlineTypes.Lf) != 0)
            {
                charClass.AddChar('\n');
            }

            if ((newlines & NewlineTypes.Unicode) != 0)
            {
                charClass.AddChar('\x000B');    // Vertical tab
                charClass.AddChar('\x000C');    // Form feed
                charClass.AddChar('\x0085');    // Next line

                if ((options & RegexOptions.PCRE_UTF8) != 0)
                {
                    // Those 3-byte UTF-8 characters are not matched in PCRE if the UTF-8 switch is off
                    charClass.AddChar('\x2028');    // Line separator
                    charClass.AddChar('\x2029');    // Paragraph separator
                }
            }

            return charClass;
        }
    }
}