// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.Identity.Web.Resource
{
    /// <summary>
    /// Factory class for creating the IssuerValidator per authority.
    /// </summary>
    public class MicrosoftIdentityIssuerValidatorFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftIdentityIssuerValidatorFactory"/> class.
        /// </summary>
        /// <param name="aadIssuerValidatorOptions">Options passed-in to create the AadIssuerValidator object.</param>
        /// <param name="httpClientFactory">HttpClientFactory.</param>
        public MicrosoftIdentityIssuerValidatorFactory(
            IOptions<AadIssuerValidatorOptions> aadIssuerValidatorOptions,
            IHttpClientFactory httpClientFactory)
        {
            if (aadIssuerValidatorOptions is null)
            {
                throw new ArgumentNullException(nameof(aadIssuerValidatorOptions));
            }

            if (httpClientFactory is null)
            {
                throw new ArgumentNullException(nameof(httpClientFactory));
            }

            AadIssuerValidatorFactory = new AadIssuerValidatorFactory(GetHttpClient(aadIssuerValidatorOptions, httpClientFactory));
        }

        private AadIssuerValidatorFactory AadIssuerValidatorFactory { get; set; }

        private static HttpClient? GetHttpClient(
            IOptions<AadIssuerValidatorOptions> aadIssuerValidatorOptions,
            IHttpClientFactory httpClientFactory)
        {
            if (!string.IsNullOrEmpty(aadIssuerValidatorOptions.Value.HttpClientName))
            {
                return httpClientFactory.CreateClient(aadIssuerValidatorOptions.Value.HttpClientName);
            }

            return null;
        }

        /// <summary>
        /// Gets an <see cref="AadIssuerValidator"/> for an authority.
        /// </summary>
        /// <param name="aadAuthority">The authority to create the validator for, e.g. https://login.microsoftonline.com/.</param>
        /// <returns>A <see cref="AadIssuerValidator"/> for the aadAuthority.</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="aadAuthority"/> is null or empty.</exception>
        public AadIssuerValidator GetAadIssuerValidator(string aadAuthority)
        {
            return AadIssuerValidatorFactory.GetAadIssuerValidator(aadAuthority);
        }
    }
}
