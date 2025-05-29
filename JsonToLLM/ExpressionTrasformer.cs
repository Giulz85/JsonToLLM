using JsonToLLM.Helpers;
using JsonToLLM.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM
{
    public interface IExpressionTrasformer
    {
        JObject Transform(JToken template, Context context);
    }

    public class ExpressionTrasformer : IExpressionTrasformer
    {
        //public string Transform(JObject source, JObject destionation)
        //{
        //    foreach (var keyValue in destionation)
        //    {


        //    }
        //}

        public ExpressionTrasformer()
            {
            // Constructor logic if needed
        }

        public JObject Transform(JToken template, Context context)
        {
            if (template == null || context == null)
            {
                throw new ArgumentNullException("Source or template or context cannot be null.");
            }
            var destination = (JObject)template.DeepClone();
            destination.WalkByCondition(
                (key,token) =>
                {
                    return token.Type == JTokenType.String && ExpressionHelper.IsFunction(token.ToString());
                },
                (name, path, node) =>
                {
                    var templateValue = (string?)node;
                    if (templateValue != null)
                    {
                        var newValue = templateValue;
                        do
                        {
                            newValue = ResolveJsonValue(context, path, newValue);
                        }
                        while (ExpressionHelper.IsFunction(newValue));

                        // Replace the node value with the new value
                        node.Replace(JToken.FromObject(newValue));
                    }
                });

            return destination;
        }

        private string ResolveJsonValue(Context context, string path, string newValue)
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
