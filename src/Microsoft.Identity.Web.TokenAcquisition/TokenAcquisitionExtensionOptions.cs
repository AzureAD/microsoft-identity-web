// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for TokenAcquisition add-ins. These options consist in a set of events, that can be subscribed to by add-ins
    /// or parts of the add-ins.
    /// </summary>
    public class TokenAcquisitionExtensionOptions
    {
#if FUTURE
        /// <summary>
        /// Event fired when the MSAL application needs to be built.
        /// </summary>
        public event BuildApplication? OnBuildConfidentialClientApplication;

        /// <summary>
        /// Event fired when an authentication result is available.
        /// </summary>
        public event AfterTokenAcquisition? OnAfterTokenAcquisition;

        /// <summary>
        /// Invoke the OnBuildConfidentialClientApplication event.
        /// </summary>
        internal void InvokeOnBuildConfidentialClientApplication(ConfidentialClientApplicationBuilder builder,
                                                                 AcquireTokenOptions? acquireTokenOptions)
        {
            if (OnBuildConfidentialClientApplication != null)
            {
                OnBuildConfidentialClientApplication(builder, acquireTokenOptions);
            }
        }

        /// <summary>
        /// Invoke the OnAfterTokenAcquisition event.
        /// </summary>
        internal void InvokeOnAfterTokenAcquisition(AuthenticationResult result,
                                                    AcquireTokenOptions? acquireTokenOptions)
        {
            if (OnAfterTokenAcquisition != null)
            {
                OnAfterTokenAcquisition(result, acquireTokenOptions);
            }
        }
#endif

        /// <summary>
        /// Event fired when a client credential flow request is being built.
        /// </summary>        
        public event BeforeTokenAcquisitionForApp? OnBeforeTokenAcquisitionForApp;

        /// <summary>
        /// Invoke the OnBeforeTokenAcquisitionForApp event.
        /// </summary>
        internal void InvokeOnBeforeTokenAcquisitionForApp(AcquireTokenForClientParameterBuilder builder,
                                                           AcquireTokenOptions? acquireTokenOptions)
        {
            if (OnBeforeTokenAcquisitionForApp != null)
            {
                OnBeforeTokenAcquisitionForApp(builder, acquireTokenOptions);
            }
        }

    }
}
