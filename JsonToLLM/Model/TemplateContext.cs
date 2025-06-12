using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Model
{
    public class TemplateContext
    {
        public static TemplateContext Create(JToken globalContext, JToken localContext) => 
            new TemplateContext(globalContext, localContext);

        public JToken GlobalContext { get; private set; }
        public JToken LocalContext { get; private set; }

        public TemplateContext(JToken globalContext, JToken localContext)
        {
            GlobalContext = globalContext;
            LocalContext = localContext;
        }
    }
}
