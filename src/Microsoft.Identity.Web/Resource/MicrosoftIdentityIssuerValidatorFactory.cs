// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            AadIssuerValidatorOptions = aadIssuerValidatorOptions;
            HttpClientFactory = httpClientFactory;
        }

        private readonly IDictionary<string, AadIssuerValidator> _issuerValidators = new ConcurrentDictionary<string, AadIssuerValidator>();

        private IOptions<AadIssuerValidatorOptions> AadIssuerValidatorOptions { get; }
        private IHttpClientFactory HttpClientFactory { get; }

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

            _issuerValidators[authorityHost] = new AadIssuerValidator(
                AadIssuerValidatorOptions,
                HttpClientFactory,
                aadAuthority);

            return _issuerValidators[authorityHost];
        }
    }
}
