using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Skoleoverblikket.Api.OpenApi;

/// <summary>
/// Marks every non-nullable property as required in the OpenAPI schema so
/// generated TypeScript clients produce non-optional fields instead of `T | undefined`.
/// </summary>
public sealed class RequireNonNullableSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema || schema.Properties is not { Count: > 0 })
        {
            return;
        }

        foreach (var (name, property) in schema.Properties)
        {
            bool isNullable = property.Type is not null
                && (property.Type.Value & JsonSchemaType.Null) != 0;

            if (!isNullable)
            {
                concreteSchema.Required.Add(name);
            }
        }
    }
}
