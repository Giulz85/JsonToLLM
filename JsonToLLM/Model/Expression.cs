using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JsonToLLM.Model
{
    /// <summary>
    /// Defines a contract for expressions that can evaluate and return a JSON value.
    /// Implementations of this interface encapsulate logic to extract or compute a value,
    /// typically from a JSON structure or context, and return it as a <see cref="JValue"/>.
    /// </summary>
    public interface IExpression
    {
        /// <summary>
        /// Evaluates the expression and returns the resulting <see cref="JValue"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="JValue"/> resulting from the evaluation of the expression.
        /// </returns>
        JValue GetValue();
    }

    /// <summary>
    /// Represents an expression that retrieves a value from a JSON structure based on a specified path.
    /// The path can refer to either the local or global context within the provided <see cref="Context"/>.
    /// If the value at the specified path is not found, a default value is returned.
    /// </summary>
    /// <remarks>
    /// The default behavior is to search within the local context. 
    /// Future enhancements may allow explicit selection between local and global contexts.
    /// </remarks>
    public class ValueExpression : IExpression
    {
        /// <summary>
        /// Gets the JSON path used to select the value.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the context containing the local and global JSON tokens.
        /// </summary>
        public Context Context { get; private set; }

        /// <summary>
        /// Gets the default value to return if the path does not resolve to a value.
        /// </summary>
        public JValue Default { get; private set; }

        /// <summary>
        /// Retrieves the value from the JSON context at the specified path.
        /// If the value is not found, returns the default value.
        /// </summary>
        /// <returns>
        /// The <see cref="JValue"/> found at the specified path, or the default value if not found.
        /// </returns>
        public JValue GetValue()
        {
            //TODO check if use local or global context. Default is local
            var value = (JValue?)Context.LocalContext.SelectToken(Path);

            return value ?? Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueExpression"/> class.
        /// </summary>
        /// <param name="context">The context containing local and global JSON tokens.</param>
        /// <param name="path">The JSON path to select the value.</param>
        /// <param name="defaultValue">The default value to return if the path does not resolve to a value.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="path"/>, <paramref name="context"/>, or <paramref name="defaultValue"/> is null or empty.
        /// </exception>
        public ValueExpression(Context context, string path, JValue defaultValue)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));

            Path = path;
            Context = context;
            Default = defaultValue;
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
    public class FormatDateExpression : IExpression
    {
        /// <summary>
        /// Gets the expected format of the input date string.
        /// </summary>
        public string OriginalFormat { get; private set; }

        /// <summary>
        /// Gets the format to which the date should be converted.
        /// </summary>
        public string OutputFormat { get; private set; }

        /// <summary>
        /// Gets the context containing local and global JSON tokens.
        /// </summary>
        public Context Context { get; private set; }

        /// <summary>
        /// Date to trasform
        /// </summary>
        public string Date { get; private set; }

        /// <summary>
        /// Evaluates the inner expression, parses its result as a date using <see cref="OriginalFormat"/>,
        /// and returns the date formatted as a string in <see cref="OutputFormat"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="JValue"/> containing the formatted date string.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if the input string cannot be parsed as a date using <see cref="OriginalFormat"/>.
        /// </exception>
        public JValue GetValue()
        {
            string input = Date;
            DateTime date;
            if (DateTime.TryParseExact(input, OriginalFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                // Conversione in un altro formato
                string output = date.ToString(OutputFormat, new CultureInfo("it-IT"));
                return new JValue(output);
            }
            //TODO: create a custom exception for this scenario
            throw new Exception("input format is not parsable");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FormatDateExpression"/> class.
        /// </summary>
        /// <param name="context">The context containing local and global JSON tokens.</param>
        /// <param name="date">Date string to be formatted.</param>
        /// <param name="originalFormat">The expected format of the input date string.</param>
        /// <param name="outputFormat">The format to which the date should be converted.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="context"/>, <paramref name="originalFormat"/>, or <paramref name="expression"/> is null or empty.
        /// </exception>
        public FormatDateExpression(Context context, string date, string originalFormat, string outputFormat)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrWhiteSpace(originalFormat)) throw new ArgumentNullException(nameof(originalFormat));
            if (string.IsNullOrWhiteSpace(outputFormat)) throw new ArgumentNullException(nameof(outputFormat));
            if (date == null) throw new ArgumentNullException(nameof(date));

            Context = context;
            OriginalFormat = originalFormat;
            OutputFormat = outputFormat;
            Date = date;
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
    public class SwitchExpression : IExpression
    {
        /// <summary>
        /// Gets the input value used as a key for the mapping.
        /// </summary>
        public string Input { get; private set; }

        /// <summary>
        /// Gets the dictionary that maps input values to output values.
        /// </summary>
        public Dictionary<string, JValue> Mapping { get; private set; }

        /// <summary>
        /// Gets the default value to return if the input value is not found in the mapping.
        /// </summary>
        public string Default { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchExpression"/> class.
        /// </summary>
        /// <param name="input">The input value used as a key for the mapping.</param>
        /// <param name="mapping">The dictionary that maps input values to output values.</param>
        /// <param name="default">The default value to return if the input value is not found in the mapping.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="input"/>, <paramref name="mapping"/>, or <paramref name="default"/> is null.
        /// </exception>
        public SwitchExpression(string input, Dictionary<string, JValue> mapping, string @default)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            Default = @default ?? throw new ArgumentNullException(nameof(@default));
        }

        /// <summary>
        /// Uses the input value as a key to look up a value in the mapping dictionary,
        /// and returns the corresponding value as a <see cref="JValue"/>,
        /// or the default value as a <see cref="JValue"/> if the key is not found.
        /// </summary>
        /// <returns>
        /// The <see cref="JValue"/> corresponding to the input value, or the default value if the input value is not found in the mapping.
        /// </returns>
        public JValue GetValue()
        {
            var inputValue = Input.ToString();
            if (inputValue != null && Mapping.TryGetValue(inputValue, out var mappedValue))
            {
                return new JValue(mappedValue);
            }
            return new JValue(Default);
        }
    }
}
