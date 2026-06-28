using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Skoleoverblikket.Api.OpenApi;

/// <summary>
/// Marks non-nullable primitive properties (int, bool, etc.) as required so generated
/// TypeScript clients produce `number` instead of `number | undefined`.
/// Reference-type properties ($ref / arrays of $ref) are left to SupportNonNullableReferenceTypes.
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
            // Only promote properties that have an explicit primitive type (no $ref, no array-of-$ref)
            bool hasPrimitiveType = property.Type is not null
                && property is not OpenApiSchemaReference
                && property.Items is null;

            bool isNullable = property.Type is not null
                && (property.Type.Value & JsonSchemaType.Null) != 0;

            if (hasPrimitiveType && !isNullable)
            {
                concreteSchema.Required?.Add(name);
            }
        }
    }
}
