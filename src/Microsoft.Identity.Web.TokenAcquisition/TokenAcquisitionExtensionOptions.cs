// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Options for TokenAcquisition add-ins. These options consist in a set of events, that can be subscribed to by add-ins
    /// or parts of the add-ins.
    /// </summary>
    public partial class TokenAcquisitionExtensionOptions
    {
        /// <summary>
        /// Callback invoked when a fire-and-forget background (proactive) token refresh completes, for confidential
        /// client and managed identity applications. Proactive refresh runs on a background thread after the caller
        /// has already received a valid cached token, so its outcome (latency, failures) is otherwise unobservable;
        /// this callback surfaces it, for example to emit telemetry.
        /// </summary>
        /// <remarks>
        /// The <see cref="ExecutionResult"/> carries the refreshed token on success
        /// (<see cref="ExecutionResult.Result"/>) or the exception on failure (<see cref="ExecutionResult.Exception"/>),
        /// whose <see cref="MsalException.AuthenticationResultMetadata"/> exposes the failed attempt's HTTP duration.
        /// The callback is invoked on a background thread; exceptions it throws are caught and logged by MSAL so they
        /// cannot disrupt the refresh. Set once; the same delegate is applied to every confidential client and managed
        /// identity application built by Microsoft.Identity.Web.
        /// </remarks>
        public Func<ExecutionResult, Task>? OnBackgroundTokenRefreshCompleted { get; set; }

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
        /// Occurs before the On-Behalf-Of flow is initialized.
        /// </summary>
        public event BeforeOnBehalfOfInitialized? OnBeforeOnBehalfOfInitialized;

        /// <summary>
        /// Occurs before the On-Behalf-Of flow is initialized.
        /// </summary>
        public event BeforeOnBehalfOfInitializedAsync? OnBeforeOnBehalfOfInitializedAsync;

        /// <summary>
        /// Invoke the OnBeforeTokenAcquisitionForApp event.
        /// </summary>
        internal async Task InvokeOnBeforeTokenAcquisitionForOnBehalfOfAsync(AcquireTokenOnBehalfOfParameterBuilder builder,
                                                           AcquireTokenOptions? acquireTokenOptions,
                                                           OnBehalfOfEventArgs eventArgs)
        {
            // Run the async event if it is not null
            if (OnBeforeTokenAcquisitionForOnBehalfOfAsync != null)
            {
                // (cannot directly await an async event because events are not tasks
                // they are multicast delegates that invoke handlers, but don't return values to the publisher,
                // nor do they support awaiting natively
                var invocationList = OnBeforeTokenAcquisitionForOnBehalfOfAsync.GetInvocationList();
                var tasks = invocationList
                    .Cast<BeforeTokenAcquisitionForOnBehalfOfAsync>()
                    .Select(handler => handler(builder, acquireTokenOptions, eventArgs));
                await Task.WhenAll(tasks);
            }

            // Run the sync event if it is not null.
            OnBeforeTokenAcquisitionForOnBehalfOf?.Invoke(builder, acquireTokenOptions, eventArgs);
        }

        /// <summary>
        /// Invoke the OnBeforeOnBehalfOfInitializedAsync event.
        /// </summary>
        internal async Task InvokeOnBeforeOnBehalfOfInitializedAsync(OnBehalfOfEventArgs eventArgs)
        {
            // Run the async event if it is not null
            if (OnBeforeOnBehalfOfInitializedAsync != null)
            {
                // (cannot directly await an async event because events are not tasks
                // they are multicast delegates that invoke handlers, but don't return values to the publisher,
                // nor do they support awaiting natively
                var invocationList = OnBeforeOnBehalfOfInitializedAsync.GetInvocationList();
                var tasks = invocationList
                    .Cast<BeforeOnBehalfOfInitializedAsync>()
                    .Select(handler => handler(eventArgs));
                await Task.WhenAll(tasks);
            }

            // Run the sync event if it is not null.
            OnBeforeOnBehalfOfInitialized?.Invoke(eventArgs);
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
