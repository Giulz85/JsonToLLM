using JsonToLLM.Extensions;
using JsonToLLM.Helpers;
using JsonToLLM.Model;
using Newtonsoft.Json.Linq;

namespace JsonToLLM
{
    public interface ITemplateEngine
    {
        JToken Transform(JToken template, TemplateContext context);
    }

    public class TemplateEngine : ITemplateEngine
    {
        private IExpressionEngine _expressionEngine;
        private IFactoryOperator _factoryOperator;

        public TemplateEngine(IExpressionEngine expressionTrasformer, IFactoryOperator factoryOperator)
        {
            _expressionEngine = expressionTrasformer ?? throw new ArgumentNullException(nameof(expressionTrasformer));
            _factoryOperator = factoryOperator ?? throw new ArgumentNullException(nameof(factoryOperator));

        }

        public JToken Transform(JToken token, TemplateContext context)
        {
            if (token == null || context == null)
            {
                throw new ArgumentNullException("Token or context cannot be null.");
            }

            JToken newToken = token;

            if (_expressionEngine.IsExpression(token))
            {
                 newToken = _expressionEngine.Evaluate(token, context);              
            }
            else if (token.TryToGetSpecificValue<string>("@operator", out var @operator) && @operator != null)
            {
                //Factory to create the operator component and check if it is a valid operator 
                var eachNode = _factoryOperator.CreateOperator(@operator, token); 

                var result = eachNode.Evaluate(context);
                   
                newToken = Transform(result, context); 
            }
            // Temporary node to update a node with a specific context  
            else if (token.TryToGetSpecificValue<string>("@type", out var @type) && @type == "context")
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
