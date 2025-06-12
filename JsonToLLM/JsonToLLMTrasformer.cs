using JsonToLLM.Helpers;
using JsonToLLM.Model;
using Newtonsoft.Json.Linq;

namespace JsonToLLM
{
    public interface IJsonToLLMTrasformer
    {
        JToken Transform(JToken template, TemplateContext context);
    }

    public class JsonToLLMTrasformer : IJsonToLLMTrasformer
    {
        private IExpressionEngine _expressionEngine;
        private IOperatorTrasformer _operationTranformet;

        public JsonToLLMTrasformer(IExpressionEngine expressionTrasformer, IOperatorTrasformer operationTranformer)
        {
            _expressionEngine = expressionTrasformer ?? throw new ArgumentNullException(nameof(expressionTrasformer));
            _operationTranformet = operationTranformer ?? throw new ArgumentNullException(nameof(operationTranformer));

        }

        public JToken Transform(JToken token, TemplateContext context)
        {
            if (token == null || context == null)
            {
                throw new ArgumentNullException("Token or context cannot be null.");
            }

            JToken newToken = token;

            // controlla se è una stringa ed eventulmente valuta expressione
            if (_expressionEngine.IsExpression(token))
            {
                 newToken = _expressionEngine.Evaluate(token, context);              
            }
            else if (token.Type == JTokenType.Object &&  ((JObject)token).TryGetValue("@operator", out var operatorToken))
            {
                //Factory to create the operator component and check if it is a valid operator 
                var eachNode = token.ToObject<EachOperator>(); // Ensure token is a JObject for further processing
                var result = eachNode.Evaluate(context);
                
                newToken = Transform(result, context); 
            }
            //temporary node to update a node with a specific context  
            else if (token.Type == JTokenType.Object && ((JObject)token).TryGetValue("@type", out var type) && 
                (type.Value<string>() == "context"))
            {
                var contextNode = token.ToObject<ContextElement>();

                TemplateContext contextFromElem = TemplateContext.Create(context.GlobalContext, contextNode.Context);
                newToken = Transform(contextNode.Element, contextFromElem);

            }
            else if (token.Type == JTokenType.Object)
            {
                foreach (var child in token.Children<JProperty>())
                {
                    var newValue = Transform(child.Value, context);
                    child.Value = newValue; // Replace the child value with the transformed value
                }
                newToken = token; // Return the modified object
            }
            else if (token.Type == JTokenType.Array)
            {
                var array = (JArray)token;
                for (var i = 0; i < array.Count; i++)
                {
                    var elem = array[i];
                    TemplateContext contextElem = new TemplateContext(context.GlobalContext, elem);

                    array[i] = Transform(elem, contextElem);
                }
                newToken = array; // Return the modified array
            }
            return newToken; // Return the token as is if no transformation is needed
        }
    }
}
