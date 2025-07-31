using JsonToLLM.Westwind;

namespace JsonToLLM.Test;

public class ExpressionParserTests
{
    [Fact]
    public void ParsesSimpleInlineExpression()
    {
        const string tpl = "Hello @(Name)";
        var matches = ExpressionParser.Parse(tpl);
        Assert.Single(matches);

        var m = matches[0];
        Assert.False(m.IsBlock);
        Assert.Equal("Name", m.Code);
        Assert.Equal(6, m.StartIndex);
        Assert.Equal(6 + m.Code.Length + 3, m.EndIndex);
    }

    [Fact]
    public void ParsesSimpleBlockExpression()
    {
        const string tpl = "Start @{ var x = 1; } End";
        var matches = ExpressionParser.Parse(tpl);
        Assert.Single(matches);

        var m = matches[0];
        Assert.True(m.IsBlock);
        Assert.Contains("var x = 1;", m.Code);
    }

    [Fact]
    public void ParsesNestedParenthesesInInline()
    {
        const string tpl = "Value @(Func(\"test(1)\")) done.";
        var matches = ExpressionParser.Parse(tpl);
        Assert.Single(matches);

        var m = matches[0];
        Assert.False(m.IsBlock);
        Assert.Equal("Func(\"test(1)\")", m.Code.Trim());
    }

    [Fact]
    public void ParsesNestedBracesAndVarsInBlock()
    {
        const string tpl = "@{ var json = \"{ \"key\": 123 }\"; } tail";
        var matches = ExpressionParser.Parse(tpl);
        Assert.Single(matches);

        var m = matches[0];
        Assert.True(m.IsBlock);
        Assert.Contains("{ \"key\": 123 }", m.Code);
    }

    [Fact]
    public void ParsesMultipleExpressionsInMixedOrder()
    {
        const string tpl = """
                           Hello @(A) world 
                           @{ Console.WriteLine("B"); } 
                           
                           Goodbye @(C)
                           """;
        var matches = ExpressionParser.Parse(tpl);
        Assert.Equal(3, matches.Count);

        Assert.Equal("A", matches[0].Code.Trim());
        Assert.Equal("Console.WriteLine(\"B\");", matches[1].Code.Trim());
        Assert.Equal("C", matches[2].Code.Trim());
    }

    [Fact]
    public void ThrowsWhenInlineMissingClosingParen()
    {
        const string tpl = "Missing @(Unfinished";
        var ex = Assert.Throws<InvalidOperationException>(() => ExpressionParser.Parse(tpl));
        Assert.Contains("Unterminated expression", ex.Message);
    }

    [Fact]
    public void ThrowsWhenBlockMissingClosingBrace()
    {
        const string tpl = "Missing @{ var x = 1;";
        var ex = Assert.Throws<InvalidOperationException>(() => ExpressionParser.Parse(tpl));
        Assert.Contains("Unterminated expression", ex.Message);
    }
}

