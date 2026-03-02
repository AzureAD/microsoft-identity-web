// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Threading.Tasks;
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

    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for ROPC flow.
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    /// <param name="user">User claims.</param>
    public delegate void BeforeTokenAcquisitionForTestUser(AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder, AcquireTokenOptions? acquireTokenOptions, ClaimsPrincipal user);

    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for ROPC flow (Async version).
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    /// <param name="user">User claims.</param>
    public delegate Task BeforeTokenAcquisitionForTestUserAsync(AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder, AcquireTokenOptions? acquireTokenOptions, ClaimsPrincipal user);

    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for on-behalf-of flow.
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    /// <param name="eventArgs">Event arguments containing user claims and additional context information.</param>
    public delegate void BeforeTokenAcquisitionForOnBehalfOf(AcquireTokenOnBehalfOfParameterBuilder builder, AcquireTokenOptions? acquireTokenOptions, OnBehalfOfEventArgs eventArgs);

    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for on-behalf-of flow (Async version).
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request. Can be null.</param>
    /// <param name="eventArgs">Event arguments containing user claims and additional context information.</param>
    public delegate Task BeforeTokenAcquisitionForOnBehalfOfAsync(AcquireTokenOnBehalfOfParameterBuilder builder, AcquireTokenOptions? acquireTokenOptions, OnBehalfOfEventArgs eventArgs);

    /// <summary>
    /// Signature for a sync event that fires before the on-behalf-of flow is initialized.
    /// </summary>
    /// <param name="eventArgs">Event arguments containing the user assertion token. Handlers can modify <see cref="OnBehalfOfEventArgs.UserAssertionToken"/> to replace the assertion.</param>
    public delegate void BeforeOnBehalfOfInitialized(OnBehalfOfEventArgs eventArgs);

    /// <summary>
    /// Signature for an async event that fires before the on-behalf-of flow is initialized.
    /// </summary>
    /// <param name="eventArgs">Event arguments containing the user assertion token. Handlers can modify <see cref="OnBehalfOfEventArgs.UserAssertionToken"/> to replace the assertion.</param>
    public delegate Task BeforeOnBehalfOfInitializedAsync(OnBehalfOfEventArgs eventArgs);
}
