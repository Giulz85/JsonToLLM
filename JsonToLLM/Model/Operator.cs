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
        [JsonProperty("@operator")]
        string Operator { get; }

        JToken Evaluate(TemplateContext templateContext);


    }

    public class EachOperator : IOperator
    {
        [JsonProperty("@operator")]
        public string Operator => "each";

        [JsonProperty("@path")]
        public string Path { get; private set; }

        [JsonProperty("@element")]
        public JToken Element { get; private set; }

        public EachOperator(string path, JToken element)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Element = element ?? throw new ArgumentNullException(nameof(element));
        }

        public JToken Evaluate(TemplateContext templateContext)
        {
            // has default use the local context
            var tokenArray = templateContext.LocalContext.SelectToken(Path);
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
    }

    //public class ContextOperator : IOperator
    //{
    //    [JsonProperty("@operator")]
    //    public string Operator => "context";

    //    [JsonProperty("@context")]
    //    public JToken Context { get; private set; }

    //    [JsonProperty("@element")]
    //    public JToken Element { get; private set; }

    //    public ContextOperator(JToken context, JToken element)
    //    {
    //        Context = context ?? throw new ArgumentNullException(nameof(context));
    //        Element = element ?? throw new ArgumentNullException(nameof(element));
    //    }

    //    public JToken Evaluate(TemplateContext templateContext)
    //    {
    //        // has default use the local context
    //        var tokenArray = templateContext.LocalContext.SelectTokens(Path);
    //        if (tokenArray == null || tokenArray.Count() == 0)
    //        {
    //            return new JArray(); // Return an empty array directly
    //        }
    //        else
    //        {
    //            var newArray = new JArray();
    //            foreach (var item in tokenArray)
    //            {
    //                var element = new ContextElement(item, Element);
    //                newArray.Add(JToken.FromObject(element));
    //            }
    //            return newArray; // Return the new array directly
    //        }

    //    }
    //}

    public class ContextElement
    {
        [JsonProperty("@type")]
        public string Type { get; } = "context"; 
        [JsonProperty("@context")]
        public JToken Context { get; set; }
        [JsonProperty("@element")]
        public JToken Element { get; set; } // Optional, can be null

        public ContextElement(JToken? context, JToken element)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Element = element ?? throw new ArgumentNullException(nameof(element)); 
        }
    }
}
