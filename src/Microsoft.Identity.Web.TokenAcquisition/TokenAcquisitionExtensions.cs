// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
#if FUTURE
    /// <summary>
    /// Signature for token acquisition extensions that act on the application builder.
    /// </summary>
    /// <param name="confidentialClientApplicationBuilder">Application builder.</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    public delegate void BuildApplication(ConfidentialClientApplicationBuilder confidentialClientApplicationBuilder, AcquireTokenOptions? acquireTokenOptions);

    /// <summary>
    /// Signature for token acquisition extensions that act on the application builder.
    /// </summary>
    /// <param name="authResult">MSAL.NET authentication result</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    public delegate void AfterTokenAcquisition(AuthenticationResult authResult, AcquireTokenOptions? acquireTokenOptions);
#endif

    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for an app token
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    public delegate void BeforeTokenAcquisitionForApp(AcquireTokenForClientParameterBuilder builder, AcquireTokenOptions? acquireTokenOptions);
}
