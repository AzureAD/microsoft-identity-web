// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.InstanceDiscovery;
using Microsoft.IdentityModel.Protocols;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Factory class for creating the IssuerValidator per authority.
    /// </summary>
    internal class MicrosoftIdentityIssuerValidatorFactory
    {
        public MicrosoftIdentityIssuerValidatorFactory(
            IOptions<AadIssuerValidatorOptions> aadIssuerValidatorOptions,
            IHttpClientFactory httpClientFactory)
        {
            _configManager =
            new ConfigurationManager<IssuerMetadata>(
                Constants.AzureADIssuerMetadataUrl,
                new IssuerConfigurationRetriever(),
                httpClientFactory?.CreateClient(aadIssuerValidatorOptions?.Value?.HttpClientFactoryName));
        }

        private readonly IDictionary<string, AadIssuerValidator> _issuerValidators = new ConcurrentDictionary<string, AadIssuerValidator>();

        private readonly ConfigurationManager<IssuerMetadata> _configManager;

        /// <summary>
        /// Gets an <see cref="AadIssuerValidator"/> for an authority.
        /// </summary>
        /// <param name="aadAuthority">The authority to create the validator for, e.g. https://login.microsoftonline.com/. </param>
        /// <returns>A <see cref="AadIssuerValidator"/> for the aadAuthority.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="aadAuthority"/> is null or empty.</exception>
        public AadIssuerValidator GetAadIssuerValidator(string aadAuthority)
        {
            if (string.IsNullOrEmpty(aadAuthority))
            {
                throw new ArgumentNullException(nameof(aadAuthority));
            }

            Uri.TryCreate(aadAuthority, UriKind.Absolute, out Uri? authorityUri);
            string authorityHost = authorityUri?.Authority ?? new Uri(Constants.FallbackAuthority).Authority;

            if (_issuerValidators.TryGetValue(authorityHost, out AadIssuerValidator? aadIssuerValidator))
            {
                return aadIssuerValidator;
            }

            // In the constructor, we hit the Azure AD issuer metadata endpoint and cache the aliases. The data is cached for 24 hrs.
            IssuerMetadata issuerMetadata = _configManager.GetConfigurationAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            // Add issuer aliases of the chosen authority to the cache
            IEnumerable<string> aliases = issuerMetadata.Metadata
                .Where(m => m.Aliases.Any(a => string.Equals(a, authorityHost, StringComparison.OrdinalIgnoreCase)))
                .SelectMany(m => m.Aliases)
                .Append(authorityHost) // For B2C scenarios, the alias will be the authority itself
                .Distinct();
            _issuerValidators[authorityHost] = new AadIssuerValidator(aliases);

            return _issuerValidators[authorityHost];
        }
    }
}
