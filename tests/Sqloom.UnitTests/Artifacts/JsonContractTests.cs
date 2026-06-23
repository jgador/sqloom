using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sqloom.AspNetCore.Endpoints;
using Sqloom.AspNetCore.OpenApi;
using Sqloom.Core.Execution;
using Sqloom.Correlation.QueryStore;
using Sqloom.OpenAI.Advice;
using Sqloom.QueryStore.QueryStore;
using Sqloom.TestApp;
using Sqloom.Host;
using Xunit;

namespace Sqloom.Core.Tests.Artifacts;

/// <summary>
/// Verifies persisted JSON contracts use explicit camel-case property names.
/// </summary>
public sealed class JsonContractTests
{
    public static TheoryData<Type, string[]> RepresentativeContracts =>
        new()
        {
            { typeof(AdviceReport), ["generatedAtUtc", "queryStoreCorrelationPath", "modelProvider"] },
            { typeof(EndpointReplayRunResult), ["appName", "discoveredOperations", "replayPlan"] },
            { typeof(QueryStoreSnapshot), ["capturedAtUtc", "databaseOptions", "plans"] },
            { typeof(SqlTuningProposalReport), ["generatedAtUtc", "sourceAdvicePath", "sqlScriptPath"] },
            { typeof(TuneWorkflowReport), ["generatedAtUtc", "queryStoreSnapshotPath", "summary"] },
        };

    private static readonly Type[] JsonContractRoots =
    [
        typeof(AdviceReport),
        typeof(OpenApiOperation),
        typeof(EndpointReplayPlan),
        typeof(EndpointReplayResult),
        typeof(EndpointReplayRunResult),
        typeof(OpenAITuningAdviceRequest),
        typeof(OpenAITuningAdviceResponse),
        typeof(ProductResponse),
        typeof(QueryCorrelationReport),
        typeof(QueryStoreSnapshot),
        typeof(ReplayLaunchOptions),
        typeof(ReplayProfile),
        typeof(SqlTuningProposalReport),
        typeof(TuneWorkflowReport),
    ];

    [Fact]
    public void JsonContractProperties_HaveExplicitCamelCaseJsonPropertyNames()
    {
        var missingOrIncorrectProperties = EnumerateJsonContractTypes()
            .SelectMany(static type => type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => new
                {
                    Type = type,
                    Property = property,
                    Attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>(),
                    ExpectedName = JsonNamingPolicy.CamelCase.ConvertName(property.Name),
                }))
            .Where(static item => item.Attribute?.Name != item.ExpectedName)
            .Select(static item => item.Attribute is null
                ? $"{item.Type.FullName}.{item.Property.Name} is missing [JsonPropertyName(\"{item.ExpectedName}\")]"
                : $"{item.Type.FullName}.{item.Property.Name} uses [JsonPropertyName(\"{item.Attribute.Name}\")] instead of [JsonPropertyName(\"{item.ExpectedName}\")]")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missingOrIncorrectProperties);
    }

    [Theory]
    [MemberData(nameof(RepresentativeContracts))]
    public void JsonContracts_SerializeRepresentativeArtifactsWithCamelCaseNames(
        Type contractType,
        string[] expectedPropertyNames)
    {
        var value = RuntimeHelpers.GetUninitializedObject(contractType);

        var json = JsonSerializer.Serialize(value, contractType);

        foreach (var expectedPropertyName in expectedPropertyNames)
        {
            Assert.Contains($"\"{expectedPropertyName}\"", json, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<Type> EnumerateJsonContractTypes()
    {
        Queue<Type> queue = new(JsonContractRoots);
        HashSet<Type> seen = new();
        List<Type> types = [];
        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
            {
                continue;
            }

            types.Add(type);
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (var nestedType in GetNestedContractTypes(property.PropertyType))
                {
                    queue.Enqueue(nestedType);
                }
            }
        }

        return types;
    }

    private static IEnumerable<Type> GetNestedContractTypes(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (type == typeof(string)
            || type.IsPrimitive
            || type.IsEnum
            || type == typeof(decimal)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan))
        {
            yield break;
        }

        if (type.IsArray)
        {
            foreach (var nestedType in GetNestedContractTypes(type.GetElementType()!))
            {
                yield return nestedType;
            }

            yield break;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var nestedType in GetNestedContractTypes(argument))
                {
                    yield return nestedType;
                }
            }

            yield break;
        }

        if (type.Namespace?.StartsWith("Sqloom.", StringComparison.Ordinal) == true)
        {
            yield return type;
        }
    }
}
