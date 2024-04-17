// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class DefaultTokenAcquirerFactoryImplementation : ITokenAcquirerFactory
    {
        public DefaultTokenAcquirerFactoryImplementation(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        private IServiceProvider ServiceProvider { get; set; }

        readonly ConcurrentDictionary<string, ITokenAcquirer> _authSchemes = new();

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(
            string authority,
            string clientId,
            IEnumerable<CredentialDescription> clientCredentials,
            string? region = null)
        {
            string key = GetKey(authority, clientId, region);

            // GetOrAdd ONLY synchronizes the outcome. So, the factory might still be invoked multiple times.
            // Therefore, all side-effects within this block must remain idempotent.
            return _authSchemes.GetOrAdd(key, (key) =>
                {
                    MicrosoftIdentityApplicationOptions MicrosoftIdentityApplicationOptions = new()
                    {
                        ClientId = clientId,
                        Authority = authority,
                        ClientCredentials = clientCredentials,
                        SendX5C = true
                    };

                    if (region != null)
                    {
                        MicrosoftIdentityApplicationOptions.AzureRegion = region;
                    }

                    IMergedOptionsStore optionsMonitor = ServiceProvider.GetRequiredService<IMergedOptionsStore>();
                    MergedOptions mergedOptions = optionsMonitor.Get(key);
                    MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(MicrosoftIdentityApplicationOptions, mergedOptions);

                    return MakeTokenAcquirer(key);
                });
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(IdentityApplicationOptions IdentityApplicationOptions)
        {
            _ = Throws.IfNull(IdentityApplicationOptions);

            // Compute the Azure region if the option is a MicrosoftIdentityApplicationOptions.
            MicrosoftIdentityApplicationOptions? MicrosoftIdentityApplicationOptions = IdentityApplicationOptions as MicrosoftIdentityApplicationOptions;
            if (MicrosoftIdentityApplicationOptions == null)
            {
                MicrosoftIdentityApplicationOptions = new MicrosoftIdentityApplicationOptions
                {
                    AllowWebApiToBeAuthorizedByACL = IdentityApplicationOptions.AllowWebApiToBeAuthorizedByACL,
                    Audience = IdentityApplicationOptions.Audience,
                    Audiences = IdentityApplicationOptions.Audiences,
                    Authority = IdentityApplicationOptions.Authority,
                    ClientCredentials = IdentityApplicationOptions.ClientCredentials,
                    ClientId = IdentityApplicationOptions.ClientId,
                    TokenDecryptionCredentials = IdentityApplicationOptions.TokenDecryptionCredentials,
                    EnablePiiLogging = IdentityApplicationOptions.EnablePiiLogging,
                };
            }

            string key = GetKey(IdentityApplicationOptions.Authority, IdentityApplicationOptions.ClientId, MicrosoftIdentityApplicationOptions.AzureRegion);

            return _authSchemes.GetOrAdd(key, (key) =>
            {
                IMergedOptionsStore optionsMonitor = ServiceProvider!.GetRequiredService<IMergedOptionsStore>();
                MergedOptions mergedOptions = optionsMonitor.Get(key);

       
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(MicrosoftIdentityApplicationOptions, mergedOptions);
                return MakeTokenAcquirer(key);
            });
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(string authenticationScheme = "")
        {
            return _authSchemes.GetOrAdd(authenticationScheme, (key) =>
            {
                return MakeTokenAcquirer(authenticationScheme);
            });
        }

        private ITokenAcquirer MakeTokenAcquirer(string authenticationScheme = "")
        {
            CheckServiceProviderNotNull();

            ITokenAcquisition tokenAcquisition = ServiceProvider!.GetRequiredService<ITokenAcquisition>();
            return new TokenAcquirer(tokenAcquisition, authenticationScheme);
        }

        private void CheckServiceProviderNotNull()
        {
            if (ServiceProvider == null)
            {
                throw new ArgumentOutOfRangeException("You need to call ITokenAcquirerFactory.Build() before using GetTokenAcquirer.");
            }
        }

        public static string GetKey(string? authority, string? clientId, string? region)
        {
            return $"{authority}{clientId}{region}";
        }
    }
}
