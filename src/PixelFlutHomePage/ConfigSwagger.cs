using Microsoft.OpenApi.Models;
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
        // Register the Swagger generator, defining 1 or more Swagger documents
        services.AddSwaggerGen(c =>
        {
            // Use method name as operationId
            c.CustomOperationIds(apiDesc => apiDesc.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null);

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "JWolf PixelFlut Client API",
                Description = "OwO what is this?",
                Contact = new OpenApiContact
                {
                    Name = "JWolf",
                    Email = "mr.javawolf@gmail.com",
                }
            });
            c.DocumentFilter<CustomOrderDocumentFilter>();

            c.EnableAnnotations();


            // Set the comments path for the Swagger JSON and UI.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
            c.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Format = "time-span" });
            c.UseAllOfToExtendReferenceSchemas();

        });

        return services;
    }

   

    /// <summary>
    /// Enables Swagger annotations (SwaggerOperationAttribute, SwaggerParameterAttribute etc.)
    /// </summary>
    /// <param name="options"></param>
    public static void EnableAnnotations(this SwaggerGenOptions options)
    {
        options.EnableAnnotations(
            enableAnnotationsForPolymorphism: false,
            enableAnnotationsForInheritance: false);
    }

    public static IApplicationBuilder AddJWolfSwagger(this IApplicationBuilder app)
    {
        // Enable middleware to serve generated Swagger as a JSON endpoint.
        app.UseSwagger();

        // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
        // specifying the Swagger JSON endpoint.
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "JWolf API V1");
        });

        return app;
    }
}


/// <summary>
/// Overwrites the default ordering. This will order all endpoints by tag (group) --> then by path --> then by verb
/// </summary>
public class CustomOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var originalPaths = swaggerDoc.Paths;

        // Generate the ordered keys
        var newPaths = new Dictionary<string, (string OriginalPath, OpenApiPathItem Item)>();
        var removeKeys = new List<string>();
        foreach (var path in originalPaths)
        {
            string tag = path.Value.Operations.First().Value.Tags.First().Name;
            string verb = VerbOrder(path.Value.Operations.First().Key);

            // Reorders the operations if they are grouped up
            path.Value.Operations = path.Value.Operations
                .OrderBy(k => VerbOrder(k.Key))
                .ToDictionary(k => k.Key, k => k.Value);

            // Overall order 
            string orderKey = $"{tag}_{path.Key}_{verb}";
            removeKeys.Add(path.Key);
            newPaths.Add(orderKey, (path.Key, path.Value));
        }

        var orderedPaths = newPaths.Keys.OrderByDescending(k => k);

        // Remove the old keys
        foreach (var key in removeKeys)
            swaggerDoc.Paths.Remove(key);

        // Add the ordered keys
        foreach (var orderedPathKey in orderedPaths)
        {
            var path = newPaths[orderedPathKey];
            swaggerDoc.Paths.Add(path.OriginalPath, path.Item);
        }
    }

    private string VerbOrder(OperationType operationType)
    {
        switch (operationType)
        {
            case OperationType.Get: return "1";
            case OperationType.Post: return "2";
            case OperationType.Put: return "3";
            case OperationType.Patch: return "4";
            case OperationType.Delete: return "5";
            case OperationType.Options: return "6";
            case OperationType.Head: return "7";
            case OperationType.Trace: return "8";
            default: return "9";
        }
    }
}
