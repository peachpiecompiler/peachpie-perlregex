using System;
using Xunit;

namespace Peachpie.Library.RegularExpressions.Tests
{
    public class PcreTests
    {
        static Match match(string pattern, string subject, int startat = 0)
        {
            return new Regex(pattern).Match(subject, startat);
        }

        static string replace(string pattern, string replacement, string subject)
        {
            return new Regex(pattern).Replace(subject, replacement);
        }

        [Fact]
        public void Test1()
        {
            var regex = new Regex("/(foo)(bar)(baz)/");
            var matches = regex.Matches("foobarbaz");

            Assert.NotNull(matches);
            Assert.Single(matches);
            Assert.Equal(4, matches[0].PcreGroups.Count);
        }

        [Fact]
        public void Test2()
        {
            Assert.True(match("/\\q\\_\\y/", "q_y").Success);

            match(@"/^[\x{9}\x{A}\x{D}\x{20}-\x{7E}\x{A0}-\x{D7FF}\x{E000}-\x{FFFD}\x{10000}-\x{10FFFF}]*$/Du", "");
        }

        [Fact]
        public void TestSubRoutines()
        {
            match(@"/([abc])(?1)(?1)/", "abcd");
            match(@"/([abc](d))(?1)(?1)/", "adcdbd");
            match(@"/([abc](d))(?:[abc](?:d))(?:[abc](?:d))/", "adcdbd");

            match(@"/^(((?=.*(::))(?!.*\3.+\3))\3?|([\dA-F]{1,4}(\3|:\b|$)|\2))(?4){5}((?4){2}|(((2[0-4]|1\d|[1-9])?\d|25[0-5])\.?\b){4})$/i", "2001:0db8:85a3:0000:0000:8a2e:0370:7334");
            match(@"/\A(\((?>[^()]|(?1))*\))\z/", "(((lorem)ipsum()))");
            match(@"/\b(([a-z])(?1)(?2)|[a-z])\b/", "racecar");
            match(@"/\b(([a-z])(?1)(?2)|[a-z])\b/", "none");
            match(@"/^Name:\ (.*) Born:\ ((?:3[01]|[12][0-9]|[1-9])-(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)-(?:19|20)[0-9][0-9]) Admitted:\ (?2) Released:\ (?2)$/",
              "Name: John Doe Born: 17-Jan-1964 Admitted: 30-Jul-2013 Released: 3-Aug-2013");
        }

        [Fact]
        public void TestReplace()
        {
            // In replacement: "\\" -> "\"
            Assert.Equal(@"\'", replace(@"/(')/", @"\\$1", "'"));
            Assert.Equal(@"\'", replace(@"/([\\'])/", @"\\$1", "'"));
            Assert.Equal(@"aaa\'bbb\'aa\\a", replace(@"/([\\'])/", @"\\$1", @"aaa'bbb'aa\a"));
            Assert.Equal(@"aaa\'/\'/bbb\'/\'/aa\\/\\/a", replace(@"/([\\'])/", @"\\$1/\\$1/", @"aaa'bbb'aa\a"));
        }

        [Fact]
        public void TestCharClass()
        {
            // \pL
            Assert.Equal("....", replace(@"/[\pL\d]+/u", "", "..Letters0123.."));
        }

        [Fact]
        public void TestAnchoredFlag()
        {
            Assert.True(match("/fo/A", "foo").Success);
            Assert.True(match("/fo/A", "barfoo", 3).Success);
            Assert.False(match("/fo/A", "barfoo").Success);
            Assert.False(match("/fo|ar/A", "barfoo").Success);
            Assert.False(match(@"/%\}/A", @"{% foo bar %}", 3).Success);
            Assert.True(match(@"/%\}/A", @"{% foo bar %}", 11).Success);
        }

        [Fact]
        public void TestDollarEndOnly()
        {
            Assert.True(match("/a$/D", "a").Success);
            Assert.False(match("/a$/D", "a\n").Success);
            Assert.True(match("/a$/mD", "a\n").Success);    // 'm' has precedence
            Assert.True(match("/a$/mD", "a\nb").Success);
        }

        [Fact]
        public void TestExtra()
        {
            Assert.True(match(@"/\j/", "j").Success);
            Assert.Throws<RegexParseException>(() => match(@"/\j/X", "j"));
            Assert.Throws<RegexParseException>(() => match(@"/[\j]+/X", "j"));
        }
    }
}