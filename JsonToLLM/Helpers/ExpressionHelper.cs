using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace JsonToLLM.Helpers;

/// <summary>
/// Provides helper methods for parsing and handling function-like expressions,
/// including extracting function names and arguments, splitting arguments with escape support,
/// and unescaping special characters.
/// </summary>
public class ExpressionHelper
{
    /// <summary>
    /// Regular expression used to match a function name and its arguments.
    /// </summary>
    private const string ContainsFunctionCallRegex = @"@(\w+)\s*\(((?:[^()@])*)\)";
    private const string ExactFunctionCallRegex = @"^@(\w+)\s*\((.*)\)$";

    /// <summary>
    /// Attempts to parse the function name and arguments from the input string.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <param name="functionName">The parsed function name, or the input if parsing fails.</param>
    /// <param name="arguments">The parsed arguments, or null if parsing fails.</param>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParseFunctionNameAndArguments(string input, 
        out string? functionName, out string?[]? arguments, out int? startIndex, out int? endIndex)
    {
        (functionName, arguments, startIndex, endIndex) = (null, null, null, null);
       
        var match = Regex.Match(input, ContainsFunctionCallRegex, RegexOptions.Compiled);
        if (!match.Success)
            return false;

        functionName = match.Groups[1].Value;
        startIndex = match.Groups[1].Index - 1;
        endIndex = match.Groups[2].Index + match.Groups[2].Length;
        arguments = SplitArguments(match.Groups[2].Value, '\\');

        return true;
    }

    /// <summary>
    /// Splits a function argument string into individual arguments, supporting escaped characters.
    /// </summary>
    /// <param name="input">The argument string to split.</param>
    /// <param name="escapeChar">The character used to escape special characters.</param>
    /// <returns>An array of argument strings.</returns>
    public static string?[]? SplitArguments(string input, char escapeChar)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var arguments = new List<string?>();
        var index = 0;
        var length = input.Length;

        while (index < length)
        {
            // Skip whitespace and commas
            while (index < length && (char.IsWhiteSpace(input[index]) || input[index] == ','))
                index++;

            if (index >= length)
                break;

            // Parse string literal
            if (input[index] == '"')
            {
                arguments.Add(ParseQuotedString(input, ref index, escapeChar));
                continue;
            }

            // Parse unquoted token
            var start = index;
            while (index < length && input[index] != ',')
                index++;

            var token = input[start..index].Trim();

            if (string.Equals(token, "null", StringComparison.OrdinalIgnoreCase))
                arguments.Add(null);

            else if (IsNumber(token))
                arguments.Add(token);

            else
                throw new FormatException($"Unrecognized token '{token}'");
        }

        return arguments.Count == 0 ? null : arguments.ToArray();
    }

    private static string ParseQuotedString(string input, ref int index, char escapeChar)
    {
        var sb = new StringBuilder();
        index++; // skip opening quote

        while (index < input.Length)
        {
            var c = input[index++];

            if (c == escapeChar && index < input.Length)
            {
                // Handle common escapes: \" \\ \n \t
                var next = input[index++];
                sb.Append(next switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => next
                });
            }
            else if (c == '"')
                break;
            
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsNumber(string token)
    {
        // Try int or double
        return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
               double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }



    /// <summary>
    /// Removes escape characters from a string, unless the string is a function.
    /// </summary>
    /// <param name="str">The string to unescape.</param>
    /// <param name="escapeChar">The escape character.</param>
    /// <returns>The unescaped string.</returns>
    public static string Unescape(string str, char escapeChar)
    {
        return !IsExactFunctionCall(str) ?
            Regex.Replace(str, $"\\{escapeChar}([\\{escapeChar}(),])", "$1") :
            str;
    }

    /// <summary>
    /// Determines if the given string represents a function (starts with '@').
    /// </summary>
    /// <param name="val">The string to check.</param>
    /// <returns>True if the string is a function; otherwise, false.</returns>
    public static bool IsExactFunctionCall(string val)
    {
        return Regex.IsMatch(val, ExactFunctionCallRegex);
    }

    /// <summary>
    /// Removes an escape character before a sharp ('@') at the start of the string.
    /// </summary>
    /// <param name="val">The string to unescape.</param>
    /// <param name="escapeChar">The escape character.</param>
    /// <returns>The unescaped string.</returns>
    public static string UnescapeSharp(string val, char escapeChar)
    {
        return Regex.Replace(val, $"^(\\s*)\\{escapeChar}(@)", "$1$2");
    }
}