using JsonToLLM.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM
{
    public interface IFactoryOperator
    {
        IOperator CreateOperator(string operatorName, JToken operatorTemplate);
    }
    public class FactoryOperator: IFactoryOperator
    {
        public IOperator CreateOperator(string operatorName, JToken operatorTemplate)
        {
            if (string.IsNullOrEmpty(operatorName))
            {
                throw new ArgumentException("Operator name cannot be null or empty.", nameof(operatorName));
            }
            if (operatorTemplate == null)
            {
                throw new ArgumentNullException(nameof(operatorTemplate), "Operator template cannot be null.");
            }
            if (operatorTemplate.Type != JTokenType.Object)
            {
                throw new ArgumentException(nameof(operatorTemplate), "Operator template has to be an object.");
            }
            return operatorName switch
            {
                EachOperator.Operator => operatorTemplate.ToObject<EachOperator>() ?? throw new InvalidOperationException("Failed to create EachOperator from template."),
                SumOperator.Operator => operatorTemplate.ToObject<SumOperator>() ?? throw new InvalidOperationException("Failed to create SumOperator from template."),
                FloatOperator.Operator => operatorTemplate.ToObject<FloatOperator>() ?? throw new InvalidOperationException("Failed to create FloatOperator from template."),
                ObjectPatchOperator.Operator => operatorTemplate.ToObject<ObjectPatchOperator>() ?? throw new InvalidOperationException("Failed to create IfNullOperator from template."),
                ContextOperator.Operator => operatorTemplate.ToObject<ContextOperator>() ?? throw new InvalidOperationException("Failed to create ContextOperator from template."),
                ElementOperator.Operator => operatorTemplate.ToObject<ElementOperator>() ?? throw new InvalidOperationException("Failed to create ElementOperator from template."),

                // Add more operators here as needed
                _ => throw new NotSupportedException($"Operator '{operatorName}' is not supported.")
            };
        }
    }
}
