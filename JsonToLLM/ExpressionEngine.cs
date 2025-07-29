using JsonToLLM.Helpers;
using JsonToLLM.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonToLLM
{
    public interface IExpressionEngine
    {
        JToken Evaluate(JToken jtoken, TemplateContext context);

        bool IsExpression(JToken token);
    }

    public class ExpressionEngine : IExpressionEngine
    {
        public ExpressionEngine()
        {
            // Constructor logic if needed
        }

        public JToken Evaluate(JToken jtoken, TemplateContext context)
        {
            if (!IsExpression(jtoken))
                return jtoken;

            if (jtoken == null)
                throw new ArgumentNullException(nameof(jtoken));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var newValue = jtoken.Value<string>() ?? throw new ArgumentException($"Field with expression cannot be null or empty in path '{jtoken.Path}'.");
            do
            {
                newValue = ResolveInternalFunction(context, jtoken.Path, newValue);
            }
            while (ExpressionHelper.IsFunction(newValue));

            return new JValue(newValue);
        }

        private string ResolveInternalFunction(TemplateContext context, string path, string newValue)
        {
            if (!ExpressionHelper.TryParseFunctionNameAndArguments(newValue, out string? functionName, out string arguments, out var startIndex, out var endIndex))
                throw new ArgumentException($"Invalid function format in path '{path}': {newValue}");

            var parameters = ExpressionHelper.SplitArguments(arguments, '\\').ToList(); // Convert to List to allow modification

            //Resolve function parameters
            for (int i = 0; i < parameters.Count; i++) // Use index-based iteration to modify elements
            {
                if (IsLiteralString(parameters[i]))
                    parameters[i] = parameters[i].Trim('`');

                while (ExpressionHelper.IsFunction(parameters[i]))
                    parameters[i] = ResolveInternalFunction(context, path, parameters[i]);
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
            else if (functionName == "switch")
            {
                if (parameters.Count != 3)
                    throw new ArgumentException($"Function 'switch' richiede 3 parametri: input, mapping, default.");

                var input = parameters[0];
                var mappingJson = parameters[1];
                var defaultValue = parameters[2];

                var mapping = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(mappingJson);
                if (mapping == null)
                    throw new ArgumentException($"Invalid mapping format in path '{path}': {mappingJson}");

                var expression = new SwitchExpression(context, parameters[0], mapping, parameters[2]);
                var expressionValue = expression.GetValue().ToString() ?? string.Empty;

                var mapped = mapping.TryGetValue(input, out var result) ? result.ToString() : defaultValue;

                newValue = newValue.Substring(0, startIndex.Value) + mapped + newValue.Substring(endIndex.Value + 1);
            }
            else if (functionName == "ifelse")
            {
                if (parameters.Count != 3)
                    throw new ArgumentException("Function 'ifelse' requires exactly 3 parameter: condition, ifValue, elseValue.");

                var condition = parameters[0];
                var ifValue = parameters[1];
                var elseValue = parameters[2];

                var expression = new IfElseExpression(context, condition, ifValue, elseValue);
                var expressionValue = expression.GetValue().Value<string>();

                newValue = string.Concat(newValue.AsSpan(0, startIndex!.Value), expressionValue.AsSpan(), newValue.AsSpan(endIndex!.Value + 1));
            }
            else
            {
                throw new ArgumentException($"Unsupported function '{functionName}' in path '{path}': {newValue}");
            }

            return newValue;
        }

        public bool IsExpression(JToken token)
        {
            return token.Type == JTokenType.String && ExpressionHelper.IsFunction(token.Value<string>() ?? string.Empty);
        }

        public static bool IsLiteralString(string str) 
            => str.Length >= 2 && str[0].Equals('`') && str[^1].Equals('`');
    }
}
