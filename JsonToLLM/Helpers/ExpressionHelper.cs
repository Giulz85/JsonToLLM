using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonToLLM.Helpers
{


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
        //private const string FunctionAndArgumentsRegex = @"^\s*@(\w+)\s*\(([^)]*)\)\s*$";
        private const string FunctionAndArgumentsRegex = @"@(\w+)\s*\(([^()@]*)\)";


        /// <summary>
        /// Attempts to parse the function name and arguments from the input string.
        /// </summary>
        /// <param name="input">The input string to parse.</param>
        /// <param name="functionName">The parsed function name, or the input if parsing fails.</param>
        /// <param name="arguments">The parsed arguments, or null if parsing fails.</param>
        /// <returns>True if parsing was successful; otherwise, false.</returns>
        public static bool TryParseFunctionNameAndArguments(string input, out string functionName, out string? arguments, out int? startIndex,
            out int? endIndex)
        {
            var match = Regex.Match(input, FunctionAndArgumentsRegex, RegexOptions.Compiled);
            functionName = match.Success ? match.Groups[1].Value : input;
            startIndex = match.Success ? match.Groups[1].Index - 1: null; 
            endIndex = match.Success ? match.Groups[2].Index + match.Groups[2].Length : null;
            arguments = match.Success ? match.Groups[2].Value : null;
            return match.Success;
        }

        /// <summary>
        /// Splits a function argument string into individual arguments, supporting escaped characters.
        /// </summary>
        /// <param name="functionString">The argument string to split.</param>
        /// <param name="escapeChar">The character used to escape special characters.</param>
        /// <returns>An array of argument strings.</returns>
        public static string[] SplitArguments(string functionString, char escapeChar)
        {
            if (string.IsNullOrEmpty(functionString))
            {
                return new string[0];
            }

            List<string> arguments = new List<string>();
            int index = 0;

            int openBrackettCount = 0;
            int closebrackettCount = 0;
            bool isEscapedChar = false;

            for (int i = 0; i < functionString.Length; i++)
            {
                char currentChar = functionString[i];
                if (currentChar == escapeChar)
                {
                    isEscapedChar = !isEscapedChar;
                    continue;
                }
                if (currentChar == '(')
                {
                    if (!isEscapedChar) { openBrackettCount++; }
                    else { isEscapedChar = !isEscapedChar; }
                }
                else if (currentChar == ')')
                {
                    if (!isEscapedChar) { closebrackettCount++; }
                    else { isEscapedChar = !isEscapedChar; }
                }

                bool brackettOpen = openBrackettCount != closebrackettCount;
                if (currentChar == ',' && !brackettOpen)
                {
                    if (!isEscapedChar)
                    {
                        arguments.Add(Unescape(index != 0 ?
                            functionString.Substring(index + 1, i - index - 1) :
                            functionString.Substring(index, i), escapeChar));
                        index = i;
                    }
                    else { isEscapedChar = !isEscapedChar; }
                }
                else { isEscapedChar = false; }
            }

            arguments.Add(index > 0 ?
                Unescape(functionString.Substring(index + 1, functionString.Length - index - 1), escapeChar) :
                Unescape(functionString, escapeChar));

            return arguments.ToArray();
        }

        /// <summary>
        /// Removes escape characters from a string, unless the string is a function.
        /// </summary>
        /// <param name="str">The string to unescape.</param>
        /// <param name="escapeChar">The escape character.</param>
        /// <returns>The unescaped string.</returns>
        public static string Unescape(string str, char escapeChar)
        {
            return !IsFunction(str) ?
                Regex.Replace(str, $"\\{escapeChar}([\\{escapeChar}(),])", "$1") :
                str;
        }

        /// <summary>
        /// Determines if the given string represents a function (starts with '@').
        /// </summary>
        /// <param name="val">The string to check.</param>
        /// <returns>True if the string is a function; otherwise, false.</returns>
        public static bool IsFunction(string val)
        {
            return Regex.IsMatch(val, FunctionAndArgumentsRegex);
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
}
