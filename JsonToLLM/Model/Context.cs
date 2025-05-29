using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Model
{
    public class Context
    {
        public static Context Create(JToken globalContext, JToken localContext) => 
            new Context(globalContext, localContext);

        public JToken GlobalContext { get; private set; }
        public JToken LocalContext { get; private set; }

        public Context(JToken globalContext, JToken localContext)
        {
            GlobalContext = globalContext;
            LocalContext = localContext;
        }
    }
}
