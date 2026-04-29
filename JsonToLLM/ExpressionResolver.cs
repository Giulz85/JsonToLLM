using JsonToLLM.Model;
using System.Globalization;
using System.Reflection;

namespace JsonToLLM;

public interface IExpressionResolver
{
    IExpression Resolve(string functionName, TemplateContext context, string?[]? args);
}

public class ExpressionResolver : IExpressionResolver
{
    public IExpression Resolve(string functionName, TemplateContext? context, string?[]? args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);

        var expressionType = Type.GetType($"JsonToLLM.Model.{functionName}Expression", throwOnError: false, ignoreCase: true) ??
                             throw new ArgumentException($"Unsupported function '{functionName}'");

        // Convert null to an empty array
        var finalArgs = args is null ? [] : args.Select(object? (arg) => arg).ToList();
        finalArgs.Insert(0, context);

        // Use Activator with optional parameter binding
        try
        {
            return (IExpression)Activator.CreateInstance(
                expressionType,
                BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding,
                binder: null,
                args: finalArgs.ToArray(),
                culture: CultureInfo.CurrentCulture
            )!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error creating an expression instance for function: '{functionName}', args: '{string.Join(',', args ?? [])}'", ex);
        }
    }
}