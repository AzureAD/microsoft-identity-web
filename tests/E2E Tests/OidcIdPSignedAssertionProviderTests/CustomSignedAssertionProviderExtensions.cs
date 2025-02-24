// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;

namespace CustomSignedAssertionProviderTests
{
    public static class CustomSignedAssertionProviderExtensions
    {
        public static IServiceCollection AddOidcSignedAssertionProvider(this IServiceCollection services)
        {
            services.AddSingleton<ICustomSignedAssertionProvider, OidcIdpSignedAssertionLoader>();
            return services;
        }
    }
}
