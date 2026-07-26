using System;
using System.Collections.Generic;
using Sqloom.Host.Replay;
using Sqloom.Pipeline.Execution;
using Xunit;

namespace Sqloom.Host.Tests.Replay;

/// <summary>
/// Exercises endpoint replay request building.
/// </summary>
public sealed class ReplayRequestBuilderTests
{
    [Fact]
    public void MergesPreparedValuesOverReplayDefaults()
    {
        var request = ReplayRequestBuilder.Build(
            CreateOperation(requestBodyRequired: true),
            new ResolvedReplayOperation
            {
                OperationKey = "POST /api/items/{itemId}",
                HttpMethod = "POST",
                Route = "/api/items/{itemId}",
                Persona = "default-user",
                RequestBodyJson = """{"name":"default"}""",
                PathValues = new Dictionary<string, string>
                {
                    ["itemId"] = "11",
                },
                QueryValues = new Dictionary<string, string>
                {
                    ["since"] = "2026-05-01T00:00:00+08:00",
                },
                HeaderValues = new Dictionary<string, string>
                {
                    ["x-trace"] = "trace-default",
                },
            },
            new PreparedReplayOperation
            {
                Persona = "prepared-user",
                RequestBodyJson = """{"name":"prepared"}""",
                PathValues = new Dictionary<string, string>
                {
                    ["itemId"] = "42",
                },
                QueryValues = new Dictionary<string, string>
                {
                    ["since"] = "2026-05-04T09:41:00+08:00",
                },
                HeaderValues = new Dictionary<string, string>
                {
                    ["x-trace"] = "trace-prepared",
                },
            });

        Assert.Equal("prepared-user", request.Persona);
        Assert.Equal("/api/items/42?since=2026-05-04T09%3A41%3A00%2B08%3A00", request.RelativePathAndQuery);
        Assert.Equal("""{"name":"prepared"}""", request.RequestBodyJson);
        Assert.Equal("trace-prepared", request.HeaderValues["x-trace"]);
    }

    [Fact]
    public void ThrowsWhenRequiredRequestBodyIsMissing()
    {

        var exception = Assert.Throws<InvalidOperationException>(() => ReplayRequestBuilder.Build(
            CreateOperation(requestBodyRequired: true),
            new ResolvedReplayOperation
            {
                OperationKey = "POST /api/items/{itemId}",
                HttpMethod = "POST",
                Route = "/api/items/{itemId}",
            },
            new PreparedReplayOperation
            {
                PathValues = new Dictionary<string, string>
                {
                    ["itemId"] = "42",
                },
                QueryValues = new Dictionary<string, string>
                {
                    ["since"] = "2026-05-04T09:41:00+08:00",
                },
                HeaderValues = new Dictionary<string, string>
                {
                    ["x-trace"] = "trace-prepared",
                },
            }));

        Assert.Contains("requires a JSON request body", exception.Message);
    }

    private static ReplayOperation CreateOperation(bool requestBodyRequired)
    {
        return new ReplayOperation
        {
            StableOperationKey = "POST /api/items/{itemId}",
            HttpMethod = "POST",
            Route = "/api/items/{itemId}",
            RequiresAuthentication = true,
            HasJsonRequestBody = requestBodyRequired,
            RequestBodyRequired = requestBodyRequired,
            Parameters =
            [
                new ReplayParameter
                {
                    Name = "itemId",
                    Location = "path",
                    Required = true,
                },
                new ReplayParameter
                {
                    Name = "since",
                    Location = "query",
                    Required = true,
                },
                new ReplayParameter
                {
                    Name = "x-trace",
                    Location = "header",
                    Required = true,
                },
            ],
        };
    }
}
