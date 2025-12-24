using Newtonsoft.Json.Linq;
using JsonToLLM.Model;

namespace JsonToLLM.Test;

public class ExpressionTest
{
    [Fact]
    public void ValueExpression_PathExists_ReturnsValue()
    {
        // Arrange
        var json = JObject.Parse("{ 'foo': 123 }");
        var context = TemplateContext.Create(json, json);
        IExpression expr = new ValueExpression(context, "foo", "0");
            
        // Act
        var result = expr.GetValue();

        // Assert
        Assert.Equal("123", result, StringComparer.InvariantCulture);
    }

    [Fact]
    public void ValueExpression_PathDoesNotExist_ReturnsDefault()
    {
        // Arrange
        var json = JObject.Parse("{ 'foo': 123 }");
        var context = TemplateContext.Create(json, json);
        var expr = new ValueExpression(context, "bar", "default");

        // Act
        var result = expr.GetValue();

        // Assert
        Assert.Equal("default", result, StringComparer.InvariantCulture);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("   ", typeof(ArgumentException))]
    public void ValueExpression_InvalidPath_ThrowsException(string path, Type exType)
    {
        var json = JObject.Parse("{ 'foo': 123 }");
        var context = TemplateContext.Create(json, json);

        Assert.Throws(exType, () => new ValueExpression(context, path, "0"));
    }

    [Fact]
    public void ValueExpression_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ValueExpression(null!, "foo", "0"));
    }

    [Fact]
    public void FormatDateExpression_ValidInput_ConvertsDate()
    {
        // Arrange
        var json = JObject.Parse("{ 'date': '2024-05-27' }");
        var context = TemplateContext.Create(json, json);
        var valueExpr = "2024-05-27";
        var formatExpr = new FormatDateExpression(context, valueExpr, "yyyy-MM-dd", "dd/MM/yyyy");

        // Act
        var result = formatExpr.GetValue();

        // Assert
        Assert.Equal("27/05/2024", result);
    }

    [Fact]
    public void FormatDateExpression_InvalidInputFormat_ThrowsException()
    {
        // Arrange
        var json = JObject.Parse(@"{ 'date': 'not-a-date' }");
        var context = TemplateContext.Create(json, json);
        var valueExpr = "not-a-date";
        var formatExpr = new FormatDateExpression(context, valueExpr, "yyyy-MM-dd", "dd/MM/yyyy");

        // Act & Assert
        Assert.Throws<Exception>(() => formatExpr.GetValue());
    }

    [Fact]
    public void FormatDateExpression_NullContext_ThrowsArgumentNullException()
    {
        var valueExpr = "2024-05-27";
        Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(null!, valueExpr, "yyyy-MM-dd", "dd/MM/yyyy"));
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("   ", typeof(ArgumentException))]
    public void FormatDateExpression_InvalidOriginalFormat_ThrowsException(string originalFormat, Type exType)
    {
        var valueExpr = "2024-05-27";
        var json = JObject.Parse("{ }");
        var context = TemplateContext.Create(json, json);

        Assert.Throws(exType, () => new FormatDateExpression(context, valueExpr, originalFormat, "dd/MM/yyyy"));
    }

    [Fact]
    public void FormatDateExpression_NullExpression_ThrowsArgumentNullException()
    {
        var json = JObject.Parse(@"{ }");
        var context = TemplateContext.Create(json, json);

        Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(context, null!, "yyyy-MM-dd", "dd/MM/yyyy"));
    }

    // SwitchExpression unit tests
    [Fact]
    public void SwitchExpression_InputMatchesNumberMapping_ReturnsNumberValue()
    {
        // Arrange
        var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
        const string mapping = """
                               {
                                 "one": "1",
                                 "two": "second"
                               }
                               """;

        var expr = new SwitchExpression(context, "one", mapping, "default");

        // Act
        var result = expr.GetValue();

        // Assert
        Assert.Equal("1", result);
    }

    [Fact]
    public void SwitchExpression_InputMatchesStringMapping_ReturnsStringValue()
    {
        // Arrange
        var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
        const string mapping = """
                               {
                                 "one": "1",
                                 "two": "second"
                               }
                               """;
        var expr = new SwitchExpression(context, "two", mapping, "default");

        // Act
        var result = expr.GetValue();

        // Assert
        Assert.Equal("second", result);
    }

    [Fact]
    public void SwitchExpression_KeyNotFound_ReturnsDefault()
    {
        // Arrange
        var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
        const string mapping = """
                               {
                                 "one": "1"
                               }
                               """;

        var expr = new SwitchExpression(context, "missing", mapping, "my-default");

        // Act
        var result = expr.GetValue();

        // Assert
        Assert.Equal("my-default", result);
    }

    [Fact]
    public void SwitchExpression_NullInput_ThrowsArgumentNullException()
    {
        var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
        const string mapping = """
                               {
                                 "one": "1"
                               }
                               """;
        Assert.Throws<ArgumentNullException>(() => new SwitchExpression(context, null!, mapping, "default"));
    }

    [Fact]
    public void SwitchExpression_NullMapping_ThrowsArgumentNullException()
    {
        var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));

        Assert.Throws<ArgumentNullException>(() => new SwitchExpression(context, "one", null!, "default"));
    }

    [Theory]
    // Single-line conditions
    [InlineData("2 > 1", true)]
    [InlineData("\"my string\" is null", false)]
    [InlineData("string.IsNullOrWhiteSpace(\"my string\")", false)]
    [InlineData("string.IsNullOrWhiteSpace(\" \")", true)]
    // Multi-line conditions
    [InlineData("var name = \"foo\"; return string.Equals(name, \"bar\");", false)]
    [InlineData("var name = \"foo\"; return string.Equals(name, \"foo\");", true)]
    [InlineData("var number1 = 16; var number2 = 2; return number1%number2 == 0;", true)]
    public async Task IfElseExpression_ConditionEvaluated_ReturnsExpectedValue(string condition, bool conditionEvaluation)
    {
        // Arrange
        const string ifValue = "if value";
        const string elseValue = "else value";
        var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
        var expr = new IfElseExpression(context, condition, ifValue, elseValue);

        // Act
        var result = await expr.GetValueAsync();

        // Assert
        Assert.Equal(conditionEvaluation ? ifValue : elseValue, result);
    }
}