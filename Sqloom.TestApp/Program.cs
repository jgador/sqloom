using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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
        builder.Services.AddOpenApi();
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
        builder.Services.AddScoped<ITestAppProductCatalogService, TestAppProductCatalogService>();
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
