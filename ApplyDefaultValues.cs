using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RepoKb;

public class ApplyDefaultValues : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
        {
            return;
        }

        foreach (var property in schema.Properties)
        {
            if (property.Value.Example == null)
            {
                var member = context.Type.GetMember(property.Key).FirstOrDefault();
                if (member != null)
                {
                    var defaultValueAttribute = member.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultValueAttribute != null)
                    {
                        property.Value.Example = ConvertToOpenApiAny(defaultValueAttribute.Value, property.Value.Type);
                    }
                }
            }
        }
    }
    
    private IOpenApiAny ConvertToOpenApiAny(object? value, string? propertyType)
    {
        if (value == null)
        {
            return new OpenApiNull();
        }

        switch (propertyType)
        {
            case "integer":
                return new OpenApiInteger((int)value);
            case "number": // Assuming this is for double/float
                return new OpenApiDouble((double)value);
            case "string":
                return new OpenApiString((string)value);
            case "boolean":
                return new OpenApiBoolean((bool)value);
            // Add more cases for other data types as needed
            default:
                // Handle unsupported types or throw an exception
                return new OpenApiString(value.ToString()); // Default to string representation
        }
    }
}