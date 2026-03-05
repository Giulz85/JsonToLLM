using JsonToLLM;
using JsonToLLM.Model;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Xunit;
using Xunit.Abstractions;

namespace JsonToLLM.Test
{
    public class TranformJsonResponseTest
    {

        private IExpressionEngine _expressionTrasformer = new ExpressionEngine();
        private readonly ITestOutputHelper _output;

        public TranformJsonResponseTest(ITestOutputHelper output)
        {
            _output = output;
        }

        // Arrange
  

        [Fact]
        public void Transform_DxlJsonWithComplexTemplate_ResolvesValue()
        {
           
            var source = JObject.Parse(File.ReadAllText(@".\json\each_operator_source_template.json"));
            var template = JObject.Parse(File.ReadAllText(@".\json\each_operator_source_template.json"));
            var ctx = TemplateContext.Create(source, source);

            IFactoryOperator factoryOperator = new FactoryOperator();

            // Act
            var transformer = new TemplateEngine(_expressionTrasformer, factoryOperator);
            var result = transformer.Transform(template, ctx)?.ToString();

            if (!string.IsNullOrWhiteSpace(result))
            {
                try
                {
                    var parsed = JToken.Parse(result);
                    var pretty = parsed.ToString(Formatting.Indented);
                    _output.WriteLine("=== JSON Result ===");
                    _output.WriteLine(pretty);
                }
                catch (JsonReaderException)
                {
                    _output.WriteLine("Result is not valid JSON:");
                    _output.WriteLine(result);
                }
            }
            else
            {
                _output.WriteLine("Result is null or empty.");
            }
            // Assert
            Assert.NotNull( result);

        }


    

    }
}
