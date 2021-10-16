// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;

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
        /// <param name="httpClient">HttpClientFactory.</param>
        public MicrosoftIdentityIssuerValidatorFactory(
            HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        private readonly IDictionary<string, AadIssuerValidator> _issuerValidators = new ConcurrentDictionary<string, AadIssuerValidator>();

        private HttpClient HttpClient { get; }

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
            string authorityHost = authorityUri?.Authority ?? new Uri(IssuerValidatorConstants.FallbackAuthority).Authority;

            if (_issuerValidators.TryGetValue(authorityHost, out AadIssuerValidator? aadIssuerValidator))
            {
                return aadIssuerValidator;
            }

            _issuerValidators[authorityHost] = new AadIssuerValidator(
                HttpClient,
                aadAuthority);

            return _issuerValidators[authorityHost];
        }
    }
}
