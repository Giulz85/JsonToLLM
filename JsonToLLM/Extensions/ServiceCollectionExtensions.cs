using JsonToLLM.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string JsonToLLMSectionName = "JsonToLLM";

        public static IServiceCollection AddJsonToLLM(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JsonToLLMSettings>(configuration.GetSection(JsonToLLMSectionName));

            services.TryAdd(ServiceDescriptor.Transient<IExpressionEngine, ExpressionEngine>());
            services.TryAdd(ServiceDescriptor.Transient<IOperatorTrasformer, OperatorTrasformer>());
            services.TryAdd(ServiceDescriptor.Transient<IJsonToLLMTrasformer, JsonToLLMTrasformer>());
          
            return services;
        }

       
    }
}
