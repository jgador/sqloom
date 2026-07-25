using Sqloom.Host.QueryStore;
using Xunit;

namespace Sqloom.Host.Tests.QueryStore;

/// <summary>
/// Exercises readonly SQL connection factory.
/// </summary>
public sealed class ReadOnlySqlConnectionFactoryTests
{
    [Fact]
    public void SetsDefaultApplicationName_WhenMissing()
    {
        var builder = ReadOnlySqlConnectionFactory.CreateBuilder("Server=tcp:readonly;Encrypt=True;");

        Assert.Equal("Sqloom", builder.ApplicationName);
        Assert.Contains("readonly", builder.DataSource);
    }

    [Fact]
    public void PreservesExistingApplicationName()
    {
        var builder = ReadOnlySqlConnectionFactory.CreateBuilder("Server=tcp:readonly;Application Name=ExistingApp;");

        Assert.Equal("ExistingApp", builder.ApplicationName);
    }
}
