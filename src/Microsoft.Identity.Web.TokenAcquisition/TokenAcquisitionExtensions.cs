// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for an app token
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    public delegate void BeforeTokenAcquisitionForApp(AcquireTokenForClientParameterBuilder builder, AcquireTokenOptions? acquireTokenOptions);
}
