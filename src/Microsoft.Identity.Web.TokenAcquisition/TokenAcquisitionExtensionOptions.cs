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
    public partial class TokenAcquisitionExtensionOptions
    {
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

        /// <summary>
        /// Event fired when a ROPC flow request is being built.
        /// </summary>        
        public event BeforeTokenAcquisitionForUsernamePassword? OnBeforeTokenAcquisitionForUsernamePassword;

        /// <summary>
        /// Invoke the BeforeTokenAcquisitionForUsernamePassword event.
        /// </summary>
        internal void InvokeOnBeforeTokenAcquisitionForUsernamePassword(AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
                                                           AcquireTokenOptions? acquireTokenOptions)
        {
            if (OnBeforeTokenAcquisitionForUsernamePassword != null)
            {
                OnBeforeTokenAcquisitionForUsernamePassword(builder, acquireTokenOptions);
            }
        }
    }
}
