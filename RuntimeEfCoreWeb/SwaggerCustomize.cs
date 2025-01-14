using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RuntimeEfCoreWeb
{
    public static class SwaggerCustomize
    {
        public static void AddDynamicCrudEndpoints(this IServiceCollection services, DbContext dbContext)
        {
            var model = dbContext.Model;
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Dynamic Tables API",
                    Version = "v1",
                    Description = "API for managing dynamic tables and their data"
                });
                foreach (var entityType in model.GetEntityTypes())
                {
                    var entityName = entityType.ClrType.Name;
                    c.OperationFilter<SwaggerCrudOperationFilter>(entityName, "GET", "/api/" + entityName);
                    c.OperationFilter<SwaggerCrudOperationFilter>(entityName, "POST", "/api/" + entityName);
                    c.OperationFilter<SwaggerCrudOperationFilter>(entityName, "PUT", "/api/" + entityName);
                    c.OperationFilter<SwaggerCrudOperationFilter>(entityName, "DELETE", "/api/" + entityName);
                }

            });
        }
    }
}
public class SwaggerCrudOperationFilter : IOperationFilter
{
    private readonly string _entityName;
    private readonly string _method;
    private readonly string _route;

    public SwaggerCrudOperationFilter(string entityName, string method, string route)
    {
        _entityName = entityName;
        _method = method;
        _route = route;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Tags == null)
            operation.Tags = new List<OpenApiTag>();

        // CRUD operasyonu için özet açıklaması ekliyoruz
        operation.Tags.Add(new OpenApiTag { Name = $"{_method} {_route}" });

        if (_method == "GET")
        {
            operation.Summary = $"Retrieve all {_entityName} items";
        }
        else if (_method == "POST")
        {
            operation.Summary = $"Create a new {_entityName} item";
        }
        else if (_method == "PUT")
        {
            operation.Summary = $"Update an existing {_entityName} item";
        }
        else if (_method == "DELETE")
        {
            operation.Summary = $"Delete an existing {_entityName} item";
        }
    }
}

