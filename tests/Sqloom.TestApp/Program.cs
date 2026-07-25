using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sqloom.TestApp;

public partial class Program
{
    private static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddControllers();
        builder.Services.AddScoped<ProductCatalogService>();
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
        app.MapControllers();
        return app.RunAsync();
    }
}
