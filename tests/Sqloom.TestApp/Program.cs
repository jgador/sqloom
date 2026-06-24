using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Sqloom.TestApp;

public partial class Program
{
    private static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                };
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
                });
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, _, _) =>
            {
                foreach (var parameter in operation.Parameters ?? [])
                {
                    if (parameter is OpenApiParameter openApiParameter
                        && openApiParameter.In == ParameterLocation.Query)
                    {
                        openApiParameter.Required = true;
                    }
                }

                return Task.CompletedTask;
            });
        });
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Sqloom Test App",
                Version = "v1",
            });
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
        });
        builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
        builder.Services.AddDbContext<TestAppProductCatalogDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseInMemoryDatabase("SqloomTestApp");
            }
        });

        var app = builder.Build();
        app.UseSwagger();
        app.MapOpenApi();
        app.MapControllers();
        return app.RunAsync();
    }
}
