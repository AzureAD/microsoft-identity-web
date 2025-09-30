// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Identity.Web.Sidecar
{
    public class OptionsOverrideOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            // Detect custom metadata (attribute example shown below).
            var overrideMeta = context.Description.RelativePath?.Contains("AuthorizationHeader", StringComparison.InvariantCulture) == true ||
                context.Description.RelativePath?.Contains("Downstream", StringComparison.InvariantCulture) == true;

            if (!overrideMeta)
            {
                return Task.CompletedTask;
            }

            OpenApiDescriptions.AddOptionsOverrideParameters(operation);

            return Task.CompletedTask;
        }
    }
}
