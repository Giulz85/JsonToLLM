using JsonToLLM.Helpers;

namespace JsonToLLM.Test;

public class ExpressionHelperTest
{
    public static IEnumerable<object?[]> TryParseFunctionNameAndArgumentsData =>
        new List<object?[]>
        {
            new object?[] { ExpressionParserVersion.Version1, "@func(arg1,arg2)", true, "func", new[] { "arg1", "arg2" }, 0, null },
            new object?[] { ExpressionParserVersion.Version1, "@sum(1,2)", true, "sum", new[] { "1", "2" }, 0, null },
            new object?[] { ExpressionParserVersion.Version1, "noFunction", false, null, null, null, null },
            new object?[] { ExpressionParserVersion.Version1, "@onlyFunc()", true, "onlyFunc", Array.Empty<string>(), 0, null },
            new object?[] { ExpressionParserVersion.Version1, "@func1(arg1,@func2(1,arg2))", true, "func2", new[]{"1", "arg2"}, 12, 25 },
            new object?[] { ExpressionParserVersion.Version1, "@value($.Live1_credito.balance)", true, "value", new[] { "$.Live1_credito.balance" }, 0, null },
            new object?[] { ExpressionParserVersion.Version1, "", false, null, null, null, null },
            new object?[] { ExpressionParserVersion.Version2, "@func(\"arg1\", \"arg2\")", true, "func", new[] { "arg1", "arg2" }, 0, 20 },
            new object?[] { ExpressionParserVersion.Version2, "@sum(1, 2)", true, "sum", new[] { "1", "2" }, 0, 9 },
            new object?[] { ExpressionParserVersion.Version2, "noFunction", false, null, null, null, null },
            new object?[] { ExpressionParserVersion.Version2, "@onlyFunc()", true, "onlyFunc", null, 0, 10 },
            new object?[] { ExpressionParserVersion.Version2, "@func1(\"arg1\", @func2(1, \"arg2\"))", true, "func2", new[]{"1", "arg2"}, 15, 31 },
            new object?[] { ExpressionParserVersion.Version2, "@value(\"$.Live1_credito.balance\")", true, "value", new[] { "$.Live1_credito.balance" }, 0, 32 },
            new object?[] { ExpressionParserVersion.Version2, "", false, null, null, null, null }
        };

    [Theory]
    [MemberData(nameof(TryParseFunctionNameAndArgumentsData))]
    public void TryParseFunctionNameAndArguments_WorksAsExpected(ExpressionParserVersion parserVersion,
        string input, bool expectedResult, string expectedName, string?[]? expectedArgs, int? expectedStartIndex, int? expectedEndIndex)
    {
        var result = ExpressionHelper.TryParseFunctionNameAndArguments(input, out var name, out var args, out var startIndex, out var endIndex, parserVersion);

        if(expectedResult) expectedEndIndex ??= input.Length - 1;
        
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedName, name);
        Assert.Equal(expectedArgs, args);
        Assert.Equal(expectedStartIndex, startIndex);
        Assert.Equal(expectedEndIndex, endIndex);
    }
}
