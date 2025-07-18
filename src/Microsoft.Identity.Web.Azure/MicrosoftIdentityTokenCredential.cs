// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
	/// <summary>
	/// Azure SDK token credential for tokens based on the <see cref="IAuthorizationHeaderProvider"/>
    /// service.
	/// </summary>
	public class MicrosoftIdentityTokenCredential : TokenCredential
	{
		private ITokenAcquirerFactory _tokenAcquirerFactory;
        private readonly IAuthenticationSchemeInformationProvider _authenticationSchemeInformationProvider;

        /// <summary>
        /// Constructor from an ITokenAcquisition service.
        /// </summary>
        /// <param name="tokenAcquirerFactory">Token acquisition factory</param>
        /// <param name="authenticationSchemeInformationProvider">Host for the token acquisition</param>
        public MicrosoftIdentityTokenCredential(ITokenAcquirerFactory tokenAcquirerFactory, IAuthenticationSchemeInformationProvider authenticationSchemeInformationProvider)
		{
			_tokenAcquirerFactory = tokenAcquirerFactory ?? throw new System.ArgumentNullException(nameof(tokenAcquirerFactory));
            _authenticationSchemeInformationProvider = authenticationSchemeInformationProvider ?? throw new System.ArgumentNullException(nameof(authenticationSchemeInformationProvider));
        }

		AuthorizationHeaderProviderOptions _options = new AuthorizationHeaderProviderOptions();

        /// <summary>
        /// Options used to configure the token acquisition behavior.
        /// </summary>
		public AuthorizationHeaderProviderOptions Options => _options;

		/// <inheritdoc/>
		public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
		{
			ITokenAcquirer tokenAcquirer = _tokenAcquirerFactory.GetTokenAcquirer(_authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(_options.AcquireTokenOptions.AuthenticationOptionsName));
			if (Options.RequestAppToken)
			{
				AcquireTokenResult result = tokenAcquirer.GetTokenForAppAsync(requestContext.Scopes.First(), cancellationToken: cancellationToken)
					.GetAwaiter()
					.GetResult();
				return new AccessToken(result.AccessToken!, result.ExpiresOn);
			}
			else
			{
				AcquireTokenResult result = tokenAcquirer.GetTokenForUserAsync(requestContext.Scopes, Options.AcquireTokenOptions, cancellationToken: cancellationToken)
					.GetAwaiter()
					.GetResult();
				return new AccessToken(result.AccessToken!, result.ExpiresOn);
			}
		}

		/// <inheritdoc/>
		public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
		{
            ITokenAcquirer tokenAcquirer = _tokenAcquirerFactory.GetTokenAcquirer(_authenticationSchemeInformationProvider.GetEffectiveAuthenticationScheme(_options.AcquireTokenOptions.AuthenticationOptionsName));
            if (Options.RequestAppToken)
			{
				AcquireTokenResult result = await tokenAcquirer.GetTokenForAppAsync(requestContext.Scopes.First(), cancellationToken: cancellationToken);
				return new AccessToken(result.AccessToken!, result.ExpiresOn);
			}

        AcquireTokenResult result = await tokenAcquirer.GetTokenForUserAsync(requestContext.Scopes, Options.AcquireTokenOptions, cancellationToken: cancellationToken);
		return new AccessToken(result.AccessToken!, result.ExpiresOn);
		}
	}
}
