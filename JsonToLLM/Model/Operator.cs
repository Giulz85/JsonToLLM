using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Model
{
    public interface IOperator
    {
    }

    public class EachOperator : IOperator
    {
        public Context Context { get; private set; }
        public string Path { get; private set; }
        public JToken Value { get; private set; }

        public IExpressionTrasformer Trasfomer { get; set; }
        public EachOperator(string path, JToken value, Context context)
        {
            Path = path;
            Value = value;
            Context = context;
        }

        public JArray Execute()
        {
            var array = Context.LocalContext.SelectToken(Path) as JArray;
            if (array == null)
            {
                throw new InvalidOperationException($"The path '{Path}' does not point to a valid JArray in the local context.");
            }
            throw new NotImplementedException("The Execute method is not implemented yet. Please implement the logic to process the JArray using Trasfomer.");


        }


    }
}
