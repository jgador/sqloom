using System.IO;
using System.Threading.Tasks;
using Sqloom.AspNetCore.OpenApi;
using Xunit;

namespace Sqloom.AspNetCore.Tests.OpenApi;

/// <summary>
/// Exercises OpenAPI operation catalog loader.
/// </summary>
public sealed class OpenApiLoaderTests
{
    [Fact]
    public async Task LoadAsync_ReadsSecurityParameterAndBodyMetadata()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "sqloom-openapi-tests", Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        var documentPath = Path.Combine(tempDirectory, "openapi.json");
        await File.WriteAllTextAsync(
            documentPath,
            """
            {
              "openapi": "3.0.1",
              "security": [
                { "Bearer": [] }
              ],
              "paths": {
                "/api/auth/login": {
                  "post": {
                    "security": [],
                    "requestBody": {
                      "required": true,
                      "content": {
                        "application/json": {
                          "example": {
                            "email": "user@example.com",
                            "password": "password"
                          }
                        }
                      }
                    }
                  }
                },
                "/api/expenses/dashboard": {
                  "get": {
                    "parameters": [
                      {
                        "name": "clientLocalNow",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string",
                          "format": "date-time"
                        }
                      }
                    ]
                  }
                }
              }
            }
            """);

        OpenApiCatalogLoader loader = new();

        var operations = await loader.LoadAsync(documentPath);

        Assert.Equal(2, operations.Count);
        var login = Assert.Single(operations, operation => operation.Route == "/api/auth/login");
        Assert.False(login.RequiresAuthentication);
        Assert.True(login.HasJsonRequestBody);
        Assert.True(login.RequestBodyRequired);
        Assert.Contains("\"email\": \"user@example.com\"", login.JsonBodyExample);

        var dashboard = Assert.Single(operations, operation => operation.Route == "/api/expenses/dashboard");
        Assert.True(dashboard.RequiresAuthentication);
        Assert.False(dashboard.HasJsonRequestBody);
        var parameter = Assert.Single(dashboard.Parameters);
        Assert.Equal("clientLocalNow", parameter.Name);
        Assert.Equal("query", parameter.Location);
        Assert.True(parameter.Required);
        Assert.Equal("date-time", parameter.Format);
    }
}
