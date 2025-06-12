using System;
using Newtonsoft.Json.Linq;
using Xunit;
using JsonToLLM;
using JsonToLLM.Model;

namespace JsonToLLM.Test
{
    public class TranformJsonResponseTest
    {

        // Arrange
        private IExpressionEngine _expressionTrasformer = new ExpressionEngine();
        private IOperatorTrasformer _operatorTrasformer = new OperatorTrasformer();

        [Fact]
        public void Transform_DxlJsonWithComplexTemplate_ResolvesValue()
        {
           
            var source = JObject.Parse(File.ReadAllText(@".\json\dxl-response-final.json"));
            var template = JObject.Parse(File.ReadAllText(@".\json\dxl-response-final-template.json"));
            var ctx = TemplateContext.Create(source, source);

            // Act
            var transformer = new JsonToLLMTrasformer(_expressionTrasformer, _operatorTrasformer);
            var result = transformer.Transform(template, ctx)?.ToString();

            // Assert
            Assert.NotNull( result);
        }


    

    }
}
