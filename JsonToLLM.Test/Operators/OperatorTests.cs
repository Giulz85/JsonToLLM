using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using JsonToLLM.Operators;
using JsonToLLM.Model;

namespace JsonToLLM.Test.Operators
{
    public class OperatorTests
    {
        [Fact]
        public void EachOperator_Evaluate_PathDoesNotExist_ReturnsEmptyArray()
        {
            var source = JObject.Parse(@"{ 'array' : [] }");
            var ctx = TemplateContext.Create(source, source);

            var each = new EachOperator("missingPath", null, JValue.CreateNull());
            var result = each.Evaluate(ctx);

            Assert.IsType<JArray>(result.Json);
            Assert.Equal(0, ((JArray)result.Json).Count);
        }

        [Fact]
        public void EachOperator_Evaluate_PathExists_ReturnsArrayWithSameCount()
        {
            var source = JObject.Parse(@"{ 'array' : [ { 'a':1 }, { 'a':2 }, { 'a':3 } ] }");
            var ctx = TemplateContext.Create(source, source);

            var each = new EachOperator("array", null, new JObject());
            var result = each.Evaluate(ctx);

            Assert.IsType<JArray>(result.Json);
            Assert.Equal(3, ((JArray)result.Json).Count);
        }

        [Fact]
        public void SumOperator_Evaluate_ArrayMissing_ReturnsZero()
        {
            var source = JObject.Parse(@"{ }");
            var ctx = TemplateContext.Create(source, source);

            var sum = new SumOperator("array", "value");
            var result = sum.Evaluate(ctx);

            Assert.Equal(0d, result.Json.ToObject<double>());
        }

        [Fact]
        public void SumOperator_Evaluate_MixedNumericTypes_ReturnsSumAsDouble()
        {
            var source = JObject.Parse(@"{ 'array' : [ { 'v': 1 }, { 'v': 2.5 } ] }");
            var ctx = TemplateContext.Create(source, source);

            var sum = new SumOperator("array", "v");
            var result = sum.Evaluate(ctx);

            Assert.Equal(3.5d, result.Json.ToObject<double>());
        }

        [Fact]
        public void SumOperator_Evaluate_UnsupportedTokenType_ThrowsInvalidOperationException()
        {
            var source = JObject.Parse(@"{ 'array' : [ { 'v': 'not-numeric' } ] }");
            var ctx = TemplateContext.Create(source, source);

            var sum = new SumOperator("array", "v");

            Assert.Throws<InvalidOperationException>(() => sum.Evaluate(ctx));
        }

        [Fact]
        public void FloatOperator_Evaluate_StringParsable_ReturnsFloatValue()
        {
            var source = JObject.Parse(@"{ 'f': '1.5' }");
            var ctx = TemplateContext.Create(source, source);

            var op = new FloatOperator("f", 0f);
            var result = op.Evaluate(ctx);

            Assert.Equal(1.5f, result.Json.ToObject<float>());
        }

        [Fact]
        public void FloatOperator_Evaluate_NonStringOrUnparsable_ReturnsDefault()
        {
            var source = JObject.Parse(@"{ 'f': 123 }"); // not a string
            var ctx = TemplateContext.Create(source, source);

            var op = new FloatOperator("f", 5.25f);
            var result = op.Evaluate(ctx);

            Assert.Equal(5.25f, result.Json.ToObject<float?>());
        }

        [Fact]
        public void ObjectPatchOperator_Evaluate_AddIfNull_AddsMissingKeys()
        {
            var source = JObject.Parse(@"{ 'existing': 'old' }");
            var ctx = TemplateContext.Create(source, source);

            var json = @"{
                ""@addIfNull"": { ""newKey"": ""newVal"" }
            }";
            var op = JsonConvert.DeserializeObject<ObjectPatchOperator>(json);
            var result = op.Evaluate(ctx);

            var obj = Assert.IsType<JObject>(result.Json);
            Assert.Equal("newVal", obj.Value<string>("newKey"));
            // existing remains
            Assert.Equal("old", obj.Value<string>("existing"));
        }

        [Fact]
        public void ObjectPatchOperator_Evaluate_AddOrUpdate_UpdatesValues()
        {
            var source = JObject.Parse(@"{ 'existing': 'old' }");
            var ctx = TemplateContext.Create(source, source);

            var json = @"{
                ""@addOrUpdate"": { ""existing"": ""updated"" }
            }";
            var op = JsonConvert.DeserializeObject<ObjectPatchOperator>(json);
            var result = op.Evaluate(ctx);

            var obj = Assert.IsType<JObject>(result.Json);
            Assert.Equal("updated", obj.Value<string>("existing"));
        }

        [Fact]
        public void ObjectPatchOperator_Evaluate_RemoveKeys_RemovesSpecifiedKeys()
        {
            var source = JObject.Parse(@"{ 'keep': 1, 'toRemove': 2 }");
            var ctx = TemplateContext.Create(source, source);

            var json = @"{
                ""@removeKeys"": [ ""toRemove"" ]
            }";
            var op = JsonConvert.DeserializeObject<ObjectPatchOperator>(json);
            var result = op.Evaluate(ctx);

            var obj = Assert.IsType<JObject>(result.Json);
            Assert.True(obj.ContainsKey("keep"));
            Assert.False(obj.ContainsKey("toRemove"));
        }

        [Fact]
        public void ObjectPatchOperator_Evaluate_OrderKeys_ReordersProperties()
        {
            var source = JObject.Parse(@"{ 'a':1, 'b':2, 'c':3 }");
            var ctx = TemplateContext.Create(source, source);

            var json = @"{
                ""@orderKeys"": [ { ""@key"": ""c"", ""@index"": 0 } ]
            }";
            var op = JsonConvert.DeserializeObject<ObjectPatchOperator>(json);
            var result = op.Evaluate(ctx);

            var obj = Assert.IsType<JObject>(result.Json);
            var firstPropName = obj.Properties().First().Name;
            Assert.Equal("c", firstPropName);
        }

        [Fact]
        public void ObjectPatchOperator_Evaluate_NonObject_ReturnsOriginalToken()
        {
            var source = JValue.CreateString("just a string");
            var ctx = TemplateContext.Create(source, source);

            var op = new ObjectPatchOperator();
            var result = op.Evaluate(ctx);

            Assert.Equal(source, result.Json);
        }

        [Fact]
        public void ContextOperator_Evaluate_ReturnsElementAndCreatesNewTemplateContext()
        {
            var global = JObject.Parse(@"{ 'g':1 }");
            var local = JObject.Parse(@"{ 'l':2 }");

            var element = JObject.Parse(@"{ 'elem': true }");
            var op = new ContextOperator(element, local);

            var ctx = TemplateContext.Create(global, global);
            var result = op.Evaluate(ctx);

            Assert.Equal(element.ToString(Formatting.None), result.Json.ToString(Formatting.None));
            Assert.NotNull(result.TemplateContext);
            Assert.Equal(global.ToString(Formatting.None), result.TemplateContext.GlobalContext.ToString(Formatting.None));
            Assert.Equal(local.ToString(Formatting.None), result.TemplateContext.LocalContext.ToString(Formatting.None));
        }

        [Fact]
        public void ElementOperator_Evaluate_PathExists_ReturnsToken()
        {
            var source = JObject.Parse(@"{ 'field': 'value' }");
            var ctx = TemplateContext.Create(source, source);

            var op = new ElementOperator{ Path = "field" };
            var result = op.Evaluate(ctx);

            Assert.Equal("value", result.Json.ToObject<string>());
        }

        [Fact]
        public void ElementOperator_Evaluate_PathMissing_ReturnsDefault()
        {
            var source = JObject.Parse(@"{ }");
            var ctx = TemplateContext.Create(source, source);

            var defaultToken = JValue.CreateNull();
            var op = new ElementOperator() { Path = "invalidPath" }; // default Path = "$", Default = null
            var result = op.Evaluate(ctx);

            // Since nothing exists at "$" in an empty object, SelectToken returns Default (null)
            Assert.Equal(defaultToken.Type, result.Json.Type);
        }
    }
}
