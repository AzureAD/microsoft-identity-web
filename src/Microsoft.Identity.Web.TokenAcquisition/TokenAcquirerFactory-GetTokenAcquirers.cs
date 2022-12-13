// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal sealed class TokenAcquirerFactory_GetTokenAcquirers
    {
        public TokenAcquirerFactory_GetTokenAcquirers(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        private IServiceProvider ServiceProvider { get; set; }

        readonly Dictionary<string, ITokenAcquirer> _authSchemes = new Dictionary<string, ITokenAcquirer>();

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(
            string authority,
            string clientId,
            IEnumerable<CredentialDescription> clientCredentials,
            string? region = null)
        {
            CheckServiceProviderNotNull();

            ITokenAcquirer? tokenAcquirer;
            // Compute the key
            string key = GetKey(authority, clientId);
            if (!_authSchemes.TryGetValue(key, out tokenAcquirer))
            {
                MicrosoftIdentityApplicationOptions MicrosoftIdentityApplicationOptions = new MicrosoftIdentityApplicationOptions
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

                var optionsMonitor = ServiceProvider.GetRequiredService<IMergedOptionsStore>();
                var mergedOptions = optionsMonitor.Get(key);
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(MicrosoftIdentityApplicationOptions, mergedOptions);
                tokenAcquirer = GetTokenAcquirer(key);
            }
            return tokenAcquirer;
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(IdentityApplicationOptions IdentityApplicationOptions)
        {
            _ = Throws.IfNull(IdentityApplicationOptions);

            CheckServiceProviderNotNull();

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

            // Compute the key
            ITokenAcquirer? tokenAcquirer;
            string key = GetKey(IdentityApplicationOptions.Authority, IdentityApplicationOptions.ClientId);
            if (!_authSchemes.TryGetValue(key, out tokenAcquirer))
            {
                var optionsMonitor = ServiceProvider!.GetRequiredService<IMergedOptionsStore>();
                var mergedOptions = optionsMonitor.Get(key);
                MergedOptions.UpdateMergedOptionsFromMicrosoftIdentityApplicationOptions(MicrosoftIdentityApplicationOptions, mergedOptions);
                tokenAcquirer = GetTokenAcquirer(key);
            }
            return tokenAcquirer;
        }

        /// <inheritdoc/>
        public ITokenAcquirer GetTokenAcquirer(string authenticationScheme = "")
        {
            CheckServiceProviderNotNull();

            ITokenAcquirer? acquirer;
            if (!_authSchemes.TryGetValue(authenticationScheme, out acquirer))
            {
                var tokenAcquisition = ServiceProvider!.GetRequiredService<ITokenAcquisition>();
                acquirer = new TokenAcquirer(tokenAcquisition, authenticationScheme);
                _authSchemes.Add(authenticationScheme, acquirer);
            }
            return acquirer;
        }
        private void CheckServiceProviderNotNull()
        {
            if (ServiceProvider == null)
            {
                throw new ArgumentOutOfRangeException("You need to call ITokenAcquirerFactory.Build() before using GetTokenAcquirer.");
            }
        }


        private static string GetKey(string? authority, string? clientId)
        {
            return $"{authority}{clientId}";
        }
    }
}
