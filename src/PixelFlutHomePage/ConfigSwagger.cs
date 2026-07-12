using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace PixelFlutHomePage;

public static class ConfigSwagger
{
    public static IServiceCollection AddJWolfSwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            // Use method name as operationId
            c.CustomOperationIds(apiDesc =>
                apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)
                    ? methodInfo.Name
                    : null);

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "JWolf PixelFlut Client API",
                Description = "OwO what is this?",
                Contact = new OpenApiContact
                {
                    Name = "JWolf",
                    Email = "mr.javawolf@gmail.com"
                }
            });


            c.EnableAnnotations();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.MapType<TimeSpan>(() => new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Format = "time"
            });

            c.UseAllOfToExtendReferenceSchemas();
        });

        return services;
    }


    public static IApplicationBuilder AddJWolfSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "JWolf API V1");
        });

        return app;
    }
}

