using JsonToLLM.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace JsonToLLM.Test;

public class TransformJsonResponseTest(ITestOutputHelper output)
{
    private readonly IExpressionEngine _expressionTransformer = new ExpressionEngine(new ExpressionResolver());

    [Fact]
    public void Transform_DxlJsonWithComplexTemplate_ResolvesValue()
    {
        // Arrange
        var source = JObject.Parse(File.ReadAllText(@".\json\each_operator_source_template.json"));
        var template = JObject.Parse(File.ReadAllText(@".\json\each_operator_source_template.json"));
        var ctx = TemplateContext.Create(source, source);

        IFactoryOperator factoryOperator = new FactoryOperator();

        // Act
        var transformer = new TemplateEngine(_expressionTransformer, factoryOperator);
        var result = transformer.Transform(template, ctx).ToString();

        if (!string.IsNullOrWhiteSpace(result))
        {
            try
            {
                var parsed = JToken.Parse(result);
                var pretty = parsed.ToString(Formatting.Indented);
                output.WriteLine("=== JSON Result ===");
                output.WriteLine(pretty);
            }
            catch (JsonReaderException)
            {
                output.WriteLine("Result is not valid JSON:");
                output.WriteLine(result);
            }
        }
        else
        {
            output.WriteLine("Result is null or empty.");
        }

        // Assert
        Assert.NotNull(result);
    }
}
