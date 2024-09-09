// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Signature for token acquisition extensions that act on the application builder.
    /// </summary>
    /// <param name="confidentialClientApplicationBuilder">Application builder.</param>
    /// <param name="acquireTokenOptions">Token acquisition options.</param>
    public delegate void BuildApplication(ConfidentialClientApplicationBuilder confidentialClientApplicationBuilder, AcquireTokenOptions acquireTokenOptions);

    /// <summary>
    /// Signature for token acquisition extensions that act on the request builder, for an app token
    /// </summary>
    /// <param name="builder">Builder</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request.</param>
    public delegate void BeforeTokenAcquisitionForApp(AcquireTokenForClientParameterBuilder builder, AcquireTokenOptions acquireTokenOptions);

    /// <summary>
    /// Signature for token acquisition extensions that act on the application builder.
    /// </summary>
    /// <param name="authResult">MSAL.NET authentication result</param>
    /// <param name="acquireTokenOptions">Token acquisition options for the request.</param>
    public delegate void AfterTokenAcquisition(AuthenticationResult authResult, AcquireTokenOptions acquireTokenOptions);

    /// <summary>
    /// Options for TokenAcquisition add-ins. These options consist in a set of events, that can be subscribed to by add-ins
    /// or parts of the add-ins.
    /// </summary>
    public class TokenAcquisitionAddInOptions
    {
        /// <summary>
        /// Event fired when the MSAL application needs to be built.
        /// </summary>
        public event BuildApplication? OnBuildConfidentialClientApplication;

        /// <summary>
        /// Event fired when a client credential flow request is being built.
        /// </summary>        
        public event BeforeTokenAcquisitionForApp? OnBeforeTokenAcquisitionForApp;


        /// <summary>
        /// Event fired when an authentication result is available.
        /// </summary>
        public event AfterTokenAcquisition? OnAfterTokenAcquisition;

        internal void InvokeOnBuildConfidentialClientApplication(ConfidentialClientApplicationBuilder builder,
            AcquireTokenOptions acquireTokenOptions)
        {
            if (OnBuildConfidentialClientApplication != null)
            {
                OnBuildConfidentialClientApplication(builder, acquireTokenOptions);
            }
        }


        internal void InvokeOnBeforeTokenAcquisitionForApp(AcquireTokenForClientParameterBuilder builder,
    AcquireTokenOptions acquireTokenOptions)
        {
            if (OnBeforeTokenAcquisitionForApp != null)
            {
                OnBeforeTokenAcquisitionForApp(builder, acquireTokenOptions);
            }
        }

        internal void InvokeOnAfterTokenAcquisition(AuthenticationResult result,
    AcquireTokenOptions acquireTokenOptions)
        {
            if (OnAfterTokenAcquisition != null)
            {
                OnAfterTokenAcquisition(result, acquireTokenOptions);
            }
        }

    }
}
