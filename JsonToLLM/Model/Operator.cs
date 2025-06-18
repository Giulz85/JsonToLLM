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

        OperatorResult Evaluate(TemplateContext templateContext);


    }

    public class OperatorResult
    {
        public static OperatorResult Create(JToken json)
        {
            return new OperatorResult(json, null);
        }

        public static OperatorResult CreateWithNewContext(JToken json, TemplateContext templateContext)
        {
            return new OperatorResult(json, templateContext);
        }

        public JToken Json { get; private set; }

        public TemplateContext? TemplateContext { get; private set; }

        private OperatorResult(JToken json, TemplateContext? templateContext)
        {
            Json = json ?? throw new ArgumentNullException(nameof(json));
            TemplateContext = templateContext; 
        }


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

        public EachOperator(string path, string? filter, JToken element)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Filter = filter; // Filter can be null, so no need for ArgumentNullException
        }

        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            // has default use the local context
            var tokenArray = templateContext.LocalContext.SelectTokens(GetJsonPathExpressionWithFilter(Path, Filter));

            if (tokenArray == null || tokenArray.Count() == 0)
            {
                return OperatorResult.Create(new JArray()); // Return an empty array directly
            }
            else
            {
                var newArray = new JArray();
                foreach (var item in tokenArray)
                {
                    // -It is used a fake object because in this way it is possible return a different context for each element in the array
                    // -A solution to overccome can be returna a list of JValue with its context but this update has a lot of refactoring effort
                    var element = new ContextElement(item, Element);
                    newArray.Add(JToken.FromObject(element));
                }
                return OperatorResult.Create(newArray); // Return the new array directly
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
        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            // has default use the local context
            var tokenArray = templateContext.LocalContext.SelectToken(Path);
            if (tokenArray == null || tokenArray.Count() == 0)
            {
                return OperatorResult.Create(JToken.FromObject(0d)); // Return an empty array directly
            }
            else
            {

                var sum = 0d;
                foreach (var item in tokenArray)
                {
                    var tokenValue = item.SelectToken(Key, errorWhenNoMatch: false);
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
                return OperatorResult.Create(JToken.FromObject(sum));
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

        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            var tokenStringValue = templateContext.LocalContext.SelectToken(Path);

            if (tokenStringValue is JValue jValue && jValue.Type == JTokenType.String)
            {
                if (float.TryParse(jValue.Value<string>(), out float intValue))
                {
                    return OperatorResult.Create(new JValue(intValue));
                }
            }
            return OperatorResult.Create(Default);
        }

    }

    /// <summary>
    /// Allow to patch an object in the context. It can add a new property if it is null, update an existing property or remove a property.
    /// </summary>
    public class ObjectPatchOperator : IOperator
    {
        public const string Operator = "objectpatch";

        /// <summary>
        /// Path json object where check if key is null
        /// </summary>
        [JsonProperty("@path")]
        public string Path { get; private set; } = "$"; // Default to root object

        [JsonProperty("@addIfNull")]
        public JObject? AddIfNull { get; private set; } = null;

        [JsonProperty("@addOrUpdate")]
        public JObject? AddOrUpdate { get; private set; } = null;

        [JsonProperty("@removeKeys")]
        public List<string>? RemoveKeys { get; private set; } = null;


        public ObjectPatchOperator()
        {
        }

        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            // add property if is null 
            var jtoken = templateContext.LocalContext.SelectToken(Path) ?? JValue.CreateNull();

            if (jtoken is JObject jObject)
            {
                var clonedObject = (JObject)jObject.DeepClone(); // Clone the object to avoid modifying the original context directly
                //if (!jobject.TryGetValue(Key,out JToken? value))
                //{
                //    // If the key does not exist, add the element to the object
                //    jobject[Key] = Element;
                //    //templateContext.UpdateLocalContext(jobject);
                //    return OperatorResult.Create(jobject, templateContext) ; // Return the modified object
                //}
                //else if (value.Type == JTokenType.Null)
                //{
                //    // If the key exists but is null, replace it with the element
                //    jobject[Key] = Element;
                //    //templateContext.UpdateLocalContext(jobject);
                //    return OperatorResult.Create(jobject, templateContext); // Return the modified object
                //}
                //else
                //{
                //    // If the key exists and is not null, return the original object
                //    return OperatorResult.Create(jobject, templateContext);
                //}

                //}
                // Upsert 
                if (AddIfNull != null && AddIfNull.Count > 0)
                {

                    foreach (var kvp in AddIfNull)
                    {
                        if (!jObject.ContainsKey(kvp.Key))
                        {
                            clonedObject[kvp.Key] = kvp.Value;
                        }
                    }

                }
                if (AddOrUpdate != null && AddOrUpdate.Count > 0)
                {
                    foreach (var kvp in AddOrUpdate)
                    {
                        clonedObject[kvp.Key] = kvp.Value;
                    }
                }
                if(RemoveKeys != null && RemoveKeys.Count > 0)
                {
                    foreach (var key in RemoveKeys)
                    {
                        clonedObject.Remove(key);
                    }
                }
                return OperatorResult.Create(clonedObject);
            }

            return OperatorResult.Create(jtoken); // If the token is not an object, return it as is (could be null or another type)
        }

    }


    /// <summary>
    /// Create a new Context based on the current context and the specified element.
    /// </summary>
    public class ContextOperator : IOperator
    {
        public const string Operator = "context";

        [JsonProperty("@context")]
        public JToken Context { get; private set; } // Default to root object

        [JsonProperty("@element")]
        public JToken Element { get; private set; }

        public ContextOperator(JToken element, JToken context)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            return OperatorResult.CreateWithNewContext(Element,TemplateContext.Create(templateContext.GlobalContext, Context));
        }

    }


    /// <summary>
    /// Create a new Context based on the current context and the specified element.
    /// </summary>
    public class ElementOperator : IOperator
    {
        public const string Operator = "element";

        [JsonProperty("@path")]
        public string Path { get; private set; } = "$";

        [JsonProperty("@default")]
        public JToken Default { get; private set; } = JValue.CreateNull();// Default to null

        public ElementOperator()
        {

        }

        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            var token = templateContext.LocalContext.SelectToken(Path) ?? Default;
            return OperatorResult.Create(token);
        }

    }


    /// <summary>
    /// Traform JToken selected by path in a new JToken specified in Element
    /// </summary>
    //public class CompositeOperator : IOperator
    //{
    //    public const string Operator = "composite";

    //    public List<IOperator> Operators { get; set; }


    //    public CompositeOperator()
    //    {

    //    }

    //    public JToken Evaluate(TemplateContext templateContext)
    //    {
    //        JToken result = JValue.CreateNull();
    //        foreach (var op in Operators)
    //        {

    //            var evaluatedToken = op.Evaluate(templateContext);
    //            if (evaluatedToken.Type != JTokenType.Null)
    //            {
    //                result = evaluatedToken; // Update result with the last evaluated token
    //            }
    //        }
    //        return result;

    //    }

    //}

}
