using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;

using Newtonsoft.Json;

using Swashbuckle.Application;
using Swashbuckle.Swagger;

namespace WebApiSwaggerGenerator
{
    public class SwaggerJsonGenerator
    {
        public string GenerateSwaggerJson(Assembly assembly, string apiVersion = "v1", string apiTitle = null, SwaggerGeneratorOptions options = null)
        {
            var httpConfig = new HttpConfiguration();

            if (apiTitle == null)
            {
                apiTitle = assembly.GetName().Name.Replace(".", "");
            }

            if (options == null)
            {
                options = new SwaggerGeneratorOptions(
                    schemaIdSelector: (type) => type.FriendlyId(),
                    conflictingActionsResolver: (apiDescriptions) => apiDescriptions.First(),
                    operationFilters: new[] { new StandardOperationFilter() },
                    schemaFilters: new[] { new StandardSchemaFilter() });
            }

            ExecuteConfigFromAssembly(assembly, httpConfig);
            httpConfig.EnsureInitialized();

            var swaggerProvider = new SwaggerGenerator(
                httpConfig.Services.GetApiExplorer(),
                httpConfig.Formatters.JsonFormatter.SerializerSettings,
                new Dictionary<string, Info> { { apiVersion, new Info { version = apiVersion, title = apiTitle } } },
                options);

            var swaggerDoc = swaggerProvider.GetSwagger("http://tempuri.org/", apiVersion);

            return JsonConvert.SerializeObject(
                swaggerDoc,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new JsonConverter[] { new VendorExtensionsConverter() }
                }
            );
        }

        public void ExecuteConfigFromAssembly(Assembly assembly, HttpConfiguration config)
        {

            var configClass = assembly.GetTypes().FirstOrDefault(t => t.Name == "WebApiConfig");

            var registerMethod = configClass.GetMethods()
                                            .FirstOrDefault(m => m.Name == "Register" &&
                                                                 m.ReturnType == typeof(void) &&
                                                                 m.GetParameters().Length == 1 &&
                                                                 m.GetParameters().First().ParameterType.FullName == typeof(HttpConfiguration).FullName);
            
            if (registerMethod == null)
            {
                throw new InvalidOperationException($"Assembly {assembly.FullName} does not contain a WebApiConfig.Register method.");
            }

            registerMethod.Invoke(null, new object[] { config });
        }
    }
}