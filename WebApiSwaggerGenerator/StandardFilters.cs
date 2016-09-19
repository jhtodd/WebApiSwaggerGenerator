using System;
using System.Linq;
using System.Reflection;
using Swashbuckle.Swagger;

namespace WebApiSwaggerGenerator
{
    using System.Web.Http.Description;

    public class StandardSchemaFilter : ISchemaFilter
    {
        public void Apply(Schema schema, SchemaRegistry schemaRegistry, Type type)
        {
            var requiredProperties = (from property in schema.properties
                                        let propertyInfo = type.GetProperty(property.Key, BindingFlags.Instance | BindingFlags.Public)
                                        let propertyType = propertyInfo?.PropertyType
                                        let isNullable = propertyType != null && propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof (Nullable<>)
                                        let isReferenceType = propertyType != null && !propertyType.IsValueType
                                        where !isNullable && !isReferenceType select property.Key).ToList();
        
            schema.required = requiredProperties;
        }
    }

    public class StandardOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            foreach (var parameter in operation?.parameters ?? new Parameter[0])
            {
                var parameterDescriptor = apiDescription?.ParameterDescriptions
                                                        ?.FirstOrDefault(pd => string.Equals(pd.Name, parameter.name, StringComparison.CurrentCultureIgnoreCase));

                var parameterType = parameterDescriptor?.ParameterDescriptor?.ParameterType;

                var isNullable = parameterType != null && parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>);
                var isReferenceType = parameterType != null && !parameterType.IsValueType;

                if (!isNullable && !isReferenceType)
                {
                    parameter.required = true;
                }
            }
        }
    }
}