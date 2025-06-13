using HandlebarsDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Model
{
    /// <summary>
    /// Operator are node in json that are used to get data from context and return a new JToken
    /// </summary>
    public interface IOperator
    {
        //[JsonProperty("@operator")]
        //string Operator { get; }

        JToken Evaluate(TemplateContext templateContext);


    }

    public class EachOperator : IOperator
    {
        public const string Operator = "each";

        [JsonProperty("@path")]
        public string Path { get; private set; }

        [JsonProperty("@filter")]
        public string? Filter { get; private set; }

        [JsonProperty("@element")]
        public JToken Element { get; private set; }

        public EachOperator(string path,string? filter, JToken element)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Filter = filter; // Filter can be null, so no need for ArgumentNullException
        }

        public JToken Evaluate(TemplateContext templateContext)
        {
            // has default use the local context
            var tokenArray = templateContext.LocalContext.SelectTokens(GetJsonPathExpressionWithFilter(Path, Filter));

            if (tokenArray == null || tokenArray.Count() == 0)
            {
                return new JArray(); // Return an empty array directly
            }
            else
            {
                var newArray = new JArray();
                foreach (var item in tokenArray)
                {
                    var element = new ContextElement(item,Element);
                    newArray.Add(JToken.FromObject(element));
                }
                return newArray; // Return the new array directly
            }
        }
        private static string GetJsonPathExpressionWithFilter(string path, string? filter)
        {
            if (string.IsNullOrEmpty(filter))
            {
                return $"{path}[*]"; // No filter, return the path as is
            }
            else
            {
                // Assuming the filter is a valid JSONPath expression, append it to the path
                return $"{path}[?({filter})]"; // Example of appending a filter condition
            }
        }

    }


    public class SumOperator : IOperator
    {
        public const string Operator = "sum";

        [JsonProperty("@path")]
        public string Path { get; private set; }

        [JsonProperty("@key")]
        public string Key { get; private set; }

        public SumOperator(string path, string key)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Return the sum of the values of the key in the array at the path in the context in float. 
        /// TODO: extend to support int
        /// </summary>
        /// <param name="templateContext"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public JToken Evaluate(TemplateContext templateContext)
        {
            // has default use the local context
            var tokenArray = templateContext.LocalContext.SelectToken(Path);
            if (tokenArray == null || tokenArray.Count() == 0)
            {
                return JToken.FromObject(0d); // Return an empty array directly
            }
            else
            {

                var sum = 0d;
                foreach (var item in tokenArray)
                {
                    var tokenValue = item.SelectToken(Key,errorWhenNoMatch:false);
                    if (tokenValue != null)
                    {
                        sum += (tokenValue.Type) switch
                        {
                            JTokenType.Integer => item.Value<int>(Key),
                            JTokenType.Float => item.Value<double>(Key),
                            _ => throw new InvalidOperationException($"Unsupported token type for summation: {tokenValue.Type}"),
                        };
                    }
                   
                }
                return JToken.FromObject(sum);
            }

        }
    }

    /// <summary>
    /// Create a int value on the context from a string value at the path. If the value is not a valid int, it will throw an exception.
    /// </summary>
    public class FloatOperator : IOperator
    {
        public const string Operator = "float";

        [JsonProperty("@path")]
        public string Path { get; private set; }

        [JsonProperty("@default")]
        public float? Default { get; private set; }

        public FloatOperator(string path, float? @default)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Default = @default;
        }

        public JToken Evaluate(TemplateContext templateContext)
        {
            var tokenStringValue = templateContext.LocalContext.SelectToken(Path);

            if (tokenStringValue is JValue jValue && jValue.Type == JTokenType.String)
            {
                if (float.TryParse(jValue.Value<string>(), out float intValue))
                {
                    return new JValue(intValue);
                }
            }
            return Default ?? throw new ArgumentException($"Field with path '{Path}' cannot be null or empty. Expected an integer value.");
        }

        }
    }
