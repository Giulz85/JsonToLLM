using JsonToLLM.Helpers;
using JsonToLLM.Model;

namespace JsonToLLM;

public interface IExpressionEngine
{
    string Evaluate(string expression, TemplateContext context);
}

public class ExpressionEngine(IExpressionResolver expressionResolver) : IExpressionEngine
{
    public string Evaluate(string expression, TemplateContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.LocalContext);
        ArgumentNullException.ThrowIfNull(context.GlobalContext);

        if (!ExpressionHelper.IsExactFunctionCall(expression))
            return expression;

        var evaluated = expression;
        while (ExpressionHelper.TryParseFunctionNameAndArguments(evaluated, out var functionName, out var arguments, out var startIndex, out var endIndex))
        {
            var expressionInstance = expressionResolver.Resolve(functionName!, context, arguments);
            
            var evalPortion = expressionInstance.GetValue();

            // If nested function call, add quotes around the result to be passed as a string argument.
            if (startIndex > 0) evalPortion = $"\"{evalPortion}\"";

            evaluated = string.Concat(evaluated.AsSpan(0, startIndex!.Value), evalPortion.AsSpan(), evaluated.AsSpan(endIndex!.Value + 1));
        }

        return evaluated;
    }
}