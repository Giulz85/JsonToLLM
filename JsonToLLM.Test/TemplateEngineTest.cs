using Newtonsoft.Json.Linq;
using JsonToLLM.Model;

namespace JsonToLLM.Test;

public class TemplateEngineTest
{
    private readonly IExpressionEngine _expressionEngine = new ExpressionEngine(new ExpressionResolver());
    private readonly IFactoryOperator _factoryOperator = new FactoryOperator();

    [Theory]
    [InlineData("{ 'result': '@value(foo)' }", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result': '{{ @value(\"foo\") }}' }", ExpressionParserVersion.Version2)]
    public void TransformV1_SingleFunction_ResolvesValue(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("""{ "foo": "bar" }""");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.Equal("bar", result["result"]);
    }

    [Theory]
    [InlineData("{ 'result': '@value(prop1)@value(prop2)' }", "value1value2", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result': ' @value(prop1) @value(prop2) ' }", " value1 value2 ", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result': '{{ @value(\"prop1\") }}{{ @value(\"prop2\") }}' }", "value1value2", ExpressionParserVersion.Version2)]
    public void Transform_MultipleFunctions_ResolvesValues(string template, string result, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'prop1':'value1', 'prop2':'value2' }");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var actualResult = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.Equal(result, actualResult["result"]);
    }

    [Theory]
    [InlineData("{ 'result': 'Result should be like this: @value($.prop1)@value($.object1.prop2) that is it!'}", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result': 'Result should be like this: {{ @value(\"$.prop1\") }}{{ @value(\"$.object1.prop2\") }} that is it!' }", ExpressionParserVersion.Version2)]
    public void Transform_MultipleNestledFunctions_ReplacesMultipleValuesFromNestledObject(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'prop1':'value1', 'object1': { 'prop2':'value2'} }");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.Equal("Result should be like this: value1value2 that is it!", result["result"]?.ToString());
    }

    [Theory]
    [InlineData("{ 'formatedDate': '@formatdate(@value($.originalDate),dd-MM-yyyy,dd/MM/yyyy)' }", ExpressionParserVersion.Version1)]
    [InlineData("{ 'formatedDate': '{{ @formatdate(@value(\"$.originalDate\"), \"dd-MM-yyyy\", \"dd/MM/yyyy\") }}' }", ExpressionParserVersion.Version2)]
    public void Transform_FieldFormatDateExpression_ReformatsDateTime(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'originalDate':'29-05-2025'}");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.Equal("29/05/2025", result["formatedDate"]?.ToString());
    }

    [Theory]
    [InlineData("{ 'result1': 'The customer @value($.name) @value($.secondName) lives in @value($.address.city)' }", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result1': 'The customer {{ @value(\"$.name\") }} {{ @value(\"$.secondName\") }} lives in {{ @value(\"$.address.city\") }}' }", ExpressionParserVersion.Version2)]
    public void Transform_FieldWithFreeTextAndExpressions_ReplacesMultipleValuesInFreeText(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'name':'giuliano', 'secondName':'arru', 'address': { 'city':'saronno'} }");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.Equal("The customer giuliano arru lives in saronno", result["result1"]?.ToString());
    }

    [Theory]
    [InlineData(ExpressionParserVersion.Version1)]
    [InlineData(ExpressionParserVersion.Version2)]
    public void Transform_FieldWithNoExpression_LeavesNonFunctionStringsUnchanged(ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'foo': 'bar' }");
        var template = JObject.Parse(@"{ 'result': 'noFunctionHere' }");
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(template, ctx, parserVersion);

        // Assert
        Assert.Equal("noFunctionHere", result["result"]?.ToString());
    }

    [Theory]
    [InlineData("{ 'result': { '@operator':'each','@path':'array','@element':{ 'field': '@value(prop)' } } } ", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result': { '@operator':'each','@path':'array','@element':{ 'field': '{{ @value(\"prop\") }}' } } } ", ExpressionParserVersion.Version2)]
    public void Transform_UseEachOperator_CreateArrayOfObject(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'array': [ { 'prop':'value'}, { 'prop':'value1'}, { 'prop':'value2'} ]}");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.NotNull(result["result"]?[0]?["field"]);
        Assert.Equal("value", result["result"]![0]!["field"]);
    }

    [Theory]
    [InlineData("{ 'result': { '@operator':'each','@path':'customers','@element': 'Customer @value(name) @value(secondName)'  } } ", ExpressionParserVersion.Version1)]
    [InlineData("{ 'result': { '@operator':'each','@path':'customers','@element': 'Customer {{ @value(\"name\") }} {{ @value(\"secondName\") }}'  } } ", ExpressionParserVersion.Version2)]

    public void Transform_UseEachOperator_CreateArrayWhereElementsAreString(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'customers': [ { 'name':'giuliano', 'secondName':'arru'}, { 'name':'mario', 'secondName':'rossi'} ]}");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.NotNull(result["result"]?[0]);
        Assert.Equal("Customer giuliano arru", result["result"]![0]);
    }

    [Theory]
    [InlineData("""
                { 'result': { 
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
                }
                """, ExpressionParserVersion.Version1)]
    [InlineData("""
                { 'result': { 
                        '@operator':'each',
                        '@path':'customers',
                        '@element': { 
                            'customer':'{{ @value("name") }} {{ @value("secondName") }}',
                            'counters': { 
                                '@operator':'each', 
                                '@path':'counters', 
                                '@element': 'Speso {{ @value("amount") }} {{ @value("unit") }} in data {{ @formatdate(@value("date"), "dd-MM-yyyy", "dd/MM/yyyy") }}'
                            } 
                        }
                    }
                }
                """, ExpressionParserVersion.Version2)]
    public void Transform_NestledEachOperators_CreateNestledArray(string template, ExpressionParserVersion parserVersion)
    {
        // Arrange
        var source = JObject.Parse("{ 'customers': [ { 'name':'mario', 'secondName':'rossi', 'counters':[{'amount':3, 'unit':'euro', 'date': '29-05-2025'  },{'amount':4, 'unit':'dollar','date': '29-05-2025' }] } ]}");
        var templateObj = JObject.Parse(template);
        var ctx = TemplateContext.Create(source, source);

        // Act
        var transformer = new TemplateEngine(_expressionEngine, _factoryOperator);
        var result = transformer.Transform(templateObj, ctx, parserVersion);

        // Assert
        Assert.NotNull(result["result"]?[0]);
        Assert.Equal("mario rossi", result["result"]![0]!["customer"]);
        Assert.Equal("Speso 3 euro in data 29/05/2025", result["result"]![0]!["counters"]![0]);
    }
}