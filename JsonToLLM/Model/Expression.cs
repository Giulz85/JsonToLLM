using HandlebarsDotNet;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Microsoft.CodeAnalysis.Scripting;

namespace JsonToLLM.Model;

/// <summary>
/// Defines a contract for expressions that can evaluate and return a JSON value.
/// Implementations of this interface encapsulate logic to extract or compute a value,
/// typically from a JSON structure or context, and return it as a <see cref="string"/>.
/// </summary>
public interface IExpression
{
    /// <summary>
    /// Evaluates the expression and returns the resulting <see cref="string"/>.
    /// </summary>
    /// <returns>
    /// The <see cref="string"/> resulting from the evaluation of the expression.
    /// </returns>
    string GetValue();

    Task<string> GetValueAsync();
}

public abstract class ExpressionBase(TemplateContext context) : IExpression
{
    /// <summary>
    /// Gets the context containing the local and global JSON tokens.
    /// </summary>
    public TemplateContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    public abstract string GetValue();

    public virtual async Task<string> GetValueAsync()
    {
        // Default: just delegating
        return await Task.FromResult(GetValue()).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents an expression that retrieves a value from a JSON structure based on a specified path.
/// The path can refer to either the local or global context within the provided <see cref="Context"/>.
/// If the value at the specified path is not found, a default value is returned.
/// </summary>
/// <remarks>
/// Default search is local context; if not found, it falls back to global context.
/// </remarks>
public class ValueExpression : ExpressionBase
{
    /// <summary>
    /// Gets the JSON path used to select the value.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the default value to return if the path does not resolve to a value.
    /// </summary>
    public string Default { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueExpression"/> class.
    /// </summary>
    /// <param name="context">The context containing local and global JSON tokens.</param>
    /// <param name="path"></param>
    /// <param name="defaultValue"></param>
    /// <exception cref="ArgumentNullException">
    /// </exception>
    public ValueExpression(TemplateContext context, string path, string defaultValue = "null") : base(context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        Path = path;
        Default = defaultValue;
    }

    /// <summary>
    /// Retrieves the value from the JSON context at the specified path.
    /// If the value is not found, returns the default value.
    /// </summary>
    /// <returns>
    /// The <see cref="string"/> found at the specified path, or the default value if not found.
    /// </returns>
    public override string GetValue()
    {
        // Try local context first, then fallback to global
        var token = Context.LocalContext.SelectToken(Path) ?? Context.GlobalContext.SelectToken(Path);

        if (token == null)
            return Default;

        // If it's already a scalar, return it as-is
        if (token is JValue jv)
            return jv.ToString(CultureInfo.InvariantCulture);

        // If it's an object or array, return a JSON string representation
        return token.Type is JTokenType.Object or JTokenType.Array
            ? token.ToString(Formatting.None)
            : token.ToString(); // Fallback: stringify anything else
    }
}

/// <summary>
/// Represents an expression that transforms the date result of another expression from one datetime format to another.
/// </summary>
/// <remarks>
/// This class wraps an <see cref="IExpression"/> whose result is expected to be a date string in a specified input format.
/// It parses the date string using <see cref="OriginalFormat"/> and outputs it as a string in <see cref="OutputFormat"/>.
/// If parsing fails, an exception is thrown.
/// </remarks>
public class FormatDateExpression : ExpressionBase
{
    /// <summary>
    /// Gets the expected format of the input date string.
    /// </summary>
    public string OriginalFormat { get; }

    /// <summary>
    /// Gets the format to which the date should be converted.
    /// </summary>
    public string OutputFormat { get; }

    /// <summary>
    /// Date to transform
    /// </summary>
    public string Date { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatDateExpression"/> class.
    /// </summary>
    /// <param name="context">The context containing local and global JSON tokens.</param>
    /// <param name="date">Date string to be formatted.</param>
    /// <param name="originalFormat">The expected format of the input date string.</param>
    /// <param name="outputFormat">The format to which the date should be converted.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="date"/>, <paramref name="originalFormat"/>, or <paramref name="outputFormat"/> is null or empty.
    /// </exception>
    public FormatDateExpression(TemplateContext context, string date, string originalFormat, string outputFormat) : base(context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(date);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFormat);

        OriginalFormat = originalFormat;
        OutputFormat = outputFormat;
        Date = date;
    }

    /// <summary>
    /// Evaluates the inner expression, parses its result as a date using <see cref="OriginalFormat"/>,
    /// and returns the date formatted as a string in <see cref="OutputFormat"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> containing the formatted date string.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if the input string cannot be parsed as a date using <see cref="OriginalFormat"/>.
    /// </exception>
    public override string GetValue()
    {
        return DateTime.TryParseExact(Date, OriginalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString(OutputFormat, new CultureInfo("it-IT"))
            : throw new Exception("Input format is not parsable.");  //TODO: create a custom exception for this scenario
    }
}

/// <summary>
/// Represents an expression that maps an input value to a predefined output value based on a dictionary.
/// </summary>  
/// <remarks>
/// This class uses the provided input string as a key to look up a value in the specified mapping dictionary.
/// If the key is found in the dictionary, the corresponding value is returned as a <see cref="JValue"/>.
/// If the key is not found, a default value is returned as a <see cref="JValue"/>.
/// </remarks>    
public class SwitchExpression : ExpressionBase
{
    /// <summary>
    /// Gets the input value used as a key for the mapping.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the dictionary that maps input values to output values.
    /// </summary>
    public Dictionary<string, JValue> Mapping { get; }

    /// <summary>
    /// Gets the default value to return if the input value is not found in the mapping.
    /// </summary>
    public string Default { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchExpression"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="input">The input value used as a key for the mapping.</param>
    /// <param name="mapping">The dictionary that maps input values to output values.</param>
    /// <param name="defaultValue">The @default value to return if the input value is not found in the mapping.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="input"/>, <paramref name="mapping"/>, or <paramref name="defaultValue"/> is null.
    /// </exception>
    public SwitchExpression(TemplateContext context, string input, string mapping, string defaultValue = "null") : base(context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping);

        Input = input;
        Mapping = JsonConvert.DeserializeObject<Dictionary<string, JValue>>(mapping) ??
                  throw new ArgumentException("Invalid mapping format.");

        Default = defaultValue;
    }

    /// <summary>
    /// Uses the input value as a key to look up a value in the mapping dictionary,
    /// and returns the corresponding value as a <see cref="JValue"/>,
    /// or the default value as a <see cref="JValue"/> if the key is not found.
    /// </summary>
    /// <returns>
    /// The <see cref="JValue"/> corresponding to the input value, or the default value if the input value is not found in the mapping.
    /// </returns>
    public override string GetValue()
        => Mapping.TryGetValue(Input, out var result) ? 
            result.ToString(CultureInfo.InvariantCulture) :
            Default;
}

/// <summary>
/// Represents a conditional expression that evaluates a Boolean <c>Condition</c>
/// and returns either the <c>IfValue</c> or <c>ElseValue</c> as a JSON value.
/// </summary>
/// <remarks>
/// This expression supports both synchronous and asynchronous evaluation. 
/// It uses <c>CSharpScript.EvaluateAsync&lt;bool&gt;(Condition)</c> to execute the condition.
/// The synchronous <see cref="GetValue"/> blocks the calling thread,
/// while <see cref="GetValueAsync"/> performs truly asynchronous evaluation.
/// </remarks>
public class IfElseExpression : ExpressionBase
{
    /// <summary>
    /// Gets the C# condition to evaluate. Must be a valid boolean expression.
    /// </summary>
    public string Condition { get; }

    /// <summary>
    /// Gets the value to return if the condition evaluates to <c>true</c>.
    /// </summary>
    public string IfValue { get; }

    /// <summary>
    /// Gets the value to return if the condition evaluates to <c>false</c>.
    /// </summary>
    public string ElseValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IfElseExpression"/> class.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="condition">A C# expression string that yields a boolean result.</param>
    /// <param name="ifValue">The string to return if the condition is <c>true</c>.</param>
    /// <param name="elseValue">The string to return if the condition is <c>false</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of <paramref name="condition"/>, <paramref name="ifValue"/>,
    /// or <paramref name="elseValue"/> are <c>null</c>.
    /// </exception>
    public IfElseExpression(TemplateContext context, string condition, string ifValue, string elseValue) : base(context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(condition);
        ArgumentException.ThrowIfNullOrWhiteSpace(ifValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(elseValue);

        Condition = condition;
        IfValue = ifValue;
        ElseValue = elseValue;
    }

    /// <summary>
    /// Evaluates the <see cref="Condition"/> synchronously using Roslyn scripting,
    /// blocking the calling thread until the result is available.
    /// </summary>
    /// <returns>
    /// A <see cref="JValue"/> containing <see cref="IfValue"/> or <see cref="ElseValue"/>,
    /// depending on whether the condition is true or false.
    /// </returns>
    /// <exception cref="CompilationErrorException">
    /// If the <see cref="Condition"/> string is not a valid boolean expression.
    /// </exception>
    public override string GetValue()
    {
        var cond = CSharpScript.EvaluateAsync<bool>(Condition)
            .ConfigureAwait(false)
            .GetAwaiter().GetResult();
        return cond ? IfValue : ElseValue;
    }

    /// <summary>
    /// Evaluates the <see cref="Condition"/> asynchronously using Roslyn scripting.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{JValue}"/> that resolves to a <see cref="JValue"/> containing
    /// <see cref="IfValue"/> or <see cref="ElseValue"/>,
    /// depending on whether the condition is true or false.
    /// </returns>
    /// <exception cref="CompilationErrorException">
    /// If the <see cref="Condition"/> string is not a valid boolean expression.
    /// </exception>
    public override async Task<string> GetValueAsync()
    {
        var cond = await CSharpScript.EvaluateAsync<bool>(Condition).ConfigureAwait(false);
        return cond ? IfValue : ElseValue;
    }
}