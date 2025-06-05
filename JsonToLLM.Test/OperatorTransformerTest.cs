using System;
using Newtonsoft.Json.Linq;
using Xunit;
using JsonToLLM;
using JsonToLLM.Model;

namespace JsonToLLM.Test
{
    public class OperatorTransformerTest
    {
        //[Fact]
        //public void Transform_ThrowsOnNullArguments()
        //{
        //    var trasformer = new ExpressionTrasformer();
        //    var ctx = Context.Create(new JObject(), new JObject());
        //    Assert.Throws<ArgumentNullException>(() => trasformer.Transform( new JObject(), ctx));
        //    Assert.Throws<ArgumentNullException>(() => trasformer.Transform( null, ctx));
        //    Assert.Throws<ArgumentNullException>(() => trasformer.Transform( new JObject(), null));
        //}

        //[Fact]
        //public void Transform_ReplaceSingleValue()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""result"": ""@value(foo)"" }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("bar", result["result"]?.ToString());
        //}

        //[Fact]
        //public void Transform_NotFunctionValueIsReportedInOutput()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""key"": ""@value(foo)"", ""result"": ""value"" }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("value", result["result"]?.ToString());
        //}

        //[Fact]
        //public void Transform_ReplaceMultipleValues()
        //{
        //    var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'value2' }");
        //    var template = JObject.Parse(@"{ 'result1': '@value(prop1)@value(prop2)' }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("value1value2", result["result1"]?.ToString());
        //}

        //[Fact]
        //public void Transform_UseEachOperatorToCreateArrayOfObject()
        //{
        //    var source = JObject.Parse(@"{ 'array': [ { 'prop':'value'}, { 'prop':'value1'}, { 'prop':'value2'} ]}");
        //    var template = JObject.Parse(@"{ 'result': { '@operator':'each','@path':'array','@element':{ 'field': '@value(prop)' } } } ");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new OperatorTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.NotNull(result["result"]?[0]?["field"]);
        //    Assert.Equal("value", result["result"][0]["field"]);
        //}

        //[Fact]
        //public void Transform_UseEachOperatorToCreateArrayOfString()
        //{
        //    var source = JObject.Parse(@"{ 'customers': [ { 'name':'giuliano', 'secondName':'arru'}, { 'name':'mario', 'secondName':'rossi'} ]}");
        //    var template = JObject.Parse(@"{ 'result': { '@operator':'each','@path':'customers','@element': 'Customer @value(name) @value(secondName)'  } } ");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new OperatorTrasformer();
        //    var result = trasformer.Transform(template, ctx);

        //    Assert.NotNull(result["result"]?[0]);
        //    Assert.Equal("Customer giuliano arru", result["result"][0]);
        //}

        //[Fact]
        //public void Transform_ReformatDateTime()
        //{
        //    var source = JObject.Parse(@"{ 'originalDate':'29-05-2025'}");
        //    var template = JObject.Parse(@"{ 'formatedDate': '@formatdate(@value($.originalDate),dd-MM-yyyy,dd/MM/yyyy)' }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("29/05/2025", result["formatedDate"]?.ToString());
        //}

        //[Fact]
        //public void Transform_ReplaceMultipleValuesInFreeText()
        //{
        //    var source = JObject.Parse(@"{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'} }");
        //    var template = JObject.Parse(@"{ 'result1': 'The customer @value($.name) @value($.secondName) lives in @value($.address.city)' }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("The customer giuliano arru lives in saronno", result["result1"]?.ToString());
        //}

        //[Fact]
        //public void Transform_LeavesNonFunctionStringsUnchanged()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""result"": ""noFunctionHere"" }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("noFunctionHere", result["result"]?.ToString());
        //}

        //[Fact]
        //public void Transform_MalformedFunctionInTemplateIsNotReplaced()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""result"": ""@value("" }");
        //    var ctx = Context.Create(source, new JObject());

        //    var trasformer = new ExpressionTrasformer();
        //    var result =  trasformer.Transform( template, ctx);

        //    Assert.Equal("@value(", result["result"]?.ToString());
        //}

        //[Fact]
        //public void Transform_ThrowsOnValueFunctionWithWrongParameterCount()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""result"": ""@value(foo,bar)"" }");
        //    var ctx = Context.Create(source, new JObject());

        //    var trasformer = new ExpressionTrasformer();
        //    Assert.Throws<ArgumentException>(() => trasformer.Transform( template, ctx));
        //}

        //[Fact]
        //public void Transform_ReplacesUnknownFunctionWithPlaceholder()
        //{
        //    var source = JObject.Parse(@"{ ""foo"": ""bar"" }");
        //    var template = JObject.Parse(@"{ ""result"": ""@unknown(1,2)"" }");
        //    var ctx = Context.Create(source, new JObject());

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("Function(unknown, 1, 2)", result["result"]?.ToString());
        //}

        //[Fact]
        //public void Transform_ReplaceNestedFunction()
        //{
        //    var source = JObject.Parse(@"{ 'prop1':'value1', 'prop2':'prop3.prop4', 'prop3':{ 'prop4':'prop1'} }");
        //    var template = JObject.Parse(@"{ 'result1': '@value(@value(@value(prop2)))' }");
        //    var ctx = Context.Create(source, source);

        //    var trasformer = new ExpressionTrasformer();
        //    var result = trasformer.Transform( template, ctx);

        //    Assert.Equal("value1", result["result1"]?.ToString());
        //}
    }
}
