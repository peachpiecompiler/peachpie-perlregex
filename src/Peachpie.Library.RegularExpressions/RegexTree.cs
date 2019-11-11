// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// RegexTree is just a wrapper for a node tree with some
// global information attached.

using System.Collections.Generic;

namespace Peachpie.Library.RegularExpressions
{
    internal sealed class RegexTree
    {
        public readonly RegexNode Root;
        public readonly Dictionary<int, int> Caps;
        public readonly int[] CapNumList;
        public readonly int CapTop;
        public readonly Dictionary<string, int> CapNames;
        public readonly string[] CapsList;
        public readonly RegexOptions Options;

        internal RegexTree(RegexNode root, Dictionary<int, int> caps, int[] capNumList, int capTop, Dictionary<string, int> capNames, string[] capsList, RegexOptions options)
        {
            Root = root;
            Caps = caps;
            CapNumList = capNumList;
            CapTop = capTop;
            CapNames = capNames;
            CapsList = capsList;
            Options = options;
        }

#if DEBUG
        public void Dump()
        {
            Root.Dump();
        }

        public bool Debug
        {
            get
            {
                return (Options & RegexOptions.Debug) != 0;
            }
        }
#endif
    }
}
