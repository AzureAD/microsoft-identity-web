// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
        public event BeforeTokenAcquisitionForTestUser? OnBeforeTokenAcquisitionForTestUser;

        /// <summary>
        /// Event fired when a ROPC flow request is being built.
        /// </summary>        
        public event BeforeTokenAcquisitionForTestUserAsync? OnBeforeTokenAcquisitionForTestUserAsync;

        /// <summary>
        /// Occurs before an asynchronous token acquisition operation for the On-Behalf-Of authentication flow is
        /// initiated.
        /// </summary>
        public event BeforeTokenAcquisitionForOnBehalfOf? OnBeforeTokenAcquisitionForOnBehalfOf;

        /// <summary>
        /// Occurs before an asynchronous token acquisition operation for the On-Behalf-Of authentication flow is
        /// initiated.
        /// </summary>
        public event BeforeTokenAcquisitionForOnBehalfOfAsync? OnBeforeTokenAcquisitionForOnBehalfOfAsync;

        /// <summary>
        /// Invoke the OnBeforeTokenAcquisitionForApp event.
        /// </summary>
        internal async Task InvokeOnBeforeTokenAcquisitionForOnBehalfOfAsync(AcquireTokenOnBehalfOfParameterBuilder builder,
                                                           AcquireTokenOptions? acquireTokenOptions,
                                                           ClaimsPrincipal user)
        {
            // Run the async event if it is not null
            if (OnBeforeTokenAcquisitionForOnBehalfOfAsync != null)
            {
                // (cannot directly await an async event because events are not tasks
                // they are multicast delegates that invoke handlers, but don’t return values to the publisher,
                // nor do they support awaiting natively
                var invocationList = OnBeforeTokenAcquisitionForOnBehalfOfAsync.GetInvocationList();
                var tasks = invocationList
                    .Cast<BeforeTokenAcquisitionForOnBehalfOfAsync>()
                    .Select(handler => handler(builder, acquireTokenOptions, user));
                await Task.WhenAll(tasks);
            }

            // Run the sync event if it is not null.
            OnBeforeTokenAcquisitionForOnBehalfOf?.Invoke(builder, acquireTokenOptions, user);
        }

        /// <summary>
        /// Invoke the BeforeTokenAcquisitionForTestUser event.
        /// </summary>
        internal async Task InvokeOnBeforeTokenAcquisitionForTestUserAsync(AcquireTokenByUsernameAndPasswordConfidentialParameterBuilder builder,
                                                                           AcquireTokenOptions? acquireTokenOptions, ClaimsPrincipal user)
        {
            // Run the async event if it is not null
            if (OnBeforeTokenAcquisitionForTestUserAsync != null)
            {
                // (cannot directly await an async event because events are not tasks
                // they are multicast delegates that invoke handlers, but don’t return values to the publisher,
                // nor do they support awaiting natively
                var invocationList = OnBeforeTokenAcquisitionForTestUserAsync.GetInvocationList();
                var tasks = invocationList
                    .Cast<BeforeTokenAcquisitionForTestUserAsync>()
                    .Select(handler => handler(builder, acquireTokenOptions, user));
                await Task.WhenAll(tasks);
            }

            // Run the sync event if it is not null.
            OnBeforeTokenAcquisitionForTestUser?.Invoke(builder, acquireTokenOptions, user);
        }

    }
}
