using System;
using Xunit;
using JsonToLLM.Helpers;

namespace JsonToLLM.Test
{
    public class ExpressionHelperTest
    {
        [Theory]
        [InlineData("@func(arg1,arg2)", true, "func", "arg1,arg2", 0, 15)]
        [InlineData("  @sum(1,2)  ", true, "sum", "1,2", 2, 10)]
        [InlineData("noFunction", false, "noFunction", null, null, null)]
        [InlineData("@onlyFunc()", true, "onlyFunc", "", 0, 10)]
        [InlineData("@value($.Live1_credito.balance)", true, "value", "$.Live1_credito.balance", 0, 30)]
        [InlineData("", false, "", null, null, null)]
        public void TryParseFunctionNameAndArguments_WorksAsExpected(
            string input, bool expectedResult, string expectedName, string expectedArgs,int? expectedStartIndex,int? expectedEndIndex)
        {
            var result = ExpressionHelper.TryParseFunctionNameAndArguments(input, out var name, out var args, out var startIndex, out var endIndex);
            
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedName, name);
            Assert.Equal(expectedArgs, args);
            Assert.Equal(expectedStartIndex, startIndex);
            Assert.Equal(expectedEndIndex, endIndex);
        }

        [Theory]
        [InlineData("a,b,c", '\\', new[] { "a", "b", "c" })]
        [InlineData("a\\,b,c", '\\', new[] { "a,b", "c" })]
        [InlineData("a,(b,c),d", '\\', new[] { "a", "(b,c)", "d" })]
        [InlineData("a\\(b\\,c\\),d", '\\', new[] { "a(b,c)", "d" })]
        [InlineData("", '\\', new string[0])]
        public void SplitArguments_WorksAsExpected(string input, char escapeChar, string[] expected)
        {
            var result = ExpressionHelper.SplitArguments(input, escapeChar);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("@func(arg)", true)]
        [InlineData("   @sum(1,2)", true)]
        [InlineData("notAFunction", false)]
        [InlineData("", false)]
        public void IsFunction_WorksAsExpected(string input, bool expected)
        {
            var result = ExpressionHelper.IsFunction(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("\\@func", '\\', "@func")]
        [InlineData("  \\@sum", '\\', "  @sum")]
        [InlineData("@func", '\\', "@func")]
        [InlineData("test", '\\', "test")]
        public void UnescapeSharp_WorksAsExpected(string input, char escapeChar, string expected)
        {
            var result = ExpressionHelper.UnescapeSharp(input, escapeChar);
            Assert.Equal(expected, result);
        }
    }
}