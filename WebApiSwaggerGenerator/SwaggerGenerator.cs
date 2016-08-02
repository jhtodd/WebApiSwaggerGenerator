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
        /// <summary>
        /// Generates a swagger.json file for the specified WebAPI assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly for which to generate a swagger.json file.
        /// </param>
        /// <param name="apiVersion">
        /// The version string that should appear in the swagger.json file. The default is "v1".
        /// </param>
        /// <param name="apiTitle">
        /// The title of the API that should appear in the swagger.json file. The default is the name of <paramref name="assembly" />.
        /// </param>
        /// <param name="options">
        /// The <see cref="SwaggerGeneratorOptions" /> that should be used to generate the swagger.json file.
        /// </param>
        /// <returns>
        /// A string containing the contents of the swagger.json file.
        /// </returns>
        public string GenerateSwaggerJson(Assembly assembly, string apiVersion = "v1", string apiTitle = null, SwaggerGeneratorOptions options = null)
        {
            // Create an in-memory configuration. We'll pass this into the WebApiConfig.Register method of the specified
            // assembly to gain information about the controllers, routes, and other API settings of the assembly.
            var httpConfig = new HttpConfiguration();

            if (apiTitle == null)
            {
                apiTitle = assembly.GetName().Name.Replace(".", "");
            }

            if (options == null)
            {
                options = new SwaggerGeneratorOptions(
                    schemaIdSelector: (type) => type.FriendlyId(true),
                    conflictingActionsResolver: (apiDescriptions) => apiDescriptions.First());
            }

            // Locate the WebApiConfig.Register method from the specified assembly and execute it.
            ExecuteConfigFromAssembly(assembly, httpConfig);
            httpConfig.EnsureInitialized();

            var swaggerProvider = new SwaggerGenerator(
                httpConfig.Services.GetApiExplorer(),
                httpConfig.Formatters.JsonFormatter.SerializerSettings,
                new Dictionary<string, Info> { { apiVersion, new Info { version = apiVersion, title = apiTitle } } },
                options);

            // This value is not used by AutoRest, so we can just pass in a temp value.
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