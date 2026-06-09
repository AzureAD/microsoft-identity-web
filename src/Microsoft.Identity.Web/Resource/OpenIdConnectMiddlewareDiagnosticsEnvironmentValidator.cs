// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Hosted service that fails fast on application startup if the OpenID Connect
    /// middleware diagnostics were enabled in a non-Development environment.
    /// </summary>
    /// <remarks>
    /// <see cref="OpenIdConnectMiddlewareDiagnostics"/> writes full OpenID Connect
    /// protocol messages (including bearer tokens, codes, and PII) to the logger at
    /// Debug level. The validator is only registered when the caller passes
    /// <c>subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: true</c>, so a
    /// successful registration outside Development means a production deployment is
    /// about to leak credentials. Throwing in <see cref="StartAsync"/> aborts host
    /// startup before any HTTP request can trigger the diagnostic.
    /// </remarks>
    internal sealed class OpenIdConnectMiddlewareDiagnosticsEnvironmentValidator : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public OpenIdConnectMiddlewareDiagnosticsEnvironmentValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            IHostEnvironment? environment = _serviceProvider.GetService<IHostEnvironment>();
            if (environment is null || !environment.IsDevelopment())
            {
                throw new InvalidOperationException(IDWebErrorMessage.OpenIdConnectMiddlewareDiagnosticsRequiresDevelopmentEnvironment);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
