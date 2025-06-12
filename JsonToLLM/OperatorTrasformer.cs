using JsonToLLM.Helpers;
using JsonToLLM.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM
{
    public interface IOperatorTrasformer
    {
        JToken Transform(string @operator, JObject operatorTemplate, TemplateContext context);
    }

    public class OperatorTrasformer : IOperatorTrasformer
    {
       
        public OperatorTrasformer()
            {
            // Constructor logic if needed
        }

        public JToken Transform(string @operator, JObject operatorTemplate, TemplateContext context)
        {
            JToken newToken = JValue.CreateNull(); // Default value if no transformation is applied
            if (@operator == "each")
            {
                var eachObject = (JObject)operatorTemplate;
                if (!eachObject.TryGetValue("@path", out JToken? pathEach) || pathEach.Type != JTokenType.String)
                    throw new ArgumentException($"The '@each' operator requires a 'source' property of type string in path '{operatorTemplate.Path}'.");
                if (!eachObject.TryGetValue("@element", out JToken? elementEach))
                    throw new ArgumentException($"The '@each' operator requires a '@element' property");
                var pathArray = pathEach.ToString() ?? string.Empty;

                var tokenArray = context.LocalContext.SelectToken(pathArray);
                if (tokenArray == null || tokenArray.Type == JTokenType.Null || tokenArray.Type == JTokenType.None)
                    newToken = new JArray(); // Replace with an empty array if the path does not exist
                
                if (tokenArray?.Type == JTokenType.Array)
                {
                    var newArray = new JArray();
                    foreach (var item in tokenArray.Children())
                    {
                        var elementEachCloned = elementEach.DeepClone();
                        elementEachCloned["@context"] = JToken.FromObject(item); // Add context to the elementEach
                        newArray.Add(elementEachCloned);
                    }
                    newToken = newArray;
                }
                else
                {
                    throw new ArgumentException($"The '@each' operator requires a valid array at path '{pathArray}'. Found type {tokenArray.Type}.");
                }
            }

            return newToken;
        }

        private string ResolveJsonValue(TemplateContext context, string path, string newValue)
        {
            if (!ExpressionHelper.TryParseFunctionNameAndArguments(newValue, out string? functionName, out string arguments, out var startIndex, out var endIndex))
                throw new ArgumentException($"Invalid function format in path '{path}': {newValue}");

            var parameters = ExpressionHelper.SplitArguments(arguments, '\\').ToList(); // Convert to List to allow modification

            for (int i = 0; i < parameters.Count; i++) // Use index-based iteration to modify elements
            {
                if (ExpressionHelper.IsFunction(parameters[i]))
                    parameters[i] = ResolveJsonValue(context, path, parameters[i]);
            }

            if (functionName == "value")
            {
                if (parameters.Count != 1)
                    throw new ArgumentException($"Function 'value' requires exactly one parameter in path '{path}': {newValue}");

                var expression = new ValueExpression(context, parameters[0], new JValue("null"));
                var expressionValue = expression.GetValue().ToString() ?? string.Empty;

                // Replace the function call in the string with the evaluated value
                newValue = newValue.Substring(0, startIndex.Value) + expressionValue + newValue.Substring(endIndex.Value + 1);
            }
            else if (functionName == "formatdate")
            {
                //formatdate(date,originalFormat,newFormat)
                if (parameters.Count != 3)
                    throw new ArgumentException($"Function 'formatdate' requires exactly 3 parameter in path '{path}': {newValue}");

                var expression = new FormatDateExpression(context, parameters[0], parameters[1], parameters[2]);
                var expressionValue = expression.GetValue().ToString() ?? string.Empty;

                // Replace the function call in the string with the evaluated value
                newValue = newValue.Substring(0, startIndex.Value) + expressionValue + newValue.Substring(endIndex.Value + 1);
            }
            else
            {
                throw new ArgumentException($"Unsupported function '{functionName}' in path '{path}': {newValue}");            
            }

            return newValue;
        }
    }
}
