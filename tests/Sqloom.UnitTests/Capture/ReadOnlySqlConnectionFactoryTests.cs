using Sqloom.AzureSql.Capture;
using Xunit;

namespace Sqloom.AzureSql.Tests.Capture;

/// <summary>
/// Exercises readonly SQL connection factory.
/// </summary>
public sealed class ReadOnlySqlConnectionFactoryTests
{
    [Fact]
    public void CreateBuilder_SetsDefaultApplicationName_WhenMissing()
    {
        ReadOnlySqlConnectionFactory factory = new();

        var builder = factory.CreateBuilder("Server=tcp:readonly;Encrypt=True;");

        Assert.Equal("Sqloom", builder.ApplicationName);
        Assert.Contains("readonly", builder.DataSource);
    }

    [Fact]
    public void CreateBuilder_PreservesExistingApplicationName()
    {
        ReadOnlySqlConnectionFactory factory = new();

        var builder = factory.CreateBuilder("Server=tcp:readonly;Application Name=ExistingApp;");

        Assert.Equal("ExistingApp", builder.ApplicationName);
    }
}
