using System.Collections.Generic;

namespace Sqloom.Host;

/// <summary>
/// Builds the closed Responses API schema for Sqloom advice JSON.
/// </summary>
internal static class OpenAIAdviceResponseSchema
{
    public static object Build()
    {
        // Keep the Responses schema closed and small so model output normalizes into reviewable artifacts.
        return new
        {
            type = "object",
            additionalProperties = false,
            properties = new Dictionary<string, object>
            {
                ["recommendations"] = new
                {
                    type = "array",
                    minItems = 1,
                    maxItems = 4,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new Dictionary<string, object>
                        {
                            ["title"] = new
                            {
                                type = "string",
                                maxLength = 120,
                            },
                            ["rootCause"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["suggestedChange"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["verificationMetric"] = new
                            {
                                type = "string",
                                maxLength = 240,
                            },
                        },
                        required = new[]
                        {
                            "title",
                            "rootCause",
                            "suggestedChange",
                            "verificationMetric",
                        },
                    },
                },
                ["proposals"] = new
                {
                    type = "array",
                    minItems = 0,
                    maxItems = 3,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new Dictionary<string, object>
                        {
                            ["title"] = new
                            {
                                type = "string",
                                maxLength = 160,
                            },
                            ["diagnosis"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["proposalKind"] = new
                            {
                                type = "string",
                                maxLength = 80,
                            },
                            ["targetObject"] = new
                            {
                                type = "string",
                                maxLength = 256,
                            },
                            ["sqlScript"] = new
                            {
                                type = "string",
                                maxLength = 4000,
                            },
                            ["rollbackSqlScript"] = new
                            {
                                type = new[] { "string", "null" },
                                maxLength = 3000,
                            },
                            ["expectedBenefit"] = new
                            {
                                type = "string",
                                maxLength = 400,
                            },
                            ["verificationMetric"] = new
                            {
                                type = "string",
                                maxLength = 240,
                            },
                            ["confidence"] = new
                            {
                                type = "number",
                                minimum = 0,
                                maximum = 1,
                            },
                            ["sourceCommandOrdinals"] = new
                            {
                                type = "array",
                                maxItems = 16,
                                items = new
                                {
                                    type = "integer",
                                    minimum = 1,
                                },
                            },
                            ["matchedPlanIds"] = new
                            {
                                type = "array",
                                maxItems = 16,
                                items = new
                                {
                                    type = "integer",
                                    minimum = 1,
                                },
                            },
                        },
                        required = new[]
                        {
                            "title",
                            "diagnosis",
                            "proposalKind",
                            "targetObject",
                            "sqlScript",
                            "rollbackSqlScript",
                            "expectedBenefit",
                            "verificationMetric",
                            "confidence",
                            "sourceCommandOrdinals",
                            "matchedPlanIds",
                        },
                    },
                },
            },
            required = new[] { "recommendations", "proposals" },
        };
    }
}
