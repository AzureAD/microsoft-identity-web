// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.OpenApi.Models;

namespace Microsoft.Identity.Web.Sidecar;

internal static class OpenApiDescriptions
{
    internal static void AddOptionsOverrideParameters(OpenApiOperation op)
    {
        // Avoid duplicate injection
        if (op.Extensions.ContainsKey("x-optionsOverride-summary"))
        {
            return;
        }

        // Scopes (repeatable)
        op.Parameters.Add(new OpenApiParameter
        {
            Name = "optionsOverride.Scopes",
            In = ParameterLocation.Query,
            Description = "Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes=User.Read",
            Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Explode = true,
            Style = ParameterStyle.Form
        });

        // Core boolean / simple toggles
        AddSimple(op, "optionsOverride.RequestAppToken", "boolean", "true = acquire an app (client credentials) token instead of user token.");

        // Base request shaping
        AddSimple(op, "optionsOverride.BaseUrl", "string", "Override downstream API base URL.");
        AddSimple(op, "optionsOverride.RelativePath", "string", "Override relative path appended to BaseUrl.");
        AddSimple(op, "optionsOverride.HttpMethod", "string", "Override HTTP method (GET, POST, PATCH, etc.).");
        AddSimple(op, "optionsOverride.AcceptHeader", "string", "Sets Accept header (e.g. application/json).");
        AddSimple(op, "optionsOverride.ContentType", "string", "Sets Content-Type used for serialized body (if body provided).");

        // AcquireTokenOptions.* (token acquisition tuning)
        AddAcquireTokenOption(op, "Tenant", "Override tenant (GUID or 'common').");
        AddAcquireTokenOption(op, "ForceRefresh", "boolean", "true = bypass token cache.");
        AddAcquireTokenOption(op, "AuthenticationOptionsName", "Named authentication configuration to use.");
        AddAcquireTokenOption(op, "Claims", "JSON claims challenge or extra claims.");
        AddAcquireTokenOption(op, "CorrelationId", "GUID correlation id for token acquisition.");
        AddAcquireTokenOption(op, "LongRunningWebApiSessionKey", "Session key for long running OBO flows.");
        AddAcquireTokenOption(op, "FmiPath", "Federated Managed Identity path (if using FMI).");
        AddAcquireTokenOption(op, "PopPublicKey", "Public key or JWK for PoP / AT-POP requests.");

        // Managed Identity (if enabled)
        AddAcquireTokenOption(op, "ManagedIdentity.UserAssignedClientId", "Managed Identity client id (user-assigned).");

        // Provide a compact summary
        op.Extensions["x-optionsOverride-summary"] =
            new OpenApi.Any.OpenApiString(
                "Supported dotted overrides: " +
                "Scopes (repeatable), RequestAppToken, BaseUrl, RelativePath, HttpMethod, AcceptHeader, ContentType, " +
                "ExtraHeaderParameters[H], ExtraQueryParameters[p], " +
                "AcquireTokenOptions.(Tenant|ForceRefresh|AuthenticationOptionsName|Claims|CorrelationId|LongRunningWebApiSessionKey|FmiPath|PopPublicKey|ManagedIdentity.UserAssignedClientId)");

        // Extended description (optional)
        op.Extensions["x-optionsOverride-notes"] =
            new OpenApi.Any.OpenApiString(
                "Dictionary-style keys use bracket syntax. Example: " +
                "optionsOverride.ExtraHeaderParameters[X-Custom]=Value & optionsOverride.AcquireTokenOptions.ExtraQueryParameters[prompt]=consent");
    }

    private static void AddSimple(OpenApiOperation op, string name, string type, string desc)
    {
        op.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Query,
            Description = desc,
            Required = false,
            Schema = new OpenApiSchema { Type = type }
        });
    }

    private static void AddAcquireTokenOption(OpenApiOperation op, string name, string description, string type = "string")
    {
        op.Parameters.Add(new OpenApiParameter
        {
            Name = $"optionsOverride.AcquireTokenOptions.{name}",
            In = ParameterLocation.Query,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema { Type = type }
        });
    }
}
