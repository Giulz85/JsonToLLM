using JsonToLLM.Helpers;
using JsonToLLM.Model;
using Newtonsoft.Json.Linq;

namespace JsonToLLM
{
    public interface IJsonToLLMTrasformer
    {
        JToken Transform(JToken template, Context context);
    }

    public class JsonToLLMTrasformer : IJsonToLLMTrasformer
    {
        private IExpressionTransformer _expressionTrasformer;
        private IOperatorTrasformer _operationTranformet;

        public JsonToLLMTrasformer(IExpressionTransformer expressionTrasformer, IOperatorTrasformer operationTranformer)
        {
            _expressionTrasformer = expressionTrasformer ?? throw new ArgumentNullException(nameof(expressionTrasformer));
            _operationTranformet = operationTranformer ?? throw new ArgumentNullException(nameof(operationTranformer));

        }

        public JToken Transform(JToken token, Context context)
        {
            if (token == null || context == null)
            {
                throw new ArgumentNullException("Token or context cannot be null.");
            }

            if (_expressionTrasformer.IsExpression(token))
            {
                var newValue = _expressionTrasformer.Transform(token, context);

                // Replace the node value with the new value
                //if (token.Parent == null)
                //        return JValue.FromObject(newValue);
                //else
                //    token.Replace(JToken.FromObject(newValue));  
                return newValue;
            }
            else if (token.Type == JTokenType.Object && ((JObject)token).TryGetValue("@operator", out var operatorToken))
            {
                var @operatorName = operatorToken.Value<string>() ?? throw new ArgumentException($"Found invalid type {operatorToken.Type} for @operator at path {operatorToken.Path}");

                //@Each operator
                if (string.Equals(@operatorName, "each", StringComparison.InvariantCultureIgnoreCase))
                {
                    var eachObject = (JObject)token;
                    if (!eachObject.TryGetValue("@path", out JToken? pathEach) || pathEach.Type != JTokenType.String)
                        throw new ArgumentException($"The '@each' operator requires a 'source' property of type string in path '{token.Path}'.");
                    if (!eachObject.TryGetValue("@element", out JToken? elementEach))
                        throw new ArgumentException($"The '@each' operator requires a '@element' property");
                    var pathArray = pathEach.ToString() ?? string.Empty;

                    var tokenArray = context.LocalContext.SelectToken(pathArray);
                    if (tokenArray == null || tokenArray.Type == JTokenType.Null || tokenArray.Type == JTokenType.None)
                    {
                        //token.Replace(new JArray()); // Replace with an empty array if the path does not exist
                        return new JArray(); // Return an empty array directly
                    }
                    else if (tokenArray.Type == JTokenType.Array)
                    {
                        var newArray = new JArray();
                        foreach (var item in tokenArray.Children())
                        {
                            var contextItem = new Context(context.GlobalContext, item);
                            var elem = Transform(elementEach.DeepClone(), contextItem);
                            newArray.Add(elem);
                        }
                        // Replace the original token with the new array 
                        // token.Replace(newArray);
                        return newArray; // Return the new array directly
                    }
                    else
                    {
                        throw new ArgumentException($"The '@each' operator requires a valid array at path '{pathArray}'. Found type {tokenArray.Type}.");
                    }
                }
                else
                {
                    throw new ArgumentException($"Unsupported operator '{@operatorName}' at path '{token.Path}'.");
                }
            }
            else if (token.Type == JTokenType.Object && ((JObject)token).TryGetValue("@context", out var elemContext))
            {
                var contextObject = (JObject)token;

                if (!contextObject.TryGetValue("@element", out JToken? element))
                    throw new ArgumentException($"The '@each' operator requires a '@element' property");

                Context contextFromElem = Context.Create(context.GlobalContext, elemContext);
                var newToken = Transform(element, context);
                return newToken;

            }
            else if (token.Type == JTokenType.Object)
            {
                foreach (var child in token.Children<JProperty>())
                {
                    var newValue = Transform(child.Value, context);
                    child.Value = newValue; // Replace the child value with the transformed value
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                var array = (JArray)token;
                for (var i = 0; i < array.Count; i++)
                {
                    var elem = array[i];
                    Context contextElem = new Context(context.GlobalContext, elem);

                    array[i] = Transform(elem, contextElem);
                }
            }
            return token; // Return the token as is if no transformation is needed
        }
    }
}
