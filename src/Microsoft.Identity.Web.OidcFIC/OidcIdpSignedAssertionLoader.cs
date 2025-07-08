// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web.OidcFic
{
    internal class OidcIdpSignedAssertionLoader : ICustomSignedAssertionProvider
    {
        private readonly ILogger<OidcIdpSignedAssertionLoader> _logger;
        private readonly IOptionsMonitor<MicrosoftIdentityApplicationOptions> _options;
        private readonly IConfiguration _configuration;
        private readonly ITokenAcquirerFactory _tokenAcquirerFactory;

        public OidcIdpSignedAssertionLoader(ILogger<OidcIdpSignedAssertionLoader> logger,
            IOptionsMonitor<MicrosoftIdentityApplicationOptions> options,
            IConfiguration configuration,
            ITokenAcquirerFactory tokenAcquirerFactory)
        {
            _logger = logger;
            _options = options;
            _configuration = configuration;
            _tokenAcquirerFactory = tokenAcquirerFactory;
        }

        public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;

        public string Name => "OidcIdpSignedAssertion";


        public async Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
        {
            OidcIdpSignedAssertionProvider? signedAssertion = credentialDescription.CachedValue as OidcIdpSignedAssertionProvider;
            if (credentialDescription.CachedValue == null)
            {
                if (credentialDescription.CustomSignedAssertionProviderData == null)
                {
                    _logger.CustomSignedAssertionProviderDataIsNull();
                    throw new InvalidOperationException("CustomSignedAssertionProviderData is null");
                }

                string? sectionName = credentialDescription.CustomSignedAssertionProviderData["ConfigurationSection"] as string;
                if (sectionName == null)
                {
                    _logger.ConfigurationSectionIsNull();
                    throw new InvalidOperationException("ConfigurationSection is null");
                }

                MicrosoftIdentityApplicationOptions microsoftIdentityApplicationOptions = _options.Get(sectionName);
                
                if (string.IsNullOrEmpty(microsoftIdentityApplicationOptions.Instance) && microsoftIdentityApplicationOptions.Authority == "//v2.0")
                {
                    _configuration.GetSection(sectionName).Bind(microsoftIdentityApplicationOptions);
                }

                // Special case for Signed assertions with an FmiPath.
                // The provider needs to postpone getting the signed assertion until the first call, when ClientAssertionFmiPath will be provided.
                signedAssertion = new OidcIdpSignedAssertionProvider(_tokenAcquirerFactory, microsoftIdentityApplicationOptions, credentialDescription.TokenExchangeUrl, _logger);
                if (credentialDescription.CustomSignedAssertionProviderData.TryGetValue("RequiresSignedAssertionFmiPath", out object? requiresSignedAssertionFmiPathObj) && requiresSignedAssertionFmiPathObj is bool requiresSignedAssertionFmiPathBool && requiresSignedAssertionFmiPathBool)
                {
                    signedAssertion.RequiresSignedAssertionFmiPath = true;
                }
            }

            try
            {
                // Try to get a signed assertion, and if it fails, move to the next credentials
                _ = await signedAssertion!.GetSignedAssertionAsync(null);
                credentialDescription.CachedValue = signedAssertion;
            }
            catch (Exception ex)
            {
                _logger.FailedToGetSignedAssertion(credentialDescription.CustomSignedAssertionProviderName, ex.Message, ex);
                credentialDescription.Skip = true;
                throw;
            }
        }
    }
}
