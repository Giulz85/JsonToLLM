using System.Text;
using JsonToLLM.Model;
using Newtonsoft.Json.Linq;

namespace JsonToLLM;

public interface ITemplateEngine
{
    JToken Transform(JToken template, TemplateContext context);
}

public class TemplateEngine : ITemplateEngine
{
    private readonly IExpressionEngine _expressionEngine;
    private readonly IFactoryOperator _factoryOperator;
    /// <summary>
    /// Delimiters used for script parsing
    /// </summary>
    public ScriptingDelimiters ScriptingDelimiters { get; set; } = ScriptingDelimiters.Default;

    public TemplateEngine(IExpressionEngine expressionTransformer, IFactoryOperator factoryOperator)
    {
        _expressionEngine = expressionTransformer ?? throw new ArgumentNullException(nameof(expressionTransformer));
        _factoryOperator = factoryOperator ?? throw new ArgumentNullException(nameof(factoryOperator));
    }

    public JToken Transform(JToken token, TemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(context);

        var resultToken = token;

        if (token.Type is JTokenType.String)
        {
            var stringTemplate = token.ToString();
            var strResult = Transform(stringTemplate, context);

            resultToken = new JValue(strResult);
        }
        else if (token.Type is JTokenType.Object)
        {
            var objectTemplate = (JObject)token;
            if (objectTemplate.TryGetValue("@operator", out var @operator))
            {
                var operatorName = @operator.Type == JTokenType.String
                    ? @operator.ToString()
                    : throw new InvalidOperationException($"Operator property must has string value, path: '{@operator.Path}'");

                // Factory to create the operator component and check if it is a valid operator 
                var operatorTemplate = _factoryOperator.CreateOperator(operatorName, objectTemplate);

                var result = operatorTemplate.Evaluate(context);

                // Operator can change context. New context can have node and expression to resolve.
                if (result.TemplateContext != null)
                {
                    var resolvedContext = Transform(result.TemplateContext.LocalContext, context);
                    context = TemplateContext.Create(context.GlobalContext, resolvedContext);
                }

                resultToken = Transform(result.Json, context);
            }
            // Temporary node to update a node with a specific context  
            else if (objectTemplate.TryGetValue("@type", out var typeNode))
            {
                var typeNodeStr = typeNode.Type == JTokenType.String
                    ? typeNode.ToString()
                    : throw new InvalidOperationException($"Operator property must has string value, path: '{typeNode.Path}'");

                if (!string.Equals(typeNodeStr, "context"))
                    throw new InvalidOperationException($"Unsupported type-operator '{typeNodeStr}', path: '{typeNode.Path}'");

                var contextNode = objectTemplate.ToObject<ContextElement>() ??
                                  throw new InvalidOperationException("Can't create ContextElement for the provided type-operator.");

                // In context can be inserted operator and expression to resolve.
                var resolvedContext = Transform(contextNode.Context, context);

                var contextFromElem = TemplateContext.Create(context.GlobalContext, resolvedContext);

                resultToken = Transform(contextNode.Element, contextFromElem);
            }
            else
            {
                foreach (var child in objectTemplate.Children<JProperty>())
                {
                    var newValue = Transform(child.Value, context);
                    child.Value = newValue; // Replace the child value with the transformed value
                }

                resultToken = objectTemplate; // Return the modified object
            }
        }
        else if (token.Type is JTokenType.Array)
        {
            var arrayTemplate = (JArray)token;
            for (var i = 0; i < arrayTemplate.Count; i++)
            {
                var elem = arrayTemplate[i];
                var contextElem = new TemplateContext(context.GlobalContext, elem);

                arrayTemplate[i] = Transform(elem, contextElem);
            }

            resultToken = arrayTemplate; // Return the modified array
        }

        return resultToken; // Return the token as is if no transformation is needed
    }

    public string Transform(string template, TemplateContext context)
        => TransformStringTemplate(template, context);

    private string TransformStringTemplate(string template, TemplateContext context)
    {
        if (string.IsNullOrEmpty(template)) 
            return template;

        var atStart = template.IndexOf(ScriptingDelimiters.StartDelimiter, StringComparison.OrdinalIgnoreCase);
        // No expressions in the template
        if (atStart < 0)
            return template;

        var result = new StringBuilder();
        do
        {
            var atEnd = template.IndexOf(ScriptingDelimiters.EndDelimiter, StringComparison.Ordinal);
            // No end delimiter
            if (atEnd < 0)
            {
                result.Append(template);
                break;
            }
            if (atEnd < atStart)
                throw new InvalidOperationException($"Scripting Error: {ScriptingDelimiters.EndDelimiter} delimiter nesting error.");

            // Take text up to the expression
            result.Append(template.AsSpan(0, atStart));

            var expression = template
                .Substring(atStart + ScriptingDelimiters.StartDelimiter.Length, atEnd - atStart - ScriptingDelimiters.EndDelimiter.Length)
                .Trim();
            
            // Evaluate extracted expression
            var expEngineResult = _expressionEngine.Evaluate(expression, context);
           
            result.Append(expEngineResult);

            // Text that is left 
            template = template[(atEnd + ScriptingDelimiters.EndDelimiter.Length)..];

            // Look for the next expression
            atStart = template.IndexOf(ScriptingDelimiters.StartDelimiter, StringComparison.Ordinal);
            if (atStart < 0)
                // Append remaining literal text
                result.Append(template);
            
        } while (atStart > -1);

        return result.ToString();
    }
}

/// <summary>
/// Class that encapsulates the delimiters used for script parsing
/// </summary>
public class ScriptingDelimiters
{
    /// <summary>
    /// Start delimiter for expressions
    /// </summary>
    public string StartDelimiter { get; set; } = "{{";

    /// <summary>
    /// End delimiter for expressions
    /// </summary>
    public string EndDelimiter { get; set; } = "}}";

    /// <summary>
    /// A default instance of the delimiters
    /// </summary>
    public static ScriptingDelimiters Default { get; } = new();
}