using Microsoft.OpenApi;
using Skoleoverblikket.Api.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Skoleoverblikket.Api.OpenApi;

public sealed class GroupMessageSchemaFilter : ISchemaFilter
{
	public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
	{
		if (context.Type == typeof(MessagesController.GroupPreviewRequest))
		{
			ApplyOneOf(schema, extraProperties: null);
		}
		else if (context.Type == typeof(MessagesController.SendGroupMessageRequest))
		{
			var extra = new Dictionary<string, IOpenApiSchema>
			{
				["subject"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
				["body"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
			};
			ApplyOneOf(schema, extraProperties: extra);
		}
	}

	private static void ApplyOneOf(IOpenApiSchema schema, Dictionary<string, IOpenApiSchema>? extraProperties)
	{
		if (schema is not OpenApiSchema s)
		{
			return;
		}

		s.Type = null;
		s.Properties?.Clear();
		s.AdditionalPropertiesAllowed = false;
		s.OneOf = BuildVariants(extraProperties);
	}

	private static List<IOpenApiSchema> BuildVariants(Dictionary<string, IOpenApiSchema>? extra)
	{
		return
		[
			Variant("ClassParents", classId: true, staffRole: false, extra),
			Variant("StaffByRole", classId: false, staffRole: true, extra),
			Variant("AllParents", classId: false, staffRole: false, extra),
			Variant("SfoParents", classId: false, staffRole: false, extra),
			Variant("AllStaff", classId: false, staffRole: false, extra),
		];
	}

	private static OpenApiSchema Variant(
		string audienceValue,
		bool classId,
		bool staffRole,
		Dictionary<string, IOpenApiSchema>? extra)
	{
		var required = new HashSet<string> { "audience" };
		var properties = new Dictionary<string, IOpenApiSchema>
		{
			["audience"] = new OpenApiSchema
			{
				Type = JsonSchemaType.String,
				Enum = [JsonValue.Create(audienceValue)!],
			},
		};

		if (classId)
		{
			required.Add("classId");
			properties["classId"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
		}

		if (staffRole)
		{
			required.Add("staffRole");
			properties["staffRole"] = new OpenApiSchemaReference("StaffRole", null, null);
		}

		if (extra is not null)
		{
			foreach (var (key, value) in extra)
			{
				properties[key] = value;
			}
		}

		return new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Required = required,
			Properties = properties,
			AdditionalPropertiesAllowed = false,
		};
	}
}
