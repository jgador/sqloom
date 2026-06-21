using System;
using System.Collections.Generic;
using System.Text;
using Sqloom.AspNetCore.OpenApi;
using Sqloom.Core.Execution;

namespace Sqloom.AspNetCore.Endpoints;

/// <summary>
/// Builds replayable HTTP requests from OpenAPI operations and prepared inputs.
/// </summary>
internal sealed class EndpointReplayRequestResolver
{
    public EndpointReplayRequest Resolve(
        DiscoveredOpenApiOperation discoveredOperation,
        ResolvedReplayOperation resolvedOperation,
        PreparedReplayOperation preparedOperation)
    {
        var pathValues = MergeValues(
            resolvedOperation.PathValues,
            preparedOperation.PathValues);
        var queryValues = MergeValues(
            resolvedOperation.QueryValues,
            preparedOperation.QueryValues);
        var headerValues = MergeValues(
            resolvedOperation.HeaderValues,
            preparedOperation.HeaderValues);
        var requestBodyJson =
            preparedOperation.RequestBodyJson
            ?? resolvedOperation.RequestBodyJson
            ?? discoveredOperation.JsonRequestBodyExample;

        foreach (var parameter in discoveredOperation.Parameters)
        {
            if (!parameter.Required)
            {
                continue;
            }

            var values = parameter.Location switch
            {
                "path" => pathValues,
                "query" => queryValues,
                "header" => headerValues,
                _ => new Dictionary<string, string>(),
            };
            if (!values.ContainsKey(parameter.Name))
            {
                throw new InvalidOperationException(
                    $"Replay request for {discoveredOperation.StableOperationKey} is missing required {parameter.Location} parameter '{parameter.Name}'.");
            }
        }

        if (discoveredOperation.RequestBodyRequired
            && string.IsNullOrWhiteSpace(requestBodyJson))
        {
            throw new InvalidOperationException(
                $"Replay request for {discoveredOperation.StableOperationKey} requires a JSON request body.");
        }

        var resolvedRoute = ResolveRoute(discoveredOperation.Route, pathValues);
        var relativePathAndQuery = AppendQueryString(resolvedRoute, queryValues);

        return new EndpointReplayRequest
        {
            OperationKey = discoveredOperation.StableOperationKey,
            HttpMethod = discoveredOperation.HttpMethod,
            Route = discoveredOperation.Route,
            Persona = preparedOperation.Persona ?? resolvedOperation.Persona,
            RelativePathAndQuery = relativePathAndQuery,
            RequestBodyJson = requestBodyJson,
            PathValues = pathValues,
            QueryValues = queryValues,
            HeaderValues = headerValues,
        };
    }

    private static IReadOnlyDictionary<string, string> MergeValues(
        IReadOnlyDictionary<string, string> primary,
        IReadOnlyDictionary<string, string> secondary)
    {
        Dictionary<string, string> merged = new(StringComparer.OrdinalIgnoreCase);
        foreach ((var key, var value) in primary)
        {
            merged[key] = value;
        }

        foreach ((var key, var value) in secondary)
        {
            merged[key] = value;
        }

        return merged;
    }

    private static string ResolveRoute(string route, IReadOnlyDictionary<string, string> pathValues)
    {
        var resolved = route;
        foreach ((var key, var value) in pathValues)
        {
            resolved = resolved.Replace(
                $"{{{key}}}",
                Uri.EscapeDataString(value),
                StringComparison.OrdinalIgnoreCase);
        }

        return resolved;
    }

    private static string AppendQueryString(string route, IReadOnlyDictionary<string, string> queryValues)
    {
        if (queryValues.Count == 0)
        {
            return route;
        }

        StringBuilder builder = new(route);
        builder.Append(route.Contains('?') ? '&' : '?');
        var needsSeparator = false;
        foreach ((var key, var value) in queryValues)
        {
            if (needsSeparator)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(key));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(value));
            needsSeparator = true;
        }

        return builder.ToString();
    }
}
