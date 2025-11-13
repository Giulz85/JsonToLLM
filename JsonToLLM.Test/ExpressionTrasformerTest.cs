using System;
using Newtonsoft.Json.Linq;
using Xunit;
using JsonToLLM;
using JsonToLLM.Model;

namespace JsonToLLM.Test
{
    public class ExpressionTrasformerTest
    {
        [Fact]
        public void Transform_ThrowsOnNullArguments()
        {
            var trasformer = new ExpressionEngine();
            var ctx = TemplateContext.Create(new JObject(), new JObject());
            Assert.Throws<ArgumentNullException>(() => trasformer.Evaluate( new JObject(), ctx));
            Assert.Throws<ArgumentNullException>(() => trasformer.Evaluate( null, ctx));
            Assert.Throws<ArgumentNullException>(() => trasformer.Evaluate( new JObject(), null));
        }

        [Fact]
        public void Transform_ReplaceSingleValue()
        {
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("@value(foo)");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template,  ctx);

            Assert.Equal("bar", result.Value<string>());
        }

        [Fact]
        public void Transform_NotFunctionValueIsReportedInOutput()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = new JValue("value");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            // Assert
            Assert.Equal("value", result.Value<string>());
        }

        [Fact]
        public void Transform_ReplaceMultipleValues()
        {
            var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'value2' }");
            var value = new JValue("@value(prop1)@value(prop2)");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( value, ctx);

            Assert.Equal("value1value2", result.Value<string>());
        }

        [Theory]
        [InlineData("{ 'prop1':'value1', 'prop2':'value2' }", "@value(prop1)@value(prop2)", "value1value2")] // no defaults, no miss
        [InlineData("{ 'prop1':'value1', 'prop2':'value2' }", "@value(prop1,default_value1)@value(prop2,default_value2)", "value1value2")] // with default, no miss
        [InlineData("{ 'prop2':'value2' }", "@value(prop1,default_value1)@value(prop2,default_value2)", "default_value1value2")] // with default, with miss 
        [InlineData("{}", "@value(prop1,default_value1)@value(prop2,default_value2)", "default_value1default_value2")] // with default, with miss 
        [InlineData("{}", "@value(prop1)@value(prop2,default_value2)", "nulldefault_value2")] // with default, with miss 
        [InlineData("{}", "@value(prop1)@value(prop2)", "nullnull")] // no default, with miss 
        public void Transform_ReplaceMultipleValuesWithDefaultValuesProvided(string sourceStr, string templateStr, string expectedResult)
        {
            var source = JObject.Parse(sourceStr);
            var template = new JValue(templateStr);
            var ctx = TemplateContext.Create(source, source);

            var transformer = new ExpressionEngine();
            var result = transformer.Evaluate(template, ctx);

            Assert.Equal(expectedResult, result.Value<string>());
        }

        [Fact]
        public void Transform_ReplaceMultipleValuesFromNestledObject()
        {
            var source = JObject.Parse(@"{ 'prop1':'value1', 'object1': { 'prop2':'value2'} }");
            var template = JObject.Parse(@"{ 'result1': '@value($.prop1)@value($.object1.prop2)' }");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            Assert.Equal("value1value2", result.Value<string>());
        }

        [Fact]
        public void Transform_ReformatDateTime()
        {
            var source = JObject.Parse(@"{ 'originalDate':'29-05-2025'}");
            var template = JObject.Parse(@"{ 'formatedDate': '@formatdate(@value($.originalDate),dd-MM-yyyy,dd/MM/yyyy)' }");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            Assert.Equal("29/05/2025", result.Value<string>());
        }

        [Fact]
        public void Transform_ReplaceMultipleValuesInFreeText()
        {
            var source = JObject.Parse(@"{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'} }");
            var template = JObject.Parse(@"{ 'result1': 'The customer @value($.name) @value($.secondName) lives in @value($.address.city)' }");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            Assert.Equal("The customer giuliano arru lives in saronno", result.Value<string>());
        }

        [Fact]
        public void Transform_LeavesNonFunctionStringsUnchanged()
        {
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = JObject.Parse(@"{ ""result"": ""noFunctionHere"" }");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            Assert.Equal("noFunctionHere", result.Value<string>());
        }

        [Fact]
        public void Transform_MalformedFunctionInTemplateIsNotReplaced()
        {
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = JObject.Parse(@"{ ""result"": ""@value("" }");
            var ctx = TemplateContext.Create(source, new JObject());

            var trasformer = new ExpressionEngine();
            var result =  trasformer.Evaluate( template, ctx);

            Assert.Equal("@value(", result.Value<string>());
        }

        [Fact]
        public void Transform_ThrowsOnValueFunctionWithWrongParameterCount()
        {
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = JObject.Parse(@"{ ""result"": ""@value(foo,bar)"" }");
            var ctx = TemplateContext.Create(source, new JObject());

            var trasformer = new ExpressionEngine();
            Assert.Throws<ArgumentException>(() => trasformer.Evaluate( template, ctx));
        }

        [Fact]
        public void Transform_ReplacesUnknownFunctionWithPlaceholder()
        {
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = JObject.Parse(@"{ ""result"": ""@unknown(1,2)"" }");
            var ctx = TemplateContext.Create(source, new JObject());

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            Assert.Equal("Function(unknown, 1, 2)", result.Value<string>());
        }

        [Fact]
        public void Transform_ReplaceNestedFunction()
        {
            var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'prop3.prop4', 'prop3':{ 'prop4':'prop1'} }");
            var template = JObject.Parse(@"{ 'result1': '@value(@value(@value(prop2)))' }");
            var ctx = TemplateContext.Create(source, source);

            var trasformer = new ExpressionEngine();
            var result = trasformer.Evaluate( template, ctx);

            Assert.Equal("value1", result.Value<string>());
        }
    }
}
