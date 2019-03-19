using System;
using Xunit;

namespace Peachpie.Library.RegularExpressions.Tests
{
    public class MatchTest
    {
        [Fact]
        public void Test1()
        {
            var regex = new Regex("/(foo)(bar)(baz)/");
            var matches = regex.Matches("foobarbaz");

            Assert.NotNull(matches);
            Assert.Single(matches);
            Assert.Equal(4, matches[0].PcreGroups.Count);
        }
    }
}
