using Newtonsoft.Json.Linq;
using JsonToLLM.Model;
using Xunit;

namespace JsonToLLM.Test
{
    public class ExpressionTrasformerTest
    {
        [Fact]
        public void Evaluate_NullArguments_ThrowsArgumentNullException()
        {
            // Arrange
            var transformer = new ExpressionEngine();
            var ctx = TemplateContext.Create(new JObject(), new JObject());

            // Act & Assert
            // one of these cases was previously commented out in your workspace; keep coverage for null template and null context
            Assert.Throws<ArgumentNullException>(() => transformer.Evaluate(null, ctx));
            Assert.Throws<ArgumentNullException>(() => transformer.Evaluate(new JObject(), null));
        }

        [Fact]
        public void Evaluate_ValueFunction_SingleValueReplaced()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("@value(foo)");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("bar", result.Value<string>());
        }

        [Fact]
        public void Evaluate_NonFunctionString_ReturnsOriginalString()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("value");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new ExpressionEngine();
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("value", result.Value<string>());
        }

        [Fact]
        public void Evaluate_TemplateWithMultipleValueFunctions_ReplacesAllValues()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'value2' }");
            var value = new JValue("@value(prop1)@value(prop2)");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(value, ctx);

            // Assert
            Assert.Equal("value1value2", result.Value<string>());
        }

        [Theory]
        [InlineData("{ 'prop1':'value1', 'prop2':'value2' }", "@value(prop1)@value(prop2)", "value1value2")] // no defaults, no miss
        [InlineData("{ 'prop1':'value1', 'prop2':'value2' }", "@value(prop1,default_value1)@value(prop2,default_value2)", "value1value2")] // with default, no miss
        [InlineData("{ 'prop2':'value2' }", "@value(prop1,default_value1)@value(prop2,default_value2)", "default_value1value2")] // with default, with miss 
        [InlineData("{}", "@value(prop1,default_value1)@value(prop2,default_value2)", "default_value1default_value2")] // with default, with miss 
        [InlineData("{}", "@value(prop1)@value(prop2,default_value2)", "nulldefault_value2")] // with default, with miss 
        [InlineData("{}", "@value(prop1)@value(prop2)", "nullnull")] // no default, with miss 
        public void Evaluate_ValueFunction_WithDefaultsAndMissingValues_ReturnsExpected(string sourceStr, string templateStr, string expectedResult)
        {
            // Arrange
            var source = JObject.Parse(sourceStr);
            var template = new JValue(templateStr);
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal(expectedResult, result.Value<string>());
        }

        [Fact]
        public void Evaluate_ValueFunction_WithJsonPath_ReplacesNestedValues()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'prop1':'value1', 'object1': { 'prop2':'value2'} }");
            var template = new JValue("@value($.prop1)@value($.object1.prop2)");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("value1value2", result.Value<string>());
        }

        [Fact]
        public void Evaluate_FormatDate_ReformatsDateFromSource()
        {
            // Arrange
            var source = JObject.Parse(@"{'originalDate':'29-05-2025'}");
            var template = new JValue("@formatdate(@value($.originalDate),dd-MM-yyyy,dd/MM/yyyy)");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("29/05/2025", result.Value<string>());
        }

        [Fact]
        public void Evaluate_FormatDateAndValue_InFreeText_ReplacesAndFormatsDate()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'},  'birthDate':'02-08-1985'  }");
            var template = new JValue("The customer was born at year @formatdate(@value($.birthDate),dd-MM-yyyy,yyyy)");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("The customer was born at year 1985", result.Value<string>());
        }

        [Fact]
        public void Evaluate_ValueFunctions_InFreeText_ReplacesValues()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'} }");
            var template = new JValue("The customer @value($.name) @value($.secondName) lives in @value($.address.city)");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("The customer giuliano arru lives in saronno", result.Value<string>());
        }

        [Fact]
        public void Evaluate_NonFunctionStrings_Unchanged()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("noFunctionHere");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("noFunctionHere", result.Value<string>());
        }

        [Fact]
        public void Evaluate_MalformedFunction_NotReplaced()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("@value(");
            var ctx = TemplateContext.Create(source, new JObject());
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("@value(", result.Value<string>());
        }

        [Fact]
        public void Evaluate_ValueFunction_WrongParameterCount_ThrowsArgumentException()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("@value(foo,bar,wrongParam)");
            var ctx = TemplateContext.Create(source, new JObject());
            var transformer = new ExpressionEngine();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => transformer.Evaluate(template, ctx));
        }

        [Fact]
        public void Evaluate_UnknownFunction_ReplacedWithPlaceholder()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("@unknown(1,2)");
            var ctx = TemplateContext.Create(source, new JObject());
            var transformer = new ExpressionEngine();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => transformer.Evaluate(template, ctx));
            Assert.Contains("Unsupported function", ex.Message);
        }

        [Fact]
        public void Evaluate_NestedValueFunctions_ProducesResolvedValue()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'prop3.prop4', 'prop3':{ 'prop4':'prop1'} }");
            var template = new JValue("@value(@value(@value(prop2)))");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("value1", result.Value<string>());
        }

        [Fact]
        public void Evaluate_IfElse_WhenConditionFalse_ReturnsElseValue()
        {
            // Arrange
            var source = JObject.Parse("{ client: {name: 'john'} }");
            var template = new JValue("Welcome! @ifelse(`string.Equals(\"@value(client.name)\",\"null\")`,not-found,Client name: @value(client.name))");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("Welcome! Client name: john", result.Value<string>());
        }

        [Fact]
        public void Evaluate_IfElse_WhenConditionTrue_ReturnsIfValue()
        {
            // Arrange
            var source = JObject.Parse("{ client: {} }");
            var template = new JValue("Welcome!@ifelse(`string.Equals(\"@value(client.name)\",\"null\")`,, Client name: @value(client.name))");
            var ctx = TemplateContext.Create(source, source);
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("Welcome!", result.Value<string>());
        }

        [Fact]
        public void Evaluate_ValueFunction_NotExistentPath_ReturnDefaultValue()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("@value(not_existent_path,default)");
            var ctx = TemplateContext.Create(source, new JObject());
            var transformer = new ExpressionEngine();

            // Act
            var result = transformer.Evaluate(template, ctx);

            // Assert
            Assert.Equal("default", result.Value<string>());
        }
    }
}
