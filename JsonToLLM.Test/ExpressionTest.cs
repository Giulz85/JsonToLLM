using Newtonsoft.Json.Linq;
using JsonToLLM.Model;

namespace JsonToLLM.Test
{
    public class ExpressionTest
    {
        [Fact]
        public void ValueExpression_ReturnsValue_WhenPathExists()
        {
            // Arrange
            var json = JObject.Parse(@"{ 'foo': 123 }");
            var context = TemplateContext.Create(json, json);
            IExpression expr = new ValueExpression(context, "foo", new JValue(0));
            
            // Act
            var result = expr.GetValue();

            // Assert
            Assert.Equal(123, result.Value<int>());
        }

        [Fact]
        public void ValueExpression_ReturnsDefault_WhenPathDoesNotExist()
        {
            // Arrange
            var json = JObject.Parse(@"{ 'foo': 123 }");
            var context = TemplateContext.Create(json, json);
            var expr = new ValueExpression(context, "bar", new JValue("default"));

            // Act
            var result = expr.GetValue();

            // Assert
            Assert.Equal("default", result.Value<string>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValueExpression_ThrowsOnInvalidPath(string path)
        {
            var json = JObject.Parse(@"{ 'foo': 123 }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new ValueExpression(context, path, new JValue(0)));
        }

        [Fact]
        public void ValueExpression_ThrowsOnNullContext()
        {
            Assert.Throws<ArgumentNullException>(() => new ValueExpression(null, "foo", new JValue(0)));
        }

        [Fact]
        public void ValueExpression_ThrowsOnNullDefault()
        {
            var json = JObject.Parse(@"{ 'foo': 123 }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new ValueExpression(context, "foo", null));
        }

        [Fact]
        public void FormatDateExpression_ConvertsDateCorrectly()
        {
            // Arrange
            var json = JObject.Parse(@"{ 'date': '2024-05-27' }");
            var context = TemplateContext.Create(json, json);
            var valueExpr = "2024-05-27";
            var formatExpr = new FormatDateExpression(context, valueExpr, "yyyy-MM-dd", "dd/MM/yyyy");

            // Act
            var result = formatExpr.GetValue();

            // Assert
            Assert.Equal("27/05/2024", result.Value<string>());
        }

        [Fact]
        public void FormatDateExpression_ThrowsOnInvalidInputFormat()
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
        public void FormatDateExpression_ThrowsOnNullContext()
        {
            var valueExpr = "2024-05-27";
            Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(null, valueExpr, "yyyy-MM-dd", "dd/MM/yyyy"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FormatDateExpression_ThrowsOnInvalidOriginalFormat(string originalFormat)
        {
            var valueExpr = "2024-05-27";
            var json = JObject.Parse(@"{ }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(context, valueExpr, originalFormat, "dd/MM/yyyy"));
        }

        [Fact]
        public void FormatDateExpression_ThrowsOnNullExpression()
        {
            var json = JObject.Parse(@"{ }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(context, null, "yyyy-MM-dd", "dd/MM/yyyy"));
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
        public async Task IfElseExpression_ConditionEvaluatesToTrue_ReturnsIfValue(string condition, bool conditionEvaluation)
        {
            // Arrange
            const string ifValue = "if value";
            const string elseValue = "else value";
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
            var expr = new IfElseExpression(context, condition, ifValue, elseValue);

            // Act
            var result = await expr.GetValueAsync();

            // Assert
            Assert.Equal(conditionEvaluation ? ifValue : elseValue, result.Value<string>());
        }
    }
}