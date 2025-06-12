using System;
using Newtonsoft.Json.Linq;
using Xunit;
using JsonToLLM;
using JsonToLLM.Model;

namespace JsonToLLM.Test
{
    public class JsonToLLMTrasformerTest
    {

        // Arrange
        private IExpressionEngine _expressionTrasformer = new ExpressionEngine();
        private IOperatorTrasformer _operatorTrasformer = new OperatorTrasformer();

        [Fact]
        public void Transform_SigleFunction_ResolvesValue()
        {
           
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = JObject.Parse(@"{ ""result"": ""@value(foo)"" }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.Equal("bar", result["result"]);
        }


        [Fact]
        public void Transform_MutipleFunctions_ResolvesValues()
        {
            // Arrange
           
            var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'value2' }");
            var template = JObject.Parse(@"{ 'result': '@value(prop1)@value(prop2)' }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.Equal("value1value2", result["result"]);
        }

        [Fact]
        public void Transform_MutipleNestledFunctions_ReplacesMultipleValuesFromNestledObject()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'prop1':'value1', 'object1': { 'prop2':'value2'} }");
            var template = JObject.Parse(@"{ 'result': '@value($.prop1)@value($.object1.prop2)' }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.Equal("value1value2", result["result"]?.ToString());
        }


        [Fact]
        public void Transform_FieldFormatDateExpression_ReformatsDateTime()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'originalDate':'29-05-2025'}");
            var template = JObject.Parse(@"{ 'formatedDate': '@formatdate(@value($.originalDate),dd-MM-yyyy,dd/MM/yyyy)' }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.Equal("29/05/2025", result["formatedDate"]?.ToString());
        }

        [Fact]
        public void Transform_FieldWithFreeTextAndExpressions_ReplacesMultipleValuesInFreeText()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'} }");
            var template = JObject.Parse(@"{ 'result1': 'The customer @value($.name) @value($.secondName) lives in @value($.address.city)' }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.Equal("The customer giuliano arru lives in saronno", result["result1"]?.ToString());
        }

        [Fact]
        public void Transform_FieldWithNoExpression_LeavesNonFunctionStringsUnchanged()
        {
            // Arrange
            var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
            var template = JObject.Parse(@"{ ""result"": ""noFunctionHere"" }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.Equal("noFunctionHere", result["result"]?.ToString());
        }


        [Fact]
        public void Transform_UseEachOperator_CreateArrayOfObject()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'array': [ { 'prop':'value'}, { 'prop':'value1'}, { 'prop':'value2'} ]}");
            var template = JObject.Parse(@"{ 'result': { '@operator':'each','@path':'array','@element':{ 'field': '@value(prop)' } } } ");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.NotNull(result["result"]?[0]?["field"]);
            Assert.Equal("value", result["result"][0]["field"]);
        }

        [Fact]
        public void Transform_UseEachOperator_CreateArrayWhereElementsAreString()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'customers': [ { 'name':'giuliano', 'secondName':'arru'}, { 'name':'mario', 'secondName':'rossi'} ]}");
            var template = JObject.Parse(@"{ 'result': { '@operator':'each','@path':'customers','@element': 'Customer @value(name) @value(secondName)'  } } ");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.NotNull(result["result"]?[0]);
            Assert.Equal("Customer giuliano arru", result["result"][0]);
        }

        [Fact]
        public void Transform_NestledEachOperators_CreateNestledArray()
        {
            // Arrange
            var source = JObject.Parse(@"{ 'customers': [ { 'name':'mario', 'secondName':'rossi', 'counters':[{'amount':3, 'unit':'euro', 'date': '29-05-2025'  },{'amount':4, 'unit':'dollar','date': '29-05-2025' }] } ]}");
            var template = JObject.Parse(@"{ 'result': { 
                                                                    '@operator':'each',
                                                                    '@path':'customers',
                                                                    '@element': { 
                                                                        'customer':'@value(name) @value(secondName)',
                                                                        'counters': { 
                                                                            '@operator':'each', 
                                                                            '@path':'counters', 
                                                                            '@element': 'Speso @value(amount) @value(unit) in data @formatdate(@value(date),dd-MM-yyyy,dd/MM/yyyy)'
                                                                        } 
                                                                    }
                                                        }
                                                    }");
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx);

            // Assert
            Assert.NotNull(result["result"]?[0]);
            Assert.Equal("mario rossi", result["result"][0]["customer"]);
            Assert.Equal("Speso 3 euro in data 29/05/2025", result["result"][0]["counters"][0]);
        }

        //[Fact]
        //public void Transform_MalformedFunctionInTemplateIsNotReplaced()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""result"": ""@value("" }");
        //    var ctx = Context.Create(source, new JObject());

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform(template, ctx);

        //    Assert.Equal("@value(", result["result"]?.ToString());
        //}




        //[Fact]
        //public void Transform_NestedFunction_ResolvesAllLevels()
        //{
        //    // Arrange
        //    var transformer = new JsonToLLMTrasformer();
        //    var obj = JObject.Parse(@"{ ""result"": ""=value('=value(\\'foo\\')')"" }");
        //    var context = new Context(new JObject { ["foo"] = "bar" }, new JObject());

        //    // Act
        //    var result = transformer.Transform(obj, context);

        //    // Assert
        //    Assert.Equal("bar", result["result"]);
        //}

        //[Fact]
        //public void Transform_UnsupportedFunction_ThrowsArgumentException()
        //{
        //    // Arrange
        //    var transformer = new JsonToLLMTrasformer();
        //    var obj = JObject.Parse(@"{ ""value"": ""=unknown('foo')"" }");
        //    var context = new Context(new JObject { ["foo"] = "bar" }, new JObject());

        //    // Act & Assert
        //    Assert.Throws<ArgumentException>(() => transformer.Transform(obj, context));
        //}

        //[Fact]
        //public void Transform_FormatDateFunction_ResolvesCorrectly()
        //{
        //    // Arrange
        //    var transformer = new JsonToLLMTrasformer();
        //    var obj = JObject.Parse(@"{ ""date"": ""=formatdate('2024-06-01','yyyy-MM-dd','dd/MM/yyyy')"" }");
        //    var context = new Context(new JObject(), new JObject());

        //    // Act
        //    var result = transformer.Transform(obj, context);

        //    // Assert
        //    Assert.Equal("01/06/2024", result["date"]);
        //}


    }
}
