using HandlebarsDotNet;
using JsonToLLM.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Operators
{
    /// <summary>
    /// Defines a contract for operators that transform or extract data from a <see cref="TemplateContext"/>.
    /// Implementations evaluate against the provided <see cref="TemplateContext"/> and return an <see cref="OperatorResult"/>.
    /// </summary>
    public interface IOperator
    {
        /// <summary>
        /// Evaluate the operator using the provided template context.
        /// Implementations may return a new JSON value and optionally a new <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">Context used to resolve values for the operator.</param>
        /// <returns>An <see cref="OperatorResult"/> containing the produced JSON and optional context.</returns>
        OperatorResult Evaluate(TemplateContext templateContext);
    }

    /// <summary>
    /// Result produced by an <see cref="IOperator"/> evaluation.
    /// Contains the JSON produced by the operator and optionally a new <see cref="TemplateContext"/> to use downstream.
    /// </summary>
    public class OperatorResult
    {
        /// <summary>
        /// Creates an <see cref="OperatorResult"/> containing the supplied JSON and no context change.
        /// </summary>
        /// <param name="json">JSON payload produced by the operator. Cannot be null.</param>
        /// <returns>New <see cref="OperatorResult"/> instance.</returns>
        public static OperatorResult Create(JToken json)
        {
            return new OperatorResult(json, null);
        }

        /// <summary>
        /// Creates an <see cref="OperatorResult"/> containing the supplied JSON and a new template context.
        /// Use this when an operator also wants to change the active context for subsequent processing.
        /// </summary>
        /// <param name="json">JSON payload produced by the operator. Cannot be null.</param>
        /// <param name="templateContext">New <see cref="TemplateContext"/> to attach to the result.</param>
        /// <returns>New <see cref="OperatorResult"/> instance.</returns>
        public static OperatorResult CreateWithNewContext(JToken json, TemplateContext templateContext)
        {
            return new OperatorResult(json, templateContext);
        }

        /// <summary>
        /// JSON produced by the operator.
        /// </summary>
        public JToken Json { get; private set; }

        /// <summary>
        /// Optional template context that should become active after this operator.
        /// When null, the current context remains unchanged.
        /// </summary>
        public TemplateContext? TemplateContext { get; private set; }

        private OperatorResult(JToken json, TemplateContext? templateContext)
        {
            Json = json ?? throw new ArgumentNullException(nameof(json));
            TemplateContext = templateContext;
        }
    }

    /// <summary>
    /// Iterates over a collection found in the current local context and produces an array of per-item context objects.
    /// </summary>
    /// <remarks>
    /// EachOperator selects tokens from <see cref="TemplateContext.LocalContext"/> using a JSONPath expression specified by <see cref="Path"/>.
    /// For every matched item it creates a new <c>ContextElement</c> pairing the item (the new local context for that element)
    /// with the configured <see cref="Element"/> template. The operator returns a <see cref="JArray"/> where each element is the
    /// serialized <c>ContextElement</c> for a matched item.
    ///
    /// Typical use:
    /// {
    ///   "@operator": "each",
    ///   "@path": "$.items",
    ///   "@filter": "@.price > 10", // optional, JSONPath predicate syntax
    ///   "@element": { /* template applied per item */ }
    /// }
    ///
    /// Notes:
    /// - If no items are matched the operator returns an empty <see cref="JArray"/>.
    /// - The operator does not change the active <see cref="TemplateContext"/>; it returns data only (no new TemplateContext is attached).
    /// - <see cref="Filter"/> is appended into the JSONPath expression as a predicate: resulting path is either "<c>{Path}[*]</c>" or "<c>{Path}[?({Filter})]</c>".
    /// - <see cref="ContextElement"/> is used to encapsulate the item + element template so downstream processing can treat each item with its own local context.
    /// </remarks>
    public class EachOperator : IOperator
    {
        public const string Operator = "each";

        /// <summary>
        /// JSONPath to the collection to iterate. Required.
        /// Example: <c>"$.items"</c>.
        /// </summary>
        [JsonProperty("@path")]
        public string Path { get; private set; }

        /// <summary>
        /// Optional JSONPath predicate to filter items. When provided it will be inserted inside the array predicate:
        /// resulting expression becomes <c>{Path}[?({Filter})]</c>.
        /// Example: <c>"@.price &gt; 10"</c>.
        /// </summary>
        [JsonProperty("@filter")]
        public string? Filter { get; private set; }

        /// <summary>
        /// Template element that will be paired with each matched item to create a <c>ContextElement</c>.
        /// Required.
        /// </summary>
        [JsonProperty("@element")]
        public JToken Element { get; private set; }

        /// <summary>
        /// Creates an EachOperator.
        /// </summary>
        /// <param name="path">JSONPath to the array or collection to iterate. Cannot be null.</param>
        /// <param name="filter">Optional filter predicate in JSONPath predicate syntax. Can be null.</param>
        /// <param name="element">Template element to use for each item. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="element"/> is null.</exception>
        public EachOperator(string path, string? filter, JToken element)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Filter = filter; // Filter can be null, so no need for ArgumentNullException
        }

        /// <summary>
        /// Evaluates the operator against the provided <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The active template context containing <see cref="TemplateContext.LocalContext"/> used as the selection root.</param>
        /// <returns>
        /// An <see cref="OperatorResult"/> whose <see cref="OperatorResult.Json"/> is a <see cref="JArray"/>. Each item in the array is a serialized
        /// <c>ContextElement</c> representing one matched element together with the configured <see cref="Element"/>.
        /// If no items are matched an empty <see cref="JArray"/> is returned.
        /// </returns>
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

        /// <summary>
        /// Builds the JSONPath expression used to select items: either "<c>{path}[*]</c>" when no filter is present,
        /// or "<c>{path}[?({filter})]</c>" when a filter is provided.
        /// </summary>
        /// <param name="path">Base JSONPath to the collection.</param>
        /// <param name="filter">Optional predicate expression to use inside the array filter.</param>
        /// <returns>Full JSONPath expression used for selection.</returns>
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

    /// <summary>
    /// Computes the sum of numeric values found in objects inside an array at the specified JSONPath.
    /// </summary>
    /// <remarks>
    /// - The operator locates the array using <see cref="Path"/> in <see cref="TemplateContext.LocalContext"/>.
    /// - For each array element it attempts to read the token at <see cref="Key"/> and supports integer and float types.
    /// - If no array or no matching elements are found, the operator returns 0.0 as a JSON number.
    /// - Non-numeric token types encountered at <see cref="Key"/> will result in an <see cref="InvalidOperationException"/>.
    /// </remarks>
    public class SumOperator : IOperator
    {
        public const string Operator = "sum";

        /// <summary>
        /// JSONPath to the array to sum values from. Required.
        /// </summary>
        [JsonProperty("@path")]
        public string Path { get; private set; }

        /// <summary>
        /// Property name or JSONPath inside each array element whose numeric value will be summed.
        /// </summary>
        [JsonProperty("@key")]
        public string Key { get; private set; }

        /// <summary>
        /// Creates a SumOperator.
        /// </summary>
        /// <param name="path">JSONPath to the array. Cannot be null.</param>
        /// <param name="key">Key or JSONPath within each element to sum. Cannot be null.</param>
        public SumOperator(string path, string key)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Evaluates the operator and returns the numeric sum as a JSON number (<c>double</c>).
        /// </summary>
        /// <param name="templateContext">Context used to resolve the target array.</param>
        /// <returns><see cref="OperatorResult"/> containing the sum as a JSON number.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a non-numeric token is encountered for the configured <see cref="Key"/>.</exception>
        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            // has default use the local context
            var tokenArray = templateContext.LocalContext.SelectToken(Path);
            if (tokenArray == null || tokenArray.Count() == 0)
            {
                return OperatorResult.Create(JToken.FromObject(0d)); // Return zero when nothing to sum
            }
            else
            {
                var sum = 0d;
                foreach (var item in tokenArray)
                {
                    var tokenValue = item.SelectToken(Key, errorWhenNoMatch: false);
                    if (tokenValue != null)
                    {
                        sum += tokenValue.Type switch
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
    /// Converts a string value found at a path into a float (single-precision) value.
    /// </summary>
    /// <remarks>
    /// - Looks up the token at <see cref="Path"/> in <see cref="TemplateContext.LocalContext"/>.
    /// - If the token is a string and can be parsed as a float, the parsed value is returned.
    /// - When parsing fails or the token is not a string, the configured <see cref="Default"/> value is returned.
    /// </remarks>
    public class FloatOperator : IOperator
    {
        public const string Operator = "float";

        /// <summary>
        /// JSONPath to the value to convert.
        /// </summary>
        [JsonProperty("@path")]
        public string Path { get; private set; }

        /// <summary>
        /// Default float value to return when conversion fails or token does not exist.
        /// </summary>
        [JsonProperty("@default")]
        public float? Default { get; private set; }

        /// <summary>
        /// Creates a FloatOperator.
        /// </summary>
        /// <param name="path">JSONPath to the string value. Cannot be null.</param>
        /// <param name="default">Default value returned when conversion fails.</param>
        public FloatOperator(string path, float? @default)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Default = @default;
        }

        /// <summary>
        /// Evaluates the operator returning a JSON number if conversion succeeds, otherwise returns the configured default (or null).
        /// </summary>
        /// <param name="templateContext">Template context used to resolve the value.</param>
        /// <returns><see cref="OperatorResult"/> containing a <see cref="JValue"/> with the parsed float or the default value.</returns>
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
    /// Patches a target JObject found in the current local context by applying a sequence of modifications.
    /// </summary>
    /// <remarks>
    /// ObjectPatchOperator is a non-destructive operator that reads the JObject at <see cref="Path"/> from the current
    /// <see cref="TemplateContext.LocalContext"/>, performs modifications on a deep-cloned copy, and returns the cloned/modified object.
    ///
    /// The modifications are applied in the following deterministic order:
    ///  1. Add missing properties from <see cref="AddIfNull"/> (only when the property does not already exist).
    ///  2. Add or update properties from <see cref="AddOrUpdate"/> (overwrite existing values).
    ///  3. Remove properties listed in <see cref="RemoveKeys"/>.
    ///  4. Reorder properties according to <see cref="OrderKeys"/>.
    ///
    /// Behavior notes:
    /// - If the token found at <see cref="Path"/> is not a <see cref="JObject"/>, the operator returns that token unchanged.
    /// - If no token is found at <see cref="Path"/>, a <see cref="JValue.CreateNull"/> is used and returned.
    /// - The operator never mutates the original object from the template context; it returns a cloned and patched JObject.
    /// - All JSON values for upsert/add operations come from the provided <see cref="JObject"/> payloads and are assigned directly.
    ///
    /// Example usage in template JSON:
    /// {
    ///   "@operator": "objectpatch",
    ///   "@path": "$.document",
    ///   "@addIfNull": { "status": "new" },
    ///   "@addOrUpdate": { "count": 5 },
    ///   "@removeKeys": [ "temporary" ],
    ///   "@orderKeys": [ { "@key":"id","@index":0 }, { "@key":"status","@index":1 } ]
    /// }
    /// </remarks>
    public class ObjectPatchOperator : IOperator
    {
        public const string Operator = "objectpatch";

        /// <summary>
        /// JSONPath to the target object inside the local context. Defaults to the root ("$").
        /// </summary>
        [JsonProperty("@path")]
        public string Path { get; private set; } = "$"; // Default to root object

        /// <summary>
        /// Properties to add only when they do not exist on the target object.
        /// Keys are property names, values are the JSON value to add.
        /// </summary>
        [JsonProperty("@addIfNull")]
        public JObject? AddIfNull { get; private set; } = null;

        /// <summary>
        /// Properties to add or overwrite on the target object. Existing properties are replaced.
        /// Keys are property names, values are the JSON value to set.
        /// </summary>
        [JsonProperty("@addOrUpdate")]
        public JObject? AddOrUpdate { get; private set; } = null;

        /// <summary>
        /// List of property names to remove from the target object if present.
        /// </summary>
        [JsonProperty("@removeKeys")]
        public List<string>? RemoveKeys { get; private set; } = null;

        /// <summary>
        /// Desired ordering for properties. Each entry specifies a property key and the zero-based index
        /// where that property should appear after reordering. Properties not listed remain in their relative order.
        /// </summary>
        [JsonProperty("@orderKeys")]
        public List<OrderKeyModel>? OrderKeys { get; private set; } = null;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectPatchOperator"/>.
        /// </summary>
        public ObjectPatchOperator()
        {
        }

        /// <summary>
        /// Applies the configured patch operations to the object found at <see cref="Path"/> within <paramref name="templateContext"/>'s local context.
        /// </summary>
        /// <param name="templateContext">Template context whose <see cref="TemplateContext.LocalContext"/> is used to locate the target object.</param>
        /// <returns>
        /// An <see cref="OperatorResult"/> wrapping:
        /// - a patched <see cref="JObject"/> (deep clone with modifications) when a JObject is found at <see cref="Path"/>, or
        /// - the original token found at <see cref="Path"/> (which may be null or another token type) when the target is not an object.
        /// </returns>
        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            // add property if is null 
            var jtoken = templateContext.LocalContext.SelectToken(Path) ?? JValue.CreateNull();

            if (jtoken is JObject jObject)
            {
                var clonedObject = (JObject)jObject.DeepClone(); // Clone the object to avoid modifying the original context directly

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
                if (RemoveKeys != null && RemoveKeys.Count > 0)
                {
                    foreach (var key in RemoveKeys)
                    {
                        clonedObject.Remove(key);
                    }
                }
                if (OrderKeys != null && OrderKeys.Count > 0)
                {
                    clonedObject = HandleOrderKeys(clonedObject, OrderKeys);
                }
                return OperatorResult.Create(clonedObject);
            }
            return OperatorResult.Create(jtoken); // If the token is not an object, return it as is (could be null or another type)
        }

        /// <summary>
        /// Reorders properties of the provided <see cref="JObject"/> according to the <paramref name="reorderList"/>.
        /// Entries in <paramref name="reorderList"/> move the matching property to the specified zero-based index.
        /// If a property from the list is not present it is ignored. The returned JObject preserves all original properties,
        /// but with the new ordering applied. Indices are clamped to valid bounds.
        /// </summary>
        /// <param name="original">Original object to reorder (not mutated).</param>
        /// <param name="reorderList">List of <see cref="OrderKeyModel"/> entries specifying target positions.</param>
        /// <returns>A new <see cref="JObject"/> instance with properties ordered per the reorder list.</returns>
        private static JObject HandleOrderKeys(JObject original, List<OrderKeyModel> reorderList)
        {
            // Lavora su una lista di coppie (chiave, valore)
            var props = new List<JProperty>(original.Properties());

            foreach (var elem in reorderList)
            {
                var prop = elem.Key;
                var index = elem.Index;

                // Trova la proprietà
                var current = props.Find(p => p.Name == prop);
                if (current == null)
                    continue;

                // Rimuovila e reinseriscila alla posizione indicata (bounded)
                props.Remove(current);
                var boundedIndex = Math.Clamp(index, 0, props.Count);
                props.Insert(boundedIndex, current);
            }

            // Ricrea un nuovo JObject nell’ordine desiderato
            var newObj = new JObject();
            foreach (var p in props)
                newObj.Add(p);

            return newObj;
        }
    }

    /// <summary>
    /// Model that describes a desired property ordering for <see cref="ObjectPatchOperator"/>.
    /// </summary>
    public class OrderKeyModel
    {
        /// <summary>
        /// Property name to move.
        /// </summary>
        [JsonProperty("@key")]
        public string Key { get; set; }

        /// <summary>
        /// Zero-based target index where the property should appear after reordering.
        /// </summary>
        [JsonProperty("@index")]
        public int Index { get; set; }
    }

    /// <summary>
    /// Creates a new <see cref="TemplateContext"/> based on the provided element and context token.
    /// </summary>
    /// <remarks>
    /// The operator returns an <see cref="OperatorResult"/> that includes both the element JSON and a new
    /// <see cref="TemplateContext"/> created from the provided context token. This allows downstream processing to
    /// switch to the supplied local context for subsequent operators/templates.
    /// </remarks>
    public class ContextOperator : IOperator
    {
        public const string Operator = "context";

        [JsonProperty("@context")]
        public JToken Context { get; private set; } // Default to root object

        [JsonProperty("@element")]
        public JToken Element { get; private set; }

        /// <summary>
        /// Creates a ContextOperator.
        /// </summary>
        /// <param name="element">Element JSON that will be returned as the operator payload.</param>
        /// <param name="context">Context token used to create the new <see cref="TemplateContext"/>.</param>
        public ContextOperator(JToken element, JToken context)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Returns the element together with a new <see cref="TemplateContext"/> built from <see cref="Context"/>.
        /// </summary>
        /// <param name="templateContext">Current template context (used only to access global context when creating the new context).</param>
        /// <returns><see cref="OperatorResult"/> containing the element JSON and the newly created context.</returns>
        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            return OperatorResult.CreateWithNewContext(
                Element,
                TemplateContext.Create(templateContext.GlobalContext, Context));
        }
    }

    /// <summary>
    /// Selects and returns a JToken from the local context using a JSONPath expression.
    /// </summary>
    /// <remarks>
    /// Useful to project a full object or a primitive value from the current local context into the output.
    /// If the path does not resolve, the configured <see cref="Default"/> value is returned.
    /// </remarks>
    public class ElementOperator : IOperator
    {
        public const string Operator = "element";

        /// <summary>
        /// JSONPath to select from the local context. Defaults to the root ("$").
        /// </summary>
        [JsonProperty("@path")]
        public string Path { get; set; } = "$";

        /// <summary>
        /// Default token returned when <see cref="Path"/> does not resolve.
        /// </summary>
        [JsonProperty("@default")]
        public JToken Default { get; set; } = JValue.CreateNull();// Default to null

        /// <summary>
        /// Creates an ElementOperator.
        /// </summary>
        public ElementOperator()
        {
        }

        /// <summary>
        /// Evaluates the operator and returns the selected token or the configured default.
        /// </summary>
        /// <param name="templateContext">Template context used to resolve the path.</param>
        /// <returns><see cref="OperatorResult"/> wrapping the selected token or default.</returns>
        public OperatorResult Evaluate(TemplateContext templateContext)
        {
            var token = templateContext.LocalContext.SelectToken(Path) ?? Default;
            return OperatorResult.Create(token);
        }
    }
}
