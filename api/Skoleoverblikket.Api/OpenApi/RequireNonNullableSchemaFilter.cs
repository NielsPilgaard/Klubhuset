using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Skoleoverblikket.Api.OpenApi;

/// <summary>
/// Marks non-nullable primitive properties (int, bool, etc.) as required so generated
/// TypeScript clients produce `number` instead of `number | undefined`.
/// Reference-type properties ($ref / arrays of $ref) are left to SupportNonNullableReferenceTypes,
/// except non-nullable enums: enums serialize as a $ref to the enum schema, but a non-nullable
/// enum property (e.g. `RecipientType SenderType`) can never be absent, so it must be promoted too.
/// </summary>
public sealed class RequireNonNullableSchemaFilter : ISchemaFilter
{
	public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
	{
		if (schema is not OpenApiSchema concreteSchema || schema.Properties is not { Count: > 0 })
		{
			return;
		}

		var clrProperties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

		foreach (var (name, property) in schema.Properties)
		{
			// Only promote properties that have an explicit primitive type (no $ref, no array-of-$ref)
			bool hasPrimitiveType = property.Type is not null
				&& property is not OpenApiSchemaReference
				&& property.Items is null;

			bool isNullable = property.Type is not null
				&& (property.Type.Value & JsonSchemaType.Null) != 0;

			bool isNonNullableEnum = property is OpenApiSchemaReference
				&& clrProperties.TryGetValue(name, out var clrProperty)
				&& clrProperty.PropertyType.IsEnum;

			if ((hasPrimitiveType && !isNullable) || isNonNullableEnum)
			{
				concreteSchema.Required ??= new HashSet<string>();
				concreteSchema.Required.Add(name);
			}
		}
	}
}
