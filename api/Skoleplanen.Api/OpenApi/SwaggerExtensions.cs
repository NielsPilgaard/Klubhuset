using Microsoft.OpenApi;

namespace Skoleplanen.Api.OpenApi;

public static class SwaggerExtensions
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        const string schemeId = "Bearer";

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Skoleplanen API", Version = "v1" });
            options.AddSecurityDefinition(schemeId,
                                          new OpenApiSecurityScheme
                                          {
                                              Name = "Authorization",
                                              Type = SecuritySchemeType.Http,
                                              Scheme = schemeId,
                                              BearerFormat = "JWT",
                                              In = ParameterLocation.Header,
                                          });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            { [new OpenApiSecuritySchemeReference(schemeId, document)] = [] });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerInDevelopment(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.UseSwagger(c => c.RouteTemplate = "api/v1/openapi/{documentName}/openapi.json");
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/api/v1/openapi/v1/openapi.json", "Skoleplanen API v1");
            c.RoutePrefix = "api/v1/openapi";
        });

        return app;
    }
}
