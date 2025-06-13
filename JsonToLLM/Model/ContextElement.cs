using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonToLLM.Model
{
   
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
