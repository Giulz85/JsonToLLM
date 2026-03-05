using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using JsonToLLM.Model;
using Xunit;

namespace JsonToLLM.Test
{
    public class ExpressionTest
    {
        [Fact]
        public void ValueExpression_PathExists_ReturnsValue()
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
        public void ValueExpression_PathDoesNotExist_ReturnsDefault()
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
        public void ValueExpression_InvalidPath_ThrowsArgumentNullException(string path)
        {
            var json = JObject.Parse(@"{ 'foo': 123 }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new ValueExpression(context, path, new JValue(0)));
        }

        [Fact]
        public void ValueExpression_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ValueExpression(null, "foo", new JValue(0)));
        }

        [Fact]
        public void ValueExpression_NullDefault_ThrowsArgumentNullException()
        {
            var json = JObject.Parse(@"{ 'foo': 123 }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new ValueExpression(context, "foo", null));
        }

        [Fact]
        public void FormatDateExpression_ValidInput_ConvertsDate()
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
            Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(null, valueExpr, "yyyy-MM-dd", "dd/MM/yyyy"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FormatDateExpression_InvalidOriginalFormat_ThrowsArgumentNullException(string originalFormat)
        {
            var valueExpr = "2024-05-27";
            var json = JObject.Parse(@"{ }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(context, valueExpr, originalFormat, "dd/MM/yyyy"));
        }

        [Fact]
        public void FormatDateExpression_NullExpression_ThrowsArgumentNullException()
        {
            var json = JObject.Parse(@"{ }");
            var context = TemplateContext.Create(json, json);

            Assert.Throws<ArgumentNullException>(() => new FormatDateExpression(context, null, "yyyy-MM-dd", "dd/MM/yyyy"));
        }

        // SwitchExpression unit tests
        [Fact]
        public void SwitchExpression_InputMatchesNumberMapping_ReturnsNumberValue()
        {
            // Arrange
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
            var mapping = new Dictionary<string, JValue>
            {
                { "one", new JValue(1) },
                { "two", new JValue("second") }
            };

            var expr = new SwitchExpression(context, "one", mapping, "default");

            // Act
            var result = expr.GetValue();

            // Assert
            Assert.Equal(1, result.Value<int>());
        }

        [Fact]
        public void SwitchExpression_InputMatchesStringMapping_ReturnsStringValue()
        {
            // Arrange
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
            var mapping = new Dictionary<string, JValue>
            {
                { "one", new JValue(1) },
                { "two", new JValue("second") }
            };

            var expr = new SwitchExpression(context, "two", mapping, "default");

            // Act
            var result = expr.GetValue();

            // Assert
            Assert.Equal("second", result.Value<string>());
        }

        [Fact]
        public void SwitchExpression_KeyNotFound_ReturnsDefault()
        {
            // Arrange
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
            var mapping = new Dictionary<string, JValue>
            {
                { "one", new JValue(1) }
            };

            var expr = new SwitchExpression(context, "missing", mapping, "my-default");

            // Act
            var result = expr.GetValue();

            // Assert
            Assert.Equal("my-default", result.Value<string>());
        }

        [Fact]
        public void SwitchExpression_NullInput_ThrowsArgumentNullException()
        {
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
            var mapping = new Dictionary<string, JValue> { { "one", new JValue(1) } };

            Assert.Throws<ArgumentNullException>(() => new SwitchExpression(context, null!, mapping, "default"));
        }

        [Fact]
        public void SwitchExpression_NullMapping_ThrowsArgumentNullException()
        {
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));

            Assert.Throws<ArgumentNullException>(() => new SwitchExpression(context, "one", null!, "default"));
        }

        [Fact]
        public void SwitchExpression_NullDefault_ThrowsArgumentNullException()
        {
            var context = TemplateContext.Create(JObject.Parse("{}"), JObject.Parse("{}"));
            var mapping = new Dictionary<string, JValue> { { "one", new JValue(1) } };

            Assert.Throws<ArgumentNullException>(() => new SwitchExpression(context, "one", mapping, null!));
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
            Assert.Equal(conditionEvaluation ? ifValue : elseValue, result.Value<string>());
        }
    }
}