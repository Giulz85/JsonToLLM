using JsonToLLM.Helpers;

namespace JsonToLLM.Test;

public class ExpressionHelperTest
{
    public static IEnumerable<object?[]> TryParseFunctionNameAndArgumentsData =>
        new List<object?[]>
        {
            new object?[] { "@func(\"arg1\", \"arg2\")", true, "func", new[] { "arg1", "arg2" }, 0, 20 },
            new object?[] { "@sum(1, 2)", true, "sum", new[] { "1", "2" }, 0, 9 },
            new object?[] { "noFunction", false, null, null, null, null },
            new object?[] { "@onlyFunc()", true, "onlyFunc", null, 0, 10 },
            new object?[] { "@func1(\"arg1\", @func2(1, \"arg2\"))", true, "func2", new[]{"1", "arg2"}, 15, 31 },
            new object?[] { "@value(\"$.Live1_credito.balance\")", true, "value", new[] { "$.Live1_credito.balance" }, 0, 32 },
            new object?[] { "", false, null, null, null, null }
        };

    [Theory]
    [MemberData(nameof(TryParseFunctionNameAndArgumentsData))]
    public void TryParseFunctionNameAndArguments_WorksAsExpected(
        string input, bool expectedResult, string expectedName, string?[]? expectedArgs, int? expectedStartIndex, int? expectedEndIndex)
    {
        var result = ExpressionHelper.TryParseFunctionNameAndArguments(input, out var name, out var args, out var startIndex, out var endIndex);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedArgs, args);
        Assert.Equal(expectedStartIndex, startIndex);
        Assert.Equal(expectedEndIndex, endIndex);
    }

    [Theory]
    [InlineData("'a', 'b', 'c'", '\\', new[] { "a", "b", "c" })]
    [InlineData("\"a\", \"b\",3", '\\', new[] { "a", "b", "3" })]
    [InlineData("\"a\", 'literal \"string\"', 3", '\\', new[] { "a", "literal \"string\"", "3" })]
    [InlineData(" ", '\\', null)]
    public void SplitArguments_WorksAsExpected(string input, char escapeChar, string[]? expected)
    {
        var result = ExpressionHelper.SplitArguments(input, escapeChar);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("@func(arg)", true)]
    [InlineData("   @sum(1,2)", false)]
    [InlineData("notAFunction", false)]
    [InlineData("", false)]
    public void IsExactFunctionCall_WorksAsExpected(string input, bool expected)
    {
        var result = ExpressionHelper.IsExactFunctionCall(input);
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